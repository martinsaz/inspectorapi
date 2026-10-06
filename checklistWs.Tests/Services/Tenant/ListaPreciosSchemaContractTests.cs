using checklistWs.Services.Tenant;
using Xunit;

namespace checklistWs.Tests.Services.Tenant
{
    public sealed class ListaPreciosSchemaContractTests
    {
        private readonly ProductosServiciosSchemaContractProvider _provider = new();
        private readonly SchemaManifestProvider _manifestProvider = new();

        [Fact]
        public void ListaPreciosScope_IsKnownAndVersioned()
        {
            var known = new KnownSchemaVersionProvider();

            Assert.Equal(ProductosServiciosSchemaContractProvider.ListaPreciosLatestVersion, known.GetKnownCurrentVersion(DatabaseScopes.ListaPrecios));
            Assert.Equal(1, _provider.GetContract(DatabaseScopes.ListaPrecios, 1).ContractVersion);
            Assert.Equal(2, _provider.GetContract(DatabaseScopes.ListaPrecios, 2).ContractVersion);
            Assert.Throws<InvalidOperationException>(() => _provider.GetContract(DatabaseScopes.ListaPrecios, 3));
        }

        [Fact]
        public void ListaPreciosInventory_MatchesContractTables()
        {
            SchemaContract contract = Contract();
            IReadOnlyCollection<string> expected = new ProductScopeInventory().GetExpectedTables(DatabaseScopes.ListaPrecios);

            Assert.Equal(new[]
            {
                "dbo.ListaPreciosDetalle",
                "dbo.ListaPreciosHistorial",
                "dbo.ListaPreciosListas",
                "dbo.ListaPreciosPromociones",
            }.OrderBy(x => x), expected.OrderBy(x => x));
            Assert.Equal(expected.OrderBy(x => x), contract.Tables.Select(t => t.FullName).OrderBy(x => x));
        }

        [Fact]
        public void Listas_DefineStableTenantLevelIdentity()
        {
            SchemaTableContract listas = Table("ListaPreciosListas");

            Assert.Contains(listas.Columns, c => c.Name == "Nivel" && c.SqlType == "TINYINT" && !c.IsNullable);
            Assert.Contains(listas.Columns, c => c.Name == "EsDefault" && c.SqlType == "BIT" && c.DefaultDefinition == "((0))");
            Assert.Contains(listas.UniqueConstraints, ux => ux.Name == "UX_ListaPreciosListas_Empresa_Nivel" && ux.Columns.SequenceEqual(new[] { "idEmpresa", "Nivel" }));
            Assert.Contains(listas.UniqueConstraints, ux => ux.Name == "UX_ListaPreciosListas_Empresa_Default_Activo" && ux.FilterDefinition == "EsDefault = 1 AND Activo = 1 AND FechaArchivado IS NULL");
            Assert.Contains(listas.CheckConstraints, ck => ck.Name == "CK_ListaPreciosListas_Nivel" && ck.Definition.Contains("Nivel BETWEEN 1 AND 10", StringComparison.Ordinal));
        }

        [Fact]
        public void Detalle_SupportsProductServiceVariantAndPresentationWithoutVariantPresentationCombo()
        {
            SchemaTableContract detalle = Table("ListaPreciosDetalle");

            Assert.Contains(detalle.Columns, c => c.Name == "TipoIdentidad" && !c.IsNullable);
            Assert.Contains(detalle.Columns, c => c.Name == "idProductoServicio" && !c.IsNullable);
            Assert.Contains(detalle.Columns, c => c.Name == "idVariante" && c.IsNullable);
            Assert.Contains(detalle.Columns, c => c.Name == "idPresentacionVenta" && c.IsNullable);
            Assert.Contains(detalle.CheckConstraints, ck => ck.Name == "CK_ListaPreciosDetalle_TipoIdentidad" && ck.Definition.Contains("TipoIdentidad IN (1, 2, 3, 4)", StringComparison.Ordinal));
            Assert.Contains(detalle.CheckConstraints, ck => ck.Name == "CK_ListaPreciosDetalle_Identidad" &&
                ck.Definition.Contains("TipoIdentidad = 3", StringComparison.Ordinal) &&
                ck.Definition.Contains("idVariante IS NOT NULL AND idPresentacionVenta IS NULL", StringComparison.Ordinal) &&
                ck.Definition.Contains("TipoIdentidad = 4", StringComparison.Ordinal) &&
                ck.Definition.Contains("idVariante IS NULL AND idPresentacionVenta IS NOT NULL", StringComparison.Ordinal));
        }

