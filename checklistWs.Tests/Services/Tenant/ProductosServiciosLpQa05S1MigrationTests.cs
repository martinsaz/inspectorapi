using checklistWs.Services.Tenant;
using Xunit;

namespace checklistWs.Tests.Services.Tenant;

public sealed class ProductosServiciosLpQa05S1MigrationTests
{
    private static readonly DatabaseIdentity Identity = new(
        "SERVER",
        "SERVER",
        string.Empty,
        "CHECKAPP",
        "cccccccc-cccc-cccc-cccc-cccccccccccc");

    private readonly ProductosServiciosSchemaContractProvider _contracts = new();
    private readonly SchemaManifestProvider _manifests = new();

    [Fact]
    public void ActiveRuntimePackageAndGateVersionAreV3()
    {
        ProductosServiciosMigrationPackageProvider provider = Packages();

        Assert.Equal(3, ProductosServiciosSchemaContractProvider.LatestVersion);
        Assert.Equal(3, _contracts.GetContract(DatabaseScopes.ProductosServicios).ContractVersion);
        Assert.Equal(3, provider.GetPackage(DatabaseScopes.ProductosServicios).Release.LatestSchemaVersion);
    }

    [Fact]
    public void PreparedPackage_IsOfficialV2ToV3ChainWithApprovedMigrationId()
    {
        SchemaMigrationPackage package = Prepared();
        SchemaMigrationDefinition migration = V3Migration(package);

        Assert.Equal(3, package.Release.LatestSchemaVersion);
        Assert.Equal(ProductosServiciosComercialContractProposal.MigrationId, migration.MigrationId);
        Assert.Equal(2, migration.FromVersion);
        Assert.Equal(3, migration.ToVersion);
        Assert.Equal("SingleTransaction", migration.TransactionMode);
        Assert.True(migration.AutoApplicable);
        Assert.Contains(migration.MigrationId, package.Release.ApprovedMigrationIds);
        Assert.Equal(package.Release.LatestManifestHash, migration.TargetManifestHash);
        Assert.Equal(SchemaMigrationHash.Sha256(migration.UpSql), migration.SqlHash);
    }

    [Fact]
    public void PreparedPackage_UsesExistingResolverAndResolvesExactV2ToV3()
    {
        SchemaMigrationPackage package = Prepared();
        string v2Hash = _manifests.CreateManifest(_contracts.GetContract(DatabaseScopes.ProductosServicios, 2)).ManifestHash;

        SchemaMigrationResolution result = new SchemaMigrationResolver().GetPendingMigrations(
            Identity,
            DatabaseScopes.ProductosServicios,
            2,
            3,
            package,
            Array.Empty<SchemaControlHistory>(),
            v2Hash);

        Assert.Equal(SchemaMigrationResolutionStatus.Ready, result.Status);
        Assert.Equal(ProductosServiciosComercialContractProposal.MigrationId, Assert.Single(result.PendingMigrations).MigrationId);
    }

    [Fact]
    public void ExactV3_IsNoOpAndFutureVersionRequiresReview()
    {
        SchemaMigrationPackage package = Prepared();
        SchemaMigrationResolver resolver = new();

        SchemaMigrationResolution exact = resolver.GetPendingMigrations(
            Identity, DatabaseScopes.ProductosServicios, 3, 3, package, Array.Empty<SchemaControlHistory>(), package.Release.LatestManifestHash);
        SchemaMigrationResolution future = resolver.GetPendingMigrations(
            Identity, DatabaseScopes.ProductosServicios, 4, null, package, Array.Empty<SchemaControlHistory>());

        Assert.Equal(SchemaMigrationResolutionStatus.NoPendingMigrations, exact.Status);
        Assert.Equal(SchemaMigrationResolutionStatus.RequiresReview, future.Status);
        Assert.Equal("VERSION_FUTURA_REQUIERE_REVISION", future.ReasonCode);
    }

