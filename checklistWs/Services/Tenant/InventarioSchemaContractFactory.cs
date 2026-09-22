namespace checklistWs.Services.Tenant
{
    internal static class InventarioSchemaContractFactory
    {
        public static SchemaContract GetContract(int version)
        {
            if (version != ProductosServiciosSchemaContractProvider.InventarioLatestVersion)
            {
                throw new InvalidOperationException("SCHEMA_CONTRACT_VERSION_NOT_SUPPORTED");
            }

            return new SchemaContract(
                DatabaseScopes.Inventario,
                ProductosServiciosSchemaContractProvider.InventarioLatestVersion,
                "Inventario",
                new[]
                {
                    Saldos(),
                    Movimientos(),
                    Series()
                },
                new[]
                {
                    "inspectorapi/checklistWs/Scripts/inventario-up.sql",
                    "inspectorapi/checklistWs/Services/Tenant/InventarioScopeLedgerService.cs",
                    "inspector/docs/compras/BL03_FASE_A_ARQ01_INVENTARIO_VARIANTE_SUCURSAL_20260921.md",
                    "inspector/docs/compras/BL03_FASE_A_OC02_MODELO_SCHEMA_OC_20260921.md",
                },
                new DateTime(2026, 9, 21, 0, 0, 0, DateTimeKind.Utc));
        }

        private static SchemaTableContract Saldos() => new(
            "dbo",
            "InventarioSaldos",
            new[]
            {
                Col("id", "UNIQUEIDENTIFIER", false, def: "(NEWID())"),
                Col("idEmpresa", "UNIQUEIDENTIFIER", false),
                Col("identityKey", "UNIQUEIDENTIFIER", false, def: "(NEWID())"),
                Col("idSucursal", "UNIQUEIDENTIFIER", false),
                Col("idProductoServicio", "UNIQUEIDENTIFIER", false),
                Col("idVariante", "UNIQUEIDENTIFIER", true),
                Col("CantidadBaseActual", "DECIMAL(28,12)", false, precision: 28, scale: 12, def: "((0))"),
                Col("FechaCreacion", "DATETIME2(0)", false, scale: 0, def: "(SYSUTCDATETIME())"),
                Col("FechaActualizacion", "DATETIME2(0)", false, scale: 0, def: "(SYSUTCDATETIME())"),
            },
            new SchemaPrimaryKeyContract("PK_InventarioSaldos", new[] { "id" }, true),
            new[]
            {
                new SchemaForeignKeyContract("FK_InventarioSaldos_Sucursales_EmpresaId", new[] { "idEmpresa", "idSucursal" }, "dbo", "Sucursales", new[] { "idEmpresa", "id" }),
                new SchemaForeignKeyContract("FK_InventarioSaldos_ProductosServicios_EmpresaId", new[] { "idEmpresa", "idProductoServicio" }, "dbo", "ProductosServicios", new[] { "idEmpresa", "id" }),
                new SchemaForeignKeyContract("FK_InventarioSaldos_Variantes_EmpresaId", new[] { "idEmpresa", "idVariante" }, "dbo", "ProductosServiciosVariantes", new[] { "idEmpresa", "id" }),
            },
            new[]
            {
                new SchemaUniqueContract("UX_InventarioSaldos_Empresa_Id", new[] { "idEmpresa", "id" }, null),
                new SchemaUniqueContract("UX_InventarioSaldos_Empresa_Sucursal_Producto_Variante", new[] { "idEmpresa", "idSucursal", "idProductoServicio", "idVariante" }, null),
            },
            new[]
            {
                new SchemaCheckContract("CK_InventarioSaldos_Cantidad", "CHECK (CantidadBaseActual >= 0)"),
            },
            new[]
            {
                Ix("UX_InventarioSaldos_Empresa_Id", true, "idEmpresa", "id"),
                Ix("UX_InventarioSaldos_Empresa_Sucursal_Producto_Variante", true, "idEmpresa", "idSucursal", "idProductoServicio", "idVariante"),
                Ix("IX_InventarioSaldos_Empresa_Producto_Variante", false, "idEmpresa", "idProductoServicio", "idVariante"),
            });

        private static SchemaTableContract Movimientos() => new(
            "dbo",
            "InventarioMovimientos",
            new[]
            {
                Col("id", "UNIQUEIDENTIFIER", false, def: "(NEWID())"),
                Col("idEmpresa", "UNIQUEIDENTIFIER", false),
                Col("identityKey", "UNIQUEIDENTIFIER", false, def: "(NEWID())"),
                Col("idSucursal", "UNIQUEIDENTIFIER", false),
                Col("idProductoServicio", "UNIQUEIDENTIFIER", false),
                Col("idVariante", "UNIQUEIDENTIFIER", true),
                Col("TipoMovimiento", "TINYINT", false),
                Col("CantidadBase", "DECIMAL(28,12)", false, precision: 28, scale: 12),
                Col("SaldoAnterior", "DECIMAL(28,12)", false, precision: 28, scale: 12),
                Col("SaldoPosterior", "DECIMAL(28,12)", false, precision: 28, scale: 12),
                Col("OrigenTipo", "NVARCHAR(40)", false, max: 40),
                Col("OrigenId", "UNIQUEIDENTIFIER", true),
                Col("OrigenPartidaId", "UNIQUEIDENTIFIER", true),
                Col("OperationKey", "NVARCHAR(120)", false, max: 120),
                Col("Observaciones", "NVARCHAR(500)", true, max: 500),
                Col("idUsuario", "UNIQUEIDENTIFIER", true),
                Col("FechaMovimiento", "DATETIME2(0)", false, scale: 0, def: "(SYSUTCDATETIME())"),
                Col("FechaCreacion", "DATETIME2(0)", false, scale: 0, def: "(SYSUTCDATETIME())"),
            },
            new SchemaPrimaryKeyContract("PK_InventarioMovimientos", new[] { "id" }, true),
            new[]
            {
                new SchemaForeignKeyContract("FK_InventarioMovimientos_Sucursales_EmpresaId", new[] { "idEmpresa", "idSucursal" }, "dbo", "Sucursales", new[] { "idEmpresa", "id" }),
                new SchemaForeignKeyContract("FK_InventarioMovimientos_ProductosServicios_EmpresaId", new[] { "idEmpresa", "idProductoServicio" }, "dbo", "ProductosServicios", new[] { "idEmpresa", "id" }),
                new SchemaForeignKeyContract("FK_InventarioMovimientos_Variantes_EmpresaId", new[] { "idEmpresa", "idVariante" }, "dbo", "ProductosServiciosVariantes", new[] { "idEmpresa", "id" }),
            },
            new[]
            {
                new SchemaUniqueContract("UX_InventarioMovimientos_Empresa_Id", new[] { "idEmpresa", "id" }, null),
                new SchemaUniqueContract("UX_InventarioMovimientos_Empresa_OperationKey", new[] { "idEmpresa", "OperationKey" }, null),
            },
            new[]
            {
                new SchemaCheckContract("CK_InventarioMovimientos_Tipo", "CHECK (TipoMovimiento IN (1, 2, 3, 4, 5))"),
                new SchemaCheckContract("CK_InventarioMovimientos_Cantidad", "CHECK (CantidadBase > 0)"),
                new SchemaCheckContract("CK_InventarioMovimientos_Saldos", "CHECK (SaldoAnterior >= 0 AND SaldoPosterior >= 0)"),
            },
            new[]
            {
                Ix("UX_InventarioMovimientos_Empresa_Id", true, "idEmpresa", "id"),
                Ix("UX_InventarioMovimientos_Empresa_OperationKey", true, "idEmpresa", "OperationKey"),
                Ix("IX_InventarioMovimientos_Empresa_Sucursal_Producto_Variante_Fecha", false, "idEmpresa", "idSucursal", "idProductoServicio", "idVariante", "FechaMovimiento"),
                Ix("IX_InventarioMovimientos_Empresa_Origen", false, "idEmpresa", "OrigenTipo", "OrigenId", "OrigenPartidaId"),
            });

        private static SchemaTableContract Series() => new(
            "dbo",
            "InventarioSeries",
            new[]
            {
                Col("id", "UNIQUEIDENTIFIER", false, def: "(NEWID())"),
                Col("idEmpresa", "UNIQUEIDENTIFIER", false),
                Col("identityKey", "UNIQUEIDENTIFIER", false, def: "(NEWID())"),
                Col("idProductoServicio", "UNIQUEIDENTIFIER", false),
                Col("idVariante", "UNIQUEIDENTIFIER", true),
                Col("NumeroSerie", "NVARCHAR(120)", false, max: 120),
                Col("idSucursalActual", "UNIQUEIDENTIFIER", false),
                Col("Estado", "TINYINT", false, def: "((1))"),
                Col("OrigenMovimientoId", "UNIQUEIDENTIFIER", true),
                Col("FechaCreacion", "DATETIME2(0)", false, scale: 0, def: "(SYSUTCDATETIME())"),
                Col("FechaActualizacion", "DATETIME2(0)", false, scale: 0, def: "(SYSUTCDATETIME())"),
                Col("FechaArchivado", "DATETIME2(0)", true, scale: 0),
            },
            new SchemaPrimaryKeyContract("PK_InventarioSeries", new[] { "id" }, true),
            new[]
            {
                new SchemaForeignKeyContract("FK_InventarioSeries_ProductosServicios_EmpresaId", new[] { "idEmpresa", "idProductoServicio" }, "dbo", "ProductosServicios", new[] { "idEmpresa", "id" }),
                new SchemaForeignKeyContract("FK_InventarioSeries_Variantes_EmpresaId", new[] { "idEmpresa", "idVariante" }, "dbo", "ProductosServiciosVariantes", new[] { "idEmpresa", "id" }),
                new SchemaForeignKeyContract("FK_InventarioSeries_Sucursales_EmpresaId", new[] { "idEmpresa", "idSucursalActual" }, "dbo", "Sucursales", new[] { "idEmpresa", "id" }),
                new SchemaForeignKeyContract("FK_InventarioSeries_Movimiento_EmpresaId", new[] { "idEmpresa", "OrigenMovimientoId" }, "dbo", "InventarioMovimientos", new[] { "idEmpresa", "id" }),
            },
            new[]
            {
                new SchemaUniqueContract("UX_InventarioSeries_Empresa_Id", new[] { "idEmpresa", "id" }, null),
                new SchemaUniqueContract("UX_InventarioSeries_Empresa_Producto_Variante_Numero_Activo", new[] { "idEmpresa", "idProductoServicio", "idVariante", "NumeroSerie" }, "Estado = 1 AND FechaArchivado IS NULL"),
            },
            new[]
            {
                new SchemaCheckContract("CK_InventarioSeries_Estado", "CHECK (Estado IN (1, 2, 3))"),
                new SchemaCheckContract("CK_InventarioSeries_Archivado", "CHECK ((Estado = 1 AND FechaArchivado IS NULL) OR (Estado = 3 OR Estado = 2))"),
            },
            new[]
            {
                Ix("UX_InventarioSeries_Empresa_Id", true, "idEmpresa", "id"),
                Ix("UX_InventarioSeries_Empresa_Producto_Variante_Numero_Activo", true, false, "Estado = 1 AND FechaArchivado IS NULL", "idEmpresa", "idProductoServicio", "idVariante", "NumeroSerie"),
                Ix("IX_InventarioSeries_Empresa_Sucursal", false, "idEmpresa", "idSucursalActual", "Estado"),
            });

        private static SchemaColumnContract Col(string name, string sqlType, bool nullable, int? max = null, byte? precision = null, int? scale = null, string? def = null)
            => new(name, sqlType, max, precision, scale, nullable, def, false, false, null);

        private static SchemaIndexContract Ix(string name, bool unique, params string[] columns)
            => Ix(name, unique, false, null, columns);

        private static SchemaIndexContract Ix(string name, bool unique, bool clustered, string? filter, params string[] columns)
            => new(name, unique, clustered, columns.Select(column => new SchemaIndexColumnContract(column, false)).ToArray(), Array.Empty<string>(), filter);
    }
}
