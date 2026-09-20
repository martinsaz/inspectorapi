using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace checklistWs.Services.Tenant
{
    public sealed class SqlSchemaOperationLock : ISchemaOperationLock, ISchemaProvisionLock
    {
        private readonly ITenantSqlConnectionFactory _connectionFactory;

        public SqlSchemaOperationLock(ITenantSqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public string BuildResource(DatabaseIdentity identity, string scope, SchemaOperationLockKind kind)
        {
            string suffix = kind == SchemaOperationLockKind.Control
                ? "Control"
                : NormalizeScope(scope);
            string resource = $"CheckApp.Schema.{suffix}:{identity.Fingerprint}";
            return SanitizeResource(resource);
        }

        public async Task<SchemaOperationLockHandle> AcquireAsync(
            SqlConnection connection,
            SchemaOperationLockRequest request,
            CancellationToken cancellationToken = default,
            SqlTransaction? transaction = null)
        {
            if (connection == null || connection.State != System.Data.ConnectionState.Open)
            {
                throw new SchemaOperationLockException(SchemaOperationLockResultCode.Failed, "LOCK_FAILED_CONNECTION_NOT_OPEN", string.Empty);
            }

            string resource = BuildResource(request.Identity, request.Scope, request.Kind);
            int timeoutMs = checked((int)Math.Min(Math.Max(request.Timeout.TotalMilliseconds, 1), int.MaxValue));
            try
            {
                using SqlCommand lockCommand = new SqlCommand("DECLARE @Result int; EXEC @Result = sp_getapplock @Resource = @Resource, @LockMode = 'Exclusive', @LockOwner = 'Session', @LockTimeout = @LockTimeout; SELECT @Result;", connection, transaction);
                lockCommand.Parameters.AddWithValue("@Resource", resource);
                lockCommand.Parameters.AddWithValue("@LockTimeout", timeoutMs);
                int lockResult = Convert.ToInt32(await lockCommand.ExecuteScalarAsync(cancellationToken));
                if (lockResult >= 0)
                {
                    return new SchemaOperationLockHandle(resource, SchemaOperationLockResultCode.Acquired, lockResult, async token =>
                    {
                        using SqlCommand releaseCommand = new SqlCommand("EXEC sp_releaseapplock @Resource = @Resource, @LockOwner = 'Session';", connection);
                        releaseCommand.Parameters.AddWithValue("@Resource", resource);
                        await releaseCommand.ExecuteNonQueryAsync(token);
                    });
                }

                SchemaOperationLockResultCode code = lockResult switch
                {
                    -1 => SchemaOperationLockResultCode.Timeout,
                    -2 => SchemaOperationLockResultCode.Cancelled,
                    -3 => SchemaOperationLockResultCode.Deadlock,
                    _ => SchemaOperationLockResultCode.Failed
                };
                throw new SchemaOperationLockException(code, Reason(code), resource, lockResult);
            }
            catch (OperationCanceledException ex)
            {
                throw new SchemaOperationLockException(SchemaOperationLockResultCode.Cancelled, "LOCK_CANCELLED", resource, null, ex);
            }
            catch (SqlException ex) when (ex.Number == 1205)
            {
                throw new SchemaOperationLockException(SchemaOperationLockResultCode.Deadlock, "LOCK_DEADLOCK", resource, null, ex);
            }
            catch (SchemaOperationLockException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SchemaOperationLockException(SchemaOperationLockResultCode.Failed, "LOCK_FAILED", resource, null, ex);
            }
        }

        public async Task<T> ExecuteWithControlAndScopeAsync<T>(
            TenantDatabaseDescriptor descriptor,
            DatabaseIdentity identity,
            string scope,
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken = default)
        {
            using SqlConnection connection = _connectionFactory.CreateConnection(descriptor);
            await connection.OpenAsync(cancellationToken);
            await using SchemaOperationLockHandle control = await AcquireAsync(connection, SchemaOperationLockRequest.Control(identity), cancellationToken);
            await using SchemaOperationLockHandle scopeLock = await AcquireAsync(connection, SchemaOperationLockRequest.ScopeLock(identity, scope), cancellationToken);
            return await action(cancellationToken);
        }

        public Task<T> ExecuteAsync<T>(
            TenantDatabaseDescriptor descriptor,
            DatabaseIdentity identity,
            string scope,
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken = default) => ExecuteWithControlAndScopeAsync(descriptor, identity, scope, action, cancellationToken);

        private static string Reason(SchemaOperationLockResultCode code) => code switch
        {
            SchemaOperationLockResultCode.Timeout => "LOCK_TIMEOUT",
            SchemaOperationLockResultCode.Cancelled => "LOCK_CANCELLED",
            SchemaOperationLockResultCode.Deadlock => "LOCK_DEADLOCK",
            _ => "LOCK_FAILED"
        };

        private static string NormalizeScope(string scope)
        {
            if (string.Equals(scope, DatabaseScopes.ProductosServicios, StringComparison.OrdinalIgnoreCase))
            {
                return DatabaseScopes.ProductosServicios;
            }

            if (string.Equals(scope, DatabaseScopes.Sucursales, StringComparison.OrdinalIgnoreCase))
            {
                return DatabaseScopes.Sucursales;
            }

            if (string.Equals(scope, DatabaseScopes.Proveedores, StringComparison.OrdinalIgnoreCase))
            {
                return DatabaseScopes.Proveedores;
            }

            return Regex.Replace(scope ?? string.Empty, @"[^A-Za-z0-9_\-]", string.Empty);
        }

        private static string SanitizeResource(string resource)
        {
            string sanitized = Regex.Replace(resource, @"[^A-Za-z0-9:_\-\.]", "_");
            return sanitized.Length <= 255 ? sanitized : sanitized[..255];
        }
    }
}
