namespace checklistWs.Services.Tenant
{
    public sealed class NullDatabaseVersionEvidenceReader : IDatabaseVersionEvidenceReader
    {
        public Task<DatabaseVersionEvidence> ReadVersionEvidenceAsync(
            TenantDatabaseDescriptor descriptor,
            DatabaseIdentity identity,
            string scope,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new DatabaseVersionEvidence
            {
                State = DatabaseVersionEvidenceState.None,
                ReasonCode = "VERSION_EVIDENCE_NOT_AVAILABLE",
                Warnings = new[] { "No existe evidencia persistente T14/T15 para declarar CURRENT, OUTDATED o FUTURE." }
            });
        }
    }
}
