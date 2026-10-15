using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using checklistWs.Services.Tenant;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace checklistWs.Tests.Services.Tenant;

public sealed class KitsV1SqlIntegrationTests
{
    private const string CheckAppErpVariable = "MOKA_CHECKAPPERP_QA_CONNECTION";
    private const string ExpectedCheckAppErpIdentity = "VPS3348900/CHECKAPPERP/A0E05B05-F509-44D3-8BE4-218F875720CA";
    private const string ExpectedUmbrellaIdentity = "SQL5111/DB_A883C3_CHECKLIST/E398ABAB-6416-4E68-8084-7FF7CB232EF5";
    private static readonly Guid UmbrellaCompany = Guid.Parse("b17aaece-2b78-4e35-b554-9e694eeb15a7");

    [Fact]
    public async Task OfficialEnvironments_CertifyCheckAppErpThenProvisionUmbrella()
    {
        string? checkAppErpConnection = Environment.GetEnvironmentVariable(CheckAppErpVariable);
        if (string.IsNullOrWhiteSpace(checkAppErpConnection)) return;

        string umbrellaConnection = await ReadUmbrellaConnectionAsync();
        TenantDatabaseDescriptor lab = new() { EmpresaKey = "CHECKAPPERP", IdEmpresa = Guid.Empty, ConnectionString = checkAppErpConnection };
        TenantDatabaseDescriptor umbrella = new() { EmpresaKey = "163", IdEmpresa = UmbrellaCompany, ConnectionString = umbrellaConnection };
        checkAppErpConnection = null;
        umbrellaConnection = string.Empty;

        Harness harness = new();
        DatabaseIdentity labIdentity = await harness.IdentityResolver.ResolveAsync(lab);
        Assert.Equal(ExpectedCheckAppErpIdentity, labIdentity.ToSanitizedString());

        await AssertFrozenDependenciesCompatibleAsync(harness, lab);
        Assert.Equal(0, await CountKitsTablesAsync(lab.ConnectionString));
        string labBusinessBefore = await BusinessFingerprintAsync(lab.ConnectionString);

        await AssertPartialRejectedAsync(harness, lab, labIdentity);
        await AssertProvisionRollbackAsync(harness, lab, labIdentity);

        SchemaProvisionResult labProvision = await harness.Bootstrapper.ProvisionScopeAsync(lab, labIdentity, DatabaseScopes.Kits);
        Assert.Equal(SchemaProvisionResultStatus.Provisioned, labProvision.Status);
        Assert.Equal("PROVISIONED", labProvision.ReasonCode);
        Assert.Equal(3, labProvision.TablesCreated);
        await AssertCertifiedAsync(harness, lab, labIdentity);

        Fixture fixture = await SeedFixtureAsync(lab.ConnectionString);
        try
        {
            await AssertPhysicalRulesAsync(lab.ConnectionString, fixture);
            await AssertConcurrentDuplicateRejectedAsync(lab.ConnectionString, fixture);
        }
        finally
        {
            await CleanupFixtureAsync(lab.ConnectionString, fixture);
        }

        Assert.Equal(0L, await CountRowsAsync(lab.ConnectionString, "dbo.Kits"));
        Assert.Equal(0L, await CountRowsAsync(lab.ConnectionString, "dbo.KitsComponentes"));
        Assert.Equal(0L, await CountRowsAsync(lab.ConnectionString, "dbo.KitsHistorial"));
        Assert.Equal(labBusinessBefore, await BusinessFingerprintAsync(lab.ConnectionString));

        await AssertDriftDetectedAndRestoredAsync(harness, lab, labIdentity);
        await AssertCertifiedAsync(harness, lab, labIdentity);
        await AssertNoPendingAsync(harness, lab, labIdentity);

        DatabaseIdentity umbrellaIdentity = await harness.IdentityResolver.ResolveAsync(umbrella);
        Assert.Equal(ExpectedUmbrellaIdentity, umbrellaIdentity.ToSanitizedString());
        await AssertFrozenDependenciesCompatibleAsync(harness, umbrella);
        Assert.Equal(0, await CountKitsTablesAsync(umbrella.ConnectionString));
        string umbrellaBusinessBefore = await BusinessFingerprintAsync(umbrella.ConnectionString);

        SchemaProvisionResult umbrellaProvision = await harness.Bootstrapper.ProvisionScopeAsync(umbrella, umbrellaIdentity, DatabaseScopes.Kits);
        Assert.Equal(SchemaProvisionResultStatus.Provisioned, umbrellaProvision.Status);
        Assert.Equal("PROVISIONED", umbrellaProvision.ReasonCode);
        await AssertCertifiedAsync(harness, umbrella, umbrellaIdentity);
        Assert.Equal(0L, await CountRowsAsync(umbrella.ConnectionString, "dbo.Kits"));
        Assert.Equal(0L, await CountRowsAsync(umbrella.ConnectionString, "dbo.KitsComponentes"));
        Assert.Equal(0L, await CountRowsAsync(umbrella.ConnectionString, "dbo.KitsHistorial"));
        Assert.Equal(umbrellaBusinessBefore, await BusinessFingerprintAsync(umbrella.ConnectionString));
        await AssertNoPendingAsync(harness, umbrella, umbrellaIdentity);
    }

