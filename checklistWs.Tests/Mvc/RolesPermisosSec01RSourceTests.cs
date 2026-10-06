using Xunit;

namespace checklistWs.Tests.Mvc;

public sealed class RolesPermisosSec01RSourceTests
{
    private static readonly string RepoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
    private static readonly string InspectorRoot = Path.Combine(RepoRoot, "inspector", "checklist");

    [Fact]
    public void RolesPermisosViewRendersOcAndRecepcionChildren()
    {
        string view = File.ReadAllText(Path.Combine(InspectorRoot, "Views", "RolesPermisos", "RolesPermisos.cshtml"));

        Assert.Contains("sw05003000A", view);
        Assert.Contains("sw05003001A", view);
        Assert.Contains("sw05003001W", view);
        Assert.Contains("sw05003002A", view);
        Assert.Contains("sw05003002W", view);
        Assert.Contains("sw05004000A", view);
        Assert.Contains("sw05004001A", view);
        Assert.Contains("sw05004001W", view);
        Assert.Contains("sw05004002A", view);
        Assert.Contains("sw05004002W", view);
        Assert.Contains("sw05005000A", view);
        Assert.Contains("sw05005001A", view);
        Assert.Contains("sw05005001W", view);
        Assert.Contains("sw05005002A", view);
        Assert.Contains("sw05005002W", view);
        Assert.Contains("sw05001008A", view);
        Assert.Contains("sw05001009A", view);
        Assert.Contains("sw05001009W", view);
    }

    [Fact]
    public void RolesPermisosJsKeepsOcAndRecepcionChildrenVisible()
    {
        string js = File.ReadAllText(Path.Combine(InspectorRoot, "wwwroot", "js", "RolesPermisos", "RolesPermisos.js"));

        Assert.Contains("$('#areaOrdenesCompra').show();", js);
        Assert.Contains("$('#areaRecepcion').show();", js);
        Assert.DoesNotContain("$('#areaOrdenesCompra').hide();", js);
        Assert.DoesNotContain("$('#areaRecepcion').hide();", js);
        Assert.Contains("mnordenescompranueva", js);
        Assert.Contains("mnordenescompranuevaw", js);
        Assert.Contains("mnordenescomprareporte", js);
        Assert.Contains("mnordenescomprareportew", js);
        Assert.Contains("mnrecepcionnueva", js);
        Assert.Contains("mnrecepcionnuevaw", js);
        Assert.Contains("mnrecepcionreporte", js);
        Assert.Contains("mnrecepcionreportew", js);
        Assert.Contains("mncurvas", js);
        Assert.Contains("mncurvascatalogo", js);
        Assert.Contains("mncurvascatalogow", js);
        Assert.Contains("mncurvassiembra", js);
        Assert.Contains("mncurvassiembraw", js);
        Assert.Contains("mnlistaprecios", js);
        Assert.Contains("mnlistapreciosadmin", js);
        Assert.Contains("mnlistapreciosadminw", js);
    }

