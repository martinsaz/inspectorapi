using System.Data.SqlClient;

namespace checklistWs.Services.Tenant
{
    public sealed class SchemaDriftValidator : ISchemaDriftValidator
    {
        private readonly ITenantSqlConnectionFactory _connectionFactory;
        private readonly ISchemaContractProvider _contractProvider;
        private readonly ISchemaManifestProvider _manifestProvider;
        private readonly ISchemaPhysicalSnapshotReader _snapshotReader;

        public SchemaDriftValidator(ITenantSqlConnectionFactory connectionFactory, ISchemaContractProvider contractProvider, ISchemaManifestProvider manifestProvider, ISchemaPhysicalSnapshotReader snapshotReader)
        {
            _connectionFactory = connectionFactory;
            _contractProvider = contractProvider;
            _manifestProvider = manifestProvider;
            _snapshotReader = snapshotReader;
        }

        public async Task<SchemaDriftReport> ValidateAsync(TenantDatabaseDescriptor descriptor, DatabaseIdentity identity, string scope, int? declaredVersion, string? persistedManifestHash, CancellationToken cancellationToken = default)
        {
            using SqlConnection connection = _connectionFactory.CreateConnection(descriptor);
            await connection.OpenAsync(cancellationToken);
            return await ValidateAsync(connection, null, identity, scope, declaredVersion, persistedManifestHash, cancellationToken);
        }

        public async Task<SchemaDriftReport> ValidateAsync(SqlConnection connection, SqlTransaction? transaction, DatabaseIdentity identity, string scope, int? declaredVersion, string? persistedManifestHash, CancellationToken cancellationToken = default)
        {
            if (declaredVersion == null)
            {
                return Build(identity, scope, null, null, persistedManifestHash, SchemaValidationGlobalResult.RequiereRevision, new[] { Item(identity, scope, null, null, "STATE", scope, "VERSION_MISSING", "ContractVersion", null, SchemaDriftSeverity.Warning, "UNKNOWN", "VERSION_MISSING") });
            }

            SchemaContract contract;
            try
            {
                contract = _contractProvider.GetContract(scope, declaredVersion);
            }
            catch
            {
                return Build(identity, scope, declaredVersion, null, persistedManifestHash, SchemaValidationGlobalResult.VersionIncompatible, new[] { Item(identity, scope, declaredVersion, null, "VERSION", scope, "VERSION_INCOMPATIBLE", "known contract", declaredVersion.ToString(), SchemaDriftSeverity.Critical, "NO", "VERSION_INCOMPATIBLE") });
            }

            SchemaManifest manifest = _manifestProvider.CreateManifest(contract);
            List<SchemaDriftItem> items = new();
            if (!string.IsNullOrWhiteSpace(persistedManifestHash) && !string.Equals(persistedManifestHash, manifest.ManifestHash, StringComparison.OrdinalIgnoreCase))
            {
                items.Add(Item(identity, scope, declaredVersion, manifest.ManifestHash, "STATE", scope, "MANIFEST_HASH_MISMATCH", manifest.ManifestHash, persistedManifestHash, SchemaDriftSeverity.Critical, "NO", "MANIFEST_HASH_MISMATCH"));
            }

            SchemaPhysicalSnapshot snapshot = await _snapshotReader.ReadAsync(connection, contract, cancellationToken, transaction);
            if (!snapshot.MetadataSufficient)
            {
                items.Add(Item(identity, scope, declaredVersion, manifest.ManifestHash, "METADATA", scope, "METADATA_INSUFFICIENT", "readable sys metadata", null, SchemaDriftSeverity.Error, "UNKNOWN", "VALIDACION_NO_CONCLUYENTE"));
                return Build(identity, scope, declaredVersion, manifest.ManifestHash, persistedManifestHash, SchemaValidationGlobalResult.ValidacionNoConcluyente, items, snapshot);
            }

            Compare(contract, snapshot, identity, scope, declaredVersion.Value, manifest.ManifestHash, items);
            SchemaValidationGlobalResult global = items.Count == 0 ? SchemaValidationGlobalResult.SchemaOk : items.Any(i => i.Severity == SchemaDriftSeverity.Critical) ? SchemaValidationGlobalResult.SchemaDriftCritico : SchemaValidationGlobalResult.SchemaDrift;
            return Build(identity, scope, declaredVersion, manifest.ManifestHash, persistedManifestHash, global, items, snapshot);
        }

