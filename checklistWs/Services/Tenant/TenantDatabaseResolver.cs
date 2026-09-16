using System.Data.SqlClient;

namespace checklistWs.Services.Tenant
{
    public sealed class TenantDatabaseResolver : ITenantDatabaseResolver
    {
        private readonly ITenantConnectionReader _connectionReader;
        private readonly ILogger<TenantDatabaseResolver> _logger;

        public TenantDatabaseResolver(ITenantConnectionReader connectionReader, ILogger<TenantDatabaseResolver> logger)
        {
            _connectionReader = connectionReader;
            _logger = logger;
        }

        public async Task<TenantDatabaseDescriptor> ResolveAsync(TenantDatabaseContext context, CancellationToken cancellationToken = default)
        {
            if (context == null ||
                context.IdEmpresa == Guid.Empty ||
                string.IsNullOrWhiteSpace(context.EmpresaKey))
            {
                throw new TenantDatabaseResolutionException(TenantDatabaseResolutionCode.TenantContextMissing);
            }

            string empresaKey = context.EmpresaKey.Trim().ToUpperInvariant();

            TenantConnectionDescriptor? connection;
            try
            {
                connection = await _connectionReader.GetConnectionAsync(empresaKey, cancellationToken);
            }
            catch (TenantDatabaseResolutionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("No fue posible resolver contexto ProductosServicios. ReferenceId={ReferenceId} ReasonCode={ReasonCode}", Guid.NewGuid().ToString("N"), "TENANT_LOOKUP_FAILED");
                throw new TenantDatabaseResolutionException(TenantDatabaseResolutionCode.TenantDatabaseResolutionFailed, ex);
            }

            if (connection == null)
            {
                throw new TenantDatabaseResolutionException(TenantDatabaseResolutionCode.TenantNotFound);
            }

            if (!string.Equals(connection.Status, "1", StringComparison.Ordinal))
            {
                throw new TenantDatabaseResolutionException(TenantDatabaseResolutionCode.TenantConnectionInactive);
            }

            if (connection.IdEmpresa == Guid.Empty)
            {
                throw new TenantDatabaseResolutionException(TenantDatabaseResolutionCode.TenantContextInvalid);
            }

            if (connection.IdEmpresa != context.IdEmpresa)
            {
                throw new TenantDatabaseResolutionException(TenantDatabaseResolutionCode.TenantIdEmpresaMismatch);
            }

            if (!IsUsableConnectionString(connection.ConnectionString))
            {
                throw new TenantDatabaseResolutionException(TenantDatabaseResolutionCode.TenantConnectionInvalid);
            }

            return new TenantDatabaseDescriptor
            {
                EmpresaKey = empresaKey,
                IdEmpresa = context.IdEmpresa,
                ConnectionString = connection.ConnectionString
            };
        }

        private static bool IsUsableConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return false;
            }

            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(connectionString);
                return !string.IsNullOrWhiteSpace(builder.DataSource) &&
                       !string.IsNullOrWhiteSpace(builder.InitialCatalog);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
}
