using checklistWs.Services.Tenant;
using Xunit;

namespace checklistWs.Tests.Services.Tenant;

public sealed class OrdenesCompraV5ContractProposalTests
{
    private readonly ProductosServiciosSchemaContractProvider _contracts = new();
    private readonly SchemaManifestProvider _manifests = new();

    [Fact]
    public void Proposal_IsV4ToV5LocalAndExplicitlyNotApprovedForExecution()
    {
        SchemaContractProposal proposal = Proposal();
        SchemaMigrationPackage localPackage = LocalPackage();
        SchemaMigrationDefinition migration = localPackage.Migrations.Single(item => item.MigrationId == OrdenesCompraV5ContractProposal.MigrationId);

        Assert.Equal(4, proposal.SourceContract.ContractVersion);
        Assert.Equal(5, proposal.TargetContract.ContractVersion);
        Assert.Equal(OrdenesCompraV5ContractProposal.MigrationId, proposal.MigrationId);
        Assert.Equal(OrdenesCompraV5ContractProposal.ReviewStatus, proposal.ReviewStatus);
        Assert.False(proposal.ApprovedForExecution);
        Assert.False(migration.AutoApplicable);
        Assert.Contains(localPackage.Migrations, item => item.MigrationId == OrdenesCompraV5ContractProposal.ReconciliationMigrationId && !item.AutoApplicable);
        Assert.Empty(localPackage.Release.ApprovedMigrationIds);
        Assert.Equal("SingleTransaction", migration.TransactionMode);
        Assert.Equal(SchemaMigrationHash.Sha256(migration.UpSql), migration.SqlHash);
    }

    [Fact]
    public void ActiveProviderGateAndPackageExposeV5AndOfficialMigrations()
    {
        SchemaMigrationPackage active = new ProductosServiciosMigrationPackageProvider(_contracts, _manifests)
            .GetPackage(DatabaseScopes.OrdenesCompra);

        Assert.Equal(5, ProductosServiciosSchemaContractProvider.OrdenesCompraLatestVersion);
        Assert.Equal(5, _contracts.GetContract(DatabaseScopes.OrdenesCompra).ContractVersion);
        Assert.Equal(5, _contracts.GetContract(DatabaseScopes.OrdenesCompra, 5).ContractVersion);
        Assert.Equal(5, active.Release.LatestSchemaVersion);
        Assert.Contains(active.Migrations, migration => migration.MigrationId == OrdenesCompraV5ContractProposal.MigrationId && migration.AutoApplicable);
        Assert.Contains(active.Migrations, migration => migration.MigrationId == OrdenesCompraV5ContractProposal.ReconciliationMigrationId && migration.AutoApplicable);
    }

