namespace checklistWs.Services.Tenant
{
    public sealed class SchemaMigrationRunner : ISchemaMigrationRunner
    {
        private readonly IDatabaseStateClassifier _classifier;
        private readonly ISchemaVersionRepository _repository;
        private readonly ISchemaMigrationPackageProvider _packageProvider;
        private readonly ISchemaMigrationResolver _resolver;
        private readonly ISchemaMigrationSqlExecutor _executor;
        private readonly ISchemaProvisionLock _lock;
        private readonly ISchemaContractPhysicalValidator _validator;

        public SchemaMigrationRunner(
            IDatabaseStateClassifier classifier,
            ISchemaVersionRepository repository,
            ISchemaMigrationPackageProvider packageProvider,
            ISchemaMigrationResolver resolver,
            ISchemaMigrationSqlExecutor executor,
            ISchemaProvisionLock schemaLock,
            ISchemaContractPhysicalValidator validator)
        {
            _classifier = classifier;
            _repository = repository;
            _packageProvider = packageProvider;
            _resolver = resolver;
            _executor = executor;
            _lock = schemaLock;
            _validator = validator;
        }

        public async Task<SchemaMigrationExecutionResult> MigrateToLatestAsync(
            TenantDatabaseDescriptor descriptor,
            DatabaseIdentity identity,
            string scope,
            CancellationToken cancellationToken = default)
        {
            if (identity == null || string.IsNullOrWhiteSpace(identity.Fingerprint) || !string.Equals(scope, DatabaseScopes.ProductosServicios, StringComparison.OrdinalIgnoreCase))
            {
                return Result(SchemaMigrationExecutionStatus.RequiresReview, "MIGRATION_CONTEXT_INVALID", identity, scope);
            }

            SchemaMigrationPackage package = _packageProvider.GetPackage(scope);
            try
            {
                return await _lock.ExecuteAsync(descriptor, identity, scope, async lockedCancellationToken =>
                {
                DatabaseClassificationResult classification = await _classifier.ClassifyAsync(descriptor, identity, scope, lockedCancellationToken);
                if (!classification.IsAvailable)
                {
                    return Result(SchemaMigrationExecutionStatus.RequiresReview, "DATABASE_UNAVAILABLE", identity, scope);
                }

                if (classification.State == DatabaseStructureState.Empty)
                {
                    return Result(SchemaMigrationExecutionStatus.NoProvision, "MIGRATION_NOT_ALLOWED_EMPTY_BOOTSTRAP_REQUIRED", identity, scope);
                }

                if (classification.State == DatabaseStructureState.Partial || classification.State == DatabaseStructureState.Unknown)
                {
                    return Result(SchemaMigrationExecutionStatus.RequiresReview, "REQUIERE_REVISION", identity, scope);
                }

                SchemaControlState? state = await _repository.GetStateAsync(descriptor, identity, scope, lockedCancellationToken);
                if (state?.CurrentVersion == null)
                {
                    return Result(SchemaMigrationExecutionStatus.RequiresReview, "STATE_VERSION_MISSING", identity, scope);
                }

                IReadOnlyCollection<SchemaControlHistory> history = await _repository.GetHistoryAsync(descriptor, identity, scope, lockedCancellationToken);
                SchemaMigrationResolution resolution = _resolver.GetPendingMigrations(identity, scope, state.CurrentVersion.Value, package.Release.LatestSchemaVersion, package, history);
                if (resolution.Status == SchemaMigrationResolutionStatus.NoPendingMigrations || resolution.Status == SchemaMigrationResolutionStatus.AlreadyApplied)
                {
                    return Result(SchemaMigrationExecutionStatus.NoProvision, resolution.ReasonCode, identity, scope);
                }

                if (resolution.Status != SchemaMigrationResolutionStatus.Ready)
                {
                    return Result(ToExecutionStatus(resolution.Status), resolution.ReasonCode, identity, scope);
                }

                SchemaMigrationExecutionResult last = Result(SchemaMigrationExecutionStatus.NoProvision, "NO_PENDING_MIGRATIONS", identity, scope);
                foreach (SchemaMigrationDefinition migration in resolution.PendingMigrations)
                {
                    last = await ApplyOneAsync(descriptor, identity, scope, migration, lockedCancellationToken);
                    if (last.Status != SchemaMigrationExecutionStatus.Migrated)
                    {
                        return last;
                    }
                }

                    return last;
                }, cancellationToken);
            }
            catch (SchemaOperationLockException ex)
            {
                return Result(ex.Code == SchemaOperationLockResultCode.Timeout ? SchemaMigrationExecutionStatus.LockTimeout : SchemaMigrationExecutionStatus.Failed, ex.ReasonCode, identity, scope);
            }
        }

