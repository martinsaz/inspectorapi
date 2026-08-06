using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using checklistWs.Models.OrdenesCompra;
using checklistWs.Utiles;
using Microsoft.AspNetCore.Mvc;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace checklistWs.Controllers.OrdenesCompra
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdenesCompraController : ControllerBase
    {
        private const byte EstadoBorrador = 1;
        private const byte EstadoGenerada = 2;
        private const byte EstadoCancelada = 3;
        private const byte TipoProducto = 1;
        private const byte TipoServicio = 2;
        private const int FolioPadding = 6;
        private const int ObservacionesLength = 1000;
        private const int MotivoCancelacionLength = 500;
        private const int BusquedaLength = 200;
        private const int ExportacionLimit = 5000;
        private static readonly TimeSpan ProxyHeaderTolerance = TimeSpan.FromMinutes(5);
        private static readonly string[] EmpresaClaimKeys = new[] { "idEmpresa", "empresaId", "tenantId", "companyId", "tenant", "idempresa" };
        private static readonly string[] EmpresaNombreClaimKeys = new[] { "empresa", "empresaNombre", "tenantName", "companyName", "nombreEmpresa" };
        private static readonly string[] UsuarioClaimKeys = new[] { ClaimTypes.NameIdentifier, "sub", "idUsuario", "userid", "uid" };
        private const string ProxyEmpresaIdHeader = "X-ProductosServicios-Proxy-EmpresaId";
        private const string ProxyEmpresaKeyHeader = "X-ProductosServicios-Proxy-Empresa";
        private const string ProxyUsuarioIdHeader = "X-ProductosServicios-Proxy-UsuarioId";
        private const string ProxyTimestampHeader = "X-ProductosServicios-Proxy-Timestamp";
        private const string ProxySignatureHeader = "X-ProductosServicios-Proxy-Signature";
        private const string ProxyContextItemKey = "__OrdenesCompraProxyContext";

        private readonly IConfiguration _configuration;
        private readonly SqlConnectionFactory _connectionFactory;
        private readonly ILogger<OrdenesCompraController> _logger;

        public OrdenesCompraController(IConfiguration configuration, ILogger<OrdenesCompraController> logger)
        {
            _configuration = configuration;
            _connectionFactory = new SqlConnectionFactory(configuration);
            _logger = logger;
        }

        [HttpGet("ObtenerOrdenesCompra")]
        public async Task<IActionResult> ObtenerOrdenesCompra(
            Guid idEmpresa,
            string busqueda = "",
            byte? estado = null,
            Guid? idProveedor = null,
            Guid? idRazonSocial = null,
            Guid? idSucursal = null,
            DateTime? fechaDesde = null,
            DateTime? fechaHasta = null)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                using SqlConnection connection = CreateConnection();
                await connection.OpenAsync();

                StringBuilder query = new StringBuilder(@"
SELECT
    oc.id,
    oc.Folio,
    oc.FechaOrden,
    oc.FechaLlegada,
    oc.idRazonSocial,
    ISNULL(rs.Nombre, '') AS RazonSocial,
    oc.idSucursal,
    ISNULL(su.Nombre, '') AS Sucursal,
    oc.idProveedor,
    ISNULL(pr.Nombre, '') AS Proveedor,
    oc.Estado,
    oc.Total,
    oc.FechaCreacion
FROM dbo.OrdenesCompra oc
LEFT JOIN dbo.RazonesSociales rs
    ON rs.id = oc.idRazonSocial AND rs.idEmpresa = oc.idEmpresa
LEFT JOIN dbo.Sucursales su
    ON su.id = oc.idSucursal AND su.idEmpresa = oc.idEmpresa
LEFT JOIN dbo.ActivosProveedores pr
    ON pr.id = oc.idProveedor AND pr.idEmpresa = oc.idEmpresa
WHERE oc.idEmpresa = @IdEmpresa
  AND oc.Activo = 1
  AND oc.FechaArchivado IS NULL");

                using SqlCommand command = new SqlCommand();
                command.Connection = connection;
                command.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);

                if (!string.IsNullOrWhiteSpace(busqueda))
                {
                    query.Append(@"
  AND (
        ISNULL(oc.Folio, '') LIKE @Busqueda
        OR ISNULL(rs.Nombre, '') LIKE @Busqueda
        OR ISNULL(su.Nombre, '') LIKE @Busqueda
        OR ISNULL(pr.Nombre, '') LIKE @Busqueda
        OR ISNULL(oc.Observaciones, '') LIKE @Busqueda
      )");
                    command.Parameters.AddWithValue("@Busqueda", $"%{Truncate(busqueda, BusquedaLength)}%");
                }

                AppendTinyIntFilter(query, command, "oc.Estado", "@Estado", estado);
                AppendGuidFilter(query, command, "oc.idProveedor", "@IdProveedor", idProveedor);
                AppendGuidFilter(query, command, "oc.idRazonSocial", "@IdRazonSocial", idRazonSocial);
                AppendGuidFilter(query, command, "oc.idSucursal", "@IdSucursal", idSucursal);
                AppendFechaDesdeFilter(query, command, "oc.FechaOrden", "@FechaDesde", fechaDesde);
                AppendFechaHastaFilter(query, command, "oc.FechaOrden", "@FechaHasta", fechaHasta);

                query.Append(" ORDER BY oc.FechaOrden DESC, oc.FechaCreacion DESC");
                command.CommandText = query.ToString();

                List<OrdenCompraListadoDto> items = new List<OrdenCompraListadoDto>();
                using SqlDataReader reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    byte estadoActual = ReadByte(reader, "Estado");
                    items.Add(new OrdenCompraListadoDto
                    {
                        Id = ReadGuid(reader, "id"),
                        Folio = ReadString(reader, "Folio"),
                        FechaOrden = ReadDateTime(reader, "FechaOrden"),
                        FechaLlegada = ReadNullableDateTime(reader, "FechaLlegada"),
                        IdRazonSocial = ReadGuid(reader, "idRazonSocial"),
                        RazonSocial = ReadString(reader, "RazonSocial"),
                        IdSucursal = ReadGuid(reader, "idSucursal"),
                        Sucursal = ReadString(reader, "Sucursal"),
                        IdProveedor = ReadGuid(reader, "idProveedor"),
                        Proveedor = ReadString(reader, "Proveedor"),
                        Estado = estadoActual,
                        EstadoNombre = GetEstadoNombre(estadoActual),
                        Total = ReadDecimal(reader, "Total"),
                        FechaCreacion = ReadDateTime(reader, "FechaCreacion"),
                        PuedeEditar = estadoActual == EstadoBorrador,
                        PuedeGenerar = estadoActual == EstadoBorrador,
                        PuedeCancelar = estadoActual == EstadoBorrador || estadoActual == EstadoGenerada
                    });
                }

                return Ok(items);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerOrdenesCompra", "Ocurrió un error al procesar la solicitud.");
            }
        }

        [HttpGet("ObtenerOrdenCompra")]
        public async Task<IActionResult> ObtenerOrdenCompra(Guid idEmpresa, Guid idOrdenCompra)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                using SqlConnection connection = CreateConnection();
                await connection.OpenAsync();

                using SqlCommand command = new SqlCommand(@"
SELECT
    oc.id,
    oc.idEmpresa,
    oc.identityKey,
    ISNULL(oc.Folio, '') AS Folio,
    oc.idRazonSocial,
    ISNULL(rs.Nombre, '') AS RazonSocial,
    oc.idSucursal,
    ISNULL(su.Nombre, '') AS Sucursal,
    oc.idProveedor,
    ISNULL(pr.Nombre, '') AS Proveedor,
    oc.FechaOrden,
    oc.FechaLlegada,
    oc.Estado,
    ISNULL(oc.Observaciones, '') AS Observaciones,
    oc.Subtotal,
    oc.Total,
    ISNULL(oc.MotivoCancelacion, '') AS MotivoCancelacion,
    oc.FechaCancelacion,
    oc.idUsuarioCreacion,
    oc.idUsuarioActualizacion,
    oc.idUsuarioCancelacion,
    oc.FechaCreacion,
    oc.FechaActualizacion
FROM dbo.OrdenesCompra oc
LEFT JOIN dbo.RazonesSociales rs
    ON rs.id = oc.idRazonSocial AND rs.idEmpresa = oc.idEmpresa
LEFT JOIN dbo.Sucursales su
    ON su.id = oc.idSucursal AND su.idEmpresa = oc.idEmpresa
LEFT JOIN dbo.ActivosProveedores pr
    ON pr.id = oc.idProveedor AND pr.idEmpresa = oc.idEmpresa
WHERE oc.idEmpresa = @IdEmpresa
  AND oc.id = @IdOrdenCompra
  AND oc.Activo = 1
  AND oc.FechaArchivado IS NULL", connection);

                command.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);
                command.Parameters.AddWithValue("@IdOrdenCompra", idOrdenCompra);

                OrdenCompraDetalleDto? detalle = null;
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (!await reader.ReadAsync())
                    {
                        return NotFound(new OrdenCompraOperacionResponse { Mensaje = "La orden de compra no está disponible." });
                    }

                    byte estadoActual = ReadByte(reader, "Estado");
                    detalle = new OrdenCompraDetalleDto
                    {
                        Id = ReadGuid(reader, "id"),
                        IdEmpresa = ReadGuid(reader, "idEmpresa"),
                        IdentityKey = ReadGuid(reader, "identityKey"),
                        Folio = ReadString(reader, "Folio"),
                        IdRazonSocial = ReadGuid(reader, "idRazonSocial"),
                        RazonSocial = ReadString(reader, "RazonSocial"),
                        IdSucursal = ReadGuid(reader, "idSucursal"),
                        Sucursal = ReadString(reader, "Sucursal"),
                        IdProveedor = ReadGuid(reader, "idProveedor"),
                        Proveedor = ReadString(reader, "Proveedor"),
                        FechaOrden = ReadDateTime(reader, "FechaOrden"),
                        FechaLlegada = ReadNullableDateTime(reader, "FechaLlegada"),
                        Estado = estadoActual,
                        EstadoNombre = GetEstadoNombre(estadoActual),
                        Observaciones = ReadString(reader, "Observaciones"),
                        Subtotal = ReadDecimal(reader, "Subtotal"),
                        Total = ReadDecimal(reader, "Total"),
                        MotivoCancelacion = ReadString(reader, "MotivoCancelacion"),
                        FechaCancelacion = ReadNullableDateTime(reader, "FechaCancelacion"),
                        IdUsuarioCreacion = ReadNullableGuid(reader, "idUsuarioCreacion"),
                        IdUsuarioActualizacion = ReadNullableGuid(reader, "idUsuarioActualizacion"),
                        IdUsuarioCancelacion = ReadNullableGuid(reader, "idUsuarioCancelacion"),
                        FechaCreacion = ReadDateTime(reader, "FechaCreacion"),
                        FechaActualizacion = ReadDateTime(reader, "FechaActualizacion")
                    };
                }

                detalle.Partidas = await ObtenerPartidasAsync(connection, context.IdEmpresa, idOrdenCompra);
                return Ok(detalle);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerOrdenCompra", "Ocurrió un error al procesar la solicitud.");
            }
        }

        [HttpPost("GuardarBorradorOrdenCompra")]
        public async Task<IActionResult> GuardarBorradorOrdenCompra([FromBody] OrdenCompraGuardarRequest request, Guid idEmpresa)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                string validacion = ValidateGuardarRequest(request, context.IdEmpresa);
                if (!string.IsNullOrWhiteSpace(validacion))
                {
                    return BadRequest(new OrdenCompraOperacionResponse { Mensaje = validacion });
                }

                Guid? usuarioId = TryResolveUsuarioId();
                DateTime utcNow = DateTime.UtcNow;

                using SqlConnection connection = CreateConnection();
                await connection.OpenAsync();

                using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable);

                try
                {
                    await ValidateEncabezadoCatalogosAsync(connection, transaction, context.IdEmpresa, request.IdRazonSocial, request.IdSucursal, request.IdProveedor);

                    List<OrdenCompraPartidaPersistencia> partidas = await BuildValidatedPartidasAsync(
                        connection,
                        transaction,
                        context.IdEmpresa,
                        request.Partidas);

                    TotalesOrdenCompra totales = CalculateTotals(partidas);

                    OrdenCompraCabeceraPersistida ordenActual;
                    bool esAlta = !request.Id.HasValue || request.Id.Value == Guid.Empty;

                    if (esAlta)
                    {
                        Guid idOrdenCompra = Guid.NewGuid();
                        string folio = await ReserveNextFolioAsync(connection, transaction, context.IdEmpresa, utcNow);

                        using SqlCommand insertCommand = new SqlCommand(@"
INSERT INTO dbo.OrdenesCompra
(
    id,
    idEmpresa,
    identityKey,
    Folio,
    idRazonSocial,
    idSucursal,
    idProveedor,
    FechaOrden,
    FechaLlegada,
    Estado,
    Subtotal,
    Total,
    Observaciones,
    Activo,
    FechaCreacion,
    FechaActualizacion,
    idUsuarioCreacion,
    idUsuarioActualizacion
)
VALUES
(
    @Id,
    @IdEmpresa,
    @IdentityKey,
    @Folio,
    @IdRazonSocial,
    @IdSucursal,
    @IdProveedor,
    @FechaOrden,
    @FechaLlegada,
    @Estado,
    @Subtotal,
    @Total,
    @Observaciones,
    1,
    @FechaCreacion,
    @FechaActualizacion,
    @IdUsuarioCreacion,
    @IdUsuarioActualizacion
)", connection, transaction);

                        insertCommand.Parameters.AddWithValue("@Id", idOrdenCompra);
                        insertCommand.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);
                        insertCommand.Parameters.AddWithValue("@IdentityKey", Guid.NewGuid());
                        insertCommand.Parameters.AddWithValue("@Folio", folio);
                        insertCommand.Parameters.AddWithValue("@IdRazonSocial", request.IdRazonSocial);
                        insertCommand.Parameters.AddWithValue("@IdSucursal", request.IdSucursal);
                        insertCommand.Parameters.AddWithValue("@IdProveedor", request.IdProveedor);
                        insertCommand.Parameters.AddWithValue("@FechaOrden", request.FechaOrden);
                        insertCommand.Parameters.AddWithValue("@FechaLlegada", (object?)request.FechaLlegada ?? DBNull.Value);
                        insertCommand.Parameters.AddWithValue("@Estado", EstadoBorrador);
                        insertCommand.Parameters.AddWithValue("@Subtotal", totales.Subtotal);
                        insertCommand.Parameters.AddWithValue("@Total", totales.Total);
                        insertCommand.Parameters.AddWithValue("@Observaciones", (object?)NormalizeNullableText(request.Observaciones, ObservacionesLength) ?? DBNull.Value);
                        insertCommand.Parameters.AddWithValue("@FechaCreacion", utcNow);
                        insertCommand.Parameters.AddWithValue("@FechaActualizacion", utcNow);
                        insertCommand.Parameters.AddWithValue("@IdUsuarioCreacion", (object?)usuarioId ?? DBNull.Value);
                        insertCommand.Parameters.AddWithValue("@IdUsuarioActualizacion", (object?)usuarioId ?? DBNull.Value);
                        await insertCommand.ExecuteNonQueryAsync();

                        ordenActual = new OrdenCompraCabeceraPersistida
                        {
                            Id = idOrdenCompra,
                            Folio = folio,
                            Estado = EstadoBorrador,
                            Subtotal = totales.Subtotal,
                            Total = totales.Total
                        };
                    }
                    else
                    {
                        ordenActual = await GetOrdenCompraForUpdateAsync(connection, transaction, context.IdEmpresa, request.Id.Value);
                        if (ordenActual.Id == Guid.Empty)
                        {
                            transaction.Rollback();
                            return NotFound(new OrdenCompraOperacionResponse { Mensaje = "La orden de compra no está disponible." });
                        }

                        if (ordenActual.Estado != EstadoBorrador)
                        {
                            transaction.Rollback();
                            return BadRequest(new OrdenCompraOperacionResponse { Mensaje = "Solo se pueden editar órdenes en borrador." });
                        }

                        using SqlCommand archiveCommand = new SqlCommand(@"
UPDATE dbo.OrdenesCompraDetalle
SET Activo = 0,
    FechaArchivado = @FechaArchivado,
    FechaActualizacion = @FechaActualizacion
WHERE idEmpresa = @IdEmpresa
  AND idOrdenCompra = @IdOrdenCompra
  AND Activo = 1
  AND FechaArchivado IS NULL", connection, transaction);

                        archiveCommand.Parameters.AddWithValue("@FechaArchivado", utcNow);
                        archiveCommand.Parameters.AddWithValue("@FechaActualizacion", utcNow);
                        archiveCommand.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);
                        archiveCommand.Parameters.AddWithValue("@IdOrdenCompra", ordenActual.Id);
                        await archiveCommand.ExecuteNonQueryAsync();

                        using SqlCommand updateCommand = new SqlCommand(@"
UPDATE dbo.OrdenesCompra
SET idRazonSocial = @IdRazonSocial,
    idSucursal = @IdSucursal,
    idProveedor = @IdProveedor,
    FechaOrden = @FechaOrden,
    FechaLlegada = @FechaLlegada,
    Observaciones = @Observaciones,
    Subtotal = @Subtotal,
    Total = @Total,
    FechaActualizacion = @FechaActualizacion,
    idUsuarioActualizacion = @IdUsuarioActualizacion
WHERE idEmpresa = @IdEmpresa
  AND id = @IdOrdenCompra", connection, transaction);

                        updateCommand.Parameters.AddWithValue("@IdRazonSocial", request.IdRazonSocial);
                        updateCommand.Parameters.AddWithValue("@IdSucursal", request.IdSucursal);
                        updateCommand.Parameters.AddWithValue("@IdProveedor", request.IdProveedor);
                        updateCommand.Parameters.AddWithValue("@FechaOrden", request.FechaOrden);
                        updateCommand.Parameters.AddWithValue("@FechaLlegada", (object?)request.FechaLlegada ?? DBNull.Value);
                        updateCommand.Parameters.AddWithValue("@Observaciones", (object?)NormalizeNullableText(request.Observaciones, ObservacionesLength) ?? DBNull.Value);
                        updateCommand.Parameters.AddWithValue("@Subtotal", totales.Subtotal);
                        updateCommand.Parameters.AddWithValue("@Total", totales.Total);
                        updateCommand.Parameters.AddWithValue("@FechaActualizacion", utcNow);
                        updateCommand.Parameters.AddWithValue("@IdUsuarioActualizacion", (object?)usuarioId ?? DBNull.Value);
                        updateCommand.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);
                        updateCommand.Parameters.AddWithValue("@IdOrdenCompra", ordenActual.Id);
                        await updateCommand.ExecuteNonQueryAsync();

                        ordenActual.Subtotal = totales.Subtotal;
                        ordenActual.Total = totales.Total;
                    }

                    await InsertPartidasAsync(connection, transaction, context.IdEmpresa, ordenActual.Id, partidas, utcNow);

                    transaction.Commit();

                    return Ok(new OrdenCompraOperacionResponse
                    {
                        Exito = true,
                        Mensaje = esAlta
                            ? "La orden de compra fue guardada como borrador."
                            : "La orden de compra fue actualizada.",
                        IdOrdenCompra = ordenActual.Id,
                        Folio = ordenActual.Folio,
                        Estado = EstadoBorrador,
                        EstadoNombre = GetEstadoNombre(EstadoBorrador),
                        Subtotal = ordenActual.Subtotal,
                        Total = ordenActual.Total
                    });
                }
                catch
                {
                    if (transaction.Connection != null)
                    {
                        transaction.Rollback();
                    }

                    throw;
                }
            }
            catch (CatalogoValidationException ex)
            {
                return BadRequest(new OrdenCompraOperacionResponse { Mensaje = ex.PublicMessage });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "GuardarBorradorOrdenCompra", "Ocurrió un error al procesar la solicitud.");
            }
        }

        [HttpPost("GenerarOrdenCompra")]
        public async Task<IActionResult> GenerarOrdenCompra([FromBody] OrdenCompraGenerarRequest request, Guid idEmpresa)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            if (request == null || request.IdOrdenCompra == Guid.Empty)
            {
                return BadRequest(new OrdenCompraOperacionResponse { Mensaje = "La orden de compra no está disponible." });
            }

            if (request.IdEmpresa != Guid.Empty && request.IdEmpresa != context.IdEmpresa)
            {
                return BadRequest(new OrdenCompraOperacionResponse { Mensaje = "La empresa solicitada no coincide con la sesión activa." });
            }

            try
            {
                Guid? usuarioId = TryResolveUsuarioId();
                DateTime utcNow = DateTime.UtcNow;

                using SqlConnection connection = CreateConnection();
                await connection.OpenAsync();
                using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable);

                try
                {
                    OrdenCompraCabeceraPersistida ordenActual = await GetOrdenCompraForUpdateAsync(connection, transaction, context.IdEmpresa, request.IdOrdenCompra);
                    if (ordenActual.Id == Guid.Empty)
                    {
                        transaction.Rollback();
                        return NotFound(new OrdenCompraOperacionResponse { Mensaje = "La orden de compra no está disponible." });
                    }

                    if (ordenActual.Estado == EstadoGenerada)
                    {
                        transaction.Rollback();
                        return BadRequest(new OrdenCompraOperacionResponse { Mensaje = "La orden de compra ya fue generada." });
                    }

                    if (ordenActual.Estado == EstadoCancelada)
                    {
                        transaction.Rollback();
                        return BadRequest(new OrdenCompraOperacionResponse { Mensaje = "La orden de compra cancelada no puede modificarse." });
                    }

                    TotalesOrdenCompra totales = await ObtenerTotalesActivosAsync(connection, transaction, context.IdEmpresa, request.IdOrdenCompra);
                    int totalPartidas = await CountPartidasActivasAsync(connection, transaction, context.IdEmpresa, request.IdOrdenCompra);
                    int partidasSinCostoValido = await CountPartidasSinCostoValidoAsync(connection, transaction, context.IdEmpresa, request.IdOrdenCompra);

                    if (totalPartidas <= 0)
                    {
                        transaction.Rollback();
                        return BadRequest(new OrdenCompraOperacionResponse { Mensaje = "La orden debe contener al menos una partida." });
                    }

                    if (partidasSinCostoValido > 0)
                    {
                        transaction.Rollback();
                        return BadRequest(new OrdenCompraOperacionResponse { Mensaje = partidasSinCostoValido == 1
                            ? "La orden contiene 1 partida sin costo válido. Corrígela antes de generar."
                            : $"La orden contiene {partidasSinCostoValido} partidas sin costo válido. Corrígelas antes de generar." });
                    }

                    if (totales.Total <= 0m)
                    {
                        transaction.Rollback();
                        return BadRequest(new OrdenCompraOperacionResponse { Mensaje = "La orden debe tener un total mayor a cero para generarse." });
                    }

                    using SqlCommand command = new SqlCommand(@"
UPDATE dbo.OrdenesCompra
SET Estado = @Estado,
    Subtotal = @Subtotal,
    Total = @Total,
    FechaActualizacion = @FechaActualizacion,
    idUsuarioActualizacion = @IdUsuarioActualizacion
WHERE idEmpresa = @IdEmpresa
  AND id = @IdOrdenCompra", connection, transaction);

                    command.Parameters.AddWithValue("@Estado", EstadoGenerada);
                    command.Parameters.AddWithValue("@Subtotal", totales.Subtotal);
                    command.Parameters.AddWithValue("@Total", totales.Total);
                    command.Parameters.AddWithValue("@FechaActualizacion", utcNow);
                    command.Parameters.AddWithValue("@IdUsuarioActualizacion", (object?)usuarioId ?? DBNull.Value);
                    command.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);
                    command.Parameters.AddWithValue("@IdOrdenCompra", request.IdOrdenCompra);
                    await command.ExecuteNonQueryAsync();

                    transaction.Commit();

                    return Ok(new OrdenCompraOperacionResponse
                    {
                        Exito = true,
                        Mensaje = "La orden de compra fue generada.",
                        IdOrdenCompra = request.IdOrdenCompra,
                        Folio = ordenActual.Folio,
                        Estado = EstadoGenerada,
                        EstadoNombre = GetEstadoNombre(EstadoGenerada),
                        Subtotal = totales.Subtotal,
                        Total = totales.Total
                    });
                }
                catch
                {
                    if (transaction.Connection != null)
                    {
                        transaction.Rollback();
                    }

                    throw;
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex, "GenerarOrdenCompra", "Ocurrió un error al procesar la solicitud.");
            }
        }

        [HttpPost("CancelarOrdenCompra")]
        public async Task<IActionResult> CancelarOrdenCompra([FromBody] OrdenCompraCancelarRequest request, Guid idEmpresa)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            string motivo = NormalizeNullableText(request?.MotivoCancelacion, MotivoCancelacionLength) ?? string.Empty;
            if (request == null || request.IdOrdenCompra == Guid.Empty)
            {
                return BadRequest(new OrdenCompraOperacionResponse { Mensaje = "La orden de compra no está disponible." });
            }

            if (request.IdEmpresa != Guid.Empty && request.IdEmpresa != context.IdEmpresa)
            {
                return BadRequest(new OrdenCompraOperacionResponse { Mensaje = "La empresa solicitada no coincide con la sesión activa." });
            }

            if (string.IsNullOrWhiteSpace(motivo))
            {
                return BadRequest(new OrdenCompraOperacionResponse { Mensaje = "El motivo de cancelación es obligatorio." });
            }

            try
            {
                Guid? usuarioId = TryResolveUsuarioId();
                DateTime utcNow = DateTime.UtcNow;

                using SqlConnection connection = CreateConnection();
                await connection.OpenAsync();
                using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable);

                try
                {
                    OrdenCompraCabeceraPersistida ordenActual = await GetOrdenCompraForUpdateAsync(connection, transaction, context.IdEmpresa, request.IdOrdenCompra);
                    if (ordenActual.Id == Guid.Empty)
                    {
                        transaction.Rollback();
                        return NotFound(new OrdenCompraOperacionResponse { Mensaje = "La orden de compra no está disponible." });
                    }

                    if (ordenActual.Estado == EstadoCancelada)
                    {
                        transaction.Rollback();
                        return BadRequest(new OrdenCompraOperacionResponse { Mensaje = "La orden de compra ya fue cancelada." });
                    }

                    using SqlCommand command = new SqlCommand(@"
UPDATE dbo.OrdenesCompra
SET Estado = @Estado,
    MotivoCancelacion = @MotivoCancelacion,
    FechaCancelacion = @FechaCancelacion,
    FechaActualizacion = @FechaActualizacion,
    idUsuarioActualizacion = @IdUsuarioActualizacion,
    idUsuarioCancelacion = @IdUsuarioCancelacion
WHERE idEmpresa = @IdEmpresa
  AND id = @IdOrdenCompra", connection, transaction);

                    command.Parameters.AddWithValue("@Estado", EstadoCancelada);
                    command.Parameters.AddWithValue("@MotivoCancelacion", motivo);
                    command.Parameters.AddWithValue("@FechaCancelacion", utcNow);
                    command.Parameters.AddWithValue("@FechaActualizacion", utcNow);
                    command.Parameters.AddWithValue("@IdUsuarioActualizacion", (object?)usuarioId ?? DBNull.Value);
                    command.Parameters.AddWithValue("@IdUsuarioCancelacion", (object?)usuarioId ?? DBNull.Value);
                    command.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);
                    command.Parameters.AddWithValue("@IdOrdenCompra", request.IdOrdenCompra);
                    await command.ExecuteNonQueryAsync();

                    transaction.Commit();

                    return Ok(new OrdenCompraOperacionResponse
                    {
                        Exito = true,
                        Mensaje = "La orden de compra fue cancelada.",
                        IdOrdenCompra = request.IdOrdenCompra,
                        Folio = ordenActual.Folio,
                        Estado = EstadoCancelada,
                        EstadoNombre = GetEstadoNombre(EstadoCancelada),
                        Subtotal = ordenActual.Subtotal,
                        Total = ordenActual.Total
                    });
                }
                catch
                {
                    if (transaction.Connection != null)
                    {
                        transaction.Rollback();
                    }

                    throw;
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex, "CancelarOrdenCompra", "Ocurrió un error al procesar la solicitud.");
            }
        }

        [HttpGet("ObtenerResumenOrdenesCompra")]
        public async Task<IActionResult> ObtenerResumenOrdenesCompra(Guid idEmpresa)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                using SqlConnection connection = CreateConnection();
                await connection.OpenAsync();

                using SqlCommand command = new SqlCommand(@"
SELECT
    COUNT(1) AS Total,
    SUM(CASE WHEN Estado = 1 THEN 1 ELSE 0 END) AS Borradores,
    SUM(CASE WHEN Estado = 2 THEN 1 ELSE 0 END) AS Generadas,
    SUM(CASE WHEN Estado = 3 THEN 1 ELSE 0 END) AS Canceladas
FROM dbo.OrdenesCompra
WHERE idEmpresa = @IdEmpresa
  AND Activo = 1
  AND FechaArchivado IS NULL", connection);

                command.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);

                using SqlDataReader reader = await command.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    return Ok(new OrdenCompraResumenDto());
                }

                return Ok(new OrdenCompraResumenDto
                {
                    Total = ReadInt(reader, "Total"),
                    Borradores = ReadInt(reader, "Borradores"),
                    Generadas = ReadInt(reader, "Generadas"),
                    Canceladas = ReadInt(reader, "Canceladas")
                });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerResumenOrdenesCompra", "Ocurrió un error al procesar la solicitud.");
            }
        }

        [HttpGet("ObtenerCombosOrdenCompra")]
        public async Task<IActionResult> ObtenerCombosOrdenCompra(Guid idEmpresa)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                using SqlConnection connection = CreateConnection();
                await connection.OpenAsync();

                OrdenCompraCombosDto combos = new OrdenCompraCombosDto
                {
                    RazonesSociales = await ObtenerRazonesSocialesAsync(connection, context.IdEmpresa),
                    Sucursales = await ObtenerSucursalesAsync(connection, context.IdEmpresa),
                    Proveedores = await ObtenerProveedoresAsync(connection, context.IdEmpresa),
                    Estados = new List<OrdenCompraEstadoOpcionDto>
                    {
                        new OrdenCompraEstadoOpcionDto { Id = EstadoBorrador, Nombre = GetEstadoNombre(EstadoBorrador) },
                        new OrdenCompraEstadoOpcionDto { Id = EstadoGenerada, Nombre = GetEstadoNombre(EstadoGenerada) },
                        new OrdenCompraEstadoOpcionDto { Id = EstadoCancelada, Nombre = GetEstadoNombre(EstadoCancelada) }
                    }
                };

                return Ok(combos);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerCombosOrdenCompra", "Ocurrió un error al procesar la solicitud.");
            }
        }

        [HttpGet("BuscarProductosServiciosOrdenCompra")]
        public async Task<IActionResult> BuscarProductosServiciosOrdenCompra(Guid idEmpresa, string texto = "", byte? tipo = null, int limite = 25)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                int limiteNormalizado = NormalizeLimit(limite);

                using SqlConnection connection = CreateConnection();
                await connection.OpenAsync();

                string termino = (texto ?? string.Empty).Trim();
                List<string> tokens = termino
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(token => Truncate(token, BusquedaLength))
                    .Where(token => !string.IsNullOrWhiteSpace(token))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(5)
                    .ToList();

                StringBuilder query = new StringBuilder(@"
SELECT TOP (@Limite)
    ps.id,
    ps.Tipo,
    CASE ps.Tipo WHEN 1 THEN 'Producto' ELSE 'Servicio' END AS TipoNombre,
    ps.Codigo,
    ps.Nombre,
    ISNULL(ps.Descripcion, '') AS Descripcion,
    ps.idUnidadMedida,
    um.Nombre AS Unidad,
    um.Abreviatura,
    ps.Costo,
    ps.CausaInventario
FROM dbo.ProductosServicios ps
INNER JOIN dbo.ProductosServiciosUnidadesMedida um
    ON um.idEmpresa = ps.idEmpresa
   AND um.id = ps.idUnidadMedida
   AND um.Activo = 1
WHERE ps.idEmpresa = @IdEmpresa
  AND ps.Activo = 1");

                using SqlCommand command = new SqlCommand();
                command.Connection = connection;
                command.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);
                command.Parameters.AddWithValue("@Limite", limiteNormalizado);

                if (tokens.Count > 0)
                {
                    for (int i = 0; i < tokens.Count; i++)
                    {
                        string parameterName = $"@Busqueda{i}";
                        query.Append($@"
  AND (
        ps.Codigo LIKE {parameterName}
        OR ps.Nombre LIKE {parameterName}
        OR ISNULL(ps.Descripcion, '') LIKE {parameterName}
      )");
                        command.Parameters.AddWithValue(parameterName, $"%{tokens[i]}%");
                    }

                    string exactToken = tokens[0];
                    string prefixToken = $"{tokens[0]}%";
                    command.Parameters.AddWithValue("@BusquedaExacta", exactToken);
                    command.Parameters.AddWithValue("@BusquedaPrefijo", prefixToken);
                }

                AppendTinyIntFilter(query, command, "ps.Tipo", "@Tipo", tipo);
                query.Append(@"
 ORDER BY
    CASE
        WHEN @HasBusqueda = 1 AND ps.Codigo = @BusquedaExacta THEN 0
        WHEN @HasBusqueda = 1 AND ps.Nombre = @BusquedaExacta THEN 1
        WHEN @HasBusqueda = 1 AND ps.Codigo LIKE @BusquedaPrefijo THEN 2
        WHEN @HasBusqueda = 1 AND ps.Nombre LIKE @BusquedaPrefijo THEN 3
        ELSE 4
    END,
    ps.Nombre,
    ps.Codigo");
                command.Parameters.AddWithValue("@HasBusqueda", tokens.Count > 0 ? 1 : 0);
                if (tokens.Count == 0)
                {
                    command.Parameters.AddWithValue("@BusquedaExacta", string.Empty);
                    command.Parameters.AddWithValue("@BusquedaPrefijo", string.Empty);
                }
                command.CommandText = query.ToString();

                List<OrdenCompraBusquedaProductoServicioDto> items = new List<OrdenCompraBusquedaProductoServicioDto>();
                using SqlDataReader reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    items.Add(new OrdenCompraBusquedaProductoServicioDto
                    {
                        Id = ReadGuid(reader, "id"),
                        Tipo = ReadByte(reader, "Tipo"),
                        TipoNombre = ReadString(reader, "TipoNombre"),
                        Codigo = ReadString(reader, "Codigo"),
                        Nombre = ReadString(reader, "Nombre"),
                        Descripcion = ReadString(reader, "Descripcion"),
                        IdUnidadMedida = ReadGuid(reader, "idUnidadMedida"),
                        Unidad = ReadString(reader, "Unidad"),
                        Abreviatura = ReadString(reader, "Abreviatura"),
                        CostoActual = ReadNullableDecimal(reader, "Costo"),
                        CausaInventario = ReadBool(reader, "CausaInventario")
                    });
                }

                return Ok(items);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "BuscarProductosServiciosOrdenCompra", "Ocurrió un error al procesar la solicitud.");
            }
        }

        [HttpPost("ValidarPendientesOrdenCompra")]
        public async Task<IActionResult> ValidarPendientesOrdenCompra([FromBody] OrdenCompraPendientesRequest request, Guid idEmpresa)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            if (request == null || request.IdOrdenCompra == Guid.Empty)
            {
                return BadRequest(new OrdenCompraOperacionResponse { Mensaje = "La orden de compra no está disponible." });
            }

            if (request.IdEmpresa != Guid.Empty && request.IdEmpresa != context.IdEmpresa)
            {
                return BadRequest(new OrdenCompraOperacionResponse { Mensaje = "La empresa solicitada no coincide con la sesión activa." });
            }

            try
            {
                using SqlConnection connection = CreateConnection();
                await connection.OpenAsync();

                OrdenCompraDocumentoExportDto documento = await ObtenerDocumentoOrdenCompraAsync(connection, context.IdEmpresa, request.IdOrdenCompra);
                if (documento.IdOrdenCompra == Guid.Empty)
                {
                    return NotFound(new OrdenCompraOperacionResponse { Mensaje = "La orden de compra no está disponible." });
                }

                OrdenCompraPendientesResponse pendientes = await ObtenerPendientesAsync(connection, context.IdEmpresa, request.IdOrdenCompra, documento);
                return Ok(pendientes);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ValidarPendientesOrdenCompra", "Ocurrió un error al procesar la solicitud.");
            }
        }

        [HttpGet("ExportarOrdenesCompra")]
        public async Task<IActionResult> ExportarOrdenesCompra(
            Guid idEmpresa,
            string busqueda = "",
            byte? estado = null,
            Guid? idProveedor = null,
            Guid? idRazonSocial = null,
            Guid? idSucursal = null,
            DateTime? fechaDesde = null,
            DateTime? fechaHasta = null)
        {
            IActionResult listadoResult = await ObtenerOrdenesCompra(idEmpresa, busqueda, estado, idProveedor, idRazonSocial, idSucursal, fechaDesde, fechaHasta);
            if (listadoResult is not OkObjectResult ok || ok.Value is not List<OrdenCompraListadoDto> items)
            {
                return listadoResult;
            }

            List<OrdenCompraExportacionDto> exportRows = items
                .Take(ExportacionLimit)
                .Select(item => new OrdenCompraExportacionDto
                {
                    Folio = item.Folio,
                    FechaOrden = item.FechaOrden,
                    FechaLlegada = item.FechaLlegada,
                    RazonSocial = item.RazonSocial,
                    Sucursal = item.Sucursal,
                    Proveedor = item.Proveedor,
                    Estado = GetEstadoNombreUsuario(item.Estado),
                    Total = item.Total,
                    FechaCreacion = item.FechaCreacion
                })
                .ToList();

            byte[] excel = BuildListadoExcelDocument(exportRows);
            string fileName = $"ordenes_compra_reporte_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
            return File(excel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet("ExportarOrdenCompraPdf")]
        public async Task<IActionResult> ExportarOrdenCompraPdf(Guid idEmpresa, Guid idOrdenCompra)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            if (idOrdenCompra == Guid.Empty)
            {
                return BadRequest(new OrdenCompraOperacionResponse { Mensaje = "La orden de compra no está disponible." });
            }

            try
            {
                using SqlConnection connection = CreateConnection();
                await connection.OpenAsync();

                OrdenCompraDocumentoExportDto documento = await ObtenerDocumentoOrdenCompraAsync(connection, context.IdEmpresa, idOrdenCompra);
                if (documento.IdOrdenCompra == Guid.Empty)
                {
                    return NotFound(new OrdenCompraOperacionResponse { Mensaje = "La orden de compra no está disponible." });
                }

                byte[] pdf = BuildPdfDocument(documento);
                string fileName = BuildSafeFileName("orden_compra", documento.Folio, ".pdf");
                return File(pdf, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ExportarOrdenCompraPdf", "Ocurrió un error al procesar la solicitud.");
            }
        }

        [HttpGet("ExportarOrdenCompraExcel")]
        public async Task<IActionResult> ExportarOrdenCompraExcel(Guid idEmpresa, Guid idOrdenCompra)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            if (idOrdenCompra == Guid.Empty)
            {
                return BadRequest(new OrdenCompraOperacionResponse { Mensaje = "La orden de compra no está disponible." });
            }

            try
            {
                using SqlConnection connection = CreateConnection();
                await connection.OpenAsync();

                OrdenCompraDocumentoExportDto documento = await ObtenerDocumentoOrdenCompraAsync(connection, context.IdEmpresa, idOrdenCompra);
                if (documento.IdOrdenCompra == Guid.Empty)
                {
                    return NotFound(new OrdenCompraOperacionResponse { Mensaje = "La orden de compra no está disponible." });
                }

                byte[] excel = BuildExcelDocument(documento);
                string fileName = BuildSafeFileName("orden_compra", documento.Folio, ".xlsx");
                return File(excel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ExportarOrdenCompraExcel", "Ocurrió un error al procesar la solicitud.");
            }
        }

        private async Task<OrdenCompraDocumentoExportDto> ObtenerDocumentoOrdenCompraAsync(SqlConnection connection, Guid idEmpresa, Guid idOrdenCompra)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT TOP (1)
    oc.id,
    ISNULL(oc.Folio, '') AS Folio,
    oc.FechaOrden,
    oc.FechaLlegada,
    ISNULL(rs.Nombre, '') AS RazonSocial,
    ISNULL(su.Nombre, '') AS Sucursal,
    ISNULL(pr.Nombre, '') AS Proveedor,
    ISNULL(oc.Observaciones, '') AS Observaciones,
    oc.Subtotal,
    oc.Total,
    oc.Estado
FROM dbo.OrdenesCompra oc
LEFT JOIN dbo.RazonesSociales rs
    ON rs.id = oc.idRazonSocial AND rs.idEmpresa = oc.idEmpresa
LEFT JOIN dbo.Sucursales su
    ON su.id = oc.idSucursal AND su.idEmpresa = oc.idEmpresa
LEFT JOIN dbo.ActivosProveedores pr
    ON pr.id = oc.idProveedor AND pr.idEmpresa = oc.idEmpresa
WHERE oc.idEmpresa = @IdEmpresa
  AND oc.id = @IdOrdenCompra
  AND oc.Activo = 1
  AND oc.FechaArchivado IS NULL", connection);

            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@IdOrdenCompra", idOrdenCompra);

            OrdenCompraDocumentoExportDto documento = new OrdenCompraDocumentoExportDto();
            using (SqlDataReader reader = await command.ExecuteReaderAsync())
            {
                if (!await reader.ReadAsync())
                {
                    return documento;
                }

                byte estado = ReadByte(reader, "Estado");
                documento.IdOrdenCompra = ReadGuid(reader, "id");
                documento.Folio = ReadString(reader, "Folio");
                documento.FechaOrden = ReadDateTime(reader, "FechaOrden");
                documento.FechaLlegada = ReadNullableDateTime(reader, "FechaLlegada");
                documento.RazonSocial = ReadString(reader, "RazonSocial");
                documento.Sucursal = ReadString(reader, "Sucursal");
                documento.Proveedor = ReadString(reader, "Proveedor");
                documento.Observaciones = ReadString(reader, "Observaciones");
                documento.Subtotal = ReadDecimal(reader, "Subtotal");
                documento.Total = ReadDecimal(reader, "Total");
                documento.Estado = GetEstadoNombre(estado);
            }

            documento.Partidas = (await ObtenerPartidasAsync(connection, idEmpresa, idOrdenCompra))
                .Select(partida => new OrdenCompraDocumentoPartidaDto
                {
                    NumeroPartida = partida.NumeroPartida,
                    Tipo = partida.TipoProductoServicioNombre,
                    Codigo = partida.Codigo,
                    Nombre = partida.Nombre,
                    Descripcion = partida.Descripcion,
                    Unidad = string.IsNullOrWhiteSpace(partida.UnidadAbreviatura)
                        ? partida.UnidadMedida
                        : $"{partida.UnidadMedida} ({partida.UnidadAbreviatura})",
                    Cantidad = partida.Cantidad,
                    CostoUnitario = partida.CostoUnitario,
                    Subtotal = partida.Subtotal
                })
                .ToList();

            return documento;
        }

        private async Task<OrdenCompraPendientesResponse> ObtenerPendientesAsync(SqlConnection connection, Guid idEmpresa, Guid idOrdenCompra, OrdenCompraDocumentoExportDto documento)
        {
            OrdenCompraPendientesResponse response = new OrdenCompraPendientesResponse();
            if (documento.Partidas.Count == 0)
            {
                return response;
            }

            Guid proveedorId;
            using (SqlCommand proveedorCommand = new SqlCommand(@"
SELECT TOP (1) idProveedor
FROM dbo.OrdenesCompra
WHERE idEmpresa = @IdEmpresa
  AND id = @IdOrdenCompra
  AND Activo = 1
  AND FechaArchivado IS NULL", connection))
            {
                proveedorCommand.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                proveedorCommand.Parameters.AddWithValue("@IdOrdenCompra", idOrdenCompra);
                object? scalar = await proveedorCommand.ExecuteScalarAsync();
                proveedorId = scalar != null && scalar != DBNull.Value ? (Guid)scalar : Guid.Empty;
            }

            if (proveedorId == Guid.Empty)
            {
                return response;
            }

            return await ObtenerPendientesFallbackAsync(connection, idEmpresa, idOrdenCompra, proveedorId, documento);
        }

        private async Task<OrdenCompraPendientesResponse> ObtenerPendientesFallbackAsync(SqlConnection connection, Guid idEmpresa, Guid idOrdenCompra, Guid proveedorId, OrdenCompraDocumentoExportDto documento)
        {
            HashSet<Guid> productoIds = new HashSet<Guid>(await ObtenerProductoIdsDeOrdenAsync(connection, idEmpresa, idOrdenCompra));
            OrdenCompraPendientesResponse response = new OrdenCompraPendientesResponse();

            using SqlCommand command = new SqlCommand(@"
SELECT
    oc.id AS IdOrdenCompra,
    ISNULL(oc.Folio, '') AS Folio,
    oc.Estado,
    oc.FechaOrden,
    det.idProductoServicio,
    ISNULL(det.Codigo, '') AS Codigo,
    ISNULL(det.Nombre, '') AS Nombre,
    det.Total
FROM dbo.OrdenesCompra oc
INNER JOIN dbo.OrdenesCompraDetalle det
    ON det.idEmpresa = oc.idEmpresa
   AND det.idOrdenCompra = oc.id
   AND det.Activo = 1
   AND det.FechaArchivado IS NULL
WHERE oc.idEmpresa = @IdEmpresa
  AND oc.id <> @IdOrdenCompra
  AND oc.idProveedor = @IdProveedor
  AND oc.Activo = 1
  AND oc.FechaArchivado IS NULL
  AND oc.Estado IN (@EstadoBorrador, @EstadoGenerada)
ORDER BY oc.FechaOrden DESC", connection);

            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@IdOrdenCompra", idOrdenCompra);
            command.Parameters.AddWithValue("@IdProveedor", proveedorId);
            command.Parameters.AddWithValue("@EstadoBorrador", EstadoBorrador);
            command.Parameters.AddWithValue("@EstadoGenerada", EstadoGenerada);

            Dictionary<Guid, OrdenCompraPendienteItemDto> map = new Dictionary<Guid, OrdenCompraPendienteItemDto>();
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                Guid productoId = ReadGuid(reader, "idProductoServicio");
                if (!productoIds.Contains(productoId))
                {
                    continue;
                }

                Guid ordenId = ReadGuid(reader, "IdOrdenCompra");
                if (!map.TryGetValue(ordenId, out OrdenCompraPendienteItemDto? item))
                {
                    byte estado = ReadByte(reader, "Estado");
                    item = new OrdenCompraPendienteItemDto
                    {
                        IdOrdenCompra = ordenId,
                        Folio = ReadString(reader, "Folio"),
                        Estado = estado,
                        EstadoNombre = GetEstadoNombre(estado),
                        FechaOrden = ReadDateTime(reader, "FechaOrden")
                    };
                    map[ordenId] = item;
                }

                item.PartidasCoincidentes++;
                item.TotalCoincidente = NormalizeMoney(item.TotalCoincidente + ReadDecimal(reader, "Total"));
                string producto = $"{ReadString(reader, "Codigo")} - {ReadString(reader, "Nombre")}".Trim();
                if (!string.IsNullOrWhiteSpace(producto) &&
                    !item.Productos.Any(existing => string.Equals(existing, producto, StringComparison.OrdinalIgnoreCase)))
                {
                    item.Productos.Add(producto);
                }
            }

            response.Ordenes = map.Values.OrderByDescending(x => x.FechaOrden).ToList();
            response.TienePendientes = response.Ordenes.Count > 0;
            response.TotalOrdenesCoincidentes = response.Ordenes.Count;
            response.TotalPartidasCoincidentes = response.Ordenes.Sum(x => x.PartidasCoincidentes);
            return response;
        }

        private async Task<List<Guid>> ObtenerProductoIdsDeOrdenAsync(SqlConnection connection, Guid idEmpresa, Guid idOrdenCompra)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT idProductoServicio
FROM dbo.OrdenesCompraDetalle
WHERE idEmpresa = @IdEmpresa
  AND idOrdenCompra = @IdOrdenCompra
  AND Activo = 1
  AND FechaArchivado IS NULL", connection);

            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@IdOrdenCompra", idOrdenCompra);

            List<Guid> ids = new List<Guid>();
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                ids.Add(ReadGuid(reader, "idProductoServicio"));
            }

            return ids;
        }

        private static byte[] BuildExcelDocument(OrdenCompraDocumentoExportDto documento)
        {
            using MemoryStream stream = new MemoryStream();
            using (SpreadsheetDocument spreadsheet = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook, true))
            {
                WorkbookPart workbookPart = spreadsheet.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();

                WorkbookStylesPart stylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
                stylesPart.Stylesheet = BuildWorkbookStylesheet();
                stylesPart.Stylesheet.Save();

                WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                SheetData sheetData = new SheetData();
                worksheetPart.Worksheet = new Worksheet(
                    new SheetViews(new SheetView { WorkbookViewId = 0U }),
                    new SheetFormatProperties { DefaultRowHeight = 15D },
                    new Columns(
                        BuildColumn(1, 1, 12D),
                        BuildColumn(2, 2, 14D),
                        BuildColumn(3, 3, 18D),
                        BuildColumn(4, 4, 24D),
                        BuildColumn(5, 5, 38D),
                        BuildColumn(6, 6, 18D),
                        BuildColumn(7, 7, 14D),
                        BuildColumn(8, 8, 14D),
                        BuildColumn(9, 9, 14D)),
                    sheetData);

                Sheets sheets = spreadsheet.WorkbookPart!.Workbook.AppendChild(new Sheets());
                Sheet sheet = new Sheet
                {
                    Id = spreadsheet.WorkbookPart.GetIdOfPart(worksheetPart),
                    SheetId = 1U,
                    Name = "Orden"
                };
                sheets.Append(sheet);

                uint rowIndex = 1;
                sheetData.Append(
                    BuildTextRow(rowIndex++, 1U, "Orden de Compra"),
                    BuildTextRow(rowIndex++, 0U, $"Folio: {TextOrDash(documento.Folio)}"),
                    BuildTextRow(rowIndex++, 0U, $"Estado: {documento.Estado}"),
                    BuildTextRow(rowIndex++, 0U, $"Razón social: {documento.RazonSocial}"),
                    BuildTextRow(rowIndex++, 0U, $"Sucursal: {documento.Sucursal}"),
                    BuildTextRow(rowIndex++, 0U, $"Proveedor: {documento.Proveedor}"),
                    BuildTextRow(rowIndex++, 0U, $"Fecha de orden: {documento.FechaOrden:dd/MM/yyyy}"),
                    BuildTextRow(rowIndex++, 0U, $"Fecha de llegada: {(documento.FechaLlegada.HasValue ? documento.FechaLlegada.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) : "—")}"),
                    BuildTextRow(rowIndex++, 0U, $"Observaciones: {TextOrDash(documento.Observaciones)}"),
                    new Row { RowIndex = rowIndex++ });

                sheetData.Append(new Row(
                    BuildTextCell("A", rowIndex, "No.", 1U),
                    BuildTextCell("B", rowIndex, "Tipo", 1U),
                    BuildTextCell("C", rowIndex, "Código", 1U),
                    BuildTextCell("D", rowIndex, "Nombre", 1U),
                    BuildTextCell("E", rowIndex, "Descripción", 1U),
                    BuildTextCell("F", rowIndex, "Unidad", 1U),
                    BuildTextCell("G", rowIndex, "Cantidad", 1U),
                    BuildTextCell("H", rowIndex, "Costo", 1U),
                    BuildTextCell("I", rowIndex, "Subtotal", 1U)));
                rowIndex++;

                foreach (OrdenCompraDocumentoPartidaDto partida in documento.Partidas)
                {
                    sheetData.Append(new Row(
                        BuildNumberCell("A", rowIndex, partida.NumeroPartida, 0U),
                        BuildTextCell("B", rowIndex, partida.Tipo, 0U),
                        BuildTextCell("C", rowIndex, partida.Codigo, 0U),
                        BuildTextCell("D", rowIndex, partida.Nombre, 0U),
                        BuildTextCell("E", rowIndex, partida.Descripcion, 0U),
                        BuildTextCell("F", rowIndex, partida.Unidad, 0U),
                        BuildNumberCell("G", rowIndex, partida.Cantidad, 2U),
                        BuildNumberCell("H", rowIndex, partida.CostoUnitario, 2U),
                        BuildNumberCell("I", rowIndex, partida.Subtotal, 2U)));
                    rowIndex++;
                }

                sheetData.Append(
                    new Row { RowIndex = rowIndex++ },
                    new Row(
                        BuildTextCell("G", rowIndex, "Subtotal", 1U),
                        BuildNumberCell("I", rowIndex, documento.Subtotal, 2U)),
                    new Row(
                        BuildTextCell("G", ++rowIndex, "Total", 1U),
                        BuildNumberCell("I", rowIndex, documento.Total, 2U)));

                workbookPart.Workbook.Save();
            }

            return stream.ToArray();
        }

        private static byte[] BuildListadoExcelDocument(List<OrdenCompraExportacionDto> items)
        {
            using MemoryStream stream = new MemoryStream();
            using (SpreadsheetDocument spreadsheet = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook, true))
            {
                WorkbookPart workbookPart = spreadsheet.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();

                WorkbookStylesPart stylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
                stylesPart.Stylesheet = BuildWorkbookStylesheet();
                stylesPart.Stylesheet.Save();

                WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                SheetData sheetData = new SheetData();
                worksheetPart.Worksheet = new Worksheet(
                    new SheetViews(new SheetView { WorkbookViewId = 0U }),
                    new SheetFormatProperties { DefaultRowHeight = 15D },
                    new Columns(
                        BuildColumn(1, 1, 18D),
                        BuildColumn(2, 2, 16D),
                        BuildColumn(3, 3, 18D),
                        BuildColumn(4, 4, 28D),
                        BuildColumn(5, 5, 24D),
                        BuildColumn(6, 6, 26D),
                        BuildColumn(7, 7, 18D),
                        BuildColumn(8, 8, 16D),
                        BuildColumn(9, 9, 20D)),
                    sheetData);

                Sheets sheets = spreadsheet.WorkbookPart!.Workbook.AppendChild(new Sheets());
                Sheet sheet = new Sheet
                {
                    Id = spreadsheet.WorkbookPart.GetIdOfPart(worksheetPart),
                    SheetId = 1U,
                    Name = "Reporte"
                };
                sheets.Append(sheet);

                uint rowIndex = 1;
                sheetData.Append(
                    BuildTextRow(rowIndex++, 1U, "Reporte de órdenes de compra"),
                    BuildTextRow(rowIndex++, 0U, $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}"),
                    new Row { RowIndex = rowIndex++ });

                sheetData.Append(new Row(
                    BuildTextCell("A", rowIndex, "Folio", 1U),
                    BuildTextCell("B", rowIndex, "Fecha de orden", 1U),
                    BuildTextCell("C", rowIndex, "Fecha de llegada", 1U),
                    BuildTextCell("D", rowIndex, "Razón social", 1U),
                    BuildTextCell("E", rowIndex, "Sucursal", 1U),
                    BuildTextCell("F", rowIndex, "Proveedor", 1U),
                    BuildTextCell("G", rowIndex, "Estado", 1U),
                    BuildTextCell("H", rowIndex, "Total", 1U),
                    BuildTextCell("I", rowIndex, "Fecha de creación", 1U)));
                rowIndex++;

                foreach (OrdenCompraExportacionDto item in items)
                {
                    sheetData.Append(new Row(
                        BuildTextCell("A", rowIndex, item.Folio, 0U),
                        BuildTextCell("B", rowIndex, item.FechaOrden.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture), 0U),
                        BuildTextCell("C", rowIndex, item.FechaLlegada.HasValue ? item.FechaLlegada.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) : "—", 0U),
                        BuildTextCell("D", rowIndex, item.RazonSocial, 0U),
                        BuildTextCell("E", rowIndex, item.Sucursal, 0U),
                        BuildTextCell("F", rowIndex, item.Proveedor, 0U),
                        BuildTextCell("G", rowIndex, item.Estado, 0U),
                        BuildNumberCell("H", rowIndex, item.Total, 2U),
                        BuildTextCell("I", rowIndex, item.FechaCreacion.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture), 0U)));
                    rowIndex++;
                }

                workbookPart.Workbook.Save();
            }

            return stream.ToArray();
        }

        private static byte[] BuildPdfDocument(OrdenCompraDocumentoExportDto documento)
        {
            List<string> lines = BuildPdfLines(documento);
            const int linesPerPage = 44;
            List<List<string>> pages = lines
                .Select((line, index) => new { line, index })
                .GroupBy(x => x.index / linesPerPage)
                .Select(group => group.Select(x => x.line).ToList())
                .ToList();

            StringBuilder pdf = new StringBuilder();
            List<long> offsets = new List<long> { 0L };

            void AppendObject(int number, string body)
            {
                offsets.Add(pdf.Length);
                pdf.Append(number.ToString(CultureInfo.InvariantCulture))
                   .Append(" 0 obj\n")
                   .Append(body)
                   .Append("\nendobj\n");
            }

            pdf.Append("%PDF-1.4\n");

            int pageCount = pages.Count == 0 ? 1 : pages.Count;
            int fontObject = 3 + (pageCount * 2);

            AppendObject(1, "<< /Type /Catalog /Pages 2 0 R >>");

            StringBuilder kids = new StringBuilder();
            for (int i = 0; i < pageCount; i++)
            {
                if (i > 0) kids.Append(' ');
                kids.Append(3 + i * 2).Append(" 0 R");
            }
            AppendObject(2, $"<< /Type /Pages /Count {pageCount} /Kids [ {kids} ] >>");

            for (int i = 0; i < pageCount; i++)
            {
                int pageObject = 3 + i * 2;
                int contentObject = pageObject + 1;
                AppendObject(pageObject, $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 {fontObject} 0 R >> >> /Contents {contentObject} 0 R >>");
                string content = BuildPdfContentStream(pages.Count == 0 ? new List<string>() : pages[i], i + 1, pageCount);
                byte[] contentBytes = Encoding.ASCII.GetBytes(content);
                AppendObject(contentObject, $"<< /Length {contentBytes.Length} >>\nstream\n{content}\nendstream");
            }

            AppendObject(fontObject, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");

            long xrefOffset = pdf.Length;
            pdf.Append("xref\n0 ")
               .Append((offsets.Count).ToString(CultureInfo.InvariantCulture))
               .Append('\n')
               .Append("0000000000 65535 f \n");

            for (int i = 1; i < offsets.Count; i++)
            {
                pdf.Append(offsets[i].ToString("D10", CultureInfo.InvariantCulture))
                   .Append(" 00000 n \n");
            }

            pdf.Append("trailer << /Size ")
               .Append(offsets.Count.ToString(CultureInfo.InvariantCulture))
               .Append(" /Root 1 0 R >>\nstartxref\n")
               .Append(xrefOffset.ToString(CultureInfo.InvariantCulture))
               .Append("\n%%EOF");

            return Encoding.ASCII.GetBytes(pdf.ToString());
        }

        private static Stylesheet BuildWorkbookStylesheet()
        {
            return new Stylesheet(
                new Fonts(
                    new Font(
                        new FontSize { Val = 11D },
                        new FontName { Val = "Arial" }),
                    new Font(
                        new Bold(),
                        new FontSize { Val = 11D },
                        new FontName { Val = "Arial" })),
                new Fills(
                    new Fill(new PatternFill { PatternType = PatternValues.None }),
                    new Fill(new PatternFill { PatternType = PatternValues.Gray125 })),
                new Borders(new Border()),
                new CellFormats(
                    new CellFormat(),
                    new CellFormat { FontId = 1U, ApplyFont = true },
                    new CellFormat
                    {
                        NumberFormatId = 4U,
                        ApplyNumberFormat = true
                    }));
        }

        private static Column BuildColumn(uint min, uint max, double width)
            => new Column { Min = min, Max = max, Width = width, CustomWidth = true };

        private static Row BuildTextRow(uint rowIndex, uint styleIndex, string text)
            => new Row(BuildTextCell("A", rowIndex, text, styleIndex)) { RowIndex = rowIndex };

        private static Cell BuildTextCell(string columnName, uint rowIndex, string value, uint styleIndex)
        {
            return new Cell
            {
                CellReference = $"{columnName}{rowIndex}",
                DataType = CellValues.InlineString,
                StyleIndex = styleIndex,
                InlineString = new InlineString(new Text(value ?? string.Empty))
            };
        }

        private static Cell BuildNumberCell(string columnName, uint rowIndex, decimal value, uint styleIndex)
        {
            return new Cell
            {
                CellReference = $"{columnName}{rowIndex}",
                CellValue = new CellValue(value.ToString(CultureInfo.InvariantCulture)),
                DataType = CellValues.Number,
                StyleIndex = styleIndex
            };
        }

        private static List<string> BuildPdfLines(OrdenCompraDocumentoExportDto documento)
        {
            List<string> lines = new List<string>
            {
                "ORDEN DE COMPRA",
                $"Folio: {TextOrDash(documento.Folio)}",
                $"Estado: {documento.Estado}",
                $"Razon social: {TextOrDash(documento.RazonSocial)}",
                $"Sucursal: {TextOrDash(documento.Sucursal)}",
                $"Proveedor: {TextOrDash(documento.Proveedor)}",
                $"Fecha de orden: {documento.FechaOrden:dd/MM/yyyy}",
                $"Fecha de llegada: {(documento.FechaLlegada.HasValue ? documento.FechaLlegada.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) : "-")}",
                $"Observaciones: {TextOrDash(documento.Observaciones)}",
                string.Empty,
                "PARTIDAS",
                "No | Tipo | Codigo | Producto o servicio | Unidad | Cantidad | Costo | Subtotal"
            };

            foreach (OrdenCompraDocumentoPartidaDto partida in documento.Partidas)
            {
                string descripcion = string.IsNullOrWhiteSpace(partida.Descripcion)
                    ? partida.Nombre
                    : $"{partida.Nombre} / {partida.Descripcion}";
                lines.Add(string.Join(" | ", new[]
                {
                    partida.NumeroPartida.ToString(CultureInfo.InvariantCulture),
                    ShortenPdfText(partida.Tipo, 10),
                    ShortenPdfText(partida.Codigo, 18),
                    ShortenPdfText(descripcion, 45),
                    ShortenPdfText(partida.Unidad, 16),
                    partida.Cantidad.ToString("N4", CultureInfo.InvariantCulture),
                    partida.CostoUnitario.ToString("N2", CultureInfo.InvariantCulture),
                    partida.Subtotal.ToString("N2", CultureInfo.InvariantCulture)
                }));
            }

            lines.Add(string.Empty);
            lines.Add($"Subtotal: {documento.Subtotal.ToString("N2", CultureInfo.InvariantCulture)}");
            lines.Add($"Total: {documento.Total.ToString("N2", CultureInfo.InvariantCulture)}");
            return lines;
        }

        private static string BuildPdfContentStream(List<string> lines, int pageNumber, int pageCount)
        {
            StringBuilder content = new StringBuilder();
            content.Append("BT\n/F1 10 Tf\n40 770 Td\n");

            for (int i = 0; i < lines.Count; i++)
            {
                if (i == 0)
                {
                    content.Append('(').Append(EscapePdfText(lines[i])).Append(") Tj\n");
                }
                else
                {
                    content.Append("T*\n(").Append(EscapePdfText(lines[i])).Append(") Tj\n");
                }
            }

            content.Append("T*\n(Page ")
                   .Append(pageNumber.ToString(CultureInfo.InvariantCulture))
                   .Append(" de ")
                   .Append(pageCount.ToString(CultureInfo.InvariantCulture))
                   .Append(") Tj\nET");

            return content.ToString();
        }

        private static string EscapePdfText(string text)
            => (text ?? string.Empty)
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("(", "\\(", StringComparison.Ordinal)
                .Replace(")", "\\)", StringComparison.Ordinal);

        private static string ShortenPdfText(string text, int maxLength)
        {
            string value = (text ?? string.Empty).Trim();
            if (value.Length <= maxLength)
            {
                return value;
            }

            return value.Substring(0, Math.Max(0, maxLength - 3)) + "...";
        }

        private static string BuildSafeFileName(string prefix, string folio, string extension)
        {
            string safeFolio = string.IsNullOrWhiteSpace(folio)
                ? "sin_folio"
                : new string(folio.Where(ch => char.IsLetterOrDigit(ch) || ch == '-' || ch == '_').ToArray());

            if (string.IsNullOrWhiteSpace(safeFolio))
            {
                safeFolio = "sin_folio";
            }

            return $"{prefix}_{safeFolio}{extension}";
        }

        private static string TextOrDash(string? value)
            => string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();

        private async Task ValidateEncabezadoCatalogosAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idRazonSocial, Guid idSucursal, Guid idProveedor)
        {
            if (!await ExistsAsync(connection, transaction,
                "SELECT COUNT(1) FROM dbo.RazonesSociales WHERE idEmpresa = @IdEmpresa AND id = @Id AND ISNULL(borrado, 0) = 0",
                idEmpresa, idRazonSocial))
            {
                throw new CatalogoValidationException("La razón social no está disponible.");
            }

            if (!await ExistsAsync(connection, transaction,
                "SELECT COUNT(1) FROM dbo.Sucursales WHERE idEmpresa = @IdEmpresa AND id = @Id AND ISNULL(borrado, 0) = 0",
                idEmpresa, idSucursal))
            {
                throw new CatalogoValidationException("La sucursal no está disponible.");
            }

            if (!await ExistsAsync(connection, transaction,
                "SELECT COUNT(1) FROM dbo.ActivosProveedores WHERE idEmpresa = @IdEmpresa AND id = @Id AND Activo = 1",
                idEmpresa, idProveedor))
            {
                throw new CatalogoValidationException("El proveedor no está disponible.");
            }
        }

        private async Task<List<OrdenCompraPartidaPersistencia>> BuildValidatedPartidasAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            Guid idEmpresa,
            List<OrdenCompraPartidaGuardarRequest> requestPartidas)
        {
            if (requestPartidas == null || requestPartidas.Count == 0)
            {
                throw new CatalogoValidationException("La orden debe contener al menos una partida.");
            }

            ValidateRequestPartidas(requestPartidas);

            List<OrdenCompraPartidaPersistencia> result = new List<OrdenCompraPartidaPersistencia>();
            int numeroPartida = 1;

            foreach (OrdenCompraPartidaGuardarRequest partidaRequest in requestPartidas)
            {
                ProductoServicioSnapshot snapshot = await ObtenerProductoServicioSnapshotAsync(connection, transaction, idEmpresa, partidaRequest.IdProductoServicio);
                if (snapshot.Id == Guid.Empty)
                {
                    throw new CatalogoValidationException("El producto o servicio no está disponible.");
                }

                decimal costoUnitario = NormalizeMoney(partidaRequest.CostoUnitario);
                decimal cantidad = NormalizeQuantity(partidaRequest.Cantidad);
                decimal subtotal = NormalizeMoney(cantidad * costoUnitario);

                result.Add(new OrdenCompraPartidaPersistencia
                {
                    Id = Guid.NewGuid(),
                    IdProductoServicio = snapshot.Id,
                    TipoProductoServicio = snapshot.Tipo,
                    Codigo = snapshot.Codigo,
                    Nombre = snapshot.Nombre,
                    Descripcion = snapshot.Descripcion,
                    IdUnidadMedida = snapshot.IdUnidadMedida,
                    UnidadMedida = snapshot.UnidadMedida,
                    UnidadAbreviatura = snapshot.UnidadAbreviatura,
                    Cantidad = cantidad,
                    CostoUnitario = costoUnitario,
                    Subtotal = subtotal,
                    Total = subtotal,
                    NumeroPartida = numeroPartida++
                });
            }

            return result;
        }

        private async Task<ProductoServicioSnapshot> ObtenerProductoServicioSnapshotAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idProductoServicio)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT TOP (1)
    ps.id,
    ps.Tipo,
    ps.Codigo,
    ps.Nombre,
    ISNULL(ps.Descripcion, '') AS Descripcion,
    ps.idUnidadMedida,
    um.Nombre AS UnidadMedida,
    um.Abreviatura AS UnidadAbreviatura
