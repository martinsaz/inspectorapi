using Xunit;

namespace checklistWs.Tests.Mvc;

public sealed class ProductosServiciosCatalogModalLifecycleTests
{
    private static readonly string RepoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
    private static readonly string InspectorRoot = Path.Combine(RepoRoot, "inspector", "checklist");

    [Fact]
    public void SharedCatalogModalBridgeDestroysDescriptionEditorBeforeResetOrHiddenCatalogs()
    {
        string shared = File.ReadAllText(Path.Combine(InspectorRoot, "wwwroot", "js", "ProductosServicios", "ProductosServiciosCatalogModalShared.js"));

        Assert.Contains("CatalogModalBridge.prototype.destroyDescriptionEditor", shared);
        Assert.Contains("this.destroyDescriptionEditor();", shared);
        Assert.Contains("initToken !== bridge._descriptionEditorInitToken", shared);
        Assert.Contains("!isFieldVisible(bridge.options.descriptionFieldSelector)", shared);
        Assert.Contains("editor.remove();", shared);
    }

    [Fact]
    public void QuickCreateUsesCatalogSpecificConfigurationBeforeTinyMceInitialization()
    {
        string script = File.ReadAllText(Path.Combine(InspectorRoot, "wwwroot", "js", "ProductosServicios", "ProductosServicios.js"));

        Assert.Contains("quickCatalogBridge.reset(config", script);
        Assert.Contains("showDescription: false", ExtractFunction(script, "resetQuickCatalogModal"));
        Assert.DoesNotContain("quickCatalogBridge.initDescriptionEditor(config);", ExtractFunction(script, "openQuickCatalogModal"));
        Assert.Contains("closeTagsPopover();", ExtractFunction(script, "openQuickCatalogModal"));
    }

    [Fact]
    public void NestedQuickCatalogHiddenRestoresParentModalAndTrimsBackdrops()
    {
        string script = File.ReadAllText(Path.Combine(InspectorRoot, "wwwroot", "js", "ProductosServicios", "ProductosServicios.js"));
        string hiddenHandler = ExtractFunction(script, "cleanupQuickCatalogModalLifecycle");
        string restore = ExtractFunction(script, "restoreParentModalAfterQuickCatalog");

        Assert.Contains("cleanupQuickCatalogModalLifecycle", script);
        Assert.Contains("resetQuickCatalogModal();", hiddenHandler);
        Assert.Contains("restoreParentModalAfterQuickCatalog();", hiddenHandler);
        Assert.Contains("document.body.classList.add(\"modal-open\")", restore);
        Assert.Contains("trimModalBackdrops(1);", restore);
        Assert.Contains("trimModalBackdrops(0);", restore);
    }

    [Fact]
    public void EtiquetasCrudKeepsSoloNombreWithoutDescriptionEditor()
    {
        string script = File.ReadAllText(Path.Combine(InspectorRoot, "wwwroot", "js", "ProductosServicios", "ProductosServiciosCatalogos.js"));

        Assert.Contains("const showDescription = !!initializeRichText && pageKey !== \"unidades\" && pageKey !== \"etiquetas\";", script);
        Assert.Contains("showDescription: showDescription", script);
        Assert.Contains("descriptionMax: 0", ExtractConfig(script, "etiquetas"));
        Assert.Contains("return validateBasic(0, 100, false, false);", ExtractConfig(script, "etiquetas"));
    }

    [Fact]
    public void CrudCatalogModalInitializesRichTextOnlyWhenOpening()
    {
        string script = File.ReadAllText(Path.Combine(InspectorRoot, "wwwroot", "js", "ProductosServicios", "ProductosServiciosCatalogos.js"));

        Assert.Contains("resetModal(false);", script);
        Assert.Contains("resetModal(true);", ExtractFunction(script, "openCreateModal"));
        Assert.Contains("resetModal(true);", script);
        Assert.Contains("hidden.bs.modal", script);
        Assert.Contains("const showDescription = !!initializeRichText", script);
    }

