using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using checklistWs.Models.Configuracion;
using checklistWs.Models.Cotizaciones;
using checklistWs.Services;
using checklistWs.Utiles;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace checklistWs.Controllers.Cotizaciones
{
    [Route("api/[controller]")]
    [ApiController]
    public class CotizacionesController : ControllerBase
    {
        private const byte TipoProducto = 1;
        private const int FolioPadding = 6;
        private const int BusquedaLength = 200;
        private const int ObservacionesLength = 1000;
        private const int CajaLength = 100;
        private const int MotivoCancelacionLength = 500;
        private const string ProxyEmpresaIdHeader = "X-ProductosServicios-Proxy-EmpresaId";
        private const string ProxyEmpresaKeyHeader = "X-ProductosServicios-Proxy-Empresa";
        private const string ProxyUsuarioIdHeader = "X-ProductosServicios-Proxy-UsuarioId";
        private const string ProxyTimestampHeader = "X-ProductosServicios-Proxy-Timestamp";
        private const string ProxySignatureHeader = "X-ProductosServicios-Proxy-Signature";
        private const string ProxyCorreoHeader = "X-Cotizaciones-Proxy-Correo";
        private static readonly TimeSpan ProxyHeaderTolerance = TimeSpan.FromMinutes(5);

        private readonly IConfiguration _configuration;
        private readonly SqlConnectionFactory _connectionFactory;
        private readonly IDataProtector _protector;
        private readonly DocumentEmailService _documentEmailService;
        private readonly ILogger<CotizacionesController> _logger;

        public CotizacionesController(
            IConfiguration configuration,
            ILogger<CotizacionesController> logger,
            IDataProtectionProvider dataProtectionProvider,
            DocumentEmailService documentEmailService)
        {
            _configuration = configuration;
            _connectionFactory = new SqlConnectionFactory(configuration);
            _protector = dataProtectionProvider.CreateProtector("checklistWs.Configuracion.CorreoSaliente.Password.v1");
            _documentEmailService = documentEmailService;
            _logger = logger;
        }

        [HttpGet("ObtenerCotizaciones")]
        public async Task<IActionResult> ObtenerCotizaciones(
            Guid idEmpresa,
            string busqueda = "",
            byte? estado = null,
            DateTime? fechaDesde = null,
            DateTime? fechaHasta = null)
        {
            if (!TryResolveRequestContext(idEmpresa, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                using SqlConnection connection = CreateConnection();
                await connection.OpenAsync();
                await EnsureSchemaAsync(connection);

                StringBuilder query = new StringBuilder(@"
SELECT
    c.id,
    c.Folio,
    c.FechaCotizacion,
    c.FechaVigencia,
    c.idCliente,
    ISNULL(cl.Nombre, '') AS Cliente,
    c.idSucursal,
    ISNULL(su.Nombre, '') AS Sucursal,
    ISNULL(c.Vendedor, '') AS Vendedor,
    c.Estado,
    c.Total,
    c.TotalPiezas,
    c.FechaCreacion
FROM dbo.Cotizaciones c
INNER JOIN dbo.Clientes cl
    ON cl.id = c.idCliente AND cl.idEmpresa = c.idEmpresa
LEFT JOIN dbo.Sucursales su
    ON su.id = c.idSucursal AND su.idEmpresa = c.idEmpresa
WHERE c.idEmpresa = @IdEmpresa
  AND c.Activo = 1
  AND c.FechaArchivado IS NULL");

                using SqlCommand command = new SqlCommand();
                command.Connection = connection;
                command.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);

                if (!string.IsNullOrWhiteSpace(busqueda))
                {
                    string term = $"%{Truncate(busqueda.Trim(), BusquedaLength)}%";
                    query.Append(@"
  AND (
        c.Folio LIKE @Busqueda
        OR ISNULL(cl.Nombre, '') LIKE @Busqueda
        OR ISNULL(c.Vendedor, '') LIKE @Busqueda
        OR ISNULL(c.Observaciones, '') LIKE @Busqueda
      )");
                    command.Parameters.AddWithValue("@Busqueda", term);
                }

                if (estado.HasValue)
                {
                    query.Append(" AND c.Estado = @Estado");
                    command.Parameters.AddWithValue("@Estado", estado.Value);
                }

                if (fechaDesde.HasValue)
                {
                    query.Append(" AND c.FechaCotizacion >= @FechaDesde");
                    command.Parameters.AddWithValue("@FechaDesde", fechaDesde.Value.Date);
                }

                if (fechaHasta.HasValue)
                {
                    query.Append(" AND c.FechaCotizacion < @FechaHasta");
                    command.Parameters.AddWithValue("@FechaHasta", fechaHasta.Value.Date.AddDays(1));
                }

                query.Append(" ORDER BY c.FechaCotizacion DESC, c.FechaCreacion DESC");
                command.CommandText = query.ToString();

                List<CotizacionListadoDto> items = new List<CotizacionListadoDto>();
                using SqlDataReader reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    byte estadoActual = ReadByte(reader, "Estado");
                    bool editable = estadoActual == CotizacionEstados.Borrador;
                    items.Add(new CotizacionListadoDto
                    {
                        Id = ReadGuid(reader, "id"),
                        Folio = ReadString(reader, "Folio"),
                        FechaCotizacion = ReadDateTime(reader, "FechaCotizacion"),
                        FechaVigencia = ReadNullableDateTime(reader, "FechaVigencia"),
                        IdCliente = ReadGuid(reader, "idCliente"),
                        Cliente = ReadString(reader, "Cliente"),
                        IdSucursal = ReadNullableGuid(reader, "idSucursal"),
                        Sucursal = ReadString(reader, "Sucursal"),
                        Vendedor = ReadString(reader, "Vendedor"),
                        Estado = estadoActual,
                        EstadoNombre = GetEstadoNombre(estadoActual),
                        Total = ReadDecimal(reader, "Total"),
                        TotalPiezas = ReadDecimal(reader, "TotalPiezas"),
                        FechaCreacion = ReadDateTime(reader, "FechaCreacion"),
                        PuedeEditar = editable,
                        PuedeCancelar = editable,
                        PuedeClonar = true,
                        PuedeExportarPdf = true,
                        PuedeAutorizar = editable
                    });
                }

                return Ok(items);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerCotizaciones", "No fue posible cargar las cotizaciones.");
            }
        }

        [HttpGet("ObtenerResumenCotizaciones")]
        public async Task<IActionResult> ObtenerResumenCotizaciones(
            Guid idEmpresa,
            DateTime? fechaDesde = null,
            DateTime? fechaHasta = null)
        {
            if (!TryResolveRequestContext(idEmpresa, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                using SqlConnection connection = CreateConnection();
                await connection.OpenAsync();
                await EnsureSchemaAsync(connection);

                StringBuilder query = new StringBuilder(@"
SELECT
    COUNT(1) AS Total,
    SUM(CASE WHEN Estado = @EstadoBorrador THEN 1 ELSE 0 END) AS Borradores,
    SUM(CASE WHEN Estado = @EstadoCancelada THEN 1 ELSE 0 END) AS Canceladas,
    SUM(Total) AS ImporteTotal
FROM dbo.Cotizaciones
WHERE idEmpresa = @IdEmpresa
  AND Activo = 1
  AND FechaArchivado IS NULL");

                using SqlCommand command = new SqlCommand();
                command.Connection = connection;
                command.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);
                command.Parameters.AddWithValue("@EstadoBorrador", CotizacionEstados.Borrador);
                command.Parameters.AddWithValue("@EstadoCancelada", CotizacionEstados.Cancelada);

                if (fechaDesde.HasValue)
                {
                    query.Append(" AND FechaCotizacion >= @FechaDesde");
                    command.Parameters.AddWithValue("@FechaDesde", fechaDesde.Value.Date);
                }

                if (fechaHasta.HasValue)
                {
                    query.Append(" AND FechaCotizacion < @FechaHasta");
                    command.Parameters.AddWithValue("@FechaHasta", fechaHasta.Value.Date.AddDays(1));
                }

                command.CommandText = query.ToString();

                using SqlDataReader reader = await command.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    return Ok(new CotizacionResumenDto());
                }

                return Ok(new CotizacionResumenDto
                {
                    Total = ReadInt(reader, "Total"),
                    Borradores = ReadInt(reader, "Borradores"),
                    Canceladas = ReadInt(reader, "Canceladas"),
                    ImporteTotal = ReadDecimal(reader, "ImporteTotal")
                });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerResumenCotizaciones", "No fue posible cargar el resumen de cotizaciones.");
            }
        }

        [HttpGet("ObtenerCotizacion")]
        public async Task<IActionResult> ObtenerCotizacion(Guid idEmpresa, Guid idCotizacion)
        {
            if (!TryResolveRequestContext(idEmpresa, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            if (idCotizacion == Guid.Empty)
            {
                return BadRequest(new CotizacionOperacionResponse { Mensaje = "La cotización solicitada no es válida." });
            }

            try
            {
                using SqlConnection connection = CreateConnection();
                await connection.OpenAsync();
                await EnsureSchemaAsync(connection);

                using SqlCommand command = new SqlCommand(@"
SELECT
    c.id,
    c.idEmpresa,
    c.identityKey,
    c.Folio,
    c.FechaCotizacion,
    c.VigenciaDias,
    c.FechaVigencia,
    c.idCliente,
    ISNULL(cl.Nombre, '') AS Cliente,
    ISNULL(cl.Telefono, '') AS ClienteTelefono,
    ISNULL(cl.Correo, '') AS ClienteCorreo,
    ISNULL(cl.Descuento, 0) AS ClienteDescuento,
    c.idSucursal,
    ISNULL(su.Nombre, '') AS Sucursal,
    ISNULL(c.Vendedor, '') AS Vendedor,
    ISNULL(c.Caja, '') AS Caja,
    c.Estado,
    ISNULL(c.Observaciones, '') AS Observaciones,
    c.Subtotal,
    c.DescuentoTotal,
    c.Total,
    c.TotalPiezas,
    ISNULL(c.MotivoCancelacion, '') AS MotivoCancelacion,
    c.FechaCancelacion,
    c.FechaCreacion,
    c.FechaActualizacion
FROM dbo.Cotizaciones c
INNER JOIN dbo.Clientes cl
    ON cl.id = c.idCliente AND cl.idEmpresa = c.idEmpresa
LEFT JOIN dbo.Sucursales su
    ON su.id = c.idSucursal AND su.idEmpresa = c.idEmpresa
WHERE c.idEmpresa = @IdEmpresa
  AND c.id = @IdCotizacion
  AND c.Activo = 1
  AND c.FechaArchivado IS NULL", connection);

                command.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);
                command.Parameters.AddWithValue("@IdCotizacion", idCotizacion);

                CotizacionDetalleDto? detalle = null;
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (!await reader.ReadAsync())
                    {
                        return NotFound(new CotizacionOperacionResponse { Mensaje = "La cotización no está disponible." });
                    }

                    byte estadoActual = ReadByte(reader, "Estado");
                    detalle = new CotizacionDetalleDto
                    {
                        Id = ReadGuid(reader, "id"),
                        IdEmpresa = ReadGuid(reader, "idEmpresa"),
                        IdentityKey = ReadGuid(reader, "identityKey"),
                        Folio = ReadString(reader, "Folio"),
                        FechaCotizacion = ReadDateTime(reader, "FechaCotizacion"),
                        VigenciaDias = ReadInt(reader, "VigenciaDias"),
                        FechaVigencia = ReadNullableDateTime(reader, "FechaVigencia"),
                        IdCliente = ReadGuid(reader, "idCliente"),
                        Cliente = ReadString(reader, "Cliente"),
                        ClienteTelefono = ReadString(reader, "ClienteTelefono"),
                        ClienteCorreo = ReadString(reader, "ClienteCorreo"),
                        ClienteDescuento = ReadDecimal(reader, "ClienteDescuento"),
                        IdSucursal = ReadNullableGuid(reader, "idSucursal"),
                        Sucursal = ReadString(reader, "Sucursal"),
                        Vendedor = ReadString(reader, "Vendedor"),
                        Caja = ReadString(reader, "Caja"),
                        Estado = estadoActual,
                        EstadoNombre = GetEstadoNombre(estadoActual),
                        Observaciones = ReadString(reader, "Observaciones"),
                        Subtotal = ReadDecimal(reader, "Subtotal"),
                        DescuentoTotal = ReadDecimal(reader, "DescuentoTotal"),
                        Total = ReadDecimal(reader, "Total"),
                        TotalPiezas = ReadDecimal(reader, "TotalPiezas"),
                        MotivoCancelacion = ReadString(reader, "MotivoCancelacion"),
                        FechaCancelacion = ReadNullableDateTime(reader, "FechaCancelacion"),
                        FechaCreacion = ReadDateTime(reader, "FechaCreacion"),
                        FechaActualizacion = ReadDateTime(reader, "FechaActualizacion")
                    };
                }

                detalle.Partidas = await ObtenerPartidasAsync(connection, context.IdEmpresa, idCotizacion);
                return Ok(detalle);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerCotizacion", "No fue posible cargar la cotización.");
            }
        }

        [HttpPost("GuardarCotizacion")]
        public async Task<IActionResult> GuardarCotizacion([FromBody] CotizacionGuardarRequest request, Guid idEmpresa)
        {
            if (!TryResolveRequestContext(idEmpresa, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            if (request == null)
            {
                return BadRequest(new CotizacionOperacionResponse { Mensaje = "No fue posible leer la cotización." });
            }

            if (request.IdEmpresa != Guid.Empty && request.IdEmpresa != context.IdEmpresa)
            {
                return BadRequest(new CotizacionOperacionResponse { Mensaje = "La empresa solicitada no coincide con la sesión activa." });
            }

            string? validation = ValidateRequest(request);
            if (!string.IsNullOrWhiteSpace(validation))
            {
                return BadRequest(new CotizacionOperacionResponse { Mensaje = validation });
            }

            try
            {
                using SqlConnection connection = CreateConnection();
                await connection.OpenAsync();
                await EnsureSchemaAsync(connection);
                using SqlTransaction transaction = connection.BeginTransaction();

                ClienteContext cliente = await ObtenerClienteAsync(connection, transaction, context.IdEmpresa, request.IdCliente);
                if (cliente.Id == Guid.Empty)
                {
                    transaction.Rollback();
                    return BadRequest(new CotizacionOperacionResponse { Mensaje = "El cliente seleccionado no está disponible." });
                }

                UserMetadata usuario = await ResolveUserMetadataAsync(connection, transaction, context);
                Guid? sucursalId = request.IdSucursal ?? usuario.IdSucursal;
                string vendedor = usuario.Nombre;

                List<CotizacionPartidaDbRow> partidas = await NormalizePartidasAsync(connection, transaction, context.IdEmpresa, request.Partidas, cliente.Descuento);
                if (partidas.Count == 0)
                {
                    transaction.Rollback();
                    return BadRequest(new CotizacionOperacionResponse { Mensaje = "Agrega al menos un producto válido a la cotización." });
                }

                TotalesCotizacion totals = BuildTotales(partidas);
                Guid cotizacionId = request.Id ?? Guid.NewGuid();
                bool isEdit = request.Id.HasValue && request.Id.Value != Guid.Empty;
                DateTime now = DateTime.UtcNow;
                int vigenciaDias = Math.Max(0, request.VigenciaDias ?? 0);
                DateTime fechaCotizacion = now;
                DateTime? fechaVigencia = vigenciaDias > 0 ? now.Date.AddDays(vigenciaDias) : null;
                string folio;

                if (isEdit)
                {
                    CotizacionPersistedRow persisted = await ObtenerCotizacionPersistidaAsync(connection, transaction, context.IdEmpresa, cotizacionId);
                    if (persisted.Id == Guid.Empty)
                    {
                        transaction.Rollback();
                        return NotFound(new CotizacionOperacionResponse { Mensaje = "La cotización no está disponible." });
                    }

                    if (persisted.Estado != CotizacionEstados.Borrador)
                    {
                        transaction.Rollback();
                        return BadRequest(new CotizacionOperacionResponse { Mensaje = "Solo se pueden editar cotizaciones en borrador." });
                    }

                    folio = persisted.Folio;
                    fechaCotizacion = persisted.FechaCotizacion;
                    if (persisted.IdSucursal.HasValue && !sucursalId.HasValue)
                    {
                        sucursalId = persisted.IdSucursal;
                    }

                    using SqlCommand update = new SqlCommand(@"
UPDATE dbo.Cotizaciones
SET idCliente = @IdCliente,
    idSucursal = @IdSucursal,
    Vendedor = @Vendedor,
    Caja = @Caja,
    Observaciones = @Observaciones,
    VigenciaDias = @VigenciaDias,
    FechaVigencia = @FechaVigencia,
    Subtotal = @Subtotal,
    DescuentoTotal = @DescuentoTotal,
    Total = @Total,
    TotalPiezas = @TotalPiezas,
    FechaActualizacion = @FechaActualizacion
WHERE idEmpresa = @IdEmpresa
  AND id = @IdCotizacion", connection, transaction);
                    FillCotizacionParameters(update, context.IdEmpresa, cotizacionId, request.IdCliente, sucursalId, vendedor, request.Caja, request.Observaciones, vigenciaDias, fechaVigencia, totals, now);
                    await update.ExecuteNonQueryAsync();

                    using SqlCommand clearPartidas = new SqlCommand(@"
DELETE FROM dbo.CotizacionesPartidas
WHERE idEmpresa = @IdEmpresa
  AND idCotizacion = @IdCotizacion", connection, transaction);
                    clearPartidas.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);
                    clearPartidas.Parameters.AddWithValue("@IdCotizacion", cotizacionId);
                    await clearPartidas.ExecuteNonQueryAsync();
                }
                else
                {
                    folio = await GenerateFolioAsync(connection, transaction, context.IdEmpresa);
                    using SqlCommand insert = new SqlCommand(@"
INSERT INTO dbo.Cotizaciones
(
    id, idEmpresa, identityKey, Folio, Estado, FechaCotizacion, VigenciaDias, FechaVigencia, idCliente, idSucursal,
    Vendedor, Caja, Observaciones, Subtotal, DescuentoTotal, Total, TotalPiezas, MotivoCancelacion, FechaCancelacion,
    idUsuarioCreacion, idUsuarioActualizacion, idUsuarioCancelacion, FechaCreacion, FechaActualizacion, FechaArchivado, Activo
)
VALUES
(
    @IdCotizacion, @IdEmpresa, @IdentityKey, @Folio, @Estado, @FechaCotizacion, @VigenciaDias, @FechaVigencia, @IdCliente, @IdSucursal,
    @Vendedor, @Caja, @Observaciones, @Subtotal, @DescuentoTotal, @Total, @TotalPiezas, N'', NULL,
    @IdUsuario, @IdUsuario, NULL, @FechaCreacion, @FechaActualizacion, NULL, 1
)", connection, transaction);
                    insert.Parameters.AddWithValue("@IdCotizacion", cotizacionId);
                    insert.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);
                    insert.Parameters.AddWithValue("@IdentityKey", Guid.NewGuid());
                    insert.Parameters.AddWithValue("@Folio", folio);
                    insert.Parameters.AddWithValue("@Estado", CotizacionEstados.Borrador);
                    insert.Parameters.AddWithValue("@FechaCotizacion", fechaCotizacion);
                    insert.Parameters.AddWithValue("@IdUsuario", (object?)context.UsuarioId ?? DBNull.Value);
                    FillCotizacionParameters(insert, context.IdEmpresa, cotizacionId, request.IdCliente, sucursalId, vendedor, request.Caja, request.Observaciones, vigenciaDias, fechaVigencia, totals, now);
                    await insert.ExecuteNonQueryAsync();
                }

                await InsertarPartidasAsync(connection, transaction, context.IdEmpresa, cotizacionId, partidas, now);
                transaction.Commit();

                return Ok(new CotizacionOperacionResponse
                {
                    Exito = true,
                    Mensaje = isEdit ? "La cotización se actualizó correctamente." : "La cotización se guardó correctamente.",
                    IdCotizacion = cotizacionId,
                    Folio = folio,
                    Estado = CotizacionEstados.Borrador,
                    EstadoNombre = GetEstadoNombre(CotizacionEstados.Borrador),
                    Subtotal = totals.Subtotal,
                    DescuentoTotal = totals.DescuentoTotal,
                    Total = totals.Total
                });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "GuardarCotizacion", "No fue posible guardar la cotización.");
            }
        }

        [HttpPost("CancelarCotizacion")]
        public async Task<IActionResult> CancelarCotizacion([FromBody] CotizacionCancelarRequest request, Guid idEmpresa)
        {
            if (!TryResolveRequestContext(idEmpresa, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            if (request == null || request.IdCotizacion == Guid.Empty)
            {
                return BadRequest(new CotizacionOperacionResponse { Mensaje = "La cotización no está disponible." });
            }

            string motivo = Truncate(request.MotivoCancelacion?.Trim() ?? string.Empty, MotivoCancelacionLength);
            if (string.IsNullOrWhiteSpace(motivo))
            {
                return BadRequest(new CotizacionOperacionResponse { Mensaje = "Captura un motivo para cancelar la cotización." });
            }

            try
            {
                using SqlConnection connection = CreateConnection();
                await connection.OpenAsync();
                await EnsureSchemaAsync(connection);

                using SqlCommand command = new SqlCommand(@"
UPDATE dbo.Cotizaciones
SET Estado = @EstadoCancelada,
    MotivoCancelacion = @MotivoCancelacion,
    FechaCancelacion = @FechaCancelacion,
    idUsuarioCancelacion = @IdUsuarioCancelacion,
    FechaActualizacion = @FechaActualizacion
WHERE idEmpresa = @IdEmpresa
  AND id = @IdCotizacion
  AND Activo = 1
  AND FechaArchivado IS NULL
  AND Estado = @EstadoBorrador", connection);

                command.Parameters.AddWithValue("@EstadoCancelada", CotizacionEstados.Cancelada);
                command.Parameters.AddWithValue("@EstadoBorrador", CotizacionEstados.Borrador);
                command.Parameters.AddWithValue("@MotivoCancelacion", motivo);
                command.Parameters.AddWithValue("@FechaCancelacion", DateTime.UtcNow);
                command.Parameters.AddWithValue("@IdUsuarioCancelacion", (object?)context.UsuarioId ?? DBNull.Value);
                command.Parameters.AddWithValue("@FechaActualizacion", DateTime.UtcNow);
                command.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);
                command.Parameters.AddWithValue("@IdCotizacion", request.IdCotizacion);

                int affected = await command.ExecuteNonQueryAsync();
                if (affected == 0)
                {
                    return BadRequest(new CotizacionOperacionResponse { Mensaje = "Solo se pueden cancelar cotizaciones en borrador." });
                }

                return Ok(new CotizacionOperacionResponse
                {
                    Exito = true,
                    Mensaje = "La cotización se canceló correctamente.",
                    IdCotizacion = request.IdCotizacion,
                    Estado = CotizacionEstados.Cancelada,
                    EstadoNombre = GetEstadoNombre(CotizacionEstados.Cancelada)
                });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "CancelarCotizacion", "No fue posible cancelar la cotización.");
            }
        }

        [HttpPost("AutorizarCotizacion")]
        public async Task<IActionResult> AutorizarCotizacion([FromBody] CotizacionAutorizarRequest request, Guid idEmpresa)
        {
            if (!TryResolveRequestContext(idEmpresa, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            if (request == null || request.IdCotizacion == Guid.Empty)
            {
                return BadRequest(new CotizacionOperacionResponse { Mensaje = "La cotización no está disponible." });
            }

            try
            {
                using SqlConnection connection = CreateConnection();
                await connection.OpenAsync();
                await EnsureSchemaAsync(connection);

                using SqlCommand command = new SqlCommand(@"
UPDATE dbo.Cotizaciones
SET Estado = @EstadoAutorizada,
    FechaActualizacion = @FechaActualizacion,
    idUsuarioActualizacion = @IdUsuarioActualizacion
WHERE idEmpresa = @IdEmpresa
  AND id = @IdCotizacion
  AND Activo = 1
  AND FechaArchivado IS NULL
  AND Estado = @EstadoBorrador", connection);

                command.Parameters.AddWithValue("@EstadoAutorizada", CotizacionEstados.Autorizada);
                command.Parameters.AddWithValue("@EstadoBorrador", CotizacionEstados.Borrador);
                command.Parameters.AddWithValue("@FechaActualizacion", DateTime.UtcNow);
                command.Parameters.AddWithValue("@IdUsuarioActualizacion", (object?)context.UsuarioId ?? DBNull.Value);
                command.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);
                command.Parameters.AddWithValue("@IdCotizacion", request.IdCotizacion);

                int affected = await command.ExecuteNonQueryAsync();
                if (affected == 0)
                {
                    return BadRequest(new CotizacionOperacionResponse { Mensaje = "Solo se pueden autorizar cotizaciones en borrador." });
                }

                return Ok(new CotizacionOperacionResponse
                {
                    Exito = true,
                    Mensaje = "La cotización se autorizó correctamente.",
                    IdCotizacion = request.IdCotizacion,
                    Estado = CotizacionEstados.Autorizada,
                    EstadoNombre = GetEstadoNombre(CotizacionEstados.Autorizada)
                });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "AutorizarCotizacion", "No fue posible autorizar la cotización.");
            }
        }

        [HttpGet("ExportarCotizacionPdf")]
        public async Task<IActionResult> ExportarCotizacionPdf(Guid idEmpresa, Guid idCotizacion)
        {
            if (!TryResolveRequestContext(idEmpresa, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            if (idCotizacion == Guid.Empty)
            {
                return BadRequest(new CotizacionOperacionResponse { Mensaje = "La cotización no está disponible." });
            }

            try
            {
                using SqlConnection connection = CreateConnection();
                await connection.OpenAsync();
                await EnsureSchemaAsync(connection);

                CotizacionDocumentoExportDto documento = await ObtenerDocumentoCotizacionAsync(connection, context.IdEmpresa, idCotizacion);
                if (documento.IdCotizacion == Guid.Empty)
                {
                    return NotFound(new CotizacionOperacionResponse { Mensaje = "La cotización no está disponible." });
                }

                byte[] pdf = BuildPdfDocument(documento);
                string fileName = BuildSafeFileName("cotizacion", documento.Folio, ".pdf");
                return File(pdf, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ExportarCotizacionPdf", "No fue posible generar el PDF de la cotización.");
            }
        }

        [HttpPost("EnviarCotizacionCorreo")]
        public async Task<IActionResult> EnviarCotizacionCorreo(Guid idEmpresa, [FromBody] CotizacionCorreoRequest? request)
        {
            if (!TryResolveRequestContext(idEmpresa, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            if (request == null || request.IdCotizacion == Guid.Empty)
            {
                return BadRequest(new CotizacionOperacionResponse { Mensaje = "La cotización no está disponible." });
            }

            string correo = (request.Correo ?? string.Empty).Trim();
            string asunto = (request.Asunto ?? string.Empty).Trim();
            string mensaje = (request.Mensaje ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(asunto) || string.IsNullOrWhiteSpace(mensaje))
            {
                return BadRequest(new CotizacionOperacionResponse { Mensaje = "Correo, asunto y mensaje son obligatorios." });
            }

            if (!IsValidEmail(correo))
            {
                return BadRequest(new CotizacionOperacionResponse { Mensaje = "Captura un correo válido." });
            }

            try
            {
                using SqlConnection connection = CreateConnection();
                await connection.OpenAsync();
                await EnsureSchemaAsync(connection);

                CorreoSalientePersistedConfiguration? storedConfiguration = await LoadCorreoSalienteConfigurationAsync(connection, context.IdEmpresa);
                if (storedConfiguration == null || string.IsNullOrWhiteSpace(storedConfiguration.CredencialProtegida))
                {
                    return Conflict(new CotizacionOperacionResponse
                    {
                        Mensaje = "No hay una cuenta de correo configurada para enviar documentos."
                    });
                }

                if (!storedConfiguration.ConfiguracionVerificada)
                {
                    return Conflict(new CotizacionOperacionResponse
                    {
                        Mensaje = "La cuenta de correo debe verificarse antes de enviar documentos."
                    });
                }

                string password;
                try
                {
                    password = _protector.Unprotect(storedConfiguration.CredencialProtegida);
                }
                catch
                {
                    return Conflict(new CotizacionOperacionResponse
                    {
                        Mensaje = "No hay una cuenta de correo configurada para enviar documentos."
                    });
                }

                if (string.IsNullOrWhiteSpace(password))
                {
                    return Conflict(new CotizacionOperacionResponse
                    {
                        Mensaje = "No hay una cuenta de correo configurada para enviar documentos."
                    });
                }

                CotizacionDocumentoExportDto documento = await ObtenerDocumentoCotizacionAsync(connection, context.IdEmpresa, request.IdCotizacion);
                if (documento.IdCotizacion == Guid.Empty)
                {
                    return NotFound(new CotizacionOperacionResponse { Mensaje = "La cotización no está disponible." });
                }

                byte[] pdf = BuildPdfDocument(documento);
                if (pdf.Length == 0)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, new CotizacionOperacionResponse
                    {
                        Mensaje = "No fue posible adjuntar el PDF de la cotización."
                    });
                }

                SmtpDocumentConfiguration smtpConfiguration = new SmtpDocumentConfiguration
                {
                    Cuenta = storedConfiguration.Cuenta,
                    Contrasena = password,
                    ServidorSmtp = storedConfiguration.ServidorSmtp,
                    Puerto = storedConfiguration.Puerto,
                    Seguridad = storedConfiguration.Seguridad
                };

                DocumentEmailMessage emailMessage = new DocumentEmailMessage
                {
                    Destinatario = correo,
                    Asunto = asunto,
                    TextoPlano = mensaje,
                    Adjuntos = new[]
                    {
                        new DocumentEmailAttachment
                        {
                            FileName = BuildSafeFileName("cotizacion", documento.Folio, ".pdf"),
                            Content = pdf,
                            ContentType = "application/pdf"
                        }
                    }
                };

                await _documentEmailService.SendDocumentEmailAsync(smtpConfiguration, emailMessage);

                return Ok(new CotizacionOperacionResponse
                {
                    Exito = true,
                    Mensaje = "La cotización se envió por correo correctamente."
                });
            }
            catch (DocumentEmailConnectionException)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new CotizacionOperacionResponse
                {
                    Mensaje = "No fue posible conectar con la cuenta de correo configurada."
                });
            }
            catch (DocumentEmailAuthenticationException)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new CotizacionOperacionResponse
                {
                    Mensaje = "No fue posible autenticar la cuenta de correo configurada."
                });
            }
            catch (DocumentEmailSendException)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new CotizacionOperacionResponse
                {
                    Mensaje = "No fue posible enviar el correo."
                });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "EnviarCotizacionCorreo", "No fue posible enviar la cotización por correo.");
            }
        }

        private async Task<CotizacionDocumentoExportDto> ObtenerDocumentoCotizacionAsync(SqlConnection connection, Guid idEmpresa, Guid idCotizacion)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT TOP (1)
    c.id,
    c.Folio,
    c.FechaCotizacion,
    c.FechaVigencia,
    c.Estado,
    ISNULL(cl.Nombre, '') AS Cliente,
    ISNULL(cl.Telefono, '') AS ClienteTelefono,
    ISNULL(cl.Correo, '') AS ClienteCorreo,
    ISNULL(su.Nombre, '') AS Sucursal,
    ISNULL(c.Vendedor, '') AS Vendedor,
    ISNULL(c.Caja, '') AS Caja,
    ISNULL(c.Observaciones, '') AS Observaciones,
    c.Subtotal,
    c.DescuentoTotal,
    c.Total,
    c.TotalPiezas
FROM dbo.Cotizaciones c
INNER JOIN dbo.Clientes cl
    ON cl.id = c.idCliente AND cl.idEmpresa = c.idEmpresa
LEFT JOIN dbo.Sucursales su
    ON su.id = c.idSucursal AND su.idEmpresa = c.idEmpresa
WHERE c.idEmpresa = @IdEmpresa
  AND c.id = @IdCotizacion
  AND c.Activo = 1
  AND c.FechaArchivado IS NULL", connection);

            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@IdCotizacion", idCotizacion);

            CotizacionDocumentoExportDto documento = new CotizacionDocumentoExportDto();
            using (SqlDataReader reader = await command.ExecuteReaderAsync())
            {
                if (!await reader.ReadAsync())
                {
                    return documento;
                }

                byte estado = ReadByte(reader, "Estado");
                documento.IdCotizacion = ReadGuid(reader, "id");
                documento.Folio = ReadString(reader, "Folio");
                documento.FechaCotizacion = ReadDateTime(reader, "FechaCotizacion");
                documento.FechaVigencia = ReadNullableDateTime(reader, "FechaVigencia");
                documento.Estado = GetEstadoNombre(estado);
                documento.Cliente = ReadString(reader, "Cliente");
                documento.ClienteTelefono = ReadString(reader, "ClienteTelefono");
                documento.ClienteCorreo = ReadString(reader, "ClienteCorreo");
                documento.Sucursal = ReadString(reader, "Sucursal");
                documento.Vendedor = ReadString(reader, "Vendedor");
                documento.Caja = ReadString(reader, "Caja");
                documento.Observaciones = ReadString(reader, "Observaciones");
                documento.Subtotal = ReadDecimal(reader, "Subtotal");
                documento.DescuentoTotal = ReadDecimal(reader, "DescuentoTotal");
                documento.Total = ReadDecimal(reader, "Total");
                documento.TotalPiezas = ReadDecimal(reader, "TotalPiezas");
            }

            documento.Partidas = (await ObtenerPartidasAsync(connection, idEmpresa, idCotizacion))
                .Select(partida => new CotizacionDocumentoPartidaDto
                {
                    NumeroPartida = partida.NumeroPartida,
                    Codigo = partida.Codigo,
                    Nombre = partida.Nombre,
                    Descripcion = partida.Descripcion,
                    Unidad = string.IsNullOrWhiteSpace(partida.UnidadAbreviatura)
                        ? partida.UnidadMedida
                        : $"{partida.UnidadMedida} ({partida.UnidadAbreviatura})",
                    Cantidad = partida.Cantidad,
                    PrecioUnitario = partida.PrecioUnitario,
                    DescuentoPct = partida.DescuentoPct,
                    Total = partida.Total,
                    ExistenciaActual = partida.ExistenciaActual
                })
                .ToList();

            return documento;
        }

        private static bool IsValidEmail(string correo)
        {
            try
            {
                _ = new MailAddress(correo);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<CorreoSalientePersistedConfiguration?> LoadCorreoSalienteConfigurationAsync(SqlConnection connection, Guid idEmpresa)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT TOP (1)
    id,
    idEmpresa,
    identityKey,
    Cuenta,
    ServidorSmtp,
    Puerto,
    Seguridad,
    CredencialProtegida,
    DestinatarioPrueba,
    ConfiguracionVerificada,
    FechaUltimaPrueba,
    FechaCreacion,
    FechaActualizacion,
    Activo
FROM dbo.ConfiguracionCorreoSaliente
WHERE idEmpresa = @IdEmpresa
  AND Activo = 1
  AND FechaArchivado IS NULL
ORDER BY FechaCreacion DESC", connection);

            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);

            using SqlDataReader reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }

            return new CorreoSalientePersistedConfiguration
            {
                Id = ReadGuid(reader, "id"),
                IdEmpresa = ReadGuid(reader, "idEmpresa"),
                IdentityKey = ReadGuid(reader, "identityKey"),
                Cuenta = ReadString(reader, "Cuenta"),
                ServidorSmtp = ReadString(reader, "ServidorSmtp"),
                Puerto = ReadInt(reader, "Puerto"),
                Seguridad = ReadString(reader, "Seguridad"),
                CredencialProtegida = ReadString(reader, "CredencialProtegida"),
                DestinatarioPrueba = ReadString(reader, "DestinatarioPrueba"),
                ConfiguracionVerificada = ReadBool(reader, "ConfiguracionVerificada"),
                FechaUltimaPrueba = ReadNullableDateTime(reader, "FechaUltimaPrueba"),
                FechaCreacion = ReadDateTime(reader, "FechaCreacion"),
                FechaActualizacion = ReadNullableDateTime(reader, "FechaActualizacion"),
                Activo = ReadBool(reader, "Activo")
            };
        }

        private async Task<List<CotizacionPartidaDetalleDto>> ObtenerPartidasAsync(SqlConnection connection, Guid idEmpresa, Guid idCotizacion)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT
    p.id,
    p.NumeroPartida,
    p.idProductoServicio,
    p.TipoProductoServicio,
    p.Codigo,
    p.Nombre,
    p.Descripcion,
    p.idUnidadMedida,
    p.UnidadMedida,
    p.UnidadAbreviatura,
    p.UnidadPermiteDecimales,
    p.PermiteVentaSinExistencia,
    p.ExistenciaActual,
    p.Cantidad,
    p.PrecioUnitario,
    p.DescuentoPct,
    p.ImporteBruto,
    p.DescuentoImporte,
    p.Total
FROM dbo.CotizacionesPartidas p
WHERE p.idEmpresa = @IdEmpresa
  AND p.idCotizacion = @IdCotizacion
  AND p.Activo = 1
  AND p.FechaArchivado IS NULL
ORDER BY p.NumeroPartida", connection);

            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@IdCotizacion", idCotizacion);

            List<CotizacionPartidaDetalleDto> items = new();
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                byte tipo = ReadByte(reader, "TipoProductoServicio");
                items.Add(new CotizacionPartidaDetalleDto
                {
                    Id = ReadGuid(reader, "id"),
                    NumeroPartida = ReadInt(reader, "NumeroPartida"),
                    IdProductoServicio = ReadGuid(reader, "idProductoServicio"),
                    TipoProductoServicio = tipo,
                    TipoProductoServicioNombre = tipo == TipoProducto ? "Producto" : "Servicio",
                    Codigo = ReadString(reader, "Codigo"),
                    Nombre = ReadString(reader, "Nombre"),
                    Descripcion = ReadString(reader, "Descripcion"),
                    IdUnidadMedida = ReadGuid(reader, "idUnidadMedida"),
                    UnidadMedida = ReadString(reader, "UnidadMedida"),
                    UnidadAbreviatura = ReadString(reader, "UnidadAbreviatura"),
                    UnidadPermiteDecimales = ReadBool(reader, "UnidadPermiteDecimales"),
                    PermiteVentaSinExistencia = ReadBool(reader, "PermiteVentaSinExistencia"),
                    ExistenciaActual = ReadNullableDecimal(reader, "ExistenciaActual"),
                    Cantidad = ReadDecimal(reader, "Cantidad"),
                    PrecioUnitario = ReadDecimal(reader, "PrecioUnitario"),
                    DescuentoPct = ReadDecimal(reader, "DescuentoPct"),
                    ImporteBruto = ReadDecimal(reader, "ImporteBruto"),
                    DescuentoImporte = ReadDecimal(reader, "DescuentoImporte"),
                    Total = ReadDecimal(reader, "Total")
                });
            }

            return items;
        }

        private async Task<List<CotizacionPartidaDbRow>> NormalizePartidasAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            Guid idEmpresa,
            List<CotizacionPartidaGuardarRequest> requests,
            decimal descuentoCliente)
        {
            List<CotizacionPartidaDbRow> result = new();
            foreach (CotizacionPartidaGuardarRequest request in requests)
            {
                if (request == null || request.IdProductoServicio == Guid.Empty || request.Cantidad <= 0 || request.PrecioUnitario <= 0)
                {
                    continue;
                }

                ProductoContext producto = await ObtenerProductoAsync(connection, transaction, idEmpresa, request.IdProductoServicio);
                if (producto.Id == Guid.Empty)
                {
                    continue;
                }

                decimal descuentoPct = Math.Max(0m, request.DescuentoPct);
                if (descuentoPct <= 0m && descuentoCliente > 0m)
                {
                    descuentoPct = descuentoCliente;
                }

                descuentoPct = Math.Min(descuentoPct, 100m);
                decimal cantidad = producto.UnidadPermiteDecimales
                    ? RoundMoney(request.Cantidad)
                    : Math.Max(1m, Math.Round(request.Cantidad, 0, MidpointRounding.AwayFromZero));
                decimal precioUnitario = RoundMoney(request.PrecioUnitario);
                decimal importeBruto = RoundMoney(cantidad * precioUnitario);
                decimal descuentoImporte = RoundMoney(importeBruto * (descuentoPct / 100m));
                decimal total = RoundMoney(importeBruto - descuentoImporte);

                result.Add(new CotizacionPartidaDbRow
                {
                    Id = Guid.NewGuid(),
                    IdProductoServicio = producto.Id,
                    Codigo = producto.Codigo,
                    Nombre = producto.Nombre,
                    Descripcion = producto.Descripcion,
                    TipoProductoServicio = producto.Tipo,
                    IdUnidadMedida = producto.IdUnidadMedida,
                    UnidadMedida = producto.UnidadMedida,
                    UnidadAbreviatura = producto.UnidadAbreviatura,
                    UnidadPermiteDecimales = producto.UnidadPermiteDecimales,
                    PermiteVentaSinExistencia = producto.PermiteVentaSinExistencia,
                    ExistenciaActual = producto.ExistenciaActual,
                    Cantidad = cantidad,
                    PrecioUnitario = precioUnitario,
                    DescuentoPct = descuentoPct,
                    ImporteBruto = importeBruto,
                    DescuentoImporte = descuentoImporte,
                    Total = total
                });
            }

            return result;
        }

        private async Task InsertarPartidasAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idCotizacion, List<CotizacionPartidaDbRow> partidas, DateTime now)
        {
            for (int index = 0; index < partidas.Count; index++)
            {
                CotizacionPartidaDbRow partida = partidas[index];
                using SqlCommand insert = new SqlCommand(@"
INSERT INTO dbo.CotizacionesPartidas
(
    id, idCotizacion, idEmpresa, identityKey, NumeroPartida, idProductoServicio, Codigo, Nombre, Descripcion, TipoProductoServicio,
    idUnidadMedida, UnidadMedida, UnidadAbreviatura, UnidadPermiteDecimales, PermiteVentaSinExistencia, ExistenciaActual,
    Cantidad, PrecioUnitario, DescuentoPct, ImporteBruto, DescuentoImporte, Total, FechaCreacion, FechaActualizacion, FechaArchivado, Activo
)
VALUES
(
    @Id, @IdCotizacion, @IdEmpresa, @IdentityKey, @NumeroPartida, @IdProductoServicio, @Codigo, @Nombre, @Descripcion, @TipoProductoServicio,
    @IdUnidadMedida, @UnidadMedida, @UnidadAbreviatura, @UnidadPermiteDecimales, @PermiteVentaSinExistencia, @ExistenciaActual,
    @Cantidad, @PrecioUnitario, @DescuentoPct, @ImporteBruto, @DescuentoImporte, @Total, @FechaCreacion, @FechaActualizacion, NULL, 1
)", connection, transaction);

                insert.Parameters.AddWithValue("@Id", partida.Id);
                insert.Parameters.AddWithValue("@IdCotizacion", idCotizacion);
                insert.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                insert.Parameters.AddWithValue("@IdentityKey", Guid.NewGuid());
                insert.Parameters.AddWithValue("@NumeroPartida", index + 1);
                insert.Parameters.AddWithValue("@IdProductoServicio", partida.IdProductoServicio);
                insert.Parameters.AddWithValue("@Codigo", partida.Codigo);
                insert.Parameters.AddWithValue("@Nombre", partida.Nombre);
                insert.Parameters.AddWithValue("@Descripcion", partida.Descripcion);
                insert.Parameters.AddWithValue("@TipoProductoServicio", partida.TipoProductoServicio);
                insert.Parameters.AddWithValue("@IdUnidadMedida", partida.IdUnidadMedida);
                insert.Parameters.AddWithValue("@UnidadMedida", partida.UnidadMedida);
                insert.Parameters.AddWithValue("@UnidadAbreviatura", partida.UnidadAbreviatura);
                insert.Parameters.AddWithValue("@UnidadPermiteDecimales", partida.UnidadPermiteDecimales);
                insert.Parameters.AddWithValue("@PermiteVentaSinExistencia", partida.PermiteVentaSinExistencia);
                insert.Parameters.AddWithValue("@ExistenciaActual", partida.ExistenciaActual.HasValue ? partida.ExistenciaActual.Value : DBNull.Value);
                insert.Parameters.AddWithValue("@Cantidad", partida.Cantidad);
                insert.Parameters.AddWithValue("@PrecioUnitario", partida.PrecioUnitario);
                insert.Parameters.AddWithValue("@DescuentoPct", partida.DescuentoPct);
                insert.Parameters.AddWithValue("@ImporteBruto", partida.ImporteBruto);
                insert.Parameters.AddWithValue("@DescuentoImporte", partida.DescuentoImporte);
                insert.Parameters.AddWithValue("@Total", partida.Total);
                insert.Parameters.AddWithValue("@FechaCreacion", now);
                insert.Parameters.AddWithValue("@FechaActualizacion", now);
                await insert.ExecuteNonQueryAsync();
            }
        }

        private static TotalesCotizacion BuildTotales(List<CotizacionPartidaDbRow> partidas)
        {
            return new TotalesCotizacion
            {
                Subtotal = RoundMoney(partidas.Sum(item => item.ImporteBruto)),
                DescuentoTotal = RoundMoney(partidas.Sum(item => item.DescuentoImporte)),
                Total = RoundMoney(partidas.Sum(item => item.Total)),
                TotalPiezas = RoundMoney(partidas.Sum(item => item.Cantidad))
            };
        }

        private async Task<string> GenerateFolioAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT MAX(TRY_CONVERT(INT, REPLACE(Folio, 'COT-', '')))
FROM dbo.Cotizaciones
WHERE idEmpresa = @IdEmpresa", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            object? raw = await command.ExecuteScalarAsync();
            int next = 1;
            if (raw != null && raw != DBNull.Value && int.TryParse(raw.ToString(), out int current))
            {
                next = current + 1;
            }

            return "COT-" + next.ToString(CultureInfo.InvariantCulture).PadLeft(FolioPadding, '0');
        }

        private async Task<ClienteContext> ObtenerClienteAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idCliente)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT TOP (1)
    c.id,
    ISNULL(c.Nombre, '') AS Nombre,
    ISNULL(c.Telefono, '') AS Telefono,
    ISNULL(c.Correo, '') AS Correo,
    ISNULL(c.Descuento, 0) AS Descuento
