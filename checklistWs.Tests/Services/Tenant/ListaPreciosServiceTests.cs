using checklistWs.Services.Tenant;
using Xunit;

namespace checklistWs.Tests.Services.Tenant
{
    public sealed class ListaPreciosServiceTests
    {
        private static readonly Guid Empresa = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid OtraEmpresa = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        private static readonly Guid Producto = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid Servicio = Guid.Parse("22222222-2222-2222-2222-222222222222");
        private static readonly Guid Variante = Guid.Parse("33333333-3333-3333-3333-333333333333");
        private static readonly Guid Presentacion = Guid.Parse("44444444-4444-4444-4444-444444444444");
        private static readonly Guid CategoriaProducto = Guid.Parse("55555555-5555-5555-5555-555555555555");
        private static readonly Guid CategoriaServicio = Guid.Parse("56565656-5656-5656-5656-565656565656");
        private static readonly Guid MarcaProducto = Guid.Parse("66666666-6666-6666-6666-666666666666");
        private static readonly Guid ColeccionProducto = Guid.Parse("77777777-7777-7777-7777-777777777777");
        private static readonly Guid EtiquetaProducto = Guid.Parse("88888888-8888-8888-8888-888888888888");
        private static readonly Guid AtributoProducto = Guid.Parse("99999999-9999-9999-9999-999999999999");
        private static readonly Guid AtributoValorProducto = Guid.Parse("98989898-9898-9898-9898-989898989898");
        private static readonly Guid SucursalCentro = Guid.Parse("abababab-abab-abab-abab-abababababab");
        private static readonly Guid SucursalNorte = Guid.Parse("cdcdcdcd-cdcd-cdcd-cdcd-cdcdcdcdcdcd");
        private const string ProductoImagenUrl = "https://storage.checkapp.test/producto.jpg";
        private const string VarianteImagenUrl = "https://storage.checkapp.test/variante.jpg";

        [Fact]
        public async Task ConsultarTipoTodos_IncluyeProductoYServicio()
        {
            Fixture fixture = Fixture.Base();

            IReadOnlyList<ListaPreciosConsultaRowDto> rows = await fixture.Service.ConsultarAsync(Empresa, new ListaPreciosConsultaRequest { Tipo = null });

            Assert.Contains(rows, row => row.Tipo == ListaPreciosConstants.TipoProducto);
            Assert.Contains(rows, row => row.Tipo == ListaPreciosConstants.TipoServicio);
        }

        [Fact]
        public async Task ConsultarTipoProducto_NoDevuelveServicios()
        {
            Fixture fixture = Fixture.Base();

            IReadOnlyList<ListaPreciosConsultaRowDto> rows = await fixture.Service.ConsultarAsync(Empresa, new ListaPreciosConsultaRequest { Tipo = ListaPreciosConstants.TipoProducto });

            Assert.NotEmpty(rows);
            Assert.All(rows, row => Assert.Equal(ListaPreciosConstants.TipoProducto, row.Tipo));
            Assert.DoesNotContain(rows, row => row.Tipo == ListaPreciosConstants.TipoServicio);
        }

        [Fact]
        public async Task ConsultarTipoServicio_DevuelveServiciosSinProductos()
        {
            Fixture fixture = Fixture.Base();

            IReadOnlyList<ListaPreciosConsultaRowDto> rows = await fixture.Service.ConsultarAsync(Empresa, new ListaPreciosConsultaRequest { Tipo = ListaPreciosConstants.TipoServicio });

            Assert.NotEmpty(rows);
            Assert.All(rows, row => Assert.Equal(ListaPreciosConstants.TipoServicio, row.Tipo));
            Assert.DoesNotContain(rows, row => row.Tipo == ListaPreciosConstants.TipoProducto);
        }

        [Fact]
        public async Task ConsultarTipoServicioConBusqueda_PreservaFiltroTipo()
        {
            Fixture fixture = Fixture.Base();

            IReadOnlyList<ListaPreciosConsultaRowDto> rows = await fixture.Service.ConsultarAsync(Empresa, new ListaPreciosConsultaRequest
            {
                Tipo = ListaPreciosConstants.TipoServicio,
                Busqueda = "Cambio"
            });

            Assert.Single(rows);
            Assert.Equal(ListaPreciosConstants.TipoServicio, rows[0].Tipo);
            Assert.Contains("Cambio", rows[0].Nombre, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ConsultarTipoServicioCrossTenant_NoDevuelveDatos()
        {
            Fixture fixture = Fixture.Base();

            IReadOnlyList<ListaPreciosConsultaRowDto> rows = await fixture.Service.ConsultarAsync(OtraEmpresa, new ListaPreciosConsultaRequest { Tipo = ListaPreciosConstants.TipoServicio });

            Assert.Empty(rows);
        }

        [Fact]
        public async Task ConsultarFiltrosCatalogo_CheckAppUsaFuentesReales()
        {
            Fixture fixture = Fixture.Base();

            IReadOnlyList<ListaPreciosConsultaRowDto> rows = await fixture.Service.ConsultarAsync(Empresa, new ListaPreciosConsultaRequest
            {
                IdCategoria = CategoriaProducto,
                IdMarca = MarcaProducto,
                IdColeccion = ColeccionProducto,
                IdEtiqueta = EtiquetaProducto,
                IdAtributo = AtributoProducto
            });

            Assert.NotEmpty(rows);
            Assert.All(rows, row =>
            {
                Assert.Equal(Producto, row.IdProductoServicio);
                Assert.Equal(CategoriaProducto, row.IdCategoria);
                Assert.Equal(MarcaProducto, row.IdMarca);
                Assert.Equal(ColeccionProducto, row.IdColeccion);
            });
        }

        [Fact]
        public async Task ConsultarValorAtributo_RequiereAtributoYFiltraServerSide()
        {
            Fixture fixture = Fixture.Base();
            InvalidOperationException missingAttribute = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.ConsultarAsync(Empresa, new ListaPreciosConsultaRequest
            {
                IdsValoresAtributo = new[] { AtributoValorProducto }
            }));
            Assert.Equal("ATRIBUTO_REQUERIDO", missingAttribute.Message);

            IReadOnlyList<ListaPreciosConsultaRowDto> rows = await fixture.Service.ConsultarAsync(Empresa, new ListaPreciosConsultaRequest
            {
                IdAtributo = AtributoProducto,
                IdsValoresAtributo = new[] { AtributoValorProducto }
            });
            Assert.NotEmpty(rows);
            Assert.All(rows, row => Assert.Equal(Producto, row.IdProductoServicio));
        }

        [Fact]
        public async Task ConsultarFiltroVariante_DevuelveSoloIdentidadVariante()
        {
            Fixture fixture = Fixture.Base();

            IReadOnlyList<ListaPreciosConsultaRowDto> rows = await fixture.Service.ConsultarAsync(Empresa, new ListaPreciosConsultaRequest { IdVariante = Variante });

            ListaPreciosConsultaRowDto row = Assert.Single(rows);
            Assert.Equal(ListaPreciosConstants.IdentidadVariante, row.TipoIdentidad);
            Assert.Equal(Variante, row.IdVariante);
            Assert.Null(row.IdPresentacionVenta);
        }

        [Fact]
        public async Task ConsultarFiltroPresentacionVenta_DevuelveSoloIdentidadPresentacion()
        {
            Fixture fixture = Fixture.Base();

            IReadOnlyList<ListaPreciosConsultaRowDto> rows = await fixture.Service.ConsultarAsync(Empresa, new ListaPreciosConsultaRequest { IdPresentacionVenta = Presentacion });

            ListaPreciosConsultaRowDto row = Assert.Single(rows);
            Assert.Equal(ListaPreciosConstants.IdentidadPresentacionVenta, row.TipoIdentidad);
            Assert.Equal(Presentacion, row.IdPresentacionVenta);
            Assert.Null(row.IdVariante);
        }

        [Fact]
        public async Task ConsultarProductoConFoto_ExponeImagenPrincipalProducto()
        {
            Fixture fixture = Fixture.Base();

            ListaPreciosConsultaRowDto row = (await fixture.Service.ConsultarAsync(Empresa, new ListaPreciosConsultaRequest()))
                .Single(item => item.TipoIdentidad == ListaPreciosConstants.IdentidadProducto);

            Assert.Equal(ProductoImagenUrl, row.ImagenUrl);
            Assert.Equal("Producto principal", row.ImagenNombre);
            Assert.Equal("Producto", row.ImagenOrigen);
        }

        [Fact]
        public async Task ConsultarVarianteConFoto_UsaImagenPropia()
        {
            Fixture fixture = Fixture.Base();

            ListaPreciosConsultaRowDto row = (await fixture.Service.ConsultarAsync(Empresa, new ListaPreciosConsultaRequest { IdVariante = Variante }))
                .Single();

            Assert.Equal(VarianteImagenUrl, row.ImagenUrl);
            Assert.Equal("Variante roja", row.ImagenNombre);
            Assert.Equal("Variante", row.ImagenOrigen);
        }

        [Fact]
        public async Task ConsultarVarianteSinFoto_UsaFallbackProducto()
        {
            Fixture fixture = Fixture.Base();
            fixture.Repository.VarianteImagenUrl = string.Empty;
            fixture.Repository.VarianteImagenNombre = string.Empty;

            ListaPreciosConsultaRowDto row = (await fixture.Service.ConsultarAsync(Empresa, new ListaPreciosConsultaRequest { IdVariante = Variante }))
                .Single();

            Assert.Equal(ProductoImagenUrl, row.ImagenUrl);
            Assert.Equal("Producto principal", row.ImagenNombre);
            Assert.Equal("Producto", row.ImagenOrigen);
        }

        [Fact]
        public async Task ConsultarPresentacionVenta_UsaFallbackProductoPorqueNoExisteImagenPropia()
        {
            Fixture fixture = Fixture.Base();

            ListaPreciosConsultaRowDto row = (await fixture.Service.ConsultarAsync(Empresa, new ListaPreciosConsultaRequest { IdPresentacionVenta = Presentacion }))
                .Single();

            Assert.Equal(ProductoImagenUrl, row.ImagenUrl);
            Assert.Equal("Producto principal", row.ImagenNombre);
            Assert.Equal("Producto", row.ImagenOrigen);
        }

        [Fact]
        public async Task ConsultarServicioSinFoto_ExponeFallbackVacio()
        {
            Fixture fixture = Fixture.Base();

            ListaPreciosConsultaRowDto row = (await fixture.Service.ConsultarAsync(Empresa, new ListaPreciosConsultaRequest { Tipo = ListaPreciosConstants.TipoServicio }))
                .Single();

            Assert.Equal(string.Empty, row.ImagenUrl);
            Assert.Equal(string.Empty, row.ImagenNombre);
            Assert.Equal(string.Empty, row.ImagenOrigen);
        }

        [Fact]
        public async Task ConsultarServicioConVariante_NoInventaCombinaciones()
        {
            Fixture fixture = Fixture.Base();

            IReadOnlyList<ListaPreciosConsultaRowDto> rows = await fixture.Service.ConsultarAsync(Empresa, new ListaPreciosConsultaRequest
            {
                Tipo = ListaPreciosConstants.TipoServicio,
                IdVariante = Variante
            });

            Assert.Empty(rows);
        }

        [Fact]
        public async Task ConsultarPrecioMinMax_IncluyePrecioCeroValido()
        {
            Fixture fixture = Fixture.Base();
            fixture.AddDetail(1, ListaPreciosConstants.IdentidadProducto, Producto, null, null, 0m);

            IReadOnlyList<ListaPreciosConsultaRowDto> rows = await fixture.Service.ConsultarAsync(Empresa, new ListaPreciosConsultaRequest
            {
                PrecioMinimo = 0m,
                PrecioMaximo = 0m
            });

            Assert.Contains(rows, row => row.IdProductoServicio == Producto && row.PrecioFinal == 0m);
        }

        [Fact]
        public async Task ConsultarPrecioMinMayorMax_RechazaControlado()
        {
            Fixture fixture = Fixture.Base();

            InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.ConsultarAsync(Empresa, new ListaPreciosConsultaRequest
            {
                PrecioMinimo = 100m,
                PrecioMaximo = 10m
            }));

            Assert.Equal("RANGO_PRECIO_INVALIDO", error.Message);
        }

        [Fact]
        public async Task ConsultarDescuento_FiltraPorDescuentoExacto()
        {
            Fixture fixture = Fixture.Base();
            fixture.AddDetail(1, ListaPreciosConstants.IdentidadProducto, Producto, null, null, 100m, descuentoPct: 10m);
            fixture.AddDetail(1, ListaPreciosConstants.IdentidadServicio, Servicio, null, null, 40m, descuentoPct: 0m);

            IReadOnlyList<ListaPreciosConsultaRowDto> rows = await fixture.Service.ConsultarAsync(Empresa, new ListaPreciosConsultaRequest { DescuentoPct = 10m });

            Assert.Single(rows);
            Assert.Equal(10m, rows[0].DescuentoPct);
            Assert.Equal(Producto, rows[0].IdProductoServicio);
        }

        [Fact]
        public async Task ConsultarDescuentoTriestado_DistingueConYSinDescuento()
        {
            Fixture fixture = Fixture.Base();
            fixture.AddDetail(1, ListaPreciosConstants.IdentidadProducto, Producto, null, null, 100m, descuentoPct: 10m);
            fixture.AddDetail(1, ListaPreciosConstants.IdentidadServicio, Servicio, null, null, 40m, descuentoPct: 0m);

            IReadOnlyList<ListaPreciosConsultaRowDto> withDiscount = await fixture.Service.ConsultarAsync(Empresa, new ListaPreciosConsultaRequest { Descuento = "con" });
            IReadOnlyList<ListaPreciosConsultaRowDto> withoutDiscount = await fixture.Service.ConsultarAsync(Empresa, new ListaPreciosConsultaRequest { Descuento = "sin" });

            Assert.Contains(withDiscount, row => row.IdProductoServicio == Producto && row.DescuentoPct == 10m);
            Assert.DoesNotContain(withDiscount, row => row.DescuentoPct.GetValueOrDefault() == 0m);
            Assert.Contains(withoutDiscount, row => row.IdProductoServicio == Servicio && row.DescuentoPct == 0m);
        }

