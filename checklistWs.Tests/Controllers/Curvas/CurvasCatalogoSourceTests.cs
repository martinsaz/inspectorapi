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
        Assert.Contains("CURVA_ELIMINAR_CON_ASIGNACIONES", source);
        Assert.Contains("CURVA_ELIMINADA_NO_REACTIVABLE", source);
        Assert.Contains("Task EliminarCatalogoAsync", source);
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
    public void CatalogoSummaryAndListUseRealTenantScopedCurvesDetailsAndSeeds()
    {
        string controller = File.ReadAllText(Path.Combine(ApiRoot, "Controllers", "Curvas", "CurvasCatalogoController.cs"));
        string service = File.ReadAllText(Path.Combine(ApiRoot, "Services", "Tenant", "CurvasScopeService.cs"));
        string models = File.ReadAllText(Path.Combine(ApiRoot, "Services", "Tenant", "CurvasScopeModels.cs"));

        Assert.Contains("HttpGet(\"Resumen\")", controller);
        Assert.Contains("ObtenerResumenCatalogoAsync", controller);
        Assert.Contains("CurvasCatalogoResumenDto", models);
        Assert.Contains("COUNT(DISTINCT idProductoServicio)", service);
        Assert.Contains("COUNT(DISTINCT idSucursal)", service);
        Assert.Contains("s.Estado = 1 AND s.FechaVigenciaFin IS NULL", service);
        Assert.Contains("d.Activo = 1 AND d.FechaArchivado IS NULL", service);
        Assert.Contains("FuenteActiva", service);
        Assert.Contains("CurvasCatalogoSucursalDto", models);
        Assert.Contains("CurvasCatalogoAsignacionDto", models);
        Assert.Contains("CodigoProducto", models);
        Assert.Contains("FechaVigenciaInicio", models);
    }

    [Fact]
    public void MvcCatalogoUsesGoldenMasterAndSingleCanonicalObjectivesTable()
    {
        string view = File.ReadAllText(Path.Combine(MvcRoot, "Views", "Curvas", "Catalogo.cshtml"));
        string script = File.ReadAllText(Path.Combine(MvcRoot, "wwwroot", "js", "Curvas", "CurvasCatalogo.js"));

        Assert.Contains("ProductosServicios.css", view);
        Assert.Contains("CheckAppUI.createDynamicGrid", script);
        Assert.Contains("ps-catalog-actions", script);
        Assert.Contains("cbFiltroEstatusCurvas", view);
        Assert.Contains("ckCurvaActiva", view);
        Assert.Contains("btAplicarCantidadTodasCurva", view);
        Assert.Contains("placeholder=\"Cantidad para todas\"", view);
        Assert.Contains("aria-label=\"Cantidad para todas\"", view);
        Assert.DoesNotContain("<span>Cantidad para todas</span>", view);
        Assert.Contains("txResumenObjetivosCurva", view);
        Assert.Contains("txKpiCurvasActivas", view);
        Assert.Contains("txKpiSucursales", view);
        Assert.Contains("role=\"switch\"", view);
        Assert.Contains("modalCurvaConsulta", view);
        Assert.Contains("tbObjetivosCurva", view);
        Assert.DoesNotContain("tbDetalleCurva", view);
        Assert.DoesNotContain("panelDetalleCurva", view);
        Assert.DoesNotContain("btLimpiarProductoCurva", view);
        Assert.DoesNotContain("Producto seleccionado", view);
        Assert.DoesNotContain("modal-xl", view);
        Assert.Contains("Selecciona un producto para cargar sus objetivos.", view);
        Assert.Contains("variantRows", script);
        Assert.DoesNotContain("state.details", script);
        Assert.Contains("curvasCatalogoIdentidades", script);
        Assert.Contains("curvasCatalogoProductos", script);
        Assert.Contains("curvasCatalogoEliminar", script);
        Assert.Contains("curvas-assignment-search", script);
        Assert.Contains("Sin productos asignados", script);
        Assert.Contains("curvas-qty-step", script);
        Assert.Contains("productSearchSequence", script);
        Assert.Contains("pluralize(total, \"pieza\", \"piezas\")", script);
        Assert.Contains("/Proveeduria/Curvas/Resumen?", script);
        Assert.Contains("activo:", script);
        Assert.DoesNotContain("txCurvaCodigo", view);
        Assert.DoesNotContain("txCurvaCodigo", script);
        Assert.DoesNotContain("btAgregarProductoCurva", view);
        Assert.DoesNotContain("addProductObjectives", script);
        Assert.DoesNotContain("<style", view, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MvcGuardResolvesRoleWithCertifiedProductosServiciosPattern()
    {
        string source = File.ReadAllText(Path.Combine(MvcRoot, "Controllers", "Curvas", "CurvasController.cs"));

        Assert.Contains("api/Usuario/ObtenerUsuarioPorEmail", source);
        Assert.Contains("api/Usuario/ObtenerUsuario", source);
        Assert.Contains("FindPermission(JsonNode.Parse(permisos), permissionCode)", source);
        Assert.Contains("HasCurvasAccessAsync(CurvasCatalogoPermissionCode)", source);
        Assert.Contains("No fue posible resolver", File.ReadAllText(Path.Combine(ApiRoot, "Controllers", "Curvas", "CurvasCatalogoController.cs")));
    }
}