        [Fact]
        public void Detalle_StoresZeroAsConfiguredPriceAndUsesAbsenceAsNoConfig()
        {
            SchemaTableContract detalle = Table("ListaPreciosDetalle");

            Assert.Contains(detalle.Columns, c => c.Name == "Precio" && c.SqlType == "DECIMAL(18,2)" && !c.IsNullable && c.Precision == 18 && c.Scale == 2);
            Assert.Contains(detalle.CheckConstraints, ck => ck.Name == "CK_ListaPreciosDetalle_Precio" && ck.Definition.Contains("Precio >= 0", StringComparison.Ordinal));
            Assert.DoesNotContain(detalle.Columns, c => c.Name.Contains("PrecioFinal", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(detalle.Columns, c => c.Name.Contains("Fallback", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void DetalleV2_StoresDiscountRoundingAndValidityWithoutFinalPriceSource()
        {
            SchemaTableContract detalle = Table("ListaPreciosDetalle");

            Assert.Contains(detalle.Columns, c => c.Name == "DescuentoPct" && c.SqlType == "DECIMAL(5,2)" && c.IsNullable && c.Precision == 5 && c.Scale == 2);
            Assert.Contains(detalle.Columns, c => c.Name == "RedondeoModo" && c.SqlType == "TINYINT" && !c.IsNullable && c.DefaultDefinition == "((0))");
            Assert.Contains(detalle.Columns, c => c.Name == "VigenciaInicio" && c.SqlType == "DATE" && c.IsNullable);
            Assert.Contains(detalle.Columns, c => c.Name == "VigenciaFin" && c.SqlType == "DATE" && c.IsNullable);
            Assert.Contains(detalle.CheckConstraints, ck => ck.Name == "CK_ListaPreciosDetalle_DescuentoPct" && ck.Definition.Contains("DescuentoPct >= 0", StringComparison.Ordinal) && ck.Definition.Contains("DescuentoPct <= 100", StringComparison.Ordinal));
            Assert.Contains(detalle.CheckConstraints, ck => ck.Name == "CK_ListaPreciosDetalle_RedondeoModo" && ck.Definition.Contains("RedondeoModo IN (0, 1, 2)", StringComparison.Ordinal));
            Assert.Contains(detalle.CheckConstraints, ck => ck.Name == "CK_ListaPreciosDetalle_Vigencia" && ck.Definition.Contains("VigenciaInicio <= VigenciaFin", StringComparison.Ordinal));
            Assert.DoesNotContain(detalle.Columns, c => c.Name.Contains("PrecioFinal", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void PromocionesV2_PreparesOnlyClosedStructuralPromotions()
        {
            SchemaTableContract promociones = Table("ListaPreciosPromociones");

            Assert.Contains(promociones.CheckConstraints, ck => ck.Name == "CK_ListaPreciosPromociones_TipoPromocion" && ck.Definition.Contains("TipoPromocion IN (1, 2, 3)", StringComparison.Ordinal));
            Assert.Contains(promociones.Columns, c => c.Name == "DescuentoSegundoPct" && c.SqlType == "DECIMAL(5,2)" && c.IsNullable);
            Assert.Contains(promociones.CheckConstraints, ck => ck.Name == "CK_ListaPreciosPromociones_DescuentoSegundo" && ck.Definition.Contains("DescuentoSegundoPct >= 0", StringComparison.Ordinal) && ck.Definition.Contains("DescuentoSegundoPct <= 100", StringComparison.Ordinal));
            Assert.DoesNotContain(promociones.Columns, c => c.Name.Contains("Monedero", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(promociones.Columns, c => c.Name.Contains("Prioridad", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(promociones.Columns, c => c.Name.Contains("Acumul", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(promociones.Columns, c => c.Name.Contains("Sucursal", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void HistorialV2_CapturesMinimumAuditReadyFields()
        {
            SchemaTableContract historial = Table("ListaPreciosHistorial");

            foreach (string columnName in new[] { "idEmpresa", "TipoIdentidad", "idProductoServicio", "Campo", "Operacion", "ValorAnterior", "ValorNuevo", "idUsuario", "Usuario", "Origen", "CorrelationId", "Motivo", "FechaUtc" })
            {
                Assert.Contains(historial.Columns, c => c.Name == columnName);
            }

            Assert.Contains(historial.CheckConstraints, ck => ck.Name == "CK_ListaPreciosHistorial_Origen" && ck.Definition.Contains("INDIVIDUAL", StringComparison.Ordinal) && ck.Definition.Contains("PROMOCION", StringComparison.Ordinal));
        }

        [Fact]
        public void Detalle_IsTenantSafeAndPreventsActiveDuplicateVendibleIdentity()
        {
            SchemaTableContract detalle = Table("ListaPreciosDetalle");

            Assert.Contains(detalle.ForeignKeys, fk => fk.Name == "FK_ListaPreciosDetalle_Listas_EmpresaId" && fk.Columns.SequenceEqual(new[] { "idEmpresa", "idListaPrecio" }));
            Assert.Contains(detalle.ForeignKeys, fk => fk.Name == "FK_ListaPreciosDetalle_ProductosServicios_EmpresaId" && fk.Columns.SequenceEqual(new[] { "idEmpresa", "idProductoServicio" }));
            Assert.Contains(detalle.ForeignKeys, fk => fk.Name == "FK_ListaPreciosDetalle_Variantes_EmpresaId" && fk.Columns.SequenceEqual(new[] { "idEmpresa", "idVariante" }));
            Assert.Contains(detalle.ForeignKeys, fk => fk.Name == "FK_ListaPreciosDetalle_PresentacionesVenta_EmpresaId" && fk.Columns.SequenceEqual(new[] { "idEmpresa", "idPresentacionVenta" }));
            Assert.Contains(detalle.UniqueConstraints, ux => ux.Name == "UX_ListaPreciosDetalle_Empresa_Lista_Identidad_Activo" && ux.FilterDefinition == "Activo = 1 AND FechaArchivado IS NULL");
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
        public void ManifestHash_IsStableForListaPreciosV1Contract()
        {
            Assert.Equal("d4f0bbdc05f56c96026d6364f9c333799ccb0ddc9acdf8993fe040b92b48ce7e", _manifestProvider.CreateManifest(Contract(1)).ManifestHash);
        }

        [Fact]
        public void ManifestHash_IsStableForListaPreciosV2Contract()
        {
            Assert.Equal("e7a388ec985a19fb2b3beb73e8c2cf28d5dda17d3f3bb2f0363683c166092882", _manifestProvider.CreateManifest(Contract(2)).ManifestHash);
        }

        private SchemaContract Contract(int? version = null)
        {
            return _provider.GetContract(DatabaseScopes.ListaPrecios, version);
        }

        private SchemaTableContract Table(string name)
        {
            return Contract().Tables.Single(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
