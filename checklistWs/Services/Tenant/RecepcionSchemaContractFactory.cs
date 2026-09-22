namespace checklistWs.Services.Tenant
{
    internal static class RecepcionSchemaContractFactory
    {
        public static SchemaContract GetContract(int version)
        {
            if (version != ProductosServiciosSchemaContractProvider.RecepcionLatestVersion)
            {
                throw new InvalidOperationException("SCHEMA_CONTRACT_VERSION_NOT_SUPPORTED");
            }

            return new SchemaContract(
                DatabaseScopes.Recepcion,
                ProductosServiciosSchemaContractProvider.RecepcionLatestVersion,
                "Recepcion de OC",
                new[] { Folios(), Cabecera(), Partidas(), Series() },
                new[]
                {
                    "inspectorapi/checklistWs/Scripts/recepcion-up.sql",
                    "inspectorapi/checklistWs/Controllers/Recepcion/RecepcionController.cs",
                    "inspectorapi/checklistWs/Services/Tenant/RecepcionScopeService.cs",
                    "inspector/docs/compras/BL03_FASE_A_REC01_MODELO_SCHEMA_RECEPCION_20260921.md",
                },
                new DateTime(2026, 9, 21, 0, 0, 0, DateTimeKind.Utc));
        }

        private static SchemaTableContract Folios() => new(
            "dbo",
            "RecepcionFolios",
            new[]
            {
                Col("id", "UNIQUEIDENTIFIER", false, def: "(NEWID())"),
                Col("idEmpresa", "UNIQUEIDENTIFIER", false),
                Col("identityKey", "UNIQUEIDENTIFIER", false, def: "(NEWID())"),
                Col("UltimoConsecutivo", "BIGINT", false, def: "((0))"),
                Col("FechaCreacion", "DATETIME2(0)", false, scale: 0, def: "(SYSUTCDATETIME())"),
                Col("FechaActualizacion", "DATETIME2(0)", false, scale: 0, def: "(SYSUTCDATETIME())"),
            },
            new SchemaPrimaryKeyContract("PK_RecepcionFolios", new[] { "id" }, true),
            Array.Empty<SchemaForeignKeyContract>(),
            new[]
            {
                new SchemaUniqueContract("UX_RecepcionFolios_Empresa_Id", new[] { "idEmpresa", "id" }, null),
                new SchemaUniqueContract("UX_RecepcionFolios_Empresa", new[] { "idEmpresa" }, null),
            },
            new[] { new SchemaCheckContract("CK_RecepcionFolios_UltimoConsecutivo", "CHECK (UltimoConsecutivo >= 0)") },
            new[]
            {
                Ix("UX_RecepcionFolios_Empresa_Id", true, "idEmpresa", "id"),
                Ix("UX_RecepcionFolios_Empresa", true, "idEmpresa"),
            });

        private static SchemaTableContract Cabecera() => new(
            "dbo",
            "Recepciones",
            new[]
            {
                Col("id", "UNIQUEIDENTIFIER", false, def: "(NEWID())"),
                Col("idEmpresa", "UNIQUEIDENTIFIER", false),
                Col("identityKey", "UNIQUEIDENTIFIER", false, def: "(NEWID())"),
                Col("idOrdenCompra", "UNIQUEIDENTIFIER", false),
                Col("FolioRecepcion", "NVARCHAR(30)", false, max: 30),
                Col("idSucursal", "UNIQUEIDENTIFIER", false),
                Col("FechaRecepcion", "DATETIME2(0)", false, scale: 0),
                Col("Estado", "TINYINT", false, def: "((2))"),
                Col("OperationKey", "NVARCHAR(120)", false, max: 120),
                Col("Observaciones", "NVARCHAR(1000)", true, max: 1000),
                Col("FechaCancelacion", "DATETIME2(0)", true, scale: 0),
                Col("MotivoCancelacion", "NVARCHAR(500)", true, max: 500),
                Col("idUsuarioCreacion", "UNIQUEIDENTIFIER", true),
                Col("idUsuarioConfirmacion", "UNIQUEIDENTIFIER", true),
                Col("FechaCreacion", "DATETIME2(0)", false, scale: 0, def: "(SYSUTCDATETIME())"),
                Col("FechaActualizacion", "DATETIME2(0)", false, scale: 0, def: "(SYSUTCDATETIME())"),
            },
            new SchemaPrimaryKeyContract("PK_Recepciones", new[] { "id" }, true),
            new[]
            {
                new SchemaForeignKeyContract("FK_Recepciones_OrdenesCompra_EmpresaId", new[] { "idEmpresa", "idOrdenCompra" }, "dbo", "OrdenesCompra", new[] { "idEmpresa", "id" }),
                new SchemaForeignKeyContract("FK_Recepciones_Sucursales_EmpresaId", new[] { "idEmpresa", "idSucursal" }, "dbo", "Sucursales", new[] { "idEmpresa", "id" }),
            },
            new[]
            {
                new SchemaUniqueContract("UX_Recepciones_Empresa_Id", new[] { "idEmpresa", "id" }, null),
                new SchemaUniqueContract("UX_Recepciones_Empresa_Folio", new[] { "idEmpresa", "FolioRecepcion" }, null),
                new SchemaUniqueContract("UX_Recepciones_Empresa_OperationKey", new[] { "idEmpresa", "OperationKey" }, null),
            },
            new[]
            {
                new SchemaCheckContract("CK_Recepciones_Estado", "CHECK (Estado IN (1, 2, 3))"),
                new SchemaCheckContract("CK_Recepciones_Cancelacion", "CHECK ([Estado]=(3) AND [FechaCancelacion] IS NOT NULL AND NULLIF(LTRIM(RTRIM([MotivoCancelacion])), N'') IS NOT NULL OR ([Estado]=(2) OR [Estado]=(1)) AND [FechaCancelacion] IS NULL AND [MotivoCancelacion] IS NULL)"),
            },
            new[]
            {
                Ix("UX_Recepciones_Empresa_Id", true, "idEmpresa", "id"),
                Ix("UX_Recepciones_Empresa_Folio", true, "idEmpresa", "FolioRecepcion"),
                Ix("UX_Recepciones_Empresa_OperationKey", true, "idEmpresa", "OperationKey"),
                Ix("IX_Recepciones_Empresa_OC", false, "idEmpresa", "idOrdenCompra"),
                Ix("IX_Recepciones_Empresa_Fecha", false, "idEmpresa", "FechaRecepcion"),
                Ix("IX_Recepciones_Empresa_Estado", false, "idEmpresa", "Estado"),
                Ix("IX_Recepciones_Empresa_Sucursal", false, "idEmpresa", "idSucursal"),
            });

        private static SchemaTableContract Partidas() => new(
            "dbo",
            "RecepcionPartidas",
            new[]
            {
                Col("id", "UNIQUEIDENTIFIER", false, def: "(NEWID())"),
                Col("idEmpresa", "UNIQUEIDENTIFIER", false),
                Col("identityKey", "UNIQUEIDENTIFIER", false, def: "(NEWID())"),
                Col("idRecepcion", "UNIQUEIDENTIFIER", false),
                Col("idOrdenCompra", "UNIQUEIDENTIFIER", false),
                Col("idOrdenCompraDetalle", "UNIQUEIDENTIFIER", false),
                Col("NumeroPartida", "INT", false),
                Col("TipoPartida", "TINYINT", false),
                Col("idProductoServicio", "UNIQUEIDENTIFIER", false),
                Col("idVariante", "UNIQUEIDENTIFIER", true),
                Col("idPresentacionCompra", "UNIQUEIDENTIFIER", true),
                Col("PresentacionCompraSnapshot", "NVARCHAR(150)", true, max: 150),
                Col("UnidadCompraSnapshot", "NVARCHAR(100)", true, max: 100),
                Col("UnidadCompraAbreviaturaSnapshot", "NVARCHAR(20)", true, max: 20),
                Col("FactorConversionSnapshot", "DECIMAL(28,12)", false, precision: 28, scale: 12),
                Col("CantidadCompraRecibida", "DECIMAL(18,4)", false, precision: 18, scale: 4),
                Col("CantidadBaseOrdenada", "DECIMAL(18,4)", false, precision: 18, scale: 4),
                Col("CantidadBaseRecibidaAnterior", "DECIMAL(18,4)", false, precision: 18, scale: 4),
                Col("CantidadBaseEstaRecepcion", "DECIMAL(18,4)", false, precision: 18, scale: 4),
                Col("CantidadBaseRecibidaAcumulada", "DECIMAL(18,4)", false, precision: 18, scale: 4),
                Col("CantidadBasePendiente", "DECIMAL(18,4)", false, precision: 18, scale: 4),
                Col("EstadoPartidaRecepcion", "TINYINT", false),
                Col("ControlSerie", "BIT", false, def: "((0))"),
                Col("idInventarioMovimiento", "UNIQUEIDENTIFIER", true),
                Col("FechaCreacion", "DATETIME2(0)", false, scale: 0, def: "(SYSUTCDATETIME())"),
            },
            new SchemaPrimaryKeyContract("PK_RecepcionPartidas", new[] { "id" }, true),
            new[]
            {
                new SchemaForeignKeyContract("FK_RecepcionPartidas_Recepciones_EmpresaId", new[] { "idEmpresa", "idRecepcion" }, "dbo", "Recepciones", new[] { "idEmpresa", "id" }),
                new SchemaForeignKeyContract("FK_RecepcionPartidas_OrdenesCompra_EmpresaId", new[] { "idEmpresa", "idOrdenCompra" }, "dbo", "OrdenesCompra", new[] { "idEmpresa", "id" }),
                new SchemaForeignKeyContract("FK_RecepcionPartidas_OrdenesCompraDetalle_EmpresaId", new[] { "idEmpresa", "idOrdenCompraDetalle" }, "dbo", "OrdenesCompraDetalle", new[] { "idEmpresa", "id" }),
                new SchemaForeignKeyContract("FK_RecepcionPartidas_ProductosServicios_EmpresaId", new[] { "idEmpresa", "idProductoServicio" }, "dbo", "ProductosServicios", new[] { "idEmpresa", "id" }),
                new SchemaForeignKeyContract("FK_RecepcionPartidas_Variantes_EmpresaId", new[] { "idEmpresa", "idVariante" }, "dbo", "ProductosServiciosVariantes", new[] { "idEmpresa", "id" }),
                new SchemaForeignKeyContract("FK_RecepcionPartidas_InventarioMovimiento_EmpresaId", new[] { "idEmpresa", "idInventarioMovimiento" }, "dbo", "InventarioMovimientos", new[] { "idEmpresa", "id" }),
            },
            new[] { new SchemaUniqueContract("UX_RecepcionPartidas_Empresa_Id", new[] { "idEmpresa", "id" }, null) },
            new[]
            {
                new SchemaCheckContract("CK_RecepcionPartidas_Tipo", "CHECK (TipoPartida IN (1, 2))"),
                new SchemaCheckContract("CK_RecepcionPartidas_Cantidades", "CHECK (CantidadCompraRecibida > 0 AND FactorConversionSnapshot > 0 AND CantidadBaseOrdenada > 0 AND CantidadBaseRecibidaAnterior >= 0 AND CantidadBaseEstaRecepcion > 0 AND CantidadBaseRecibidaAcumulada >= CantidadBaseRecibidaAnterior AND CantidadBasePendiente >= 0 AND CantidadBaseOrdenada = CantidadBaseRecibidaAcumulada + CantidadBasePendiente)"),
                new SchemaCheckContract("CK_RecepcionPartidas_NoSobreRecepcion", "CHECK (CantidadBaseEstaRecepcion <= (CantidadBaseOrdenada - CantidadBaseRecibidaAnterior))"),
                new SchemaCheckContract("CK_RecepcionPartidas_Estado", "CHECK (EstadoPartidaRecepcion IN (1, 2, 3))"),
            },
            new[]
            {
                Ix("UX_RecepcionPartidas_Empresa_Id", true, "idEmpresa", "id"),
                Ix("IX_RecepcionPartidas_Empresa_Recepcion", false, "idEmpresa", "idRecepcion"),
                Ix("IX_RecepcionPartidas_Empresa_OCDetalle", false, "idEmpresa", "idOrdenCompraDetalle"),
                Ix("IX_RecepcionPartidas_Empresa_Producto_Variante", false, "idEmpresa", "idProductoServicio", "idVariante"),
            });

        private static SchemaTableContract Series() => new(
            "dbo",
            "RecepcionSeries",
            new[]
            {
                Col("id", "UNIQUEIDENTIFIER", false, def: "(NEWID())"),
                Col("idEmpresa", "UNIQUEIDENTIFIER", false),
                Col("identityKey", "UNIQUEIDENTIFIER", false, def: "(NEWID())"),
                Col("idRecepcion", "UNIQUEIDENTIFIER", false),
                Col("idRecepcionPartida", "UNIQUEIDENTIFIER", false),
                Col("idProductoServicio", "UNIQUEIDENTIFIER", false),
                Col("idVariante", "UNIQUEIDENTIFIER", true),
                Col("NumeroSerie", "NVARCHAR(120)", false, max: 120),
                Col("idInventarioSerie", "UNIQUEIDENTIFIER", true),
                Col("FechaCreacion", "DATETIME2(0)", false, scale: 0, def: "(SYSUTCDATETIME())"),
            },
            new SchemaPrimaryKeyContract("PK_RecepcionSeries", new[] { "id" }, true),
            new[]
            {
                new SchemaForeignKeyContract("FK_RecepcionSeries_Recepciones_EmpresaId", new[] { "idEmpresa", "idRecepcion" }, "dbo", "Recepciones", new[] { "idEmpresa", "id" }),
                new SchemaForeignKeyContract("FK_RecepcionSeries_RecepcionPartidas_EmpresaId", new[] { "idEmpresa", "idRecepcionPartida" }, "dbo", "RecepcionPartidas", new[] { "idEmpresa", "id" }),
                new SchemaForeignKeyContract("FK_RecepcionSeries_ProductosServicios_EmpresaId", new[] { "idEmpresa", "idProductoServicio" }, "dbo", "ProductosServicios", new[] { "idEmpresa", "id" }),
                new SchemaForeignKeyContract("FK_RecepcionSeries_Variantes_EmpresaId", new[] { "idEmpresa", "idVariante" }, "dbo", "ProductosServiciosVariantes", new[] { "idEmpresa", "id" }),
                new SchemaForeignKeyContract("FK_RecepcionSeries_InventarioSeries_EmpresaId", new[] { "idEmpresa", "idInventarioSerie" }, "dbo", "InventarioSeries", new[] { "idEmpresa", "id" }),
            },
            new[]
            {
                new SchemaUniqueContract("UX_RecepcionSeries_Empresa_Id", new[] { "idEmpresa", "id" }, null),
                new SchemaUniqueContract("UX_RecepcionSeries_Empresa_Partida_Numero", new[] { "idEmpresa", "idRecepcionPartida", "NumeroSerie" }, null),
            },
            Array.Empty<SchemaCheckContract>(),
            new[]
            {
                Ix("UX_RecepcionSeries_Empresa_Id", true, "idEmpresa", "id"),
                Ix("UX_RecepcionSeries_Empresa_Partida_Numero", true, "idEmpresa", "idRecepcionPartida", "NumeroSerie"),
                Ix("IX_RecepcionSeries_Empresa_Producto_Variante", false, "idEmpresa", "idProductoServicio", "idVariante", "NumeroSerie"),
            });

        private static SchemaColumnContract Col(string name, string type, bool nullable, int? max = null, byte? precision = null, int? scale = null, string? def = null)
            => new(name, type, max, precision, scale, nullable, def, false, false, null);

        private static SchemaIndexContract Ix(string name, bool unique, params string[] columns)
            => new(name, unique, false, Keys(columns), Array.Empty<string>(), null);

        private static SchemaIndexColumnContract[] Keys(params string[] columns)
            => columns.Select(column => new SchemaIndexColumnContract(column, false)).ToArray();
    }
}
