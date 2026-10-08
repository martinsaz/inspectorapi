using checklistWs.Models.OrdenesCompra;
using Xunit;

namespace checklistWs.Tests.Controllers.OrdenesCompra
{
    public sealed class OrdenesCompraOc03SourceTests
    {
        [Fact]
        public void OrdenCompraDeliveryRange_RoundTripsThroughUiAndApi()
        {
            string source = ReadSource("checklistWs", "Controllers", "OrdenesCompra", "OrdenesCompraController.cs");
            string models = ReadSource("checklistWs", "Models", "OrdenesCompra", "OrdenesCompraModels.cs");
            string script = ReadSource("inspector", "checklist", "wwwroot", "js", "Activos", "OrdenesCompra", "OrdenesCompra.js");

            Assert.Contains("FechaMinima", models);
            Assert.Contains("FechaMaxima", models);
            Assert.Contains("@FechaMinima", source);
            Assert.Contains("@FechaMaxima", source);
            Assert.Contains("fechaMinima: $(\"#txOcFechaMinima\").val() || null", script);
            Assert.Contains("fechaMaxima: $(\"#txOcFechaMaxima\").val() || null", script);
            Assert.Contains("formatInputDate(detail.fechaMinima)", script);
            Assert.Contains("formatInputDate(detail.fechaMaxima)", script);
        }

        [Fact]
        public void BusquedaDto_ExposesVariantAndPurchasePresentationOptions()
        {
            OrdenCompraBusquedaProductoServicioDto dto = new();

            Assert.NotNull(dto.Variantes);
            Assert.NotNull(dto.PresentacionesCompra);
            Assert.False(dto.RequiereVariante);
            Assert.Contains(typeof(OrdenCompraBusquedaVarianteDto).GetProperties(), property => property.Name == "ClaveCombinacion");
            Assert.Contains(typeof(OrdenCompraBusquedaPresentacionCompraDto).GetProperties(), property => property.Name == "FactorConversionBase");
        }

        [Fact]
        public void OrdenesCompraController_UsesPurchasePresentationV1Only()
        {
            string source = ReadSource("checklistWs", "Controllers", "OrdenesCompra", "OrdenesCompraController.cs");

            Assert.Contains("OrdenesCompraPresentacionesCompra", source);
            Assert.Contains("ProductosServiciosVariantes", source);
            Assert.Contains("ResolvePresentacionCompraAsync", source);
            Assert.Contains("ResolveVarianteOrdenCompraAsync", source);
            Assert.DoesNotContain("PresentacionesVenta", source);
            Assert.DoesNotContain("ProductosServiciosExistencias", source);
            Assert.DoesNotContain("ProductosServiciosMovimientosInventario", source);
        }

        [Fact]
        public void NuevaOcClient_SendsV1PartidaContractAndDoesNotMoveInventory()
        {
            string script = ReadSource("inspector", "checklist", "wwwroot", "js", "Activos", "OrdenesCompra", "OrdenesCompra.js");

            Assert.Contains("idVariante", script);
            Assert.Contains("idPresentacionCompra", script);
            Assert.Contains("cantidadCompra", script);
            Assert.Contains("factorConversionSnapshot", script);
            Assert.Contains("cantidadBaseOrdenada", script);
            Assert.DoesNotContain("InventarioSaldos", script);
            Assert.DoesNotContain("InventarioMovimientos", script);
            Assert.DoesNotContain("InventarioSeries", script);
        }