    private static async Task AssertFrozenDependenciesCompatibleAsync(Harness harness, TenantDatabaseDescriptor descriptor)
    {
        CompatibilityDecision products = await harness.Gate.EvaluateAsync(descriptor, DatabaseScopes.ProductosServicios);
        CompatibilityDecision prices = await harness.Gate.EvaluateAsync(descriptor, DatabaseScopes.ListaPrecios);
        Assert.True(products.IsAllowed, $"ProductosServicios: {products.ReasonCode}");
        Assert.Equal(4, products.CurrentVersion);
        Assert.True(prices.IsAllowed, $"ListaPrecios: {prices.ReasonCode}");
        Assert.Equal(2, prices.CurrentVersion);
    }

    private static async Task AssertPartialRejectedAsync(Harness harness, TenantDatabaseDescriptor descriptor, DatabaseIdentity identity)
    {
        await ExecuteAsync(descriptor.ConnectionString, "CREATE TABLE dbo.Kits(id uniqueidentifier NOT NULL CONSTRAINT PK_Kits_QA_PARTIAL PRIMARY KEY);");
        try
        {
            SchemaProvisionResult result = await harness.Bootstrapper.ProvisionScopeAsync(descriptor, identity, DatabaseScopes.Kits);
            Assert.Equal(SchemaProvisionResultStatus.NoProvision, result.Status);
            Assert.Equal("PROVISION_NOT_ALLOWED_PARTIAL", result.ReasonCode);
            Assert.Equal(1, await CountKitsTablesAsync(descriptor.ConnectionString));
        }
        finally
        {
            await ExecuteAsync(descriptor.ConnectionString, "DROP TABLE IF EXISTS dbo.Kits;");
        }
    }

    private static async Task AssertProvisionRollbackAsync(Harness harness, TenantDatabaseDescriptor descriptor, DatabaseIdentity identity)
    {
        SchemaContract valid = harness.Contracts.GetContract(DatabaseScopes.Kits);
        SchemaTableContract components = valid.Tables.Single(table => table.Name == "KitsComponentes");
        SchemaForeignKeyContract invalidForeignKey = new("FK_KitsComponentes_QA_Rollback", new[] { "idEmpresa", "idProductoServicio" }, "dbo", "MOKA_KITS01R_NO_EXISTE", new[] { "idEmpresa", "id" });
        SchemaContract invalid = valid with
        {
            Tables = valid.Tables.Select(table => table.Name == components.Name
                ? table with { ForeignKeys = table.ForeignKeys.Concat(new[] { invalidForeignKey }).ToArray() }
                : table).ToArray()
        };

        await Assert.ThrowsAsync<SqlException>(() => harness.ProvisionExecutor.ProvisionAsync(descriptor, identity, invalid, harness.Manifests.CreateManifest(invalid)));
        Assert.Equal(0, await CountKitsTablesAsync(descriptor.ConnectionString));
    }