        private static void Compare(SchemaContract contract, SchemaPhysicalSnapshot snapshot, DatabaseIdentity identity, string scope, int version, string hash, List<SchemaDriftItem> items)
        {
            foreach (SchemaTableContract table in contract.Tables.OrderBy(t => t.FullName))
            {
                SchemaObjectSnapshot? homonym = snapshot.Objects.SingleOrDefault(o => Eq(o.Schema, table.Schema) && Eq(o.Name, table.Name));
                if (homonym != null && !Eq(homonym.TypeDescription, "USER_TABLE"))
                {
                    Add(items, identity, scope, version, hash, "TABLE", table.FullName, "OBJECT_TYPE_MISMATCH", "USER_TABLE", homonym.TypeDescription, SchemaDriftSeverity.Critical, "NO");
                    continue;
                }

                if (!snapshot.Tables.Any(t => Eq(t.Schema, table.Schema) && Eq(t.Name, table.Name)))
                {
                    Add(items, identity, scope, version, hash, "TABLE", table.FullName, "TABLE_MISSING", "TABLE", null, SchemaDriftSeverity.Critical, "YES");
                    continue;
                }

                CompareColumns(table, snapshot, identity, scope, version, hash, items);
                ComparePrimaryKey(table, snapshot, identity, scope, version, hash, items);
                CompareIndexes(table, snapshot, identity, scope, version, hash, items);
                CompareForeignKeys(table, snapshot, identity, scope, version, hash, items);
                CompareChecks(table, snapshot, identity, scope, version, hash, items);
            }

            foreach (SchemaTableSnapshot extra in snapshot.Tables.Where(t => contract.Tables.All(e => !Eq(e.Schema, t.Schema) || !Eq(e.Name, t.Name))))
            {
                Add(items, identity, scope, version, hash, "TABLE", $"{extra.Schema}.{extra.Name}", "TABLE_UNEXPECTED", null, "TABLE", SchemaDriftSeverity.Warning, "UNKNOWN");
            }

            foreach (string extra in snapshot.Extras)
            {
                Add(items, identity, scope, version, hash, "EXTRA", extra, "EXTRA_REVIEW", null, extra, SchemaDriftSeverity.Info, "UNKNOWN");
            }
        }

