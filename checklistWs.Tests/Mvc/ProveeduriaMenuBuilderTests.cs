using checklist.Clases;
using checklist.Models.Roles;
using Xunit;

namespace checklistWs.Tests.Mvc;

public sealed class ProveeduriaMenuBuilderTests
{
    [Fact]
    public void BuildDoesNotGrantOrdenesCompraChildrenFromParentOnly()
    {
        string html = ProveeduriaMenuBuilder.Build(new[]
        {
            Option("05000000", 1, Option("05003000", 1))
        });

        Assert.DoesNotContain(@"id=""menu-proveeduria-ordenes-compra-nueva""", html);
        Assert.DoesNotContain(@"id=""menu-proveeduria-ordenes-compra-reporte""", html);
    }

    [Fact]
    public void BuildShowsOrdenesCompraChildrenOnlyWhenExplicitlyGranted()
    {
        string html = ProveeduriaMenuBuilder.Build(new[]
        {
            Option("05000000", 1, Option("05003000", 1, Option("05003001", 1), Option("05003002", 1)))
        });

        Assert.Contains(@"id=""menu-proveeduria-ordenes-compra""", html);
        Assert.Contains(@"id=""menu-proveeduria-ordenes-compra-nueva""", html);
        Assert.Contains(@"id=""menu-proveeduria-ordenes-compra-reporte""", html);
    }

    [Fact]
    public void BuildShowsRecepcionAsSiblingOfOrdenesCompra()
    {
        string html = ProveeduriaMenuBuilder.Build(new[]
        {
            Option("05000000", 1,
                Option("05003000", 1, Option("05003001", 1)),
                Option("05004000", 1, Option("05004001", 1), Option("05004002", 1)))
        });

        Assert.Contains(@"id=""menu-proveeduria-ordenes-compra""", html);
        Assert.DoesNotContain(@"id=""menu-proveeduria-recepcion""", html);
        Assert.DoesNotContain(@"/Activos/Recepcion/Nueva", html);
        Assert.DoesNotContain(@"/Activos/Recepcion/Reporte", html);
    }

    [Fact]
    public void BuildAcceptsFlatExplicitChildrenWithoutParentFallback()
    {
        string html = ProveeduriaMenuBuilder.Build(new[]
        {
            Option("05003000", 1),
            Option("05003002", 1),
            Option("05004000", 1),
            Option("05004002", 1)
        });

        Assert.DoesNotContain(@"id=""menu-proveeduria-ordenes-compra-nueva""", html);
        Assert.Contains(@"id=""menu-proveeduria-ordenes-compra-reporte""", html);
        Assert.DoesNotContain(@"id=""menu-proveeduria-recepcion-nueva""", html);
        Assert.DoesNotContain(@"id=""menu-proveeduria-recepcion-reporte""", html);
    }

    [Fact]
    public void BuildDoesNotRenderRecepcionLinksUntilFunctionalUiExists()
    {
        string html = ProveeduriaMenuBuilder.Build(new[]
        {
            Option("05000000", 1, Option("05004000", 1, Option("05004001", 1), Option("05004002", 1)))
        });

        Assert.DoesNotContain(@"id=""menu-proveeduria-recepcion""", html);
        Assert.DoesNotContain(@"/Activos/Recepcion/Nueva", html);
        Assert.DoesNotContain(@"/Activos/Recepcion/Reporte", html);
    }

    [Fact]
    public void BuildForSuperAdminShowsAllFunctionalOrdenesCompraRoutesWithoutRecepcionLinks()
    {
        string html = ProveeduriaMenuBuilder.BuildForSuperAdmin();

        Assert.Contains(@"id=""menu-proveeduria-productos-servicios""", html);
        Assert.Contains(@"id=""menu-proveeduria-proveedores""", html);
        Assert.Contains(@"id=""menu-proveeduria-ordenes-compra""", html);
        Assert.Contains(@"id=""menu-proveeduria-ordenes-compra-nueva""", html);
        Assert.Contains(@"id=""menu-proveeduria-ordenes-compra-reporte""", html);
        Assert.DoesNotContain(@"id=""menu-proveeduria-recepcion""", html);
        Assert.DoesNotContain(@"/Activos/Recepcion/Nueva", html);
        Assert.DoesNotContain(@"/Activos/Recepcion/Reporte", html);
    }

