using Xunit;

namespace checklistWs.Tests.Mvc;

public sealed class ListaPreciosLpQa04RSourceTests
{
    private static readonly string WorkspaceRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
    private static readonly string InspectorRoot = Path.Combine(WorkspaceRoot, "inspector", "checklist");
    private static readonly string ApiRoot = Path.Combine(WorkspaceRoot, "inspectorapi", "checklistWs");

    [Fact]
    public void SucursalesDropdownEscapesAccordionOnlyInsideListaPrecios()
    {
        string css = File.ReadAllText(Path.Combine(InspectorRoot, "wwwroot", "css", "ListaPrecios", "ListaPrecios.css"));

        Assert.Contains("#accordionFiltrosListaPrecios.is-open", css);
        Assert.Contains("overflow: visible", css);
        Assert.Contains("overscroll-behavior: contain", css);
    }

    [Fact]
    public void EditorUsesCompactLegacyCompositionWithoutExcludedFields()
    {
        string view = File.ReadAllText(Path.Combine(InspectorRoot, "Views", "ListaPrecios", "Index.cshtml"));
        string editor = view[view.IndexOf("id=\"modalListaPreciosEditor\"", StringComparison.Ordinal)..view.IndexOf("id=\"modalListaPreciosMasivo\"", StringComparison.Ordinal)];

        Assert.Contains("lpEditorContext", editor);
        Assert.Contains("lpEditorDescription", editor);
        Assert.Contains("modal-footer lp-editor-footer", editor);
        Assert.Contains("Datos adicionales", editor);
        Assert.Contains("Promociones", editor);
        Assert.DoesNotContain("Corrida manual", editor);
        Assert.DoesNotContain("Preview e historial", editor);
        Assert.DoesNotContain("Calcular precios finales", editor);
        Assert.DoesNotContain("Vigencia de la lista seleccionada", editor);
    }

    [Fact]
    public void DescriptionComesFromCanonicalReadOnlyQueryAndSaveRemainsDirtyOnly()
    {
        string models = File.ReadAllText(Path.Combine(ApiRoot, "Services", "Tenant", "ListaPreciosModels.cs"));
        string service = File.ReadAllText(Path.Combine(ApiRoot, "Services", "Tenant", "ListaPreciosService.cs"));
        string script = File.ReadAllText(Path.Combine(InspectorRoot, "wwwroot", "js", "ListaPrecios", "ListaPrecios.js"));

        Assert.Contains("public string Descripcion { get; set; } = string.Empty;", models);
        Assert.Contains("ISNULL(ps.Descripcion, N'') AS Descripcion", service);
        Assert.Contains("Descripcion = identity.Descripcion", service);
        Assert.Contains("$rows.filter(\"[data-dirty='true']\")", script);
        Assert.Contains("Modifica al menos un valor antes de guardar.", script);
        Assert.Contains("updateEditorSaveState", script);
    }
}
