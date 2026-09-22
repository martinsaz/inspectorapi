using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace checklistWs.Services.Tenant
{
    public enum SchemaMigrationResolutionStatus
    {
        Ready,
        NoPendingMigrations,
        AlreadyApplied,
        PackageInvalid,
        ChainGap,
        ChainBranch,
        ChainCycle,
        VersionMismatch,
        BaselineMismatch,
        HistoryInconsistent,
        TargetVersionInvalid,
        RequiresReview
    }

    public enum SchemaMigrationExecutionStatus
    {
        Migrated,
        NoProvision,
        Failed,
        LockTimeout,
        RequiresReview,
        Recovered
    }

    public sealed record SchemaReleaseManifest(
        string Scope,
        string BaselineId,
        int BaselineVersion,
        int LatestSchemaVersion,
        string LatestManifestHash,
        IReadOnlyCollection<string> ApprovedMigrationIds);

    public sealed record SchemaMigrationDefinition(
        string MigrationId,
        string BaselineId,
        string Scope,
        int FromVersion,
        int ToVersion,
        int Order,
        IReadOnlyCollection<string> ObjectsAffected,
        IReadOnlyCollection<string> Preconditions,
        IReadOnlyCollection<string> DataPreconditions,
        string SqlHash,
        string TargetManifestHash,
        string TransactionMode,
        string Risk,
        bool AutoApplicable,
        TimeSpan Timeout,
        IReadOnlyCollection<string> Dependencies,
        string RecoveryPolicy,
        string UpSql,
        SchemaContract TargetContract);

    public sealed class SchemaMigrationPackage
    {
        public SchemaMigrationPackage(SchemaReleaseManifest release, IReadOnlyCollection<SchemaMigrationDefinition> migrations)
        {
            Release = release ?? throw new ArgumentNullException(nameof(release));
            Migrations = migrations ?? Array.Empty<SchemaMigrationDefinition>();
        }

        public SchemaReleaseManifest Release { get; }
        public IReadOnlyCollection<SchemaMigrationDefinition> Migrations { get; }
    }

    public sealed class SchemaMigrationResolution
    {
        public SchemaMigrationResolutionStatus Status { get; init; }
        public string ReasonCode { get; init; } = string.Empty;
        public IReadOnlyCollection<SchemaMigrationDefinition> PendingMigrations { get; init; } = Array.Empty<SchemaMigrationDefinition>();
        public IReadOnlyCollection<string> Evidence { get; init; } = Array.Empty<string>();
    }

    public sealed class SchemaMigrationExecutionResult
    {
        public SchemaMigrationExecutionStatus Status { get; init; }
        public string ReasonCode { get; init; } = string.Empty;
        public string SanitizedIdentity { get; init; } = string.Empty;
        public string Scope { get; init; } = string.Empty;
        public Guid? AttemptId { get; init; }
        public string? MigrationId { get; init; }
        public int? FromVersion { get; init; }
        public int? ToVersion { get; init; }
        public string? ManifestHash { get; init; }
        public IReadOnlyCollection<string> Evidence { get; init; } = Array.Empty<string>();
        public IReadOnlyCollection<string> Discrepancies { get; init; } = Array.Empty<string>();
    }

    public interface ISchemaMigrationPackageProvider
    {
        SchemaMigrationPackage GetPackage(string scope);
    }

    public interface ISchemaMigrationResolver
    {
        SchemaMigrationResolution GetPendingMigrations(
            DatabaseIdentity identity,
            string scope,
            int currentVersion,
            int? targetVersion,
            SchemaMigrationPackage package,
            IReadOnlyCollection<SchemaControlHistory> history);
    }

    public interface ISchemaMigrationSqlExecutor
    {
        Task ExecuteAsync(
            TenantDatabaseDescriptor descriptor,
            DatabaseIdentity identity,
            SchemaMigrationDefinition migration,
            Func<SchemaMigrationDefinition, CancellationToken, Task<SchemaContractValidationResult>> validateTargetAsync,
            CancellationToken cancellationToken = default);
    }

    public interface ISchemaMigrationRunner
    {
        Task<SchemaMigrationExecutionResult> MigrateToLatestAsync(
            TenantDatabaseDescriptor descriptor,
            DatabaseIdentity identity,
            string scope,
            CancellationToken cancellationToken = default);

        Task<SchemaMigrationExecutionResult> RecoverUncertainCommitAsync(
            TenantDatabaseDescriptor descriptor,
            DatabaseIdentity identity,
            string scope,
            string migrationId,
            Guid attemptId,
            CancellationToken cancellationToken = default);
    }

    public sealed class ProductosServiciosMigrationPackageProvider : ISchemaMigrationPackageProvider
    {
        private readonly ISchemaContractProvider _contractProvider;
        private readonly ISchemaManifestProvider _manifestProvider;

        public ProductosServiciosMigrationPackageProvider(ISchemaContractProvider contractProvider, ISchemaManifestProvider manifestProvider)
        {
            _contractProvider = contractProvider;
            _manifestProvider = manifestProvider;
        }

        public SchemaMigrationPackage GetPackage(string scope)
        {
            if (string.Equals(scope, DatabaseScopes.Recepcion, StringComparison.OrdinalIgnoreCase))
            {
                SchemaContract recepcion = _contractProvider.GetContract(DatabaseScopes.Recepcion, ProductosServiciosSchemaContractProvider.RecepcionLatestVersion);
                SchemaManifest manifest = _manifestProvider.CreateManifest(recepcion);
                return new SchemaMigrationPackage(
                    new SchemaReleaseManifest(
                        DatabaseScopes.Recepcion,
                        "REC-B20260921",
                        ProductosServiciosSchemaContractProvider.RecepcionLatestVersion,
                        ProductosServiciosSchemaContractProvider.RecepcionLatestVersion,
                        manifest.ManifestHash,
                        Array.Empty<string>()),
                    Array.Empty<SchemaMigrationDefinition>());
            }

            if (string.Equals(scope, DatabaseScopes.Inventario, StringComparison.OrdinalIgnoreCase))
            {
                SchemaContract inventario = _contractProvider.GetContract(DatabaseScopes.Inventario, ProductosServiciosSchemaContractProvider.InventarioLatestVersion);
                SchemaManifest manifest = _manifestProvider.CreateManifest(inventario);
                return new SchemaMigrationPackage(
                    new SchemaReleaseManifest(
                        DatabaseScopes.Inventario,
                        "INV-B20260921",
                        ProductosServiciosSchemaContractProvider.InventarioLatestVersion,
                        ProductosServiciosSchemaContractProvider.InventarioLatestVersion,
                        manifest.ManifestHash,
                        Array.Empty<string>()),
                    Array.Empty<SchemaMigrationDefinition>());
            }

            if (string.Equals(scope, DatabaseScopes.OrdenesCompra, StringComparison.OrdinalIgnoreCase))
            {
                SchemaContract ordenesCompra = _contractProvider.GetContract(DatabaseScopes.OrdenesCompra, ProductosServiciosSchemaContractProvider.OrdenesCompraLatestVersion);
                SchemaManifest manifest = _manifestProvider.CreateManifest(ordenesCompra);
                return new SchemaMigrationPackage(
                    new SchemaReleaseManifest(
                        DatabaseScopes.OrdenesCompra,
                        "OC-B20260921",
                        ProductosServiciosSchemaContractProvider.OrdenesCompraLatestVersion,
                        ProductosServiciosSchemaContractProvider.OrdenesCompraLatestVersion,
                        manifest.ManifestHash,
                        Array.Empty<string>()),
                    Array.Empty<SchemaMigrationDefinition>());
            }

            if (string.Equals(scope, DatabaseScopes.Proveedores, StringComparison.OrdinalIgnoreCase))
            {
                SchemaContract proveedores = _contractProvider.GetContract(DatabaseScopes.Proveedores, ProductosServiciosSchemaContractProvider.ProveedoresLatestVersion);
                SchemaManifest manifest = _manifestProvider.CreateManifest(proveedores);
                return new SchemaMigrationPackage(
                    new SchemaReleaseManifest(
                        DatabaseScopes.Proveedores,
                        "PROVEEDORES-B20260917",
                        ProductosServiciosSchemaContractProvider.ProveedoresLatestVersion,
                        ProductosServiciosSchemaContractProvider.ProveedoresLatestVersion,
                        manifest.ManifestHash,
                        Array.Empty<string>()),
	                    Array.Empty<SchemaMigrationDefinition>());
            }

            if (string.Equals(scope, DatabaseScopes.Sucursales, StringComparison.OrdinalIgnoreCase))
            {
                SchemaContract sucursalesV1 = _contractProvider.GetContract(DatabaseScopes.Sucursales, ProductosServiciosSchemaContractProvider.V1);
                SchemaContract sucursalesV2 = _contractProvider.GetContract(DatabaseScopes.Sucursales, ProductosServiciosSchemaContractProvider.SucursalesLatestVersion);
                SchemaManifest sucursalesV2Manifest = _manifestProvider.CreateManifest(sucursalesV2);
                string sucursalesMigrationSql = BuildSucursalesV1ToV2Sql();
                SchemaMigrationDefinition sucursalesV1ToV2 = new(
                    "SUC-M20260917-V1-V2-NOTAS-NVARCHAR-MAX",
                    "SUC-B20260917",
                    DatabaseScopes.Sucursales,
                    sucursalesV1.ContractVersion,
                    sucursalesV2.ContractVersion,
                    1,
                    new[]
                    {
                        "dbo.RazonesSociales.Notas",
                        "dbo.Zonas.Notas",
                        "dbo.SucursalesTipos.Notas",
                        "dbo.Sucursales.Notas"
                    },
                    BuildSucursalesV1ToV2Preconditions(),
                    new[]
                    {
                        "ALTER_COLUMN_TEXT_VARCHAR_TO_NVARCHAR_MAX_IS_NON_DESTRUCTIVE",
                        "DATA_PRESERVED_NO_DML",
                        "NULLABILITY_REMAINS_NULL"
                    },
                    SchemaMigrationHash.Sha256(sucursalesMigrationSql),
                    sucursalesV2Manifest.ManifestHash,
                    "SingleTransaction",
                    "Low",
                    true,
                    TimeSpan.FromMinutes(2),
                    Array.Empty<string>(),
                    "ReconcileAfterUncertainCommit",
                    sucursalesMigrationSql,
                    sucursalesV2);

                return new SchemaMigrationPackage(
                    new SchemaReleaseManifest(
                        DatabaseScopes.Sucursales,
                        "SUC-B20260917",
                        sucursalesV1.ContractVersion,
                        sucursalesV2.ContractVersion,
                        sucursalesV2Manifest.ManifestHash,
                        new[] { sucursalesV1ToV2.MigrationId }),
                    new[] { sucursalesV1ToV2 });
            }

            if (!string.Equals(scope, DatabaseScopes.ProductosServicios, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentOutOfRangeException(nameof(scope), scope, null);
            }

            SchemaContract v1 = _contractProvider.GetContract(DatabaseScopes.ProductosServicios, 1);
            SchemaContract v2 = _contractProvider.GetContract(DatabaseScopes.ProductosServicios, ProductosServiciosSchemaContractProvider.V2);
            SchemaManifest v2Manifest = _manifestProvider.CreateManifest(v2);
            string migrationSql = BuildV1ToV2Sql();
            SchemaMigrationDefinition v1ToV2 = new(
                "PS-M20260916-V1-V2-DESCRIPCIONES-NVARCHAR-MAX",
                "PS-B20260909",
                DatabaseScopes.ProductosServicios,
                1,
                2,
                1,
                new[]
                {
                    "dbo.ProductosServiciosCategorias.Descripcion",
                    "dbo.ProductosServiciosMarcas.Descripcion",
                    "dbo.ProductosServiciosColecciones.Descripcion"
                },
                BuildV1ToV2Preconditions(),
                new[]
                {
                    "ALTER_COLUMN_NVARCHAR_500_TO_MAX_IS_NON_DESTRUCTIVE",
                    "DATA_PRESERVED_NO_DML",
                    "NULLABILITY_REMAINS_NULL"
                },
                SchemaMigrationHash.Sha256(migrationSql),
                v2Manifest.ManifestHash,
                "SingleTransaction",
                "Low",
                true,
                TimeSpan.FromMinutes(2),
                Array.Empty<string>(),
                "ReconcileAfterUncertainCommit",
                migrationSql,
                v2);

            return new SchemaMigrationPackage(
                new SchemaReleaseManifest(
                    DatabaseScopes.ProductosServicios,
                    "PS-B20260909",
                    1,
                    2,
                    v2Manifest.ManifestHash,
                    new[] { v1ToV2.MigrationId }),
                new[] { v1ToV2 });
        }

        private static IReadOnlyCollection<string> BuildV1ToV2Preconditions()
        {
            return new[]
            {
                ColumnExistsPrecondition("ProductosServiciosCategorias"),
                ColumnExistsPrecondition("ProductosServiciosMarcas"),
                ColumnExistsPrecondition("ProductosServiciosColecciones"),
                ColumnIsNvarchar500OrMaxPrecondition("ProductosServiciosCategorias"),
                ColumnIsNvarchar500OrMaxPrecondition("ProductosServiciosMarcas"),
                ColumnIsNvarchar500OrMaxPrecondition("ProductosServiciosColecciones"),
                ColumnIsNullablePrecondition("ProductosServiciosCategorias"),
                ColumnIsNullablePrecondition("ProductosServiciosMarcas"),
                ColumnIsNullablePrecondition("ProductosServiciosColecciones")
            };
        }

        private static string BuildV1ToV2Sql()
        {
            return string.Join(Environment.NewLine + Environment.NewLine, new[]
            {
                AlterDescriptionToMax("ProductosServiciosCategorias"),
                AlterDescriptionToMax("ProductosServiciosMarcas"),
                AlterDescriptionToMax("ProductosServiciosColecciones")
            });
        }

        private static IReadOnlyCollection<string> BuildSucursalesV1ToV2Preconditions()
        {
            return new[]
            {
                ColumnExistsPrecondition("RazonesSociales", "Notas"),
                ColumnExistsPrecondition("Zonas", "Notas"),
                ColumnExistsPrecondition("SucursalesTipos", "Notas"),
                ColumnExistsPrecondition("Sucursales", "Notas"),
                ColumnIsTextVarcharOrNvarcharPrecondition("RazonesSociales", "Notas"),
                ColumnIsTextVarcharOrNvarcharPrecondition("Zonas", "Notas"),
                ColumnIsTextVarcharOrNvarcharPrecondition("SucursalesTipos", "Notas"),
                ColumnIsTextVarcharOrNvarcharPrecondition("Sucursales", "Notas"),
                ColumnIsNullablePrecondition("RazonesSociales", "Notas"),
                ColumnIsNullablePrecondition("Zonas", "Notas"),
                ColumnIsNullablePrecondition("SucursalesTipos", "Notas"),
                ColumnIsNullablePrecondition("Sucursales", "Notas")
            };
        }

        private static string BuildSucursalesV1ToV2Sql()
        {
            return string.Join(Environment.NewLine + Environment.NewLine, new[]
            {
                AlterColumnToNvarcharMax("RazonesSociales", "Notas"),
                AlterColumnToNvarcharMax("Zonas", "Notas"),
                AlterColumnToNvarcharMax("SucursalesTipos", "Notas"),
                AlterColumnToNvarcharMax("Sucursales", "Notas")
            });
        }

        private static string ColumnExistsPrecondition(string table)
        {
            return $@"SELECT CASE WHEN EXISTS (
    SELECT 1
    FROM sys.columns c
    INNER JOIN sys.tables t ON t.object_id = c.object_id
    INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE s.name = N'dbo'
      AND t.name = N'{table}'
      AND c.name = N'Descripcion'
) THEN 1 ELSE 0 END;";
        }

        private static string ColumnExistsPrecondition(string table, string column)
        {
            return $@"SELECT CASE WHEN EXISTS (
    SELECT 1
    FROM sys.columns c
    INNER JOIN sys.tables t ON t.object_id = c.object_id
    INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE s.name = N'dbo'
      AND t.name = N'{table}'
      AND c.name = N'{column}'
) THEN 1 ELSE 0 END;";
        }

        private static string ColumnIsNvarchar500OrMaxPrecondition(string table)
        {
            return $@"SELECT CASE WHEN EXISTS (
    SELECT 1
    FROM sys.columns c
    INNER JOIN sys.types ty ON ty.user_type_id = c.user_type_id
    INNER JOIN sys.tables t ON t.object_id = c.object_id
    INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE s.name = N'dbo'
      AND t.name = N'{table}'
      AND c.name = N'Descripcion'
      AND ty.name = N'nvarchar'
      AND c.max_length IN (1000, -1)
) THEN 1 ELSE 0 END;";
        }

        private static string ColumnIsNullablePrecondition(string table)
        {
            return $@"SELECT CASE WHEN EXISTS (
    SELECT 1
    FROM sys.columns c
    INNER JOIN sys.tables t ON t.object_id = c.object_id
    INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE s.name = N'dbo'
      AND t.name = N'{table}'
      AND c.name = N'Descripcion'
      AND c.is_nullable = 1
) THEN 1 ELSE 0 END;";
        }

        private static string ColumnIsNullablePrecondition(string table, string column)
        {
            return $@"SELECT CASE WHEN EXISTS (
    SELECT 1
    FROM sys.columns c
    INNER JOIN sys.tables t ON t.object_id = c.object_id
    INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE s.name = N'dbo'
      AND t.name = N'{table}'
      AND c.name = N'{column}'
      AND c.is_nullable = 1
) THEN 1 ELSE 0 END;";
        }

        private static string ColumnIsTextVarcharOrNvarcharPrecondition(string table, string column)
        {
            return $@"SELECT CASE WHEN EXISTS (
    SELECT 1
    FROM sys.columns c
    INNER JOIN sys.types ty ON ty.user_type_id = c.user_type_id
    INNER JOIN sys.tables t ON t.object_id = c.object_id
    INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE s.name = N'dbo'
      AND t.name = N'{table}'
      AND c.name = N'{column}'
      AND ty.name IN (N'text', N'varchar', N'nvarchar')
) THEN 1 ELSE 0 END;";
        }

        private static string AlterDescriptionToMax(string table)
        {
            return $@"IF EXISTS (
    SELECT 1
    FROM sys.columns c
    INNER JOIN sys.types ty ON ty.user_type_id = c.user_type_id
    INNER JOIN sys.tables t ON t.object_id = c.object_id
    INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE s.name = N'dbo'
      AND t.name = N'{table}'
      AND c.name = N'Descripcion'
      AND ty.name = N'nvarchar'
      AND c.max_length <> -1
)
BEGIN
    ALTER TABLE [dbo].[{table}] ALTER COLUMN [Descripcion] NVARCHAR(MAX) NULL;