    private static async Task AssertCertifiedAsync(Harness harness, TenantDatabaseDescriptor descriptor, DatabaseIdentity identity)
    {
        SchemaContract contract = harness.Contracts.GetContract(DatabaseScopes.Kits);
        string hash = harness.Manifests.CreateManifest(contract).ManifestHash;
        SchemaContractValidationResult validation = await harness.Validator.ValidateAsync(descriptor, contract);
        Assert.True(validation.IsValid, string.Join("; ", validation.Discrepancies));
        SchemaDriftReport drift = await harness.Drift.ValidateAsync(descriptor, identity, DatabaseScopes.Kits, 1, hash);
        Assert.Equal(SchemaValidationGlobalResult.SchemaOk, drift.GlobalResult);
        Assert.Empty(drift.Items);
        CompatibilityDecision gate = await harness.Gate.EvaluateAsync(descriptor, DatabaseScopes.Kits);
        Assert.True(gate.IsAllowed, gate.ReasonCode);
        Assert.Equal("COMPATIBLE", gate.ReasonCode);
        Assert.Equal(3, await CountKitsTablesAsync(descriptor.ConnectionString));
    }

    private static async Task AssertNoPendingAsync(Harness harness, TenantDatabaseDescriptor descriptor, DatabaseIdentity identity)
    {
        SchemaMigrationExecutionResult second = await harness.Runner.MigrateToLatestAsync(descriptor, identity, DatabaseScopes.Kits);
        Assert.Equal(SchemaMigrationExecutionStatus.NoProvision, second.Status);
        Assert.Contains(second.ReasonCode, new[] { "NO_PENDING_MIGRATIONS", "TARGET_ALREADY_REACHED" });
    }

    private static async Task AssertDriftDetectedAndRestoredAsync(Harness harness, TenantDatabaseDescriptor descriptor, DatabaseIdentity identity)
    {
        SchemaContract contract = harness.Contracts.GetContract(DatabaseScopes.Kits);
        string hash = harness.Manifests.CreateManifest(contract).ManifestHash;
        await ExecuteAsync(descriptor.ConnectionString, "DROP INDEX UX_KitsComponentes_Variante_Activo ON dbo.KitsComponentes;");
        try
        {
            SchemaDriftReport drift = await harness.Drift.ValidateAsync(descriptor, identity, DatabaseScopes.Kits, 1, hash);
            Assert.Equal(SchemaValidationGlobalResult.SchemaDrift, drift.GlobalResult);
            CompatibilityDecision gate = await harness.Gate.EvaluateAsync(descriptor, DatabaseScopes.Kits);
            Assert.False(gate.IsAllowed);
            Assert.Equal("SCHEMA_DRIFT", gate.ReasonCode);
        }
        finally
        {
            await ExecuteAsync(descriptor.ConnectionString, "CREATE UNIQUE NONCLUSTERED INDEX UX_KitsComponentes_Variante_Activo ON dbo.KitsComponentes(idEmpresa,idKit,idProductoServicio,idVariante) WHERE Activo = 1 AND FechaArchivado IS NULL AND TipoIdentidad = 3 AND idVariante IS NOT NULL;");
        }
    }

    private static async Task<Fixture> SeedFixtureAsync(string connectionString)
    {
        Fixture f = new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        const string sql = @"
INSERT dbo.ProductosServiciosCategorias(id,idEmpresa,Codigo,Nombre,AplicaA) VALUES (@Category,@Company,N'MOKA-KITS01R',N'MOKA KITS01R',0);
INSERT dbo.ProductosServiciosUnidadesMedida(id,idEmpresa,Codigo,Nombre,Abreviatura,TipoUnidad) VALUES (@Unit,@Company,N'MK01R',N'Pieza MOKA',N'MK',N'ITEM');
INSERT dbo.ProductosServicios(id,idEmpresa,Tipo,Codigo,Nombre,idCategoria,idUnidadMedida,Costo,PrecioPublico,CausaInventario,EsProductoFisico)
VALUES (@ProductA,@Company,1,N'MK-A',N'MOKA Producto A',@Category,@Unit,1,2,1,1),(@ProductB,@Company,1,N'MK-B',N'MOKA Producto B',@Category,@Unit,1,2,1,1);
INSERT dbo.ProductosServiciosVariantes(id,idEmpresa,idProductoServicio,Sku,Nombre,ClaveCombinacion) VALUES(@Variant,@Company,@ProductB,N'MK-B-V',N'MOKA Variante',N'MOKA=1');
INSERT dbo.ProductosServiciosPresentacionesVenta(id,idEmpresa,idProductoServicio,Nombre,EquivalenciaBase,Precio,EsPredeterminada,Orden,Activo,CantidadVenta,idUnidadVenta)
VALUES(@Presentation,@Company,@ProductB,N'MOKA Caja',1,2,1,0,1,1,@Unit);
INSERT dbo.Kits(id,idEmpresa,Nombre,Tipo,PrecioKit) VALUES(@Kit,@Company,N'MOKA-KITS01R-KIT',1,10);";
        await ExecuteAsync(connectionString, sql, command => BindFixture(command, f));
        return f;
    }

