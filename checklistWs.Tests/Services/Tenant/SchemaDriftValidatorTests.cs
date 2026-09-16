using System.Data.SqlClient;
using checklistWs.Services.Tenant;
using Xunit;

namespace checklistWs.Tests.Services.Tenant
{
    public sealed class SchemaDriftValidatorTests
    {
        private static readonly DatabaseIdentity Identity = new("SERVER", "SERVER", string.Empty, "CHECKAPP", "dddddddd-dddd-dddd-dddd-dddddddddddd");

        [Fact] public async Task ExactV1_ReturnsSchemaOk() => await Expect(null, SchemaValidationGlobalResult.SchemaOk, null);
        [Fact] public async Task MissingTable_IsCritical() => await Expect(s => s.Tables = Array.Empty<SchemaTableSnapshot>(), SchemaValidationGlobalResult.SchemaDriftCritico, "TABLE_MISSING");
        [Fact] public async Task ExtraTable_IsWarning() => await Expect(s => s.Tables = s.Tables.Append(new("dbo", "ProductosServiciosExtra")).ToArray(), SchemaValidationGlobalResult.SchemaDrift, "TABLE_UNEXPECTED");
        [Fact] public async Task HomonymView_IsObjectTypeMismatch() => await Expect(s => { s.Objects = new[] { new SchemaObjectSnapshot("dbo", "ProductosServicios", "VIEW") }; s.Tables = Array.Empty<SchemaTableSnapshot>(); }, SchemaValidationGlobalResult.SchemaDriftCritico, "OBJECT_TYPE_MISMATCH");
        [Fact] public async Task MissingColumn_IsDetected() => await Expect(s => s.Columns = s.Columns.Where(c => c.Name != "Nombre").ToArray(), SchemaValidationGlobalResult.SchemaDrift, "COLUMN_MISSING");
        [Fact] public async Task ExtraColumn_IsDetected() => await Expect(s => s.Columns = s.Columns.Append(new("dbo", "ProductosServicios", "Extra", "INT", null, null, null, true, null, false, null, null, false, null, null, null)).ToArray(), SchemaValidationGlobalResult.SchemaDrift, "COLUMN_UNEXPECTED");
        [Fact] public async Task ColumnType_IsDetected() => await Expect(s => ReplaceColumn(s, "Nombre", c => c with { SqlType = "INT" }), SchemaValidationGlobalResult.SchemaDrift, "COLUMN_TYPE_MISMATCH");
        [Fact] public async Task ColumnLength_IsDetected() => await Expect(s => ReplaceColumn(s, "Nombre", c => c with { MaxLength = 200, SqlType = "NVARCHAR(200)" }), SchemaValidationGlobalResult.SchemaDrift, "COLUMN_LENGTH_MISMATCH");
        [Fact] public async Task ColumnPrecision_IsDetected() => await Expect(s => ReplaceColumn(s, "Precio", c => c with { Precision = 10, SqlType = "DECIMAL(10,2)" }), SchemaValidationGlobalResult.SchemaDrift, "COLUMN_PRECISION_MISMATCH");
        [Fact] public async Task ColumnScale_IsDetected() => await Expect(s => ReplaceColumn(s, "Precio", c => c with { Scale = 4, SqlType = "DECIMAL(18,4)" }), SchemaValidationGlobalResult.SchemaDrift, "COLUMN_SCALE_MISMATCH");
        [Fact] public async Task Nullability_IsDetected() => await Expect(s => ReplaceColumn(s, "Nombre", c => c with { IsNullable = true }), SchemaValidationGlobalResult.SchemaDrift, "COLUMN_NULLABILITY_MISMATCH");
        [Fact] public async Task CollationMismatch_IsRepresentableWithoutRepair() => await Expect(s => ReplaceColumn(s, "Nombre", c => c with { CollationName = "Modern_Spanish_CI_AS" }), SchemaValidationGlobalResult.SchemaOk, null);
        [Fact] public async Task IdentityMismatch_IsCritical() => await Expect(s => ReplaceColumn(s, "id", c => c with { IsIdentity = true, IdentitySeed = 1, IdentityIncrement = 1 }), SchemaValidationGlobalResult.SchemaDriftCritico, "COLUMN_IDENTITY_MISMATCH");
        [Fact] public async Task ComputedMismatch_IsDetected() => await Expect(s => ReplaceColumn(s, "NombreNormalizado", c => c with { ComputedDefinition = "LOWER(Nombre)" }), SchemaValidationGlobalResult.SchemaDrift, "COLUMN_COMPUTED_DEFINITION_MISMATCH");
        [Fact] public async Task EquivalentDefault_IsNotDrift() => await Expect(s => ReplaceColumn(s, "Activo", c => c with { DefaultDefinition = "((1))" }), SchemaValidationGlobalResult.SchemaOk, null);
        [Fact] public async Task DifferentDefault_IsDrift() => await Expect(s => ReplaceColumn(s, "Activo", c => c with { DefaultDefinition = "(0)" }), SchemaValidationGlobalResult.SchemaDrift, "COLUMN_DEFAULT_MISMATCH");
        [Fact] public async Task PkMismatch_IsCritical() => await Expect(s => s.PrimaryKeys = new[] { s.PrimaryKeys.Single() with { Columns = new[] { new SchemaIndexColumnContract("Nombre", false) } } }, SchemaValidationGlobalResult.SchemaDriftCritico, "PK_DEFINITION_MISMATCH");
        [Fact] public async Task UniqueMissingIdEmpresa_IsCritical() => await Expect(s => s.Indexes = new[] { s.Indexes.Single(i => i.Name == "UX_PS_Nombre") with { KeyColumns = new[] { new SchemaIndexColumnContract("Nombre", false) } } }, SchemaValidationGlobalResult.SchemaDriftCritico, "INDEX_KEYS_MISMATCH");
        [Fact] public async Task MissingIndex_IsDetected() => await Expect(s => s.Indexes = s.Indexes.Where(i => i.Name != "IX_PS_Activo").ToArray(), SchemaValidationGlobalResult.SchemaDrift, "INDEX_MISSING");
        [Fact] public async Task IndexKeys_IsDetected() => await Expect(s => ReplaceIndex(s, "IX_PS_Activo", i => i with { KeyColumns = new[] { new SchemaIndexColumnContract("Nombre", false) } }), SchemaValidationGlobalResult.SchemaDrift, "INDEX_KEYS_MISMATCH");
        [Fact] public async Task IndexDescending_IsDetected() => await Expect(s => ReplaceIndex(s, "IX_PS_Activo", i => i with { KeyColumns = new[] { new SchemaIndexColumnContract("idEmpresa", false), new SchemaIndexColumnContract("Activo", true) } }), SchemaValidationGlobalResult.SchemaDrift, "INDEX_KEYS_MISMATCH");
        [Fact] public async Task IndexInclude_IsDetected() => await Expect(s => ReplaceIndex(s, "IX_PS_Activo", i => i with { IncludedColumns = Array.Empty<string>() }), SchemaValidationGlobalResult.SchemaDrift, "INDEX_INCLUDE_MISMATCH");
        [Fact] public async Task IndexFilter_IsDetected() => await Expect(s => ReplaceIndex(s, "IX_PS_Activo", i => i with { FilterDefinition = "Activo = 0" }), SchemaValidationGlobalResult.SchemaDrift, "INDEX_FILTER_MISMATCH");
        [Fact] public async Task DisabledIndex_IsDetected() => await Expect(s => ReplaceIndex(s, "IX_PS_Activo", i => i with { IsDisabled = true }), SchemaValidationGlobalResult.SchemaDrift, "INDEX_STATE_MISMATCH");
        [Fact] public async Task MissingFk_IsDetected() => await Expect(s => s.ForeignKeys = Array.Empty<SchemaForeignKeySnapshot>(), SchemaValidationGlobalResult.SchemaDrift, "FK_MISSING");
        [Fact] public async Task FkColumnOrder_IsDetected() => await Expect(s => s.ForeignKeys = new[] { s.ForeignKeys.Single() with { Columns = new[] { "idCategoria", "idEmpresa" } } }, SchemaValidationGlobalResult.SchemaDrift, "FK_COLUMNS_MISMATCH");
        [Fact] public async Task FkTarget_IsDetected() => await Expect(s => s.ForeignKeys = new[] { s.ForeignKeys.Single() with { ReferencedTable = "Otra" } }, SchemaValidationGlobalResult.SchemaDrift, "FK_TARGET_MISMATCH");
        [Fact] public async Task FkActions_AreDetected() => await Expect(s => s.ForeignKeys = new[] { s.ForeignKeys.Single() with { DeleteAction = "CASCADE" } }, SchemaValidationGlobalResult.SchemaDrift, "FK_ACTIONS_MISMATCH");
        [Fact] public async Task FkWithoutIdEmpresa_IsCritical() => await Expect(s => s.ForeignKeys = new[] { s.ForeignKeys.Single() with { Columns = new[] { "idCategoria" } } }, SchemaValidationGlobalResult.SchemaDriftCritico, "FK_MULTITENANT_INSEGURA");
        [Fact] public async Task FkUntrustedDisabled_IsDetected() => await Expect(s => s.ForeignKeys = new[] { s.ForeignKeys.Single() with { IsNotTrusted = true } }, SchemaValidationGlobalResult.SchemaDrift, "FK_STATE_MISMATCH");
        [Fact] public async Task MissingCheck_IsDetected() => await Expect(s => s.Checks = Array.Empty<SchemaCheckSnapshot>(), SchemaValidationGlobalResult.SchemaDrift, "CHECK_MISSING");
        [Fact] public async Task CheckDefinition_IsDetected() => await Expect(s => s.Checks = new[] { s.Checks.Single() with { Definition = "Activo IN (0)" } }, SchemaValidationGlobalResult.SchemaDrift, "CHECK_DEFINITION_MISMATCH");
        [Fact] public async Task CheckDisabledUntrusted_IsDetected() => await Expect(s => s.Checks = new[] { s.Checks.Single() with { IsDisabled = true } }, SchemaValidationGlobalResult.SchemaDrift, "CHECK_STATE_MISMATCH");
        [Fact] public async Task ManifestHashMismatch_IsCritical() => await Expect(null, SchemaValidationGlobalResult.SchemaDriftCritico, "MANIFEST_HASH_MISMATCH", persistedHash: "bad");
        [Fact] public async Task VersionWithoutContract_FailsClosed() { var report = await Validator().ValidateAsync(new SqlConnection(), null, Identity, DatabaseScopes.ProductosServicios, 99, null); Assert.Equal(SchemaValidationGlobalResult.VersionIncompatible, report.GlobalResult); }
        [Fact] public async Task FutureVersion_FailsClosed() { var report = await Validator().ValidateAsync(new SqlConnection(), null, Identity, DatabaseScopes.ProductosServicios, 99, null); Assert.Contains(report.Items, i => i.ReasonCode == "VERSION_INCOMPATIBLE"); }
        [Fact] public async Task OutdatedVersion_ValidatesOwnContract() { var report = await Validator().ValidateAsync(new SqlConnection(), null, Identity, DatabaseScopes.ProductosServicios, 1, Hash(Contract())); Assert.Equal(SchemaValidationGlobalResult.SchemaOk, report.GlobalResult); }
        [Fact] public async Task MissingVersion_DoesNotAdopt() { var report = await Validator().ValidateAsync(new SqlConnection(), null, Identity, DatabaseScopes.ProductosServicios, null, null); Assert.Equal(SchemaValidationGlobalResult.RequiereRevision, report.GlobalResult); Assert.False(report.ChangedState); }
        [Fact] public async Task MetadataInsufficient_IsNoConcluyente() { var report = await Validator(new SchemaPhysicalSnapshot { MetadataSufficient = false }).ValidateAsync(new SqlConnection(), null, Identity, DatabaseScopes.ProductosServicios, 1, Hash(Contract())); Assert.Equal(SchemaValidationGlobalResult.ValidacionNoConcluyente, report.GlobalResult); }
        [Fact] public async Task Extras_AreClassifiedAndNotDeleted() => await Expect(s => s.Extras = new[] { "dbo.ProductosServiciosAudit:SQL_TRIGGER" }, SchemaValidationGlobalResult.SchemaDrift, "EXTRA_REVIEW");
        [Fact] public async Task DoesNotChangeCurrentVersion() { var report = await Validator().ValidateAsync(new SqlConnection(), null, Identity, DatabaseScopes.ProductosServicios, 1, Hash(Contract())); Assert.False(report.ChangedState); }
        [Fact] public async Task DoesNotChangeManifestHash() { var report = await Validator().ValidateAsync(new SqlConnection(), null, Identity, DatabaseScopes.ProductosServicios, 1, Hash(Contract())); Assert.Equal(Hash(Contract()), report.PersistedManifestHash); }
        [Fact] public async Task DoesNotExecuteDdl() { var report = await Validator().ValidateAsync(new SqlConnection(), null, Identity, DatabaseScopes.ProductosServicios, 1, Hash(Contract())); Assert.False(report.ExecutedDdl); }
        [Fact] public async Task T17CanUseSameConnectionTransactionApi() { FakeSnapshotReader reader = new(Snapshot()); var validator = Validator(reader); await validator.ValidateAsync(new SqlConnection(), null, Identity, DatabaseScopes.ProductosServicios, 1, Hash(Contract())); Assert.Equal(1, reader.ReadCount); }
        [Fact] public async Task PostValidationRereadsMetadata() { FakeSnapshotReader reader = new(Snapshot()); var validator = Validator(reader); await validator.ValidateAsync(new SqlConnection(), null, Identity, DatabaseScopes.ProductosServicios, 1, Hash(Contract())); await validator.ValidateAsync(new SqlConnection(), null, Identity, DatabaseScopes.ProductosServicios, 1, Hash(Contract())); Assert.Equal(2, reader.ReadCount); }
        [Fact] public async Task ResultIsDeterministic() { var a = await Validator().ValidateAsync(new SqlConnection(), null, Identity, DatabaseScopes.ProductosServicios, 1, Hash(Contract())); var b = await Validator().ValidateAsync(new SqlConnection(), null, Identity, DatabaseScopes.ProductosServicios, 1, Hash(Contract())); Assert.Equal(a.GlobalResult, b.GlobalResult); Assert.Equal(a.Items.Select(i => i.ReasonCode), b.Items.Select(i => i.ReasonCode)); }
        [Fact] public async Task SecretsAreAbsentFromItems() { var report = await Validator().ValidateAsync(new SqlConnection(), null, Identity, DatabaseScopes.ProductosServicios, 1, Hash(Contract())); Assert.DoesNotContain(report.Items, i => (i.Expected ?? string.Empty).Contains("Password") || (i.Actual ?? string.Empty).Contains("Password")); }

