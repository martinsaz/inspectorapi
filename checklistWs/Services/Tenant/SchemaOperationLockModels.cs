using System.Data.SqlClient;

namespace checklistWs.Services.Tenant
{
    public enum SchemaOperationLockResultCode
    {
        Acquired,
        Timeout,
        Cancelled,
        Deadlock,
        Failed
    }

    public enum SchemaOperationLockKind
    {
        Control,
        Scope
    }

    public sealed record SchemaOperationLockRequest(
        DatabaseIdentity Identity,
        string Scope,
        SchemaOperationLockKind Kind,
        TimeSpan Timeout)
    {
        public static SchemaOperationLockRequest Control(DatabaseIdentity identity, TimeSpan? timeout = null) =>
            new(identity, "Control", SchemaOperationLockKind.Control, timeout ?? TimeSpan.FromSeconds(30));

        public static SchemaOperationLockRequest ScopeLock(DatabaseIdentity identity, string scope, TimeSpan? timeout = null) =>
            new(identity, scope, SchemaOperationLockKind.Scope, timeout ?? TimeSpan.FromSeconds(30));
    }

    public sealed class SchemaOperationLockException : Exception
    {
        public SchemaOperationLockException(SchemaOperationLockResultCode code, string reasonCode, string resource, int? sqlReturnCode = null, Exception? innerException = null)
            : base(reasonCode, innerException)
        {
            Code = code;
            ReasonCode = reasonCode;
            Resource = resource;
            SqlReturnCode = sqlReturnCode;
        }

        public SchemaOperationLockResultCode Code { get; }
        public string ReasonCode { get; }
        public string Resource { get; }
        public int? SqlReturnCode { get; }
    }

    public sealed class SchemaOperationLockHandle : IAsyncDisposable
    {
        private readonly Func<CancellationToken, Task> _releaseAsync;
        private int _released;

        public SchemaOperationLockHandle(string resource, SchemaOperationLockResultCode resultCode, int sqlReturnCode, Func<CancellationToken, Task> releaseAsync)
        {
            Resource = resource;
            ResultCode = resultCode;
            SqlReturnCode = sqlReturnCode;
            _releaseAsync = releaseAsync;
        }

        public string Resource { get; }
        public SchemaOperationLockResultCode ResultCode { get; }
        public int SqlReturnCode { get; }
        public bool Released => _released != 0;

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0)
            {
                await _releaseAsync(CancellationToken.None);
            }
        }
    }

    public interface ISchemaOperationLock
    {
        string BuildResource(DatabaseIdentity identity, string scope, SchemaOperationLockKind kind);

        Task<SchemaOperationLockHandle> AcquireAsync(
            SqlConnection connection,
            SchemaOperationLockRequest request,
            CancellationToken cancellationToken = default,
            SqlTransaction? transaction = null);

        Task<T> ExecuteWithControlAndScopeAsync<T>(
            TenantDatabaseDescriptor descriptor,
            DatabaseIdentity identity,
            string scope,
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken = default);
    }
}