    [Fact]
    public void RolesPermisosControllerPersistsChildrenWriteAndAccessOnlyParents()
    {
        string controller = File.ReadAllText(Path.Combine(InspectorRoot, "Controllers", "RolesPermisos", "RolesPermisosController.cs"));

        Assert.Contains("OrdenesCompraPermissionCode = \"05003000\"", controller);
        Assert.Contains("OrdenesCompraNuevaPermissionCode = \"05003001\"", controller);
        Assert.Contains("OrdenesCompraReportePermissionCode = \"05003002\"", controller);
        Assert.Contains("RecepcionPermissionCode = \"05004000\"", controller);
        Assert.Contains("RecepcionNuevaPermissionCode = \"05004001\"", controller);
        Assert.Contains("RecepcionReportePermissionCode = \"05004002\"", controller);
        Assert.Contains("CurvasPermissionCode = \"05005000\"", controller);
        Assert.Contains("CurvasCatalogoPermissionCode = \"05005001\"", controller);
        Assert.Contains("CurvasSiembraPermissionCode = \"05005002\"", controller);
        Assert.Contains("ListaPreciosPermissionCode = \"05001008\"", controller);
        Assert.Contains("ListaPreciosAdministrarPermissionCode = \"05001009\"", controller);
        Assert.Contains("Escritura = 0", controller);
        Assert.Contains("Escritura = listaPreciosAdminAcceso && IsChecked(mnlistapreciosadminw) ? 1 : 0", controller);
        Assert.Contains("Escritura = ordenesCompraNuevaAcceso && IsChecked(mnordenescompranuevaw) ? 1 : 0", controller);
        Assert.Contains("Escritura = ordenesCompraReporteAcceso && IsChecked(mnordenescomprareportew) ? 1 : 0", controller);
        Assert.Contains("Escritura = recepcionNuevaAcceso && IsChecked(mnrecepcionnuevaw) ? 1 : 0", controller);
        Assert.Contains("Escritura = recepcionReporteAcceso && IsChecked(mnrecepcionreportew) ? 1 : 0", controller);
        Assert.Contains("Escritura = curvasCatalogoAcceso && IsChecked(mncurvascatalogow) ? 1 : 0", controller);
        Assert.Contains("Escritura = curvasSiembraAcceso && IsChecked(mncurvassiembraw) ? 1 : 0", controller);
        Assert.Contains("AddPermissionSwitch(result, \"#sw05003001A\", \"#sw05003001W\", ordenesCompraNueva)", controller);
        Assert.Contains("AddPermissionSwitch(result, \"#sw05004002A\", \"#sw05004002W\", recepcionReporte)", controller);
        Assert.Contains("AddPermissionSwitch(result, \"#sw05005001A\", \"#sw05005001W\", curvasCatalogo)", controller);
        Assert.Contains("AddPermissionSwitch(result, \"#sw05005002A\", \"#sw05005002W\", curvasSiembra)", controller);
        Assert.Contains("AddPermissionSwitch(result, \"#sw05001008A\", null, listaPrecios)", controller);
        Assert.Contains("AddPermissionSwitch(result, \"#sw05001009A\", \"#sw05001009W\", listaPreciosAdmin)", controller);
    }

    [Fact]
    public void ListaPreciosMvcRouteIsFunctionalAndPermissionProtected()
    {
        string controller = File.ReadAllText(Path.Combine(InspectorRoot, "Controllers", "ListaPrecios", "ListaPreciosController.cs"));
        string view = File.ReadAllText(Path.Combine(InspectorRoot, "Views", "ListaPrecios", "Index.cshtml"));

        Assert.Contains("ListaPreciosPermissionCode = \"05001008\"", controller);
        Assert.Contains("ProveeduriaMenuBuilder.HasOfficialSuperAdminPermission(ListaPreciosPermissionCode, requireWrite: false)", controller);
        Assert.Contains("FindPermission(JsonNode.Parse(permisos), ListaPreciosPermissionCode)", controller);
        Assert.Contains("data-lista-precios-page=\"index\"", view);
        Assert.Contains("grListaPrecios", view);
        Assert.Contains("Lista de precio", view);
        Assert.Contains("Exportar Excel", view);
        Assert.Contains("modalListaPreciosEditor", view);
        Assert.Contains("Guardar cambios", view);
        Assert.Contains("lpEditorMatrizRows", view);
    }

