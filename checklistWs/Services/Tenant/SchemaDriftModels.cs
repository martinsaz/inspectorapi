using System.Data.SqlClient;

namespace checklistWs.Services.Tenant
{
    public enum SchemaDriftSeverity { Info, Warning, Error, Critical }
    public enum SchemaValidationGlobalResult { SchemaOk, SchemaDrift, SchemaDriftCritico, ValidacionNoConcluyente, VersionIncompatible, RequiereRevision }

    public sealed record SchemaDriftItem(
        string DatabaseIdentity,
        string Scope,
        int? DeclaredVersion,
        string? ExpectedManifestHash,
        string ObjectType,
        string ObjectName,
        string DifferenceType,
        string? Expected,
        string? Actual,
        SchemaDriftSeverity Severity,
        string AutoRepairableFuture,
        string ReasonCode);

    public sealed class SchemaDriftReport
    {
        public string ValidationRunId { get; init; } = Guid.NewGuid().ToString("N");
        public string SanitizedIdentity { get; init; } = string.Empty;
        public string Scope { get; init; } = string.Empty;
        public int? DeclaredVersion { get; init; }
        public string? ExpectedManifestHash { get; init; }
        public string? PersistedManifestHash { get; init; }
        public SchemaValidationGlobalResult GlobalResult { get; init; }
        public IReadOnlyCollection<SchemaDriftItem> Items { get; init; } = Array.Empty<SchemaDriftItem>();
        public int DetectedTables { get; init; }
        public int DetectedColumns { get; init; }
        public int DetectedIndexes { get; init; }
        public int DetectedForeignKeys { get; init; }
        public int DetectedChecks { get; init; }
        public bool ChangedState { get; init; }
        public bool ExecutedDdl { get; init; }
    }

    public sealed class SchemaPhysicalSnapshot
    {
        public bool MetadataSufficient { get; init; } = true;
        public IReadOnlyCollection<SchemaObjectSnapshot> Objects { get; init; } = Array.Empty<SchemaObjectSnapshot>();
        public IReadOnlyCollection<SchemaTableSnapshot> Tables { get; init; } = Array.Empty<SchemaTableSnapshot>();
        public IReadOnlyCollection<SchemaColumnSnapshot> Columns { get; init; } = Array.Empty<SchemaColumnSnapshot>();
        public IReadOnlyCollection<SchemaPrimaryKeySnapshot> PrimaryKeys { get; init; } = Array.Empty<SchemaPrimaryKeySnapshot>();
        public IReadOnlyCollection<SchemaIndexSnapshot> Indexes { get; init; } = Array.Empty<SchemaIndexSnapshot>();
        public IReadOnlyCollection<SchemaForeignKeySnapshot> ForeignKeys { get; init; } = Array.Empty<SchemaForeignKeySnapshot>();
        public IReadOnlyCollection<SchemaCheckSnapshot> Checks { get; init; } = Array.Empty<SchemaCheckSnapshot>();
        public IReadOnlyCollection<string> Extras { get; init; } = Array.Empty<string>();
    }

    public sealed record SchemaObjectSnapshot(string Schema, string Name, string TypeDescription);
    public sealed record SchemaTableSnapshot(string Schema, string Name);
    public sealed record SchemaColumnSnapshot(string Schema, string Table, string Name, string SqlType, int? MaxLength, byte? Precision, int? Scale, bool IsNullable, string? CollationName, bool IsIdentity, decimal? IdentitySeed, decimal? IdentityIncrement, bool IsComputed, string? ComputedDefinition, bool? IsComputedPersisted, string? DefaultDefinition);
    public sealed record SchemaPrimaryKeySnapshot(string Schema, string Table, string Name, IReadOnlyCollection<SchemaIndexColumnContract> Columns, bool IsClustered, bool IsDisabled);
    public sealed record SchemaIndexSnapshot(string Schema, string Table, string Name, bool IsUnique, bool IsClustered, IReadOnlyCollection<SchemaIndexColumnContract> KeyColumns, IReadOnlyCollection<string> IncludedColumns, string? FilterDefinition, bool IsDisabled, bool IsUniqueConstraint);
    public sealed record SchemaForeignKeySnapshot(string Schema, string Table, string Name, IReadOnlyCollection<string> Columns, string ReferencedSchema, string ReferencedTable, IReadOnlyCollection<string> ReferencedColumns, string DeleteAction, string UpdateAction, bool IsDisabled, bool IsNotTrusted, bool IsNotForReplication);
    public sealed record SchemaCheckSnapshot(string Schema, string Table, string Name, string Definition, bool IsDisabled, bool IsNotTrusted);

    public interface ISchemaPhysicalSnapshotReader
    {
        Task<SchemaPhysicalSnapshot> ReadAsync(SqlConnection connection, SchemaContract contract, CancellationToken cancellationToken = default, SqlTransaction? transaction = null);
    }

    public interface ISchemaDriftValidator
    {
        Task<SchemaDriftReport> ValidateAsync(TenantDatabaseDescriptor descriptor, DatabaseIdentity identity, string scope, int? declaredVersion, string? persistedManifestHash, CancellationToken cancellationToken = default);
        Task<SchemaDriftReport> ValidateAsync(SqlConnection connection, SqlTransaction? transaction, DatabaseIdentity identity, string scope, int? declaredVersion, string? persistedManifestHash, CancellationToken cancellationToken = default);
    }
}