        [Fact]
        public void NuevaOcClient_RendersSafePlainDescriptionsAndDraftCopy()
        {
            string script = ReadSource("inspector", "checklist", "wwwroot", "js", "Activos", "OrdenesCompra", "OrdenesCompra.js");
            string view = ReadSource("inspector", "checklist", "Views", "Activos", "OrdenesCompra", "Nueva.cshtml");

            Assert.Contains("function toPlainText", script);
            Assert.Contains("toPlainText(item.descripcion", script);
            Assert.Contains("toPlainText(partida.descripcion", script);
            Assert.Contains("Guardar borrador", script);
            Assert.Contains("Guardar borrador", view);
            Assert.DoesNotContain("Guardar orden", script);
            Assert.DoesNotContain("Guardar orden", view);
            Assert.DoesNotContain("sin mover inventario", view, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void NuevaOc_ReproducesFiveLegacyOperationalStagesWithoutInventedReviewUi()
        {
            string view = ReadSource("inspector", "checklist", "Views", "Activos", "OrdenesCompra", "Nueva.cshtml");
            string script = ReadSource("inspector", "checklist", "wwwroot", "js", "Activos", "OrdenesCompra", "OrdenesCompra.js");

            Assert.Contains("data-step-target=\"1\"", view);
            Assert.Contains("data-step-target=\"2\"", view);
            Assert.Contains("data-step-target=\"3\"", view);
            Assert.Contains("data-step-target=\"4\"", view);
            Assert.Contains("data-step-target=\"5\"", view);
            Assert.Contains("Sucursales destino", view);
            Assert.Contains("Producto / servicio", view);
            Assert.Contains("Partidas / Guardar", view);
            Assert.Contains("id=\"modalOcCaptura\"", view);
            Assert.Contains("id=\"panelOcCaptureRows\"", view);
            Assert.Contains("data-oc-card-item", script);
            Assert.DoesNotContain("data-oc-variant-item", script);
            Assert.DoesNotContain("Seleccionar y capturar", script);
            Assert.Contains("addPartidaFromCapture", script);
            Assert.DoesNotContain("Revisar y guardar", view);
            Assert.DoesNotContain("Avance del wizard", view);
            Assert.DoesNotContain("oc-sidebar", view);
            Assert.DoesNotContain("grOcRevisionPartidas", view);
        }

        [Fact]
        public void NuevaOcQa07_UsesWholeCardsRealAttributesAndCompleteCurveMetrics()
        {
            string view = ReadSource("inspector", "checklist", "Views", "Activos", "OrdenesCompra", "Nueva.cshtml");
            string script = ReadSource("inspector", "checklist", "wwwroot", "js", "Activos", "OrdenesCompra", "OrdenesCompra.js");
            string models = ReadSource("checklistWs", "Models", "OrdenesCompra", "OrdenesCompraModels.cs");
            string source = ReadSource("checklistWs", "Controllers", "OrdenesCompra", "OrdenesCompraController.cs");

            Assert.Contains("id=\"panelOcPreparationSummary\"", view);
            Assert.Contains("id=\"btOcEditarPreparacion\"", view);
            Assert.Contains("SELECCIONA EL PRODUCTO PARA CARGAR VARIANTES Y PRESENTACIONES", script);
            Assert.Contains("Piezas propuestas", script);
            Assert.Contains("Piezas finales", script);
            Assert.Contains("Categoria", models);
            Assert.Contains("Marca", models);
            Assert.Contains("AS Categoria", source);
            Assert.Contains("AS Marca", source);
            Assert.DoesNotContain("<b>Costo:</b>", script);
        }

        [Fact]
        public void NuevaOcQa02_KeepsFiveStagesContinuousAndPersistsPersonalFolioSeparately()
        {
            string view = ReadSource("inspector", "checklist", "Views", "Activos", "OrdenesCompra", "Nueva.cshtml");
            string script = ReadSource("inspector", "checklist", "wwwroot", "js", "Activos", "OrdenesCompra", "OrdenesCompra.js");
            string models = ReadSource("checklistWs", "Models", "OrdenesCompra", "OrdenesCompraModels.cs");
            string source = ReadSource("checklistWs", "Controllers", "OrdenesCompra", "OrdenesCompraController.cs");

            Assert.Contains("oc-main--continuous", view);
            Assert.Contains("id=\"panelOcPreparacion\"", view);
            Assert.Contains("id=\"panelOcPaso3\"", view);
            Assert.Contains("id=\"panelOcPaso4\"", view);
            Assert.Contains("id=\"panelOcPaso5\"", view);
            Assert.Contains("id=\"txOcFechaOrden\" type=\"hidden\"", view);
            Assert.Contains("id=\"cbOcRazonSocial\" hidden", view);
            Assert.Contains("id=\"txOcFolioReferencia\"", view);
            Assert.Contains("id=\"ckOcSoloProveedor\"", view);
            Assert.Contains("id=\"ckOcSoloProveedor\" type=\"checkbox\" checked", view);
            Assert.Contains("Solo productos de este proveedor", view);
            Assert.Contains("setInitialLegacyDates", script);
            Assert.Contains("today.getDate() + 7", script);
            Assert.Contains("folioReferencia: String($(\"#txOcFolioReferencia\").val()", script);
            Assert.Contains("idProveedor", script);
            Assert.Contains("captureDisabled", script);
            Assert.DoesNotContain("[data-step-panel]').prop('hidden'", script);
            Assert.Contains("FolioReferencia", models);
            Assert.Contains("@FolioReferencia", source);
        }

        [Fact]
        public void NuevaOcCss_KeepsDesktopOperationalColumnsWithoutRequiredHorizontalScroll()
        {
            string css = ReadSource("inspector", "checklist", "wwwroot", "css", "Activos", "OrdenesCompra", "OrdenesCompra.css");
            string view = ReadSource("inspector", "checklist", "Views", "Activos", "OrdenesCompra", "Nueva.cshtml");

            Assert.Contains(".oc-grid-table", css);
            Assert.Contains("table-layout: fixed", css);
            Assert.Contains("-webkit-line-clamp", css);
            Assert.Contains("oc-line-title--partida", css);
            Assert.Contains("<th>Concepto</th>", view);
            Assert.DoesNotContain("<th>Número</th>", view);
            Assert.Contains("<th>Sucursal</th>", view);
            Assert.Contains("colspan='8'", ReadSource("inspector", "checklist", "wwwroot", "js", "Activos", "OrdenesCompra", "OrdenesCompra.js"));
        }

        [Fact]
        public void NuevaOcV5_PersistsDestinationsAndBranchPerLineWithoutHeaderFallback()
        {
            string source = ReadSource("checklistWs", "Controllers", "OrdenesCompra", "OrdenesCompraController.cs");
            string models = ReadSource("checklistWs", "Models", "OrdenesCompra", "OrdenesCompraModels.cs");
            string script = ReadSource("inspector", "checklist", "wwwroot", "js", "Activos", "OrdenesCompra", "OrdenesCompra.js");
            string view = ReadSource("inspector", "checklist", "Views", "Activos", "OrdenesCompra", "Nueva.cshtml");

            Assert.Contains("List<Guid> IdSucursales", models);
            Assert.Contains("public Guid IdSucursal", models);
            Assert.Contains("UpsertSucursalesAsync", source);
            Assert.Contains("insertCommand.Parameters.AddWithValue(\"@IdSucursal\", DBNull.Value)", source);
            Assert.Contains("idSucursales: getSelectedSucursalIds()", script);
            Assert.Contains("idSucursal: normalizeGuid(partida.idSucursal)", script);
            Assert.Contains("\"all\", \"Todas las tiendas\"", script);
            Assert.Contains("data-oc-remove-branch", script);
            Assert.Contains("id=\"panelOcSucursalChips\"", view);
            Assert.Contains("id=\"panelOcCaptureRows\"", view);
            Assert.Contains("data-capture-row", script);
            Assert.Contains("idSucursal: row.idSucursal", script);
        }

        [Fact]
        public void NuevaOcQa08_GroupsCaptureByBranchAndTransitUsesTheV5DetailBranch()
        {
            string view = ReadSource("inspector", "checklist", "Views", "Activos", "OrdenesCompra", "Nueva.cshtml");
            string script = ReadSource("inspector", "checklist", "wwwroot", "js", "Activos", "OrdenesCompra", "OrdenesCompra.js");
            string motor = ReadSource("checklistWs", "Services", "Tenant", "CurvasSugerenciasMotorService.cs");

            Assert.Contains("id=\"panelOcCaptureGlobal\"", view);
            Assert.Contains("function renderProductCaptureStore", script);
            Assert.Contains("data-capture-store-mode", script);
            Assert.Contains("function renderServiceCaptureStore", script);
            Assert.Contains("function resolveManualCaptureBaseQuantity", script);
            Assert.Contains("toNumber(row.cantidad) * factor", script);
            Assert.Contains("d.idSucursal = @IdSucursal", motor);
            Assert.DoesNotContain("oc.idSucursal = @IdSucursal", motor);
        }

        [Fact]
        public void NuevaOcQa09_UsesCheckAppConfirmationAndTemporaryCurveWithoutEditableModalCost()
        {
            string view = ReadSource("inspector", "checklist", "Views", "Activos", "OrdenesCompra", "Nueva.cshtml");
            string script = ReadSource("inspector", "checklist", "wwwroot", "js", "Activos", "OrdenesCompra", "OrdenesCompra.js");
            string motor = ReadSource("checklistWs", "Services", "Tenant", "CurvasSugerenciasMotorService.cs");

            Assert.Contains("La selección manual de curva aplica sólo a esta orden", view);
            Assert.Contains("Swal.fire", script);
            Assert.Contains("a todas las sucursales preparadas para esta captura", script);
            Assert.DoesNotContain("window.confirm", script);
            Assert.DoesNotContain("data-capture-cost", script);
            Assert.DoesNotContain("Captura independiente por sucursal", script);
            Assert.Contains("data-capture-curve", script);
            Assert.Contains("idCurvaTemporal", script);
            Assert.Contains("LoadCurvaTemporalAsync", motor);
            Assert.Contains("Task<CurvaSiembraSnapshot?> LoadCurvaTemporalAsync", motor);
            Assert.Contains("return null;", motor);
            Assert.Contains("d.idVariante = @IdVariante OR d.idVariante IS NULL", motor);
            Assert.Contains("ORDER BY CASE WHEN d.idVariante = @IdVariante THEN 0 ELSE 1 END", motor);
        }

        [Fact]
        public void NuevaOcQa09_ExposesSemanticStatesAndRealQuantityControls()
        {
            string script = ReadSource("inspector", "checklist", "wwwroot", "js", "Activos", "OrdenesCompra", "OrdenesCompra.js");
            string css = ReadSource("inspector", "checklist", "wwwroot", "css", "Activos", "OrdenesCompra", "OrdenesCompra.css");

            Assert.Contains("Sin curva", script);
            Assert.Contains("Curva configurada", script);
            Assert.Contains("Hueco", script);
            Assert.Contains("Copete", script);
            Assert.Contains("No pedir", script);
            Assert.Contains("data-capture-step", script);
            Assert.Contains("data-capture-equal", script);
            Assert.Contains("accent-color: var(--ca-primary)", css);
            Assert.Contains(".oc-capture-state.is-over", css);
        }

        [Fact]
        public void NuevaOcV5_UsesTheOfficialSucursalSchemaWithoutInventedCodigoColumn()
        {
            string source = ReadSource("checklistWs", "Controllers", "OrdenesCompra", "OrdenesCompraController.cs");

            Assert.DoesNotContain("ISNULL(s.Codigo", source);
            Assert.Contains("CAST('' AS nvarchar(50)) AS Codigo", source);
        }

        [Fact]
        public void OrdenesCompraApiAuthzAcceptsSignedStringUserIdAndKeepsGuidAuditOptional()
        {
            string source = ReadSource("checklistWs", "Controllers", "OrdenesCompra", "OrdenesCompraController.cs");

            Assert.Contains("string usuarioId = TryResolveUserId();", source);
            Assert.Contains("UserId = usuarioId", source);
            Assert.Contains("UserId = usuarioIdRaw", source);
            Assert.Contains("UsuarioId = Guid.TryParse(usuarioIdRaw", source);
            Assert.DoesNotContain("string usuarioId = TryResolveUsuarioId()?.ToString() ?? string.Empty;", source);
        }

        [Fact]
        public void ReporteOc_UsesFiveRealStatesAndExistingReceiptTracking()
        {
            string source = ReadSource("checklistWs", "Controllers", "OrdenesCompra", "OrdenesCompraController.cs");
            OrdenCompraListadoDto listado = new();
            OrdenCompraPartidaDetalleDto partida = new();

            Assert.Contains("EstadoParcialmenteRecibida = 4", source);
            Assert.Contains("EstadoRecibida = 5", source);
            Assert.Contains("Parcialmente recibida", source);
            Assert.Contains("CantidadBaseRecibidaAcumulada", source);
            Assert.Contains("CantidadBasePendiente", source);
            Assert.Equal(0, listado.CantidadOrdenada);
            Assert.Equal(0, listado.CantidadRecibida);
            Assert.Equal(0, listado.CantidadPendiente);
            Assert.Equal(string.Empty, partida.EstadoPartidaNombre);
        }

        [Fact]
        public void ReporteOc_RemovesInventedKpisAndExposesTrackingColumns()
        {
            string view = ReadSource("inspector", "checklist", "Views", "Activos", "OrdenesCompra", "Index.cshtml");
            string script = ReadSource("inspector", "checklist", "wwwroot", "js", "Activos", "OrdenesCompra", "OrdenesCompra.js");

            Assert.DoesNotContain("ocKpiStrip", view);
            Assert.DoesNotContain("updateReportKpis", script);
            Assert.Contains("<th>Ordenado</th>", view);
            Assert.Contains("<th>Recibido</th>", view);
            Assert.Contains("<th>Pendiente</th>", view);
            Assert.Contains("cantidadBaseRecibidaAcumulada", script);
            Assert.Contains("estadoPartidaNombre", script);
        }

        [Fact]
        public void CatalogoCurvas_DoesNotExposeUnauthorizedRollbackAction()
        {
            string view = ReadSource("inspector", "checklist", "Views", "Curvas", "Catalogo.cshtml");
            string script = ReadSource("inspector", "checklist", "wwwroot", "js", "Curvas", "CurvasCatalogo.js");

            Assert.DoesNotContain("btRollbackCurva", view);
            Assert.DoesNotContain("Revertir cambios", view);
            Assert.DoesNotContain("rollbackForm", script);
        }

        private static string ReadSource(params string[] segments)
        {
            string current = AppContext.BaseDirectory;
            for (int index = 0; index < 10; index++)
            {
                string candidate = Path.GetFullPath(Path.Combine(new[] { current }.Concat(segments).ToArray()));
                if (File.Exists(candidate))
                {
                    return File.ReadAllText(candidate);
                }

                DirectoryInfo? parent = Directory.GetParent(current);
                if (parent == null)
                {
                    break;
                }

                current = parent.FullName;
            }

            throw new FileNotFoundException("No se pudo localizar el archivo fuente requerido por la prueba.", Path.Combine(segments));
        }
    }
}
