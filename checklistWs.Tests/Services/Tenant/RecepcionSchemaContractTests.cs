using checklistWs.Services.Tenant;
using Xunit;

namespace checklistWs.Tests.Services.Tenant
{
    public sealed class RecepcionSchemaContractTests
    {
        private readonly ProductosServiciosSchemaContractProvider _provider = new();
        private readonly SchemaManifestProvider _manifestProvider = new();

        [Fact]
        public void RecepcionScope_IsKnownAndVersioned()
        {
            var known = new KnownSchemaVersionProvider();

            Assert.Equal(ProductosServiciosSchemaContractProvider.RecepcionLatestVersion, known.GetKnownCurrentVersion(DatabaseScopes.Recepcion));
            Assert.Equal(1, _provider.GetContract(DatabaseScopes.Recepcion, 1).ContractVersion);
            Assert.Throws<InvalidOperationException>(() => _provider.GetContract(DatabaseScopes.Recepcion, 2));
        }

        [Fact]
        public void RecepcionInventory_MatchesContractTables()
        {
            SchemaContract contract = Contract();
            IReadOnlyCollection<string> expected = new ProductScopeInventory().GetExpectedTables(DatabaseScopes.Recepcion);

            Assert.Equal(new[]
            {
                "dbo.RecepcionFolios",
                "dbo.Recepciones",
                "dbo.RecepcionPartidas",
                "dbo.RecepcionSeries"
            }.OrderBy(x => x), expected.OrderBy(x => x));
            Assert.Equal(expected.OrderBy(x => x), contract.Tables.Select(t => t.FullName).OrderBy(x => x));
        }

        [Fact]
        public void Cabecera_UsesOcSucursalOperationKeyAndStates()
        {
            SchemaTableContract table = Table("Recepciones");

            Assert.Contains(table.Columns, c => c.Name == "idOrdenCompra" && !c.IsNullable);
            Assert.Contains(table.Columns, c => c.Name == "idSucursal" && !c.IsNullable);
            Assert.Contains(table.Columns, c => c.Name == "OperationKey" && !c.IsNullable);
            Assert.Contains(table.UniqueConstraints, ux => ux.Name == "UX_Recepciones_Empresa_OperationKey");
            Assert.Contains(table.ForeignKeys, fk => fk.Name == "FK_Recepciones_OrdenesCompra_EmpresaId");
            Assert.Contains(table.ForeignKeys, fk => fk.Name == "FK_Recepciones_Sucursales_EmpresaId");
            Assert.Contains(table.CheckConstraints, ck => ck.Name == "CK_Recepciones_Estado");
        }

        [Fact]
        public void Partidas_CapturePurchasePresentationFactorAndInventoryMovement()
        {
            SchemaTableContract table = Table("RecepcionPartidas");

            string[] expectedColumns =
            {
                "idOrdenCompraDetalle",
                "idVariante",
                "idPresentacionCompra",
                "PresentacionCompraSnapshot",
                "UnidadCompraSnapshot",
                "FactorConversionSnapshot",
                "CantidadCompraRecibida",
                "CantidadBaseEstaRecepcion",
                "CantidadBaseRecibidaAnterior",
                "CantidadBaseRecibidaAcumulada",
                "CantidadBasePendiente",
                "ControlSerie",
                "idInventarioMovimiento"
            };

            foreach (string column in expectedColumns)
            {
                Assert.Contains(table.Columns, c => c.Name == column);
            }

            Assert.Contains(table.ForeignKeys, fk => fk.Name == "FK_RecepcionPartidas_OrdenesCompraDetalle_EmpresaId");
            Assert.Contains(table.ForeignKeys, fk => fk.Name == "FK_RecepcionPartidas_InventarioMovimiento_EmpresaId");
            Assert.Contains(table.CheckConstraints, ck => ck.Name == "CK_RecepcionPartidas_NoSobreRecepcion");
        }

        [Fact]
        public void Series_LinkReceptionPartidaAndInventarioSerie()
        {
            SchemaTableContract table = Table("RecepcionSeries");

            Assert.Contains(table.Columns, c => c.Name == "NumeroSerie" && !c.IsNullable);
            Assert.Contains(table.Columns, c => c.Name == "idInventarioSerie" && c.IsNullable);
            Assert.Contains(table.UniqueConstraints, ux => ux.Name == "UX_RecepcionSeries_Empresa_Partida_Numero");
            Assert.Contains(table.ForeignKeys, fk => fk.Name == "FK_RecepcionSeries_InventarioSeries_EmpresaId");
        }

        [Fact]
        public void ManifestHash_IsStableForRec01Contract()
        {
            Assert.Equal("c26551d2eb625dda1138a2b5aad2ad83a3b074ea026082feb7714c97c229fd5f", _manifestProvider.CreateManifest(Contract()).ManifestHash);
        }

        private SchemaContract Contract()
        {
            return _provider.GetContract(DatabaseScopes.Recepcion);
        }

        private SchemaTableContract Table(string name)
        {
            return Contract().Tables.Single(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