FROM dbo.ProductosServicios ps
INNER JOIN dbo.ProductosServiciosUnidadesMedida um
    ON um.idEmpresa = ps.idEmpresa
   AND um.id = ps.idUnidadMedida
   AND um.Activo = 1
WHERE ps.idEmpresa = @IdEmpresa
  AND ps.id = @IdProductoServicio
  AND ps.Activo = 1", connection, transaction);

            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);

            using SqlDataReader reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return new ProductoServicioSnapshot();
            }

            return new ProductoServicioSnapshot
            {
                Id = ReadGuid(reader, "id"),
                Tipo = ReadByte(reader, "Tipo"),
                Codigo = ReadString(reader, "Codigo"),
                Nombre = ReadString(reader, "Nombre"),
                Descripcion = ReadString(reader, "Descripcion"),
                IdUnidadMedida = ReadGuid(reader, "idUnidadMedida"),
                UnidadMedida = ReadString(reader, "UnidadMedida"),
                UnidadAbreviatura = ReadString(reader, "UnidadAbreviatura")
            };
        }

        private async Task InsertPartidasAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idOrdenCompra, List<OrdenCompraPartidaPersistencia> partidas, DateTime utcNow)
        {
            foreach (OrdenCompraPartidaPersistencia partida in partidas)
            {
                using SqlCommand command = new SqlCommand(@"
INSERT INTO dbo.OrdenesCompraDetalle
(
    id,
    idEmpresa,
    identityKey,
    idOrdenCompra,
    NumeroPartida,
    idProductoServicio,
    TipoProductoServicio,
    Codigo,
    Nombre,
    Descripcion,
    idUnidadMedida,
    UnidadMedida,
    UnidadAbreviatura,
    Cantidad,
    CostoUnitario,
    Subtotal,
    Total,
    Activo,
    FechaCreacion,
    FechaActualizacion
)
VALUES
(
    @Id,
    @IdEmpresa,
    @IdentityKey,
    @IdOrdenCompra,
    @NumeroPartida,
    @IdProductoServicio,
    @TipoProductoServicio,
    @Codigo,
    @Nombre,
    @Descripcion,
    @IdUnidadMedida,
    @UnidadMedida,
    @UnidadAbreviatura,
    @Cantidad,
    @CostoUnitario,
    @Subtotal,
    @Total,
    1,
    @FechaCreacion,
    @FechaActualizacion
)", connection, transaction);

                command.Parameters.AddWithValue("@Id", partida.Id);
                command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                command.Parameters.AddWithValue("@IdentityKey", Guid.NewGuid());
                command.Parameters.AddWithValue("@IdOrdenCompra", idOrdenCompra);
                command.Parameters.AddWithValue("@NumeroPartida", partida.NumeroPartida);
                command.Parameters.AddWithValue("@IdProductoServicio", partida.IdProductoServicio);
                command.Parameters.AddWithValue("@TipoProductoServicio", partida.TipoProductoServicio);
                command.Parameters.AddWithValue("@Codigo", partida.Codigo);
                command.Parameters.AddWithValue("@Nombre", partida.Nombre);
                command.Parameters.AddWithValue("@Descripcion", (object?)NullIfEmpty(partida.Descripcion) ?? DBNull.Value);
                command.Parameters.AddWithValue("@IdUnidadMedida", partida.IdUnidadMedida);
                command.Parameters.AddWithValue("@UnidadMedida", partida.UnidadMedida);
                command.Parameters.AddWithValue("@UnidadAbreviatura", partida.UnidadAbreviatura);
                command.Parameters.AddWithValue("@Cantidad", partida.Cantidad);
                command.Parameters.AddWithValue("@CostoUnitario", partida.CostoUnitario);
                command.Parameters.AddWithValue("@Subtotal", partida.Subtotal);
                command.Parameters.AddWithValue("@Total", partida.Total);
                command.Parameters.AddWithValue("@FechaCreacion", utcNow);
                command.Parameters.AddWithValue("@FechaActualizacion", utcNow);
                await command.ExecuteNonQueryAsync();
            }
        }

        private async Task<List<OrdenCompraPartidaDetalleDto>> ObtenerPartidasAsync(SqlConnection connection, Guid idEmpresa, Guid idOrdenCompra)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT
    id,
    NumeroPartida,
    idProductoServicio,
    TipoProductoServicio,
    Codigo,
    Nombre,
    ISNULL(Descripcion, '') AS Descripcion,
    idUnidadMedida,
    UnidadMedida,
    UnidadAbreviatura,
    Cantidad,
    CostoUnitario,
    Subtotal,
    Total
FROM dbo.OrdenesCompraDetalle
WHERE idEmpresa = @IdEmpresa
  AND idOrdenCompra = @IdOrdenCompra
  AND Activo = 1
  AND FechaArchivado IS NULL
ORDER BY NumeroPartida", connection);

            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@IdOrdenCompra", idOrdenCompra);

            List<OrdenCompraPartidaDetalleDto> partidas = new List<OrdenCompraPartidaDetalleDto>();
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                byte tipo = ReadByte(reader, "TipoProductoServicio");
                partidas.Add(new OrdenCompraPartidaDetalleDto
                {
                    Id = ReadGuid(reader, "id"),
                    NumeroPartida = ReadInt(reader, "NumeroPartida"),
                    IdProductoServicio = ReadGuid(reader, "idProductoServicio"),
                    TipoProductoServicio = tipo,
                    TipoProductoServicioNombre = GetTipoNombre(tipo),
                    Codigo = ReadString(reader, "Codigo"),
                    Nombre = ReadString(reader, "Nombre"),
                    Descripcion = ReadString(reader, "Descripcion"),
                    IdUnidadMedida = ReadGuid(reader, "idUnidadMedida"),
                    UnidadMedida = ReadString(reader, "UnidadMedida"),
                    UnidadAbreviatura = ReadString(reader, "UnidadAbreviatura"),
                    Cantidad = ReadDecimal(reader, "Cantidad"),
                    CostoUnitario = ReadDecimal(reader, "CostoUnitario"),
                    Subtotal = ReadDecimal(reader, "Subtotal"),
                    Total = ReadDecimal(reader, "Total")
                });
            }

            return partidas;
        }

        private async Task<OrdenCompraCabeceraPersistida> GetOrdenCompraForUpdateAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idOrdenCompra)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT TOP (1)
    id,
    ISNULL(Folio, '') AS Folio,
    Estado,
    Subtotal,
    Total
