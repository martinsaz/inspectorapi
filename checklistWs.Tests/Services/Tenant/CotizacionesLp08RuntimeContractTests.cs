using Xunit;

namespace checklistWs.Tests.Services.Tenant
{
    public sealed class CotizacionesLp08RuntimeContractTests
    {
        private static string Controller => File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory, "../../../../checklistWs/Controllers/Cotizaciones/CotizacionesController.cs"));

        [Fact]
        public void Runtime_UsesNormalTenantResolutionAndBothCompatibilityGates()
        {
            Assert.Contains("ITenantDatabaseResolver", Controller);
            Assert.Contains("ITenantSqlConnectionFactory", Controller);
            Assert.Contains("DatabaseScopes.Cotizaciones", Controller);
            Assert.Contains("DatabaseScopes.ListaPrecios", Controller);
            Assert.Contains("CreateConnection(context)", Controller);
        }

        [Fact]
        public void Save_UsesLp08ServerSideAndDoesNotTrustClientSnapshot()
        {
            Assert.Contains("ResolverPrecioAsync", Controller);
            Assert.Contains("BuildLp08PartidasAsync", Controller);
            Assert.Contains("EnsureListaAsync", Controller);
            Assert.Contains("PrecioEfectivo", Controller);
            Assert.DoesNotContain("request.PrecioUnitario", Controller);
        }

        [Fact]
        public void Save_MaterializesMissingHeaderThenReResolvesWithoutCreatingPriceDetails()
        {
            string save = Slice("private async Task<List<CotizacionPartidaDbRow>> BuildLp08PartidasAsync", "private static CotizacionPartidaDbRow FromResolution");
            Assert.Contains("resolution.Resuelto && resolution.PrecioEfectivo.HasValue && !resolution.IdListaPrecio.HasValue", save);
            Assert.Contains("EnsureListaAsync", save);
            Assert.Equal(2, Count(save, "ResolverPrecioAsync"));
            Assert.DoesNotContain("GuardarPrecioAsync", save);
        }

        [Fact]
        public void Preview_RemainsReadOnlyAndNeverMaterializesAHeader()
        {
            string preview = Slice("[HttpPost(\"PreviewPrecios\")]", "[HttpPost(\"GuardarCotizacion\")]");
            Assert.Contains("ResolverPrecioAsync", preview);
            Assert.DoesNotContain("EnsureListaAsync", preview);
            Assert.DoesNotContain("GuardarPrecioAsync", preview);
        }

        [Fact]
        public void DraftLines_AreStableAndRemovedLinesUseLogicalDelete()
        {
            Assert.Contains("SincronizarPartidasLp08Async", Controller);
            Assert.Contains("SET Activo = 0, FechaArchivado", Controller);
            Assert.DoesNotContain("DELETE FROM dbo.CotizacionesPartidas", Controller);
        }

        [Fact]
        public void QuantityOnly_PreservesSnapshotWhileIdentityAndListReResolve()
        {
            Assert.Contains("preserveSnapshot", Controller);
            Assert.Contains("CopySnapshot", Controller);
            Assert.Contains("IdentityMatches", Controller);
            Assert.Contains("ConfirmarCambioLista", Controller);
        }

        [Fact]
        public void PriceZeroAndStrictAdditionalDiscountAreAcceptedWithoutClamp()
        {
            Assert.Contains("request.PrecioAplicado.Value < 0", Controller);
            Assert.Contains("item.DescuentoPct < 0 || item.DescuentoPct > 100", Controller);
            Assert.DoesNotContain("Math.Min(descuentoPct", Controller);
            Assert.DoesNotContain("Math.Max(0m, request.DescuentoPct", Controller);
        }

        [Fact]
        public void Override_RequiresAuthorizationReasonActorAndAudit()
        {
            Assert.Contains("ListaPreciosAdministrarPermissionCode", Controller);
            Assert.Contains("PRECIO_OVERRIDE_FORBIDDEN", Controller);
            Assert.Contains("OVERRIDE_INVALIDO", Controller);
            Assert.Contains("OVERRIDE_PRECIO", Controller);
        }

        [Fact]
        public void Clone_RepricesAndPersistsTenantScopedOrigin()
        {
            Assert.Contains("idCotizacionOrigen", Controller);
            Assert.Contains("CotizacionOrigenPerteneceAEmpresaAsync", Controller);
            Assert.Contains("CLON_RE_RESOLUCION", Controller);
        }

        [Fact]
        public void HistoricalQuotes_AreReadOnlyAndNeverAutoConverted()
        {
            Assert.Contains("La cotización es PRE_LP08 y se conserva sin recalcular", Controller);
            Assert.Contains("EsPreLp08", Controller);
        }

        [Fact]
        public void AllFourLp08IdentityShapesAreValidated()
        {
            Assert.Contains("IdentidadProducto", Controller);
            Assert.Contains("IdentidadServicio", Controller);
            Assert.Contains("IdentidadVariante", Controller);
            Assert.Contains("IdentidadPresentacionVenta", Controller);
        }

        [Fact]
        public void Audit_IsAppendOnlyAndUsesCertifiedOperationNames()
        {
            Assert.Contains("INSERT INTO dbo.CotizacionesHistorial", Controller);
            Assert.Contains("ALTA_PARTIDA", Controller);
            Assert.Contains("CAMBIO_CANTIDAD", Controller);
            Assert.Contains("CAMBIO_IDENTIDAD", Controller);
            Assert.Contains("CAMBIO_LISTA", Controller);
            Assert.Contains("NUEVA_RESOLUCION", Controller);
            Assert.Contains("DESCUENTO_ADICIONAL", Controller);
            Assert.Contains("BAJA_PARTIDA", Controller);
        }

        private static int Count(string source, string value)
            => source.Split(value, StringSplitOptions.None).Length - 1;

        private static string Slice(string start, string end)
        {
            int startIndex = Controller.IndexOf(start, StringComparison.Ordinal);
            int endIndex = Controller.IndexOf(end, startIndex, StringComparison.Ordinal);
            Assert.True(startIndex >= 0 && endIndex > startIndex);
            return Controller[startIndex..endIndex];
        }
    }
}
