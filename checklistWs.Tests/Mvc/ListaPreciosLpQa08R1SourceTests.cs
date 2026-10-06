using Xunit;

namespace checklistWs.Tests.Mvc;

public sealed class ListaPreciosLpQa08R1SourceTests
{
    private static readonly string WorkspaceRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
    private static readonly string ScriptPath = Path.Combine(WorkspaceRoot, "inspector", "checklist", "wwwroot", "js", "ListaPrecios", "ListaPrecios.js");

    [Fact]
    public void BulkPreviewRendersResponseAndNeverExecutesBeforeExplicitConfirmation()
    {
        string script = File.ReadAllText(ScriptPath);
        int start = script.IndexOf("function previewBulkAdjustment()", StringComparison.Ordinal);
        int end = script.IndexOf("function renderBulkPreview", start, StringComparison.Ordinal);
        string preview = script[start..end];

        Assert.Contains("state.bulkPreview = result;", preview);
        Assert.Contains("renderBulkPreview(result);", preview);
        Assert.Contains(".finally(function ()", preview);
        Assert.Contains("setBusy(\"#btLpBulkPreview\", false);", preview);
        Assert.DoesNotContain("confirmCheckApp", preview);
        Assert.DoesNotContain("EjecutarAjusteMasivo", preview);
    }

    [Fact]
    public void BulkExecutionRequiresRenderedConfirmationAndUsesPreviewCorrelation()
    {
        string script = File.ReadAllText(ScriptPath);
        int start = script.IndexOf("function executeBulkAdjustment()", StringComparison.Ordinal);
        int end = script.IndexOf("function invalidateBulkPreview", start, StringComparison.Ordinal);
        string execute = script[start..end];

        Assert.Contains("!state.bulkPreview || !$(\"#lpBulkConfirm\").prop(\"checked\")", execute);
        Assert.Contains("payload.correlationId = state.bulkPreview.correlationId;", execute);
        Assert.Contains("postJson(\"/ListaPrecios/EjecutarAjusteMasivo\", payload)", execute);
        Assert.Contains("reloadGrid();", execute);
        Assert.Contains("setBusy(\"#btLpBulkExecute\", false);", execute);
    }
}