        private static async Task Expect(Action<MutableSnapshot>? mutate, SchemaValidationGlobalResult global, string? reason, string? persistedHash = null)
        {
            MutableSnapshot mutable = MutableSnapshot.From(Snapshot()); mutate?.Invoke(mutable);
            SchemaDriftReport report = await Validator(mutable.ToSnapshot()).ValidateAsync(new SqlConnection(), null, Identity, DatabaseScopes.ProductosServicios, 1, persistedHash ?? Hash(Contract()));
            Assert.Equal(global, report.GlobalResult);
            if (reason != null) Assert.Contains(report.Items, i => i.ReasonCode == reason || i.DifferenceType == reason);
            Assert.False(report.ChangedState);
            Assert.False(report.ExecutedDdl);
        }

        private static SchemaDriftValidator Validator(SchemaPhysicalSnapshot? snapshot = null) => Validator(new FakeSnapshotReader(snapshot ?? Snapshot()));
        private static SchemaDriftValidator Validator(FakeSnapshotReader reader) => new(new ThrowingFactory(), new FakeContractProvider(), new SchemaManifestProvider(), reader);
        private static string Hash(SchemaContract c) => new SchemaManifestProvider().CreateManifest(c).ManifestHash;
        private static void ReplaceColumn(MutableSnapshot s, string name, Func<SchemaColumnSnapshot, SchemaColumnSnapshot> map) => s.Columns = s.Columns.Select(c => c.Name == name ? map(c) : c).ToArray();
        private static void ReplaceIndex(MutableSnapshot s, string name, Func<SchemaIndexSnapshot, SchemaIndexSnapshot> map) => s.Indexes = s.Indexes.Select(i => i.Name == name ? map(i) : i).ToArray();

