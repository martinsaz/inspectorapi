namespace checklistWs.Services.Tenant
{
    public sealed class DatabaseStateClassifier : IDatabaseStateClassifier
    {
        private readonly IDatabaseSchemaProbe _schemaProbe;
        private readonly IDatabaseVersionEvidenceReader _versionEvidenceReader;

        public DatabaseStateClassifier(
            IDatabaseSchemaProbe schemaProbe,
            IDatabaseVersionEvidenceReader versionEvidenceReader)
        {
            _schemaProbe = schemaProbe;
            _versionEvidenceReader = versionEvidenceReader;
        }

        public async Task<DatabaseClassificationResult> ClassifyAsync(
            TenantDatabaseDescriptor descriptor,
            DatabaseIdentity identity,
            string scope,
            CancellationToken cancellationToken = default)
        {
            if (identity == null ||
                string.IsNullOrWhiteSpace(identity.ServerName) ||
                string.IsNullOrWhiteSpace(identity.DatabaseName) ||
                string.IsNullOrWhiteSpace(identity.DatabaseGuid) ||
                string.IsNullOrWhiteSpace(scope))
            {
                return Unknown(identity, scope, "DATABASE_IDENTITY_INVALID", "DatabaseIdentity o Scope inválido.");
            }

            DatabaseSchemaProbeResult probe;
            try
            {
                probe = await _schemaProbe.ProbeAsync(descriptor, scope, cancellationToken);
            }
            catch (DatabaseIdentityResolutionException ex) when (ex.Code == DatabaseIdentityResolutionCode.DatabaseIdentityUnavailable)
            {
                return new DatabaseClassificationResult
                {
                    Identity = identity,
                    SanitizedIdentity = identity.ToSanitizedString(),
                    Scope = scope.Trim(),
                    State = DatabaseStructureState.Unknown,
                    IsAvailable = false,
                    ReasonCode = "UNAVAILABLE",
                    Evidence = new[] { "La base no pudo consultarse mediante metadata read-only." }
                };
            }

            int expectedCount = probe.ExpectedTables.Count;
            int existingCount = probe.ExistingScopeTables.Count;
            List<string> evidence = new(probe.Evidence);
            List<string> warnings = new(probe.Warnings);

            if (!probe.MetadataSufficient || expectedCount == 0)
            {
                return Build(identity, scope, DatabaseStructureState.Unknown, "METADATA_INSUFFICIENT", expectedCount, existingCount, probe, evidence, warnings);
            }

            if (existingCount == 0)
            {
                return Build(identity, scope, DatabaseStructureState.Empty, "NO_SCOPE_TABLES_FOUND", expectedCount, existingCount, probe, evidence, warnings);
            }

            if (existingCount < expectedCount)
            {
                return Build(identity, scope, DatabaseStructureState.Partial, "SCOPE_TABLES_INCOMPLETE", expectedCount, existingCount, probe, evidence, warnings);
            }

            DatabaseVersionEvidence versionEvidence = await _versionEvidenceReader.ReadVersionEvidenceAsync(descriptor, identity, scope, cancellationToken);
            evidence.AddRange(versionEvidence.Evidence);
            warnings.AddRange(versionEvidence.Warnings);

            return versionEvidence.State switch
            {
                DatabaseVersionEvidenceState.Current => Build(identity, scope, DatabaseStructureState.Current, versionEvidence.ReasonCode, expectedCount, existingCount, probe, evidence, warnings),
                DatabaseVersionEvidenceState.Outdated => Build(identity, scope, DatabaseStructureState.Outdated, versionEvidence.ReasonCode, expectedCount, existingCount, probe, evidence, warnings),
                DatabaseVersionEvidenceState.Future => Build(identity, scope, DatabaseStructureState.Future, versionEvidence.ReasonCode, expectedCount, existingCount, probe, evidence, warnings),
                _ => Build(identity, scope, DatabaseStructureState.Unknown, "VERSION_EVIDENCE_MISSING", expectedCount, existingCount, probe, evidence, warnings)
            };
        }

        private static DatabaseClassificationResult Unknown(DatabaseIdentity? identity, string scope, string reasonCode, string evidence)
        {
            return new DatabaseClassificationResult
            {
                Identity = identity ?? new DatabaseIdentity(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty),
                SanitizedIdentity = identity?.ToSanitizedString() ?? string.Empty,
                Scope = scope?.Trim() ?? string.Empty,
                State = DatabaseStructureState.Unknown,
                ReasonCode = reasonCode,
                Evidence = new[] { evidence }
            };
        }

        private static DatabaseClassificationResult Build(
            DatabaseIdentity identity,
            string scope,
            DatabaseStructureState state,
            string reasonCode,
            int expectedCount,
            int existingCount,
            DatabaseSchemaProbeResult probe,
            IReadOnlyCollection<string> evidence,
            IReadOnlyCollection<string> warnings)
        {
            return new DatabaseClassificationResult
            {
                Identity = identity,
                SanitizedIdentity = identity.ToSanitizedString(),
                Scope = scope.Trim(),
                State = state,
                ReasonCode = reasonCode,
                ExpectedScopeTableCount = expectedCount,
                ExistingScopeTableCount = existingCount,
                ExistingScopeTables = probe.ExistingScopeTables
                    .OrderBy(table => table, StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                Evidence = evidence.ToArray(),
                Warnings = warnings.ToArray()
            };
        }
    }
}
