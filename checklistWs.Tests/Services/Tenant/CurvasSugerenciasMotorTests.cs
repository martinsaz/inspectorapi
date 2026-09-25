using checklistWs.Services.Tenant;
using Xunit;

namespace checklistWs.Tests.Services.Tenant
{
    public sealed class CurvasSugerenciasMotorTests
    {
        [Fact]
        public void RellenarCurva_CalculatesCoverageGapAndComplete()
        {
            CurvasSugerenciaItemRequest request = Item(CurvasModoCaptura.RellenarCurva);
            CurvaSiembraSnapshot siembra = new(Guid.NewGuid(), Guid.NewGuid(), 10);

            CurvasSugerenciaResult hueco = CurvasSugerenciasCalculator.Calculate(request, siembra, existenciaBase: 2, transitoBase: 3, presentacion: null);
            CurvasSugerenciaResult completa = CurvasSugerenciasCalculator.Calculate(request, siembra, existenciaBase: 4, transitoBase: 6, presentacion: null);

            Assert.Equal(CurvasSugerenciaEstado.Hueco, hueco.Estado);
            Assert.Equal(5, hueco.CoberturaBase);
            Assert.Equal(5, hueco.HuecoBase);
            Assert.Equal(0, hueco.CopeteBase);
            Assert.Equal(5, hueco.CantidadPropuestaBase);
            Assert.Equal(CurvasSugerenciaEstado.Completa, completa.Estado);
            Assert.Equal(0, completa.CantidadPropuestaBase);
        }

        [Fact]
        public void Copete_PreservesOverCoverageAndSuggestsZeroForRellenar()
        {
            CurvasSugerenciaResult result = CurvasSugerenciasCalculator.Calculate(
                Item(CurvasModoCaptura.RellenarCurva),
                new CurvaSiembraSnapshot(Guid.NewGuid(), Guid.NewGuid(), 8),
                existenciaBase: 12,
                transitoBase: 1,
                presentacion: null);

            Assert.Equal(CurvasSugerenciaEstado.Copete, result.Estado);
            Assert.Equal(13, result.CoberturaBase);
            Assert.Equal(5, result.CopeteBase);
            Assert.Equal(0, result.HuecoBase);
            Assert.Equal(0, result.CantidadPropuestaBase);
        }

        [Fact]
        public void SinCurva_DoesNotCalculateAutomaticSuggestion()
        {
            CurvasSugerenciaResult result = CurvasSugerenciasCalculator.Calculate(
                Item(CurvasModoCaptura.PedidoInicial),
                siembra: null,
                existenciaBase: 4,
                transitoBase: 2,
                presentacion: null);

            Assert.Equal(CurvasSugerenciaEstado.SinCurva, result.Estado);
            Assert.Null(result.IdCurva);
            Assert.Null(result.IdSiembra);
            Assert.Equal(0, result.CurvaObjetivoBase);
            Assert.Equal(0, result.CantidadPropuestaBase);
        }

        [Fact]
        public void PedidoInicial_ManualAndNoPedir_RespectApprovedModes()
        {
            CurvaSiembraSnapshot siembra = new(Guid.NewGuid(), Guid.NewGuid(), 7);

            CurvasSugerenciaResult inicial = CurvasSugerenciasCalculator.Calculate(Item(CurvasModoCaptura.PedidoInicial), siembra, 99, 99, null);
            CurvasSugerenciaResult manual = CurvasSugerenciasCalculator.Calculate(Item(CurvasModoCaptura.Manual, cantidadManual: 3), siembra, 2, 1, null);
            CurvasSugerenciaResult noPedir = CurvasSugerenciasCalculator.Calculate(Item(CurvasModoCaptura.NoPedir), siembra, 0, 0, null);

            Assert.Equal(7, inicial.CantidadPropuestaBase);
            Assert.Equal(CurvasSugerenciaEstado.Manual, manual.Estado);
            Assert.True(manual.OverrideManual);
            Assert.Equal(3, manual.CantidadFinalBase);
            Assert.Equal(CurvasSugerenciaEstado.NoPedir, noPedir.Estado);
            Assert.True(noPedir.NoPedir);
            Assert.Equal(0, noPedir.CantidadFinalBase);
        }

        [Fact]
        public void NegativeExistence_IsPreservedInCoverageAndGap()
        {
            CurvasSugerenciaResult result = CurvasSugerenciasCalculator.Calculate(
                Item(CurvasModoCaptura.RellenarCurva),
                new CurvaSiembraSnapshot(Guid.NewGuid(), Guid.NewGuid(), 6),
                existenciaBase: -2,
                transitoBase: 1,
                presentacion: null);

            Assert.Equal(-1, result.CoberturaBase);
            Assert.Equal(7, result.HuecoBase);
            Assert.Equal(7, result.CantidadPropuestaBase);
        }

