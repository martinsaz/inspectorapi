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
            "dbo.ProductosServiciosPresentacionesVenta",
            "dbo.ProductosServiciosIdentidadComercial",
            "dbo.ProductosServiciosIdentidadComercialHistorial"
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

        private static readonly IReadOnlyCollection<string> ListaPreciosTables = new[]
        {
            "dbo.ListaPreciosListas",
            "dbo.ListaPreciosDetalle",
            "dbo.ListaPreciosPromociones",
            "dbo.ListaPreciosHistorial"
        };

        private static readonly IReadOnlyCollection<string> CotizacionesTables = new[]
        {
            "dbo.Cotizaciones",
            "dbo.CotizacionesPartidas",
            "dbo.CotizacionesHistorial"
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

            if (string.Equals(scope, DatabaseScopes.ListaPrecios, StringComparison.OrdinalIgnoreCase))
            {
                return ListaPreciosTables;
            }

            if (string.Equals(scope, DatabaseScopes.Cotizaciones, StringComparison.OrdinalIgnoreCase))
            {
                return CotizacionesTables;
            }

            return Array.Empty<string>();
        }
    }
}