        private static SchemaContract Contract() => new(DatabaseScopes.ProductosServicios, 1, "ProductosServicios", new[]
        {
            new SchemaTableContract("dbo", "ProductosServicios", new[]
            {
                new SchemaColumnContract("id", "UNIQUEIDENTIFIER", null, null, null, false, null, false, false, null),
                new SchemaColumnContract("idEmpresa", "UNIQUEIDENTIFIER", null, null, null, false, null, false, false, null),
                new SchemaColumnContract("idCategoria", "UNIQUEIDENTIFIER", null, null, null, false, null, false, false, null),
                new SchemaColumnContract("Nombre", "NVARCHAR(100)", 100, null, null, false, null, false, false, null),
                new SchemaColumnContract("NombreNormalizado", "NVARCHAR(100)", 100, null, null, true, null, false, true, "UPPER(LTRIM(RTRIM(Nombre)))"),
                new SchemaColumnContract("Precio", "DECIMAL(18,2)", null, 18, 2, false, "(0)", false, false, null),
                new SchemaColumnContract("Activo", "BIT", null, null, null, false, "(1)", false, false, null)
            }, new SchemaPrimaryKeyContract("PK_ProductosServicios", new[] { "idEmpresa", "id" }, true), new[] { new SchemaForeignKeyContract("FK_PS_Categoria", new[] { "idEmpresa", "idCategoria" }, "dbo", "ProductosServiciosCategorias", new[] { "idEmpresa", "id" }) }, Array.Empty<SchemaUniqueContract>(), new[] { new SchemaCheckContract("CK_PS_Activo", "Activo IN (0,1)") }, new[] { new SchemaIndexContract("IX_PS_Activo", false, false, new[] { new SchemaIndexColumnContract("idEmpresa", false), new SchemaIndexColumnContract("Activo", false) }, new[] { "Nombre" }, "Activo = 1"), new SchemaIndexContract("UX_PS_Nombre", true, false, new[] { new SchemaIndexColumnContract("idEmpresa", false), new SchemaIndexColumnContract("Nombre", false) }, Array.Empty<string>(), null) })
        }, new[] { "fixture" }, new DateTime(2026, 9, 11, 0, 0, 0, DateTimeKind.Utc));

