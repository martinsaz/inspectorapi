using System.Data.SqlClient;
using System.Security.Claims;
using checklistWs.Models.Recepcion;
using checklistWs.Services.Tenant;
using Microsoft.AspNetCore.Mvc;

namespace checklistWs.Controllers.Recepcion
{
    [Route("api/[controller]")]
    [ApiController]
    public sealed class RecepcionController : ControllerBase
    {
        private static readonly string[] EmpresaClaimKeys = { "idEmpresa", "empresaId", "tenantId", "companyId", "tenant", "idempresa" };
        private static readonly string[] EmpresaKeyClaimKeys = { "empresa", "empresaNombre", "tenantName", "companyName", "nombreEmpresa" };
        private static readonly string[] UsuarioClaimKeys = { ClaimTypes.NameIdentifier, "sub", "idUsuario", "userid", "uid" };

        private readonly ITenantDatabaseResolver _tenantResolver;
        private readonly ITenantSqlConnectionFactory _connectionFactory;
        private readonly IProductosServiciosAuthorizationService _authorizationService;
        private readonly IProductosServiciosCompatibilityGate _compatibilityGate;
        private readonly IRecepcionScopeService _recepcionService;
        private readonly ILogger<RecepcionController> _logger;

        public RecepcionController(
            ITenantDatabaseResolver tenantResolver,
            ITenantSqlConnectionFactory connectionFactory,
            IProductosServiciosAuthorizationService authorizationService,
            IProductosServiciosCompatibilityGate compatibilityGate,
            IRecepcionScopeService recepcionService,
            ILogger<RecepcionController> logger)
        {
            _tenantResolver = tenantResolver;
            _connectionFactory = connectionFactory;
            _authorizationService = authorizationService;
            _compatibilityGate = compatibilityGate;
            _recepcionService = recepcionService;
            _logger = logger;
        }

