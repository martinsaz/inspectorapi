using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace checklistWs.Services.Tenant
{
    public sealed class SucursalesScopeRequestContext
    {
        public Guid IdEmpresa { get; init; }
        public string EmpresaKey { get; init; } = string.Empty;
        public string UserId { get; init; } = string.Empty;
        public TenantDatabaseDescriptor TenantDatabase { get; init; } = null!;
    }

    public interface ISucursalesScopeRequestContextResolver
    {
        Task<SucursalesScopeRequestContext?> TryResolveAsync(
            ControllerBase controller,
            string permissionCode,
            ProductosServiciosPermissionRequirement requirement,
            CancellationToken cancellationToken = default);

        SqlConnection CreateConnection(SucursalesScopeRequestContext context);
        IActionResult ToErrorResult(ControllerBase controller, SucursalesScopeRequestContext? context = null);
    }

    public sealed class SucursalesScopeRequestContextResolver : ISucursalesScopeRequestContextResolver
    {
        private readonly IConfiguration _configuration;
        private readonly ITenantDatabaseResolver _tenantDatabaseResolver;
        private readonly ITenantSqlConnectionFactory _connectionFactory;
        private readonly IProductosServiciosAuthorizationService _authorizationService;
        private readonly IProductosServiciosCompatibilityGate _compatibilityGate;
        private readonly ILogger<SucursalesScopeRequestContextResolver> _logger;
        private IActionResult? _lastError;

        public SucursalesScopeRequestContextResolver(
            IConfiguration configuration,
            ITenantDatabaseResolver tenantDatabaseResolver,
            ITenantSqlConnectionFactory connectionFactory,
            IProductosServiciosAuthorizationService authorizationService,
            IProductosServiciosCompatibilityGate compatibilityGate,
            ILogger<SucursalesScopeRequestContextResolver> logger)
        {
            _configuration = configuration;
            _tenantDatabaseResolver = tenantDatabaseResolver;
            _connectionFactory = connectionFactory;
            _authorizationService = authorizationService;
            _compatibilityGate = compatibilityGate;
            _logger = logger;
        }

        public async Task<SucursalesScopeRequestContext?> TryResolveAsync(
            ControllerBase controller,
            string permissionCode,
            ProductosServiciosPermissionRequirement requirement,
            CancellationToken cancellationToken = default)
        {
            _lastError = null;
            if (!ProductosServiciosIdentityValidation.TryValidate(
                controller.User,
                controller.Request.Headers,
                _configuration["fireBdata:fireClave"] ?? string.Empty,
                DateTimeOffset.UtcNow,
                out TenantDatabaseContext verified,
                out string userId))
            {
                _lastError = controller.Unauthorized(new { code = "IDENTITY_NOT_VERIFIED", message = "No fue posible verificar la identidad de la solicitud." });
                return null;
            }

            TenantDatabaseDescriptor tenantDatabase;
            try
            {
                tenantDatabase = await _tenantDatabaseResolver.ResolveAsync(verified, cancellationToken);
            }
            catch (TenantDatabaseResolutionException ex)
            {
                _logger.LogWarning(ex, "No fue posible resolver la base tenant para Sucursales. Code={Code}", ex.Code);
                _lastError = controller.StatusCode(503, new { code = ex.Code.ToString(), message = "No fue posible resolver la base de datos de la empresa." });
                return null;
            }

            ProductosServiciosAuthorizationDecision authorization = await _authorizationService.AuthorizeAsync(
                new ProductosServiciosAuthorizationRequest
                {
                    IdEmpresa = verified.IdEmpresa,
                    UserId = userId,
                    PermissionCode = permissionCode,
                    Requirement = requirement,
                    TenantDatabase = tenantDatabase
                },
                cancellationToken);

            if (!authorization.IsAllowed(requirement))
            {
                _lastError = controller.StatusCode(403, new
                {
                    code = authorization.ReasonCode,
                    message = "No tienes permiso para realizar esta operación.",
                    referenceId = authorization.ReferenceId
                });
                return null;
            }

            CompatibilityDecision compatibility = await _compatibilityGate.EvaluateAsync(
                tenantDatabase,
                DatabaseScopes.Sucursales,
                cancellationToken);

            if (!compatibility.IsAllowed)
            {
                _logger.LogWarning("Gate Sucursales bloqueó operación. ReferenceId={ReferenceId} IdentityHash={IdentityHash} ReasonCode={ReasonCode}",
                    compatibility.ReferenceId,
                    Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(compatibility.SanitizedIdentity ?? string.Empty))),
                    compatibility.ReasonCode);

                _lastError = controller.StatusCode(503, new
                {
                    code = compatibility.ReasonCode,
                    message = "Sucursales no está disponible temporalmente para esta empresa.",
                    referenceId = compatibility.ReferenceId
                });
                return null;
            }

            return new SucursalesScopeRequestContext
            {
                IdEmpresa = verified.IdEmpresa,
                EmpresaKey = verified.EmpresaKey,
                UserId = userId,
                TenantDatabase = tenantDatabase
            };
        }

        public SqlConnection CreateConnection(SucursalesScopeRequestContext context)
            => _connectionFactory.CreateConnection(context.TenantDatabase);

        public IActionResult ToErrorResult(ControllerBase controller, SucursalesScopeRequestContext? context = null)
            => _lastError ?? controller.StatusCode(503, new { code = "SUCURSALES_SCOPE_UNAVAILABLE", message = "Sucursales no está disponible temporalmente." });
    }
}
