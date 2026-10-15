using checklistWs.Models.Kits;
using Xunit;

namespace checklistWs.Tests.Mvc;

public sealed class KitsListadoSourceTests
{
    private static readonly string RepoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
    private static readonly string InspectorRoot = Path.Combine(RepoRoot, "inspector", "checklist");
    private static readonly string ApiRoot = Path.Combine(RepoRoot, "inspectorapi", "checklistWs");

    [Fact]
    public void RouteIsReadProtectedAndTenantComesOnlyFromSignedServerContext()
    {
        string controller = File.ReadAllText(Path.Combine(InspectorRoot, "Controllers", "Kits", "KitsController.cs"));

        Assert.Contains("[Authorize]", controller);
        Assert.Contains("[HttpGet(\"Index\")]", controller);
        Assert.Contains("ReadPermissionCode = \"05001010\"", controller);
        Assert.Contains("WritePermissionCode = \"05001011\"", controller);
        Assert.Contains("AuthorizeRead()", controller);
        Assert.Contains("AddSignedTenantHeaders(request)", controller);
        Assert.Contains("query.Add(new KeyValuePair<string, string?>(\"idEmpresa\", ResolveIdEmpresa()))", controller);
        Assert.Contains("TenantInputNames.Contains(key, StringComparer.OrdinalIgnoreCase)", controller);
        Assert.DoesNotContain("Request.Query[\"idEmpresa\"]", controller);
    }

    [Fact]
    public void ViewUsesOfficialPatternAndExposesWriteOnlyCreateFlow()
    {
        string view = File.ReadAllText(Path.Combine(InspectorRoot, "Views", "Kits", "Index.cshtml"));

        Assert.Contains("checkapp-scope checkapp-page kits-page", view);
        Assert.Contains("checkapp-hero", view);
        Assert.Contains("PROVEEDURÍA", view);
        Assert.Contains("Administra conjuntos de productos, servicios o combinaciones de ambos.", view);
        Assert.Contains("checkapp-accordion", view);
        Assert.Contains("grKits", view);
        Assert.Contains("panelColumnasKits", view);
        Assert.Contains("Valor componentes", view);
        Assert.Contains("Actualización", view);
        Assert.Contains("ViewBag.PuedeAdministrarKits == true", view);
        Assert.Contains("Nuevo Kit", view);
        Assert.Contains("kits-modal-dialog", view);
        Assert.DoesNotContain("modal-xl", view);
        Assert.Contains("Componentes del Kit", view);
        Assert.Contains("tipoKitAlta", view);
        Assert.Contains("estatusKitAlta", view);
        // El Paso 1 aprobado conserva el control compacto de imagen; la copia
        // "Imagen principal" no forma parte del contrato funcional vigente.
        Assert.Contains("kits-image-panel", view);
        Assert.Contains("vistaImagenKit", view);
        Assert.Contains("flImagenKit", view);
        Assert.Contains("btSeleccionarImagenKit", view);
        Assert.Contains("btEliminarImagenKit", view);
        Assert.Contains("cbFuenteKit", view);
        Assert.Contains("cbIdentidadKit", view);
        Assert.DoesNotContain("txBuscarComponenteKit", view);
        Assert.DoesNotContain("cbComponenteKit", view);
    }

    [Fact]
    public void ScriptUsesDynamicGridAndBackendValuesWithoutClientBusinessCalculation()
    {
        string script = File.ReadAllText(Path.Combine(InspectorRoot, "wwwroot", "js", "Kits", "Kits.js"));

        Assert.Contains("CheckAppUI.createDynamicGrid", script);
        Assert.Contains("CheckAppUI.createFilterAccordion", script);
        Assert.Contains("/Kits/Consultar?", script);
        Assert.Contains("busqueda", script);
        Assert.Contains("tipo", script);
        Assert.Contains("activo", script);
        Assert.Contains("No hay Kits registrados.", script);
        Assert.Contains("window.Swal.fire", script);
        Assert.Contains("mobileCardTitleKey: \"nombre\"", script);
        Assert.Contains("key: \"valorComponentes\"", script);
        Assert.Contains("key: \"precioKit\"", script);
        Assert.Contains("key: \"diferencia\"", script);
        Assert.Contains("key: \"fechaActualizacion\"", script);
        Assert.DoesNotContain("precioKit -", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("alert(", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("localStorage", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sessionStorage", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/Kits/ResolverPrecio", script);
        Assert.Contains("/Kits/Crear", script);
        Assert.Contains("/Kits/Actualizar/", script);
        Assert.Contains("/Kits/Detalle/", script);
        Assert.Contains("data-kit-archive", script);
        Assert.Contains("data-kit-reactivate", script);
        Assert.Contains("select2", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/Kits/SubirImagenTemporal", script);
        Assert.Contains("COMPONENTE", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No se puede cambiar el tipo", script);
        Assert.Contains("crypto.randomUUID", script);
    }

    [Fact]
    public void WriteEndpointsAreServerAuthorizedAndImageUsesTemporaryPipeline()
    {
        string mvc = File.ReadAllText(Path.Combine(InspectorRoot, "Controllers", "Kits", "KitsController.cs"));
        string api = File.ReadAllText(Path.Combine(ApiRoot, "Controllers", "Kits", "KitsController.cs"));
        string image = File.ReadAllText(Path.Combine(ApiRoot, "Services", "Tenant", "KitsImageStorage.cs"));

        Assert.Contains("CanWrite() ? null : Forbid()", mvc);
        Assert.Contains("[HttpPost(\"SubirImagenTemporal\")]", mvc);
        Assert.Contains("[HttpPost(\"SubirImagenTemporal\")]", api);
        Assert.Contains("PromoteAsync", api);
        Assert.Contains("DeleteFinalQuietlyAsync", api);
        Assert.Contains("FindByOperationKeyAsync", api);
        Assert.Contains("request.ImagenUrl = existing.ImagenUrl", api);
        Assert.Contains("/Kits/Temporal/Imagen", image);
        Assert.Contains("/Kits/{kitId:N}/Imagen", image);
        Assert.DoesNotContain("base64,", image, StringComparison.OrdinalIgnoreCase);
        string script = File.ReadAllText(Path.Combine(InspectorRoot, "wwwroot", "js", "Kits", "Kits.js"));
        Assert.Contains("imageToken || null", script);
        Assert.Contains("safeImageUrl", script);
        Assert.Contains("[HttpPut(\"Actualizar/{idKit:guid}\")]", mvc);
        Assert.Contains("[HttpPost(\"Baja/{idKit:guid}\")]", mvc);
        Assert.Contains("[HttpPost(\"Reactivar/{idKit:guid}\")]", mvc);
    }

    [Fact]
    public void ApiListContractSupportsAllStatusesDateAndEscapedNameSearch()
    {
        KitListQuery query = new();
        Assert.Null(query.Activo);

        string models = File.ReadAllText(Path.Combine(ApiRoot, "Models", "Kits", "KitsModels.cs"));
        string service = File.ReadAllText(Path.Combine(ApiRoot, "Services", "Tenant", "KitsService.cs"));
        Assert.Contains("public DateTime FechaActualizacion", models);
        Assert.Contains("Nombre LIKE @Search ESCAPE N'~'", service);
        Assert.Contains("EscapeLike(query.Busqueda.Trim())", service);
        Assert.Contains("detail.NumeroComponentes=active.Length", service);
        Assert.Contains("detail.ValorComponentes=KitsPolicy.Money", service);
        Assert.Contains("detail.Diferencia=KitsPolicy.Money", service);
    }
}