FROM dbo.OrdenesCompra WITH (UPDLOCK, HOLDLOCK)
WHERE idEmpresa = @IdEmpresa
  AND id = @IdOrdenCompra
  AND Activo = 1
  AND FechaArchivado IS NULL", connection, transaction);

            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@IdOrdenCompra", idOrdenCompra);

            using SqlDataReader reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return new OrdenCompraCabeceraPersistida();
            }

            return new OrdenCompraCabeceraPersistida
            {
                Id = ReadGuid(reader, "id"),
                Folio = ReadString(reader, "Folio"),
                Estado = ReadByte(reader, "Estado"),
                Subtotal = ReadDecimal(reader, "Subtotal"),
                Total = ReadDecimal(reader, "Total")
            };
        }

        private async Task<TotalesOrdenCompra> ObtenerTotalesActivosAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idOrdenCompra)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT
    ISNULL(SUM(Subtotal), 0) AS Subtotal,
    ISNULL(SUM(Total), 0) AS Total
FROM dbo.OrdenesCompraDetalle
WHERE idEmpresa = @IdEmpresa
  AND idOrdenCompra = @IdOrdenCompra
  AND Activo = 1
  AND FechaArchivado IS NULL", connection, transaction);

            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@IdOrdenCompra", idOrdenCompra);

            using SqlDataReader reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return new TotalesOrdenCompra();
            }

            return new TotalesOrdenCompra
            {
                Subtotal = ReadDecimal(reader, "Subtotal"),
                Total = ReadDecimal(reader, "Total")
            };
        }

        private async Task<int> CountPartidasActivasAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idOrdenCompra)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT COUNT(1)
