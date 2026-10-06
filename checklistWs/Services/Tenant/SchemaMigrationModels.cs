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
        SchemaContract TargetContract,
        SchemaContract? SourceContract = null,
        IReadOnlyCollection<string>? SupersededTargetManifestHashes = null);

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
            IReadOnlyCollection<SchemaControlHistory> history,
            string? currentManifestHash = null);
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
            if (string.Equals(scope, DatabaseScopes.Cotizaciones, StringComparison.OrdinalIgnoreCase))
            {
                SchemaContract cotizacionesV1 = _contractProvider.GetContract(DatabaseScopes.Cotizaciones, ProductosServiciosSchemaContractProvider.V1);
                SchemaContract cotizacionesV2 = _contractProvider.GetContract(DatabaseScopes.Cotizaciones, ProductosServiciosSchemaContractProvider.CotizacionesLatestVersion);
                SchemaContract supersededCotizacionesV2 = CotizacionesSchemaContractFactory.GetSupersededV2Contract();
                SchemaManifest manifest = _manifestProvider.CreateManifest(cotizacionesV2);
                string cotizacionesMigrationSql = CotizacionesSchemaContractFactory.BuildV1ToV2Sql();
                SchemaMigrationDefinition migration = new(
                    "COT-M20260930-V1-V2-LP08-SNAPSHOT",
                    ProductosServiciosHistoricalBaselineAdopter.CotizacionesBaselineId,
                    DatabaseScopes.Cotizaciones,
                    cotizacionesV1.ContractVersion,
                    cotizacionesV2.ContractVersion,
                    1,
                    new[]
                    {
                        "dbo.Cotizaciones.idListaPrecio",
                        "dbo.Cotizaciones.ListaPrecioNivel",
                        "dbo.Cotizaciones.idCotizacionOrigen",
                        "dbo.CotizacionesPartidas.LP08Snapshot",
                        "dbo.CotizacionesHistorial"
                    },
                    CotizacionesSchemaContractFactory.BuildV1ToV2Preconditions(),
                    new[]
                    {
                        "NO_DML_BUSINESS_ROWS",
                        "NO_COMMERCIAL_BACKFILL",
                        "PRESERVE_PRE_LP08_NULLS",
                        "PRESERVE_PRECIO_UNITARIO_AS_PRECIO_APLICADO",
                        "PRESERVE_PARTIDA_ACTIVO_FOR_LOGICAL_DELETE"
                    },
                    SchemaMigrationHash.Sha256(cotizacionesMigrationSql),
                    manifest.ManifestHash,
                    "SingleTransaction",
                    "Low",
                    true,
                    TimeSpan.FromMinutes(2),
                    new[] { _manifestProvider.CreateManifest(cotizacionesV1).ManifestHash },
                    "ReconcileAfterUncertainCommit",
                    cotizacionesMigrationSql,
                    cotizacionesV2,
                    cotizacionesV1,
                    new[] { CotizacionesSchemaContractFactory.SupersededV2ManifestHash });

                string reconciliationSql = CotizacionesSchemaContractFactory.BuildV2HistoricalObjectsReconciliationSql();
                SchemaMigrationDefinition reconciliation = new(
                    "COT-M20261001-V2-RECONCILE-HISTORICAL-OBJECTS",
                    ProductosServiciosHistoricalBaselineAdopter.CotizacionesBaselineId,
                    DatabaseScopes.Cotizaciones,
                    cotizacionesV2.ContractVersion,
                    cotizacionesV2.ContractVersion,
                    2,
                    new[]
                    {
                        "dbo.Cotizaciones.IX_Cotizaciones_Empresa_Cliente",
                        "dbo.CotizacionesPartidas.FK_CotizacionesPartidas_Cotizaciones"
                    },
                    CotizacionesSchemaContractFactory.BuildV2HistoricalObjectsReconciliationPreconditions(),
                    new[]
                    {
                        "NO_DML_BUSINESS_ROWS",
                        "PRESERVE_V2_COMMERCIAL_VERSION",
                        "PRESERVE_EXISTING_V2_OBJECTS"
                    },
                    SchemaMigrationHash.Sha256(reconciliationSql),
                    manifest.ManifestHash,
                    "SingleTransaction",
                    "Low",
                    true,
                    TimeSpan.FromMinutes(2),
                    new[] { CotizacionesSchemaContractFactory.SupersededV2ManifestHash },
                    "ReconcileAfterUncertainCommit",
                    reconciliationSql,
                    cotizacionesV2,
                    supersededCotizacionesV2);

                return new SchemaMigrationPackage(
                    new SchemaReleaseManifest(
                        DatabaseScopes.Cotizaciones,
                        ProductosServiciosHistoricalBaselineAdopter.CotizacionesBaselineId,
                        cotizacionesV1.ContractVersion,
                        cotizacionesV2.ContractVersion,
                        manifest.ManifestHash,
                        new[] { migration.MigrationId, reconciliation.MigrationId }),
                    new[] { migration, reconciliation });
            }

            if (string.Equals(scope, DatabaseScopes.ListaPrecios, StringComparison.OrdinalIgnoreCase))
            {
                SchemaContract listaPreciosV1 = _contractProvider.GetContract(DatabaseScopes.ListaPrecios, ProductosServiciosSchemaContractProvider.V1);
                SchemaContract listaPreciosV2 = _contractProvider.GetContract(DatabaseScopes.ListaPrecios, ProductosServiciosSchemaContractProvider.ListaPreciosLatestVersion);
                SchemaManifest manifest = _manifestProvider.CreateManifest(listaPreciosV2);
                string listaPreciosMigrationSql = BuildListaPreciosV1ToV2Sql();
                SchemaMigrationDefinition lpV1ToV2 = new(
                    "LP-M20260929-V1-V2-COMERCIAL",
                    "LP-B20260928",
                    DatabaseScopes.ListaPrecios,
                    listaPreciosV1.ContractVersion,
                    listaPreciosV2.ContractVersion,
                    1,
                    new[]
                    {
                        "dbo.ListaPreciosDetalle.DescuentoPct",
                        "dbo.ListaPreciosDetalle.RedondeoModo",
                        "dbo.ListaPreciosDetalle.VigenciaInicio",
                        "dbo.ListaPreciosDetalle.VigenciaFin",
                        "dbo.ListaPreciosPromociones",
                        "dbo.ListaPreciosHistorial"
                    },
                    BuildListaPreciosV1ToV2Preconditions(),
                    new[]
                    {
                        "PRESERVE_V1_PRECIO_AND_ZERO",
                        "NO_PRICE_FINAL_SOURCE_OF_TRUTH",
                        "NO_LEGACY_P1_D1_COLUMNS",
                        "NO_MONEDERO_FUNCTIONAL_SCHEMA"
                    },
                    SchemaMigrationHash.Sha256(listaPreciosMigrationSql),
                    manifest.ManifestHash,
                    "SingleTransaction",
                    "Low",
                    true,
                    TimeSpan.FromMinutes(2),
                    new[] { _manifestProvider.CreateManifest(listaPreciosV1).ManifestHash },
                    "ReconcileAfterUncertainCommit",
                    listaPreciosMigrationSql,
                    listaPreciosV2,
                    listaPreciosV1);

                return new SchemaMigrationPackage(
                    new SchemaReleaseManifest(
                        DatabaseScopes.ListaPrecios,
                        "LP-B20260928",
                        listaPreciosV1.ContractVersion,
                        ProductosServiciosSchemaContractProvider.ListaPreciosLatestVersion,
                        manifest.ManifestHash,
                        new[] { lpV1ToV2.MigrationId }),
                    new[] { lpV1ToV2 });
            }

            if (string.Equals(scope, DatabaseScopes.Curvas, StringComparison.OrdinalIgnoreCase))
            {
                SchemaContract curvas = _contractProvider.GetContract(DatabaseScopes.Curvas, ProductosServiciosSchemaContractProvider.CurvasLatestVersion);
                SchemaManifest manifest = _manifestProvider.CreateManifest(curvas);
                return new SchemaMigrationPackage(
                    new SchemaReleaseManifest(
                        DatabaseScopes.Curvas,
                        "CUR-B20260923",
                        ProductosServiciosSchemaContractProvider.CurvasLatestVersion,
                        ProductosServiciosSchemaContractProvider.CurvasLatestVersion,
                        manifest.ManifestHash,
                        Array.Empty<string>()),
                    Array.Empty<SchemaMigrationDefinition>());
            }

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
                SchemaContract ordenesCompraV1 = _contractProvider.GetContract(DatabaseScopes.OrdenesCompra, ProductosServiciosSchemaContractProvider.V1);
                SchemaContract ordenesCompraV2 = _contractProvider.GetContract(DatabaseScopes.OrdenesCompra, ProductosServiciosSchemaContractProvider.OrdenesCompraLatestVersion);
                SchemaManifest manifest = _manifestProvider.CreateManifest(ordenesCompraV2);
                string ordenesCompraMigrationSql = BuildOrdenesCompraV1ToV2Sql();
                SchemaMigrationDefinition ocV1ToV2 = new(
                    "OC-M20260923-V1-V2-PRESENTACIONCOMPRA-CANTIDAD-BASE",
                    "OC-B20260921",
                    DatabaseScopes.OrdenesCompra,
                    ordenesCompraV1.ContractVersion,
                    ordenesCompraV2.ContractVersion,
                    1,
                    new[] { "dbo.OrdenesCompraPresentacionesCompra.PermiteCantidadBase" },
                    new[]
                    {
                        "TABLE_EXISTS:dbo.OrdenesCompraPresentacionesCompra",
                        "COLUMN_MISSING_OR_COMPATIBLE:PermiteCantidadBase",
                        "NO_DML_BUSINESS_ROWS"
                    },
                    new[]
                    {
                        "ADD_BIT_NOT_NULL_DEFAULT_ZERO",
                        "DEFAULT_CLOSED_MULTIPLES_FOR_EXISTING_PRESENTATIONS",
                        "NO_PRESENTACIONESVENTA"
                    },
                    SchemaMigrationHash.Sha256(ordenesCompraMigrationSql),
                    manifest.ManifestHash,
                    "SingleTransaction",
                    "Low",
                    true,
                    TimeSpan.FromMinutes(2),
                    Array.Empty<string>(),
                    "ReconcileAfterUncertainCommit",
                    ordenesCompraMigrationSql,
                    ordenesCompraV2);

                return new SchemaMigrationPackage(
                    new SchemaReleaseManifest(
                        DatabaseScopes.OrdenesCompra,
                        "OC-B20260921",
                        ordenesCompraV1.ContractVersion,
                        ProductosServiciosSchemaContractProvider.OrdenesCompraLatestVersion,
                        manifest.ManifestHash,
                        new[] { ocV1ToV2.MigrationId }),
                    new[] { ocV1ToV2 });
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

            return ProductosServiciosSchemaContractProvider.LatestVersion == ProductosServiciosSchemaContractProvider.V3
                ? GetPreparedProductosServiciosV3Package()
                : BuildProductosServiciosV2Package();
        }

        private SchemaMigrationPackage BuildProductosServiciosV2Package()
        {
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

        /// <summary>
        /// Paquete oficial V3 de ProductosServicios activado después de certificar
        /// CHECKAPPERP y UMBRELLA mediante LP-QA05S3.
        /// </summary>
        public SchemaMigrationPackage GetPreparedProductosServiciosV3Package()
        {
            SchemaMigrationPackage activeV2Package = BuildProductosServiciosV2Package();
            SchemaContract source = _contractProvider.GetContract(
                DatabaseScopes.ProductosServicios,
                ProductosServiciosComercialContractProposal.SourceVersion);
            SchemaContract target = _contractProvider.GetContract(
                DatabaseScopes.ProductosServicios,
                ProductosServiciosComercialContractProposal.TargetVersion);
            SchemaManifest sourceManifest = _manifestProvider.CreateManifest(source);
            SchemaManifest targetManifest = _manifestProvider.CreateManifest(target);
            string sql = ProductosServiciosComercialContractProposal.BuildV2ToV3Sql();

            SchemaMigrationDefinition v2ToV3 = new(
                ProductosServiciosComercialContractProposal.MigrationId,
                activeV2Package.Release.BaselineId,
                DatabaseScopes.ProductosServicios,
                source.ContractVersion,
                target.ContractVersion,
                2,
                new[]
                {
                    "dbo.ProductosServiciosIdentidadComercial",
                    "dbo.ProductosServiciosIdentidadComercialHistorial"
                },
                BuildProductosServiciosV2ToV3ExecutablePreconditions(sourceManifest.ManifestHash),
                ProductosServiciosComercialContractProposal.BuildV2ToV3DataPreconditions(),
                SchemaMigrationHash.Sha256(sql),
                targetManifest.ManifestHash,
                "SingleTransaction",
                "Medium",
                true,
                TimeSpan.FromMinutes(2),
                new[] { sourceManifest.ManifestHash },
                "ReconcileAfterUncertainCommit",
                sql,
                target,
                source,
                new[] { "086c8e7fe0aced219dda9e3937ac0ddc2299c27b202ec2827c5eeda3916b732d" });

            return new SchemaMigrationPackage(
                new SchemaReleaseManifest(
                    DatabaseScopes.ProductosServicios,
                    activeV2Package.Release.BaselineId,
                    activeV2Package.Release.BaselineVersion,
                    target.ContractVersion,
                    targetManifest.ManifestHash,
                    activeV2Package.Release.ApprovedMigrationIds.Concat(new[] { v2ToV3.MigrationId }).ToArray()),
                activeV2Package.Migrations.Concat(new[] { v2ToV3 }).ToArray());
        }

        private static IReadOnlyCollection<string> BuildProductosServiciosV2ToV3ExecutablePreconditions(string sourceManifestHash) => new[]
        {
            $@"SELECT CASE WHEN EXISTS (
    SELECT 1
    FROM dbo.CheckAppSchemaState
    WHERE Scope = N'{DatabaseScopes.ProductosServicios}'
      AND CurrentVersion = {ProductosServiciosComercialContractProposal.SourceVersion}
      AND ManifestHash = N'{sourceManifestHash}'
) THEN 1 ELSE 0 END;",
            @"SELECT CASE WHEN
    OBJECT_ID(N'dbo.ProductosServicios', N'U') IS NOT NULL
    AND OBJECT_ID(N'dbo.ProductosServiciosVariantes', N'U') IS NOT NULL
    AND OBJECT_ID(N'dbo.ProductosServiciosPresentacionesVenta', N'U') IS NOT NULL
THEN 1 ELSE 0 END;",
            @"SELECT CASE WHEN
    OBJECT_ID(N'dbo.ProductosServiciosIdentidadComercial', N'U') IS NULL
    AND OBJECT_ID(N'dbo.ProductosServiciosIdentidadComercialHistorial', N'U') IS NULL
THEN 1 ELSE 0 END;"
        };

        private static string BuildOrdenesCompraV1ToV2Sql() => @"
