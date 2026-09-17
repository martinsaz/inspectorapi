using checklist.Clases;
using checklist.Models.Roles;
using Xunit;

namespace checklistWs.Tests.Mvc;

public sealed class SucursalesMenuBuilderTests
{
    [Fact]
    public void BuildShowsEveryExplicitSucursalesChildIndependently()
    {
        string html = AjustesSucursalesMenuBuilder.Build(new[]
        {
            Option("04003000", 1, Option("04003100", 1), Option("04004000", 1), Option("04005000", 1))
        });

        Assert.Contains(@"id=""04003100""", html);
        Assert.Contains(@"id=""04004000""", html);
        Assert.Contains(@"id=""04005000""", html);
        Assert.Contains("ABC Sucursales", html);
        Assert.Contains("Razones Sociales", html);
        Assert.Contains("Regiones", html);
    }

    [Fact]
    public void BuildDoesNotGrantAbcSucursalesFromParentAccessOnly()
    {
        string html = AjustesSucursalesMenuBuilder.Build(new[]
        {
            Option("04003000", 1)
        });

        Assert.DoesNotContain(@"id=""04003100""", html);
        Assert.DoesNotContain(@"id=""04004000""", html);
        Assert.DoesNotContain(@"id=""04005000""", html);
        Assert.Equal(string.Empty, html);
    }

    [Fact]
    public void BuildAcceptsFlatExplicitRazonesAndRegionesWithoutParentFallback()
    {
        string html = AjustesSucursalesMenuBuilder.Build(new[]
        {
            Option("04003000", 1),
            Option("04004000", 1),
            Option("04005000", 1)
        });

        Assert.DoesNotContain(@"id=""04003100""", html);
        Assert.Contains(@"id=""04004000""", html);
        Assert.Contains(@"id=""04005000""", html);
    }

    [Theory]
    [InlineData("04003100", "04003100")]
    [InlineData("04004000", "04004000")]
    [InlineData("04005000", "04005000")]
    public void BuildShowsSingleExplicitChild(string grantedCode, string expectedId)
    {
        string html = AjustesSucursalesMenuBuilder.Build(new[]
        {
            Option("04003000", 1, Option(grantedCode, 1))
        });

        Assert.Contains($@"id=""{expectedId}""", html);
        Assert.Equal(grantedCode == "04003100", html.Contains(@"id=""04003100"""));
        Assert.Equal(grantedCode == "04004000", html.Contains(@"id=""04004000"""));
        Assert.Equal(grantedCode == "04005000", html.Contains(@"id=""04005000"""));
    }

    [Fact]
    public void BuildShowsAbcAndRazonesWhenBothAreGranted()
    {
        string html = AjustesSucursalesMenuBuilder.Build(new[]
        {
            Option("04003000", 1, Option("04003100", 1), Option("04004000", 1))
        });

        Assert.Contains(@"id=""04003100""", html);
        Assert.Contains(@"id=""04004000""", html);
        Assert.DoesNotContain(@"id=""04005000""", html);
    }

    [Fact]
    public void BuildShowsAbcAndRegionesWhenBothAreGranted()
    {
        string html = AjustesSucursalesMenuBuilder.Build(new[]
        {
            Option("04003000", 1, Option("04003100", 1), Option("04005000", 1))
        });

        Assert.Contains(@"id=""04003100""", html);
        Assert.DoesNotContain(@"id=""04004000""", html);
        Assert.Contains(@"id=""04005000""", html);
    }

    [Fact]
    public void BuildShowsRealSuperAdminMixedShapeAfterAbcNormalization()
    {
        string html = AjustesSucursalesMenuBuilder.Build(new[]
        {
            Option("04003000", 1, Option("04003100", 1)),
            Option("04004000", 1),
            Option("04005000", 1)
        });

        Assert.Contains(@"id=""04003100""", html);
        Assert.Contains(@"id=""04004000""", html);
        Assert.Contains(@"id=""04005000""", html);
        Assert.True(html.IndexOf(@"id=""04003100""", StringComparison.Ordinal) < html.IndexOf(@"id=""04004000""", StringComparison.Ordinal));
        Assert.True(html.IndexOf(@"id=""04004000""", StringComparison.Ordinal) < html.IndexOf(@"id=""04005000""", StringComparison.Ordinal));
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
