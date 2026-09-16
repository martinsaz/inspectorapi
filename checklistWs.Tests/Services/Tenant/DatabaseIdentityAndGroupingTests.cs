using checklistWs.Services.Tenant;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace checklistWs.Tests.Services.Tenant
{
    public sealed class DatabaseIdentityAndGroupingTests
    {
        private static readonly Guid EmpresaA = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid EmpresaB = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly Guid EmpresaC = Guid.Parse("33333333-3333-3333-3333-333333333333");
        private const string DbXPrincipal = "Server=alias-a;Database=checkapp;TrustServerCertificate=True";
        private const string DbXAlias = "Server=alias-b;Database=checkapp;Application Name=qa;TrustServerCertificate=True";
        private const string DbY = "Server=server-y;Database=checkapp;TrustServerCertificate=True";

        [Fact]
        public void DatabaseIdentity_Equality_UsesPhysicalMetadata()
        {
            DatabaseIdentity first = Identity("SERVER-X", "CHECKAPP", "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            DatabaseIdentity second = Identity("SERVER-X", "CHECKAPP", "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

            Assert.Equal(first, second);
            Assert.Equal(first.GetHashCode(), second.GetHashCode());
            Assert.Equal(first.Fingerprint, second.Fingerprint);
        }

        [Fact]
        public void DatabaseIdentity_Inequality_SeparatesDifferentDatabases()
        {
            DatabaseIdentity first = Identity("SERVER-X", "CHECKAPP", "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            DatabaseIdentity second = Identity("SERVER-X", "CHECKAPP", "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

            Assert.NotEqual(first, second);
            Assert.NotEqual(first.Fingerprint, second.Fingerprint);
        }

        [Fact]
        public async Task ResolveAsync_TenantsDifferent_SamePhysicalDatabase_ProduceSameIdentity()
        {
            IDatabaseIdentityResolver resolver = CreateIdentityResolver(new Dictionary<string, DatabaseMetadata>
            {
                [DbXPrincipal] = Metadata("SERVER-X", "CHECKAPP", "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                [DbXAlias] = Metadata("SERVER-X", "CHECKAPP", "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
            });

            DatabaseIdentity first = await resolver.ResolveAsync(Database("163", EmpresaA, DbXPrincipal));
            DatabaseIdentity second = await resolver.ResolveAsync(Database("164", EmpresaB, DbXAlias));

            Assert.Equal(first, second);
        }

        [Fact]
        public async Task ResolveAsync_SameDatabaseName_DifferentServer_ProducesDifferentIdentity()
        {
            IDatabaseIdentityResolver resolver = CreateIdentityResolver(new Dictionary<string, DatabaseMetadata>
            {
                [DbXPrincipal] = Metadata("SERVER-X", "CHECKAPP", "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                [DbY] = Metadata("SERVER-Y", "CHECKAPP", "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")
            });

            DatabaseIdentity first = await resolver.ResolveAsync(Database("163", EmpresaA, DbXPrincipal));
            DatabaseIdentity second = await resolver.ResolveAsync(Database("165", EmpresaC, DbY));

            Assert.NotEqual(first, second);
        }

        [Fact]
        public async Task ResolveAsync_DifferentConnectionStrings_CanMapToSameRealIdentity()
        {
            IDatabaseIdentityResolver resolver = CreateIdentityResolver(new Dictionary<string, DatabaseMetadata>
            {
                [DbXPrincipal] = Metadata("SERVER-X", "CHECKAPP", "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                [DbXAlias] = Metadata("SERVER-X", "CHECKAPP", "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
            });

            DatabaseIdentity first = await resolver.ResolveAsync(Database("163", EmpresaA, DbXPrincipal));
            DatabaseIdentity second = await resolver.ResolveAsync(Database("164", EmpresaB, DbXAlias));

            Assert.NotEqual(DbXPrincipal, DbXAlias);
            Assert.Equal(first, second);
        }

        [Fact]
        public async Task ResolveAsync_SameDatabaseGuid_DifferentServer_ProducesDifferentIdentity()
        {
            IDatabaseIdentityResolver resolver = CreateIdentityResolver(new Dictionary<string, DatabaseMetadata>
            {
                [DbXPrincipal] = Metadata("SERVER-X", "CHECKAPP", "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                [DbY] = Metadata("SERVER-Y", "CHECKAPP", "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
            });

            DatabaseIdentity first = await resolver.ResolveAsync(Database("163", EmpresaA, DbXPrincipal));
            DatabaseIdentity second = await resolver.ResolveAsync(Database("165", EmpresaC, DbY));

            Assert.NotEqual(first, second);
        }

        [Fact]
        public async Task GroupActiveTenantsAsync_ReportsInvalidTenantConfiguration()
        {
            DatabaseGroupingResult result = await CreateGroupingService(
                new[]
                {
                    Connection("163", EmpresaA, "1", DbXPrincipal),
                    Connection("164", Guid.Empty, "1", DbXAlias)
                },
                new Dictionary<string, DatabaseMetadata>
                {
                    [DbXPrincipal] = Metadata("SERVER-X", "CHECKAPP", "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
                }).GroupActiveTenantsAsync();

            Assert.Single(result.Groups);
            Assert.Single(result.Errors);
            Assert.Equal("164", result.Errors.Single().EmpresaKey);
            Assert.Equal(TenantDatabaseResolutionCode.TenantContextInvalid.ToString(), result.Errors.Single().Code);
        }

        [Fact]
        public async Task ResolveAsync_WhenSqlUnavailable_ReturnsUnavailable()
        {
            IDatabaseIdentityResolver resolver = new DatabaseIdentityResolver(new ThrowingMetadataReader(DatabaseIdentityResolutionCode.DatabaseIdentityUnavailable));

            DatabaseIdentityResolutionException ex = await Assert.ThrowsAsync<DatabaseIdentityResolutionException>(() =>
                resolver.ResolveAsync(Database("163", EmpresaA, DbXPrincipal)));

            Assert.Equal(DatabaseIdentityResolutionCode.DatabaseIdentityUnavailable, ex.Code);
        }

        [Fact]
        public async Task ResolveAsync_WhenMetadataMissing_ReturnsUnverifiable()
        {
            IDatabaseIdentityResolver resolver = CreateIdentityResolver(new Dictionary<string, DatabaseMetadata>
            {
                [DbXPrincipal] = Metadata("SERVER-X", "CHECKAPP", string.Empty)
            });

            DatabaseIdentityResolutionException ex = await Assert.ThrowsAsync<DatabaseIdentityResolutionException>(() =>
                resolver.ResolveAsync(Database("163", EmpresaA, DbXPrincipal)));

            Assert.Equal(DatabaseIdentityResolutionCode.DatabaseIdentityUnverifiable, ex.Code);
        }

        [Fact]
        public async Task ResolveAsync_WhenMetadataAmbiguous_ReturnsAmbiguous()
        {
            IDatabaseIdentityResolver resolver = CreateIdentityResolver(new Dictionary<string, DatabaseMetadata>
            {
                [DbXPrincipal] = Metadata("SERVER-X", "CHECKAPP", "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", ambiguous: true)
            });

            DatabaseIdentityResolutionException ex = await Assert.ThrowsAsync<DatabaseIdentityResolutionException>(() =>
                resolver.ResolveAsync(Database("163", EmpresaA, DbXPrincipal)));

            Assert.Equal(DatabaseIdentityResolutionCode.DatabaseIdentityAmbiguous, ex.Code);
        }

        [Fact]
        public async Task GroupActiveTenantsAsync_GroupsAAndBOnSameDatabase()
        {
            DatabaseGroupingResult result = await CreateGroupingService(
                new[]
                {
                    Connection("163", EmpresaA, "1", DbXPrincipal),
                    Connection("164", EmpresaB, "1", DbXAlias)
                },
                new Dictionary<string, DatabaseMetadata>
                {
                    [DbXPrincipal] = Metadata("SERVER-X", "CHECKAPP", "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    [DbXAlias] = Metadata("SERVER-X", "CHECKAPP", "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
                }).GroupActiveTenantsAsync();

            DatabaseGroup group = Assert.Single(result.Groups);
            Assert.Empty(result.Errors);
            Assert.Equal(new[] { "163", "164" }, group.Tenants.Select(tenant => tenant.EmpresaKey).ToArray());
        }

        [Fact]
        public async Task GroupActiveTenantsAsync_PutsCOnAnotherDatabase()
        {
            DatabaseGroupingResult result = await CreateGroupingService(
                new[]
                {
                    Connection("163", EmpresaA, "1", DbXPrincipal),
                    Connection("164", EmpresaB, "1", DbXAlias),
                    Connection("165", EmpresaC, "1", DbY)
                },
                new Dictionary<string, DatabaseMetadata>
                {
                    [DbXPrincipal] = Metadata("SERVER-X", "CHECKAPP", "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    [DbXAlias] = Metadata("SERVER-X", "CHECKAPP", "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    [DbY] = Metadata("SERVER-Y", "CHECKAPP", "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")
                }).GroupActiveTenantsAsync();

            Assert.Equal(2, result.Groups.Count);
            Assert.Contains(result.Groups, group => group.Tenants.Select(tenant => tenant.EmpresaKey).SequenceEqual(new[] { "163", "164" }));
            Assert.Contains(result.Groups, group => group.Tenants.Select(tenant => tenant.EmpresaKey).SequenceEqual(new[] { "165" }));
        }

        [Fact]
        public async Task GroupActiveTenantsAsync_ErrorOnB_DoesNotBlockAOrC()
        {
            DatabaseGroupingResult result = await CreateGroupingService(
                new[]
                {
                    Connection("163", EmpresaA, "1", DbXPrincipal),
                    Connection("164", EmpresaB, "1", "Server=broken"),
                    Connection("165", EmpresaC, "1", DbY)
                },
                new Dictionary<string, DatabaseMetadata>
                {
                    [DbXPrincipal] = Metadata("SERVER-X", "CHECKAPP", "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    [DbY] = Metadata("SERVER-Y", "CHECKAPP", "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")
                }).GroupActiveTenantsAsync();

            Assert.Equal(2, result.Groups.Count);
            Assert.Single(result.Errors);
            Assert.Equal("164", result.Errors.Single().EmpresaKey);
        }

        [Fact]
        public async Task GroupActiveTenantsAsync_ConcurrentCalls_DoNotShareMutableTenantState()
        {
            IDatabaseGroupingService service = CreateGroupingService(
                new[]
                {
                    Connection("163", EmpresaA, "1", DbXPrincipal),
                    Connection("164", EmpresaB, "1", DbXAlias),
                    Connection("165", EmpresaC, "1", DbY)
                },
                new Dictionary<string, DatabaseMetadata>
                {
                    [DbXPrincipal] = Metadata("SERVER-X", "CHECKAPP", "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    [DbXAlias] = Metadata("SERVER-X", "CHECKAPP", "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    [DbY] = Metadata("SERVER-Y", "CHECKAPP", "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")
                });

            DatabaseGroupingResult[] results = await Task.WhenAll(
                service.GroupActiveTenantsAsync(),
                service.GroupActiveTenantsAsync());

            Assert.All(results, result => Assert.Equal(2, result.Groups.Count));
            Assert.Equal(results[0].Groups.First().Identity, results[1].Groups.First().Identity);
        }

        [Fact]
        public void DatabaseIdentity_SanitizedRepresentation_DoesNotContainSecrets()
        {
            DatabaseIdentity identity = Identity("SERVER-X", "CHECKAPP", "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

            string sanitized = identity.ToSanitizedString();

            Assert.DoesNotContain("Password", sanitized, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("User Id", sanitized, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ConnectionString", sanitized, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Token", sanitized, StringComparison.OrdinalIgnoreCase);
        }

        private static IDatabaseIdentityResolver CreateIdentityResolver(Dictionary<string, DatabaseMetadata> metadata)
        {
            return new DatabaseIdentityResolver(new FakeMetadataReader(metadata));
        }

        private static IDatabaseGroupingService CreateGroupingService(
            IReadOnlyCollection<TenantConnectionDescriptor> tenants,
            Dictionary<string, DatabaseMetadata> metadata)
        {
            FakeTenantCatalogReader catalog = new FakeTenantCatalogReader(tenants);
            return new DatabaseGroupingService(
                catalog,
                new TenantDatabaseResolver(catalog, NullLogger<TenantDatabaseResolver>.Instance),
                new DatabaseIdentityResolver(new FakeMetadataReader(metadata)));
        }

        private static DatabaseIdentity Identity(string serverName, string databaseName, string databaseGuid)
        {
            return new DatabaseIdentity(serverName, serverName, string.Empty, databaseName, databaseGuid);
        }

        private static DatabaseMetadata Metadata(string serverName, string databaseName, string databaseGuid, bool ambiguous = false)
        {
            return new DatabaseMetadata
            {
                ServerName = serverName,
                PhysicalServerName = serverName,
                InstanceName = string.Empty,
                DatabaseName = databaseName,
                DatabaseId = 5,
                DatabaseGuid = databaseGuid,
                Ambiguous = ambiguous
            };
        }

        private static TenantDatabaseDescriptor Database(string empresaKey, Guid idEmpresa, string connectionString)
        {
            return new TenantDatabaseDescriptor
            {
                EmpresaKey = empresaKey,
                IdEmpresa = idEmpresa,
                ConnectionString = connectionString
            };
        }

        private static TenantConnectionDescriptor Connection(string empresaKey, Guid idEmpresa, string status, string connectionString)
        {
            return new TenantConnectionDescriptor
            {
                EmpresaKey = empresaKey,
                IdEmpresa = idEmpresa,
                Status = status,
                ConnectionString = connectionString
            };
        }

        private sealed class FakeMetadataReader : IDatabaseMetadataReader
        {
            private readonly Dictionary<string, DatabaseMetadata> _metadata;

            public FakeMetadataReader(Dictionary<string, DatabaseMetadata> metadata)
            {
                _metadata = metadata;
            }

            public Task<DatabaseMetadata> ReadMetadataAsync(TenantDatabaseDescriptor descriptor, CancellationToken cancellationToken = default)
            {
                if (!_metadata.TryGetValue(descriptor.ConnectionString, out DatabaseMetadata? metadata))
                {
                    throw new DatabaseIdentityResolutionException(DatabaseIdentityResolutionCode.DatabaseIdentityUnavailable);
                }

                return Task.FromResult(metadata);
            }
        }

        private sealed class FakeTenantCatalogReader : ITenantCatalogReader, ITenantConnectionReader
        {
            private readonly IReadOnlyCollection<TenantConnectionDescriptor> _tenants;

            public FakeTenantCatalogReader(IReadOnlyCollection<TenantConnectionDescriptor> tenants)
            {
                _tenants = tenants;
            }

            public Task<IReadOnlyCollection<TenantConnectionDescriptor>> GetConnectionsAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_tenants);
            }

            public Task<TenantConnectionDescriptor?> GetConnectionAsync(string empresaKey, CancellationToken cancellationToken = default)
            {
                TenantConnectionDescriptor? tenant = _tenants.FirstOrDefault(item =>
                    string.Equals(item.EmpresaKey, empresaKey, StringComparison.OrdinalIgnoreCase));
                return Task.FromResult(tenant);
            }
        }

        private sealed class ThrowingMetadataReader : IDatabaseMetadataReader
        {
            private readonly DatabaseIdentityResolutionCode _code;

            public ThrowingMetadataReader(DatabaseIdentityResolutionCode code)
            {
                _code = code;
            }

            public Task<DatabaseMetadata> ReadMetadataAsync(TenantDatabaseDescriptor descriptor, CancellationToken cancellationToken = default)
            {
                throw new DatabaseIdentityResolutionException(_code);
            }
        }
    }
}
