namespace checklistWs.Services.Tenant
{
    public sealed class SchemaMigrationResolver : ISchemaMigrationResolver
    {
        public SchemaMigrationResolution GetPendingMigrations(
            DatabaseIdentity identity,
            string scope,
            int currentVersion,
            int? targetVersion,
            SchemaMigrationPackage package,
            IReadOnlyCollection<SchemaControlHistory> history)
        {
            if (identity == null || string.IsNullOrWhiteSpace(scope) || package == null)
            {
                return Block(SchemaMigrationResolutionStatus.PackageInvalid, "MIGRATION_CONTEXT_INVALID");
            }

            if (!string.Equals(package.Release.Scope, scope, StringComparison.OrdinalIgnoreCase))
            {
                return Block(SchemaMigrationResolutionStatus.PackageInvalid, "PACKAGE_SCOPE_MISMATCH");
            }

            int target = targetVersion ?? package.Release.LatestSchemaVersion;
            if (target > package.Release.LatestSchemaVersion)
            {
                return Block(SchemaMigrationResolutionStatus.TargetVersionInvalid, "TARGET_VERSION_ABOVE_RELEASE");
            }

            if (currentVersion > package.Release.LatestSchemaVersion)
            {
                return Block(SchemaMigrationResolutionStatus.RequiresReview, "VERSION_FUTURA_REQUIERE_REVISION");
            }

            if (currentVersion == target)
            {
                return new SchemaMigrationResolution { Status = SchemaMigrationResolutionStatus.NoPendingMigrations, ReasonCode = "NO_PENDING_MIGRATIONS" };
            }

            if (currentVersion > target)
            {
                return Block(SchemaMigrationResolutionStatus.VersionMismatch, "CURRENT_VERSION_ABOVE_TARGET");
            }

            Dictionary<string, SchemaMigrationDefinition> byId = new(StringComparer.OrdinalIgnoreCase);
            foreach (SchemaMigrationDefinition migration in package.Migrations)
            {
                string? invalid = ValidatePackageEntry(migration, package);
                if (invalid != null)
                {
                    return Block(SchemaMigrationResolutionStatus.PackageInvalid, invalid);
                }

                if (!byId.TryAdd(migration.MigrationId, migration))
                {
                    return Block(SchemaMigrationResolutionStatus.PackageInvalid, "DUPLICATE_MIGRATION_ID");
                }
            }

            foreach (string approvedId in package.Release.ApprovedMigrationIds)
            {
                if (!byId.ContainsKey(approvedId))
                {
                    return Block(SchemaMigrationResolutionStatus.PackageInvalid, "APPROVED_MIGRATION_MISSING");
                }
            }

            foreach (SchemaControlHistory applied in history.Where(item => string.Equals(item.EventType, "MIGRATED", StringComparison.OrdinalIgnoreCase) && string.Equals(item.Result, "PASS", StringComparison.OrdinalIgnoreCase)))
            {
                string migrationId = SchemaMigrationHistoryDetails.Get(applied.Details, "MigrationId") ?? applied.OperationId ?? string.Empty;
                if (string.IsNullOrWhiteSpace(migrationId))
                {
                    return Block(SchemaMigrationResolutionStatus.HistoryInconsistent, "HISTORIAL_INCONSISTENTE");
                }

                if (!byId.TryGetValue(migrationId, out SchemaMigrationDefinition? packageMigration))
                {
                    return Block(SchemaMigrationResolutionStatus.HistoryInconsistent, "HISTORIAL_INCONSISTENTE");
                }

                string? sqlHash = SchemaMigrationHistoryDetails.Get(applied.Details, "SqlHash");
                string? targetHash = SchemaMigrationHistoryDetails.Get(applied.Details, "TargetManifestHash");
                if (!string.Equals(sqlHash, packageMigration.SqlHash, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(targetHash, packageMigration.TargetManifestHash, StringComparison.OrdinalIgnoreCase) ||
                    applied.FromVersion != packageMigration.FromVersion ||
                    applied.ToVersion != packageMigration.ToVersion)
                {
                    return Block(SchemaMigrationResolutionStatus.HistoryInconsistent, "HISTORIAL_INCONSISTENTE");
                }
            }

            List<SchemaMigrationDefinition> pending = new();
            HashSet<int> seenVersions = new();
            int cursor = currentVersion;
            while (cursor < target)
            {
                if (!seenVersions.Add(cursor))
                {
                    return Block(SchemaMigrationResolutionStatus.ChainCycle, "MIGRATION_CHAIN_CYCLE");
                }

                SchemaMigrationDefinition[] candidates = package.Migrations
                    .Where(migration => migration.FromVersion == cursor && migration.ToVersion <= target)
                    .OrderBy(migration => migration.Order)
                    .ToArray();

                if (candidates.Length == 0)
                {
                    return Block(SchemaMigrationResolutionStatus.ChainGap, "MIGRATION_CHAIN_GAP");
                }

                if (candidates.Length > 1)
                {
                    return Block(SchemaMigrationResolutionStatus.ChainBranch, "MIGRATION_CHAIN_BRANCH");
                }

                SchemaMigrationDefinition next = candidates[0];
                if (next.ToVersion <= next.FromVersion)
                {
                    return Block(SchemaMigrationResolutionStatus.ChainCycle, "MIGRATION_CHAIN_CYCLE");
                }

                if (history.Any(item => string.Equals(item.EventType, "MIGRATED", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(item.Result, "PASS", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(SchemaMigrationHistoryDetails.Get(item.Details, "MigrationId") ?? item.OperationId, next.MigrationId, StringComparison.OrdinalIgnoreCase)))
                {
                    cursor = next.ToVersion;
                    continue;
                }

                pending.Add(next);
                cursor = next.ToVersion;
            }

            return pending.Count == 0
                ? new SchemaMigrationResolution { Status = SchemaMigrationResolutionStatus.AlreadyApplied, ReasonCode = "ALREADY_APPLIED" }
                : new SchemaMigrationResolution { Status = SchemaMigrationResolutionStatus.Ready, ReasonCode = "PENDING_MIGRATIONS_RESOLVED", PendingMigrations = pending };
        }

        private static string? ValidatePackageEntry(SchemaMigrationDefinition migration, SchemaMigrationPackage package)
        {
            if (string.IsNullOrWhiteSpace(migration.MigrationId) ||
                string.IsNullOrWhiteSpace(migration.BaselineId) ||
                string.IsNullOrWhiteSpace(migration.Scope) ||
                string.IsNullOrWhiteSpace(migration.SqlHash) ||
                string.IsNullOrWhiteSpace(migration.TargetManifestHash) ||
                string.IsNullOrWhiteSpace(migration.TransactionMode) ||
                string.IsNullOrWhiteSpace(migration.Risk) ||
                string.IsNullOrWhiteSpace(migration.RecoveryPolicy) ||
                migration.Timeout <= TimeSpan.Zero)
            {
                return "MIGRATION_METADATA_INCOMPLETE";
            }

            if (!string.Equals(migration.Scope, package.Release.Scope, StringComparison.OrdinalIgnoreCase))
            {
                return "MIGRATION_SCOPE_MISMATCH";
            }

            if (!string.Equals(migration.BaselineId, package.Release.BaselineId, StringComparison.OrdinalIgnoreCase))
            {
                return "BASELINE_INCOMPATIBLE";
            }

            if (migration.FromVersion <= 0 || migration.ToVersion <= 0)
            {
                return "FROM_VERSION_INCOMPATIBLE";
            }

            if (!migration.AutoApplicable)
            {
                return "MIGRATION_NOT_AUTO_APPLICABLE";
            }

            if (!string.Equals(migration.TransactionMode, "SingleTransaction", StringComparison.OrdinalIgnoreCase))
            {
                return "TRANSACTION_MODE_UNSUPPORTED";
            }

            if (migration.UpSql.Contains("\nGO", StringComparison.OrdinalIgnoreCase) || migration.UpSql.Trim().Equals("GO", StringComparison.OrdinalIgnoreCase))
            {
                return "SQL_BATCH_GO_NOT_ALLOWED";
            }

            if (!string.Equals(SchemaMigrationHash.Sha256(migration.UpSql), migration.SqlHash, StringComparison.OrdinalIgnoreCase))
            {
                return "SQL_HASH_MISMATCH";
            }

            SchemaManifest targetManifest = new SchemaManifestProvider().CreateManifest(migration.TargetContract);
            if (!string.Equals(targetManifest.ManifestHash, migration.TargetManifestHash, StringComparison.OrdinalIgnoreCase))
            {
                return "TARGET_MANIFEST_HASH_MISMATCH";
            }

            return null;
        }

        private static SchemaMigrationResolution Block(SchemaMigrationResolutionStatus status, string reasonCode)
        {
            return new SchemaMigrationResolution { Status = status, ReasonCode = reasonCode };
        }
    }
}
