using System.Text.RegularExpressions;

namespace checklistWs.Services.Tenant
{
    public sealed class ProductosServiciosSchemaBootstrapper : IProductosServiciosSchemaBootstrapper
    {
        private readonly IDatabaseStateClassifier _classifier;
        private readonly ISchemaVersionRepository _schemaVersionRepository;
        private readonly ISchemaContractProvider _contractProvider;
        private readonly ISchemaManifestProvider _manifestProvider;
        private readonly ISchemaProvisionExecutor _executor;
        private readonly ISchemaProvisionLock _provisionLock;

        public ProductosServiciosSchemaBootstrapper(
            IDatabaseStateClassifier classifier,
            ISchemaVersionRepository schemaVersionRepository,
            ISchemaContractProvider contractProvider,
            ISchemaManifestProvider manifestProvider,
            ISchemaProvisionExecutor executor,
            ISchemaProvisionLock provisionLock)
        {
            _classifier = classifier;
            _schemaVersionRepository = schemaVersionRepository;
            _contractProvider = contractProvider;
            _manifestProvider = manifestProvider;
            _executor = executor;
            _provisionLock = provisionLock;
        }

        public async Task<SchemaProvisionResult> ProvisionScopeAsync(
            TenantDatabaseDescriptor descriptor,
            DatabaseIdentity identity,
            string scope,
            CancellationToken cancellationToken = default)
        {
            if (identity == null || string.IsNullOrWhiteSpace(identity.Fingerprint) || !string.Equals(scope, DatabaseScopes.ProductosServicios, StringComparison.OrdinalIgnoreCase))
            {
                return NoProvision(identity, scope, "PROVISION_NOT_ALLOWED_INVALID_CONTEXT");
            }

            DatabaseClassificationResult initialClassification = await _classifier.ClassifyAsync(descriptor, identity, scope, cancellationToken);
            if (initialClassification.State != DatabaseStructureState.Empty || !initialClassification.IsAvailable)
            {
                return NotAllowed(initialClassification);
            }

            SchemaContract contract = _contractProvider.GetContract(DatabaseScopes.ProductosServicios, 1);
            SchemaManifest manifest = _manifestProvider.CreateManifest(contract);

            try
            {
                return await _provisionLock.ExecuteAsync(descriptor, identity, scope, async lockedCancellationToken =>
                {
                    SchemaControlInfrastructureResult infrastructure = await _schemaVersionRepository.EnsureSchemaControlInfrastructureAsync(descriptor, lockedCancellationToken);
                    if (infrastructure.Status != SchemaControlInfrastructureStatus.Ready)
                    {
                        return NoProvision(identity, scope, "SCHEMA_CONTROL_INFRASTRUCTURE_CONFLICT", warnings: infrastructure.Conflicts);
                    }

                    DateTime started = DateTime.UtcNow;
                    Guid attemptId = await _schemaVersionRepository.BeginAttemptAsync(descriptor, new SchemaControlAttempt
                    {
                        DatabaseIdentityKey = identity.Fingerprint,
                        Scope = scope,
                        OperationType = "PROVISION",
                        FromVersion = null,
                        ToVersion = contract.ContractVersion,
                        StartedAtUtc = started,
                        Result = "STARTED",
                        ReasonCode = "EMPTY_SCOPE_CONFIRMED"
                    }, lockedCancellationToken);

                    bool stateWritten = false;
                    try
                    {
                        await _schemaVersionRepository.GetAttemptsAsync(descriptor, identity, scope, lockedCancellationToken);
                        DatabaseClassificationResult lockedClassification = await _classifier.ClassifyAsync(descriptor, identity, scope, lockedCancellationToken);
                        if (lockedClassification.State != DatabaseStructureState.Empty || !lockedClassification.IsAvailable)
                        {
                            string reason = ReasonForState(lockedClassification);
                            await _schemaVersionRepository.CompleteAttemptAsync(descriptor, attemptId, "PASS", reason, null, lockedCancellationToken);
                            return NoProvision(identity, scope, reason, attemptId, contract.ContractVersion, manifest.ManifestHash, lockedClassification.Evidence, lockedClassification.Warnings);
                        }

                        SchemaProvisionExecutionResult execution = await _executor.ProvisionAsync(descriptor, identity, contract, manifest, lockedCancellationToken);
                        if (!execution.Succeeded)
                        {
                            await _schemaVersionRepository.CompleteAttemptAsync(descriptor, attemptId, "FAIL", execution.ReasonCode, string.Join("; ", execution.Discrepancies), lockedCancellationToken);
                            return new SchemaProvisionResult
                            {
                                Status = SchemaProvisionResultStatus.Failed,
                                ReasonCode = execution.ReasonCode,
                                Scope = scope,
                                SanitizedIdentity = identity.ToSanitizedString(),
                                AttemptId = attemptId,
                                ContractVersion = contract.ContractVersion,
                                ManifestHash = manifest.ManifestHash,
                                Discrepancies = execution.Discrepancies.ToArray(),
                                Evidence = execution.Evidence.ToArray()
                            };
                        }

                        await _schemaVersionRepository.SetConfirmedStateAsync(descriptor, new SchemaControlState
                        {
                            DatabaseIdentityKey = identity.Fingerprint,
                            Scope = scope,
                            CurrentVersion = contract.ContractVersion,
                            ManifestHash = manifest.ManifestHash,
                            LastValidatedAtUtc = DateTime.UtcNow,
                            LastMigratedAtUtc = null,
                            LastResult = "PROVISIONED/PASS",
                            CreatedAtUtc = started,
                            UpdatedAtUtc = DateTime.UtcNow
                        }, lockedCancellationToken);
                        stateWritten = true;

                        await _schemaVersionRepository.RecordHistoryAsync(descriptor, new SchemaControlHistory
                        {
                            DatabaseIdentityKey = identity.Fingerprint,
                            Scope = scope,
                            EventType = "PROVISIONED",
                            FromVersion = null,
                            ToVersion = contract.ContractVersion,
                            OperationId = attemptId.ToString("D"),
                            StartedAtUtc = started,
                            CompletedAtUtc = DateTime.UtcNow,
                            Result = "PASS",
                            Details = $"ManifestHash={manifest.ManifestHash}; Tables={execution.TablesCreated}; Columns={execution.ColumnsCreated}; Indexes={execution.IndexesCreated}; ForeignKeys={execution.ForeignKeysCreated}; Checks={execution.ChecksCreated}"
                        }, lockedCancellationToken);

                        await _schemaVersionRepository.CompleteAttemptAsync(descriptor, attemptId, "PASS", "PROVISIONED", null, lockedCancellationToken);

                        return new SchemaProvisionResult
                        {
                            Status = SchemaProvisionResultStatus.Provisioned,
                            ReasonCode = "PROVISIONED",
                            Scope = scope,
                            SanitizedIdentity = identity.ToSanitizedString(),
                            AttemptId = attemptId,
                            ContractVersion = contract.ContractVersion,
                            ManifestHash = manifest.ManifestHash,
                            TablesCreated = execution.TablesCreated,
                            ColumnsCreated = execution.ColumnsCreated,
                            IndexesCreated = execution.IndexesCreated,
                            ForeignKeysCreated = execution.ForeignKeysCreated,
                            ChecksCreated = execution.ChecksCreated,
                            Evidence = execution.Evidence.ToArray()
                        };
                    }
                    catch (Exception ex)
                    {
                        if (stateWritten)
                        {
                            await _schemaVersionRepository.SetConfirmedStateAsync(descriptor, new SchemaControlState
                            {
                                DatabaseIdentityKey = identity.Fingerprint,
                                Scope = scope,
                                CurrentVersion = null,
                                ManifestHash = null,
                                LastValidatedAtUtc = null,
                                LastMigratedAtUtc = null,
                                LastResult = "PROVISION_FAILED",
                                CreatedAtUtc = started,
                                UpdatedAtUtc = DateTime.UtcNow
                            }, CancellationToken.None);
                        }

                        await _schemaVersionRepository.CompleteAttemptAsync(descriptor, attemptId, "FAIL", "PROVISION_FAILED", Sanitize(ex.Message), CancellationToken.None);
                        return new SchemaProvisionResult
                        {
                            Status = SchemaProvisionResultStatus.Failed,
                            ReasonCode = "PROVISION_FAILED",
                            Scope = scope,
                            SanitizedIdentity = identity.ToSanitizedString(),
                            AttemptId = attemptId,
                            ContractVersion = contract.ContractVersion,
                            ManifestHash = manifest.ManifestHash,
                            Warnings = new[] { Sanitize(ex.Message) ?? "PROVISION_FAILED" }
                        };
                    }
                }, cancellationToken);
            }
            catch (SchemaOperationLockException ex)
            {
                return NoProvision(identity, scope, ex.ReasonCode, contractVersion: contract.ContractVersion, manifestHash: manifest.ManifestHash, warnings: new[] { ex.Resource });
            }
        }