FROM dbo.OrdenesCompraDetalle
WHERE idEmpresa = @IdEmpresa
  AND idOrdenCompra = @IdOrdenCompra
  AND Activo = 1
  AND FechaArchivado IS NULL", connection, transaction);

            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@IdOrdenCompra", idOrdenCompra);

            object? scalar = await command.ExecuteScalarAsync();
            return scalar != null && scalar != DBNull.Value ? Convert.ToInt32(scalar, CultureInfo.InvariantCulture) : 0;
        }

        private async Task<int> CountPartidasSinCostoValidoAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idOrdenCompra)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT COUNT(1)
FROM dbo.OrdenesCompraDetalle
WHERE idEmpresa = @IdEmpresa
  AND idOrdenCompra = @IdOrdenCompra
  AND Activo = 1
  AND FechaArchivado IS NULL
  AND (CostoUnitario <= 0 OR Subtotal <= 0 OR Total <= 0)", connection, transaction);

            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@IdOrdenCompra", idOrdenCompra);

            object? scalar = await command.ExecuteScalarAsync();
            return scalar != null && scalar != DBNull.Value ? Convert.ToInt32(scalar, CultureInfo.InvariantCulture) : 0;
        }

        private async Task<List<OrdenCompraComboDto>> ObtenerRazonesSocialesAsync(SqlConnection connection, Guid idEmpresa)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT id, Nombre, ISNULL(Notas, '') AS Descripcion
