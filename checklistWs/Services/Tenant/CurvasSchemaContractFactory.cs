namespace checklistWs.Services.Tenant
{
    internal static class CurvasSchemaContractFactory
    {
        public static SchemaContract GetContract(int version)
        {
            if (version != ProductosServiciosSchemaContractProvider.CurvasLatestVersion)
            {
                throw new InvalidOperationException("SCHEMA_CONTRACT_VERSION_NOT_SUPPORTED");
            }

            return new SchemaContract(
                DatabaseScopes.Curvas,
                ProductosServiciosSchemaContractProvider.CurvasLatestVersion,
                "Curvas CheckApp",
                new[]
                {
                    Catalogo(),
                    Detalle(),
                    Siembra(),
                    OperacionesCompra(),
                    OperacionOrdenesCompra(),
                    SugerenciasSnapshot(),
                },
                new[]
                {
                    "inspector/docs/compras/MOKA_AUDITORIA_TARAHUMARA_CURVAS_OC_CHECKAPP_20260922.md",
                    "inspector/docs/compras/BL03_FASE_C_OC_CUR_01_CONTRATO_FUNCIONAL_CURVAS_CHECKAPP_20260923.md",
                    "inspector/docs/compras/BL03_FASE_C_OC_CUR_02_SCHEMA_VERSIONADO_CURVAS_SIEMBRA_20260923.md",
                },
                new DateTime(2026, 9, 23, 0, 0, 0, DateTimeKind.Utc));
        }

        private static SchemaTableContract Catalogo() => new(
            "dbo",
            "CurvasCatalogo",
            new[]
            {
                Col("id", "UNIQUEIDENTIFIER", false, def: "(NEWID())"),
                Col("idEmpresa", "UNIQUEIDENTIFIER", false),
                Col("identityKey", "UNIQUEIDENTIFIER", false, def: "(NEWID())"),
                Col("Nombre", "NVARCHAR(150)", false, max: 150),
                Col("Codigo", "NVARCHAR(50)", true, max: 50),
                Col("Descripcion", "NVARCHAR(500)", true, max: 500),
                Col("Estado", "TINYINT", false, def: "((1))"),
                Col("Activo", "BIT", false, def: "((1))"),
                Col("FechaCreacion", "DATETIME2(0)", false, scale: 0, def: "(SYSUTCDATETIME())"),
                Col("FechaActualizacion", "DATETIME2(0)", false, scale: 0, def: "(SYSUTCDATETIME())"),
                Col("FechaArchivado", "DATETIME2(0)", true, scale: 0),
                Col("idUsuarioCreacion", "UNIQUEIDENTIFIER", true),
                Col("idUsuarioActualizacion", "UNIQUEIDENTIFIER", true),
                Col("idUsuarioArchivado", "UNIQUEIDENTIFIER", true),
            },
            new SchemaPrimaryKeyContract("PK_CurvasCatalogo", new[] { "id" }, true),
            Array.Empty<SchemaForeignKeyContract>(),
            new[]
            {
                Ux("UX_CurvasCatalogo_Empresa_Id", null, "idEmpresa", "id"),
                Ux("UX_CurvasCatalogo_Empresa_Nombre_Activo", "Activo = 1 AND FechaArchivado IS NULL", "idEmpresa", "Nombre"),
                Ux("UX_CurvasCatalogo_Empresa_Codigo_Activo", "Codigo IS NOT NULL AND Activo = 1 AND FechaArchivado IS NULL", "idEmpresa", "Codigo"),
            },
            new[]
            {
                new SchemaCheckContract("CK_CurvasCatalogo_Estado", "CHECK (Estado IN (1, 2))"),
                new SchemaCheckContract("CK_CurvasCatalogo_Archivado", "CHECK ((Activo = 1 AND Estado = 1 AND FechaArchivado IS NULL AND idUsuarioArchivado IS NULL) OR (Activo = 0 AND Estado = 2 AND FechaArchivado IS NOT NULL))"),
            },
            new[]
            {
                Ix("UX_CurvasCatalogo_Empresa_Id", true, "idEmpresa", "id"),
                Ix("UX_CurvasCatalogo_Empresa_Nombre_Activo", true, false, "Activo = 1 AND FechaArchivado IS NULL", "idEmpresa", "Nombre"),
                Ix("UX_CurvasCatalogo_Empresa_Codigo_Activo", true, false, "Codigo IS NOT NULL AND Activo = 1 AND FechaArchivado IS NULL", "idEmpresa", "Codigo"),
                Ix("IX_CurvasCatalogo_Empresa_Estado", false, "idEmpresa", "Estado"),
            });

        private static SchemaTableContract Detalle() => new(
            "dbo",
            "CurvasDetalle",
            new[]
            {
                Col("id", "UNIQUEIDENTIFIER", false, def: "(NEWID())"),
                Col("idEmpresa", "UNIQUEIDENTIFIER", false),
                Col("identityKey", "UNIQUEIDENTIFIER", false, def: "(NEWID())"),
                Col("idCurva", "UNIQUEIDENTIFIER", false),
                Col("idProductoServicio", "UNIQUEIDENTIFIER", false),
                Col("idVariante", "UNIQUEIDENTIFIER", true),
                Col("TipoProductoServicio", "TINYINT", false, def: "((1))"),
                Col("CantidadBaseObjetivo", "DECIMAL(18,4)", false, precision: 18, scale: 4),
                Col("Activo", "BIT", false, def: "((1))"),
                Col("FechaCreacion", "DATETIME2(0)", false, scale: 0, def: "(SYSUTCDATETIME())"),
                Col("FechaActualizacion", "DATETIME2(0)", false, scale: 0, def: "(SYSUTCDATETIME())"),
                Col("FechaArchivado", "DATETIME2(0)", true, scale: 0),
                Col("idUsuarioCreacion", "UNIQUEIDENTIFIER", true),
                Col("idUsuarioActualizacion", "UNIQUEIDENTIFIER", true),
            },
            new SchemaPrimaryKeyContract("PK_CurvasDetalle", new[] { "id" }, true),
            new[]
            {
                new SchemaForeignKeyContract("FK_CurvasDetalle_CurvasCatalogo_EmpresaId", new[] { "idEmpresa", "idCurva" }, "dbo", "CurvasCatalogo", new[] { "idEmpresa", "id" }),
                new SchemaForeignKeyContract("FK_CurvasDetalle_ProductosServicios_EmpresaId", new[] { "idEmpresa", "idProductoServicio" }, "dbo", "ProductosServicios", new[] { "idEmpresa", "id" }),
                new SchemaForeignKeyContract("FK_CurvasDetalle_Variantes_EmpresaId", new[] { "idEmpresa", "idVariante" }, "dbo", "ProductosServiciosVariantes", new[] { "idEmpresa", "id" }),
            },
            new[]
            {
                Ux("UX_CurvasDetalle_Empresa_Id", null, "idEmpresa", "id"),
                Ux("UX_CurvasDetalle_Empresa_Curva_Producto_Variante_Activo", "Activo = 1 AND FechaArchivado IS NULL", "idEmpresa", "idCurva", "idProductoServicio", "idVariante"),
            },
            new[]
            {
                new SchemaCheckContract("CK_CurvasDetalle_TipoProducto", "CHECK (TipoProductoServicio = 1)"),
                new SchemaCheckContract("CK_CurvasDetalle_Cantidad", "CHECK (CantidadBaseObjetivo >= 0)"),
                new SchemaCheckContract("CK_CurvasDetalle_Archivado", "CHECK ((Activo = 1 AND FechaArchivado IS NULL) OR (Activo = 0 AND FechaArchivado IS NOT NULL))"),
            },
            new[]
            {
                Ix("UX_CurvasDetalle_Empresa_Id", true, "idEmpresa", "id"),
                Ix("UX_CurvasDetalle_Empresa_Curva_Producto_Variante_Activo", true, false, "Activo = 1 AND FechaArchivado IS NULL", "idEmpresa", "idCurva", "idProductoServicio", "idVariante"),
                Ix("IX_CurvasDetalle_Empresa_Producto_Variante", false, "idEmpresa", "idProductoServicio", "idVariante"),
            });

        private static SchemaTableContract Siembra() => new(
            "dbo",
            "CurvasSiembra",
            new[]
            {
                Col("id", "UNIQUEIDENTIFIER", false, def: "(NEWID())"),
                Col("idEmpresa", "UNIQUEIDENTIFIER", false),
                Col("identityKey", "UNIQUEIDENTIFIER", false, def: "(NEWID())"),
                Col("idSucursal", "UNIQUEIDENTIFIER", false),
                Col("idProductoServicio", "UNIQUEIDENTIFIER", false),
                Col("idVariante", "UNIQUEIDENTIFIER", true),
                Col("idCurva", "UNIQUEIDENTIFIER", false),
                Col("TipoProductoServicio", "TINYINT", false, def: "((1))"),
                Col("Estado", "TINYINT", false, def: "((1))"),
                Col("FechaVigenciaInicio", "DATETIME2(0)", false, scale: 0, def: "(SYSUTCDATETIME())"),
                Col("FechaVigenciaFin", "DATETIME2(0)", true, scale: 0),
                Col("FechaCreacion", "DATETIME2(0)", false, scale: 0, def: "(SYSUTCDATETIME())"),
                Col("FechaActualizacion", "DATETIME2(0)", false, scale: 0, def: "(SYSUTCDATETIME())"),
                Col("idUsuarioCreacion", "UNIQUEIDENTIFIER", true),
                Col("idUsuarioActualizacion", "UNIQUEIDENTIFIER", true),
            },
            new SchemaPrimaryKeyContract("PK_CurvasSiembra", new[] { "id" }, true),
            new[]
            {
                new SchemaForeignKeyContract("FK_CurvasSiembra_Sucursales_EmpresaId", new[] { "idEmpresa", "idSucursal" }, "dbo", "Sucursales", new[] { "idEmpresa", "id" }),
                new SchemaForeignKeyContract("FK_CurvasSiembra_ProductosServicios_EmpresaId", new[] { "idEmpresa", "idProductoServicio" }, "dbo", "ProductosServicios", new[] { "idEmpresa", "id" }),
                new SchemaForeignKeyContract("FK_CurvasSiembra_Variantes_EmpresaId", new[] { "idEmpresa", "idVariante" }, "dbo", "ProductosServiciosVariantes", new[] { "idEmpresa", "id" }),
                new SchemaForeignKeyContract("FK_CurvasSiembra_CurvasCatalogo_EmpresaId", new[] { "idEmpresa", "idCurva" }, "dbo", "CurvasCatalogo", new[] { "idEmpresa", "id" }),
            },
            new[]
            {
                Ux("UX_CurvasSiembra_Empresa_Id", null, "idEmpresa", "id"),
                Ux("UX_CurvasSiembra_Empresa_Sucursal_Producto_Variante_Vigente", "Estado = 1 AND FechaVigenciaFin IS NULL", "idEmpresa", "idSucursal", "idProductoServicio", "idVariante"),
            },
            new[]
            {
                new SchemaCheckContract("CK_CurvasSiembra_TipoProducto", "CHECK (TipoProductoServicio = 1)"),
                new SchemaCheckContract("CK_CurvasSiembra_Estado", "CHECK (Estado IN (1, 2, 3))"),
                new SchemaCheckContract("CK_CurvasSiembra_Vigencia", "CHECK ((Estado = 1 AND FechaVigenciaFin IS NULL) OR (Estado IN (2, 3) AND FechaVigenciaFin IS NOT NULL))"),
            },
            new[]
            {
                Ix("UX_CurvasSiembra_Empresa_Id", true, "idEmpresa", "id"),
                Ix("UX_CurvasSiembra_Empresa_Sucursal_Producto_Variante_Vigente", true, false, "Estado = 1 AND FechaVigenciaFin IS NULL", "idEmpresa", "idSucursal", "idProductoServicio", "idVariante"),
                Ix("IX_CurvasSiembra_Empresa_Curva", false, "idEmpresa", "idCurva", "Estado"),
                Ix("IX_CurvasSiembra_Empresa_Sucursal", false, "idEmpresa", "idSucursal", "Estado"),
            });

        private static SchemaTableContract OperacionesCompra() => new(
            "dbo",
            "CurvasOperacionesCompra",
            new[]
            {
                Col("id", "UNIQUEIDENTIFIER", false, def: "(NEWID())"),
                Col("idEmpresa", "UNIQUEIDENTIFIER", false),
                Col("identityKey", "UNIQUEIDENTIFIER", false, def: "(NEWID())"),
                Col("OperationKey", "NVARCHAR(120)", false, max: 120),
                Col("idProveedor", "UNIQUEIDENTIFIER", false),
                Col("Estado", "TINYINT", false, def: "((1))"),
                Col("SucursalCount", "INT", false, def: "((0))"),
                Col("OrdenCompraCount", "INT", false, def: "((0))"),
                Col("FechaOperacion", "DATETIME2(0)", false, scale: 0, def: "(SYSUTCDATETIME())"),
                Col("FechaCierre", "DATETIME2(0)", true, scale: 0),
                Col("ContextoJson", "NVARCHAR(MAX)", true, max: -1),
                Col("idUsuarioCreacion", "UNIQUEIDENTIFIER", true),
                Col("idUsuarioActualizacion", "UNIQUEIDENTIFIER", true),
            },
            new SchemaPrimaryKeyContract("PK_CurvasOperacionesCompra", new[] { "id" }, true),
            Array.Empty<SchemaForeignKeyContract>(),
            new[]
            {
                Ux("UX_CurvasOperacionesCompra_Empresa_Id", null, "idEmpresa", "id"),
                Ux("UX_CurvasOperacionesCompra_Empresa_OperationKey", null, "idEmpresa", "OperationKey"),
            },
            new[]
            {
                new SchemaCheckContract("CK_CurvasOperacionesCompra_Estado", "CHECK (Estado IN (1, 2, 3, 4))"),
                new SchemaCheckContract("CK_CurvasOperacionesCompra_Conteos", "CHECK (SucursalCount >= 0 AND OrdenCompraCount >= 0)"),
            },
            new[]
            {
                Ix("UX_CurvasOperacionesCompra_Empresa_Id", true, "idEmpresa", "id"),
                Ix("UX_CurvasOperacionesCompra_Empresa_OperationKey", true, "idEmpresa", "OperationKey"),
                Ix("IX_CurvasOperacionesCompra_Empresa_Proveedor_Estado", false, "idEmpresa", "idProveedor", "Estado"),
            });

        private static SchemaTableContract OperacionOrdenesCompra() => new(
            "dbo",
            "CurvasOperacionOrdenesCompra",
            new[]
            {
                Col("id", "UNIQUEIDENTIFIER", false, def: "(NEWID())"),
                Col("idEmpresa", "UNIQUEIDENTIFIER", false),
                Col("identityKey", "UNIQUEIDENTIFIER", false, def: "(NEWID())"),
                Col("idOperacionCurva", "UNIQUEIDENTIFIER", false),
                Col("idOrdenCompra", "UNIQUEIDENTIFIER", false),
                Col("idSucursal", "UNIQUEIDENTIFIER", false),
                Col("Estado", "TINYINT", false, def: "((1))"),
                Col("FechaCreacion", "DATETIME2(0)", false, scale: 0, def: "(SYSUTCDATETIME())"),
            },
            new SchemaPrimaryKeyContract("PK_CurvasOperacionOrdenesCompra", new[] { "id" }, true),
            new[]
            {
                new SchemaForeignKeyContract("FK_CurvasOperacionOC_Operacion_EmpresaId", new[] { "idEmpresa", "idOperacionCurva" }, "dbo", "CurvasOperacionesCompra", new[] { "idEmpresa", "id" }),
                new SchemaForeignKeyContract("FK_CurvasOperacionOC_OrdenesCompra_EmpresaId", new[] { "idEmpresa", "idOrdenCompra" }, "dbo", "OrdenesCompra", new[] { "idEmpresa", "id" }),
                new SchemaForeignKeyContract("FK_CurvasOperacionOC_Sucursales_EmpresaId", new[] { "idEmpresa", "idSucursal" }, "dbo", "Sucursales", new[] { "idEmpresa", "id" }),
            },
            new[]
            {
                Ux("UX_CurvasOperacionOC_Empresa_Id", null, "idEmpresa", "id"),
                Ux("UX_CurvasOperacionOC_Empresa_Operacion_OC", null, "idEmpresa", "idOperacionCurva", "idOrdenCompra"),
                Ux("UX_CurvasOperacionOC_Empresa_OC", null, "idEmpresa", "idOrdenCompra"),
            },
            new[] { new SchemaCheckContract("CK_CurvasOperacionOC_Estado", "CHECK (Estado IN (1, 2, 3))") },
            new[]
            {
                Ix("UX_CurvasOperacionOC_Empresa_Id", true, "idEmpresa", "id"),
                Ix("UX_CurvasOperacionOC_Empresa_Operacion_OC", true, "idEmpresa", "idOperacionCurva", "idOrdenCompra"),
                Ix("UX_CurvasOperacionOC_Empresa_OC", true, "idEmpresa", "idOrdenCompra"),
                Ix("IX_CurvasOperacionOC_Empresa_Sucursal", false, "idEmpresa", "idSucursal"),
            });

        private static SchemaTableContract SugerenciasSnapshot() => new(
            "dbo",
            "CurvasSugerenciasSnapshot",
            new[]
            {
                Col("id", "UNIQUEIDENTIFIER", false, def: "(NEWID())"),
                Col("idEmpresa", "UNIQUEIDENTIFIER", false),
                Col("identityKey", "UNIQUEIDENTIFIER", false, def: "(NEWID())"),
                Col("idOperacionCurva", "UNIQUEIDENTIFIER", false),
                Col("idOrdenCompra", "UNIQUEIDENTIFIER", true),
                Col("idOrdenCompraDetalle", "UNIQUEIDENTIFIER", true),
                Col("idCurva", "UNIQUEIDENTIFIER", true),
                Col("idSiembra", "UNIQUEIDENTIFIER", true),
                Col("idSucursal", "UNIQUEIDENTIFIER", false),
                Col("idProductoServicio", "UNIQUEIDENTIFIER", false),
                Col("idVariante", "UNIQUEIDENTIFIER", true),
                Col("TipoProductoServicio", "TINYINT", false, def: "((1))"),
                Col("Modo", "TINYINT", false),
                Col("CurvaObjetivoBase", "DECIMAL(18,4)", false, precision: 18, scale: 4),
                Col("ExistenciaSnapshotBase", "DECIMAL(18,4)", false, precision: 18, scale: 4),
                Col("TransitoSnapshotBase", "DECIMAL(18,4)", false, precision: 18, scale: 4),
                Col("CoberturaSnapshotBase", "DECIMAL(18,4)", false, precision: 18, scale: 4),
                Col("HuecoSnapshotBase", "DECIMAL(18,4)", false, precision: 18, scale: 4),
                Col("CopeteSnapshotBase", "DECIMAL(18,4)", false, precision: 18, scale: 4),
                Col("CantidadPropuestaBase", "DECIMAL(18,4)", false, precision: 18, scale: 4),
                Col("CantidadFinalBase", "DECIMAL(18,4)", false, precision: 18, scale: 4),
                Col("OverrideManual", "BIT", false, def: "((0))"),
                Col("NoPedir", "BIT", false, def: "((0))"),
                Col("idPresentacionCompra", "UNIQUEIDENTIFIER", true),
                Col("PermiteCantidadBaseSnapshot", "BIT", false, def: "((0))"),
                Col("ContextoJson", "NVARCHAR(MAX)", true, max: -1),
                Col("FechaSnapshot", "DATETIME2(0)", false, scale: 0, def: "(SYSUTCDATETIME())"),
                Col("idUsuarioCreacion", "UNIQUEIDENTIFIER", true),
            },
            new SchemaPrimaryKeyContract("PK_CurvasSugerenciasSnapshot", new[] { "id" }, true),
            new[]
            {
                new SchemaForeignKeyContract("FK_CurvasSnapshot_Operacion_EmpresaId", new[] { "idEmpresa", "idOperacionCurva" }, "dbo", "CurvasOperacionesCompra", new[] { "idEmpresa", "id" }),
                new SchemaForeignKeyContract("FK_CurvasSnapshot_OrdenesCompra_EmpresaId", new[] { "idEmpresa", "idOrdenCompra" }, "dbo", "OrdenesCompra", new[] { "idEmpresa", "id" }),
                new SchemaForeignKeyContract("FK_CurvasSnapshot_OrdenesCompraDetalle_EmpresaId", new[] { "idEmpresa", "idOrdenCompraDetalle" }, "dbo", "OrdenesCompraDetalle", new[] { "idEmpresa", "id" }),
                new SchemaForeignKeyContract("FK_CurvasSnapshot_CurvasCatalogo_EmpresaId", new[] { "idEmpresa", "idCurva" }, "dbo", "CurvasCatalogo", new[] { "idEmpresa", "id" }),
                new SchemaForeignKeyContract("FK_CurvasSnapshot_Siembra_EmpresaId", new[] { "idEmpresa", "idSiembra" }, "dbo", "CurvasSiembra", new[] { "idEmpresa", "id" }),
                new SchemaForeignKeyContract("FK_CurvasSnapshot_Sucursales_EmpresaId", new[] { "idEmpresa", "idSucursal" }, "dbo", "Sucursales", new[] { "idEmpresa", "id" }),
                new SchemaForeignKeyContract("FK_CurvasSnapshot_ProductosServicios_EmpresaId", new[] { "idEmpresa", "idProductoServicio" }, "dbo", "ProductosServicios", new[] { "idEmpresa", "id" }),
                new SchemaForeignKeyContract("FK_CurvasSnapshot_Variantes_EmpresaId", new[] { "idEmpresa", "idVariante" }, "dbo", "ProductosServiciosVariantes", new[] { "idEmpresa", "id" }),
                new SchemaForeignKeyContract("FK_CurvasSnapshot_PresentacionCompra_EmpresaId", new[] { "idEmpresa", "idPresentacionCompra" }, "dbo", "OrdenesCompraPresentacionesCompra", new[] { "idEmpresa", "id" }),
            },
            new[] { Ux("UX_CurvasSnapshot_Empresa_Id", null, "idEmpresa", "id") },
            new[]
            {
                new SchemaCheckContract("CK_CurvasSnapshot_TipoProducto", "CHECK (TipoProductoServicio = 1)"),
                new SchemaCheckContract("CK_CurvasSnapshot_Modo", "CHECK (Modo IN (1, 2, 3, 4))"),
                new SchemaCheckContract("CK_CurvasSnapshot_Cantidades", "CHECK (CurvaObjetivoBase >= 0 AND TransitoSnapshotBase >= 0 AND CoberturaSnapshotBase >= 0 AND HuecoSnapshotBase >= 0 AND CopeteSnapshotBase >= 0 AND CantidadPropuestaBase >= 0 AND CantidadFinalBase >= 0)"),
                new SchemaCheckContract("CK_CurvasSnapshot_Cobertura", "CHECK (CoberturaSnapshotBase = ExistenciaSnapshotBase + TransitoSnapshotBase)"),
                new SchemaCheckContract("CK_CurvasSnapshot_NoPedir", "CHECK ((NoPedir = 1 AND CantidadFinalBase = 0) OR NoPedir = 0)"),
            },
            new[]
            {
                Ix("UX_CurvasSnapshot_Empresa_Id", true, "idEmpresa", "id"),
                Ix("IX_CurvasSnapshot_Empresa_Operacion", false, "idEmpresa", "idOperacionCurva"),
                Ix("IX_CurvasSnapshot_Empresa_OC_Detalle", false, "idEmpresa", "idOrdenCompra", "idOrdenCompraDetalle"),
                Ix("IX_CurvasSnapshot_Empresa_Sucursal_Producto_Variante", false, "idEmpresa", "idSucursal", "idProductoServicio", "idVariante"),
            });

        private static SchemaColumnContract Col(
            string name,
            string type,
            bool nullable,
            int? max = null,
            byte? precision = null,
            int? scale = null,
            string? def = null) => new(name, type, max, precision, scale, nullable, def, false, false, null);

        private static SchemaUniqueContract Ux(string name, string? filter, params string[] columns) => new(name, columns, filter);

        private static SchemaIndexContract Ix(string name, bool unique, params string[] columns) =>
            new(name, unique, false, Keys(columns), Array.Empty<string>(), null);

        private static SchemaIndexContract Ix(string name, bool unique, bool clustered, string? filter, params string[] columns) =>
            new(name, unique, clustered, Keys(columns), Array.Empty<string>(), filter);

        private static SchemaIndexColumnContract[] Keys(params string[] columns) =>
            columns.Select(column => new SchemaIndexColumnContract(column, false)).ToArray();
    }
}
