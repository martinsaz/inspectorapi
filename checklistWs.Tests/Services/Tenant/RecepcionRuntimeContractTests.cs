using Xunit;

namespace checklistWs.Tests.Services.Tenant
{
    public sealed class RecepcionRuntimeContractTests
    {
        [Fact]
        public void RecepcionRuntime_UsesInventarioV1AndDoesNotReadLegacyInventoryTruth()
        {
            string service = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "../../../../checklistWs/Services/Tenant/RecepcionScopeService.cs"));
            string controller = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "../../../../checklistWs/Controllers/Recepcion/RecepcionController.cs"));

            Assert.Contains("IInventarioScopeLedgerService", service);
            Assert.Contains("ApplyMovementAsync", service);
            Assert.Contains("InventarioSeries", service);
            Assert.DoesNotContain("ProductosServiciosExistencias", service);
            Assert.DoesNotContain("ProductosServiciosMovimientosInventario", service);
            Assert.DoesNotContain("ProductosServiciosExistencias", controller);
            Assert.DoesNotContain("ProductosServiciosMovimientosInventario", controller);
        }

        [Fact]
        public void RecepcionRuntime_DeclaresIdempotenceAndConcurrencyGuards()
        {
            string service = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "../../../../checklistWs/Services/Tenant/RecepcionScopeService.cs"));

            Assert.Contains("FindExistingAsync", service);
            Assert.Contains("OperationKey", service);
            Assert.Contains("IsolationLevel.Serializable", service);
            Assert.Contains("sp_getapplock", service);
            Assert.Contains("oc.idSucursal = @IdSucursal", service);
            Assert.Contains("RECEPCION_SOBRE_RECEPCION_NO_PERMITIDA", service);
            Assert.Contains("RECEPCION_SERIES_DUPLICADAS", service);
        }

        [Fact]
        public void RecepcionController_RequiresReceptionPermissionsAndCompatibilityGate()
        {
            string controller = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "../../../../checklistWs/Controllers/Recepcion/RecepcionController.cs"));

            Assert.Contains("RecepcionNuevaPermissionCode", controller);
            Assert.Contains("RecepcionReportePermissionCode", controller);
            Assert.Contains("EvaluateAsync", controller);
            Assert.Contains("DatabaseScopes.Recepcion", controller);
            Assert.Contains("DatabaseScopes.OrdenesCompra", controller);
            Assert.Contains("DatabaseScopes.Inventario", controller);
        }
    }
}
