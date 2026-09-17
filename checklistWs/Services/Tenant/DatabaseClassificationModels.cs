namespace checklistWs.Services.Tenant
{
    public enum DatabaseStructureState
    {
        Empty,
        Partial,
        Current,
        Outdated,
        Future,
        Unknown
    }

    public enum DatabaseVersionEvidenceState
    {
        None,
        Current,
        Outdated,
        Future,
        Unknown
    }

    public static class DatabaseScopes
    {
        public const string ProductosServicios = "ProductosServicios";
        public const string Sucursales = "Sucursales";
    }

    public sealed class DatabaseSchemaProbeResult
    {
        public string Scope { get; init; } = string.Empty;
        public IReadOnlyCollection<string> ExpectedTables { get; init; } = Array.Empty<string>();
        public IReadOnlyCollection<string> ExistingScopeTables { get; init; } = Array.Empty<string>();
        public IReadOnlyCollection<string> Evidence { get; init; } = Array.Empty<string>();
        public IReadOnlyCollection<string> Warnings { get; init; } = Array.Empty<string>();
        public bool MetadataSufficient { get; init; } = true;
    }

    public sealed class DatabaseVersionEvidence
    {
        public DatabaseVersionEvidenceState State { get; init; } = DatabaseVersionEvidenceState.None;
        public string ReasonCode { get; init; } = "VERSION_EVIDENCE_NOT_AVAILABLE";
        public IReadOnlyCollection<string> Evidence { get; init; } = Array.Empty<string>();
        public IReadOnlyCollection<string> Warnings { get; init; } = Array.Empty<string>();
    }

    public sealed class DatabaseClassificationResult
    {
        public DatabaseIdentity Identity { get; init; } = null!;
        public string SanitizedIdentity { get; init; } = string.Empty;
        public string Scope { get; init; } = string.Empty;
        public DatabaseStructureState State { get; init; } = DatabaseStructureState.Unknown;
        public bool IsAvailable { get; init; } = true;
        public string ReasonCode { get; init; } = string.Empty;
        public int ExpectedScopeTableCount { get; init; }
        public int ExistingScopeTableCount { get; init; }
        public IReadOnlyCollection<string> ExistingScopeTables { get; init; } = Array.Empty<string>();
        public IReadOnlyCollection<string> Evidence { get; init; } = Array.Empty<string>();
        public IReadOnlyCollection<string> Warnings { get; init; } = Array.Empty<string>();
    }

    public interface IProductScopeInventory
    {
        IReadOnlyCollection<string> GetExpectedTables(string scope);
    }

    public interface IDatabaseSchemaProbe
    {
        Task<DatabaseSchemaProbeResult> ProbeAsync(
            TenantDatabaseDescriptor descriptor,
            string scope,
            CancellationToken cancellationToken = default);
    }

    public interface IDatabaseVersionEvidenceReader
    {
        Task<DatabaseVersionEvidence> ReadVersionEvidenceAsync(
            TenantDatabaseDescriptor descriptor,
            DatabaseIdentity identity,
            string scope,
            CancellationToken cancellationToken = default);
    }

    public interface IDatabaseStateClassifier
    {
        Task<DatabaseClassificationResult> ClassifyAsync(
            TenantDatabaseDescriptor descriptor,
            DatabaseIdentity identity,
            string scope,
            CancellationToken cancellationToken = default);
    }
}
