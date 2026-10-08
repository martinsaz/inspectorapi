using System.Text.Json;
using checklistWs.Models.ProductosServicios;
using checklistWs.Services.Tenant;
using Xunit;

namespace checklistWs.Tests.Services.Tenant;

public sealed class ProveedorPrincipalContractTests
{
    private readonly ProductosServiciosSchemaContractProvider _provider = new();
    private readonly SchemaManifestProvider _manifests = new();

    [Fact]
    public void OrdenCompraSearch_UsesMasterSupplierAndNotPurchaseHistory()
    {
        string source = File.ReadAllText(Path.Combine(
            FindWorkspaceRoot(),
            "inspectorapi", "checklistWs", "Controllers", "OrdenesCompra", "OrdenesCompraController.cs"));

        Assert.Contains("ps.idProveedorPrincipal = @IdProveedor", source, StringComparison.Ordinal);
        Assert.DoesNotContain("EXISTS (SELECT 1 FROM dbo.OrdenesCompraDetalle", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProveedoresV2_AddsTenantCandidateKeyAndApprovedMigration()
    {
        SchemaContract contract = _provider.GetContract(DatabaseScopes.Proveedores, 2);
        SchemaTableContract table = contract.Tables.Single(x => x.Name == "ActivosProveedores");
        Assert.Contains(table.Indexes, x => x.Name == "UX_ActivosProveedores_Empresa_Id" && x.IsUnique);
        SchemaMigrationPackage package = new ProductosServiciosMigrationPackageProvider(_provider, _manifests).GetPackage(DatabaseScopes.Proveedores);
        Assert.Contains(ProveedorPrincipalContractProposal.ProveedoresMigrationId, package.Release.ApprovedMigrationIds);
        Assert.Contains("RETURN;", package.Migrations.Single().UpSql);
    }

    [Fact]
    public void ProductosServiciosV4_AddsNullableTenantSafePrincipalSupplier()
    {
        SchemaContract contract = _provider.GetContract(DatabaseScopes.ProductosServicios, 4);
        SchemaTableContract table = contract.Tables.Single(x => x.Name == "ProductosServicios");
        Assert.Contains(table.Columns, x => x.Name == "idProveedorPrincipal" && x.IsNullable);
        Assert.Contains(table.ForeignKeys, x =>
            x.Name == "FK_ProductosServicios_ProveedorPrincipal_EmpresaId" &&
            x.Columns.SequenceEqual(new[] { "idEmpresa", "idProveedorPrincipal" }) &&
            x.ReferencedColumns.SequenceEqual(new[] { "idEmpresa", "id" }));
        Assert.Contains(table.Indexes, x => x.Name == "IX_ProductosServicios_Empresa_ProveedorPrincipal_Activo_Tipo");
        SchemaMigrationPackage package = new ProductosServiciosMigrationPackageProvider(_provider, _manifests).GetPackage(DatabaseScopes.ProductosServicios);
        Assert.Contains(ProveedorPrincipalContractProposal.ProductosServiciosMigrationId, package.Release.ApprovedMigrationIds);
        Assert.Contains("NO_BACKFILL", package.Migrations.Single(x => x.MigrationId == ProveedorPrincipalContractProposal.ProductosServiciosMigrationId).DataPreconditions);
    }

    [Fact]
    public void SaveRequest_DistinguishesOmittedNullAndGuid()
    {
        ProductoServicioGuardarRequest omitted = JsonSerializer.Deserialize<ProductoServicioGuardarRequest>("{}", new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        ProductoServicioGuardarRequest cleared = JsonSerializer.Deserialize<ProductoServicioGuardarRequest>("{\"idProveedorPrincipal\":null}", new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        Guid id = Guid.NewGuid();
        ProductoServicioGuardarRequest assigned = JsonSerializer.Deserialize<ProductoServicioGuardarRequest>($"{{\"idProveedorPrincipal\":\"{id}\"}}", new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        Assert.False(omitted.ProveedorPrincipalFueEnviado);
        Assert.True(cleared.ProveedorPrincipalFueEnviado);
        Assert.Null(cleared.IdProveedorPrincipal);
        Assert.True(assigned.ProveedorPrincipalFueEnviado);
        Assert.Equal(id, assigned.IdProveedorPrincipal);
    }

    private static string FindWorkspaceRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "inspectorapi")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("WORKSPACE_ROOT_NOT_FOUND");
    }
}
