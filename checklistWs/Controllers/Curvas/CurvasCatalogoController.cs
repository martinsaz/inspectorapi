using System.Security.Claims;
using checklistWs.Services.Tenant;
using Microsoft.AspNetCore.Mvc;

namespace checklistWs.Controllers.Curvas
{
    [Route("api/[controller]")]
    [ApiController]
    public sealed class CurvasCatalogoController : ControllerBase
    {
        private static readonly string[] EmpresaClaimKeys = { "idEmpresa", "empresaId", "tenantId", "companyId", "tenant", "idempresa" };
        private static readonly string[] EmpresaKeyClaimKeys = { "empresa", "empresaNombre", "tenantName", "companyName", "nombreEmpresa" };
        private static readonly string[] UsuarioClaimKeys = { ClaimTypes.NameIdentifier, "sub", "idUsuario", "userid", "uid" };
        private const string ProxyEmpresaIdHeader = "X-ProductosServicios-Proxy-EmpresaId";
        private const string ProxyEmpresaKeyHeader = "X-ProductosServicios-Proxy-Empresa";
        private const string ProxyUsuarioIdHeader = "X-ProductosServicios-Proxy-UsuarioId";
        private const string LegacyProxyEmpresaIdHeader = "X-CheckApp-IdEmpresa";
        private const string LegacyProxyEmpresaKeyHeader = "X-CheckApp-Empresa";
        private const string LegacyProxyUsuarioIdHeader = "X-CheckApp-UsuarioId";

        private readonly ITenantDatabaseResolver _tenantResolver;
        private readonly IProductosServiciosAuthorizationService _authorizationService;
        private readonly IProductosServiciosCompatibilityGate _compatibilityGate;
        private readonly IProductosServiciosSchemaBootstrapper _schemaBootstrapper;
        private readonly IDatabaseIdentityResolver _identityResolver;
        private readonly ICurvasScopeService _curvasService;
        private readonly ILogger<CurvasCatalogoController> _logger;

        public CurvasCatalogoController(
            ITenantDatabaseResolver tenantResolver,
            IProductosServiciosAuthorizationService authorizationService,
            IProductosServiciosCompatibilityGate compatibilityGate,
            IProductosServiciosSchemaBootstrapper schemaBootstrapper,
            IDatabaseIdentityResolver identityResolver,
            ICurvasScopeService curvasService,
            ILogger<CurvasCatalogoController> logger)
        {
            _tenantResolver = tenantResolver;
            _authorizationService = authorizationService;
            _compatibilityGate = compatibilityGate;
            _schemaBootstrapper = schemaBootstrapper;
            _identityResolver = identityResolver;
            _curvasService = curvasService;
            _logger = logger;
        }

        [HttpGet("Listar")]
        public async Task<IActionResult> Listar([FromQuery] Guid idEmpresa, [FromQuery] string empresaKey = "", [FromQuery] string busqueda = "", [FromQuery] string estatus = "activos")
        {
            TenantDatabaseDescriptor? descriptor = await ResolveDescriptorAsync(idEmpresa, empresaKey);
            IActionResult? guard = await GuardAsync(descriptor, ProductosServiciosPermissionRequirement.Read);
            if (guard != null) return guard;

            IReadOnlyList<CurvasCatalogoItemDto> items = await _curvasService.ListarCatalogoAsync(descriptor!, descriptor!.IdEmpresa, new CurvasCatalogoListRequest { Busqueda = busqueda, Estatus = estatus }, HttpContext.RequestAborted);
            return Ok(items);
        }

        [HttpGet("Detalle")]
        public async Task<IActionResult> Detalle([FromQuery] Guid idEmpresa, [FromQuery] Guid id, [FromQuery] string empresaKey = "")
        {
            TenantDatabaseDescriptor? descriptor = await ResolveDescriptorAsync(idEmpresa, empresaKey);
            IActionResult? guard = await GuardAsync(descriptor, ProductosServiciosPermissionRequirement.Read);
            if (guard != null) return guard;

            CurvasCatalogoDetalleDto? curva = await _curvasService.ObtenerCatalogoAsync(descriptor!, descriptor!.IdEmpresa, id, HttpContext.RequestAborted);
            return curva == null ? NotFound(BuildResponse(false, "La curva no está disponible.")) : Ok(curva);
        }