        private static SchemaPhysicalSnapshot Snapshot() => new()
        {
            Objects = new[] { new SchemaObjectSnapshot("dbo", "ProductosServicios", "USER_TABLE") },
            Tables = new[] { new SchemaTableSnapshot("dbo", "ProductosServicios") },
            Columns = new[]
            {
                new SchemaColumnSnapshot("dbo", "ProductosServicios", "id", "UNIQUEIDENTIFIER", null, null, null, false, null, false, null, null, false, null, null, null),
                new SchemaColumnSnapshot("dbo", "ProductosServicios", "idEmpresa", "UNIQUEIDENTIFIER", null, null, null, false, null, false, null, null, false, null, null, null),
                new SchemaColumnSnapshot("dbo", "ProductosServicios", "idCategoria", "UNIQUEIDENTIFIER", null, null, null, false, null, false, null, null, false, null, null, null),
                new SchemaColumnSnapshot("dbo", "ProductosServicios", "Nombre", "NVARCHAR(100)", 100, null, null, false, "SQL_Latin1_General_CP1_CI_AS", false, null, null, false, null, null, null),
                new SchemaColumnSnapshot("dbo", "ProductosServicios", "NombreNormalizado", "NVARCHAR(100)", 100, null, null, true, "SQL_Latin1_General_CP1_CI_AS", false, null, null, true, "UPPER(LTRIM(RTRIM(Nombre)))", false, null),
                new SchemaColumnSnapshot("dbo", "ProductosServicios", "Precio", "DECIMAL(18,2)", null, 18, 2, false, null, false, null, null, false, null, null, "((0))"),
                new SchemaColumnSnapshot("dbo", "ProductosServicios", "Activo", "BIT", null, null, null, false, null, false, null, null, false, null, null, "((1))")
            },
            PrimaryKeys = new[] { new SchemaPrimaryKeySnapshot("dbo", "ProductosServicios", "PK_ProductosServicios", new[] { new SchemaIndexColumnContract("idEmpresa", false), new SchemaIndexColumnContract("id", false) }, true, false) },
            Indexes = new[] { new SchemaIndexSnapshot("dbo", "ProductosServicios", "IX_PS_Activo", false, false, new[] { new SchemaIndexColumnContract("idEmpresa", false), new SchemaIndexColumnContract("Activo", false) }, new[] { "Nombre" }, "([Activo]=(1))", false, false), new SchemaIndexSnapshot("dbo", "ProductosServicios", "UX_PS_Nombre", true, false, new[] { new SchemaIndexColumnContract("idEmpresa", false), new SchemaIndexColumnContract("Nombre", false) }, Array.Empty<string>(), null, false, false) },
            ForeignKeys = new[] { new SchemaForeignKeySnapshot("dbo", "ProductosServicios", "FK_PS_Categoria", new[] { "idEmpresa", "idCategoria" }, "dbo", "ProductosServiciosCategorias", new[] { "idEmpresa", "id" }, "NO_ACTION", "NO_ACTION", false, false, false) },
            Checks = new[] { new SchemaCheckSnapshot("dbo", "ProductosServicios", "CK_PS_Activo", "([Activo] IN ((0),(1)))", false, false) }
        };

