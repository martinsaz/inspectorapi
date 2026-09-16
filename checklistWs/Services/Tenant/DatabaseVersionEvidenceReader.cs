namespace checklistWs.Services.Tenant
{
    public sealed class DatabaseVersionEvidenceReader : IDatabaseVersionEvidenceReader
    {
        private readonly ISchemaVersionRepository _repository;
        private readonly IKnownSchemaVersionProvider _knownSchemaVersionProvider;

        public DatabaseVersionEvidenceReader(
            ISchemaVersionRepository repository,
            IKnownSchemaVersionProvider knownSchemaVersionProvider)
        {
            _repository = repository;
            _knownSchemaVersionProvider = knownSchemaVersionProvider;
        }

        public async Task<DatabaseVersionEvidence> ReadVersionEvidenceAsync(
            TenantDatabaseDescriptor descriptor,
            DatabaseIdentity identity,
            string scope,
            CancellationToken cancellationToken = default)
        {
            SchemaControlState? state = await _repository.GetStateAsync(descriptor, identity, scope, cancellationToken);
            if (state == null || state.CurrentVersion == null)
            {
                return new DatabaseVersionEvidence
                {
                    State = DatabaseVersionEvidenceState.None,
                    ReasonCode = "VERSION_EVIDENCE_MISSING",
                    Evidence = new[] { "No existe versión confirmada en CheckAppSchemaState para DatabaseIdentity + Scope." }
                };
            }

            int? knownCurrentVersion = _knownSchemaVersionProvider.GetKnownCurrentVersion(scope);
            if (knownCurrentVersion == null)
            {
                return new DatabaseVersionEvidence
                {
                    State = DatabaseVersionEvidenceState.Unknown,
                    ReasonCode = "KNOWN_SCOPE_VERSION_MISSING",
                    Warnings = new[] { "La aplicación no conoce una versión vigente para este scope." }
                };
            }

            DatabaseVersionEvidenceState evidenceState = state.CurrentVersion.Value == knownCurrentVersion.Value
                ? DatabaseVersionEvidenceState.Current
                : state.CurrentVersion.Value < knownCurrentVersion.Value
                    ? DatabaseVersionEvidenceState.Outdated
                    : DatabaseVersionEvidenceState.Future;

            return new DatabaseVersionEvidence
            {
                State = evidenceState,
                ReasonCode = $"VERSION_EVIDENCE_{evidenceState.ToString().ToUpperInvariant()}",
                Evidence = new[]
                {
                    $"Versión confirmada: {state.CurrentVersion.Value}.",
                    $"Versión conocida por aplicación: {knownCurrentVersion.Value}."
                },
                Warnings = string.IsNullOrWhiteSpace(state.ManifestHash)
                    ? new[] { "ManifestHash pendiente hasta T15." }
                    : Array.Empty<string>()
            };
        }
    }
}