    [Fact]
    public void ResponsiveCatalogActionsDelegateFromGridHostAndCannotFallThroughToExport()
    {
        string script = File.ReadAllText(Path.Combine(InspectorRoot, "wwwroot", "js", "ProductosServicios", "ProductosServiciosCatalogos.js"));
        string view = File.ReadAllText(Path.Combine(InspectorRoot, "Views", "ProductosServicios", "_ProductosServiciosCatalogoPage.cshtml"));
        string actionBuilder = ExtractFunction(script, "buildActionLink");

        Assert.Contains("#gridCatalogoHost [data-ps-catalog-action]", script);
        Assert.Contains("click.psCatalogActions", script);
        Assert.Contains("event.preventDefault();", script);
        Assert.Contains("event.stopPropagation();", script);
        Assert.Contains("type='button'", actionBuilder);
        Assert.Contains("data-ps-catalog-action='", actionBuilder);
        Assert.Contains("action === \"edit\"", script);
        Assert.Contains("window.psCatalogoEditar(id);", script);
        Assert.Contains("exportButtonSelector: \"#btExportarCatalogo\"", script);
        Assert.Contains("id=\"btExportarCatalogo\"", view);
        Assert.DoesNotContain("data-ps-catalog-action", ExtractAround(view, "btExportarCatalogo", 250));
    }

    [Fact]
    public void DynamicGridExportResolvesSheetJsFromWindowOrGlobalScope()
    {
        string checkappUi = File.ReadAllText(Path.Combine(InspectorRoot, "wwwroot", "js", "checkapp-ui.js"));
        string resolver = ExtractFunction(checkappUi, "resolveXlsxLibrary");
        string loader = ExtractFunction(checkappUi, "loadXlsxLibrary");
        string fallback = ExtractFunction(checkappUi, "exportSpreadsheetFallback");
        string exportGrid = ExtractFunction(checkappUi, "exportGrid");

        Assert.Contains("window.XLSX", resolver);
        Assert.Contains("typeof XLSX !== \"undefined\"", resolver);
        Assert.Contains("xlsx.full.min.js", loader);
        Assert.Contains("caExportReload", loader);
        Assert.Contains("window.XLSX = window.XLSX || {}", loader);
        Assert.Contains("window.define && window.define.amd", loader);
        Assert.Contains("restoreDefine();", loader);
        Assert.Contains("await loadXlsxLibrary()", exportGrid);
        Assert.Contains("xlsx.utils.aoa_to_sheet", exportGrid);
        Assert.Contains("exportSpreadsheetFallback(exportData, sheetName, fileName);", exportGrid);
        Assert.Contains("application/vnd.ms-excel", fallback);
        Assert.Contains("document.createElement(\"a\")", fallback);
        Assert.DoesNotContain("typeof window.XLSX === \"undefined\"", exportGrid);
    }

    private static string ExtractFunction(string source, string functionName)
    {
        string marker = "function " + functionName;
        int start = source.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Function {functionName} was not found.");

        int brace = source.IndexOf('{', start);
        Assert.True(brace >= 0, $"Function {functionName} does not have a body.");

        int depth = 0;
        for (int index = brace; index < source.Length; index++)
        {
            if (source[index] == '{')
            {
                depth++;
            }
            else if (source[index] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return source.Substring(start, index - start + 1);
                }
            }
        }

        throw new InvalidOperationException($"Function {functionName} body was not closed.");
    }

    private static string ExtractConfig(string source, string configName)
    {
        string marker = configName + ": {";
        int start = source.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Config {configName} was not found.");

        int nextConfig = source.IndexOf("\n        }", start, StringComparison.Ordinal);
        Assert.True(nextConfig > start, $"Config {configName} was not closed.");
        return source.Substring(start, nextConfig - start);
    }

    private static string ExtractAround(string source, string marker, int radius)
    {
        int index = source.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(index >= 0, $"Marker {marker} was not found.");
        int start = Math.Max(0, index - radius);
        int length = Math.Min(source.Length - start, marker.Length + (radius * 2));
        return source.Substring(start, length);
    }
}
