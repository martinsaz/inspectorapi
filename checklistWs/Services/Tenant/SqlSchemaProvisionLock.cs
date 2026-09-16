using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace checklistWs.Services.Tenant
{
    public sealed class SqlSchemaProvisionLock : ISchemaProvisionLock
    {
        private readonly ITenantSqlConnectionFactory _connectionFactory;

        public SqlSchemaProvisionLock(ITenantSqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<T> ExecuteAsync<T>(
            TenantDatabaseDescriptor descriptor,
            DatabaseIdentity identity,
            string scope,
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken = default)
        {
            using SqlConnection connection = _connectionFactory.CreateConnection(descriptor);
            await connection.OpenAsync(cancellationToken);
            string resource = $"CheckAppSchemaBootstrap:{identity.Fingerprint}:{scope}";

            using SqlCommand lockCommand = new SqlCommand("DECLARE @Result int; EXEC @Result = sp_getapplock @Resource = @Resource, @LockMode = 'Exclusive', @LockOwner = 'Session', @LockTimeout = 30000; SELECT @Result;", connection);
            lockCommand.Parameters.AddWithValue("@Resource", SanitizeResource(resource));
            int lockResult = Convert.ToInt32(await lockCommand.ExecuteScalarAsync(cancellationToken));
            if (lockResult < 0)
            {
                throw new InvalidOperationException("SCHEMA_PROVISION_LOCK_NOT_ACQUIRED");
            }

            try
            {
                return await action(cancellationToken);
            }
            finally
            {
                using SqlCommand releaseCommand = new SqlCommand("EXEC sp_releaseapplock @Resource = @Resource, @LockOwner = 'Session';", connection);
                releaseCommand.Parameters.AddWithValue("@Resource", SanitizeResource(resource));
                await releaseCommand.ExecuteNonQueryAsync(CancellationToken.None);
            }
        }

        private static string SanitizeResource(string resource)
        {
            string sanitized = Regex.Replace(resource, @"[^A-Za-z0-9:_\-]", "_");
            return sanitized.Length <= 255 ? sanitized : sanitized[..255];
        }
    }
}