    private static async Task AssertPhysicalRulesAsync(string connectionString, Fixture f)
    {
        await AssertRejectedAsync(connectionString, "INSERT dbo.KitsComponentes(id,idEmpresa,idKit,TipoIdentidad,idProductoServicio,Cantidad,PrecioUnitario) VALUES(NEWID(),@Other,@Kit,1,@ProductA,1,1);", f);
        await AssertRejectedAsync(connectionString, "INSERT dbo.KitsComponentes(id,idEmpresa,idKit,TipoIdentidad,idProductoServicio,idVariante,Cantidad,PrecioUnitario) VALUES(NEWID(),@Company,@Kit,1,@ProductA,@Variant,1,1);", f);
        await AssertRejectedAsync(connectionString, "INSERT dbo.KitsComponentes(id,idEmpresa,idKit,TipoIdentidad,idProductoServicio,Cantidad,PrecioUnitario) VALUES(NEWID(),@Company,@Kit,1,@ProductA,0,1);", f);
        await AssertRejectedAsync(connectionString, "INSERT dbo.KitsComponentes(id,idEmpresa,idKit,TipoIdentidad,idProductoServicio,Cantidad,PrecioUnitario) VALUES(NEWID(),@Company,@Kit,1,@ProductA,1,-1);", f);
        await ExecuteAsync(connectionString, "INSERT dbo.KitsComponentes(id,idEmpresa,idKit,TipoIdentidad,idProductoServicio,idVariante,Cantidad,PrecioUnitario) VALUES(NEWID(),@Company,@Kit,3,@ProductA,@Variant,1,1);", command => BindFixture(command, f));
        await AssertRejectedAsync(connectionString, "INSERT dbo.KitsComponentes(id,idEmpresa,idKit,TipoIdentidad,idProductoServicio,idVariante,Cantidad,PrecioUnitario) VALUES(NEWID(),@Company,@Kit,3,@ProductA,@Variant,1,1);", f);

        await ExecuteAsync(connectionString, "UPDATE dbo.Kits SET Activo=0,FechaArchivado=SYSUTCDATETIME() WHERE id=@Kit; INSERT dbo.Kits(id,idEmpresa,Nombre,Tipo,PrecioKit) VALUES(@SecondKit,@Company,N'MOKA-KITS01R-KIT',1,5);", command => BindFixture(command, f));
        await AssertRejectedAsync(connectionString, "UPDATE dbo.Kits SET Activo=1,FechaArchivado=NULL,idUsuarioArchivado=NULL WHERE id=@Kit;", f);
        await ExecuteAsync(connectionString, "DELETE dbo.Kits WHERE id=@SecondKit; UPDATE dbo.Kits SET Activo=1,FechaArchivado=NULL,idUsuarioArchivado=NULL WHERE id=@Kit;", command => BindFixture(command, f));
    }

    private static async Task AssertConcurrentDuplicateRejectedAsync(string connectionString, Fixture f)
    {
        const string sql = "INSERT dbo.KitsComponentes(id,idEmpresa,idKit,TipoIdentidad,idProductoServicio,Cantidad,PrecioUnitario) VALUES(NEWID(),@Company,@Kit,1,@ProductB,1,1);";
        bool[] results = await Task.WhenAll(TryExecuteAsync(connectionString, sql, f), TryExecuteAsync(connectionString, sql, f));
        Assert.Equal(1, results.Count(result => result));
        Assert.Equal(1, results.Count(result => !result));
    }

    private static async Task<bool> TryExecuteAsync(string connectionString, string sql, Fixture f)
    {
        try { await ExecuteAsync(connectionString, sql, command => BindFixture(command, f)); return true; }
        catch (SqlException) { return false; }
    }