    [Fact]
    public void ListaPreciosLp09MvcUsesLp08EndpointsAndNoClientSideCommercialFormula()
    {
        string controller = File.ReadAllText(Path.Combine(InspectorRoot, "Controllers", "ListaPrecios", "ListaPreciosController.cs"));
        string view = File.ReadAllText(Path.Combine(InspectorRoot, "Views", "ListaPrecios", "Index.cshtml"));
        string script = File.ReadAllText(Path.Combine(InspectorRoot, "wwwroot", "js", "ListaPrecios", "ListaPrecios.js"));

        Assert.Contains("ListaPreciosAdministrarPermissionCode = \"05001009\"", controller);
        Assert.Contains("public Task<IActionResult> Preview() => ProxyPostAsync(\"Preview\", requireWrite: false)", controller);
        Assert.Contains("public Task<IActionResult> GuardarPrecio() => ProxyPostAsync(\"GuardarPrecio\", requireWrite: true)", controller);
        Assert.Contains("public Task<IActionResult> Historial() => ProxyPostAsync(\"Historial\", requireWrite: false)", controller);
        Assert.Contains("public Task<IActionResult> BajaPrecio(Guid idPrecio) => ProxyPostAsync($\"BajaPrecio/{idPrecio:D}\", requireWrite: true)", controller);
        Assert.Contains("CanAdministrarListaPreciosMvc", controller);
        Assert.Contains("data-can-write", view);
        Assert.Contains("lpEditorMatrizRows", view);
        Assert.Contains("P@(nivel)", view);
        Assert.Contains("D@(nivel)%", view);
        Assert.Contains("postJson(\"/ListaPrecios/PreviewMatriz\"", script);
        Assert.Contains("postJson(\"/ListaPrecios/GuardarMatriz\"", script);
        Assert.Contains("postJson(\"/ListaPrecios/Historial\"", script);
        Assert.Contains("postJson(\"/ListaPrecios/BajaPrecio/\"", script);
        Assert.Contains("renderPreview", script);
        Assert.Contains("document.addEventListener(\"click\", handleDynamicGridAction, true)", script);
        Assert.Contains("action.closest(\"#gridListaPreciosHost\")", script);
        Assert.DoesNotContain("$(\"#grListaPrecios\").on(\"click\", \"[data-lp-edit-row]\"", script);
        Assert.Contains("function applyCommercialRounding", script);
        Assert.Contains("function calculateRoundedFinal", script);
        Assert.Contains("postJson(\"/ListaPrecios/GuardarMatriz\"", script);
    }

    [Fact]
    public void ListaPreciosLpQa02UsesCompactCheckAppFilterCompositionAndPublicCopy()
    {
        string view = File.ReadAllText(Path.Combine(InspectorRoot, "Views", "ListaPrecios", "Index.cshtml"));
        string script = File.ReadAllText(Path.Combine(InspectorRoot, "wwwroot", "js", "ListaPrecios", "ListaPrecios.js"));
        string css = File.ReadAllText(Path.Combine(InspectorRoot, "wwwroot", "css", "ListaPrecios", "ListaPrecios.css"));

        Assert.Contains("<h1>Lista de Precios</h1>", view);
        Assert.Contains("<p>Consulta y edición por producto o servicio</p>", view);
        Assert.DoesNotContain("Listado de precios por identidad vendible", view);
        Assert.DoesNotContain("motor de resolución", view);
        Assert.DoesNotContain("fallback", view);
        Assert.DoesNotContain("Identidad vendible", view);
        Assert.DoesNotContain("Identidad vendible", script);
        Assert.DoesNotContain("fallback", script);
        Assert.DoesNotContain("tenant", script);

        Assert.Contains("lp-filter-row--primary", view);
        Assert.Contains("lp-filter-row--classification", view);
        Assert.Contains("lp-filter-row--commercial", view);
        Assert.Contains("lp-inventory-filter-group", view);
        Assert.Contains("lp-inventory-filter-controls", view);
        Assert.Contains("lp-filter-footer", view);
        Assert.Contains("class=\"lp-photo-switch-track\"", view);
        Assert.DoesNotContain("lp-photo-switch ps-switch-card", view);
        Assert.Contains("lp-results-head", view);

        Assert.Contains("grid-template-columns: minmax(18rem, 2.2fr) repeat(3", css);
        Assert.Contains("grid-template-columns: repeat(5, minmax(0, 1fr))", css);
        Assert.Contains("justify-content: space-between", css);
        Assert.Contains("@media (max-width: 1199.98px)", css);
        Assert.Contains("@media (max-width: 767.98px)", css);
    }

