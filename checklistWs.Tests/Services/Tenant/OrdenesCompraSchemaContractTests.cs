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
            Assert.Throws<InvalidOperationException>(() => _provider.GetContract(DatabaseScopes.OrdenesCompra, 2));
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
            Assert.Contains(table.ForeignKeys, fk => fk.Name == "FK_OCPresentacionesCompra_ProductosServicios_EmpresaId");
            Assert.DoesNotContain(table.Columns, c => c.Name.Contains("Venta", StringComparison.OrdinalIgnoreCase) || c.Name.Contains("Precio", StringComparison.OrdinalIgnoreCase));
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

            Assert.Equal("dcccb6270d0642625823ac410a273431368f26d035d21cc28e47fb8301d8af55", _manifestProvider.CreateManifest(contract).ManifestHash);
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
