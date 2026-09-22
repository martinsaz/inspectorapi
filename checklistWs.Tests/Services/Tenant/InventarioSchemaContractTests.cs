using checklistWs.Services.Tenant;
using Xunit;

namespace checklistWs.Tests.Services.Tenant
{
    public sealed class InventarioSchemaContractTests
    {
        private readonly ProductosServiciosSchemaContractProvider _provider = new();
        private readonly SchemaManifestProvider _manifestProvider = new();

        [Fact]
        public void InventarioScope_IsKnownAndVersioned()
        {
            var known = new KnownSchemaVersionProvider();

            Assert.Equal(ProductosServiciosSchemaContractProvider.InventarioLatestVersion, known.GetKnownCurrentVersion(DatabaseScopes.Inventario));
            Assert.Equal(1, _provider.GetContract(DatabaseScopes.Inventario, 1).ContractVersion);
            Assert.Throws<InvalidOperationException>(() => _provider.GetContract(DatabaseScopes.Inventario, 2));
        }

        [Fact]
        public void InventarioInventory_MatchesContractTables()
        {
            SchemaContract contract = Contract();
            IReadOnlyCollection<string> expected = new ProductScopeInventory().GetExpectedTables(DatabaseScopes.Inventario);

            Assert.Equal(new[]
            {
                "dbo.InventarioSaldos",
                "dbo.InventarioMovimientos",
                "dbo.InventarioSeries"
            }.OrderBy(x => x), expected.OrderBy(x => x));
            Assert.Equal(expected.OrderBy(x => x), contract.Tables.Select(t => t.FullName).OrderBy(x => x));
        }

        [Fact]
        public void Saldos_UseBranchVariantProductGranularity()
        {
            SchemaTableContract table = Table("InventarioSaldos");

            Assert.Contains(table.Columns, c => c.Name == "idSucursal" && !c.IsNullable);
            Assert.Contains(table.Columns, c => c.Name == "idProductoServicio" && !c.IsNullable);
            Assert.Contains(table.Columns, c => c.Name == "idVariante" && c.IsNullable);
            Assert.Contains(table.Columns, c => c.Name == "CantidadBaseActual" && c.SqlType == "DECIMAL(28,12)");
            Assert.Contains(table.UniqueConstraints, ux => ux.Name == "UX_InventarioSaldos_Empresa_Sucursal_Producto_Variante");
        }

        [Fact]
        public void Movimientos_UseOperationKeyAndLedgerBalances()
        {
            SchemaTableContract table = Table("InventarioMovimientos");

            Assert.Contains(table.Columns, c => c.Name == "OperationKey" && !c.IsNullable);
            Assert.Contains(table.Columns, c => c.Name == "CantidadBase" && !c.IsNullable);
            Assert.Contains(table.Columns, c => c.Name == "SaldoAnterior" && !c.IsNullable);
            Assert.Contains(table.Columns, c => c.Name == "SaldoPosterior" && !c.IsNullable);
            Assert.Contains(table.UniqueConstraints, ux => ux.Name == "UX_InventarioMovimientos_Empresa_OperationKey");
        }

        [Fact]
        public void Series_DoNotAllowSameActiveNumberAcrossBranches()
        {
            SchemaTableContract table = Table("InventarioSeries");

            SchemaUniqueContract unique = Assert.Single(table.UniqueConstraints, ux => ux.Name == "UX_InventarioSeries_Empresa_Producto_Variante_Numero_Activo");
            Assert.DoesNotContain("idSucursalActual", unique.Columns);
            Assert.Equal("Estado = 1 AND FechaArchivado IS NULL", unique.FilterDefinition);
        }

        [Fact]
        public void ManifestHash_IsStableForInv01Contract()
        {
            Assert.Equal("146bd87ec94a7f69f9c84d61c867ee7e99ccf931138d843b0356573fe0e293b2", _manifestProvider.CreateManifest(Contract()).ManifestHash);
        }

        private SchemaContract Contract()
        {
            return _provider.GetContract(DatabaseScopes.Inventario);
        }

        private SchemaTableContract Table(string name)
        {
            return Contract().Tables.Single(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