        private static void CompareColumns(SchemaTableContract table, SchemaPhysicalSnapshot snapshot, DatabaseIdentity identity, string scope, int version, string hash, List<SchemaDriftItem> items)
        {
            SchemaColumnSnapshot[] actual = snapshot.Columns.Where(c => Eq(c.Schema, table.Schema) && Eq(c.Table, table.Name)).ToArray();
            foreach (SchemaColumnContract expected in table.Columns)
            {
                SchemaColumnSnapshot? found = actual.SingleOrDefault(c => Eq(c.Name, expected.Name));
                string obj = $"{table.FullName}.{expected.Name}";
                if (found == null) { Add(items, identity, scope, version, hash, "COLUMN", obj, "COLUMN_MISSING", expected.SqlType, null, SchemaDriftSeverity.Error, "YES"); continue; }
                if (!Eq(NT(expected.SqlType), NT(found.SqlType))) Add(items, identity, scope, version, hash, "COLUMN", obj, "COLUMN_TYPE_MISMATCH", NT(expected.SqlType), NT(found.SqlType), ColumnSeverity(expected), "UNKNOWN");
                if (expected.MaxLength != null && expected.MaxLength != found.MaxLength) Add(items, identity, scope, version, hash, "COLUMN", obj, "COLUMN_LENGTH_MISMATCH", expected.MaxLength.ToString(), found.MaxLength?.ToString(), ColumnSeverity(expected), "UNKNOWN");
                if (expected.Precision != null && expected.Precision != found.Precision) Add(items, identity, scope, version, hash, "COLUMN", obj, "COLUMN_PRECISION_MISMATCH", expected.Precision.ToString(), found.Precision?.ToString(), ColumnSeverity(expected), "UNKNOWN");
                if (expected.Scale != null && expected.Scale != found.Scale) Add(items, identity, scope, version, hash, "COLUMN", obj, "COLUMN_SCALE_MISMATCH", expected.Scale.ToString(), found.Scale?.ToString(), ColumnSeverity(expected), "UNKNOWN");
                if (expected.IsNullable != found.IsNullable) Add(items, identity, scope, version, hash, "COLUMN", obj, "COLUMN_NULLABILITY_MISMATCH", expected.IsNullable.ToString(), found.IsNullable.ToString(), ColumnSeverity(expected), "UNKNOWN");
                if (expected.IsIdentity != found.IsIdentity) Add(items, identity, scope, version, hash, "COLUMN", obj, "COLUMN_IDENTITY_MISMATCH", expected.IsIdentity.ToString(), found.IsIdentity.ToString(), SchemaDriftSeverity.Critical, "UNKNOWN");
                if (expected.IsComputed != found.IsComputed) Add(items, identity, scope, version, hash, "COLUMN", obj, "COLUMN_COMPUTED_MISMATCH", expected.IsComputed.ToString(), found.IsComputed.ToString(), SchemaDriftSeverity.Error, "UNKNOWN");
                if (expected.IsComputed && !SchemaDefinitionNormalizer.Same(found.ComputedDefinition, expected.ComputedDefinition)) Add(items, identity, scope, version, hash, "COLUMN", obj, "COLUMN_COMPUTED_DEFINITION_MISMATCH", expected.ComputedDefinition, found.ComputedDefinition, SchemaDriftSeverity.Error, "UNKNOWN");
                if (!SchemaDefinitionNormalizer.Same(found.DefaultDefinition, expected.DefaultDefinition)) Add(items, identity, scope, version, hash, "COLUMN", obj, "COLUMN_DEFAULT_MISMATCH", expected.DefaultDefinition, found.DefaultDefinition, SchemaDriftSeverity.Warning, "UNKNOWN");
            }
            foreach (SchemaColumnSnapshot extra in actual.Where(a => table.Columns.All(e => !Eq(e.Name, a.Name)))) Add(items, identity, scope, version, hash, "COLUMN", $"{table.FullName}.{extra.Name}", "COLUMN_UNEXPECTED", null, extra.SqlType, SchemaDriftSeverity.Warning, "UNKNOWN");
        }

        private static void ComparePrimaryKey(SchemaTableContract table, SchemaPhysicalSnapshot snapshot, DatabaseIdentity identity, string scope, int version, string hash, List<SchemaDriftItem> items)
        {
            SchemaPrimaryKeySnapshot? actual = snapshot.PrimaryKeys.SingleOrDefault(x => Eq(x.Schema, table.Schema) && Eq(x.Table, table.Name));
            if (table.PrimaryKey == null) { if (actual != null) Add(items, identity, scope, version, hash, "PK", table.FullName, "PK_UNEXPECTED", null, actual.Name, SchemaDriftSeverity.Error, "UNKNOWN"); return; }
            if (actual == null) { Add(items, identity, scope, version, hash, "PK", table.FullName, "PK_MISSING", table.PrimaryKey.Name, null, SchemaDriftSeverity.Critical, "YES"); return; }
            if (!Eq(actual.Name, table.PrimaryKey.Name) || !Seq(actual.Columns.Select(c => c.Name), table.PrimaryKey.Columns) || actual.IsClustered != table.PrimaryKey.IsClustered || actual.IsDisabled) Add(items, identity, scope, version, hash, "PK", table.FullName, "PK_DEFINITION_MISMATCH", table.PrimaryKey.Name, actual.Name, SchemaDriftSeverity.Critical, "UNKNOWN");
        }