FROM dbo.RazonesSociales
WHERE idEmpresa = @IdEmpresa
  AND ISNULL(borrado, 0) = 0
ORDER BY Nombre", connection);

            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            return await ReadComboAsync(command, false);
        }

        private async Task<List<OrdenCompraComboDto>> ObtenerSucursalesAsync(SqlConnection connection, Guid idEmpresa)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT id, Nombre, ISNULL(Direccion, '') AS Descripcion
FROM dbo.Sucursales
WHERE idEmpresa = @IdEmpresa
  AND ISNULL(borrado, 0) = 0
ORDER BY Nombre", connection);

            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            return await ReadComboAsync(command, false);
        }

        private async Task<List<OrdenCompraComboDto>> ObtenerProveedoresAsync(SqlConnection connection, Guid idEmpresa)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT id, Codigo, Nombre, ISNULL(Descripcion, '') AS Descripcion, Activo
FROM dbo.ActivosProveedores
WHERE idEmpresa = @IdEmpresa
  AND Activo = 1
ORDER BY Nombre", connection);

            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            return await ReadComboAsync(command, true);
        }

        private static async Task<List<OrdenCompraComboDto>> ReadComboAsync(SqlCommand command, bool hasCodigo)
        {
            List<OrdenCompraComboDto> items = new List<OrdenCompraComboDto>();
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new OrdenCompraComboDto
                {
                    Id = ReadGuid(reader, "id"),
                    Codigo = hasCodigo ? ReadString(reader, "Codigo") : string.Empty,
                    Nombre = ReadString(reader, "Nombre"),
                    Descripcion = ReadString(reader, "Descripcion"),
                    Activo = !reader.HasRows || !HasColumn(reader, "Activo") || ReadBool(reader, "Activo")
                });
            }

            return items;
        }

        private async Task<bool> ExistsAsync(SqlConnection connection, SqlTransaction transaction, string sql, Guid idEmpresa, Guid id)
        {
            using SqlCommand command = new SqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@Id", id);
            object? scalar = await command.ExecuteScalarAsync();
            return scalar != null && scalar != DBNull.Value && Convert.ToInt32(scalar, CultureInfo.InvariantCulture) > 0;
        }

        private async Task<string> ReserveNextFolioAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, DateTime utcNow)
        {
            using SqlCommand seedCommand = new SqlCommand(@"
IF NOT EXISTS (
    SELECT 1
    FROM dbo.OrdenesCompraFolios WITH (UPDLOCK, HOLDLOCK)
    WHERE idEmpresa = @IdEmpresa
)
BEGIN
    INSERT INTO dbo.OrdenesCompraFolios (id, idEmpresa, identityKey, UltimoConsecutivo, FechaCreacion, FechaActualizacion)
    VALUES (NEWID(), @IdEmpresa, NEWID(), 0, @FechaCreacion, @FechaActualizacion);
END", connection, transaction);

            seedCommand.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            seedCommand.Parameters.AddWithValue("@FechaCreacion", utcNow);
            seedCommand.Parameters.AddWithValue("@FechaActualizacion", utcNow);
            await seedCommand.ExecuteNonQueryAsync();

            using SqlCommand updateCommand = new SqlCommand(@"
UPDATE dbo.OrdenesCompraFolios
SET UltimoConsecutivo = UltimoConsecutivo + 1,
    FechaActualizacion = @FechaActualizacion
OUTPUT INSERTED.UltimoConsecutivo
WHERE idEmpresa = @IdEmpresa", connection, transaction);

            updateCommand.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            updateCommand.Parameters.AddWithValue("@FechaActualizacion", utcNow);

            object? scalar = await updateCommand.ExecuteScalarAsync();
            long consecutivo = scalar != null && scalar != DBNull.Value
                ? Convert.ToInt64(scalar, CultureInfo.InvariantCulture)
                : 0L;

            return $"OC-{consecutivo.ToString($"D{FolioPadding}", CultureInfo.InvariantCulture)}";
        }

        private string ValidateGuardarRequest(OrdenCompraGuardarRequest request, Guid idEmpresa)
        {
            if (request == null)
            {
                return "Ocurrió un error al procesar la solicitud.";
            }

            if (request.IdEmpresa != Guid.Empty && request.IdEmpresa != idEmpresa)
            {
                return "La empresa solicitada no coincide con la sesión activa.";
            }

            if (request.IdRazonSocial == Guid.Empty)
            {
                return "La razón social no está disponible.";
            }

            if (request.IdSucursal == Guid.Empty)
            {
                return "La sucursal no está disponible.";
            }

            if (request.IdProveedor == Guid.Empty)
            {
                return "El proveedor no está disponible.";
            }

            if (request.FechaOrden == default)
            {
                return "La fecha de la orden es obligatoria.";
            }

            if (request.FechaLlegada.HasValue && request.FechaLlegada.Value.Date < request.FechaOrden.Date)
            {
                return "La fecha de llegada no puede ser anterior a la fecha de la orden.";
            }

            if (!string.IsNullOrWhiteSpace(request.Observaciones) && request.Observaciones.Trim().Length > ObservacionesLength)
            {
                return "Las observaciones exceden la longitud permitida.";
            }

            if (request.Partidas == null || request.Partidas.Count == 0)
            {
                return "La orden debe contener al menos una partida.";
            }

            return string.Empty;
        }

        private void ValidateRequestPartidas(List<OrdenCompraPartidaGuardarRequest> partidas)
        {
            HashSet<Guid> ids = new HashSet<Guid>();

            foreach (OrdenCompraPartidaGuardarRequest partida in partidas)
            {
                if (partida == null || partida.IdProductoServicio == Guid.Empty)
                {
                    throw new CatalogoValidationException("El producto o servicio no está disponible.");
                }

                if (!ids.Add(partida.IdProductoServicio))
                {
                    throw new CatalogoValidationException("No se permiten partidas duplicadas.");
                }

                if (partida.Cantidad <= 0m)
                {
                    throw new CatalogoValidationException("La orden debe contener al menos una partida.");
                }

                if (!HasScale(partida.Cantidad, 4))
                {
                    throw new CatalogoValidationException("La cantidad tiene un formato inválido.");
                }

                if (partida.CostoUnitario < 0m)
                {
                    throw new CatalogoValidationException("El costo unitario debe ser mayor o igual a cero.");
                }

                if (!HasScale(partida.CostoUnitario, 2))
                {
                    throw new CatalogoValidationException("El costo unitario tiene un formato inválido.");
                }
            }
        }

        private static TotalesOrdenCompra CalculateTotals(List<OrdenCompraPartidaPersistencia> partidas)
        {
            TotalesOrdenCompra totals = new TotalesOrdenCompra();
            foreach (OrdenCompraPartidaPersistencia partida in partidas)
            {
                totals.Subtotal += partida.Subtotal;
                totals.Total += partida.Total;
            }

            totals.Subtotal = NormalizeMoney(totals.Subtotal);
            totals.Total = NormalizeMoney(totals.Total);
            return totals;
        }

        private bool TryResolveRequestContext(Guid? clientEmpresaId, string? clientEmpresaKey, out RequestContext context, out IActionResult? error)
        {
            context = null!;
            error = null;

            Guid? effectiveEmpresaId = TryResolveEmpresaId(out string? proxyEmpresaKey);
            if (!effectiveEmpresaId.HasValue || effectiveEmpresaId.Value == Guid.Empty)
            {
                error = Unauthorized(new OrdenCompraOperacionResponse { Mensaje = "No fue posible resolver la empresa activa." });
                return false;
            }

            if (clientEmpresaId.HasValue && clientEmpresaId.Value != Guid.Empty && clientEmpresaId.Value != effectiveEmpresaId.Value)
            {
                error = BadRequest(new OrdenCompraOperacionResponse { Mensaje = "La empresa solicitada no coincide con la sesión activa." });
                return false;
            }

            string empresaStorageKey = TryResolveEmpresaStorageKey(effectiveEmpresaId.Value, proxyEmpresaKey);
            if (!string.IsNullOrWhiteSpace(clientEmpresaKey) &&
                !string.Equals(clientEmpresaKey.Trim(), empresaStorageKey, StringComparison.OrdinalIgnoreCase))
            {
                error = BadRequest(new OrdenCompraOperacionResponse { Mensaje = "La empresa solicitada no coincide con la sesión activa." });
                return false;
            }

            context = new RequestContext
            {
                IdEmpresa = effectiveEmpresaId.Value,
                EmpresaStorageKey = empresaStorageKey
            };
            return true;
        }

        private Guid? TryResolveEmpresaId(out string? proxyEmpresaKey)
        {
            proxyEmpresaKey = null;

            foreach (string claimKey in EmpresaClaimKeys)
            {
                string? value = User.FindFirstValue(claimKey);
                if (Guid.TryParse(value, out Guid parsed) && parsed != Guid.Empty)
                {
                    return parsed;
                }
            }

            if (TryResolveSignedProxyContext(out SignedProxyContext? proxyContext) && proxyContext != null)
            {
                proxyEmpresaKey = proxyContext.EmpresaStorageKey;
                return proxyContext.IdEmpresa;
            }

            return null;
        }

        private string TryResolveEmpresaStorageKey(Guid empresaId, string? proxyEmpresaKey = null)
        {
            foreach (string claimKey in EmpresaNombreClaimKeys)
            {
                string? value = User.FindFirstValue(claimKey);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim().ToUpperInvariant();
                }
            }

            if (!string.IsNullOrWhiteSpace(proxyEmpresaKey))
            {
                return proxyEmpresaKey.Trim().ToUpperInvariant();
            }

            return empresaId.ToString("N").ToUpperInvariant();
        }

        private Guid? TryResolveUsuarioId()
        {
            foreach (string claimKey in UsuarioClaimKeys)
            {
                string? value = User.FindFirstValue(claimKey);
                if (Guid.TryParse(value, out Guid parsed) && parsed != Guid.Empty)
                {
                    return parsed;
                }
            }

            if (TryResolveSignedProxyContext(out SignedProxyContext? proxyContext) &&
                proxyContext != null &&
                proxyContext.UsuarioId.HasValue)
            {
                return proxyContext.UsuarioId.Value;
            }

            return null;
        }

        private bool TryResolveSignedProxyContext(out SignedProxyContext? context)
        {
            if (HttpContext.Items.TryGetValue(ProxyContextItemKey, out object? cached))
            {
                context = cached as SignedProxyContext;
                return context != null;
            }

            context = null;

            if (!Request.Headers.TryGetValue(ProxyEmpresaIdHeader, out var empresaIdHeader) ||
                !Request.Headers.TryGetValue(ProxyEmpresaKeyHeader, out var empresaKeyHeader) ||
                !Request.Headers.TryGetValue(ProxyTimestampHeader, out var timestampHeader) ||
                !Request.Headers.TryGetValue(ProxySignatureHeader, out var signatureHeader))
            {
                return false;
            }

            string secret = _configuration["fireBdata:fireClave"] ?? string.Empty;
            if (string.IsNullOrWhiteSpace(secret))
            {
                _logger.LogWarning("OrdenesCompra proxy headers recibidos sin secreto compartido configurado.");
                return false;
            }

            string empresaIdRaw = empresaIdHeader.ToString().Trim();
            string empresaKeyRaw = empresaKeyHeader.ToString().Trim();
            string usuarioIdRaw = Request.Headers.TryGetValue(ProxyUsuarioIdHeader, out var usuarioIdHeader)
                ? usuarioIdHeader.ToString().Trim()
                : string.Empty;
            string timestampRaw = timestampHeader.ToString().Trim();
            string signatureRaw = signatureHeader.ToString().Trim();

            if (!Guid.TryParse(empresaIdRaw, out Guid empresaId) || empresaId == Guid.Empty)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(empresaKeyRaw) ||
                !DateTimeOffset.TryParseExact(timestampRaw, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset timestamp))
            {
                return false;
            }

            TimeSpan age = DateTimeOffset.UtcNow - timestamp.ToUniversalTime();
            if (age.Duration() > ProxyHeaderTolerance)
            {
                _logger.LogWarning("OrdenesCompra proxy headers expirados o fuera de tolerancia para empresa {EmpresaId}.", empresaId);
                return false;
            }

            string payload = BuildProxySignaturePayload(empresaIdRaw, empresaKeyRaw, usuarioIdRaw, timestampRaw);
            string expectedSignature = ComputeProxySignature(secret, payload);

            if (!SignaturesMatch(expectedSignature, signatureRaw))
            {
                _logger.LogWarning("OrdenesCompra proxy headers con firma invalida para empresa {EmpresaId}.", empresaId);
                return false;
            }

            context = new SignedProxyContext
            {
                IdEmpresa = empresaId,
                EmpresaStorageKey = empresaKeyRaw.ToUpperInvariant(),
                UsuarioId = Guid.TryParse(usuarioIdRaw, out Guid usuarioId) && usuarioId != Guid.Empty ? usuarioId : null
            };

            HttpContext.Items[ProxyContextItemKey] = context;
            return true;
        }

        private static string BuildProxySignaturePayload(string empresaId, string empresaKey, string usuarioId, string timestamp)
        {
            return string.Join('\n', empresaId.Trim(), empresaKey.Trim().ToUpperInvariant(), usuarioId.Trim(), timestamp.Trim());
        }

        private static string ComputeProxySignature(string secret, string payload)
        {
            using HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToBase64String(hash);
        }

        private static bool SignaturesMatch(string expectedSignature, string providedSignature)
        {
            byte[] expectedBytes = Encoding.UTF8.GetBytes(expectedSignature);
            byte[] providedBytes = Encoding.UTF8.GetBytes(providedSignature);
            return CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
        }

        private SqlConnection CreateConnection()
        {
            return _connectionFactory.CreateConnection();
        }

        private IActionResult HandleException(Exception ex, string operation, string safeMessage)
        {
            _logger.LogError(ex, "Error en OrdenesCompra durante {Operation}.", operation);
            return StatusCode(500, new OrdenCompraOperacionResponse { Mensaje = safeMessage });
        }

        private static void AppendGuidFilter(StringBuilder query, SqlCommand command, string columnName, string parameterName, Guid? value)
        {
            if (value.HasValue && value.Value != Guid.Empty)
            {
                query.Append($" AND {columnName} = {parameterName}");
                command.Parameters.AddWithValue(parameterName, value.Value);
            }
        }

        private static void AppendTinyIntFilter(StringBuilder query, SqlCommand command, string columnName, string parameterName, byte? value)
        {
            if (value.HasValue)
            {
                query.Append($" AND {columnName} = {parameterName}");
                command.Parameters.AddWithValue(parameterName, value.Value);
            }
        }

        private static void AppendFechaDesdeFilter(StringBuilder query, SqlCommand command, string columnName, string parameterName, DateTime? value)
        {
            if (value.HasValue)
            {
                query.Append($" AND {columnName} >= {parameterName}");
                command.Parameters.AddWithValue(parameterName, value.Value.Date);
            }
        }

        private static void AppendFechaHastaFilter(StringBuilder query, SqlCommand command, string columnName, string parameterName, DateTime? value)
        {
            if (value.HasValue)
            {
                query.Append($" AND {columnName} < DATEADD(DAY, 1, {parameterName})");
                command.Parameters.AddWithValue(parameterName, value.Value.Date);
            }
        }

        private static decimal NormalizeMoney(decimal value)
        {
            return decimal.Round(value, 2, MidpointRounding.AwayFromZero);
        }

        private static decimal NormalizeQuantity(decimal value)
        {
            return decimal.Round(value, 4, MidpointRounding.AwayFromZero);
        }

        private static bool HasScale(decimal value, int maxScale)
        {
            int[] bits = decimal.GetBits(value);
            byte scale = (byte)((bits[3] >> 16) & 0x7F);
            return scale <= maxScale;
        }

        private static string Truncate(string value, int maxLength)
        {
            string normalized = (value ?? string.Empty).Trim();
            return normalized.Length > maxLength ? normalized[..maxLength] : normalized;
        }

        private static string? NormalizeNullableText(string? value, int maxLength)
        {
            string normalized = Truncate(value ?? string.Empty, maxLength);
            return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
        }

        private static string? NullIfEmpty(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private static int NormalizeLimit(int limite)
        {
            if (limite <= 0)
            {
                return 25;
            }

            return limite > 100 ? 100 : limite;
        }

        private static string GetEstadoNombre(byte estado)
        {
            return estado switch
            {
                EstadoBorrador => "Borrador",
                EstadoGenerada => "Generada",
                EstadoCancelada => "Cancelada",
                _ => "Desconocido"
            };
        }

        private static string GetEstadoNombreUsuario(byte estado)
        {
            return estado switch
            {
                EstadoBorrador => "En captura",
                EstadoGenerada => "Confirmada",
                EstadoCancelada => "Detenida",
                _ => "Desconocido"
            };
        }

        private static string GetTipoNombre(byte tipo)
        {
            return tipo == TipoServicio ? "Servicio" : "Producto";
        }

        private static Guid ReadGuid(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return !reader.IsDBNull(ordinal) ? reader.GetGuid(ordinal) : Guid.Empty;
        }

        private static Guid? ReadNullableGuid(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetGuid(ordinal);
        }

        private static string ReadString(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
        }

        private static byte ReadByte(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return !reader.IsDBNull(ordinal) ? reader.GetByte(ordinal) : (byte)0;
        }

        private static bool ReadBool(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return !reader.IsDBNull(ordinal) && reader.GetBoolean(ordinal);
        }

        private static int ReadInt(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? 0 : Convert.ToInt32(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
        }

        private static decimal ReadDecimal(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? 0m : reader.GetDecimal(ordinal);
        }

        private static decimal? ReadNullableDecimal(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
        }

        private static DateTime ReadDateTime(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? DateTime.MinValue : reader.GetDateTime(ordinal);
        }

        private static DateTime? ReadNullableDateTime(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
        }

        private static bool HasColumn(SqlDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (string.Equals(reader.GetName(i), columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private sealed class RequestContext
        {
            public Guid IdEmpresa { get; set; }
            public string EmpresaStorageKey { get; set; } = string.Empty;
        }

        private sealed class SignedProxyContext
        {
            public Guid IdEmpresa { get; set; }
            public string EmpresaStorageKey { get; set; } = string.Empty;
            public Guid? UsuarioId { get; set; }
        }

        private sealed class CatalogoValidationException : Exception
        {
            public CatalogoValidationException(string publicMessage) : base(publicMessage)
            {
                PublicMessage = publicMessage;
            }

            public string PublicMessage { get; }
        }

        private sealed class ProductoServicioSnapshot
        {
            public Guid Id { get; set; }
            public byte Tipo { get; set; }
            public string Codigo { get; set; } = string.Empty;
            public string Nombre { get; set; } = string.Empty;
            public string Descripcion { get; set; } = string.Empty;
            public Guid IdUnidadMedida { get; set; }
            public string UnidadMedida { get; set; } = string.Empty;
            public string UnidadAbreviatura { get; set; } = string.Empty;
        }

        private sealed class OrdenCompraPartidaPersistencia
        {
            public Guid Id { get; set; }
            public int NumeroPartida { get; set; }
            public Guid IdProductoServicio { get; set; }
            public byte TipoProductoServicio { get; set; }
            public string Codigo { get; set; } = string.Empty;
            public string Nombre { get; set; } = string.Empty;
            public string Descripcion { get; set; } = string.Empty;
            public Guid IdUnidadMedida { get; set; }
            public string UnidadMedida { get; set; } = string.Empty;
            public string UnidadAbreviatura { get; set; } = string.Empty;
            public decimal Cantidad { get; set; }
            public decimal CostoUnitario { get; set; }
            public decimal Subtotal { get; set; }
            public decimal Total { get; set; }
        }

        private sealed class TotalesOrdenCompra
        {
            public decimal Subtotal { get; set; }
            public decimal Total { get; set; }
        }

        private sealed class OrdenCompraCabeceraPersistida
        {
            public Guid Id { get; set; }
            public string Folio { get; set; } = string.Empty;
            public byte Estado { get; set; }
            public decimal Subtotal { get; set; }
            public decimal Total { get; set; }
        }
    }
}