END;";
        }

        private static string AlterColumnToNvarcharMax(string table, string column)
        {
            return $@"IF EXISTS (
    SELECT 1
    FROM sys.columns c
    INNER JOIN sys.types ty ON ty.user_type_id = c.user_type_id
    INNER JOIN sys.tables t ON t.object_id = c.object_id
    INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE s.name = N'dbo'
      AND t.name = N'{table}'
      AND c.name = N'{column}'
      AND (ty.name <> N'nvarchar' OR c.max_length <> -1)
)
BEGIN
    ALTER TABLE [dbo].[{table}] ALTER COLUMN [{column}] NVARCHAR(MAX) NULL;
END;";
        }
    }

    public static class SchemaMigrationHash
    {
        public static string Sha256(string value)
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(value ?? string.Empty));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }

    public static class SchemaMigrationHistoryDetails
    {
        public static string Build(SchemaMigrationDefinition migration)
        {
            return $"MigrationId={migration.MigrationId}; SqlHash={migration.SqlHash}; TargetManifestHash={migration.TargetManifestHash}";
        }

        public static string? Get(string? details, string key)
        {
            if (string.IsNullOrWhiteSpace(details))
            {
                return null;
            }

            foreach (string part in details.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                int separator = part.IndexOf('=', StringComparison.Ordinal);
                if (separator <= 0)
                {
                    continue;
                }

                if (string.Equals(part[..separator].Trim(), key, StringComparison.OrdinalIgnoreCase))
                {
                    return part[(separator + 1)..].Trim();
                }
            }

            return null;
        }
    }

    internal static class SchemaMigrationSanitizer
    {
        public static string? Sanitize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            string sanitized = Regex.Replace(value, "(Password|Pwd|User Id|UID|Token|Secret)\\s*=\\s*[^;\\s]+", "$1=***", RegexOptions.IgnoreCase);
            return sanitized.Length > 2000 ? sanitized[..2000] : sanitized;
        }
    }
}