IF COL_LENGTH('dbo.OrdenesCompraPresentacionesCompra', 'PermiteCantidadBase') IS NULL
BEGIN
    ALTER TABLE dbo.OrdenesCompraPresentacionesCompra
        ADD PermiteCantidadBase bit NOT NULL
            CONSTRAINT DF_OrdenesCompraPresentacionesCompra_PermiteCantidadBase DEFAULT ((0));
END";

        private static IReadOnlyCollection<string> BuildListaPreciosV1ToV2Preconditions()
        {
            return new[]
            {
                TableExistsPrecondition("ListaPreciosListas"),
                TableExistsPrecondition("ListaPreciosDetalle"),
                ColumnExistsPrecondition("ListaPreciosDetalle", "Precio"),
                ColumnMissingOrCompatiblePrecondition("ListaPreciosDetalle", "DescuentoPct"),
                ColumnMissingOrCompatiblePrecondition("ListaPreciosDetalle", "RedondeoModo"),
                ColumnMissingOrCompatiblePrecondition("ListaPreciosDetalle", "VigenciaInicio"),
                ColumnMissingOrCompatiblePrecondition("ListaPreciosDetalle", "VigenciaFin"),
                TableMissingOrCompatiblePrecondition("ListaPreciosPromociones"),
                TableMissingOrCompatiblePrecondition("ListaPreciosHistorial")
            };
        }

        private static string BuildListaPreciosV1ToV2Sql() => @"