    private static Task AssertRejectedAsync(string connectionString, string sql, Fixture f)
        => Assert.ThrowsAsync<SqlException>(() => ExecuteAsync(connectionString, sql, command => BindFixture(command, f)));

    private static async Task CleanupFixtureAsync(string connectionString, Fixture f)
    {
        const string sql = @"
DELETE dbo.KitsHistorial WHERE idEmpresa=@Company;
DELETE dbo.KitsComponentes WHERE idEmpresa=@Company;
DELETE dbo.Kits WHERE idEmpresa=@Company;
DELETE dbo.ProductosServiciosPresentacionesVenta WHERE idEmpresa=@Company;
DELETE dbo.ProductosServiciosVariantes WHERE idEmpresa=@Company;
DELETE dbo.ProductosServicios WHERE idEmpresa=@Company;
DELETE dbo.ProductosServiciosUnidadesMedida WHERE idEmpresa=@Company;
DELETE dbo.ProductosServiciosCategorias WHERE idEmpresa=@Company;";
        await ExecuteAsync(connectionString, sql, command => BindFixture(command, f));
    }

    private static void BindFixture(SqlCommand command, Fixture f)
    {
        command.Parameters.AddWithValue("@Company", f.Company);
        command.Parameters.AddWithValue("@Other", f.OtherCompany);
        command.Parameters.AddWithValue("@Category", f.Category);
        command.Parameters.AddWithValue("@Unit", f.Unit);
        command.Parameters.AddWithValue("@ProductA", f.ProductA);
        command.Parameters.AddWithValue("@ProductB", f.ProductB);
        command.Parameters.AddWithValue("@Variant", f.Variant);
        command.Parameters.AddWithValue("@Presentation", f.Presentation);
        command.Parameters.AddWithValue("@Kit", f.Kit);
        command.Parameters.AddWithValue("@SecondKit", f.SecondKit);
    }

    private static async Task<int> CountKitsTablesAsync(string connectionString)
        => Convert.ToInt32(await ScalarAsync(connectionString, "SELECT COUNT(*) FROM sys.tables WHERE schema_id=SCHEMA_ID(N'dbo') AND name IN (N'Kits',N'KitsComponentes',N'KitsHistorial');"));

    private static async Task<long> CountRowsAsync(string connectionString, string table)
        => Convert.ToInt64(await ScalarAsync(connectionString, $"SELECT COUNT_BIG(*) FROM {table};"));