    [Fact]
    public void ListaPreciosLp12UsesOperationalGridWithoutAdvancingReservedActions()
    {
        string view = File.ReadAllText(Path.Combine(InspectorRoot, "Views", "ListaPrecios", "Index.cshtml"));
        string script = File.ReadAllText(Path.Combine(InspectorRoot, "wwwroot", "js", "ListaPrecios", "ListaPrecios.js"));

        Assert.Contains("mobileCardTitleKey: \"nombre\"", script);
        Assert.Contains("key: \"descuentoPct\"", script);
        Assert.Contains("title: \"Descuento\"", script);
        Assert.Contains("key: \"precioFinal\"", script);
        Assert.Contains("title: \"Precio final\"", script);
        Assert.Contains("columnToggleButtonSelector: \"#btColumnasListaPrecios\"", script);
        Assert.Contains("searchInputSelector: \"#txBusquedaGridListaPrecios\"", script);
        Assert.Contains("footerPageSizeSelector: \"#txGridListaPreciosPageSize\"", script);
        Assert.Contains("<th>Descuento</th>", view);
        Assert.Contains("<th>Precio final</th>", view);
        Assert.Contains("Copiar lista", view);
        Assert.Contains("Sugerir acciones", view);
        Assert.DoesNotContain("data-lp-existencias", script);
    }

    [Fact]
    public void ListaPreciosLp13IntegratesInventoryReadOnlyWithoutReservedFeatures()
    {
        string mvcController = File.ReadAllText(Path.Combine(InspectorRoot, "Controllers", "ListaPrecios", "ListaPreciosController.cs"));
        string view = File.ReadAllText(Path.Combine(InspectorRoot, "Views", "ListaPrecios", "Index.cshtml"));
        string script = File.ReadAllText(Path.Combine(InspectorRoot, "wwwroot", "js", "ListaPrecios", "ListaPrecios.js"));
        string apiController = File.ReadAllText(Path.Combine(RepoRoot, "inspectorapi", "checklistWs", "Controllers", "ListaPrecios", "ListaPreciosController.cs"));
        string service = File.ReadAllText(Path.Combine(RepoRoot, "inspectorapi", "checklistWs", "Services", "Tenant", "ListaPreciosService.cs"));

        Assert.Contains("public Task<IActionResult> Inventario() => ProxyGetAsync(\"Inventario\")", mvcController);
        Assert.Contains("[HttpGet(\"Inventario\")]", apiController);
        Assert.Contains("AccessPermissionCode, ProductosServiciosPermissionRequirement.Read", apiController);
        Assert.Contains("FROM dbo.InventarioSaldos", service);
        Assert.Contains("FROM dbo.InventarioMovimientos", service);
        Assert.Contains("WHERE s.idEmpresa=@IdEmpresa", service);
        Assert.Contains("WHERE m.idEmpresa=@IdEmpresa", service);
        Assert.Contains("modalListaPreciosInventario", view);
        Assert.Contains("data-lp-inventory-row", script);
        Assert.Contains("key: \"existencia\"", script);
        Assert.Contains("exportable: false", script);
        Assert.Contains("Copiar lista", view);
        Assert.Contains("Sugerencias comerciales", view);
    }

    [Fact]
    public void ListaPreciosLp14RequiresExplicitSelectionPreviewAndWritePermission()
    {
        string mvcController = File.ReadAllText(Path.Combine(InspectorRoot, "Controllers", "ListaPrecios", "ListaPreciosController.cs"));
        string view = File.ReadAllText(Path.Combine(InspectorRoot, "Views", "ListaPrecios", "Index.cshtml"));
        string script = File.ReadAllText(Path.Combine(InspectorRoot, "wwwroot", "js", "ListaPrecios", "ListaPrecios.js"));
        string apiController = File.ReadAllText(Path.Combine(RepoRoot, "inspectorapi", "checklistWs", "Controllers", "ListaPrecios", "ListaPreciosController.cs"));
        string service = File.ReadAllText(Path.Combine(RepoRoot, "inspectorapi", "checklistWs", "Services", "Tenant", "ListaPreciosService.cs"));

        Assert.Contains("Ajuste masivo", view);
        Assert.Contains("data-lp-select-row", script);
        Assert.Contains("state.selected", script);
        Assert.Contains("Se aplicará a los productos con los filtros actuales.", view);
        Assert.Contains("identidades: state.rows.map", script);
        Assert.Contains("PreviewAjusteMasivo", mvcController);
        Assert.Contains("EjecutarAjusteMasivo", mvcController);
        Assert.Contains("PreviewAjusteMasivo\", requireWrite: false", mvcController);
        Assert.Contains("EjecutarAjusteMasivo\", requireWrite: true", mvcController);
        Assert.Contains("AccessPermissionCode, ProductosServiciosPermissionRequirement.Read", apiController);
        Assert.Contains("AdminPermissionCode, ProductosServiciosPermissionRequirement.Write", apiController);
        Assert.Contains("BeginTransaction(IsolationLevel.Serializable)", service);
        Assert.Contains("OrigenHistorialMasivo", service);
        Assert.Contains("Copiar lista", view);
    }