    [Fact]
    public void V5UsesNormalizedDestinationsAndMandatoryDetailBranch()
    {
        SchemaContract target = Proposal().TargetContract;
        SchemaTableContract destinations = Table(target, "OrdenesCompraSucursales");
        SchemaTableContract detail = Table(target, "OrdenesCompraDetalle");

        Assert.Contains(destinations.UniqueConstraints, unique =>
            unique.Name == "UX_OrdenesCompraSucursales_Empresa_Orden_Sucursal" &&
            unique.Columns.SequenceEqual(new[] { "idEmpresa", "idOrdenCompra", "idSucursal" }));
        Assert.Contains(destinations.ForeignKeys, foreignKey =>
            foreignKey.Name == "FK_OrdenesCompraSucursales_Sucursales_EmpresaId" &&
            foreignKey.Columns.SequenceEqual(new[] { "idEmpresa", "idSucursal" }));
        Assert.Contains(detail.Columns, column => column.Name == "idSucursal" && !column.IsNullable);
        Assert.Contains(detail.ForeignKeys, foreignKey =>
            foreignKey.Name == "FK_OrdenesCompraDetalle_OCSucursales_EmpresaOrdenSucursal" &&
            foreignKey.Columns.SequenceEqual(new[] { "idEmpresa", "idOrdenCompra", "idSucursal" }));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public void ContractSupportsOneOrderWithOneOrFourDestinations(int branchCount)
    {
        SchemaTableContract destinations = Table(Proposal().TargetContract, "OrdenesCompraSucursales");
        var order = Guid.NewGuid();
        var company = Guid.NewGuid();
        var rows = Enumerable.Range(0, branchCount)
            .Select(_ => (Company: company, Order: order, Branch: Guid.NewGuid()))
            .ToArray();

        Assert.Equal(branchCount, rows.Distinct().Count());
        Assert.All(rows, row => Assert.Equal((company, order), (row.Company, row.Order)));
        Assert.Contains(destinations.UniqueConstraints, unique =>
            unique.Columns.SequenceEqual(new[] { "idEmpresa", "idOrdenCompra", "idSucursal" }));
    }

    [Fact]
    public void SameAndDistributedDetailsAreRepresentedWithoutChildOrders()
    {
        Guid order = Guid.NewGuid();
        Guid branchA = Guid.NewGuid();
        Guid branchB = Guid.NewGuid();
        var detailAssignments = new[]
        {
            (Order: order, Branch: branchA),
            (Order: order, Branch: branchA),
            (Order: order, Branch: branchB),
        };

        Assert.Single(detailAssignments.Select(row => row.Order).Distinct());
        Assert.Equal(2, detailAssignments.Select(row => row.Branch).Distinct().Count());
        Assert.DoesNotContain(Proposal().TargetContract.Tables, table => table.Name.Contains("OrdenesHijas", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ProductServiceVariantPresentationAndMixedOrderContractsArePreserved()
    {
        SchemaTableContract source = Table(Proposal().SourceContract, "OrdenesCompraDetalle");
        SchemaTableContract target = Table(Proposal().TargetContract, "OrdenesCompraDetalle");

        foreach (string column in new[]
        {
            "TipoProductoServicio", "idProductoServicio", "idVariante", "idPresentacionCompra",
            "VarianteSnapshot", "PresentacionCompraSnapshot", "CantidadBaseOrdenada",
            "CantidadBaseRecibidaAcumulada", "CantidadBasePendiente",
        })
        {
            Assert.Equal(source.Columns.Single(candidate => candidate.Name == column), target.Columns.Single(candidate => candidate.Name == column));
        }

        Assert.Contains(target.CheckConstraints, check =>
            check.Name == "CK_OrdenesCompraDetalle_TipoProductoServicio" &&
            check.Definition.Contains("IN (1, 2)", StringComparison.Ordinal));
    }

    [Fact]
    public void HeaderBranchBecomesNullableCompatibilityDataAndIsNotV5Authority()
    {
        SchemaTableContract sourceHeader = Table(Proposal().SourceContract, "OrdenesCompra");
        SchemaTableContract targetHeader = Table(Proposal().TargetContract, "OrdenesCompra");

        Assert.False(sourceHeader.Columns.Single(column => column.Name == "idSucursal").IsNullable);
        Assert.True(targetHeader.Columns.Single(column => column.Name == "idSucursal").IsNullable);
        Assert.DoesNotContain(Table(Proposal().TargetContract, "OrdenesCompraDetalle").ForeignKeys,
            foreignKey => foreignKey.ReferencedTable == "OrdenesCompra" && foreignKey.Columns.Contains("idSucursal"));
    }

    [Fact]
    public void TenantForeignAndIncompatibleBranchAssignmentsFailClosedByCompositeKeys()
    {
        SchemaContract target = Proposal().TargetContract;
        SchemaTableContract destinations = Table(target, "OrdenesCompraSucursales");
        SchemaTableContract detail = Table(target, "OrdenesCompraDetalle");

        Assert.All(destinations.ForeignKeys, foreignKey => Assert.Equal("idEmpresa", foreignKey.Columns.First()));
        SchemaForeignKeyContract assignment = Assert.Single(detail.ForeignKeys,
            foreignKey => foreignKey.Name == "FK_OrdenesCompraDetalle_OCSucursales_EmpresaOrdenSucursal");
        Assert.Equal(new[] { "idEmpresa", "idOrdenCompra", "idSucursal" }, assignment.Columns);
        Assert.Equal(new[] { "idEmpresa", "idOrdenCompra", "idSucursal" }, assignment.ReferencedColumns);
    }

    [Fact]
    public void HistoricalBackfillCreatesOneDestinationAndCopiesHeaderBranchToEveryDetail()
    {
        string sql = Migration().UpSql;

        Assert.Contains("INSERT INTO dbo.OrdenesCompraSucursales", sql, StringComparison.Ordinal);
        Assert.Contains("SELECT NEWID(),oc.idEmpresa,NEWID(),oc.id,oc.idSucursal", sql, StringComparison.Ordinal);
        Assert.Contains("SET idSucursal=oc.idSucursal", sql, StringComparison.Ordinal);
        Assert.Contains("ALTER TABLE dbo.OrdenesCompraDetalle ALTER COLUMN idSucursal UNIQUEIDENTIFIER NOT NULL", sql, StringComparison.Ordinal);
        Assert.Contains("@HeaderCount", sql, StringComparison.Ordinal);
        Assert.Contains("@DetailCount", sql, StringComparison.Ordinal);
        Assert.Contains("@Ordered", sql, StringComparison.Ordinal);
        Assert.Contains("@Received", sql, StringComparison.Ordinal);
        Assert.Contains("@Pending", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void ReceptionAndInventoryRequirementsUseDetailBranchAndPreserveOperationalRules()
    {
        string requirements = string.Join("\n", OrdenesCompraV5ContractProposal.BuildReceptionV5Requirements());

        Assert.Contains("OrdenesCompraSucursales", requirements, StringComparison.Ordinal);
        Assert.Contains("OrdenesCompraDetalle", requirements, StringComparison.Ordinal);
        Assert.Contains("no-overreceipt", requirements, StringComparison.Ordinal);
        Assert.Contains("OperationKey", requirements, StringComparison.Ordinal);
        Assert.Contains("Servicio", requirements, StringComparison.Ordinal);
        Assert.Contains("idInventarioMovimiento NULL", requirements, StringComparison.Ordinal);
        Assert.Contains("Producto", requirements, StringComparison.Ordinal);
        Assert.Contains("header OrdenesCompra.idSucursal is forbidden", requirements, StringComparison.Ordinal);
        Assert.Contains("serial control", requirements, StringComparison.Ordinal);
    }

    [Fact]
    public void MigrationHasRealPreconditionsRejectsPartialTargetAndSecondSqlRunIsNoOp()
    {
        SchemaContractProposal proposal = Proposal();
        string sql = Migration().UpSql;

        Assert.All(proposal.Preconditions.Concat(proposal.DataPreconditions), condition =>
            Assert.StartsWith("SELECT CASE", condition));
        Assert.Contains("OC_V5_PARTIAL_OR_DRIFTED_TARGET_REJECTED", sql, StringComparison.Ordinal);
        Assert.Contains("IF @TargetComplete = 1", sql, StringComparison.Ordinal);
        Assert.Contains("RETURN;", sql, StringComparison.Ordinal);
        Assert.Contains("SET XACT_ABORT ON", sql, StringComparison.Ordinal);
        Assert.Contains("EXEC sys.sp_executesql", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("\nGO", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LocalPackageIsRejectedByOfficialResolverUntilPoApproval()
    {
        DatabaseIdentity identity = new("SERVER", "SERVER", string.Empty, "CHECKAPP", "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        SchemaMigrationResolution resolution = new SchemaMigrationResolver().GetPendingMigrations(
            identity,
            DatabaseScopes.OrdenesCompra,
            4,
            5,
            LocalPackage(),
            Array.Empty<SchemaControlHistory>(),
            Proposal().SourceManifestHash);

        Assert.Equal(SchemaMigrationResolutionStatus.PackageInvalid, resolution.Status);
        Assert.Equal("MIGRATION_NOT_AUTO_APPLICABLE", resolution.ReasonCode);
    }

    [Fact]
    public void ManifestIsStableAndDifferentFromV4()
    {
        SchemaContractProposal first = Proposal();
        SchemaContractProposal second = Proposal();
        SchemaMigrationDefinition migration = Migration();

        Assert.Equal(first.TargetManifestHash, second.TargetManifestHash);
        Assert.NotEqual(first.SourceManifestHash, first.TargetManifestHash);
        Assert.Equal("843193ced8cccf043953c4b81eec7991a1cce65a666d58321385c637075b9b8d", first.TargetManifestHash);
        Assert.Equal("19fbdeb7de4af7b77b893c36491e455e94c1d446d4d27aba76e0ae844686d71d", migration.SqlHash);

        SchemaTableContract detail = Table(first.TargetContract, "OrdenesCompraDetalle");
        SchemaTableContract destinations = Table(first.TargetContract, "OrdenesCompraSucursales");
        Assert.DoesNotContain(detail.Indexes, index => index.Name == "UX_OrdenesCompraDetalle_Empresa_Id_Orden_Sucursal");
        Assert.DoesNotContain(destinations.Indexes, index => index.Name == "UX_OrdenesCompraSucursales_Empresa_Id");
        Assert.DoesNotContain(destinations.Indexes, index => index.Name == "UX_OrdenesCompraSucursales_Empresa_Orden_Sucursal");
    }

    private SchemaContractProposal Proposal()
        => OrdenesCompraV5ContractProposal.Create(_contracts, _manifests);

    private SchemaMigrationPackage LocalPackage()
        => OrdenesCompraV5ContractProposal.CreateLocalPackage(_contracts, _manifests);

    private SchemaMigrationDefinition Migration()
        => LocalPackage().Migrations.Single(item => item.MigrationId == OrdenesCompraV5ContractProposal.MigrationId);

    private static SchemaTableContract Table(SchemaContract contract, string name)
        => contract.Tables.Single(table => string.Equals(table.Name, name, StringComparison.OrdinalIgnoreCase));
}
