namespace checklistWs.Services.Tenant
{
    public enum SchemaControlInfrastructureStatus
    {
        Ready,
        Conflict
    }

    public sealed class SchemaControlInfrastructureResult
    {
        public SchemaControlInfrastructureStatus Status { get; init; }
        public IReadOnlyCollection<string> CreatedObjects { get; init; } = Array.Empty<string>();
        public IReadOnlyCollection<string> VerifiedObjects { get; init; } = Array.Empty<string>();
        public IReadOnlyCollection<string> Conflicts { get; init; } = Array.Empty<string>();
    }

    public sealed class SchemaControlConflictException : Exception
    {
        public SchemaControlConflictException(IReadOnlyCollection<string> conflicts)
            : base("CONTROL_SCHEMA_CONFLICT")
        {
            Conflicts = conflicts;
        }

        public IReadOnlyCollection<string> Conflicts { get; }
    }

    public sealed class SchemaControlState
    {
        public string DatabaseIdentityKey { get; init; } = string.Empty;
        public string Scope { get; init; } = string.Empty;
        public int? CurrentVersion { get; init; }
        public string? BaselineId { get; init; }
        public int? BaselineVersion { get; init; }
        public string? ManifestHash { get; init; }
        public DateTime? LastValidatedAtUtc { get; init; }
        public DateTime? LastMigratedAtUtc { get; init; }
        public string LastResult { get; init; } = string.Empty;
        public DateTime CreatedAtUtc { get; init; }
        public DateTime UpdatedAtUtc { get; init; }
    }

    public sealed class SchemaControlHistory
    {
        public Guid HistoryId { get; init; } = Guid.NewGuid();
        public string DatabaseIdentityKey { get; init; } = string.Empty;
        public string Scope { get; init; } = string.Empty;
        public string EventType { get; init; } = string.Empty;
        public int? FromVersion { get; init; }
        public int? ToVersion { get; init; }
        public string? OperationId { get; init; }
        public DateTime StartedAtUtc { get; init; }
        public DateTime CompletedAtUtc { get; init; }
        public string Result { get; init; } = string.Empty;
        public string? Details { get; init; }
    }

    public sealed class SchemaControlAttempt
    {
        public Guid AttemptId { get; init; } = Guid.NewGuid();
        public string DatabaseIdentityKey { get; init; } = string.Empty;
        public string Scope { get; init; } = string.Empty;
        public string OperationType { get; init; } = string.Empty;
        public int? FromVersion { get; init; }
        public int? ToVersion { get; init; }
        public DateTime StartedAtUtc { get; init; }
        public DateTime? CompletedAtUtc { get; init; }
        public string Result { get; init; } = string.Empty;
        public string? ReasonCode { get; init; }
        public string? Error { get; init; }
    }

    public interface IKnownSchemaVersionProvider
    {
        int? GetKnownCurrentVersion(string scope);
    }

    public sealed class KnownSchemaVersionProvider : IKnownSchemaVersionProvider
    {
        public int? GetKnownCurrentVersion(string scope)
        {
            if (string.Equals(scope, DatabaseScopes.ProductosServicios, StringComparison.OrdinalIgnoreCase))
            {
                return ProductosServiciosSchemaContractProvider.LatestVersion;
            }

            if (string.Equals(scope, DatabaseScopes.Sucursales, StringComparison.OrdinalIgnoreCase))
            {
                return ProductosServiciosSchemaContractProvider.SucursalesLatestVersion;
            }

            if (string.Equals(scope, DatabaseScopes.Proveedores, StringComparison.OrdinalIgnoreCase))
            {
                return ProductosServiciosSchemaContractProvider.ProveedoresLatestVersion;
            }

            if (string.Equals(scope, DatabaseScopes.OrdenesCompra, StringComparison.OrdinalIgnoreCase))
            {
                return ProductosServiciosSchemaContractProvider.OrdenesCompraLatestVersion;
            }

            if (string.Equals(scope, DatabaseScopes.Inventario, StringComparison.OrdinalIgnoreCase))
            {
                return ProductosServiciosSchemaContractProvider.InventarioLatestVersion;
            }

            if (string.Equals(scope, DatabaseScopes.Recepcion, StringComparison.OrdinalIgnoreCase))
            {
                return ProductosServiciosSchemaContractProvider.RecepcionLatestVersion;
            }

            if (string.Equals(scope, DatabaseScopes.Curvas, StringComparison.OrdinalIgnoreCase))
            {
                return ProductosServiciosSchemaContractProvider.CurvasLatestVersion;
            }

            return null;
        }
    }

    public interface ISchemaVersionRepository
    {
        Task<SchemaControlInfrastructureResult> EnsureSchemaControlInfrastructureAsync(
            TenantDatabaseDescriptor descriptor,
            CancellationToken cancellationToken = default);

        Task<SchemaControlState?> GetStateAsync(
            TenantDatabaseDescriptor descriptor,
            DatabaseIdentity identity,
            string scope,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<SchemaControlHistory>> GetHistoryAsync(
            TenantDatabaseDescriptor descriptor,
            DatabaseIdentity identity,
            string scope,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<SchemaControlAttempt>> GetAttemptsAsync(
            TenantDatabaseDescriptor descriptor,
            DatabaseIdentity identity,
            string scope,
            CancellationToken cancellationToken = default);

        Task<Guid> BeginAttemptAsync(
            TenantDatabaseDescriptor descriptor,
            SchemaControlAttempt attempt,
            CancellationToken cancellationToken = default);

        Task CompleteAttemptAsync(
            TenantDatabaseDescriptor descriptor,
            Guid attemptId,
            string result,
            string reasonCode,
            string? error,
            CancellationToken cancellationToken = default);

        Task RecordHistoryAsync(
            TenantDatabaseDescriptor descriptor,
            SchemaControlHistory history,
            CancellationToken cancellationToken = default);

        Task SetConfirmedStateAsync(
            TenantDatabaseDescriptor descriptor,
            SchemaControlState state,
            CancellationToken cancellationToken = default);
    }
}
