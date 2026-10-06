using checklistWs.Services.Tenant;
using Xunit;

namespace checklistWs.Tests.Services.Tenant
{
    public sealed class CotizacionesSchemaContractTests
    {
        private const string SupersededV2Hash = "e936afda626b83bafbc3c09091f721c94bffaa0957ef96f00fad9c0a38c1c8da";
        private readonly ProductosServiciosSchemaContractProvider _contracts = new();
        private readonly SchemaManifestProvider _manifests = new();
        private readonly ProductosServiciosMigrationPackageProvider _packages;

        public CotizacionesSchemaContractTests()
        {
            _packages = new ProductosServiciosMigrationPackageProvider(_contracts, _manifests);
        }

        [Fact]
        public void Scope_IsKnownAndVersionedV1ToV2()
        {
            Assert.Equal(2, new KnownSchemaVersionProvider().GetKnownCurrentVersion(DatabaseScopes.Cotizaciones));
            Assert.Equal(1, Contract(1).ContractVersion);
            Assert.Equal(2, Contract(2).ContractVersion);
            Assert.Throws<InvalidOperationException>(() => Contract(3));
        }

        [Fact]
        public void CompatibilityGateAndRunner_RecognizeCotizacionesScope()
        {
            var gateMethod = typeof(ProductosServiciosCompatibilityGate).GetMethod("IsSupportedScope", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var runnerMethod = typeof(SchemaMigrationRunner).GetMethod("IsSupportedScope", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            Assert.True((bool)gateMethod!.Invoke(null, new object[] { DatabaseScopes.Cotizaciones })!);
            Assert.True((bool)runnerMethod!.Invoke(null, new object[] { DatabaseScopes.Cotizaciones })!);
            Assert.Equal(2, new KnownSchemaVersionProvider().GetKnownCurrentVersion(DatabaseScopes.Cotizaciones));
            Assert.True(Contract(1).ContractVersion < Contract(2).ContractVersion);
        }

        [Fact]
        public void V1_RepresentsHistoricalPhysicalTablesExactly()
        {
            SchemaContract contract = Contract(1);

            Assert.Equal(new[] { "Cotizaciones", "CotizacionesPartidas" }, contract.Tables.Select(table => table.Name));
            Assert.Equal(26, Table(contract, "Cotizaciones").Columns.Count);
            Assert.Equal(26, Table(contract, "CotizacionesPartidas").Columns.Count);
            Assert.All(contract.Tables, table => Assert.Empty(table.CheckConstraints));
            Assert.Equal(new[] { "IX_Cotizaciones_Empresa_Cliente", "IX_Cotizaciones_Empresa_Fecha_Estado", "UX_Cotizaciones_Empresa_Folio" }, Table(contract, "Cotizaciones").Indexes.Select(index => index.Name).OrderBy(name => name));
            Assert.Equal(new[] { "IX_CotizacionesPartidas_Cotizacion_Numero" }, Table(contract, "CotizacionesPartidas").Indexes.Select(index => index.Name));
            SchemaForeignKeyContract historicalFk = Assert.Single(Table(contract, "CotizacionesPartidas").ForeignKeys);
            Assert.Equal("FK_CotizacionesPartidas_Cotizaciones", historicalFk.Name);
            Assert.Equal(new[] { "idCotizacion" }, historicalFk.Columns);
            Assert.Equal(new[] { "id" }, historicalFk.ReferencedColumns);
        }

        [Fact]
        public void V1_HistoricalDefaultsAndDescendingDateArePreserved()
        {
            SchemaTableContract cotizaciones = Table(Contract(1), "Cotizaciones");
            SchemaTableContract partidas = Table(Contract(1), "CotizacionesPartidas");

            Assert.Equal("((0))", Column(cotizaciones, "VigenciaDias").DefaultDefinition);
            Assert.Equal("(N'')", Column(cotizaciones, "Observaciones").DefaultDefinition);
            Assert.Equal("((1))", Column(cotizaciones, "Activo").DefaultDefinition);
            Assert.Equal("((0))", Column(partidas, "DescuentoPct").DefaultDefinition);
            Assert.True(cotizaciones.Indexes.Single(index => index.Name == "IX_Cotizaciones_Empresa_Fecha_Estado").KeyColumns.Single(column => column.Name == "FechaCotizacion").IsDescending);
        }

        [Fact]
        public void Inventory_ContainsV2ScopeTables()
        {
            Assert.Equal(
                new[] { "dbo.Cotizaciones", "dbo.CotizacionesHistorial", "dbo.CotizacionesPartidas" },
                new ProductScopeInventory().GetExpectedTables(DatabaseScopes.Cotizaciones).OrderBy(name => name));
        }

        [Fact]
        public void V2_HeaderStoresListAndCloneOriginTenantSafely()
        {
            SchemaTableContract table = Table(Contract(2), "Cotizaciones");

            AssertNullable(table, "idListaPrecio", "UNIQUEIDENTIFIER");
            AssertNullable(table, "ListaPrecioNivel", "TINYINT");
            AssertNullable(table, "idCotizacionOrigen", "UNIQUEIDENTIFIER");
            Assert.Contains(table.CheckConstraints, check => check.Name == "CK_Cotizaciones_ListaPrecio" && check.Definition.Contains("BETWEEN 1 AND 10", StringComparison.Ordinal));
            Assert.Contains(table.ForeignKeys, fk => fk.Name == "FK_Cotizaciones_Origen_EmpresaId" && fk.Columns.SequenceEqual(new[] { "idEmpresa", "idCotizacionOrigen" }));
        }

        [Fact]
        public void V2_SnapshotContainsAllLp08FieldsAndKeepsHistoricalFieldsNullable()
        {
            SchemaTableContract table = Partidas();
            string[] fields =
            {
                "TipoIdentidad", "idVariante", "idPresentacionVenta", "idListaPrecio", "ListaPrecioNivel",
                "PrecioBase", "PrecioLista", "OrigenPrecio", "DescuentoListaPct", "SubtotalAntesRedondeo",
                "RedondeoModo", "PrecioFinal", "VigenciaInicio", "VigenciaFin", "ReglaVersion",
                "FechaResolucionUtc", "CorrelationId"
            };

            Assert.All(fields, field => Assert.True(Column(table, field).IsNullable, field));
            Assert.Contains(table.CheckConstraints, check => check.Name == "CK_CotizacionesPartidas_Snapshot" && check.Definition.Contains("ReglaVersion IS NULL", StringComparison.Ordinal) && check.Definition.Contains("ReglaVersion IS NOT NULL", StringComparison.Ordinal));
        }

        [Fact]
        public void V2_PriceZeroIsValidAndNegativePricesAreRejectedByContract()
        {
            string definition = Check("CK_CotizacionesPartidas_Precios").Definition;

            Assert.Contains("PrecioFinal >= 0", definition, StringComparison.Ordinal);
            Assert.DoesNotContain("PrecioFinal > 0", definition, StringComparison.Ordinal);
            Assert.Contains("PrecioUnitario >= 0", definition, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData(1, "TipoProductoServicio = 1", "idVariante IS NULL", "idPresentacionVenta IS NULL")]
        [InlineData(2, "TipoProductoServicio = 2", "idVariante IS NULL", "idPresentacionVenta IS NULL")]
        [InlineData(3, "TipoProductoServicio = 1", "idVariante IS NOT NULL", "idPresentacionVenta IS NULL")]
        [InlineData(4, "TipoProductoServicio = 1", "idVariante IS NULL", "idPresentacionVenta IS NOT NULL")]
        public void V2_IdentityFormsAreRepresented(int type, string productType, string variant, string presentation)
        {
            string definition = Check("CK_CotizacionesPartidas_Identidad").Definition;

            Assert.Contains($"TipoIdentidad = {type}", definition, StringComparison.Ordinal);
            Assert.Contains(productType, definition, StringComparison.Ordinal);
            Assert.Contains(variant, definition, StringComparison.Ordinal);
            Assert.Contains(presentation, definition, StringComparison.Ordinal);
        }

        [Fact]
        public void V2_InvalidIdentityShapeIsNotAnAllowedBranch()
        {
            string definition = Check("CK_CotizacionesPartidas_Identidad").Definition;

            Assert.DoesNotContain("idVariante IS NOT NULL AND idPresentacionVenta IS NOT NULL", definition, StringComparison.Ordinal);
            Assert.Contains("TipoIdentidad IS NULL", definition, StringComparison.Ordinal);
        }

        [Fact]
        public void V2_ListBoundariesAndDiscountBoundariesAreExplicit()
        {
            Assert.Contains("BETWEEN 1 AND 10", Check("CK_CotizacionesPartidas_ListaNivel").Definition, StringComparison.Ordinal);
            Assert.Contains("DescuentoListaPct BETWEEN 0 AND 100", Check("CK_CotizacionesPartidas_Descuentos").Definition, StringComparison.Ordinal);
            Assert.Contains("DescuentoPct BETWEEN 0 AND 100", Check("CK_CotizacionesPartidas_Descuentos").Definition, StringComparison.Ordinal);
        }

        [Fact]
        public void V2_RoundingValidityAndOverrideCoherenceAreProtected()
        {
            Assert.Contains("IN (0, 1, 2)", Check("CK_CotizacionesPartidas_Redondeo").Definition, StringComparison.Ordinal);
            Assert.Contains("VigenciaInicio <= VigenciaFin", Check("CK_CotizacionesPartidas_Vigencia").Definition, StringComparison.Ordinal);
            string definition = Check("CK_CotizacionesPartidas_Override").Definition;
            Assert.Contains("PrecioOverride = 1", definition, StringComparison.Ordinal);
            Assert.Contains("MotivoPrecioOverride", definition, StringComparison.Ordinal);
            Assert.Contains("idUsuarioPrecioOverride IS NOT NULL", definition, StringComparison.Ordinal);
            Assert.Contains("FechaPrecioOverrideUtc IS NOT NULL", definition, StringComparison.Ordinal);
        }

        [Fact]
        public void V2_ReusesPrecioUnitarioAndActivoWithoutRedundantAppliedPrice()
        {
            SchemaTableContract table = Partidas();

            Assert.Contains(table.Columns, column => column.Name == "PrecioUnitario" && !column.IsNullable);
            Assert.Contains(table.Columns, column => column.Name == "Activo" && !column.IsNullable);
            Assert.DoesNotContain(table.Columns, column => column.Name == "PrecioAplicado");
        }

        [Fact]
        public void V2_HasOwnAppendOnlyAuditContract()
        {
            SchemaTableContract history = Table(Contract(2), "CotizacionesHistorial");

            Assert.DoesNotContain(history.Columns, column => column.Name == "Activo");
            Assert.Contains(history.Columns, column => column.Name == "CorrelationId" && !column.IsNullable);
            Assert.Contains(history.CheckConstraints, check => check.Name == "CK_CotizacionesHistorial_Operacion" && check.Definition.Contains("CLON_RE_RESOLUCION", StringComparison.Ordinal));
            Assert.DoesNotContain(Contract(2).Tables, table => table.Name == "ListaPreciosHistorial");
        }

        [Fact]
        public void V2_PreservesHistoricalForeignKeyAndAddsTenantSafeForeignKeys()
        {
            foreach (SchemaTableContract table in Contract(2).Tables)
            {
                Assert.All(
                    table.ForeignKeys.Where(fk => fk.Name != "FK_CotizacionesPartidas_Cotizaciones"),
                    fk => Assert.Equal("idEmpresa", fk.Columns.First()));
            }

            SchemaTableContract partidas = Table(Contract(2), "CotizacionesPartidas");
            Assert.Contains(partidas.ForeignKeys, fk => fk.Name == "FK_CotizacionesPartidas_Cotizaciones" && fk.Columns.SequenceEqual(new[] { "idCotizacion" }));
            Assert.Contains(partidas.ForeignKeys, fk => fk.Name == "FK_CotizacionesPartidas_Cotizaciones_EmpresaId" && fk.Columns.SequenceEqual(new[] { "idEmpresa", "idCotizacion" }));
            Assert.Contains(Table(Contract(2), "Cotizaciones").Indexes, index => index.Name == "IX_Cotizaciones_Empresa_Cliente");
        }

        [Fact]
        public void Package_IsApprovedAdditiveTransactionalAndHasNoCommercialDml()
        {
            SchemaMigrationPackage package = Package();
            SchemaMigrationDefinition migration = package.Migrations.Single(item => item.MigrationId == "COT-M20260930-V1-V2-LP08-SNAPSHOT");

            Assert.Equal("COT-M20260930-V1-V2-LP08-SNAPSHOT", migration.MigrationId);
            Assert.Equal("SingleTransaction", migration.TransactionMode);
            Assert.True(migration.AutoApplicable);
            Assert.DoesNotContain("\nGO", migration.UpSql, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("UPDATE dbo.Cotizaciones", migration.UpSql, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("DELETE FROM", migration.UpSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("NO_COMMERCIAL_BACKFILL", migration.DataPreconditions);
            Assert.Equal(SchemaMigrationHash.Sha256(migration.UpSql), migration.SqlHash);
        }

        [Fact]
        public void Package_SourceTargetAndManifestAreConsistent()
        {
            SchemaMigrationPackage package = Package();
            SchemaMigrationDefinition migration = package.Migrations.Single(item => item.MigrationId == "COT-M20260930-V1-V2-LP08-SNAPSHOT");

            Assert.Equal(1, migration.SourceContract!.ContractVersion);
            Assert.Equal(2, migration.TargetContract.ContractVersion);
            Assert.Equal(_manifests.CreateManifest(Contract(2)).ManifestHash, migration.TargetManifestHash);
            Assert.Equal(new[] { _manifests.CreateManifest(Contract(1)).ManifestHash }, migration.Dependencies);
            Assert.DoesNotContain(new string('0', 64), migration.Dependencies);
        }

        [Fact]
        public void Package_ResolvesV1ToV2AndSecondRunHasNoPendingMigrations()
        {
            SchemaMigrationPackage package = Package();
            DatabaseIdentity identity = new("SERVER", "SERVER", string.Empty, "DATABASE", "11111111-1111-1111-1111-111111111111");
            SchemaMigrationResolver resolver = new();

            Assert.Equal(SchemaMigrationResolutionStatus.Ready, resolver.GetPendingMigrations(identity, DatabaseScopes.Cotizaciones, 1, 2, package, Array.Empty<SchemaControlHistory>()).Status);
            string currentV2Hash = _manifests.CreateManifest(Contract(2)).ManifestHash;
            SchemaMigrationResolution second = resolver.GetPendingMigrations(identity, DatabaseScopes.Cotizaciones, 2, 2, package, Array.Empty<SchemaControlHistory>(), currentV2Hash);
            Assert.Equal(SchemaMigrationResolutionStatus.NoPendingMigrations, second.Status);
            Assert.Equal("NO_PENDING_MIGRATIONS", second.ReasonCode);
        }

        [Fact]
        public void ManifestHashes_AreStable()
        {
            Assert.Equal("ae905dfc622135c5858fdf2c551287b38193fe3f92d4ccbe61eec374a1deb9b8", _manifests.CreateManifest(Contract(1)).ManifestHash);
            Assert.Equal("5310c00e5991ed0e1c06bc84560a7d94b861b8ab1e310ad395a0303e9426e765", _manifests.CreateManifest(Contract(2)).ManifestHash);
        }

        [Fact]
        public void ListaPreciosV2Hash_RemainsUnchanged()
        {
            Assert.Equal("e7a388ec985a19fb2b3beb73e8c2cf28d5dda17d3f3bb2f0363683c166092882", _manifests.CreateManifest(_contracts.GetContract(DatabaseScopes.ListaPrecios, 2)).ManifestHash);
        }

        [Fact]
        public void PackageCheckDefinitions_MatchFrozenTargetContract()
        {
            string sql = Package().Migrations.Single(item => item.MigrationId == "COT-M20260930-V1-V2-LP08-SNAPSHOT").UpSql;

            foreach (SchemaCheckContract check in Contract(2).Tables.SelectMany(table => table.CheckConstraints))
            {
                string escapedDefinition = check.Definition.Replace("'", "''", StringComparison.Ordinal);
                Assert.Contains($"CONSTRAINT {check.Name} {escapedDefinition}", sql, StringComparison.Ordinal);
            }

            Assert.Equal("5310c00e5991ed0e1c06bc84560a7d94b861b8ab1e310ad395a0303e9426e765", _manifests.CreateManifest(Contract(2)).ManifestHash);
        }

        [Fact]
        public void ReconciliationPackage_RepairsOnlyMissingHistoricalObjectsAtCommercialV2()
        {
            SchemaMigrationDefinition reconciliation = Package().Migrations.Single(item => item.MigrationId == "COT-M20261001-V2-RECONCILE-HISTORICAL-OBJECTS");

            Assert.Equal(2, reconciliation.FromVersion);
            Assert.Equal(2, reconciliation.ToVersion);
            Assert.Equal(new[] { SupersededV2Hash }, reconciliation.Dependencies);
            Assert.Equal(SupersededV2Hash, _manifests.CreateManifest(reconciliation.SourceContract!).ManifestHash);
            Assert.Equal(_manifests.CreateManifest(Contract(2)).ManifestHash, reconciliation.TargetManifestHash);
            Assert.Equal(2, reconciliation.Preconditions.Count);
            Assert.All(reconciliation.Preconditions, precondition => Assert.Contains("SELECT CASE WHEN OBJECT_ID", precondition, StringComparison.Ordinal));
            Assert.Contains("CREATE INDEX IX_Cotizaciones_Empresa_Cliente", reconciliation.UpSql, StringComparison.Ordinal);
            Assert.Contains("ADD CONSTRAINT FK_CotizacionesPartidas_Cotizaciones", reconciliation.UpSql, StringComparison.Ordinal);
            Assert.DoesNotContain("DROP ", reconciliation.UpSql, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("UPDATE ", reconciliation.UpSql, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("DELETE ", reconciliation.UpSql, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void SupersededV2State_ResolvesReconciliationAndCurrentV2IsIdempotent()
        {
            SchemaMigrationPackage package = Package();
            SchemaMigrationDefinition original = package.Migrations.Single(item => item.MigrationId == "COT-M20260930-V1-V2-LP08-SNAPSHOT");
            DatabaseIdentity identity = new("SERVER", "SERVER", string.Empty, "DATABASE", "11111111-1111-1111-1111-111111111111");
            SchemaControlHistory oldHistory = new()
            {
                EventType = "MIGRATED",
                Result = "PASS",
                OperationId = original.MigrationId,
                FromVersion = 1,
                ToVersion = 2,
                Details = $"MigrationId={original.MigrationId}; SqlHash={original.SqlHash}; TargetManifestHash={SupersededV2Hash}"
            };
            SchemaMigrationResolver resolver = new();

            SchemaMigrationResolution pending = resolver.GetPendingMigrations(identity, DatabaseScopes.Cotizaciones, 2, 2, package, new[] { oldHistory }, SupersededV2Hash);
            Assert.Equal(SchemaMigrationResolutionStatus.Ready, pending.Status);
            Assert.Equal("COT-M20261001-V2-RECONCILE-HISTORICAL-OBJECTS", Assert.Single(pending.PendingMigrations).MigrationId);

            string currentHash = _manifests.CreateManifest(Contract(2)).ManifestHash;
            SchemaMigrationResolution second = resolver.GetPendingMigrations(identity, DatabaseScopes.Cotizaciones, 2, 2, package, new[] { oldHistory }, currentHash);
            Assert.Equal(SchemaMigrationResolutionStatus.NoPendingMigrations, second.Status);
        }

        [Fact]
        public void UnknownV2Hash_DoesNotReceiveAuthorizedReconciliation()
        {
            SchemaMigrationPackage package = Package();
            DatabaseIdentity identity = new("SERVER", "SERVER", string.Empty, "DATABASE", "11111111-1111-1111-1111-111111111111");

            SchemaMigrationResolution result = new SchemaMigrationResolver().GetPendingMigrations(
                identity, DatabaseScopes.Cotizaciones, 2, 2, package, Array.Empty<SchemaControlHistory>(), new string('a', 64));

            Assert.Equal(SchemaMigrationResolutionStatus.NoPendingMigrations, result.Status);
            Assert.Empty(result.PendingMigrations);
        }

        [Fact]
        public void SqlServerCanonicalizedCotizacionChecks_MatchFrozenContract()
        {
            var actual = new Dictionary<string, string>
            {
                ["CK_Cotizaciones_ListaPrecio"] = "([idListaPrecio] IS NULL AND [ListaPrecioNivel] IS NULL OR [idListaPrecio] IS NOT NULL AND ([ListaPrecioNivel]>=(1) AND [ListaPrecioNivel]<=(10)))",
                ["CK_CotizacionesPartidas_Descuentos"] = "(([DescuentoListaPct] IS NULL OR [DescuentoListaPct]>=(0) AND [DescuentoListaPct]<=(100)) AND ([ReglaVersion] IS NULL OR [DescuentoPct]>=(0) AND [DescuentoPct]<=(100)))",
                ["CK_CotizacionesPartidas_ListaNivel"] = "([ListaPrecioNivel] IS NULL OR [ListaPrecioNivel]>=(1) AND [ListaPrecioNivel]<=(10))",
                ["CK_CotizacionesPartidas_Snapshot"] = "([ReglaVersion] IS NULL AND [TipoIdentidad] IS NULL AND [idVariante] IS NULL AND [idPresentacionVenta] IS NULL AND [idListaPrecio] IS NULL AND [ListaPrecioNivel] IS NULL AND [PrecioBase] IS NULL AND [PrecioLista] IS NULL AND [OrigenPrecio] IS NULL AND [DescuentoListaPct] IS NULL AND [SubtotalAntesRedondeo] IS NULL AND [RedondeoModo] IS NULL AND [PrecioFinal] IS NULL AND [VigenciaInicio] IS NULL AND [VigenciaFin] IS NULL AND [FechaResolucionUtc] IS NULL AND [CorrelationId] IS NULL AND [PrecioOverride] IS NULL OR [ReglaVersion] IS NOT NULL AND [TipoIdentidad] IS NOT NULL AND [idListaPrecio] IS NOT NULL AND ([ListaPrecioNivel]>=(1) AND [ListaPrecioNivel]<=(10)) AND [PrecioBase] IS NOT NULL AND nullif(ltrim(rtrim([OrigenPrecio])),N'') IS NOT NULL AND [SubtotalAntesRedondeo] IS NOT NULL AND [RedondeoModo] IS NOT NULL AND [PrecioFinal] IS NOT NULL AND [FechaResolucionUtc] IS NOT NULL AND [CorrelationId] IS NOT NULL AND ([PrecioOverride]=(1) OR [PrecioOverride]=(0)))"
            };

            foreach ((string name, string definition) in actual)
            {
                SchemaCheckContract expected = Contract(2).Tables.SelectMany(table => table.CheckConstraints).Single(check => check.Name == name);
                Assert.True(SchemaDefinitionNormalizer.Same(definition, expected.Definition), name);
            }
        }

        [Fact]
        public void SemanticallyDifferentCotizacionChecks_RemainMismatches()
        {
            Assert.False(SchemaDefinitionNormalizer.Same("ListaPrecioNivel IS NULL OR (ListaPrecioNivel >= 1 AND ListaPrecioNivel <= 11)", Check("CK_CotizacionesPartidas_ListaNivel").Definition));
            Assert.False(SchemaDefinitionNormalizer.Same("(DescuentoListaPct IS NULL OR (DescuentoListaPct >= 0 AND DescuentoListaPct <= 101)) AND (ReglaVersion IS NULL OR (DescuentoPct >= 0 AND DescuentoPct <= 100))", Check("CK_CotizacionesPartidas_Descuentos").Definition));
            Assert.False(SchemaDefinitionNormalizer.Same("(idListaPrecio IS NULL AND ListaPrecioNivel IS NULL) OR (idListaPrecio IS NOT NULL AND ListaPrecioNivel >= 0 AND ListaPrecioNivel <= 10)", Table(Contract(2), "Cotizaciones").CheckConstraints.Single(check => check.Name == "CK_Cotizaciones_ListaPrecio").Definition));
            Assert.False(SchemaDefinitionNormalizer.Same("ReglaVersion IS NULL OR (ReglaVersion IS NOT NULL AND ListaPrecioNivel >= 1 AND ListaPrecioNivel <= 10)", Check("CK_CotizacionesPartidas_Snapshot").Definition));
        }

        private SchemaContract Contract(int version) => _contracts.GetContract(DatabaseScopes.Cotizaciones, version);
        private SchemaMigrationPackage Package() => _packages.GetPackage(DatabaseScopes.Cotizaciones);
        private SchemaTableContract Partidas() => Table(Contract(2), "CotizacionesPartidas");
        private SchemaCheckContract Check(string name) => Partidas().CheckConstraints.Single(check => check.Name == name);
        private static SchemaTableContract Table(SchemaContract contract, string name) => contract.Tables.Single(table => table.Name == name);
        private static SchemaColumnContract Column(SchemaTableContract table, string name) => table.Columns.Single(column => column.Name == name);

        private static void AssertNullable(SchemaTableContract table, string name, string sqlType)
        {
            SchemaColumnContract column = Column(table, name);
            Assert.True(column.IsNullable);
            Assert.Equal(sqlType, column.SqlType);
        }
    }
}