        [HttpPost("Guardar")]
        public async Task<IActionResult> Guardar([FromBody] CurvasCatalogoGuardarRequest request)
        {
            TenantDatabaseDescriptor? descriptor = await ResolveDescriptorAsync(Guid.Empty, string.Empty);
            IActionResult? guard = await GuardAsync(descriptor, ProductosServiciosPermissionRequirement.Write);
            if (guard != null) return guard;

            request.UsuarioId = TryResolveUsuarioGuid();
            try
            {
                Guid id = await _curvasService.GuardarCatalogoAsync(descriptor!, descriptor!.IdEmpresa, request, HttpContext.RequestAborted);
                return Ok(BuildResponse(true, "Curva guardada.", id));
            }
            catch (Exception ex) when (ex is InvalidOperationException)
            {
                return BadRequest(BuildResponse(false, ex.Message));
            }
        }

        [HttpPost("Baja")]
        public async Task<IActionResult> Baja([FromQuery] Guid id)
        {
            TenantDatabaseDescriptor? descriptor = await ResolveDescriptorAsync(Guid.Empty, string.Empty);
            IActionResult? guard = await GuardAsync(descriptor, ProductosServiciosPermissionRequirement.Write);
            if (guard != null) return guard;

            try
            {
                await _curvasService.BajaCatalogoAsync(descriptor!, descriptor!.IdEmpresa, id, TryResolveUsuarioGuid(), HttpContext.RequestAborted);
                return Ok(BuildResponse(true, "Curva dada de baja.", id));
            }
            catch (Exception ex) when (ex is InvalidOperationException)
            {
                return BadRequest(BuildResponse(false, ex.Message));
            }
        }

        [HttpPost("Reactivar")]
        public async Task<IActionResult> Reactivar([FromQuery] Guid id)
        {
            TenantDatabaseDescriptor? descriptor = await ResolveDescriptorAsync(Guid.Empty, string.Empty);
            IActionResult? guard = await GuardAsync(descriptor, ProductosServiciosPermissionRequirement.Write);
            if (guard != null) return guard;

            try
            {
                await _curvasService.ReactivarCatalogoAsync(descriptor!, descriptor!.IdEmpresa, id, TryResolveUsuarioGuid(), HttpContext.RequestAborted);
                return Ok(BuildResponse(true, "Curva reactivada.", id));
            }
            catch (Exception ex) when (ex is InvalidOperationException)
            {
                return BadRequest(BuildResponse(false, ex.Message));
            }
        }

        [HttpGet("ProductosElegibles")]
        public async Task<IActionResult> ProductosElegibles([FromQuery] Guid idEmpresa, [FromQuery] string empresaKey = "", [FromQuery] string busqueda = "")
        {
            TenantDatabaseDescriptor? descriptor = await ResolveDescriptorAsync(idEmpresa, empresaKey);
            IActionResult? guard = await GuardAsync(descriptor, ProductosServiciosPermissionRequirement.Read);
            if (guard != null) return guard;

            return Ok(await _curvasService.BuscarProductosElegiblesAsync(descriptor!, descriptor!.IdEmpresa, busqueda, HttpContext.RequestAborted));
        }

        [HttpGet("Variantes")]
        public async Task<IActionResult> Variantes([FromQuery] Guid idEmpresa, [FromQuery] Guid idProductoServicio, [FromQuery] string empresaKey = "")
        {
            TenantDatabaseDescriptor? descriptor = await ResolveDescriptorAsync(idEmpresa, empresaKey);
            IActionResult? guard = await GuardAsync(descriptor, ProductosServiciosPermissionRequirement.Read);
            if (guard != null) return guard;

            try
            {
                return Ok(await _curvasService.ObtenerVariantesAsync(descriptor!, descriptor!.IdEmpresa, idProductoServicio, HttpContext.RequestAborted));
            }
            catch (Exception ex) when (ex is InvalidOperationException)
            {
                return BadRequest(BuildResponse(false, ex.Message));
            }
        }

        private async Task<IActionResult?> GuardAsync(TenantDatabaseDescriptor? descriptor, ProductosServiciosPermissionRequirement requirement)
        {
            if (descriptor == null)
            {
                return Unauthorized(BuildResponse(false, "No fue posible resolver la empresa activa."));
            }

            string userId = TryResolveUsuarioId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(BuildResponse(false, "No fue posible resolver el usuario activo."));
            }

