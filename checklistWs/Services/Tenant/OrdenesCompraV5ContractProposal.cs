namespace checklistWs.Services.Tenant
{
    /// <summary>
    /// OC-QA03S: propuesta local y no ejecutable del contrato V5 multisucursal.
    /// No forma parte de <see cref="ProductosServiciosSchemaContractProvider"/> ni del package activo.
    /// </summary>
    public static class OrdenesCompraV5ContractProposal
    {
        public const int SourceVersion = ProductosServiciosSchemaContractProvider.V4;
        public const int TargetVersion = 5;
        public const string MigrationId = "OC-M20261006-V4-V5-MULTISUCURSAL-PARTIDA";
        public const string ReconciliationMigrationId = "OC-M20261007-V5-RECONCILE-CONTRACT-INDEX-METADATA";
        public const string SupersededTargetManifestHash = "4e88ac49a73e3169903a6399a5b8c4fa2ea4c9b747005082ecc086636cb8462a";
        public const string ReviewStatus = "APROBADO_MODELO_PO_OC_QA03S1_PENDIENTE_PRE_EJECUCION";

        public static SchemaContractProposal Create(
            ISchemaContractProvider contractProvider,
            ISchemaManifestProvider manifestProvider)
        {
            ArgumentNullException.ThrowIfNull(contractProvider);
            ArgumentNullException.ThrowIfNull(manifestProvider);

            SchemaContract source = contractProvider.GetContract(DatabaseScopes.OrdenesCompra, SourceVersion);
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
                ExecutableSql: BuildV4ToV5Sql());
        }

        public static SchemaMigrationPackage CreateLocalPackage(
            ISchemaContractProvider contractProvider,
            ISchemaManifestProvider manifestProvider)
        {
            SchemaContractProposal proposal = Create(contractProvider, manifestProvider);
            string sql = proposal.ExecutableSql ?? throw new InvalidOperationException("OC_V5_SQL_NOT_PREPARED");
            SchemaMigrationDefinition migration = new(
                proposal.MigrationId,
                "OC-B20260921",
                DatabaseScopes.OrdenesCompra,
                SourceVersion,
                TargetVersion,
                4,
                new[]
                {
                    "dbo.OrdenesCompraSucursales",
                    "dbo.OrdenesCompra.idSucursal",
                    "dbo.OrdenesCompraDetalle.idSucursal",
                },
                proposal.Preconditions,
                proposal.DataPreconditions,
                SchemaMigrationHash.Sha256(sql),
                proposal.TargetManifestHash,
                proposal.TransactionMode,
                "High",
                AutoApplicable: false,
                TimeSpan.FromMinutes(5),
                new[] { proposal.SourceManifestHash },
                "ReconcileAfterUncertainCommit",
                sql,
                proposal.TargetContract,
                proposal.SourceContract,
                new[] { SupersededTargetManifestHash });

            SchemaContract supersededTarget = CreateSupersededTargetContract(proposal.SourceContract);
            string reconciliationSql = BuildV5ContractMetadataReconciliationSql();
            SchemaMigrationDefinition reconciliation = new(
                ReconciliationMigrationId,
                "OC-B20260921",
                DatabaseScopes.OrdenesCompra,
                TargetVersion,
                TargetVersion,
                5,
                new[]
                {
                    "dbo.OrdenesCompraSucursales.UNIQUE_CONSTRAINT_METADATA",
                    "dbo.OrdenesCompraDetalle.UNIQUE_CONSTRAINT_METADATA",
                },
                new[]
                {
                    "SELECT CASE WHEN OBJECT_ID(N'dbo.OrdenesCompraSucursales',N'U') IS NOT NULL AND COL_LENGTH(N'dbo.OrdenesCompraDetalle',N'idSucursal') IS NOT NULL THEN 1 ELSE 0 END;",
                },
                new[]
                {
                    "NO_DDL",
                    "NO_DML_BUSINESS_ROWS",
                    "PRESERVE_OC_V5_PHYSICAL_OBJECTS",
                },
                SchemaMigrationHash.Sha256(reconciliationSql),
                proposal.TargetManifestHash,
                "SingleTransaction",
                "Low",
                AutoApplicable: false,
                TimeSpan.FromMinutes(2),
                new[] { SupersededTargetManifestHash },
                "ReconcileAfterValidatedEquivalentPhysicalContract",
                reconciliationSql,
                proposal.TargetContract,
                supersededTarget);

            return new SchemaMigrationPackage(
                new SchemaReleaseManifest(
                    DatabaseScopes.OrdenesCompra,
                    "OC-B20260921",
                    ProductosServiciosSchemaContractProvider.V1,
                    TargetVersion,
                    proposal.TargetManifestHash,
                    Array.Empty<string>()),
                new[] { migration, reconciliation });
        }

        public static SchemaContract CreateSupersededTargetContract(SchemaContract source)
        {
            SchemaContract corrected = CreateTargetContract(source);
            SchemaTableContract[] tables = corrected.Tables.Select(table => table.Name switch
            {
                "OrdenesCompraDetalle" => table with
                {
                    Indexes = table.Indexes.Concat(new[]
                    {
                        Ix("UX_OrdenesCompraDetalle_Empresa_Id_Orden_Sucursal", true, null, "idEmpresa", "id", "idOrdenCompra", "idSucursal"),
                    }).ToArray(),
                },
                "OrdenesCompraSucursales" => table with
                {
                    Indexes = table.Indexes.Concat(new[]
                    {
                        Ix("UX_OrdenesCompraSucursales_Empresa_Id", true, null, "idEmpresa", "id"),
                        Ix("UX_OrdenesCompraSucursales_Empresa_Orden_Sucursal", true, null, "idEmpresa", "idOrdenCompra", "idSucursal"),
                    }).ToArray(),
                },
                _ => table,
            }).ToArray();

            return corrected with { Tables = tables };
        }

        public static string BuildV5ContractMetadataReconciliationSql() => @"
SET XACT_ABORT ON;
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.OrdenesCompraSucursales',N'U') IS NULL
   OR COL_LENGTH(N'dbo.OrdenesCompraDetalle',N'idSucursal') IS NULL
   OR COLUMNPROPERTY(OBJECT_ID(N'dbo.OrdenesCompraDetalle'),N'idSucursal','AllowsNull')<>0
   OR COLUMNPROPERTY(OBJECT_ID(N'dbo.OrdenesCompra'),N'idSucursal','AllowsNull')<>1
   OR OBJECT_ID(N'dbo.UX_OrdenesCompraSucursales_Empresa_Id',N'UQ') IS NULL
   OR OBJECT_ID(N'dbo.UX_OrdenesCompraSucursales_Empresa_Orden_Sucursal',N'UQ') IS NULL
   OR OBJECT_ID(N'dbo.UX_OrdenesCompraDetalle_Empresa_Id_Orden_Sucursal',N'UQ') IS NULL
   OR NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.OrdenesCompraSucursales') AND name=N'IX_OrdenesCompraSucursales_Empresa_Sucursal_Orden' AND is_unique_constraint=0)
   OR NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.OrdenesCompraSucursales') AND name=N'IX_OrdenesCompraSucursales_Empresa_Orden_Activo' AND is_unique_constraint=0)
   OR NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.OrdenesCompraSucursales') AND name=N'IX_OrdenesCompraSucursales_Empresa_Correlation' AND is_unique_constraint=0)
   OR NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.OrdenesCompraDetalle') AND name=N'IX_OrdenesCompraDetalle_Empresa_Orden_Sucursal' AND is_unique_constraint=0)
    THROW 51058, 'OC_V5_CONTRACT_METADATA_RECONCILIATION_PHYSICAL_MISMATCH', 1;
";

        public static SchemaContract CreateTargetContract(SchemaContract source)
        {
            ArgumentNullException.ThrowIfNull(source);
            if (!string.Equals(source.Scope, DatabaseScopes.OrdenesCompra, StringComparison.OrdinalIgnoreCase) ||
                source.ContractVersion != SourceVersion)
            {
                throw new InvalidOperationException("OC_V5_SOURCE_CONTRACT_MUST_BE_V4");
            }

            List<SchemaTableContract> tables = source.Tables
                .Select(table => table.Name switch
                {
                    "OrdenesCompra" => MakeHeaderV5(table),
                    "OrdenesCompraDetalle" => MakeDetailV5(table),
                    _ => table,
                })
                .ToList();
            tables.Add(DestinationBranches());

            return new SchemaContract(
                DatabaseScopes.OrdenesCompra,
                TargetVersion,
                "Ordenes de Compra",
                tables,
                source.Sources.Concat(new[]
                {
                    "inspectorapi/checklistWs/Services/Tenant/OrdenesCompraV5ContractProposal.cs",
                    "inspector/docs/compras/OC_QA03S_CONTRATO_SCHEMA_V5_MULTISUCURSAL_20261006.md",
                }).ToArray(),
                new DateTime(2026, 10, 6, 0, 0, 0, DateTimeKind.Utc));
        }

        public static IReadOnlyCollection<string> BuildReceptionV5Requirements() => new[]
        {
            "Recepciones.idSucursal remains NOT NULL and identifies the one destination branch handled by that receipt.",
            "Recepciones must reference OrdenesCompraSucursales by (idEmpresa,idOrdenCompra,idSucursal).",
            "RecepcionPartidas V2 must persist idSucursal NOT NULL and match both its Recepcion and OrdenesCompraDetalle through tenant-safe composite foreign keys.",
            "A receipt may include only details assigned to its own branch; partial and repeated receipts keep current no-overreceipt and OperationKey rules.",
            "TipoPartida=2 (Servicio) is receivable but must keep idInventarioMovimiento NULL and must never create InventarioMovimientos or InventarioSaldos.",
            "TipoPartida=1 (Producto) sends the detail branch to Inventario; header OrdenesCompra.idSucursal is forbidden as a V5 inventory source.",
            "Variant, purchase-presentation snapshots, quantities, serial control, audit and idempotency remain unchanged.",
            "Runtime V5 writes remain blocked until a separately reviewed Recepcion V2 contract enforces these relations.",
        };

        public static IReadOnlyCollection<string> BuildMutationRules() => new[]
        {
            "OrdenesCompraDetalle.idSucursal may change only while OrdenesCompra.Estado=1 (Borrador) and no RecepcionPartidas exists for the order/detail.",
            "OrdenesCompraDetalle.idSucursal is immutable when OrdenesCompra.Estado IN (2,3,4,5).",
            "OrdenesCompraSucursales may be archived or removed only when no OrdenesCompraDetalle references that destination.",
            "A destination referenced by any Recepciones or RecepcionPartidas row may never be archived, removed or reassigned.",
            "OrdenesCompra.idSucursal is historical compatibility data only; new V5 orders write NULL and runtime reads are forbidden.",
        };

        public static IReadOnlyCollection<string> BuildPreconditions() => new[]
        {
            "SELECT CASE WHEN OBJECT_ID(N'dbo.OrdenesCompra', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.OrdenesCompraDetalle', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.Sucursales', N'U') IS NOT NULL THEN 1 ELSE 0 END;",
            "SELECT CASE WHEN COL_LENGTH(N'dbo.OrdenesCompra', N'idSucursal') IS NOT NULL AND COLUMNPROPERTY(OBJECT_ID(N'dbo.OrdenesCompra'), N'idSucursal', 'AllowsNull') = 0 THEN 1 ELSE 0 END;",
            "SELECT CASE WHEN OBJECT_ID(N'dbo.OrdenesCompraSucursales', N'U') IS NULL AND COL_LENGTH(N'dbo.OrdenesCompraDetalle', N'idSucursal') IS NULL THEN 1 ELSE 0 END;",
            "SELECT CASE WHEN NOT EXISTS (SELECT 1 FROM dbo.OrdenesCompra WHERE idSucursal IS NULL) THEN 1 ELSE 0 END;",
        };

        public static IReadOnlyCollection<string> BuildDataPreconditions() => new[]
        {
            "SELECT CASE WHEN NOT EXISTS (SELECT 1 FROM dbo.OrdenesCompra oc LEFT JOIN dbo.Sucursales s ON s.idEmpresa=oc.idEmpresa AND s.id=oc.idSucursal WHERE s.id IS NULL) THEN 1 ELSE 0 END;",
            "SELECT CASE WHEN NOT EXISTS (SELECT 1 FROM dbo.OrdenesCompraDetalle d LEFT JOIN dbo.OrdenesCompra oc ON oc.idEmpresa=d.idEmpresa AND oc.id=d.idOrdenCompra WHERE oc.id IS NULL) THEN 1 ELSE 0 END;",
            "SELECT CASE WHEN NOT EXISTS (SELECT 1 FROM dbo.OrdenesCompraDetalle d JOIN dbo.OrdenesCompra oc ON oc.id=d.idOrdenCompra WHERE d.idEmpresa<>oc.idEmpresa) THEN 1 ELSE 0 END;",
            "SELECT CASE WHEN NOT EXISTS (SELECT 1 FROM dbo.RecepcionPartidas rp JOIN dbo.Recepciones r ON r.idEmpresa=rp.idEmpresa AND r.id=rp.idRecepcion JOIN dbo.OrdenesCompra oc ON oc.idEmpresa=rp.idEmpresa AND oc.id=rp.idOrdenCompra WHERE r.idSucursal<>oc.idSucursal OR rp.idOrdenCompraDetalle IS NULL) THEN 1 ELSE 0 END;",
        };

        public static string BuildV4ToV5Sql() => @"
SET XACT_ABORT ON;
SET NOCOUNT ON;

DECLARE @TargetComplete bit = CASE WHEN
    OBJECT_ID(N'dbo.OrdenesCompraSucursales', N'U') IS NOT NULL
    AND COL_LENGTH(N'dbo.OrdenesCompraDetalle', N'idSucursal') IS NOT NULL
    AND COLUMNPROPERTY(OBJECT_ID(N'dbo.OrdenesCompraDetalle'), N'idSucursal', 'AllowsNull') = 0
    AND COLUMNPROPERTY(OBJECT_ID(N'dbo.OrdenesCompra'), N'idSucursal', 'AllowsNull') = 1
    AND OBJECT_ID(N'dbo.UX_OrdenesCompraSucursales_Empresa_Orden_Sucursal', N'UQ') IS NOT NULL
    AND OBJECT_ID(N'dbo.FK_OrdenesCompraSucursales_OrdenesCompra_EmpresaId', N'F') IS NOT NULL
    AND OBJECT_ID(N'dbo.FK_OrdenesCompraSucursales_Sucursales_EmpresaId', N'F') IS NOT NULL
    AND OBJECT_ID(N'dbo.FK_OrdenesCompraDetalle_Sucursales_EmpresaId', N'F') IS NOT NULL
    AND OBJECT_ID(N'dbo.FK_OrdenesCompraDetalle_OCSucursales_EmpresaOrdenSucursal', N'F') IS NOT NULL
    AND OBJECT_ID(N'dbo.UX_OrdenesCompraDetalle_Empresa_Id_Orden_Sucursal', N'UQ') IS NOT NULL
    AND EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.OrdenesCompraDetalle') AND name=N'IX_OrdenesCompraDetalle_Empresa_Orden_Sucursal')
    THEN 1 ELSE 0 END;

IF @TargetComplete = 1
    RETURN;

IF OBJECT_ID(N'dbo.OrdenesCompraSucursales', N'U') IS NOT NULL
   OR COL_LENGTH(N'dbo.OrdenesCompraDetalle', N'idSucursal') IS NOT NULL
   OR COLUMNPROPERTY(OBJECT_ID(N'dbo.OrdenesCompra'), N'idSucursal', 'AllowsNull') <> 0
    THROW 51050, 'OC_V5_PARTIAL_OR_DRIFTED_TARGET_REJECTED', 1;

IF EXISTS (SELECT 1 FROM dbo.OrdenesCompra WHERE idSucursal IS NULL)
    THROW 51051, 'OC_V4_HEADER_BRANCH_REQUIRED_FOR_BACKFILL', 1;
IF EXISTS (
    SELECT 1 FROM dbo.OrdenesCompra oc
    LEFT JOIN dbo.Sucursales s ON s.idEmpresa=oc.idEmpresa AND s.id=oc.idSucursal
    WHERE s.id IS NULL)
    THROW 51052, 'OC_V4_FOREIGN_OR_MISSING_BRANCH', 1;
IF EXISTS (
    SELECT 1 FROM dbo.OrdenesCompraDetalle d
    LEFT JOIN dbo.OrdenesCompra oc ON oc.idEmpresa=d.idEmpresa AND oc.id=d.idOrdenCompra
    WHERE oc.id IS NULL)
    THROW 51053, 'OC_V4_ORPHAN_DETAIL', 1;
IF EXISTS (
    SELECT 1
    FROM dbo.RecepcionPartidas rp
    JOIN dbo.Recepciones r ON r.idEmpresa=rp.idEmpresa AND r.id=rp.idRecepcion
    JOIN dbo.OrdenesCompra oc ON oc.idEmpresa=rp.idEmpresa AND oc.id=rp.idOrdenCompra
    WHERE r.idSucursal<>oc.idSucursal OR rp.idOrdenCompraDetalle IS NULL)
    THROW 51054, 'OC_V4_RECEIPT_BRANCH_INCONSISTENT', 1;

DECLARE @HeaderCount bigint = (SELECT COUNT_BIG(*) FROM dbo.OrdenesCompra);
DECLARE @DetailCount bigint = (SELECT COUNT_BIG(*) FROM dbo.OrdenesCompraDetalle);
DECLARE @HeaderSubtotal decimal(38,2) = (SELECT COALESCE(SUM(CONVERT(decimal(38,2), Subtotal)),0) FROM dbo.OrdenesCompra);
DECLARE @HeaderTotal decimal(38,2) = (SELECT COALESCE(SUM(CONVERT(decimal(38,2), Total)),0) FROM dbo.OrdenesCompra);
DECLARE @Ordered decimal(38,4) = (SELECT COALESCE(SUM(CONVERT(decimal(38,4), CantidadBaseOrdenada)),0) FROM dbo.OrdenesCompraDetalle);
DECLARE @Received decimal(38,4) = (SELECT COALESCE(SUM(CONVERT(decimal(38,4), CantidadBaseRecibidaAcumulada)),0) FROM dbo.OrdenesCompraDetalle);
DECLARE @Pending decimal(38,4) = (SELECT COALESCE(SUM(CONVERT(decimal(38,4), CantidadBasePendiente)),0) FROM dbo.OrdenesCompraDetalle);

CREATE TABLE dbo.OrdenesCompraSucursales
(
    id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_OrdenesCompraSucursales_id DEFAULT (NEWID()),
    idEmpresa UNIQUEIDENTIFIER NOT NULL,
    identityKey UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_OrdenesCompraSucursales_identityKey DEFAULT (NEWID()),
    idOrdenCompra UNIQUEIDENTIFIER NOT NULL,
    idSucursal UNIQUEIDENTIFIER NOT NULL,
    Activo BIT NOT NULL CONSTRAINT DF_OrdenesCompraSucursales_Activo DEFAULT ((1)),
    FechaCreacion DATETIME2(0) NOT NULL CONSTRAINT DF_OrdenesCompraSucursales_FechaCreacion DEFAULT (SYSUTCDATETIME()),
    FechaActualizacion DATETIME2(0) NOT NULL CONSTRAINT DF_OrdenesCompraSucursales_FechaActualizacion DEFAULT (SYSUTCDATETIME()),
    FechaArchivado DATETIME2(0) NULL,
    idUsuarioCreacion UNIQUEIDENTIFIER NULL,
    idUsuarioActualizacion UNIQUEIDENTIFIER NULL,
    CorrelationId UNIQUEIDENTIFIER NULL,
    CONSTRAINT PK_OrdenesCompraSucursales PRIMARY KEY CLUSTERED (id),
    CONSTRAINT UX_OrdenesCompraSucursales_Empresa_Id UNIQUE (idEmpresa,id),
    CONSTRAINT UX_OrdenesCompraSucursales_Empresa_Orden_Sucursal UNIQUE (idEmpresa,idOrdenCompra,idSucursal),
    CONSTRAINT FK_OrdenesCompraSucursales_OrdenesCompra_EmpresaId FOREIGN KEY (idEmpresa,idOrdenCompra) REFERENCES dbo.OrdenesCompra(idEmpresa,id),
    CONSTRAINT FK_OrdenesCompraSucursales_Sucursales_EmpresaId FOREIGN KEY (idEmpresa,idSucursal) REFERENCES dbo.Sucursales(idEmpresa,id),
    CONSTRAINT CK_OrdenesCompraSucursales_Archivado CHECK ((Activo=1 AND FechaArchivado IS NULL) OR (Activo=0 AND FechaArchivado IS NOT NULL))
);

EXEC sys.sp_executesql N'
CREATE INDEX IX_OrdenesCompraSucursales_Empresa_Sucursal_Orden
    ON dbo.OrdenesCompraSucursales(idEmpresa,idSucursal,idOrdenCompra);
CREATE INDEX IX_OrdenesCompraSucursales_Empresa_Orden_Activo
    ON dbo.OrdenesCompraSucursales(idEmpresa,idOrdenCompra,Activo);
CREATE INDEX IX_OrdenesCompraSucursales_Empresa_Correlation
    ON dbo.OrdenesCompraSucursales(idEmpresa,CorrelationId) WHERE CorrelationId IS NOT NULL;

INSERT INTO dbo.OrdenesCompraSucursales
    (id,idEmpresa,identityKey,idOrdenCompra,idSucursal,Activo,FechaCreacion,FechaActualizacion,FechaArchivado,idUsuarioCreacion,idUsuarioActualizacion,CorrelationId)
SELECT NEWID(),oc.idEmpresa,NEWID(),oc.id,oc.idSucursal,oc.Activo,oc.FechaCreacion,oc.FechaActualizacion,oc.FechaArchivado,oc.idUsuarioCreacion,oc.idUsuarioActualizacion,NULL
FROM dbo.OrdenesCompra oc;';

ALTER TABLE dbo.OrdenesCompraDetalle ADD idSucursal UNIQUEIDENTIFIER NULL;
EXEC sys.sp_executesql N'
UPDATE d
SET idSucursal=oc.idSucursal
FROM dbo.OrdenesCompraDetalle d
JOIN dbo.OrdenesCompra oc ON oc.idEmpresa=d.idEmpresa AND oc.id=d.idOrdenCompra
WHERE d.idSucursal IS NULL;

IF EXISTS (SELECT 1 FROM dbo.OrdenesCompraDetalle WHERE idSucursal IS NULL)
    THROW 51055, ''OC_V5_DETAIL_BRANCH_BACKFILL_INCOMPLETE'', 1;';

EXEC sys.sp_executesql N'
ALTER TABLE dbo.OrdenesCompraDetalle ALTER COLUMN idSucursal UNIQUEIDENTIFIER NOT NULL;
ALTER TABLE dbo.OrdenesCompraDetalle ADD
    CONSTRAINT FK_OrdenesCompraDetalle_Sucursales_EmpresaId FOREIGN KEY (idEmpresa,idSucursal) REFERENCES dbo.Sucursales(idEmpresa,id),
    CONSTRAINT FK_OrdenesCompraDetalle_OCSucursales_EmpresaOrdenSucursal FOREIGN KEY (idEmpresa,idOrdenCompra,idSucursal) REFERENCES dbo.OrdenesCompraSucursales(idEmpresa,idOrdenCompra,idSucursal),
    CONSTRAINT UX_OrdenesCompraDetalle_Empresa_Id_Orden_Sucursal UNIQUE (idEmpresa,id,idOrdenCompra,idSucursal);
CREATE INDEX IX_OrdenesCompraDetalle_Empresa_Orden_Sucursal
    ON dbo.OrdenesCompraDetalle(idEmpresa,idOrdenCompra,idSucursal);';

ALTER TABLE dbo.OrdenesCompra ALTER COLUMN idSucursal UNIQUEIDENTIFIER NULL;

EXEC sys.sp_executesql N'
IF (SELECT COUNT_BIG(*) FROM dbo.OrdenesCompraSucursales)<>@HeaderCount
   OR (SELECT COUNT_BIG(*) FROM dbo.OrdenesCompraDetalle)<>@DetailCount
   OR (SELECT COALESCE(SUM(CONVERT(decimal(38,2), Subtotal)),0) FROM dbo.OrdenesCompra)<>@HeaderSubtotal
   OR (SELECT COALESCE(SUM(CONVERT(decimal(38,2), Total)),0) FROM dbo.OrdenesCompra)<>@HeaderTotal
   OR (SELECT COALESCE(SUM(CONVERT(decimal(38,4), CantidadBaseOrdenada)),0) FROM dbo.OrdenesCompraDetalle)<>@Ordered
   OR (SELECT COALESCE(SUM(CONVERT(decimal(38,4), CantidadBaseRecibidaAcumulada)),0) FROM dbo.OrdenesCompraDetalle)<>@Received
   OR (SELECT COALESCE(SUM(CONVERT(decimal(38,4), CantidadBasePendiente)),0) FROM dbo.OrdenesCompraDetalle)<>@Pending
    THROW 51056, ''OC_V5_RECONCILIATION_FAILED'', 1;
IF EXISTS (
    SELECT 1 FROM dbo.OrdenesCompraDetalle d
    LEFT JOIN dbo.OrdenesCompraSucursales os ON os.idEmpresa=d.idEmpresa AND os.idOrdenCompra=d.idOrdenCompra AND os.idSucursal=d.idSucursal
    WHERE os.id IS NULL)
    THROW 51057, ''OC_V5_DETAIL_DESTINATION_INCOMPATIBLE'', 1;',
    N'@HeaderCount bigint,@DetailCount bigint,@HeaderSubtotal decimal(38,2),@HeaderTotal decimal(38,2),@Ordered decimal(38,4),@Received decimal(38,4),@Pending decimal(38,4)',
    @HeaderCount=@HeaderCount,@DetailCount=@DetailCount,@HeaderSubtotal=@HeaderSubtotal,@HeaderTotal=@HeaderTotal,@Ordered=@Ordered,@Received=@Received,@Pending=@Pending;
";

        private static SchemaTableContract MakeHeaderV5(SchemaTableContract table)
        {
            SchemaColumnContract[] columns = table.Columns
                .Select(column => column.Name == "idSucursal" ? column with { IsNullable = true } : column)
                .ToArray();
            return table with { Columns = columns };
        }

        private static SchemaTableContract MakeDetailV5(SchemaTableContract table)
        {
            List<SchemaColumnContract> columns = table.Columns.ToList();
            int orderIndex = columns.FindIndex(column => column.Name == "idOrdenCompra");
            columns.Insert(orderIndex + 1, Col("idSucursal", "UNIQUEIDENTIFIER", false));

            return table with
            {
                Columns = columns,
                ForeignKeys = table.ForeignKeys.Concat(new[]
                {
                    new SchemaForeignKeyContract("FK_OrdenesCompraDetalle_Sucursales_EmpresaId", new[] { "idEmpresa", "idSucursal" }, "dbo", "Sucursales", new[] { "idEmpresa", "id" }),
                    new SchemaForeignKeyContract("FK_OrdenesCompraDetalle_OCSucursales_EmpresaOrdenSucursal", new[] { "idEmpresa", "idOrdenCompra", "idSucursal" }, "dbo", "OrdenesCompraSucursales", new[] { "idEmpresa", "idOrdenCompra", "idSucursal" }),
                }).ToArray(),
                UniqueConstraints = table.UniqueConstraints.Concat(new[]
                {
                    new SchemaUniqueContract("UX_OrdenesCompraDetalle_Empresa_Id_Orden_Sucursal", new[] { "idEmpresa", "id", "idOrdenCompra", "idSucursal" }, null),
                }).ToArray(),
                Indexes = table.Indexes.Concat(new[]
                {
                    Ix("IX_OrdenesCompraDetalle_Empresa_Orden_Sucursal", false, null, "idEmpresa", "idOrdenCompra", "idSucursal"),
                }).ToArray(),
            };
        }

        private static SchemaTableContract DestinationBranches() => new(
            "dbo",
            "OrdenesCompraSucursales",
            new[]
            {
                Col("id", "UNIQUEIDENTIFIER", false, def: "(NEWID())"),
                Col("idEmpresa", "UNIQUEIDENTIFIER", false),
                Col("identityKey", "UNIQUEIDENTIFIER", false, def: "(NEWID())"),
                Col("idOrdenCompra", "UNIQUEIDENTIFIER", false),
                Col("idSucursal", "UNIQUEIDENTIFIER", false),
                Col("Activo", "BIT", false, def: "((1))"),
                Col("FechaCreacion", "DATETIME2(0)", false, scale: 0, def: "(SYSUTCDATETIME())"),
                Col("FechaActualizacion", "DATETIME2(0)", false, scale: 0, def: "(SYSUTCDATETIME())"),
                Col("FechaArchivado", "DATETIME2(0)", true, scale: 0),
                Col("idUsuarioCreacion", "UNIQUEIDENTIFIER", true),
                Col("idUsuarioActualizacion", "UNIQUEIDENTIFIER", true),
                Col("CorrelationId", "UNIQUEIDENTIFIER", true),
            },
            new SchemaPrimaryKeyContract("PK_OrdenesCompraSucursales", new[] { "id" }, true),
            new[]
            {
                new SchemaForeignKeyContract("FK_OrdenesCompraSucursales_OrdenesCompra_EmpresaId", new[] { "idEmpresa", "idOrdenCompra" }, "dbo", "OrdenesCompra", new[] { "idEmpresa", "id" }),
                new SchemaForeignKeyContract("FK_OrdenesCompraSucursales_Sucursales_EmpresaId", new[] { "idEmpresa", "idSucursal" }, "dbo", "Sucursales", new[] { "idEmpresa", "id" }),
            },
            new[]
            {
                new SchemaUniqueContract("UX_OrdenesCompraSucursales_Empresa_Id", new[] { "idEmpresa", "id" }, null),
                new SchemaUniqueContract("UX_OrdenesCompraSucursales_Empresa_Orden_Sucursal", new[] { "idEmpresa", "idOrdenCompra", "idSucursal" }, null),
            },
            new[]
            {
                new SchemaCheckContract("CK_OrdenesCompraSucursales_Archivado", "CHECK ((Activo = 1 AND FechaArchivado IS NULL) OR (Activo = 0 AND FechaArchivado IS NOT NULL))"),
            },
            new[]
            {
                Ix("IX_OrdenesCompraSucursales_Empresa_Sucursal_Orden", false, null, "idEmpresa", "idSucursal", "idOrdenCompra"),
                Ix("IX_OrdenesCompraSucursales_Empresa_Orden_Activo", false, null, "idEmpresa", "idOrdenCompra", "Activo"),
                Ix("IX_OrdenesCompraSucursales_Empresa_Correlation", false, "CorrelationId IS NOT NULL", "idEmpresa", "CorrelationId"),
            });

        private static SchemaColumnContract Col(
            string name,
            string sqlType,
            bool nullable,
            int? max = null,
            byte? precision = null,
            int? scale = null,
            string? def = null)
            => new(name, sqlType, max, precision, scale, nullable, def, false, false, null);

        private static SchemaIndexContract Ix(string name, bool unique, string? filter, params string[] columns)
            => new(name, unique, false, columns.Select(column => new SchemaIndexColumnContract(column, false)).ToArray(), Array.Empty<string>(), filter);
    }
}