FROM dbo.Clientes c
WHERE c.idEmpresa = @IdEmpresa
  AND c.id = @IdCliente
  AND c.Activo = 1", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@IdCliente", idCliente);

            using SqlDataReader reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return new ClienteContext();
            }

            return new ClienteContext
            {
                Id = ReadGuid(reader, "id"),
                Nombre = ReadString(reader, "Nombre"),
                Telefono = ReadString(reader, "Telefono"),
                Correo = ReadString(reader, "Correo"),
                Descuento = ReadDecimal(reader, "Descuento")
            };
        }

        private async Task<ProductoContext> ObtenerProductoAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idProductoServicio)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT TOP (1)
    ps.id,
    ps.Tipo,
    ps.Codigo,
    ps.Nombre,
    ISNULL(ps.Descripcion, '') AS Descripcion,
    ps.idUnidadMedida,
    ISNULL(um.Nombre, '') AS UnidadMedida,
    ISNULL(um.Abreviatura, '') AS UnidadAbreviatura,
    ISNULL(um.PermiteDecimales, 0) AS UnidadPermiteDecimales,
    ISNULL(ps.PermiteVentaSinExistencia, 0) AS PermiteVentaSinExistencia,
    ex.ExistenciaActual
FROM dbo.ProductosServicios ps
INNER JOIN dbo.ProductosServiciosUnidadesMedida um
    ON um.idEmpresa = ps.idEmpresa
   AND um.id = ps.idUnidadMedida
