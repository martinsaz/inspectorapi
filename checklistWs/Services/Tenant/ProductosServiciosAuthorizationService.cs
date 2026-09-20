using System.Data;
using System.Data.SqlClient;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace checklistWs.Services.Tenant
{
    public sealed class ProductosServiciosAuthorizationService : IProductosServiciosAuthorizationService
    {
        private readonly ITenantSqlConnectionFactory _connectionFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ProductosServiciosAuthorizationService> _logger;

        public ProductosServiciosAuthorizationService(
            ITenantSqlConnectionFactory connectionFactory,
            IConfiguration configuration,
            ILogger<ProductosServiciosAuthorizationService> logger)
        {
            _connectionFactory = connectionFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<ProductosServiciosAuthorizationDecision> AuthorizeAsync(
            ProductosServiciosAuthorizationRequest request,
            CancellationToken cancellationToken = default)
        {
            string referenceId = Guid.NewGuid().ToString("N");
            string permissionCode = ResolvePermissionCode(request.PermissionCode);
            if (request.TenantDatabase == null ||
                request.IdEmpresa == Guid.Empty ||
                string.IsNullOrWhiteSpace(request.UserId))
            {
                return Deny(permissionCode, "AUTH_CONTEXT_INVALID", referenceId);
            }

            try
            {
                using SqlConnection connection = CreateAuthorizationConnection(request.TenantDatabase);
                await connection.OpenAsync(cancellationToken);

                using SqlCommand command = new SqlCommand(@"
SELECT TOP (1)
    r.NombreRol,
    r.Permisos
FROM dbo.Usuarios u
INNER JOIN dbo.Roles r
    ON r.idEmpresa = u.idEmpresa
   AND r.id = u.idRol
WHERE u.idEmpresa = @IdEmpresa
  AND r.idEmpresa = @IdEmpresa
  AND (
        u.idFirebase = @UserId
        OR CONVERT(nvarchar(36), u.id) = @UserId
      )
  AND ISNULL(u.borrado, 0) = 0", connection);

                command.Parameters.Add("@IdEmpresa", SqlDbType.UniqueIdentifier).Value = request.IdEmpresa;
                command.Parameters.Add("@UserId", SqlDbType.NVarChar, 256).Value = request.UserId.Trim();

                string nombreRol = string.Empty;
                string permisos = string.Empty;
                using (SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken))
                {
                    if (await reader.ReadAsync(cancellationToken))
                    {
                        nombreRol = reader["NombreRol"] != DBNull.Value ? reader["NombreRol"].ToString()?.Trim() ?? string.Empty : string.Empty;
                        permisos = reader["Permisos"] != DBNull.Value ? reader["Permisos"].ToString()?.Trim() ?? string.Empty : string.Empty;
                    }
                }

                if (string.Equals(nombreRol, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
                {
                    return new ProductosServiciosAuthorizationDecision
                    {
                        HasAccess = true,
                        CanWrite = true,
                        PermissionCode = permissionCode,
                        ReasonCode = "ALLOW_SUPERADMIN",
                        ReferenceId = referenceId
                    };
                }

                if (string.IsNullOrWhiteSpace(permisos))
                {
                    return Deny(permissionCode, "ROLE_PERMISSION_NOT_FOUND", referenceId);
                }

                LegacyPermissionNode? node = FindPermission(permisos, permissionCode);
                if (node == null)
                {
                    return Deny(permissionCode, "PERMISSION_NOT_FOUND", referenceId);
                }

                bool hasAccess = node.Permisos?.Acceso == 1;
                bool accessOnlyPermission = IsAccessOnlyPermission(permissionCode);
                bool canWrite = hasAccess && !accessOnlyPermission && node.Permisos?.Escritura == 1;
                string reason = hasAccess
                    ? canWrite ? "ALLOW_WRITE" : accessOnlyPermission ? "ALLOW_READ_ACCESS_ONLY" : "ALLOW_READ"
                    : "ACCESS_DENIED";

                return new ProductosServiciosAuthorizationDecision
                {
                    HasAccess = hasAccess,
                    CanWrite = canWrite,
                    PermissionCode = permissionCode,
                    ReasonCode = reason,
                    ReferenceId = referenceId
                };
            }
            catch (Exception ex) when (ex is JsonException || ex is SqlException || ex is InvalidOperationException)
            {
                _logger.LogWarning("AuthZ ProductosServicios falló cerrado. ReferenceId={ReferenceId} ReasonCode={ReasonCode}", referenceId, "AUTHZ_SOURCE_FAILED");
                return Deny(permissionCode, "AUTHZ_SOURCE_FAILED", referenceId);
            }
        }

        private SqlConnection CreateAuthorizationConnection(TenantDatabaseDescriptor tenantDatabase)
        {
            string configured = ResolveAuthorizationConnectionString(_configuration);

            if (!string.IsNullOrWhiteSpace(configured))
            {
                return new SqlConnection(configured);
            }

            return _connectionFactory.CreateConnection(tenantDatabase);
        }

        public static string ResolveAuthorizationConnectionString(IConfiguration configuration)
        {
            return configuration["ProductosServicios:AuthorizationConnectionString"]
                ?? configuration.GetConnectionString("AuthorizationSqlServer")
                ?? configuration.GetConnectionString("CadenaConexionSQLServer")
                ?? string.Empty;
        }

        private string ResolvePermissionCode(string requestedCode)
        {
            string configured = _configuration[ProductosServiciosAuthorizationDefaults.PermissionCodeConfigurationKey] ?? string.Empty;
            string code = string.IsNullOrWhiteSpace(requestedCode) ||
                          string.Equals(requestedCode, ProductosServiciosAuthorizationDefaults.PermissionCode, StringComparison.OrdinalIgnoreCase)
                ? string.IsNullOrWhiteSpace(configured) ? requestedCode : configured
                : requestedCode;
            return string.IsNullOrWhiteSpace(code) ? ProductosServiciosAuthorizationDefaults.PermissionCode : code.Trim();
        }

        private static ProductosServiciosAuthorizationDecision Deny(string permissionCode, string reasonCode, string referenceId)
        {
            return new ProductosServiciosAuthorizationDecision
            {
                HasAccess = false,
                CanWrite = false,
                PermissionCode = permissionCode,
                ReasonCode = reasonCode,
                ReferenceId = referenceId
            };
        }

        private static LegacyPermissionNode? FindPermission(string json, string permissionCode)
        {
            List<LegacyPermissionNode>? roots = JsonSerializer.Deserialize<List<LegacyPermissionNode>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (roots == null)
            {
                return null;
            }

            foreach (LegacyPermissionNode root in roots)
            {
                LegacyPermissionNode? match = FindPermission(root, permissionCode);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static LegacyPermissionNode? FindPermission(LegacyPermissionNode node, string permissionCode)
        {
            if (string.Equals(node.Opcion, permissionCode, StringComparison.OrdinalIgnoreCase))
            {
                return node;
            }

            foreach (LegacyPermissionNode child in node.Hijos)
            {
                LegacyPermissionNode? match = FindPermission(child, permissionCode);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static bool IsAccessOnlyPermission(string permissionCode)
        {
            return string.Equals(permissionCode, "05000000", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(permissionCode, ProductosServiciosAuthorizationDefaults.ModulePermissionCode, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(permissionCode, ProductosServiciosAuthorizationDefaults.CatalogosPermissionCode, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(permissionCode, ProductosServiciosAuthorizationDefaults.AjustesPermissionCode, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(permissionCode, ProductosServiciosAuthorizationDefaults.SucursalesPermissionCode, StringComparison.OrdinalIgnoreCase);
        }

        private sealed class LegacyPermissionNode
        {
            public string Opcion { get; set; } = string.Empty;
            public LegacyPermissionValue? Permisos { get; set; }
            public List<LegacyPermissionNode> Hijos { get; set; } = new List<LegacyPermissionNode>();
        }

        private sealed class LegacyPermissionValue
        {
            public int Acceso { get; set; }
            public int Escritura { get; set; }
        }
    }
}
