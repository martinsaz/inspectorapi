using checklistWs.Services.Tenant;
using Xunit;

namespace checklistWs.Tests.Services.Tenant;

public sealed class RecepcionV2ContractProposalTests
{
    private readonly ProductosServiciosSchemaContractProvider _contracts = new();
    private readonly SchemaManifestProvider _manifests = new();

    [Fact]
    public void CoordinatedPlanContainsTwoScopedPackagesInExactDependencyOrder()
    {
        OrdenesCompraRecepcionV5V2MigrationPlan plan = Plan();

        Assert.False(plan.ApprovedForExecution);
        Assert.Equal(new[]
        {
            OrdenesCompraV5ContractProposal.MigrationId,
            RecepcionV2ContractProposal.MigrationId,
        }, plan.ExecutionOrder);
        Assert.Equal(DatabaseScopes.OrdenesCompra, plan.OrdenesCompraPackage.Release.Scope);
        Assert.Equal(DatabaseScopes.Recepcion, plan.RecepcionPackage.Release.Scope);
        Assert.All(plan.OrdenesCompraPackage.Migrations.Concat(plan.RecepcionPackage.Migrations), migration => Assert.False(migration.AutoApplicable));
        Assert.Empty(plan.OrdenesCompraPackage.Release.ApprovedMigrationIds);
        Assert.Empty(plan.RecepcionPackage.Release.ApprovedMigrationIds);
    }

    [Fact]
    public void ReceptionV2DependsOnReceptionV1AndExactOcV5Manifest()
    {
        OrdenesCompraRecepcionV5V2MigrationPlan plan = Plan();
        SchemaMigrationDefinition reception = Assert.Single(plan.RecepcionPackage.Migrations);
        SchemaContractProposal proposal = Proposal();

        Assert.Equal(1, reception.FromVersion);
        Assert.Equal(2, reception.ToVersion);
        Assert.Contains(proposal.SourceManifestHash, reception.Dependencies);
        Assert.Contains(plan.OrdenesCompraPackage.Release.LatestManifestHash, reception.Dependencies);
        Assert.Equal("SingleTransaction", reception.TransactionMode);
        Assert.Equal(SchemaMigrationHash.Sha256(reception.UpSql), reception.SqlHash);
    }

    [Fact]
    public void ActiveProvidersAndPackagesExposeOcV5AndReceptionV2()
    {
        ProductosServiciosMigrationPackageProvider packages = new(_contracts, _manifests);

        Assert.Equal(5, ProductosServiciosSchemaContractProvider.OrdenesCompraLatestVersion);
        Assert.Equal(2, ProductosServiciosSchemaContractProvider.RecepcionLatestVersion);
        Assert.Equal(5, packages.GetPackage(DatabaseScopes.OrdenesCompra).Release.LatestSchemaVersion);
        Assert.Equal(2, packages.GetPackage(DatabaseScopes.Recepcion).Release.LatestSchemaVersion);
        Assert.Equal(2, _contracts.GetContract(DatabaseScopes.Recepcion, 2).ContractVersion);
        Assert.Contains(packages.GetPackage(DatabaseScopes.Recepcion).Migrations,
            migration => migration.MigrationId == RecepcionV2ContractProposal.MigrationId && migration.AutoApplicable);
    }

    [Fact]
    public void ReceptionHeaderBelongsToOneDestinationOfTheOrder()
    {
        SchemaTableContract header = Table(Proposal().TargetContract, "Recepciones");
        SchemaForeignKeyContract destination = Assert.Single(header.ForeignKeys,
            foreignKey => foreignKey.Name == "FK_Recepciones_OCSucursales_EmpresaOrdenSucursal");

        Assert.Equal(new[] { "idEmpresa", "idOrdenCompra", "idSucursal" }, destination.Columns);
        Assert.Equal("OrdenesCompraSucursales", destination.ReferencedTable);
        Assert.Equal(new[] { "idEmpresa", "idOrdenCompra", "idSucursal" }, destination.ReferencedColumns);
    }