    [Fact]
    public void ListaPreciosLp15RequiresExplicitModePreviewConfirmationAndWritePermission()
    {
        string mvcController = File.ReadAllText(Path.Combine(InspectorRoot, "Controllers", "ListaPrecios", "ListaPreciosController.cs"));
        string view = File.ReadAllText(Path.Combine(InspectorRoot, "Views", "ListaPrecios", "Index.cshtml"));
        string script = File.ReadAllText(Path.Combine(InspectorRoot, "wwwroot", "js", "ListaPrecios", "ListaPrecios.js"));
        string apiController = File.ReadAllText(Path.Combine(RepoRoot, "inspectorapi", "checklistWs", "Controllers", "ListaPrecios", "ListaPreciosController.cs"));
        string service = File.ReadAllText(Path.Combine(RepoRoot, "inspectorapi", "checklistWs", "Services", "Tenant", "ListaPreciosService.cs"));

        Assert.Contains("btCopiarListaPrecios", view);
        Assert.Contains("SOBRESCRIBIR", view);
        Assert.DoesNotContain(">Merge<", view);
        Assert.Contains("Copiar también descuentos", view);
        Assert.Contains("confirmCheckApp(\"Se copiará la Lista", script);
        Assert.Contains("PreviewCopiarLista\", requireWrite: false", mvcController);
        Assert.Contains("EjecutarCopiarLista\", requireWrite: true", mvcController);
        Assert.Contains("PreviewCopiarListaAsync", apiController);
        Assert.Contains("ProductosServiciosPermissionRequirement.Read", apiController);
        Assert.Contains("EjecutarCopiarListaAsync", apiController);
        Assert.Contains("ProductosServiciosPermissionRequirement.Write", apiController);
        Assert.Contains("OrigenHistorialCopiaLista", service);
        Assert.Contains("PrepareCopyAsync", service);
        Assert.Contains("/ListaPrecios/PreviewCopiarLista", script);
        Assert.Contains("/ListaPrecios/EjecutarCopiarLista", script);
    }

    [Fact]
    public void ListaPreciosLp16UsesBrandCatalogPreviewConfirmationAndExistingPermissions()
    {
        string mvcController = File.ReadAllText(Path.Combine(InspectorRoot, "Controllers", "ListaPrecios", "ListaPreciosController.cs"));
        string view = File.ReadAllText(Path.Combine(InspectorRoot, "Views", "ListaPrecios", "Index.cshtml"));
        string script = File.ReadAllText(Path.Combine(InspectorRoot, "wwwroot", "js", "ListaPrecios", "ListaPrecios.js"));
        string apiController = File.ReadAllText(Path.Combine(RepoRoot, "inspectorapi", "checklistWs", "Controllers", "ListaPrecios", "ListaPreciosController.cs"));
        string service = File.ReadAllText(Path.Combine(RepoRoot, "inspectorapi", "checklistWs", "Services", "Tenant", "ListaPreciosService.cs"));

        Assert.Contains("btDescuentoMarcaListaPrecios", view);
        Assert.Contains("lpBrandDiscountMarca", view);
        Assert.Contains("Descuento % (0–100)", view);
        Assert.Contains("confirmCheckApp(\"Se aplicará \"", script);
        Assert.Contains("PreviewDescuentoMarca\", requireWrite: false", mvcController);
        Assert.Contains("EjecutarDescuentoMarca\", requireWrite: true", mvcController);
        Assert.Contains("PreviewDescuentoMarcaAsync", apiController);
        Assert.Contains("ProductosServiciosPermissionRequirement.Read", apiController);
        Assert.Contains("EjecutarDescuentoMarcaAsync", apiController);
        Assert.Contains("ProductosServiciosPermissionRequirement.Write", apiController);
        Assert.Contains("OrigenHistorialDescuentoMarca", service);
        Assert.Contains("ConsultarIdentidadesAsync", service);
        Assert.Contains("/ListaPrecios/PreviewDescuentoMarca", script);
        Assert.Contains("/ListaPrecios/EjecutarDescuentoMarca", script);
        Assert.DoesNotContain("payload.descuentoPct =", script);
    }

