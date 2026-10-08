using checklistWs.Services.Tenant;
using Xunit;

namespace checklistWs.Tests.Services.Tenant;

public sealed class ListaPreciosLpQa05SContractTests
{
    private readonly ProductosServiciosSchemaContractProvider _provider = new();
    private readonly SchemaManifestProvider _manifestProvider = new();

    [Fact]
    public void Proposal_IsProductosServiciosV2ToV3AndIsApprovedForLpQa05S3Execution()
    {
        SchemaContractProposal proposal = Proposal();

        Assert.Equal(DatabaseScopes.ProductosServicios, proposal.SourceContract.Scope);
        Assert.Equal(2, proposal.SourceContract.ContractVersion);
        Assert.Equal(3, proposal.TargetContract.ContractVersion);
        Assert.Equal("PS-M20261005-V2-V3-IDENTIDAD-COMERCIAL", proposal.MigrationId);
        Assert.Equal("APROBADO_EJECUCION_SQL_LP_QA05S3", proposal.ReviewStatus);
        Assert.True(proposal.ApprovedForExecution);
        Assert.NotNull(proposal.ExecutableSql);
    }

    [Fact]
    public void OfficialProviderAndReleasePackage_PreserveV3InsideActiveV4Chain()
    {
        Assert.Equal(4, ProductosServiciosSchemaContractProvider.LatestVersion);
        Assert.Equal(4, _provider.GetContract(DatabaseScopes.ProductosServicios).ContractVersion);

        SchemaMigrationPackage package = new ProductosServiciosMigrationPackageProvider(_provider, _manifestProvider)
            .GetPackage(DatabaseScopes.ProductosServicios);

        Assert.Equal(4, package.Release.LatestSchemaVersion);
        Assert.Contains(ProductosServiciosComercialContractProposal.MigrationId, package.Release.ApprovedMigrationIds);
        Assert.Contains(package.Migrations, migration => migration.MigrationId == ProductosServiciosComercialContractProposal.MigrationId);
    }

