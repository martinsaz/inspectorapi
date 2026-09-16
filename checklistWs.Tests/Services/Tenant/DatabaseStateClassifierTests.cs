using checklistWs.Services.Tenant;
using Xunit;

namespace checklistWs.Tests.Services.Tenant
{
    public sealed class DatabaseStateClassifierTests
    {
        private static readonly DatabaseIdentity Identity = new DatabaseIdentity(
            "SERVER-X",
            "SERVER-X",
            string.Empty,
            "CHECKAPP",
            "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        private static readonly TenantDatabaseDescriptor Descriptor = new TenantDatabaseDescriptor
        {
            EmpresaKey = "163",
            IdEmpresa = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            ConnectionString = "Server=alias;Database=checkapp;TrustServerCertificate=True"
        };

        [Fact]
        public async Task ClassifyAsync_GeneralTablesButNoScopeTables_ReturnsEmpty()
        {
            DatabaseClassificationResult result = await CreateClassifier(Array.Empty<string>()).ClassifyAsync(
                Descriptor,
                Identity,
                DatabaseScopes.ProductosServicios);

            Assert.Equal(DatabaseStructureState.Empty, result.State);
            Assert.Equal("NO_SCOPE_TABLES_FOUND", result.ReasonCode);
            Assert.Equal(0, result.ExistingScopeTableCount);
        }

        [Fact]
        public async Task ClassifyAsync_NoScopeTables_ReturnsEmpty()
        {
            DatabaseClassificationResult result = await CreateClassifier(Array.Empty<string>()).ClassifyAsync(
                Descriptor,
                Identity,
                DatabaseScopes.ProductosServicios);

            Assert.Equal(DatabaseStructureState.Empty, result.State);
            Assert.True(result.IsAvailable);
        }

        [Fact]
        public async Task ClassifyAsync_OneOfTwentyScopeTables_ReturnsPartial()
        {
            DatabaseClassificationResult result = await CreateClassifier(ExpectedTables().Take(1)).ClassifyAsync(
                Descriptor,
                Identity,
                DatabaseScopes.ProductosServicios);

            Assert.Equal(DatabaseStructureState.Partial, result.State);
            Assert.Equal(1, result.ExistingScopeTableCount);
        }

        [Fact]
        public async Task ClassifyAsync_EightOfTwentyScopeTables_ReturnsPartial()
        {
            DatabaseClassificationResult result = await CreateClassifier(ExpectedTables().Take(8)).ClassifyAsync(
                Descriptor,
                Identity,
                DatabaseScopes.ProductosServicios);

            Assert.Equal(DatabaseStructureState.Partial, result.State);
            Assert.Equal(8, result.ExistingScopeTableCount);
        }

        [Fact]
        public async Task ClassifyAsync_NineteenOfTwentyScopeTables_ReturnsPartial()
        {
            DatabaseClassificationResult result = await CreateClassifier(ExpectedTables().Take(19)).ClassifyAsync(
                Descriptor,
                Identity,
                DatabaseScopes.ProductosServicios);

            Assert.Equal(DatabaseStructureState.Partial, result.State);
            Assert.Equal(19, result.ExistingScopeTableCount);
        }

        [Fact]
        public async Task ClassifyAsync_AllTablesWithoutVersionEvidence_DoesNotInventCurrent()
        {
            DatabaseClassificationResult result = await CreateClassifier(ExpectedTables()).ClassifyAsync(
                Descriptor,
                Identity,
                DatabaseScopes.ProductosServicios);

            Assert.Equal(DatabaseStructureState.Unknown, result.State);
            Assert.Equal("VERSION_EVIDENCE_MISSING", result.ReasonCode);
            Assert.Equal(20, result.ExistingScopeTableCount);
        }

        [Fact]
        public async Task ClassifyAsync_WhenSqlUnavailable_ReturnsUnavailableNotEmpty()
        {
            DatabaseClassificationResult result = await CreateClassifier(
                ExpectedTables(),
                unavailable: true).ClassifyAsync(Descriptor, Identity, DatabaseScopes.ProductosServicios);

            Assert.Equal(DatabaseStructureState.Unknown, result.State);
            Assert.False(result.IsAvailable);
            Assert.Equal("UNAVAILABLE", result.ReasonCode);
        }

        [Fact]
        public async Task ClassifyAsync_WhenMetadataInsufficient_ReturnsUnknown()
        {
            DatabaseClassificationResult result = await CreateClassifier(
                ExpectedTables(),
                metadataSufficient: false).ClassifyAsync(Descriptor, Identity, DatabaseScopes.ProductosServicios);

            Assert.Equal(DatabaseStructureState.Unknown, result.State);
            Assert.Equal("METADATA_INSUFFICIENT", result.ReasonCode);
        }

        [Fact]
        public async Task ClassifyAsync_WhenDatabaseIdentityInvalid_ReturnsControlledUnknown()
        {
            DatabaseIdentity invalidIdentity = new DatabaseIdentity(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);

            DatabaseClassificationResult result = await CreateClassifier(ExpectedTables()).ClassifyAsync(
                Descriptor,
                invalidIdentity,
                DatabaseScopes.ProductosServicios);

            Assert.Equal(DatabaseStructureState.Unknown, result.State);
            Assert.Equal("DATABASE_IDENTITY_INVALID", result.ReasonCode);
        }

        [Fact]
        public async Task ClassifyAsync_WithOutdatedVersionEvidence_ReturnsOutdated()
        {
            DatabaseClassificationResult result = await CreateClassifier(
                ExpectedTables(),
                versionEvidenceState: DatabaseVersionEvidenceState.Outdated).ClassifyAsync(Descriptor, Identity, DatabaseScopes.ProductosServicios);

            Assert.Equal(DatabaseStructureState.Outdated, result.State);
        }

        [Fact]
        public async Task ClassifyAsync_WithCurrentVersionEvidence_ReturnsCurrent()
        {
            DatabaseClassificationResult result = await CreateClassifier(
                ExpectedTables(),
                versionEvidenceState: DatabaseVersionEvidenceState.Current).ClassifyAsync(Descriptor, Identity, DatabaseScopes.ProductosServicios);

            Assert.Equal(DatabaseStructureState.Current, result.State);
        }

        [Fact]
        public async Task ClassifyAsync_WithFutureVersionEvidence_ReturnsFuture()
        {
            DatabaseClassificationResult result = await CreateClassifier(
                ExpectedTables(),
                versionEvidenceState: DatabaseVersionEvidenceState.Future).ClassifyAsync(Descriptor, Identity, DatabaseScopes.ProductosServicios);

            Assert.Equal(DatabaseStructureState.Future, result.State);
        }

        [Fact]
        public async Task ClassifyAsync_ScopeA_DoesNotContaminateScopeB()
        {
            FakeSchemaProbe probe = new FakeSchemaProbe(new Dictionary<string, IReadOnlyCollection<string>>
            {
                [DatabaseScopes.ProductosServicios] = ExpectedTables().Take(1).ToArray(),
                ["OtherScope"] = Array.Empty<string>()
            });
            DatabaseStateClassifier classifier = new DatabaseStateClassifier(probe, new FakeVersionEvidenceReader(DatabaseVersionEvidenceState.None));

            DatabaseClassificationResult scopeA = await classifier.ClassifyAsync(Descriptor, Identity, DatabaseScopes.ProductosServicios);
            DatabaseClassificationResult scopeB = await classifier.ClassifyAsync(Descriptor, Identity, "OtherScope");

            Assert.Equal(DatabaseStructureState.Partial, scopeA.State);
            Assert.Equal(DatabaseStructureState.Empty, scopeB.State);
            Assert.Equal("OtherScope", scopeB.Scope);
        }

        [Fact]
        public async Task ClassifyAsync_ConcurrentCalls_DoNotShareMutableState()
        {
            DatabaseStateClassifier classifier = CreateClassifier(ExpectedTables().Take(8));

            DatabaseClassificationResult[] results = await Task.WhenAll(
                classifier.ClassifyAsync(Descriptor, Identity, DatabaseScopes.ProductosServicios),
                classifier.ClassifyAsync(Descriptor, Identity, DatabaseScopes.ProductosServicios));

            Assert.All(results, result => Assert.Equal(DatabaseStructureState.Partial, result.State));
            Assert.All(results, result => Assert.Equal(8, result.ExistingScopeTableCount));
        }

        [Fact]
        public async Task ClassifyAsync_EvidenceAndWarnings_DoNotContainDdlOrDml()
        {
            DatabaseClassificationResult result = await CreateClassifier(ExpectedTables().Take(1)).ClassifyAsync(
                Descriptor,
                Identity,
                DatabaseScopes.ProductosServicios);

            string material = string.Join(' ', result.Evidence.Concat(result.Warnings));

            Assert.DoesNotContain("CREATE", material, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ALTER", material, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("DROP", material, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("TRUNCATE", material, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("INSERT", material, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("UPDATE", material, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("DELETE", material, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("MERGE", material, StringComparison.OrdinalIgnoreCase);
        }

        private static DatabaseStateClassifier CreateClassifier(
            IEnumerable<string> existingTables,
            bool unavailable = false,
            bool metadataSufficient = true,
            DatabaseVersionEvidenceState versionEvidenceState = DatabaseVersionEvidenceState.None)
        {
            return new DatabaseStateClassifier(
                new FakeSchemaProbe(existingTables.ToArray(), unavailable, metadataSufficient),
                new FakeVersionEvidenceReader(versionEvidenceState));
        }

        private static IReadOnlyCollection<string> ExpectedTables()
        {
            return new ProductScopeInventory().GetExpectedTables(DatabaseScopes.ProductosServicios);
        }

        private sealed class FakeSchemaProbe : IDatabaseSchemaProbe
        {
            private readonly Dictionary<string, IReadOnlyCollection<string>> _scopeTables;
            private readonly bool _unavailable;
            private readonly bool _metadataSufficient;

            public FakeSchemaProbe(
                IReadOnlyCollection<string> existingTables,
                bool unavailable = false,
                bool metadataSufficient = true)
                : this(new Dictionary<string, IReadOnlyCollection<string>>
                {
                    [DatabaseScopes.ProductosServicios] = existingTables
                }, unavailable, metadataSufficient)
            {
            }

            public FakeSchemaProbe(
                Dictionary<string, IReadOnlyCollection<string>> scopeTables,
                bool unavailable = false,
                bool metadataSufficient = true)
            {
                _scopeTables = scopeTables;
                _unavailable = unavailable;
                _metadataSufficient = metadataSufficient;
            }

            public Task<DatabaseSchemaProbeResult> ProbeAsync(
                TenantDatabaseDescriptor descriptor,
                string scope,
                CancellationToken cancellationToken = default)
            {
                if (_unavailable)
                {
                    throw new DatabaseIdentityResolutionException(DatabaseIdentityResolutionCode.DatabaseIdentityUnavailable);
                }

                IReadOnlyCollection<string> expected = string.Equals(scope, DatabaseScopes.ProductosServicios, StringComparison.OrdinalIgnoreCase)
                    ? ExpectedTables()
                    : new[] { $"{scope}.Sentinel" };

                _scopeTables.TryGetValue(scope, out IReadOnlyCollection<string>? existing);
                existing ??= Array.Empty<string>();

                return Task.FromResult(new DatabaseSchemaProbeResult
                {
                    Scope = scope,
                    ExpectedTables = expected,
                    ExistingScopeTables = existing,
                    MetadataSufficient = _metadataSufficient,
                    Evidence = new[] { $"Tablas scope encontradas: {existing.Count}/{expected.Count}." },
                    Warnings = _metadataSufficient ? Array.Empty<string>() : new[] { "Metadata insuficiente." }
                });
            }
        }

        private sealed class FakeVersionEvidenceReader : IDatabaseVersionEvidenceReader
        {
            private readonly DatabaseVersionEvidenceState _state;

            public FakeVersionEvidenceReader(DatabaseVersionEvidenceState state)
            {
                _state = state;
            }

            public Task<DatabaseVersionEvidence> ReadVersionEvidenceAsync(
                TenantDatabaseDescriptor descriptor,
                DatabaseIdentity identity,
                string scope,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new DatabaseVersionEvidence
                {
                    State = _state,
                    ReasonCode = _state == DatabaseVersionEvidenceState.None
                        ? "VERSION_EVIDENCE_NOT_AVAILABLE"
                        : $"VERSION_EVIDENCE_{_state.ToString().ToUpperInvariant()}",
                    Evidence = _state == DatabaseVersionEvidenceState.None
                        ? Array.Empty<string>()
                        : new[] { $"Evidencia simulada {_state} para {scope}." }
                });
            }
        }
    }
}
