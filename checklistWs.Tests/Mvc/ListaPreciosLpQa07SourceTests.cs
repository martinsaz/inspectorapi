using Xunit;

namespace checklistWs.Tests.Mvc;

public sealed class ListaPreciosLpQa07SourceTests
{
    private static readonly string WorkspaceRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
    private static readonly string ScriptPath = Path.Combine(WorkspaceRoot, "inspector", "checklist", "wwwroot", "js", "ListaPrecios", "ListaPrecios.js");

    [Fact]
    public void SaveUsesCheckAppConfirmationAndClosesOnlyAfterPersistence()
    {
        string script = File.ReadAllText(ScriptPath);
        int request = script.IndexOf("postJson(\"/ListaPrecios/GuardarMatriz\"", StringComparison.Ordinal);
        int close = script.IndexOf("closeEditorModal();", request, StringComparison.Ordinal);
        int refresh = script.IndexOf("reloadGrid();", close, StringComparison.Ordinal);

        Assert.Contains("confirmCheckApp(\"¿Guardar los cambios?\", \"Guardar\")", script);
        Assert.DoesNotContain("window.confirm", script);
        Assert.DoesNotContain("window.alert", script);
        Assert.DoesNotContain("window.prompt", script);
        Assert.True(request >= 0 && close > request && refresh > close);
        Assert.Contains("showCheckAppSuccess(\"Los cambios se guardaron correctamente.\")", script);
        Assert.Contains("showCheckAppError(error.message)", script);
    }

    [Fact]
    public void RoundingUsesCurrentFinalForLegacyAllScope()
    {
        string script = File.ReadAllText(ScriptPath);

        Assert.Contains("function applyRoundingToCurrentFinal", script);
        Assert.Contains("const currentFinal = clampNumber(toDecimal($row.find(\"[data-lp-matrix-final-input]\").val()), 0, price);", script);
        Assert.Contains("if (all) applyRoundingToCurrentFinal($row, mode);", script);
        Assert.Contains("applyRoundingToCurrentFinal($row, mode);", script);
    }

    [Fact]
    public void UnchangedCommercialDataIsNotMarkedDirtyByItsOwnFlag()
    {
        string script = File.ReadAllText(ScriptPath);

        Assert.Contains("const commercialSnapshot = JSON.stringify(commercial);", script);
        Assert.Contains("commercial.modificado = !!state.editorCommercialInitial && commercialSnapshot !== state.editorCommercialInitial;", script);
        Assert.DoesNotContain("commercial.modificado = !!state.editorCommercialInitial && JSON.stringify(commercial)", script);
    }

    [Fact]
    public void InheritedDescriptionKeepsOriginalPayloadWhileOwnFieldsRemainEditable()
    {
        string script = File.ReadAllText(ScriptPath);

        Assert.Contains("const inheritedDescription = !!(state.editorCommercial && state.editorCommercial.descripcionReadOnly);", script);
        Assert.Contains("inheritedDescription ? (state.editorCommercial.descripcion || \"\")", script);
        Assert.Contains("web: $(\"#lpEditorWeb\").val()", script);
        Assert.Contains("monedero: $(\"#lpEditorMonedero\").prop(\"checked\")", script);
    }

    [Fact]
    public void Qa08ToolbarAndCommercialDialogsUseLegacyFacingTerms()
    {
        string viewPath = Path.Combine(WorkspaceRoot, "inspector", "checklist", "Views", "ListaPrecios", "Index.cshtml");
        string view = File.ReadAllText(viewPath);
        string script = File.ReadAllText(ScriptPath);

        Assert.DoesNotContain("id=\"btHistorialListaPrecios\"", view);
        Assert.Contains("Ajuste masivo", view);
        Assert.Contains("Copiar lista", view);
        Assert.Contains("Desc. Marca", view);
        Assert.Contains("Sugerir acciones", view);
        Assert.Contains("Se aplicará a los productos con los filtros actuales.", view);
        Assert.Contains("Copiar también descuentos", view);
        Assert.DoesNotContain(">Generar preview<", view);
        Assert.DoesNotContain("checkapp-panel-eyebrow\">LP-", view);
        Assert.Contains("Ventas, costo y margen se muestran como N/A", view);
        Assert.Contains("confirmCheckApp", script);
    }
}
