using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace checklistWs.Services.Tenant
{
    public enum SchemaMigrationResolutionStatus
    {
        Ready,
        NoPendingMigrations,
        AlreadyApplied,
        PackageInvalid,
        ChainGap,
        ChainBranch,
        ChainCycle,
        VersionMismatch,
        BaselineMismatch,
        HistoryInconsistent,
        TargetVersionInvalid,
        RequiresReview
    }

    public enum SchemaMigrationExecutionStatus
    {
        Migrated,
        NoProvision,
        Failed,
        LockTimeout,
        RequiresReview,
        Recovered
    }

    public sealed record SchemaReleaseManifest(
        string Scope,
        string BaselineId,
        int BaselineVersion,
        int LatestSchemaVersion,
        string LatestManifestHash,
        IReadOnlyCollection<string> ApprovedMigrationIds);

    public sealed record SchemaMigrationDefinition(
        string MigrationId,
        string BaselineId,
        string Scope,
        int FromVersion,
        int ToVersion,
        int Order,
        IReadOnlyCollection<string> ObjectsAffected,
        IReadOnlyCollection<string> Preconditions,
        IReadOnlyCollection<string> DataPreconditions,
        string SqlHash,
        string TargetManifestHash,
        string TransactionMode,
        string Risk,
        bool AutoApplicable,
        TimeSpan Timeout,
        IReadOnlyCollection<string> Dependencies,
        string RecoveryPolicy,
        string UpSql,
        SchemaContract TargetContract);

    public sealed class SchemaMigrationPackage
    {
        public SchemaMigrationPackage(SchemaReleaseManifest release, IReadOnlyCollection<SchemaMigrationDefinition> migrations)
        {
            Release = release ?? throw new ArgumentNullException(nameof(release));
            Migrations = migrations ?? Array.Empty<SchemaMigrationDefinition>();
        }

        public SchemaReleaseManifest Release { get; }
        public IReadOnlyCollection<SchemaMigrationDefinition> Migrations { get; }
    }

    public sealed class SchemaMigrationResolution
    {
        public SchemaMigrationResolutionStatus Status { get; init; }
        public string ReasonCode { get; init; } = string.Empty;
        public IReadOnlyCollection<SchemaMigrationDefinition> PendingMigrations { get; init; } = Array.Empty<SchemaMigrationDefinition>();
        public IReadOnlyCollection<string> Evidence { get; init; } = Array.Empty<string>();
    }

    public sealed class SchemaMigrationExecutionResult
    {
        public SchemaMigrationExecutionStatus Status { get; init; }
        public string ReasonCode { get; init; } = string.Empty;
        public string SanitizedIdentity { get; init; } = string.Empty;
        public string Scope { get; init; } = string.Empty;
        public Guid? AttemptId { get; init; }
        public string? MigrationId { get; init; }
        public int? FromVersion { get; init; }
        public int? ToVersion { get; init; }
        public string? ManifestHash { get; init; }
        public IReadOnlyCollection<string> Evidence { get; init; } = Array.Empty<string>();
        public IReadOnlyCollection<string> Discrepancies { get; init; } = Array.Empty<string>();
    }

    public interface ISchemaMigrationPackageProvider
    {
        SchemaMigrationPackage GetPackage(string scope);
    }

    public interface ISchemaMigrationResolver
    {
        SchemaMigrationResolution GetPendingMigrations(
            DatabaseIdentity identity,
            string scope,
            int currentVersion,
            int? targetVersion,
            SchemaMigrationPackage package,
            IReadOnlyCollection<SchemaControlHistory> history);
    }

    public interface ISchemaMigrationSqlExecutor
    {
        Task ExecuteAsync(
            TenantDatabaseDescriptor descriptor,
            DatabaseIdentity identity,
            SchemaMigrationDefinition migration,
            Func<SchemaMigrationDefinition, CancellationToken, Task<SchemaContractValidationResult>> validateTargetAsync,
            CancellationToken cancellationToken = default);
    }

    public interface ISchemaMigrationRunner
    {
        Task<SchemaMigrationExecutionResult> MigrateToLatestAsync(
            TenantDatabaseDescriptor descriptor,
            DatabaseIdentity identity,
            string scope,
            CancellationToken cancellationToken = default);

        Task<SchemaMigrationExecutionResult> RecoverUncertainCommitAsync(
            TenantDatabaseDescriptor descriptor,
            DatabaseIdentity identity,
            string scope,
            string migrationId,
            Guid attemptId,
            CancellationToken cancellationToken = default);
    }

    public sealed class ProductosServiciosMigrationPackageProvider : ISchemaMigrationPackageProvider
    {
        private readonly ISchemaContractProvider _contractProvider;
        private readonly ISchemaManifestProvider _manifestProvider;

        public ProductosServiciosMigrationPackageProvider(ISchemaContractProvider contractProvider, ISchemaManifestProvider manifestProvider)
        {
            _contractProvider = contractProvider;
            _manifestProvider = manifestProvider;
        }

        public SchemaMigrationPackage GetPackage(string scope)
        {
            if (!string.Equals(scope, DatabaseScopes.ProductosServicios, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentOutOfRangeException(nameof(scope), scope, null);
            }

            SchemaContract v1 = _contractProvider.GetContract(DatabaseScopes.ProductosServicios, 1);
            SchemaManifest manifest = _manifestProvider.CreateManifest(v1);
            return new SchemaMigrationPackage(
                new SchemaReleaseManifest(
                    DatabaseScopes.ProductosServicios,
                    "PS-B20260909",
                    1,
                    1,
                    manifest.ManifestHash,
                    Array.Empty<string>()),
                Array.Empty<SchemaMigrationDefinition>());
        }
    }

    public static class SchemaMigrationHash
    {
        public static string Sha256(string value)
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(value ?? string.Empty));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }

    public static class SchemaMigrationHistoryDetails
    {
        public static string Build(SchemaMigrationDefinition migration)
        {
            return $"MigrationId={migration.MigrationId}; SqlHash={migration.SqlHash}; TargetManifestHash={migration.TargetManifestHash}";
        }

        public static string? Get(string? details, string key)
        {
            if (string.IsNullOrWhiteSpace(details))
            {
                return null;
            }

            foreach (string part in details.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                int separator = part.IndexOf('=', StringComparison.Ordinal);
                if (separator <= 0)
                {
                    continue;
                }

                if (string.Equals(part[..separator].Trim(), key, StringComparison.OrdinalIgnoreCase))
                {
                    return part[(separator + 1)..].Trim();
                }
            }

            return null;
        }
    }

    internal static class SchemaMigrationSanitizer
    {
        public static string? Sanitize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            string sanitized = Regex.Replace(value, "(Password|Pwd|User Id|UID|Token|Secret)\\s*=\\s*[^;\\s]+", "$1=***", RegexOptions.IgnoreCase);
            return sanitized.Length > 2000 ? sanitized[..2000] : sanitized;
        }
    }
}