        public async Task<SchemaMigrationExecutionResult> RecoverUncertainCommitAsync(
            TenantDatabaseDescriptor descriptor,
            DatabaseIdentity identity,
            string scope,
            string migrationId,
            Guid attemptId,
            CancellationToken cancellationToken = default)
        {
            SchemaMigrationPackage package = _packageProvider.GetPackage(scope);
            try
            {
                return await _lock.ExecuteAsync(descriptor, identity, scope, async lockedCancellationToken =>
                {
                SchemaMigrationDefinition? migration = package.Migrations.SingleOrDefault(item => string.Equals(item.MigrationId, migrationId, StringComparison.OrdinalIgnoreCase));
                if (migration == null)
                {
                    await _repository.CompleteAttemptAsync(descriptor, attemptId, "FAIL", "MIGRATION_NOT_IN_PACKAGE", null, lockedCancellationToken);
                    return Result(SchemaMigrationExecutionStatus.RequiresReview, "MIGRATION_NOT_IN_PACKAGE", identity, scope, attemptId, migrationId);
                }

                SchemaControlState? state = await _repository.GetStateAsync(descriptor, identity, scope, lockedCancellationToken);
                IReadOnlyCollection<SchemaControlHistory> history = await _repository.GetHistoryAsync(descriptor, identity, scope, lockedCancellationToken);
                SchemaControlHistory? migrated = history.SingleOrDefault(item =>
                    string.Equals(item.EventType, "MIGRATED", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(item.Result, "PASS", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(SchemaMigrationHistoryDetails.Get(item.Details, "MigrationId") ?? item.OperationId, migration.MigrationId, StringComparison.OrdinalIgnoreCase));
                SchemaContractValidationResult validation = await _validator.ValidateAsync(descriptor, migration.TargetContract, lockedCancellationToken);

                if (state?.CurrentVersion == migration.ToVersion &&
                    string.Equals(state.ManifestHash, migration.TargetManifestHash, StringComparison.OrdinalIgnoreCase) &&
                    migrated != null &&
                    validation.IsValid)
                {
                    await _repository.CompleteAttemptAsync(descriptor, attemptId, "PASS", "COMMIT_RECOVERED", null, lockedCancellationToken);
                    return Result(SchemaMigrationExecutionStatus.Recovered, "COMMIT_RECOVERED", identity, scope, attemptId, migration.MigrationId, migration.FromVersion, migration.ToVersion, migration.TargetManifestHash);
                }

                    await _repository.CompleteAttemptAsync(descriptor, attemptId, "FAIL", "REQUIERE_REVISION", string.Join("; ", validation.Discrepancies), lockedCancellationToken);
                    return Result(SchemaMigrationExecutionStatus.RequiresReview, "REQUIERE_REVISION", identity, scope, attemptId, migration.MigrationId, migration.FromVersion, migration.ToVersion, migration.TargetManifestHash, validation.Discrepancies);
                }, cancellationToken);
            }
            catch (SchemaOperationLockException ex)
            {
                await _repository.CompleteAttemptAsync(descriptor, attemptId, ex.Code == SchemaOperationLockResultCode.Timeout ? "LOCK_TIMEOUT" : "FAIL", ex.ReasonCode, null, CancellationToken.None);
                return Result(ex.Code == SchemaOperationLockResultCode.Timeout ? SchemaMigrationExecutionStatus.LockTimeout : SchemaMigrationExecutionStatus.Failed, ex.ReasonCode, identity, scope, attemptId, migrationId);
            }
        }

        private async Task<SchemaMigrationExecutionResult> ApplyOneAsync(
            TenantDatabaseDescriptor descriptor,
            DatabaseIdentity identity,
            string scope,
            SchemaMigrationDefinition migration,
            CancellationToken cancellationToken)
        {
            DateTime started = DateTime.UtcNow;
            Guid attemptId = await _repository.BeginAttemptAsync(descriptor, new SchemaControlAttempt
            {
                DatabaseIdentityKey = identity.Fingerprint,
                Scope = scope,
                OperationType = "MIGRATE",
                FromVersion = migration.FromVersion,
                ToVersion = migration.ToVersion,
                StartedAtUtc = started,
                Result = "STARTED",
                ReasonCode = migration.MigrationId
            }, cancellationToken);

            try
            {
                await _executor.ExecuteAsync(descriptor, identity, migration, (_, token) => _validator.ValidateAsync(descriptor, migration.TargetContract, token), cancellationToken);

                await _repository.RecordHistoryAsync(descriptor, new SchemaControlHistory
                {
                    DatabaseIdentityKey = identity.Fingerprint,
                    Scope = scope,
                    EventType = "MIGRATED",
                    FromVersion = migration.FromVersion,
                    ToVersion = migration.ToVersion,
                    OperationId = migration.MigrationId,
                    StartedAtUtc = started,
                    CompletedAtUtc = DateTime.UtcNow,
                    Result = "PASS",
                    Details = SchemaMigrationHistoryDetails.Build(migration)
                }, cancellationToken);

                await _repository.SetConfirmedStateAsync(descriptor, new SchemaControlState
                {
                    DatabaseIdentityKey = identity.Fingerprint,
                    Scope = scope,
                    CurrentVersion = migration.ToVersion,
                    ManifestHash = migration.TargetManifestHash,
                    LastValidatedAtUtc = DateTime.UtcNow,
                    LastMigratedAtUtc = DateTime.UtcNow,
                    LastResult = $"MIGRATED/PASS/{migration.MigrationId}",
                    CreatedAtUtc = started,
                    UpdatedAtUtc = DateTime.UtcNow
                }, cancellationToken);

                await _repository.CompleteAttemptAsync(descriptor, attemptId, "PASS", "MIGRATED", null, cancellationToken);
                return Result(SchemaMigrationExecutionStatus.Migrated, "MIGRATED", identity, scope, attemptId, migration.MigrationId, migration.FromVersion, migration.ToVersion, migration.TargetManifestHash);
            }
            catch (SchemaMigrationValidationException ex)
            {
                await _repository.CompleteAttemptAsync(descriptor, attemptId, "FAIL", "MIGRATION_TARGET_VALIDATION_FAILED", string.Join("; ", ex.Discrepancies), CancellationToken.None);
                return Result(SchemaMigrationExecutionStatus.Failed, "MIGRATION_TARGET_VALIDATION_FAILED", identity, scope, attemptId, migration.MigrationId, migration.FromVersion, migration.ToVersion, migration.TargetManifestHash, ex.Discrepancies);
            }
            catch (Exception ex)
            {
                await _repository.CompleteAttemptAsync(descriptor, attemptId, "FAIL", "MIGRATION_FAILED", SchemaMigrationSanitizer.Sanitize(ex.Message), CancellationToken.None);
                return Result(SchemaMigrationExecutionStatus.Failed, "MIGRATION_FAILED", identity, scope, attemptId, migration.MigrationId, migration.FromVersion, migration.ToVersion, migration.TargetManifestHash);
            }
        }

        private static SchemaMigrationExecutionStatus ToExecutionStatus(SchemaMigrationResolutionStatus status)
        {
            return status == SchemaMigrationResolutionStatus.HistoryInconsistent || status == SchemaMigrationResolutionStatus.RequiresReview
                ? SchemaMigrationExecutionStatus.RequiresReview
                : SchemaMigrationExecutionStatus.Failed;
        }

        private static SchemaMigrationExecutionResult Result(
            SchemaMigrationExecutionStatus status,
            string reasonCode,
            DatabaseIdentity? identity,
            string scope,
            Guid? attemptId = null,
            string? migrationId = null,
            int? fromVersion = null,
            int? toVersion = null,
            string? manifestHash = null,
            IReadOnlyCollection<string>? discrepancies = null)
        {
            return new SchemaMigrationExecutionResult
            {
                Status = status,
                ReasonCode = reasonCode,
                SanitizedIdentity = identity?.ToSanitizedString() ?? string.Empty,
                Scope = scope,
                AttemptId = attemptId,
                MigrationId = migrationId,
                FromVersion = fromVersion,
                ToVersion = toVersion,
                ManifestHash = manifestHash,
                Discrepancies = discrepancies ?? Array.Empty<string>()
            };
        }
    }
}