    [Fact]
    public void ListaPreciosLp17ExportsFilteredUniverseWithOperationalColumnsAndReadSecurity()
    {
        string view = File.ReadAllText(Path.Combine(InspectorRoot, "Views", "ListaPrecios", "Index.cshtml"));
        string script = File.ReadAllText(Path.Combine(InspectorRoot, "wwwroot", "js", "ListaPrecios", "ListaPrecios.js"));
        string checkappUi = File.ReadAllText(Path.Combine(InspectorRoot, "wwwroot", "js", "checkapp-ui.js"));
        string mvcController = File.ReadAllText(Path.Combine(InspectorRoot, "Controllers", "ListaPrecios", "ListaPreciosController.cs"));
        string apiController = File.ReadAllText(Path.Combine(RepoRoot, "inspectorapi", "checklistWs", "Controllers", "ListaPrecios", "ListaPreciosController.cs"));

        Assert.Contains("id=\"btExportarListaPrecios\"", view);
        Assert.Contains("exportButtonSelector: \"#btExportarListaPrecios\"", script);
        Assert.Contains("ListaPrecios_\" + formatDateForFile(new Date()) + \".xlsx", script);
        Assert.Contains("rows({ search: \"applied\" })", checkappUi);
        Assert.DoesNotContain("rows({ page: \"current\"", checkappUi);
        Assert.Contains("grid.rows[index]", checkappUi);
        Assert.Contains("exportLock", checkappUi);
        Assert.Contains("Se generó un archivo sin registros", checkappUi);

        string[] filters =
        {
            "nivel", "busqueda", "tipo", "idCategoria", "idMarca", "idColeccion",
            "idEtiqueta", "idAtributo", "idVariante", "idPresentacionVenta",
            "precioMinimo", "precioMaximo", "descuento", "sucursales",
            "existencia", "cantidadMenorA", "estatus"
        };
        foreach (string filter in filters)
        {
            Assert.Contains($"appendQuery(query, \"{filter}\"", script);
        }

        string[] headers =
        {
            "Identidad", "Código", "Tipo", "Categoría", "Marca", "Lista seleccionada",
            "Precio Base", "Precio Lista", "Descuento", "Precio final", "Origen",
            "Estatus", "Existencia"
        };
        foreach (string header in headers)
        {
            Assert.Contains($"title: \"{header}\"", script);
        }

        Assert.Contains("key: \"precioBase\"", script);
        Assert.Contains("key: \"existencia\"", script);
        Assert.Contains("row.inventarioAplicable", script);
        Assert.Contains(": \"N/A\"", script);
        Assert.Contains("value !== null && value !== undefined ? value : 0", script);
        Assert.Contains("type: \"currency\"", script);
        Assert.Contains("type: \"number\"", script);

        Assert.Contains("key: \"imagenUrl\"", script);
        Assert.DoesNotContain("key: \"acciones\"", script);
        Assert.True(CountOccurrences(script, "exportable: false") >= 2);
        Assert.DoesNotContain("title: \"CorrelationId\"", script);
        Assert.DoesNotContain("title: \"Tenant\"", script);
        Assert.DoesNotContain("title: \"idEmpresa\"", script);

        Assert.Contains("public Task<IActionResult> Consultar() => ProxyGetAsync(\"Consulta\")", mvcController);
        Assert.Contains("AccessPermissionCode, ProductosServiciosPermissionRequirement.Read", apiController);
        Assert.DoesNotContain("[FromQuery] Guid idEmpresa", apiController);
    }

