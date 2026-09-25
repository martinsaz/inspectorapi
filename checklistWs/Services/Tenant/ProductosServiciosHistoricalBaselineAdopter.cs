using Microsoft.Extensions.Logging;

namespace checklistWs.Services.Tenant
{
    public sealed class ProductosServiciosHistoricalBaselineAdopter : IProductosServiciosHistoricalBaselineAdopter
    {
        public const string BaselineId = "PRODUCTOSSERVICIOS_V1_HISTORICAL_BASELINE";
        public const string SucursalesBaselineId = "SUCURSALES_V1_HISTORICAL_BASELINE";
        public const string ProveedoresBaselineId = "PROVEEDORES_V1_HISTORICAL_BASELINE";

        private readonly IDatabaseStateClassifier _classifier;
        private readonly ISchemaVersionRepository _repository;
        private readonly IKnownSchemaVersionProvider _versionProvider;
        private readonly ISchemaContractProvider _contractProvider;
        private readonly ISchemaManifestProvider _manifestProvider;
        private readonly ISchemaDriftValidator _driftValidator;
        private readonly ISchemaOperationLock _operationLock;
        private readonly ILogger<ProductosServiciosHistoricalBaselineAdopter> _logger;

        public ProductosServiciosHistoricalBaselineAdopter(
            IDatabaseStateClassifier classifier,
            ISchemaVersionRepository repository,
            IKnownSchemaVersionProvider versionProvider,
            ISchemaContractProvider contractProvider,
            ISchemaManifestProvider manifestProvider,
            ISchemaDriftValidator driftValidator,
            ISchemaOperationLock operationLock,
            ILogger<ProductosServiciosHistoricalBaselineAdopter> logger)
        {
            _classifier = classifier;
            _repository = repository;
            _versionProvider = versionProvider;
            _contractProvider = contractProvider;
            _manifestProvider = manifestProvider;
            _driftValidator = driftValidator;
            _operationLock = operationLock;
            _logger = logger;
        }

        public Task<HistoricalBaselineAdoptionResult> AdoptIfEligibleAsync(
            TenantDatabaseDescriptor descriptor,
            DatabaseIdentity identity,
            string scope = DatabaseScopes.ProductosServicios,
            CancellationToken cancellationToken = default)
        {
            return _operationLock.ExecuteWithControlAndScopeAsync(
                descriptor,
                identity,
                scope,
                innerCancellationToken => AdoptInsideLockAsync(descriptor, identity, scope, innerCancellationToken),
                cancellationToken);
        }