        [HttpGet("ObtenerOrdenesCompraPendientesRecepcion")]
        public async Task<IActionResult> ObtenerOrdenesCompraPendientesRecepcion(Guid idEmpresa, string empresaKey = "")
        {
            TenantDatabaseDescriptor? descriptor = await ResolveDescriptorAsync(idEmpresa, empresaKey);
            if (descriptor == null)
            {
                return Unauthorized(new RecepcionOperacionResponse { Mensaje = "No fue posible resolver la empresa activa." });
            }

            IActionResult? guard = await GuardAsync(descriptor, ProductosServiciosAuthorizationDefaults.RecepcionNuevaPermissionCode, ProductosServiciosPermissionRequirement.Read, DatabaseScopes.Recepcion, DatabaseScopes.OrdenesCompra);
            if (guard != null)
            {
                return guard;
            }

            try
            {
                await using SqlConnection connection = _connectionFactory.CreateConnection(descriptor);
                await connection.OpenAsync(HttpContext.RequestAborted);
                using SqlCommand command = new(@"
SELECT
    oc.id,
    ISNULL(oc.Folio, '') AS Folio,
    oc.idSucursal,
    ISNULL(s.Nombre, '') AS Sucursal,
    oc.Estado
FROM dbo.OrdenesCompra oc
LEFT JOIN dbo.Sucursales s ON s.idEmpresa = oc.idEmpresa AND s.id = oc.idSucursal
WHERE oc.idEmpresa = @IdEmpresa
  AND oc.Activo = 1
  AND oc.FechaArchivado IS NULL
  AND oc.Estado IN (2, 4)
  AND EXISTS (
      SELECT 1
      FROM dbo.OrdenesCompraDetalle d
      WHERE d.idEmpresa = oc.idEmpresa
        AND d.idOrdenCompra = oc.id
        AND d.Activo = 1
        AND d.FechaArchivado IS NULL
        AND d.CantidadBasePendiente > 0
  )
ORDER BY oc.FechaOrden DESC, oc.FechaCreacion DESC", connection);
                command.Parameters.AddWithValue("@IdEmpresa", descriptor.IdEmpresa);

                List<OrdenCompraRecepcionPendienteDto> items = new();
                using SqlDataReader reader = await command.ExecuteReaderAsync(HttpContext.RequestAborted);
                while (await reader.ReadAsync(HttpContext.RequestAborted))
                {
                    byte estado = ReadByte(reader, "Estado");
                    items.Add(new OrdenCompraRecepcionPendienteDto
                    {
                        IdOrdenCompra = ReadGuid(reader, "id"),
                        Folio = ReadString(reader, "Folio"),
                        IdSucursal = ReadGuid(reader, "idSucursal"),
                        Sucursal = ReadString(reader, "Sucursal"),
                        Estado = estado,
                        EstadoNombre = GetOrdenEstadoNombre(estado)
                    });
                }

                return Ok(items);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerOrdenesCompraPendientesRecepcion");
            }
        }

        [HttpGet("ObtenerDetallePendienteOrdenCompra")]
        public async Task<IActionResult> ObtenerDetallePendienteOrdenCompra(Guid idEmpresa, Guid idOrdenCompra, string empresaKey = "")
        {
            TenantDatabaseDescriptor? descriptor = await ResolveDescriptorAsync(idEmpresa, empresaKey);
            if (descriptor == null)
            {
                return Unauthorized(new RecepcionOperacionResponse { Mensaje = "No fue posible resolver la empresa activa." });
            }

            IActionResult? guard = await GuardAsync(descriptor, ProductosServiciosAuthorizationDefaults.RecepcionNuevaPermissionCode, ProductosServiciosPermissionRequirement.Read, DatabaseScopes.Recepcion, DatabaseScopes.OrdenesCompra);
            if (guard != null)
            {
                return guard;
            }

            try
            {
                OrdenCompraRecepcionPendienteDto? detalle = await LoadOrdenPendienteAsync(descriptor, idOrdenCompra);
                return detalle == null ? NotFound(new RecepcionOperacionResponse { Mensaje = "Orden de compra no disponible para recepción." }) : Ok(detalle);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerDetallePendienteOrdenCompra");
            }
        }

        [HttpPost("ConfirmarRecepcion")]
        public async Task<IActionResult> ConfirmarRecepcion([FromBody] RecepcionConfirmarRequest request, string empresaKey = "")
        {
            TenantDatabaseDescriptor? descriptor = await ResolveDescriptorAsync(request?.IdEmpresa ?? Guid.Empty, empresaKey);
            if (descriptor == null)
            {
                return Unauthorized(new RecepcionOperacionResponse { Mensaje = "No fue posible resolver la empresa activa." });
            }

            IActionResult? guard = await GuardAsync(descriptor, ProductosServiciosAuthorizationDefaults.RecepcionNuevaPermissionCode, ProductosServiciosPermissionRequirement.Write, DatabaseScopes.Recepcion, DatabaseScopes.OrdenesCompra, DatabaseScopes.Inventario);
            if (guard != null)
            {
                return guard;
            }

            try
            {
                RecepcionOperacionResponse response = await _recepcionService.ConfirmarAsync(descriptor, request!, TryResolveUsuarioGuid(), HttpContext.RequestAborted);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ConfirmarRecepcion");
            }
        }

        [HttpGet("ObtenerRecepciones")]
        public async Task<IActionResult> ObtenerRecepciones(Guid idEmpresa, string empresaKey = "", Guid? idOrdenCompra = null, Guid? idSucursal = null)
        {
            TenantDatabaseDescriptor? descriptor = await ResolveDescriptorAsync(idEmpresa, empresaKey);
            if (descriptor == null)
            {
                return Unauthorized(new RecepcionOperacionResponse { Mensaje = "No fue posible resolver la empresa activa." });
            }

            IActionResult? guard = await GuardAsync(descriptor, ProductosServiciosAuthorizationDefaults.RecepcionReportePermissionCode, ProductosServiciosPermissionRequirement.Read, DatabaseScopes.Recepcion);
            if (guard != null)
            {
                return guard;
            }

            try
            {
                await using SqlConnection connection = _connectionFactory.CreateConnection(descriptor);
                await connection.OpenAsync(HttpContext.RequestAborted);
                using SqlCommand command = new(@"
SELECT
    r.id,
    r.FolioRecepcion,
    r.idOrdenCompra,
    ISNULL(oc.Folio, '') AS FolioOrdenCompra,
    r.idSucursal,
    ISNULL(s.Nombre, '') AS Sucursal,
    r.FechaRecepcion,
    r.Estado
FROM dbo.Recepciones r
INNER JOIN dbo.OrdenesCompra oc ON oc.idEmpresa = r.idEmpresa AND oc.id = r.idOrdenCompra
LEFT JOIN dbo.Sucursales s ON s.idEmpresa = r.idEmpresa AND s.id = r.idSucursal
WHERE r.idEmpresa = @IdEmpresa
  AND (@IdOrdenCompra IS NULL OR r.idOrdenCompra = @IdOrdenCompra)
  AND (@IdSucursal IS NULL OR r.idSucursal = @IdSucursal)
ORDER BY r.FechaRecepcion DESC, r.FechaCreacion DESC", connection);
                command.Parameters.AddWithValue("@IdEmpresa", descriptor.IdEmpresa);
                command.Parameters.AddWithValue("@IdOrdenCompra", (object?)idOrdenCompra ?? DBNull.Value);
                command.Parameters.AddWithValue("@IdSucursal", (object?)idSucursal ?? DBNull.Value);

                List<RecepcionListadoDto> items = new();
                using SqlDataReader reader = await command.ExecuteReaderAsync(HttpContext.RequestAborted);
                while (await reader.ReadAsync(HttpContext.RequestAborted))
                {
                    byte estado = ReadByte(reader, "Estado");
                    items.Add(new RecepcionListadoDto
                    {
                        Id = ReadGuid(reader, "id"),
                        FolioRecepcion = ReadString(reader, "FolioRecepcion"),
                        IdOrdenCompra = ReadGuid(reader, "idOrdenCompra"),
                        FolioOrdenCompra = ReadString(reader, "FolioOrdenCompra"),
                        IdSucursal = ReadGuid(reader, "idSucursal"),
                        Sucursal = ReadString(reader, "Sucursal"),
                        FechaRecepcion = ReadDateTime(reader, "FechaRecepcion"),
                        Estado = estado,
                        EstadoNombre = GetRecepcionEstadoNombre(estado)
                    });
                }

                return Ok(items);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerRecepciones");
            }
        }

        [HttpGet("ObtenerRecepcion")]
        public async Task<IActionResult> ObtenerRecepcion(Guid idEmpresa, Guid idRecepcion, string empresaKey = "")
        {
            TenantDatabaseDescriptor? descriptor = await ResolveDescriptorAsync(idEmpresa, empresaKey);
            if (descriptor == null)
            {
                return Unauthorized(new RecepcionOperacionResponse { Mensaje = "No fue posible resolver la empresa activa." });
            }

            IActionResult? guard = await GuardAsync(descriptor, ProductosServiciosAuthorizationDefaults.RecepcionReportePermissionCode, ProductosServiciosPermissionRequirement.Read, DatabaseScopes.Recepcion);
            if (guard != null)
            {
                return guard;
            }

            try
            {
                RecepcionDetalleDto? detalle = await LoadRecepcionAsync(descriptor, idRecepcion);
                return detalle == null ? NotFound(new RecepcionOperacionResponse { Mensaje = "Recepción no encontrada." }) : Ok(detalle);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerRecepcion");
            }
        }

        private async Task<IActionResult?> GuardAsync(TenantDatabaseDescriptor descriptor, string permissionCode, ProductosServiciosPermissionRequirement requirement, params string[] scopes)
        {
            string userId = TryResolveUsuarioId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new RecepcionOperacionResponse { Mensaje = "No fue posible resolver el usuario activo." });
            }

            ProductosServiciosAuthorizationDecision decision = await _authorizationService.AuthorizeAsync(new ProductosServiciosAuthorizationRequest
            {
                IdEmpresa = descriptor.IdEmpresa,
                UserId = userId,
                PermissionCode = permissionCode,
                Requirement = requirement,
                TenantDatabase = descriptor
            }, HttpContext.RequestAborted);

            if (!decision.IsAllowed(requirement))
            {
                return StatusCode(403, new RecepcionOperacionResponse { Mensaje = $"No tienes permiso para recepción. Ref: {decision.ReferenceId}" });
            }

            foreach (string scope in scopes.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                CompatibilityDecision compatibility = await _compatibilityGate.EvaluateAsync(descriptor, scope, HttpContext.RequestAborted);
                if (!compatibility.IsAllowed)
                {
                    return StatusCode(503, new RecepcionOperacionResponse { Mensaje = $"Recepción no disponible para {scope}. Ref: {compatibility.ReferenceId}" });
                }
            }

            return null;
        }

