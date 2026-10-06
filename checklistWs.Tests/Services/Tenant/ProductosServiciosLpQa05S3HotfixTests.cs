using checklistWs.Services.Tenant;
using Xunit;

namespace checklistWs.Tests.Services.Tenant;

public sealed class ProductosServiciosLpQa05S3HotfixTests
{
    private readonly ProductosServiciosSchemaContractProvider _contracts = new();
    private readonly SchemaManifestProvider _manifests = new();

    [Fact]
    public void ProductAndServiceUseSeparateSupportedFilteredUniqueIndexes()
    {
        SchemaTableContract table = V3().Tables.Single(x => x.Name == "ProductosServiciosIdentidadComercial");
        SchemaIndexContract product = table.Indexes.Single(x => x.Name == "UX_PSIdentidadComercial_Empresa_Producto_Activo");
        SchemaIndexContract service = table.Indexes.Single(x => x.Name == "UX_PSIdentidadComercial_Empresa_Servicio_Activo");

        Assert.True(product.IsUnique);
        Assert.True(service.IsUnique);
        Assert.Equal(new[] { "idEmpresa", "idProductoServicio" }, product.KeyColumns.Select(x => x.Name));
        Assert.Equal(new[] { "idEmpresa", "idProductoServicio" }, service.KeyColumns.Select(x => x.Name));
        Assert.Equal("Activo = 1 AND FechaArchivado IS NULL AND TipoIdentidad = 1", product.FilterDefinition);
        Assert.Equal("Activo = 1 AND FechaArchivado IS NULL AND TipoIdentidad = 2", service.FilterDefinition);
    }

    [Fact]
    public void EveryV3FilteredIndexUsesOnlySqlServerSupportedSimplePredicates()
    {
        SchemaIndexContract[] filtered = V3().Tables.SelectMany(x => x.Indexes)
            .Where(x => !string.IsNullOrWhiteSpace(x.FilterDefinition))
            .ToArray();

        Assert.NotEmpty(filtered);
        Assert.All(filtered, index =>
        {
            Assert.DoesNotContain(" IN ", index.FilterDefinition!, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(" OR ", index.FilterDefinition!, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("(", index.FilterDefinition!, StringComparison.Ordinal);
            Assert.DoesNotContain("CONVERT", index.FilterDefinition!, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("CAST", index.FilterDefinition!, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void CorrectedDdlKeepsAllFourActiveIdentityUniquenessRules()
    {
        string sql = Package().Migrations.Single(x => x.MigrationId == ProductosServiciosComercialContractProposal.MigrationId).UpSql;

        Assert.Contains("TipoIdentidad = 1", sql, StringComparison.Ordinal);
        Assert.Contains("TipoIdentidad = 2", sql, StringComparison.Ordinal);
        Assert.Contains("TipoIdentidad = 3", sql, StringComparison.Ordinal);
        Assert.Contains("TipoIdentidad = 4", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("TipoIdentidad IN (1, 2)", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(12, V3().Tables.Where(x => x.Name.StartsWith("ProductosServiciosIdentidadComercial", StringComparison.Ordinal)).Sum(x => x.Indexes.Count));
    }

    [Fact]
    public void CorrectedPackageRetainsMigrationIdentityAndSupersedesOriginalManifest()
    {
        SchemaMigrationPackage package = Package();
        SchemaMigrationDefinition migration = package.Migrations.Single(x => x.MigrationId == ProductosServiciosComercialContractProposal.MigrationId);

        Assert.Equal("PS-M20261005-V2-V3-IDENTIDAD-COMERCIAL", migration.MigrationId);
        Assert.NotEqual("086c8e7fe0aced219dda9e3937ac0ddc2299c27b202ec2827c5eeda3916b732d", migration.TargetManifestHash);
        Assert.Contains("086c8e7fe0aced219dda9e3937ac0ddc2299c27b202ec2827c5eeda3916b732d", migration.SupersededTargetManifestHashes!);
        Assert.Equal(package.Release.LatestManifestHash, migration.TargetManifestHash);
        Assert.Equal(SchemaMigrationHash.Sha256(migration.UpSql), migration.SqlHash);
    }

    [Fact]
    public void RunnerPreconditionsAreExecutableSqlRatherThanReviewLabels()
    {
        SchemaMigrationDefinition migration = Package().Migrations.Single(x => x.MigrationId == ProductosServiciosComercialContractProposal.MigrationId);

        Assert.NotEmpty(migration.Preconditions);
        Assert.All(migration.Preconditions, precondition => Assert.StartsWith("SELECT CASE", precondition.TrimStart(), StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(migration.Preconditions, x => x.Contains("SOURCE_CONTRACT_EXACT:", StringComparison.Ordinal));
        Assert.Contains(migration.Preconditions, x => x.Contains("CheckAppSchemaState", StringComparison.Ordinal));
    }

    private SchemaContract V3() => _contracts.GetContract(DatabaseScopes.ProductosServicios, 3);
    private SchemaMigrationPackage Package() => new ProductosServiciosMigrationPackageProvider(_contracts, _manifests).GetPreparedProductosServiciosV3Package();
}
