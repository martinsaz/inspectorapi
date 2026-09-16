using System.Security.Cryptography;
using System.Text;

namespace checklistWs.Services.Tenant
{
    public sealed class DatabaseIdentity : IEquatable<DatabaseIdentity>
    {
        public DatabaseIdentity(
            string serverName,
            string physicalServerName,
            string instanceName,
            string databaseName,
            string databaseGuid)
        {
            ServerName = Normalize(serverName);
            PhysicalServerName = Normalize(physicalServerName);
            InstanceName = Normalize(instanceName);
            DatabaseName = Normalize(databaseName);
            DatabaseGuid = Normalize(databaseGuid);
        }

        public string ServerName { get; }
        public string PhysicalServerName { get; }
        public string InstanceName { get; }
        public string DatabaseName { get; }
        public string DatabaseGuid { get; }
        public string Fingerprint => CreateFingerprint();

        public string ToSanitizedString()
        {
            return $"{ServerName}/{DatabaseName}/{DatabaseGuid}";
        }

        public bool Equals(DatabaseIdentity? other)
        {
            return other != null &&
                string.Equals(ServerName, other.ServerName, StringComparison.Ordinal) &&
                string.Equals(PhysicalServerName, other.PhysicalServerName, StringComparison.Ordinal) &&
                string.Equals(InstanceName, other.InstanceName, StringComparison.Ordinal) &&
                string.Equals(DatabaseName, other.DatabaseName, StringComparison.Ordinal) &&
                string.Equals(DatabaseGuid, other.DatabaseGuid, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as DatabaseIdentity);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ServerName, PhysicalServerName, InstanceName, DatabaseName, DatabaseGuid);
        }

        private string CreateFingerprint()
        {
            string material = string.Join('|',
                Normalize(ServerName),
                Normalize(PhysicalServerName),
                Normalize(InstanceName),
                Normalize(DatabaseName),
                Normalize(DatabaseGuid));

            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(material));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static string Normalize(string value)
        {
            return (value ?? string.Empty).Trim().ToUpperInvariant();
        }
    }

    public sealed class DatabaseMetadata
    {
        public string ServerName { get; init; } = string.Empty;
        public string PhysicalServerName { get; init; } = string.Empty;
        public string InstanceName { get; init; } = string.Empty;
        public string DatabaseName { get; init; } = string.Empty;
        public int DatabaseId { get; init; }
        public string DatabaseGuid { get; init; } = string.Empty;
        public bool Ambiguous { get; init; }
    }

    public sealed class DatabaseGroup
    {
        public DatabaseIdentity Identity { get; init; } = null!;
        public IReadOnlyCollection<TenantDescriptor> Tenants { get; init; } = Array.Empty<TenantDescriptor>();
    }

    public sealed class DatabaseGroupingError
    {
        public string EmpresaKey { get; init; } = string.Empty;
        public Guid? IdEmpresa { get; init; }
        public string Code { get; init; } = string.Empty;
    }

    public sealed class DatabaseGroupingResult
    {
        public IReadOnlyCollection<DatabaseGroup> Groups { get; init; } = Array.Empty<DatabaseGroup>();
        public IReadOnlyCollection<DatabaseGroupingError> Errors { get; init; } = Array.Empty<DatabaseGroupingError>();
    }

    public interface IDatabaseMetadataReader
    {
        Task<DatabaseMetadata> ReadMetadataAsync(TenantDatabaseDescriptor descriptor, CancellationToken cancellationToken = default);
    }

    public interface IDatabaseIdentityResolver
    {
        Task<DatabaseIdentity> ResolveAsync(TenantDatabaseDescriptor descriptor, CancellationToken cancellationToken = default);
    }

    public interface IDatabaseGroupingService
    {
        Task<DatabaseGroupingResult> GroupActiveTenantsAsync(CancellationToken cancellationToken = default);
    }
}
