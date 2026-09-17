using System.Data.SqlClient;

namespace checklistWs.Services.Tenant
{
    public sealed class SqlDatabaseSchemaProbe : IDatabaseSchemaProbe
    {
        private readonly ITenantSqlConnectionFactory _connectionFactory;
        private readonly IProductScopeInventory _scopeInventory;

        public SqlDatabaseSchemaProbe(
            ITenantSqlConnectionFactory connectionFactory,
            IProductScopeInventory scopeInventory)
        {
            _connectionFactory = connectionFactory;
            _scopeInventory = scopeInventory;
        }

        public async Task<DatabaseSchemaProbeResult> ProbeAsync(
            TenantDatabaseDescriptor descriptor,
            string scope,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<string> expectedTables = _scopeInventory.GetExpectedTables(scope);
            if (expectedTables.Count == 0)
            {
                return new DatabaseSchemaProbeResult
                {
                    Scope = scope,
                    MetadataSufficient = false,
                    Warnings = new[] { $"Scope no reconocido: {scope}" }
                };
            }

            try
            {
                using SqlConnection connection = _connectionFactory.CreateConnection(descriptor);
                await connection.OpenAsync(cancellationToken);

                using SqlCommand command = new SqlCommand(@"
SELECT
    s.name AS SchemaName,
    t.name AS TableName
FROM sys.tables t
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE s.name = @SchemaName
  AND t.name IN (
    @Table01, @Table02, @Table03, @Table04, @Table05,
    @Table06, @Table07, @Table08, @Table09, @Table10,
    @Table11, @Table12, @Table13, @Table14, @Table15,
    @Table16, @Table17, @Table18, @Table19, @Table20
  );", connection);

                command.Parameters.AddWithValue("@SchemaName", "dbo");

                string[] tableNames = expectedTables
                    .Select(GetUnqualifiedTableName)
                    .ToArray();

                for (int i = 0; i < 20; i++)
                {
                    string tableName = i < tableNames.Length
                        ? tableNames[i]
                        : $"__CheckAppScopeUnused{i + 1:00}";
                    command.Parameters.AddWithValue($"@Table{i + 1:00}", tableName);
                }

                List<string> existing = new();
                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    string schemaName = reader.GetString(reader.GetOrdinal("SchemaName")).Trim();
                    string tableName = reader.GetString(reader.GetOrdinal("TableName")).Trim();
                    existing.Add($"{schemaName}.{tableName}");
                }

                return new DatabaseSchemaProbeResult
                {
                    Scope = scope,
                    ExpectedTables = expectedTables.ToArray(),
                    ExistingScopeTables = existing
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(table => table, StringComparer.OrdinalIgnoreCase)
                        .ToArray(),
                    Evidence = new[]
                    {
                        $"Metadata sys.tables/sys.schemas consultada para scope {scope}.",
                        $"Tablas scope encontradas: {existing.Count}/{expectedTables.Count}."
                    }
                };
            }
            catch (SqlException ex)
            {
                throw new DatabaseIdentityResolutionException(DatabaseIdentityResolutionCode.DatabaseIdentityUnavailable, ex);
            }
            catch (InvalidOperationException ex)
            {
                throw new DatabaseIdentityResolutionException(DatabaseIdentityResolutionCode.DatabaseIdentityUnavailable, ex);
            }
        }

        private static string GetUnqualifiedTableName(string fullName)
        {
            int separator = fullName.IndexOf('.', StringComparison.Ordinal);
            return separator >= 0 ? fullName[(separator + 1)..] : fullName;
        }
    }
}