        private sealed class MutableSnapshot
        {
            public IReadOnlyCollection<SchemaObjectSnapshot> Objects { get; set; } = Array.Empty<SchemaObjectSnapshot>();
            public IReadOnlyCollection<SchemaTableSnapshot> Tables { get; set; } = Array.Empty<SchemaTableSnapshot>();
            public IReadOnlyCollection<SchemaColumnSnapshot> Columns { get; set; } = Array.Empty<SchemaColumnSnapshot>();
            public IReadOnlyCollection<SchemaPrimaryKeySnapshot> PrimaryKeys { get; set; } = Array.Empty<SchemaPrimaryKeySnapshot>();
            public IReadOnlyCollection<SchemaIndexSnapshot> Indexes { get; set; } = Array.Empty<SchemaIndexSnapshot>();
            public IReadOnlyCollection<SchemaForeignKeySnapshot> ForeignKeys { get; set; } = Array.Empty<SchemaForeignKeySnapshot>();
            public IReadOnlyCollection<SchemaCheckSnapshot> Checks { get; set; } = Array.Empty<SchemaCheckSnapshot>();
            public IReadOnlyCollection<string> Extras { get; set; } = Array.Empty<string>();
            public static MutableSnapshot From(SchemaPhysicalSnapshot s) => new() { Objects = s.Objects, Tables = s.Tables, Columns = s.Columns, PrimaryKeys = s.PrimaryKeys, Indexes = s.Indexes, ForeignKeys = s.ForeignKeys, Checks = s.Checks, Extras = s.Extras };
            public SchemaPhysicalSnapshot ToSnapshot() => new() { Objects = Objects, Tables = Tables, Columns = Columns, PrimaryKeys = PrimaryKeys, Indexes = Indexes, ForeignKeys = ForeignKeys, Checks = Checks, Extras = Extras };
        }

        private sealed class FakeSnapshotReader : ISchemaPhysicalSnapshotReader
        {
            private readonly SchemaPhysicalSnapshot _snapshot;
            public FakeSnapshotReader(SchemaPhysicalSnapshot snapshot) => _snapshot = snapshot;
            public int ReadCount { get; private set; }
            public Task<SchemaPhysicalSnapshot> ReadAsync(SqlConnection connection, SchemaContract contract, CancellationToken cancellationToken = default, SqlTransaction? transaction = null) { ReadCount++; return Task.FromResult(_snapshot); }
        }

        private sealed class FakeContractProvider : ISchemaContractProvider
        {
            public SchemaContract GetContract(string scope, int? version = null)
            {
                if (version is null or 1) return Contract();
                throw new ArgumentOutOfRangeException(nameof(version));
            }
        }

        private sealed class ThrowingFactory : ITenantSqlConnectionFactory
        {
            public SqlConnection CreateConnection(TenantDatabaseDescriptor descriptor) => throw new InvalidOperationException("not used");
        }
    }
}
