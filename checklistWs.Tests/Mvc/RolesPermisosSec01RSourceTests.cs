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
        Assert.Contains("Escritura = 0", controller);
        Assert.Contains("Escritura = ordenesCompraNuevaAcceso && IsChecked(mnordenescompranuevaw) ? 1 : 0", controller);
        Assert.Contains("Escritura = ordenesCompraReporteAcceso && IsChecked(mnordenescomprareportew) ? 1 : 0", controller);
        Assert.Contains("Escritura = recepcionNuevaAcceso && IsChecked(mnrecepcionnuevaw) ? 1 : 0", controller);
        Assert.Contains("Escritura = recepcionReporteAcceso && IsChecked(mnrecepcionreportew) ? 1 : 0", controller);
        Assert.Contains("Escritura = curvasCatalogoAcceso && IsChecked(mncurvascatalogow) ? 1 : 0", controller);
        Assert.Contains("AddPermissionSwitch(result, \"#sw05003001A\", \"#sw05003001W\", ordenesCompraNueva)", controller);
        Assert.Contains("AddPermissionSwitch(result, \"#sw05004002A\", \"#sw05004002W\", recepcionReporte)", controller);
        Assert.Contains("AddPermissionSwitch(result, \"#sw05005001A\", \"#sw05005001W\", curvasCatalogo)", controller);
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
}
