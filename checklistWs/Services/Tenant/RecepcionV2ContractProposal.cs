namespace checklistWs.Services.Tenant
{
    /// <summary>
    /// OC-QA03S1: contrato local Recepcion V2 coordinado con OrdenesCompra V5.
    /// No se registra en el provider ni en el package activo mientras no exista autorización pre-ejecución.
    /// </summary>
    public static class RecepcionV2ContractProposal
    {
        public const int SourceVersion = 1;
        public const int TargetVersion = 2;
        public const string MigrationId = "REC-M20261006-V1-V2-SUCURSAL-PARTIDA-OCV5";
        public const string ReviewStatus = "CONTRATO_CERRADO_PO_OC_QA03S1_PENDIENTE_PRE_EJECUCION";

        public static SchemaContractProposal Create(
            ISchemaContractProvider contractProvider,
            ISchemaManifestProvider manifestProvider)
        {
            ArgumentNullException.ThrowIfNull(contractProvider);
            ArgumentNullException.ThrowIfNull(manifestProvider);

            SchemaContract source = contractProvider.GetContract(DatabaseScopes.Recepcion, SourceVersion);
            SchemaContract target = CreateTargetContract(source);
            SchemaManifest sourceManifest = manifestProvider.CreateManifest(source);
            SchemaManifest targetManifest = manifestProvider.CreateManifest(target);

            return new SchemaContractProposal(
                MigrationId,
                ReviewStatus,
                source,
                target,
                sourceManifest.ManifestHash,
                targetManifest.ManifestHash,
                BuildPreconditions(),
                BuildDataPreconditions(),
                "SingleTransaction",
                "ROLLBACK_BEFORE_COMMIT_ON_ANY_VALIDATION_FAILURE; RECONCILE_AFTER_UNCERTAIN_COMMIT",
                ApprovedForExecution: false,
                ExecutableSql: BuildV1ToV2Sql());
        }

        public static SchemaMigrationPackage CreateLocalPackage(
            ISchemaContractProvider contractProvider,
            ISchemaManifestProvider manifestProvider,
            string ordenesCompraV5ManifestHash)
        {
            SchemaContractProposal proposal = Create(contractProvider, manifestProvider);
            string sql = proposal.ExecutableSql ?? throw new InvalidOperationException("RECEPCION_V2_SQL_NOT_PREPARED");
            SchemaMigrationDefinition migration = new(
                MigrationId,
                "REC-B20260921",
                DatabaseScopes.Recepcion,
                SourceVersion,
                TargetVersion,
                1,
                new[]
                {
                    "dbo.Recepciones.OCSucursal",
                    "dbo.RecepcionPartidas.idSucursal",
                    "dbo.RecepcionPartidas.RecepcionOrdenSucursal",
                    "dbo.RecepcionPartidas.OCDetalleOrdenSucursal",
                },
                proposal.Preconditions,
                proposal.DataPreconditions,
                SchemaMigrationHash.Sha256(sql),
                proposal.TargetManifestHash,
                proposal.TransactionMode,
                "High",
                AutoApplicable: false,
                TimeSpan.FromMinutes(5),
                new[] { proposal.SourceManifestHash, ordenesCompraV5ManifestHash },
                "ReconcileAfterUncertainCommit",
                sql,
                proposal.TargetContract,
                proposal.SourceContract);

            return new SchemaMigrationPackage(
                new SchemaReleaseManifest(
                    DatabaseScopes.Recepcion,
                    "REC-B20260921",
                    SourceVersion,
                    TargetVersion,
                    proposal.TargetManifestHash,
                    Array.Empty<string>()),
                new[] { migration });
        }

        public static SchemaContract CreateTargetContract(SchemaContract source)
        {
            ArgumentNullException.ThrowIfNull(source);
            if (!string.Equals(source.Scope, DatabaseScopes.Recepcion, StringComparison.OrdinalIgnoreCase) ||
                source.ContractVersion != SourceVersion)
            {
                throw new InvalidOperationException("RECEPCION_V2_SOURCE_CONTRACT_MUST_BE_V1");
            }

            SchemaTableContract[] tables = source.Tables.Select(table => table.Name switch
            {
                "Recepciones" => MakeHeaderV2(table),
                "RecepcionPartidas" => MakeDetailsV2(table),
                _ => table,
            }).ToArray();

            return new SchemaContract(
                DatabaseScopes.Recepcion,
                TargetVersion,
                "Recepcion de OC",
                tables,
                source.Sources.Concat(new[]
                {
                    "inspectorapi/checklistWs/Services/Tenant/RecepcionV2ContractProposal.cs",
                    "inspector/docs/compras/OC_QA03S1_CONTRATO_RECEPCION_V2_OC_V5_20261006.md",
                }).ToArray(),
                new DateTime(2026, 10, 6, 0, 0, 0, DateTimeKind.Utc));
        }

        public static IReadOnlyCollection<string> BuildPreconditions() => new[]
        {
            "SELECT CASE WHEN OBJECT_ID(N'dbo.Recepciones', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.RecepcionPartidas', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.OrdenesCompraSucursales', N'U') IS NOT NULL THEN 1 ELSE 0 END;",
            "SELECT CASE WHEN COL_LENGTH(N'dbo.OrdenesCompraDetalle', N'idSucursal') IS NOT NULL AND COLUMNPROPERTY(OBJECT_ID(N'dbo.OrdenesCompraDetalle'), N'idSucursal', 'AllowsNull')=0 THEN 1 ELSE 0 END;",
            "SELECT CASE WHEN COL_LENGTH(N'dbo.RecepcionPartidas', N'idSucursal') IS NULL AND OBJECT_ID(N'dbo.FK_Recepciones_OCSucursales_EmpresaOrdenSucursal', N'F') IS NULL AND OBJECT_ID(N'dbo.FK_RecepcionPartidas_Recepciones_EmpresaRecepcionOrdenSucursal', N'F') IS NULL THEN 1 ELSE 0 END;",
            "SELECT CASE WHEN COLUMNPROPERTY(OBJECT_ID(N'dbo.Recepciones'), N'idSucursal', 'AllowsNull')=0 THEN 1 ELSE 0 END;",
        };

        public static IReadOnlyCollection<string> BuildDataPreconditions() => new[]
        {
            "SELECT CASE WHEN NOT EXISTS (SELECT 1 FROM dbo.Recepciones r LEFT JOIN dbo.OrdenesCompraSucursales os ON os.idEmpresa=r.idEmpresa AND os.idOrdenCompra=r.idOrdenCompra AND os.idSucursal=r.idSucursal WHERE os.id IS NULL) THEN 1 ELSE 0 END;",
            "SELECT CASE WHEN NOT EXISTS (SELECT 1 FROM dbo.RecepcionPartidas rp JOIN dbo.Recepciones r ON r.idEmpresa=rp.idEmpresa AND r.id=rp.idRecepcion LEFT JOIN dbo.OrdenesCompraDetalle d ON d.idEmpresa=rp.idEmpresa AND d.id=rp.idOrdenCompraDetalle AND d.idOrdenCompra=rp.idOrdenCompra AND d.idSucursal=r.idSucursal WHERE d.id IS NULL) THEN 1 ELSE 0 END;",
            "SELECT CASE WHEN NOT EXISTS (SELECT 1 FROM dbo.RecepcionPartidas rp LEFT JOIN dbo.InventarioMovimientos m ON m.idEmpresa=rp.idEmpresa AND m.id=rp.idInventarioMovimiento WHERE (rp.TipoPartida=2 AND rp.idInventarioMovimiento IS NOT NULL) OR (rp.idInventarioMovimiento IS NOT NULL AND (m.id IS NULL OR m.idSucursal<>(SELECT r.idSucursal FROM dbo.Recepciones r WHERE r.idEmpresa=rp.idEmpresa AND r.id=rp.idRecepcion)))) THEN 1 ELSE 0 END;",
            "SELECT CASE WHEN NOT EXISTS (SELECT 1 FROM dbo.RecepcionSeries rs JOIN dbo.RecepcionPartidas rp ON rp.idEmpresa=rs.idEmpresa AND rp.id=rs.idRecepcionPartida WHERE rs.idRecepcion<>rp.idRecepcion OR rs.idProductoServicio<>rp.idProductoServicio OR ISNULL(rs.idVariante,'00000000-0000-0000-0000-000000000000')<>ISNULL(rp.idVariante,'00000000-0000-0000-0000-000000000000')) THEN 1 ELSE 0 END;",
        };

        public static IReadOnlyCollection<string> BuildRuntimeRules() => new[]
        {
            "Recepciones.idSucursal is server-resolved from an active OrdenesCompraSucursales row for the same idEmpresa and idOrdenCompra; client tenant authority is forbidden.",
            "Every RecepcionPartidas row repeats that idSucursal and must match both its Recepcion and OrdenesCompraDetalle through the V2 composite foreign keys.",
            "Receipt creation remains one serializable transaction with UPDLOCK/HOLDLOCK over the OC and its pending details; CantidadBaseEstaRecepcion must be positive and no greater than the locked pending quantity.",
            "OperationKey remains unique per tenant; an identical retry returns the existing receipt and never repeats inventory or series effects.",
            "TipoPartida=1 (Producto) creates inventory movement, balance and required series only for the receipt branch while preserving product, variant and purchase-presentation conversion snapshots.",
            "TipoPartida=2 (Servicio) may be received for the branch but idInventarioMovimiento remains NULL and no movement, balance or series is created.",
            "When serial control applies, received base quantity must be integral, series count must equal CantidadBaseEstaRecepcion, and existing tenant/product/variant uniqueness rules remain mandatory.",
            "OC state is derived after each receipt from all locked details: partial when any pending quantity remains and received when all pending quantities are zero.",
        };

        public static string BuildV1ToV2Sql() => @"
SET XACT_ABORT ON;
SET NOCOUNT ON;

DECLARE @TargetComplete bit = CASE WHEN
    COL_LENGTH(N'dbo.RecepcionPartidas', N'idSucursal') IS NOT NULL
    AND COLUMNPROPERTY(OBJECT_ID(N'dbo.RecepcionPartidas'), N'idSucursal', 'AllowsNull')=0
    AND OBJECT_ID(N'dbo.UX_Recepciones_Empresa_Id_Orden_Sucursal', N'UQ') IS NOT NULL
    AND OBJECT_ID(N'dbo.FK_Recepciones_OCSucursales_EmpresaOrdenSucursal', N'F') IS NOT NULL
    AND OBJECT_ID(N'dbo.UX_RecepcionPartidas_Empresa_Id_Sucursal', N'UQ') IS NOT NULL
    AND OBJECT_ID(N'dbo.FK_RecepcionPartidas_Sucursales_EmpresaId', N'F') IS NOT NULL
    AND OBJECT_ID(N'dbo.FK_RecepcionPartidas_Recepciones_EmpresaRecepcionOrdenSucursal', N'F') IS NOT NULL
    AND OBJECT_ID(N'dbo.FK_RecepcionPartidas_OCDetalle_EmpresaDetalleOrdenSucursal', N'F') IS NOT NULL
    AND EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RecepcionPartidas') AND name=N'IX_RecepcionPartidas_Empresa_Sucursal_Orden')
    THEN 1 ELSE 0 END;
IF @TargetComplete=1
    RETURN;

IF COL_LENGTH(N'dbo.RecepcionPartidas', N'idSucursal') IS NOT NULL
   OR OBJECT_ID(N'dbo.UX_Recepciones_Empresa_Id_Orden_Sucursal', N'UQ') IS NOT NULL
   OR OBJECT_ID(N'dbo.FK_Recepciones_OCSucursales_EmpresaOrdenSucursal', N'F') IS NOT NULL
   OR OBJECT_ID(N'dbo.UX_RecepcionPartidas_Empresa_Id_Sucursal', N'UQ') IS NOT NULL
   OR OBJECT_ID(N'dbo.FK_RecepcionPartidas_Sucursales_EmpresaId', N'F') IS NOT NULL
   OR OBJECT_ID(N'dbo.FK_RecepcionPartidas_Recepciones_EmpresaRecepcionOrdenSucursal', N'F') IS NOT NULL
   OR OBJECT_ID(N'dbo.FK_RecepcionPartidas_OCDetalle_EmpresaDetalleOrdenSucursal', N'F') IS NOT NULL
   OR EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.RecepcionPartidas') AND name=N'IX_RecepcionPartidas_Empresa_Sucursal_Orden')
    THROW 51100, 'RECEPCION_V2_PARTIAL_OR_DRIFTED_TARGET_REJECTED', 1;

IF OBJECT_ID(N'dbo.OrdenesCompraSucursales', N'U') IS NULL
   OR COL_LENGTH(N'dbo.OrdenesCompraDetalle', N'idSucursal') IS NULL
   OR COLUMNPROPERTY(OBJECT_ID(N'dbo.OrdenesCompraDetalle'), N'idSucursal', 'AllowsNull')<>0
    THROW 51101, 'RECEPCION_V2_REQUIRES_OC_V5', 1;
IF EXISTS (
    SELECT 1 FROM dbo.Recepciones r
    LEFT JOIN dbo.OrdenesCompraSucursales os
      ON os.idEmpresa=r.idEmpresa AND os.idOrdenCompra=r.idOrdenCompra AND os.idSucursal=r.idSucursal
    WHERE os.id IS NULL)
    THROW 51102, 'RECEPCION_V1_BRANCH_NOT_IN_OC_V5', 1;
IF EXISTS (
    SELECT 1 FROM dbo.RecepcionPartidas rp
    JOIN dbo.Recepciones r ON r.idEmpresa=rp.idEmpresa AND r.id=rp.idRecepcion
    LEFT JOIN dbo.OrdenesCompraDetalle d
      ON d.idEmpresa=rp.idEmpresa AND d.id=rp.idOrdenCompraDetalle
     AND d.idOrdenCompra=rp.idOrdenCompra AND d.idSucursal=r.idSucursal
    WHERE d.id IS NULL)
    THROW 51103, 'RECEPCION_V1_DETAIL_BRANCH_INCOMPATIBLE', 1;
IF EXISTS (
    SELECT 1 FROM dbo.RecepcionPartidas rp
    LEFT JOIN dbo.InventarioMovimientos m ON m.idEmpresa=rp.idEmpresa AND m.id=rp.idInventarioMovimiento
    JOIN dbo.Recepciones r ON r.idEmpresa=rp.idEmpresa AND r.id=rp.idRecepcion
    WHERE (rp.TipoPartida=2 AND rp.idInventarioMovimiento IS NOT NULL)
       OR (rp.idInventarioMovimiento IS NOT NULL AND
           (m.id IS NULL OR m.idSucursal<>r.idSucursal OR m.idProductoServicio<>rp.idProductoServicio OR
            ISNULL(m.idVariante,'00000000-0000-0000-0000-000000000000')<>ISNULL(rp.idVariante,'00000000-0000-0000-0000-000000000000'))))
    THROW 51104, 'RECEPCION_V1_INVENTORY_RECONCILIATION_FAILED', 1;
IF EXISTS (
    SELECT 1 FROM dbo.RecepcionSeries rs
    JOIN dbo.RecepcionPartidas rp ON rp.idEmpresa=rs.idEmpresa AND rp.id=rs.idRecepcionPartida
    LEFT JOIN dbo.InventarioSeries s ON s.idEmpresa=rs.idEmpresa AND s.id=rs.idInventarioSerie
    JOIN dbo.Recepciones r ON r.idEmpresa=rp.idEmpresa AND r.id=rp.idRecepcion
    WHERE rs.idRecepcion<>rp.idRecepcion OR rs.idProductoServicio<>rp.idProductoServicio
       OR ISNULL(rs.idVariante,'00000000-0000-0000-0000-000000000000')<>ISNULL(rp.idVariante,'00000000-0000-0000-0000-000000000000')
       OR (rs.idInventarioSerie IS NOT NULL AND
           (s.id IS NULL OR s.idSucursalActual<>r.idSucursal OR s.idProductoServicio<>rp.idProductoServicio OR
            ISNULL(s.idVariante,'00000000-0000-0000-0000-000000000000')<>ISNULL(rp.idVariante,'00000000-0000-0000-0000-000000000000'))))
    THROW 51105, 'RECEPCION_V1_SERIES_RECONCILIATION_FAILED', 1;
IF EXISTS (
    SELECT 1 FROM dbo.RecepcionPartidas rp
    OUTER APPLY (SELECT COUNT_BIG(*) AS SeriesCount FROM dbo.RecepcionSeries rs WHERE rs.idEmpresa=rp.idEmpresa AND rs.idRecepcionPartida=rp.id) sc
    WHERE rp.ControlSerie=1 AND
      (rp.CantidadBaseEstaRecepcion<>FLOOR(rp.CantidadBaseEstaRecepcion) OR sc.SeriesCount<>CONVERT(bigint,rp.CantidadBaseEstaRecepcion)))
    THROW 51106, 'RECEPCION_V1_SERIES_COUNT_MISMATCH', 1;

DECLARE @ReceiptCount bigint=(SELECT COUNT_BIG(*) FROM dbo.Recepciones);
DECLARE @DetailCount bigint=(SELECT COUNT_BIG(*) FROM dbo.RecepcionPartidas);
DECLARE @Ordered decimal(38,4)=(SELECT COALESCE(SUM(CONVERT(decimal(38,4),CantidadBaseOrdenada)),0) FROM dbo.RecepcionPartidas);
DECLARE @Before decimal(38,4)=(SELECT COALESCE(SUM(CONVERT(decimal(38,4),CantidadBaseRecibidaAnterior)),0) FROM dbo.RecepcionPartidas);
DECLARE @ThisReceipt decimal(38,4)=(SELECT COALESCE(SUM(CONVERT(decimal(38,4),CantidadBaseEstaRecepcion)),0) FROM dbo.RecepcionPartidas);
DECLARE @Accumulated decimal(38,4)=(SELECT COALESCE(SUM(CONVERT(decimal(38,4),CantidadBaseRecibidaAcumulada)),0) FROM dbo.RecepcionPartidas);
DECLARE @Pending decimal(38,4)=(SELECT COALESCE(SUM(CONVERT(decimal(38,4),CantidadBasePendiente)),0) FROM dbo.RecepcionPartidas);
DECLARE @MovementCount bigint=(SELECT COUNT_BIG(*) FROM dbo.InventarioMovimientos);
DECLARE @SeriesCount bigint=(SELECT COUNT_BIG(*) FROM dbo.InventarioSeries);

ALTER TABLE dbo.Recepciones ADD
    CONSTRAINT UX_Recepciones_Empresa_Id_Orden_Sucursal UNIQUE (idEmpresa,id,idOrdenCompra,idSucursal),
    CONSTRAINT FK_Recepciones_OCSucursales_EmpresaOrdenSucursal FOREIGN KEY (idEmpresa,idOrdenCompra,idSucursal)
        REFERENCES dbo.OrdenesCompraSucursales(idEmpresa,idOrdenCompra,idSucursal);

ALTER TABLE dbo.RecepcionPartidas ADD idSucursal UNIQUEIDENTIFIER NULL;
EXEC sys.sp_executesql N'
UPDATE rp
SET idSucursal=r.idSucursal
FROM dbo.RecepcionPartidas rp
JOIN dbo.Recepciones r ON r.idEmpresa=rp.idEmpresa AND r.id=rp.idRecepcion
WHERE rp.idSucursal IS NULL;
IF EXISTS (SELECT 1 FROM dbo.RecepcionPartidas WHERE idSucursal IS NULL)
    THROW 51107, ''RECEPCION_V2_BRANCH_BACKFILL_INCOMPLETE'', 1;';
EXEC sys.sp_executesql N'
ALTER TABLE dbo.RecepcionPartidas ALTER COLUMN idSucursal UNIQUEIDENTIFIER NOT NULL;
ALTER TABLE dbo.RecepcionPartidas ADD
    CONSTRAINT UX_RecepcionPartidas_Empresa_Id_Sucursal UNIQUE (idEmpresa,id,idSucursal),
    CONSTRAINT FK_RecepcionPartidas_Sucursales_EmpresaId FOREIGN KEY (idEmpresa,idSucursal) REFERENCES dbo.Sucursales(idEmpresa,id),
    CONSTRAINT FK_RecepcionPartidas_Recepciones_EmpresaRecepcionOrdenSucursal
        FOREIGN KEY (idEmpresa,idRecepcion,idOrdenCompra,idSucursal)
        REFERENCES dbo.Recepciones(idEmpresa,id,idOrdenCompra,idSucursal),
    CONSTRAINT FK_RecepcionPartidas_OCDetalle_EmpresaDetalleOrdenSucursal
        FOREIGN KEY (idEmpresa,idOrdenCompraDetalle,idOrdenCompra,idSucursal)
        REFERENCES dbo.OrdenesCompraDetalle(idEmpresa,id,idOrdenCompra,idSucursal);
CREATE INDEX IX_RecepcionPartidas_Empresa_Sucursal_Orden
    ON dbo.RecepcionPartidas(idEmpresa,idSucursal,idOrdenCompra,idOrdenCompraDetalle);';

EXEC sys.sp_executesql N'
IF (SELECT COUNT_BIG(*) FROM dbo.Recepciones)<>@ReceiptCount
   OR (SELECT COUNT_BIG(*) FROM dbo.RecepcionPartidas)<>@DetailCount
   OR (SELECT COALESCE(SUM(CONVERT(decimal(38,4),CantidadBaseOrdenada)),0) FROM dbo.RecepcionPartidas)<>@Ordered
   OR (SELECT COALESCE(SUM(CONVERT(decimal(38,4),CantidadBaseRecibidaAnterior)),0) FROM dbo.RecepcionPartidas)<>@Before
   OR (SELECT COALESCE(SUM(CONVERT(decimal(38,4),CantidadBaseEstaRecepcion)),0) FROM dbo.RecepcionPartidas)<>@ThisReceipt
   OR (SELECT COALESCE(SUM(CONVERT(decimal(38,4),CantidadBaseRecibidaAcumulada)),0) FROM dbo.RecepcionPartidas)<>@Accumulated
   OR (SELECT COALESCE(SUM(CONVERT(decimal(38,4),CantidadBasePendiente)),0) FROM dbo.RecepcionPartidas)<>@Pending
   OR (SELECT COUNT_BIG(*) FROM dbo.InventarioMovimientos)<>@MovementCount
   OR (SELECT COUNT_BIG(*) FROM dbo.InventarioSeries)<>@SeriesCount
    THROW 51108, ''RECEPCION_V2_RECONCILIATION_FAILED'', 1;',
    N'@ReceiptCount bigint,@DetailCount bigint,@Ordered decimal(38,4),@Before decimal(38,4),@ThisReceipt decimal(38,4),@Accumulated decimal(38,4),@Pending decimal(38,4),@MovementCount bigint,@SeriesCount bigint',
    @ReceiptCount=@ReceiptCount,@DetailCount=@DetailCount,@Ordered=@Ordered,@Before=@Before,@ThisReceipt=@ThisReceipt,@Accumulated=@Accumulated,@Pending=@Pending,@MovementCount=@MovementCount,@SeriesCount=@SeriesCount;
";

        private static SchemaTableContract MakeHeaderV2(SchemaTableContract table) => table with
        {
            ForeignKeys = table.ForeignKeys.Concat(new[]
            {
                new SchemaForeignKeyContract("FK_Recepciones_OCSucursales_EmpresaOrdenSucursal", new[] { "idEmpresa", "idOrdenCompra", "idSucursal" }, "dbo", "OrdenesCompraSucursales", new[] { "idEmpresa", "idOrdenCompra", "idSucursal" }),
            }).ToArray(),
            UniqueConstraints = table.UniqueConstraints.Concat(new[]
            {
                new SchemaUniqueContract("UX_Recepciones_Empresa_Id_Orden_Sucursal", new[] { "idEmpresa", "id", "idOrdenCompra", "idSucursal" }, null),
            }).ToArray(),
        };

        private static SchemaTableContract MakeDetailsV2(SchemaTableContract table)
        {
            List<SchemaColumnContract> columns = table.Columns.ToList();
            int orderIndex = columns.FindIndex(column => column.Name == "idOrdenCompra");
            columns.Insert(orderIndex + 1, Col("idSucursal", "UNIQUEIDENTIFIER", false));

            return table with
            {
                Columns = columns,
                ForeignKeys = table.ForeignKeys.Concat(new[]
                {
                    new SchemaForeignKeyContract("FK_RecepcionPartidas_Sucursales_EmpresaId", new[] { "idEmpresa", "idSucursal" }, "dbo", "Sucursales", new[] { "idEmpresa", "id" }),
                    new SchemaForeignKeyContract("FK_RecepcionPartidas_Recepciones_EmpresaRecepcionOrdenSucursal", new[] { "idEmpresa", "idRecepcion", "idOrdenCompra", "idSucursal" }, "dbo", "Recepciones", new[] { "idEmpresa", "id", "idOrdenCompra", "idSucursal" }),
                    new SchemaForeignKeyContract("FK_RecepcionPartidas_OCDetalle_EmpresaDetalleOrdenSucursal", new[] { "idEmpresa", "idOrdenCompraDetalle", "idOrdenCompra", "idSucursal" }, "dbo", "OrdenesCompraDetalle", new[] { "idEmpresa", "id", "idOrdenCompra", "idSucursal" }),
                }).ToArray(),
                UniqueConstraints = table.UniqueConstraints.Concat(new[]
                {
                    new SchemaUniqueContract("UX_RecepcionPartidas_Empresa_Id_Sucursal", new[] { "idEmpresa", "id", "idSucursal" }, null),
                }).ToArray(),
                Indexes = table.Indexes.Concat(new[]
                {
                    Ix("IX_RecepcionPartidas_Empresa_Sucursal_Orden", false, "idEmpresa", "idSucursal", "idOrdenCompra", "idOrdenCompraDetalle"),
                }).ToArray(),
            };
        }

        private static SchemaColumnContract Col(string name, string type, bool nullable)
            => new(name, type, null, null, null, nullable, null, false, false, null);

        private static SchemaIndexContract Ix(string name, bool unique, params string[] columns)
            => new(name, unique, false, columns.Select(column => new SchemaIndexColumnContract(column, false)).ToArray(), Array.Empty<string>(), null);
    }

    public sealed record OrdenesCompraRecepcionV5V2MigrationPlan(
        string ReviewStatus,
        SchemaMigrationPackage OrdenesCompraPackage,
        SchemaMigrationPackage RecepcionPackage,
        IReadOnlyCollection<string> ExecutionOrder,
        bool ApprovedForExecution);

    public static class OrdenesCompraRecepcionV5V2ContractPlan
    {
        public const string ReviewStatus = "CONTRATO_CERRADO_PO_OC_QA03S1_PENDIENTE_PRE_EJECUCION";

        public static OrdenesCompraRecepcionV5V2MigrationPlan Create(
            ISchemaContractProvider contractProvider,
            ISchemaManifestProvider manifestProvider)
        {
            SchemaMigrationPackage oc = OrdenesCompraV5ContractProposal.CreateLocalPackage(contractProvider, manifestProvider);
            SchemaMigrationPackage reception = RecepcionV2ContractProposal.CreateLocalPackage(
                contractProvider,
                manifestProvider,
                oc.Release.LatestManifestHash);

            return new OrdenesCompraRecepcionV5V2MigrationPlan(
                ReviewStatus,
                oc,
                reception,
                new[]
                {
                    OrdenesCompraV5ContractProposal.MigrationId,
                    RecepcionV2ContractProposal.MigrationId,
                },
                ApprovedForExecution: false);
        }
    }
}