    [Fact]
    public void ListaPreciosLp18ExposesReadOnlyServerPagedAuditConsole()
    {
        string view = File.ReadAllText(Path.Combine(InspectorRoot, "Views", "ListaPrecios", "Index.cshtml"));
        string script = File.ReadAllText(Path.Combine(InspectorRoot, "wwwroot", "js", "ListaPrecios", "ListaPrecios.js"));
        string mvcController = File.ReadAllText(Path.Combine(InspectorRoot, "Controllers", "ListaPrecios", "ListaPreciosController.cs"));
        string apiController = File.ReadAllText(Path.Combine(RepoRoot, "inspectorapi", "checklistWs", "Controllers", "ListaPrecios", "ListaPreciosController.cs"));
        string service = File.ReadAllText(Path.Combine(RepoRoot, "inspectorapi", "checklistWs", "Services", "Tenant", "ListaPreciosService.cs"));

        Assert.Contains("modalListaPreciosHistorial", view);
        Assert.Contains("lpAuditCorrelation", view);
        Assert.Contains("HistorialConsulta?", script);
        Assert.Contains("tamanoPagina", script);
        Assert.Contains("data-lp-audit-correlation", script);
        Assert.Contains("public Task<IActionResult> HistorialConsulta() => ProxyGetAsync(\"HistorialConsulta\")", mvcController);
        Assert.Contains("[HttpGet(\"HistorialConsulta\")]", apiController);
        Assert.Contains("AccessPermissionCode, ProductosServiciosPermissionRequirement.Read", apiController);
        Assert.Contains("ORDER BY h.FechaUtc DESC, h.id DESC", service);
        Assert.Contains("OFFSET @Offset ROWS FETCH NEXT @TamanoPagina ROWS ONLY", service);
        Assert.Contains("WHERE h.idEmpresa=@IdEmpresa", service);
        Assert.DoesNotContain("HistorialExcel", view);
        Assert.DoesNotContain("HistorialExcel", script);
    }

    [Fact]
    public void ListaPreciosApiUsesDedicatedReadAndAdminPermissions()
    {
        string controller = File.ReadAllText(Path.Combine(RepoRoot, "inspectorapi", "checklistWs", "Controllers", "ListaPrecios", "ListaPreciosController.cs"));
        string defaults = File.ReadAllText(Path.Combine(RepoRoot, "inspectorapi", "checklistWs", "Services", "Tenant", "ProductosServiciosAuthorizationModels.cs"));
        string service = File.ReadAllText(Path.Combine(RepoRoot, "inspectorapi", "checklistWs", "Services", "Tenant", "ProductosServiciosAuthorizationService.cs"));

        Assert.Contains("ListaPreciosPermissionCode = \"05001008\"", defaults);
        Assert.Contains("ListaPreciosAdministrarPermissionCode = \"05001009\"", defaults);
        Assert.Contains("AccessPermissionCode = ProductosServiciosAuthorizationDefaults.ListaPreciosPermissionCode", controller);
        Assert.Contains("AdminPermissionCode = ProductosServiciosAuthorizationDefaults.ListaPreciosAdministrarPermissionCode", controller);
        Assert.Contains("TryResolveContextAsync(idEmpresa, AccessPermissionCode, ProductosServiciosPermissionRequirement.Read", controller);
        Assert.Contains("TryResolveContextAsync(idEmpresa, AdminPermissionCode, ProductosServiciosPermissionRequirement.Write", controller);
        Assert.Contains("PermissionCode = AccessPermissionCode", controller);
        Assert.Contains("PermissionCode = permissionCode", controller);
        Assert.DoesNotContain("ProductosServiciosAuthorizationDefaults.AbcPermissionCode", controller);
        Assert.Contains("ProductosServiciosAuthorizationDefaults.ListaPreciosPermissionCode", service);
    }

