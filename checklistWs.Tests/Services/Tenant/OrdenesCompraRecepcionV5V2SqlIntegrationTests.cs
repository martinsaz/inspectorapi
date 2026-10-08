using System.Data.SqlClient;
using checklistWs.Services.Tenant;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace checklistWs.Tests.Services.Tenant;

public sealed class OrdenesCompraRecepcionV5V2SqlIntegrationTests
{
    private const string ConnectionVariable = "MOKA_SCHEMA_SQL_INTEGRATION_CONNECTION";

    [Fact]
    public async Task Packages_RunOnIsolatedSqlServer_BackfillValidateAndBecomeNoOp()
    {
        string? raw = Environment.GetEnvironmentVariable(ConnectionVariable);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return;
        }

        string databaseName = $"MOKA_OCQA03S5_{Guid.NewGuid():N}";
        SqlConnectionStringBuilder admin = BuildConnection(raw, "master", "MOKA-OC-QA03S5-ADMIN");
        SqlConnectionStringBuilder isolated = BuildConnection(raw, databaseName, "MOKA-OC-QA03S5-INTEGRATION");
        raw = null;

        await CreateDatabaseAsync(admin.ConnectionString, databaseName);
        try
        {
            await CertifyAsync(isolated.ConnectionString);
        }
        finally
        {
            SqlConnection.ClearAllPools();
            await DropDatabaseAsync(admin.ConnectionString, databaseName);
        }
    }

    private static async Task CertifyAsync(string connectionString)
    {
        var descriptor = new TenantDatabaseDescriptor
        {
            EmpresaKey = "OC-QA03S5-TEMP",
            IdEmpresa = Guid.Empty,
            ConnectionString = connectionString,
        };
        var connectionFactory = new TenantSqlConnectionFactory();
        var contracts = new ProductosServiciosSchemaContractProvider();
        var manifests = new SchemaManifestProvider();
        var identityResolver = new DatabaseIdentityResolver(new SqlDatabaseMetadataReader(connectionFactory));
        DatabaseIdentity identity = await identityResolver.ResolveAsync(descriptor);

        await ProvisionAsync(descriptor, identity, connectionFactory, contracts, manifests, DatabaseScopes.Sucursales, 2);
        await ProvisionAsync(descriptor, identity, connectionFactory, contracts, manifests, DatabaseScopes.Proveedores, 1);
        await ProvisionAsync(descriptor, identity, connectionFactory, contracts, manifests, DatabaseScopes.ProductosServicios, 3);
        await ProvisionAsync(descriptor, identity, connectionFactory, contracts, manifests, DatabaseScopes.OrdenesCompra, 4);
        await ProvisionAsync(descriptor, identity, connectionFactory, contracts, manifests, DatabaseScopes.Inventario, 1);
        await ProvisionAsync(descriptor, identity, connectionFactory, contracts, manifests, DatabaseScopes.Recepcion, 1);

        Fixture fixture = await SeedAsync(descriptor, connectionFactory);
        SchemaContractProposal ocProposal = OrdenesCompraV5ContractProposal.Create(contracts, manifests);
        SchemaContractProposal receptionProposal = RecepcionV2ContractProposal.Create(contracts, manifests);
        SchemaMigrationDefinition ocV5 = OrdenesCompraV5ContractProposal.CreateLocalPackage(contracts, manifests).Migrations.Single(item => item.MigrationId == OrdenesCompraV5ContractProposal.MigrationId);
        SchemaMigrationDefinition receptionV2 = Assert.Single(RecepcionV2ContractProposal.CreateLocalPackage(contracts, manifests, ocProposal.TargetManifestHash).Migrations);

        await AssertPartialTargetRejectedAndRolledBackAsync(descriptor, connectionFactory, ocV5);

        var repository = new SchemaVersionRepository(connectionFactory);
        SchemaControlInfrastructureResult infrastructure = await repository.EnsureSchemaControlInfrastructureAsync(descriptor);
        Assert.Equal(SchemaControlInfrastructureStatus.Ready, infrastructure.Status);
        await SetStateAsync(repository, descriptor, identity, DatabaseScopes.OrdenesCompra, 4, manifests.CreateManifest(contracts.GetContract(DatabaseScopes.OrdenesCompra, 4)).ManifestHash);
        await SetStateAsync(repository, descriptor, identity, DatabaseScopes.Recepcion, 1, manifests.CreateManifest(contracts.GetContract(DatabaseScopes.Recepcion, 1)).ManifestHash);

        var classifier = new DatabaseStateClassifier(
            new SqlDatabaseSchemaProbe(connectionFactory, new ProductScopeInventory()),
            new DatabaseVersionEvidenceReader(repository, new KnownSchemaVersionProvider()));
        var validator = new SchemaContractPhysicalValidator(connectionFactory);
        var resolver = new SchemaMigrationResolver();
        var executor = new SchemaMigrationSqlExecutor(connectionFactory);
        var schemaLock = new SqlSchemaOperationLock(connectionFactory);
        var activePackages = new ProductosServiciosMigrationPackageProvider(contracts, manifests);

        SchemaMigrationPackage activeOc = activePackages.GetPackage(DatabaseScopes.OrdenesCompra);
        SchemaMigrationPackage executableOc = new(
            activeOc.Release with
            {
                LatestSchemaVersion = 5,
                LatestManifestHash = ocProposal.TargetManifestHash,
                ApprovedMigrationIds = activeOc.Release.ApprovedMigrationIds.Concat(new[] { ocV5.MigrationId }).ToArray(),
            },
            activeOc.Migrations.Concat(new[] { ocV5 with { AutoApplicable = true } }).ToArray());
        var ocRunner = new SchemaMigrationRunner(classifier, repository, new FixedPackageProvider(executableOc), resolver, executor, schemaLock, validator);

        SchemaMigrationExecutionResult ocResult = await ocRunner.MigrateToLatestAsync(descriptor, identity, DatabaseScopes.OrdenesCompra);
        Assert.True(ocResult.Status == SchemaMigrationExecutionStatus.Migrated, $"{ocResult.Status}/{ocResult.ReasonCode}: {string.Join("; ", ocResult.Discrepancies)}");
        Assert.Equal(OrdenesCompraV5ContractProposal.MigrationId, ocResult.MigrationId);
        SchemaContractValidationResult ocValidation = await validator.ValidateAsync(descriptor, ocProposal.TargetContract);
        Assert.True(ocValidation.IsValid, string.Join("; ", ocValidation.Discrepancies));
        await AssertOcBackfillAsync(descriptor, connectionFactory, fixture);

        SchemaMigrationPackage executableReception = new(
            new SchemaReleaseManifest(DatabaseScopes.Recepcion, "REC-B20260921", 1, 2, receptionProposal.TargetManifestHash, new[] { receptionV2.MigrationId }),
            new[] { receptionV2 with { AutoApplicable = true } });
        var receptionRunner = new SchemaMigrationRunner(classifier, repository, new FixedPackageProvider(executableReception), resolver, executor, schemaLock, validator);

        SchemaMigrationExecutionResult receptionResult = await receptionRunner.MigrateToLatestAsync(descriptor, identity, DatabaseScopes.Recepcion);
        Assert.True(receptionResult.Status == SchemaMigrationExecutionStatus.Migrated, $"{receptionResult.Status}/{receptionResult.ReasonCode}: {string.Join("; ", receptionResult.Discrepancies)}");
        Assert.Equal(RecepcionV2ContractProposal.MigrationId, receptionResult.MigrationId);
        SchemaContractValidationResult receptionValidation = await validator.ValidateAsync(descriptor, receptionProposal.TargetContract);
        Assert.True(receptionValidation.IsValid, string.Join("; ", receptionValidation.Discrepancies));

        var expandedContracts = new ExpandedContractProvider(contracts, ocProposal.TargetContract, receptionProposal.TargetContract);
        var expandedVersions = new ExpandedVersionProvider();
        var driftValidator = new SchemaDriftValidator(connectionFactory, expandedContracts, manifests, new SqlSchemaPhysicalSnapshotReader());
        SchemaDriftReport ocDrift = await driftValidator.ValidateAsync(descriptor, identity, DatabaseScopes.OrdenesCompra, 5, ocProposal.TargetManifestHash);
        SchemaDriftReport receptionDrift = await driftValidator.ValidateAsync(descriptor, identity, DatabaseScopes.Recepcion, 2, receptionProposal.TargetManifestHash);
        Assert.Equal(SchemaValidationGlobalResult.SchemaOk, ocDrift.GlobalResult);
        Assert.Empty(ocDrift.Items);
        Assert.Equal(SchemaValidationGlobalResult.SchemaOk, receptionDrift.GlobalResult);
        Assert.Empty(receptionDrift.Items);

        var gateClassifier = new DatabaseStateClassifier(
            new SqlDatabaseSchemaProbe(connectionFactory, new ProductScopeInventory()),
            new DatabaseVersionEvidenceReader(repository, expandedVersions));
        var gate = new ProductosServiciosCompatibilityGate(identityResolver, gateClassifier, repository, expandedVersions, expandedContracts, manifests, driftValidator, NullLogger<ProductosServiciosCompatibilityGate>.Instance);
        CompatibilityDecision ocGate = await gate.EvaluateAsync(descriptor, DatabaseScopes.OrdenesCompra);
        CompatibilityDecision receptionGate = await gate.EvaluateAsync(descriptor, DatabaseScopes.Recepcion);
        Assert.True(ocGate.IsAllowed, ocGate.ReasonCode);
        Assert.Equal("COMPATIBLE", ocGate.ReasonCode);
        Assert.True(receptionGate.IsAllowed, receptionGate.ReasonCode);
        Assert.Equal("COMPATIBLE", receptionGate.ReasonCode);

        SchemaMigrationExecutionResult ocSecond = await ocRunner.MigrateToLatestAsync(descriptor, identity, DatabaseScopes.OrdenesCompra);
        SchemaMigrationExecutionResult receptionSecond = await receptionRunner.MigrateToLatestAsync(descriptor, identity, DatabaseScopes.Recepcion);
        Assert.Equal(SchemaMigrationExecutionStatus.NoProvision, ocSecond.Status);
        Assert.Equal(SchemaMigrationExecutionStatus.NoProvision, receptionSecond.Status);
        Assert.Contains(ocSecond.ReasonCode, new[] { "NO_PENDING_MIGRATIONS", "TARGET_ALREADY_REACHED" });
        Assert.Contains(receptionSecond.ReasonCode, new[] { "NO_PENDING_MIGRATIONS", "TARGET_ALREADY_REACHED" });

        await ExecuteAsync(descriptor, connectionFactory, "DROP INDEX IX_OrdenesCompraDetalle_Empresa_Orden_Sucursal ON dbo.OrdenesCompraDetalle;");
        SchemaDriftReport inducedDrift = await driftValidator.ValidateAsync(descriptor, identity, DatabaseScopes.OrdenesCompra, 5, ocProposal.TargetManifestHash);
        Assert.Equal(SchemaValidationGlobalResult.SchemaDrift, inducedDrift.GlobalResult);
        Assert.Contains(inducedDrift.Items, item => item.ObjectName.Contains("IX_OrdenesCompraDetalle_Empresa_Orden_Sucursal", StringComparison.Ordinal));
    }

    private static async Task ProvisionAsync(
        TenantDatabaseDescriptor descriptor,
        DatabaseIdentity identity,
        ITenantSqlConnectionFactory connectionFactory,
        ISchemaContractProvider contracts,
        ISchemaManifestProvider manifests,
        string scope,
        int version)
    {
        SchemaContract contract = contracts.GetContract(scope, version);
        var provisioner = new SchemaProvisionExecutor(connectionFactory, new SchemaProvisionSqlGenerator());
        SchemaProvisionExecutionResult result = await provisioner.ProvisionAsync(descriptor, identity, contract, manifests.CreateManifest(contract));
        Assert.True(result.Succeeded, $"{scope} V{version}: {result.ReasonCode} {string.Join("; ", result.Discrepancies)}");
    }

    private static async Task<Fixture> SeedAsync(TenantDatabaseDescriptor descriptor, ITenantSqlConnectionFactory connectionFactory)
    {
        var fixture = new Fixture(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        const string sql = @"
INSERT dbo.RazonesSociales (Id,IdEmpresa,Nombre) VALUES (@Reason,@Company,N'MOKA QA');
INSERT dbo.SucursalesTipos (Id,IdEmpresa,Nombre) VALUES (@BranchType,@Company,N'TEMPORAL');
INSERT dbo.Sucursales (Id,IdEmpresa,Nombre,IdRazonSocial,IdSucursalTipo) VALUES (@Branch,@Company,N'SUCURSAL QA',@Reason,@BranchType);
INSERT dbo.ProductosServiciosCategorias (id,idEmpresa,Codigo,Nombre,AplicaA) VALUES (@Category,@Company,N'QA',N'QA TEMPORAL',0);
INSERT dbo.ProductosServiciosUnidadesMedida (id,idEmpresa,Codigo,Nombre,Abreviatura,TipoUnidad) VALUES (@Unit,@Company,N'PZA',N'Pieza',N'PZA',N'ITEM');
INSERT dbo.ProductosServicios (id,idEmpresa,Tipo,Codigo,Nombre,idCategoria,idUnidadMedida,Costo,PrecioPublico,CausaInventario,EsProductoFisico)
VALUES (@Product,@Company,1,N'P-001',N'Producto fixture',@Category,@Unit,20,30,1,1),
       (@Service,@Company,2,N'S-001',N'Servicio fixture',@Category,@Unit,20,30,0,0);
INSERT dbo.ProductosServiciosVariantes (id,idEmpresa,idProductoServicio,Sku,Nombre,ClaveCombinacion)
VALUES (@Variant,@Company,@Product,N'P-001-ROJO',N'Rojo',N'COLOR=ROJO');
INSERT dbo.OrdenesCompraPresentacionesCompra
 (id,idEmpresa,idProductoServicio,idVariante,Nombre,idUnidadCompra,UnidadCompra,UnidadCompraAbreviatura,FactorConversionBase,PermiteCantidadBase)
VALUES (@Presentation,@Company,@Product,@Variant,N'Caja QA',@Unit,N'Pieza',N'PZA',1,1);
INSERT dbo.OrdenesCompra (id,idEmpresa,Folio,idRazonSocial,idSucursal,idProveedor,FechaOrden,FechaLlegada,FechaMinima,FechaMaxima,FolioReferencia,Estado,Subtotal,Total,Observaciones)
VALUES (@Order,@Company,N'OC-QA03S5',@Reason,@Branch,@Provider,'2026-10-01','2026-10-07','2026-10-01','2026-10-15',N'REF-HOTFIX',2,60.00,60.00,N'fixture preservado');
INSERT dbo.OrdenesCompraDetalle
 (id,idEmpresa,idOrdenCompra,NumeroPartida,idProductoServicio,TipoProductoServicio,idVariante,idPresentacionCompra,Codigo,Nombre,VarianteSnapshot,PresentacionCompraSnapshot,idUnidadMedida,UnidadMedida,UnidadAbreviatura,UnidadCompraSnapshot,UnidadCompraAbreviaturaSnapshot,Cantidad,CantidadCompra,FactorConversionSnapshot,CantidadBaseOrdenada,CantidadBaseRecibidaAcumulada,CantidadBasePendiente,EstadoPartida,CostoUnitario,Subtotal,Total)
VALUES
 (@DetailA,@Company,@Order,1,@Product,1,@Variant,@Presentation,N'P-001',N'Producto fixture',N'Rojo',N'Caja QA',@Unit,N'Pieza',N'PZA',N'Pieza',N'PZA',2,2,1,2,0,2,1,20,40,40),
 (@DetailB,@Company,@Order,2,@Service,2,NULL,NULL,N'S-001',N'Servicio fixture',NULL,NULL,@Unit,N'Pieza',N'PZA',NULL,NULL,1,1,1,1,0,1,1,20,20,20);";
        await ExecuteAsync(descriptor, connectionFactory, sql, command =>
        {
            command.Parameters.AddWithValue("@Company", fixture.Company);
            command.Parameters.AddWithValue("@Reason", fixture.Reason);
            command.Parameters.AddWithValue("@BranchType", fixture.BranchType);
            command.Parameters.AddWithValue("@Branch", fixture.Branch);
            command.Parameters.AddWithValue("@Provider", fixture.Provider);
            command.Parameters.AddWithValue("@Order", fixture.Order);
            command.Parameters.AddWithValue("@Category", fixture.Category);
            command.Parameters.AddWithValue("@Unit", fixture.Unit);
            command.Parameters.AddWithValue("@Product", fixture.Product);
            command.Parameters.AddWithValue("@Service", fixture.Service);
            command.Parameters.AddWithValue("@Variant", fixture.Variant);
            command.Parameters.AddWithValue("@Presentation", fixture.Presentation);
            command.Parameters.AddWithValue("@DetailA", fixture.DetailA);
            command.Parameters.AddWithValue("@DetailB", fixture.DetailB);
        });
        return fixture;
    }

    private static async Task AssertPartialTargetRejectedAndRolledBackAsync(
        TenantDatabaseDescriptor descriptor,
        ITenantSqlConnectionFactory connectionFactory,
        SchemaMigrationDefinition migration)
    {
        using SqlConnection connection = connectionFactory.CreateConnection(descriptor);
        await connection.OpenAsync();
        using SqlTransaction transaction = connection.BeginTransaction();
        using (var partial = new SqlCommand("ALTER TABLE dbo.OrdenesCompraDetalle ADD idSucursal UNIQUEIDENTIFIER NULL;", connection, transaction))
        {
            await partial.ExecuteNonQueryAsync();
        }

        using var command = new SqlCommand(migration.UpSql, connection, transaction) { CommandTimeout = 300 };
        SqlException error = await Assert.ThrowsAsync<SqlException>(() => command.ExecuteNonQueryAsync());
        Assert.Contains("OC_V5_PARTIAL_OR_DRIFTED_TARGET_REJECTED", error.Message, StringComparison.Ordinal);
        transaction.Rollback();

        object? exists = await ScalarAsync(descriptor, connectionFactory, "SELECT COL_LENGTH(N'dbo.OrdenesCompraDetalle',N'idSucursal');");
        Assert.True(exists is null or DBNull);
    }

    private static async Task AssertOcBackfillAsync(TenantDatabaseDescriptor descriptor, ITenantSqlConnectionFactory connectionFactory, Fixture fixture)
    {
        const string sql = @"
SELECT
 (SELECT COUNT_BIG(*) FROM dbo.OrdenesCompraSucursales WHERE idEmpresa=@Company AND idOrdenCompra=@Order AND idSucursal=@Branch),
 (SELECT COUNT_BIG(*) FROM dbo.OrdenesCompraDetalle WHERE idEmpresa=@Company AND idOrdenCompra=@Order AND idSucursal=@Branch),
 (SELECT COALESCE(SUM(CantidadBaseOrdenada),0) FROM dbo.OrdenesCompraDetalle WHERE idEmpresa=@Company AND idOrdenCompra=@Order),
 (SELECT COALESCE(SUM(CantidadBaseRecibidaAcumulada),0) FROM dbo.OrdenesCompraDetalle WHERE idEmpresa=@Company AND idOrdenCompra=@Order),
 (SELECT COALESCE(SUM(CantidadBasePendiente),0) FROM dbo.OrdenesCompraDetalle WHERE idEmpresa=@Company AND idOrdenCompra=@Order),
 (SELECT COALESCE(SUM(CostoUnitario),0) FROM dbo.OrdenesCompraDetalle WHERE idEmpresa=@Company AND idOrdenCompra=@Order),
 (SELECT COUNT_BIG(*) FROM dbo.OrdenesCompraDetalle WHERE id=@DetailA AND idProductoServicio=@Product AND TipoProductoServicio=1 AND idVariante=@Variant AND idPresentacionCompra=@Presentation AND VarianteSnapshot=N'Rojo' AND PresentacionCompraSnapshot=N'Caja QA' AND EstadoPartida=1),
 (SELECT COUNT_BIG(*) FROM dbo.OrdenesCompraDetalle WHERE id=@DetailB AND idProductoServicio=@Service AND TipoProductoServicio=2 AND idVariante IS NULL AND idPresentacionCompra IS NULL AND EstadoPartida=1),
 (SELECT Subtotal FROM dbo.OrdenesCompra WHERE idEmpresa=@Company AND id=@Order),
 (SELECT Total FROM dbo.OrdenesCompra WHERE idEmpresa=@Company AND id=@Order);";
        using SqlConnection connection = connectionFactory.CreateConnection(descriptor);
        await connection.OpenAsync();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Company", fixture.Company);
        command.Parameters.AddWithValue("@Order", fixture.Order);
        command.Parameters.AddWithValue("@Branch", fixture.Branch);
        command.Parameters.AddWithValue("@DetailA", fixture.DetailA);
        command.Parameters.AddWithValue("@DetailB", fixture.DetailB);
        command.Parameters.AddWithValue("@Product", fixture.Product);
        command.Parameters.AddWithValue("@Service", fixture.Service);
        command.Parameters.AddWithValue("@Variant", fixture.Variant);
        command.Parameters.AddWithValue("@Presentation", fixture.Presentation);
        using SqlDataReader reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal(1L, reader.GetInt64(0));
        Assert.Equal(2L, reader.GetInt64(1));
        Assert.Equal(3m, reader.GetDecimal(2));
        Assert.Equal(0m, reader.GetDecimal(3));
        Assert.Equal(3m, reader.GetDecimal(4));
        Assert.Equal(40m, reader.GetDecimal(5));
        Assert.Equal(1L, reader.GetInt64(6));
        Assert.Equal(1L, reader.GetInt64(7));
        Assert.Equal(60m, reader.GetDecimal(8));
        Assert.Equal(60m, reader.GetDecimal(9));
    }

    private static Task SetStateAsync(SchemaVersionRepository repository, TenantDatabaseDescriptor descriptor, DatabaseIdentity identity, string scope, int version, string hash)
        => repository.SetConfirmedStateAsync(descriptor, new SchemaControlState
        {
            DatabaseIdentityKey = identity.Fingerprint,
            Scope = scope,
            CurrentVersion = version,
            ManifestHash = hash,
            LastValidatedAtUtc = DateTime.UtcNow,
            LastResult = $"TEMP_SOURCE_V{version}_VALIDATED",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        });

    private static async Task CreateDatabaseAsync(string connectionString, string databaseName)
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        using var command = new SqlCommand($"CREATE DATABASE [{databaseName}];", connection) { CommandTimeout = 120 };
        await command.ExecuteNonQueryAsync();
    }

    private static async Task DropDatabaseAsync(string connectionString, string databaseName)
    {
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        using var command = new SqlCommand($"IF DB_ID(N'{databaseName}') IS NOT NULL BEGIN ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{databaseName}]; END", connection) { CommandTimeout = 120 };
        await command.ExecuteNonQueryAsync();
    }

    private static SqlConnectionStringBuilder BuildConnection(string raw, string database, string applicationName)
        => new(raw) { InitialCatalog = database, ApplicationName = applicationName, Pooling = false, ConnectTimeout = 15 };

    private static async Task ExecuteAsync(TenantDatabaseDescriptor descriptor, ITenantSqlConnectionFactory connectionFactory, string sql, Action<SqlCommand>? bind = null)
    {
        using SqlConnection connection = connectionFactory.CreateConnection(descriptor);
        await connection.OpenAsync();
        using var command = new SqlCommand(sql, connection) { CommandTimeout = 300 };
        bind?.Invoke(command);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<object?> ScalarAsync(TenantDatabaseDescriptor descriptor, ITenantSqlConnectionFactory connectionFactory, string sql)
    {
        using SqlConnection connection = connectionFactory.CreateConnection(descriptor);
        await connection.OpenAsync();
        using var command = new SqlCommand(sql, connection);
        return await command.ExecuteScalarAsync();
    }

    private sealed record Fixture(
        Guid Company,
        Guid Reason,
        Guid BranchType,
        Guid Branch,
        Guid Provider,
        Guid Order,
        Guid Category,
        Guid Unit,
        Guid Product,
        Guid Service,
        Guid Variant,
        Guid Presentation)
    {
        public Guid DetailA { get; } = Guid.NewGuid();
        public Guid DetailB { get; } = Guid.NewGuid();
    }

    private sealed class FixedPackageProvider(SchemaMigrationPackage package) : ISchemaMigrationPackageProvider
    {
        public SchemaMigrationPackage GetPackage(string scope)
        {
            Assert.Equal(package.Release.Scope, scope, ignoreCase: true);
            return package;
        }
    }

    private sealed class ExpandedContractProvider(ISchemaContractProvider inner, SchemaContract ocV5, SchemaContract receptionV2) : ISchemaContractProvider
    {
        public SchemaContract GetContract(string scope, int? version = null)
        {
            if (string.Equals(scope, DatabaseScopes.OrdenesCompra, StringComparison.OrdinalIgnoreCase) && version == 5) return ocV5;
            if (string.Equals(scope, DatabaseScopes.Recepcion, StringComparison.OrdinalIgnoreCase) && version == 2) return receptionV2;
            return inner.GetContract(scope, version);
        }
    }

    private sealed class ExpandedVersionProvider : IKnownSchemaVersionProvider
    {
        private readonly IKnownSchemaVersionProvider _inner = new KnownSchemaVersionProvider();
        public int? GetKnownCurrentVersion(string scope)
        {
            if (string.Equals(scope, DatabaseScopes.OrdenesCompra, StringComparison.OrdinalIgnoreCase)) return 5;
            if (string.Equals(scope, DatabaseScopes.Recepcion, StringComparison.OrdinalIgnoreCase)) return 2;
            return _inner.GetKnownCurrentVersion(scope);
        }
    }
}
