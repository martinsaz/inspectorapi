using System.Data;
using System.Data.SqlClient;

namespace checklistWs.Services.Tenant
{
    public sealed class SqlSchemaPhysicalSnapshotReader : ISchemaPhysicalSnapshotReader
    {
        public async Task<SchemaPhysicalSnapshot> ReadAsync(SqlConnection connection, SchemaContract contract, CancellationToken cancellationToken = default, SqlTransaction? transaction = null)
        {
            try
            {
                DataTable objects = await QueryAsync(connection, transaction, BuildObjectsSql(contract), contract, cancellationToken);
                DataTable columns = await QueryAsync(connection, transaction, BuildColumnsSql(contract), contract, cancellationToken);
                DataTable primaryKeys = await QueryAsync(connection, transaction, BuildPrimaryKeysSql(contract), contract, cancellationToken);
                DataTable indexes = await QueryAsync(connection, transaction, BuildIndexesSql(contract), contract, cancellationToken);
                DataTable fks = await QueryAsync(connection, transaction, BuildForeignKeysSql(contract), contract, cancellationToken);
                DataTable checks = await QueryAsync(connection, transaction, BuildChecksSql(contract), contract, cancellationToken);
                DataTable extras = await QueryAsync(connection, transaction, BuildExtrasSql(contract), contract, cancellationToken);

                return new SchemaPhysicalSnapshot
                {
                    Objects = objects.AsEnumerable().Select(r => new SchemaObjectSnapshot(S(r, "SchemaName"), S(r, "ObjectName"), S(r, "TypeDescription"))).OrderBy(x => x.Schema).ThenBy(x => x.Name).ToArray(),
                    Tables = objects.AsEnumerable().Where(r => string.Equals(S(r, "TypeDescription"), "USER_TABLE", StringComparison.OrdinalIgnoreCase)).Select(r => new SchemaTableSnapshot(S(r, "SchemaName"), S(r, "ObjectName"))).OrderBy(x => x.Schema).ThenBy(x => x.Name).ToArray(),
                    Columns = columns.AsEnumerable().Select(r => new SchemaColumnSnapshot(S(r, "SchemaName"), S(r, "TableName"), S(r, "ColumnName"), ToSqlType(r), ML(r), B(r, "Precision"), I(r, "Scale"), Bool(r, "IsNullable"), N(r, "CollationName"), Bool(r, "IsIdentity"), Dec(r, "SeedValue"), Dec(r, "IncrementValue"), Bool(r, "IsComputed"), N(r, "ComputedDefinition"), BoolN(r, "IsPersisted"), N(r, "DefaultDefinition"))).OrderBy(x => x.Schema).ThenBy(x => x.Table).ThenBy(x => x.Name).ToArray(),
                    PrimaryKeys = primaryKeys.AsEnumerable().GroupBy(r => new { Schema = S(r, "SchemaName"), Table = S(r, "TableName"), Name = S(r, "PrimaryKeyName") }).Select(g => new SchemaPrimaryKeySnapshot(g.Key.Schema, g.Key.Table, g.Key.Name, g.OrderBy(r => Convert.ToInt32(r["KeyOrdinal"])).Select(r => new SchemaIndexColumnContract(S(r, "ColumnName"), Bool(r, "IsDescending"))).ToArray(), Bool(g.First(), "IsClustered"), Bool(g.First(), "IsDisabled"))).OrderBy(x => x.Schema).ThenBy(x => x.Table).ThenBy(x => x.Name).ToArray(),
                    Indexes = indexes.AsEnumerable().GroupBy(r => new { Schema = S(r, "SchemaName"), Table = S(r, "TableName"), Name = S(r, "IndexName") }).Select(g => new SchemaIndexSnapshot(g.Key.Schema, g.Key.Table, g.Key.Name, Bool(g.First(), "IsUnique"), Bool(g.First(), "IsClustered"), g.Where(r => !Bool(r, "IsIncluded")).OrderBy(r => Convert.ToInt32(r["KeyOrdinal"])).Select(r => new SchemaIndexColumnContract(S(r, "ColumnName"), Bool(r, "IsDescending"))).ToArray(), g.Where(r => Bool(r, "IsIncluded")).Select(r => S(r, "ColumnName")).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray(), N(g.First(), "FilterDefinition"), Bool(g.First(), "IsDisabled"), Bool(g.First(), "IsUniqueConstraint"))).OrderBy(x => x.Schema).ThenBy(x => x.Table).ThenBy(x => x.Name).ToArray(),
                    ForeignKeys = fks.AsEnumerable().GroupBy(r => new { Schema = S(r, "SchemaName"), Table = S(r, "TableName"), Name = S(r, "ForeignKeyName") }).Select(g => new SchemaForeignKeySnapshot(g.Key.Schema, g.Key.Table, g.Key.Name, g.OrderBy(r => Convert.ToInt32(r["ConstraintColumnId"])).Select(r => S(r, "ColumnName")).ToArray(), S(g.First(), "ReferencedSchema"), S(g.First(), "ReferencedTable"), g.OrderBy(r => Convert.ToInt32(r["ConstraintColumnId"])).Select(r => S(r, "ReferencedColumnName")).ToArray(), S(g.First(), "DeleteAction"), S(g.First(), "UpdateAction"), Bool(g.First(), "IsDisabled"), Bool(g.First(), "IsNotTrusted"), Bool(g.First(), "IsNotForReplication"))).OrderBy(x => x.Schema).ThenBy(x => x.Table).ThenBy(x => x.Name).ToArray(),
                    Checks = checks.AsEnumerable().Select(r => new SchemaCheckSnapshot(S(r, "SchemaName"), S(r, "TableName"), S(r, "CheckName"), S(r, "Definition"), Bool(r, "IsDisabled"), Bool(r, "IsNotTrusted"))).OrderBy(x => x.Schema).ThenBy(x => x.Table).ThenBy(x => x.Name).ToArray(),
                    Extras = extras.AsEnumerable().Select(r => $"{S(r, "SchemaName")}.{S(r, "ObjectName")}:{S(r, "TypeDescription")}").OrderBy(x => x).ToArray()
                };
            }
            catch (SqlException)
            {
                return new SchemaPhysicalSnapshot { MetadataSufficient = false };
            }
        }