        private static void CompareIndexes(SchemaTableContract table, SchemaPhysicalSnapshot snapshot, DatabaseIdentity identity, string scope, int version, string hash, List<SchemaDriftItem> items)
        {
            SchemaIndexSnapshot[] actual = snapshot.Indexes.Where(x => Eq(x.Schema, table.Schema) && Eq(x.Table, table.Name) && !x.IsUniqueConstraint).ToArray();
            foreach (SchemaIndexContract expected in table.Indexes)
            {
                SchemaIndexSnapshot? found = actual.SingleOrDefault(x => Eq(x.Name, expected.Name));
                string obj = $"{table.FullName}.{expected.Name}";
                if (found == null) { Add(items, identity, scope, version, hash, "INDEX", obj, "INDEX_MISSING", expected.Name, null, SchemaDriftSeverity.Error, "YES"); continue; }
                if (found.IsUnique != expected.IsUnique) Add(items, identity, scope, version, hash, "INDEX", obj, "INDEX_UNIQUE_MISMATCH", expected.IsUnique.ToString(), found.IsUnique.ToString(), IndexSeverity(expected, table), "UNKNOWN");
                if (found.IsClustered != expected.IsClustered) Add(items, identity, scope, version, hash, "INDEX", obj, "INDEX_CLUSTERING_MISMATCH", expected.IsClustered.ToString(), found.IsClustered.ToString(), SchemaDriftSeverity.Error, "UNKNOWN");
                if (!Seq(found.KeyColumns.Select(c => c.Name + (c.IsDescending ? " DESC" : " ASC")), expected.KeyColumns.Select(c => c.Name + (c.IsDescending ? " DESC" : " ASC"))))
                {
                    SchemaDriftSeverity severity = expected.IsUnique && table.Columns.Any(c => Eq(c.Name, "idEmpresa")) && !found.KeyColumns.Any(c => Eq(c.Name, "idEmpresa"))
                        ? SchemaDriftSeverity.Critical
                        : IndexSeverity(expected, table);
                    string reason = severity == SchemaDriftSeverity.Critical ? "UNIQUE_MULTITENANT_INSEGURA" : "INDEX_KEYS_MISMATCH";
                    Add(items, identity, scope, version, hash, "INDEX", obj, "INDEX_KEYS_MISMATCH", string.Join(',', expected.KeyColumns.Select(c => c.Name)), string.Join(',', found.KeyColumns.Select(c => c.Name)), severity, "UNKNOWN", reason);
                }
                if (!Set(found.IncludedColumns, expected.IncludedColumns)) Add(items, identity, scope, version, hash, "INDEX", obj, "INDEX_INCLUDE_MISMATCH", string.Join(',', expected.IncludedColumns), string.Join(',', found.IncludedColumns), SchemaDriftSeverity.Error, "UNKNOWN");
                if (!SchemaDefinitionNormalizer.Same(found.FilterDefinition, expected.FilterDefinition)) Add(items, identity, scope, version, hash, "INDEX", obj, "INDEX_FILTER_MISMATCH", expected.FilterDefinition, found.FilterDefinition, IndexSeverity(expected, table), "UNKNOWN");
                if (found.IsDisabled) Add(items, identity, scope, version, hash, "INDEX", obj, "INDEX_STATE_MISMATCH", "enabled", "disabled", SchemaDriftSeverity.Error, "UNKNOWN");
            }
            foreach (SchemaIndexSnapshot extra in actual.Where(a => table.Indexes.All(e => !Eq(e.Name, a.Name)))) Add(items, identity, scope, version, hash, "INDEX", $"{table.FullName}.{extra.Name}", "INDEX_UNEXPECTED", null, extra.Name, SchemaDriftSeverity.Info, "UNKNOWN");
        }

