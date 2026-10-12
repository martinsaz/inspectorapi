using checklistWs.Services.Tenant;
using Xunit;

namespace checklistWs.Tests.Services.Tenant;

public sealed class OrdenesCompraV6ContractProposalTests
{
    private readonly ProductosServiciosSchemaContractProvider _contracts = new();
    private readonly SchemaManifestProvider _manifests = new();

    [Fact]
    public void V6AddsOnlyClosureOperationAndReplacementTraceTables()
    {
        SchemaContract source = _contracts.GetContract(DatabaseScopes.OrdenesCompra, 5);
        SchemaContract target = _contracts.GetContract(DatabaseScopes.OrdenesCompra, 6);

        Assert.Equal(source.Tables.Count + 2, target.Tables.Count);
        Assert.Equal(source.Tables.Select(table => table.Name), target.Tables.Take(source.Tables.Count).Select(table => table.Name));
        Assert.Contains(target.Tables, table => table.Name == "OrdenesCompraOperacionesCierre");
        Assert.Contains(target.Tables, table => table.Name == "OrdenesCompraReemplazos");
    }

    [Fact]
    public void ClosureOperationEnforcesTenantTokenIdempotencyAndSingleUseResult()
    {
        SchemaTableContract operations = Table("OrdenesCompraOperacionesCierre");

        Assert.Contains(operations.UniqueConstraints, unique =>
            unique.Name == "UX_OCCierre_Empresa_Token" &&
            unique.Columns.SequenceEqual(new[] { "idEmpresa", "Token" }));
        Assert.Contains(operations.UniqueConstraints, unique =>
            unique.Name == "UX_OCCierre_Empresa_IdempotencyKey" &&
            unique.Columns.SequenceEqual(new[] { "idEmpresa", "IdempotencyKey" }));
        Assert.Contains(operations.Columns, column => column.Name == "PayloadHash" && !column.IsNullable);
        Assert.Contains(operations.Columns, column => column.Name == "FechaExpiracion" && !column.IsNullable);
        Assert.Contains(operations.CheckConstraints, check => check.Name == "CK_OCCierre_Consumo");
        Assert.Contains(operations.ForeignKeys, foreignKey =>
            foreignKey.Name == "FK_OCCierre_Resultado_EmpresaId" &&
            foreignKey.Columns.SequenceEqual(new[] { "idEmpresa", "idOrdenCompraResultado" }));
    }

    [Fact]
    public void ReplacementTracePreservesOldToNewAuditWithoutHardDelete()
    {
        SchemaTableContract replacements = Table("OrdenesCompraReemplazos");
        SchemaMigrationDefinition migration = Migration();

        Assert.Contains(replacements.UniqueConstraints, unique =>
            unique.Name == "UX_OCReemplazos_Empresa_Anterior" &&
            unique.Columns.SequenceEqual(new[] { "idEmpresa", "idOrdenCompraAnterior" }));
        Assert.Contains(replacements.Columns, column => column.Name == "Motivo" && !column.IsNullable);
        Assert.Contains(replacements.Columns, column => column.Name == "idUsuario");
        Assert.Contains(replacements.Columns, column => column.Name == "FechaReemplazo" && !column.IsNullable);
        Assert.Contains("NO_HARD_DELETE", migration.DataPreconditions);
        Assert.DoesNotContain("DELETE FROM", migration.UpSql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MigrationIsAtomicIdempotentAndFailsClosedOnPartialTarget()
    {
        SchemaMigrationDefinition migration = Migration();

        Assert.Equal(5, migration.FromVersion);
        Assert.Equal(6, migration.ToVersion);
        Assert.Equal("SingleTransaction", migration.TransactionMode);
        Assert.True(migration.AutoApplicable);
        Assert.Contains("IF @Complete=1 RETURN", migration.UpSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("OC_V6_PARTIAL_OR_DRIFTED_TARGET_REJECTED", migration.UpSql, StringComparison.Ordinal);
        Assert.Equal(SchemaMigrationHash.Sha256(migration.UpSql), migration.SqlHash);
        Assert.Equal(_manifests.CreateManifest(_contracts.GetContract(DatabaseScopes.OrdenesCompra, 6)).ManifestHash, migration.TargetManifestHash);
    }

    private SchemaTableContract Table(string name) =>
        _contracts.GetContract(DatabaseScopes.OrdenesCompra, 6).Tables.Single(table => table.Name == name);

    private SchemaMigrationDefinition Migration() =>
        new ProductosServiciosMigrationPackageProvider(_contracts, _manifests)
            .GetPackage(DatabaseScopes.OrdenesCompra)
            .Migrations.Single(migration => migration.MigrationId == OrdenesCompraV6ContractProposal.MigrationId);
}
