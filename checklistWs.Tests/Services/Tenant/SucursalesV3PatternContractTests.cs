using checklistWs.Services.Security;
using checklistWs.Services.Tenant;
using Xunit;

namespace checklistWs.Tests.Services.Tenant
{
    public sealed class SucursalesV3PatternContractTests
    {
        private readonly ISchemaContractProvider _contractProvider = new ProductosServiciosSchemaContractProvider();
        private readonly ISchemaManifestProvider _manifestProvider = new SchemaManifestProvider();

        [Fact]
        public void SucursalesV2_NotesColumns_AreNvarcharMax()
        {
            SchemaContract contract = _contractProvider.GetContract(DatabaseScopes.Sucursales, ProductosServiciosSchemaContractProvider.SucursalesLatestVersion);

            AssertNotasColumn(contract, "RazonesSociales");
            AssertNotasColumn(contract, "Zonas");
            AssertNotasColumn(contract, "SucursalesTipos");
            AssertNotasColumn(contract, "Sucursales");
        }

        [Fact]
        public void SucursalesV1_NotesColumns_RemainAvailableForMigrationDiff()
        {
            SchemaContract contract = _contractProvider.GetContract(DatabaseScopes.Sucursales, ProductosServiciosSchemaContractProvider.V1);

            Assert.Equal("TEXT", Column(contract, "RazonesSociales", "Notas").SqlType);
            Assert.Equal("VARCHAR(255)", Column(contract, "Zonas", "Notas").SqlType);
            Assert.Equal("VARCHAR(256)", Column(contract, "SucursalesTipos", "Notas").SqlType);
            Assert.Equal("VARCHAR(255)", Column(contract, "Sucursales", "Notas").SqlType);
        }

        [Fact]
        public void SucursalesPackage_UsesVersionedMigrationForNotesHtmlStorage()
        {
            ISchemaMigrationPackageProvider provider = new ProductosServiciosMigrationPackageProvider(_contractProvider, _manifestProvider);

            SchemaMigrationPackage package = provider.GetPackage(DatabaseScopes.Sucursales);
            SchemaMigrationDefinition migration = Assert.Single(package.Migrations);

            Assert.Equal("SUC-M20260917-V1-V2-NOTAS-NVARCHAR-MAX", migration.MigrationId);
            Assert.Equal(1, migration.FromVersion);
            Assert.Equal(2, migration.ToVersion);
            Assert.True(migration.AutoApplicable);
            Assert.Contains("dbo.RazonesSociales.Notas", migration.ObjectsAffected);
            Assert.Contains("dbo.Zonas.Notas", migration.ObjectsAffected);
            Assert.Contains("dbo.SucursalesTipos.Notas", migration.ObjectsAffected);
            Assert.Contains("dbo.Sucursales.Notas", migration.ObjectsAffected);
            Assert.Contains("ALTER TABLE [dbo].[RazonesSociales] ALTER COLUMN [Notas] NVARCHAR(MAX) NULL", migration.UpSql);
            Assert.Contains("ALTER TABLE [dbo].[Zonas] ALTER COLUMN [Notas] NVARCHAR(MAX) NULL", migration.UpSql);
            Assert.Contains("ALTER TABLE [dbo].[SucursalesTipos] ALTER COLUMN [Notas] NVARCHAR(MAX) NULL", migration.UpSql);
            Assert.Contains("ALTER TABLE [dbo].[Sucursales] ALTER COLUMN [Notas] NVARCHAR(MAX) NULL", migration.UpSql);
            Assert.DoesNotContain("\nGO", migration.UpSql, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void SharedRichTextSanitizer_StripsXssAndKeepsApprovedHtml()
        {
            string sanitized = CheckAppRichTextSanitizer.Sanitize(
                "<p onclick=\"alert(1)\">Hola <strong>equipo</strong></p><script>alert(2)</script><a href=\"javascript:alert(3)\">x</a><ul><li>Uno</li></ul>");

            Assert.Contains("<p>", sanitized);
            Assert.Contains("<strong>", sanitized);
            Assert.Contains("<ul>", sanitized);
            Assert.Contains("<li>", sanitized);
            Assert.DoesNotContain("script", sanitized, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("onclick", sanitized, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("javascript:", sanitized, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("href=", sanitized, StringComparison.OrdinalIgnoreCase);
        }

        private static void AssertNotasColumn(SchemaContract contract, string tableName)
        {
            SchemaColumnContract column = Column(contract, tableName, "Notas");

            Assert.Equal("NVARCHAR(MAX)", column.SqlType);
            Assert.Equal(-1, column.MaxLength);
            Assert.True(column.IsNullable);
        }

        private static SchemaColumnContract Column(SchemaContract contract, string tableName, string columnName)
        {
            SchemaTableContract table = Assert.Single(contract.Tables, item => item.Name == tableName);
            return Assert.Single(table.Columns, item => item.Name == columnName);
        }
    }
}
