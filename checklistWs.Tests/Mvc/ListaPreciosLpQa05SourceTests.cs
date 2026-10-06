using Xunit;

namespace checklistWs.Tests.Mvc;

public sealed class ListaPreciosLpQa05SourceTests
{
    private static readonly string WorkspaceRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
    private static readonly string InspectorRoot = Path.Combine(WorkspaceRoot, "inspector", "checklist");

    private static string EditorMarkup()
    {
        string view = File.ReadAllText(Path.Combine(InspectorRoot, "Views", "ListaPrecios", "Index.cshtml"));
        return view[view.IndexOf("id=\"modalListaPreciosEditor\"", StringComparison.Ordinal)..view.IndexOf("id=\"modalListaPreciosMasivo\"", StringComparison.Ordinal)];
    }

    [Fact]
    public void ModalKeepsOnlyFrozenLegacySectionsInExactOrder()
    {
        string editor = EditorMarkup();
        int context = editor.IndexOf("lpEditorContext", StringComparison.Ordinal);
        int prices = editor.IndexOf("<h3 class=\"lp-editor-section-title\">Precios y descuentos</h3>", StringComparison.Ordinal);
        int additional = editor.IndexOf("Datos adicionales", StringComparison.Ordinal);
        int promotions = editor.IndexOf("Promociones", StringComparison.Ordinal);
        int footer = editor.IndexOf("modal-footer lp-editor-footer", StringComparison.Ordinal);

        Assert.True(context < prices && prices < additional && additional < promotions && promotions < footer);
        Assert.DoesNotContain("Preview", editor, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Historial", editor, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Calcular precios finales", editor);
        Assert.DoesNotContain("Vigencia de la lista seleccionada", editor);
    }

    [Fact]
    public void RoundingCopyAndMatrixContractMatchLegacy()
    {
        string editor = EditorMarkup();

        Assert.Contains("El redondeo ajusta el precio final y recalcula el descuento correspondiente. No modifica el precio base.", editor);
        Assert.Contains("Aplicar redondeo a todas", editor);
        Assert.Contains("Sin redondeo", editor);
        Assert.Contains("A 4/9", editor);
        Assert.Contains("Sólo a 9", editor);
        Assert.Contains("Sólo la lista editada", editor);
        Assert.Contains("Todas las listas", editor);
        Assert.Contains("class=\"lp-round49-btn-apply\"", editor);
        Assert.Contains("<th>Lista</th><th>Precio</th><th>Descuento %</th><th>Precio final</th>", editor);
    }

    [Fact]
    public void ClientImplementsImmediateLegacyRoundingForFocusedAndAllRows()
    {
        string script = File.ReadAllText(Path.Combine(InspectorRoot, "wwwroot", "js", "ListaPrecios", "ListaPrecios.js"));

        Assert.Contains("function calculateRoundedFinal", script);
        Assert.Contains("function calculateDiscount", script);
        Assert.Contains("function applyCommercialRounding", script);
        Assert.Contains("last <= 4 ? integer + (4 - last) : integer + (9 - last)", script);
        Assert.Contains("integer + (9 - last + 10) % 10", script);
        Assert.Contains("applyEditorRoundingToAll", script);
        Assert.Contains("all ? $(\"#lpEditorMatrizRows tr[data-lp-matrix-level]\")", script);
        Assert.Contains("[data-lp-matrix-final-input]", script);
    }

    [Fact]
    public void MatrixRendersTenRowsAndPreservesZeroAsAValidValue()
    {
        string script = File.ReadAllText(Path.Combine(InspectorRoot, "wwwroot", "js", "ListaPrecios", "ListaPrecios.js"));

        Assert.Contains("for (let level = 1; level <= 10; level += 1)", script);
        Assert.Contains("Math.max(0, toDecimal", script);
        Assert.Contains("if (normalizedPrice === 0) return 0", script);
        Assert.Contains("value='\" + escapeHtml(formatNumberInput(finalPrice))", script);
    }

    [Fact]
    public void SaveIsDirtyOnlyAndNoOpDisablesSave()
    {
        string script = File.ReadAllText(Path.Combine(InspectorRoot, "wwwroot", "js", "ListaPrecios", "ListaPrecios.js"));

        Assert.Contains("$rows.filter(\"[data-dirty='true']\")", script);
        Assert.Contains("function refreshMatrixRowDirty", script);
        Assert.Contains("data-initial-price", script);
        Assert.Contains("data-initial-discount", script);
        Assert.Contains("data-initial-round", script);
        Assert.Contains("!state.canWrite || (!hasDirtyRows && !commercialDirty)", script);
    }

    [Fact]
    public void AdditionalAndPromotionControlsUseAuthorizedV3Runtime()
    {
        string editor = EditorMarkup();

        Assert.Contains(">Descripción<", editor);
        Assert.Contains(">Web<", editor);
        Assert.Contains(">Liverpool<", editor);
        Assert.Contains(">Mercado Libre<", editor);
        Assert.Contains(">Observaciones<", editor);
        Assert.DoesNotContain("Corrida manual", editor);
        Assert.Contains("2 x 1", editor);
        Assert.Contains("3 x 2", editor);
        Assert.Contains("Descuento en 2º", editor);
        Assert.Contains("Monedero", editor);
        Assert.DoesNotContain("Pendiente autorización de persistencia", editor);
        Assert.Contains("id=\"lpEditorDescription\" data-lp-commercial", editor);
        Assert.Contains("id=\"lpEditorDosPorUno\" data-lp-commercial", editor);
    }
}
