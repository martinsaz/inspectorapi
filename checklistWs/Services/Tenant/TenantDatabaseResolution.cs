using System.Data.SqlClient;

namespace checklistWs.Services.Tenant
{
    public enum TenantDatabaseResolutionCode
    {
        TenantContextMissing,
        TenantContextInvalid,
        TenantNotFound,
        TenantConnectionInactive,
        TenantIdEmpresaMismatch,
        TenantConnectionInvalid,
        TenantDatabaseResolutionFailed
    }

    public enum DatabaseIdentityResolutionCode
    {
        DatabaseIdentityUnavailable,
        DatabaseIdentityUnverifiable,
        DatabaseIdentityAmbiguous
    }

    public sealed class TenantDatabaseResolutionException : Exception
    {
        public TenantDatabaseResolutionException(TenantDatabaseResolutionCode code)
            : base(code.ToString())
        {
            Code = code;
        }

        public TenantDatabaseResolutionException(TenantDatabaseResolutionCode code, Exception innerException)
            : base(code.ToString(), innerException)
        {
            Code = code;
        }

        public TenantDatabaseResolutionCode Code { get; }
    }

    public sealed class DatabaseIdentityResolutionException : Exception
    {
        public DatabaseIdentityResolutionException(DatabaseIdentityResolutionCode code)
            : base(code.ToString())
        {
            Code = code;
        }

        public DatabaseIdentityResolutionException(DatabaseIdentityResolutionCode code, Exception innerException)
            : base(code.ToString(), innerException)
        {
            Code = code;
        }

        public DatabaseIdentityResolutionCode Code { get; }
    }

    public sealed class TenantDatabaseContext
    {
        public string EmpresaKey { get; init; } = string.Empty;
        public Guid IdEmpresa { get; init; }
    }

    public sealed class TenantConnectionDescriptor
    {
        public string EmpresaKey { get; init; } = string.Empty;
        public Guid IdEmpresa { get; init; }
        public string Status { get; init; } = string.Empty;
        public string ConnectionString { get; init; } = string.Empty;
    }

    public sealed class TenantDescriptor
    {
        public string EmpresaKey { get; init; } = string.Empty;
        public Guid IdEmpresa { get; init; }
        public string Status { get; init; } = string.Empty;
    }

    public sealed class TenantDatabaseDescriptor
    {
        public string EmpresaKey { get; init; } = string.Empty;
        public Guid IdEmpresa { get; init; }
        public string ConnectionString { get; init; } = string.Empty;
    }

    public interface ITenantConnectionReader
    {
        Task<TenantConnectionDescriptor?> GetConnectionAsync(string empresaKey, CancellationToken cancellationToken = default);
    }

    public interface ITenantCatalogReader
    {
        Task<IReadOnlyCollection<TenantConnectionDescriptor>> GetConnectionsAsync(CancellationToken cancellationToken = default);
    }

    public interface ITenantDatabaseResolver
    {
        Task<TenantDatabaseDescriptor> ResolveAsync(TenantDatabaseContext context, CancellationToken cancellationToken = default);
    }

    public interface ITenantSqlConnectionFactory
    {
        SqlConnection CreateConnection(TenantDatabaseDescriptor descriptor);
    }
}