        [Fact]
        public async Task ConsultarMatriz_ConservaCeroConfiguradoYNullAusente()
        {
            Fixture fixture = Fixture.Base();
            fixture.AddDetail(1, ListaPreciosConstants.IdentidadProducto, Producto, null, null, 0m, descuentoPct: 0m);

            ListaPreciosConsultaRowDto row = Assert.Single((await fixture.Service.ConsultarAsync(Empresa, new ListaPreciosConsultaRequest { IdCategoria = CategoriaProducto }))
                .Where(item => item.TipoIdentidad == ListaPreciosConstants.IdentidadProducto));

            Assert.Equal(0m, row.P1);
            Assert.Equal(0m, row.D1);
            Assert.Null(row.P2);
            Assert.Null(row.D2);
        }

        [Fact]
        public async Task GuardarMatriz_PersisteCambiosConUnCorrelationId()
        {
            Fixture fixture = Fixture.Base();
            ListaPreciosMatrizPreviewDto result = await fixture.Service.GuardarMatrizAsync(Empresa, new ListaPreciosMatrizRequest
            {
                TipoIdentidad = ListaPreciosConstants.IdentidadProducto,
                IdProductoServicio = Producto,
                Motivo = "QA matriz",
                Listas = new[]
                {
                    new ListaPreciosMatrizItemRequest { Nivel = 1, Precio = 0m, DescuentoPct = 0m },
                    new ListaPreciosMatrizItemRequest { Nivel = 2, Precio = 125m, DescuentoPct = 10m }
                }
            });

            Assert.True(result.Persistido);
            Assert.Equal(2, fixture.Repository.LastBulkCommands.Count);
            Assert.Single(fixture.Repository.LastBulkCommands.Select(command => command.CorrelationId).Distinct());
            Assert.Contains(fixture.Repository.Detalles.Values, detail => detail.Nivel == 1 && detail.Precio == 0m);
            Assert.Contains(fixture.Repository.Detalles.Values, detail => detail.Nivel == 2 && detail.DescuentoPct == 10m);
        }

        [Fact]
        public async Task ConsultarDescuentoFueraRango_Rechaza()
        {
            Fixture fixture = Fixture.Base();

            InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.ConsultarAsync(Empresa, new ListaPreciosConsultaRequest { DescuentoPct = 101m }));

            Assert.Equal(ListaPreciosResolutionCodes.DescuentoInvalido, error.Message);
        }

        [Fact]
        public async Task Lp13_ConsultaExponeExistenciaPorIdentidadSinNMasUno()
        {
            Fixture fixture = Fixture.Base();
            fixture.Repository.Existencias[(Producto, null, SucursalCentro)] = 8m;
            fixture.Repository.Existencias[(Producto, Variante, SucursalCentro)] = 3m;

            IReadOnlyList<ListaPreciosConsultaRowDto> rows = await fixture.Service.ConsultarAsync(Empresa, new ListaPreciosConsultaRequest());

            Assert.Equal(1, fixture.Repository.ConsultaCalls);
            Assert.Equal(8m, rows.Single(x => x.TipoIdentidad == ListaPreciosConstants.IdentidadProducto).Existencia);
            Assert.Equal(3m, rows.Single(x => x.TipoIdentidad == ListaPreciosConstants.IdentidadVariante).Existencia);
            Assert.Equal(8m, rows.Single(x => x.TipoIdentidad == ListaPreciosConstants.IdentidadPresentacionVenta).Existencia);
            Assert.False(rows.Single(x => x.TipoIdentidad == ListaPreciosConstants.IdentidadServicio).InventarioAplicable);
            Assert.Null(rows.Single(x => x.TipoIdentidad == ListaPreciosConstants.IdentidadServicio).Existencia);
        }

        [Fact]
        public async Task Lp13_FiltrosSucursalExistenciaYCantidadSeAplicanEnBackend()
        {
            Fixture fixture = Fixture.Base();
            fixture.Repository.Existencias[(Producto, null, SucursalCentro)] = 8m;
            fixture.Repository.Existencias[(Producto, null, SucursalNorte)] = 2m;
            fixture.Repository.Existencias[(Producto, Variante, SucursalCentro)] = 0m;

            IReadOnlyList<ListaPreciosConsultaRowDto> conExistencia = await fixture.Service.ConsultarAsync(Empresa, new ListaPreciosConsultaRequest
            {
                IdSucursal = SucursalCentro,
                Existencia = "con",
                CantidadMenorA = 9m
            });

            Assert.Contains(conExistencia, x => x.TipoIdentidad == ListaPreciosConstants.IdentidadProducto && x.Existencia == 8m);
            Assert.DoesNotContain(conExistencia, x => x.TipoIdentidad == ListaPreciosConstants.IdentidadVariante);
            Assert.DoesNotContain(conExistencia, x => x.TipoIdentidad == ListaPreciosConstants.IdentidadServicio);
        }

        [Fact]
        public async Task Lp13_DetalleMovimientosRespetaIdentidadYSucursal()
        {
            Fixture fixture = Fixture.Base();
            fixture.Repository.Existencias[(Producto, Variante, SucursalCentro)] = 3m;
            fixture.Repository.Movimientos.Add(new ListaPreciosInventarioMovimientoDto(
                Guid.NewGuid(), SucursalCentro, "Centro", 1, "Entrada", 3m, 0m, 3m, "RECEPCION", "Ingreso", DateTime.UtcNow));

            ListaPreciosInventarioDetalleDto detail = await fixture.Service.ObtenerInventarioDetalleAsync(Empresa, Request(Producto, tipo: ListaPreciosConstants.IdentidadVariante, variante: Variante));

            Assert.True(detail.Aplicable);
            Assert.Equal(3m, detail.ExistenciaTotal);
            Assert.Single(detail.Saldos);
            Assert.Single(detail.Movimientos);
            Assert.Equal("RECEPCION", detail.Movimientos[0].Referencia);
        }

        [Fact]
        public async Task Lp13_ServicioDevuelveNoAplicableSinConsultarMovimientos()
        {
            Fixture fixture = Fixture.Base();

            ListaPreciosInventarioDetalleDto detail = await fixture.Service.ObtenerInventarioDetalleAsync(Empresa, Request(Servicio, tipo: ListaPreciosConstants.IdentidadServicio));

            Assert.False(detail.Aplicable);
            Assert.Equal(0, fixture.Repository.DetalleInventarioCalls);
        }

