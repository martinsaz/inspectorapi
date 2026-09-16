using System.Text;

namespace checklistWs.Services.Tenant
{
    public sealed class SchemaProvisionSqlGenerator : ISchemaContractSqlGenerator
    {
        public IReadOnlyCollection<string> CreateProvisioningCommands(SchemaContract contract)
        {
            if (contract == null)
            {
                throw new ArgumentNullException(nameof(contract));
            }

            List<string> commands = new();
            foreach (SchemaTableContract table in OrderedTables(contract))
            {
                commands.Add(CreateTableSql(table));
            }

            foreach (SchemaTableContract table in OrderedTables(contract))
            {
                foreach (SchemaCheckContract check in table.CheckConstraints.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
                {
                    commands.Add($"ALTER TABLE {Quote(table.Schema)}.{Quote(table.Name)} ADD CONSTRAINT {Quote(check.Name)} {check.Definition};");
                }
            }

            foreach (SchemaTableContract table in OrderedTables(contract))
            {
                foreach (SchemaIndexContract index in table.Indexes.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
                {
                    commands.Add(CreateIndexSql(table, index));
                }
            }

            foreach (SchemaTableContract table in OrderedTables(contract))
            {
                foreach (SchemaForeignKeyContract foreignKey in table.ForeignKeys.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase))
                {
                    commands.Add(CreateForeignKeySql(table, foreignKey));
                }
            }

            return commands.ToArray();
        }

        private static IReadOnlyCollection<SchemaTableContract> OrderedTables(SchemaContract contract)
        {
            return contract.Tables
                .OrderBy(table => table.ForeignKeys.Count, Comparer<int>.Default)
                .ThenBy(table => table.Schema, StringComparer.OrdinalIgnoreCase)
                .ThenBy(table => table.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static string CreateTableSql(SchemaTableContract table)
        {
            StringBuilder sql = new();
            sql.AppendLine($"CREATE TABLE {Quote(table.Schema)}.{Quote(table.Name)}");
            sql.AppendLine("(");

            List<string> definitions = new();
            foreach (SchemaColumnContract column in table.Columns.Where(column => !column.IsComputed))
            {
                string line = $"    {Quote(column.Name)} {column.SqlType} {(column.IsNullable ? "NULL" : "NOT NULL")}";
                if (!string.IsNullOrWhiteSpace(column.DefaultDefinition))
                {
                    line += $" CONSTRAINT {Quote($"DF_{table.Name}_{column.Name}")} DEFAULT {column.DefaultDefinition}";
                }

                definitions.Add(line);
            }

            foreach (SchemaColumnContract column in table.Columns.Where(column => column.IsComputed))
            {
                definitions.Add($"    {Quote(column.Name)} AS {column.ComputedDefinition}");
            }

            if (table.PrimaryKey != null)
            {
                string clustered = table.PrimaryKey.IsClustered ? "CLUSTERED" : "NONCLUSTERED";
                definitions.Add($"    CONSTRAINT {Quote(table.PrimaryKey.Name)} PRIMARY KEY {clustered} ({JoinColumns(table.PrimaryKey.Columns)})");
            }

            sql.AppendLine(string.Join(",\n", definitions));
            sql.AppendLine(");");
            return sql.ToString();
        }

        private static string CreateIndexSql(SchemaTableContract table, SchemaIndexContract index)
        {
            string unique = index.IsUnique ? "UNIQUE " : string.Empty;
            string clustered = index.IsClustered ? "CLUSTERED" : "NONCLUSTERED";
            string keys = string.Join(", ", index.KeyColumns.Select(column => $"{Quote(column.Name)}{(column.IsDescending ? " DESC" : string.Empty)}"));
            string includes = index.IncludedColumns.Count > 0
                ? $" INCLUDE ({JoinColumns(index.IncludedColumns)})"
                : string.Empty;
            string filter = string.IsNullOrWhiteSpace(index.FilterDefinition)
                ? string.Empty
                : $" WHERE {index.FilterDefinition}";

            return $"CREATE {unique}{clustered} INDEX {Quote(index.Name)} ON {Quote(table.Schema)}.{Quote(table.Name)} ({keys}){includes}{filter};";
        }

        private static string CreateForeignKeySql(SchemaTableContract table, SchemaForeignKeyContract foreignKey)
        {
            return $"ALTER TABLE {Quote(table.Schema)}.{Quote(table.Name)} ADD CONSTRAINT {Quote(foreignKey.Name)} FOREIGN KEY ({JoinColumns(foreignKey.Columns)}) REFERENCES {Quote(foreignKey.ReferencedSchema)}.{Quote(foreignKey.ReferencedTable)} ({JoinColumns(foreignKey.ReferencedColumns)});";
        }

        private static string JoinColumns(IEnumerable<string> columns)
        {
            return string.Join(", ", columns.Select(Quote));
        }

        private static string Quote(string identifier)
        {
            return $"[{identifier.Replace("]", "]]", StringComparison.Ordinal)}]";
        }
    }
}
