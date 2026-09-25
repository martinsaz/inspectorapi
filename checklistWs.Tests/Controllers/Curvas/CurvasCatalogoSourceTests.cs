using Xunit;

namespace checklistWs.Tests.Controllers.Curvas;

public sealed class CurvasCatalogoSourceTests
{
    private static readonly string RepoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
    private static readonly string ApiRoot = Path.Combine(RepoRoot, "inspectorapi", "checklistWs");
    private static readonly string MvcRoot = Path.Combine(RepoRoot, "inspector", "checklist");

    [Fact]
    public void ApiControllerUsesCurvasGateAndCatalogoPermission()
    {
        string source = File.ReadAllText(Path.Combine(ApiRoot, "Controllers", "Curvas", "CurvasCatalogoController.cs"));

        Assert.Contains("CurvasCatalogoPermissionCode", source);
        Assert.Contains("DatabaseScopes.Curvas", source);
        Assert.Contains("ProductosServiciosPermissionRequirement.Read", source);
        Assert.Contains("ProductosServiciosPermissionRequirement.Write", source);
        Assert.Contains("ResolveDescriptorAsync(Guid.Empty, string.Empty)", source);
    }

    [Fact]
    public void CatalogoServiceRejectsServicesAndPersistsLogicalLifecycle()
    {
        string source = File.ReadAllText(Path.Combine(ApiRoot, "Services", "Tenant", "CurvasScopeService.cs"));

        Assert.Contains("requireProducto: true", source);
        Assert.Contains("CURVA_SERVICIO_RECHAZADO", source);
        Assert.Contains("ps.Tipo = 1", source);
        Assert.Contains("TipoProductoServicio, CantidadBaseObjetivo", source);
        Assert.Contains("CURVA_DETALLE_DUPLICADO", source);
        Assert.Contains("FechaArchivado=SYSUTCDATETIME()", source);
        Assert.Contains("FechaArchivado=NULL", source);
    }

    [Fact]
    public void CatalogoServiceOwnsGeneratedCodeAndActiveState()
    {
        string source = File.ReadAllText(Path.Combine(ApiRoot, "Services", "Tenant", "CurvasScopeService.cs"));

        Assert.Contains("GenerateCatalogoCodeAsync", source);
        Assert.Contains("SELECT Codigo FROM dbo.CurvasCatalogo WHERE idEmpresa=@IdEmpresa AND id=@Id", source);
        Assert.Contains("request.Activo", source);
        Assert.Contains("Estado=1, Activo=1", source);
        Assert.Contains("Estado=2, Activo=0", source);
        Assert.Contains("FechaArchivado=COALESCE(FechaArchivado, SYSUTCDATETIME())", source);
        Assert.Contains("string codigo = exists && !string.IsNullOrWhiteSpace(existingCodigo) ? existingCodigo : await GenerateCatalogoCodeAsync", source);
    }

    [Fact]
    public void MvcCatalogoUsesGoldenMasterDynamicGridWithoutParallelCss()
    {
        string view = File.ReadAllText(Path.Combine(MvcRoot, "Views", "Curvas", "Catalogo.cshtml"));
        string script = File.ReadAllText(Path.Combine(MvcRoot, "wwwroot", "js", "Curvas", "CurvasCatalogo.js"));

        Assert.Contains("ProductosServicios.css", view);
        Assert.Contains("CheckAppUI.createDynamicGrid", script);
        Assert.Contains("ps-catalog-actions", script);
        Assert.Contains("cbFiltroEstatusCurvas", view);
        Assert.Contains("ckCurvaActiva", view);
        Assert.Contains("btAplicarCantidadTodasCurva", view);
        Assert.Contains("txResumenObjetivosCurva", view);
        Assert.Contains("tbObjetivosCurva", view);
        Assert.Contains("tbDetalleCurva", view);
        Assert.Contains("variantRows", script);
        Assert.Contains("activo:", script);
        Assert.DoesNotContain("txCurvaCodigo", view);
        Assert.DoesNotContain("txCurvaCodigo", script);
        Assert.DoesNotContain("<style", view, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MvcGuardResolvesRoleWithCertifiedProductosServiciosPattern()
    {
        string source = File.ReadAllText(Path.Combine(MvcRoot, "Controllers", "Curvas", "CurvasController.cs"));

        Assert.Contains("api/Usuario/ObtenerUsuarioPorEmail", source);
        Assert.Contains("api/Usuario/ObtenerUsuario", source);
        Assert.Contains("FindPermission(JsonNode.Parse(permisos), CurvasCatalogoPermissionCode)", source);
        Assert.Contains("No fue posible resolver", File.ReadAllText(Path.Combine(ApiRoot, "Controllers", "Curvas", "CurvasCatalogoController.cs")));
    }
}