        [Fact]
        public async Task Lp13_CrossTenantFallaCerrado()
        {
            Fixture fixture = Fixture.Base();

            await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.ObtenerInventarioDetalleAsync(OtraEmpresa, Request(Producto)));
            Assert.Equal(0, fixture.Repository.DetalleInventarioCalls);
        }

        [Fact]
        public async Task ProductoConPrecioLista_ResuelvePrecioEspecifico()
        {
            Fixture fixture = Fixture.Base();
            fixture.AddDetail(1, ListaPreciosConstants.IdentidadProducto, Producto, null, null, 150m);

            ListaPreciosResolutionResult result = await fixture.Service.ResolverPrecioAsync(Empresa, Request(Producto, nivel: 1));

            Assert.True(result.Resuelto);
            Assert.Equal(150m, result.PrecioEfectivo);
            Assert.Equal(ListaPreciosResolutionCodes.PrecioLista, result.CodigoResolucion);
            Assert.Equal(150m, result.PrecioFinal);
            Assert.Equal(150m, result.PrecioLista);
            Assert.Null(result.DescuentoPct);
            Assert.Equal(ListaPreciosConstants.RedondeoSinRedondeo, result.RedondeoModo);
        }

        [Fact]
        public async Task Lp03Compatibilidad_SinDescuentoNiRedondeo_MantienePrecioEfectivoAnterior()
        {
            Fixture fixture = Fixture.Base();
            fixture.AddDetail(1, ListaPreciosConstants.IdentidadProducto, Producto, null, null, 150m, descuentoPct: null, redondeoModo: 0);

            ListaPreciosResolutionResult result = await fixture.Service.ResolverPrecioAsync(Empresa, Request(Producto, nivel: 1));

            Assert.Equal(150m, result.PrecioEfectivo);
            Assert.Equal(result.PrecioFinal, result.PrecioEfectivo);
            Assert.Equal(ListaPreciosConstants.ReglaVersionLp08, result.ReglaVersion);
        }

        [Fact]
        public async Task DescuentoAntesDeRedondeo_A49_ClampaAPrecioBaseCuandoExcede()
        {
            Fixture fixture = Fixture.Base();
            fixture.AddDetail(1, ListaPreciosConstants.IdentidadProducto, Producto, null, null, 100m, descuentoPct: 10m, redondeoModo: ListaPreciosConstants.RedondeoA49);

            ListaPreciosResolutionResult result = await fixture.Service.ResolverPrecioAsync(Empresa, Request(Producto, nivel: 1));

            Assert.Equal(90m, result.SubtotalAntesRedondeo);
            Assert.Equal(94m, result.PrecioFinal);
            Assert.Equal(94m, result.PrecioEfectivo);
        }

        [Fact]
        public async Task RedondeoSolo9_AplicaAlgoritmoCanonico()
        {
            Fixture fixture = Fixture.Base();
            fixture.AddDetail(1, ListaPreciosConstants.IdentidadProducto, Producto, null, null, 100m, descuentoPct: 10m, redondeoModo: ListaPreciosConstants.RedondeoSolo9);

            ListaPreciosResolutionResult result = await fixture.Service.ResolverPrecioAsync(Empresa, Request(Producto, nivel: 1));

            Assert.Equal(90m, result.SubtotalAntesRedondeo);
            Assert.Equal(99m, result.PrecioFinal);
        }

        [Fact]
        public async Task DescuentoCien_DejaPrecioFinalCero()
        {
            Fixture fixture = Fixture.Base();
            fixture.AddDetail(1, ListaPreciosConstants.IdentidadProducto, Producto, null, null, 100m, descuentoPct: 100m);

            ListaPreciosResolutionResult result = await fixture.Service.ResolverPrecioAsync(Empresa, Request(Producto, nivel: 1));

            Assert.Equal(0m, result.SubtotalAntesRedondeo);
            Assert.Equal(0m, result.PrecioFinal);
        }

        [Fact]
        public async Task ProductoSinPrecioLista_UsaPrecioPublico()
        {
            Fixture fixture = Fixture.Base();

            ListaPreciosResolutionResult result = await fixture.Service.ResolverPrecioAsync(Empresa, Request(Producto, nivel: 1));

            Assert.True(result.Resuelto);
            Assert.Equal(100m, result.PrecioEfectivo);
            Assert.Equal(ListaPreciosResolutionCodes.FallbackPrecioPublico, result.Origen);
        }

        [Fact]
        public async Task ServicioConPrecioLista_ResuelvePrecioEspecifico()
        {
            Fixture fixture = Fixture.Base();
            fixture.AddDetail(1, ListaPreciosConstants.IdentidadServicio, Servicio, null, null, 80m);

            ListaPreciosResolutionResult result = await fixture.Service.ResolverPrecioAsync(Empresa, Request(Servicio, nivel: 1));

            Assert.Equal(80m, result.PrecioEfectivo);
            Assert.Equal(ListaPreciosResolutionCodes.PrecioLista, result.CodigoResolucion);
        }

        [Fact]
        public async Task ServicioSinPrecioLista_UsaPrecioPublico()
        {
            Fixture fixture = Fixture.Base();

            ListaPreciosResolutionResult result = await fixture.Service.ResolverPrecioAsync(Empresa, Request(Servicio, nivel: 1));

            Assert.Equal(40m, result.PrecioEfectivo);
            Assert.Equal(ListaPreciosResolutionCodes.FallbackPrecioPublico, result.Origen);
        }

        [Fact]
        public async Task ServicioConVariante_FallaCerrado()
        {
            Fixture fixture = Fixture.Base();

            ListaPreciosResolutionResult result = await fixture.Service.ResolverPrecioAsync(Empresa, Request(Servicio, variante: Variante));

            Assert.False(result.Resuelto);
            Assert.Equal(ListaPreciosResolutionCodes.IdentidadInvalida, result.CodigoResolucion);
        }

        [Fact]
        public async Task VarianteConPrecioLista_ResuelvePrecioEspecifico()
        {
            Fixture fixture = Fixture.Base();
            fixture.AddDetail(1, ListaPreciosConstants.IdentidadVariante, Producto, Variante, null, 155m);

            ListaPreciosResolutionResult result = await fixture.Service.ResolverPrecioAsync(Empresa, Request(Producto, variante: Variante));

            Assert.Equal(155m, result.PrecioEfectivo);
            Assert.Equal(ListaPreciosResolutionCodes.PrecioLista, result.CodigoResolucion);
        }

        [Fact]
        public async Task VarianteSinPrecioLista_UsaPrecioPublicoDeVariante()
        {
            Fixture fixture = Fixture.Base();

            ListaPreciosResolutionResult result = await fixture.Service.ResolverPrecioAsync(Empresa, Request(Producto, variante: Variante));

            Assert.Equal(120m, result.PrecioEfectivo);
            Assert.Equal(ListaPreciosResolutionCodes.FallbackPrecioPublico, result.Origen);
        }

        [Fact]
        public async Task VarianteSinPrecioPublico_UsaPrecioPublicoDeProducto()
        {
            Fixture fixture = Fixture.Base();
            fixture.Repository.Variantes[Variante] = fixture.Repository.Variantes[Variante] with { PrecioPublico = null };

            ListaPreciosResolutionResult result = await fixture.Service.ResolverPrecioAsync(Empresa, Request(Producto, variante: Variante));

            Assert.Equal(100m, result.PrecioEfectivo);
            Assert.Equal(ListaPreciosResolutionCodes.FallbackPrecioPublico, result.Origen);
        }

        [Fact]
        public async Task VarianteDeOtroProducto_FallaCerrado()
        {
            Fixture fixture = Fixture.Base();
            fixture.Repository.Variantes[Variante] = fixture.Repository.Variantes[Variante] with { IdProductoServicio = Servicio };

            ListaPreciosResolutionResult result = await fixture.Service.ResolverPrecioAsync(Empresa, Request(Producto, variante: Variante));

            Assert.False(result.Resuelto);
            Assert.Equal(ListaPreciosResolutionCodes.IdentidadInvalida, result.CodigoResolucion);
        }

        [Fact]
        public async Task VarianteInactiva_FallaCerradoAunqueRequestPidaNoActivo()
        {
            Fixture fixture = Fixture.Base();
            fixture.Repository.Variantes[Variante] = fixture.Repository.Variantes[Variante] with { Activo = false };

            ListaPreciosResolutionResult result = await fixture.Service.ResolverPrecioAsync(Empresa, Request(Producto, variante: Variante, requiereActivo: false));

            Assert.False(result.Resuelto);
            Assert.Equal(ListaPreciosResolutionCodes.IdentidadInactiva, result.CodigoResolucion);
        }

        [Fact]
        public async Task PresentacionConPrecioLista_ResuelvePrecioEspecifico()
        {
            Fixture fixture = Fixture.Base();
            fixture.AddDetail(1, ListaPreciosConstants.IdentidadPresentacionVenta, Producto, null, Presentacion, 12m);

            ListaPreciosResolutionResult result = await fixture.Service.ResolverPrecioAsync(Empresa, Request(Producto, presentacion: Presentacion));

            Assert.Equal(12m, result.PrecioEfectivo);
            Assert.Equal(ListaPreciosResolutionCodes.PrecioLista, result.CodigoResolucion);
        }

        [Fact]
        public async Task PresentacionSinPrecioLista_UsaPrecioDePresentacion()
        {
            Fixture fixture = Fixture.Base();

            ListaPreciosResolutionResult result = await fixture.Service.ResolverPrecioAsync(Empresa, Request(Producto, presentacion: Presentacion));

            Assert.Equal(10m, result.PrecioEfectivo);
            Assert.Equal(ListaPreciosResolutionCodes.FallbackPrecioPublico, result.Origen);
        }

        [Fact]
        public async Task PresentacionDeOtroProducto_FallaCerrado()
        {
            Fixture fixture = Fixture.Base();
            fixture.Repository.Presentaciones[Presentacion] = fixture.Repository.Presentaciones[Presentacion] with { IdProductoServicio = Servicio };

            ListaPreciosResolutionResult result = await fixture.Service.ResolverPrecioAsync(Empresa, Request(Producto, presentacion: Presentacion));

            Assert.False(result.Resuelto);
            Assert.Equal(ListaPreciosResolutionCodes.IdentidadInvalida, result.CodigoResolucion);
        }

        [Fact]
        public async Task VarianteMasPresentacion_EsFueraDeV1()
        {
            Fixture fixture = Fixture.Base();

            ListaPreciosResolutionResult result = await fixture.Service.ResolverPrecioAsync(Empresa, Request(Producto, variante: Variante, presentacion: Presentacion));

            Assert.False(result.Resuelto);
            Assert.Equal(ListaPreciosResolutionCodes.FueraDeV1, result.CodigoResolucion);
        }

        [Fact]
        public async Task TenantAjeno_NoCruzaIdentidades()
        {
            Fixture fixture = Fixture.Base();

            ListaPreciosResolutionResult result = await fixture.Service.ResolverPrecioAsync(OtraEmpresa, Request(Producto, variante: Variante));

            Assert.False(result.Resuelto);
            Assert.Equal(ListaPreciosResolutionCodes.IdentidadInvalida, result.CodigoResolucion);
        }

        [Fact]
        public async Task ListaInvalida_UsaNivelDefaultSinBuscarPrioridades()
        {
            Fixture fixture = Fixture.Base();
            fixture.AddDetail(2, ListaPreciosConstants.IdentidadProducto, Producto, null, null, 200m);

            ListaPreciosResolutionResult result = await fixture.Service.ResolverPrecioAsync(Empresa, Request(Producto, nivel: 99));

            Assert.False(result.Resuelto);
            Assert.Equal(ListaPreciosResolutionCodes.ListaInvalida, result.CodigoResolucion);
        }

        [Fact]
        public async Task ListaSeleccionadaSinPrecio_NoBuscaOtroNivel()
        {
            Fixture fixture = Fixture.Base();
            fixture.AddDetail(1, ListaPreciosConstants.IdentidadProducto, Producto, null, null, 150m);

            ListaPreciosResolutionResult result = await fixture.Service.ResolverPrecioAsync(Empresa, Request(Producto, nivel: 2));

            Assert.Equal(2, result.ListaEfectiva);
            Assert.Equal(100m, result.PrecioEfectivo);
            Assert.Equal(ListaPreciosResolutionCodes.FallbackPrecioPublico, result.Origen);
        }

        [Fact]
        public async Task PrecioCeroConfigurado_NoHaceFallback()
        {
            Fixture fixture = Fixture.Base();
            fixture.AddDetail(1, ListaPreciosConstants.IdentidadProducto, Producto, null, null, 0m);

            ListaPreciosResolutionResult result = await fixture.Service.ResolverPrecioAsync(Empresa, Request(Producto, nivel: 1));

            Assert.True(result.Resuelto);
            Assert.Equal(0m, result.PrecioEfectivo);
            Assert.Equal(ListaPreciosResolutionCodes.PrecioLista, result.CodigoResolucion);
        }

        [Fact]
        public async Task DetalleFueraDeVigencia_HaceFallbackContractual()
        {
            Fixture fixture = Fixture.Base();
            fixture.AddDetail(1, ListaPreciosConstants.IdentidadProducto, Producto, null, null, 150m, vigenciaInicio: DateTime.UtcNow.Date.AddDays(1));

            ListaPreciosResolutionResult result = await fixture.Service.ResolverPrecioAsync(Empresa, Request(Producto, nivel: 1));

            Assert.Equal(100m, result.PrecioEfectivo);
            Assert.Equal(ListaPreciosResolutionCodes.FallbackPrecioPublico, result.Origen);
        }

        [Fact]
        public async Task GuardarPrecioNegativo_Rechaza()
        {
            Fixture fixture = Fixture.Base();

            InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.GuardarPrecioAsync(Empresa, Save(Producto, -1m)));

            Assert.Equal("PRECIO_NEGATIVO", error.Message);
        }

        [Theory]
        [InlineData(-0.01)]
        [InlineData(100.01)]
        public async Task GuardarDescuentoFueraDeRango_Rechaza(decimal descuento)
        {
            Fixture fixture = Fixture.Base();

            ListaPreciosGuardarPrecioRequest request = Save(Producto, 100m);
            request.DescuentoPct = descuento;

            InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.GuardarPrecioAsync(Empresa, request));

            Assert.Equal(ListaPreciosResolutionCodes.DescuentoInvalido, error.Message);
        }

        [Fact]
        public async Task GuardarVigenciaInicioMayorAFin_Rechaza()
        {
            Fixture fixture = Fixture.Base();

            ListaPreciosGuardarPrecioRequest request = Save(Producto, 100m);
            request.VigenciaInicio = DateTime.UtcNow.Date.AddDays(1);
            request.VigenciaFin = DateTime.UtcNow.Date;

            InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.GuardarPrecioAsync(Empresa, request));

            Assert.Equal(ListaPreciosResolutionCodes.VigenciaInvalida, error.Message);
        }

        [Fact]
        public async Task PreviewCalculaSinPersistir()
        {
            Fixture fixture = Fixture.Base();

            ListaPreciosResolutionResult result = await fixture.Service.PreviewAsync(Empresa, new ListaPreciosPreviewRequest
            {
                Nivel = 1,
                IdProductoServicio = Producto,
                Precio = 100m,
                DescuentoPct = 50m,
                RedondeoModo = ListaPreciosConstants.RedondeoSolo9
            });

            Assert.Equal(59m, result.PrecioFinal);
            Assert.Empty(fixture.Repository.Detalles);
        }

        [Fact]
        public async Task EnsureLista_CreaCabeceraSinDetalleYEsIdempotente()
        {
            Fixture fixture = Fixture.Base();

            Guid first = await fixture.Service.EnsureListaAsync(Empresa, 3, null);
            Guid second = await fixture.Service.EnsureListaAsync(Empresa, 3, null);

            Assert.Equal(first, second);
            Assert.Equal(first, fixture.Repository.Listas[3].Id);
            Assert.Empty(fixture.Repository.Detalles);
        }

        [Fact]
        public async Task EnsureLista_ReutilizaCabeceraActiva()
        {
            Fixture fixture = Fixture.Base();
            Guid expected = fixture.Repository.Listas[2].Id;

            Guid actual = await fixture.Service.EnsureListaAsync(Empresa, 2, null);

            Assert.Equal(expected, actual);
            Assert.Single(fixture.Repository.Listas.Where(pair => pair.Key == 2));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(11)]
        public async Task EnsureLista_NivelFueraDeRango_Rechaza(int nivel)
        {
            Fixture fixture = Fixture.Base();

            InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                fixture.Service.EnsureListaAsync(Empresa, nivel, null));

            Assert.Equal(ListaPreciosResolutionCodes.ListaInvalida, error.Message);
        }

        [Fact]
        public async Task EnsureLista_CrossTenant_FallaCerrado()
        {
            Fixture fixture = Fixture.Base();

            InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                fixture.Service.EnsureListaAsync(OtraEmpresa, 2, null));

            Assert.Equal(ListaPreciosResolutionCodes.ListaInvalida, error.Message);
        }

        [Fact]
        public async Task GuardarPrecioReactivaDetalleArchivado()
        {
            Fixture fixture = Fixture.Base();
            Guid id = fixture.AddDetail(1, ListaPreciosConstants.IdentidadProducto, Producto, null, null, 150m, active: false);

            Guid saved = await fixture.Service.GuardarPrecioAsync(Empresa, Save(Producto, 175m));

            Assert.Equal(id, saved);
            ListaPreciosResolutionResult result = await fixture.Service.ResolverPrecioAsync(Empresa, Request(Producto, nivel: 1));
            Assert.Equal(175m, result.PrecioEfectivo);
        }

        [Fact]
        public async Task BajaPrecio_LogicaHaceFallback()
        {
            Fixture fixture = Fixture.Base();
            Guid id = fixture.AddDetail(1, ListaPreciosConstants.IdentidadProducto, Producto, null, null, 150m);

            await fixture.Service.BajaPrecioAsync(Empresa, id, null);
            ListaPreciosResolutionResult result = await fixture.Service.ResolverPrecioAsync(Empresa, Request(Producto, nivel: 1));

            Assert.Equal(100m, result.PrecioEfectivo);
            Assert.Equal(ListaPreciosResolutionCodes.FallbackPrecioPublico, result.Origen);
        }

        [Fact]
        public async Task DuplicidadActiva_RechazaGuardar()
        {
            Fixture fixture = Fixture.Base();
            fixture.Repository.ForceDuplicate = true;

            InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.GuardarPrecioAsync(Empresa, Save(Producto, 175m)));

            Assert.Equal(ListaPreciosResolutionCodes.Duplicado, error.Message);
        }

        [Fact]
        public async Task TipoIdentidadInconsistenteConLlaves_FallaCerrado()
        {
            Fixture fixture = Fixture.Base();

            ListaPreciosResolutionResult result = await fixture.Service.ResolverPrecioAsync(Empresa, Request(Producto, tipo: ListaPreciosConstants.IdentidadProducto, variante: Variante));

            Assert.False(result.Resuelto);
            Assert.Equal(ListaPreciosResolutionCodes.IdentidadInvalida, result.CodigoResolucion);
        }

        [Fact]
        public async Task Lp14_PreviewValorExacto_NoPersiste()
        {
            Fixture fixture = Fixture.Base();
            ListaPreciosAjusteMasivoResultadoDto result = await fixture.Service.PreviewAjusteMasivoAsync(Empresa, Bulk("PRECIO", "VALOR", "ASIGNAR", 125m, Identity(Producto, ListaPreciosConstants.IdentidadProducto)));
            Assert.Equal(125m, Assert.Single(result.Items).ValorPropuesto);
            Assert.False(result.Persistido);
            Assert.Empty(fixture.Repository.Detalles);
        }

        [Theory]
        [InlineData("MONTO", "SUMAR", 10, 110)]
        [InlineData("MONTO", "RESTAR", 10, 90)]
        [InlineData("PORCENTAJE", "SUMAR", 10, 110)]
        [InlineData("PORCENTAJE", "RESTAR", 10, 90)]
        public async Task Lp14_AjustesPrecio_CalculanEnBackend(string type, string operation, decimal value, decimal expected)
        {
            Fixture fixture = Fixture.Base();
            ListaPreciosAjusteMasivoResultadoDto result = await fixture.Service.PreviewAjusteMasivoAsync(Empresa, Bulk("PRECIO", type, operation, value, Identity(Producto, ListaPreciosConstants.IdentidadProducto)));
            Assert.Equal(expected, Assert.Single(result.Items).ValorPropuesto);
        }

        [Fact]
        public async Task Lp14_DescuentoValido_ConservaPrecioYCalculaFinal()
        {
            Fixture fixture = Fixture.Base();
            fixture.AddDetail(1, ListaPreciosConstants.IdentidadProducto, Producto, null, null, 100m, descuentoPct: 5m);
            ListaPreciosAjusteMasivoResultadoDto result = await fixture.Service.PreviewAjusteMasivoAsync(Empresa, Bulk("DESCUENTO", "MONTO", "SUMAR", 5m, Identity(Producto, ListaPreciosConstants.IdentidadProducto)));
            ListaPreciosAjusteMasivoItemDto item = Assert.Single(result.Items);
            Assert.Equal(10m, item.DescuentoPct);
            Assert.Equal(90m, item.PrecioFinal);
        }

        [Fact]
        public async Task Lp14_DescuentoInvalido_RechazaSinPersistencia()
        {
            Fixture fixture = Fixture.Base();
            ListaPreciosAjusteMasivoResultadoDto result = await fixture.Service.PreviewAjusteMasivoAsync(Empresa, Bulk("DESCUENTO", "VALOR", "ASIGNAR", 101m, Identity(Producto, ListaPreciosConstants.IdentidadProducto)));
            Assert.Equal(ListaPreciosResolutionCodes.DescuentoInvalido, Assert.Single(result.Items).Codigo);
            Assert.Equal(0, result.Aceptados);
        }

        [Fact]
        public async Task Lp14_SeleccionVacia_Rechaza()
        {
            Fixture fixture = Fixture.Base();
            InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.PreviewAjusteMasivoAsync(Empresa, Bulk("PRECIO", "VALOR", "ASIGNAR", 10m)));
            Assert.Equal("SELECCION_VACIA", error.Message);
        }

        [Fact]
        public async Task Lp14_ListaInvalida_Rechaza()
        {
            Fixture fixture = Fixture.Base();
            ListaPreciosAjusteMasivoRequest request = Bulk("PRECIO", "VALOR", "ASIGNAR", 10m, Identity(Producto, ListaPreciosConstants.IdentidadProducto));
            request.Nivel = 11;
            InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.PreviewAjusteMasivoAsync(Empresa, request));
            Assert.Equal(ListaPreciosResolutionCodes.ListaInvalida, error.Message);
        }

        [Fact]
        public async Task Lp14_SoportaCuatroIdentidadesCanonicas()
        {
            Fixture fixture = Fixture.Base();
            ListaPreciosAjusteMasivoResultadoDto result = await fixture.Service.PreviewAjusteMasivoAsync(Empresa, Bulk("PRECIO", "VALOR", "ASIGNAR", 77m,
                Identity(Producto, ListaPreciosConstants.IdentidadProducto), Identity(Servicio, ListaPreciosConstants.IdentidadServicio),
                Identity(Producto, ListaPreciosConstants.IdentidadVariante, Variante), Identity(Producto, ListaPreciosConstants.IdentidadPresentacionVenta, presentacion: Presentacion)));
            Assert.Equal(4, result.Aceptados);
            Assert.All(result.Items, item => Assert.Equal(77m, item.ValorPropuesto));
        }

        [Fact]
        public async Task Lp14_DuplicadoEnSeleccion_RechazaEjecucionCompleta()
        {
            Fixture fixture = Fixture.Base();
            ListaPreciosAjusteMasivoIdentidadRequest identity = Identity(Producto, ListaPreciosConstants.IdentidadProducto);
            InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.EjecutarAjusteMasivoAsync(Empresa, Bulk("PRECIO", "VALOR", "ASIGNAR", 77m, identity, identity)));
            Assert.Equal(ListaPreciosResolutionCodes.Duplicado, error.Message);
            Assert.Empty(fixture.Repository.Detalles);
        }

        [Fact]
        public async Task Lp14_CrossTenant_FallaCerrado()
        {
            Fixture fixture = Fixture.Base();
            ListaPreciosAjusteMasivoResultadoDto preview = await fixture.Service.PreviewAjusteMasivoAsync(OtraEmpresa, Bulk("PRECIO", "VALOR", "ASIGNAR", 77m, Identity(Producto, ListaPreciosConstants.IdentidadProducto)));
            Assert.Equal(ListaPreciosResolutionCodes.IdentidadInvalida, Assert.Single(preview.Items).Codigo);
            await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.EjecutarAjusteMasivoAsync(OtraEmpresa, Bulk("PRECIO", "VALOR", "ASIGNAR", 77m, Identity(Producto, ListaPreciosConstants.IdentidadProducto))));
        }

        [Fact]
        public async Task Lp14_Ejecucion_PersisteAtomicamenteConOrigenMasivo()
        {
            Fixture fixture = Fixture.Base();
            ListaPreciosAjusteMasivoResultadoDto result = await fixture.Service.EjecutarAjusteMasivoAsync(Empresa, Bulk("PRECIO", "VALOR", "ASIGNAR", 88m,
                Identity(Producto, ListaPreciosConstants.IdentidadProducto), Identity(Servicio, ListaPreciosConstants.IdentidadServicio)));
            Assert.True(result.Persistido);
            Assert.Equal(2, fixture.Repository.Detalles.Count);
            Assert.All(fixture.Repository.LastBulkCommands, command => Assert.Equal(ListaPreciosConstants.OrigenHistorialMasivo, command.Origen));
            Assert.Single(fixture.Repository.LastBulkCommands.Select(command => command.CorrelationId).Distinct());
        }

        [Fact]
        public async Task Lp14_ErrorDuranteLote_RestauraTodosLosCambios()
        {
            Fixture fixture = Fixture.Base();
            fixture.Repository.FailBulkAt = 2;
            await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.EjecutarAjusteMasivoAsync(Empresa, Bulk("PRECIO", "VALOR", "ASIGNAR", 88m,
                Identity(Producto, ListaPreciosConstants.IdentidadProducto), Identity(Servicio, ListaPreciosConstants.IdentidadServicio))));
            Assert.Empty(fixture.Repository.Detalles);
        }

        [Fact]
        public void Lp14R2_ReactivacionMismosValores_GeneraHistorialActivo()
        {
            ListaPreciosDetalleRecord previous = HistoryDetail(active: false, price: 88m);
            ListaPreciosGuardarPrecioCommand command = HistoryCommand(88m, ListaPreciosConstants.OrigenHistorialMasivo);

            Assert.True(ListaPreciosHistorialPolicy.RequiresReactivationHistory(previous, command));
        }

        [Fact]
        public void Lp14R2_ReactivacionConCambioPrecio_NoDuplicaHistorial()
        {
            ListaPreciosDetalleRecord previous = HistoryDetail(active: false, price: 80m);
            ListaPreciosGuardarPrecioCommand command = HistoryCommand(88m, ListaPreciosConstants.OrigenHistorialMasivo);

            Assert.False(ListaPreciosHistorialPolicy.RequiresReactivationHistory(previous, command));
        }

        [Fact]
        public void Lp14R2_ActivoSinCambio_NoGeneraHistorial()
        {
            ListaPreciosDetalleRecord previous = HistoryDetail(active: true, price: 88m);
            ListaPreciosGuardarPrecioCommand command = HistoryCommand(88m, ListaPreciosConstants.OrigenHistorialMasivo);

            Assert.False(ListaPreciosHistorialPolicy.RequiresReactivationHistory(previous, command));
        }

        [Fact]
        public async Task Lp14R2_LoteReactivaciones_ComparteCorrelationId()
        {
            Fixture fixture = Fixture.Base();
            fixture.AddDetail(1, ListaPreciosConstants.IdentidadProducto, Producto, null, null, 88m, active: false);
            fixture.AddDetail(1, ListaPreciosConstants.IdentidadServicio, Servicio, null, null, 88m, active: false);

            await fixture.Service.EjecutarAjusteMasivoAsync(Empresa, Bulk("PRECIO", "VALOR", "ASIGNAR", 88m,
                Identity(Producto, ListaPreciosConstants.IdentidadProducto),
                Identity(Servicio, ListaPreciosConstants.IdentidadServicio)));

            Assert.Equal(2, fixture.Repository.LastBulkCommands.Count);
            Assert.Single(fixture.Repository.LastBulkCommands.Select(command => command.CorrelationId).Distinct());
            Assert.All(fixture.Repository.LastBulkCommands, command => Assert.Equal(ListaPreciosConstants.OrigenHistorialMasivo, command.Origen));
        }

        [Fact]
        public async Task Lp14R2_BajaLogica_PreservaComportamientoExistente()
        {
            Fixture fixture = Fixture.Base();
            Guid id = fixture.AddDetail(1, ListaPreciosConstants.IdentidadProducto, Producto, null, null, 88m);

            await fixture.Service.BajaPrecioAsync(Empresa, id, null);

            Assert.False(fixture.Repository.Detalles[id].Activo);
        }

        [Fact]
        public void Lp14R2_ReactivacionIndividual_ConservaOrigenIndividual()
        {
            ListaPreciosDetalleRecord previous = HistoryDetail(active: false, price: 88m);
            ListaPreciosGuardarPrecioCommand command = HistoryCommand(88m, ListaPreciosConstants.OrigenHistorialIndividual);

            Assert.True(ListaPreciosHistorialPolicy.RequiresReactivationHistory(previous, command));
            Assert.Equal(ListaPreciosConstants.OrigenHistorialIndividual, command.Origen);
        }

        [Fact]
        public async Task Lp15_PreviewSoloIncluyeOrigenActivoYSinPersistencia()
        {
            Fixture fixture = Fixture.Base();
            fixture.AddDetail(1, ListaPreciosConstants.IdentidadProducto, Producto, null, null, 0m, descuentoPct: 10m, redondeoModo: ListaPreciosConstants.RedondeoA49, vigenciaInicio: new DateTime(2026, 1, 1), vigenciaFin: new DateTime(2026, 12, 31));
            fixture.AddDetail(1, ListaPreciosConstants.IdentidadServicio, Servicio, null, null, 50m, active: false);

            ListaPreciosCopiarListaResultadoDto result = await fixture.Service.PreviewCopiarListaAsync(Empresa, Copy("SOBRESCRIBIR"));

            ListaPreciosCopiarListaItemDto item = Assert.Single(result.Items);
            Assert.Equal("NUEVO", item.Accion);
            Assert.Equal(0m, item.PrecioAfter);
            Assert.Equal(10m, item.DescuentoAfter);
            Assert.Equal(ListaPreciosConstants.RedondeoA49, item.RedondeoAfter);
            Assert.False(result.Persistido);
            Assert.DoesNotContain(fixture.Repository.Detalles.Values, d => d.Nivel == 2);
        }

        [Theory]
        [InlineData(0, 2)]
        [InlineData(1, 11)]
        public async Task Lp15_ListaInvalida_Rechaza(int source, int target)
        {
            Fixture fixture = Fixture.Base();
            ListaPreciosCopiarListaRequest request = Copy("MERGE");
            request.NivelOrigen = source;
            request.NivelDestino = target;
            InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.PreviewCopiarListaAsync(Empresa, request));
            Assert.Equal(ListaPreciosResolutionCodes.ListaInvalida, error.Message);
        }

        [Fact]
        public async Task Lp15_OrigenDestinoIguales_Rechaza()
        {
            Fixture fixture = Fixture.Base();
            ListaPreciosCopiarListaRequest request = Copy("MERGE");
            request.NivelDestino = 1;
            InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.PreviewCopiarListaAsync(Empresa, request));
            Assert.Equal("ORIGEN_DESTINO_IGUALES", error.Message);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(9)]
        [InlineData(10)]
        public async Task Lp15R2_DestinoCanonicoSinRegistroPrevio_EsValido(int target)
        {
            Fixture fixture = Fixture.Base();
            fixture.Repository.Listas[8] = new(Guid.Parse("58585858-5858-5858-5858-585858585858"), 8, "Lista 8", false, true);
            fixture.AddDetail(8, ListaPreciosConstants.IdentidadProducto, Producto, null, null, 0m);
            fixture.Repository.Listas.Remove(target);
            ListaPreciosCopiarListaRequest request = Copy("MERGE");
            request.NivelOrigen = 8;
            request.NivelDestino = target;

            ListaPreciosCopiarListaResultadoDto result = await fixture.Service.PreviewCopiarListaAsync(Empresa, request);

            Assert.Equal(target, result.NivelDestino);
            Assert.Equal(1, result.Nuevos);
            Assert.Equal(0, result.Rechazados);
            Assert.False(result.Persistido);
        }

        [Fact]
        public async Task Lp15R2_Lista8Destino_EsValidaCuandoOrigenEsDistinto()
        {
            Fixture fixture = Fixture.Base();
            fixture.Repository.Listas.Remove(8);
            fixture.AddDetail(1, ListaPreciosConstants.IdentidadProducto, Producto, null, null, 0m);
            ListaPreciosCopiarListaRequest request = Copy("MERGE");
            request.NivelDestino = 8;

            ListaPreciosCopiarListaResultadoDto result = await fixture.Service.PreviewCopiarListaAsync(Empresa, request);

            Assert.Equal(8, result.NivelDestino);
            Assert.Equal(1, result.Nuevos);
            Assert.Equal(0, result.Rechazados);
        }

        [Fact]
        public async Task Lp15_Sobrescribir_CreaReactivarActualizarYOmitirIdentico()
        {
            Fixture fixture = Fixture.Base();
            fixture.AddDetail(1, ListaPreciosConstants.IdentidadProducto, Producto, null, null, 10m);
            fixture.AddDetail(1, ListaPreciosConstants.IdentidadServicio, Servicio, null, null, 20m);
            fixture.AddDetail(1, ListaPreciosConstants.IdentidadVariante, Producto, Variante, null, 30m);
            fixture.AddDetail(1, ListaPreciosConstants.IdentidadPresentacionVenta, Producto, null, Presentacion, 40m);
            fixture.AddDetail(2, ListaPreciosConstants.IdentidadServicio, Servicio, null, null, 1m, active: false);
            fixture.AddDetail(2, ListaPreciosConstants.IdentidadVariante, Producto, Variante, null, 2m);
            fixture.AddDetail(2, ListaPreciosConstants.IdentidadPresentacionVenta, Producto, null, Presentacion, 40m);

            ListaPreciosCopiarListaResultadoDto preview = await fixture.Service.PreviewCopiarListaAsync(Empresa, Copy("SOBRESCRIBIR"));

            Assert.Equal(1, preview.Nuevos);
            Assert.Equal(1, preview.Reactivados);
            Assert.Equal(1, preview.Sobrescritos);
            Assert.Equal(1, preview.Omitidos);
            Assert.Equal(0, preview.Rechazados);
            Assert.Contains(preview.Items, x => x.TipoIdentidad == ListaPreciosConstants.IdentidadProducto && x.Accion == "NUEVO");
            Assert.Contains(preview.Items, x => x.TipoIdentidad == ListaPreciosConstants.IdentidadServicio && x.Accion == "REACTIVAR");
            Assert.Contains(preview.Items, x => x.TipoIdentidad == ListaPreciosConstants.IdentidadVariante && x.Accion == "SOBRESCRIBIR");
            Assert.Contains(preview.Items, x => x.TipoIdentidad == ListaPreciosConstants.IdentidadPresentacionVenta && x.Accion == "OMITIDO");
        }

        [Fact]
        public async Task Lp15_Merge_CreaReactivaYOmitirPreservaDestinoActivo()
        {
            Fixture fixture = Fixture.Base();
            fixture.AddDetail(1, ListaPreciosConstants.IdentidadProducto, Producto, null, null, 10m);
            fixture.AddDetail(1, ListaPreciosConstants.IdentidadServicio, Servicio, null, null, 20m);
            fixture.AddDetail(1, ListaPreciosConstants.IdentidadVariante, Producto, Variante, null, 30m);
            fixture.AddDetail(2, ListaPreciosConstants.IdentidadServicio, Servicio, null, null, 1m, active: false);
            Guid existing = fixture.AddDetail(2, ListaPreciosConstants.IdentidadVariante, Producto, Variante, null, 99m);

            ListaPreciosCopiarListaResultadoDto result = await fixture.Service.EjecutarCopiarListaAsync(Empresa, Copy("MERGE"));

            Assert.Equal(1, result.Nuevos);
            Assert.Equal(1, result.Reactivados);
            Assert.Equal(1, result.Omitidos);
            Assert.Equal(99m, fixture.Repository.Detalles[existing].Precio);
            Assert.Equal(99m, Assert.Single(result.Items.Where(x => x.Accion == "OMITIDO")).PrecioAfter);
            Assert.DoesNotContain(fixture.Repository.LastBulkCommands, c => c.Identity.TipoIdentidad == ListaPreciosConstants.IdentidadVariante);
        }

        [Fact]
        public async Task Lp15_Ejecucion_UsaOrigenOperacionYCorrelationIdComunes()
        {
            Fixture fixture = Fixture.Base();
            fixture.AddDetail(1, ListaPreciosConstants.IdentidadProducto, Producto, null, null, 10m);
            fixture.AddDetail(1, ListaPreciosConstants.IdentidadServicio, Servicio, null, null, 20m);

            ListaPreciosCopiarListaResultadoDto result = await fixture.Service.EjecutarCopiarListaAsync(Empresa, Copy("SOBRESCRIBIR"));

            Assert.True(result.Persistido);
            Assert.Equal(2, fixture.Repository.LastBulkCommands.Count);
            Assert.All(fixture.Repository.LastBulkCommands, command => Assert.Equal(ListaPreciosConstants.OrigenHistorialCopiaLista, command.Origen));
            Assert.All(fixture.Repository.LastBulkCommands, command => Assert.Equal("NUEVO", command.OperacionHistorial));
            Assert.Single(fixture.Repository.LastBulkCommands.Select(command => command.CorrelationId).Distinct());
        }

        [Fact]
        public async Task LpQa08_CopiaSinDescuentos_PreservaDescuentoDestino()
        {
            Fixture fixture = Fixture.Base();
            fixture.AddDetail(1, ListaPreciosConstants.IdentidadProducto, Producto, null, null, 210m, descuentoPct: 12m);
            fixture.AddDetail(2, ListaPreciosConstants.IdentidadProducto, Producto, null, null, 100m, descuentoPct: 3m);
            ListaPreciosCopiarListaRequest request = Copy("SOBRESCRIBIR");
            request.CopiarDescuentos = false;

            ListaPreciosCopiarListaResultadoDto result = await fixture.Service.EjecutarCopiarListaAsync(Empresa, request);

            ListaPreciosGuardarPrecioCommand command = Assert.Single(fixture.Repository.LastBulkCommands);
            Assert.Equal(210m, command.Precio);
            Assert.Equal(3m, command.DescuentoPct);
            Assert.Equal(3m, Assert.Single(result.Items).DescuentoAfter);
        }

        [Fact]
        public async Task Lp15_CrossTenant_FallaCerrado()
        {
            Fixture fixture = Fixture.Base();
            InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.PreviewCopiarListaAsync(OtraEmpresa, Copy("MERGE")));
            Assert.Equal(ListaPreciosResolutionCodes.ListaInvalida, error.Message);
        }

        [Fact]
        public async Task Lp15_ErrorDuranteLote_RollbackTotal()
        {
            Fixture fixture = Fixture.Base();
            fixture.AddDetail(1, ListaPreciosConstants.IdentidadProducto, Producto, null, null, 10m);
            fixture.AddDetail(1, ListaPreciosConstants.IdentidadServicio, Servicio, null, null, 20m);
            fixture.Repository.FailBulkAt = 2;
            int before = fixture.Repository.Detalles.Count;

            await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.EjecutarCopiarListaAsync(Empresa, Copy("SOBRESCRIBIR")));

            Assert.Equal(before, fixture.Repository.Detalles.Count);
            Assert.DoesNotContain(fixture.Repository.Detalles.Values, detail => detail.Nivel == 2);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(25)]
        [InlineData(100)]
        public async Task Lp16_DescuentoValido_ReemplazaValorSinAlterarPrecio(decimal discount)
        {
            Fixture fixture = Fixture.Base();
            Guid detailId = fixture.AddDetail(1, ListaPreciosConstants.IdentidadProducto, Producto, null, null, 0m, descuentoPct: 10m);

            ListaPreciosDescuentoMarcaResultadoDto result = await fixture.Service.EjecutarDescuentoMarcaAsync(Empresa, BrandDiscount(discount));

            Assert.True(result.Persistido);
            Assert.Equal(0m, fixture.Repository.Detalles[detailId].Precio);
            Assert.Equal(discount, fixture.Repository.Detalles[detailId].DescuentoPct);
            Assert.All(fixture.Repository.LastBulkCommands, command => Assert.Equal(ListaPreciosConstants.OrigenHistorialDescuentoMarca, command.Origen));
            Assert.Single(fixture.Repository.LastBulkCommands.Select(command => command.CorrelationId).Distinct());
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(101)]
        public async Task Lp16_DescuentoFueraDeRango_Rechaza(decimal discount)
        {
            Fixture fixture = Fixture.Base();

            InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.PreviewDescuentoMarcaAsync(Empresa, BrandDiscount(discount)));

            Assert.Equal(ListaPreciosResolutionCodes.DescuentoInvalido, error.Message);
            Assert.Empty(fixture.Repository.LastBulkCommands);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(8)]
        [InlineData(9)]
        [InlineData(10)]
        public async Task Lp16_ListaCanonicaVacia_EsValida(int level)
        {
            Fixture fixture = Fixture.Base();
            fixture.Repository.Listas.Remove(level);
            ListaPreciosDescuentoMarcaRequest request = BrandDiscount(15m);
            request.Nivel = level;

            ListaPreciosDescuentoMarcaResultadoDto result = await fixture.Service.PreviewDescuentoMarcaAsync(Empresa, request);

            Assert.Equal(level, result.Nivel);
            Assert.Equal(3, result.Evaluados);
            Assert.Equal(3, result.Afectados);
            Assert.False(result.Persistido);
        }

        [Fact]
        public async Task Lp16_MarcaInvalidaOCrossTenant_FallaCerrado()
        {
            Fixture fixture = Fixture.Base();
            ListaPreciosDescuentoMarcaRequest invalid = BrandDiscount(15m);
            invalid.IdMarca = Guid.NewGuid();

            InvalidOperationException invalidError = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.PreviewDescuentoMarcaAsync(Empresa, invalid));
            InvalidOperationException tenantError = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.PreviewDescuentoMarcaAsync(OtraEmpresa, BrandDiscount(15m)));

            Assert.Equal("MARCA_INVALIDA", invalidError.Message);
            Assert.Equal("MARCA_INVALIDA", tenantError.Message);
        }

        [Fact]
        public async Task Lp16_UniversoServerSide_IncluyeIdentidadesDeMarcaYExcluyeOtraMarca()
        {
            Fixture fixture = Fixture.Base();
            fixture.Repository.MarcasPorProducto[Servicio] = Guid.NewGuid();

            ListaPreciosDescuentoMarcaResultadoDto result = await fixture.Service.PreviewDescuentoMarcaAsync(Empresa, BrandDiscount(15m));

            Assert.Equal(3, result.Evaluados);
            Assert.Contains(result.Items, item => item.TipoIdentidad == ListaPreciosConstants.IdentidadProducto);
            Assert.Contains(result.Items, item => item.TipoIdentidad == ListaPreciosConstants.IdentidadVariante);
            Assert.Contains(result.Items, item => item.TipoIdentidad == ListaPreciosConstants.IdentidadPresentacionVenta);
            Assert.DoesNotContain(result.Items, item => item.IdProductoServicio == Servicio);
        }

        [Fact]
        public async Task Lp16_ServicioConMarcaReal_EntraEnUniverso()
        {
            Fixture fixture = Fixture.Base();
            fixture.Repository.MarcasPorProducto[Servicio] = MarcaProducto;

            ListaPreciosDescuentoMarcaResultadoDto result = await fixture.Service.PreviewDescuentoMarcaAsync(Empresa, BrandDiscount(15m));

            Assert.Equal(4, result.Evaluados);
            Assert.Contains(result.Items, item => item.TipoIdentidad == ListaPreciosConstants.IdentidadServicio);
        }

        [Fact]
        public async Task Lp16_CreaActualizaReactivaYOmitir_NoGeneraComandoParaNoOp()
        {
            Fixture fixture = Fixture.Base();
            fixture.AddDetail(1, ListaPreciosConstants.IdentidadProducto, Producto, null, null, 100m, descuentoPct: 5m);
            fixture.AddDetail(1, ListaPreciosConstants.IdentidadVariante, Producto, Variante, null, 120m, active: false, descuentoPct: 7m);
            fixture.AddDetail(1, ListaPreciosConstants.IdentidadPresentacionVenta, Producto, null, Presentacion, 10m, descuentoPct: 15m);

            ListaPreciosDescuentoMarcaResultadoDto result = await fixture.Service.EjecutarDescuentoMarcaAsync(Empresa, BrandDiscount(15m));

            Assert.Contains(result.Items, item => item.TipoIdentidad == ListaPreciosConstants.IdentidadProducto && item.Accion == "ACTUALIZAR");
            Assert.Contains(result.Items, item => item.TipoIdentidad == ListaPreciosConstants.IdentidadVariante && item.Accion == "REACTIVAR");
            Assert.Contains(result.Items, item => item.TipoIdentidad == ListaPreciosConstants.IdentidadPresentacionVenta && item.Accion == "OMITIR");
            Assert.Equal(2, fixture.Repository.LastBulkCommands.Count);
            Assert.Equal(1, result.Omitidos);
        }

        [Fact]
        public async Task Lp16_PreviewNoPersiste_YMarcaSinIdentidadesEsControlada()
        {
            Fixture fixture = Fixture.Base();
            fixture.Repository.MarcasPorProducto[Producto] = Guid.NewGuid();

            ListaPreciosDescuentoMarcaResultadoDto result = await fixture.Service.PreviewDescuentoMarcaAsync(Empresa, BrandDiscount(15m));

            Assert.Equal(0, result.Evaluados);
            Assert.False(result.Persistido);
            Assert.Empty(fixture.Repository.Detalles);
        }

        [Fact]
        public async Task Lp16_ErrorDuranteLote_RollbackTotalYPersistenciaParcialCero()
        {
            Fixture fixture = Fixture.Base();
            fixture.Repository.FailBulkAt = 2;

            await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.EjecutarDescuentoMarcaAsync(Empresa, BrandDiscount(15m)));

            Assert.Empty(fixture.Repository.Detalles);
        }

        [Theory]
        [InlineData(1, "Producto base")]
        [InlineData(2, "Servicio")]
        [InlineData(3, "Variante")]
        [InlineData(4, "Presentación venta")]
        public async Task Lp18_HistorialIncluyeCuatroIdentidades(byte tipo, string nombre)
        {
            Fixture fixture = Fixture.Base();
            fixture.Repository.HistorialConsulta.Add(HistoryEvent(tipo, nombre, "INDIVIDUAL", "UPDATE", DateTime.UtcNow));

            ListaPreciosHistorialPaginaDto result = await fixture.Service.ConsultarHistorialAsync(Empresa, new ListaPreciosHistorialConsultaRequest { TipoIdentidad = tipo });

            ListaPreciosHistorialConsultaItemDto item = Assert.Single(result.Items);
            Assert.Equal(nombre, item.TipoIdentidadNombre);
        }

        [Theory]
        [InlineData("INDIVIDUAL", "UPDATE")]
        [InlineData("MASIVO", "UPDATE")]
        [InlineData("COPIA_LISTA", "NUEVO")]
        [InlineData("DESCUENTO_MARCA", "UPDATE")]
        [InlineData("COPIA_LISTA", "REACTIVACION")]
        [InlineData("INDIVIDUAL", "BAJA")]
        public async Task Lp18_HistorialConservaOrigenYOperacion(string origen, string operacion)
        {
            Fixture fixture = Fixture.Base();
            fixture.Repository.HistorialConsulta.Add(HistoryEvent(1, "Producto base", origen, operacion, DateTime.UtcNow));

            ListaPreciosHistorialPaginaDto result = await fixture.Service.ConsultarHistorialAsync(Empresa, new ListaPreciosHistorialConsultaRequest { Origen = origen, Operacion = operacion });

            ListaPreciosHistorialConsultaItemDto item = Assert.Single(result.Items);
            Assert.Equal(origen, item.Origen);
            Assert.Equal(operacion, item.Operacion);
        }

        [Fact]
        public async Task Lp18_FiltrosCombinadosAplicanFechaListaIdentidadBusquedaYCorrelation()
        {
            Fixture fixture = Fixture.Base();
            Guid correlation = Guid.NewGuid();
            fixture.Repository.HistorialConsulta.Add(HistoryEvent(3, "Variante", "MASIVO", "UPDATE", new DateTime(2026, 9, 30, 12, 0, 0, DateTimeKind.Utc), correlation, 2, "Aceite 10 L"));
            fixture.Repository.HistorialConsulta.Add(HistoryEvent(1, "Producto base", "INDIVIDUAL", "UPDATE", new DateTime(2026, 9, 20, 12, 0, 0, DateTimeKind.Utc)));

            ListaPreciosHistorialPaginaDto result = await fixture.Service.ConsultarHistorialAsync(Empresa, new ListaPreciosHistorialConsultaRequest
            {
                FechaDesde = new DateTime(2026, 9, 30), FechaHasta = new DateTime(2026, 9, 30), Nivel = 2,
                TipoIdentidad = 3, Busqueda = "10 L", Origen = "MASIVO", Operacion = "UPDATE", CorrelationId = correlation
            });

            Assert.Single(result.Items);
            Assert.Equal(correlation, result.Items[0].CorrelationId);
        }

        [Fact]
        public async Task Lp18_PaginacionServidorOrdenaFechaDescendente()
        {
            Fixture fixture = Fixture.Base();
            for (int index = 0; index < 30; index++) fixture.Repository.HistorialConsulta.Add(HistoryEvent(1, "Producto base", "INDIVIDUAL", "UPDATE", DateTime.UtcNow.AddMinutes(-index)));

            ListaPreciosHistorialPaginaDto result = await fixture.Service.ConsultarHistorialAsync(Empresa, new ListaPreciosHistorialConsultaRequest { Pagina = 2, TamanoPagina = 25 });

            Assert.Equal(30, result.Total);
            Assert.Equal(2, result.TotalPaginas);
            Assert.Equal(5, result.Items.Count);
            Assert.True(result.Items.SequenceEqual(result.Items.OrderByDescending(item => item.FechaUtc)));
        }

        [Fact]
        public async Task Lp18_CrossTenantYEstadoVacio_FailClosed()
        {
            Fixture fixture = Fixture.Base();
            fixture.Repository.HistorialConsulta.Add(HistoryEvent(1, "Producto base", "INDIVIDUAL", "UPDATE", DateTime.UtcNow));

            ListaPreciosHistorialPaginaDto result = await fixture.Service.ConsultarHistorialAsync(OtraEmpresa, new ListaPreciosHistorialConsultaRequest());

            Assert.Empty(result.Items);
            Assert.Equal(0, result.Total);
        }

        [Fact]
        public async Task Lp18_RangoFechaInvalido_RechazaConsulta()
        {
            Fixture fixture = Fixture.Base();

            InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.ConsultarHistorialAsync(Empresa, new ListaPreciosHistorialConsultaRequest
            {
                FechaDesde = new DateTime(2026, 10, 1), FechaHasta = new DateTime(2026, 9, 30)
            }));

            Assert.Equal("RANGO_FECHAS_INVALIDO", error.Message);
        }

        [Theory]
        [InlineData(ListaPreciosConstants.IdentidadProducto)]
        [InlineData(ListaPreciosConstants.IdentidadServicio)]
        [InlineData(ListaPreciosConstants.IdentidadVariante)]
        [InlineData(ListaPreciosConstants.IdentidadPresentacionVenta)]
        public async Task LpQa05R_CargaComercial_SoportaCuatroIdentidades(byte tipo)
        {
            Fixture fixture = Fixture.Base(); Guid product = tipo == ListaPreciosConstants.IdentidadServicio ? Servicio : Producto;
            ListaPreciosEditorDto editor = await fixture.Service.ObtenerEditorAsync(Empresa, Request(product, tipo: tipo, variante: tipo == 3 ? Variante : null, presentacion: tipo == 4 ? Presentacion : null));
            Assert.Equal(tipo is 3 or 4, editor.Comercial.DescripcionReadOnly);
            Assert.False(editor.Comercial.Materializada);
        }

        [Fact]
        public async Task LpQa05R_GuardadoComercial_MaterializaFlagsYTextosSinPrecios()
        {
            Fixture fixture = Fixture.Base();
            ListaPreciosMatrizPreviewDto result = await fixture.Service.GuardarMatrizAsync(Empresa, new ListaPreciosMatrizRequest
            {
                TipoIdentidad = 1, IdProductoServicio = Producto, Listas = Array.Empty<ListaPreciosMatrizItemRequest>(),
                Comercial = new ListaPreciosComercialRequest { Modificado = true, Descripcion = "Aceite QA", Web = "https://qa.invalid", Liverpool = "L", MercadoLibre = "ML", Observaciones = "O", DosPorUno = true, TresPorDos = true, DescuentoSegundo = true, Monedero = true }
            });
            ListaPreciosComercialDto stored = fixture.Repository.Comerciales[new(1, Producto, null, null)];
            Assert.True(result.Persistido); Assert.True(stored.DosPorUno); Assert.True(stored.TresPorDos); Assert.True(stored.DescuentoSegundo); Assert.True(stored.Monedero); Assert.Equal("Aceite QA", stored.Descripcion);
            Assert.Empty(fixture.Repository.LastBulkCommands);
        }

        [Fact]
        public async Task LpQa05R_DescripcionVariante_EsReadOnlyFailClosed()
        {
            Fixture fixture = Fixture.Base();
            InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.GuardarMatrizAsync(Empresa, new ListaPreciosMatrizRequest
            {
                TipoIdentidad = 3, IdProductoServicio = Producto, IdVariante = Variante, Listas = Array.Empty<ListaPreciosMatrizItemRequest>(),
                Comercial = new ListaPreciosComercialRequest { Modificado = true, Descripcion = "Intento" }
            }));
            Assert.Equal("DESCRIPCION_SOLO_LECTURA", error.Message); Assert.Empty(fixture.Repository.Comerciales);
        }

        [Fact]
        public async Task LpQa08_DescripcionVarianteSinCambios_PermiteGuardarDatosPropios()
        {
            Fixture fixture = Fixture.Base();
            ListaPreciosMatrizPreviewDto result = await fixture.Service.GuardarMatrizAsync(Empresa, new ListaPreciosMatrizRequest
            {
                TipoIdentidad = ListaPreciosConstants.IdentidadVariante,
                IdProductoServicio = Producto,
                IdVariante = Variante,
                Listas = Array.Empty<ListaPreciosMatrizItemRequest>(),
                Comercial = new ListaPreciosComercialRequest
                {
                    Modificado = true,
                    Descripcion = "Aceite sintético",
                    Web = "https://qa.invalid/variante",
                    Observaciones = "Dato propio de variante",
                    DosPorUno = true
                }
            });

            ListaPreciosComercialDto stored = fixture.Repository.Comerciales[new(ListaPreciosConstants.IdentidadVariante, Producto, Variante, null)];
            Assert.True(result.Persistido);
            Assert.True(stored.DescripcionReadOnly);
            Assert.Equal("Aceite sintético", stored.Descripcion);
            Assert.Equal("https://qa.invalid/variante", stored.Web);
            Assert.Equal("Dato propio de variante", stored.Observaciones);
            Assert.True(stored.DosPorUno);
        }

        [Fact]
        public async Task LpQa05R_GuardadoConjunto_RollbackNoDejaPrecioParcial()
        {
            Fixture fixture = Fixture.Base(); fixture.Repository.FailCommercial = true;
            await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.GuardarMatrizAsync(Empresa, new ListaPreciosMatrizRequest
            {
                TipoIdentidad = 1, IdProductoServicio = Producto,
                Listas = new[] { new ListaPreciosMatrizItemRequest { Nivel = 1, Precio = 77m, DescuentoPct = 0m } },
                Comercial = new ListaPreciosComercialRequest { Modificado = true, Descripcion = "Rollback", DosPorUno = true }
            }));
            Assert.Empty(fixture.Repository.Detalles); Assert.Empty(fixture.Repository.Comerciales);
        }

        [Fact]
        public async Task LpQa05R_CrossTenant_FallaCerrado()
        {
            Fixture fixture = Fixture.Base();
            await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.ObtenerEditorAsync(OtraEmpresa, Request(Producto, tipo: 1)));
        }

        private static ListaPreciosHistorialConsultaItemDto HistoryEvent(byte tipo, string tipoNombre, string origen, string operacion, DateTime fecha, Guid? correlation = null, int nivel = 1, string identidad = "Aceite")
            => new(Guid.NewGuid(), fecha, nivel, "Lista " + nivel, tipo, tipoNombre, "QA-001", identidad, "Precio", operacion, "100.00", "110.00", "QA", origen, correlation ?? Guid.NewGuid(), "LP-18");

        private static ListaPreciosDetalleRecord HistoryDetail(bool active, decimal price)
            => new(Guid.NewGuid(), Guid.NewGuid(), 1, ListaPreciosConstants.IdentidadProducto, Producto, null, null,
                price, 0m, ListaPreciosConstants.RedondeoSinRedondeo, null, null, active, DateTime.UtcNow);

        private static ListaPreciosGuardarPrecioCommand HistoryCommand(decimal price, string origin)
            => new(1, ListaPreciosConstants.TipoProducto,
                new ListaPreciosIdentityKey(ListaPreciosConstants.IdentidadProducto, Producto, null, null),
                price, 0m, ListaPreciosConstants.RedondeoSinRedondeo, null, null, null, "QA", "LP-14R2", Guid.NewGuid(), origin);

        private static ListaPreciosAjusteMasivoIdentidadRequest Identity(Guid product, byte type, Guid? variant = null, Guid? presentacion = null) => new()
        {
            TipoIdentidad = type,
            IdProductoServicio = product,
            IdVariante = variant,
            IdPresentacionVenta = presentacion
        };

        private static ListaPreciosAjusteMasivoRequest Bulk(string field, string type, string operation, decimal value, params ListaPreciosAjusteMasivoIdentidadRequest[] identities) => new()
        {
            Nivel = 1,
            Campo = field,
            TipoAjuste = type,
            Operacion = operation,
            Valor = value,
            Identidades = identities,
            Motivo = "QA LP-14"
        };

        private static ListaPreciosCopiarListaRequest Copy(string mode) => new()
        {
            NivelOrigen = 1,
            NivelDestino = 2,
            Modo = mode,
            Motivo = "QA LP-15"
        };

        private static ListaPreciosDescuentoMarcaRequest BrandDiscount(decimal discount) => new()
        {
            IdMarca = MarcaProducto,
            Nivel = 1,
            DescuentoPct = discount,
            Motivo = "QA LP-16"
        };

        private static ListaPreciosResolverRequest Request(
            Guid producto,
            int? nivel = 1,
            byte? tipo = null,
            Guid? variante = null,
            Guid? presentacion = null,
            bool requiereActivo = true) => new()
            {
                Nivel = nivel,
                TipoIdentidad = tipo,
                IdProductoServicio = producto,
                IdVariante = variante,
                IdPresentacionVenta = presentacion,
                RequiereActivo = requiereActivo
            };

        private static ListaPreciosGuardarPrecioRequest Save(Guid producto, decimal precio) => new()
        {
            Nivel = 1,
            IdProductoServicio = producto,
            Precio = precio
        };

        private sealed class Fixture
        {
            public FakeListaPreciosRepository Repository { get; } = new();
            public ListaPreciosService Service { get; }

            private Fixture()
            {
                Service = new ListaPreciosService(Repository);
            }

            public static Fixture Base()
            {
                Fixture fixture = new();
                fixture.Repository.Productos[Producto] = new(Producto, ListaPreciosConstants.TipoProducto, true, 100m);
                fixture.Repository.Productos[Servicio] = new(Servicio, ListaPreciosConstants.TipoServicio, true, 40m);
                fixture.Repository.Variantes[Variante] = new(Variante, Producto, true, 120m);
                fixture.Repository.Presentaciones[Presentacion] = new(Presentacion, Producto, true, 10m);
                fixture.Repository.Listas[1] = new(Guid.Parse("51515151-5151-5151-5151-515151515151"), 1, "Lista 1", true, true);
                fixture.Repository.Listas[2] = new(Guid.Parse("52525252-5252-5252-5252-525252525252"), 2, "Lista 2", false, true);
                return fixture;
            }

            public Guid AddDetail(
                int nivel,
                byte tipo,
                Guid producto,
                Guid? variante,
                Guid? presentacion,
                decimal precio,
                bool active = true,
                decimal? descuentoPct = null,
                byte redondeoModo = ListaPreciosConstants.RedondeoSinRedondeo,
                DateTime? vigenciaInicio = null,
                DateTime? vigenciaFin = null)
            {
                Guid id = Guid.NewGuid();
                Repository.Detalles[id] = new(id, Repository.Listas[nivel].Id, nivel, tipo, producto, variante, presentacion, precio, descuentoPct, redondeoModo, vigenciaInicio, vigenciaFin, active, DateTime.UtcNow);
                return id;
            }
        }

        private sealed class FakeListaPreciosRepository : IListaPreciosRepository
        {
            public Dictionary<Guid, ListaPreciosProductoRecord> Productos { get; } = new();
            public Dictionary<Guid, ListaPreciosVarianteRecord> Variantes { get; } = new();
            public Dictionary<Guid, ListaPreciosPresentacionVentaRecord> Presentaciones { get; } = new();
            public Dictionary<int, ListaPreciosListaRecord> Listas { get; } = new();
            public Dictionary<Guid, ListaPreciosDetalleRecord> Detalles { get; } = new();
            public Dictionary<Guid, Guid?> MarcasPorProducto { get; } = new()
            {
                [Producto] = MarcaProducto,
                [Servicio] = null
            };
            public Dictionary<Guid, HashSet<Guid>> EtiquetasPorProducto { get; } = new()
            {
                [Producto] = new HashSet<Guid> { EtiquetaProducto }
            };
            public Dictionary<Guid, HashSet<Guid>> AtributosPorProducto { get; } = new()
            {
                [Producto] = new HashSet<Guid> { AtributoProducto }
            };
            public Dictionary<Guid, HashSet<Guid>> ValoresAtributoPorProducto { get; } = new()
            {
                [Producto] = new HashSet<Guid> { AtributoValorProducto }
            };
            public string ProductoImagenUrl { get; set; } = ListaPreciosServiceTests.ProductoImagenUrl;
            public string ProductoImagenNombre { get; set; } = "Producto principal";
            public string VarianteImagenUrl { get; set; } = ListaPreciosServiceTests.VarianteImagenUrl;
            public string VarianteImagenNombre { get; set; } = "Variante roja";
            public bool ForceDuplicate { get; set; }
            public int? FailBulkAt { get; set; }
            public bool FailCommercial { get; set; }
            public IReadOnlyList<ListaPreciosGuardarPrecioCommand> LastBulkCommands { get; private set; } = Array.Empty<ListaPreciosGuardarPrecioCommand>();
            public int ConsultaCalls { get; private set; }
            public int DetalleInventarioCalls { get; private set; }
            public Dictionary<(Guid Producto, Guid? Variante, Guid Sucursal), decimal> Existencias { get; } = new();
            public List<ListaPreciosInventarioMovimientoDto> Movimientos { get; } = new();
            public List<ListaPreciosHistorialConsultaItemDto> HistorialConsulta { get; } = new();
            public Dictionary<ListaPreciosIdentityKey, ListaPreciosComercialDto> Comerciales { get; } = new();

            public Task<ListaPreciosProductoRecord?> ObtenerProductoAsync(Guid idEmpresa, Guid idProductoServicio, CancellationToken cancellationToken = default)
                => Task.FromResult(idEmpresa == Empresa && Productos.TryGetValue(idProductoServicio, out var value) ? value : null);

            public Task<ListaPreciosVarianteRecord?> ObtenerVarianteAsync(Guid idEmpresa, Guid idVariante, CancellationToken cancellationToken = default)
                => Task.FromResult(idEmpresa == Empresa && Variantes.TryGetValue(idVariante, out var value) ? value : null);

            public Task<ListaPreciosPresentacionVentaRecord?> ObtenerPresentacionVentaAsync(Guid idEmpresa, Guid idPresentacionVenta, CancellationToken cancellationToken = default)
                => Task.FromResult(idEmpresa == Empresa && Presentaciones.TryGetValue(idPresentacionVenta, out var value) ? value : null);

            public Task<ListaPreciosListaRecord?> ObtenerListaPorNivelAsync(Guid idEmpresa, int nivel, CancellationToken cancellationToken = default)
                => Task.FromResult(idEmpresa == Empresa && Listas.TryGetValue(nivel, out var value) && value.Activo ? value : null);

            public Task<IReadOnlyList<ListaPreciosListaDto>> ObtenerListasAsync(Guid idEmpresa, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<ListaPreciosListaDto>>(Listas.Values.Select(l => new ListaPreciosListaDto { Id = l.Id, Nivel = l.Nivel, Nombre = l.Nombre, EsDefault = l.EsDefault, Activo = l.Activo }).ToList());

            public Task<IReadOnlyList<ListaPreciosComboItemDto>> ObtenerCategoriasAsync(Guid idEmpresa, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<ListaPreciosComboItemDto>>(Array.Empty<ListaPreciosComboItemDto>());

            public Task<IReadOnlyList<ListaPreciosComboItemDto>> ObtenerMarcasAsync(Guid idEmpresa, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<ListaPreciosComboItemDto>>(idEmpresa == Empresa
                    ? new[] { new ListaPreciosComboItemDto { Id = MarcaProducto, Clave = MarcaProducto.ToString(), Nombre = "Mobil 1" } }
                    : Array.Empty<ListaPreciosComboItemDto>());

            public Task<IReadOnlyList<ListaPreciosComboItemDto>> ObtenerColeccionesAsync(Guid idEmpresa, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<ListaPreciosComboItemDto>>(Array.Empty<ListaPreciosComboItemDto>());

            public Task<IReadOnlyList<ListaPreciosComboItemDto>> ObtenerEtiquetasAsync(Guid idEmpresa, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<ListaPreciosComboItemDto>>(Array.Empty<ListaPreciosComboItemDto>());

            public Task<IReadOnlyList<ListaPreciosComboItemDto>> ObtenerAtributosAsync(Guid idEmpresa, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<ListaPreciosComboItemDto>>(Array.Empty<ListaPreciosComboItemDto>());

            public Task<IReadOnlyList<ListaPreciosComboItemDto>> ObtenerValoresAtributosAsync(Guid idEmpresa, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<ListaPreciosComboItemDto>>(idEmpresa == Empresa
                    ? new[] { new ListaPreciosComboItemDto { Id = AtributoValorProducto, ParentId = AtributoProducto, Clave = AtributoValorProducto.ToString(), Nombre = "Rojo" } }
                    : Array.Empty<ListaPreciosComboItemDto>());

            public Task<IReadOnlyList<ListaPreciosComboItemDto>> ObtenerVariantesAsync(Guid idEmpresa, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<ListaPreciosComboItemDto>>(Array.Empty<ListaPreciosComboItemDto>());

            public Task<IReadOnlyList<ListaPreciosComboItemDto>> ObtenerPresentacionesVentaAsync(Guid idEmpresa, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<ListaPreciosComboItemDto>>(Array.Empty<ListaPreciosComboItemDto>());

            public Task<IReadOnlyList<ListaPreciosComboItemDto>> ObtenerSucursalesAsync(Guid idEmpresa, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<ListaPreciosComboItemDto>>(new[]
                {
                    new ListaPreciosComboItemDto { Id = SucursalCentro, Clave = SucursalCentro.ToString(), Nombre = "Centro" },
                    new ListaPreciosComboItemDto { Id = SucursalNorte, Clave = SucursalNorte.ToString(), Nombre = "Norte" }
                });

            public Task<IReadOnlyList<ListaPreciosConsultaIdentityRecord>> ConsultarIdentidadesAsync(Guid idEmpresa, ListaPreciosConsultaRequest request, CancellationToken cancellationToken = default)
            {
                ConsultaCalls++;
                if (idEmpresa != Empresa)
                {
                    return Task.FromResult<IReadOnlyList<ListaPreciosConsultaIdentityRecord>>(Array.Empty<ListaPreciosConsultaIdentityRecord>());
                }

                IEnumerable<ListaPreciosProductoRecord> products = Productos.Values;
                if (request.Tipo.HasValue && request.Tipo.Value is ListaPreciosConstants.TipoProducto or ListaPreciosConstants.TipoServicio)
                {
                    products = products.Where(p => p.Tipo == request.Tipo.Value);
                }

                List<ListaPreciosConsultaIdentityRecord> rows = new();
                foreach (ListaPreciosProductoRecord product in products)
                {
                    rows.Add(ToIdentity(product));
                    if (product.Tipo == ListaPreciosConstants.TipoProducto)
                    {
                        rows.AddRange(Variantes.Values.Where(v => v.IdProductoServicio == product.Id).Select(v => ToVariantIdentity(product, v)));
                        rows.AddRange(Presentaciones.Values.Where(p => p.IdProductoServicio == product.Id).Select(p => ToPresentationIdentity(product, p)));
                    }
                }

                rows = rows.Select(row => EnrichPrice(row, request.Nivel.GetValueOrDefault(1))).ToList();

                if (!string.IsNullOrWhiteSpace(request.Busqueda))
                {
                    string search = request.Busqueda.Trim();
                    rows = rows
                        .Where(row => Contains(row.Nombre, search)
                            || Contains(row.ProductoPadre, search)
                            || Contains(row.TipoNombre, search)
                            || Contains(row.IdentidadVendible, search))
                        .ToList();
                }

                if (request.IdCategoria.HasValue)
                {
                    rows = rows.Where(row => row.IdCategoria == request.IdCategoria.Value).ToList();
                }

                if (request.IdMarca.HasValue)
                {
                    rows = rows.Where(row => row.IdMarca == request.IdMarca.Value).ToList();
                }

                if (request.IdColeccion.HasValue)
                {
                    rows = rows.Where(row => row.IdColeccion == request.IdColeccion.Value).ToList();
                }

                if (request.IdEtiqueta.HasValue)
                {
                    rows = rows.Where(row => EtiquetasPorProducto.TryGetValue(row.IdProductoServicio, out HashSet<Guid>? tags) && tags.Contains(request.IdEtiqueta.Value)).ToList();
                }

                if (request.IdAtributo.HasValue)
                {
                    rows = rows.Where(row => AtributosPorProducto.TryGetValue(row.IdProductoServicio, out HashSet<Guid>? attrs) && attrs.Contains(request.IdAtributo.Value)).ToList();
                }

                if (request.IdsValoresAtributo.Count > 0)
                {
                    rows = rows.Where(row => ValoresAtributoPorProducto.TryGetValue(row.IdProductoServicio, out HashSet<Guid>? values)
                        && request.IdsValoresAtributo.Any(values.Contains)).ToList();
                }

                if (request.IdVariante.HasValue)
                {
                    rows = rows.Where(row => row.IdVariante == request.IdVariante.Value).ToList();
                }

                if (request.IdPresentacionVenta.HasValue)
                {
                    rows = rows.Where(row => row.IdPresentacionVenta == request.IdPresentacionVenta.Value).ToList();
                }

                string status = string.IsNullOrWhiteSpace(request.Estatus) ? "activos" : request.Estatus.Trim().ToLowerInvariant();
                if (status == "activos") rows = rows.Where(row => row.Activo && row.ProductoActivo).ToList();
                if (status == "inactivos") rows = rows.Where(row => !row.Activo || !row.ProductoActivo).ToList();

                rows = rows.Select(row =>
                {
                    if (!row.InventarioAplicable)
                    {
                        return row with { Existencia = null };
                    }

                    Guid? variante = row.TipoIdentidad == ListaPreciosConstants.IdentidadVariante ? row.IdVariante : null;
                    IReadOnlyList<Guid> branches = request.IdsSucursales.Count > 0
                        ? request.IdsSucursales
                        : (request.IdSucursal.HasValue ? new[] { request.IdSucursal.Value } : Array.Empty<Guid>());
                    decimal existencia = Existencias
                        .Where(x => x.Key.Producto == row.IdProductoServicio && x.Key.Variante == variante && (branches.Count == 0 || branches.Contains(x.Key.Sucursal)))
                        .Sum(x => x.Value);
                    return row with { Existencia = existencia };
                }).ToList();

                string existenciaFiltro = (request.Existencia ?? string.Empty).Trim().ToLowerInvariant();
                if (existenciaFiltro == "con") rows = rows.Where(x => x.InventarioAplicable && x.Existencia > 0m).ToList();
                if (existenciaFiltro == "sin") rows = rows.Where(x => x.InventarioAplicable && x.Existencia == 0m).ToList();
                if (request.CantidadMenorA.HasValue) rows = rows.Where(x => x.InventarioAplicable && x.Existencia < request.CantidadMenorA.Value).ToList();

                return Task.FromResult<IReadOnlyList<ListaPreciosConsultaIdentityRecord>>(rows);
            }

            private ListaPreciosConsultaIdentityRecord EnrichPrice(ListaPreciosConsultaIdentityRecord row, int selectedLevel)
            {
                ListaPreciosDetalleRecord? At(int level) => Detalles.Values
                    .Where(d => d.Nivel == level && d.Activo && IsVigente(d) && d.TipoIdentidad == row.TipoIdentidad && d.IdProductoServicio == row.IdProductoServicio && d.IdVariante == row.IdVariante && d.IdPresentacionVenta == row.IdPresentacionVenta)
                    .OrderByDescending(d => d.FechaActualizacion)
                    .FirstOrDefault();
                ListaPreciosDetalleRecord? selected = At(selectedLevel);
                ListaPreciosDetalleRecord? p1 = At(1); ListaPreciosDetalleRecord? p2 = At(2); ListaPreciosDetalleRecord? p3 = At(3); ListaPreciosDetalleRecord? p4 = At(4); ListaPreciosDetalleRecord? p5 = At(5);
                ListaPreciosDetalleRecord? p6 = At(6); ListaPreciosDetalleRecord? p7 = At(7); ListaPreciosDetalleRecord? p8 = At(8); ListaPreciosDetalleRecord? p9 = At(9); ListaPreciosDetalleRecord? p10 = At(10);
                return row with
                {
                    IdDetalleSeleccionado = selected?.Id, PrecioSeleccionado = selected?.Precio, DescuentoSeleccionado = selected?.DescuentoPct,
                    RedondeoSeleccionado = selected?.RedondeoModo ?? 0, VigenciaInicioSeleccionada = selected?.VigenciaInicio, VigenciaFinSeleccionada = selected?.VigenciaFin,
                    P1 = p1?.Precio, D1 = p1?.DescuentoPct, P2 = p2?.Precio, D2 = p2?.DescuentoPct, P3 = p3?.Precio, D3 = p3?.DescuentoPct,
                    P4 = p4?.Precio, D4 = p4?.DescuentoPct, P5 = p5?.Precio, D5 = p5?.DescuentoPct, P6 = p6?.Precio, D6 = p6?.DescuentoPct,
                    P7 = p7?.Precio, D7 = p7?.DescuentoPct, P8 = p8?.Precio, D8 = p8?.DescuentoPct, P9 = p9?.Precio, D9 = p9?.DescuentoPct, P10 = p10?.Precio, D10 = p10?.DescuentoPct
                };
            }

            private ListaPreciosConsultaIdentityRecord ToIdentity(ListaPreciosProductoRecord p)
            {
                bool service = p.Tipo == ListaPreciosConstants.TipoServicio;
                string name = service ? "Cambio de Aceite" : "Aceite Motor Sintetico";
                Guid category = service ? CategoriaServicio : CategoriaProducto;
                Guid? brand = MarcasPorProducto.TryGetValue(p.Id, out Guid? assignedBrand) ? assignedBrand : null;
                return new ListaPreciosConsultaIdentityRecord(
                    p.Id,
                    null,
                    null,
                    service ? ListaPreciosConstants.IdentidadServicio : ListaPreciosConstants.IdentidadProducto,
                    service ? "Servicio" : "Producto base",
                    p.Tipo,
                    service ? "Servicio" : "Producto",
                    service ? "002" : "001",
                    name,
                    name,
                    category,
                    service ? "Mantenimiento" : "Alimentos",
                    brand,
                    brand.HasValue ? "Mobil 1" : "Sin marca",
                    service ? null : ColeccionProducto,
                    service ? string.Empty : "Temporada Base",
                    service ? "Servicio" : "Producto base",
                    service ? string.Empty : ProductoImagenUrl,
                    service ? string.Empty : ProductoImagenNombre,
                    service ? string.Empty : "Producto",
                    p.Activo,
                    p.Activo,
                    !service && p.CausaInventario,
                    !service && p.CausaInventario ? 0m : null,
                    p.PrecioPublico);
            }

            private ListaPreciosConsultaIdentityRecord ToVariantIdentity(ListaPreciosProductoRecord p, ListaPreciosVarianteRecord v)
            {
                bool hasVariantImage = !string.IsNullOrWhiteSpace(VarianteImagenUrl);
                Guid? brand = MarcasPorProducto.TryGetValue(p.Id, out Guid? assignedBrand) ? assignedBrand : null;
                return new ListaPreciosConsultaIdentityRecord(
                    p.Id,
                    v.Id,
                    null,
                    ListaPreciosConstants.IdentidadVariante,
                    "Variante",
                    p.Tipo,
                    "Producto",
                    "001-R",
                    "Aceite Motor Sintetico Rojo",
                    "Aceite Motor Sintetico",
                    CategoriaProducto,
                    "Alimentos",
                    brand,
                    brand.HasValue ? "Mobil 1" : "Sin marca",
                    ColeccionProducto,
                    "Temporada Base",
                    "Variante · Rojo",
                    hasVariantImage ? VarianteImagenUrl : ProductoImagenUrl,
                    hasVariantImage ? VarianteImagenNombre : ProductoImagenNombre,
                    hasVariantImage ? "Variante" : "Producto",
                    v.Activo,
                    p.Activo,
                    p.CausaInventario,
                    p.CausaInventario ? 0m : null,
                    v.PrecioPublico ?? p.PrecioPublico);
            }

            private ListaPreciosConsultaIdentityRecord ToPresentationIdentity(ListaPreciosProductoRecord p, ListaPreciosPresentacionVentaRecord presentation)
            {
                Guid? brand = MarcasPorProducto.TryGetValue(p.Id, out Guid? assignedBrand) ? assignedBrand : null;
                return new ListaPreciosConsultaIdentityRecord(
                    p.Id,
                    null,
                    presentation.Id,
                    ListaPreciosConstants.IdentidadPresentacionVenta,
                    "Presentación venta",
                    p.Tipo,
                    "Producto",
                    "001",
                    "Aceite Motor Sintetico Pieza",
                    "Aceite Motor Sintetico",
                    CategoriaProducto,
                    "Alimentos",
                    brand,
                    brand.HasValue ? "Mobil 1" : "Sin marca",
                    ColeccionProducto,
                    "Temporada Base",
                    "Presentación · Pieza",
                    ProductoImagenUrl,
                    ProductoImagenNombre,
                    "Producto",
                    presentation.Activo,
                    p.Activo,
                    p.CausaInventario,
                    p.CausaInventario ? 0m : null,
                    presentation.Precio);
            }

            private static bool Contains(string value, string search)
                => value.Contains(search, StringComparison.OrdinalIgnoreCase);

            public Task<ListaPreciosDetalleRecord?> ObtenerPrecioActivoAsync(Guid idEmpresa, Guid idListaPrecio, ListaPreciosIdentityKey identity, CancellationToken cancellationToken = default)
                => Task.FromResult(idEmpresa == Empresa
                    ? Detalles.Values.FirstOrDefault(d => d.Activo && d.IdListaPrecio == idListaPrecio && IsVigente(d) && SameIdentity(d, identity))
                    : null);

            public Task<IReadOnlyList<ListaPreciosDetalleRecord>> ObtenerDetallesPorNivelAsync(Guid idEmpresa, int nivel, bool soloActivos, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<ListaPreciosDetalleRecord>>(idEmpresa == Empresa
                    ? Detalles.Values.Where(d => d.Nivel == nivel && (!soloActivos || d.Activo)).ToList()
                    : Array.Empty<ListaPreciosDetalleRecord>());

            public Task<ListaPreciosInventarioDetalleDto> ObtenerInventarioDetalleAsync(Guid idEmpresa, ListaPreciosIdentityKey identity, CancellationToken cancellationToken = default)
            {
                DetalleInventarioCalls++;
                if (idEmpresa != Empresa)
                {
                    return Task.FromResult(new ListaPreciosInventarioDetalleDto { Aplicable = false });
                }

                Guid? variante = identity.TipoIdentidad == ListaPreciosConstants.IdentidadVariante ? identity.IdVariante : null;
                List<ListaPreciosInventarioSaldoDto> saldos = Existencias
                    .Where(x => x.Key.Producto == identity.IdProductoServicio && x.Key.Variante == variante)
                    .Select(x => new ListaPreciosInventarioSaldoDto(x.Key.Sucursal, x.Key.Sucursal == SucursalCentro ? "Centro" : "Norte", x.Value))
                    .ToList();
                return Task.FromResult(new ListaPreciosInventarioDetalleDto
                {
                    Aplicable = true,
                    ExistenciaTotal = saldos.Sum(x => x.Existencia),
                    Saldos = saldos,
                    Movimientos = Movimientos
                });
            }

            public Task<IReadOnlyList<ListaPreciosPrecioDto>> ObtenerPreciosPorProductoAsync(Guid idEmpresa, Guid idProductoServicio, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<ListaPreciosPrecioDto>>(Detalles.Values.Where(d => d.IdProductoServicio == idProductoServicio).Select(d => new ListaPreciosPrecioDto { Id = d.Id, IdListaPrecio = d.IdListaPrecio, Nivel = d.Nivel, TipoIdentidad = d.TipoIdentidad, IdProductoServicio = d.IdProductoServicio, IdVariante = d.IdVariante, IdPresentacionVenta = d.IdPresentacionVenta, Precio = d.Precio, DescuentoPct = d.DescuentoPct, RedondeoModo = d.RedondeoModo, VigenciaInicio = d.VigenciaInicio, VigenciaFin = d.VigenciaFin, Activo = d.Activo, FechaActualizacion = d.FechaActualizacion }).ToList());

            public Task<ListaPreciosComercialDto> ObtenerComercialAsync(Guid idEmpresa, ListaPreciosIdentityKey identity, CancellationToken cancellationToken = default)
            {
                if (idEmpresa != Empresa) throw new InvalidOperationException(ListaPreciosResolutionCodes.IdentidadInvalida);
                if (Comerciales.TryGetValue(identity, out ListaPreciosComercialDto? value)) return Task.FromResult(value);
                return Task.FromResult(new ListaPreciosComercialDto { Descripcion = identity.TipoIdentidad == ListaPreciosConstants.IdentidadServicio ? "Cambio de aceite" : "Aceite sintético", DescripcionReadOnly = identity.TipoIdentidad is ListaPreciosConstants.IdentidadVariante or ListaPreciosConstants.IdentidadPresentacionVenta });
            }

            public Task<Guid> EnsureListaAsync(Guid idEmpresa, int nivel, Guid? usuarioId, CancellationToken cancellationToken = default)
            {
                if (idEmpresa != Empresa)
                {
                    throw new InvalidOperationException(ListaPreciosResolutionCodes.ListaInvalida);
                }

                if (Listas.TryGetValue(nivel, out ListaPreciosListaRecord? existing))
                {
                    Listas[nivel] = existing with { Activo = true };
                    return Task.FromResult(existing.Id);
                }

                Guid id = Guid.NewGuid();
                Listas[nivel] = new ListaPreciosListaRecord(id, nivel, $"Lista {nivel}", nivel == ListaPreciosConstants.NivelDefault, true);
                return Task.FromResult(id);
            }

            public Task<Guid> GuardarPrecioAsync(Guid idEmpresa, ListaPreciosGuardarPrecioCommand command, CancellationToken cancellationToken = default)
            {
                if (ForceDuplicate) throw new InvalidOperationException(ListaPreciosResolutionCodes.Duplicado);
                ListaPreciosListaRecord list = Listas[command.Nivel];
                ListaPreciosDetalleRecord? existing = Detalles.Values.FirstOrDefault(d => d.IdListaPrecio == list.Id && SameIdentity(d, command.Identity));
                Guid id = existing?.Id ?? Guid.NewGuid();
                Detalles[id] = new(id, list.Id, command.Nivel, command.Identity.TipoIdentidad, command.Identity.IdProductoServicio, command.Identity.IdVariante, command.Identity.IdPresentacionVenta, command.Precio, command.DescuentoPct, command.RedondeoModo, command.VigenciaInicio, command.VigenciaFin, true, DateTime.UtcNow);
                return Task.FromResult(id);
            }

            public async Task<IReadOnlyList<Guid>> GuardarPreciosMasivosAsync(Guid idEmpresa, IReadOnlyList<ListaPreciosGuardarPrecioCommand> commands, CancellationToken cancellationToken = default)
            {
                if (ForceDuplicate) throw new InvalidOperationException(ListaPreciosResolutionCodes.Duplicado);
                LastBulkCommands = commands.ToList();
                Dictionary<Guid, ListaPreciosDetalleRecord> snapshot = Detalles.ToDictionary(pair => pair.Key, pair => pair.Value);
                try
                {
                    List<Guid> ids = new(commands.Count);
                    for (int index = 0; index < commands.Count; index++)
                    {
                        if (FailBulkAt == index + 1) throw new InvalidOperationException("FALLO_TRANSACCIONAL_QA");
                        ids.Add(await GuardarPrecioAsync(idEmpresa, commands[index], cancellationToken));
                    }

                    return ids;
                }
                catch
                {
                    Detalles.Clear();
                    foreach (KeyValuePair<Guid, ListaPreciosDetalleRecord> pair in snapshot) Detalles[pair.Key] = pair.Value;
                    throw;
                }
            }

            public async Task<IReadOnlyList<Guid>> GuardarMatrizConComercialAsync(Guid idEmpresa, IReadOnlyList<ListaPreciosGuardarPrecioCommand> commands, ListaPreciosComercialCommand? comercial, CancellationToken cancellationToken = default)
            {
                Dictionary<Guid, ListaPreciosDetalleRecord> priceSnapshot = Detalles.ToDictionary(x => x.Key, x => x.Value);
                Dictionary<ListaPreciosIdentityKey, ListaPreciosComercialDto> commercialSnapshot = Comerciales.ToDictionary(x => x.Key, x => x.Value);
                try
                {
                    IReadOnlyList<Guid> ids = commands.Count == 0 ? Array.Empty<Guid>() : await GuardarPreciosMasivosAsync(idEmpresa, commands, cancellationToken);
                    if (FailCommercial) throw new InvalidOperationException("FALLO_COMERCIAL_QA");
                    if (comercial != null)
                        Comerciales[comercial.Identity] = new ListaPreciosComercialDto { Descripcion = comercial.Descripcion ?? string.Empty, DescripcionReadOnly = comercial.Identity.TipoIdentidad is ListaPreciosConstants.IdentidadVariante or ListaPreciosConstants.IdentidadPresentacionVenta, Web = comercial.Web ?? string.Empty, Liverpool = comercial.Liverpool ?? string.Empty, MercadoLibre = comercial.MercadoLibre ?? string.Empty, Observaciones = comercial.Observaciones ?? string.Empty, DosPorUno = comercial.DosPorUno, TresPorDos = comercial.TresPorDos, DescuentoSegundo = comercial.DescuentoSegundo, Monedero = comercial.Monedero, Materializada = true };
                    return ids;
                }
                catch { Detalles.Clear(); foreach (var x in priceSnapshot) Detalles[x.Key] = x.Value; Comerciales.Clear(); foreach (var x in commercialSnapshot) Comerciales[x.Key] = x.Value; throw; }
            }

            public Task BajaPrecioAsync(Guid idEmpresa, Guid idPrecio, Guid? usuarioId, CancellationToken cancellationToken = default)
            {
                if (Detalles.TryGetValue(idPrecio, out var detail))
                {
                    Detalles[idPrecio] = detail with { Activo = false, FechaActualizacion = DateTime.UtcNow };
                }

                return Task.CompletedTask;
            }

            public Task<IReadOnlyList<ListaPreciosHistorialDto>> ObtenerHistorialAsync(Guid idEmpresa, ListaPreciosIdentityKey identity, int? nivel, CancellationToken cancellationToken = default)
                => Task.FromResult<IReadOnlyList<ListaPreciosHistorialDto>>(Array.Empty<ListaPreciosHistorialDto>());

            public Task<ListaPreciosHistorialPaginaDto> ConsultarHistorialAsync(Guid idEmpresa, ListaPreciosHistorialConsultaRequest request, CancellationToken cancellationToken = default)
            {
                IEnumerable<ListaPreciosHistorialConsultaItemDto> query = idEmpresa == Empresa ? HistorialConsulta : Array.Empty<ListaPreciosHistorialConsultaItemDto>();
                if (request.FechaDesde.HasValue) query = query.Where(item => item.FechaUtc >= request.FechaDesde.Value.Date);
                if (request.FechaHasta.HasValue) query = query.Where(item => item.FechaUtc < request.FechaHasta.Value.Date.AddDays(1));
                if (request.Nivel.HasValue) query = query.Where(item => item.Nivel == request.Nivel);
                if (request.TipoIdentidad.HasValue) query = query.Where(item => item.TipoIdentidad == request.TipoIdentidad);
                if (!string.IsNullOrWhiteSpace(request.Busqueda)) query = query.Where(item => item.Identidad.Contains(request.Busqueda, StringComparison.OrdinalIgnoreCase) || item.Codigo.Contains(request.Busqueda, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(request.Origen)) query = query.Where(item => item.Origen == request.Origen);
                if (!string.IsNullOrWhiteSpace(request.Operacion)) query = query.Where(item => item.Operacion == request.Operacion);
                if (request.CorrelationId.HasValue) query = query.Where(item => item.CorrelationId == request.CorrelationId);
                if (!string.IsNullOrWhiteSpace(request.Usuario)) query = query.Where(item => item.Usuario.Contains(request.Usuario, StringComparison.OrdinalIgnoreCase));
                List<ListaPreciosHistorialConsultaItemDto> filtered = query.OrderByDescending(item => item.FechaUtc).ThenByDescending(item => item.Id).ToList();
                return Task.FromResult(new ListaPreciosHistorialPaginaDto
                {
                    Items = filtered.Skip((request.Pagina - 1) * request.TamanoPagina).Take(request.TamanoPagina).ToList(),
                    Pagina = request.Pagina,
                    TamanoPagina = request.TamanoPagina,
                    Total = filtered.Count,
                    TotalPaginas = filtered.Count == 0 ? 0 : (int)Math.Ceiling(filtered.Count / (double)request.TamanoPagina)
                });
            }

            private static bool SameIdentity(ListaPreciosDetalleRecord detail, ListaPreciosIdentityKey identity)
            {
                return detail.TipoIdentidad == identity.TipoIdentidad
                    && detail.IdProductoServicio == identity.IdProductoServicio
                    && detail.IdVariante == identity.IdVariante
                    && detail.IdPresentacionVenta == identity.IdPresentacionVenta;
            }

            private static bool IsVigente(ListaPreciosDetalleRecord detail)
            {
                DateTime today = DateTime.UtcNow.Date;
                return (!detail.VigenciaInicio.HasValue || detail.VigenciaInicio.Value.Date <= today)
                    && (!detail.VigenciaFin.HasValue || detail.VigenciaFin.Value.Date >= today);
            }
        }
    }
}