IF COL_LENGTH('dbo.ListaPreciosDetalle', 'DescuentoPct') IS NULL
BEGIN
    ALTER TABLE dbo.ListaPreciosDetalle ADD DescuentoPct decimal(5,2) NULL;
END;

IF COL_LENGTH('dbo.ListaPreciosDetalle', 'RedondeoModo') IS NULL
BEGIN
    ALTER TABLE dbo.ListaPreciosDetalle ADD RedondeoModo tinyint NOT NULL
        CONSTRAINT DF_ListaPreciosDetalle_RedondeoModo DEFAULT ((0));
END;

IF COL_LENGTH('dbo.ListaPreciosDetalle', 'VigenciaInicio') IS NULL
BEGIN
    ALTER TABLE dbo.ListaPreciosDetalle ADD VigenciaInicio date NULL;
END;

IF COL_LENGTH('dbo.ListaPreciosDetalle', 'VigenciaFin') IS NULL
BEGIN
    ALTER TABLE dbo.ListaPreciosDetalle ADD VigenciaFin date NULL;
END;

IF OBJECT_ID(N'dbo.CK_ListaPreciosDetalle_DescuentoPct', N'C') IS NULL
BEGIN
    EXEC(N'ALTER TABLE dbo.ListaPreciosDetalle ADD CONSTRAINT CK_ListaPreciosDetalle_DescuentoPct CHECK (DescuentoPct IS NULL OR (DescuentoPct >= 0 AND DescuentoPct <= 100));');
