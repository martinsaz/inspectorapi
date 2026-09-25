using System.Security.Claims;
using checklistWs.Services.Tenant;
using Microsoft.AspNetCore.Mvc;

namespace checklistWs.Controllers.Curvas
{
    [Route("api/[controller]")]
    [ApiController]
    public sealed class CurvasSugerenciasController : ControllerBase
    {
        private static readonly string[] EmpresaClaimKeys = { "idEmpresa", "empresaId", "tenantId", "companyId", "tenant", "idempresa" };
        private static readonly string[] EmpresaKeyClaimKeys = { "empresa", "empresaNombre", "tenantName", "companyName", "nombreEmpresa" };
        private static readonly string[] UsuarioClaimKeys = { ClaimTypes.NameIdentifier, "sub", "idUsuario", "userid", "uid" };

        private readonly ITenantDatabaseResolver _tenantResolver;
        private readonly IProductosServiciosAuthorizationService _authorizationService;
        private readonly IProductosServiciosCompatibilityGate _compatibilityGate;
        private readonly ICurvasSugerenciasMotorService _motorService;
        private readonly ILogger<CurvasSugerenciasController> _logger;

        public CurvasSugerenciasController(
            ITenantDatabaseResolver tenantResolver,
            IProductosServiciosAuthorizationService authorizationService,
            IProductosServiciosCompatibilityGate compatibilityGate,
            ICurvasSugerenciasMotorService motorService,
            ILogger<CurvasSugerenciasController> logger)
        {
            _tenantResolver = tenantResolver;
            _authorizationService = authorizationService;
            _compatibilityGate = compatibilityGate;
            _motorService = motorService;
            _logger = logger;
        }

        [HttpPost("Preview")]
        public async Task<IActionResult> Preview([FromBody] CurvasSugerenciaPreviewRequest request)
        {
            TenantDatabaseDescriptor? descriptor = await ResolveDescriptorAsync(request?.IdEmpresa ?? Guid.Empty, request?.EmpresaKey ?? string.Empty);
            if (descriptor == null)
            {
                return Unauthorized(new CurvasSugerenciaPreviewResponse { Mensaje = "No fue posible resolver la empresa activa." });
            }

            IActionResult? guard = await GuardAsync(
                descriptor,
                ProductosServiciosAuthorizationDefaults.OrdenesCompraNuevaPermissionCode,
                ProductosServiciosPermissionRequirement.Read,
                DatabaseScopes.Curvas,
                DatabaseScopes.Inventario,
                DatabaseScopes.OrdenesCompra,
                DatabaseScopes.Recepcion);
            if (guard != null)
            {
                return guard;
            }

            try
            {
                CurvasSugerenciaPreviewResponse response = await _motorService.PreviewAsync(
                    descriptor,
                    descriptor.IdEmpresa,
                    request!.Items,
                    HttpContext.RequestAborted);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Preview");
            }
        }

        private async Task<IActionResult?> GuardAsync(TenantDatabaseDescriptor descriptor, string permissionCode, ProductosServiciosPermissionRequirement requirement, params string[] scopes)
        {
            string userId = TryResolveUsuarioId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new CurvasSugerenciaPreviewResponse { Mensaje = "No fue posible resolver el usuario activo." });
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
                return StatusCode(403, new CurvasSugerenciaPreviewResponse { Mensaje = $"No tienes permiso para calcular sugerencias. Ref: {decision.ReferenceId}" });
            }

            foreach (string scope in scopes.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                CompatibilityDecision compatibility = await _compatibilityGate.EvaluateAsync(descriptor, scope, HttpContext.RequestAborted);
                if (!compatibility.IsAllowed)
                {
                    return StatusCode(503, new CurvasSugerenciaPreviewResponse { Mensaje = $"Curvas no disponible para {scope}. Ref: {compatibility.ReferenceId}" });
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
                _logger.LogWarning(ex, "No fue posible resolver tenant para Curvas. IdEmpresa={IdEmpresa}", effectiveEmpresaId);
                return null;
            }
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

        private IActionResult HandleException(Exception ex, string operation)
        {
            _logger.LogError(ex, "Error en CurvasSugerencias.{Operation}", operation);
            string message = ex is InvalidOperationException ? ex.Message : "Ocurrió un error al procesar la solicitud.";
            return StatusCode(500, new CurvasSugerenciaPreviewResponse { Mensaje = message });
        }
    }
}
