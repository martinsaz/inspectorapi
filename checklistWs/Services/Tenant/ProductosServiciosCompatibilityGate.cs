namespace checklistWs.Services.Tenant
{
    public sealed class ProductosServiciosCompatibilityGate : IProductosServiciosCompatibilityGate
    {
        private readonly IDatabaseIdentityResolver _identityResolver;
        private readonly IDatabaseStateClassifier _classifier;
        private readonly ISchemaVersionRepository _repository;
        private readonly IKnownSchemaVersionProvider _knownSchemaVersionProvider;
        private readonly ISchemaContractProvider _contractProvider;
        private readonly ISchemaManifestProvider _manifestProvider;
        private readonly ISchemaDriftValidator _driftValidator;
        private readonly ILogger<ProductosServiciosCompatibilityGate> _logger;

        public ProductosServiciosCompatibilityGate(
            IDatabaseIdentityResolver identityResolver,
            IDatabaseStateClassifier classifier,
            ISchemaVersionRepository repository,
            IKnownSchemaVersionProvider knownSchemaVersionProvider,
            ISchemaContractProvider contractProvider,
            ISchemaManifestProvider manifestProvider,
            ISchemaDriftValidator driftValidator,
            ILogger<ProductosServiciosCompatibilityGate> logger)
        {
            _identityResolver = identityResolver;
            _classifier = classifier;
            _repository = repository;
            _knownSchemaVersionProvider = knownSchemaVersionProvider;
            _contractProvider = contractProvider;
            _manifestProvider = manifestProvider;
            _driftValidator = driftValidator;
            _logger = logger;
        }

        public async Task<CompatibilityDecision> EvaluateAsync(
            TenantDatabaseDescriptor descriptor,
            string scope = DatabaseScopes.ProductosServicios,
            CancellationToken cancellationToken = default)
        {
            string referenceId = Guid.NewGuid().ToString("N");
            DateTime started = DateTime.UtcNow;
            DatabaseIdentity? identity = null;
            try
            {
                if (descriptor == null || string.IsNullOrWhiteSpace(descriptor.ConnectionString) || !string.Equals(scope, DatabaseScopes.ProductosServicios, StringComparison.OrdinalIgnoreCase))
                {
                    return Block(referenceId, identity, scope, null, null, null, "TENANT_CONTEXT_INVALID", false, started);
                }

                identity = await _identityResolver.ResolveAsync(descriptor, cancellationToken);
                if (identity == null || string.IsNullOrWhiteSpace(identity.Fingerprint))
                {
                    return Block(referenceId, identity, scope, null, null, null, "DATABASE_IDENTITY_INVALID", true, started);
                }

                int? latest = _knownSchemaVersionProvider.GetKnownCurrentVersion(scope);
                if (latest == null)
                {
                    return Block(referenceId, identity, scope, null, null, null, "VERSION_INCOMPATIBLE", false, started);
                }

                DatabaseClassificationResult classification = await _classifier.ClassifyAsync(descriptor, identity, scope, cancellationToken);
                if (!classification.IsAvailable)
                {
                    return Block(referenceId, identity, scope, null, latest, null, "SCHEMA_UNAVAILABLE", true, started);
                }

                string? classificationBlock = ClassificationBlockReason(classification.State);
                if (classificationBlock != null)
                {
                    return Block(referenceId, identity, scope, null, latest, null, classificationBlock, IsRetryable(classificationBlock), started);
                }

                SchemaControlState? state = await _repository.GetStateAsync(descriptor, identity, scope, cancellationToken);
                if (state?.CurrentVersion == null)
                {
                    return Block(referenceId, identity, scope, null, latest, null, "VERSION_EVIDENCE_MISSING", false, started);
                }

                IReadOnlyCollection<SchemaControlAttempt> attempts = await _repository.GetAttemptsAsync(descriptor, identity, scope, cancellationToken);
                SchemaControlAttempt? active = attempts
                    .Where(item => string.Equals(item.Result, "STARTED", StringComparison.OrdinalIgnoreCase) && item.CompletedAtUtc == null)
                    .OrderByDescending(item => item.StartedAtUtc)
                    .FirstOrDefault();
                if (active != null)
                {
                    string activeOperationReason = string.Equals(active.OperationType, "PROVISION", StringComparison.OrdinalIgnoreCase)
                        ? "SCHEMA_PREPARING"
                        : "MIGRATION_IN_PROGRESS";
                    return Block(referenceId, identity, scope, state.CurrentVersion, latest, null, activeOperationReason, true, started);
                }

                if (state.CurrentVersion > latest)
                {
                    return Block(referenceId, identity, scope, state.CurrentVersion, latest, null, "SCHEMA_FUTURE", false, started);
                }

                if (state.CurrentVersion < latest)
                {
                    return Block(referenceId, identity, scope, state.CurrentVersion, latest, null, "SCHEMA_OUTDATED", false, started);
                }

                SchemaContract contract;
                try
                {
                    contract = _contractProvider.GetContract(scope, state.CurrentVersion);
                }
                catch
                {
                    return Block(referenceId, identity, scope, state.CurrentVersion, latest, null, "VERSION_INCOMPATIBLE", false, started);
                }

                SchemaManifest expectedManifest = _manifestProvider.CreateManifest(contract);
                if (!string.Equals(state.ManifestHash, expectedManifest.ManifestHash, StringComparison.OrdinalIgnoreCase))
                {
                    return Block(referenceId, identity, scope, state.CurrentVersion, latest, null, "MANIFEST_HASH_MISMATCH", false, started);
                }

                SchemaDriftReport drift = await _driftValidator.ValidateAsync(descriptor, identity, scope, state.CurrentVersion, state.ManifestHash, cancellationToken);
                string reason = ReasonFromDrift(drift);
                if (!string.Equals(reason, "COMPATIBLE", StringComparison.OrdinalIgnoreCase))
                {
                    return Block(referenceId, identity, scope, state.CurrentVersion, latest, drift.GlobalResult, reason, IsRetryable(reason), started);
                }

                return new CompatibilityDecision
                {
                    IsAllowed = true,
                    SanitizedIdentity = identity.ToSanitizedString(),
                    Scope = scope,
                    CurrentVersion = state.CurrentVersion,
                    LatestSupportedVersion = latest,
                    SchemaResult = drift.GlobalResult,
                    ReasonCode = "COMPATIBLE",
                    Retryable = false,
                    ReferenceId = referenceId,
                    CheckedAtUtc = DateTime.UtcNow
                };
            }
            catch (TenantDatabaseResolutionException)
            {
                return Block(referenceId, identity, scope, null, null, null, "TENANT_CONTEXT_INVALID", false, started);
            }
            catch (DatabaseIdentityResolutionException)
            {
                return Block(referenceId, identity, scope, null, null, null, "DATABASE_IDENTITY_INVALID", true, started);
            }
            catch (OperationCanceledException)
            {
                return Block(referenceId, identity, scope, null, null, null, "SCHEMA_UNAVAILABLE", true, started);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Gate ProductosServicios bloqueó. ReferenceId={ReferenceId} Scope={Scope} ReasonCode={ReasonCode}", referenceId, scope, "REQUIRES_REVIEW");
                return Block(referenceId, identity, scope, null, null, null, "REQUIRES_REVIEW", true, started);
            }
        }

        private static string? ClassificationBlockReason(DatabaseStructureState state) => state switch
        {
            DatabaseStructureState.Empty => "SCHEMA_EMPTY",
            DatabaseStructureState.Partial => "SCHEMA_PARTIAL",
            DatabaseStructureState.Outdated => "SCHEMA_OUTDATED",
            DatabaseStructureState.Future => "SCHEMA_FUTURE",
            DatabaseStructureState.Unknown => "SCHEMA_UNKNOWN",
            DatabaseStructureState.Current => null,
            _ => "REQUIRES_REVIEW"
        };

        private static string ReasonFromDrift(SchemaDriftReport drift) => drift.GlobalResult switch
        {
            SchemaValidationGlobalResult.SchemaOk when drift.Items.Count == 0 => "COMPATIBLE",
            SchemaValidationGlobalResult.SchemaDrift => "SCHEMA_DRIFT",
            SchemaValidationGlobalResult.SchemaDriftCritico => "SCHEMA_DRIFT_CRITICAL",
            SchemaValidationGlobalResult.ValidacionNoConcluyente => "VALIDATION_INCONCLUSIVE",
            SchemaValidationGlobalResult.VersionIncompatible => "VERSION_INCOMPATIBLE",
            SchemaValidationGlobalResult.RequiereRevision => "REQUIRES_REVIEW",
            _ => "REQUIRES_REVIEW"
        };

        private static bool IsRetryable(string reasonCode) => reasonCode is "SCHEMA_PREPARING" or "MIGRATION_IN_PROGRESS" or "SCHEMA_UNAVAILABLE" or "VALIDATION_INCONCLUSIVE" or "REQUIRES_REVIEW";

        private static CompatibilityDecision Block(string referenceId, DatabaseIdentity? identity, string scope, int? currentVersion, int? latest, SchemaValidationGlobalResult? schemaResult, string reasonCode, bool retryable, DateTime started) => new()
        {
            IsAllowed = false,
            SanitizedIdentity = identity?.ToSanitizedString() ?? string.Empty,
            Scope = scope,
            CurrentVersion = currentVersion,
            LatestSupportedVersion = latest,
            SchemaResult = schemaResult,
            ReasonCode = reasonCode,
            Retryable = retryable,
            ReferenceId = referenceId,
            CheckedAtUtc = DateTime.UtcNow
        };
    }
}