        [Fact]
        public void ClosedPresentation_RoundsUpAndReportsExcess()
        {
            Guid presentationId = Guid.NewGuid();
            CurvasSugerenciaResult result = CurvasSugerenciasCalculator.Calculate(
                Item(CurvasModoCaptura.RellenarCurva, presentationId: presentationId),
                new CurvaSiembraSnapshot(Guid.NewGuid(), Guid.NewGuid(), 10),
                existenciaBase: 0,
                transitoBase: 0,
                presentacion: new PresentacionCompraSnapshot(presentationId, 6, false));

            Assert.False(result.PermiteCantidadBase);
            Assert.Equal(2, result.CantidadCompraSugerida);
            Assert.Equal(12, result.CantidadBaseConvertida);
            Assert.Equal(2, result.ExcedenteRedondeoBase);
            Assert.Equal(12, result.CantidadFinalBase);
        }

        [Fact]
        public void FreePresentation_KeepsExactBaseQuantity()
        {
            Guid presentationId = Guid.NewGuid();
            CurvasSugerenciaResult result = CurvasSugerenciasCalculator.Calculate(
                Item(CurvasModoCaptura.RellenarCurva, presentationId: presentationId),
                new CurvaSiembraSnapshot(Guid.NewGuid(), Guid.NewGuid(), 10),
                existenciaBase: 0,
                transitoBase: 0,
                presentacion: new PresentacionCompraSnapshot(presentationId, 6, true));

            Assert.True(result.PermiteCantidadBase);
            Assert.Equal(10, result.CantidadCompraSugerida);
            Assert.Equal(10, result.CantidadBaseConvertida);
            Assert.Equal(0, result.ExcedenteRedondeoBase);
        }

        [Fact]
        public void PreviewPayload_IsSnapshotCompatibleButReadOnly()
        {
            CurvasSugerenciaResult result = CurvasSugerenciasCalculator.Calculate(
                Item(CurvasModoCaptura.RellenarCurva, contextoJson: "{\"qa\":true}"),
                new CurvaSiembraSnapshot(Guid.NewGuid(), Guid.NewGuid(), 5),
                existenciaBase: 1,
                transitoBase: 1,
                presentacion: null);

            Assert.True(result.PreviewReadOnly);
            Assert.Null(result.SnapshotPayload.IdOperacionCurva);
            Assert.Equal(result.IdCurva, result.SnapshotPayload.IdCurva);
            Assert.Equal(result.IdSiembra, result.SnapshotPayload.IdSiembra);
            Assert.Equal(result.HuecoBase, result.SnapshotPayload.HuecoSnapshotBase);
            Assert.Equal(result.CantidadFinalBase, result.SnapshotPayload.CantidadFinalBase);
            Assert.Equal("{\"qa\":true}", result.SnapshotPayload.ContextoJson);
        }

        [Fact]
        public void Multisucursal_CalculatesEachBranchIndependently()
        {
            Guid productId = Guid.NewGuid();
            CurvaSiembraSnapshot siembra = new(Guid.NewGuid(), Guid.NewGuid(), 10);
            CurvasSugerenciaResult first = CurvasSugerenciasCalculator.Calculate(Item(CurvasModoCaptura.RellenarCurva, productId: productId, branchId: Guid.NewGuid()), siembra, 1, 1, null);
            CurvasSugerenciaResult second = CurvasSugerenciasCalculator.Calculate(Item(CurvasModoCaptura.RellenarCurva, productId: productId, branchId: Guid.NewGuid()), siembra, 8, 1, null);

            Assert.NotEqual(first.IdSucursal, second.IdSucursal);
            Assert.Equal(8, first.CantidadPropuestaBase);
            Assert.Equal(1, second.CantidadPropuestaBase);
        }

        [Fact]
        public void MotorSource_UsesInventarioV1AndOcTransitAndRejectsServices()
        {
            string source = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "../../../../checklistWs/Services/Tenant/CurvasSugerenciasMotorService.cs"));

            Assert.Contains("dbo.InventarioSaldos", source);
            Assert.Contains("CantidadBaseActual", source);
            Assert.Contains("dbo.OrdenesCompraDetalle", source);
            Assert.Contains("CantidadBasePendiente", source);
            Assert.Contains("CURVA_SERVICIO_RECHAZADO", source);
            Assert.DoesNotContain("ProductosServiciosExistencias", source);
            Assert.DoesNotContain("ProductosServiciosMovimientos", source);
        }

        private static CurvasSugerenciaItemRequest Item(
            CurvasModoCaptura modo,
            Guid? productId = null,
            Guid? branchId = null,
            Guid? presentationId = null,
            decimal? cantidadManual = null,
            string? contextoJson = null)
            => new()
            {
                IdSucursal = branchId ?? Guid.NewGuid(),
                IdProductoServicio = productId ?? Guid.NewGuid(),
                IdPresentacionCompra = presentationId,
                Modo = modo,
                CantidadManualBase = cantidadManual,
                ContextoJson = contextoJson
            };
    }
}
