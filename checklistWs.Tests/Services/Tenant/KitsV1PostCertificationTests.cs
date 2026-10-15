using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using checklistWs.Services.Tenant;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace checklistWs.Tests.Services.Tenant;

public sealed class KitsV1PostCertificationTests
{
    private readonly ITestOutputHelper _output;
    public KitsV1PostCertificationTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public async Task ReadOnlyFinalEvidence_IsExactInBothOfficialEnvironments()
    {
        string? labConnection = Environment.GetEnvironmentVariable("MOKA_CHECKAPPERP_QA_CONNECTION");
        if (string.IsNullOrWhiteSpace(labConnection)) return;

        await VerifyAsync("CHECKAPPERP", labConnection, "VPS3348900/CHECKAPPERP/A0E05B05-F509-44D3-8BE4-218F875720CA");
        labConnection = null;
        await VerifyAsync("UMBRELLA", await ReadUmbrellaConnectionAsync(), "SQL5111/DB_A883C3_CHECKLIST/E398ABAB-6416-4E68-8084-7FF7CB232EF5");
    }

    private async Task VerifyAsync(string label, string connectionString, string expectedIdentity)
    {
        TenantDatabaseDescriptor descriptor = new() { EmpresaKey = label, IdEmpresa = Guid.Empty, ConnectionString = connectionString };
        TenantSqlConnectionFactory connections = new();
        ProductosServiciosSchemaContractProvider contracts = new();
        SchemaManifestProvider manifests = new();
        SchemaVersionRepository repository = new(connections);
        DatabaseIdentityResolver identities = new(new SqlDatabaseMetadataReader(connections));
        DatabaseIdentity identity = await identities.ResolveAsync(descriptor);
        Assert.Equal(expectedIdentity, identity.ToSanitizedString());
        KnownSchemaVersionProvider versions = new();
        DatabaseStateClassifier classifier = new(new SqlDatabaseSchemaProbe(connections, new ProductScopeInventory()), new DatabaseVersionEvidenceReader(repository, versions));
        SchemaDriftValidator driftValidator = new(connections, contracts, manifests, new SqlSchemaPhysicalSnapshotReader());
        ProductosServiciosCompatibilityGate gate = new(identities, classifier, repository, versions, contracts, manifests, driftValidator, NullLogger<ProductosServiciosCompatibilityGate>.Instance);
        SchemaContract contract = contracts.GetContract(DatabaseScopes.Kits);
        string hash = manifests.CreateManifest(contract).ManifestHash;
        SchemaControlState? state = await repository.GetStateAsync(descriptor, identity, DatabaseScopes.Kits);
        Assert.NotNull(state);
        Assert.Equal(1, state!.CurrentVersion);
        Assert.Equal(hash, state.ManifestHash);
        SchemaDriftReport drift = await driftValidator.ValidateAsync(descriptor, identity, DatabaseScopes.Kits, 1, hash);
        Assert.Equal(SchemaValidationGlobalResult.SchemaOk, drift.GlobalResult);
        Assert.Empty(drift.Items);
        CompatibilityDecision decision = await gate.EvaluateAsync(descriptor, DatabaseScopes.Kits);
        Assert.True(decision.IsAllowed, decision.ReasonCode);

        using SqlConnection connection = new(connectionString);
        await connection.OpenAsync();
        const string sql = @"
SELECT
 (SELECT COUNT_BIG(*) FROM sys.tables WHERE schema_id=SCHEMA_ID(N'dbo') AND name IN(N'Kits',N'KitsComponentes',N'KitsHistorial')) TablesCount,
 (SELECT COUNT_BIG(*) FROM sys.foreign_keys WHERE parent_object_id IN(OBJECT_ID(N'dbo.Kits'),OBJECT_ID(N'dbo.KitsComponentes'),OBJECT_ID(N'dbo.KitsHistorial'))) FksCount,
 (SELECT COUNT_BIG(*) FROM sys.check_constraints WHERE parent_object_id IN(OBJECT_ID(N'dbo.Kits'),OBJECT_ID(N'dbo.KitsComponentes'),OBJECT_ID(N'dbo.KitsHistorial'))) ChecksCount,
 (SELECT COUNT_BIG(*) FROM sys.indexes WHERE object_id IN(OBJECT_ID(N'dbo.Kits'),OBJECT_ID(N'dbo.KitsComponentes'),OBJECT_ID(N'dbo.KitsHistorial')) AND index_id>1) IndexesCount,
 (SELECT COUNT_BIG(*) FROM dbo.Kits) KitsRows,
 (SELECT COUNT_BIG(*) FROM dbo.KitsComponentes) ComponentRows,
 (SELECT COUNT_BIG(*) FROM dbo.KitsHistorial) HistoryRows;";
        using SqlCommand command = new(sql, connection);
        using SqlDataReader reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        long tables = reader.GetInt64(0), fks = reader.GetInt64(1), checks = reader.GetInt64(2), indexes = reader.GetInt64(3);
        long kits = reader.GetInt64(4), components = reader.GetInt64(5), history = reader.GetInt64(6);
        Assert.Equal(3L, tables);
        Assert.Equal(6L, fks);
        Assert.Equal(9L, checks);
        Assert.Equal(12L, indexes);
        Assert.Equal(0L, kits);
        Assert.Equal(0L, components);
        Assert.Equal(0L, history);

        IReadOnlyCollection<SchemaControlHistory> controlHistory = await repository.GetHistoryAsync(descriptor, identity, DatabaseScopes.Kits);
        IReadOnlyCollection<SchemaControlAttempt> attempts = await repository.GetAttemptsAsync(descriptor, identity, DatabaseScopes.Kits);
        Assert.Single(controlHistory, item => item.EventType == "PROVISIONED" && item.Result == "PASS");
        Assert.Contains(attempts, item => item.OperationType == "PROVISION" && item.Result == "PASS");
        Assert.DoesNotContain(attempts, item => item.Result == "STARTED" && item.CompletedAtUtc == null);

        string fingerprint = await BusinessFingerprintAsync(connectionString);
        _output.WriteLine($"{label}|Identity={identity.ToSanitizedString()}|Version=1|Hash={hash}|Tables={tables}|FKs={fks}|Checks={checks}|Indexes={indexes}|Rows={kits}/{components}/{history}|Drift={drift.GlobalResult}/{drift.Items.Count}|Gate={decision.ReasonCode}|Fingerprint={fingerprint}|ProvisionPass={controlHistory.Count(item => item.EventType == "PROVISIONED" && item.Result == "PASS")}");
    }

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
        using SqlConnection connection = new(connectionString); await connection.OpenAsync();
        using SqlCommand command = new(sql, connection); using SqlDataReader reader = await command.ExecuteReaderAsync();
        StringBuilder canonical = new();
        while (await reader.ReadAsync()) canonical.Append(reader.GetString(0)).Append('|').Append(reader.GetInt64(1)).Append('|').Append(reader.GetInt32(2)).Append(';');
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString()))).ToLowerInvariant();
    }

    private static async Task<string> ReadUmbrellaConnectionAsync()
    {
        string path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "checklistWs", "appsettings.json"));
        using JsonDocument configuration = JsonDocument.Parse(await File.ReadAllTextAsync(path));
        return configuration.RootElement.GetProperty("ConnectionStrings").GetProperty("CadenaConexionSQLServer").GetString()
            ?? throw new InvalidOperationException("Missing SQL connection configuration.");
    }
}
