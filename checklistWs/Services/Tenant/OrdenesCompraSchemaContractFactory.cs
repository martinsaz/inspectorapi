namespace checklistWs.Services.Tenant
{
    internal static class OrdenesCompraSchemaContractFactory
    {
        public static SchemaContract GetContract(int version)
        {
            if (version is not ProductosServiciosSchemaContractProvider.V1 and not ProductosServiciosSchemaContractProvider.V2)
            {
                throw new InvalidOperationException("SCHEMA_CONTRACT_VERSION_NOT_SUPPORTED");
            }

            return new SchemaContract(
                DatabaseScopes.OrdenesCompra,
                version,
                "Ordenes de Compra",
                new[]
                {
                    PresentacionesCompra(version),
                    Folios(),
                    Cabecera(),
                    Detalle(),
                },
                new[]
                {
                    "inspectorapi/checklistWs/Scripts/ordenes-compra-up.sql",
                    "inspectorapi/checklistWs/Controllers/OrdenesCompra/OrdenesCompraController.cs",
                    "inspector/docs/compras/BL03_FASE_A_OC01_CONTRATO_FUNCIONAL_OC_RECEPCION_20260921.md",
                    "inspector/docs/compras/BL03_FASE_A_ARQ01_INVENTARIO_VARIANTE_SUCURSAL_20260921.md",
                    "inspector/docs/compras/BL03_FASE_C_OC_CUR_02_SCHEMA_VERSIONADO_CURVAS_SIEMBRA_20260923.md",
                },
                new DateTime(2026, 9, 23, 0, 0, 0, DateTimeKind.Utc));
        }

        private static SchemaTableContract PresentacionesCompra(int version)
        {
            List<SchemaColumnContract> columns = new()
            {
                Col("id", "UNIQUEIDENTIFIER", false, def: "(NEWID())"),
                Col("idEmpresa", "UNIQUEIDENTIFIER", false),
                Col("identityKey", "UNIQUEIDENTIFIER", false, def: "(NEWID())"),
                Col("idProductoServicio", "UNIQUEIDENTIFIER", false),
                Col("idVariante", "UNIQUEIDENTIFIER", true),
                Col("Nombre", "NVARCHAR(150)", false, max: 150),
                Col("idUnidadCompra", "UNIQUEIDENTIFIER", false),
                Col("UnidadCompra", "NVARCHAR(100)", false, max: 100),
                Col("UnidadCompraAbreviatura", "NVARCHAR(20)", false, max: 20),
                Col("FactorConversionBase", "DECIMAL(28,12)", false, precision: 28, scale: 12),
            };

            if (version >= ProductosServiciosSchemaContractProvider.V2)
            {
                columns.Add(Col("PermiteCantidadBase", "BIT", false, def: "((0))"));
            }

            columns.AddRange(new[]
            {
                Col("Activo", "BIT", false, def: "((1))"),
                Col("FechaCreacion", "DATETIME2(0)", false, scale: 0, def: "(SYSUTCDATETIME())"),
                Col("FechaActualizacion", "DATETIME2(0)", false, scale: 0, def: "(SYSUTCDATETIME())"),
                Col("FechaArchivado", "DATETIME2(0)", true, scale: 0),
            });

            return new(
            "dbo",
            "OrdenesCompraPresentacionesCompra",
            columns,
            new SchemaPrimaryKeyContract("PK_OrdenesCompraPresentacionesCompra", new[] { "id" }, true),
            new[]
            {
                new SchemaForeignKeyContract("FK_OCPresentacionesCompra_ProductosServicios_EmpresaId", new[] { "idEmpresa", "idProductoServicio" }, "dbo", "ProductosServicios", new[] { "idEmpresa", "id" }),
                new SchemaForeignKeyContract("FK_OCPresentacionesCompra_Variantes_EmpresaId", new[] { "idEmpresa", "idVariante" }, "dbo", "ProductosServiciosVariantes", new[] { "idEmpresa", "id" }),
                new SchemaForeignKeyContract("FK_OCPresentacionesCompra_Unidades_EmpresaId", new[] { "idEmpresa", "idUnidadCompra" }, "dbo", "ProductosServiciosUnidadesMedida", new[] { "idEmpresa", "id" }),
            },
            new[]
            {
                new SchemaUniqueContract("UX_OCPresentacionesCompra_Empresa_Id", new[] { "idEmpresa", "id" }, null),
            },
            new[]
            {
                new SchemaCheckContract("CK_OCPresentacionesCompra_Factor", "CHECK (FactorConversionBase > 0)"),
                new SchemaCheckContract("CK_OCPresentacionesCompra_Archivado", "CHECK ((Activo = 1 AND FechaArchivado IS NULL) OR (Activo = 0 AND FechaArchivado IS NOT NULL))"),
            },
            new[]
            {
                Ix("UX_OCPresentacionesCompra_Empresa_Id", true, "idEmpresa", "id"),
                Ix("IX_OCPresentacionesCompra_Empresa_Producto_Variante", false, "idEmpresa", "idProductoServicio", "idVariante", "Activo"),
                Ix("IX_OCPresentacionesCompra_Empresa_Unidad", false, "idEmpresa", "idUnidadCompra", "Activo"),
            });
        }

        private static SchemaTableContract Folios() => new(
            "dbo",
            "OrdenesCompraFolios",
            new[]
            {
                Col("id", "UNIQUEIDENTIFIER", false, def: "(NEWID())"),
                Col("idEmpresa", "UNIQUEIDENTIFIER", false),
                Col("identityKey", "UNIQUEIDENTIFIER", false, def: "(NEWID())"),
                Col("UltimoConsecutivo", "BIGINT", false, def: "((0))"),
                Col("FechaCreacion", "DATETIME2(0)", false, scale: 0, def: "(SYSUTCDATETIME())"),
                Col("FechaActualizacion", "DATETIME2(0)", false, scale: 0, def: "(SYSUTCDATETIME())"),
            },
            new SchemaPrimaryKeyContract("PK_OrdenesCompraFolios", new[] { "id" }, true),
            Array.Empty<SchemaForeignKeyContract>(),
            new[]
            {
                new SchemaUniqueContract("UX_OrdenesCompraFolios_Empresa_Id", new[] { "idEmpresa", "id" }, null),
                new SchemaUniqueContract("UX_OrdenesCompraFolios_Empresa", new[] { "idEmpresa" }, null),
            },
            new[] { new SchemaCheckContract("CK_OrdenesCompraFolios_UltimoConsecutivo", "CHECK (UltimoConsecutivo >= 0)") },
            new[]
            {
                Ix("UX_OrdenesCompraFolios_Empresa_Id", true, "idEmpresa", "id"),
                Ix("UX_OrdenesCompraFolios_Empresa", true, "idEmpresa"),
            });

        private static SchemaTableContract Cabecera() => new(
            "dbo",
            "OrdenesCompra",
            new[]
            {
                Col("id", "UNIQUEIDENTIFIER", false, def: "(NEWID())"),
                Col("idEmpresa", "UNIQUEIDENTIFIER", false),
                Col("identityKey", "UNIQUEIDENTIFIER", false, def: "(NEWID())"),
                Col("Folio", "NVARCHAR(30)", true, max: 30),
                Col("idRazonSocial", "UNIQUEIDENTIFIER", false),
                Col("idSucursal", "UNIQUEIDENTIFIER", false),
                Col("idProveedor", "UNIQUEIDENTIFIER", false),
                Col("FechaOrden", "DATETIME2(0)", false, scale: 0),
                Col("FechaLlegada", "DATETIME2(0)", true, scale: 0),
                Col("Estado", "TINYINT", false, def: "((1))"),
                Col("Subtotal", "DECIMAL(18,2)", false, precision: 18, scale: 2, def: "((0))"),
                Col("Total", "DECIMAL(18,2)", false, precision: 18, scale: 2, def: "((0))"),
                Col("Observaciones", "NVARCHAR(1000)", true, max: 1000),
                Col("MotivoCancelacion", "NVARCHAR(500)", true, max: 500),
                Col("FechaCancelacion", "DATETIME2(0)", true, scale: 0),
                Col("Activo", "BIT", false, def: "((1))"),
                Col("FechaCreacion", "DATETIME2(0)", false, scale: 0, def: "(SYSUTCDATETIME())"),
                Col("FechaActualizacion", "DATETIME2(0)", false, scale: 0, def: "(SYSUTCDATETIME())"),
                Col("FechaArchivado", "DATETIME2(0)", true, scale: 0),
                Col("idUsuarioCreacion", "UNIQUEIDENTIFIER", true),
                Col("idUsuarioActualizacion", "UNIQUEIDENTIFIER", true),
                Col("idUsuarioCancelacion", "UNIQUEIDENTIFIER", true),
            },
            new SchemaPrimaryKeyContract("PK_OrdenesCompra", new[] { "id" }, true),
            Array.Empty<SchemaForeignKeyContract>(),
            new[]
            {
                new SchemaUniqueContract("UX_OrdenesCompra_Empresa_Id", new[] { "idEmpresa", "id" }, null),
                new SchemaUniqueContract("UX_OrdenesCompra_Empresa_Folio", new[] { "idEmpresa", "Folio" }, "Folio IS NOT NULL"),
            },
            new[]
            {
                new SchemaCheckContract("CK_OrdenesCompra_Estado", "CHECK (Estado IN (1, 2, 3, 4, 5))"),
                new SchemaCheckContract("CK_OrdenesCompra_ImportesNoNegativos", "CHECK (Subtotal >= 0 AND Total >= 0)"),
                new SchemaCheckContract("CK_OrdenesCompra_TotalIgualSubtotal", "CHECK (Subtotal = Total)"),
                new SchemaCheckContract("CK_OrdenesCompra_TotalPorEstado", "CHECK (((Estado = 5 OR Estado = 4 OR Estado = 2) AND Total > 0) OR ((Estado = 3 OR Estado = 1) AND Total >= 0))"),
                new SchemaCheckContract("CK_OrdenesCompra_FechaLlegada", "CHECK (FechaLlegada IS NULL OR FechaLlegada >= FechaOrden)"),
                new SchemaCheckContract("CK_OrdenesCompra_Cancelacion", "CHECK ((Estado = 3 AND FechaCancelacion IS NOT NULL AND NULLIF(LTRIM(RTRIM(MotivoCancelacion)), N'') IS NOT NULL) OR ((Estado = 5 OR Estado = 4 OR Estado = 2 OR Estado = 1) AND FechaCancelacion IS NULL AND MotivoCancelacion IS NULL AND idUsuarioCancelacion IS NULL))"),
                new SchemaCheckContract("CK_OrdenesCompra_FolioGenerada", "CHECK (((Estado = 5 OR Estado = 4 OR Estado = 2) AND NULLIF(LTRIM(RTRIM(Folio)), N'') IS NOT NULL) OR (Estado = 3 OR Estado = 1))"),
                new SchemaCheckContract("CK_OrdenesCompra_Archivado", "CHECK ((Activo = 1 AND FechaArchivado IS NULL) OR (Activo = 0 AND FechaArchivado IS NOT NULL))"),
            },
            new[]
            {
                Ix("UX_OrdenesCompra_Empresa_Id", true, "idEmpresa", "id"),
                new SchemaIndexContract("UX_OrdenesCompra_Empresa_Folio", true, false, Keys("idEmpresa", "Folio"), Array.Empty<string>(), "Folio IS NOT NULL"),
                new SchemaIndexContract("IX_OrdenesCompra_Empresa_Estado_FechaOrden", false, false, new[] { new SchemaIndexColumnContract("idEmpresa", false), new SchemaIndexColumnContract("Estado", false), new SchemaIndexColumnContract("FechaOrden", true) }, Array.Empty<string>(), null),
                new SchemaIndexContract("IX_OrdenesCompra_Empresa_Proveedor", false, false, new[] { new SchemaIndexColumnContract("idEmpresa", false), new SchemaIndexColumnContract("idProveedor", false), new SchemaIndexColumnContract("FechaOrden", true) }, Array.Empty<string>(), null),
                new SchemaIndexContract("IX_OrdenesCompra_Empresa_Sucursal", false, false, new[] { new SchemaIndexColumnContract("idEmpresa", false), new SchemaIndexColumnContract("idSucursal", false), new SchemaIndexColumnContract("FechaOrden", true) }, Array.Empty<string>(), null),
                new SchemaIndexContract("IX_OrdenesCompra_Empresa_RazonSocial", false, false, new[] { new SchemaIndexColumnContract("idEmpresa", false), new SchemaIndexColumnContract("idRazonSocial", false), new SchemaIndexColumnContract("FechaOrden", true) }, Array.Empty<string>(), null),
            });

        private static SchemaTableContract Detalle() => new(
            "dbo",
            "OrdenesCompraDetalle",
            new[]
            {
                Col("id", "UNIQUEIDENTIFIER", false, def: "(NEWID())"),
                Col("idEmpresa", "UNIQUEIDENTIFIER", false),
                Col("identityKey", "UNIQUEIDENTIFIER", false, def: "(NEWID())"),
                Col("idOrdenCompra", "UNIQUEIDENTIFIER", false),
                Col("NumeroPartida", "INT", false),
                Col("idProductoServicio", "UNIQUEIDENTIFIER", false),
                Col("TipoProductoServicio", "TINYINT", false),
                Col("idVariante", "UNIQUEIDENTIFIER", true),
                Col("idPresentacionCompra", "UNIQUEIDENTIFIER", true),
                Col("Codigo", "NVARCHAR(50)", false, max: 50),
                Col("Nombre", "NVARCHAR(150)", false, max: 150),
                Col("Descripcion", "NVARCHAR(1000)", true, max: 1000),
                Col("VarianteSnapshot", "NVARCHAR(200)", true, max: 200),
                Col("PresentacionCompraSnapshot", "NVARCHAR(150)", true, max: 150),
                Col("idUnidadMedida", "UNIQUEIDENTIFIER", false),
                Col("UnidadMedida", "NVARCHAR(100)", false, max: 100),
                Col("UnidadAbreviatura", "NVARCHAR(20)", false, max: 20),
                Col("UnidadCompraSnapshot", "NVARCHAR(100)", true, max: 100),
                Col("UnidadCompraAbreviaturaSnapshot", "NVARCHAR(20)", true, max: 20),
                Col("Cantidad", "DECIMAL(18,4)", false, precision: 18, scale: 4),
                Col("CantidadCompra", "DECIMAL(18,4)", false, precision: 18, scale: 4),
                Col("FactorConversionSnapshot", "DECIMAL(28,12)", false, precision: 28, scale: 12, def: "((1))"),
                Col("CantidadBaseOrdenada", "DECIMAL(18,4)", false, precision: 18, scale: 4),
                Col("CantidadBaseRecibidaAcumulada", "DECIMAL(18,4)", false, precision: 18, scale: 4, def: "((0))"),
                Col("CantidadBasePendiente", "DECIMAL(18,4)", false, precision: 18, scale: 4),
                Col("EstadoPartida", "TINYINT", false, def: "((1))"),
                Col("CostoUnitario", "DECIMAL(18,2)", false, precision: 18, scale: 2, def: "((0))"),
                Col("Subtotal", "DECIMAL(18,2)", false, precision: 18, scale: 2, def: "((0))"),
                Col("Total", "DECIMAL(18,2)", false, precision: 18, scale: 2, def: "((0))"),
                Col("Activo", "BIT", false, def: "((1))"),
                Col("FechaCreacion", "DATETIME2(0)", false, scale: 0, def: "(SYSUTCDATETIME())"),
                Col("FechaActualizacion", "DATETIME2(0)", false, scale: 0, def: "(SYSUTCDATETIME())"),
                Col("FechaArchivado", "DATETIME2(0)", true, scale: 0),
            },
            new SchemaPrimaryKeyContract("PK_OrdenesCompraDetalle", new[] { "id" }, true),
            new[]
            {
                new SchemaForeignKeyContract("FK_OrdenesCompraDetalle_OrdenesCompra_EmpresaId", new[] { "idEmpresa", "idOrdenCompra" }, "dbo", "OrdenesCompra", new[] { "idEmpresa", "id" }),
                new SchemaForeignKeyContract("FK_OrdenesCompraDetalle_Variantes_EmpresaId", new[] { "idEmpresa", "idVariante" }, "dbo", "ProductosServiciosVariantes", new[] { "idEmpresa", "id" }),
                new SchemaForeignKeyContract("FK_OrdenesCompraDetalle_PresentacionCompra_EmpresaId", new[] { "idEmpresa", "idPresentacionCompra" }, "dbo", "OrdenesCompraPresentacionesCompra", new[] { "idEmpresa", "id" }),
            },
            new[]
            {
                new SchemaUniqueContract("UX_OrdenesCompraDetalle_Empresa_Id", new[] { "idEmpresa", "id" }, null),
                new SchemaUniqueContract("UX_OrdenesCompraDetalle_Empresa_Orden_NumeroPartida", new[] { "idEmpresa", "idOrdenCompra", "NumeroPartida" }, "Activo = 1 AND FechaArchivado IS NULL"),
            },
            new[]
            {
                new SchemaCheckContract("CK_OrdenesCompraDetalle_NumeroPartida", "CHECK (NumeroPartida > 0)"),
                new SchemaCheckContract("CK_OrdenesCompraDetalle_TipoProductoServicio", "CHECK (TipoProductoServicio IN (1, 2))"),
                new SchemaCheckContract("CK_OrdenesCompraDetalle_Cantidad", "CHECK (Cantidad > 0 AND CantidadCompra > 0 AND FactorConversionSnapshot > 0)"),
                new SchemaCheckContract("CK_OrdenesCompraDetalle_Base", "CHECK (CantidadBaseOrdenada > 0 AND CantidadBaseRecibidaAcumulada >= 0 AND CantidadBasePendiente >= 0 AND CantidadBaseOrdenada = CantidadBaseRecibidaAcumulada + CantidadBasePendiente)"),
                new SchemaCheckContract("CK_OrdenesCompraDetalle_EstadoPartida", "CHECK (EstadoPartida IN (1, 2, 3, 4))"),
                new SchemaCheckContract("CK_OrdenesCompraDetalle_ImportesNoNegativos", "CHECK (CostoUnitario >= 0 AND Subtotal >= 0 AND Total >= 0)"),
                new SchemaCheckContract("CK_OrdenesCompraDetalle_Calculo", "CHECK (Subtotal = ROUND(CantidadCompra * CostoUnitario, 2) AND Total = Subtotal)"),
                new SchemaCheckContract("CK_OrdenesCompraDetalle_Archivado", "CHECK ((Activo = 1 AND FechaArchivado IS NULL) OR (Activo = 0 AND FechaArchivado IS NOT NULL))"),
            },
            new[]
            {
                Ix("UX_OrdenesCompraDetalle_Empresa_Id", true, "idEmpresa", "id"),
                Ix("IX_OrdenesCompraDetalle_Empresa_Orden", false, "idEmpresa", "idOrdenCompra"),
                new SchemaIndexContract("UX_OrdenesCompraDetalle_Empresa_Orden_NumeroPartida", true, false, Keys("idEmpresa", "idOrdenCompra", "NumeroPartida"), Array.Empty<string>(), "Activo = 1 AND FechaArchivado IS NULL"),
                Ix("IX_OrdenesCompraDetalle_Empresa_Producto_Variante", false, "idEmpresa", "idProductoServicio", "idVariante"),
                Ix("IX_OrdenesCompraDetalle_Empresa_PresentacionCompra", false, "idEmpresa", "idPresentacionCompra"),
                Ix("IX_OrdenesCompraDetalle_Empresa_EstadoPartida", false, "idEmpresa", "EstadoPartida"),
            });

        private static SchemaColumnContract Col(
            string name,
            string type,
            bool nullable,
            int? max = null,
            byte? precision = null,
            int? scale = null,
            string? def = null) => new(name, type, max, precision, scale, nullable, def, false, false, null);

        private static SchemaIndexContract Ix(string name, bool unique, params string[] columns) =>
            new(name, unique, false, Keys(columns), Array.Empty<string>(), null);

        private static SchemaIndexColumnContract[] Keys(params string[] columns) =>
            columns.Select(column => new SchemaIndexColumnContract(column, false)).ToArray();
    }
}