        private static void CompareForeignKeys(SchemaTableContract table, SchemaPhysicalSnapshot snapshot, DatabaseIdentity identity, string scope, int version, string hash, List<SchemaDriftItem> items)
        {
            SchemaForeignKeySnapshot[] actual = snapshot.ForeignKeys.Where(x => Eq(x.Schema, table.Schema) && Eq(x.Table, table.Name)).ToArray();
            foreach (SchemaForeignKeyContract expected in table.ForeignKeys)
            {
                SchemaForeignKeySnapshot? found = actual.SingleOrDefault(x => Eq(x.Name, expected.Name)); string obj = $"{table.FullName}.{expected.Name}";
                if (found == null) { Add(items, identity, scope, version, hash, "FK", obj, "FK_MISSING", expected.Name, null, SchemaDriftSeverity.Error, "YES"); continue; }
                bool insecureTenant = expected.Columns.Contains("idEmpresa", StringComparer.OrdinalIgnoreCase) && !found.Columns.Contains("idEmpresa", StringComparer.OrdinalIgnoreCase);
                if (!Seq(found.Columns, expected.Columns)) Add(items, identity, scope, version, hash, "FK", obj, "FK_COLUMNS_MISMATCH", string.Join(',', expected.Columns), string.Join(',', found.Columns), insecureTenant ? SchemaDriftSeverity.Critical : SchemaDriftSeverity.Error, "UNKNOWN", insecureTenant ? "FK_MULTITENANT_INSEGURA" : "FK_COLUMNS_MISMATCH");
                if (!Eq(found.ReferencedSchema, expected.ReferencedSchema) || !Eq(found.ReferencedTable, expected.ReferencedTable) || !Seq(found.ReferencedColumns, expected.ReferencedColumns)) Add(items, identity, scope, version, hash, "FK", obj, "FK_TARGET_MISMATCH", $"{expected.ReferencedSchema}.{expected.ReferencedTable}({string.Join(',', expected.ReferencedColumns)})", $"{found.ReferencedSchema}.{found.ReferencedTable}({string.Join(',', found.ReferencedColumns)})", SchemaDriftSeverity.Error, "UNKNOWN");
                if (!Eq(found.DeleteAction, "NO_ACTION") || !Eq(found.UpdateAction, "NO_ACTION")) Add(items, identity, scope, version, hash, "FK", obj, "FK_ACTIONS_MISMATCH", "NO_ACTION/NO_ACTION", $"{found.DeleteAction}/{found.UpdateAction}", SchemaDriftSeverity.Error, "UNKNOWN");
                if (found.IsDisabled || found.IsNotTrusted || found.IsNotForReplication) Add(items, identity, scope, version, hash, "FK", obj, "FK_STATE_MISMATCH", "enabled/trusted/not replicated", $"disabled={found.IsDisabled};untrusted={found.IsNotTrusted};nfr={found.IsNotForReplication}", SchemaDriftSeverity.Error, "UNKNOWN");
            }
            foreach (SchemaForeignKeySnapshot extra in actual.Where(a => table.ForeignKeys.All(e => !Eq(e.Name, a.Name)))) Add(items, identity, scope, version, hash, "FK", $"{table.FullName}.{extra.Name}", "FK_UNEXPECTED", null, extra.Name, SchemaDriftSeverity.Info, "UNKNOWN");
        }