        private async Task<TenantDatabaseDescriptor?> ResolveDescriptorAsync(Guid idEmpresa, string empresaKey)
        {
            Guid effectiveEmpresaId = idEmpresa != Guid.Empty ? idEmpresa : TryResolveEmpresaId();
            string effectiveEmpresaKey = !string.IsNullOrWhiteSpace(empresaKey) ? empresaKey.Trim().ToUpperInvariant() : TryResolveEmpresaKey(effectiveEmpresaId);
            if (effectiveEmpresaId == Guid.Empty || string.IsNullOrWhiteSpace(effectiveEmpresaKey))
            {
                return null;
            }

            try
            {
                return await _tenantResolver.ResolveAsync(new TenantDatabaseContext
                {
                    IdEmpresa = effectiveEmpresaId,
                    EmpresaKey = effectiveEmpresaKey
                }, HttpContext.RequestAborted);
            }
            catch (TenantDatabaseResolutionException ex)
            {
                _logger.LogWarning(ex, "No fue posible resolver tenant para Recepcion. IdEmpresa={IdEmpresa}", effectiveEmpresaId);
                return null;
            }
        }

        private async Task<OrdenCompraRecepcionPendienteDto?> LoadOrdenPendienteAsync(TenantDatabaseDescriptor descriptor, Guid idOrdenCompra)
        {
            await using SqlConnection connection = _connectionFactory.CreateConnection(descriptor);
            await connection.OpenAsync(HttpContext.RequestAborted);
            using SqlCommand header = new(@"
SELECT oc.id, ISNULL(oc.Folio, '') AS Folio, oc.idSucursal, ISNULL(s.Nombre, '') AS Sucursal, oc.Estado
FROM dbo.OrdenesCompra oc
LEFT JOIN dbo.Sucursales s ON s.idEmpresa = oc.idEmpresa AND s.id = oc.idSucursal
WHERE oc.idEmpresa = @IdEmpresa
  AND oc.id = @IdOrdenCompra
  AND oc.Activo = 1
  AND oc.FechaArchivado IS NULL
  AND oc.Estado IN (2, 4)", connection);
            header.Parameters.AddWithValue("@IdEmpresa", descriptor.IdEmpresa);
            header.Parameters.AddWithValue("@IdOrdenCompra", idOrdenCompra);

            OrdenCompraRecepcionPendienteDto? dto = null;
            using (SqlDataReader reader = await header.ExecuteReaderAsync(HttpContext.RequestAborted))
            {
                if (!await reader.ReadAsync(HttpContext.RequestAborted))
                {
                    return null;
                }

                byte estado = ReadByte(reader, "Estado");
                dto = new OrdenCompraRecepcionPendienteDto
                {
                    IdOrdenCompra = ReadGuid(reader, "id"),
                    Folio = ReadString(reader, "Folio"),
                    IdSucursal = ReadGuid(reader, "idSucursal"),
                    Sucursal = ReadString(reader, "Sucursal"),
                    Estado = estado,
                    EstadoNombre = GetOrdenEstadoNombre(estado)
                };
            }

            using SqlCommand details = new(@"
SELECT
    d.id,
    d.NumeroPartida,
    d.TipoProductoServicio,
    d.idProductoServicio,
    d.idVariante,
    d.idPresentacionCompra,
    ISNULL(ps.Nombre, '') AS Producto,
    ISNULL(v.Nombre, '') AS Variante,
    ISNULL(d.PresentacionCompraSnapshot, '') AS PresentacionCompraSnapshot,
    d.FactorConversionSnapshot,
    d.CantidadBaseOrdenada,
    d.CantidadBaseRecibidaAcumulada,
    d.CantidadBasePendiente,
    ps.CausaInventario,
    ps.UsaNumeroSerie
FROM dbo.OrdenesCompraDetalle d
INNER JOIN dbo.ProductosServicios ps ON ps.idEmpresa = d.idEmpresa AND ps.id = d.idProductoServicio
LEFT JOIN dbo.ProductosServiciosVariantes v ON v.idEmpresa = d.idEmpresa AND v.id = d.idVariante
WHERE d.idEmpresa = @IdEmpresa
  AND d.idOrdenCompra = @IdOrdenCompra
  AND d.Activo = 1
  AND d.FechaArchivado IS NULL
  AND d.CantidadBasePendiente > 0
ORDER BY d.NumeroPartida", connection);
            details.Parameters.AddWithValue("@IdEmpresa", descriptor.IdEmpresa);
            details.Parameters.AddWithValue("@IdOrdenCompra", idOrdenCompra);

            using SqlDataReader detailReader = await details.ExecuteReaderAsync(HttpContext.RequestAborted);
            while (await detailReader.ReadAsync(HttpContext.RequestAborted))
            {
                byte tipo = ReadByte(detailReader, "TipoProductoServicio");
                dto!.Partidas.Add(new OrdenCompraRecepcionPartidaPendienteDto
                {
                    IdOrdenCompraDetalle = ReadGuid(detailReader, "id"),
                    NumeroPartida = ReadInt(detailReader, "NumeroPartida"),
                    TipoPartida = tipo,
                    TipoPartidaNombre = tipo == 2 ? "Servicio" : "Producto",
                    IdProductoServicio = ReadGuid(detailReader, "idProductoServicio"),
                    IdVariante = ReadNullableGuid(detailReader, "idVariante"),
                    IdPresentacionCompra = ReadNullableGuid(detailReader, "idPresentacionCompra"),
                    Producto = ReadString(detailReader, "Producto"),
                    Variante = ReadString(detailReader, "Variante"),
                    PresentacionCompra = ReadString(detailReader, "PresentacionCompraSnapshot"),
                    FactorConversionSnapshot = ReadDecimal(detailReader, "FactorConversionSnapshot"),
                    CantidadBaseOrdenada = ReadDecimal(detailReader, "CantidadBaseOrdenada"),
                    CantidadBaseRecibidaAcumulada = ReadDecimal(detailReader, "CantidadBaseRecibidaAcumulada"),
                    CantidadBasePendiente = ReadDecimal(detailReader, "CantidadBasePendiente"),
                    CausaInventario = ReadBool(detailReader, "CausaInventario"),
                    UsaNumeroSerie = ReadBool(detailReader, "UsaNumeroSerie")
                });
            }

            return dto;
        }

        private async Task<RecepcionDetalleDto?> LoadRecepcionAsync(TenantDatabaseDescriptor descriptor, Guid idRecepcion)
        {
            await using SqlConnection connection = _connectionFactory.CreateConnection(descriptor);
            await connection.OpenAsync(HttpContext.RequestAborted);
            using SqlCommand header = new(@"
SELECT
    r.id, r.FolioRecepcion, r.idOrdenCompra, ISNULL(oc.Folio, '') AS FolioOrdenCompra,
    r.idSucursal, ISNULL(s.Nombre, '') AS Sucursal, r.FechaRecepcion, r.Estado,
    r.OperationKey, ISNULL(r.Observaciones, '') AS Observaciones
FROM dbo.Recepciones r
INNER JOIN dbo.OrdenesCompra oc ON oc.idEmpresa = r.idEmpresa AND oc.id = r.idOrdenCompra
LEFT JOIN dbo.Sucursales s ON s.idEmpresa = r.idEmpresa AND s.id = r.idSucursal
WHERE r.idEmpresa = @IdEmpresa AND r.id = @IdRecepcion", connection);
            header.Parameters.AddWithValue("@IdEmpresa", descriptor.IdEmpresa);
            header.Parameters.AddWithValue("@IdRecepcion", idRecepcion);

            RecepcionDetalleDto? dto = null;
            using (SqlDataReader reader = await header.ExecuteReaderAsync(HttpContext.RequestAborted))
            {
                if (!await reader.ReadAsync(HttpContext.RequestAborted))
                {
                    return null;
                }

                byte estado = ReadByte(reader, "Estado");
                dto = new RecepcionDetalleDto
                {
                    Id = ReadGuid(reader, "id"),
                    FolioRecepcion = ReadString(reader, "FolioRecepcion"),
                    IdOrdenCompra = ReadGuid(reader, "idOrdenCompra"),
                    FolioOrdenCompra = ReadString(reader, "FolioOrdenCompra"),
                    IdSucursal = ReadGuid(reader, "idSucursal"),
                    Sucursal = ReadString(reader, "Sucursal"),
                    FechaRecepcion = ReadDateTime(reader, "FechaRecepcion"),
                    Estado = estado,
                    EstadoNombre = GetRecepcionEstadoNombre(estado),
                    OperationKey = ReadString(reader, "OperationKey"),
                    Observaciones = ReadString(reader, "Observaciones")
                };
            }

            using SqlCommand details = new(@"
SELECT
    p.id, p.idOrdenCompraDetalle, p.NumeroPartida, p.TipoPartida, p.idProductoServicio, p.idVariante,
    p.CantidadCompraRecibida, p.CantidadBaseEstaRecepcion, p.CantidadBaseRecibidaAnterior,
    p.CantidadBaseRecibidaAcumulada, p.CantidadBasePendiente, p.ControlSerie
FROM dbo.RecepcionPartidas p
WHERE p.idEmpresa = @IdEmpresa AND p.idRecepcion = @IdRecepcion
ORDER BY p.NumeroPartida", connection);
            details.Parameters.AddWithValue("@IdEmpresa", descriptor.IdEmpresa);
            details.Parameters.AddWithValue("@IdRecepcion", idRecepcion);

            Dictionary<Guid, RecepcionPartidaDto> partidas = new();
            using (SqlDataReader reader = await details.ExecuteReaderAsync(HttpContext.RequestAborted))
            {
                while (await reader.ReadAsync(HttpContext.RequestAborted))
                {
                    Guid partidaId = ReadGuid(reader, "id");
                    byte tipo = ReadByte(reader, "TipoPartida");
                    RecepcionPartidaDto partida = new()
                    {
                        Id = partidaId,
                        IdOrdenCompraDetalle = ReadGuid(reader, "idOrdenCompraDetalle"),
                        NumeroPartida = ReadInt(reader, "NumeroPartida"),
                        TipoPartida = tipo,
                        TipoPartidaNombre = tipo == 2 ? "Servicio" : "Producto",
                        IdProductoServicio = ReadGuid(reader, "idProductoServicio"),
                        IdVariante = ReadNullableGuid(reader, "idVariante"),
                        CantidadCompraRecibida = ReadDecimal(reader, "CantidadCompraRecibida"),
                        CantidadBaseEstaRecepcion = ReadDecimal(reader, "CantidadBaseEstaRecepcion"),
                        CantidadBaseRecibidaAnterior = ReadDecimal(reader, "CantidadBaseRecibidaAnterior"),
                        CantidadBaseRecibidaAcumulada = ReadDecimal(reader, "CantidadBaseRecibidaAcumulada"),
                        CantidadBasePendiente = ReadDecimal(reader, "CantidadBasePendiente"),
                        ControlSerie = ReadBool(reader, "ControlSerie")
                    };
                    partidas[partidaId] = partida;
                    dto!.Partidas.Add(partida);
                }
            }

            using SqlCommand series = new("SELECT idRecepcionPartida, NumeroSerie FROM dbo.RecepcionSeries WHERE idEmpresa = @IdEmpresa AND idRecepcion = @IdRecepcion ORDER BY NumeroSerie", connection);
            series.Parameters.AddWithValue("@IdEmpresa", descriptor.IdEmpresa);
            series.Parameters.AddWithValue("@IdRecepcion", idRecepcion);
            using SqlDataReader seriesReader = await series.ExecuteReaderAsync(HttpContext.RequestAborted);
            while (await seriesReader.ReadAsync(HttpContext.RequestAborted))
            {
                Guid partidaId = ReadGuid(seriesReader, "idRecepcionPartida");
                if (partidas.TryGetValue(partidaId, out RecepcionPartidaDto? partida))
                {
                    partida.Series.Add(ReadString(seriesReader, "NumeroSerie"));
                }
            }

            return dto;
        }

        private Guid TryResolveEmpresaId()
        {
            foreach (string claimKey in EmpresaClaimKeys)
            {
                if (Guid.TryParse(User.FindFirstValue(claimKey), out Guid parsed) && parsed != Guid.Empty)
                {
                    return parsed;
                }
            }

            return Guid.Empty;
        }

        private string TryResolveEmpresaKey(Guid empresaId)
        {
            foreach (string claimKey in EmpresaKeyClaimKeys)
            {
                string? value = User.FindFirstValue(claimKey);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim().ToUpperInvariant();
                }
            }

            return empresaId == Guid.Empty ? string.Empty : empresaId.ToString("N").ToUpperInvariant();
        }

        private string TryResolveUsuarioId()
            => UsuarioClaimKeys
                .Select(key => User.FindFirstValue(key))
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;

        private Guid? TryResolveUsuarioGuid()
            => Guid.TryParse(TryResolveUsuarioId(), out Guid parsed) && parsed != Guid.Empty ? parsed : null;

        private IActionResult HandleException(Exception ex, string operation)
        {
            _logger.LogError(ex, "Error en Recepcion.{Operation}", operation);
            string message = ex is InvalidOperationException ? ex.Message : "Ocurrió un error al procesar la solicitud.";
            return StatusCode(500, new RecepcionOperacionResponse { Mensaje = message });
        }

        private static string GetOrdenEstadoNombre(byte estado)
            => estado switch
            {
                2 => "Generada",
                4 => "Parcialmente recibida",
                5 => "Recibida",
                _ => "No disponible"
            };

        private static string GetRecepcionEstadoNombre(byte estado)
            => estado switch
            {
                1 => "Borrador",
                2 => "Confirmada",
                3 => "Cancelada",
                _ => "No disponible"
            };

        private static Guid ReadGuid(SqlDataReader reader, string column) => reader.GetGuid(reader.GetOrdinal(column));
        private static Guid? ReadNullableGuid(SqlDataReader reader, string column) => reader.IsDBNull(reader.GetOrdinal(column)) ? null : reader.GetGuid(reader.GetOrdinal(column));
        private static string ReadString(SqlDataReader reader, string column) => reader.IsDBNull(reader.GetOrdinal(column)) ? string.Empty : reader.GetString(reader.GetOrdinal(column));
        private static byte ReadByte(SqlDataReader reader, string column) => reader.GetByte(reader.GetOrdinal(column));
        private static int ReadInt(SqlDataReader reader, string column) => reader.GetInt32(reader.GetOrdinal(column));
        private static bool ReadBool(SqlDataReader reader, string column) => reader.GetBoolean(reader.GetOrdinal(column));
        private static decimal ReadDecimal(SqlDataReader reader, string column) => reader.GetDecimal(reader.GetOrdinal(column));
        private static DateTime ReadDateTime(SqlDataReader reader, string column) => reader.GetDateTime(reader.GetOrdinal(column));
    }
}
