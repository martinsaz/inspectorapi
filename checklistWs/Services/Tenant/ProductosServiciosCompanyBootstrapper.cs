namespace checklistWs.Services.Tenant
{
    public sealed class CompanyBootstrapResult
    {
        public string Status { get; init; } = "BLOCKED";
        public string ReasonCode { get; init; } = string.Empty;
        public IReadOnlyList<string> CreatedItems { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> ExistingItems { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> SkippedItems { get; init; } = Array.Empty<string>();
        public string ReferenceId { get; init; } = Guid.NewGuid().ToString("N");
        public DateTime StartedAtUtc { get; init; }
        public DateTime CompletedAtUtc { get; init; }
        public bool ExecutedDdl => false;
    }

    public interface IProductosServiciosCompanyBootstrapper
    {
        // descriptor must come from the server-side tenant resolver, never model binding.
        Task<CompanyBootstrapResult> BootstrapAsync(TenantDatabaseDescriptor descriptor,
            string scope = DatabaseScopes.ProductosServicios, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// T22 has no mandatory company seeds. Categories and units are authored through their
    /// existing CRUD, not invented at company registration. Every invocation checks T20;
    /// there is no persistent initialized flag, business transaction or schema operation.
    /// </summary>
    public sealed class ProductosServiciosCompanyBootstrapper : IProductosServiciosCompanyBootstrapper
    {
        private readonly IProductosServiciosCompatibilityGate _gate;
        private readonly ILogger<ProductosServiciosCompanyBootstrapper> _logger;

        public ProductosServiciosCompanyBootstrapper(IProductosServiciosCompatibilityGate gate,
            ILogger<ProductosServiciosCompanyBootstrapper> logger)
        {
            _gate = gate;
            _logger = logger;
        }

        public async Task<CompanyBootstrapResult> BootstrapAsync(TenantDatabaseDescriptor descriptor,
            string scope = DatabaseScopes.ProductosServicios, CancellationToken cancellationToken = default)
        {
            var started = DateTime.UtcNow;
            var reference = Guid.NewGuid().ToString("N");
            string status = "BLOCKED", reason = "TENANT_CONTEXT_INVALID", identity = string.Empty;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (descriptor != null && descriptor.IdEmpresa != Guid.Empty &&
                    !string.IsNullOrWhiteSpace(descriptor.EmpresaKey) &&
                    !string.IsNullOrWhiteSpace(descriptor.ConnectionString) &&
                    string.Equals(scope, DatabaseScopes.ProductosServicios, StringComparison.Ordinal))
                {
                    CompatibilityDecision decision = await _gate.EvaluateAsync(descriptor, scope, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                    if (decision.IsAllowed && decision.ReasonCode == "COMPATIBLE" &&
                        decision.Scope == scope && !string.IsNullOrWhiteSpace(decision.SanitizedIdentity) &&
                        decision.SchemaResult == SchemaValidationGlobalResult.SchemaOk)
                    {
                        identity = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(decision.SanitizedIdentity)));
                        status = "NO_CHANGES";
                        reason = "NO_REQUIRED_COMPANY_SEEDS";
                    }
                    else
                    {
                        // Do not reflect exception messages, connection strings or arbitrary reason payloads.
                        reason = "COMPATIBILITY_BLOCKED";
                    }
                }
            }
            catch (OperationCanceledException) { reason = "CANCELLED"; }
            catch (Exception) { reason = "REQUIRES_REVIEW"; }

            var result = new CompanyBootstrapResult
            {
                Status = status, ReasonCode = reason, ReferenceId = reference,
                StartedAtUtc = started, CompletedAtUtc = DateTime.UtcNow
            };
            _logger.LogInformation("CompanyBootstrap ReferenceId={ReferenceId} DatabaseIdentity={DatabaseIdentity} Scope={Scope} IdEmpresa={IdEmpresa} Status={Status} CreatedCount={CreatedCount} ExistingCount={ExistingCount} SkippedCount={SkippedCount} DurationMs={DurationMs}",
                reference, identity, DatabaseScopes.ProductosServicios, descriptor?.IdEmpresa, status,
                0, 0, 0, (result.CompletedAtUtc - started).TotalMilliseconds);
            return result;
        }
    }
}