            ProductosServiciosAuthorizationDecision decision = await _authorizationService.AuthorizeAsync(new ProductosServiciosAuthorizationRequest
            {
                IdEmpresa = descriptor.IdEmpresa,
                UserId = userId,
                PermissionCode = ProductosServiciosAuthorizationDefaults.CurvasCatalogoPermissionCode,
                Requirement = requirement,
                TenantDatabase = descriptor
            }, HttpContext.RequestAborted);

            if (!decision.IsAllowed(requirement))
            {
                return StatusCode(403, BuildResponse(false, $"No tienes permiso para administrar curvas. Ref: {decision.ReferenceId}"));
            }

            CompatibilityDecision compatibility = await EnsureCurvasCompatibilityAsync(descriptor, HttpContext.RequestAborted);
            if (!compatibility.IsAllowed)
            {
                return StatusCode(503, BuildResponse(false, $"Curvas no disponible. Ref: {compatibility.ReferenceId}"));
            }

            return null;
        }

        private async Task<CompatibilityDecision> EnsureCurvasCompatibilityAsync(TenantDatabaseDescriptor descriptor, CancellationToken cancellationToken)
        {
            CompatibilityDecision compatibility = await _compatibilityGate.EvaluateAsync(descriptor, DatabaseScopes.Curvas, cancellationToken);
            if (!string.Equals(compatibility.ReasonCode, "SCHEMA_EMPTY", StringComparison.OrdinalIgnoreCase))
            {
                return compatibility;
            }

            DatabaseIdentity identity = await _identityResolver.ResolveAsync(descriptor, cancellationToken);
            SchemaProvisionResult provision = await _schemaBootstrapper.ProvisionScopeAsync(descriptor, identity, DatabaseScopes.Curvas, cancellationToken);
            if (provision.Status is SchemaProvisionResultStatus.Provisioned ||
                string.Equals(provision.ReasonCode, "ALREADY_PROVISIONED", StringComparison.OrdinalIgnoreCase))
            {
                return await _compatibilityGate.EvaluateAsync(descriptor, DatabaseScopes.Curvas, cancellationToken);
            }

            _logger.LogWarning("Curvas schema provision bloqueado. ReferenceId={ReferenceId} Scope={Scope} ReasonCode={ReasonCode}",
                compatibility.ReferenceId,
                DatabaseScopes.Curvas,
                provision.ReasonCode);
            return compatibility;
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
                _logger.LogWarning(ex, "No fue posible resolver tenant para Catalogo de Curvas. IdEmpresa={IdEmpresa}", effectiveEmpresaId);
                return null;
            }
        }

        private Guid TryResolveEmpresaId()
        {
            string? proxyEmpresaId = FirstHeaderValue(ProxyEmpresaIdHeader, LegacyProxyEmpresaIdHeader);
            if (Guid.TryParse(proxyEmpresaId, out Guid headerParsed) && headerParsed != Guid.Empty)
            {
                return headerParsed;
            }

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
            string? proxyEmpresaKey = FirstHeaderValue(ProxyEmpresaKeyHeader, LegacyProxyEmpresaKeyHeader);
            if (!string.IsNullOrWhiteSpace(proxyEmpresaKey))
            {
                return proxyEmpresaKey.Trim().ToUpperInvariant();
            }

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
        {
            string? proxyUsuarioId = FirstHeaderValue(ProxyUsuarioIdHeader, LegacyProxyUsuarioIdHeader);
            if (!string.IsNullOrWhiteSpace(proxyUsuarioId))
            {
                return proxyUsuarioId.Trim();
            }

            return UsuarioClaimKeys.Select(key => User.FindFirstValue(key)).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
        }

        private Guid? TryResolveUsuarioGuid()
            => Guid.TryParse(TryResolveUsuarioId(), out Guid id) && id != Guid.Empty ? id : null;

        private string? FirstHeaderValue(params string[] headerNames)
        {
            foreach (string headerName in headerNames)
            {
                if (Request.Headers.TryGetValue(headerName, out var values))
                {
                    string? value = values.FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value.Trim();
                    }
                }
            }

            return null;
        }

        private static CurvasCatalogoOperacionResponse BuildResponse(bool exito, string mensaje, Guid? id = null)
            => new() { Exito = exito, Mensaje = mensaje, Id = id };
    }
}