        private static async Task<DataTable> QueryAsync(SqlConnection connection, SqlTransaction? transaction, string sql, SchemaContract contract, CancellationToken cancellationToken)
        {
            using SqlCommand command = new SqlCommand(sql, connection, transaction);
            string[] names = contract.Tables.Select(t => t.Name).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToArray();
            for (int i = 0; i < names.Length; i++) command.Parameters.AddWithValue($"@Table{i:00}", names[i]);
            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            DataTable table = new(); table.Load(reader); return table;
        }
        private static string InClause(SchemaContract c) => string.Join(", ", c.Tables.Select((_, i) => $"@Table{i:00}"));
        private static string S(DataRow r, string c) => r[c] == DBNull.Value ? string.Empty : Convert.ToString(r[c])!.Trim();
        private static string? N(DataRow r, string c) => r[c] == DBNull.Value ? null : Convert.ToString(r[c])!.Trim();
        private static bool Bool(DataRow r, string c) => r[c] != DBNull.Value && Convert.ToBoolean(r[c]);
        private static bool? BoolN(DataRow r, string c) => r[c] == DBNull.Value ? null : Convert.ToBoolean(r[c]);
        private static byte? B(DataRow r, string c) => r[c] == DBNull.Value ? null : Convert.ToByte(r[c]);
        private static int? I(DataRow r, string c) => r[c] == DBNull.Value ? null : Convert.ToInt32(r[c]);
        private static decimal? Dec(DataRow r, string c) => r[c] == DBNull.Value ? null : Convert.ToDecimal(r[c]);
        private static int? ML(DataRow r) { short max = Convert.ToInt16(r["MaxLength"]); string ty = S(r, "TypeName").ToUpperInvariant(); return ty is "NVARCHAR" or "NCHAR" ? (max == -1 ? -1 : max / 2) : max; }
        private static string ToSqlType(DataRow r)
        {
            string type = S(r, "TypeName").ToUpperInvariant(); int? max = ML(r); byte? precision = B(r, "Precision"); int? scale = I(r, "Scale");
            return type switch { "NVARCHAR" or "NCHAR" => max == -1 ? $"{type}(MAX)" : $"{type}({max})", "VARCHAR" or "CHAR" or "VARBINARY" => max == -1 ? $"{type}(MAX)" : $"{type}({max})", "DECIMAL" or "NUMERIC" => $"{type}({precision},{scale})", "DATETIME2" => $"{type}({scale})", _ => type };
        }

