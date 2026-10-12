using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using checklistWs.Models.OrdenesCompra;
using checklistWs.Services.Tenant;
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
        private const byte EstadoParcialmenteRecibida = 4;
        private const byte EstadoRecibida = 5;
        private const byte TipoProducto = 1;
        private const byte TipoServicio = 2;
        private const int FolioPadding = 6;
        private const int ObservacionesLength = 1000;
        private const int MotivoCancelacionLength = 500;
        private const int BusquedaLength = 200;
        private const int ExportacionLimit = 5000;
        private static readonly TimeSpan CierreTokenLifetime = TimeSpan.FromMinutes(20);
        private static readonly JsonSerializerOptions CierreJsonOptions = new(JsonSerializerDefaults.Web);
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
        private readonly IProductosServiciosAuthorizationService? _authorizationService;

        public OrdenesCompraController(
            IConfiguration configuration,
            ILogger<OrdenesCompraController> logger,
            IProductosServiciosAuthorizationService? authorizationService = null)
        {
            _configuration = configuration;
            _connectionFactory = new SqlConnectionFactory(configuration);
            _logger = logger;
            _authorizationService = authorizationService;
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
            IActionResult? auth = await AuthorizeOrdenCompraAsync(
                context,
                ProductosServiciosAuthorizationDefaults.OrdenesCompraReportePermissionCode,
                ProductosServiciosPermissionRequirement.Read);
            if (auth != null)
            {
                return auth;
            }

            try
            {
                using SqlConnection connection = CreateConnection();
                await connection.OpenAsync();

                StringBuilder query = new StringBuilder(@"
SELECT
    oc.id,
	    oc.Folio,
	    ISNULL(oc.FolioReferencia, '') AS FolioReferencia,
    oc.FechaOrden,
    oc.FechaLlegada,
    oc.FechaMinima,
    oc.FechaMaxima,
    oc.idRazonSocial,
    ISNULL(rs.Nombre, '') AS RazonSocial,
    oc.idSucursal,
    ISNULL(dest.Sucursales, ISNULL(su.Nombre, '')) AS Sucursal,
    oc.idProveedor,
    ISNULL(pr.Nombre, '') AS Proveedor,
    oc.Estado,
    ISNULL(tr.CantidadOrdenada, 0) AS CantidadOrdenada,
    ISNULL(tr.CantidadRecibida, 0) AS CantidadRecibida,
    ISNULL(tr.CantidadPendiente, 0) AS CantidadPendiente,
    oc.Total,
    oc.FechaCreacion
FROM dbo.OrdenesCompra oc
LEFT JOIN dbo.RazonesSociales rs
    ON rs.id = oc.idRazonSocial AND rs.idEmpresa = oc.idEmpresa
LEFT JOIN dbo.Sucursales su
    ON su.id = oc.idSucursal AND su.idEmpresa = oc.idEmpresa
OUTER APPLY (
    SELECT STRING_AGG(CONVERT(nvarchar(max), s2.Nombre), ', ') WITHIN GROUP (ORDER BY s2.Nombre) AS Sucursales
    FROM dbo.OrdenesCompraSucursales os
    INNER JOIN dbo.Sucursales s2 ON s2.idEmpresa=os.idEmpresa AND s2.id=os.idSucursal
    WHERE os.idEmpresa=oc.idEmpresa AND os.idOrdenCompra=oc.id AND os.Activo=1 AND os.FechaArchivado IS NULL
) dest
LEFT JOIN dbo.ActivosProveedores pr
    ON pr.id = oc.idProveedor AND pr.idEmpresa = oc.idEmpresa
OUTER APPLY (
    SELECT
        SUM(d.CantidadBaseOrdenada) AS CantidadOrdenada,
        SUM(d.CantidadBaseRecibidaAcumulada) AS CantidadRecibida,
        SUM(d.CantidadBasePendiente) AS CantidadPendiente
    FROM dbo.OrdenesCompraDetalle d
    WHERE d.idEmpresa = oc.idEmpresa
      AND d.idOrdenCompra = oc.id
      AND d.Activo = 1
      AND d.FechaArchivado IS NULL
) tr
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
	        OR ISNULL(oc.FolioReferencia, '') LIKE @Busqueda
        OR ISNULL(rs.Nombre, '') LIKE @Busqueda
        OR ISNULL(dest.Sucursales, ISNULL(su.Nombre, '')) LIKE @Busqueda
        OR ISNULL(pr.Nombre, '') LIKE @Busqueda
        OR ISNULL(oc.Observaciones, '') LIKE @Busqueda
      )");
                    command.Parameters.AddWithValue("@Busqueda", $"%{Truncate(busqueda, BusquedaLength)}%");
                }

                AppendTinyIntFilter(query, command, "oc.Estado", "@Estado", estado);
                AppendGuidFilter(query, command, "oc.idProveedor", "@IdProveedor", idProveedor);
                AppendGuidFilter(query, command, "oc.idRazonSocial", "@IdRazonSocial", idRazonSocial);
                if (idSucursal.HasValue && idSucursal.Value != Guid.Empty)
                {
                    query.Append(" AND EXISTS (SELECT 1 FROM dbo.OrdenesCompraSucursales osf WHERE osf.idEmpresa=oc.idEmpresa AND osf.idOrdenCompra=oc.id AND osf.idSucursal=@IdSucursal AND osf.Activo=1 AND osf.FechaArchivado IS NULL)");
                    command.Parameters.AddWithValue("@IdSucursal", idSucursal.Value);
                }
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
                        FolioReferencia = ReadString(reader, "FolioReferencia"),
                        FechaOrden = ReadDateTime(reader, "FechaOrden"),
                        FechaLlegada = ReadNullableDateTime(reader, "FechaLlegada"),
                        FechaMinima = ReadNullableDateTime(reader, "FechaMinima"),
                        FechaMaxima = ReadNullableDateTime(reader, "FechaMaxima"),
                        IdRazonSocial = ReadGuid(reader, "idRazonSocial"),
                        RazonSocial = ReadString(reader, "RazonSocial"),
                        IdSucursal = ReadNullableGuid(reader, "idSucursal"),
                        Sucursal = ReadString(reader, "Sucursal"),
                        IdProveedor = ReadGuid(reader, "idProveedor"),
                        Proveedor = ReadString(reader, "Proveedor"),
                        Estado = estadoActual,
                        EstadoNombre = GetEstadoNombre(estadoActual),
                        CantidadOrdenada = ReadDecimal(reader, "CantidadOrdenada"),
                        CantidadRecibida = ReadDecimal(reader, "CantidadRecibida"),
                        CantidadPendiente = ReadDecimal(reader, "CantidadPendiente"),
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
            IActionResult? auth = await AuthorizeOrdenCompraAnyAsync(
                context,
                ProductosServiciosPermissionRequirement.Read,
                ProductosServiciosAuthorizationDefaults.OrdenesCompraNuevaPermissionCode,
                ProductosServiciosAuthorizationDefaults.OrdenesCompraReportePermissionCode);
            if (auth != null)
            {
                return auth;
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
	    ISNULL(oc.FolioReferencia, '') AS FolioReferencia,
    oc.idRazonSocial,
    ISNULL(rs.Nombre, '') AS RazonSocial,
    oc.idSucursal,
    ISNULL(su.Nombre, '') AS Sucursal,
    oc.idProveedor,
    ISNULL(pr.Nombre, '') AS Proveedor,
    oc.FechaOrden,
    oc.FechaLlegada,
    oc.FechaMinima,
    oc.FechaMaxima,
    oc.Estado,
    ISNULL(oc.Observaciones, '') AS Observaciones,
    oc.Subtotal,
    oc.Total,
    ISNULL(oc.MotivoCancelacion, '') AS MotivoCancelacion,
    oc.FechaCancelacion,
    oc.idUsuarioCreacion,
    LTRIM(RTRIM(CONCAT(
        ISNULL(uc.Nombre, ''),
        CASE WHEN NULLIF(LTRIM(RTRIM(ISNULL(uc.ApellidoPaterno, ''))), '') IS NULL THEN '' ELSE ' ' + LTRIM(RTRIM(uc.ApellidoPaterno)) END,
        CASE WHEN NULLIF(LTRIM(RTRIM(ISNULL(uc.ApellidoMaterno, ''))), '') IS NULL THEN '' ELSE ' ' + LTRIM(RTRIM(uc.ApellidoMaterno)) END
    ))) AS UsuarioCreacionNombre,
    ISNULL(up.Nombre, '') AS UsuarioCreacionPuesto,
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
LEFT JOIN dbo.Usuarios uc
    ON uc.id = oc.idUsuarioCreacion AND uc.idEmpresa = oc.idEmpresa
LEFT JOIN dbo.UsuariosPuestos up
    ON up.id = uc.idPuesto AND up.idEmpresa = oc.idEmpresa
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
                        FolioReferencia = ReadString(reader, "FolioReferencia"),
                        IdRazonSocial = ReadGuid(reader, "idRazonSocial"),
                        RazonSocial = ReadString(reader, "RazonSocial"),
                        IdSucursal = ReadNullableGuid(reader, "idSucursal"),
                        Sucursal = ReadString(reader, "Sucursal"),
                        IdProveedor = ReadGuid(reader, "idProveedor"),
                        Proveedor = ReadString(reader, "Proveedor"),
                        FechaOrden = ReadDateTime(reader, "FechaOrden"),
                        FechaLlegada = ReadNullableDateTime(reader, "FechaLlegada"),
                        FechaMinima = ReadNullableDateTime(reader, "FechaMinima"),
                        FechaMaxima = ReadNullableDateTime(reader, "FechaMaxima"),
                        Estado = estadoActual,
                        EstadoNombre = GetEstadoNombre(estadoActual),
                        Observaciones = ReadString(reader, "Observaciones"),
                        Subtotal = ReadDecimal(reader, "Subtotal"),
                        Total = ReadDecimal(reader, "Total"),
                        MotivoCancelacion = ReadString(reader, "MotivoCancelacion"),
                        FechaCancelacion = ReadNullableDateTime(reader, "FechaCancelacion"),
                        IdUsuarioCreacion = ReadNullableGuid(reader, "idUsuarioCreacion"),
                        UsuarioCreacionNombre = ReadString(reader, "UsuarioCreacionNombre"),
                        UsuarioCreacionPuesto = ReadString(reader, "UsuarioCreacionPuesto"),
                        IdUsuarioActualizacion = ReadNullableGuid(reader, "idUsuarioActualizacion"),
                        IdUsuarioCancelacion = ReadNullableGuid(reader, "idUsuarioCancelacion"),
                        FechaCreacion = ReadDateTime(reader, "FechaCreacion"),
                        FechaActualizacion = ReadDateTime(reader, "FechaActualizacion")
                    };
                }

                detalle.Sucursales = await ObtenerSucursalesAsync(connection, context.IdEmpresa, idOrdenCompra);
                detalle.Sucursal = string.Join(", ", detalle.Sucursales.Select(s => s.Nombre));
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
            IActionResult? auth = await AuthorizeOrdenCompraAsync(
                context,
                ProductosServiciosAuthorizationDefaults.OrdenesCompraNuevaPermissionCode,
                ProductosServiciosPermissionRequirement.Write);
            if (auth != null)
            {
                return auth;
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
                    List<OrdenCompraSucursalDto> sucursales = await ValidateEncabezadoCatalogosAsync(connection, transaction, context.IdEmpresa, request.IdSucursales, request.IdProveedor);
                    request.IdRazonSocial = sucursales[0].IdRazonSocial;

                    List<OrdenCompraPartidaPersistencia> partidas = await BuildValidatedPartidasAsync(
                        connection,
                        transaction,
                        context.IdEmpresa,
                        request.Partidas,
                        request.IdSucursales);

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
    FolioReferencia,
    idRazonSocial,
    idSucursal,
    idProveedor,
    FechaOrden,
    FechaLlegada,
    FechaMinima,
    FechaMaxima,
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
    @FolioReferencia,
    @IdRazonSocial,
    @IdSucursal,
    @IdProveedor,
    @FechaOrden,
    @FechaLlegada,
    @FechaMinima,
    @FechaMaxima,
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
                        insertCommand.Parameters.AddWithValue("@FolioReferencia", (object?)NormalizeNullableText(request.FolioReferencia, 100) ?? DBNull.Value);
                        insertCommand.Parameters.AddWithValue("@IdRazonSocial", request.IdRazonSocial);
                        insertCommand.Parameters.AddWithValue("@IdSucursal", DBNull.Value);
                        insertCommand.Parameters.AddWithValue("@IdProveedor", request.IdProveedor);
                        insertCommand.Parameters.AddWithValue("@FechaOrden", request.FechaOrden);
                        insertCommand.Parameters.AddWithValue("@FechaLlegada", (object?)request.FechaLlegada ?? DBNull.Value);
                        insertCommand.Parameters.AddWithValue("@FechaMinima", (object?)request.FechaMinima?.Date ?? DBNull.Value);
                        insertCommand.Parameters.AddWithValue("@FechaMaxima", (object?)request.FechaMaxima?.Date ?? DBNull.Value);
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
    FolioReferencia = @FolioReferencia,
    idSucursal = @IdSucursal,
    idProveedor = @IdProveedor,
    FechaOrden = @FechaOrden,
    FechaLlegada = @FechaLlegada,
    FechaMinima = @FechaMinima,
    FechaMaxima = @FechaMaxima,
    Observaciones = @Observaciones,
    Subtotal = @Subtotal,
    Total = @Total,
    FechaActualizacion = @FechaActualizacion,
    idUsuarioActualizacion = @IdUsuarioActualizacion
WHERE idEmpresa = @IdEmpresa
  AND id = @IdOrdenCompra", connection, transaction);

                        updateCommand.Parameters.AddWithValue("@IdRazonSocial", request.IdRazonSocial);
                        updateCommand.Parameters.AddWithValue("@FolioReferencia", (object?)NormalizeNullableText(request.FolioReferencia, 100) ?? DBNull.Value);
                        updateCommand.Parameters.AddWithValue("@IdSucursal", DBNull.Value);
                        updateCommand.Parameters.AddWithValue("@IdProveedor", request.IdProveedor);
                        updateCommand.Parameters.AddWithValue("@FechaOrden", request.FechaOrden);
                        updateCommand.Parameters.AddWithValue("@FechaLlegada", (object?)request.FechaLlegada ?? DBNull.Value);
                        updateCommand.Parameters.AddWithValue("@FechaMinima", (object?)request.FechaMinima?.Date ?? DBNull.Value);
                        updateCommand.Parameters.AddWithValue("@FechaMaxima", (object?)request.FechaMaxima?.Date ?? DBNull.Value);
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

                    await UpsertSucursalesAsync(connection, transaction, context.IdEmpresa, ordenActual.Id, request.IdSucursales, usuarioId, utcNow);
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

        [HttpPost("PrepararCierreOrdenCompra")]
        public async Task<IActionResult> PrepararCierreOrdenCompra([FromBody] OrdenCompraPrepararCierreRequest request, Guid idEmpresa)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error)) return error!;
            IActionResult? auth = await AuthorizeOrdenCompraAsync(context, ProductosServiciosAuthorizationDefaults.OrdenesCompraNuevaPermissionCode, ProductosServiciosPermissionRequirement.Write);
            if (auth != null) return auth;

            string validation = ValidateGuardarRequest(request, context.IdEmpresa);
            if (!string.IsNullOrWhiteSpace(validation)) return BadRequest(new OrdenCompraOperacionResponse { Mensaje = validation });
            string idempotencyKey = NormalizeNullableText(request.IdempotencyKey, 120) ?? string.Empty;
            if (idempotencyKey.Length < 16) return BadRequest(new OrdenCompraOperacionResponse { Mensaje = "La clave idempotente del cierre no es válida." });

            try
            {
                string proposalJson = JsonSerializer.Serialize((OrdenCompraGuardarRequest)request, CierreJsonOptions);
                string payloadHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(proposalJson))).ToLowerInvariant();
                Guid? usuarioId = TryResolveUsuarioId();
                DateTime now = DateTime.UtcNow;
                using SqlConnection connection = CreateConnection();
                await connection.OpenAsync();
                using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable);
                try
                {
                    Guid existingOperationId = Guid.Empty;
                    byte existingState = 0;
                    DateTime existingExpiration = DateTime.MinValue;
                    string existingHash = string.Empty;
                    string existingJson = string.Empty;
                    using (SqlCommand existing = new(@"
SELECT TOP (1) id,Token,PayloadHash,ValidacionJson,ResultadoJson,Estado,FechaExpiracion
FROM dbo.OrdenesCompraOperacionesCierre WITH (UPDLOCK,HOLDLOCK)
WHERE idEmpresa=@IdEmpresa AND IdempotencyKey=@IdempotencyKey", connection, transaction))
                    {
                        existing.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);
                        existing.Parameters.AddWithValue("@IdempotencyKey", idempotencyKey);
                        using SqlDataReader reader = await existing.ExecuteReaderAsync();
                        if (await reader.ReadAsync())
                        {
                            existingOperationId = ReadGuid(reader, "id");
                            existingState = ReadByte(reader, "Estado");
                            existingExpiration = ReadDateTime(reader, "FechaExpiracion");
                            existingHash = ReadString(reader, "PayloadHash");
                            existingJson = existingState == 2 ? ReadString(reader, "ResultadoJson") : ReadString(reader, "ValidacionJson");
                        }
                    }

                    if (existingOperationId != Guid.Empty)
                    {
                        if (!string.Equals(existingHash, payloadHash, StringComparison.Ordinal))
                        {
                            transaction.Rollback();
                            return Conflict(new OrdenCompraOperacionResponse { Mensaje = "La clave idempotente ya pertenece a otra captura." });
                        }
                        if (existingState == 2 || (existingState == 1 && existingExpiration > now))
                        {
                            transaction.Commit();
                            return Content(existingJson, "application/json");
                        }
                        if (existingState == 1)
                        {
                            using SqlCommand expire = new(@"
UPDATE dbo.OrdenesCompraOperacionesCierre
SET Estado=3,FechaConsumo=@Now,FechaActualizacion=@Now,
    IdempotencyKey=CONCAT(N'expired:',CONVERT(nvarchar(36),id))
WHERE idEmpresa=@IdEmpresa AND id=@Id AND Estado=1", connection, transaction);
                            expire.Parameters.AddWithValue("@Now", now);
                            expire.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);
                            expire.Parameters.AddWithValue("@Id", existingOperationId);
                            if (await expire.ExecuteNonQueryAsync() != 1) throw new InvalidOperationException("OC_CIERRE_EXPIRED_TOKEN_NOT_RELEASED");
                        }
                        else
                        {
                            transaction.Rollback();
                            return Conflict(new OrdenCompraOperacionResponse { Mensaje = "La operación de cierre ya no está disponible. Inicia una nueva confirmación." });
                        }
                    }

                    List<OrdenCompraSucursalDto> branches = await ValidateEncabezadoCatalogosAsync(connection, transaction, context.IdEmpresa, request.IdSucursales, request.IdProveedor);
                    request.IdRazonSocial = branches[0].IdRazonSocial;
                    List<OrdenCompraPartidaPersistencia> proposal = await BuildValidatedPartidasAsync(connection, transaction, context.IdEmpresa, request.Partidas, request.IdSucursales);
                    if (proposal.Any(x => x.CostoUnitario <= 0m) || CalculateTotals(proposal).Total <= 0m)
                    {
                        transaction.Rollback();
                        return BadRequest(new OrdenCompraOperacionResponse { Mensaje = "Todas las partidas deben tener costo válido y la orden un total mayor a cero." });
                    }

                    List<OrdenCompraConflictoDto> conflicts = await FindClosureConflictsAsync(connection, transaction, context.IdEmpresa, request.IdProveedor, proposal);
                    List<OrdenCompraPartidaPersistencia> priorRows = conflicts.Count == 0
                        ? new List<OrdenCompraPartidaPersistencia>()
                        : await LoadPriorOrderRowsAsync(connection, transaction, context.IdEmpresa, conflicts.Select(x => x.IdOrdenCompra).ToHashSet());
                    Guid token = Guid.NewGuid();
                    OrdenCompraPrepararCierreResponse response = new()
                    {
                        Exito = true,
                        Token = token,
                        FechaExpiracion = now.Add(CierreTokenLifetime),
                        TieneConflictos = conflicts.Count > 0,
                        PuedeConsolidar = conflicts.Count > 0 && conflicts.All(x => x.Consolidable),
                        Mensaje = conflicts.Count > 0 ? "Ya existen pedidos pendientes." : "La orden está lista para confirmar.",
                        Conflictos = conflicts,
                        VistaPrevia = BuildClosurePreview(proposal, priorRows, branches)
                    };
                    string validationJson = JsonSerializer.Serialize(response, CierreJsonOptions);
                    using SqlCommand insert = new(@"
INSERT INTO dbo.OrdenesCompraOperacionesCierre
(id,idEmpresa,Token,IdempotencyKey,PayloadHash,PropuestaJson,ValidacionJson,Estado,FechaExpiracion,idUsuario,FechaCreacion,FechaActualizacion)
VALUES(@Id,@IdEmpresa,@Token,@IdempotencyKey,@PayloadHash,@PropuestaJson,@ValidacionJson,1,@FechaExpiracion,@IdUsuario,@Now,@Now)", connection, transaction);
                    insert.Parameters.AddWithValue("@Id", Guid.NewGuid());
                    insert.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);
                    insert.Parameters.AddWithValue("@Token", token);
                    insert.Parameters.AddWithValue("@IdempotencyKey", idempotencyKey);
                    insert.Parameters.AddWithValue("@PayloadHash", payloadHash);
                    insert.Parameters.AddWithValue("@PropuestaJson", proposalJson);
                    insert.Parameters.AddWithValue("@ValidacionJson", validationJson);
                    insert.Parameters.AddWithValue("@FechaExpiracion", response.FechaExpiracion);
                    insert.Parameters.AddWithValue("@IdUsuario", (object?)usuarioId ?? DBNull.Value);
                    insert.Parameters.AddWithValue("@Now", now);
                    await insert.ExecuteNonQueryAsync();
                    transaction.Commit();
                    return Ok(response);
                }
                catch { if (transaction.Connection != null) transaction.Rollback(); throw; }
            }
            catch (CatalogoValidationException ex) { return BadRequest(new OrdenCompraOperacionResponse { Mensaje = ex.PublicMessage }); }
            catch (Exception ex) { return HandleException(ex, "PrepararCierreOrdenCompra", "No fue posible preparar el cierre de la orden."); }
        }

        [HttpPost("EjecutarCierreOrdenCompra")]
        public async Task<IActionResult> EjecutarCierreOrdenCompra([FromBody] OrdenCompraEjecutarCierreRequest request, Guid idEmpresa)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error)) return error!;
            IActionResult? auth = await AuthorizeOrdenCompraAsync(context, ProductosServiciosAuthorizationDefaults.OrdenesCompraNuevaPermissionCode, ProductosServiciosPermissionRequirement.Write);
            if (auth != null) return auth;
            if (request == null || request.Token == Guid.Empty || string.IsNullOrWhiteSpace(request.IdempotencyKey)) return BadRequest(new OrdenCompraOperacionResponse { Mensaje = "La confirmación del cierre no es válida." });
            string decision = (request.Decision ?? string.Empty).Trim().ToUpperInvariant();
            if (decision is not "SEPARATE" and not "CONSOLIDATE") return BadRequest(new OrdenCompraOperacionResponse { Mensaje = "Selecciona cómo continuar con la orden." });

            try
            {
                Guid? usuarioId = TryResolveUsuarioId();
                if (!usuarioId.HasValue)
                {
                    return Unauthorized(new OrdenCompraOperacionResponse
                    {
                        Mensaje = "No fue posible identificar al usuario que confirma la orden de compra."
                    });
                }
                DateTime now = DateTime.UtcNow;
                using SqlConnection connection = CreateConnection();
                await connection.OpenAsync();
                using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable);
                try
                {
                    ClosureOperation operation = await LoadClosureOperationAsync(connection, transaction, context.IdEmpresa, request.Token, request.IdempotencyKey);
                    if (operation.Id == Guid.Empty) { transaction.Rollback(); return NotFound(new OrdenCompraOperacionResponse { Mensaje = "La validación de cierre no está disponible." }); }
                    if (operation.Estado == 2 && !string.IsNullOrWhiteSpace(operation.ResultadoJson)) { transaction.Commit(); return Content(operation.ResultadoJson, "application/json"); }
                    if (operation.Estado != 1 || operation.FechaExpiracion <= now) { transaction.Rollback(); return Conflict(new OrdenCompraOperacionResponse { Mensaje = "La validación expiró. Vuelve a guardar para revalidar los pendientes." }); }

                    OrdenCompraGuardarRequest proposalRequest = JsonSerializer.Deserialize<OrdenCompraGuardarRequest>(operation.PropuestaJson, CierreJsonOptions)
                        ?? throw new InvalidOperationException("OC_CIERRE_PROPOSAL_INVALID");
                    List<OrdenCompraSucursalDto> branches = await ValidateEncabezadoCatalogosAsync(connection, transaction, context.IdEmpresa, proposalRequest.IdSucursales, proposalRequest.IdProveedor);
                    proposalRequest.IdRazonSocial = branches[0].IdRazonSocial;
                    List<OrdenCompraPartidaPersistencia> proposal = await BuildValidatedPartidasAsync(connection, transaction, context.IdEmpresa, proposalRequest.Partidas, proposalRequest.IdSucursales);
                    List<OrdenCompraConflictoDto> conflicts = await FindClosureConflictsAsync(connection, transaction, context.IdEmpresa, proposalRequest.IdProveedor, proposal);
                    HashSet<Guid> priorOrders = conflicts.Select(x => x.IdOrdenCompra).ToHashSet();
                    OrdenCompraPrepararCierreResponse initialValidation = JsonSerializer.Deserialize<OrdenCompraPrepararCierreResponse>(operation.ValidacionJson,CierreJsonOptions) ?? new();
                    HashSet<string> initialConflictKeys = initialValidation.Conflictos.Select(ClosureConflictIdentity).ToHashSet(StringComparer.Ordinal);
                    HashSet<string> currentConflictKeys = conflicts.Select(ClosureConflictIdentity).ToHashSet(StringComparer.Ordinal);
                    if (decision == "CONSOLIDATE" && !initialConflictKeys.SetEquals(currentConflictKeys))
                    {
                        transaction.Rollback();
                        return Conflict(new OrdenCompraOperacionResponse { Mensaje = "Los pendientes cambiaron desde la vista previa. Vuelve a validar antes de consolidar." });
                    }
                    if (decision == "CONSOLIDATE" && (priorOrders.Count == 0 || conflicts.Any(x => !x.Consolidable)))
                    {
                        transaction.Rollback();
                        return Conflict(new OrdenCompraOperacionResponse { Mensaje = "Los pendientes cambiaron o incluyen recepción; revalida antes de consolidar." });
                    }

                    if (decision == "CONSOLIDATE")
                    {
                        List<OrdenCompraPartidaPersistencia> inherited = await LoadPriorOrderRowsAsync(connection, transaction, context.IdEmpresa, priorOrders);
                        proposal = MergeClosureRows(proposal, inherited);
                        proposalRequest.IdSucursales = proposal.Select(x => x.IdSucursal).Distinct().ToList();
                    }

                    OrdenCompraCierreResultadoDto result = await InsertGeneratedOrderAsync(connection, transaction, context.IdEmpresa, proposalRequest, proposal, usuarioId, now);
                    if (decision == "CONSOLIDATE")
                    {
                        await ReplacePriorOrdersAsync(connection, transaction, context.IdEmpresa, operation.Id, result.IdOrdenCompra, priorOrders, usuarioId, now);
                        result.Consolidada = true;
                        result.OrdenesReemplazadas = priorOrders.Count;
                    }

                    string resultJson = JsonSerializer.Serialize(result, CierreJsonOptions);
                    using SqlCommand complete = new(@"
UPDATE dbo.OrdenesCompraOperacionesCierre
SET Estado=2,FechaConsumo=@Now,idOrdenCompraResultado=@IdOrdenCompra,ResultadoJson=@ResultadoJson,FechaActualizacion=@Now
WHERE idEmpresa=@IdEmpresa AND id=@Id AND Estado=1", connection, transaction);
                    complete.Parameters.AddWithValue("@Now", now);
                    complete.Parameters.AddWithValue("@IdOrdenCompra", result.IdOrdenCompra);
                    complete.Parameters.AddWithValue("@ResultadoJson", resultJson);
                    complete.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);
                    complete.Parameters.AddWithValue("@Id", operation.Id);
                    if (await complete.ExecuteNonQueryAsync() != 1) throw new InvalidOperationException("OC_CIERRE_TOKEN_NOT_CONSUMED");
                    transaction.Commit();
                    return Ok(result);
                }
                catch { if (transaction.Connection != null) transaction.Rollback(); throw; }
            }
            catch (CatalogoValidationException ex) { return BadRequest(new OrdenCompraOperacionResponse { Mensaje = ex.PublicMessage }); }
            catch (Exception ex) { return HandleException(ex, "EjecutarCierreOrdenCompra", "No fue posible cerrar la orden de compra."); }
        }

        [HttpPost("GenerarOrdenCompra")]
        public async Task<IActionResult> GenerarOrdenCompra([FromBody] OrdenCompraGenerarRequest request, Guid idEmpresa)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }
            IActionResult? auth = await AuthorizeOrdenCompraAsync(
                context,
                ProductosServiciosAuthorizationDefaults.OrdenesCompraNuevaPermissionCode,
                ProductosServiciosPermissionRequirement.Write);
            if (auth != null)
            {
                return auth;
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
            IActionResult? auth = await AuthorizeOrdenCompraAsync(
                context,
                ProductosServiciosAuthorizationDefaults.OrdenesCompraReportePermissionCode,
                ProductosServiciosPermissionRequirement.Write);
            if (auth != null)
            {
                return auth;
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
            IActionResult? auth = await AuthorizeOrdenCompraAsync(
                context,
                ProductosServiciosAuthorizationDefaults.OrdenesCompraReportePermissionCode,
                ProductosServiciosPermissionRequirement.Read);
            if (auth != null)
            {
                return auth;
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
            IActionResult? auth = await AuthorizeOrdenCompraAsync(
                context,
                ProductosServiciosAuthorizationDefaults.OrdenesCompraNuevaPermissionCode,
                ProductosServiciosPermissionRequirement.Read);
            if (auth != null)
            {
                return auth;
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
                        new OrdenCompraEstadoOpcionDto { Id = EstadoCancelada, Nombre = GetEstadoNombre(EstadoCancelada) },
                        new OrdenCompraEstadoOpcionDto { Id = EstadoParcialmenteRecibida, Nombre = GetEstadoNombre(EstadoParcialmenteRecibida) },
                        new OrdenCompraEstadoOpcionDto { Id = EstadoRecibida, Nombre = GetEstadoNombre(EstadoRecibida) }
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
        public async Task<IActionResult> BuscarProductosServiciosOrdenCompra(Guid idEmpresa, string texto = "", byte? tipo = null, Guid? idProveedor = null, int limite = 25)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }
            IActionResult? auth = await AuthorizeOrdenCompraAsync(
                context,
                ProductosServiciosAuthorizationDefaults.OrdenesCompraNuevaPermissionCode,
                ProductosServiciosPermissionRequirement.Read);
            if (auth != null)
            {
                return auth;
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
    ISNULL(cat.Nombre, '') AS Categoria,
    ISNULL(m.Nombre, '') AS Marca,
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
LEFT JOIN dbo.ProductosServiciosCategorias cat
    ON cat.idEmpresa = ps.idEmpresa AND cat.id = ps.idCategoria AND cat.Activo = 1
LEFT JOIN dbo.ProductosServiciosMarcas m
    ON m.idEmpresa = ps.idEmpresa AND m.id = ps.idMarca AND m.Activo = 1
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
                if (idProveedor.HasValue && idProveedor.Value != Guid.Empty)
                {
                    query.Append(" AND ps.idProveedorPrincipal = @IdProveedor");
                    command.Parameters.AddWithValue("@IdProveedor", idProveedor.Value);
                }
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
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
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
                            Categoria = ReadString(reader, "Categoria"),
                            Marca = ReadString(reader, "Marca"),
                            IdUnidadMedida = ReadGuid(reader, "idUnidadMedida"),
                            Unidad = ReadString(reader, "Unidad"),
                            Abreviatura = ReadString(reader, "Abreviatura"),
                            CostoActual = ReadNullableDecimal(reader, "Costo"),
                            CausaInventario = ReadBool(reader, "CausaInventario")
                        });
                    }
                }

                await HydrateBusquedaV1Async(connection, context.IdEmpresa, items);

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
            IActionResult? auth = await AuthorizeOrdenCompraAsync(
                context,
                ProductosServiciosAuthorizationDefaults.OrdenesCompraNuevaPermissionCode,
                ProductosServiciosPermissionRequirement.Read);
            if (auth != null)
            {
                return auth;
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
                    CantidadOrdenada = item.CantidadOrdenada,
                    CantidadRecibida = item.CantidadRecibida,
                    CantidadPendiente = item.CantidadPendiente,
                    Total = item.Total,
                    FechaCreacion = item.FechaCreacion
                })
                .ToList();

            byte[] excel = BuildListadoExcelDocument(exportRows);
            string fileName = $"ordenes_compra_reporte_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
            return File(excel, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet("ExportarOrdenCompraExcel")]
        public async Task<IActionResult> ExportarOrdenCompraExcel(Guid idEmpresa, Guid idOrdenCompra)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }
            IActionResult? auth = await AuthorizeOrdenCompraAsync(
                context,
                ProductosServiciosAuthorizationDefaults.OrdenesCompraReportePermissionCode,
                ProductosServiciosPermissionRequirement.Read);
            if (auth != null)
            {
                return auth;
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
    ISNULL(dest.Sucursales, ISNULL(su.Nombre, '')) AS Sucursal,
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
OUTER APPLY (
    SELECT STRING_AGG(CONVERT(nvarchar(max), s2.Nombre), ', ') WITHIN GROUP (ORDER BY s2.Nombre) AS Sucursales
    FROM dbo.OrdenesCompraSucursales os
    INNER JOIN dbo.Sucursales s2 ON s2.idEmpresa=os.idEmpresa AND s2.id=os.idSucursal
    WHERE os.idEmpresa=oc.idEmpresa AND os.idOrdenCompra=oc.id AND os.Activo=1 AND os.FechaArchivado IS NULL
) dest
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
                    Sucursal = partida.Sucursal,
                    Tipo = partida.TipoProductoServicioNombre,
                    Codigo = partida.Codigo,
                    Nombre = partida.Nombre,
                    Descripcion = partida.Descripcion,
                    Categoria = partida.Categoria,
                    Marca = partida.Marca,
                    Variante = partida.VarianteSnapshot,
                    PresentacionCompra = partida.PresentacionCompraSnapshot,
                    Unidad = string.IsNullOrWhiteSpace(partida.UnidadAbreviatura)
                        ? partida.UnidadMedida
                        : $"{partida.UnidadMedida} ({partida.UnidadAbreviatura})",
                    Cantidad = partida.Cantidad,
                    CantidadOrdenada = partida.CantidadBaseOrdenada,
                    CantidadRecibida = partida.CantidadBaseRecibidaAcumulada,
                    CantidadPendiente = partida.CantidadBasePendiente,
                    EstadoPartida = partida.EstadoPartidaNombre,
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
                        BuildColumn(9, 9, 18D),
                        BuildColumn(10, 10, 18D),
                        BuildColumn(11, 11, 22D),
                        BuildColumn(12, 15, 16D),
                        BuildColumn(16, 17, 14D),
                        BuildColumn(18, 18, 22D)),
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
                    BuildTextCell("F", rowIndex, "Categoría", 1U),
                    BuildTextCell("G", rowIndex, "Marca", 1U),
                    BuildTextCell("H", rowIndex, "Variante", 1U),
                    BuildTextCell("I", rowIndex, "Presentación compra", 1U),
                    BuildTextCell("J", rowIndex, "Unidad", 1U),
                    BuildTextCell("K", rowIndex, "Ordenado", 1U),
                    BuildTextCell("L", rowIndex, "Recibido", 1U),
                    BuildTextCell("M", rowIndex, "Pendiente", 1U),
                    BuildTextCell("N", rowIndex, "Estado partida", 1U),
                    BuildTextCell("O", rowIndex, "Cantidad compra", 1U),
                    BuildTextCell("P", rowIndex, "Costo", 1U),
                    BuildTextCell("Q", rowIndex, "Subtotal", 1U),
                    BuildTextCell("R", rowIndex, "Sucursal", 1U)));
                rowIndex++;

                foreach (OrdenCompraDocumentoPartidaDto partida in documento.Partidas)
                {
                    sheetData.Append(new Row(
                        BuildNumberCell("A", rowIndex, partida.NumeroPartida, 0U),
                        BuildTextCell("B", rowIndex, partida.Tipo, 0U),
                        BuildTextCell("C", rowIndex, partida.Codigo, 0U),
                        BuildTextCell("D", rowIndex, partida.Nombre, 0U),
                        BuildTextCell("E", rowIndex, partida.Descripcion, 0U),
                        BuildTextCell("F", rowIndex, partida.Categoria, 0U),
                        BuildTextCell("G", rowIndex, partida.Marca, 0U),
                        BuildTextCell("H", rowIndex, partida.Variante, 0U),
                        BuildTextCell("I", rowIndex, partida.PresentacionCompra, 0U),
                        BuildTextCell("J", rowIndex, partida.Unidad, 0U),
                        BuildNumberCell("K", rowIndex, partida.CantidadOrdenada, 2U),
                        BuildNumberCell("L", rowIndex, partida.CantidadRecibida, 2U),
                        BuildNumberCell("M", rowIndex, partida.CantidadPendiente, 2U),
                        BuildTextCell("N", rowIndex, partida.EstadoPartida, 0U),
                        BuildNumberCell("O", rowIndex, partida.Cantidad, 2U),
                        BuildNumberCell("P", rowIndex, partida.CostoUnitario, 2U),
                        BuildNumberCell("Q", rowIndex, partida.Subtotal, 2U),
                        BuildTextCell("R", rowIndex, partida.Sucursal, 0U)));
                    rowIndex++;
                }

                sheetData.Append(
                    new Row { RowIndex = rowIndex++ },
                    new Row(
                        BuildTextCell("O", rowIndex, "Subtotal", 1U),
                        BuildNumberCell("Q", rowIndex, documento.Subtotal, 2U)),
                    new Row(
                        BuildTextCell("O", ++rowIndex, "Total", 1U),
                        BuildNumberCell("Q", rowIndex, documento.Total, 2U)));

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
                        BuildColumn(9, 12, 18D)),
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
                    BuildTextCell("H", rowIndex, "Ordenado", 1U),
                    BuildTextCell("I", rowIndex, "Recibido", 1U),
                    BuildTextCell("J", rowIndex, "Pendiente", 1U),
                    BuildTextCell("K", rowIndex, "Total", 1U),
                    BuildTextCell("L", rowIndex, "Fecha de creación", 1U)));
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
                        BuildNumberCell("H", rowIndex, item.CantidadOrdenada, 2U),
                        BuildNumberCell("I", rowIndex, item.CantidadRecibida, 2U),
                        BuildNumberCell("J", rowIndex, item.CantidadPendiente, 2U),
                        BuildNumberCell("K", rowIndex, item.Total, 2U),
                        BuildTextCell("L", rowIndex, item.FechaCreacion.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture), 0U)));
                    rowIndex++;
                }

                workbookPart.Workbook.Save();
            }

            return stream.ToArray();
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

        private async Task<List<OrdenCompraSucursalDto>> ValidateEncabezadoCatalogosAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, List<Guid> idSucursales, Guid idProveedor)
        {
            List<Guid> ids = (idSucursales ?? new List<Guid>()).Where(id => id != Guid.Empty).Distinct().ToList();
            if (ids.Count == 0) throw new CatalogoValidationException("Selecciona al menos una sucursal destino.");

            List<OrdenCompraSucursalDto> sucursales = new List<OrdenCompraSucursalDto>();
            foreach (Guid idSucursal in ids)
            {
                using SqlCommand branchCommand = new SqlCommand(@"
SELECT TOP (1) s.id, s.idRazonSocial, CAST('' AS nvarchar(50)) AS Codigo, ISNULL(s.Nombre, '') AS Nombre
FROM dbo.Sucursales s
INNER JOIN dbo.RazonesSociales rs ON rs.idEmpresa=s.idEmpresa AND rs.id=s.idRazonSocial AND ISNULL(rs.borrado,0)=0
WHERE s.idEmpresa=@IdEmpresa AND s.id=@IdSucursal AND ISNULL(s.borrado,0)=0", connection, transaction);
                branchCommand.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                branchCommand.Parameters.AddWithValue("@IdSucursal", idSucursal);
                using SqlDataReader reader = await branchCommand.ExecuteReaderAsync();
                if (!await reader.ReadAsync()) throw new CatalogoValidationException("Una sucursal destino no está disponible.");
                sucursales.Add(new OrdenCompraSucursalDto { Id=ReadGuid(reader,"id"), IdRazonSocial=ReadGuid(reader,"idRazonSocial"), Codigo=ReadString(reader,"Codigo"), Nombre=ReadString(reader,"Nombre") });
            }

            if (!await ExistsAsync(connection, transaction,
                "SELECT COUNT(1) FROM dbo.ActivosProveedores WHERE idEmpresa = @IdEmpresa AND id = @Id AND Activo = 1",
                idEmpresa, idProveedor))
            {
                throw new CatalogoValidationException("El proveedor no está disponible.");
            }
            return sucursales;
        }

        private async Task<ClosureOperation> LoadClosureOperationAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid token, string idempotencyKey)
        {
            using SqlCommand command = new(@"
SELECT TOP (1) id,Estado,FechaExpiracion,PropuestaJson,ValidacionJson,ResultadoJson
FROM dbo.OrdenesCompraOperacionesCierre WITH (UPDLOCK,HOLDLOCK)
WHERE idEmpresa=@IdEmpresa AND Token=@Token AND IdempotencyKey=@IdempotencyKey", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@Token", token);
            command.Parameters.AddWithValue("@IdempotencyKey", idempotencyKey.Trim());
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return new ClosureOperation();
            return new ClosureOperation
            {
                Id = ReadGuid(reader,"id"), Estado = ReadByte(reader,"Estado"), FechaExpiracion = ReadDateTime(reader,"FechaExpiracion"),
                PropuestaJson = ReadString(reader,"PropuestaJson"), ValidacionJson=ReadString(reader,"ValidacionJson"), ResultadoJson = ReadString(reader,"ResultadoJson")
            };
        }

        private async Task<List<OrdenCompraConflictoDto>> FindClosureConflictsAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idProveedor, IEnumerable<OrdenCompraPartidaPersistencia> proposal)
        {
            List<OrdenCompraConflictoDto> result = new();
            foreach (OrdenCompraPartidaPersistencia row in proposal)
            {
                using SqlCommand command = new(@"
SELECT oc.id,ISNULL(oc.Folio,'') Folio,oc.Estado,ISNULL(s.Nombre,'') Sucursal,
       CONCAT(d.Codigo,N' · ',d.Nombre) Concepto,ISNULL(d.VarianteSnapshot,'') Variante,
       ISNULL(d.PresentacionCompraSnapshot,'') PresentacionCompra,d.CantidadCompra,
       CASE WHEN oc.Estado=2
              AND NOT EXISTS (SELECT 1 FROM dbo.OrdenesCompraDetalle dx WHERE dx.idEmpresa=oc.idEmpresa AND dx.idOrdenCompra=oc.id AND dx.Activo=1 AND dx.FechaArchivado IS NULL AND dx.CantidadBaseRecibidaAcumulada<>0)
              AND NOT EXISTS (SELECT 1 FROM dbo.RecepcionPartidas rp WHERE rp.idEmpresa=oc.idEmpresa AND rp.idOrdenCompra=oc.id)
            THEN CONVERT(bit,1) ELSE CONVERT(bit,0) END Consolidable
FROM dbo.OrdenesCompra oc WITH (UPDLOCK,HOLDLOCK)
JOIN dbo.OrdenesCompraDetalle d WITH (UPDLOCK,HOLDLOCK) ON d.idEmpresa=oc.idEmpresa AND d.idOrdenCompra=oc.id AND d.Activo=1 AND d.FechaArchivado IS NULL
JOIN dbo.Sucursales s ON s.idEmpresa=d.idEmpresa AND s.id=d.idSucursal
WHERE oc.idEmpresa=@IdEmpresa AND oc.idProveedor=@IdProveedor AND oc.Estado IN (2,4)
  AND oc.Activo=1 AND oc.FechaArchivado IS NULL AND d.CantidadBasePendiente>0
  AND d.idSucursal=@IdSucursal AND d.idProductoServicio=@IdProductoServicio
  AND ((d.idVariante=@IdVariante) OR (d.idVariante IS NULL AND @IdVariante IS NULL))
  AND ((d.idPresentacionCompra=@IdPresentacion) OR (d.idPresentacionCompra IS NULL AND @IdPresentacion IS NULL))
ORDER BY oc.FechaOrden,oc.id,d.NumeroPartida", connection, transaction);
                command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                command.Parameters.AddWithValue("@IdProveedor", idProveedor);
                command.Parameters.AddWithValue("@IdSucursal", row.IdSucursal);
                command.Parameters.AddWithValue("@IdProductoServicio", row.IdProductoServicio);
                command.Parameters.AddWithValue("@IdVariante", (object?)row.IdVariante ?? DBNull.Value);
                command.Parameters.AddWithValue("@IdPresentacion", (object?)row.IdPresentacionCompra ?? DBNull.Value);
                using SqlDataReader reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    Guid orderId = ReadGuid(reader,"id");
                    if (result.Any(x => x.IdOrdenCompra == orderId && x.Sucursal == ReadString(reader,"Sucursal") && x.Concepto == ReadString(reader,"Concepto") && x.Variante == ReadString(reader,"Variante") && x.PresentacionCompra == ReadString(reader,"PresentacionCompra"))) continue;
                    result.Add(new OrdenCompraConflictoDto
                    {
                        IdOrdenCompra=orderId,Folio=ReadString(reader,"Folio"),Estado=ReadByte(reader,"Estado"),Consolidable=ReadBool(reader,"Consolidable"),
                        Sucursal=ReadString(reader,"Sucursal"),Concepto=ReadString(reader,"Concepto"),Variante=ReadString(reader,"Variante"),
                        PresentacionCompra=ReadString(reader,"PresentacionCompra"),CantidadAnterior=ReadDecimal(reader,"CantidadCompra"),CantidadPropuesta=row.CantidadCompra
                    });
                }
            }
            return result;
        }

        private async Task<List<OrdenCompraPartidaPersistencia>> LoadPriorOrderRowsAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, HashSet<Guid> orderIds)
        {
            if (orderIds.Count == 0) return new();
            List<Guid> ids = orderIds.OrderBy(x => x).ToList();
            string parameters = string.Join(",", ids.Select((_,i)=>"@Order"+i));
            using SqlCommand command = new($@"
SELECT d.idSucursal,d.NumeroPartida,d.idProductoServicio,d.TipoProductoServicio,d.idVariante,d.idPresentacionCompra,d.Codigo,d.Nombre,
 ISNULL(d.Descripcion,'') Descripcion,ISNULL(d.VarianteSnapshot,'') VarianteSnapshot,ISNULL(d.PresentacionCompraSnapshot,'') PresentacionCompraSnapshot,
 d.idUnidadMedida,d.UnidadMedida,d.UnidadAbreviatura,ISNULL(d.UnidadCompraSnapshot,'') UnidadCompraSnapshot,
 ISNULL(d.UnidadCompraAbreviaturaSnapshot,'') UnidadCompraAbreviaturaSnapshot,d.Cantidad,d.CantidadCompra,d.FactorConversionSnapshot,
 d.CantidadBaseOrdenada,d.CostoUnitario,d.Subtotal,d.Total
FROM dbo.OrdenesCompraDetalle d WITH (UPDLOCK,HOLDLOCK)
WHERE d.idEmpresa=@IdEmpresa AND d.idOrdenCompra IN ({parameters}) AND d.Activo=1 AND d.FechaArchivado IS NULL
ORDER BY d.idOrdenCompra,d.NumeroPartida", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa",idEmpresa);
            AddGuidInParameters(command,ids,"@Order");
            List<OrdenCompraPartidaPersistencia> rows = new();
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync()) rows.Add(new OrdenCompraPartidaPersistencia
            {
                Id=Guid.NewGuid(),IdSucursal=ReadGuid(reader,"idSucursal"),NumeroPartida=ReadInt(reader,"NumeroPartida"),IdProductoServicio=ReadGuid(reader,"idProductoServicio"),
                TipoProductoServicio=ReadByte(reader,"TipoProductoServicio"),IdVariante=ReadNullableGuid(reader,"idVariante"),IdPresentacionCompra=ReadNullableGuid(reader,"idPresentacionCompra"),
                Codigo=ReadString(reader,"Codigo"),Nombre=ReadString(reader,"Nombre"),Descripcion=ReadString(reader,"Descripcion"),VarianteSnapshot=ReadString(reader,"VarianteSnapshot"),
                PresentacionCompraSnapshot=ReadString(reader,"PresentacionCompraSnapshot"),IdUnidadMedida=ReadGuid(reader,"idUnidadMedida"),UnidadMedida=ReadString(reader,"UnidadMedida"),
                UnidadAbreviatura=ReadString(reader,"UnidadAbreviatura"),UnidadCompraSnapshot=ReadString(reader,"UnidadCompraSnapshot"),UnidadCompraAbreviaturaSnapshot=ReadString(reader,"UnidadCompraAbreviaturaSnapshot"),
                Cantidad=ReadDecimal(reader,"Cantidad"),CantidadCompra=ReadDecimal(reader,"CantidadCompra"),FactorConversionSnapshot=ReadDecimal(reader,"FactorConversionSnapshot"),
                CantidadBaseOrdenada=ReadDecimal(reader,"CantidadBaseOrdenada"),CantidadBasePendiente=ReadDecimal(reader,"CantidadBaseOrdenada"),EstadoPartida=1,
                CostoUnitario=ReadDecimal(reader,"CostoUnitario"),Subtotal=ReadDecimal(reader,"Subtotal"),Total=ReadDecimal(reader,"Total")
            });
            return rows;
        }

        private static List<OrdenCompraPartidaPersistencia> MergeClosureRows(List<OrdenCompraPartidaPersistencia> proposal, List<OrdenCompraPartidaPersistencia> inherited)
        {
            Dictionary<string,OrdenCompraPartidaPersistencia> merged = proposal.ToDictionary(ClosureIdentity, CloneClosureRow);
            HashSet<string> proposalKeys = merged.Keys.ToHashSet(StringComparer.Ordinal);
            foreach (OrdenCompraPartidaPersistencia old in inherited)
            {
                string key = ClosureIdentity(old);
                if (proposalKeys.Contains(key)) continue; // La propuesta reemplaza, nunca suma.
                if (merged.TryGetValue(key,out OrdenCompraPartidaPersistencia? existing))
                {
                    if (existing.FactorConversionSnapshot != old.FactorConversionSnapshot || existing.CostoUnitario != old.CostoUnitario || existing.IdUnidadMedida != old.IdUnidadMedida)
                        throw new CatalogoValidationException("Las órdenes anteriores contienen renglones ambiguos que no se pueden consolidar de forma segura.");
                    existing.Cantidad=NormalizeQuantity(existing.Cantidad+old.Cantidad);
                    existing.CantidadCompra=NormalizeQuantity(existing.CantidadCompra+old.CantidadCompra);
                    existing.CantidadBaseOrdenada=NormalizeQuantity(existing.CantidadBaseOrdenada+old.CantidadBaseOrdenada);
                    existing.Subtotal=NormalizeMoney(existing.Subtotal+old.Subtotal); existing.Total=existing.Subtotal;
                    continue;
                }
                OrdenCompraPartidaPersistencia clone = CloneClosureRow(old);
                merged.Add(key,clone);
            }
            int number=1;
            foreach (OrdenCompraPartidaPersistencia row in merged.Values) { row.Id=Guid.NewGuid(); row.NumeroPartida=number++; row.CantidadBaseRecibidaAcumulada=0; row.CantidadBasePendiente=row.CantidadBaseOrdenada; row.EstadoPartida=1; }
            return merged.Values.ToList();
        }

        private static string ClosureIdentity(OrdenCompraPartidaPersistencia row)
            => string.Join("|",row.IdSucursal.ToString("N"),row.IdProductoServicio.ToString("N"),row.IdVariante?.ToString("N")??"-",row.IdPresentacionCompra?.ToString("N")??"-");

        private static string ClosureConflictIdentity(OrdenCompraConflictoDto row)
            => string.Join("|",row.IdOrdenCompra.ToString("N"),row.Sucursal,row.Concepto,row.Variante,row.PresentacionCompra,row.CantidadAnterior.ToString(CultureInfo.InvariantCulture),row.CantidadPropuesta.ToString(CultureInfo.InvariantCulture),row.Consolidable);

        private static List<OrdenCompraCierreVistaPreviaDto> BuildClosurePreview(List<OrdenCompraPartidaPersistencia> proposal, List<OrdenCompraPartidaPersistencia> inherited, List<OrdenCompraSucursalDto> branches)
        {
            Dictionary<string,List<OrdenCompraPartidaPersistencia>> old = inherited.GroupBy(ClosureIdentity).ToDictionary(x=>x.Key,x=>x.ToList());
            List<OrdenCompraCierreVistaPreviaDto> preview = new();
            foreach(OrdenCompraPartidaPersistencia row in proposal)
            {
                old.TryGetValue(ClosureIdentity(row),out List<OrdenCompraPartidaPersistencia>? prior);
                preview.Add(PreviewRow(prior is { Count: > 0 } ? "Actualizado" : "Agregado",row,prior?.Sum(x=>x.CantidadCompra)??0,row.CantidadCompra,branches));
                old.Remove(ClosureIdentity(row));
            }
            foreach(List<OrdenCompraPartidaPersistencia> group in old.Values)
            {
                OrdenCompraPartidaPersistencia row=group[0]; decimal quantity=group.Sum(x=>x.CantidadCompra);
                preview.Add(PreviewRow("Conservado",row,quantity,quantity,branches));
            }
            return preview;
        }

        private static OrdenCompraCierreVistaPreviaDto PreviewRow(string action, OrdenCompraPartidaPersistencia row, decimal oldQuantity, decimal newQuantity, List<OrdenCompraSucursalDto> branches)
            => new(){Accion=action,Sucursal=branches.FirstOrDefault(x=>x.Id==row.IdSucursal)?.Nombre??row.IdSucursal.ToString(),Concepto=$"{row.Codigo} · {row.Nombre}",Variante=row.VarianteSnapshot,PresentacionCompra=row.PresentacionCompraSnapshot,CantidadAnterior=oldQuantity,CantidadNueva=newQuantity};

        private static OrdenCompraPartidaPersistencia CloneClosureRow(OrdenCompraPartidaPersistencia x) => new()
        {
            Id=x.Id,IdSucursal=x.IdSucursal,NumeroPartida=x.NumeroPartida,IdProductoServicio=x.IdProductoServicio,TipoProductoServicio=x.TipoProductoServicio,
            IdVariante=x.IdVariante,IdPresentacionCompra=x.IdPresentacionCompra,Codigo=x.Codigo,Nombre=x.Nombre,Descripcion=x.Descripcion,VarianteSnapshot=x.VarianteSnapshot,
            PresentacionCompraSnapshot=x.PresentacionCompraSnapshot,IdUnidadMedida=x.IdUnidadMedida,UnidadMedida=x.UnidadMedida,UnidadAbreviatura=x.UnidadAbreviatura,
            UnidadCompraSnapshot=x.UnidadCompraSnapshot,UnidadCompraAbreviaturaSnapshot=x.UnidadCompraAbreviaturaSnapshot,Cantidad=x.Cantidad,CantidadCompra=x.CantidadCompra,
            FactorConversionSnapshot=x.FactorConversionSnapshot,CantidadBaseOrdenada=x.CantidadBaseOrdenada,CantidadBaseRecibidaAcumulada=x.CantidadBaseRecibidaAcumulada,
            CantidadBasePendiente=x.CantidadBasePendiente,EstadoPartida=x.EstadoPartida,CostoUnitario=x.CostoUnitario,Subtotal=x.Subtotal,Total=x.Total
        };

        private async Task<OrdenCompraCierreResultadoDto> InsertGeneratedOrderAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, OrdenCompraGuardarRequest request, List<OrdenCompraPartidaPersistencia> rows, Guid? userId, DateTime now)
        {
            TotalesOrdenCompra totals=CalculateTotals(rows); Guid orderId=Guid.NewGuid(); string folio=await ReserveNextFolioAsync(connection,transaction,idEmpresa,now);
            using SqlCommand insert = new(@"
INSERT INTO dbo.OrdenesCompra(id,idEmpresa,identityKey,Folio,FolioReferencia,idRazonSocial,idSucursal,idProveedor,FechaOrden,FechaLlegada,FechaMinima,FechaMaxima,Estado,Subtotal,Total,Observaciones,Activo,FechaCreacion,FechaActualizacion,idUsuarioCreacion,idUsuarioActualizacion)
VALUES(@Id,@IdEmpresa,NEWID(),@Folio,@FolioReferencia,@IdRazonSocial,NULL,@IdProveedor,@FechaOrden,@FechaLlegada,@FechaMinima,@FechaMaxima,2,@Subtotal,@Total,@Observaciones,1,@Now,@Now,@User,@User)", connection, transaction);
            insert.Parameters.AddWithValue("@Id",orderId);insert.Parameters.AddWithValue("@IdEmpresa",idEmpresa);insert.Parameters.AddWithValue("@Folio",folio);
            insert.Parameters.AddWithValue("@FolioReferencia",(object?)NormalizeNullableText(request.FolioReferencia,100)??DBNull.Value);insert.Parameters.AddWithValue("@IdRazonSocial",request.IdRazonSocial);
            insert.Parameters.AddWithValue("@IdProveedor",request.IdProveedor);insert.Parameters.AddWithValue("@FechaOrden",request.FechaOrden);insert.Parameters.AddWithValue("@FechaLlegada",(object?)request.FechaLlegada??DBNull.Value);
            insert.Parameters.AddWithValue("@FechaMinima",(object?)request.FechaMinima?.Date??DBNull.Value);insert.Parameters.AddWithValue("@FechaMaxima",(object?)request.FechaMaxima?.Date??DBNull.Value);
            insert.Parameters.AddWithValue("@Subtotal",totals.Subtotal);insert.Parameters.AddWithValue("@Total",totals.Total);insert.Parameters.AddWithValue("@Observaciones",(object?)NormalizeNullableText(request.Observaciones,ObservacionesLength)??DBNull.Value);
            insert.Parameters.AddWithValue("@Now",now);insert.Parameters.AddWithValue("@User",(object?)userId??DBNull.Value);await insert.ExecuteNonQueryAsync();
            await UpsertSucursalesAsync(connection,transaction,idEmpresa,orderId,rows.Select(x=>x.IdSucursal).Distinct(),userId,now);
            await InsertPartidasAsync(connection,transaction,idEmpresa,orderId,rows,now);
            return new OrdenCompraCierreResultadoDto{IdOrdenCompra=orderId,Folio=folio,Estado=EstadoGenerada,EstadoNombre=GetEstadoNombre(EstadoGenerada),Renglones=rows.Count,Piezas=rows.Sum(x=>x.CantidadBaseOrdenada),Total=totals.Total};
        }

        private async Task ReplacePriorOrdersAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid operationId, Guid newOrderId, HashSet<Guid> oldOrderIds, Guid? userId, DateTime now)
        {
            List<Guid> ids=oldOrderIds.OrderBy(x=>x).ToList(); string inSql=string.Join(",",ids.Select((_,i)=>"@Old"+i));
            using SqlCommand cancel = new($@"
UPDATE oc SET Estado=3,MotivoCancelacion=N'Reemplazada por consolidación OC-QA15',FechaCancelacion=@Now,idUsuarioCancelacion=@User,idUsuarioActualizacion=@User,FechaActualizacion=@Now
FROM dbo.OrdenesCompra oc
WHERE oc.idEmpresa=@IdEmpresa AND oc.id IN ({inSql}) AND oc.Estado=2
 AND NOT EXISTS(SELECT 1 FROM dbo.OrdenesCompraDetalle d WHERE d.idEmpresa=oc.idEmpresa AND d.idOrdenCompra=oc.id AND d.Activo=1 AND d.FechaArchivado IS NULL AND d.CantidadBaseRecibidaAcumulada<>0)
 AND NOT EXISTS(SELECT 1 FROM dbo.RecepcionPartidas rp WHERE rp.idEmpresa=oc.idEmpresa AND rp.idOrdenCompra=oc.id)",connection,transaction);
            cancel.Parameters.AddWithValue("@Now",now);cancel.Parameters.AddWithValue("@User",(object?)userId??DBNull.Value);cancel.Parameters.AddWithValue("@IdEmpresa",idEmpresa);AddGuidInParameters(cancel,ids,"@Old");
            if(await cancel.ExecuteNonQueryAsync()!=ids.Count) throw new CatalogoValidationException("Una orden pendiente cambió o recibió mercancía; no se realizó ningún reemplazo.");
            foreach(Guid oldId in ids){using SqlCommand trace=new(@"INSERT INTO dbo.OrdenesCompraReemplazos(id,idEmpresa,idOperacionCierre,idOrdenCompraAnterior,idOrdenCompraNueva,Motivo,idUsuario,FechaReemplazo) VALUES(NEWID(),@Empresa,@Operacion,@Anterior,@Nueva,N'Consolidación y reemplazo confirmados por usuario',@Usuario,@Fecha)",connection,transaction);trace.Parameters.AddWithValue("@Empresa",idEmpresa);trace.Parameters.AddWithValue("@Operacion",operationId);trace.Parameters.AddWithValue("@Anterior",oldId);trace.Parameters.AddWithValue("@Nueva",newOrderId);trace.Parameters.AddWithValue("@Usuario",(object?)userId??DBNull.Value);trace.Parameters.AddWithValue("@Fecha",now);await trace.ExecuteNonQueryAsync();}
        }

        private async Task<List<OrdenCompraPartidaPersistencia>> BuildValidatedPartidasAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            Guid idEmpresa,
            List<OrdenCompraPartidaGuardarRequest> requestPartidas,
            List<Guid> idSucursales)
        {
            if (requestPartidas == null || requestPartidas.Count == 0)
            {
                throw new CatalogoValidationException("La orden debe contener al menos una partida.");
            }

            ValidateRequestPartidas(requestPartidas);
            HashSet<Guid> sucursales = (idSucursales ?? new List<Guid>()).ToHashSet();

            List<OrdenCompraPartidaPersistencia> result = new List<OrdenCompraPartidaPersistencia>();
            int numeroPartida = 1;

            foreach (OrdenCompraPartidaGuardarRequest partidaRequest in requestPartidas)
            {
                if (!sucursales.Contains(partidaRequest.IdSucursal))
                {
                    throw new CatalogoValidationException("La sucursal de una partida no pertenece a los destinos seleccionados.");
                }
                ProductoServicioSnapshot snapshot = await ObtenerProductoServicioSnapshotAsync(connection, transaction, idEmpresa, partidaRequest.IdProductoServicio);
                if (snapshot.Id == Guid.Empty)
                {
                    throw new CatalogoValidationException("El producto o servicio no está disponible.");
                }

                VarianteOrdenCompraSnapshot variante = await ResolveVarianteOrdenCompraAsync(connection, transaction, idEmpresa, snapshot, partidaRequest.IdVariante);
                PresentacionCompraSnapshot presentacion = await ResolvePresentacionCompraAsync(connection, transaction, idEmpresa, snapshot, variante.Id, partidaRequest.IdPresentacionCompra);
                decimal costoUnitario = NormalizeMoney(partidaRequest.CostoUnitario);
                decimal cantidadCompra = NormalizeQuantity(partidaRequest.CantidadCompra ?? partidaRequest.Cantidad);
                decimal factorConversion = NormalizeQuantity(presentacion.Id.HasValue ? presentacion.FactorConversionBase : 1m);
                decimal cantidadBase = NormalizeQuantity(cantidadCompra * factorConversion);
                decimal subtotal = NormalizeMoney(cantidadCompra * costoUnitario);

                result.Add(new OrdenCompraPartidaPersistencia
                {
                    Id = Guid.NewGuid(),
                    IdSucursal = partidaRequest.IdSucursal,
                    IdProductoServicio = snapshot.Id,
                    TipoProductoServicio = snapshot.Tipo,
                    IdVariante = variante.Id,
                    IdPresentacionCompra = presentacion.Id,
                    Codigo = snapshot.Codigo,
                    Nombre = snapshot.Nombre,
                    Descripcion = snapshot.Descripcion,
                    VarianteSnapshot = variante.Nombre,
                    PresentacionCompraSnapshot = presentacion.Nombre,
                    IdUnidadMedida = snapshot.IdUnidadMedida,
                    UnidadMedida = snapshot.UnidadMedida,
                    UnidadAbreviatura = snapshot.UnidadAbreviatura,
                    UnidadCompraSnapshot = presentacion.Id.HasValue ? presentacion.UnidadCompra : snapshot.UnidadMedida,
                    UnidadCompraAbreviaturaSnapshot = presentacion.Id.HasValue ? presentacion.UnidadCompraAbreviatura : snapshot.UnidadAbreviatura,
                    Cantidad = cantidadBase,
                    CantidadCompra = cantidadCompra,
                    FactorConversionSnapshot = factorConversion,
                    CantidadBaseOrdenada = cantidadBase,
                    CantidadBaseRecibidaAcumulada = 0m,
                    CantidadBasePendiente = cantidadBase,
                    EstadoPartida = 1,
                    CostoUnitario = costoUnitario,
                    Subtotal = subtotal,
                    Total = subtotal,
                    NumeroPartida = numeroPartida++
                });
            }

            return result;
        }

        private async Task UpsertSucursalesAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            Guid idEmpresa,
            Guid idOrdenCompra,
            IEnumerable<Guid> idSucursales,
            Guid? usuarioId,
            DateTime utcNow)
        {
            foreach (Guid idSucursal in idSucursales.Where(id => id != Guid.Empty).Distinct())
            {
                using SqlCommand command = new SqlCommand(@"
IF EXISTS (
    SELECT 1 FROM dbo.OrdenesCompraSucursales WITH (UPDLOCK, HOLDLOCK)
    WHERE idEmpresa=@IdEmpresa AND idOrdenCompra=@IdOrdenCompra AND idSucursal=@IdSucursal)
BEGIN
    UPDATE dbo.OrdenesCompraSucursales
    SET Activo=1, FechaArchivado=NULL, FechaActualizacion=@FechaActualizacion, idUsuarioActualizacion=@IdUsuario
    WHERE idEmpresa=@IdEmpresa AND idOrdenCompra=@IdOrdenCompra AND idSucursal=@IdSucursal;
END
ELSE
BEGIN
    INSERT INTO dbo.OrdenesCompraSucursales
        (id,idEmpresa,identityKey,idOrdenCompra,idSucursal,Activo,FechaCreacion,FechaActualizacion,FechaArchivado,idUsuarioCreacion,idUsuarioActualizacion,CorrelationId)
    VALUES
        (NEWID(),@IdEmpresa,NEWID(),@IdOrdenCompra,@IdSucursal,1,@FechaActualizacion,@FechaActualizacion,NULL,@IdUsuario,@IdUsuario,NULL);
END", connection, transaction);
                command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                command.Parameters.AddWithValue("@IdOrdenCompra", idOrdenCompra);
                command.Parameters.AddWithValue("@IdSucursal", idSucursal);
                command.Parameters.AddWithValue("@FechaActualizacion", utcNow);
                command.Parameters.AddWithValue("@IdUsuario", (object?)usuarioId ?? DBNull.Value);
                await command.ExecuteNonQueryAsync();
            }
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

        private async Task<VarianteOrdenCompraSnapshot> ResolveVarianteOrdenCompraAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            Guid idEmpresa,
            ProductoServicioSnapshot producto,
            Guid? idVariante)
        {
            if (producto.Tipo != 1)
            {
                if (idVariante.HasValue && idVariante.Value != Guid.Empty)
                {
                    throw new CatalogoValidationException("Los servicios no aceptan variante.");
                }

                return new VarianteOrdenCompraSnapshot();
            }

            int variantesActivas = await CountAsync(connection, transaction, @"
SELECT COUNT(1)
FROM dbo.ProductosServiciosVariantes
WHERE idEmpresa = @IdEmpresa
  AND idProductoServicio = @IdProductoServicio
  AND Activo = 1", idEmpresa, producto.Id);

            if (variantesActivas == 0)
            {
                if (idVariante.HasValue && idVariante.Value != Guid.Empty)
                {
                    throw new CatalogoValidationException("La variante no está disponible para el producto seleccionado.");
                }

                return new VarianteOrdenCompraSnapshot();
            }

            if (!idVariante.HasValue || idVariante.Value == Guid.Empty)
            {
                throw new CatalogoValidationException("Selecciona una variante para el producto.");
            }

            using SqlCommand command = new SqlCommand(@"
SELECT TOP (1)
    id,
    Nombre
FROM dbo.ProductosServiciosVariantes
WHERE idEmpresa = @IdEmpresa
  AND idProductoServicio = @IdProductoServicio
  AND id = @IdVariante
  AND Activo = 1", connection, transaction);

            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@IdProductoServicio", producto.Id);
            command.Parameters.AddWithValue("@IdVariante", idVariante.Value);

            using SqlDataReader reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                throw new CatalogoValidationException("La variante no está disponible para el producto seleccionado.");
            }

            return new VarianteOrdenCompraSnapshot
            {
                Id = ReadGuid(reader, "id"),
                Nombre = ReadString(reader, "Nombre")
            };
        }

        private async Task<PresentacionCompraSnapshot> ResolvePresentacionCompraAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            Guid idEmpresa,
            ProductoServicioSnapshot producto,
            Guid? idVariante,
            Guid? idPresentacionCompra)
        {
            if (!idPresentacionCompra.HasValue || idPresentacionCompra.Value == Guid.Empty)
            {
                return new PresentacionCompraSnapshot();
            }

            if (producto.Tipo != 1)
            {
                throw new CatalogoValidationException("Los servicios no aceptan presentación de compra.");
            }

            using SqlCommand command = new SqlCommand(@"
SELECT TOP (1)
    id,
    idVariante,
    Nombre,
    UnidadCompra,
    UnidadCompraAbreviatura,
    FactorConversionBase,
    PermiteCantidadBase
FROM dbo.OrdenesCompraPresentacionesCompra
WHERE idEmpresa = @IdEmpresa
  AND idProductoServicio = @IdProductoServicio
  AND id = @IdPresentacionCompra
  AND Activo = 1
  AND FechaArchivado IS NULL", connection, transaction);

            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@IdProductoServicio", producto.Id);
            command.Parameters.AddWithValue("@IdPresentacionCompra", idPresentacionCompra.Value);

            using SqlDataReader reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                throw new CatalogoValidationException("La presentación de compra no está disponible.");
            }

            Guid? presentacionVariante = ReadNullableGuid(reader, "idVariante");
            if (presentacionVariante.HasValue && (!idVariante.HasValue || presentacionVariante.Value != idVariante.Value))
            {
                throw new CatalogoValidationException("La presentación de compra no corresponde a la variante seleccionada.");
            }

            decimal factor = ReadDecimal(reader, "FactorConversionBase");
            if (factor <= 0m)
            {
                throw new CatalogoValidationException("La presentación de compra tiene un factor inválido.");
            }

            return new PresentacionCompraSnapshot
            {
                Id = ReadGuid(reader, "id"),
                Nombre = ReadString(reader, "Nombre"),
                UnidadCompra = ReadString(reader, "UnidadCompra"),
                UnidadCompraAbreviatura = ReadString(reader, "UnidadCompraAbreviatura"),
                FactorConversionBase = factor,
                PermiteCantidadBase = ReadBool(reader, "PermiteCantidadBase")
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
    idSucursal,
    NumeroPartida,
    idProductoServicio,
    TipoProductoServicio,
    idVariante,
    idPresentacionCompra,
    Codigo,
    Nombre,
    Descripcion,
    VarianteSnapshot,
    PresentacionCompraSnapshot,
    idUnidadMedida,
    UnidadMedida,
    UnidadAbreviatura,
    UnidadCompraSnapshot,
    UnidadCompraAbreviaturaSnapshot,
    Cantidad,
    CantidadCompra,
    FactorConversionSnapshot,
    CantidadBaseOrdenada,
    CantidadBaseRecibidaAcumulada,
    CantidadBasePendiente,
    EstadoPartida,
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
    @IdSucursal,
    @NumeroPartida,
    @IdProductoServicio,
    @TipoProductoServicio,
    @IdVariante,
    @IdPresentacionCompra,
    @Codigo,
    @Nombre,
    @Descripcion,
    @VarianteSnapshot,
    @PresentacionCompraSnapshot,
    @IdUnidadMedida,
    @UnidadMedida,
    @UnidadAbreviatura,
    @UnidadCompraSnapshot,
    @UnidadCompraAbreviaturaSnapshot,
    @Cantidad,
    @CantidadCompra,
    @FactorConversionSnapshot,
    @CantidadBaseOrdenada,
    @CantidadBaseRecibidaAcumulada,
    @CantidadBasePendiente,
    @EstadoPartida,
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
                command.Parameters.AddWithValue("@IdSucursal", partida.IdSucursal);
                command.Parameters.AddWithValue("@NumeroPartida", partida.NumeroPartida);
                command.Parameters.AddWithValue("@IdProductoServicio", partida.IdProductoServicio);
                command.Parameters.AddWithValue("@TipoProductoServicio", partida.TipoProductoServicio);
                command.Parameters.AddWithValue("@IdVariante", partida.IdVariante.HasValue ? partida.IdVariante.Value : DBNull.Value);
                command.Parameters.AddWithValue("@IdPresentacionCompra", partida.IdPresentacionCompra.HasValue ? partida.IdPresentacionCompra.Value : DBNull.Value);
                command.Parameters.AddWithValue("@Codigo", partida.Codigo);
                command.Parameters.AddWithValue("@Nombre", partida.Nombre);
                command.Parameters.AddWithValue("@Descripcion", (object?)NullIfEmpty(partida.Descripcion) ?? DBNull.Value);
                command.Parameters.AddWithValue("@VarianteSnapshot", (object?)NullIfEmpty(partida.VarianteSnapshot) ?? DBNull.Value);
                command.Parameters.AddWithValue("@PresentacionCompraSnapshot", (object?)NullIfEmpty(partida.PresentacionCompraSnapshot) ?? DBNull.Value);
                command.Parameters.AddWithValue("@IdUnidadMedida", partida.IdUnidadMedida);
                command.Parameters.AddWithValue("@UnidadMedida", partida.UnidadMedida);
                command.Parameters.AddWithValue("@UnidadAbreviatura", partida.UnidadAbreviatura);
                command.Parameters.AddWithValue("@UnidadCompraSnapshot", (object?)NullIfEmpty(partida.UnidadCompraSnapshot) ?? DBNull.Value);
                command.Parameters.AddWithValue("@UnidadCompraAbreviaturaSnapshot", (object?)NullIfEmpty(partida.UnidadCompraAbreviaturaSnapshot) ?? DBNull.Value);
                command.Parameters.AddWithValue("@Cantidad", partida.Cantidad);
                command.Parameters.AddWithValue("@CantidadCompra", partida.CantidadCompra);
                command.Parameters.AddWithValue("@FactorConversionSnapshot", partida.FactorConversionSnapshot);
                command.Parameters.AddWithValue("@CantidadBaseOrdenada", partida.CantidadBaseOrdenada);
                command.Parameters.AddWithValue("@CantidadBaseRecibidaAcumulada", partida.CantidadBaseRecibidaAcumulada);
                command.Parameters.AddWithValue("@CantidadBasePendiente", partida.CantidadBasePendiente);
                command.Parameters.AddWithValue("@EstadoPartida", partida.EstadoPartida);
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
    d.id,
    d.NumeroPartida,
    d.idSucursal,
    ISNULL(su.Nombre, '') AS Sucursal,
    d.idProductoServicio,
    d.TipoProductoServicio,
    d.idVariante,
    d.idPresentacionCompra,
    d.Codigo,
    d.Nombre,
    ISNULL(d.Descripcion, '') AS Descripcion,
    ISNULL(cat.Nombre, '') AS Categoria,
    ISNULL(m.Nombre, '') AS Marca,
    ISNULL(d.VarianteSnapshot, '') AS VarianteSnapshot,
    ISNULL(d.PresentacionCompraSnapshot, '') AS PresentacionCompraSnapshot,
    d.idUnidadMedida,
    d.UnidadMedida,
    d.UnidadAbreviatura,
    ISNULL(d.UnidadCompraSnapshot, '') AS UnidadCompraSnapshot,
    ISNULL(d.UnidadCompraAbreviaturaSnapshot, '') AS UnidadCompraAbreviaturaSnapshot,
    d.Cantidad,
    d.CantidadCompra,
    d.FactorConversionSnapshot,
    d.CantidadBaseOrdenada,
    d.CantidadBaseRecibidaAcumulada,
    d.CantidadBasePendiente,
    d.EstadoPartida,
    d.CostoUnitario,
    d.Subtotal,
    d.Total
FROM dbo.OrdenesCompraDetalle d
LEFT JOIN dbo.ProductosServicios ps
    ON ps.idEmpresa = d.idEmpresa AND ps.id = d.idProductoServicio
LEFT JOIN dbo.ProductosServiciosCategorias cat
    ON cat.idEmpresa = ps.idEmpresa AND cat.id = ps.idCategoria
LEFT JOIN dbo.ProductosServiciosMarcas m
    ON m.idEmpresa = ps.idEmpresa AND m.id = ps.idMarca
INNER JOIN dbo.Sucursales su
    ON su.idEmpresa=d.idEmpresa AND su.id=d.idSucursal
WHERE d.idEmpresa = @IdEmpresa
  AND d.idOrdenCompra = @IdOrdenCompra
  AND d.Activo = 1
  AND d.FechaArchivado IS NULL
ORDER BY d.NumeroPartida", connection);

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
                    IdSucursal = ReadGuid(reader, "idSucursal"),
                    Sucursal = ReadString(reader, "Sucursal"),
                    IdProductoServicio = ReadGuid(reader, "idProductoServicio"),
                    TipoProductoServicio = tipo,
                    TipoProductoServicioNombre = GetTipoNombre(tipo),
                    IdVariante = ReadNullableGuid(reader, "idVariante"),
                    IdPresentacionCompra = ReadNullableGuid(reader, "idPresentacionCompra"),
                    Codigo = ReadString(reader, "Codigo"),
                    Nombre = ReadString(reader, "Nombre"),
                    Descripcion = ReadString(reader, "Descripcion"),
                    Categoria = ReadString(reader, "Categoria"),
                    Marca = ReadString(reader, "Marca"),
                    VarianteSnapshot = ReadString(reader, "VarianteSnapshot"),
                    PresentacionCompraSnapshot = ReadString(reader, "PresentacionCompraSnapshot"),
                    IdUnidadMedida = ReadGuid(reader, "idUnidadMedida"),
                    UnidadMedida = ReadString(reader, "UnidadMedida"),
                    UnidadAbreviatura = ReadString(reader, "UnidadAbreviatura"),
                    UnidadCompraSnapshot = ReadString(reader, "UnidadCompraSnapshot"),
                    UnidadCompraAbreviaturaSnapshot = ReadString(reader, "UnidadCompraAbreviaturaSnapshot"),
                    Cantidad = ReadDecimal(reader, "Cantidad"),
                    CantidadCompra = ReadDecimal(reader, "CantidadCompra"),
                    FactorConversionSnapshot = ReadDecimal(reader, "FactorConversionSnapshot"),
                    CantidadBaseOrdenada = ReadDecimal(reader, "CantidadBaseOrdenada"),
                    CantidadBaseRecibidaAcumulada = ReadDecimal(reader, "CantidadBaseRecibidaAcumulada"),
                    CantidadBasePendiente = ReadDecimal(reader, "CantidadBasePendiente"),
                    EstadoPartida = ReadByte(reader, "EstadoPartida"),
                    EstadoPartidaNombre = GetEstadoPartidaNombre(ReadByte(reader, "EstadoPartida")),
                    CostoUnitario = ReadDecimal(reader, "CostoUnitario"),
                    Subtotal = ReadDecimal(reader, "Subtotal"),
                    Total = ReadDecimal(reader, "Total")
                });
            }

            return partidas;
        }

        private async Task<List<OrdenCompraSucursalDto>> ObtenerSucursalesAsync(SqlConnection connection, Guid idEmpresa, Guid idOrdenCompra)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT s.id, s.idRazonSocial, CAST('' AS nvarchar(50)) AS Codigo, ISNULL(s.Nombre,'') AS Nombre
