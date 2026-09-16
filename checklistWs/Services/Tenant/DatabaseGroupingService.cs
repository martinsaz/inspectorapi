namespace checklistWs.Services.Tenant
{
    public sealed class DatabaseGroupingService : IDatabaseGroupingService
    {
        private readonly ITenantCatalogReader _tenantCatalogReader;
        private readonly ITenantDatabaseResolver _tenantDatabaseResolver;
        private readonly IDatabaseIdentityResolver _databaseIdentityResolver;

        public DatabaseGroupingService(
            ITenantCatalogReader tenantCatalogReader,
            ITenantDatabaseResolver tenantDatabaseResolver,
            IDatabaseIdentityResolver databaseIdentityResolver)
        {
            _tenantCatalogReader = tenantCatalogReader;
            _tenantDatabaseResolver = tenantDatabaseResolver;
            _databaseIdentityResolver = databaseIdentityResolver;
        }

        public async Task<DatabaseGroupingResult> GroupActiveTenantsAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<TenantConnectionDescriptor> tenants = await _tenantCatalogReader.GetConnectionsAsync(cancellationToken);
            Dictionary<DatabaseIdentity, List<TenantDescriptor>> groups = new();
            List<DatabaseGroupingError> errors = new();

            foreach (TenantConnectionDescriptor tenant in tenants)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!string.Equals(tenant.Status?.Trim(), "1", StringComparison.Ordinal))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(tenant.EmpresaKey) || tenant.IdEmpresa == Guid.Empty)
                {
                    errors.Add(ToError(tenant, TenantDatabaseResolutionCode.TenantContextInvalid.ToString()));
                    continue;
                }

                try
                {
                    TenantDatabaseDescriptor tenantDatabase = await _tenantDatabaseResolver.ResolveAsync(
                        new TenantDatabaseContext
                        {
                            EmpresaKey = tenant.EmpresaKey,
                            IdEmpresa = tenant.IdEmpresa
                        },
                        cancellationToken);

                    DatabaseIdentity identity = await _databaseIdentityResolver.ResolveAsync(tenantDatabase, cancellationToken);
                    if (!groups.TryGetValue(identity, out List<TenantDescriptor>? groupTenants))
                    {
                        groupTenants = new List<TenantDescriptor>();
                        groups[identity] = groupTenants;
                    }

                    groupTenants.Add(new TenantDescriptor
                    {
                        EmpresaKey = tenant.EmpresaKey.Trim().ToUpperInvariant(),
                        IdEmpresa = tenant.IdEmpresa,
                        Status = tenant.Status.Trim()
                    });
                }
                catch (TenantDatabaseResolutionException ex)
                {
                    errors.Add(ToError(tenant, ex.Code.ToString()));
                }
                catch (DatabaseIdentityResolutionException ex)
                {
                    errors.Add(ToError(tenant, ex.Code.ToString()));
                }
            }

            return new DatabaseGroupingResult
            {
                Groups = groups
                    .Select(item => new DatabaseGroup
                    {
                        Identity = item.Key,
                        Tenants = item.Value
                            .OrderBy(tenant => tenant.EmpresaKey, StringComparer.OrdinalIgnoreCase)
                            .ToArray()
                    })
                    .OrderBy(group => group.Identity.Fingerprint, StringComparer.Ordinal)
                    .ToArray(),
                Errors = errors.ToArray()
            };
        }

        private static DatabaseGroupingError ToError(TenantConnectionDescriptor tenant, string code)
        {
            return new DatabaseGroupingError
            {
                EmpresaKey = (tenant.EmpresaKey ?? string.Empty).Trim().ToUpperInvariant(),
                IdEmpresa = tenant.IdEmpresa == Guid.Empty ? null : tenant.IdEmpresa,
                Code = code
            };
        }
    }
}