        private static string BuildObjectsSql(SchemaContract c) => $@"SELECT s.name SchemaName, o.name ObjectName, o.type_desc TypeDescription FROM sys.objects o JOIN sys.schemas s ON s.schema_id=o.schema_id WHERE s.name=N'dbo' AND o.name IN ({InClause(c)});";
        private static string BuildColumnsSql(SchemaContract c) => $@"SELECT s.name SchemaName,t.name TableName,c.name ColumnName,ty.name TypeName,c.max_length MaxLength,c.precision Precision,c.scale Scale,c.is_nullable IsNullable,c.collation_name CollationName,c.is_identity IsIdentity,ic.seed_value SeedValue,ic.increment_value IncrementValue,c.is_computed IsComputed,cc.definition ComputedDefinition,cc.is_persisted IsPersisted,dc.definition DefaultDefinition FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id JOIN sys.columns c ON c.object_id=t.object_id JOIN sys.types ty ON ty.user_type_id=c.user_type_id LEFT JOIN sys.identity_columns ic ON ic.object_id=c.object_id AND ic.column_id=c.column_id LEFT JOIN sys.computed_columns cc ON cc.object_id=c.object_id AND cc.column_id=c.column_id LEFT JOIN sys.default_constraints dc ON dc.object_id=c.default_object_id WHERE s.name=N'dbo' AND t.name IN ({InClause(c)});";
        private static string BuildPrimaryKeysSql(SchemaContract c) => $@"SELECT s.name SchemaName,t.name TableName,kc.name PrimaryKeyName,CAST(CASE WHEN i.type=1 THEN 1 ELSE 0 END AS bit) IsClustered,i.is_disabled IsDisabled,ic.key_ordinal KeyOrdinal,col.name ColumnName,ic.is_descending_key IsDescending FROM sys.key_constraints kc JOIN sys.tables t ON t.object_id=kc.parent_object_id JOIN sys.schemas s ON s.schema_id=t.schema_id JOIN sys.indexes i ON i.object_id=kc.parent_object_id AND i.index_id=kc.unique_index_id JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id JOIN sys.columns col ON col.object_id=ic.object_id AND col.column_id=ic.column_id WHERE kc.type='PK' AND ic.is_included_column=0 AND s.name=N'dbo' AND t.name IN ({InClause(c)});";
        private static string BuildIndexesSql(SchemaContract c) => $@"SELECT s.name SchemaName,t.name TableName,i.name IndexName,i.is_unique IsUnique,CAST(CASE WHEN i.type=1 THEN 1 ELSE 0 END AS bit) IsClustered,i.is_disabled IsDisabled,i.is_unique_constraint IsUniqueConstraint,i.filter_definition FilterDefinition,ic.key_ordinal KeyOrdinal,ic.is_included_column IsIncluded,ic.is_descending_key IsDescending,col.name ColumnName FROM sys.indexes i JOIN sys.tables t ON t.object_id=i.object_id JOIN sys.schemas s ON s.schema_id=t.schema_id JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id JOIN sys.columns col ON col.object_id=ic.object_id AND col.column_id=ic.column_id WHERE s.name=N'dbo' AND t.name IN ({InClause(c)}) AND i.name IS NOT NULL AND i.is_primary_key=0;";
        private static string BuildForeignKeysSql(SchemaContract c) => $@"SELECT s.name SchemaName,t.name TableName,fk.name ForeignKeyName,rs.name ReferencedSchema,rt.name ReferencedTable,fkc.constraint_column_id ConstraintColumnId,pc.name ColumnName,rc.name ReferencedColumnName,fk.delete_referential_action_desc DeleteAction,fk.update_referential_action_desc UpdateAction,fk.is_disabled IsDisabled,fk.is_not_trusted IsNotTrusted,fk.is_not_for_replication IsNotForReplication FROM sys.foreign_keys fk JOIN sys.tables t ON t.object_id=fk.parent_object_id JOIN sys.schemas s ON s.schema_id=t.schema_id JOIN sys.tables rt ON rt.object_id=fk.referenced_object_id JOIN sys.schemas rs ON rs.schema_id=rt.schema_id JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id=fk.object_id JOIN sys.columns pc ON pc.object_id=fkc.parent_object_id AND pc.column_id=fkc.parent_column_id JOIN sys.columns rc ON rc.object_id=fkc.referenced_object_id AND rc.column_id=fkc.referenced_column_id WHERE s.name=N'dbo' AND t.name IN ({InClause(c)});";
        private static string BuildChecksSql(SchemaContract c) => $@"SELECT s.name SchemaName,t.name TableName,cc.name CheckName,cc.definition Definition,cc.is_disabled IsDisabled,cc.is_not_trusted IsNotTrusted FROM sys.check_constraints cc JOIN sys.tables t ON t.object_id=cc.parent_object_id JOIN sys.schemas s ON s.schema_id=t.schema_id WHERE s.name=N'dbo' AND t.name IN ({InClause(c)});";
        private static string BuildExtrasSql(SchemaContract c)
        {
            string scopePrefix = string.Equals(c.Scope, DatabaseScopes.Sucursales, StringComparison.OrdinalIgnoreCase)
                ? "Sucursales%"
                : string.Equals(c.Scope, DatabaseScopes.ProductosServicios, StringComparison.OrdinalIgnoreCase)
                    ? "ProductosServicios%"
                    : $"{c.Scope}%";

            return $@"SELECT s.name SchemaName,o.name ObjectName,o.type_desc TypeDescription FROM sys.objects o JOIN sys.schemas s ON s.schema_id=o.schema_id WHERE s.name=N'dbo' AND o.name LIKE N'{scopePrefix}' AND o.name NOT IN ({InClause(c)}) AND o.type NOT IN ('PK','F','C','D');";
        }
    }
}
