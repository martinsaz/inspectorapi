namespace checklistWs.Services.Tenant
{
    public enum HistoricalBaselineAdoptionStatus
    {
        Adopted,
        NoAdoption,
        Blocked
    }

    public sealed class HistoricalBaselineAdoptionResult
    {
        public HistoricalBaselineAdoptionStatus Status { get; init; }
        public string ReasonCode { get; init; } = string.Empty;
        public string Scope { get; init; } = DatabaseScopes.ProductosServicios;
        public string SanitizedIdentity { get; init; } = string.Empty;
        public int? CurrentVersion { get; init; }
        public string? ManifestHash { get; init; }
        public SchemaValidationGlobalResult? SchemaResult { get; init; }
        public int DriftCount { get; init; }
        public int DetectedTables { get; init; }
        public int DetectedColumns { get; init; }
        public int DetectedIndexes { get; init; }
        public int DetectedForeignKeys { get; init; }
        public int DetectedChecks { get; init; }
        public Guid? AttemptId { get; init; }
        public bool StateModified { get; init; }
        public bool HistoryModified { get; init; }
        public bool AttemptsModified { get; init; }
    }

    public interface IProductosServiciosHistoricalBaselineAdopter
    {
        Task<HistoricalBaselineAdoptionResult> AdoptIfEligibleAsync(
            TenantDatabaseDescriptor descriptor,
            DatabaseIdentity identity,
            string scope = DatabaseScopes.ProductosServicios,
            CancellationToken cancellationToken = default);
    }
}
