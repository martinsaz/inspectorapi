using checklistWs.Models.OrdenesCompra;
using Xunit;

namespace checklistWs.Tests.Controllers.OrdenesCompra
{
    public sealed class OrdenesCompraOc03SourceTests
    {
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
        public void NuevaOcCss_KeepsDesktopOperationalColumnsWithoutRequiredHorizontalScroll()
        {
            string css = ReadSource("inspector", "checklist", "wwwroot", "css", "Activos", "OrdenesCompra", "OrdenesCompra.css");
            string view = ReadSource("inspector", "checklist", "Views", "Activos", "OrdenesCompra", "Nueva.cshtml");

            Assert.Contains(".oc-grid-table", css);
            Assert.Contains("table-layout: fixed", css);
            Assert.Contains("-webkit-line-clamp", css);
            Assert.Contains("oc-line-title--partida", css);
            Assert.Contains("<th>Producto o servicio</th>", view);
            Assert.DoesNotContain("<th>Número</th>", view);
            Assert.Contains("colspan='10'", ReadSource("inspector", "checklist", "wwwroot", "js", "Activos", "OrdenesCompra", "OrdenesCompra.js"));
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
