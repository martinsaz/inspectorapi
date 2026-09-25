using checklistWs.Services.Tenant;
using Xunit;

namespace checklistWs.Tests.Services.Tenant
{
    public sealed class CurvasSchemaContractTests
    {
        private readonly ProductosServiciosSchemaContractProvider _provider = new();
        private readonly SchemaManifestProvider _manifestProvider = new();

        [Fact]
        public void CurvasScope_IsKnownAndVersioned()
        {
            var known = new KnownSchemaVersionProvider();

            Assert.Equal(ProductosServiciosSchemaContractProvider.CurvasLatestVersion, known.GetKnownCurrentVersion(DatabaseScopes.Curvas));
            Assert.Equal(1, _provider.GetContract(DatabaseScopes.Curvas, 1).ContractVersion);
            Assert.Throws<InvalidOperationException>(() => _provider.GetContract(DatabaseScopes.Curvas, 2));
        }

        [Fact]
        public void CurvasInventory_MatchesContractTables()
        {
            SchemaContract contract = Contract();
            IReadOnlyCollection<string> expected = new ProductScopeInventory().GetExpectedTables(DatabaseScopes.Curvas);

            Assert.Equal(new[]
            {
                "dbo.CurvasCatalogo",
                "dbo.CurvasDetalle",
                "dbo.CurvasOperacionOrdenesCompra",
                "dbo.CurvasOperacionesCompra",
                "dbo.CurvasSiembra",
                "dbo.CurvasSugerenciasSnapshot"
            }.OrderBy(x => x), expected.OrderBy(x => x));
            Assert.Equal(expected.OrderBy(x => x), contract.Tables.Select(t => t.FullName).OrderBy(x => x));
        }

        [Fact]
        public void CatalogoDetalleAndSiembra_AreProductVariantScopedAndRejectServicesByContract()
        {
            SchemaTableContract catalogo = Table("CurvasCatalogo");
            SchemaTableContract detalle = Table("CurvasDetalle");
            SchemaTableContract siembra = Table("CurvasSiembra");

            Assert.Contains(catalogo.UniqueConstraints, ux => ux.Name == "UX_CurvasCatalogo_Empresa_Nombre_Activo");
            Assert.Contains(detalle.Columns, c => c.Name == "idProductoServicio" && !c.IsNullable);
            Assert.Contains(detalle.Columns, c => c.Name == "idVariante" && c.IsNullable);
            Assert.Contains(detalle.Columns, c => c.Name == "TipoProductoServicio" && c.DefaultDefinition == "((1))");
            Assert.Contains(detalle.CheckConstraints, ck => ck.Name == "CK_CurvasDetalle_TipoProducto" && ck.Definition.Contains("TipoProductoServicio = 1", StringComparison.Ordinal));
            Assert.Contains(detalle.ForeignKeys, fk => fk.Name == "FK_CurvasDetalle_Variantes_EmpresaId");
            Assert.Contains(siembra.UniqueConstraints, ux => ux.Name == "UX_CurvasSiembra_Empresa_Sucursal_Producto_Variante_Vigente");
            Assert.Contains(siembra.ForeignKeys, fk => fk.Name == "FK_CurvasSiembra_Sucursales_EmpresaId");
        }

        [Fact]
        public void MultisucursalOperation_RelatesGroupingOperationToIndependentChildOrders()
        {
            SchemaTableContract operation = Table("CurvasOperacionesCompra");
            SchemaTableContract relation = Table("CurvasOperacionOrdenesCompra");

            Assert.Contains(operation.Columns, c => c.Name == "OperationKey" && c.SqlType == "NVARCHAR(120)");
            Assert.Contains(operation.UniqueConstraints, ux => ux.Name == "UX_CurvasOperacionesCompra_Empresa_OperationKey");
            Assert.Contains(operation.Columns, c => c.Name == "SucursalCount");
            Assert.Contains(operation.Columns, c => c.Name == "OrdenCompraCount");

            Assert.Contains(relation.ForeignKeys, fk => fk.Name == "FK_CurvasOperacionOC_Operacion_EmpresaId");
            Assert.Contains(relation.ForeignKeys, fk => fk.Name == "FK_CurvasOperacionOC_OrdenesCompra_EmpresaId");
            Assert.Contains(relation.UniqueConstraints, ux => ux.Name == "UX_CurvasOperacionOC_Empresa_OC");
            Assert.Contains(relation.Columns, c => c.Name == "idSucursal" && !c.IsNullable);
        }

        [Fact]
        public void Snapshot_PreservesSuggestionMathModesAndPresentationCompraDecision()
        {
            SchemaTableContract snapshot = Table("CurvasSugerenciasSnapshot");
            string[] expectedColumns =
            {
                "Modo",
                "CurvaObjetivoBase",
                "ExistenciaSnapshotBase",
                "TransitoSnapshotBase",
                "CoberturaSnapshotBase",
                "HuecoSnapshotBase",
                "CopeteSnapshotBase",
                "CantidadPropuestaBase",
                "CantidadFinalBase",
                "OverrideManual",
                "NoPedir",
                "idPresentacionCompra",
                "PermiteCantidadBaseSnapshot"
            };

            foreach (string column in expectedColumns)
            {
                Assert.Contains(snapshot.Columns, c => c.Name == column);
            }

            Assert.Contains(snapshot.CheckConstraints, ck => ck.Name == "CK_CurvasSnapshot_Modo" && ck.Definition.Contains("Modo IN (1, 2, 3, 4)", StringComparison.Ordinal));
            Assert.Contains(snapshot.CheckConstraints, ck => ck.Name == "CK_CurvasSnapshot_Cobertura" && ck.Definition.Contains("ExistenciaSnapshotBase + TransitoSnapshotBase", StringComparison.Ordinal));
            Assert.Contains(snapshot.CheckConstraints, ck => ck.Name == "CK_CurvasSnapshot_NoPedir");
            Assert.Contains(snapshot.ForeignKeys, fk => fk.Name == "FK_CurvasSnapshot_PresentacionCompra_EmpresaId");
        }

        [Fact]
        public void AllTables_AreMultitenantAndIndexedByEmpresa()
        {
            foreach (SchemaTableContract table in Contract().Tables)
            {
                Assert.Contains(table.Columns, c => c.Name == "idEmpresa" && !c.IsNullable);
                Assert.All(table.ForeignKeys, fk => Assert.Equal("idEmpresa", fk.Columns.First()));
                Assert.All(table.UniqueConstraints, ux => Assert.Equal("idEmpresa", ux.Columns.First()));
                Assert.Contains(table.Indexes, index => index.KeyColumns.Any(column => column.Name == "idEmpresa"));
            }
        }

        [Fact]
        public void ManifestHash_IsStableForCurvasV1Contract()
        {
            Assert.Equal("85167e40a617c4535514563c03cfd3c49ec5c0f915f122b0d0c533779e88d4f5", _manifestProvider.CreateManifest(Contract()).ManifestHash);
        }

        private SchemaContract Contract()
        {
            return _provider.GetContract(DatabaseScopes.Curvas);
        }

        private SchemaTableContract Table(string name)
        {
            return Contract().Tables.Single(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
