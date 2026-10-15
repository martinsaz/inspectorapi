using checklistWs.Services.Tenant;
using Xunit;

namespace checklistWs.Tests.Services.Tenant;

public sealed class KitsSchemaContractTests
{
    private readonly ProductosServiciosSchemaContractProvider _contracts = new();

    [Fact]
    public void V1_IsOfficialDeterministicAndContainsOnlyApprovedTables()
    {
        SchemaContract contract = _contracts.GetContract(DatabaseScopes.Kits);
        SchemaManifestProvider manifests = new();

        Assert.Equal(1, contract.ContractVersion);
        Assert.Equal(1, new KnownSchemaVersionProvider().GetKnownCurrentVersion(DatabaseScopes.Kits));
        Assert.Equal(new[] { "dbo.Kits", "dbo.KitsComponentes", "dbo.KitsHistorial" }, contract.Tables.Select(table => table.FullName));
        Assert.Equal(contract.Tables.Select(table => table.FullName), new ProductScopeInventory().GetExpectedTables(DatabaseScopes.Kits));
        Assert.Equal(manifests.CreateManifest(contract).ManifestHash, manifests.CreateManifest(_contracts.GetContract(DatabaseScopes.Kits, 1)).ManifestHash);
        Assert.Throws<InvalidOperationException>(() => _contracts.GetContract(DatabaseScopes.Kits, 2));

        SchemaMigrationPackage package = new ProductosServiciosMigrationPackageProvider(_contracts, manifests).GetPackage(DatabaseScopes.Kits);
        Assert.Equal(KitsSchemaContractFactory.BaselineId, package.Release.BaselineId);
        Assert.Equal(1, package.Release.LatestSchemaVersion);
        Assert.Equal(manifests.CreateManifest(contract).ManifestHash, package.Release.LatestManifestHash);
        Assert.Empty(package.Migrations);
    }

    [Fact]
    public void V1_UsesExactApprovedColumnsAndExcludesEntregaInicial()
    {
        SchemaContract contract = _contracts.GetContract(DatabaseScopes.Kits);
        AssertColumns(contract, "Kits", "id", "idEmpresa", "identityKey", "Nombre", "Tipo", "PrecioKit", "ImagenUrl", "ImagenNombre", "Activo", "FechaCreacion", "FechaActualizacion", "FechaArchivado", "idUsuarioCreacion", "idUsuarioActualizacion", "idUsuarioArchivado");
        AssertColumns(contract, "KitsComponentes", "id", "idEmpresa", "identityKey", "idKit", "TipoIdentidad", "idProductoServicio", "idVariante", "idPresentacionVenta", "Cantidad", "PrecioUnitario", "Activo", "FechaCreacion", "FechaActualizacion", "FechaArchivado", "idUsuarioCreacion", "idUsuarioActualizacion", "idUsuarioArchivado");
        AssertColumns(contract, "KitsHistorial", "id", "idEmpresa", "idKit", "idKitComponente", "TipoEntidad", "Operacion", "DatosAntes", "DatosDespues", "idUsuario", "CorrelationId", "FechaUtc");
        Assert.DoesNotContain(contract.Tables.SelectMany(table => table.Columns), column => string.Equals(column.Name, "EntregaInicial", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void V1_HasTenantSafeForeignKeysChecksAndFilteredIdentityUniqueness()
    {
        SchemaContract contract = _contracts.GetContract(DatabaseScopes.Kits);
        SchemaTableContract components = Table(contract, "KitsComponentes");
        SchemaTableContract history = Table(contract, "KitsHistorial");

        Assert.All(components.ForeignKeys.Concat(history.ForeignKeys), foreignKey => Assert.Equal("idEmpresa", foreignKey.Columns.First()));
        Assert.Contains(components.CheckConstraints, check => check.Name == "CK_KitsComponentes_Identidad");
        Assert.Contains(components.CheckConstraints, check => check.Name == "CK_KitsComponentes_Cantidad");
        Assert.Contains(components.CheckConstraints, check => check.Name == "CK_KitsComponentes_PrecioUnitario");
        Assert.Equal(3, components.Indexes.Count(index => index.IsUnique && index.FilterDefinition != null && index.Name.Contains("_Activo", StringComparison.Ordinal)));
        Assert.Contains(Table(contract, "Kits").Indexes, index => index.Name == "UX_Kits_Empresa_Nombre_Activo" && index.IsUnique && index.FilterDefinition != null);
        Assert.Contains(history.Indexes, index => index.Name == "IX_KitsHistorial_Empresa_Kit_Fecha" && index.KeyColumns.Last().IsDescending);
    }

    private static SchemaTableContract Table(SchemaContract contract, string name) => Assert.Single(contract.Tables, table => table.Name == name);

    private static void AssertColumns(SchemaContract contract, string table, params string[] expected)
        => Assert.Equal(expected, Table(contract, table).Columns.Select(column => column.Name));
}