LEFT JOIN dbo.ProductosServiciosExistencias ex
    ON ex.idEmpresa = ps.idEmpresa
   AND ex.idProductoServicio = ps.id
WHERE ps.idEmpresa = @IdEmpresa
  AND ps.id = @IdProductoServicio
  AND ps.Activo = 1", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);

            using SqlDataReader reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return new ProductoContext();
            }

            return new ProductoContext
            {
                Id = ReadGuid(reader, "id"),
                Tipo = ReadByte(reader, "Tipo"),
                Codigo = ReadString(reader, "Codigo"),
                Nombre = ReadString(reader, "Nombre"),
                Descripcion = ReadString(reader, "Descripcion"),
                IdUnidadMedida = ReadGuid(reader, "idUnidadMedida"),
                UnidadMedida = ReadString(reader, "UnidadMedida"),
                UnidadAbreviatura = ReadString(reader, "UnidadAbreviatura"),
                UnidadPermiteDecimales = ReadBool(reader, "UnidadPermiteDecimales"),
                PermiteVentaSinExistencia = ReadBool(reader, "PermiteVentaSinExistencia"),
                ExistenciaActual = ReadNullableDecimal(reader, "ExistenciaActual")
            };
        }

        private async Task<UserMetadata> ResolveUserMetadataAsync(SqlConnection connection, SqlTransaction transaction, RequestContext context)
        {
            if (string.IsNullOrWhiteSpace(context.Correo))
            {
                return new UserMetadata();
            }

            using SqlCommand command = new SqlCommand(@"
SELECT TOP (1)
    u.id,
    ISNULL(u.Nombre, '') AS Nombre,
    ISNULL(u.apellidoPaterno, '') AS ApellidoPaterno,
    ISNULL(u.apellidoMaterno, '') AS ApellidoMaterno,
    u.idSucursal
FROM dbo.Usuarios u
WHERE u.idEmpresa = @IdEmpresa
  AND u.borrado = 0
  AND (
        LOWER(ISNULL(u.CorreoInstitucional, '')) = LOWER(@Correo)
        OR LOWER(ISNULL(u.CorreoPersonal, '')) = LOWER(@Correo)
      )
ORDER BY u.fechaAlta DESC", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);
            command.Parameters.AddWithValue("@Correo", context.Correo);

            using SqlDataReader reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return new UserMetadata();
            }

            string nombre = string.Join(" ", new[]
            {
                ReadString(reader, "Nombre"),
                ReadString(reader, "ApellidoPaterno"),
                ReadString(reader, "ApellidoMaterno")
            }.Where(part => !string.IsNullOrWhiteSpace(part))).Trim();

            return new UserMetadata
            {
                Id = ReadGuid(reader, "id"),
                Nombre = nombre,
                IdSucursal = ReadNullableGuid(reader, "idSucursal")
            };
        }

        private async Task<CotizacionPersistedRow> ObtenerCotizacionPersistidaAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idCotizacion)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT TOP (1)
    id,
    Folio,
    Estado,
    FechaCotizacion,
    idSucursal
