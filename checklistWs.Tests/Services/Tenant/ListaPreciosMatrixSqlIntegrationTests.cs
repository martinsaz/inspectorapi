using System.Text.Json;
using checklistWs.Services.Tenant;
using Xunit;

namespace checklistWs.Tests.Services.Tenant;

public sealed class ListaPreciosMatrixSqlIntegrationTests
{
    private static readonly Guid UmbrellaIdEmpresa = Guid.Parse("b17aaece-2b78-4e35-b554-9e694eeb15a7");
    private const byte TipoIdentidadProducto = 1;
    private const byte TipoIdentidadServicio = 2;
    private const byte TipoIdentidadVariante = 3;
    private const byte TipoIdentidadPresentacionVenta = 4;

    [Fact]
    public async Task ConsultaMatricialReal_MaterializaTodasLasIdentidadesYDiezNiveles()
    {
        string configurationPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "checklistWs",
            "appsettings.json"));

        using JsonDocument configuration = JsonDocument.Parse(await File.ReadAllTextAsync(configurationPath));
        string connectionString = configuration.RootElement
            .GetProperty("ConnectionStrings")
            .GetProperty("CadenaConexionSQLServer")
            .GetString() ?? throw new InvalidOperationException("Missing SQL connection configuration.");

        TenantDatabaseDescriptor descriptor = new()
        {
            EmpresaKey = "163",
            IdEmpresa = UmbrellaIdEmpresa,
            ConnectionString = connectionString
        };
        SqlListaPreciosRepository repository = new(new TenantSqlConnectionFactory(), descriptor);

        IReadOnlyList<ListaPreciosConsultaIdentityRecord> identities = await repository.ConsultarIdentidadesAsync(
            UmbrellaIdEmpresa,
            new ListaPreciosConsultaRequest { Nivel = 1, Estatus = "activos" });

        Assert.NotEmpty(identities);
        Assert.Contains(identities, item => item.TipoIdentidad == TipoIdentidadProducto);
        Assert.Contains(identities, item => item.TipoIdentidad == TipoIdentidadServicio);
        Assert.Contains(identities, item => item.TipoIdentidad == TipoIdentidadVariante && item.IdVariante.HasValue);
        Assert.Contains(identities, item => item.TipoIdentidad == TipoIdentidadPresentacionVenta && item.IdPresentacionVenta.HasValue);

        foreach (ListaPreciosConsultaIdentityRecord identity in identities)
        {
            decimal?[] prices =
            [
                identity.P1, identity.P2, identity.P3, identity.P4, identity.P5,
                identity.P6, identity.P7, identity.P8, identity.P9, identity.P10
            ];
            decimal?[] discounts =
            [
                identity.D1, identity.D2, identity.D3, identity.D4, identity.D5,
                identity.D6, identity.D7, identity.D8, identity.D9, identity.D10
            ];

            Assert.Equal(10, prices.Length);
            Assert.Equal(10, discounts.Length);
            Assert.All(prices, Assert.Null);
            Assert.All(discounts, Assert.Null);
        }
    }
}
