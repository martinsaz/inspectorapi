namespace checklistWs.Services.Tenant
{
    public sealed class CompatibilityDecision
    {
        public bool IsAllowed { get; init; }
        public string SanitizedIdentity { get; init; } = string.Empty;
        public string Scope { get; init; } = DatabaseScopes.ProductosServicios;
        public int? CurrentVersion { get; init; }
        public int? LatestSupportedVersion { get; init; }
        public SchemaValidationGlobalResult? SchemaResult { get; init; }
        public string ReasonCode { get; init; } = string.Empty;
        public bool Retryable { get; init; }
        public string ReferenceId { get; init; } = Guid.NewGuid().ToString("N");
        public DateTime CheckedAtUtc { get; init; } = DateTime.UtcNow;
    }

    public interface IProductosServiciosCompatibilityGate
    {
        Task<CompatibilityDecision> EvaluateAsync(
            TenantDatabaseDescriptor descriptor,
            string scope = DatabaseScopes.ProductosServicios,
            CancellationToken cancellationToken = default);
    }
}