        private static SchemaProvisionResult NotAllowed(DatabaseClassificationResult classification)
        {
            return new SchemaProvisionResult
            {
                Status = SchemaProvisionResultStatus.NoProvision,
                ReasonCode = ReasonForState(classification),
                Scope = classification.Scope,
                SanitizedIdentity = classification.SanitizedIdentity,
                Evidence = classification.Evidence.ToArray(),
                Warnings = classification.Warnings.ToArray()
            };
        }

        private static string ReasonForState(DatabaseClassificationResult classification)
        {
            if (!classification.IsAvailable)
            {
                return "PROVISION_NOT_ALLOWED_UNAVAILABLE";
            }

            return classification.State switch
            {
                DatabaseStructureState.Partial => "PROVISION_NOT_ALLOWED_PARTIAL",
                DatabaseStructureState.Current => "ALREADY_PROVISIONED",
                DatabaseStructureState.Outdated => "PROVISION_NOT_ALLOWED_OUTDATED",
                DatabaseStructureState.Future => "PROVISION_NOT_ALLOWED_FUTURE",
                DatabaseStructureState.Unknown => "PROVISION_NOT_ALLOWED_UNKNOWN",
                DatabaseStructureState.Empty => "EMPTY_SCOPE_CONFIRMED",
                _ => "PROVISION_NOT_ALLOWED"
            };
        }

        private static SchemaProvisionResult NoProvision(
            DatabaseIdentity? identity,
            string scope,
            string reasonCode,
            Guid? attemptId = null,
            int? contractVersion = null,
            string? manifestHash = null,
            IReadOnlyCollection<string>? evidence = null,
            IReadOnlyCollection<string>? warnings = null)
        {
            return new SchemaProvisionResult
            {
                Status = SchemaProvisionResultStatus.NoProvision,
                ReasonCode = reasonCode,
                Scope = scope,
                SanitizedIdentity = identity?.ToSanitizedString() ?? string.Empty,
                AttemptId = attemptId,
                ContractVersion = contractVersion,
                ManifestHash = manifestHash,
                Evidence = evidence ?? Array.Empty<string>(),
                Warnings = warnings ?? Array.Empty<string>()
            };
        }

        private static string? Sanitize(string? value)
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
