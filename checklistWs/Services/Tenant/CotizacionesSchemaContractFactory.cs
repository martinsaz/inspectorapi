namespace checklistWs.Services.Tenant
{
    internal static class CotizacionesSchemaContractFactory
    {
        internal const string SupersededV2ManifestHash = "e936afda626b83bafbc3c09091f721c94bffaa0957ef96f00fad9c0a38c1c8da";

        public static SchemaContract GetContract(int version)
        {
            if (version is not 1 and not 2)
            {
                throw new InvalidOperationException("SCHEMA_CONTRACT_VERSION_NOT_SUPPORTED");
            }

            return new SchemaContract(
                DatabaseScopes.Cotizaciones,
                version,
                "Cotizaciones",
                version == 1 ? BuildV1Tables() : BuildV2Tables(),
                new[] { version == 1 ? "COTIZACIONES_V1_HISTORICAL_BASELINE" : "COT-M20260930-V1-V2-LP08-SNAPSHOT" },
                new DateTime(2026, 9, 30, 0, 0, 0, DateTimeKind.Utc));
        }

        internal static SchemaContract GetSupersededV2Contract() => new(
            DatabaseScopes.Cotizaciones,
            2,
            "Cotizaciones",
            BuildV2Tables(includeHistoricalObjects: false),
            new[] { "COT-M20260930-V1-V2-LP08-SNAPSHOT" },
            new DateTime(2026, 9, 30, 0, 0, 0, DateTimeKind.Utc));

        private static IReadOnlyCollection<SchemaTableContract> BuildV1Tables()
        {
            return new[] { BuildCotizacionesV1(), BuildPartidasV1() };
        }

        private static IReadOnlyCollection<SchemaTableContract> BuildV2Tables(bool includeHistoricalObjects = true)
        {
            return new[] { BuildCotizacionesV2(includeHistoricalObjects), BuildPartidasV2(includeHistoricalObjects), BuildHistorialV2() };
        }

        private static SchemaTableContract BuildCotizacionesV1(bool includeHistoricalObjects = true) => new(
            "dbo",
            "Cotizaciones",
            new[]
            {
                Col("id", "UNIQUEIDENTIFIER"),
                Col("idEmpresa", "UNIQUEIDENTIFIER"),
                Col("identityKey", "UNIQUEIDENTIFIER"),
                Col("Folio", "NVARCHAR(30)", 30),
                Col("Estado", "TINYINT"),
                Col("FechaCotizacion", "DATETIME2(0)", scale: 0),
                Col("VigenciaDias", "INT", defaultDefinition: "((0))"),
                Col("FechaVigencia", "DATETIME2(0)", scale: 0, nullable: true),
                Col("idCliente", "UNIQUEIDENTIFIER"),
                Col("idSucursal", "UNIQUEIDENTIFIER", nullable: true),
                Col("Vendedor", "NVARCHAR(200)", 200, defaultDefinition: "(N'')"),
                Col("Caja", "NVARCHAR(100)", 100, defaultDefinition: "(N'')"),
                Col("Observaciones", "NVARCHAR(1000)", 1000, defaultDefinition: "(N'')"),
                Decimal("Subtotal", nullable: false, defaultDefinition: "((0))"),
                Decimal("DescuentoTotal", nullable: false, defaultDefinition: "((0))"),
                Decimal("Total", nullable: false, defaultDefinition: "((0))"),
                Decimal("TotalPiezas", nullable: false, defaultDefinition: "((0))"),
                Col("MotivoCancelacion", "NVARCHAR(500)", 500, defaultDefinition: "(N'')"),
                Col("FechaCancelacion", "DATETIME2(0)", scale: 0, nullable: true),
                Col("idUsuarioCreacion", "UNIQUEIDENTIFIER", nullable: true),
                Col("idUsuarioActualizacion", "UNIQUEIDENTIFIER", nullable: true),
                Col("idUsuarioCancelacion", "UNIQUEIDENTIFIER", nullable: true),
                Col("FechaCreacion", "DATETIME2(0)", scale: 0),
                Col("FechaActualizacion", "DATETIME2(0)", scale: 0),
                Col("FechaArchivado", "DATETIME2(0)", scale: 0, nullable: true),
                Col("Activo", "BIT", defaultDefinition: "((1))")
            },
            new SchemaPrimaryKeyContract("PK_Cotizaciones", new[] { "id" }, true),
            Array.Empty<SchemaForeignKeyContract>(),
            Array.Empty<SchemaUniqueContract>(),
            Array.Empty<SchemaCheckContract>(),
            BuildCotizacionesV1Indexes(includeHistoricalObjects));

        private static SchemaTableContract BuildPartidasV1(bool includeHistoricalObjects = true) => new(
            "dbo",
            "CotizacionesPartidas",
            new[]
            {
                Col("id", "UNIQUEIDENTIFIER"),
                Col("idCotizacion", "UNIQUEIDENTIFIER"),
                Col("idEmpresa", "UNIQUEIDENTIFIER"),
                Col("identityKey", "UNIQUEIDENTIFIER"),
                Col("NumeroPartida", "INT"),
                Col("idProductoServicio", "UNIQUEIDENTIFIER"),
                Col("Codigo", "NVARCHAR(50)", 50),
                Col("Nombre", "NVARCHAR(200)", 200),
                Col("Descripcion", "NVARCHAR(1000)", 1000, defaultDefinition: "(N'')"),
                Col("TipoProductoServicio", "TINYINT"),
                Col("idUnidadMedida", "UNIQUEIDENTIFIER"),
                Col("UnidadMedida", "NVARCHAR(100)", 100),
                Col("UnidadAbreviatura", "NVARCHAR(30)", 30, defaultDefinition: "(N'')"),
                Col("UnidadPermiteDecimales", "BIT", defaultDefinition: "((0))"),
                Col("PermiteVentaSinExistencia", "BIT", defaultDefinition: "((0))"),
                Decimal("ExistenciaActual", nullable: true),
                Decimal("Cantidad"),
                Decimal("PrecioUnitario"),
                Decimal("DescuentoPct", precision: 9, scale: 2, defaultDefinition: "((0))"),
                Decimal("ImporteBruto"),
                Decimal("DescuentoImporte", defaultDefinition: "((0))"),
                Decimal("Total"),
                Col("FechaCreacion", "DATETIME2(0)", scale: 0),
                Col("FechaActualizacion", "DATETIME2(0)", scale: 0),
                Col("FechaArchivado", "DATETIME2(0)", scale: 0, nullable: true),
                Col("Activo", "BIT", defaultDefinition: "((1))")
            },
            new SchemaPrimaryKeyContract("PK_CotizacionesPartidas", new[] { "id" }, true),
            includeHistoricalObjects
                ? new[] { new SchemaForeignKeyContract("FK_CotizacionesPartidas_Cotizaciones", new[] { "idCotizacion" }, "dbo", "Cotizaciones", new[] { "id" }) }
                : Array.Empty<SchemaForeignKeyContract>(),
            Array.Empty<SchemaUniqueContract>(),
            Array.Empty<SchemaCheckContract>(),
            new[] { Ix("IX_CotizacionesPartidas_Cotizacion_Numero", false, ("idCotizacion", false), ("NumeroPartida", false), ("Activo", false)) });

        private static IReadOnlyCollection<SchemaIndexContract> BuildCotizacionesV1Indexes(bool includeHistoricalObjects)
        {
            var indexes = new List<SchemaIndexContract>
            {
                Ix("UX_Cotizaciones_Empresa_Folio", true, ("idEmpresa", false), ("Folio", false)),
                Ix("IX_Cotizaciones_Empresa_Fecha_Estado", false, ("idEmpresa", false), ("FechaCotizacion", true), ("Estado", false), ("Activo", false))
            };
            if (includeHistoricalObjects)
            {
                indexes.Add(Ix("IX_Cotizaciones_Empresa_Cliente", false, ("idEmpresa", false), ("idCliente", false), ("Activo", false)));
            }

            return indexes;
        }

        private static SchemaTableContract BuildCotizacionesV2(bool includeHistoricalObjects = true)
        {
            SchemaTableContract v1 = BuildCotizacionesV1(includeHistoricalObjects);
            return v1 with
            {
                Columns = v1.Columns.Concat(new[]
                {
                    Col("idListaPrecio", "UNIQUEIDENTIFIER", nullable: true),
                    Col("ListaPrecioNivel", "TINYINT", nullable: true),
                    Col("idCotizacionOrigen", "UNIQUEIDENTIFIER", nullable: true)
                }).ToArray(),
                ForeignKeys = new[]
                {
                    new SchemaForeignKeyContract("FK_Cotizaciones_ListaPreciosListas_EmpresaId", new[] { "idEmpresa", "idListaPrecio" }, "dbo", "ListaPreciosListas", new[] { "idEmpresa", "id" }),
                    new SchemaForeignKeyContract("FK_Cotizaciones_Origen_EmpresaId", new[] { "idEmpresa", "idCotizacionOrigen" }, "dbo", "Cotizaciones", new[] { "idEmpresa", "id" })
                },
                CheckConstraints = new[]
                {
                    new SchemaCheckContract("CK_Cotizaciones_ListaPrecio", "CHECK ((idListaPrecio IS NULL AND ListaPrecioNivel IS NULL) OR (idListaPrecio IS NOT NULL AND ListaPrecioNivel BETWEEN 1 AND 10))")
                },
                Indexes = v1.Indexes.Concat(new[]
                {
                    Ix("UX_Cotizaciones_Empresa_Id", true, ("idEmpresa", false), ("id", false)),
                    Ix("IX_Cotizaciones_Empresa_Lista_Activo", false, ("idEmpresa", false), ("idListaPrecio", false), ("Activo", false))
                }).ToArray()
            };
        }

        private static SchemaTableContract BuildPartidasV2(bool includeHistoricalObjects = true)
        {
            SchemaTableContract v1 = BuildPartidasV1(includeHistoricalObjects);
            return v1 with
            {
                Columns = v1.Columns.Concat(new[]
                {
                    Col("TipoIdentidad", "TINYINT", nullable: true),
                    Col("idVariante", "UNIQUEIDENTIFIER", nullable: true),
                    Col("idPresentacionVenta", "UNIQUEIDENTIFIER", nullable: true),
                    Col("idListaPrecio", "UNIQUEIDENTIFIER", nullable: true),
                    Col("ListaPrecioNivel", "TINYINT", nullable: true),
                    Decimal("PrecioBase", nullable: true),
                    Decimal("PrecioLista", nullable: true),
                    Col("OrigenPrecio", "NVARCHAR(50)", 50, nullable: true),
                    Decimal("DescuentoListaPct", nullable: true, precision: 9, scale: 2),
                    Decimal("SubtotalAntesRedondeo", nullable: true),
                    Col("RedondeoModo", "TINYINT", nullable: true),
                    Decimal("PrecioFinal", nullable: true),
                    Col("VigenciaInicio", "DATETIME2(0)", scale: 0, nullable: true),
                    Col("VigenciaFin", "DATETIME2(0)", scale: 0, nullable: true),
                    Col("ReglaVersion", "NVARCHAR(30)", 30, nullable: true),
                    Col("FechaResolucionUtc", "DATETIME2(0)", scale: 0, nullable: true),
                    Col("CorrelationId", "UNIQUEIDENTIFIER", nullable: true),
                    Col("PrecioOverride", "BIT", nullable: true),
                    Col("MotivoPrecioOverride", "NVARCHAR(500)", 500, nullable: true),
                    Col("idUsuarioPrecioOverride", "UNIQUEIDENTIFIER", nullable: true),
                    Col("FechaPrecioOverrideUtc", "DATETIME2(0)", scale: 0, nullable: true)
                }).ToArray(),
                ForeignKeys = v1.ForeignKeys.Concat(new[]
                {
                    new SchemaForeignKeyContract("FK_CotizacionesPartidas_Cotizaciones_EmpresaId", new[] { "idEmpresa", "idCotizacion" }, "dbo", "Cotizaciones", new[] { "idEmpresa", "id" }),
                    new SchemaForeignKeyContract("FK_CotizacionesPartidas_Listas_EmpresaId", new[] { "idEmpresa", "idListaPrecio" }, "dbo", "ListaPreciosListas", new[] { "idEmpresa", "id" }),
                    new SchemaForeignKeyContract("FK_CotizacionesPartidas_ProductosServicios_EmpresaId", new[] { "idEmpresa", "idProductoServicio" }, "dbo", "ProductosServicios", new[] { "idEmpresa", "id" }),
                    new SchemaForeignKeyContract("FK_CotizacionesPartidas_Variantes_EmpresaId", new[] { "idEmpresa", "idVariante" }, "dbo", "ProductosServiciosVariantes", new[] { "idEmpresa", "id" }),
                    new SchemaForeignKeyContract("FK_CotizacionesPartidas_PresentacionesVenta_EmpresaId", new[] { "idEmpresa", "idPresentacionVenta" }, "dbo", "ProductosServiciosPresentacionesVenta", new[] { "idEmpresa", "id" })
                }).ToArray(),
                CheckConstraints = new[]
                {
                    new SchemaCheckContract("CK_CotizacionesPartidas_ListaNivel", "CHECK (ListaPrecioNivel IS NULL OR ListaPrecioNivel BETWEEN 1 AND 10)"),
                    new SchemaCheckContract("CK_CotizacionesPartidas_Precios", "CHECK ((PrecioBase IS NULL OR PrecioBase >= 0) AND (PrecioLista IS NULL OR PrecioLista >= 0) AND (SubtotalAntesRedondeo IS NULL OR SubtotalAntesRedondeo >= 0) AND (PrecioFinal IS NULL OR PrecioFinal >= 0) AND (ReglaVersion IS NULL OR PrecioUnitario >= 0))"),
                    new SchemaCheckContract("CK_CotizacionesPartidas_Descuentos", "CHECK ((DescuentoListaPct IS NULL OR DescuentoListaPct BETWEEN 0 AND 100) AND (ReglaVersion IS NULL OR DescuentoPct BETWEEN 0 AND 100))"),
                    new SchemaCheckContract("CK_CotizacionesPartidas_Redondeo", "CHECK (RedondeoModo IS NULL OR RedondeoModo IN (0, 1, 2))"),
                    new SchemaCheckContract("CK_CotizacionesPartidas_Vigencia", "CHECK (VigenciaInicio IS NULL OR VigenciaFin IS NULL OR VigenciaInicio <= VigenciaFin)"),
                    new SchemaCheckContract("CK_CotizacionesPartidas_Identidad", "CHECK (TipoIdentidad IS NULL OR (TipoIdentidad = 1 AND TipoProductoServicio = 1 AND idVariante IS NULL AND idPresentacionVenta IS NULL) OR (TipoIdentidad = 2 AND TipoProductoServicio = 2 AND idVariante IS NULL AND idPresentacionVenta IS NULL) OR (TipoIdentidad = 3 AND TipoProductoServicio = 1 AND idVariante IS NOT NULL AND idPresentacionVenta IS NULL) OR (TipoIdentidad = 4 AND TipoProductoServicio = 1 AND idVariante IS NULL AND idPresentacionVenta IS NOT NULL))"),
                    new SchemaCheckContract("CK_CotizacionesPartidas_Override", "CHECK ((PrecioOverride IS NULL AND MotivoPrecioOverride IS NULL AND idUsuarioPrecioOverride IS NULL AND FechaPrecioOverrideUtc IS NULL) OR (PrecioOverride = 0 AND MotivoPrecioOverride IS NULL AND idUsuarioPrecioOverride IS NULL AND FechaPrecioOverrideUtc IS NULL) OR (PrecioOverride = 1 AND NULLIF(LTRIM(RTRIM(MotivoPrecioOverride)), N'') IS NOT NULL AND idUsuarioPrecioOverride IS NOT NULL AND FechaPrecioOverrideUtc IS NOT NULL))"),
                    new SchemaCheckContract("CK_CotizacionesPartidas_Snapshot", "CHECK ((ReglaVersion IS NULL AND TipoIdentidad IS NULL AND idVariante IS NULL AND idPresentacionVenta IS NULL AND idListaPrecio IS NULL AND ListaPrecioNivel IS NULL AND PrecioBase IS NULL AND PrecioLista IS NULL AND OrigenPrecio IS NULL AND DescuentoListaPct IS NULL AND SubtotalAntesRedondeo IS NULL AND RedondeoModo IS NULL AND PrecioFinal IS NULL AND VigenciaInicio IS NULL AND VigenciaFin IS NULL AND FechaResolucionUtc IS NULL AND CorrelationId IS NULL AND PrecioOverride IS NULL) OR (ReglaVersion IS NOT NULL AND TipoIdentidad IS NOT NULL AND idListaPrecio IS NOT NULL AND ListaPrecioNivel BETWEEN 1 AND 10 AND PrecioBase IS NOT NULL AND NULLIF(LTRIM(RTRIM(OrigenPrecio)), N'') IS NOT NULL AND SubtotalAntesRedondeo IS NOT NULL AND RedondeoModo IS NOT NULL AND PrecioFinal IS NOT NULL AND FechaResolucionUtc IS NOT NULL AND CorrelationId IS NOT NULL AND PrecioOverride IN (0, 1)))")
                },
                Indexes = v1.Indexes.Concat(new[]
                {
                    Ix("UX_CotizacionesPartidas_Empresa_Id", true, ("idEmpresa", false), ("id", false)),
                    Ix("IX_CotizacionesPartidas_Empresa_Lista_Identidad", false, "ReglaVersion IS NOT NULL", ("idEmpresa", false), ("idListaPrecio", false), ("TipoIdentidad", false), ("idProductoServicio", false), ("idVariante", false), ("idPresentacionVenta", false))
                }).ToArray()
            };
        }

        private static SchemaTableContract BuildHistorialV2() => new(
            "dbo",
            "CotizacionesHistorial",
            new[]
            {
                Col("id", "UNIQUEIDENTIFIER", defaultDefinition: "(NEWID())"),
                Col("idEmpresa", "UNIQUEIDENTIFIER"),
                Col("idCotizacion", "UNIQUEIDENTIFIER"),
                Col("idPartida", "UNIQUEIDENTIFIER", nullable: true),
                Col("Operacion", "NVARCHAR(50)", 50),
                Col("Campo", "NVARCHAR(100)", 100, nullable: true),
                Col("ValorAnterior", "NVARCHAR(MAX)", -1, nullable: true),
                Col("ValorNuevo", "NVARCHAR(MAX)", -1, nullable: true),
                Col("idUsuario", "UNIQUEIDENTIFIER", nullable: true),
                Col("Usuario", "NVARCHAR(320)", 320, nullable: true),
                Col("Motivo", "NVARCHAR(500)", 500, nullable: true),
                Col("CorrelationId", "UNIQUEIDENTIFIER"),
                Col("FechaUtc", "DATETIME2(0)", scale: 0, defaultDefinition: "(SYSUTCDATETIME())")
            },
            new SchemaPrimaryKeyContract("PK_CotizacionesHistorial", new[] { "id" }, true),
            new[]
            {
                new SchemaForeignKeyContract("FK_CotizacionesHistorial_Cotizaciones_EmpresaId", new[] { "idEmpresa", "idCotizacion" }, "dbo", "Cotizaciones", new[] { "idEmpresa", "id" }),
                new SchemaForeignKeyContract("FK_CotizacionesHistorial_Partidas_EmpresaId", new[] { "idEmpresa", "idPartida" }, "dbo", "CotizacionesPartidas", new[] { "idEmpresa", "id" })
            },
            Array.Empty<SchemaUniqueContract>(),
            new[]
            {
                new SchemaCheckContract("CK_CotizacionesHistorial_Operacion", "CHECK (Operacion IN (N'ALTA_PARTIDA', N'CAMBIO_CANTIDAD', N'CAMBIO_IDENTIDAD', N'CAMBIO_LISTA', N'NUEVA_RESOLUCION', N'OVERRIDE_PRECIO', N'DESCUENTO_ADICIONAL', N'BAJA_PARTIDA', N'CLON_RE_RESOLUCION'))")
            },
            new[]
            {
                Ix("IX_CotizacionesHistorial_Empresa_Cotizacion_Fecha", false, ("idEmpresa", false), ("idCotizacion", false), ("FechaUtc", false)),
                Ix("IX_CotizacionesHistorial_Empresa_Correlation", false, ("idEmpresa", false), ("CorrelationId", false))
            });

        internal static IReadOnlyCollection<string> BuildV1ToV2Preconditions() => new[]
        {
            TableExists("Cotizaciones"),
            TableExists("CotizacionesPartidas"),
            TableExists("ListaPreciosListas"),
            TableExists("ProductosServicios"),
            TableExists("ProductosServiciosVariantes"),
            TableExists("ProductosServiciosPresentacionesVenta")
        };

        internal static string BuildV2HistoricalObjectsReconciliationSql() => @"
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Cotizaciones')
      AND name = N'IX_Cotizaciones_Empresa_Cliente')
    CREATE INDEX IX_Cotizaciones_Empresa_Cliente
        ON dbo.Cotizaciones (idEmpresa, idCliente, Activo);

IF OBJECT_ID(N'dbo.FK_CotizacionesPartidas_Cotizaciones', N'F') IS NULL
    ALTER TABLE dbo.CotizacionesPartidas WITH CHECK
        ADD CONSTRAINT FK_CotizacionesPartidas_Cotizaciones
        FOREIGN KEY (idCotizacion) REFERENCES dbo.Cotizaciones (id);
";

        internal static IReadOnlyCollection<string> BuildV2HistoricalObjectsReconciliationPreconditions() => new[]
        {
            TableExists("Cotizaciones"),
            TableExists("CotizacionesPartidas")
        };

        internal static string BuildV1ToV2Sql() => @"
IF COL_LENGTH('dbo.Cotizaciones', 'idListaPrecio') IS NULL
    ALTER TABLE dbo.Cotizaciones ADD idListaPrecio uniqueidentifier NULL;
IF COL_LENGTH('dbo.Cotizaciones', 'ListaPrecioNivel') IS NULL
    ALTER TABLE dbo.Cotizaciones ADD ListaPrecioNivel tinyint NULL;
IF COL_LENGTH('dbo.Cotizaciones', 'idCotizacionOrigen') IS NULL
    ALTER TABLE dbo.Cotizaciones ADD idCotizacionOrigen uniqueidentifier NULL;

IF COL_LENGTH('dbo.CotizacionesPartidas', 'TipoIdentidad') IS NULL
    ALTER TABLE dbo.CotizacionesPartidas ADD TipoIdentidad tinyint NULL;
IF COL_LENGTH('dbo.CotizacionesPartidas', 'idVariante') IS NULL
    ALTER TABLE dbo.CotizacionesPartidas ADD idVariante uniqueidentifier NULL;
IF COL_LENGTH('dbo.CotizacionesPartidas', 'idPresentacionVenta') IS NULL
    ALTER TABLE dbo.CotizacionesPartidas ADD idPresentacionVenta uniqueidentifier NULL;
IF COL_LENGTH('dbo.CotizacionesPartidas', 'idListaPrecio') IS NULL
    ALTER TABLE dbo.CotizacionesPartidas ADD idListaPrecio uniqueidentifier NULL;
IF COL_LENGTH('dbo.CotizacionesPartidas', 'ListaPrecioNivel') IS NULL
    ALTER TABLE dbo.CotizacionesPartidas ADD ListaPrecioNivel tinyint NULL;
IF COL_LENGTH('dbo.CotizacionesPartidas', 'PrecioBase') IS NULL
    ALTER TABLE dbo.CotizacionesPartidas ADD PrecioBase decimal(18,2) NULL;
IF COL_LENGTH('dbo.CotizacionesPartidas', 'PrecioLista') IS NULL
    ALTER TABLE dbo.CotizacionesPartidas ADD PrecioLista decimal(18,2) NULL;
IF COL_LENGTH('dbo.CotizacionesPartidas', 'OrigenPrecio') IS NULL
    ALTER TABLE dbo.CotizacionesPartidas ADD OrigenPrecio nvarchar(50) NULL;
IF COL_LENGTH('dbo.CotizacionesPartidas', 'DescuentoListaPct') IS NULL
    ALTER TABLE dbo.CotizacionesPartidas ADD DescuentoListaPct decimal(9,2) NULL;
IF COL_LENGTH('dbo.CotizacionesPartidas', 'SubtotalAntesRedondeo') IS NULL
    ALTER TABLE dbo.CotizacionesPartidas ADD SubtotalAntesRedondeo decimal(18,2) NULL;
IF COL_LENGTH('dbo.CotizacionesPartidas', 'RedondeoModo') IS NULL
    ALTER TABLE dbo.CotizacionesPartidas ADD RedondeoModo tinyint NULL;
IF COL_LENGTH('dbo.CotizacionesPartidas', 'PrecioFinal') IS NULL
    ALTER TABLE dbo.CotizacionesPartidas ADD PrecioFinal decimal(18,2) NULL;
IF COL_LENGTH('dbo.CotizacionesPartidas', 'VigenciaInicio') IS NULL
    ALTER TABLE dbo.CotizacionesPartidas ADD VigenciaInicio datetime2(0) NULL;
IF COL_LENGTH('dbo.CotizacionesPartidas', 'VigenciaFin') IS NULL
    ALTER TABLE dbo.CotizacionesPartidas ADD VigenciaFin datetime2(0) NULL;
IF COL_LENGTH('dbo.CotizacionesPartidas', 'ReglaVersion') IS NULL
    ALTER TABLE dbo.CotizacionesPartidas ADD ReglaVersion nvarchar(30) NULL;
IF COL_LENGTH('dbo.CotizacionesPartidas', 'FechaResolucionUtc') IS NULL
    ALTER TABLE dbo.CotizacionesPartidas ADD FechaResolucionUtc datetime2(0) NULL;
IF COL_LENGTH('dbo.CotizacionesPartidas', 'CorrelationId') IS NULL
    ALTER TABLE dbo.CotizacionesPartidas ADD CorrelationId uniqueidentifier NULL;
IF COL_LENGTH('dbo.CotizacionesPartidas', 'PrecioOverride') IS NULL
    ALTER TABLE dbo.CotizacionesPartidas ADD PrecioOverride bit NULL;
IF COL_LENGTH('dbo.CotizacionesPartidas', 'MotivoPrecioOverride') IS NULL
    ALTER TABLE dbo.CotizacionesPartidas ADD MotivoPrecioOverride nvarchar(500) NULL;
IF COL_LENGTH('dbo.CotizacionesPartidas', 'idUsuarioPrecioOverride') IS NULL
    ALTER TABLE dbo.CotizacionesPartidas ADD idUsuarioPrecioOverride uniqueidentifier NULL;
IF COL_LENGTH('dbo.CotizacionesPartidas', 'FechaPrecioOverrideUtc') IS NULL
    ALTER TABLE dbo.CotizacionesPartidas ADD FechaPrecioOverrideUtc datetime2(0) NULL;

IF OBJECT_ID(N'dbo.CotizacionesHistorial', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CotizacionesHistorial
    (
        id uniqueidentifier NOT NULL CONSTRAINT DF_CotizacionesHistorial_id DEFAULT (NEWID()),
        idEmpresa uniqueidentifier NOT NULL,
        idCotizacion uniqueidentifier NOT NULL,
        idPartida uniqueidentifier NULL,
        Operacion nvarchar(50) NOT NULL,
        Campo nvarchar(100) NULL,
        ValorAnterior nvarchar(max) NULL,
        ValorNuevo nvarchar(max) NULL,
        idUsuario uniqueidentifier NULL,
        Usuario nvarchar(320) NULL,
        Motivo nvarchar(500) NULL,
        CorrelationId uniqueidentifier NOT NULL,
        FechaUtc datetime2(0) NOT NULL CONSTRAINT DF_CotizacionesHistorial_FechaUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_CotizacionesHistorial PRIMARY KEY CLUSTERED (id)
    );
END;

IF OBJECT_ID(N'dbo.CK_Cotizaciones_ListaPrecio', N'C') IS NULL
    EXEC(N'ALTER TABLE dbo.Cotizaciones ADD CONSTRAINT CK_Cotizaciones_ListaPrecio CHECK ((idListaPrecio IS NULL AND ListaPrecioNivel IS NULL) OR (idListaPrecio IS NOT NULL AND ListaPrecioNivel BETWEEN 1 AND 10));');
IF OBJECT_ID(N'dbo.CK_CotizacionesPartidas_ListaNivel', N'C') IS NULL
    EXEC(N'ALTER TABLE dbo.CotizacionesPartidas ADD CONSTRAINT CK_CotizacionesPartidas_ListaNivel CHECK (ListaPrecioNivel IS NULL OR ListaPrecioNivel BETWEEN 1 AND 10);');
IF OBJECT_ID(N'dbo.CK_CotizacionesPartidas_Precios', N'C') IS NULL
    EXEC(N'ALTER TABLE dbo.CotizacionesPartidas ADD CONSTRAINT CK_CotizacionesPartidas_Precios CHECK ((PrecioBase IS NULL OR PrecioBase >= 0) AND (PrecioLista IS NULL OR PrecioLista >= 0) AND (SubtotalAntesRedondeo IS NULL OR SubtotalAntesRedondeo >= 0) AND (PrecioFinal IS NULL OR PrecioFinal >= 0) AND (ReglaVersion IS NULL OR PrecioUnitario >= 0));');
IF OBJECT_ID(N'dbo.CK_CotizacionesPartidas_Descuentos', N'C') IS NULL
    EXEC(N'ALTER TABLE dbo.CotizacionesPartidas ADD CONSTRAINT CK_CotizacionesPartidas_Descuentos CHECK ((DescuentoListaPct IS NULL OR DescuentoListaPct BETWEEN 0 AND 100) AND (ReglaVersion IS NULL OR DescuentoPct BETWEEN 0 AND 100));');
IF OBJECT_ID(N'dbo.CK_CotizacionesPartidas_Redondeo', N'C') IS NULL
    EXEC(N'ALTER TABLE dbo.CotizacionesPartidas ADD CONSTRAINT CK_CotizacionesPartidas_Redondeo CHECK (RedondeoModo IS NULL OR RedondeoModo IN (0, 1, 2));');
IF OBJECT_ID(N'dbo.CK_CotizacionesPartidas_Vigencia', N'C') IS NULL
    EXEC(N'ALTER TABLE dbo.CotizacionesPartidas ADD CONSTRAINT CK_CotizacionesPartidas_Vigencia CHECK (VigenciaInicio IS NULL OR VigenciaFin IS NULL OR VigenciaInicio <= VigenciaFin);');
IF OBJECT_ID(N'dbo.CK_CotizacionesPartidas_Identidad', N'C') IS NULL
    EXEC(N'ALTER TABLE dbo.CotizacionesPartidas ADD CONSTRAINT CK_CotizacionesPartidas_Identidad CHECK (TipoIdentidad IS NULL OR (TipoIdentidad = 1 AND TipoProductoServicio = 1 AND idVariante IS NULL AND idPresentacionVenta IS NULL) OR (TipoIdentidad = 2 AND TipoProductoServicio = 2 AND idVariante IS NULL AND idPresentacionVenta IS NULL) OR (TipoIdentidad = 3 AND TipoProductoServicio = 1 AND idVariante IS NOT NULL AND idPresentacionVenta IS NULL) OR (TipoIdentidad = 4 AND TipoProductoServicio = 1 AND idVariante IS NULL AND idPresentacionVenta IS NOT NULL));');
IF OBJECT_ID(N'dbo.CK_CotizacionesPartidas_Override', N'C') IS NULL
    EXEC(N'ALTER TABLE dbo.CotizacionesPartidas ADD CONSTRAINT CK_CotizacionesPartidas_Override CHECK ((PrecioOverride IS NULL AND MotivoPrecioOverride IS NULL AND idUsuarioPrecioOverride IS NULL AND FechaPrecioOverrideUtc IS NULL) OR (PrecioOverride = 0 AND MotivoPrecioOverride IS NULL AND idUsuarioPrecioOverride IS NULL AND FechaPrecioOverrideUtc IS NULL) OR (PrecioOverride = 1 AND NULLIF(LTRIM(RTRIM(MotivoPrecioOverride)), N'''') IS NOT NULL AND idUsuarioPrecioOverride IS NOT NULL AND FechaPrecioOverrideUtc IS NOT NULL));');
IF OBJECT_ID(N'dbo.CK_CotizacionesPartidas_Snapshot', N'C') IS NULL
    EXEC(N'ALTER TABLE dbo.CotizacionesPartidas ADD CONSTRAINT CK_CotizacionesPartidas_Snapshot CHECK ((ReglaVersion IS NULL AND TipoIdentidad IS NULL AND idVariante IS NULL AND idPresentacionVenta IS NULL AND idListaPrecio IS NULL AND ListaPrecioNivel IS NULL AND PrecioBase IS NULL AND PrecioLista IS NULL AND OrigenPrecio IS NULL AND DescuentoListaPct IS NULL AND SubtotalAntesRedondeo IS NULL AND RedondeoModo IS NULL AND PrecioFinal IS NULL AND VigenciaInicio IS NULL AND VigenciaFin IS NULL AND FechaResolucionUtc IS NULL AND CorrelationId IS NULL AND PrecioOverride IS NULL) OR (ReglaVersion IS NOT NULL AND TipoIdentidad IS NOT NULL AND idListaPrecio IS NOT NULL AND ListaPrecioNivel BETWEEN 1 AND 10 AND PrecioBase IS NOT NULL AND NULLIF(LTRIM(RTRIM(OrigenPrecio)), N'''') IS NOT NULL AND SubtotalAntesRedondeo IS NOT NULL AND RedondeoModo IS NOT NULL AND PrecioFinal IS NOT NULL AND FechaResolucionUtc IS NOT NULL AND CorrelationId IS NOT NULL AND PrecioOverride IN (0, 1)));');
IF OBJECT_ID(N'dbo.CK_CotizacionesHistorial_Operacion', N'C') IS NULL
    EXEC(N'ALTER TABLE dbo.CotizacionesHistorial ADD CONSTRAINT CK_CotizacionesHistorial_Operacion CHECK (Operacion IN (N''ALTA_PARTIDA'', N''CAMBIO_CANTIDAD'', N''CAMBIO_IDENTIDAD'', N''CAMBIO_LISTA'', N''NUEVA_RESOLUCION'', N''OVERRIDE_PRECIO'', N''DESCUENTO_ADICIONAL'', N''BAJA_PARTIDA'', N''CLON_RE_RESOLUCION''));');

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Cotizaciones_Empresa_Id' AND object_id = OBJECT_ID(N'dbo.Cotizaciones'))
    EXEC(N'CREATE UNIQUE NONCLUSTERED INDEX UX_Cotizaciones_Empresa_Id ON dbo.Cotizaciones (idEmpresa, id);');
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Cotizaciones_Empresa_Lista_Activo' AND object_id = OBJECT_ID(N'dbo.Cotizaciones'))
    EXEC(N'CREATE NONCLUSTERED INDEX IX_Cotizaciones_Empresa_Lista_Activo ON dbo.Cotizaciones (idEmpresa, idListaPrecio, Activo);');
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_CotizacionesPartidas_Empresa_Id' AND object_id = OBJECT_ID(N'dbo.CotizacionesPartidas'))
    EXEC(N'CREATE UNIQUE NONCLUSTERED INDEX UX_CotizacionesPartidas_Empresa_Id ON dbo.CotizacionesPartidas (idEmpresa, id);');
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CotizacionesPartidas_Empresa_Lista_Identidad' AND object_id = OBJECT_ID(N'dbo.CotizacionesPartidas'))
    EXEC(N'CREATE NONCLUSTERED INDEX IX_CotizacionesPartidas_Empresa_Lista_Identidad ON dbo.CotizacionesPartidas (idEmpresa, idListaPrecio, TipoIdentidad, idProductoServicio, idVariante, idPresentacionVenta) WHERE ReglaVersion IS NOT NULL;');
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CotizacionesHistorial_Empresa_Cotizacion_Fecha' AND object_id = OBJECT_ID(N'dbo.CotizacionesHistorial'))
    EXEC(N'CREATE NONCLUSTERED INDEX IX_CotizacionesHistorial_Empresa_Cotizacion_Fecha ON dbo.CotizacionesHistorial (idEmpresa, idCotizacion, FechaUtc);');
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CotizacionesHistorial_Empresa_Correlation' AND object_id = OBJECT_ID(N'dbo.CotizacionesHistorial'))
    EXEC(N'CREATE NONCLUSTERED INDEX IX_CotizacionesHistorial_Empresa_Correlation ON dbo.CotizacionesHistorial (idEmpresa, CorrelationId);');

IF OBJECT_ID(N'dbo.FK_Cotizaciones_ListaPreciosListas_EmpresaId', N'F') IS NULL
    EXEC(N'ALTER TABLE dbo.Cotizaciones ADD CONSTRAINT FK_Cotizaciones_ListaPreciosListas_EmpresaId FOREIGN KEY (idEmpresa, idListaPrecio) REFERENCES dbo.ListaPreciosListas (idEmpresa, id);');
IF OBJECT_ID(N'dbo.FK_Cotizaciones_Origen_EmpresaId', N'F') IS NULL
    EXEC(N'ALTER TABLE dbo.Cotizaciones ADD CONSTRAINT FK_Cotizaciones_Origen_EmpresaId FOREIGN KEY (idEmpresa, idCotizacionOrigen) REFERENCES dbo.Cotizaciones (idEmpresa, id);');
IF OBJECT_ID(N'dbo.FK_CotizacionesPartidas_Cotizaciones_EmpresaId', N'F') IS NULL
    EXEC(N'ALTER TABLE dbo.CotizacionesPartidas ADD CONSTRAINT FK_CotizacionesPartidas_Cotizaciones_EmpresaId FOREIGN KEY (idEmpresa, idCotizacion) REFERENCES dbo.Cotizaciones (idEmpresa, id);');
IF OBJECT_ID(N'dbo.FK_CotizacionesPartidas_Listas_EmpresaId', N'F') IS NULL
    EXEC(N'ALTER TABLE dbo.CotizacionesPartidas ADD CONSTRAINT FK_CotizacionesPartidas_Listas_EmpresaId FOREIGN KEY (idEmpresa, idListaPrecio) REFERENCES dbo.ListaPreciosListas (idEmpresa, id);');
IF OBJECT_ID(N'dbo.FK_CotizacionesPartidas_ProductosServicios_EmpresaId', N'F') IS NULL
    EXEC(N'ALTER TABLE dbo.CotizacionesPartidas ADD CONSTRAINT FK_CotizacionesPartidas_ProductosServicios_EmpresaId FOREIGN KEY (idEmpresa, idProductoServicio) REFERENCES dbo.ProductosServicios (idEmpresa, id);');
IF OBJECT_ID(N'dbo.FK_CotizacionesPartidas_Variantes_EmpresaId', N'F') IS NULL
    EXEC(N'ALTER TABLE dbo.CotizacionesPartidas ADD CONSTRAINT FK_CotizacionesPartidas_Variantes_EmpresaId FOREIGN KEY (idEmpresa, idVariante) REFERENCES dbo.ProductosServiciosVariantes (idEmpresa, id);');
IF OBJECT_ID(N'dbo.FK_CotizacionesPartidas_PresentacionesVenta_EmpresaId', N'F') IS NULL
    EXEC(N'ALTER TABLE dbo.CotizacionesPartidas ADD CONSTRAINT FK_CotizacionesPartidas_PresentacionesVenta_EmpresaId FOREIGN KEY (idEmpresa, idPresentacionVenta) REFERENCES dbo.ProductosServiciosPresentacionesVenta (idEmpresa, id);');
IF OBJECT_ID(N'dbo.FK_CotizacionesHistorial_Cotizaciones_EmpresaId', N'F') IS NULL
    EXEC(N'ALTER TABLE dbo.CotizacionesHistorial ADD CONSTRAINT FK_CotizacionesHistorial_Cotizaciones_EmpresaId FOREIGN KEY (idEmpresa, idCotizacion) REFERENCES dbo.Cotizaciones (idEmpresa, id);');
IF OBJECT_ID(N'dbo.FK_CotizacionesHistorial_Partidas_EmpresaId', N'F') IS NULL
    EXEC(N'ALTER TABLE dbo.CotizacionesHistorial ADD CONSTRAINT FK_CotizacionesHistorial_Partidas_EmpresaId FOREIGN KEY (idEmpresa, idPartida) REFERENCES dbo.CotizacionesPartidas (idEmpresa, id);');";

        private static string TableExists(string table) => $@"SELECT CASE WHEN OBJECT_ID(N'dbo.{table}', N'U') IS NOT NULL THEN 1 ELSE 0 END;";

        private static SchemaColumnContract Col(string name, string type, int? length = null, int? scale = null, bool nullable = false, string? defaultDefinition = null)
            => new(name, type, length, null, scale, nullable, defaultDefinition, false, false, null);

        private static SchemaColumnContract Decimal(string name, bool nullable = false, byte precision = 18, int scale = 2, string? defaultDefinition = null)
            => new(name, $"DECIMAL({precision},{scale})", null, precision, scale, nullable, defaultDefinition, false, false, null);

        private static SchemaIndexContract Ix(string name, bool unique, params (string Name, bool Descending)[] columns)
            => Ix(name, unique, null, columns);

        private static SchemaIndexContract Ix(string name, bool unique, string? filter, params (string Name, bool Descending)[] columns)
            => new(name, unique, false, columns.Select(column => new SchemaIndexColumnContract(column.Name, column.Descending)).ToArray(), Array.Empty<string>(), filter);
    }
}
