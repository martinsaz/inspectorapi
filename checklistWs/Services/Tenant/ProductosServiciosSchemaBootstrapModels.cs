namespace checklistWs.Services.Tenant
{
    public enum SchemaProvisionResultStatus
    {
        Provisioned,
        NoProvision,
        Failed
    }

    public sealed class SchemaProvisionResult
    {
        public SchemaProvisionResultStatus Status { get; init; }
        public string ReasonCode { get; init; } = string.Empty;
        public string Scope { get; init; } = string.Empty;
        public string SanitizedIdentity { get; init; } = string.Empty;
        public Guid? AttemptId { get; init; }
        public int? ContractVersion { get; init; }
        public string? ManifestHash { get; init; }
        public int TablesCreated { get; init; }
        public int ColumnsCreated { get; init; }
        public int IndexesCreated { get; init; }
        public int ForeignKeysCreated { get; init; }
        public int ChecksCreated { get; init; }
        public IReadOnlyCollection<string> Evidence { get; init; } = Array.Empty<string>();
        public IReadOnlyCollection<string> Warnings { get; init; } = Array.Empty<string>();
        public IReadOnlyCollection<string> Discrepancies { get; init; } = Array.Empty<string>();
    }

    public sealed class SchemaProvisionExecutionResult
    {
        public bool Succeeded { get; init; }
        public string ReasonCode { get; init; } = string.Empty;
        public int TablesCreated { get; init; }
        public int ColumnsCreated { get; init; }
        public int IndexesCreated { get; init; }
        public int ForeignKeysCreated { get; init; }
        public int ChecksCreated { get; init; }
        public IReadOnlyCollection<string> Discrepancies { get; init; } = Array.Empty<string>();
        public IReadOnlyCollection<string> Evidence { get; init; } = Array.Empty<string>();
    }

    public interface IProductosServiciosSchemaBootstrapper
    {
        Task<SchemaProvisionResult> ProvisionScopeAsync(
            TenantDatabaseDescriptor descriptor,
            DatabaseIdentity identity,
            string scope,
            CancellationToken cancellationToken = default);
    }

    public interface ISchemaProvisionExecutor
    {
        Task<SchemaProvisionExecutionResult> ProvisionAsync(
            TenantDatabaseDescriptor descriptor,
            DatabaseIdentity identity,
            SchemaContract contract,
            SchemaManifest manifest,
            CancellationToken cancellationToken = default);
    }

    public interface ISchemaContractSqlGenerator
    {
        IReadOnlyCollection<string> CreateProvisioningCommands(SchemaContract contract);
    }

    public interface ISchemaContractPhysicalValidator
    {
        Task<SchemaContractValidationResult> ValidateAsync(
            TenantDatabaseDescriptor descriptor,
            SchemaContract contract,
            CancellationToken cancellationToken = default);
    }

    public sealed class SchemaContractValidationResult
    {
        public bool IsValid => Discrepancies.Count == 0;
        public int DetectedTables { get; init; }
        public int DetectedColumns { get; init; }
        public int DetectedIndexes { get; init; }
        public int DetectedForeignKeys { get; init; }
        public int DetectedChecks { get; init; }
        public IReadOnlyCollection<string> Discrepancies { get; init; } = Array.Empty<string>();
    }

    public interface ISchemaProvisionLock
    {
        Task<T> ExecuteAsync<T>(
            TenantDatabaseDescriptor descriptor,
            DatabaseIdentity identity,
            string scope,
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken = default);
    }
}