    private static async Task<string> BusinessFingerprintAsync(string connectionString)
    {
        const string sql = @"
SELECT N'ProductosServicios',COUNT_BIG(*),COALESCE(CHECKSUM_AGG(BINARY_CHECKSUM(id,idEmpresa,Tipo,Codigo,Nombre,Costo,PrecioPublico,Activo)),0) FROM dbo.ProductosServicios
UNION ALL SELECT N'Variantes',COUNT_BIG(*),COALESCE(CHECKSUM_AGG(BINARY_CHECKSUM(id,idEmpresa,idProductoServicio,Sku,Nombre,Activo)),0) FROM dbo.ProductosServiciosVariantes
UNION ALL SELECT N'PresentacionesVenta',COUNT_BIG(*),COALESCE(CHECKSUM_AGG(BINARY_CHECKSUM(id,idEmpresa,idProductoServicio,Nombre,Precio,Activo)),0) FROM dbo.ProductosServiciosPresentacionesVenta
UNION ALL SELECT N'ListaPreciosListas',COUNT_BIG(*),COALESCE(CHECKSUM_AGG(BINARY_CHECKSUM(id,idEmpresa,Nombre,Activo)),0) FROM dbo.ListaPreciosListas
UNION ALL SELECT N'ListaPreciosDetalle',COUNT_BIG(*),COALESCE(CHECKSUM_AGG(BINARY_CHECKSUM(id,idEmpresa,idListaPrecio,idProductoServicio,Precio,Activo)),0) FROM dbo.ListaPreciosDetalle
UNION ALL SELECT N'OrdenesCompra',COUNT_BIG(*),COALESCE(CHECKSUM_AGG(BINARY_CHECKSUM(id,idEmpresa,Folio,Estado,Subtotal,Total,Activo)),0) FROM dbo.OrdenesCompra
UNION ALL SELECT N'OrdenesCompraDetalle',COUNT_BIG(*),COALESCE(CHECKSUM_AGG(BINARY_CHECKSUM(id,idEmpresa,idOrdenCompra,idProductoServicio,Cantidad,CostoUnitario,Subtotal,Total,Activo)),0) FROM dbo.OrdenesCompraDetalle
ORDER BY 1;";
        using SqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        using SqlCommand command = new(sql, connection);
        using SqlDataReader reader = await command.ExecuteReaderAsync();
        StringBuilder canonical = new();
        while (await reader.ReadAsync()) canonical.Append(reader.GetString(0)).Append('|').Append(reader.GetInt64(1)).Append('|').Append(reader.GetInt32(2)).Append(';');
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString()))).ToLowerInvariant();
    }

    private static async Task<object?> ScalarAsync(string connectionString, string sql)
    {
        using SqlConnection connection = new(connectionString); await connection.OpenAsync();
        using SqlCommand command = new(sql, connection); return await command.ExecuteScalarAsync();
    }

    private static async Task ExecuteAsync(string connectionString, string sql, Action<SqlCommand>? bind = null)
    {
        using SqlConnection connection = new(connectionString); await connection.OpenAsync();
        using SqlCommand command = new(sql, connection) { CommandTimeout = 300 }; bind?.Invoke(command); await command.ExecuteNonQueryAsync();
    }

    private static async Task<string> ReadUmbrellaConnectionAsync()
    {
        string path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "checklistWs", "appsettings.json"));
        using JsonDocument configuration = JsonDocument.Parse(await File.ReadAllTextAsync(path));
        return configuration.RootElement.GetProperty("ConnectionStrings").GetProperty("CadenaConexionSQLServer").GetString()
            ?? throw new InvalidOperationException("Missing SQL connection configuration.");
    }

    private sealed class Harness
    {
        public TenantSqlConnectionFactory Connections { get; } = new();
        public ProductosServiciosSchemaContractProvider Contracts { get; } = new();
        public SchemaManifestProvider Manifests { get; } = new();
        public DatabaseIdentityResolver IdentityResolver { get; }
        public SchemaContractPhysicalValidator Validator { get; }
        public SchemaDriftValidator Drift { get; }
        public ProductosServiciosCompatibilityGate Gate { get; }
        public ProductosServiciosSchemaBootstrapper Bootstrapper { get; }
        public SchemaProvisionExecutor ProvisionExecutor { get; }
        public SchemaMigrationRunner Runner { get; }

        public Harness()
        {
            SchemaVersionRepository repository = new(Connections);
            KnownSchemaVersionProvider versions = new();
            IdentityResolver = new DatabaseIdentityResolver(new SqlDatabaseMetadataReader(Connections));
            DatabaseStateClassifier classifier = new(new SqlDatabaseSchemaProbe(Connections, new ProductScopeInventory()), new DatabaseVersionEvidenceReader(repository, versions));
            Validator = new SchemaContractPhysicalValidator(Connections);
            Drift = new SchemaDriftValidator(Connections, Contracts, Manifests, new SqlSchemaPhysicalSnapshotReader());
            Gate = new ProductosServiciosCompatibilityGate(IdentityResolver, classifier, repository, versions, Contracts, Manifests, Drift, NullLogger<ProductosServiciosCompatibilityGate>.Instance);
            ProvisionExecutor = new SchemaProvisionExecutor(Connections, new SchemaProvisionSqlGenerator());
            SqlSchemaOperationLock schemaLock = new(Connections);
            Bootstrapper = new ProductosServiciosSchemaBootstrapper(classifier, repository, Contracts, Manifests, ProvisionExecutor, schemaLock);
            Runner = new SchemaMigrationRunner(classifier, repository, new ProductosServiciosMigrationPackageProvider(Contracts, Manifests), new SchemaMigrationResolver(), new SchemaMigrationSqlExecutor(Connections), schemaLock, Validator);
        }
    }

    private sealed record Fixture(Guid Company, Guid OtherCompany, Guid Category, Guid Unit, Guid ProductA, Guid ProductB, Guid Variant, Guid Presentation, Guid Kit)
    {
        public Guid SecondKit { get; } = Guid.NewGuid();
    }
}