    [Fact]
    public void Ddl_RejectsAnyPreexistingV3ObjectSoPartialAndV2PlusObjectsFailClosed()
    {
        string sql = V3Migration(Prepared()).UpSql;

        Assert.Contains("OBJECT_ID(N'dbo.ProductosServiciosIdentidadComercial', N'U') IS NOT NULL", sql, StringComparison.Ordinal);
        Assert.Contains("OBJECT_ID(N'dbo.ProductosServiciosIdentidadComercialHistorial', N'U') IS NOT NULL", sql, StringComparison.Ordinal);
        Assert.Contains("THROW 51000, 'PS_V3_TARGET_OBJECTS_ALREADY_EXIST_REQUIRES_REVIEW'", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("\nGO", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Ddl_CreatesBothTablesWithoutBackfillOrBusinessDml()
    {
        SchemaMigrationDefinition migration = V3Migration(Prepared());

        Assert.Contains("CREATE TABLE dbo.ProductosServiciosIdentidadComercial", migration.UpSql, StringComparison.Ordinal);
        Assert.Contains("CREATE TABLE dbo.ProductosServiciosIdentidadComercialHistorial", migration.UpSql, StringComparison.Ordinal);
        Assert.DoesNotContain("INSERT INTO", migration.UpSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UPDATE dbo.", migration.UpSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("NO_BUSINESS_DATA_BACKFILL", migration.DataPreconditions);
    }

    [Fact]
    public void ActiveUniqueness_DoesNotDependOnNullableCompositeKeySemantics()
    {
        SchemaTableContract extension = _contracts
            .GetContract(DatabaseScopes.ProductosServicios, 3)
            .Tables.Single(table => table.Name == "ProductosServiciosIdentidadComercial");

        Assert.DoesNotContain(extension.Indexes, index =>
            index.IsUnique &&
            index.KeyColumns.Select(column => column.Name).SequenceEqual(
                new[] { "idEmpresa", "TipoIdentidad", "idProductoServicio", "idVariante", "idPresentacionVenta" }));
        Assert.Contains(extension.Indexes, index => index.Name == "UX_PSIdentidadComercial_Empresa_Producto_Activo");
        Assert.Contains(extension.Indexes, index => index.Name == "UX_PSIdentidadComercial_Empresa_Servicio_Activo");
        Assert.Contains(extension.Indexes, index => index.Name == "UX_PSIdentidadComercial_Empresa_Variante_Activa");
        Assert.Contains(extension.Indexes, index => index.Name == "UX_PSIdentidadComercial_Empresa_Presentacion_Activa");
    }

    [Fact]
    public void Ddl_ContainsTenantSafeForeignKeysChecksAndAuditIndexes()
    {
        string sql = V3Migration(Prepared()).UpSql;

        Assert.Contains("FOREIGN KEY (idEmpresa, idProductoServicio)", sql, StringComparison.Ordinal);
        Assert.Contains("FOREIGN KEY (idEmpresa, idVariante)", sql, StringComparison.Ordinal);
        Assert.Contains("FOREIGN KEY (idEmpresa, idPresentacionVenta)", sql, StringComparison.Ordinal);
        Assert.Contains("CK_PSIdentidadComercial_Identidad", sql, StringComparison.Ordinal);
        Assert.Contains("IX_PSIdentidadComercialHistorial_Empresa_Correlation", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void ContractKeepsDescriptionCanonicalAndCommercialFieldsPerIdentity()
    {
        SchemaContract v3 = _contracts.GetContract(DatabaseScopes.ProductosServicios, 3);
        SchemaTableContract extension = v3.Tables.Single(table => table.Name == "ProductosServiciosIdentidadComercial");

        Assert.DoesNotContain(extension.Columns, column => column.Name == "Descripcion");
        Assert.Contains(v3.Tables.Single(table => table.Name == "ProductosServicios").Columns, column => column.Name == "Descripcion");
        Assert.All(new[] { "Web", "Liverpool", "MercadoLibre", "Observaciones", "DosPorUno", "TresPorDos", "DescuentoSegundo", "Monedero" },
            field => Assert.Contains(extension.Columns, column => column.Name == field));
    }

    private ProductosServiciosMigrationPackageProvider Packages() => new(_contracts, _manifests);

    private SchemaMigrationPackage Prepared() => Packages().GetPreparedProductosServiciosV3Package();

    private static SchemaMigrationDefinition V3Migration(SchemaMigrationPackage package)
        => package.Migrations.Single(migration => migration.MigrationId == ProductosServiciosComercialContractProposal.MigrationId);
}
