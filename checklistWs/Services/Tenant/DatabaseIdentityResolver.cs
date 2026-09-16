namespace checklistWs.Services.Tenant
{
    public sealed class DatabaseIdentityResolver : IDatabaseIdentityResolver
    {
        private readonly IDatabaseMetadataReader _metadataReader;

        public DatabaseIdentityResolver(IDatabaseMetadataReader metadataReader)
        {
            _metadataReader = metadataReader;
        }

        public async Task<DatabaseIdentity> ResolveAsync(TenantDatabaseDescriptor descriptor, CancellationToken cancellationToken = default)
        {
            if (descriptor == null || string.IsNullOrWhiteSpace(descriptor.ConnectionString))
            {
                throw new DatabaseIdentityResolutionException(DatabaseIdentityResolutionCode.DatabaseIdentityUnavailable);
            }

            DatabaseMetadata metadata = await _metadataReader.ReadMetadataAsync(descriptor, cancellationToken);
            if (metadata.Ambiguous)
            {
                throw new DatabaseIdentityResolutionException(DatabaseIdentityResolutionCode.DatabaseIdentityAmbiguous);
            }

            string serverName = Normalize(metadata.ServerName);
            string physicalServerName = Normalize(metadata.PhysicalServerName);
            string instanceName = Normalize(metadata.InstanceName);
            string databaseName = Normalize(metadata.DatabaseName);
            string databaseGuid = NormalizeGuid(metadata.DatabaseGuid);

            if (string.IsNullOrWhiteSpace(serverName) ||
                string.IsNullOrWhiteSpace(databaseName) ||
                string.IsNullOrWhiteSpace(databaseGuid) ||
                metadata.DatabaseId <= 0)
            {
                throw new DatabaseIdentityResolutionException(DatabaseIdentityResolutionCode.DatabaseIdentityUnverifiable);
            }

            return new DatabaseIdentity(
                serverName,
                physicalServerName,
                instanceName,
                databaseName,
                databaseGuid);
        }

        private static string Normalize(string value)
        {
            return (value ?? string.Empty).Trim().ToUpperInvariant();
        }

        private static string NormalizeGuid(string value)
        {
            return Guid.TryParse(value, out Guid parsed) && parsed != Guid.Empty
                ? parsed.ToString("D")
                : string.Empty;
        }
    }
}