FROM dbo.Cotizaciones
WHERE idEmpresa = @IdEmpresa
  AND id = @IdCotizacion
  AND Activo = 1
  AND FechaArchivado IS NULL", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@IdCotizacion", idCotizacion);

            using SqlDataReader reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return new CotizacionPersistedRow();
            }

            return new CotizacionPersistedRow
            {
                Id = ReadGuid(reader, "id"),
                Folio = ReadString(reader, "Folio"),
                Estado = ReadByte(reader, "Estado"),
                FechaCotizacion = ReadDateTime(reader, "FechaCotizacion"),
                IdSucursal = ReadNullableGuid(reader, "idSucursal")
            };
        }

        private void FillCotizacionParameters(
            SqlCommand command,
            Guid idEmpresa,
            Guid idCotizacion,
            Guid idCliente,
            Guid? idSucursal,
            string vendedor,
            string caja,
            string observaciones,
            int vigenciaDias,
            DateTime? fechaVigencia,
            TotalesCotizacion totals,
            DateTime now)
        {
            AddIfMissing(command, "@IdEmpresa", idEmpresa);
            AddIfMissing(command, "@IdCotizacion", idCotizacion);
            AddIfMissing(command, "@IdCliente", idCliente);
            AddIfMissing(command, "@IdSucursal", idSucursal.HasValue ? idSucursal.Value : DBNull.Value);
            AddIfMissing(command, "@Vendedor", Truncate(vendedor ?? string.Empty, 200));
            AddIfMissing(command, "@Caja", Truncate(caja ?? string.Empty, CajaLength));
            AddIfMissing(command, "@Observaciones", Truncate(observaciones ?? string.Empty, ObservacionesLength));
            AddIfMissing(command, "@VigenciaDias", vigenciaDias);
            AddIfMissing(command, "@FechaVigencia", fechaVigencia.HasValue ? fechaVigencia.Value : DBNull.Value);
            AddIfMissing(command, "@Subtotal", totals.Subtotal);
            AddIfMissing(command, "@DescuentoTotal", totals.DescuentoTotal);
            AddIfMissing(command, "@Total", totals.Total);
            AddIfMissing(command, "@TotalPiezas", totals.TotalPiezas);
            AddIfMissing(command, "@FechaCreacion", now);
            AddIfMissing(command, "@FechaActualizacion", now);
        }

        private static void AddIfMissing(SqlCommand command, string name, object value)
        {
            if (!command.Parameters.Contains(name))
            {
                command.Parameters.AddWithValue(name, value ?? DBNull.Value);
            }
        }

        private static string? ValidateRequest(CotizacionGuardarRequest request)
        {
            if (request.IdCliente == Guid.Empty)
            {
                return "Selecciona un cliente.";
            }

            if (request.Partidas == null || request.Partidas.Count == 0)
            {
                return "Agrega al menos un producto.";
            }

            if (request.VigenciaDias.HasValue && request.VigenciaDias.Value < 0)
            {
                return "La vigencia no puede ser negativa.";
            }

            return null;
        }

        private async Task EnsureSchemaAsync(SqlConnection connection)
        {
            using SqlCommand command = new SqlCommand(@"
IF OBJECT_ID(N'dbo.Cotizaciones', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cotizaciones
    (
        id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Cotizaciones PRIMARY KEY,
        idEmpresa UNIQUEIDENTIFIER NOT NULL,
        identityKey UNIQUEIDENTIFIER NOT NULL,
        Folio NVARCHAR(30) NOT NULL,
        Estado TINYINT NOT NULL,
        FechaCotizacion DATETIME2(0) NOT NULL,
        VigenciaDias INT NOT NULL CONSTRAINT DF_Cotizaciones_VigenciaDias DEFAULT (0),
        FechaVigencia DATETIME2(0) NULL,
        idCliente UNIQUEIDENTIFIER NOT NULL,
        idSucursal UNIQUEIDENTIFIER NULL,
        Vendedor NVARCHAR(200) NOT NULL CONSTRAINT DF_Cotizaciones_Vendedor DEFAULT (N''),
        Caja NVARCHAR(100) NOT NULL CONSTRAINT DF_Cotizaciones_Caja DEFAULT (N''),
        Observaciones NVARCHAR(1000) NOT NULL CONSTRAINT DF_Cotizaciones_Observaciones DEFAULT (N''),
        Subtotal DECIMAL(18,2) NOT NULL CONSTRAINT DF_Cotizaciones_Subtotal DEFAULT (0),
        DescuentoTotal DECIMAL(18,2) NOT NULL CONSTRAINT DF_Cotizaciones_DescuentoTotal DEFAULT (0),
        Total DECIMAL(18,2) NOT NULL CONSTRAINT DF_Cotizaciones_Total DEFAULT (0),
        TotalPiezas DECIMAL(18,2) NOT NULL CONSTRAINT DF_Cotizaciones_TotalPiezas DEFAULT (0),
        MotivoCancelacion NVARCHAR(500) NOT NULL CONSTRAINT DF_Cotizaciones_MotivoCancelacion DEFAULT (N''),
        FechaCancelacion DATETIME2(0) NULL,
        idUsuarioCreacion UNIQUEIDENTIFIER NULL,
        idUsuarioActualizacion UNIQUEIDENTIFIER NULL,
        idUsuarioCancelacion UNIQUEIDENTIFIER NULL,
        FechaCreacion DATETIME2(0) NOT NULL,
        FechaActualizacion DATETIME2(0) NOT NULL,
        FechaArchivado DATETIME2(0) NULL,
        Activo BIT NOT NULL CONSTRAINT DF_Cotizaciones_Activo DEFAULT (1)
    );
END;

IF OBJECT_ID(N'dbo.CotizacionesPartidas', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CotizacionesPartidas
    (
        id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CotizacionesPartidas PRIMARY KEY,
        idCotizacion UNIQUEIDENTIFIER NOT NULL,
        idEmpresa UNIQUEIDENTIFIER NOT NULL,
        identityKey UNIQUEIDENTIFIER NOT NULL,
        NumeroPartida INT NOT NULL,
        idProductoServicio UNIQUEIDENTIFIER NOT NULL,
        Codigo NVARCHAR(50) NOT NULL,
        Nombre NVARCHAR(200) NOT NULL,
        Descripcion NVARCHAR(1000) NOT NULL CONSTRAINT DF_CotizacionesPartidas_Descripcion DEFAULT (N''),
        TipoProductoServicio TINYINT NOT NULL,
        idUnidadMedida UNIQUEIDENTIFIER NOT NULL,
        UnidadMedida NVARCHAR(100) NOT NULL,
        UnidadAbreviatura NVARCHAR(30) NOT NULL CONSTRAINT DF_CotizacionesPartidas_UnidadAbreviatura DEFAULT (N''),
        UnidadPermiteDecimales BIT NOT NULL CONSTRAINT DF_CotizacionesPartidas_UnidadPermiteDecimales DEFAULT (0),
        PermiteVentaSinExistencia BIT NOT NULL CONSTRAINT DF_CotizacionesPartidas_PermiteVentaSinExistencia DEFAULT (0),
        ExistenciaActual DECIMAL(18,2) NULL,
        Cantidad DECIMAL(18,2) NOT NULL,
        PrecioUnitario DECIMAL(18,2) NOT NULL,
        DescuentoPct DECIMAL(9,2) NOT NULL CONSTRAINT DF_CotizacionesPartidas_DescuentoPct DEFAULT (0),
        ImporteBruto DECIMAL(18,2) NOT NULL,
        DescuentoImporte DECIMAL(18,2) NOT NULL CONSTRAINT DF_CotizacionesPartidas_DescuentoImporte DEFAULT (0),
        Total DECIMAL(18,2) NOT NULL,
        FechaCreacion DATETIME2(0) NOT NULL,
        FechaActualizacion DATETIME2(0) NOT NULL,
        FechaArchivado DATETIME2(0) NULL,
        Activo BIT NOT NULL CONSTRAINT DF_CotizacionesPartidas_Activo DEFAULT (1)
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Cotizaciones_Empresa_Folio' AND object_id = OBJECT_ID(N'dbo.Cotizaciones'))
BEGIN
    CREATE UNIQUE INDEX UX_Cotizaciones_Empresa_Folio ON dbo.Cotizaciones (idEmpresa, Folio);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Cotizaciones_Empresa_Fecha_Estado' AND object_id = OBJECT_ID(N'dbo.Cotizaciones'))
BEGIN
    CREATE INDEX IX_Cotizaciones_Empresa_Fecha_Estado ON dbo.Cotizaciones (idEmpresa, FechaCotizacion DESC, Estado, Activo);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CotizacionesPartidas_Cotizacion_Numero' AND object_id = OBJECT_ID(N'dbo.CotizacionesPartidas'))
BEGIN
    CREATE INDEX IX_CotizacionesPartidas_Cotizacion_Numero ON dbo.CotizacionesPartidas (idCotizacion, NumeroPartida, Activo);
END;", connection);
            await command.ExecuteNonQueryAsync();
        }

        private bool TryResolveRequestContext(Guid? clientEmpresaId, out RequestContext context, out IActionResult? error)
        {
            context = new RequestContext();
            error = null;

            string empresaIdHeader = Request.Headers[ProxyEmpresaIdHeader].FirstOrDefault() ?? string.Empty;
            string empresaHeader = Request.Headers[ProxyEmpresaKeyHeader].FirstOrDefault() ?? string.Empty;
            string timestamp = Request.Headers[ProxyTimestampHeader].FirstOrDefault() ?? string.Empty;
            string signature = Request.Headers[ProxySignatureHeader].FirstOrDefault() ?? string.Empty;
            string correo = Request.Headers[ProxyCorreoHeader].FirstOrDefault() ?? string.Empty;
            string? usuarioIdRaw = Request.Headers[ProxyUsuarioIdHeader].FirstOrDefault();

            if (!Guid.TryParse(empresaIdHeader, out Guid empresaId) || empresaId == Guid.Empty)
            {
                error = BadRequest(new CotizacionOperacionResponse { Mensaje = "No fue posible resolver el contexto de empresa." });
                return false;
            }

            if (clientEmpresaId.HasValue && clientEmpresaId.Value != Guid.Empty && clientEmpresaId.Value != empresaId)
            {
                error = BadRequest(new CotizacionOperacionResponse { Mensaje = "La empresa solicitada no coincide con la sesión activa." });
                return false;
            }

            if (string.IsNullOrWhiteSpace(empresaHeader) || string.IsNullOrWhiteSpace(timestamp) || string.IsNullOrWhiteSpace(signature))
            {
                error = BadRequest(new CotizacionOperacionResponse { Mensaje = "No fue posible validar la sesión activa." });
                return false;
            }

            if (!DateTimeOffset.TryParse(timestamp, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset parsedTimestamp))
            {
                error = BadRequest(new CotizacionOperacionResponse { Mensaje = "No fue posible validar la sesión activa." });
                return false;
            }

            if (DateTimeOffset.UtcNow - parsedTimestamp > ProxyHeaderTolerance || parsedTimestamp - DateTimeOffset.UtcNow > ProxyHeaderTolerance)
            {
                error = BadRequest(new CotizacionOperacionResponse { Mensaje = "La sesión activa expiró. Recarga la página e inténtalo nuevamente." });
                return false;
            }

            string secret = _configuration["fireBdata:fireClave"] ?? string.Empty;
            string expected = ComputeSignature(secret, empresaId.ToString(), empresaHeader, usuarioIdRaw ?? string.Empty, timestamp);
            if (!FixedTimeEquals(signature, expected))
            {
                error = BadRequest(new CotizacionOperacionResponse { Mensaje = "No fue posible validar la sesión activa." });
                return false;
            }

            Guid? usuarioId = null;
            if (Guid.TryParse(usuarioIdRaw, out Guid parsedUserId) && parsedUserId != Guid.Empty)
            {
                usuarioId = parsedUserId;
            }

            context = new RequestContext
            {
                IdEmpresa = empresaId,
                Empresa = empresaHeader,
                UsuarioId = usuarioId,
                Correo = correo?.Trim() ?? string.Empty
            };

            return true;
        }

        private static string ComputeSignature(string secret, string empresaId, string empresa, string usuarioId, string timestamp)
        {
            using HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            string payload = string.Join('\n', empresaId.Trim(), empresa.Trim().ToUpperInvariant(), usuarioId.Trim(), timestamp.Trim());
            return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
        }

        private static bool FixedTimeEquals(string left, string right)
        {
            byte[] leftBytes = Encoding.UTF8.GetBytes(left ?? string.Empty);
            byte[] rightBytes = Encoding.UTF8.GetBytes(right ?? string.Empty);
            return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
        }

        private SqlConnection CreateConnection() => _connectionFactory.CreateConnection();

        private ObjectResult HandleException(Exception ex, string actionName, string userMessage)
        {
            _logger.LogError(ex, "Cotizaciones::{Action}", actionName);
            return StatusCode(500, new CotizacionOperacionResponse { Mensaje = userMessage });
        }

        private static string GetEstadoNombre(byte estado)
        {
            return estado switch
            {
                CotizacionEstados.Borrador => "Borrador",
                CotizacionEstados.Cancelada => "Cancelada",
                CotizacionEstados.Autorizada => "Autorizada",
                _ => "Desconocido"
            };
        }

        private static decimal RoundMoney(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
        private static string Truncate(string value, int maxLength) => string.IsNullOrEmpty(value) || value.Length <= maxLength ? value : value.Substring(0, maxLength);

        private static Guid ReadGuid(SqlDataReader reader, string name) => reader.GetGuid(reader.GetOrdinal(name));
        private static Guid? ReadNullableGuid(SqlDataReader reader, string name) => reader.IsDBNull(reader.GetOrdinal(name)) ? null : reader.GetGuid(reader.GetOrdinal(name));
        private static string ReadString(SqlDataReader reader, string name) => reader.IsDBNull(reader.GetOrdinal(name)) ? string.Empty : Convert.ToString(reader[name], CultureInfo.InvariantCulture) ?? string.Empty;
        private static byte ReadByte(SqlDataReader reader, string name) => reader.IsDBNull(reader.GetOrdinal(name)) ? (byte)0 : Convert.ToByte(reader[name], CultureInfo.InvariantCulture);
        private static int ReadInt(SqlDataReader reader, string name) => reader.IsDBNull(reader.GetOrdinal(name)) ? 0 : Convert.ToInt32(reader[name], CultureInfo.InvariantCulture);
        private static bool ReadBool(SqlDataReader reader, string name) => !reader.IsDBNull(reader.GetOrdinal(name)) && Convert.ToBoolean(reader[name], CultureInfo.InvariantCulture);
        private static decimal ReadDecimal(SqlDataReader reader, string name) => reader.IsDBNull(reader.GetOrdinal(name)) ? 0m : Convert.ToDecimal(reader[name], CultureInfo.InvariantCulture);
        private static decimal? ReadNullableDecimal(SqlDataReader reader, string name) => reader.IsDBNull(reader.GetOrdinal(name)) ? null : Convert.ToDecimal(reader[name], CultureInfo.InvariantCulture);
        private static DateTime ReadDateTime(SqlDataReader reader, string name) => reader.GetDateTime(reader.GetOrdinal(name));
        private static DateTime? ReadNullableDateTime(SqlDataReader reader, string name) => reader.IsDBNull(reader.GetOrdinal(name)) ? null : reader.GetDateTime(reader.GetOrdinal(name));

        private static string BuildSafeFileName(string prefix, string folio, string extension)
        {
            string cleanFolio = new string((folio ?? string.Empty).Where(ch => char.IsLetterOrDigit(ch) || ch == '-' || ch == '_').ToArray());
            if (string.IsNullOrWhiteSpace(cleanFolio))
            {
                cleanFolio = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            }

            return $"{prefix}_{cleanFolio}{extension}";
        }

        private static byte[] BuildPdfDocument(CotizacionDocumentoExportDto documento)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            byte[]? logo = LoadSharedCheckAppLogo();
            string fechaEmision = documento.FechaCotizacion.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            string fechaVigencia = documento.FechaVigencia.HasValue
                ? documento.FechaVigencia.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
                : "Sin definir";
            string observaciones = string.IsNullOrWhiteSpace(documento.Observaciones)
                ? "Sin observaciones registradas."
                : documento.Observaciones.Trim();

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(32);
                    page.DefaultTextStyle(x => x.FontFamily(Fonts.Calibri).FontSize(10).FontColor("#333638"));

                    page.Header().Element(header =>
                    {
                        header.Row(row =>
                        {
                            row.RelativeItem(2.8f).Column(column =>
                            {
                                column.Spacing(5);
                                if (logo != null)
                                {
                                    column.Item().Height(24).AlignLeft().Image(logo).FitHeight();
                                }

                                column.Item().Text("Cotización").SemiBold().FontSize(20).FontColor("#39394D");
                                column.Item().Text($"{CotizacionTextOrDash(documento.Cliente)} · {CotizacionTextOrDash(documento.Sucursal)}").FontSize(10).FontColor("#4791AA");
                                column.Item().Text($"Vendedor: {CotizacionTextOrDash(documento.Vendedor)}").FontSize(10).FontColor("#333638");
                            });

                            row.ConstantItem(190).Element(card =>
                            {
                                card
                                    .Border(1)
                                    .BorderColor("#4791AA")
                                    .CornerRadius(12)
                                    .Padding(10)
                                    .Background("#FAFAFA")
                                    .Column(column =>
                                {
                                    column.Spacing(4);
                                    column.Item().AlignCenter().Text($"Folio {CotizacionTextOrDash(documento.Folio)}").SemiBold().FontColor("#39394D");
                                    column.Item().AlignCenter().Text($"Emitida {fechaEmision}").FontSize(9).FontColor("#4791AA");
                                    column.Item().AlignCenter().Text($"Estado: {CotizacionTextOrDash(documento.Estado)}").FontSize(9).FontColor("#333638");
                                });
                            });
                        });
                    });

                    page.Content().Column(content =>
                    {
                        content.Spacing(14);

                        content.Item().Element(x => ComposeCotizacionMetadataTable(x, new[]
                        {
                            ("Folio", CotizacionTextOrDash(documento.Folio)),
                            ("Estado", CotizacionTextOrDash(documento.Estado)),
                            ("Fecha de emisión", fechaEmision),
                            ("Vigencia", fechaVigencia),
                            ("Cliente", CotizacionTextOrDash(documento.Cliente)),
                            ("Teléfono", CotizacionTextOrDash(documento.ClienteTelefono)),
                            ("Correo", CotizacionTextOrDash(documento.ClienteCorreo)),
                            ("Sucursal", CotizacionTextOrDash(documento.Sucursal)),
                            ("Vendedor", CotizacionTextOrDash(documento.Vendedor)),
                            ("Caja", CotizacionTextOrDash(documento.Caja)),
                            ("Piezas", documento.TotalPiezas.ToString("0.##", CultureInfo.InvariantCulture)),
                            ("Partidas", documento.Partidas.Count.ToString(CultureInfo.InvariantCulture))
                        }));

                        content.Item().Element(x => ComposeCotizacionInfoCard(x, "Observaciones", new[]
                        {
                            ("Detalle", observaciones)
                        }));

                        content.Item().Element(x => ComposeCotizacionPartidasTable(x, documento.Partidas));

                        content.Item().AlignRight().Width(220).Element(x => ComposeCotizacionTotalsCard(x, documento));
                    });

                    page.Footer()
                        .AlignCenter()
                        .DefaultTextStyle(x => x.FontSize(8).FontColor("#39394D"))
                        .Text(text =>
                        {
                            text.Span("CheckApp · Cotización · ");
                            text.CurrentPageNumber();
                            text.Span(" / ");
                            text.TotalPages();
                        });
                });
            }).GeneratePdf();
        }

        private static void ComposeCotizacionMetadataTable(IContainer container, IReadOnlyCollection<(string Label, string Value)> rows)
        {
            container
                .Border(1)
                .BorderColor("#4791AA")
                .CornerRadius(12)
                .Background("#FAFAFA")
                .Padding(10)
                .Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    int index = 0;
                    foreach ((string label, string value) in rows)
                    {
                        table.Cell().Element(cell =>
                        {
                            cell
                                .Padding(8)
                                .BorderBottom(index < rows.Count - 4 ? 1 : 0)
                                .BorderRight(index % 4 != 3 ? 1 : 0)
                                .BorderColor("#4791AA")
                                .Column(column =>
                                {
                                    column.Spacing(3);
                                    column.Item().Text(label).FontSize(8).SemiBold().FontColor("#4791AA");
                                    column.Item().Text(CotizacionTextOrDash(value)).FontSize(10).FontColor("#333638");
                                });
                        });
                        index++;
                    }
                });
        }

        private static void ComposeCotizacionInfoCard(IContainer container, string title, IEnumerable<(string Label, string Value)> rows)
        {
            container
                .Background("#FAFAFA")
                .Border(1)
                .BorderColor("#4791AA")
                .CornerRadius(12)
                .Padding(16)
                .Column(column =>
                {
                    column.Spacing(8);
                    column.Item().Text(title).SemiBold().FontSize(12).FontColor("#39394D");

                    foreach ((string label, string value) in rows)
                    {
                        column.Item().Column(item =>
                        {
                            item.Spacing(2);
                            item.Item().Text(label).FontSize(8).SemiBold().FontColor("#4791AA");
                            item.Item().Text(CotizacionTextOrDash(value)).FontSize(10).FontColor("#333638");
                        });
                    }
                });
        }

        private static void ComposeCotizacionPartidasTable(IContainer container, IReadOnlyCollection<CotizacionDocumentoPartidaDto> partidas)
        {
            container.Column(column =>
            {
                column.Spacing(10);
                column.Item().Text("Partidas").SemiBold().FontSize(12).FontColor("#39394D");
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(34);
                        columns.RelativeColumn(1.15f);
                        columns.RelativeColumn(4.3f);
                        columns.RelativeColumn(1.45f);
                        columns.RelativeColumn(1.05f);
                        columns.RelativeColumn(1.3f);
                        columns.RelativeColumn(1.1f);
                        columns.RelativeColumn(1.45f);
                    });

                    table.Header(header =>
                    {
                        const string background = "#39394D";
                        header.Cell().Element(x => CotizacionPdfHeaderCell(x, "No.", background));
                        header.Cell().Element(x => CotizacionPdfHeaderCell(x, "Código", background));
                        header.Cell().Element(x => CotizacionPdfHeaderCell(x, "Producto o servicio", background));
                        header.Cell().Element(x => CotizacionPdfHeaderCell(x, "Unidad", background));
                        header.Cell().Element(x => CotizacionPdfHeaderCell(x, "Cant.", background));
                        header.Cell().Element(x => CotizacionPdfHeaderCell(x, "Precio", background));
                        header.Cell().Element(x => CotizacionPdfHeaderCell(x, "Desc.", background));
                        header.Cell().Element(x => CotizacionPdfHeaderCell(x, "Total", background));
                    });

                    if (partidas.Count == 0)
                    {
                        table.Cell().ColumnSpan(8).Element(x => CotizacionPdfBodyCell(x, "Sin partidas capturadas.", "#FAFAFA"));
                        return;
                    }

                    int index = 0;
                    foreach (CotizacionDocumentoPartidaDto partida in partidas)
                    {
                        string rowBackground = index % 2 == 0 ? "#FAFAFA" : "#FFFFFF";
                        string detalle = BuildCotizacionProductLine(partida);
                        string unidad = BuildCotizacionUnidad(partida);
                        table.Cell().Element(x => CotizacionPdfBodyCell(x, partida.NumeroPartida.ToString(CultureInfo.InvariantCulture), rowBackground, TextHorizontalAlignment.Center));
                        table.Cell().Element(x => CotizacionPdfBodyCell(x, CotizacionTextOrDash(partida.Codigo), rowBackground));
                        table.Cell().Element(x => CotizacionPdfBodyCell(x, detalle, rowBackground));
                        table.Cell().Element(x => CotizacionPdfBodyCell(x, unidad, rowBackground));
                        table.Cell().Element(x => CotizacionPdfBodyCell(x, partida.Cantidad.ToString("0.##", CultureInfo.InvariantCulture), rowBackground, TextHorizontalAlignment.Right));
                        table.Cell().Element(x => CotizacionPdfBodyCell(x, CotizacionFormatCurrency(partida.PrecioUnitario), rowBackground, TextHorizontalAlignment.Right));
                        table.Cell().Element(x => CotizacionPdfBodyCell(x, partida.DescuentoPct.ToString("0.##", CultureInfo.InvariantCulture) + "%", rowBackground, TextHorizontalAlignment.Right));
                        table.Cell().Element(x => CotizacionPdfBodyCell(x, CotizacionFormatCurrency(partida.Total), rowBackground, TextHorizontalAlignment.Right));
                        index++;
                    }
                });
            });
        }

        private static void ComposeCotizacionTotalsCard(IContainer container, CotizacionDocumentoExportDto documento)
        {
            container
                .Background("#FAFAFA")
                .Border(1)
                .BorderColor("#4791AA")
                .CornerRadius(12)
                .Padding(14)
                .Column(column =>
                {
                    column.Spacing(8);
                    column.Item().Text("Totales").SemiBold().FontSize(12).FontColor("#39394D");
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Partidas").FontColor("#4791AA");
                        row.ConstantItem(96).AlignRight().Text(documento.Partidas.Count.ToString(CultureInfo.InvariantCulture)).SemiBold();
                    });
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Piezas").FontColor("#4791AA");
                        row.ConstantItem(96).AlignRight().Text(documento.TotalPiezas.ToString("0.##", CultureInfo.InvariantCulture)).SemiBold();
                    });
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Subtotal").FontColor("#4791AA");
                        row.ConstantItem(96).AlignRight().Text(CotizacionFormatCurrency(documento.Subtotal)).SemiBold();
                    });
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Descuento").FontColor("#4791AA");
                        row.ConstantItem(96).AlignRight().Text(CotizacionFormatCurrency(documento.DescuentoTotal)).SemiBold();
                    });
                    column.Item().LineHorizontal(1).LineColor("#4791AA");
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Total").SemiBold().FontColor("#39394D");
                        row.ConstantItem(96).AlignRight().Text(CotizacionFormatCurrency(documento.Total)).SemiBold().FontColor("#FF9230");
                    });
                });
        }

        private static void CotizacionPdfHeaderCell(IContainer container, string text, string background)
        {
            container
                .Background(background)
                .PaddingVertical(8)
                .PaddingHorizontal(6)
                .Text(text)
                .FontSize(9)
                .SemiBold()
                .FontColor("#FAFAFA");
        }

        private static void CotizacionPdfBodyCell(IContainer container, string text, string background, TextHorizontalAlignment alignment = TextHorizontalAlignment.Left)
        {
            IContainer alignedContainer = container
                .Background(background)
                .BorderBottom(1)
                .BorderColor("#4791AA")
                .PaddingVertical(8)
                .PaddingHorizontal(6);

            alignedContainer = alignment switch
            {
                TextHorizontalAlignment.Right => alignedContainer.AlignRight(),
                TextHorizontalAlignment.Center => alignedContainer.AlignCenter(),
                _ => alignedContainer.AlignLeft()
            };

            alignedContainer
                .Text(CotizacionTextOrDash(text))
                .FontSize(9)
                .FontColor("#333638");
        }

        private static byte[]? LoadSharedCheckAppLogo()
        {
            DirectoryInfo? current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current != null)
            {
                string candidate = Path.Combine(current.FullName, "inspector", "checklist", "wwwroot", "assets", "media", "logos", "checkapp2.png");
                if (System.IO.File.Exists(candidate))
                {
                    return System.IO.File.ReadAllBytes(candidate);
                }

                current = current.Parent;
            }

            return null;
        }

        private static string BuildCotizacionProductLine(CotizacionDocumentoPartidaDto partida)
        {
            string nombre = CotizacionTextOrDash(partida.Nombre);
            string descripcion = string.IsNullOrWhiteSpace(partida.Descripcion) ? string.Empty : $" · {partida.Descripcion.Trim()}";
            string existencia = partida.ExistenciaActual.HasValue ? $" · Existencia actual: {partida.ExistenciaActual.Value.ToString("0.##", CultureInfo.InvariantCulture)}" : string.Empty;
            return $"{nombre}{descripcion}{existencia}";
        }

        private static string BuildCotizacionUnidad(CotizacionDocumentoPartidaDto partida)
        {
            return CotizacionTextOrDash(partida.Unidad);
        }

        private static string CotizacionTextOrDash(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();
        }

        private static string CotizacionFormatCurrency(decimal amount)
        {
            return amount.ToString("$#,##0.00", CultureInfo.GetCultureInfo("es-MX"));
        }

        private static List<CotizacionPdfPartidaLayout> BuildCotizacionPdfPartidas(CotizacionDocumentoExportDto documento)
        {
            return documento.Partidas
                .Select(partida =>
                {
                    string unidad = string.IsNullOrWhiteSpace(partida.Unidad) ? "—" : partida.Unidad.Trim();
                    string descripcion = string.IsNullOrWhiteSpace(partida.Descripcion)
                        ? PdfValueOrDash(partida.Nombre)
                        : $"{PdfValueOrDash(partida.Nombre)} / {partida.Descripcion.Trim()}";
                    List<string> descripcionLineas = WrapPdfText(descripcion, 32).ToList();
                    if (descripcionLineas.Count == 0)
                    {
                        descripcionLineas.Add("—");
                    }

                    string? existencia = partida.ExistenciaActual.HasValue
                        ? $"{partida.ExistenciaActual.Value.ToString("0.##", CultureInfo.InvariantCulture)} {unidad}".Trim()
                        : null;

                    decimal height = 26m + (descripcionLineas.Count * 11m) + (string.IsNullOrWhiteSpace(existencia) ? 0m : 11m);
                    return new CotizacionPdfPartidaLayout
                    {
                        Numero = partida.NumeroPartida.ToString(CultureInfo.InvariantCulture),
                        Codigo = PdfValueOrDash(partida.Codigo),
                        Unidad = unidad,
                        Cantidad = partida.Cantidad.ToString("0.##", CultureInfo.InvariantCulture),
                        Precio = FormatCurrencyPdf(partida.PrecioUnitario),
                        Descuento = partida.DescuentoPct.ToString("0.##", CultureInfo.InvariantCulture) + "%",
                        Total = FormatCurrencyPdf(partida.Total),
                        DescripcionLineas = descripcionLineas,
                        Existencia = existencia,
                        Height = height
                    };
                })
                .ToList();
        }

        private static List<string> BuildCotizacionPdfObservaciones(CotizacionDocumentoExportDto documento)
        {
            List<string> observaciones = WrapPdfText(PdfValueOrDash(documento.Observaciones), 92).ToList();
            if (observaciones.Count == 0)
            {
                observaciones.Add("Sin observaciones registradas.");
            }

            return observaciones;
        }

        private static List<List<CotizacionPdfPartidaLayout>> BuildCotizacionPdfPages(List<CotizacionPdfPartidaLayout> partidas, int observacionesCount)
        {
            decimal firstPageTableStart = 548m - (34m + (Math.Max(observacionesCount, 1) * 11m)) - 48m;
            decimal firstPageCapacity = Math.Max(180m, firstPageTableStart - 168m);
            const decimal standardCapacity = 560m;
            const decimal lastPageCapacity = 448m;

            List<List<CotizacionPdfPartidaLayout>> pages = new List<List<CotizacionPdfPartidaLayout>>();
            List<CotizacionPdfPartidaLayout> current = new List<CotizacionPdfPartidaLayout>();
            decimal used = 0m;
            decimal capacity = firstPageCapacity;

            foreach (CotizacionPdfPartidaLayout partida in partidas)
            {
                if (current.Count > 0 && used + partida.Height > capacity)
                {
                    pages.Add(current);
                    current = new List<CotizacionPdfPartidaLayout>();
                    used = 0m;
                    capacity = standardCapacity;
                }

                current.Add(partida);
                used += partida.Height;
            }

            if (current.Count > 0 || pages.Count == 0)
            {
                pages.Add(current);
            }

            while (pages.Count > 1 && pages[^1].Sum(item => item.Height) > lastPageCapacity)
            {
                List<CotizacionPdfPartidaLayout> overflow = new List<CotizacionPdfPartidaLayout>();
                while (pages[^1].Count > 1 && pages[^1].Sum(item => item.Height) > lastPageCapacity)
                {
                    CotizacionPdfPartidaLayout moved = pages[^1][^1];
                    pages[^1].RemoveAt(pages[^1].Count - 1);
                    overflow.Insert(0, moved);
                }

                if (overflow.Count == 0)
                {
                    break;
                }

                pages.Add(overflow);
            }

            return pages;
        }

        private static string BuildPdfContentStream(
            CotizacionDocumentoExportDto documento,
            List<CotizacionPdfPartidaLayout> partidas,
            List<string> observaciones,
            int pageNumber,
            int pageCount,
            bool isFirstPage,
            bool isLastPage)
        {
            StringBuilder content = new StringBuilder();
            const decimal pageWidth = 595m;
            const decimal left = 32m;
            const decimal contentWidth = 531m;

            if (isFirstPage)
            {
                AppendText(content, "F2", 24m, left, 798m, "Check", 21, 28, 40);
                AppendText(content, "F2", 24m, left + 68m, 798m, "App", 245, 128, 32);
                AppendText(content, "F2", 18m, left, 767m, "COTIZACION", 21, 28, 40);
                AppendText(content, "F1", 10m, left, 748m, "Documento comercial de CheckApp", 107, 114, 128);
                AppendLine(content, left, 734m, left + contentWidth, 734m, 236, 0, 0, 1m);

                AppendFillStrokeRect(content, 393m, 716m, 170m, 74m, 255, 255, 255, 231, 236, 242, 1m);
                AppendText(content, "F2", 8.8m, 409m, 772m, "FOLIO", 236, 0, 0);
                AppendText(content, "F2", 17m, 409m, 750m, PdfValueOrDash(documento.Folio), 21, 28, 40);
                AppendText(content, "F1", 9m, 409m, 732m, "Emision: " + documento.FechaCotizacion.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture), 107, 114, 128);

                AppendFillStrokeRect(content, left, 588m, contentWidth, 118m, 255, 255, 255, 231, 236, 242, 1m);
                AppendText(content, "F2", 8.8m, left + 16m, 690m, "DATOS GENERALES", 236, 0, 0);
                AppendInfoPair(content, left + 16m, 668m, "Cliente", PdfValueOrDash(documento.Cliente));
                AppendInfoPair(content, left + 16m, 646m, "Telefono", PdfValueOrDash(documento.ClienteTelefono));
                AppendInfoPair(content, left + 16m, 624m, "Correo", PdfValueOrDash(documento.ClienteCorreo));
                AppendInfoPair(content, left + 16m, 602m, "Sucursal", PdfValueOrDash(documento.Sucursal));
                AppendInfoPair(content, left + 286m, 668m, "Vendedor", PdfValueOrDash(documento.Vendedor));
                AppendInfoPair(content, left + 286m, 646m, "Estado", PdfValueOrDash(documento.Estado));
                AppendInfoPair(content, left + 286m, 624m, "Vigencia", documento.FechaVigencia.HasValue ? documento.FechaVigencia.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) : "No definida");
                AppendInfoPair(content, left + 286m, 602m, "Caja", PdfValueOrDash(documento.Caja));

                decimal obsHeight = 34m + (Math.Max(observaciones.Count, 1) * 11m);
                decimal obsTop = 548m;
                AppendFillStrokeRect(content, left, obsTop - obsHeight, contentWidth, obsHeight, 255, 255, 255, 231, 236, 242, 1m);
                AppendText(content, "F2", 8.8m, left + 16m, obsTop - 16m, "OBSERVACIONES", 236, 0, 0);
                decimal obsTextY = obsTop - 34m;
                foreach (string line in observaciones)
                {
                    AppendText(content, "F1", 9.8m, left + 16m, obsTextY, line, 79, 89, 107);
                    obsTextY -= 11m;
                }

                DrawCotizacionPdfTable(content, partidas, obsTop - obsHeight - 22m, isLastPage);
            }
            else
            {
                AppendText(content, "F2", 15m, left, 798m, "COTIZACION · CONTINUACION", 21, 28, 40);
                AppendText(content, "F1", 9.5m, left, 780m, "Folio " + PdfValueOrDash(documento.Folio), 107, 114, 128);
                AppendFillStrokeRect(content, 430m, 770m, 133m, 34m, 255, 255, 255, 231, 236, 242, 1m);
                AppendText(content, "F2", 10m, 444m, 783m, FormatCurrencyPdf(documento.Total), 21, 28, 40);
                AppendText(content, "F1", 8.5m, 444m, 771m, "Total cotizado", 107, 114, 128);
                AppendLine(content, left, 756m, pageWidth - left, 756m, 236, 0, 0, 1m);
                DrawCotizacionPdfTable(content, partidas, 732m, isLastPage);
            }

            if (isLastPage)
            {
                DrawCotizacionPdfTotals(content, documento);
            }

            AppendLine(content, left, 44m, pageWidth - left, 44m, 231, 236, 242, 0.8m);
            AppendText(content, "F1", 8.8m, left, 28m, "CheckApp · Cotizaciones", 107, 114, 128);
            AppendText(content, "F1", 8.8m, pageWidth - 138m, 28m, $"Pagina {pageNumber} de {pageCount}", 107, 114, 128);
            return content.ToString();
        }

        private static void DrawCotizacionPdfTable(StringBuilder content, List<CotizacionPdfPartidaLayout> partidas, decimal tableTop, bool isLastPage)
        {
            const decimal left = 32m;
            decimal[] widths = { 28m, 52m, 182m, 62m, 42m, 54m, 40m, 71m };
            decimal[] xs = new decimal[widths.Length];
            decimal cursor = left;
            for (int i = 0; i < widths.Length; i++)
            {
                xs[i] = cursor;
                cursor += widths[i];
            }

            AppendFillRect(content, left, tableTop - 26m, 531m, 26m, 58, 74, 96);
            string[] headers = { "#", "Codigo", "Producto o servicio", "Unidad", "Cant.", "PU", "Desc.", "Total" };
            for (int i = 0; i < headers.Length; i++)
            {
                AppendText(content, "F2", 8.6m, xs[i] + 4m, tableTop - 16m, headers[i], 255, 255, 255);
            }

            decimal y = tableTop - 32m;
            if (partidas.Count == 0)
            {
                AppendStrokeRect(content, left, y - 32m, 531m, 32m, 231, 236, 242, 1m);
                AppendText(content, "F1", 10m, left + 8m, y - 20m, "Sin partidas capturadas.", 79, 89, 107);
                return;
            }

            for (int index = 0; index < partidas.Count; index++)
            {
                CotizacionPdfPartidaLayout partida = partidas[index];
                decimal rowTop = y;
                decimal rowBottom = rowTop - partida.Height;

                if (index % 2 == 0)
                {
                    AppendFillRect(content, left, rowBottom, 531m, partida.Height, 252, 248, 245);
                }

                AppendLine(content, left, rowBottom, left + 531m, rowBottom, 231, 236, 242, 0.75m);
                decimal textY = rowTop - 16m;
                AppendText(content, "F1", 9.4m, xs[0] + 4m, textY, partida.Numero, 33, 37, 41);
                AppendText(content, "F1", 9.4m, xs[1] + 4m, textY, partida.Codigo, 33, 37, 41);

                for (int i = 0; i < partida.DescripcionLineas.Count; i++)
                {
                    AppendText(content, i == 0 ? "F2" : "F1", i == 0 ? 9.4m : 8.9m, xs[2] + 4m, textY - (i * 11m), partida.DescripcionLineas[i], i == 0 ? 33 : 79, i == 0 ? 37 : 89, i == 0 ? 41 : 107);
                }

                decimal metaY = textY - ((partida.DescripcionLineas.Count - 1) * 11m);
                if (!string.IsNullOrWhiteSpace(partida.Existencia))
                {
                    AppendText(content, "F1", 8.3m, xs[2] + 4m, metaY - 11m, "Existencia actual: " + partida.Existencia, 107, 114, 128);
                }

                AppendText(content, "F1", 9m, xs[3] + 4m, textY, partida.Unidad, 79, 89, 107);
                AppendText(content, "F1", 9m, xs[4] + 4m, textY, partida.Cantidad, 33, 37, 41);
                AppendText(content, "F1", 9m, xs[5] + 4m, textY, partida.Precio, 33, 37, 41);
                AppendText(content, "F1", 9m, xs[6] + 4m, textY, partida.Descuento, 33, 37, 41);
                AppendText(content, "F2", 9.2m, xs[7] + 4m, textY, partida.Total, 33, 37, 41);

                y = rowBottom;
            }

            if (isLastPage)
            {
                AppendText(content, "F1", 8.4m, left, y - 16m, "Las partidas y totales corresponden al estado actual de la cotizacion.", 107, 114, 128);
            }
        }

        private static void DrawCotizacionPdfTotals(StringBuilder content, CotizacionDocumentoExportDto documento)
        {
            const decimal left = 32m;
            AppendFillStrokeRect(content, 360m, 78m, 203m, 94m, 255, 255, 255, 231, 236, 242, 1m);
            AppendText(content, "F2", 8.8m, 376m, 156m, "TOTALES", 236, 0, 0);
            AppendSummaryLine(content, 376m, 136m, "Piezas", documento.TotalPiezas.ToString("0.##", CultureInfo.InvariantCulture), false);
            AppendSummaryLine(content, 376m, 118m, "Subtotal", FormatCurrencyPdf(documento.Subtotal), false);
            AppendSummaryLine(content, 376m, 100m, "Descuento", FormatCurrencyPdf(documento.DescuentoTotal), false);
            AppendSummaryLine(content, 376m, 82m, "Total", FormatCurrencyPdf(documento.Total), true);

            AppendFillStrokeRect(content, left, 78m, 300m, 94m, 252, 248, 245, 231, 236, 242, 1m);
            AppendText(content, "F2", 8.8m, left + 16m, 156m, "REFERENCIA", 236, 0, 0);
            AppendText(content, "F1", 9.2m, left + 16m, 136m, "Cliente: " + PdfValueOrDash(documento.Cliente), 79, 89, 107);
            AppendText(content, "F1", 9.2m, left + 16m, 118m, "Folio: " + PdfValueOrDash(documento.Folio), 79, 89, 107);
            AppendText(content, "F1", 9.2m, left + 16m, 100m, "Emision: " + documento.FechaCotizacion.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture), 79, 89, 107);
            AppendText(content, "F1", 9.2m, left + 16m, 82m, "Documento generado por CheckApp.", 79, 89, 107);
        }

        private static void AppendInfoPair(StringBuilder content, decimal x, decimal y, string label, string value)
        {
            AppendText(content, "F2", 8.4m, x, y, label.ToUpperInvariant(), 107, 114, 128);
            AppendText(content, "F1", 9.8m, x, y - 12m, value, 33, 37, 41);
        }

        private static void AppendSummaryLine(StringBuilder content, decimal x, decimal y, string label, string value, bool emphasize)
        {
            AppendText(content, "F1", 8.8m, x, y, label, 107, 114, 128);
            AppendText(content, emphasize ? "F2" : "F1", emphasize ? 12m : 9.6m, x + 88m, y, value, 21, 28, 40);
        }

        private static void AppendText(StringBuilder content, string font, decimal size, decimal x, decimal y, string text, int red, int green, int blue)
        {
            content.Append(PdfColor(red)).Append(' ').Append(PdfColor(green)).Append(' ').Append(PdfColor(blue)).Append(" rg\n");
            content.Append("BT\n/")
                .Append(font)
                .Append(' ')
                .Append(PdfNumber(size))
                .Append(" Tf\n1 0 0 1 ")
                .Append(PdfNumber(x))
                .Append(' ')
                .Append(PdfNumber(y))
                .Append(" Tm\n(")
                .Append(EscapePdfText(text))
                .Append(") Tj\nET\n");
        }

        private static void AppendFillRect(StringBuilder content, decimal x, decimal y, decimal width, decimal height, int red, int green, int blue)
        {
            content.Append(PdfColor(red)).Append(' ').Append(PdfColor(green)).Append(' ').Append(PdfColor(blue)).Append(" rg\n")
                .Append(PdfNumber(x)).Append(' ')
                .Append(PdfNumber(y)).Append(' ')
                .Append(PdfNumber(width)).Append(' ')
                .Append(PdfNumber(height)).Append(" re f\n");
        }

        private static void AppendStrokeRect(StringBuilder content, decimal x, decimal y, decimal width, decimal height, int red, int green, int blue, decimal lineWidth)
        {
            content.Append(PdfColor(red)).Append(' ').Append(PdfColor(green)).Append(' ').Append(PdfColor(blue)).Append(" RG\n")
                .Append(PdfNumber(lineWidth)).Append(" w\n")
                .Append(PdfNumber(x)).Append(' ')
                .Append(PdfNumber(y)).Append(' ')
                .Append(PdfNumber(width)).Append(' ')
                .Append(PdfNumber(height)).Append(" re S\n");
        }

        private static void AppendFillStrokeRect(StringBuilder content, decimal x, decimal y, decimal width, decimal height, int fillRed, int fillGreen, int fillBlue, int strokeRed, int strokeGreen, int strokeBlue, decimal lineWidth)
        {
            content.Append(PdfColor(fillRed)).Append(' ').Append(PdfColor(fillGreen)).Append(' ').Append(PdfColor(fillBlue)).Append(" rg\n")
                .Append(PdfColor(strokeRed)).Append(' ').Append(PdfColor(strokeGreen)).Append(' ').Append(PdfColor(strokeBlue)).Append(" RG\n")
                .Append(PdfNumber(lineWidth)).Append(" w\n")
                .Append(PdfNumber(x)).Append(' ')
                .Append(PdfNumber(y)).Append(' ')
                .Append(PdfNumber(width)).Append(' ')
                .Append(PdfNumber(height)).Append(" re B\n");
        }

        private static void AppendLine(StringBuilder content, decimal x1, decimal y1, decimal x2, decimal y2, int red, int green, int blue, decimal lineWidth)
        {
            content.Append(PdfColor(red)).Append(' ').Append(PdfColor(green)).Append(' ').Append(PdfColor(blue)).Append(" RG\n")
                .Append(PdfNumber(lineWidth)).Append(" w\n")
                .Append(PdfNumber(x1)).Append(' ').Append(PdfNumber(y1)).Append(" m\n")
                .Append(PdfNumber(x2)).Append(' ').Append(PdfNumber(y2)).Append(" l S\n");
        }

        private static string FormatCurrencyPdf(decimal amount)
            => "$" + amount.ToString("#,##0.00", CultureInfo.InvariantCulture);

        private static string PdfNumber(decimal value)
            => value.ToString("0.##", CultureInfo.InvariantCulture);

        private static string PdfColor(int channel)
            => (channel / 255m).ToString("0.###", CultureInfo.InvariantCulture);

        private static string PdfValueOrDash(string? value)
            => string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();

        private sealed class CotizacionPdfPartidaLayout
        {
            public string Numero { get; set; } = string.Empty;
            public string Codigo { get; set; } = string.Empty;
            public List<string> DescripcionLineas { get; set; } = new List<string>();
            public string Unidad { get; set; } = string.Empty;
            public string Cantidad { get; set; } = string.Empty;
            public string Precio { get; set; } = string.Empty;
            public string Descuento { get; set; } = string.Empty;
            public string Total { get; set; } = string.Empty;
            public string? Existencia { get; set; }
            public decimal Height { get; set; }
        }

        private static IEnumerable<string> WrapPdfText(string text, int maxLength)
        {
            string current = string.Empty;
            foreach (string word in (text ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                string candidate = string.IsNullOrWhiteSpace(current) ? word : current + " " + word;
                if (candidate.Length <= maxLength)
                {
                    current = candidate;
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(current))
                {
                    yield return current;
                }

                current = word.Length <= maxLength ? word : word.Substring(0, maxLength);
            }

            if (!string.IsNullOrWhiteSpace(current))
            {
                yield return current;
            }
        }

        private static string EscapePdfText(string text)
        {
            return (text ?? string.Empty)
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("(", "\\(", StringComparison.Ordinal)
                .Replace(")", "\\)", StringComparison.Ordinal);
        }

        private static string ShortenPdfText(string text, int maxLength)
        {
            string value = (text ?? string.Empty).Trim();
            if (value.Length <= maxLength)
            {
                return value;
            }

            return value.Substring(0, Math.Max(0, maxLength - 1)) + "…";
        }

        private sealed class RequestContext
        {
            public Guid IdEmpresa { get; set; }
            public string Empresa { get; set; } = string.Empty;
            public Guid? UsuarioId { get; set; }
            public string Correo { get; set; } = string.Empty;
        }

        private sealed class UserMetadata
        {
            public Guid Id { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public Guid? IdSucursal { get; set; }
        }

        private sealed class ClienteContext
        {
            public Guid Id { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public string Telefono { get; set; } = string.Empty;
            public string Correo { get; set; } = string.Empty;
            public decimal Descuento { get; set; }
        }

        private sealed class ProductoContext
        {
            public Guid Id { get; set; }
            public byte Tipo { get; set; }
            public string Codigo { get; set; } = string.Empty;
            public string Nombre { get; set; } = string.Empty;
            public string Descripcion { get; set; } = string.Empty;
            public Guid IdUnidadMedida { get; set; }
            public string UnidadMedida { get; set; } = string.Empty;
            public string UnidadAbreviatura { get; set; } = string.Empty;
            public bool UnidadPermiteDecimales { get; set; }
            public bool PermiteVentaSinExistencia { get; set; }
            public decimal? ExistenciaActual { get; set; }
        }

        private sealed class CotizacionPartidaDbRow
        {
            public Guid Id { get; set; }
            public Guid IdProductoServicio { get; set; }
            public string Codigo { get; set; } = string.Empty;
            public string Nombre { get; set; } = string.Empty;
            public string Descripcion { get; set; } = string.Empty;
            public byte TipoProductoServicio { get; set; }
            public Guid IdUnidadMedida { get; set; }
            public string UnidadMedida { get; set; } = string.Empty;
            public string UnidadAbreviatura { get; set; } = string.Empty;
            public bool UnidadPermiteDecimales { get; set; }
            public bool PermiteVentaSinExistencia { get; set; }
            public decimal? ExistenciaActual { get; set; }
            public decimal Cantidad { get; set; }
            public decimal PrecioUnitario { get; set; }
            public decimal DescuentoPct { get; set; }
            public decimal ImporteBruto { get; set; }
            public decimal DescuentoImporte { get; set; }
            public decimal Total { get; set; }
        }

        private sealed class TotalesCotizacion
        {
            public decimal Subtotal { get; set; }
            public decimal DescuentoTotal { get; set; }
            public decimal Total { get; set; }
            public decimal TotalPiezas { get; set; }
        }

        private sealed class CotizacionPersistedRow
        {
            public Guid Id { get; set; }
            public string Folio { get; set; } = string.Empty;
            public byte Estado { get; set; }
            public DateTime FechaCotizacion { get; set; }
            public Guid? IdSucursal { get; set; }
        }
    }
}
