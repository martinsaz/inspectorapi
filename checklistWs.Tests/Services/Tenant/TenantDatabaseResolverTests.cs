using checklistWs.Services.Tenant;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace checklistWs.Tests.Services.Tenant
{
    public sealed class TenantDatabaseResolverTests
    {
        private static readonly Guid EmpresaA = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid EmpresaB = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private const string DbX = "Server=tenant-server;Database=db_x;TrustServerCertificate=True";
        private const string DbY = "Server=tenant-server;Database=db_y;TrustServerCertificate=True";

        [Fact]
        public async Task ResolveAsync_UsesExactEmpresaKey()
        {
            TenantDatabaseResolver resolver = CreateResolver(new Dictionary<string, TenantConnectionDescriptor>
            {
                ["163"] = ActiveConnection("163", EmpresaA, DbX),
                ["164"] = ActiveConnection("164", EmpresaB, DbY)
            });

            TenantDatabaseDescriptor result = await resolver.ResolveAsync(new TenantDatabaseContext
            {
                EmpresaKey = "163",
                IdEmpresa = EmpresaA
            });

            Assert.Equal("163", result.EmpresaKey);
            Assert.Equal(EmpresaA, result.IdEmpresa);
            Assert.Equal(DbX, result.ConnectionString);
        }

        [Fact]
        public async Task ResolveAsync_RejectsMissingTenantWithoutFallback()
        {
            TenantDatabaseResolver resolver = CreateResolver(new Dictionary<string, TenantConnectionDescriptor>());

            TenantDatabaseResolutionException ex = await Assert.ThrowsAsync<TenantDatabaseResolutionException>(() =>
                resolver.ResolveAsync(new TenantDatabaseContext { EmpresaKey = "999", IdEmpresa = EmpresaA }));

            Assert.Equal(TenantDatabaseResolutionCode.TenantNotFound, ex.Code);
        }

        [Fact]
        public async Task ResolveAsync_RejectsIdEmpresaMismatch()
        {
            TenantDatabaseResolver resolver = CreateResolver(new Dictionary<string, TenantConnectionDescriptor>
            {
                ["163"] = ActiveConnection("163", EmpresaB, DbX)
            });

            TenantDatabaseResolutionException ex = await Assert.ThrowsAsync<TenantDatabaseResolutionException>(() =>
                resolver.ResolveAsync(new TenantDatabaseContext { EmpresaKey = "163", IdEmpresa = EmpresaA }));

            Assert.Equal(TenantDatabaseResolutionCode.TenantIdEmpresaMismatch, ex.Code);
        }

        [Fact]
        public async Task ResolveAsync_RejectsInactiveConnection()
        {
            TenantDatabaseResolver resolver = CreateResolver(new Dictionary<string, TenantConnectionDescriptor>
            {
                ["163"] = Connection("163", EmpresaA, "0", DbX)
            });

            TenantDatabaseResolutionException ex = await Assert.ThrowsAsync<TenantDatabaseResolutionException>(() =>
                resolver.ResolveAsync(new TenantDatabaseContext { EmpresaKey = "163", IdEmpresa = EmpresaA }));

            Assert.Equal(TenantDatabaseResolutionCode.TenantConnectionInactive, ex.Code);
        }

        [Theory]
        [InlineData("")]
        [InlineData("not-a-connection-string")]
        [InlineData("Server=tenant-server")]
        public async Task ResolveAsync_RejectsInvalidConnectionString(string connectionString)
        {
            TenantDatabaseResolver resolver = CreateResolver(new Dictionary<string, TenantConnectionDescriptor>
            {
                ["163"] = ActiveConnection("163", EmpresaA, connectionString)
            });

            TenantDatabaseResolutionException ex = await Assert.ThrowsAsync<TenantDatabaseResolutionException>(() =>
                resolver.ResolveAsync(new TenantDatabaseContext { EmpresaKey = "163", IdEmpresa = EmpresaA }));

            Assert.Equal(TenantDatabaseResolutionCode.TenantConnectionInvalid, ex.Code);
        }

        [Fact]
        public async Task ResolveAsync_AllowsTwoTenantsOnSameDatabase()
        {
            TenantDatabaseResolver resolver = CreateResolver(new Dictionary<string, TenantConnectionDescriptor>
            {
                ["163"] = ActiveConnection("163", EmpresaA, DbX),
                ["164"] = ActiveConnection("164", EmpresaB, DbX)
            });

            TenantDatabaseDescriptor first = await resolver.ResolveAsync(new TenantDatabaseContext { EmpresaKey = "163", IdEmpresa = EmpresaA });
            TenantDatabaseDescriptor second = await resolver.ResolveAsync(new TenantDatabaseContext { EmpresaKey = "164", IdEmpresa = EmpresaB });

            Assert.Equal(DbX, first.ConnectionString);
            Assert.Equal(DbX, second.ConnectionString);
            Assert.NotEqual(first.IdEmpresa, second.IdEmpresa);
        }

        [Fact]
        public async Task ResolveAsync_SeparatesConcurrentTenants()
        {
            TenantDatabaseResolver resolver = CreateResolver(new Dictionary<string, TenantConnectionDescriptor>
            {
                ["163"] = ActiveConnection("163", EmpresaA, DbX),
                ["164"] = ActiveConnection("164", EmpresaB, DbY)
            });

            Task<TenantDatabaseDescriptor> first = resolver.ResolveAsync(new TenantDatabaseContext { EmpresaKey = "163", IdEmpresa = EmpresaA });
            Task<TenantDatabaseDescriptor> second = resolver.ResolveAsync(new TenantDatabaseContext { EmpresaKey = "164", IdEmpresa = EmpresaB });
            TenantDatabaseDescriptor[] results = await Task.WhenAll(first, second);

            Assert.Equal(DbX, results[0].ConnectionString);
            Assert.Equal(DbY, results[1].ConnectionString);
            Assert.Equal(EmpresaA, results[0].IdEmpresa);
            Assert.Equal(EmpresaB, results[1].IdEmpresa);
        }

        [Fact]
        public async Task ResolveAsync_FailsClosedWhenReaderFails()
        {
            TenantDatabaseResolver resolver = new TenantDatabaseResolver(
                new ThrowingTenantConnectionReader(),
                NullLogger<TenantDatabaseResolver>.Instance);

            TenantDatabaseResolutionException ex = await Assert.ThrowsAsync<TenantDatabaseResolutionException>(() =>
                resolver.ResolveAsync(new TenantDatabaseContext { EmpresaKey = "163", IdEmpresa = EmpresaA }));

            Assert.Equal(TenantDatabaseResolutionCode.TenantDatabaseResolutionFailed, ex.Code);
        }

        private static TenantDatabaseResolver CreateResolver(Dictionary<string, TenantConnectionDescriptor> connections)
        {
            return new TenantDatabaseResolver(
                new FakeTenantConnectionReader(connections),
                NullLogger<TenantDatabaseResolver>.Instance);
        }

        private static TenantConnectionDescriptor ActiveConnection(string empresaKey, Guid idEmpresa, string connectionString)
        {
            return Connection(empresaKey, idEmpresa, "1", connectionString);
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

        private sealed class FakeTenantConnectionReader : ITenantConnectionReader
        {
            private readonly Dictionary<string, TenantConnectionDescriptor> _connections;

            public FakeTenantConnectionReader(Dictionary<string, TenantConnectionDescriptor> connections)
            {
                _connections = connections;
            }

            public Task<TenantConnectionDescriptor?> GetConnectionAsync(string empresaKey, CancellationToken cancellationToken = default)
            {
                _connections.TryGetValue(empresaKey.Trim().ToUpperInvariant(), out TenantConnectionDescriptor? connection);
                return Task.FromResult(connection);
            }
        }

        private sealed class ThrowingTenantConnectionReader : ITenantConnectionReader
        {
            public Task<TenantConnectionDescriptor?> GetConnectionAsync(string empresaKey, CancellationToken cancellationToken = default)
            {
                throw new InvalidOperationException("Firebase unavailable");
            }
        }
    }
}
