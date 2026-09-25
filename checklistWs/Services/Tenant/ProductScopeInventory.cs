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

        private static readonly IReadOnlyCollection<string> ProveedoresTables = new[]
        {
            "dbo.ActivosProveedores"
        };

        private static readonly IReadOnlyCollection<string> OrdenesCompraTables = new[]
        {
            "dbo.OrdenesCompra",
            "dbo.OrdenesCompraDetalle",
            "dbo.OrdenesCompraFolios",
            "dbo.OrdenesCompraPresentacionesCompra"
        };

        private static readonly IReadOnlyCollection<string> InventarioTables = new[]
        {
            "dbo.InventarioSaldos",
            "dbo.InventarioMovimientos",
            "dbo.InventarioSeries"
        };

        private static readonly IReadOnlyCollection<string> RecepcionTables = new[]
        {
            "dbo.RecepcionFolios",
            "dbo.Recepciones",
            "dbo.RecepcionPartidas",
            "dbo.RecepcionSeries"
        };

        private static readonly IReadOnlyCollection<string> CurvasTables = new[]
        {
            "dbo.CurvasCatalogo",
            "dbo.CurvasDetalle",
            "dbo.CurvasSiembra",
            "dbo.CurvasOperacionesCompra",
            "dbo.CurvasOperacionOrdenesCompra",
            "dbo.CurvasSugerenciasSnapshot"
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

            if (string.Equals(scope, DatabaseScopes.Proveedores, StringComparison.OrdinalIgnoreCase))
            {
                return ProveedoresTables;
            }

            if (string.Equals(scope, DatabaseScopes.OrdenesCompra, StringComparison.OrdinalIgnoreCase))
            {
                return OrdenesCompraTables;
            }

            if (string.Equals(scope, DatabaseScopes.Inventario, StringComparison.OrdinalIgnoreCase))
            {
                return InventarioTables;
            }

            if (string.Equals(scope, DatabaseScopes.Recepcion, StringComparison.OrdinalIgnoreCase))
            {
                return RecepcionTables;
            }

            if (string.Equals(scope, DatabaseScopes.Curvas, StringComparison.OrdinalIgnoreCase))
            {
                return CurvasTables;
            }

            return Array.Empty<string>();
        }
    }
}