FROM dbo.OrdenesCompraSucursales os
INNER JOIN dbo.Sucursales s ON s.idEmpresa=os.idEmpresa AND s.id=os.idSucursal
WHERE os.idEmpresa=@IdEmpresa AND os.idOrdenCompra=@IdOrdenCompra AND os.Activo=1 AND os.FechaArchivado IS NULL
ORDER BY s.Nombre, s.id", connection);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@IdOrdenCompra", idOrdenCompra);
            List<OrdenCompraSucursalDto> result = new List<OrdenCompraSucursalDto>();
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new OrdenCompraSucursalDto { Id=ReadGuid(reader,"id"), IdRazonSocial=ReadGuid(reader,"idRazonSocial"), Codigo=ReadString(reader,"Codigo"), Nombre=ReadString(reader,"Nombre") });
            }
            return result;
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
SELECT id, idRazonSocial, Nombre, ISNULL(Direccion, '') AS Descripcion
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
                    IdRazonSocial = HasColumn(reader, "idRazonSocial") ? ReadNullableGuid(reader, "idRazonSocial") : null,
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

        private async Task<int> CountAsync(SqlConnection connection, SqlTransaction transaction, string sql, Guid idEmpresa, Guid idProductoServicio)
        {
            using SqlCommand command = new SqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);
            object? scalar = await command.ExecuteScalarAsync();
            return scalar != null && scalar != DBNull.Value
                ? Convert.ToInt32(scalar, CultureInfo.InvariantCulture)
                : 0;
        }

        private async Task HydrateBusquedaV1Async(SqlConnection connection, Guid idEmpresa, List<OrdenCompraBusquedaProductoServicioDto> items)
        {
            List<Guid> productIds = items
                .Where(item => item.Tipo == 1)
                .Select(item => item.Id)
                .Distinct()
                .ToList();

            if (productIds.Count == 0)
            {
                return;
            }

            Dictionary<Guid, OrdenCompraBusquedaProductoServicioDto> byProduct = items.ToDictionary(item => item.Id, item => item);
            string productFilter = BuildGuidInClause(productIds, "@Producto");

            using (SqlCommand variantCommand = new SqlCommand($@"
SELECT
    id,
    idProductoServicio,
    ISNULL(Sku, '') AS Sku,
    Nombre,
    ClaveCombinacion,
    Costo
FROM dbo.ProductosServiciosVariantes
WHERE idEmpresa = @IdEmpresa
  AND Activo = 1
  AND idProductoServicio IN ({productFilter})
ORDER BY idProductoServicio, Orden, Nombre", connection))
            {
                variantCommand.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                AddGuidInParameters(variantCommand, productIds, "@Producto");

                using SqlDataReader reader = await variantCommand.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    Guid idProductoServicio = ReadGuid(reader, "idProductoServicio");
                    if (!byProduct.TryGetValue(idProductoServicio, out OrdenCompraBusquedaProductoServicioDto? product))
                    {
                        continue;
                    }

                    product.Variantes.Add(new OrdenCompraBusquedaVarianteDto
                    {
                        Id = ReadGuid(reader, "id"),
                        IdProductoServicio = idProductoServicio,
                        Sku = ReadString(reader, "Sku"),
                        Nombre = ReadString(reader, "Nombre"),
                        ClaveCombinacion = ReadString(reader, "ClaveCombinacion"),
                        CostoActual = ReadNullableDecimal(reader, "Costo")
                    });
                }
            }

            using (SqlCommand presentationCommand = new SqlCommand($@"
SELECT
    id,
    idProductoServicio,
    idVariante,
    Nombre,
    idUnidadCompra,
    UnidadCompra,
    UnidadCompraAbreviatura,
    FactorConversionBase,
    PermiteCantidadBase
FROM dbo.OrdenesCompraPresentacionesCompra
WHERE idEmpresa = @IdEmpresa
  AND Activo = 1
  AND FechaArchivado IS NULL
  AND idProductoServicio IN ({productFilter})
ORDER BY idProductoServicio, Nombre", connection))
            {
                presentationCommand.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                AddGuidInParameters(presentationCommand, productIds, "@Producto");

                using SqlDataReader reader = await presentationCommand.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    Guid idProductoServicio = ReadGuid(reader, "idProductoServicio");
                    if (!byProduct.TryGetValue(idProductoServicio, out OrdenCompraBusquedaProductoServicioDto? product))
                    {
                        continue;
                    }

                    product.PresentacionesCompra.Add(new OrdenCompraBusquedaPresentacionCompraDto
                    {
                        Id = ReadGuid(reader, "id"),
                        IdProductoServicio = idProductoServicio,
                        IdVariante = ReadNullableGuid(reader, "idVariante"),
                        Nombre = ReadString(reader, "Nombre"),
                        IdUnidadCompra = ReadGuid(reader, "idUnidadCompra"),
                        UnidadCompra = ReadString(reader, "UnidadCompra"),
                        UnidadCompraAbreviatura = ReadString(reader, "UnidadCompraAbreviatura"),
                        FactorConversionBase = ReadDecimal(reader, "FactorConversionBase"),
                        PermiteCantidadBase = ReadBool(reader, "PermiteCantidadBase")
                    });
                }
            }

            foreach (OrdenCompraBusquedaProductoServicioDto item in items)
            {
                item.RequiereVariante = item.Tipo == 1 && item.Variantes.Count > 0;
            }
        }

        private static string BuildGuidInClause(List<Guid> ids, string prefix)
        {
            return string.Join(", ", ids.Select((_, index) => $"{prefix}{index}"));
        }

        private static void AddGuidInParameters(SqlCommand command, List<Guid> ids, string prefix)
        {
            for (int index = 0; index < ids.Count; index++)
            {
                command.Parameters.AddWithValue($"{prefix}{index}", ids[index]);
            }
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

            if (request.IdSucursales == null || request.IdSucursales.Count == 0 || request.IdSucursales.Any(id => id == Guid.Empty))
            {
                return "Selecciona al menos una sucursal destino.";
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

            if (request.FechaMinima.HasValue && request.FechaMinima.Value.Date < request.FechaOrden.Date)
            {
                return "La fecha mínima no puede ser anterior a la fecha de la orden.";
            }

            if (request.FechaMinima.HasValue && request.FechaMaxima.HasValue
                && request.FechaMinima.Value.Date > request.FechaMaxima.Value.Date)
            {
                return "La fecha mínima no puede ser posterior a la fecha máxima.";
            }

            if (request.FechaLlegada.HasValue && request.FechaMinima.HasValue
                && request.FechaLlegada.Value.Date < request.FechaMinima.Value.Date)
            {
                return "La fecha de llegada no puede ser anterior a la fecha mínima.";
            }

            if (request.FechaLlegada.HasValue && request.FechaMaxima.HasValue
                && request.FechaLlegada.Value.Date > request.FechaMaxima.Value.Date)
            {
                return "La fecha de llegada no puede ser posterior a la fecha máxima.";
            }

            if (!string.IsNullOrWhiteSpace(request.Observaciones) && request.Observaciones.Trim().Length > ObservacionesLength)
            {
                return "Las observaciones exceden la longitud permitida.";
            }

            if (!string.IsNullOrWhiteSpace(request.FolioReferencia) && request.FolioReferencia.Trim().Length > 100)
            {
                return "El folio opcional excede la longitud permitida.";
            }

            if (request.Partidas == null || request.Partidas.Count == 0)
            {
                return "La orden debe contener al menos una partida.";
            }

            return string.Empty;
        }

        private void ValidateRequestPartidas(List<OrdenCompraPartidaGuardarRequest> partidas)
        {
            HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (OrdenCompraPartidaGuardarRequest partida in partidas)
            {
                if (partida == null || partida.IdProductoServicio == Guid.Empty)
                {
                    throw new CatalogoValidationException("El producto o servicio no está disponible.");
                }

                if (partida.IdSucursal == Guid.Empty)
                {
                    throw new CatalogoValidationException("Cada partida debe tener una sucursal destino.");
                }
                string variantKey = partida.IdVariante.HasValue && partida.IdVariante.Value != Guid.Empty
                    ? partida.IdVariante.Value.ToString("D")
                    : "NULL";
                string presentationKey = partida.IdPresentacionCompra.HasValue && partida.IdPresentacionCompra.Value != Guid.Empty
                    ? partida.IdPresentacionCompra.Value.ToString("D")
                    : "BASE";
                if (!ids.Add($"{partida.IdSucursal:D}:{partida.IdProductoServicio:D}:{variantKey}:{presentationKey}"))
                {
                    throw new CatalogoValidationException("No se permiten partidas duplicadas para la misma sucursal, producto, variante y presentación.");
                }

                decimal cantidadCompra = partida.CantidadCompra ?? partida.Cantidad;
                decimal factor = partida.FactorConversionSnapshot ?? 1m;
                if (cantidadCompra <= 0m || factor <= 0m)
                {
                    throw new CatalogoValidationException("La orden debe contener al menos una partida.");
                }

                if (!HasScale(cantidadCompra, 4) || !HasScale(factor, 12))
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

        private async Task<IActionResult?> AuthorizeOrdenCompraAnyAsync(
            RequestContext context,
            ProductosServiciosPermissionRequirement requirement,
            params string[] permissionCodes)
        {
            IActionResult? lastDeny = null;
            foreach (string permissionCode in permissionCodes)
            {
                IActionResult? auth = await AuthorizeOrdenCompraAsync(context, permissionCode, requirement);
                if (auth == null)
                {
                    return null;
                }

                lastDeny = auth;
            }

            return lastDeny;
        }

        private async Task<IActionResult?> AuthorizeOrdenCompraAsync(
            RequestContext context,
            string permissionCode,
            ProductosServiciosPermissionRequirement requirement)
        {
            if (_authorizationService == null)
            {
                return StatusCode(503, new OrdenCompraOperacionResponse { Mensaje = "La autorización de órdenes de compra no está disponible." });
            }

            string usuarioId = TryResolveUserId();
            if (string.IsNullOrWhiteSpace(usuarioId))
            {
                return Unauthorized(new OrdenCompraOperacionResponse { Mensaje = "No fue posible resolver el usuario activo." });
            }

            ProductosServiciosAuthorizationDecision decision = await _authorizationService.AuthorizeAsync(new ProductosServiciosAuthorizationRequest
            {
                IdEmpresa = context.IdEmpresa,
                UserId = usuarioId,
                PermissionCode = permissionCode,
                Requirement = requirement,
                TenantDatabase = new TenantDatabaseDescriptor
                {
                    IdEmpresa = context.IdEmpresa,
                    EmpresaKey = context.EmpresaStorageKey,
                    ConnectionString = string.Empty
                }
            }, HttpContext.RequestAborted);

            if (decision.IsAllowed(requirement))
            {
                return null;
            }

            _logger.LogWarning(
                "AuthZ OrdenesCompra bloqueó operación. ReferenceId={ReferenceId} PermissionCode={PermissionCode} Requirement={Requirement} ReasonCode={ReasonCode}",
                decision.ReferenceId,
                decision.PermissionCode,
                requirement,
                decision.ReasonCode);

            return StatusCode(403, new OrdenCompraOperacionResponse
            {
                Mensaje = $"No tienes permiso para órdenes de compra. Ref: {decision.ReferenceId}"
            });
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

        private string TryResolveUserId()
        {
            foreach (string claimKey in UsuarioClaimKeys)
            {
                string? value = User.FindFirstValue(claimKey);
                if (!string.IsNullOrWhiteSpace(value) && value.Length <= 256 && !value.Any(char.IsControl))
                {
                    return value.Trim();
                }
            }

            if (TryResolveSignedProxyContext(out SignedProxyContext? proxyContext) &&
                proxyContext != null &&
                !string.IsNullOrWhiteSpace(proxyContext.UserId))
            {
                return proxyContext.UserId;
            }

            return string.Empty;
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
                UserId = usuarioIdRaw,
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
                EstadoParcialmenteRecibida => "Parcialmente recibida",
                EstadoRecibida => "Recibida",
                _ => "Desconocido"
            };
        }

        private static string GetEstadoNombreUsuario(byte estado)
        {
            return estado switch
            {
                EstadoBorrador => "Borrador",
                EstadoGenerada => "Generada",
                EstadoCancelada => "Cancelada",
                EstadoParcialmenteRecibida => "Parcialmente recibida",
                EstadoRecibida => "Recibida",
                _ => "Desconocido"
            };
        }

        private static string GetEstadoPartidaNombre(byte estado)
        {
            return estado switch
            {
                1 => "Pendiente",
                2 => "Parcialmente recibida",
                3 => "Recibida",
                4 => "Cancelada",
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
            public string UserId { get; set; } = string.Empty;
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

        private sealed class VarianteOrdenCompraSnapshot
        {
            public Guid? Id { get; set; }
            public string Nombre { get; set; } = string.Empty;
        }

        private sealed class PresentacionCompraSnapshot
        {
            public Guid? Id { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public string UnidadCompra { get; set; } = string.Empty;
            public string UnidadCompraAbreviatura { get; set; } = string.Empty;
            public decimal FactorConversionBase { get; set; } = 1m;
            public bool PermiteCantidadBase { get; set; }
        }

        private sealed class OrdenCompraPartidaPersistencia
        {
            public Guid Id { get; set; }
            public Guid IdSucursal { get; set; }
            public int NumeroPartida { get; set; }
            public Guid IdProductoServicio { get; set; }
            public byte TipoProductoServicio { get; set; }
            public Guid? IdVariante { get; set; }
            public Guid? IdPresentacionCompra { get; set; }
            public string Codigo { get; set; } = string.Empty;
            public string Nombre { get; set; } = string.Empty;
            public string Descripcion { get; set; } = string.Empty;
            public string VarianteSnapshot { get; set; } = string.Empty;
            public string PresentacionCompraSnapshot { get; set; } = string.Empty;
            public Guid IdUnidadMedida { get; set; }
            public string UnidadMedida { get; set; } = string.Empty;
            public string UnidadAbreviatura { get; set; } = string.Empty;
            public string UnidadCompraSnapshot { get; set; } = string.Empty;
            public string UnidadCompraAbreviaturaSnapshot { get; set; } = string.Empty;
            public decimal Cantidad { get; set; }
            public decimal CantidadCompra { get; set; }
            public decimal FactorConversionSnapshot { get; set; }
            public decimal CantidadBaseOrdenada { get; set; }
            public decimal CantidadBaseRecibidaAcumulada { get; set; }
            public decimal CantidadBasePendiente { get; set; }
            public byte EstadoPartida { get; set; }
            public decimal CostoUnitario { get; set; }
            public decimal Subtotal { get; set; }
            public decimal Total { get; set; }
        }

        private sealed class ClosureOperation
        {
            public Guid Id { get; set; }
            public byte Estado { get; set; }
            public DateTime FechaExpiracion { get; set; }
            public string PropuestaJson { get; set; } = string.Empty;
            public string ValidacionJson { get; set; } = string.Empty;
            public string ResultadoJson { get; set; } = string.Empty;
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
