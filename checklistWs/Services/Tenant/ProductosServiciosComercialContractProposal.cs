namespace checklistWs.Services.Tenant
{
    /// <summary>
    /// Contrato V3 y DDL local aprobado en LP-QA05S1. El paquete queda preparado
    /// con la infraestructura oficial, pero no sustituye el release V2 activo.
    /// </summary>
    public static class ProductosServiciosComercialContractProposal
    {
        public const int SourceVersion = ProductosServiciosSchemaContractProvider.V2;
        public const int TargetVersion = ProductosServiciosSchemaContractProvider.V3;
        public const string MigrationId = "PS-M20261005-V2-V3-IDENTIDAD-COMERCIAL";
        public const string ReviewStatus = "APROBADO_EJECUCION_SQL_LP_QA05S3";

        public static SchemaContract CreateTargetContract(ISchemaContractProvider provider)
        {
            SchemaContract source = provider.GetContract(DatabaseScopes.ProductosServicios, SourceVersion);
            List<SchemaTableContract> tables = source.Tables.ToList();
            tables.Add(IdentidadComercial());
            tables.Add(IdentidadComercialHistorial());

            return new SchemaContract(
                DatabaseScopes.ProductosServicios,
                TargetVersion,
                source.Product,
                tables,
                source.Sources.Concat(new[]
                {
                    "inspector/docs/lista-precios/LP_QA05S_CONTRATO_DATOS_ADICIONALES_PROMOCIONES_20261005.md",
                    "inspector/docs/lista-precios/LP_QA05S1_MIGRACION_PRODUCTOSSERVICIOS_V3_20261005.md",
                    "inspector/docs/lista-precios/LP_QA05S2_CERTIFICACION_SQL_PRODUCTOSSERVICIOS_V3_20261005.md"
                }).ToArray(),
                new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc));
        }

        public static SchemaContractProposal Create(
            ISchemaContractProvider provider,
            ISchemaManifestProvider manifestProvider)
        {
            SchemaContract source = provider.GetContract(DatabaseScopes.ProductosServicios, SourceVersion);
            SchemaContract target = CreateTargetContract(provider);

            return new SchemaContractProposal(
                MigrationId,
                ReviewStatus,
                source,
                target,
                manifestProvider.CreateManifest(source).ManifestHash,
                manifestProvider.CreateManifest(target).ManifestHash,
                BuildV2ToV3Preconditions(),
                BuildV2ToV3DataPreconditions(),
                "SingleTransaction",
                "Drop only empty LP-QA05S objects before runtime adoption; after adoption use a forward corrective migration",
                true,
                BuildV2ToV3Sql());
        }

        public static IReadOnlyCollection<string> BuildV2ToV3Preconditions() => new[]
        {
            "SOURCE_CONTRACT_EXACT:ProductosServicios:2",
            "TARGET_TABLES_BOTH_ABSENT",
            "REJECT_PARTIAL_TARGET_OBJECTS",
            "REJECT_V2_WITH_ANY_V3_OBJECT",
            "REJECT_DRIFT_OR_FUTURE_VERSION",
            "NO_DML_BUSINESS_ROWS",
            "NO_ACTIVE_DUPLICATE_SELLABLE_IDENTITY"
        };

        public static IReadOnlyCollection<string> BuildV2ToV3DataPreconditions() => new[]
        {
            "NO_BUSINESS_DATA_BACKFILL",
            "NO_CHANGE_TO_PRODUCTOSSERVICIOS_DESCRIPCION",
            "NO_CHANGE_TO_LISTAPRECIOSPROMOCIONES",
            "HISTORICAL_ABSENCE_MEANS_NO_EXTENSION_ROW",
            "NEW_FLAGS_DEFAULT_FALSE_ONLY_ON_NEW_ROW"
        };

        public static string BuildV2ToV3Sql() => @"
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.ProductosServiciosIdentidadComercial', N'U') IS NOT NULL
   OR OBJECT_ID(N'dbo.ProductosServiciosIdentidadComercialHistorial', N'U') IS NOT NULL
BEGIN
    THROW 51000, 'PS_V3_TARGET_OBJECTS_ALREADY_EXIST_REQUIRES_REVIEW', 1;
END;

CREATE TABLE dbo.ProductosServiciosIdentidadComercial
(
    id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_ProductosServiciosIdentidadComercial_id DEFAULT (NEWID()),
    idEmpresa UNIQUEIDENTIFIER NOT NULL,
    identityKey UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_ProductosServiciosIdentidadComercial_identityKey DEFAULT (NEWID()),
    TipoIdentidad TINYINT NOT NULL,
    idProductoServicio UNIQUEIDENTIFIER NOT NULL,
    TipoProductoServicio TINYINT NOT NULL,
    idVariante UNIQUEIDENTIFIER NULL,
    idPresentacionVenta UNIQUEIDENTIFIER NULL,
    Web NVARCHAR(250) NULL,
    Liverpool NVARCHAR(MAX) NULL,
    MercadoLibre NVARCHAR(MAX) NULL,
    Observaciones NVARCHAR(MAX) NULL,
    DosPorUno BIT NOT NULL CONSTRAINT DF_ProductosServiciosIdentidadComercial_DosPorUno DEFAULT ((0)),
    TresPorDos BIT NOT NULL CONSTRAINT DF_ProductosServiciosIdentidadComercial_TresPorDos DEFAULT ((0)),
    DescuentoSegundo BIT NOT NULL CONSTRAINT DF_ProductosServiciosIdentidadComercial_DescuentoSegundo DEFAULT ((0)),
    Monedero BIT NOT NULL CONSTRAINT DF_ProductosServiciosIdentidadComercial_Monedero DEFAULT ((0)),
    Activo BIT NOT NULL CONSTRAINT DF_ProductosServiciosIdentidadComercial_Activo DEFAULT ((1)),
    FechaCreacion DATETIME2(0) NOT NULL CONSTRAINT DF_ProductosServiciosIdentidadComercial_FechaCreacion DEFAULT (SYSUTCDATETIME()),
    FechaActualizacion DATETIME2(0) NOT NULL CONSTRAINT DF_ProductosServiciosIdentidadComercial_FechaActualizacion DEFAULT (SYSUTCDATETIME()),
    FechaArchivado DATETIME2(0) NULL,
    idUsuarioCreacion UNIQUEIDENTIFIER NULL,
    idUsuarioActualizacion UNIQUEIDENTIFIER NULL,
    idUsuarioArchivado UNIQUEIDENTIFIER NULL,
    CONSTRAINT PK_ProductosServiciosIdentidadComercial PRIMARY KEY CLUSTERED (id),
    CONSTRAINT CK_PSIdentidadComercial_Identidad CHECK ((TipoIdentidad = 1 AND TipoProductoServicio = 1 AND idVariante IS NULL AND idPresentacionVenta IS NULL) OR (TipoIdentidad = 2 AND TipoProductoServicio = 2 AND idVariante IS NULL AND idPresentacionVenta IS NULL) OR (TipoIdentidad = 3 AND TipoProductoServicio = 1 AND idVariante IS NOT NULL AND idPresentacionVenta IS NULL) OR (TipoIdentidad = 4 AND TipoProductoServicio = 1 AND idVariante IS NULL AND idPresentacionVenta IS NOT NULL)),
    CONSTRAINT CK_PSIdentidadComercial_TipoProductoServicio CHECK (TipoProductoServicio IN (1, 2)),
    CONSTRAINT CK_PSIdentidadComercial_Archivado CHECK ((Activo = 1 AND FechaArchivado IS NULL AND idUsuarioArchivado IS NULL) OR (Activo = 0 AND FechaArchivado IS NOT NULL)),
    CONSTRAINT FK_ProductosServiciosIdentidadComercial_ProductosServicios_EmpresaId FOREIGN KEY (idEmpresa, idProductoServicio) REFERENCES dbo.ProductosServicios (idEmpresa, id),
    CONSTRAINT FK_ProductosServiciosIdentidadComercial_Variantes_EmpresaId FOREIGN KEY (idEmpresa, idVariante) REFERENCES dbo.ProductosServiciosVariantes (idEmpresa, id),
    CONSTRAINT FK_ProductosServiciosIdentidadComercial_Presentaciones_EmpresaId FOREIGN KEY (idEmpresa, idPresentacionVenta) REFERENCES dbo.ProductosServiciosPresentacionesVenta (idEmpresa, id)
);

CREATE UNIQUE NONCLUSTERED INDEX UX_PSIdentidadComercial_Empresa_Id ON dbo.ProductosServiciosIdentidadComercial (idEmpresa, id);
CREATE UNIQUE NONCLUSTERED INDEX UX_PSIdentidadComercial_Empresa_Producto_Activo ON dbo.ProductosServiciosIdentidadComercial (idEmpresa, idProductoServicio) WHERE Activo = 1 AND FechaArchivado IS NULL AND TipoIdentidad = 1;
CREATE UNIQUE NONCLUSTERED INDEX UX_PSIdentidadComercial_Empresa_Servicio_Activo ON dbo.ProductosServiciosIdentidadComercial (idEmpresa, idProductoServicio) WHERE Activo = 1 AND FechaArchivado IS NULL AND TipoIdentidad = 2;
CREATE UNIQUE NONCLUSTERED INDEX UX_PSIdentidadComercial_Empresa_Variante_Activa ON dbo.ProductosServiciosIdentidadComercial (idEmpresa, idVariante) WHERE Activo = 1 AND FechaArchivado IS NULL AND TipoIdentidad = 3;
CREATE UNIQUE NONCLUSTERED INDEX UX_PSIdentidadComercial_Empresa_Presentacion_Activa ON dbo.ProductosServiciosIdentidadComercial (idEmpresa, idPresentacionVenta) WHERE Activo = 1 AND FechaArchivado IS NULL AND TipoIdentidad = 4;
CREATE NONCLUSTERED INDEX IX_PSIdentidadComercial_Empresa_Producto ON dbo.ProductosServiciosIdentidadComercial (idEmpresa, idProductoServicio);
CREATE NONCLUSTERED INDEX IX_PSIdentidadComercial_Empresa_Variante ON dbo.ProductosServiciosIdentidadComercial (idEmpresa, idVariante) WHERE idVariante IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_PSIdentidadComercial_Empresa_Presentacion ON dbo.ProductosServiciosIdentidadComercial (idEmpresa, idPresentacionVenta) WHERE idPresentacionVenta IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_PSIdentidadComercial_Empresa_Activo ON dbo.ProductosServiciosIdentidadComercial (idEmpresa, Activo);

CREATE TABLE dbo.ProductosServiciosIdentidadComercialHistorial
(
    id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_PSIdentidadComercialHistorial_id DEFAULT (NEWID()),
    idEmpresa UNIQUEIDENTIFIER NOT NULL,
    TipoIdentidad TINYINT NOT NULL,
    idProductoServicio UNIQUEIDENTIFIER NOT NULL,
    TipoProductoServicio TINYINT NOT NULL,
    idVariante UNIQUEIDENTIFIER NULL,
    idPresentacionVenta UNIQUEIDENTIFIER NULL,
    Campo NVARCHAR(60) NOT NULL,
    Operacion NVARCHAR(20) NOT NULL,
    ValorAnterior NVARCHAR(MAX) NULL,
    ValorNuevo NVARCHAR(MAX) NULL,
    idUsuario UNIQUEIDENTIFIER NULL,
    Usuario NVARCHAR(256) NULL,
    CorrelationId UNIQUEIDENTIFIER NOT NULL,
    Origen NVARCHAR(40) NOT NULL,
    FechaUtc DATETIME2(3) NOT NULL CONSTRAINT DF_PSIdentidadComercialHistorial_FechaUtc DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_PSIdentidadComercialHistorial PRIMARY KEY CLUSTERED (id),
    CONSTRAINT CK_PSIdentidadComercialHistorial_Identidad CHECK ((TipoIdentidad = 1 AND TipoProductoServicio = 1 AND idVariante IS NULL AND idPresentacionVenta IS NULL) OR (TipoIdentidad = 2 AND TipoProductoServicio = 2 AND idVariante IS NULL AND idPresentacionVenta IS NULL) OR (TipoIdentidad = 3 AND TipoProductoServicio = 1 AND idVariante IS NOT NULL AND idPresentacionVenta IS NULL) OR (TipoIdentidad = 4 AND TipoProductoServicio = 1 AND idVariante IS NULL AND idPresentacionVenta IS NOT NULL)),
    CONSTRAINT CK_PSIdentidadComercialHistorial_TipoProductoServicio CHECK (TipoProductoServicio IN (1, 2)),
    CONSTRAINT CK_PSIdentidadComercialHistorial_Campo CHECK (Campo IN (N'Descripcion', N'Web', N'Liverpool', N'MercadoLibre', N'Observaciones', N'DosPorUno', N'TresPorDos', N'DescuentoSegundo', N'Monedero', N'Activo')),
    CONSTRAINT CK_PSIdentidadComercialHistorial_Operacion CHECK (Operacion IN (N'INSERT', N'UPDATE', N'ARCHIVE', N'REACTIVATE')),
    CONSTRAINT CK_PSIdentidadComercialHistorial_Origen CHECK (Origen IN (N'LISTA_PRECIOS', N'PRODUCTOS_SERVICIOS')),
    CONSTRAINT FK_PSIdentidadComercialHistorial_ProductosServicios_EmpresaId FOREIGN KEY (idEmpresa, idProductoServicio) REFERENCES dbo.ProductosServicios (idEmpresa, id),
    CONSTRAINT FK_PSIdentidadComercialHistorial_Variantes_EmpresaId FOREIGN KEY (idEmpresa, idVariante) REFERENCES dbo.ProductosServiciosVariantes (idEmpresa, id),
    CONSTRAINT FK_PSIdentidadComercialHistorial_Presentaciones_EmpresaId FOREIGN KEY (idEmpresa, idPresentacionVenta) REFERENCES dbo.ProductosServiciosPresentacionesVenta (idEmpresa, id)
);

CREATE UNIQUE NONCLUSTERED INDEX UX_PSIdentidadComercialHistorial_Empresa_Id ON dbo.ProductosServiciosIdentidadComercialHistorial (idEmpresa, id);
CREATE NONCLUSTERED INDEX IX_PSIdentidadComercialHistorial_Empresa_Identidad_Fecha ON dbo.ProductosServiciosIdentidadComercialHistorial (idEmpresa, TipoIdentidad, idProductoServicio, idVariante, idPresentacionVenta, FechaUtc);
CREATE NONCLUSTERED INDEX IX_PSIdentidadComercialHistorial_Empresa_Correlation ON dbo.ProductosServiciosIdentidadComercialHistorial (idEmpresa, CorrelationId);";

        private static SchemaTableContract IdentidadComercial() => new(
            "dbo",
            "ProductosServiciosIdentidadComercial",
            new[]
            {
                Col("id", "UNIQUEIDENTIFIER", false, def: "(NEWID())"),
                Col("idEmpresa", "UNIQUEIDENTIFIER", false),
                Col("identityKey", "UNIQUEIDENTIFIER", false, def: "(NEWID())"),
                Col("TipoIdentidad", "TINYINT", false),
                Col("idProductoServicio", "UNIQUEIDENTIFIER", false),
                Col("TipoProductoServicio", "TINYINT", false),
                Col("idVariante", "UNIQUEIDENTIFIER", true),
                Col("idPresentacionVenta", "UNIQUEIDENTIFIER", true),
                Col("Web", "NVARCHAR(250)", true, max: 250),
                Col("Liverpool", "NVARCHAR(MAX)", true, max: -1),
                Col("MercadoLibre", "NVARCHAR(MAX)", true, max: -1),
                Col("Observaciones", "NVARCHAR(MAX)", true, max: -1),
                Col("DosPorUno", "BIT", false, def: "((0))"),
                Col("TresPorDos", "BIT", false, def: "((0))"),
                Col("DescuentoSegundo", "BIT", false, def: "((0))"),
                Col("Monedero", "BIT", false, def: "((0))"),
                Col("Activo", "BIT", false, def: "((1))"),
                Col("FechaCreacion", "DATETIME2(0)", false, scale: 0, def: "(SYSUTCDATETIME())"),
                Col("FechaActualizacion", "DATETIME2(0)", false, scale: 0, def: "(SYSUTCDATETIME())"),
                Col("FechaArchivado", "DATETIME2(0)", true, scale: 0),
                Col("idUsuarioCreacion", "UNIQUEIDENTIFIER", true),
                Col("idUsuarioActualizacion", "UNIQUEIDENTIFIER", true),
                Col("idUsuarioArchivado", "UNIQUEIDENTIFIER", true),
            },
            new SchemaPrimaryKeyContract("PK_ProductosServiciosIdentidadComercial", new[] { "id" }, true),
            IdentityForeignKeys("ProductosServiciosIdentidadComercial"),
            new[]
            {
                Ux("UX_PSIdentidadComercial_Empresa_Id", null, "idEmpresa", "id"),
                Ux("UX_PSIdentidadComercial_Empresa_Producto_Activo", "Activo = 1 AND FechaArchivado IS NULL AND TipoIdentidad = 1", "idEmpresa", "idProductoServicio"),
                Ux("UX_PSIdentidadComercial_Empresa_Servicio_Activo", "Activo = 1 AND FechaArchivado IS NULL AND TipoIdentidad = 2", "idEmpresa", "idProductoServicio"),
                Ux("UX_PSIdentidadComercial_Empresa_Variante_Activa", "Activo = 1 AND FechaArchivado IS NULL AND TipoIdentidad = 3", "idEmpresa", "idVariante"),
                Ux("UX_PSIdentidadComercial_Empresa_Presentacion_Activa", "Activo = 1 AND FechaArchivado IS NULL AND TipoIdentidad = 4", "idEmpresa", "idPresentacionVenta"),
            },
            new[]
            {
                IdentityCheck("CK_PSIdentidadComercial_Identidad"),
                new SchemaCheckContract("CK_PSIdentidadComercial_TipoProductoServicio", "CHECK (TipoProductoServicio IN (1, 2))"),
                new SchemaCheckContract("CK_PSIdentidadComercial_Archivado", "CHECK ((Activo = 1 AND FechaArchivado IS NULL AND idUsuarioArchivado IS NULL) OR (Activo = 0 AND FechaArchivado IS NOT NULL))"),
            },
            new[]
            {
                Ix("UX_PSIdentidadComercial_Empresa_Id", true, null, "idEmpresa", "id"),
                Ix("UX_PSIdentidadComercial_Empresa_Producto_Activo", true, "Activo = 1 AND FechaArchivado IS NULL AND TipoIdentidad = 1", "idEmpresa", "idProductoServicio"),
                Ix("UX_PSIdentidadComercial_Empresa_Servicio_Activo", true, "Activo = 1 AND FechaArchivado IS NULL AND TipoIdentidad = 2", "idEmpresa", "idProductoServicio"),
                Ix("UX_PSIdentidadComercial_Empresa_Variante_Activa", true, "Activo = 1 AND FechaArchivado IS NULL AND TipoIdentidad = 3", "idEmpresa", "idVariante"),
                Ix("UX_PSIdentidadComercial_Empresa_Presentacion_Activa", true, "Activo = 1 AND FechaArchivado IS NULL AND TipoIdentidad = 4", "idEmpresa", "idPresentacionVenta"),
                Ix("IX_PSIdentidadComercial_Empresa_Producto", false, null, "idEmpresa", "idProductoServicio"),
                Ix("IX_PSIdentidadComercial_Empresa_Variante", false, "idVariante IS NOT NULL", "idEmpresa", "idVariante"),
                Ix("IX_PSIdentidadComercial_Empresa_Presentacion", false, "idPresentacionVenta IS NOT NULL", "idEmpresa", "idPresentacionVenta"),
                Ix("IX_PSIdentidadComercial_Empresa_Activo", false, null, "idEmpresa", "Activo"),
            });

        private static SchemaTableContract IdentidadComercialHistorial() => new(
            "dbo",
            "ProductosServiciosIdentidadComercialHistorial",
            new[]
            {
                Col("id", "UNIQUEIDENTIFIER", false, def: "(NEWID())"),
                Col("idEmpresa", "UNIQUEIDENTIFIER", false),
                Col("TipoIdentidad", "TINYINT", false),
                Col("idProductoServicio", "UNIQUEIDENTIFIER", false),
                Col("TipoProductoServicio", "TINYINT", false),
                Col("idVariante", "UNIQUEIDENTIFIER", true),
                Col("idPresentacionVenta", "UNIQUEIDENTIFIER", true),
                Col("Campo", "NVARCHAR(60)", false, max: 60),
                Col("Operacion", "NVARCHAR(20)", false, max: 20),
                Col("ValorAnterior", "NVARCHAR(MAX)", true, max: -1),
                Col("ValorNuevo", "NVARCHAR(MAX)", true, max: -1),
                Col("idUsuario", "UNIQUEIDENTIFIER", true),
                Col("Usuario", "NVARCHAR(256)", true, max: 256),
                Col("CorrelationId", "UNIQUEIDENTIFIER", false),
                Col("Origen", "NVARCHAR(40)", false, max: 40),
                Col("FechaUtc", "DATETIME2(3)", false, scale: 3, def: "(SYSUTCDATETIME())"),
            },
            new SchemaPrimaryKeyContract("PK_PSIdentidadComercialHistorial", new[] { "id" }, true),
            IdentityForeignKeys("PSIdentidadComercialHistorial"),
            new[]
            {
                Ux("UX_PSIdentidadComercialHistorial_Empresa_Id", null, "idEmpresa", "id"),
            },
            new[]
            {
                IdentityCheck("CK_PSIdentidadComercialHistorial_Identidad"),
                new SchemaCheckContract("CK_PSIdentidadComercialHistorial_TipoProductoServicio", "CHECK (TipoProductoServicio IN (1, 2))"),
                new SchemaCheckContract("CK_PSIdentidadComercialHistorial_Campo", "CHECK (Campo IN (N'Descripcion', N'Web', N'Liverpool', N'MercadoLibre', N'Observaciones', N'DosPorUno', N'TresPorDos', N'DescuentoSegundo', N'Monedero', N'Activo'))"),
                new SchemaCheckContract("CK_PSIdentidadComercialHistorial_Operacion", "CHECK (Operacion IN (N'INSERT', N'UPDATE', N'ARCHIVE', N'REACTIVATE'))"),
                new SchemaCheckContract("CK_PSIdentidadComercialHistorial_Origen", "CHECK (Origen IN (N'LISTA_PRECIOS', N'PRODUCTOS_SERVICIOS'))"),
            },
            new[]
            {
                Ix("UX_PSIdentidadComercialHistorial_Empresa_Id", true, null, "idEmpresa", "id"),
                Ix("IX_PSIdentidadComercialHistorial_Empresa_Identidad_Fecha", false, null, "idEmpresa", "TipoIdentidad", "idProductoServicio", "idVariante", "idPresentacionVenta", "FechaUtc"),
                Ix("IX_PSIdentidadComercialHistorial_Empresa_Correlation", false, null, "idEmpresa", "CorrelationId"),
            });

        private static SchemaForeignKeyContract[] IdentityForeignKeys(string owner) => new[]
        {
            new SchemaForeignKeyContract($"FK_{owner}_ProductosServicios_EmpresaId", new[] { "idEmpresa", "idProductoServicio" }, "dbo", "ProductosServicios", new[] { "idEmpresa", "id" }),
            new SchemaForeignKeyContract($"FK_{owner}_Variantes_EmpresaId", new[] { "idEmpresa", "idVariante" }, "dbo", "ProductosServiciosVariantes", new[] { "idEmpresa", "id" }),
            new SchemaForeignKeyContract($"FK_{owner}_Presentaciones_EmpresaId", new[] { "idEmpresa", "idPresentacionVenta" }, "dbo", "ProductosServiciosPresentacionesVenta", new[] { "idEmpresa", "id" }),
        };

        private static SchemaCheckContract IdentityCheck(string name) => new(
            name,
            "CHECK ((TipoIdentidad = 1 AND TipoProductoServicio = 1 AND idVariante IS NULL AND idPresentacionVenta IS NULL) OR (TipoIdentidad = 2 AND TipoProductoServicio = 2 AND idVariante IS NULL AND idPresentacionVenta IS NULL) OR (TipoIdentidad = 3 AND TipoProductoServicio = 1 AND idVariante IS NOT NULL AND idPresentacionVenta IS NULL) OR (TipoIdentidad = 4 AND TipoProductoServicio = 1 AND idVariante IS NULL AND idPresentacionVenta IS NOT NULL))");

        private static SchemaColumnContract Col(
            string name,
            string sqlType,
            bool nullable,
            int? max = null,
            byte? precision = null,
            int? scale = null,
            string? def = null)
            => new(name, sqlType, max, precision, scale, nullable, def, false, false, null);

        private static SchemaUniqueContract Ux(string name, string? filter, params string[] columns)
            => new(name, columns, filter);

        private static SchemaIndexContract Ix(string name, bool unique, string? filter, params string[] columns)
            => new(name, unique, false, columns.Select(column => new SchemaIndexColumnContract(column, false)).ToArray(), Array.Empty<string>(), filter);
    }

    public sealed record SchemaContractProposal(
        string MigrationId,
        string ReviewStatus,
        SchemaContract SourceContract,
        SchemaContract TargetContract,
        string SourceManifestHash,
        string TargetManifestHash,
        IReadOnlyCollection<string> Preconditions,
        IReadOnlyCollection<string> DataPreconditions,
        string TransactionMode,
        string RollbackPolicy,
        bool ApprovedForExecution,
        string? ExecutableSql);
}