        private static void CompareChecks(SchemaTableContract table, SchemaPhysicalSnapshot snapshot, DatabaseIdentity identity, string scope, int version, string hash, List<SchemaDriftItem> items)
        {
            SchemaCheckSnapshot[] actual = snapshot.Checks.Where(x => Eq(x.Schema, table.Schema) && Eq(x.Table, table.Name)).ToArray();
            foreach (SchemaCheckContract expected in table.CheckConstraints)
            {
                SchemaCheckSnapshot? found = actual.SingleOrDefault(x => Eq(x.Name, expected.Name)); string obj = $"{table.FullName}.{expected.Name}";
                if (found == null) { Add(items, identity, scope, version, hash, "CHECK", obj, "CHECK_MISSING", expected.Name, null, SchemaDriftSeverity.Error, "YES"); continue; }
                if (!SchemaDefinitionNormalizer.Same(found.Definition, expected.Definition)) Add(items, identity, scope, version, hash, "CHECK", obj, "CHECK_DEFINITION_MISMATCH", expected.Definition, found.Definition, SchemaDriftSeverity.Error, "UNKNOWN");
                if (found.IsDisabled || found.IsNotTrusted) Add(items, identity, scope, version, hash, "CHECK", obj, "CHECK_STATE_MISMATCH", "enabled/trusted", $"disabled={found.IsDisabled};untrusted={found.IsNotTrusted}", SchemaDriftSeverity.Error, "UNKNOWN");
            }
            foreach (SchemaCheckSnapshot extra in actual.Where(a => table.CheckConstraints.All(e => !Eq(e.Name, a.Name)))) Add(items, identity, scope, version, hash, "CHECK", $"{table.FullName}.{extra.Name}", "CHECK_UNEXPECTED", null, extra.Name, SchemaDriftSeverity.Info, "UNKNOWN");
        }

        private static SchemaDriftSeverity ColumnSeverity(SchemaColumnContract c) => Eq(c.Name, "idEmpresa") || Eq(c.Name, "id") ? SchemaDriftSeverity.Critical : SchemaDriftSeverity.Error;
        private static SchemaDriftSeverity IndexSeverity(SchemaIndexContract i, SchemaTableContract t) => i.IsUnique && !i.KeyColumns.Any(c => Eq(c.Name, "idEmpresa")) && t.Columns.Any(c => Eq(c.Name, "idEmpresa")) ? SchemaDriftSeverity.Critical : SchemaDriftSeverity.Error;
        private static void Add(List<SchemaDriftItem> items, DatabaseIdentity id, string scope, int? v, string? h, string ot, string on, string diff, string? exp, string? act, SchemaDriftSeverity sev, string repair, string? reason = null) => items.Add(Item(id, scope, v, h, ot, on, diff, exp, act, sev, repair, reason ?? diff));
        private static SchemaDriftItem Item(DatabaseIdentity id, string scope, int? v, string? h, string ot, string on, string diff, string? exp, string? act, SchemaDriftSeverity sev, string repair, string reason) => new(id.ToSanitizedString(), scope, v, h, ot, on, diff, exp, act, sev, repair, reason);
        private static SchemaDriftReport Build(DatabaseIdentity id, string scope, int? v, string? expectedHash, string? persistedHash, SchemaValidationGlobalResult result, IReadOnlyCollection<SchemaDriftItem> items, SchemaPhysicalSnapshot? snapshot = null) => new() { SanitizedIdentity = id.ToSanitizedString(), Scope = scope, DeclaredVersion = v, ExpectedManifestHash = expectedHash, PersistedManifestHash = persistedHash, GlobalResult = result, Items = items.OrderBy(i => i.ObjectType).ThenBy(i => i.ObjectName).ThenBy(i => i.DifferenceType).ToArray(), DetectedTables = snapshot?.Tables.Count ?? 0, DetectedColumns = snapshot?.Columns.Count ?? 0, DetectedIndexes = snapshot?.Indexes.Count(i => !i.IsUniqueConstraint) ?? 0, DetectedForeignKeys = snapshot?.ForeignKeys.Count ?? 0, DetectedChecks = snapshot?.Checks.Count ?? 0, ChangedState = false, ExecutedDdl = false };
        private static bool Eq(string? a, string? b) => string.Equals(a ?? string.Empty, b ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        private static string NT(string value) => value.Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
        private static bool Seq(IEnumerable<string> a, IEnumerable<string> b) => a.SequenceEqual(b, StringComparer.OrdinalIgnoreCase);
        private static bool Set(IEnumerable<string> a, IEnumerable<string> b) => new HashSet<string>(a, StringComparer.OrdinalIgnoreCase).SetEquals(b);
    }
}
