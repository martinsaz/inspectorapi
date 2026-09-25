using checklistWs.Services.Tenant;
using Xunit;

namespace checklistWs.Tests.Services.Tenant
{
    public sealed class OrdenesCompraSchemaContractTests
    {
        private readonly ProductosServiciosSchemaContractProvider _provider = new();
        private readonly SchemaManifestProvider _manifestProvider = new();

        [Fact]
        public void OrdenesCompraScope_IsKnownAndVersioned()
        {
            var known = new KnownSchemaVersionProvider();

            Assert.Equal(ProductosServiciosSchemaContractProvider.OrdenesCompraLatestVersion, known.GetKnownCurrentVersion(DatabaseScopes.OrdenesCompra));
            Assert.Equal(1, _provider.GetContract(DatabaseScopes.OrdenesCompra, 1).ContractVersion);
            Assert.Equal(2, _provider.GetContract(DatabaseScopes.OrdenesCompra, 2).ContractVersion);
            Assert.Throws<InvalidOperationException>(() => _provider.GetContract(DatabaseScopes.OrdenesCompra, 3));
        }

        [Fact]
        public void OrdenesCompraInventory_MatchesContractTables()
        {
            SchemaContract contract = Contract();
            IReadOnlyCollection<string> expected = new ProductScopeInventory().GetExpectedTables(DatabaseScopes.OrdenesCompra);

            Assert.Equal(new[]
            {
                "dbo.OrdenesCompra",
                "dbo.OrdenesCompraDetalle",
                "dbo.OrdenesCompraFolios",
                "dbo.OrdenesCompraPresentacionesCompra"
            }.OrderBy(x => x), expected.OrderBy(x => x));
            Assert.Equal(expected.OrderBy(x => x), contract.Tables.Select(t => t.FullName).OrderBy(x => x));
        }

        [Fact]
        public void PresentacionCompra_IsSeparatedFromPresentacionVenta()
        {
            SchemaTableContract table = Table("OrdenesCompraPresentacionesCompra");

            Assert.Contains(table.Columns, c => c.Name == "idProductoServicio" && !c.IsNullable);
            Assert.Contains(table.Columns, c => c.Name == "idVariante" && c.IsNullable);
            Assert.Contains(table.Columns, c => c.Name == "FactorConversionBase" && !c.IsNullable);
            Assert.Contains(table.Columns, c => c.Name == "PermiteCantidadBase" && !c.IsNullable && c.SqlType == "BIT" && c.DefaultDefinition == "((0))");
            Assert.Contains(table.ForeignKeys, fk => fk.Name == "FK_OCPresentacionesCompra_ProductosServicios_EmpresaId");
            Assert.DoesNotContain(table.Columns, c => c.Name.Contains("Venta", StringComparison.OrdinalIgnoreCase) || c.Name.Contains("Precio", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void PresentacionCompraV1_RemainsImmutableWithoutCantidadBaseFlag()
        {
            SchemaContract v1 = _provider.GetContract(DatabaseScopes.OrdenesCompra, 1);
            SchemaContract v2 = _provider.GetContract(DatabaseScopes.OrdenesCompra, 2);

            SchemaTableContract v1Table = v1.Tables.Single(t => t.Name == "OrdenesCompraPresentacionesCompra");
            SchemaTableContract v2Table = v2.Tables.Single(t => t.Name == "OrdenesCompraPresentacionesCompra");

            Assert.DoesNotContain(v1Table.Columns, c => c.Name == "PermiteCantidadBase");
            Assert.Contains(v2Table.Columns, c => c.Name == "PermiteCantidadBase");
        }

        [Fact]
        public void DetalleOrdenCompra_ModelsVariantsSnapshotsAndBaseQuantities()
        {
            SchemaTableContract table = Table("OrdenesCompraDetalle");
            string[] expectedColumns =
            {
                "idVariante",
                "idPresentacionCompra",
                "VarianteSnapshot",
                "PresentacionCompraSnapshot",
                "UnidadCompraSnapshot",
                "CantidadCompra",
                "FactorConversionSnapshot",
                "CantidadBaseOrdenada",
                "CantidadBaseRecibidaAcumulada",
                "CantidadBasePendiente",
                "EstadoPartida"
            };

            foreach (string column in expectedColumns)
            {
                Assert.Contains(table.Columns, c => c.Name == column);
            }

            Assert.Contains(table.ForeignKeys, fk => fk.Name == "FK_OrdenesCompraDetalle_Variantes_EmpresaId");
            Assert.Contains(table.ForeignKeys, fk => fk.Name == "FK_OrdenesCompraDetalle_PresentacionCompra_EmpresaId");
            Assert.DoesNotContain(table.ForeignKeys, fk => fk.Name == "FK_OrdenesCompraDetalle_ProductosServicios_EmpresaId");
            Assert.DoesNotContain(table.UniqueConstraints, ux => ux.Name == "UX_OrdenesCompraDetalle_Empresa_Orden_ProductoServicio_Activo");
        }

        [Fact]
        public void ManifestHash_IsStableForOc02Contract()
        {
            SchemaContract contract = Contract();

            Assert.Equal("0977353cc806ec35d21c95c4149e16cdf41b3480d52b929b13ec82185957e802", _manifestProvider.CreateManifest(contract).ManifestHash);
        }

        private SchemaContract Contract()
        {
            return _provider.GetContract(DatabaseScopes.OrdenesCompra);
        }

        private SchemaTableContract Table(string name)
        {
            return Contract().Tables.Single(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
