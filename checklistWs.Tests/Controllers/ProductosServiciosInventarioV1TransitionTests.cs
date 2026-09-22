using Xunit;

namespace checklistWs.Tests.Controllers
{
    public sealed class ProductosServiciosInventarioV1TransitionTests
    {
        private static readonly string ControllerPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "checklistWs",
            "Controllers",
            "ProductosServicios",
            "ProductosServiciosController.cs"));

        [Fact]
        public void ProductosServiciosController_DoesNotUseLegacyInventoryTables()
        {
            string source = File.ReadAllText(ControllerPath);

            Assert.DoesNotContain("ProductosServiciosExistencias", source);
            Assert.DoesNotContain("ProductosServiciosMovimientosInventario", source);
        }

        [Fact]
        public void ProductosServiciosController_ReadsInventoryV1AndBlocksLegacyMutations()
        {
            string source = File.ReadAllText(ControllerPath);

            Assert.Contains("dbo.InventarioSaldos", source);
            Assert.Contains("dbo.InventarioMovimientos", source);
            Assert.Contains("Inventario V1 requiere sucursal, origen e idempotencia", source);
        }
    }
}
