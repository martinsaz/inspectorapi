namespace checklistWs.Services.Tenant
{
    internal static class ListaPreciosSchemaContractFactory
    {
        public static SchemaContract GetContract(int version)
        {
            if (version is not ProductosServiciosSchemaContractProvider.V1 and not ProductosServiciosSchemaContractProvider.V2)
            {
                throw new InvalidOperationException("SCHEMA_CONTRACT_VERSION_NOT_SUPPORTED");
            }

            return new SchemaContract(
                DatabaseScopes.ListaPrecios,
                version,
                "Lista de Precios CheckApp",
                Tables(version),
                Sources(version),
                version == ProductosServiciosSchemaContractProvider.V1
                    ? new DateTime(2026, 9, 28, 0, 0, 0, DateTimeKind.Utc)
                    : new DateTime(2026, 9, 29, 0, 0, 0, DateTimeKind.Utc));
        }

        private static IReadOnlyCollection<string> Sources(int version)
        {
            List<string> sources = new()
            {
                "inspector/docs/lista-precios/LP_01_CONTRATO_FUNCIONAL_MODELO_RESOLUCION_LISTA_PRECIOS_CHECKAPP_20260928.md",
                "inspector/docs/lista-precios/LP_02_SCHEMA_V1_VERSIONADO_GATE_20260928.md",
            };

            if (version >= ProductosServiciosSchemaContractProvider.V2)
            {
                sources.Add("inspector/docs/lista-precios/LP_06_CONTRATO_FUNCIONAL_EXTENDIDO_COMERCIAL_20260929.md");
            }

            return sources;
        }

        private static IReadOnlyCollection<SchemaTableContract> Tables(int version)
        {
            List<SchemaTableContract> tables = new()
            {
                Listas(),
                Detalle(version),
            };

            if (version >= ProductosServiciosSchemaContractProvider.V2)
            {
                tables.Add(Promociones());
                tables.Add(Historial());
            }

            return tables;
        }

        private static SchemaTableContract Listas() => new(
            "dbo",
            "ListaPreciosListas",
            new[]
            {
                Col("id", "UNIQUEIDENTIFIER", false, def: "(NEWID())"),
                Col("idEmpresa", "UNIQUEIDENTIFIER", false),
                Col("identityKey", "UNIQUEIDENTIFIER", false, def: "(NEWID())"),
                Col("Nivel", "TINYINT", false),
                Col("Nombre", "NVARCHAR(100)", false, max: 100),
                Col("EsDefault", "BIT", false, def: "((0))"),
                Col("Activo", "BIT", false, def: "((1))"),
                Col("FechaCreacion", "DATETIME2(0)", false, scale: 0, def: "(SYSUTCDATETIME())"),
                Col("FechaActualizacion", "DATETIME2(0)", false, scale: 0, def: "(SYSUTCDATETIME())"),
                Col("FechaArchivado", "DATETIME2(0)", true, scale: 0),
                Col("idUsuarioCreacion", "UNIQUEIDENTIFIER", true),
                Col("idUsuarioActualizacion", "UNIQUEIDENTIFIER", true),
                Col("idUsuarioArchivado", "UNIQUEIDENTIFIER", true),
            },
            new SchemaPrimaryKeyContract("PK_ListaPreciosListas", new[] { "id" }, true),
            Array.Empty<SchemaForeignKeyContract>(),
            new[]
            {
                Ux("UX_ListaPreciosListas_Empresa_Id", null, "idEmpresa", "id"),
                Ux("UX_ListaPreciosListas_Empresa_Nivel", null, "idEmpresa", "Nivel"),
                Ux("UX_ListaPreciosListas_Empresa_Default_Activo", "EsDefault = 1 AND Activo = 1 AND FechaArchivado IS NULL", "idEmpresa"),
            },
            new[]
            {
                new SchemaCheckContract("CK_ListaPreciosListas_Nivel", "CHECK (Nivel BETWEEN 1 AND 10)"),
                new SchemaCheckContract("CK_ListaPreciosListas_Archivado", "CHECK ((Activo = 1 AND FechaArchivado IS NULL AND idUsuarioArchivado IS NULL) OR (Activo = 0 AND FechaArchivado IS NOT NULL))"),
            },
            new[]
            {
                Ix("UX_ListaPreciosListas_Empresa_Id", true, "idEmpresa", "id"),
                Ix("UX_ListaPreciosListas_Empresa_Nivel", true, "idEmpresa", "Nivel"),
                Ix("UX_ListaPreciosListas_Empresa_Default_Activo", true, false, "EsDefault = 1 AND Activo = 1 AND FechaArchivado IS NULL", "idEmpresa"),
                Ix("IX_ListaPreciosListas_Empresa_Activo", false, "idEmpresa", "Activo"),
            });

        private static SchemaTableContract Detalle(int version) => new(
            "dbo",
            "ListaPreciosDetalle",
            DetalleColumns(version),
            new SchemaPrimaryKeyContract("PK_ListaPreciosDetalle", new[] { "id" }, true),
            new[]
            {
                new SchemaForeignKeyContract("FK_ListaPreciosDetalle_Listas_EmpresaId", new[] { "idEmpresa", "idListaPrecio" }, "dbo", "ListaPreciosListas", new[] { "idEmpresa", "id" }),
                new SchemaForeignKeyContract("FK_ListaPreciosDetalle_ProductosServicios_EmpresaId", new[] { "idEmpresa", "idProductoServicio" }, "dbo", "ProductosServicios", new[] { "idEmpresa", "id" }),
                new SchemaForeignKeyContract("FK_ListaPreciosDetalle_Variantes_EmpresaId", new[] { "idEmpresa", "idVariante" }, "dbo", "ProductosServiciosVariantes", new[] { "idEmpresa", "id" }),
                new SchemaForeignKeyContract("FK_ListaPreciosDetalle_PresentacionesVenta_EmpresaId", new[] { "idEmpresa", "idPresentacionVenta" }, "dbo", "ProductosServiciosPresentacionesVenta", new[] { "idEmpresa", "id" }),
            },
            new[]
            {
                Ux("UX_ListaPreciosDetalle_Empresa_Id", null, "idEmpresa", "id"),
                Ux("UX_ListaPreciosDetalle_Empresa_Lista_Identidad_Activo", "Activo = 1 AND FechaArchivado IS NULL", "idEmpresa", "idListaPrecio", "TipoIdentidad", "idProductoServicio", "idVariante", "idPresentacionVenta"),
            },
            DetalleChecks(version),
            new[]
            {
                Ix("UX_ListaPreciosDetalle_Empresa_Id", true, "idEmpresa", "id"),
                Ix("UX_ListaPreciosDetalle_Empresa_Lista_Identidad_Activo", true, false, "Activo = 1 AND FechaArchivado IS NULL", "idEmpresa", "idListaPrecio", "TipoIdentidad", "idProductoServicio", "idVariante", "idPresentacionVenta"),
                Ix("IX_ListaPreciosDetalle_Empresa_Lista", false, "idEmpresa", "idListaPrecio"),
                Ix("IX_ListaPreciosDetalle_Empresa_Producto", false, "idEmpresa", "idProductoServicio"),
                Ix("IX_ListaPreciosDetalle_Empresa_Variante", false, false, "idVariante IS NOT NULL", "idEmpresa", "idVariante"),
                Ix("IX_ListaPreciosDetalle_Empresa_PresentacionVenta", false, false, "idPresentacionVenta IS NOT NULL", "idEmpresa", "idPresentacionVenta"),
                Ix("IX_ListaPreciosDetalle_Empresa_Activo", false, "idEmpresa", "Activo"),
            });

        private static SchemaColumnContract[] DetalleColumns(int version)
        {
            List<SchemaColumnContract> columns = new()
            {
                Col("id", "UNIQUEIDENTIFIER", false, def: "(NEWID())"),
                Col("idEmpresa", "UNIQUEIDENTIFIER", false),
                Col("identityKey", "UNIQUEIDENTIFIER", false, def: "(NEWID())"),
                Col("idListaPrecio", "UNIQUEIDENTIFIER", false),
                Col("TipoIdentidad", "TINYINT", false),
                Col("idProductoServicio", "UNIQUEIDENTIFIER", false),
                Col("TipoProductoServicio", "TINYINT", false),
                Col("idVariante", "UNIQUEIDENTIFIER", true),
                Col("idPresentacionVenta", "UNIQUEIDENTIFIER", true),
                Col("Precio", "DECIMAL(18,2)", false, precision: 18, scale: 2),
            };

            if (version >= ProductosServiciosSchemaContractProvider.V2)
            {
                columns.Add(Col("DescuentoPct", "DECIMAL(5,2)", true, precision: 5, scale: 2));
                columns.Add(Col("RedondeoModo", "TINYINT", false, def: "((0))"));
                columns.Add(Col("VigenciaInicio", "DATE", true));
                columns.Add(Col("VigenciaFin", "DATE", true));
            }

            columns.AddRange(new[]
            {
                Col("Activo", "BIT", false, def: "((1))"),
                Col("FechaCreacion", "DATETIME2(0)", false, scale: 0, def: "(SYSUTCDATETIME())"),
                Col("FechaActualizacion", "DATETIME2(0)", false, scale: 0, def: "(SYSUTCDATETIME())"),
                Col("FechaArchivado", "DATETIME2(0)", true, scale: 0),
                Col("idUsuarioCreacion", "UNIQUEIDENTIFIER", true),
                Col("idUsuarioActualizacion", "UNIQUEIDENTIFIER", true),
                Col("idUsuarioArchivado", "UNIQUEIDENTIFIER", true),
            });

            return columns.ToArray();
        }

        private static SchemaCheckContract[] DetalleChecks(int version)
        {
            List<SchemaCheckContract> checks = new()
            {
                new SchemaCheckContract("CK_ListaPreciosDetalle_TipoIdentidad", "CHECK (TipoIdentidad IN (1, 2, 3, 4))"),
                new SchemaCheckContract("CK_ListaPreciosDetalle_TipoProductoServicio", "CHECK (TipoProductoServicio IN (1, 2))"),
                new SchemaCheckContract("CK_ListaPreciosDetalle_Identidad", "CHECK ((TipoIdentidad = 1 AND TipoProductoServicio = 1 AND idVariante IS NULL AND idPresentacionVenta IS NULL) OR (TipoIdentidad = 2 AND TipoProductoServicio = 2 AND idVariante IS NULL AND idPresentacionVenta IS NULL) OR (TipoIdentidad = 3 AND TipoProductoServicio = 1 AND idVariante IS NOT NULL AND idPresentacionVenta IS NULL) OR (TipoIdentidad = 4 AND TipoProductoServicio = 1 AND idVariante IS NULL AND idPresentacionVenta IS NOT NULL))"),
                new SchemaCheckContract("CK_ListaPreciosDetalle_Precio", "CHECK (Precio >= 0)"),
            };

            if (version >= ProductosServiciosSchemaContractProvider.V2)
            {
                checks.Add(new SchemaCheckContract("CK_ListaPreciosDetalle_DescuentoPct", "CHECK (DescuentoPct IS NULL OR (DescuentoPct >= 0 AND DescuentoPct <= 100))"));
                checks.Add(new SchemaCheckContract("CK_ListaPreciosDetalle_RedondeoModo", "CHECK (RedondeoModo IN (0, 1, 2))"));
                checks.Add(new SchemaCheckContract("CK_ListaPreciosDetalle_Vigencia", "CHECK (VigenciaInicio IS NULL OR VigenciaFin IS NULL OR VigenciaInicio <= VigenciaFin)"));
            }

            checks.Add(new SchemaCheckContract("CK_ListaPreciosDetalle_Archivado", "CHECK ((Activo = 1 AND FechaArchivado IS NULL AND idUsuarioArchivado IS NULL) OR (Activo = 0 AND FechaArchivado IS NOT NULL))"));
            return checks.ToArray();
        }

        private static SchemaTableContract Promociones() => new(
            "dbo",
            "ListaPreciosPromociones",
            new[]
            {
                Col("id", "UNIQUEIDENTIFIER", false, def: "(NEWID())"),
                Col("idEmpresa", "UNIQUEIDENTIFIER", false),
                Col("identityKey", "UNIQUEIDENTIFIER", false, def: "(NEWID())"),
                Col("idListaPrecio", "UNIQUEIDENTIFIER", false),
                Col("TipoPromocion", "TINYINT", false),
                Col("TipoIdentidad", "TINYINT", false),
                Col("idProductoServicio", "UNIQUEIDENTIFIER", false),
                Col("TipoProductoServicio", "TINYINT", false),
                Col("idVariante", "UNIQUEIDENTIFIER", true),
                Col("idPresentacionVenta", "UNIQUEIDENTIFIER", true),
                Col("DescuentoSegundoPct", "DECIMAL(5,2)", true, precision: 5, scale: 2),
                Col("VigenciaInicio", "DATE", true),
                Col("VigenciaFin", "DATE", true),
                Col("Activo", "BIT", false, def: "((1))"),
                Col("FechaCreacion", "DATETIME2(0)", false, scale: 0, def: "(SYSUTCDATETIME())"),
                Col("FechaActualizacion", "DATETIME2(0)", false, scale: 0, def: "(SYSUTCDATETIME())"),
                Col("FechaArchivado", "DATETIME2(0)", true, scale: 0),
                Col("idUsuarioCreacion", "UNIQUEIDENTIFIER", true),
                Col("idUsuarioActualizacion", "UNIQUEIDENTIFIER", true),
                Col("idUsuarioArchivado", "UNIQUEIDENTIFIER", true),
            },
            new SchemaPrimaryKeyContract("PK_ListaPreciosPromociones", new[] { "id" }, true),
            new[]
            {
                new SchemaForeignKeyContract("FK_ListaPreciosPromociones_Listas_EmpresaId", new[] { "idEmpresa", "idListaPrecio" }, "dbo", "ListaPreciosListas", new[] { "idEmpresa", "id" }),
                new SchemaForeignKeyContract("FK_ListaPreciosPromociones_ProductosServicios_EmpresaId", new[] { "idEmpresa", "idProductoServicio" }, "dbo", "ProductosServicios", new[] { "idEmpresa", "id" }),
                new SchemaForeignKeyContract("FK_ListaPreciosPromociones_Variantes_EmpresaId", new[] { "idEmpresa", "idVariante" }, "dbo", "ProductosServiciosVariantes", new[] { "idEmpresa", "id" }),
                new SchemaForeignKeyContract("FK_ListaPreciosPromociones_PresentacionesVenta_EmpresaId", new[] { "idEmpresa", "idPresentacionVenta" }, "dbo", "ProductosServiciosPresentacionesVenta", new[] { "idEmpresa", "id" }),
            },
            new[]
            {
                Ux("UX_ListaPreciosPromociones_Empresa_Id", null, "idEmpresa", "id"),
                Ux("UX_ListaPreciosPromociones_Empresa_Lista_Tipo_Identidad_Activo", "Activo = 1 AND FechaArchivado IS NULL", "idEmpresa", "idListaPrecio", "TipoPromocion", "TipoIdentidad", "idProductoServicio", "idVariante", "idPresentacionVenta"),
            },
            new[]
            {
                new SchemaCheckContract("CK_ListaPreciosPromociones_TipoPromocion", "CHECK (TipoPromocion IN (1, 2, 3))"),
                new SchemaCheckContract("CK_ListaPreciosPromociones_TipoIdentidad", "CHECK (TipoIdentidad IN (1, 2, 3, 4))"),
                new SchemaCheckContract("CK_ListaPreciosPromociones_TipoProductoServicio", "CHECK (TipoProductoServicio IN (1, 2))"),
                new SchemaCheckContract("CK_ListaPreciosPromociones_Identidad", "CHECK ((TipoIdentidad = 1 AND TipoProductoServicio = 1 AND idVariante IS NULL AND idPresentacionVenta IS NULL) OR (TipoIdentidad = 2 AND TipoProductoServicio = 2 AND idVariante IS NULL AND idPresentacionVenta IS NULL) OR (TipoIdentidad = 3 AND TipoProductoServicio = 1 AND idVariante IS NOT NULL AND idPresentacionVenta IS NULL) OR (TipoIdentidad = 4 AND TipoProductoServicio = 1 AND idVariante IS NULL AND idPresentacionVenta IS NOT NULL))"),
                new SchemaCheckContract("CK_ListaPreciosPromociones_DescuentoSegundo", "CHECK ((TipoPromocion = 3 AND DescuentoSegundoPct IS NOT NULL AND DescuentoSegundoPct >= 0 AND DescuentoSegundoPct <= 100) OR (TipoPromocion IN (1, 2) AND DescuentoSegundoPct IS NULL))"),
                new SchemaCheckContract("CK_ListaPreciosPromociones_Vigencia", "CHECK (VigenciaInicio IS NULL OR VigenciaFin IS NULL OR VigenciaInicio <= VigenciaFin)"),
                new SchemaCheckContract("CK_ListaPreciosPromociones_Archivado", "CHECK ((Activo = 1 AND FechaArchivado IS NULL AND idUsuarioArchivado IS NULL) OR (Activo = 0 AND FechaArchivado IS NOT NULL))"),
            },
            new[]
            {
                Ix("UX_ListaPreciosPromociones_Empresa_Id", true, "idEmpresa", "id"),
                Ix("UX_ListaPreciosPromociones_Empresa_Lista_Tipo_Identidad_Activo", true, false, "Activo = 1 AND FechaArchivado IS NULL", "idEmpresa", "idListaPrecio", "TipoPromocion", "TipoIdentidad", "idProductoServicio", "idVariante", "idPresentacionVenta"),
                Ix("IX_ListaPreciosPromociones_Empresa_Lista", false, "idEmpresa", "idListaPrecio"),
                Ix("IX_ListaPreciosPromociones_Empresa_Producto", false, "idEmpresa", "idProductoServicio"),
                Ix("IX_ListaPreciosPromociones_Empresa_Activo", false, "idEmpresa", "Activo"),
            });

        private static SchemaTableContract Historial() => new(
            "dbo",
            "ListaPreciosHistorial",
            new[]
            {
                Col("id", "UNIQUEIDENTIFIER", false, def: "(NEWID())"),
                Col("idEmpresa", "UNIQUEIDENTIFIER", false),
                Col("idListaPrecio", "UNIQUEIDENTIFIER", true),
                Col("TipoIdentidad", "TINYINT", true),
                Col("idProductoServicio", "UNIQUEIDENTIFIER", true),
                Col("idVariante", "UNIQUEIDENTIFIER", true),
                Col("idPresentacionVenta", "UNIQUEIDENTIFIER", true),
                Col("Campo", "NVARCHAR(60)", false, max: 60),
                Col("Operacion", "NVARCHAR(40)", false, max: 40),
                Col("ValorAnterior", "NVARCHAR(4000)", true, max: 4000),
                Col("ValorNuevo", "NVARCHAR(4000)", true, max: 4000),
                Col("idUsuario", "UNIQUEIDENTIFIER", true),
                Col("Usuario", "NVARCHAR(256)", true, max: 256),
                Col("Origen", "NVARCHAR(40)", false, max: 40),
                Col("CorrelationId", "UNIQUEIDENTIFIER", false, def: "(NEWID())"),
                Col("Motivo", "NVARCHAR(500)", true, max: 500),
                Col("FechaUtc", "DATETIME2(0)", false, scale: 0, def: "(SYSUTCDATETIME())"),
            },
            new SchemaPrimaryKeyContract("PK_ListaPreciosHistorial", new[] { "id" }, true),
            new[]
            {
                new SchemaForeignKeyContract("FK_ListaPreciosHistorial_Listas_EmpresaId", new[] { "idEmpresa", "idListaPrecio" }, "dbo", "ListaPreciosListas", new[] { "idEmpresa", "id" }),
            },
            new[]
            {
                Ux("UX_ListaPreciosHistorial_Empresa_Id", null, "idEmpresa", "id"),
            },
            new[]
            {
                new SchemaCheckContract("CK_ListaPreciosHistorial_TipoIdentidad", "CHECK (TipoIdentidad IS NULL OR TipoIdentidad IN (1, 2, 3, 4))"),
                new SchemaCheckContract("CK_ListaPreciosHistorial_Origen", "CHECK (Origen IN (N'INDIVIDUAL', N'MASIVO', N'COPIA_LISTA', N'DESCUENTO_MARCA', N'PROMOCION', N'SISTEMA'))"),
            },
            new[]
            {
                Ix("UX_ListaPreciosHistorial_Empresa_Id", true, "idEmpresa", "id"),
                Ix("IX_ListaPreciosHistorial_Empresa_Fecha", false, "idEmpresa", "FechaUtc"),
                Ix("IX_ListaPreciosHistorial_Empresa_Correlation", false, "idEmpresa", "CorrelationId"),
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
