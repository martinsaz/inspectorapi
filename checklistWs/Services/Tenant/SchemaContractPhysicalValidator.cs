using System.Data;
using System.Data.SqlClient;

namespace checklistWs.Services.Tenant
{
    public sealed class SchemaContractPhysicalValidator : ISchemaContractPhysicalValidator
    {
        private readonly ITenantSqlConnectionFactory _connectionFactory;

        public SchemaContractPhysicalValidator(ITenantSqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<SchemaContractValidationResult> ValidateAsync(
            TenantDatabaseDescriptor descriptor,
            SchemaContract contract,
            CancellationToken cancellationToken = default)
        {
            using SqlConnection connection = _connectionFactory.CreateConnection(descriptor);
            await connection.OpenAsync(cancellationToken);
            return await ValidateOpenConnectionAsync(connection, contract, cancellationToken);
        }

        internal static async Task<SchemaContractValidationResult> ValidateOpenConnectionAsync(
            SqlConnection connection,
            SchemaContract contract,
            CancellationToken cancellationToken = default,
            SqlTransaction? transaction = null)
        {
            DataTable columns = await QueryAsync(connection, transaction, BuildColumnsSql(contract), contract, cancellationToken);
            DataTable primaryKeys = await QueryAsync(connection, transaction, BuildPrimaryKeysSql(contract), contract, cancellationToken);
            DataTable indexes = await QueryAsync(connection, transaction, BuildIndexesSql(contract), contract, cancellationToken);
            DataTable fks = await QueryAsync(connection, transaction, BuildForeignKeysSql(contract), contract, cancellationToken);
            DataTable checks = await QueryAsync(connection, transaction, BuildChecksSql(contract), contract, cancellationToken);

            List<string> discrepancies = new();
            foreach (SchemaTableContract table in contract.Tables)
            {
                string fullName = table.FullName;
                Dictionary<string, DataRow> dbColumns = columns.AsEnumerable()
                    .Where(row => string.Equals((string)row["SchemaName"], table.Schema, StringComparison.OrdinalIgnoreCase) && string.Equals((string)row["TableName"], table.Name, StringComparison.OrdinalIgnoreCase))
                    .ToDictionary(row => (string)row["ColumnName"], StringComparer.OrdinalIgnoreCase);

                if (dbColumns.Count == 0)
                {
                    discrepancies.Add($"TABLE_MISSING {fullName}");
                    continue;
                }

                foreach (SchemaColumnContract column in table.Columns)
                {
                    if (!dbColumns.TryGetValue(column.Name, out DataRow? row))
                    {
                        discrepancies.Add($"COLUMN_MISSING {fullName}.{column.Name}");
                        continue;
                    }

                    string dbType = ToSqlType(row);
                    if (!string.Equals(dbType, NormalizeType(column.SqlType), StringComparison.OrdinalIgnoreCase))
                    {
                        discrepancies.Add($"COLUMN_TYPE {fullName}.{column.Name} expected={NormalizeType(column.SqlType)} detected={dbType}");
                    }

                    if ((bool)row["IsNullable"] != column.IsNullable)
                    {
                        discrepancies.Add($"COLUMN_NULLABILITY {fullName}.{column.Name}");
                    }

                    if ((bool)row["IsComputed"] != column.IsComputed)
                    {
                        discrepancies.Add($"COLUMN_COMPUTED {fullName}.{column.Name}");
                    }

                    if (!SameDefinition(row["DefaultDefinition"], column.DefaultDefinition))
                    {
                        discrepancies.Add($"COLUMN_DEFAULT {fullName}.{column.Name}");
                    }
                }

                foreach (string extraColumn in dbColumns.Keys.Except(table.Columns.Select(column => column.Name), StringComparer.OrdinalIgnoreCase))
                {
                    discrepancies.Add($"COLUMN_EXTRA {fullName}.{extraColumn}");
                }

                ValidatePrimaryKey(table, primaryKeys, discrepancies);
                ValidateIndexes(table, indexes, discrepancies);
                ValidateForeignKeys(table, fks, discrepancies);
                ValidateChecks(table, checks, discrepancies);
            }

            return new SchemaContractValidationResult
            {
                DetectedTables = columns.AsEnumerable().Select(row => $"{row["SchemaName"]}.{row["TableName"]}").Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                DetectedColumns = columns.Rows.Count,
                DetectedIndexes = indexes.AsEnumerable().Select(row => $"{row["SchemaName"]}.{row["TableName"]}.{row["IndexName"]}").Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                DetectedForeignKeys = fks.AsEnumerable().Select(row => $"{row["SchemaName"]}.{row["TableName"]}.{row["ForeignKeyName"]}").Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                DetectedChecks = checks.Rows.Count,
                Discrepancies = discrepancies.ToArray()
            };
        }


        private static void ValidatePrimaryKey(SchemaTableContract table, DataTable primaryKeys, List<string> discrepancies)
        {
            SchemaPrimaryKeyContract? expected = table.PrimaryKey;
            DataRow? actual = primaryKeys.AsEnumerable()
                .SingleOrDefault(row => string.Equals((string)row["SchemaName"], table.Schema, StringComparison.OrdinalIgnoreCase) && string.Equals((string)row["TableName"], table.Name, StringComparison.OrdinalIgnoreCase));

            if (expected == null)
            {
                if (actual != null)
                {
                    discrepancies.Add($"PK_EXTRA {table.FullName}.{actual["PrimaryKeyName"]}");
                }

                return;
            }

            if (actual == null)
            {
                discrepancies.Add($"PK_MISSING {table.FullName}.{expected.Name}");
                return;
            }

            if (!string.Equals((string)actual["PrimaryKeyName"], expected.Name, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals((string)actual["Columns"], string.Join(',', expected.Columns), StringComparison.OrdinalIgnoreCase) ||
                ((string)actual["TypeDescription"]).StartsWith("CLUSTERED", StringComparison.OrdinalIgnoreCase) != expected.IsClustered)
            {
                discrepancies.Add($"PK_DEFINITION {table.FullName}.{expected.Name}");
            }
        }

        private static void ValidateIndexes(SchemaTableContract table, DataTable indexes, List<string> discrepancies)
        {
            Dictionary<string, DataRow> dbIndexes = indexes.AsEnumerable()
                .Where(row => string.Equals((string)row["SchemaName"], table.Schema, StringComparison.OrdinalIgnoreCase) && string.Equals((string)row["TableName"], table.Name, StringComparison.OrdinalIgnoreCase))
                .GroupBy(row => (string)row["IndexName"], StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

            foreach (SchemaIndexContract index in table.Indexes)
            {
                if (!dbIndexes.TryGetValue(index.Name, out DataRow? row))
                {
                    discrepancies.Add($"INDEX_MISSING {table.FullName}.{index.Name}");
                    continue;
                }

                if ((bool)row["IsUnique"] != index.IsUnique)
                {
                    discrepancies.Add($"INDEX_UNIQUE {table.FullName}.{index.Name}");
                }

                if (!string.Equals((string)row["KeyColumns"], string.Join(',', index.KeyColumns.Select(column => column.Name)), StringComparison.OrdinalIgnoreCase))
                {
                    discrepancies.Add($"INDEX_KEYS {table.FullName}.{index.Name}");
                }

                if (!SameDefinition(row["FilterDefinition"], index.FilterDefinition))
                {
                    discrepancies.Add($"INDEX_FILTER {table.FullName}.{index.Name}");
                }
            }

            foreach (string extra in dbIndexes.Keys.Except(table.Indexes.Select(index => index.Name), StringComparer.OrdinalIgnoreCase))
            {
                discrepancies.Add($"INDEX_EXTRA {table.FullName}.{extra}");
            }
        }

        private static void ValidateForeignKeys(SchemaTableContract table, DataTable fks, List<string> discrepancies)
        {
            Dictionary<string, DataRow> dbFks = fks.AsEnumerable()
                .Where(row => string.Equals((string)row["SchemaName"], table.Schema, StringComparison.OrdinalIgnoreCase) && string.Equals((string)row["TableName"], table.Name, StringComparison.OrdinalIgnoreCase))
                .ToDictionary(row => (string)row["ForeignKeyName"], StringComparer.OrdinalIgnoreCase);

            foreach (SchemaForeignKeyContract fk in table.ForeignKeys)
            {
                if (!dbFks.TryGetValue(fk.Name, out DataRow? row))
                {
                    discrepancies.Add($"FK_MISSING {table.FullName}.{fk.Name}");
                    continue;
                }

                if (!string.Equals((string)row["Columns"], string.Join(',', fk.Columns), StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals((string)row["ReferencedSchema"], fk.ReferencedSchema, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals((string)row["ReferencedTable"], fk.ReferencedTable, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals((string)row["ReferencedColumns"], string.Join(',', fk.ReferencedColumns), StringComparison.OrdinalIgnoreCase))
                {
                    discrepancies.Add($"FK_DEFINITION {table.FullName}.{fk.Name}");
                }
            }

            foreach (string extra in dbFks.Keys.Except(table.ForeignKeys.Select(fk => fk.Name), StringComparer.OrdinalIgnoreCase))
            {
                discrepancies.Add($"FK_EXTRA {table.FullName}.{extra}");
            }
        }

        private static void ValidateChecks(SchemaTableContract table, DataTable checks, List<string> discrepancies)
        {
            HashSet<string> dbChecks = checks.AsEnumerable()
                .Where(row => string.Equals((string)row["SchemaName"], table.Schema, StringComparison.OrdinalIgnoreCase) && string.Equals((string)row["TableName"], table.Name, StringComparison.OrdinalIgnoreCase))
                .Select(row => (string)row["CheckName"])
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (SchemaCheckContract check in table.CheckConstraints)
            {
                if (!dbChecks.Contains(check.Name))
                {
                    discrepancies.Add($"CHECK_MISSING {table.FullName}.{check.Name}");
                }
            }

            foreach (string extra in dbChecks.Except(table.CheckConstraints.Select(check => check.Name), StringComparer.OrdinalIgnoreCase))
            {
                discrepancies.Add($"CHECK_EXTRA {table.FullName}.{extra}");
            }
        }

        private static async Task<DataTable> QueryAsync(SqlConnection connection, SqlTransaction? transaction, string sql, SchemaContract contract, CancellationToken cancellationToken)
        {
            using SqlCommand command = new SqlCommand(sql, connection, transaction);
            AddTableParameters(command, contract);
            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            DataTable table = new();
            table.Load(reader);
            return table;
        }

        private static void AddTableParameters(SqlCommand command, SchemaContract contract)
        {
            string[] names = contract.Tables.Select(table => table.Name).OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();
            for (int i = 0; i < names.Length; i++)
            {
                command.Parameters.AddWithValue($"@Table{i:00}", names[i]);
            }
        }

        private static string InClause(SchemaContract contract)
        {
            return string.Join(", ", contract.Tables.Select((_, index) => $"@Table{index:00}"));
        }

        private static string BuildColumnsSql(SchemaContract contract) => $@"
SELECT s.name AS SchemaName, t.name AS TableName, c.name AS ColumnName, ty.name AS TypeName,
       c.max_length AS MaxLength, c.precision AS Precision, c.scale AS Scale, c.is_nullable AS IsNullable,
       dc.definition AS DefaultDefinition, c.is_identity AS IsIdentity, c.is_computed AS IsComputed, cc.definition AS ComputedDefinition
FROM sys.tables t
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
INNER JOIN sys.columns c ON c.object_id = t.object_id
INNER JOIN sys.types ty ON ty.user_type_id = c.user_type_id
LEFT JOIN sys.default_constraints dc ON dc.object_id = c.default_object_id
LEFT JOIN sys.computed_columns cc ON cc.object_id = c.object_id AND cc.column_id = c.column_id
WHERE s.name = N'dbo' AND t.name IN ({InClause(contract)});";


        private static string BuildPrimaryKeysSql(SchemaContract contract) => $@"
SELECT s.name AS SchemaName, t.name AS TableName, kc.name AS PrimaryKeyName, i.type_desc AS TypeDescription,
       STUFF((SELECT ',' + c2.name FROM sys.index_columns ic2 INNER JOIN sys.columns c2 ON c2.object_id = ic2.object_id AND c2.column_id = ic2.column_id WHERE ic2.object_id = i.object_id AND ic2.index_id = i.index_id AND ic2.is_included_column = 0 ORDER BY ic2.key_ordinal FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, '') AS Columns
FROM sys.key_constraints kc
INNER JOIN sys.tables t ON t.object_id = kc.parent_object_id
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
INNER JOIN sys.indexes i ON i.object_id = kc.parent_object_id AND i.index_id = kc.unique_index_id
WHERE kc.type = 'PK' AND s.name = N'dbo' AND t.name IN ({InClause(contract)});";

        private static string BuildIndexesSql(SchemaContract contract) => $@"
SELECT s.name AS SchemaName, t.name AS TableName, i.name AS IndexName, i.is_unique AS IsUnique, i.type_desc AS TypeDescription, i.filter_definition AS FilterDefinition,
       STUFF((SELECT ',' + c2.name FROM sys.index_columns ic2 INNER JOIN sys.columns c2 ON c2.object_id = ic2.object_id AND c2.column_id = ic2.column_id WHERE ic2.object_id = i.object_id AND ic2.index_id = i.index_id AND ic2.is_included_column = 0 ORDER BY ic2.key_ordinal FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, '') AS KeyColumns
FROM sys.indexes i
INNER JOIN sys.tables t ON t.object_id = i.object_id
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE s.name = N'dbo' AND t.name IN ({InClause(contract)}) AND i.name IS NOT NULL AND i.is_primary_key = 0;";

        private static string BuildForeignKeysSql(SchemaContract contract) => $@"
SELECT s.name AS SchemaName, t.name AS TableName, fk.name AS ForeignKeyName, rs.name AS ReferencedSchema, rt.name AS ReferencedTable,
       STUFF((SELECT ',' + c2.name FROM sys.foreign_key_columns fkc2 INNER JOIN sys.columns c2 ON c2.object_id = fkc2.parent_object_id AND c2.column_id = fkc2.parent_column_id WHERE fkc2.constraint_object_id = fk.object_id ORDER BY fkc2.constraint_column_id FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, '') AS Columns,
       STUFF((SELECT ',' + rc2.name FROM sys.foreign_key_columns fkc3 INNER JOIN sys.columns rc2 ON rc2.object_id = fkc3.referenced_object_id AND rc2.column_id = fkc3.referenced_column_id WHERE fkc3.constraint_object_id = fk.object_id ORDER BY fkc3.constraint_column_id FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 1, '') AS ReferencedColumns
FROM sys.foreign_keys fk
INNER JOIN sys.tables t ON t.object_id = fk.parent_object_id
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
INNER JOIN sys.tables rt ON rt.object_id = fk.referenced_object_id
INNER JOIN sys.schemas rs ON rs.schema_id = rt.schema_id
WHERE s.name = N'dbo' AND t.name IN ({InClause(contract)});";

        private static string BuildChecksSql(SchemaContract contract) => $@"
SELECT s.name AS SchemaName, t.name AS TableName, cc.name AS CheckName, cc.definition AS Definition
FROM sys.check_constraints cc
INNER JOIN sys.tables t ON t.object_id = cc.parent_object_id
INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE s.name = N'dbo' AND t.name IN ({InClause(contract)});";

        private static string ToSqlType(DataRow row)
        {
            string type = ((string)row["TypeName"]).ToUpperInvariant();
            short maxLength = (short)row["MaxLength"];
            byte precision = (byte)row["Precision"];
            byte scale = (byte)row["Scale"];
            return type switch
            {
                "NVARCHAR" or "NCHAR" => maxLength == -1 ? $"{type}(MAX)" : $"{type}({maxLength / 2})",
                "VARCHAR" or "CHAR" or "VARBINARY" => maxLength == -1 ? $"{type}(MAX)" : $"{type}({maxLength})",
                "DECIMAL" or "NUMERIC" => $"{type}({precision},{scale})",
                "DATETIME2" => $"{type}({scale})",
                _ => type
            };
        }

        private static string NormalizeType(string type) => type.Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();

        private static bool SameDefinition(object dbValue, string? expected)
        {
            string? actual = dbValue == DBNull.Value ? null : dbValue.ToString();
            if (string.IsNullOrWhiteSpace(actual) && string.IsNullOrWhiteSpace(expected))
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(actual) || string.IsNullOrWhiteSpace(expected))
            {
                return false;
            }

            return SchemaDefinitionNormalizer.Same(actual, expected);
        }

    }
}