    [Fact]
    public void AddOfficialSuperAdminPermissionsKeepsExistingGlobalMenuOptions()
    {
        List<Opciones> existingMenu =
        [
            Option("01000000", 1, Option("01001000", 1)),
            Option("02000000", 1, Option("02001000", 1)),
            Option("03000000", 1, Option("03001000", 1)),
            Option("04000000", 1, Option("04001000", 1)),
            Option("05000000", 1, Option("05001000", 1, Option("05001001", 1))),
            Option("06000000", 1, Option("06001000", 1)),
            Option("07000000", 1, Option("07001000", 1)),
            Option("08000000", 1, Option("08001000", 1))
        ];

        List<Opciones> merged = ProveeduriaMenuBuilder.AddOfficialSuperAdminPermissions(existingMenu);
        string proveeduriaHtml = ProveeduriaMenuBuilder.Build(merged);

        Assert.Contains(merged, option => option.Opcion == "01000000");
        Assert.Contains(merged, option => option.Opcion == "02000000");
        Assert.Contains(merged, option => option.Opcion == "03000000");
        Assert.Contains(merged, option => option.Opcion == "04000000");
        Assert.Contains(merged, option => option.Opcion == "05000000");
        Assert.Contains(merged, option => option.Opcion == "06000000");
        Assert.Contains(merged, option => option.Opcion == "07000000");
        Assert.Contains(merged, option => option.Opcion == "08000000");
        Assert.Contains(@"id=""menu-proveeduria-ordenes-compra-nueva""", proveeduriaHtml);
        Assert.Contains(@"id=""menu-proveeduria-ordenes-compra-reporte""", proveeduriaHtml);
        Assert.DoesNotContain(@"id=""menu-proveeduria-recepcion""", proveeduriaHtml);
    }

    [Fact]
    public void AddOfficialSuperAdminPermissionsKeepsExistingOptionsWhenFutureProveeduriaPermissionIsRegistered()
    {
        List<Opciones> existingMenu =
        [
            Option("01000000", 1),
            Option("02000000", 1),
            Option("05000000", 1)
        ];

        List<Opciones> merged = ProveeduriaMenuBuilder.AddOfficialSuperAdminPermissions(existingMenu);
        List<Opciones> mergedAgain = ProveeduriaMenuBuilder.AddOfficialSuperAdminPermissions(merged);

        Assert.Equal(3, mergedAgain.Count);
        Assert.Contains(mergedAgain, option => option.Opcion == "01000000");
        Assert.Contains(mergedAgain, option => option.Opcion == "02000000");
        Assert.Contains(mergedAgain, option => option.Opcion == "05000000");
        Opciones proveeduria = mergedAgain.Single(option => option.Opcion == "05000000");
        Opciones ordenesCompra = proveeduria.Hijos.Single(option => option.Opcion == "05003000");
        Assert.Contains(ordenesCompra.Hijos, option => option.Opcion == "05003001");
    }

    [Theory]
    [InlineData("05003000", false)]
    [InlineData("05003001", false)]
    [InlineData("05003002", false)]
    [InlineData("05004000", false)]
    [InlineData("05004001", false)]
    [InlineData("05004002", false)]
    [InlineData("05003001", true)]
    [InlineData("05004001", true)]
    public void SuperAdminOfficialPermissionsResolveProveeduriaCodes(string permissionCode, bool requireWrite)
    {
        Assert.True(ProveeduriaMenuBuilder.HasOfficialSuperAdminPermission(permissionCode, requireWrite));
    }

    private static Opciones Option(string code, int acceso, params Opciones[] hijos)
    {
        Opciones option = new Opciones
        {
            Opcion = code,
            Permisos = new Permisos { Acceso = acceso, Escritura = acceso }
        };
        option.Hijos.AddRange(hijos);
        return option;
    }
}