        private async Task<HistoricalBaselineAdoptionResult> AdoptInsideLockAsync(
            TenantDatabaseDescriptor descriptor,
            DatabaseIdentity identity,
            string scope,
            CancellationToken cancellationToken)
        {
            if (!IsSupportedScope(scope))
            {
                return Blocked(identity, scope, "SCOPE_NOT_SUPPORTED");
            }

            int baselineVersion = ProductosServiciosSchemaContractProvider.V1;
            string baselineId = ResolveBaselineId(scope);
            if (_versionProvider.GetKnownCurrentVersion(scope) == null)
            {
                return Blocked(identity, scope, "KNOWN_VERSION_MISSING");
            }

            SchemaContract contract;
            try
            {
                contract = _contractProvider.GetContract(scope, baselineVersion);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Historical baseline adoption blocked because the schema contract is unavailable for {Scope}.", scope);
                return Blocked(identity, scope, "CONTRACT_UNAVAILABLE");
            }

            SchemaManifest manifest = _manifestProvider.CreateManifest(contract);
            DatabaseClassificationResult classification = await _classifier.ClassifyAsync(descriptor, identity, scope, cancellationToken);
            if (!classification.IsAvailable)
            {
                return NoAdoption(identity, scope, "DATABASE_UNAVAILABLE");
            }

            if (classification.State != DatabaseStructureState.Unknown ||
                !string.Equals(classification.ReasonCode, "VERSION_EVIDENCE_MISSING", StringComparison.OrdinalIgnoreCase))
            {
                return NoAdoption(identity, scope, "CLASSIFICATION_NOT_ADOPTABLE");
            }

            if (classification.ExpectedScopeTableCount == 0 ||
                classification.ExistingScopeTableCount != classification.ExpectedScopeTableCount)
            {
                return NoAdoption(identity, scope, "PHYSICAL_SCOPE_NOT_COMPLETE");
            }

            SchemaControlState? state = await _repository.GetStateAsync(descriptor, identity, scope, cancellationToken);
            if (state?.CurrentVersion != null)
            {
                return NoAdoption(identity, scope, "STATE_ALREADY_CONFIRMED");
            }

            IReadOnlyCollection<SchemaControlHistory> history = await _repository.GetHistoryAsync(descriptor, identity, scope, cancellationToken);
            if (history.Any(IsBaselineEvent))
            {
                return NoAdoption(identity, scope, "BASELINE_ALREADY_RECORDED");
            }

            SchemaDriftReport drift = await _driftValidator.ValidateAsync(
                descriptor,
                identity,
                scope,
                baselineVersion,
                manifest.ManifestHash,
                cancellationToken);

            if (drift.GlobalResult != SchemaValidationGlobalResult.SchemaOk || drift.Items.Count > 0)
            {
                return new HistoricalBaselineAdoptionResult
                {
                    Status = HistoricalBaselineAdoptionStatus.Blocked,
                    ReasonCode = "PHYSICAL_VALIDATION_NOT_SCHEMA_OK",
                    Scope = scope,
                    SanitizedIdentity = identity.ToSanitizedString(),
                    CurrentVersion = baselineVersion,
                    ManifestHash = manifest.ManifestHash,
                    SchemaResult = drift.GlobalResult,
                    DriftCount = drift.Items.Count,
                    DetectedTables = drift.DetectedTables,
                    DetectedColumns = drift.DetectedColumns,
                    DetectedIndexes = drift.DetectedIndexes,
                    DetectedForeignKeys = drift.DetectedForeignKeys,
                    DetectedChecks = drift.DetectedChecks
                };
            }

            DateTime now = DateTime.UtcNow;
            Guid attemptId = Guid.NewGuid();
            await _repository.BeginAttemptAsync(descriptor, new SchemaControlAttempt
            {
                AttemptId = attemptId,
                DatabaseIdentityKey = identity.Fingerprint,
                Scope = scope,
                OperationType = "ADOPT_BASELINE",
                FromVersion = null,
                ToVersion = baselineVersion,
                StartedAtUtc = now,
                Result = "STARTED",
                ReasonCode = "HISTORICAL_BASELINE_VALIDATION"
            }, cancellationToken);

            try
            {
                await _repository.SetConfirmedStateAsync(descriptor, new SchemaControlState
                {
                    DatabaseIdentityKey = identity.Fingerprint,
                    Scope = scope,
                    CurrentVersion = baselineVersion,
                    BaselineId = baselineId,
                    BaselineVersion = baselineVersion,
                    ManifestHash = manifest.ManifestHash,
                    LastValidatedAtUtc = now,
                    LastMigratedAtUtc = null,
                    LastResult = "ADOPTED",
                    CreatedAtUtc = state?.CreatedAtUtc ?? now,
                    UpdatedAtUtc = now
                }, cancellationToken);

                await _repository.RecordHistoryAsync(descriptor, new SchemaControlHistory
                {
                    HistoryId = Guid.NewGuid(),
                    DatabaseIdentityKey = identity.Fingerprint,
                    Scope = scope,
                    EventType = "ADOPTED",
                    FromVersion = null,
                    ToVersion = baselineVersion,
                    OperationId = attemptId.ToString("N"),
                    StartedAtUtc = now,
                    CompletedAtUtc = now,
                    Result = "PASS",
                    Details = $"BaselineId={baselineId}; ManifestHash={manifest.ManifestHash}; Tables={drift.DetectedTables}; Columns={drift.DetectedColumns}; Indexes={drift.DetectedIndexes}; ForeignKeys={drift.DetectedForeignKeys}; Checks={drift.DetectedChecks}; DriftCount={drift.Items.Count}"
                }, cancellationToken);

                await _repository.CompleteAttemptAsync(
                    descriptor,
                    attemptId,
                    "PASS",
                    "ADOPTED",
                    null,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                await _repository.CompleteAttemptAsync(
                    descriptor,
                    attemptId,
                    "FAIL",
                    "ADOPTION_FAILED",
                    ex.GetType().Name,
                    cancellationToken);
                throw;
            }

            return new HistoricalBaselineAdoptionResult
            {
                Status = HistoricalBaselineAdoptionStatus.Adopted,
                ReasonCode = "ADOPTED",
                Scope = scope,
                SanitizedIdentity = identity.ToSanitizedString(),
                CurrentVersion = baselineVersion,
                ManifestHash = manifest.ManifestHash,
                SchemaResult = drift.GlobalResult,
                DriftCount = drift.Items.Count,
                DetectedTables = drift.DetectedTables,
                DetectedColumns = drift.DetectedColumns,
                DetectedIndexes = drift.DetectedIndexes,
                DetectedForeignKeys = drift.DetectedForeignKeys,
                DetectedChecks = drift.DetectedChecks,
                AttemptId = attemptId,
                StateModified = true,
                HistoryModified = true,
                AttemptsModified = true
            };
        }

        private static bool IsBaselineEvent(SchemaControlHistory history)
        {
            return string.Equals(history.Result, "PASS", StringComparison.OrdinalIgnoreCase) &&
                (string.Equals(history.EventType, "ADOPTED", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(history.EventType, "PROVISIONED", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(history.EventType, "MIGRATED", StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsSupportedScope(string scope)
        {
            return string.Equals(scope, DatabaseScopes.ProductosServicios, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(scope, DatabaseScopes.Sucursales, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(scope, DatabaseScopes.Proveedores, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(scope, DatabaseScopes.OrdenesCompra, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(scope, DatabaseScopes.Inventario, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(scope, DatabaseScopes.Recepcion, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(scope, DatabaseScopes.Curvas, StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolveBaselineId(string scope)
        {
            if (string.Equals(scope, DatabaseScopes.Sucursales, StringComparison.OrdinalIgnoreCase))
            {
                return SucursalesBaselineId;
            }

            if (string.Equals(scope, DatabaseScopes.Proveedores, StringComparison.OrdinalIgnoreCase))
            {
                return ProveedoresBaselineId;
            }

            if (string.Equals(scope, DatabaseScopes.OrdenesCompra, StringComparison.OrdinalIgnoreCase))
            {
                return "ORDENESCOMPRA_V1_HISTORICAL_BASELINE";
            }

            if (string.Equals(scope, DatabaseScopes.Inventario, StringComparison.OrdinalIgnoreCase))
            {
                return "INVENTARIO_V1_EMPTY_BASELINE";
            }

            if (string.Equals(scope, DatabaseScopes.Recepcion, StringComparison.OrdinalIgnoreCase))
            {
                return "RECEPCION_V1_EMPTY_BASELINE";
            }

            if (string.Equals(scope, DatabaseScopes.Curvas, StringComparison.OrdinalIgnoreCase))
            {
                return "CURVAS_V1_EMPTY_BASELINE";
            }

            return BaselineId;
        }

        private static HistoricalBaselineAdoptionResult NoAdoption(DatabaseIdentity identity, string scope, string reasonCode)
        {
            return new HistoricalBaselineAdoptionResult
            {
                Status = HistoricalBaselineAdoptionStatus.NoAdoption,
                ReasonCode = reasonCode,
                Scope = scope,
                SanitizedIdentity = identity.ToSanitizedString()
            };
        }

        private static HistoricalBaselineAdoptionResult Blocked(DatabaseIdentity identity, string scope, string reasonCode)
        {
            return new HistoricalBaselineAdoptionResult
            {
                Status = HistoricalBaselineAdoptionStatus.Blocked,
                ReasonCode = reasonCode,
                Scope = scope,
                SanitizedIdentity = identity.ToSanitizedString()
            };
        }
    }
}
