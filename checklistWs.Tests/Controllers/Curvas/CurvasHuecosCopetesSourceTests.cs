using Xunit;

namespace checklistWs.Tests.Controllers.Curvas;

public sealed class CurvasHuecosCopetesSourceTests
{
    private static readonly string RepoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
    private static readonly string ApiRoot = Path.Combine(RepoRoot, "inspectorapi", "checklistWs");
    private static readonly string MvcRoot = Path.Combine(RepoRoot, "inspector", "checklist");

    [Fact]
    public void ApiUsesDedicatedReadPermissionAndExistingMotor()
    {
        string controller = Read(ApiRoot, "Controllers", "Curvas", "CurvasSugerenciasController.cs");
        string defaults = Read(ApiRoot, "Services", "Tenant", "ProductosServiciosAuthorizationModels.cs");
        string service = Read(ApiRoot, "Services", "Tenant", "CurvasSugerenciasMotorService.cs");

        Assert.Contains("CurvasHuecosCopetesPermissionCode = \"05005003\"", defaults);
        Assert.Contains("[HttpPost(\"Analisis\")]", controller);
        Assert.Contains("[HttpGet(\"Analisis/Sucursales\")]", controller);
        Assert.Contains("ProductosServiciosAuthorizationDefaults.CurvasHuecosCopetesPermissionCode", controller);
        Assert.Contains("ProductosServiciosPermissionRequirement.Read", controller);
        Assert.Contains("_motorService.AnalizarAsync", controller);
        Assert.Contains("ResolveDescriptorAsync(Guid.Empty, string.Empty)", controller);
        Assert.Contains("Task<CurvasAnalisisResponse> AnalizarAsync", service);
        Assert.DoesNotContain("CrearSnapshotAsync", controller);
        Assert.DoesNotContain("CurvasSugerenciasSnapshot", service);
    }

    [Fact]
    public void AnalysisIsSetBasedTenantScopedPagedAndExcludesServices()
    {
        string service = Read(ApiRoot, "Services", "Tenant", "CurvasSugerenciasMotorService.cs");

        Assert.Contains("p.idEmpresa = @IdEmpresa", service);
        Assert.Contains("p.Tipo = @TipoProducto", service);
        Assert.Contains("SUM(i.CantidadBaseActual)", service);
        Assert.Contains("SUM(d.CantidadBasePendiente)", service);
        Assert.Contains("OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY", service);
        Assert.Contains("COUNT(1) OVER() AS TotalRegistros", service);
        Assert.Contains("@IdSucursal IS NULL OR s.id = @IdSucursal", service);
        Assert.Contains("@Producto = ''", service);
        Assert.Contains("@Estado = 'TODOS' OR Estado = @Estado", service);
        Assert.DoesNotContain("foreach (CurvasAnalisisItemDto", service);
    }

    [Fact]
    public void MvcIsIndependentReadOnlyResponsiveAndUsesDynamicGrid()
    {
        string controller = Read(MvcRoot, "Controllers", "Curvas", "CurvasController.cs");
        string view = Read(MvcRoot, "Views", "Curvas", "HuecosCopetes.cshtml");
        string script = Read(MvcRoot, "wwwroot", "js", "Curvas", "HuecosCopetes.js");
        string css = Read(MvcRoot, "wwwroot", "css", "Curvas", "HuecosCopetes.css");

        Assert.Contains("[HttpGet(\"/Activos/Curvas/HuecosCopetes\")]", controller);
        Assert.Contains("CurvasHuecosCopetesPermissionCode = \"05005003\"", controller);
        Assert.Contains("<h1>Huecos y Copetes</h1>", view);
        Assert.Contains("no crean órdenes de compra ni snapshots", view);
        Assert.Contains("CheckAppUI.createDynamicGrid", script);
        Assert.Contains("tamanoPagina", script);
        Assert.Contains("HUECO", script);
        Assert.Contains("COPETE", script);
        Assert.Contains("EN_OBJETIVO", script);
        Assert.Contains("SIN_CURVA", script);
        Assert.Contains("@media (max-width: 900px)", css);
        Assert.Contains("@media (max-width: 575.98px)", css);
        Assert.DoesNotContain("Guardar", view);
        Assert.DoesNotContain("CrearSnapshot", script);
    }

    private static string Read(string root, params string[] parts)
        => File.ReadAllText(Path.Combine(new[] { root }.Concat(parts).ToArray()));
}
