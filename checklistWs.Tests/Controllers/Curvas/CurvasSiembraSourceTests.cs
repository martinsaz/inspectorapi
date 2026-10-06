using Xunit;

namespace checklistWs.Tests.Controllers.Curvas;

public sealed class CurvasSiembraSourceTests
{
    private static readonly string RepoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
    private static readonly string ApiRoot = Path.Combine(RepoRoot, "inspectorapi", "checklistWs");
    private static readonly string MvcRoot = Path.Combine(RepoRoot, "inspector", "checklist");

    [Fact]
    public void ApiUsesExistingServiceCurvasGateAndDedicatedPermission()
    {
        string controller = File.ReadAllText(Path.Combine(ApiRoot, "Controllers", "Curvas", "CurvasSiembraController.cs"));
        string defaults = File.ReadAllText(Path.Combine(ApiRoot, "Services", "Tenant", "ProductosServiciosAuthorizationModels.cs"));

        Assert.Contains("CurvasSiembraPermissionCode = \"05005002\"", defaults);
        Assert.Contains("ProductosServiciosAuthorizationDefaults.CurvasSiembraPermissionCode", controller);
        Assert.Contains("DatabaseScopes.Curvas", controller);
        Assert.Contains("ProductosServiciosPermissionRequirement.Read", controller);
        Assert.Contains("ProductosServiciosPermissionRequirement.Write", controller);
        Assert.Contains("_curvasService.SembrarAsync", controller);
        Assert.Contains("ResolveDescriptorAsync(Guid.Empty, string.Empty)", controller);
        Assert.Contains("ProductosServiciosIdentityValidation.TryValidate", controller);
        Assert.Contains("_configuration[\"fireBdata:fireClave\"]", controller);
        Assert.DoesNotContain("Request.Headers[ProxyEmpresaIdHeader]", controller);
        Assert.DoesNotContain("Sucursales(Guid idEmpresa", controller);
        Assert.DoesNotContain("CurvasActivas(Guid idEmpresa", controller);
    }

    [Fact]
    public void ApiExposesOnlyOperationalSeedReadsAndWrite()
    {
        string controller = File.ReadAllText(Path.Combine(ApiRoot, "Controllers", "Curvas", "CurvasSiembraController.cs"));

        Assert.Contains("[HttpGet(\"Sucursales\")]", controller);
        Assert.Contains("[HttpGet(\"CurvasActivas\")]", controller);
        Assert.Contains("[HttpGet(\"DetalleCurva\")]", controller);
        Assert.Contains("[HttpGet(\"Vigentes\")]", controller);
        Assert.Contains("[HttpPost(\"Sembrar\")]", controller);
        Assert.Contains("[HttpPost(\"Cerrar\")]", controller);
        Assert.Contains("_curvasService.CerrarSiembraAsync", controller);
        Assert.Contains("curva.Detalles", controller);
        Assert.DoesNotContain("CurvasSugerenciasMotorService", controller);
    }

    [Fact]
    public void ExistingSeedServiceClosesCurrentRowAndPreservesTenantAudit()
    {
        string service = File.ReadAllText(Path.Combine(ApiRoot, "Services", "Tenant", "CurvasScopeService.cs"));
        string controller = File.ReadAllText(Path.Combine(ApiRoot, "Controllers", "Curvas", "CurvasSiembraController.cs"));

        Assert.Contains("EnsureSucursalAsync(connection, transaction, idEmpresa", service);
        Assert.Contains("EnsureCurvaActivaAsync(connection, transaction, idEmpresa", service);
        Assert.Contains("EnsureProductoAsync(connection, transaction, idEmpresa, request.IdProductoServicio, requireProducto: true", service);
        Assert.Contains("EnsureVarianteAsync(connection, transaction, idEmpresa, request.IdProductoServicio", service);
        Assert.Contains("EnsureCurvaDetalleAsync(connection, transaction, idEmpresa, request.IdCurva, request.IdProductoServicio", service);
        Assert.Contains("CURVA_DETALLE_NO_DISPONIBLE", service);
        Assert.Contains("AND ISNULL(borrado,0)=0", service);
        Assert.Contains("UPDATE dbo.CurvasSiembra", service);
        Assert.Contains("SET Estado = 3, FechaVigenciaFin = SYSUTCDATETIME()", service);
        Assert.Contains("AND Estado = 1", service);
        Assert.Contains("AND FechaVigenciaFin IS NULL", service);
        Assert.Contains("INSERT INTO dbo.CurvasSiembra", service);
        Assert.Contains("idUsuarioCreacion", service);
        Assert.Contains("idUsuarioActualizacion", service);
        Assert.Contains("Task<bool> CerrarSiembraAsync", service);
        Assert.Contains("WITH (UPDLOCK, HOLDLOCK)", service);
        Assert.Contains("AND id = @IdSiembra", service);
        Assert.Contains("EnsureSucursalAsync(connection, transaction, idEmpresa, idSucursal.Value", service);
        Assert.Contains("La siembra ya no está vigente", controller);
    }

    [Fact]
    public void MvcSeparatesCatalogFromSeedAndProtectsReadAndWrite()
    {
        string controller = File.ReadAllText(Path.Combine(MvcRoot, "Controllers", "Curvas", "CurvasController.cs"));
        string view = File.ReadAllText(Path.Combine(MvcRoot, "Views", "Curvas", "Siembra.cshtml"));
        string script = File.ReadAllText(Path.Combine(MvcRoot, "wwwroot", "js", "Curvas", "CurvasSiembra.js"));

        Assert.Contains("[HttpGet(\"Siembra\")]", controller);
        Assert.Contains("CurvasSiembraPermissionCode = \"05005002\"", controller);
        Assert.Contains("requireWrite: false", controller);
        Assert.Contains("requireWrite: true", controller);
        Assert.Contains("data-can-write", view);
        Assert.Contains("SUCURSAL", view.ToUpperInvariant());
        Assert.Contains("CURVA", view.ToUpperInvariant());
        Assert.Contains("PRODUCTO", view.ToUpperInvariant());
        Assert.Contains("VARIANTE", view.ToUpperInvariant());
        Assert.Contains("OBJETIVO", view.ToUpperInvariant());
        Assert.Contains("CheckAppUI.createDynamicGrid", script);
        Assert.Contains("data-curvas-siembra-action='close'", script);
        Assert.Contains("/Proveeduria/Curvas/Siembra/Cerrar", script);
        Assert.Contains("¿Quitar siembra?", script);
        Assert.Contains("conservando su histórico", script);
        Assert.Contains("Swal.fire", script);
        Assert.DoesNotContain("window.alert", script);
        Assert.DoesNotContain("window.confirm", script);
        Assert.DoesNotContain("window.prompt", script);
        Assert.DoesNotContain("<style", view, StringComparison.OrdinalIgnoreCase);
    }
}
