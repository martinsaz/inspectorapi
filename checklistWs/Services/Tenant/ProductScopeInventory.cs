namespace checklistWs.Services.Tenant
{
    public sealed class ProductScopeInventory : IProductScopeInventory
    {
        private static readonly IReadOnlyCollection<string> ProductosServiciosTables = new[]
        {
            "dbo.ProductosServicios",
            "dbo.ProductosServiciosCategorias",
            "dbo.ProductosServiciosMarcas",
            "dbo.ProductosServiciosColecciones",
            "dbo.ProductosServiciosUnidadesMedida",
            "dbo.ProductosServiciosPaquetes",
            "dbo.ProductosServiciosTags",
            "dbo.ProductosServiciosProductoTags",
            "dbo.ProductosServiciosAtributos",
            "dbo.ProductosServiciosAtributosValores",
            "dbo.ProductosServiciosProductoAtributos",
            "dbo.ProductosServiciosProductoAtributoValores",
            "dbo.ProductosServiciosOpcionesVariante",
            "dbo.ProductosServiciosOpcionesVarianteValores",
            "dbo.ProductosServiciosVariantes",
            "dbo.ProductosServiciosVarianteValores",
            "dbo.ProductosServiciosMultimedia",
            "dbo.ProductosServiciosExistencias",
            "dbo.ProductosServiciosMovimientosInventario",
            "dbo.ProductosServiciosPresentacionesVenta"
        };

        private static readonly IReadOnlyCollection<string> SucursalesTables = new[]
        {
            "dbo.RazonesSociales",
            "dbo.Zonas",
            "dbo.Sucursales"
        };

        public IReadOnlyCollection<string> GetExpectedTables(string scope)
        {
            if (string.Equals(scope, DatabaseScopes.ProductosServicios, StringComparison.OrdinalIgnoreCase))
            {
                return ProductosServiciosTables;
            }

            if (string.Equals(scope, DatabaseScopes.Sucursales, StringComparison.OrdinalIgnoreCase))
            {
                return SucursalesTables;
            }

            return Array.Empty<string>();
        }
    }
}
