using System.Security.Claims;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
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
        private static readonly TimeSpan ProxyHeaderTolerance = TimeSpan.FromMinutes(5);
        private const string ProxyEmpresaIdHeader = "X-ProductosServicios-Proxy-EmpresaId";
        private const string ProxyEmpresaKeyHeader = "X-ProductosServicios-Proxy-Empresa";
        private const string ProxyUsuarioIdHeader = "X-ProductosServicios-Proxy-UsuarioId";
        private const string ProxyTimestampHeader = "X-ProductosServicios-Proxy-Timestamp";
        private const string ProxySignatureHeader = "X-ProductosServicios-Proxy-Signature";

        private readonly ITenantDatabaseResolver _tenantResolver;
        private readonly IProductosServiciosAuthorizationService _authorizationService;
        private readonly IProductosServiciosCompatibilityGate _compatibilityGate;
        private readonly ICurvasSugerenciasMotorService _motorService;
        private readonly ILogger<CurvasSugerenciasController> _logger;
        private readonly IConfiguration _configuration;

        public CurvasSugerenciasController(
            ITenantDatabaseResolver tenantResolver,
            IProductosServiciosAuthorizationService authorizationService,
            IProductosServiciosCompatibilityGate compatibilityGate,
            ICurvasSugerenciasMotorService motorService,
            IConfiguration configuration,
            ILogger<CurvasSugerenciasController> logger)
        {
            _tenantResolver = tenantResolver;
            _authorizationService = authorizationService;
            _compatibilityGate = compatibilityGate;
            _motorService = motorService;
            _configuration = configuration;
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

        [HttpGet("Aplicables")]
        public async Task<IActionResult> Aplicables([FromQuery] Guid idEmpresa, [FromQuery] Guid idProductoServicio, [FromQuery] string empresaKey = "")
        {
            TenantDatabaseDescriptor? descriptor = await ResolveDescriptorAsync(idEmpresa, empresaKey);
            if (descriptor == null)
            {
                return Unauthorized(new { mensaje = "No fue posible resolver la empresa activa." });
            }

            IActionResult? guard = await GuardAsync(
                descriptor,
                ProductosServiciosAuthorizationDefaults.OrdenesCompraNuevaPermissionCode,
                ProductosServiciosPermissionRequirement.Read,
                DatabaseScopes.Curvas,
                DatabaseScopes.OrdenesCompra);
            if (guard != null) return guard;

            try
            {
                return Ok(await _motorService.ListarCurvasAplicablesAsync(descriptor, descriptor.IdEmpresa, idProductoServicio, HttpContext.RequestAborted));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Aplicables");
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
            if (TryResolveSignedProxyContext(out SignedProxyContext? proxyContext))
            {
                return proxyContext!.IdEmpresa;
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
            if (TryResolveSignedProxyContext(out SignedProxyContext? proxyContext))
            {
                return proxyContext!.EmpresaKey;
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
            if (TryResolveSignedProxyContext(out SignedProxyContext? proxyContext) &&
                !string.IsNullOrWhiteSpace(proxyContext!.UsuarioId))
            {
                return proxyContext.UsuarioId;
            }

            return UsuarioClaimKeys
                .Select(key => User.FindFirstValue(key))
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
        }

        private bool TryResolveSignedProxyContext(out SignedProxyContext? context)
        {
            context = null;
            if (!Request.Headers.TryGetValue(ProxyEmpresaIdHeader, out var empresaIdHeader) ||
                !Request.Headers.TryGetValue(ProxyEmpresaKeyHeader, out var empresaKeyHeader) ||
                !Request.Headers.TryGetValue(ProxyTimestampHeader, out var timestampHeader) ||
                !Request.Headers.TryGetValue(ProxySignatureHeader, out var signatureHeader))
            {
                return false;
            }

            string empresaIdRaw = empresaIdHeader.ToString().Trim();
            string empresaKeyRaw = empresaKeyHeader.ToString().Trim();
            string usuarioIdRaw = Request.Headers.TryGetValue(ProxyUsuarioIdHeader, out var usuarioIdHeader)
                ? usuarioIdHeader.ToString().Trim()
                : string.Empty;
            string timestampRaw = timestampHeader.ToString().Trim();
            string signatureRaw = signatureHeader.ToString().Trim();
            string secret = _configuration["fireBdata:fireClave"] ?? string.Empty;

            if (string.IsNullOrWhiteSpace(secret) ||
                !Guid.TryParse(empresaIdRaw, out Guid empresaId) || empresaId == Guid.Empty ||
                string.IsNullOrWhiteSpace(empresaKeyRaw) ||
                !DateTimeOffset.TryParseExact(timestampRaw, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset timestamp) ||
                (DateTimeOffset.UtcNow - timestamp.ToUniversalTime()).Duration() > ProxyHeaderTolerance)
            {
                return false;
            }

            string payload = string.Join('\n', empresaIdRaw, empresaKeyRaw.ToUpperInvariant(), usuarioIdRaw, timestampRaw);
            using HMACSHA256 hmac = new(Encoding.UTF8.GetBytes(secret));
            string expected = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
            byte[] expectedBytes = Encoding.UTF8.GetBytes(expected);
            byte[] actualBytes = Encoding.UTF8.GetBytes(signatureRaw);
            if (expectedBytes.Length != actualBytes.Length || !CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes))
            {
                return false;
            }

            context = new SignedProxyContext(empresaId, empresaKeyRaw.ToUpperInvariant(), usuarioIdRaw);
            return true;
        }

        private sealed record SignedProxyContext(Guid IdEmpresa, string EmpresaKey, string UsuarioId);

        private IActionResult HandleException(Exception ex, string operation)
        {
            _logger.LogError(ex, "Error en CurvasSugerencias.{Operation}", operation);
            string message = ex is InvalidOperationException ? ex.Message : "Ocurrió un error al procesar la solicitud.";
            return StatusCode(500, new CurvasSugerenciaPreviewResponse { Mensaje = message });
        }
    }
}