    [Fact]
    public void Target_AddsOnlyCommercialExtensionAndItsHistory()
    {
        SchemaContractProposal proposal = Proposal();
        string[] added = proposal.TargetContract.Tables
            .Select(table => table.FullName)
            .Except(proposal.SourceContract.Tables.Select(table => table.FullName), StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.Equal(new[]
        {
            "dbo.ProductosServiciosIdentidadComercial",
            "dbo.ProductosServiciosIdentidadComercialHistorial"
        }, added);
        Assert.DoesNotContain(proposal.TargetContract.Tables, table => table.Name == "ListaPreciosPromociones");
    }

    [Fact]
    public void Description_ReusesExistingCanonicalColumnWithoutDuplication()
    {
        SchemaContract target = Proposal().TargetContract;
        SchemaTableContract productos = Table(target, "ProductosServicios");
        SchemaTableContract extension = Table(target, "ProductosServiciosIdentidadComercial");

        Assert.Contains(productos.Columns, column => column.Name == "Descripcion" && column.SqlType == "NVARCHAR(MAX)" && column.IsNullable);
        Assert.DoesNotContain(extension.Columns, column => column.Name == "Descripcion");
    }

    [Theory]
    [InlineData("Web", "NVARCHAR(250)", 250)]
    [InlineData("Liverpool", "NVARCHAR(MAX)", -1)]
    [InlineData("MercadoLibre", "NVARCHAR(MAX)", -1)]
    [InlineData("Observaciones", "NVARCHAR(MAX)", -1)]
    public void AdditionalData_AreNullableAndDoNotInventHistoricalBackfill(string name, string sqlType, int maxLength)
    {
        SchemaColumnContract column = Column(Extension(), name);

        Assert.Equal(sqlType, column.SqlType);
        Assert.Equal(maxLength, column.MaxLength);
        Assert.True(column.IsNullable);
        Assert.Null(column.DefaultDefinition);
    }

    [Theory]
    [InlineData("DosPorUno")]
    [InlineData("TresPorDos")]
    [InlineData("DescuentoSegundo")]
    [InlineData("Monedero")]
    public void PromotionFlags_AreIndependentBooleanConfigurationWithSafeNewRowDefault(string name)
    {
        SchemaColumnContract column = Column(Extension(), name);

        Assert.Equal("BIT", column.SqlType);
        Assert.False(column.IsNullable);
        Assert.Equal("((0))", column.DefaultDefinition);
    }

    [Fact]
    public void Extension_HasNoListBranchValidityOrPromotionEngineFields()
    {
        string[] names = Extension().Columns.Select(column => column.Name).ToArray();

        Assert.DoesNotContain(names, name => name.Contains("ListaPrecio", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, name => name.Contains("Sucursal", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, name => name.Contains("Vigencia", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, name => name.Contains("Porcentaje", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, name => name.Contains("Prioridad", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, name => name.Contains("Acumul", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Extension_SupportsExactlyFourTenantSafeSellableIdentities()
    {
        SchemaTableContract table = Extension();
        SchemaCheckContract identity = Assert.Single(table.CheckConstraints, check => check.Name == "CK_PSIdentidadComercial_Identidad");

        foreach (int type in new[] { 1, 2, 3, 4 })
        {
            Assert.Contains($"TipoIdentidad = {type}", identity.Definition, StringComparison.Ordinal);
        }

        Assert.All(table.ForeignKeys, foreignKey => Assert.Equal("idEmpresa", foreignKey.Columns.First()));
        Assert.Contains(table.ForeignKeys, foreignKey => foreignKey.ReferencedTable == "ProductosServicios");
        Assert.Contains(table.ForeignKeys, foreignKey => foreignKey.ReferencedTable == "ProductosServiciosVariantes");
        Assert.Contains(table.ForeignKeys, foreignKey => foreignKey.ReferencedTable == "ProductosServiciosPresentacionesVenta");
    }

    [Fact]
    public void Extension_PreventsDuplicateActiveIdentityAndDefinesLogicalDelete()
    {
        SchemaTableContract table = Extension();

        Assert.Contains(table.UniqueConstraints, unique => unique.Name == "UX_PSIdentidadComercial_Empresa_Producto_Activo" && unique.Columns.SequenceEqual(new[] { "idEmpresa", "idProductoServicio" }));
        Assert.Contains(table.UniqueConstraints, unique => unique.Name == "UX_PSIdentidadComercial_Empresa_Servicio_Activo" && unique.Columns.SequenceEqual(new[] { "idEmpresa", "idProductoServicio" }));
        Assert.Contains(table.UniqueConstraints, unique => unique.Name == "UX_PSIdentidadComercial_Empresa_Variante_Activa" && unique.Columns.SequenceEqual(new[] { "idEmpresa", "idVariante" }));
        Assert.Contains(table.UniqueConstraints, unique => unique.Name == "UX_PSIdentidadComercial_Empresa_Presentacion_Activa" && unique.Columns.SequenceEqual(new[] { "idEmpresa", "idPresentacionVenta" }));
        Assert.Contains(table.CheckConstraints, check => check.Name == "CK_PSIdentidadComercial_Archivado");
        Assert.Contains(table.Columns, column => column.Name == "Activo" && column.DefaultDefinition == "((1))");
        Assert.Contains(table.Columns, column => column.Name == "FechaArchivado" && column.IsNullable);
        Assert.Contains(table.Columns, column => column.Name == "idUsuarioArchivado" && column.IsNullable);
    }

    [Fact]
    public void Extension_CapturesTimestampsAndUserOwnership()
    {
        SchemaTableContract table = Extension();

        foreach (string name in new[] { "FechaCreacion", "FechaActualizacion", "FechaArchivado", "idUsuarioCreacion", "idUsuarioActualizacion", "idUsuarioArchivado" })
        {
            Assert.Contains(table.Columns, column => column.Name == name);
        }
    }

    [Fact]
    public void CommercialHistory_IsSeparateFromPriceHistoryAndAuditsEveryField()
    {
        SchemaTableContract history = Table(Proposal().TargetContract, "ProductosServiciosIdentidadComercialHistorial");
        SchemaCheckContract fields = Assert.Single(history.CheckConstraints, check => check.Name == "CK_PSIdentidadComercialHistorial_Campo");

        foreach (string field in new[] { "Descripcion", "Web", "Liverpool", "MercadoLibre", "Observaciones", "DosPorUno", "TresPorDos", "DescuentoSegundo", "Monedero" })
        {
            Assert.Contains(field, fields.Definition, StringComparison.Ordinal);
        }

        foreach (string column in new[] { "idUsuario", "Usuario", "CorrelationId", "Origen", "FechaUtc", "ValorAnterior", "ValorNuevo" })
        {
            Assert.Contains(history.Columns, candidate => candidate.Name == column);
        }
    }

    [Fact]
    public void Proposal_RequiresNoBackfillAndPreservesCurrentPricePromotionContract()
    {
        SchemaContractProposal proposal = Proposal();

        Assert.Contains("NO_BUSINESS_DATA_BACKFILL", proposal.DataPreconditions);
        Assert.Contains("NO_CHANGE_TO_PRODUCTOSSERVICIOS_DESCRIPCION", proposal.DataPreconditions);
        Assert.Contains("NO_CHANGE_TO_LISTAPRECIOSPROMOCIONES", proposal.DataPreconditions);
        Assert.Contains("HISTORICAL_ABSENCE_MEANS_NO_EXTENSION_ROW", proposal.DataPreconditions);
    }

    [Fact]
    public void ManifestHash_IsStableAndDiffersFromSource()
    {
        SchemaContractProposal first = Proposal();
        SchemaContractProposal second = Proposal();

        Assert.Equal(first.TargetManifestHash, second.TargetManifestHash);
        Assert.NotEqual(first.SourceManifestHash, first.TargetManifestHash);
        Assert.Equal("0ce8a1e391f89577ce20a0e29f5a048b48109a93951346a395b5618073b794e5", first.TargetManifestHash);
    }

    [Fact]
    public void PartialTargetAndSecondRunAreContractuallyBlockedOrNoOp()
    {
        SchemaContractProposal proposal = Proposal();

        Assert.Contains("TARGET_TABLES_BOTH_ABSENT", proposal.Preconditions);
        Assert.Contains("REJECT_PARTIAL_TARGET_OBJECTS", proposal.Preconditions);
        Assert.Contains("NO_ACTIVE_DUPLICATE_SELLABLE_IDENTITY", proposal.Preconditions);
        Assert.True(proposal.ApprovedForExecution);
    }

    [Fact]
    public void Rollback_IsContractualAndCannotDeleteAdoptedBusinessData()
    {
        SchemaContractProposal proposal = Proposal();

        Assert.Contains("only empty", proposal.RollbackPolicy, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("forward corrective migration", proposal.RollbackPolicy, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("SingleTransaction", proposal.TransactionMode);
    }

    private SchemaContractProposal Proposal()
        => ProductosServiciosComercialContractProposal.Create(_provider, _manifestProvider);

    private SchemaTableContract Extension()
        => Table(Proposal().TargetContract, "ProductosServiciosIdentidadComercial");

    private static SchemaTableContract Table(SchemaContract contract, string name)
        => contract.Tables.Single(table => string.Equals(table.Name, name, StringComparison.OrdinalIgnoreCase));

    private static SchemaColumnContract Column(SchemaTableContract table, string name)
        => table.Columns.Single(column => string.Equals(column.Name, name, StringComparison.OrdinalIgnoreCase));
}
