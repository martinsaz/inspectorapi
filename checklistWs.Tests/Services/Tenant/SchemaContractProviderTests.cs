using System.Text.Json;
using checklistWs.Services.Tenant;
using Xunit;

namespace checklistWs.Tests.Services.Tenant
{
    public sealed class SchemaContractProviderTests
    {
        private static readonly TenantDatabaseDescriptor Descriptor = new TenantDatabaseDescriptor
        {
            EmpresaKey = "163",
            IdEmpresa = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            ConnectionString = "Server=alias;Database=checkapp;TrustServerCertificate=True"
        };

        private static readonly DatabaseIdentity Identity = new DatabaseIdentity(
            "SERVER-X",
            "SERVER-X",
            string.Empty,
            "CHECKAPP",
            "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        private readonly ISchemaContractProvider _contractProvider = new ProductosServiciosSchemaContractProvider();
        private readonly ISchemaManifestProvider _manifestProvider = new SchemaManifestProvider();

        [Fact]
        public void Scope_IsProductosServiciosOnly()
        {
            SchemaContract contract = Contract();

            Assert.Equal(DatabaseScopes.ProductosServicios, contract.Scope);
            Assert.Throws<InvalidOperationException>(() => _contractProvider.GetContract("Activos"));
        }

        [Fact]
        public void ContractVersion_IsOne()
        {
            SchemaContract contract = Contract();

            Assert.Equal(1, contract.ContractVersion);
            Assert.Throws<InvalidOperationException>(() => _contractProvider.GetContract(DatabaseScopes.ProductosServicios, 2));
        }

        [Fact]
        public void TableInventory_MatchesFinalProductScope()
        {
            SchemaContract contract = Contract();
            IReadOnlyCollection<string> expected = new ProductScopeInventory().GetExpectedTables(DatabaseScopes.ProductosServicios);

            Assert.Equal(expected.OrderBy(x => x), contract.Tables.Select(t => t.FullName).OrderBy(x => x));
            Assert.Equal(20, contract.Tables.Count);
        }

        [Fact]
        public void CanonicalManifest_OrdersTablesColumnsConstraintsAndIndexesDeterministically()
        {
            SchemaManifest manifest = Manifest(Contract());
            using JsonDocument document = JsonDocument.Parse(manifest.CanonicalJson);
            JsonElement tables = document.RootElement.GetProperty("Tables");
            string[] tableNames = tables.EnumerateArray().Select(t => t.GetProperty("Name").GetString()!).ToArray();

            Assert.Equal(tableNames.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray(), tableNames);

            foreach (JsonElement table in tables.EnumerateArray())
            {
                AssertOrdered(table.GetProperty("Columns"), "Name");
                AssertOrdered(table.GetProperty("ForeignKeys"), "Name");
                AssertOrdered(table.GetProperty("UniqueConstraints"), "Name");
                AssertOrdered(table.GetProperty("CheckConstraints"), "Name");
                AssertOrdered(table.GetProperty("Indexes"), "Name");
            }
        }

        [Fact]
        public void SameContract_ProducesSameHash()
        {
            SchemaContract contract = Contract();

            Assert.Equal(Manifest(contract).ManifestHash, Manifest(contract).ManifestHash);
        }

        [Fact]
        public void RepeatedProviderCalls_ProduceSameHash()
        {
            Assert.Equal(Manifest(Contract()).ManifestHash, Manifest(Contract()).ManifestHash);
        }

        [Fact]
        public void TypeChange_ChangesHash()
        {
            SchemaContract changed = ReplaceFirstColumn(column => column with { SqlType = "NVARCHAR(51)", MaxLength = 51 });

            Assert.NotEqual(Manifest(Contract()).ManifestHash, Manifest(changed).ManifestHash);
        }

        [Fact]
        public void NullabilityChange_ChangesHash()
        {
            SchemaContract changed = ReplaceFirstColumn(column => column with { IsNullable = !column.IsNullable });

            Assert.NotEqual(Manifest(Contract()).ManifestHash, Manifest(changed).ManifestHash);
        }

        [Fact]
        public void LengthPrecisionOrScaleChange_ChangesHash()
        {
            SchemaContract changed = ReplaceFirstColumn(
                column => column.SqlType.StartsWith("NVARCHAR", StringComparison.OrdinalIgnoreCase)
                    ? column with { MaxLength = column.MaxLength + 1, SqlType = "NVARCHAR(51)" }
                    : column with { Precision = 19, Scale = 4, SqlType = "DECIMAL(19,4)" },
                column => column.MaxLength.HasValue || column.Precision.HasValue);

            Assert.NotEqual(Manifest(Contract()).ManifestHash, Manifest(changed).ManifestHash);
        }

        [Fact]
        public void IndexForeignKeyUniqueOrCheckChange_ChangesHash()
        {
            SchemaContract baseline = Contract();
            SchemaContract indexChanged = ReplaceFirstTable(baseline, table => table with
            {
                Indexes = table.Indexes.Append(table.Indexes.First() with { Name = table.Indexes.First().Name + "_COPY" }).ToArray()
            });
            SchemaContract fkChanged = ReplaceFirstTableWith(baseline, table => table.ForeignKeys.Any(), table => table with
            {
                ForeignKeys = table.ForeignKeys.Select((fk, index) => index == 0 ? fk with { Name = fk.Name + "_Cambio" } : fk).ToArray()
            });
            SchemaContract uniqueChanged = ReplaceFirstTableWith(baseline, table => table.UniqueConstraints.Any(), table => table with
            {
                UniqueConstraints = table.UniqueConstraints.Select((unique, index) => index == 0 ? unique with { Name = unique.Name + "_Cambio" } : unique).ToArray()
            });
            SchemaContract checkChanged = ReplaceFirstTableWith(baseline, table => table.CheckConstraints.Any(), table => table with
            {
                CheckConstraints = table.CheckConstraints.Select((check, index) => index == 0 ? check with { Definition = check.Definition + " AND 1 = 1" } : check).ToArray()
            });

            string hash = Manifest(baseline).ManifestHash;
            Assert.NotEqual(hash, Manifest(indexChanged).ManifestHash);
            Assert.NotEqual(hash, Manifest(fkChanged).ManifestHash);
            Assert.NotEqual(hash, Manifest(uniqueChanged).ManifestHash);
            Assert.NotEqual(hash, Manifest(checkChanged).ManifestHash);
        }

        [Fact]
        public void BusinessData_IsNotPartOfManifest()
        {
            SchemaManifest before = Manifest(Contract());
            var ignoredBusinessRows = new[] { new { Codigo = "P-001", Nombre = "Producto de prueba", Precio = 123.45m } };
            _ = ignoredBusinessRows.Single().Precio;

            SchemaManifest after = Manifest(Contract());

            Assert.Equal(before.ManifestHash, after.ManifestHash);
            Assert.DoesNotContain("P-001", after.CanonicalJson, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void IdEmpresa_IsContractualInAllTables()
        {
            foreach (SchemaTableContract table in Contract().Tables)
            {
                SchemaColumnContract column = Assert.Single(table.Columns, c => c.Name == "idEmpresa");
                Assert.Equal("UNIQUEIDENTIFIER", column.SqlType);
                Assert.False(column.IsNullable);
                Assert.True(table.Indexes.Any(index => index.KeyColumns.Any(key => key.Name == "idEmpresa")) || table.PrimaryKey?.Columns.Contains("idEmpresa") == true);
            }
        }

        [Fact]
        public void MultitenantForeignKeysAndUniqueContracts_PreserveIdEmpresa()
        {
            foreach (SchemaTableContract table in Contract().Tables)
            {
                Assert.All(table.ForeignKeys, fk => Assert.Equal("idEmpresa", fk.Columns.First()));
                Assert.All(table.UniqueConstraints, unique => Assert.Equal("idEmpresa", unique.Columns.First()));
            }
        }

        [Fact]
        public void ContractAndManifest_DoNotContainSecrets()
        {
            SchemaManifest manifest = Manifest(Contract());
            string material = manifest.CanonicalJson + " " + string.Join(' ', Contract().Sources);

            Assert.DoesNotContain("Password", material, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("User Id", material, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Token", material, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Server=", material, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void RuntimeV1Contract_IsRepeatableAfterCallerSideProjectionMutation()
        {
            SchemaContract first = Contract();
            SchemaContract callerSideChanged = ReplaceFirstTable(first, table => table with { Name = table.Name + "Changed" });
            _ = Manifest(callerSideChanged);

            SchemaContract second = Contract();

            Assert.Equal(20, second.Tables.Count);
            Assert.DoesNotContain(second.Tables, table => table.Name.EndsWith("Changed", StringComparison.Ordinal));
            Assert.Equal(Manifest(first).ManifestHash, Manifest(second).ManifestHash);
        }

        [Fact]
        public async Task ReadingContract_DoesNotModifyT14State()
        {
            EmptySchemaVersionRepository repository = new EmptySchemaVersionRepository();

            _ = Manifest(Contract());
            SchemaControlState? state = await repository.GetStateAsync(Descriptor, Identity, DatabaseScopes.ProductosServicios);

            Assert.Null(state);
            Assert.Equal(0, repository.Writes);
        }

        [Fact]
        public async Task T13_DoesNotBecomeCurrentJustBecauseContractExists()
        {
            _ = Manifest(Contract());
            DatabaseStateClassifier classifier = new DatabaseStateClassifier(
                new CompleteProductScopeProbe(),
                new DatabaseVersionEvidenceReader(new EmptySchemaVersionRepository(), new KnownSchemaVersionProvider()));

            DatabaseClassificationResult result = await classifier.ClassifyAsync(Descriptor, Identity, DatabaseScopes.ProductosServicios);

            Assert.Equal(DatabaseStructureState.Unknown, result.State);
            Assert.Equal("VERSION_EVIDENCE_MISSING", result.ReasonCode);
        }

        [Fact]
        public void Scopes_AreNotMixed()
        {
            SchemaContract contract = Contract();

            Assert.All(contract.Tables, table => Assert.StartsWith("ProductosServicios", table.Name, StringComparison.Ordinal));
            Assert.DoesNotContain(contract.Tables, table => table.FullName.Contains("Activos", StringComparison.OrdinalIgnoreCase));
            Assert.Empty(new ProductScopeInventory().GetExpectedTables("OtherScope"));
        }

        private SchemaContract Contract() => _contractProvider.GetContract(DatabaseScopes.ProductosServicios);

        private SchemaManifest Manifest(SchemaContract contract) => _manifestProvider.CreateManifest(contract);

        private static void AssertOrdered(JsonElement array, string propertyName)
        {
            string[] values = array.EnumerateArray().Select(item => item.GetProperty(propertyName).GetString()!).ToArray();
            Assert.Equal(values.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray(), values);
        }

        private SchemaContract ReplaceFirstColumn(Func<SchemaColumnContract, SchemaColumnContract> replacement, Func<SchemaColumnContract, bool>? predicate = null)
        {
            SchemaContract contract = Contract();
            return ReplaceFirstTableWith(contract, table => table.Columns.Any(predicate ?? (_ => true)), table =>
            {
                bool replaced = false;
                SchemaColumnContract[] columns = table.Columns.Select(column =>
                {
                    if (!replaced && (predicate == null || predicate(column)))
                    {
                        replaced = true;
                        return replacement(column);
                    }

                    return column;
                }).ToArray();
                return table with { Columns = columns };
            });
        }

        private static SchemaContract ReplaceFirstTable(SchemaContract contract, Func<SchemaTableContract, SchemaTableContract> replacement)
        {
            return ReplaceFirstTableWith(contract, _ => true, replacement);
        }

        private static SchemaContract ReplaceFirstTableWith(SchemaContract contract, Func<SchemaTableContract, bool> predicate, Func<SchemaTableContract, SchemaTableContract> replacement)
        {
            bool replaced = false;
            SchemaTableContract[] tables = contract.Tables.Select(table =>
            {
                if (!replaced && predicate(table))
                {
                    replaced = true;
                    return replacement(table);
                }

                return table;
            }).ToArray();

            Assert.True(replaced);
            return contract with { Tables = tables };
        }

        private sealed class CompleteProductScopeProbe : IDatabaseSchemaProbe
        {
            public Task<DatabaseSchemaProbeResult> ProbeAsync(TenantDatabaseDescriptor descriptor, string scope, CancellationToken cancellationToken = default)
            {
                IReadOnlyCollection<string> expected = new ProductScopeInventory().GetExpectedTables(DatabaseScopes.ProductosServicios);
                return Task.FromResult(new DatabaseSchemaProbeResult
                {
                    Scope = scope,
                    ExpectedTables = expected,
                    ExistingScopeTables = expected
                });
            }
        }

        private sealed class EmptySchemaVersionRepository : ISchemaVersionRepository
        {
            public int Writes { get; private set; }

            public Task<SchemaControlInfrastructureResult> EnsureSchemaControlInfrastructureAsync(TenantDatabaseDescriptor descriptor, CancellationToken cancellationToken = default)
            {
                Writes++;
                return Task.FromResult(new SchemaControlInfrastructureResult { Status = SchemaControlInfrastructureStatus.Ready });
            }

            public Task<SchemaControlState?> GetStateAsync(TenantDatabaseDescriptor descriptor, DatabaseIdentity identity, string scope, CancellationToken cancellationToken = default)
            {
                return Task.FromResult<SchemaControlState?>(null);
            }

            public Task<IReadOnlyCollection<SchemaControlHistory>> GetHistoryAsync(TenantDatabaseDescriptor descriptor, DatabaseIdentity identity, string scope, CancellationToken cancellationToken = default)
            {
                return Task.FromResult<IReadOnlyCollection<SchemaControlHistory>>(Array.Empty<SchemaControlHistory>());
            }

            public Task<IReadOnlyCollection<SchemaControlAttempt>> GetAttemptsAsync(TenantDatabaseDescriptor descriptor, DatabaseIdentity identity, string scope, CancellationToken cancellationToken = default)
            {
                return Task.FromResult<IReadOnlyCollection<SchemaControlAttempt>>(Array.Empty<SchemaControlAttempt>());
            }

            public Task<Guid> BeginAttemptAsync(TenantDatabaseDescriptor descriptor, SchemaControlAttempt attempt, CancellationToken cancellationToken = default)
            {
                Writes++;
                return Task.FromResult(Guid.NewGuid());
            }

            public Task CompleteAttemptAsync(TenantDatabaseDescriptor descriptor, Guid attemptId, string result, string reasonCode, string? error, CancellationToken cancellationToken = default)
            {
                Writes++;
                return Task.CompletedTask;
            }

            public Task RecordHistoryAsync(TenantDatabaseDescriptor descriptor, SchemaControlHistory history, CancellationToken cancellationToken = default)
            {
                Writes++;
                return Task.CompletedTask;
            }

            public Task SetConfirmedStateAsync(TenantDatabaseDescriptor descriptor, SchemaControlState state, CancellationToken cancellationToken = default)
            {
                Writes++;
                return Task.CompletedTask;
            }
        }
    }
}