END;

IF OBJECT_ID(N'dbo.CK_ListaPreciosDetalle_RedondeoModo', N'C') IS NULL
BEGIN
    EXEC(N'ALTER TABLE dbo.ListaPreciosDetalle ADD CONSTRAINT CK_ListaPreciosDetalle_RedondeoModo CHECK (RedondeoModo IN (0, 1, 2));');
END;

IF OBJECT_ID(N'dbo.CK_ListaPreciosDetalle_Vigencia', N'C') IS NULL
BEGIN
    EXEC(N'ALTER TABLE dbo.ListaPreciosDetalle ADD CONSTRAINT CK_ListaPreciosDetalle_Vigencia CHECK (VigenciaInicio IS NULL OR VigenciaFin IS NULL OR VigenciaInicio <= VigenciaFin);');
END;

IF OBJECT_ID(N'dbo.ListaPreciosPromociones', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ListaPreciosPromociones
    (
        id uniqueidentifier NOT NULL CONSTRAINT DF_ListaPreciosPromociones_id DEFAULT (NEWID()),
        idEmpresa uniqueidentifier NOT NULL,
        identityKey uniqueidentifier NOT NULL CONSTRAINT DF_ListaPreciosPromociones_identityKey DEFAULT (NEWID()),
        idListaPrecio uniqueidentifier NOT NULL,
        TipoPromocion tinyint NOT NULL,
        TipoIdentidad tinyint NOT NULL,
        idProductoServicio uniqueidentifier NOT NULL,
        TipoProductoServicio tinyint NOT NULL,
        idVariante uniqueidentifier NULL,
        idPresentacionVenta uniqueidentifier NULL,
        DescuentoSegundoPct decimal(5,2) NULL,
        VigenciaInicio date NULL,
        VigenciaFin date NULL,
        Activo bit NOT NULL CONSTRAINT DF_ListaPreciosPromociones_Activo DEFAULT ((1)),
        FechaCreacion datetime2(0) NOT NULL CONSTRAINT DF_ListaPreciosPromociones_FechaCreacion DEFAULT (SYSUTCDATETIME()),
        FechaActualizacion datetime2(0) NOT NULL CONSTRAINT DF_ListaPreciosPromociones_FechaActualizacion DEFAULT (SYSUTCDATETIME()),
        FechaArchivado datetime2(0) NULL,
        idUsuarioCreacion uniqueidentifier NULL,
        idUsuarioActualizacion uniqueidentifier NULL,
        idUsuarioArchivado uniqueidentifier NULL,
        CONSTRAINT PK_ListaPreciosPromociones PRIMARY KEY CLUSTERED (id),
        CONSTRAINT CK_ListaPreciosPromociones_TipoPromocion CHECK (TipoPromocion IN (1, 2, 3)),
        CONSTRAINT CK_ListaPreciosPromociones_TipoIdentidad CHECK (TipoIdentidad IN (1, 2, 3, 4)),
        CONSTRAINT CK_ListaPreciosPromociones_TipoProductoServicio CHECK (TipoProductoServicio IN (1, 2)),
        CONSTRAINT CK_ListaPreciosPromociones_Identidad CHECK ((TipoIdentidad = 1 AND TipoProductoServicio = 1 AND idVariante IS NULL AND idPresentacionVenta IS NULL) OR (TipoIdentidad = 2 AND TipoProductoServicio = 2 AND idVariante IS NULL AND idPresentacionVenta IS NULL) OR (TipoIdentidad = 3 AND TipoProductoServicio = 1 AND idVariante IS NOT NULL AND idPresentacionVenta IS NULL) OR (TipoIdentidad = 4 AND TipoProductoServicio = 1 AND idVariante IS NULL AND idPresentacionVenta IS NOT NULL)),
        CONSTRAINT CK_ListaPreciosPromociones_DescuentoSegundo CHECK ((TipoPromocion = 3 AND DescuentoSegundoPct IS NOT NULL AND DescuentoSegundoPct >= 0 AND DescuentoSegundoPct <= 100) OR (TipoPromocion IN (1, 2) AND DescuentoSegundoPct IS NULL)),
        CONSTRAINT CK_ListaPreciosPromociones_Vigencia CHECK (VigenciaInicio IS NULL OR VigenciaFin IS NULL OR VigenciaInicio <= VigenciaFin),
        CONSTRAINT CK_ListaPreciosPromociones_Archivado CHECK ((Activo = 1 AND FechaArchivado IS NULL AND idUsuarioArchivado IS NULL) OR (Activo = 0 AND FechaArchivado IS NOT NULL))
    );
END;

IF OBJECT_ID(N'dbo.ListaPreciosHistorial', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ListaPreciosHistorial
    (
        id uniqueidentifier NOT NULL CONSTRAINT DF_ListaPreciosHistorial_id DEFAULT (NEWID()),
        idEmpresa uniqueidentifier NOT NULL,
        idListaPrecio uniqueidentifier NULL,
        TipoIdentidad tinyint NULL,
        idProductoServicio uniqueidentifier NULL,
        idVariante uniqueidentifier NULL,
        idPresentacionVenta uniqueidentifier NULL,
        Campo nvarchar(60) NOT NULL,
        Operacion nvarchar(40) NOT NULL,
        ValorAnterior nvarchar(4000) NULL,
        ValorNuevo nvarchar(4000) NULL,
        idUsuario uniqueidentifier NULL,
        Usuario nvarchar(256) NULL,
        Origen nvarchar(40) NOT NULL,
        CorrelationId uniqueidentifier NOT NULL CONSTRAINT DF_ListaPreciosHistorial_CorrelationId DEFAULT (NEWID()),
        Motivo nvarchar(500) NULL,
        FechaUtc datetime2(0) NOT NULL CONSTRAINT DF_ListaPreciosHistorial_FechaUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_ListaPreciosHistorial PRIMARY KEY CLUSTERED (id),
        CONSTRAINT CK_ListaPreciosHistorial_TipoIdentidad CHECK (TipoIdentidad IS NULL OR TipoIdentidad IN (1, 2, 3, 4)),
        CONSTRAINT CK_ListaPreciosHistorial_Origen CHECK (Origen IN (N'INDIVIDUAL', N'MASIVO', N'COPIA_LISTA', N'DESCUENTO_MARCA', N'PROMOCION', N'SISTEMA'))
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_ListaPreciosPromociones_Empresa_Id' AND object_id = OBJECT_ID(N'dbo.ListaPreciosPromociones'))
    EXEC(N'CREATE UNIQUE NONCLUSTERED INDEX UX_ListaPreciosPromociones_Empresa_Id ON dbo.ListaPreciosPromociones (idEmpresa, id);');
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_ListaPreciosPromociones_Empresa_Lista_Tipo_Identidad_Activo' AND object_id = OBJECT_ID(N'dbo.ListaPreciosPromociones'))
    EXEC(N'CREATE UNIQUE NONCLUSTERED INDEX UX_ListaPreciosPromociones_Empresa_Lista_Tipo_Identidad_Activo ON dbo.ListaPreciosPromociones (idEmpresa, idListaPrecio, TipoPromocion, TipoIdentidad, idProductoServicio, idVariante, idPresentacionVenta) WHERE Activo = 1 AND FechaArchivado IS NULL;');
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ListaPreciosPromociones_Empresa_Lista' AND object_id = OBJECT_ID(N'dbo.ListaPreciosPromociones'))
    EXEC(N'CREATE NONCLUSTERED INDEX IX_ListaPreciosPromociones_Empresa_Lista ON dbo.ListaPreciosPromociones (idEmpresa, idListaPrecio);');
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ListaPreciosPromociones_Empresa_Producto' AND object_id = OBJECT_ID(N'dbo.ListaPreciosPromociones'))
    EXEC(N'CREATE NONCLUSTERED INDEX IX_ListaPreciosPromociones_Empresa_Producto ON dbo.ListaPreciosPromociones (idEmpresa, idProductoServicio);');
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ListaPreciosPromociones_Empresa_Activo' AND object_id = OBJECT_ID(N'dbo.ListaPreciosPromociones'))
    EXEC(N'CREATE NONCLUSTERED INDEX IX_ListaPreciosPromociones_Empresa_Activo ON dbo.ListaPreciosPromociones (idEmpresa, Activo);');

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_ListaPreciosHistorial_Empresa_Id' AND object_id = OBJECT_ID(N'dbo.ListaPreciosHistorial'))
    EXEC(N'CREATE UNIQUE NONCLUSTERED INDEX UX_ListaPreciosHistorial_Empresa_Id ON dbo.ListaPreciosHistorial (idEmpresa, id);');
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ListaPreciosHistorial_Empresa_Fecha' AND object_id = OBJECT_ID(N'dbo.ListaPreciosHistorial'))
    EXEC(N'CREATE NONCLUSTERED INDEX IX_ListaPreciosHistorial_Empresa_Fecha ON dbo.ListaPreciosHistorial (idEmpresa, FechaUtc);');
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ListaPreciosHistorial_Empresa_Correlation' AND object_id = OBJECT_ID(N'dbo.ListaPreciosHistorial'))
    EXEC(N'CREATE NONCLUSTERED INDEX IX_ListaPreciosHistorial_Empresa_Correlation ON dbo.ListaPreciosHistorial (idEmpresa, CorrelationId);');

IF OBJECT_ID(N'dbo.FK_ListaPreciosPromociones_Listas_EmpresaId', N'F') IS NULL
    EXEC(N'ALTER TABLE dbo.ListaPreciosPromociones ADD CONSTRAINT FK_ListaPreciosPromociones_Listas_EmpresaId FOREIGN KEY (idEmpresa, idListaPrecio) REFERENCES dbo.ListaPreciosListas (idEmpresa, id);');
IF OBJECT_ID(N'dbo.FK_ListaPreciosPromociones_ProductosServicios_EmpresaId', N'F') IS NULL
    EXEC(N'ALTER TABLE dbo.ListaPreciosPromociones ADD CONSTRAINT FK_ListaPreciosPromociones_ProductosServicios_EmpresaId FOREIGN KEY (idEmpresa, idProductoServicio) REFERENCES dbo.ProductosServicios (idEmpresa, id);');
IF OBJECT_ID(N'dbo.FK_ListaPreciosPromociones_Variantes_EmpresaId', N'F') IS NULL
    EXEC(N'ALTER TABLE dbo.ListaPreciosPromociones ADD CONSTRAINT FK_ListaPreciosPromociones_Variantes_EmpresaId FOREIGN KEY (idEmpresa, idVariante) REFERENCES dbo.ProductosServiciosVariantes (idEmpresa, id);');
IF OBJECT_ID(N'dbo.FK_ListaPreciosPromociones_PresentacionesVenta_EmpresaId', N'F') IS NULL
    EXEC(N'ALTER TABLE dbo.ListaPreciosPromociones ADD CONSTRAINT FK_ListaPreciosPromociones_PresentacionesVenta_EmpresaId FOREIGN KEY (idEmpresa, idPresentacionVenta) REFERENCES dbo.ProductosServiciosPresentacionesVenta (idEmpresa, id);');
IF OBJECT_ID(N'dbo.FK_ListaPreciosHistorial_Listas_EmpresaId', N'F') IS NULL
    EXEC(N'ALTER TABLE dbo.ListaPreciosHistorial ADD CONSTRAINT FK_ListaPreciosHistorial_Listas_EmpresaId FOREIGN KEY (idEmpresa, idListaPrecio) REFERENCES dbo.ListaPreciosListas (idEmpresa, id);');";

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

        private static string TableExistsPrecondition(string table)
        {
            return $@"SELECT CASE WHEN EXISTS (
    SELECT 1
    FROM sys.tables t
    INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE s.name = N'dbo'
      AND t.name = N'{table}'
) THEN 1 ELSE 0 END;";
        }

        private static string TableMissingOrCompatiblePrecondition(string table)
        {
            return $@"SELECT CASE WHEN NOT EXISTS (
    SELECT 1
    FROM sys.tables t
    INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE s.name = N'dbo'
      AND t.name = N'{table}'
) THEN 1 ELSE 0 END;";
        }

        private static string ColumnMissingOrCompatiblePrecondition(string table, string column)
        {
            return $@"SELECT CASE WHEN NOT EXISTS (
    SELECT 1
    FROM sys.columns c
    INNER JOIN sys.tables t ON t.object_id = c.object_id
    INNER JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE s.name = N'dbo'
      AND t.name = N'{table}'
      AND c.name = N'{column}'
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