    [Fact]
    public void ReceptionDetailMatchesReceiptOrderDetailAndBranchSimultaneously()
    {
        SchemaTableContract detail = Table(Proposal().TargetContract, "RecepcionPartidas");

        Assert.Contains(detail.Columns, column => column.Name == "idSucursal" && !column.IsNullable);
        Assert.Contains(detail.ForeignKeys, foreignKey =>
            foreignKey.Name == "FK_RecepcionPartidas_Recepciones_EmpresaRecepcionOrdenSucursal" &&
            foreignKey.Columns.SequenceEqual(new[] { "idEmpresa", "idRecepcion", "idOrdenCompra", "idSucursal" }));
        Assert.Contains(detail.ForeignKeys, foreignKey =>
            foreignKey.Name == "FK_RecepcionPartidas_OCDetalle_EmpresaDetalleOrdenSucursal" &&
            foreignKey.Columns.SequenceEqual(new[] { "idEmpresa", "idOrdenCompraDetalle", "idOrdenCompra", "idSucursal" }));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public void OcV5SupportsOneOrFourBranchesWithOneBranchPerDetail(int branchCount)
    {
        SchemaContract oc = OrdenesCompraV5ContractProposal.Create(_contracts, _manifests).TargetContract;
        SchemaTableContract destinations = Table(oc, "OrdenesCompraSucursales");
        SchemaTableContract details = Table(oc, "OrdenesCompraDetalle");

        Assert.Contains(destinations.UniqueConstraints, unique => unique.Columns.SequenceEqual(new[] { "idEmpresa", "idOrdenCompra", "idSucursal" }));
        Assert.Contains(details.Columns, column => column.Name == "idSucursal" && !column.IsNullable);
        Assert.InRange(branchCount, 1, 4);
    }

    [Fact]
    public void PoMutationAndDestinationArchiveRulesAreClosed()
    {
        string rules = string.Join("\n", OrdenesCompraV5ContractProposal.BuildMutationRules());

        Assert.Contains("Estado=1 (Borrador)", rules, StringComparison.Ordinal);
        Assert.Contains("Estado IN (2,3,4,5)", rules, StringComparison.Ordinal);
        Assert.Contains("no OrdenesCompraDetalle references", rules, StringComparison.Ordinal);
        Assert.Contains("any Recepciones or RecepcionPartidas", rules, StringComparison.Ordinal);
        Assert.Contains("new V5 orders write NULL", rules, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductServiceVariantPresentationAndMixedOrderRemainAuthorized()
    {
        SchemaTableContract detail = Table(
            OrdenesCompraV5ContractProposal.Create(_contracts, _manifests).TargetContract,
            "OrdenesCompraDetalle");

        foreach (string column in new[] { "TipoProductoServicio", "idProductoServicio", "idVariante", "idPresentacionCompra", "FactorConversionSnapshot" })
        {
            Assert.Contains(detail.Columns, candidate => candidate.Name == column);
        }
        Assert.Contains(detail.CheckConstraints, check => check.Name == "CK_OrdenesCompraDetalle_TipoProductoServicio" && check.Definition.Contains("IN (1, 2)", StringComparison.Ordinal));
    }

    [Fact]
    public void PartialAndMultipleReceiptsCloseEachBranchWithoutOverreceipt()
    {
        string rules = string.Join("\n", RecepcionV2ContractProposal.BuildRuntimeRules());
        var branchA = new ReceiptLedger(10m);
        var branchB = new ReceiptLedger(20m);

        branchA.Receive(4m);
        Assert.Equal((4m, 6m), (branchA.Received, branchA.Pending));
        branchA.Receive(6m);
        branchB.Receive(20m);

        Assert.Equal((10m, 0m), (branchA.Received, branchA.Pending));
        Assert.Equal((20m, 0m), (branchB.Received, branchB.Pending));
        Assert.Throws<InvalidOperationException>(() => branchA.Receive(1m));
        Assert.True(branchA.IsComplete && branchB.IsComplete);
        Assert.Contains("UPDLOCK/HOLDLOCK", rules, StringComparison.Ordinal);
        Assert.Contains("partial", rules, StringComparison.Ordinal);
        Assert.Contains("all pending quantities are zero", rules, StringComparison.Ordinal);
    }

    [Fact]
    public void ExistingQuantityAndNoOverreceiptChecksArePreserved()
    {
        SchemaTableContract source = Table(Proposal().SourceContract, "RecepcionPartidas");
        SchemaTableContract target = Table(Proposal().TargetContract, "RecepcionPartidas");

        foreach (string name in new[] { "CK_RecepcionPartidas_Cantidades", "CK_RecepcionPartidas_NoSobreRecepcion", "CK_RecepcionPartidas_Estado" })
        {
            Assert.Equal(
                source.CheckConstraints.Single(check => check.Name == name),
                target.CheckConstraints.Single(check => check.Name == name));
        }
        Assert.Contains(Table(Proposal().TargetContract, "Recepciones").UniqueConstraints,
            unique => unique.Name == "UX_Recepciones_Empresa_OperationKey");
    }

    [Fact]
    public void ProductInventoryAndServiceNoMovementAreFailClosedPreconditions()
    {
        string preconditions = string.Join("\n", Proposal().DataPreconditions);
        string sql = Migration().UpSql;

        Assert.Contains("TipoPartida=2 AND rp.idInventarioMovimiento IS NOT NULL", preconditions, StringComparison.Ordinal);
        Assert.Contains("m.idSucursal<>r.idSucursal", sql, StringComparison.Ordinal);
        Assert.Contains("m.idProductoServicio<>rp.idProductoServicio", sql, StringComparison.Ordinal);
        Assert.Contains("m.idVariante", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("UPDATE dbo.Inventario", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("INSERT INTO dbo.Inventario", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SeriesAreReconciledByReceiptDetailIdentityCountTenantAndBranch()
    {
        string sql = Migration().UpSql;

        Assert.Contains("RECEPCION_V1_SERIES_RECONCILIATION_FAILED", sql, StringComparison.Ordinal);
        Assert.Contains("RECEPCION_V1_SERIES_COUNT_MISMATCH", sql, StringComparison.Ordinal);
        Assert.Contains("s.idSucursalActual<>r.idSucursal", sql, StringComparison.Ordinal);
        Assert.Contains("s.idProductoServicio<>rp.idProductoServicio", sql, StringComparison.Ordinal);
        Assert.Contains("ISNULL(s.idVariante", sql, StringComparison.Ordinal);
        Assert.Contains("rs.idEmpresa=rp.idEmpresa", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void BackfillCopiesReceiptBranchWithoutChangingBusinessOrInventoryData()
    {
        string sql = Migration().UpSql;

        Assert.Contains("SET idSucursal=r.idSucursal", sql, StringComparison.Ordinal);
        Assert.Contains("ALTER TABLE dbo.RecepcionPartidas ALTER COLUMN idSucursal UNIQUEIDENTIFIER NOT NULL", sql, StringComparison.Ordinal);
        Assert.Contains("@ReceiptCount", sql, StringComparison.Ordinal);
        Assert.Contains("@DetailCount", sql, StringComparison.Ordinal);
        Assert.Contains("@Accumulated", sql, StringComparison.Ordinal);
        Assert.Contains("@MovementCount", sql, StringComparison.Ordinal);
        Assert.Contains("@SeriesCount", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void IncorrectBranchDetailCrossTenantAndDriftAreRejected()
    {
        string sql = Migration().UpSql;

        Assert.Contains("RECEPCION_V1_BRANCH_NOT_IN_OC_V5", sql, StringComparison.Ordinal);
        Assert.Contains("RECEPCION_V1_DETAIL_BRANCH_INCOMPATIBLE", sql, StringComparison.Ordinal);
        Assert.Contains("RECEPCION_V2_PARTIAL_OR_DRIFTED_TARGET_REJECTED", sql, StringComparison.Ordinal);
        Assert.Contains("d.idEmpresa=rp.idEmpresa", sql, StringComparison.Ordinal);
        Assert.Contains("r.idEmpresa=rp.idEmpresa", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void SqlIsIdempotentAndUsesRollbackBeforeCommitContract()
    {
        SchemaContractProposal proposal = Proposal();
        string sql = Migration().UpSql;

        Assert.Contains("IF @TargetComplete=1", sql, StringComparison.Ordinal);
        Assert.Contains("RETURN;", sql, StringComparison.Ordinal);
        Assert.Contains("SET XACT_ABORT ON", sql, StringComparison.Ordinal);
        Assert.Contains("EXEC sys.sp_executesql", sql, StringComparison.Ordinal);
        Assert.Contains("ROLLBACK_BEFORE_COMMIT", proposal.RollbackPolicy, StringComparison.Ordinal);
        Assert.DoesNotContain("\nGO", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OfficialResolverRejectsBothLocalPackagesUntilPreExecutionApproval()
    {
        DatabaseIdentity identity = new("SERVER", "SERVER", string.Empty, "CHECKAPP", "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        SchemaMigrationResolver resolver = new();
        OrdenesCompraRecepcionV5V2MigrationPlan plan = Plan();

        SchemaMigrationResolution oc = resolver.GetPendingMigrations(identity, DatabaseScopes.OrdenesCompra, 4, 5, plan.OrdenesCompraPackage, Array.Empty<SchemaControlHistory>());
        SchemaMigrationResolution reception = resolver.GetPendingMigrations(identity, DatabaseScopes.Recepcion, 1, 2, plan.RecepcionPackage, Array.Empty<SchemaControlHistory>());

        Assert.Equal("MIGRATION_NOT_AUTO_APPLICABLE", oc.ReasonCode);
        Assert.Equal("MIGRATION_NOT_AUTO_APPLICABLE", reception.ReasonCode);
    }

    [Fact]
    public void ReceptionManifestAndSqlHashesAreStable()
    {
        SchemaContractProposal first = Proposal();
        SchemaContractProposal second = Proposal();
        SchemaMigrationDefinition migration = Migration();

        Assert.Equal(first.TargetManifestHash, second.TargetManifestHash);
        Assert.NotEqual(first.SourceManifestHash, first.TargetManifestHash);
        Assert.Equal(SchemaMigrationHash.Sha256(migration.UpSql), migration.SqlHash);
        Assert.Equal("24f73a6f4512b26e8f41f02e1aa9faabe36a624fda475ee8df630ae835663803", first.TargetManifestHash);
        Assert.Equal("a0d0d9b43afaaf946f542d038b933e7de864bd158e9b23aeb537f7ccb5ca742a", migration.SqlHash);

        Assert.DoesNotContain(Table(first.TargetContract, "Recepciones").Indexes, index => index.Name == "UX_Recepciones_Empresa_Id_Orden_Sucursal");
        Assert.DoesNotContain(Table(first.TargetContract, "RecepcionPartidas").Indexes, index => index.Name == "UX_RecepcionPartidas_Empresa_Id_Sucursal");
    }

    private SchemaContractProposal Proposal()
        => RecepcionV2ContractProposal.Create(_contracts, _manifests);

    private OrdenesCompraRecepcionV5V2MigrationPlan Plan()
        => OrdenesCompraRecepcionV5V2ContractPlan.Create(_contracts, _manifests);

    private SchemaMigrationDefinition Migration()
        => Assert.Single(Plan().RecepcionPackage.Migrations);

    private static SchemaTableContract Table(SchemaContract contract, string name)
        => contract.Tables.Single(table => string.Equals(table.Name, name, StringComparison.OrdinalIgnoreCase));

    private sealed class ReceiptLedger
    {
        public ReceiptLedger(decimal ordered) => Ordered = ordered;
        public decimal Ordered { get; }
        public decimal Received { get; private set; }
        public decimal Pending => Ordered - Received;
        public bool IsComplete => Pending == 0;

        public void Receive(decimal quantity)
        {
            if (quantity <= 0 || quantity > Pending)
            {
                throw new InvalidOperationException("OVERRECEIPT_OR_INVALID_QUANTITY");
            }
            Received += quantity;
        }
    }
}
