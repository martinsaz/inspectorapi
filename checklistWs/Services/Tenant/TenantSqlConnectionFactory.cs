using System.Data.SqlClient;

namespace checklistWs.Services.Tenant
{
    public sealed class TenantSqlConnectionFactory : ITenantSqlConnectionFactory
    {
        public SqlConnection CreateConnection(TenantDatabaseDescriptor descriptor)
        {
            if (descriptor == null || string.IsNullOrWhiteSpace(descriptor.ConnectionString))
            {
                throw new TenantDatabaseResolutionException(TenantDatabaseResolutionCode.TenantConnectionInvalid);
            }

            return new SqlConnection(descriptor.ConnectionString);
        }
    }
}