    [Fact]
    public void RolesPermisosControllerDisplaysOfficialSuperAdminPermissionsWithoutEditingJson()
    {
        string controller = File.ReadAllText(Path.Combine(InspectorRoot, "Controllers", "RolesPermisos", "RolesPermisosController.cs"));

        Assert.Contains("role.NombreRol?.Trim(), \"SuperAdmin\"", controller);
        Assert.Contains("ProveeduriaMenuBuilder.AddOfficialSuperAdminPermissions(lstPerm)", controller);
        Assert.DoesNotContain("role.Permisos = ProveeduriaMenuBuilder", controller);
        Assert.DoesNotContain("item.Permisos = ProveeduriaMenuBuilder", controller);
    }

    [Fact]
    public void RolesPermisosJsKeepsSuperAdminProtectedFromManualSave()
    {
        string js = File.ReadAllText(Path.Combine(InspectorRoot, "wwwroot", "js", "RolesPermisos", "RolesPermisos.js"));

        Assert.Contains("No se pueden cambiar los permisos del SuperAdmin", js);
        Assert.Contains("$('#cbRol option:selected').text() != 'SuperAdmin'", js);
        Assert.Contains("aplicaProteccionRol();", js);
        Assert.Contains("setSwitchesRolProtegido($('#cbRol option:selected').text() === 'SuperAdmin')", js);
        Assert.Contains("$('input.form-check-input[role=\"switch\"]').prop('disabled', protegido)", js);
    }

    [Fact]
    public void OrdenesCompraMvcAuthzAllowsSuperAdminThroughOfficialResolver()
    {
        string controller = File.ReadAllText(Path.Combine(InspectorRoot, "Controllers", "Activos", "OrdenesCompraController.cs"));

        Assert.Contains("IsSuperAdminSession()", controller);
        Assert.Contains("ProveeduriaMenuBuilder.HasOfficialSuperAdminPermission(permissionCode, requireWrite)", controller);
        Assert.Contains("OrdenesCompraNuevaPermissionCode = \"05003001\"", controller);
        Assert.Contains("OrdenesCompraReportePermissionCode = \"05003002\"", controller);
        Assert.DoesNotContain("05003000, requireWrite", controller);
    }

    [Fact]
    public void OrdenesCompraMvcProxyUsesOfficialIdentityResolverForFirebaseUidSessions()
    {
        string helper = File.ReadAllText(Path.Combine(InspectorRoot, "Clases", "CheckAppProxyHeaders.cs"));
        string controller = File.ReadAllText(Path.Combine(InspectorRoot, "Controllers", "Activos", "OrdenesCompraController.cs"));

        Assert.Contains("public static string? ResolveCheckAppUsuarioId(this Controller controller)", helper);
        Assert.Contains("IsSafeUsuarioId(safeClaim)", helper);
        Assert.Contains("return this.ResolveCheckAppUsuarioId();", controller);
        Assert.DoesNotContain("Guid.TryParse(claimValue, out Guid usuarioId) && usuarioId != Guid.Empty", controller);
    }

    [Fact]
    public void HomeMenuUsesOfficialSuperAdminPermissionsWithoutEditingRoleJson()
    {
        string controller = File.ReadAllText(Path.Combine(InspectorRoot, "Controllers", "HomeController.cs"));

        Assert.Contains("role.NombreRol?.Trim(), \"SuperAdmin\"", controller);
        Assert.Contains("ProveeduriaMenuBuilder.AddOfficialSuperAdminPermissions(opciones)", controller);
        Assert.Contains("List<Opciones> opciones = JsonConvert.DeserializeObject<List<Opciones>>(cookieValue)", controller);
        Assert.DoesNotContain("? ProveeduriaMenuBuilder.OfficialSuperAdminPermissions().ToList()", controller);
        Assert.DoesNotContain("ProveeduriaMenuBuilder.BuildForSuperAdmin()", controller);
        Assert.DoesNotContain("role.Permisos = ProveeduriaMenuBuilder", controller);
    }

    private static int CountOccurrences(string source, string value)
    {
        int count = 0;
        int index = 0;
        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}
