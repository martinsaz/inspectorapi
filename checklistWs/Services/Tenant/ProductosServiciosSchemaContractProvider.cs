namespace checklistWs.Services.Tenant
{
    public sealed class ProductosServiciosSchemaContractProvider : ISchemaContractProvider
    {
        public const int V1 = 1;
        public const int V2 = 2;
        public const int LatestVersion = V2;
        public const int SucursalesLatestVersion = V2;
        public const int ProveedoresLatestVersion = V1;
        public const int OrdenesCompraLatestVersion = V2;
        public const int InventarioLatestVersion = V1;
        public const int RecepcionLatestVersion = V1;
        public const int CurvasLatestVersion = V1;

        public SchemaContract GetContract(string scope, int? version = null)
        {
            if (string.Equals(scope, DatabaseScopes.Recepcion, StringComparison.OrdinalIgnoreCase))
            {
                return RecepcionSchemaContractFactory.GetContract(version ?? RecepcionLatestVersion);
            }

            if (string.Equals(scope, DatabaseScopes.Curvas, StringComparison.OrdinalIgnoreCase))
            {
                return CurvasSchemaContractFactory.GetContract(version ?? CurvasLatestVersion);
            }

            if (string.Equals(scope, DatabaseScopes.Inventario, StringComparison.OrdinalIgnoreCase))
            {
                return InventarioSchemaContractFactory.GetContract(version ?? InventarioLatestVersion);
            }

            if (string.Equals(scope, DatabaseScopes.OrdenesCompra, StringComparison.OrdinalIgnoreCase))
            {
                return OrdenesCompraSchemaContractFactory.GetContract(version ?? OrdenesCompraLatestVersion);
            }

            if (string.Equals(scope, DatabaseScopes.Sucursales, StringComparison.OrdinalIgnoreCase))
            {
                return GetSucursalesContract(version);
            }

            if (string.Equals(scope, DatabaseScopes.Proveedores, StringComparison.OrdinalIgnoreCase))
            {
                return GetProveedoresContract(version);
            }

            if (!string.Equals(scope, DatabaseScopes.ProductosServicios, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("SCHEMA_CONTRACT_SCOPE_NOT_SUPPORTED");
            }

            int contractVersion = version ?? LatestVersion;
            if (contractVersion is not V1 and not V2)
            {
                throw new InvalidOperationException("SCHEMA_CONTRACT_VERSION_NOT_SUPPORTED");
            }

            return new SchemaContract(
                DatabaseScopes.ProductosServicios,
                contractVersion,
                "Productos y Servicios",
                new[]
                {
                    new SchemaTableContract(
                        "dbo",
                        @"ProductosServiciosCategorias",
                        new[]
                        {
                            new SchemaColumnContract(@"id", @"UNIQUEIDENTIFIER", null, null, null, false, @"(NEWID())", false, false, null),
                            new SchemaColumnContract(@"idEmpresa", @"UNIQUEIDENTIFIER", null, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"identityKey", @"UNIQUEIDENTIFIER", null, null, null, false, @"(NEWID())", false, false, null),
                            new SchemaColumnContract(@"Codigo", @"NVARCHAR(50)", 50, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"Nombre", @"NVARCHAR(150)", 150, null, null, false, null, false, false, null),
                            DescripcionCatalogo(contractVersion),
                            new SchemaColumnContract(@"AplicaA", @"TINYINT", null, null, null, false, @"((0))", false, false, null),
                            new SchemaColumnContract(@"Activo", @"BIT", null, null, null, false, @"((1))", false, false, null),
                            new SchemaColumnContract(@"FechaCreacion", @"DATETIME2(0)", null, null, 0, false, @"(SYSUTCDATETIME())", false, false, null),
                            new SchemaColumnContract(@"FechaActualizacion", @"DATETIME2(0)", null, null, 0, false, @"(SYSUTCDATETIME())", false, false, null),
                            new SchemaColumnContract(@"FechaArchivado", @"DATETIME2(0)", null, null, 0, true, null, false, false, null),
                        },
                        new SchemaPrimaryKeyContract(@"PK_ProductosServiciosCategorias", new[] { "id" }, true),
                        Array.Empty<SchemaForeignKeyContract>(),
                        new[]
                        {
                            new SchemaUniqueContract(@"UX_ProductosServiciosCategorias_Empresa_Codigo", new[] { @"idEmpresa", @"Codigo" }, null),
                            new SchemaUniqueContract(@"UX_ProductosServiciosCategorias_Empresa_Id", new[] { @"idEmpresa", @"id" }, null),
                        },
                        new[]
                        {
                            new SchemaCheckContract(@"CK_ProductosServiciosCategorias_AplicaA", @"CHECK (AplicaA IN (0, 1, 2))"),
                        },
                        new[]
                        {
                            new SchemaIndexContract(@"UX_ProductosServiciosCategorias_Empresa_Codigo", true, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"Codigo", false) }, Array.Empty<string>(), null),
                            new SchemaIndexContract(@"UX_ProductosServiciosCategorias_Empresa_Id", true, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"id", false) }, Array.Empty<string>(), null),
                            new SchemaIndexContract(@"IX_ProductosServiciosCategorias_Empresa_Nombre", false, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"Nombre", false) }, Array.Empty<string>(), null),
                            new SchemaIndexContract(@"IX_ProductosServiciosCategorias_Empresa_Activo", false, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"Activo", false) }, Array.Empty<string>(), null),
                        }),
                    new SchemaTableContract(
                        "dbo",
                        @"ProductosServiciosMarcas",
                        new[]
                        {
                            new SchemaColumnContract(@"id", @"UNIQUEIDENTIFIER", null, null, null, false, @"(NEWID())", false, false, null),
                            new SchemaColumnContract(@"idEmpresa", @"UNIQUEIDENTIFIER", null, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"identityKey", @"UNIQUEIDENTIFIER", null, null, null, false, @"(NEWID())", false, false, null),
                            new SchemaColumnContract(@"Codigo", @"NVARCHAR(50)", 50, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"Nombre", @"NVARCHAR(150)", 150, null, null, false, null, false, false, null),
                            DescripcionCatalogo(contractVersion),
                            new SchemaColumnContract(@"Activo", @"BIT", null, null, null, false, @"((1))", false, false, null),
                            new SchemaColumnContract(@"FechaCreacion", @"DATETIME2(0)", null, null, 0, false, @"(SYSUTCDATETIME())", false, false, null),
                            new SchemaColumnContract(@"FechaActualizacion", @"DATETIME2(0)", null, null, 0, false, @"(SYSUTCDATETIME())", false, false, null),
                            new SchemaColumnContract(@"FechaArchivado", @"DATETIME2(0)", null, null, 0, true, null, false, false, null),
                        },
                        new SchemaPrimaryKeyContract(@"PK_ProductosServiciosMarcas", new[] { "id" }, true),
                        Array.Empty<SchemaForeignKeyContract>(),
                        new[]
                        {
                            new SchemaUniqueContract(@"UX_ProductosServiciosMarcas_Empresa_Codigo", new[] { @"idEmpresa", @"Codigo" }, null),
                            new SchemaUniqueContract(@"UX_ProductosServiciosMarcas_Empresa_Id", new[] { @"idEmpresa", @"id" }, null),
                        },
                        Array.Empty<SchemaCheckContract>(),
                        new[]
                        {
                            new SchemaIndexContract(@"UX_ProductosServiciosMarcas_Empresa_Codigo", true, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"Codigo", false) }, Array.Empty<string>(), null),
                            new SchemaIndexContract(@"UX_ProductosServiciosMarcas_Empresa_Id", true, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"id", false) }, Array.Empty<string>(), null),
                            new SchemaIndexContract(@"IX_ProductosServiciosMarcas_Empresa_Nombre", false, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"Nombre", false) }, Array.Empty<string>(), null),
                            new SchemaIndexContract(@"IX_ProductosServiciosMarcas_Empresa_Activo", false, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"Activo", false) }, Array.Empty<string>(), null),
                        }),
                    new SchemaTableContract(
                        "dbo",
                        @"ProductosServiciosUnidadesMedida",
                        new[]
                        {
                            new SchemaColumnContract(@"id", @"UNIQUEIDENTIFIER", null, null, null, false, @"(NEWID())", false, false, null),
                            new SchemaColumnContract(@"idEmpresa", @"UNIQUEIDENTIFIER", null, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"identityKey", @"UNIQUEIDENTIFIER", null, null, null, false, @"(NEWID())", false, false, null),
                            new SchemaColumnContract(@"Codigo", @"NVARCHAR(30)", 30, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"Nombre", @"NVARCHAR(100)", 100, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"Abreviatura", @"NVARCHAR(20)", 20, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"PermiteDecimales", @"BIT", null, null, null, false, @"((0))", false, false, null),
                            new SchemaColumnContract(@"Activo", @"BIT", null, null, null, false, @"((1))", false, false, null),
                            new SchemaColumnContract(@"FechaCreacion", @"DATETIME2(0)", null, null, 0, false, @"(SYSUTCDATETIME())", false, false, null),
                            new SchemaColumnContract(@"FechaActualizacion", @"DATETIME2(0)", null, null, 0, false, @"(SYSUTCDATETIME())", false, false, null),
                            new SchemaColumnContract(@"FechaArchivado", @"DATETIME2(0)", null, null, 0, true, null, false, false, null),
                            new SchemaColumnContract(@"TipoUnidad", @"NVARCHAR(20)", 20, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"EsSistema", @"BIT", null, null, null, false, @"((0))", false, false, null),
                            new SchemaColumnContract(@"EsPersonalizada", @"BIT", null, null, null, false, @"((1))", false, false, null),
                            new SchemaColumnContract(@"FactorConversion", @"DECIMAL(28,12)", null, 28, 12, true, null, false, false, null),
                            new SchemaColumnContract(@"Convertible", @"BIT", null, null, null, false, @"((0))", false, false, null),
                            new SchemaColumnContract(@"ClaveSistema", @"NVARCHAR(30)", 30, null, null, true, null, false, false, null),
                        },
                        new SchemaPrimaryKeyContract(@"PK_ProductosServiciosUnidadesMedida", new[] { "id" }, true),
                        Array.Empty<SchemaForeignKeyContract>(),
                        new[]
                        {
                            new SchemaUniqueContract(@"UX_ProductosServiciosUnidadesMedida_Empresa_Codigo", new[] { @"idEmpresa", @"Codigo" }, null),
                            new SchemaUniqueContract(@"UX_ProductosServiciosUnidadesMedida_Empresa_Id", new[] { @"idEmpresa", @"id" }, null),
                            new SchemaUniqueContract(@"UX_PSUnidades_Empresa_ClaveSistema", new[] { @"idEmpresa", @"ClaveSistema" }, @"ClaveSistema IS NOT NULL"),
                        },
                        new[]
                        {
                            new SchemaCheckContract(@"CK_PSUnidades_TipoUnidad", @"CHECK (TipoUnidad IN (N'WEIGHT',N'VOLUME',N'LENGTH',N'AREA',N'ITEM',N'OTHER',N'TIME'))"),
                            new SchemaCheckContract(@"CK_PSUnidades_Conversion", @"CHECK ((Convertible = 0 AND FactorConversion IS NULL) OR (Convertible = 1 AND FactorConversion > 0))"),
                        },
                        new[]
                        {
                            new SchemaIndexContract(@"UX_ProductosServiciosUnidadesMedida_Empresa_Codigo", true, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"Codigo", false) }, Array.Empty<string>(), null),
                            new SchemaIndexContract(@"UX_ProductosServiciosUnidadesMedida_Empresa_Id", true, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"id", false) }, Array.Empty<string>(), null),
                            new SchemaIndexContract(@"IX_ProductosServiciosUnidadesMedida_Empresa_Nombre", false, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"Nombre", false) }, Array.Empty<string>(), null),
                            new SchemaIndexContract(@"IX_ProductosServiciosUnidadesMedida_Empresa_Activo", false, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"Activo", false) }, Array.Empty<string>(), null),
                            new SchemaIndexContract(@"UX_PSUnidades_Empresa_ClaveSistema", true, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"ClaveSistema", false) }, Array.Empty<string>(), @"ClaveSistema IS NOT NULL"),
                        }),
                    new SchemaTableContract(
                        "dbo",
                        @"ProductosServicios",
                        new[]
                        {
                            new SchemaColumnContract(@"id", @"UNIQUEIDENTIFIER", null, null, null, false, @"(NEWID())", false, false, null),
                            new SchemaColumnContract(@"idEmpresa", @"UNIQUEIDENTIFIER", null, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"identityKey", @"UNIQUEIDENTIFIER", null, null, null, false, @"(NEWID())", false, false, null),
                            new SchemaColumnContract(@"Tipo", @"TINYINT", null, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"Codigo", @"NVARCHAR(50)", 50, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"Tag", @"NVARCHAR(100)", 100, null, null, true, null, false, false, null),
                            new SchemaColumnContract(@"Nombre", @"NVARCHAR(150)", 150, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"Descripcion", @"NVARCHAR(MAX)", -1, null, null, true, null, false, false, null),
                            new SchemaColumnContract(@"idCategoria", @"UNIQUEIDENTIFIER", null, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"idMarca", @"UNIQUEIDENTIFIER", null, null, null, true, null, false, false, null),
                            new SchemaColumnContract(@"idUnidadMedida", @"UNIQUEIDENTIFIER", null, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"Costo", @"DECIMAL(18,2)", null, 18, 2, true, null, false, false, null),
                            new SchemaColumnContract(@"PrecioPublico", @"DECIMAL(18,2)", null, 18, 2, false, null, false, false, null),
                            new SchemaColumnContract(@"CausaInventario", @"BIT", null, null, null, false, @"((0))", false, false, null),
                            new SchemaColumnContract(@"PermiteVentaSinExistencia", @"BIT", null, null, null, false, @"((0))", false, false, null),
                            new SchemaColumnContract(@"ImagenUrl", @"NVARCHAR(1000)", 1000, null, null, true, null, false, false, null),
                            new SchemaColumnContract(@"ImagenNombre", @"NVARCHAR(255)", 255, null, null, true, null, false, false, null),
                            new SchemaColumnContract(@"Activo", @"BIT", null, null, null, false, @"((1))", false, false, null),
                            new SchemaColumnContract(@"FechaCreacion", @"DATETIME2(0)", null, null, 0, false, @"(SYSUTCDATETIME())", false, false, null),
                            new SchemaColumnContract(@"FechaActualizacion", @"DATETIME2(0)", null, null, 0, true, null, false, false, null),
                            new SchemaColumnContract(@"FechaArchivado", @"DATETIME2(0)", null, null, 0, true, null, false, false, null),
                            new SchemaColumnContract(@"PrecioComparacion", @"DECIMAL(18,2)", null, 18, 2, true, null, false, false, null),
                            new SchemaColumnContract(@"PrecioUnitarioMonto", @"DECIMAL(18,6)", null, 18, 6, true, null, false, false, null),
                            new SchemaColumnContract(@"PrecioUnitarioCantidadTotal", @"DECIMAL(18,6)", null, 18, 6, true, null, false, false, null),
                            new SchemaColumnContract(@"PrecioUnitarioUnidadTotal", @"NVARCHAR(20)", 20, null, null, true, null, false, false, null),
                            new SchemaColumnContract(@"PrecioUnitarioBaseCantidad", @"DECIMAL(18,6)", null, 18, 6, true, null, false, false, null),
                            new SchemaColumnContract(@"PrecioUnitarioUnidad", @"NVARCHAR(20)", 20, null, null, true, null, false, false, null),
                            new SchemaColumnContract(@"PrecioUnitarioUnidadBase", @"NVARCHAR(20)", 20, null, null, true, null, false, false, null),
                            new SchemaColumnContract(@"ObjetoImpuesto", @"NVARCHAR(4)", 4, null, null, true, null, false, false, null),
                            new SchemaColumnContract(@"PorcentajeIVA", @"DECIMAL(5,2)", null, 5, 2, false, @"(0)", false, false, null),
                            new SchemaColumnContract(@"ClaveProductoSat", @"NVARCHAR(20)", 20, null, null, true, null, false, false, null),
                            new SchemaColumnContract(@"ClaveUnidadSat", @"NVARCHAR(10)", 10, null, null, true, null, false, false, null),
                            new SchemaColumnContract(@"EsProductoFisico", @"BIT", null, null, null, false, @"((0))", false, false, null),
                            new SchemaColumnContract(@"PesoKg", @"DECIMAL(18,5)", null, 18, 5, true, null, false, false, null),
                            new SchemaColumnContract(@"LargoCm", @"DECIMAL(18,2)", null, 18, 2, true, null, false, false, null),
                            new SchemaColumnContract(@"AnchoCm", @"DECIMAL(18,2)", null, 18, 2, true, null, false, false, null),
                            new SchemaColumnContract(@"AltoCm", @"DECIMAL(18,2)", null, 18, 2, true, null, false, false, null),
                            new SchemaColumnContract(@"UsaNumeroSerie", @"BIT", null, null, null, false, @"((0))", false, false, null),
                            new SchemaColumnContract(@"idColeccion", @"UNIQUEIDENTIFIER", null, null, null, true, null, false, false, null),
                            new SchemaColumnContract(@"idPaquete", @"UNIQUEIDENTIFIER", null, null, null, true, null, false, false, null),
                        },
                        new SchemaPrimaryKeyContract(@"PK_ProductosServicios", new[] { "id" }, true),
                        new[]
                        {
                            new SchemaForeignKeyContract(@"FK_ProductosServicios_Categorias_EmpresaId", new[] { @"idEmpresa", @"idCategoria" }, @"dbo", @"ProductosServiciosCategorias", new[] { @"idEmpresa", @"id" }),
                            new SchemaForeignKeyContract(@"FK_ProductosServicios_Marcas_EmpresaId", new[] { @"idEmpresa", @"idMarca" }, @"dbo", @"ProductosServiciosMarcas", new[] { @"idEmpresa", @"id" }),
                            new SchemaForeignKeyContract(@"FK_ProductosServicios_Unidades_EmpresaId", new[] { @"idEmpresa", @"idUnidadMedida" }, @"dbo", @"ProductosServiciosUnidadesMedida", new[] { @"idEmpresa", @"id" }),
                            new SchemaForeignKeyContract(@"FK_ProductosServicios_Colecciones_EmpresaId", new[] { @"idEmpresa", @"idColeccion" }, @"dbo", @"ProductosServiciosColecciones", new[] { @"idEmpresa", @"id" }),
                            new SchemaForeignKeyContract(@"FK_ProductosServicios_Paquetes_EmpresaId", new[] { @"idEmpresa", @"idPaquete" }, @"dbo", @"ProductosServiciosPaquetes", new[] { @"idEmpresa", @"id" }),
                        },
                        new[]
                        {
                            new SchemaUniqueContract(@"UX_ProductosServicios_Empresa_Codigo", new[] { @"idEmpresa", @"Codigo" }, null),
                            new SchemaUniqueContract(@"UX_ProductosServicios_Empresa_Id", new[] { @"idEmpresa", @"id" }, null),
                        },
                        new[]
                        {
                            new SchemaCheckContract(@"CK_ProductosServicios_Tipo", @"CHECK (Tipo IN (1, 2))"),
                            new SchemaCheckContract(@"CK_ProductosServicios_ValoresMonetarios", @"CHECK (PrecioPublico >= 0 AND (Costo IS NULL OR Costo >= 0))"),
                            new SchemaCheckContract(@"CK_ProductosServicios_ServicioSinInventario", @"CHECK (Tipo = 1 OR ( idMarca IS NULL AND CausaInventario = 0 AND PermiteVentaSinExistencia = 0 ))"),
                        },
                        new[]
                        {
                            new SchemaIndexContract(@"UX_ProductosServicios_Empresa_Codigo", true, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"Codigo", false) }, Array.Empty<string>(), null),
                            new SchemaIndexContract(@"UX_ProductosServicios_Empresa_Id", true, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"id", false) }, Array.Empty<string>(), null),
                            new SchemaIndexContract(@"IX_ProductosServicios_Empresa_Tipo_Activo", false, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"Tipo", false), new SchemaIndexColumnContract(@"Activo", false) }, Array.Empty<string>(), null),
                            new SchemaIndexContract(@"IX_ProductosServicios_Empresa_Categoria_Activo", false, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"idCategoria", false), new SchemaIndexColumnContract(@"Activo", false) }, Array.Empty<string>(), null),
                            new SchemaIndexContract(@"IX_ProductosServicios_Empresa_Marca_Activo", false, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"idMarca", false), new SchemaIndexColumnContract(@"Activo", false) }, Array.Empty<string>(), null),
                            new SchemaIndexContract(@"IX_ProductosServicios_Empresa_Unidad_Activo", false, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"idUnidadMedida", false), new SchemaIndexColumnContract(@"Activo", false) }, Array.Empty<string>(), null),
                            new SchemaIndexContract(@"IX_ProductosServicios_Empresa_Tag", false, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"Tag", false) }, Array.Empty<string>(), null),
                        }),
                    new SchemaTableContract(
                        "dbo",
                        @"ProductosServiciosExistencias",
                        new[]
                        {
                            new SchemaColumnContract(@"id", @"UNIQUEIDENTIFIER", null, null, null, false, @"(NEWID())", false, false, null),
                            new SchemaColumnContract(@"idEmpresa", @"UNIQUEIDENTIFIER", null, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"identityKey", @"UNIQUEIDENTIFIER", null, null, null, false, @"(NEWID())", false, false, null),
                            new SchemaColumnContract(@"idProductoServicio", @"UNIQUEIDENTIFIER", null, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"ExistenciaActual", @"DECIMAL(18,4)", null, 18, 4, false, @"((0))", false, false, null),
                            new SchemaColumnContract(@"ExistenciaMinima", @"DECIMAL(18,4)", null, 18, 4, false, @"((0))", false, false, null),
                            new SchemaColumnContract(@"CostoPromedio", @"DECIMAL(18,2)", null, 18, 2, true, null, false, false, null),
                            new SchemaColumnContract(@"FechaCreacion", @"DATETIME2(0)", null, null, 0, false, @"(SYSUTCDATETIME())", false, false, null),
                            new SchemaColumnContract(@"FechaActualizacion", @"DATETIME2(0)", null, null, 0, false, @"(SYSUTCDATETIME())", false, false, null),
                        },
                        new SchemaPrimaryKeyContract(@"PK_ProductosServiciosExistencias", new[] { "id" }, true),
                        new[]
                        {
                            new SchemaForeignKeyContract(@"FK_ProductosServiciosExistencias_ProductosServicios_EmpresaId", new[] { @"idEmpresa", @"idProductoServicio" }, @"dbo", @"ProductosServicios", new[] { @"idEmpresa", @"id" }),
                        },
                        new[]
                        {
                            new SchemaUniqueContract(@"UX_ProductosServiciosExistencias_Empresa_ProductoServicio", new[] { @"idEmpresa", @"idProductoServicio" }, null),
                        },
                        new[]
                        {
                            new SchemaCheckContract(@"CK_ProductosServiciosExistencias_Valores", @"CHECK (ExistenciaMinima >= 0 AND (CostoPromedio IS NULL OR CostoPromedio >= 0))"),
                        },
                        new[]
                        {
                            new SchemaIndexContract(@"UX_ProductosServiciosExistencias_Empresa_ProductoServicio", true, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"idProductoServicio", false) }, Array.Empty<string>(), null),
                        }),
                    new SchemaTableContract(
                        "dbo",
                        @"ProductosServiciosMovimientosInventario",
                        new[]
                        {
                            new SchemaColumnContract(@"id", @"UNIQUEIDENTIFIER", null, null, null, false, @"(NEWID())", false, false, null),
                            new SchemaColumnContract(@"idEmpresa", @"UNIQUEIDENTIFIER", null, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"identityKey", @"UNIQUEIDENTIFIER", null, null, null, false, @"(NEWID())", false, false, null),
                            new SchemaColumnContract(@"idProductoServicio", @"UNIQUEIDENTIFIER", null, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"TipoMovimiento", @"TINYINT", null, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"Cantidad", @"DECIMAL(18,4)", null, 18, 4, false, null, false, false, null),
                            new SchemaColumnContract(@"ExistenciaAnterior", @"DECIMAL(18,4)", null, 18, 4, false, null, false, false, null),
                            new SchemaColumnContract(@"ExistenciaPosterior", @"DECIMAL(18,4)", null, 18, 4, false, null, false, false, null),
                            new SchemaColumnContract(@"CostoUnitario", @"DECIMAL(18,2)", null, 18, 2, true, null, false, false, null),
                            new SchemaColumnContract(@"Referencia", @"NVARCHAR(150)", 150, null, null, true, null, false, false, null),
                            new SchemaColumnContract(@"Observaciones", @"NVARCHAR(1000)", 1000, null, null, true, null, false, false, null),
                            new SchemaColumnContract(@"idUsuario", @"UNIQUEIDENTIFIER", null, null, null, true, null, false, false, null),
                            new SchemaColumnContract(@"FechaMovimiento", @"DATETIME2(0)", null, null, 0, false, @"(SYSUTCDATETIME())", false, false, null),
                        },
                        new SchemaPrimaryKeyContract(@"PK_ProductosServiciosMovimientosInventario", new[] { "id" }, true),
                        new[]
                        {
                            new SchemaForeignKeyContract(@"FK_ProductosServiciosMovimientos_ProductosServicios_EmpresaId", new[] { @"idEmpresa", @"idProductoServicio" }, @"dbo", @"ProductosServicios", new[] { @"idEmpresa", @"id" }),
                        },
                        Array.Empty<SchemaUniqueContract>(),
                        new[]
                        {
                            new SchemaCheckContract(@"CK_ProductosServiciosMovimientos_Tipo", @"CHECK (TipoMovimiento IN (1, 2, 3, 4, 5))"),
                            new SchemaCheckContract(@"CK_ProductosServiciosMovimientos_Cantidad", @"CHECK (Cantidad > 0)"),
                            new SchemaCheckContract(@"CK_ProductosServiciosMovimientos_ValoresMonetarios", @"CHECK (CostoUnitario IS NULL OR CostoUnitario >= 0)"),
                        },
                        new[]
                        {
                            new SchemaIndexContract(@"IX_ProductosServiciosMovimientos_Empresa_ProductoServicio_FechaMovimiento", false, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"idProductoServicio", false), new SchemaIndexColumnContract(@"FechaMovimiento", false) }, Array.Empty<string>(), null),
                            new SchemaIndexContract(@"IX_ProductosServiciosMovimientos_Empresa_FechaMovimiento", false, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"FechaMovimiento", false) }, Array.Empty<string>(), null),
                        }),
                    new SchemaTableContract(
                        "dbo",
                        @"ProductosServiciosColecciones",
                        new[]
                        {
                            new SchemaColumnContract(@"id", @"UNIQUEIDENTIFIER", null, null, null, false, @"(NEWID())", false, false, null),
                            new SchemaColumnContract(@"idEmpresa", @"UNIQUEIDENTIFIER", null, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"identityKey", @"UNIQUEIDENTIFIER", null, null, null, false, @"(NEWID())", false, false, null),
                            new SchemaColumnContract(@"Numero", @"NVARCHAR(50)", 50, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"Nombre", @"NVARCHAR(150)", 150, null, null, false, null, false, false, null),
                            DescripcionCatalogo(contractVersion),
                            new SchemaColumnContract(@"Activo", @"BIT", null, null, null, false, @"((1))", false, false, null),
                            new SchemaColumnContract(@"FechaCreacion", @"DATETIME2(0)", null, null, 0, false, @"(SYSUTCDATETIME())", false, false, null),
                            new SchemaColumnContract(@"FechaActualizacion", @"DATETIME2(0)", null, null, 0, false, @"(SYSUTCDATETIME())", false, false, null),
                            new SchemaColumnContract(@"FechaArchivado", @"DATETIME2(0)", null, null, 0, true, null, false, false, null),
                        },
                        new SchemaPrimaryKeyContract(@"PK_ProductosServiciosColecciones", new[] { "id" }, true),
                        Array.Empty<SchemaForeignKeyContract>(),
                        new[]
                        {
                            new SchemaUniqueContract(@"UX_ProductosServiciosColecciones_Empresa_Id", new[] { @"idEmpresa", @"id" }, null),
                            new SchemaUniqueContract(@"UX_ProductosServiciosColecciones_Empresa_Numero", new[] { @"idEmpresa", @"Numero" }, null),
                        },
                        Array.Empty<SchemaCheckContract>(),
                        new[]
                        {
                            new SchemaIndexContract(@"UX_ProductosServiciosColecciones_Empresa_Id", true, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"id", false) }, Array.Empty<string>(), null),
                            new SchemaIndexContract(@"UX_ProductosServiciosColecciones_Empresa_Numero", true, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"Numero", false) }, Array.Empty<string>(), null),
                            new SchemaIndexContract(@"IX_ProductosServiciosColecciones_Empresa_Nombre", false, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"Nombre", false) }, Array.Empty<string>(), null),
                        }),
                    new SchemaTableContract(
                        "dbo",
                        @"ProductosServiciosPaquetes",
                        new[]
                        {
                            new SchemaColumnContract(@"id", @"UNIQUEIDENTIFIER", null, null, null, false, @"(NEWID())", false, false, null),
                            new SchemaColumnContract(@"idEmpresa", @"UNIQUEIDENTIFIER", null, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"identityKey", @"UNIQUEIDENTIFIER", null, null, null, false, @"(NEWID())", false, false, null),
                            new SchemaColumnContract(@"Nombre", @"NVARCHAR(150)", 150, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"TipoPaquete", @"NVARCHAR(30)", 30, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"LargoCm", @"DECIMAL(18,2)", null, 18, 2, true, null, false, false, null),
                            new SchemaColumnContract(@"AnchoCm", @"DECIMAL(18,2)", null, 18, 2, true, null, false, false, null),
                            new SchemaColumnContract(@"AltoCm", @"DECIMAL(18,2)", null, 18, 2, true, null, false, false, null),
                            new SchemaColumnContract(@"PesoEmpaqueVacioKg", @"DECIMAL(18,5)", null, 18, 5, true, null, false, false, null),
                            new SchemaColumnContract(@"EsPredeterminado", @"BIT", null, null, null, false, @"((0))", false, false, null),
                            new SchemaColumnContract(@"Activo", @"BIT", null, null, null, false, @"((1))", false, false, null),
                            new SchemaColumnContract(@"FechaCreacion", @"DATETIME2(0)", null, null, 0, false, @"(SYSUTCDATETIME())", false, false, null),
                            new SchemaColumnContract(@"FechaActualizacion", @"DATETIME2(0)", null, null, 0, false, @"(SYSUTCDATETIME())", false, false, null),
                            new SchemaColumnContract(@"FechaArchivado", @"DATETIME2(0)", null, null, 0, true, null, false, false, null),
                        },
                        new SchemaPrimaryKeyContract(@"PK_ProductosServiciosPaquetes", new[] { "id" }, true),
                        Array.Empty<SchemaForeignKeyContract>(),
                        new[]
                        {
                            new SchemaUniqueContract(@"UX_ProductosServiciosPaquetes_Empresa_Id", new[] { @"idEmpresa", @"id" }, null),
                        },
                        new[]
                        {
                            new SchemaCheckContract(@"CK_ProductosServiciosPaquetes_Tipo", @"CHECK (TipoPaquete IN (N'caja', N'sobre', N'flexible'))"),
                        },
                        new[]
                        {
                            new SchemaIndexContract(@"UX_ProductosServiciosPaquetes_Empresa_Id", true, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"id", false) }, Array.Empty<string>(), null),
                            new SchemaIndexContract(@"IX_ProductosServiciosPaquetes_Empresa_Nombre", false, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"Nombre", false) }, Array.Empty<string>(), null),
                        }),
                    new SchemaTableContract(
                        "dbo",
                        @"ProductosServiciosAtributos",
                        new[]
                        {
                            new SchemaColumnContract(@"id", @"UNIQUEIDENTIFIER", null, null, null, false, @"(NEWID())", false, false, null),
                            new SchemaColumnContract(@"idEmpresa", @"UNIQUEIDENTIFIER", null, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"identityKey", @"UNIQUEIDENTIFIER", null, null, null, false, @"(NEWID())", false, false, null),
                            new SchemaColumnContract(@"Nombre", @"NVARCHAR(100)", 100, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"Activo", @"BIT", null, null, null, false, @"((1))", false, false, null),
                            new SchemaColumnContract(@"FechaCreacion", @"DATETIME2(0)", null, null, 0, false, @"(SYSUTCDATETIME())", false, false, null),
                            new SchemaColumnContract(@"FechaActualizacion", @"DATETIME2(0)", null, null, 0, false, @"(SYSUTCDATETIME())", false, false, null),
                            new SchemaColumnContract(@"FechaArchivado", @"DATETIME2(0)", null, null, 0, true, null, false, false, null),
                        },
                        new SchemaPrimaryKeyContract(@"PK_ProductosServiciosAtributos", new[] { "id" }, true),
                        Array.Empty<SchemaForeignKeyContract>(),
                        new[]
                        {
                            new SchemaUniqueContract(@"UX_ProductosServiciosAtributos_Empresa_Id", new[] { @"idEmpresa", @"id" }, null),
                            new SchemaUniqueContract(@"UX_ProductosServiciosAtributos_Empresa_Nombre", new[] { @"idEmpresa", @"Nombre" }, null),
                        },
                        Array.Empty<SchemaCheckContract>(),
                        new[]
                        {
                            new SchemaIndexContract(@"UX_ProductosServiciosAtributos_Empresa_Id", true, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"id", false) }, Array.Empty<string>(), null),
                            new SchemaIndexContract(@"UX_ProductosServiciosAtributos_Empresa_Nombre", true, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"Nombre", false) }, Array.Empty<string>(), null),
                        }),
                    new SchemaTableContract(
                        "dbo",
                        @"ProductosServiciosAtributosValores",
                        new[]
                        {
                            new SchemaColumnContract(@"id", @"UNIQUEIDENTIFIER", null, null, null, false, @"(NEWID())", false, false, null),
                            new SchemaColumnContract(@"idEmpresa", @"UNIQUEIDENTIFIER", null, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"identityKey", @"UNIQUEIDENTIFIER", null, null, null, false, @"(NEWID())", false, false, null),
                            new SchemaColumnContract(@"idAtributo", @"UNIQUEIDENTIFIER", null, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"Valor", @"NVARCHAR(100)", 100, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"Orden", @"INT", null, null, null, false, @"((1))", false, false, null),
                            new SchemaColumnContract(@"Activo", @"BIT", null, null, null, false, @"((1))", false, false, null),
                            new SchemaColumnContract(@"FechaCreacion", @"DATETIME2(0)", null, null, 0, false, @"(SYSUTCDATETIME())", false, false, null),
                            new SchemaColumnContract(@"FechaActualizacion", @"DATETIME2(0)", null, null, 0, false, @"(SYSUTCDATETIME())", false, false, null),
                            new SchemaColumnContract(@"FechaArchivado", @"DATETIME2(0)", null, null, 0, true, null, false, false, null),
                        },
                        new SchemaPrimaryKeyContract(@"PK_ProductosServiciosAtributosValores", new[] { "id" }, true),
                        new[]
                        {
                            new SchemaForeignKeyContract(@"FK_ProductosServiciosAtributosValores_Atributos_EmpresaId", new[] { @"idEmpresa", @"idAtributo" }, @"dbo", @"ProductosServiciosAtributos", new[] { @"idEmpresa", @"id" }),
                        },
                        new[]
                        {
                            new SchemaUniqueContract(@"UX_ProductosServiciosAtributosValores_Empresa_Id", new[] { @"idEmpresa", @"id" }, null),
                            new SchemaUniqueContract(@"UX_ProductosServiciosAtributosValores_Empresa_Atributo_Valor", new[] { @"idEmpresa", @"idAtributo", @"Valor" }, null),
                        },
                        Array.Empty<SchemaCheckContract>(),
                        new[]
                        {
                            new SchemaIndexContract(@"UX_ProductosServiciosAtributosValores_Empresa_Id", true, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"id", false) }, Array.Empty<string>(), null),
                            new SchemaIndexContract(@"UX_ProductosServiciosAtributosValores_Empresa_Atributo_Valor", true, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"idAtributo", false), new SchemaIndexColumnContract(@"Valor", false) }, Array.Empty<string>(), null),
                        }),
                    new SchemaTableContract(
                        "dbo",
                        @"ProductosServiciosProductoAtributos",
                        new[]
                        {
                            new SchemaColumnContract(@"id", @"UNIQUEIDENTIFIER", null, null, null, false, @"(NEWID())", false, false, null),
                            new SchemaColumnContract(@"idEmpresa", @"UNIQUEIDENTIFIER", null, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"identityKey", @"UNIQUEIDENTIFIER", null, null, null, false, @"(NEWID())", false, false, null),
                            new SchemaColumnContract(@"idProductoServicio", @"UNIQUEIDENTIFIER", null, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"idAtributo", @"UNIQUEIDENTIFIER", null, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"Orden", @"INT", null, null, null, false, @"((1))", false, false, null),
                            new SchemaColumnContract(@"Activo", @"BIT", null, null, null, false, @"((1))", false, false, null),
                            new SchemaColumnContract(@"FechaCreacion", @"DATETIME2(0)", null, null, 0, false, @"(SYSUTCDATETIME())", false, false, null),
                            new SchemaColumnContract(@"FechaActualizacion", @"DATETIME2(0)", null, null, 0, false, @"(SYSUTCDATETIME())", false, false, null),
                        },
                        new SchemaPrimaryKeyContract(@"PK_ProductosServiciosProductoAtributos", new[] { "id" }, true),
                        new[]
                        {
                            new SchemaForeignKeyContract(@"FK_ProductosServiciosProductoAtributos_Productos_EmpresaId", new[] { @"idEmpresa", @"idProductoServicio" }, @"dbo", @"ProductosServicios", new[] { @"idEmpresa", @"id" }),
                            new SchemaForeignKeyContract(@"FK_ProductosServiciosProductoAtributos_Atributos_EmpresaId", new[] { @"idEmpresa", @"idAtributo" }, @"dbo", @"ProductosServiciosAtributos", new[] { @"idEmpresa", @"id" }),
                        },
                        new[]
                        {
                            new SchemaUniqueContract(@"UX_ProductosServiciosProductoAtributos_Empresa_Id", new[] { @"idEmpresa", @"id" }, null),
                            new SchemaUniqueContract(@"UX_ProductosServiciosProductoAtributos_Empresa_Producto_Atributo", new[] { @"idEmpresa", @"idProductoServicio", @"idAtributo" }, null),
                        },
                        Array.Empty<SchemaCheckContract>(),
                        new[]
                        {
                            new SchemaIndexContract(@"UX_ProductosServiciosProductoAtributos_Empresa_Id", true, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"id", false) }, Array.Empty<string>(), null),
                            new SchemaIndexContract(@"UX_ProductosServiciosProductoAtributos_Empresa_Producto_Atributo", true, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"idProductoServicio", false), new SchemaIndexColumnContract(@"idAtributo", false) }, Array.Empty<string>(), null),
                        }),
                    new SchemaTableContract(
                        "dbo",
                        @"ProductosServiciosProductoAtributoValores",
                        new[]
                        {
                            new SchemaColumnContract(@"id", @"UNIQUEIDENTIFIER", null, null, null, false, @"(NEWID())", false, false, null),
                            new SchemaColumnContract(@"idEmpresa", @"UNIQUEIDENTIFIER", null, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"identityKey", @"UNIQUEIDENTIFIER", null, null, null, false, @"(NEWID())", false, false, null),
                            new SchemaColumnContract(@"idProductoAtributo", @"UNIQUEIDENTIFIER", null, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"idAtributoValor", @"UNIQUEIDENTIFIER", null, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"Orden", @"INT", null, null, null, false, @"((1))", false, false, null),
                            new SchemaColumnContract(@"Activo", @"BIT", null, null, null, false, @"((1))", false, false, null),
                            new SchemaColumnContract(@"FechaCreacion", @"DATETIME2(0)", null, null, 0, false, @"(SYSUTCDATETIME())", false, false, null),
                            new SchemaColumnContract(@"FechaActualizacion", @"DATETIME2(0)", null, null, 0, false, @"(SYSUTCDATETIME())", false, false, null),
                        },
                        new SchemaPrimaryKeyContract(@"PK_ProductosServiciosProductoAtributoValores", new[] { "id" }, true),
                        new[]
                        {
                            new SchemaForeignKeyContract(@"FK_ProductosServiciosProductoAtributoValores_ProductoAtributos_EmpresaId", new[] { @"idEmpresa", @"idProductoAtributo" }, @"dbo", @"ProductosServiciosProductoAtributos", new[] { @"idEmpresa", @"id" }),
                            new SchemaForeignKeyContract(@"FK_ProductosServiciosProductoAtributoValores_AtributosValores_EmpresaId", new[] { @"idEmpresa", @"idAtributoValor" }, @"dbo", @"ProductosServiciosAtributosValores", new[] { @"idEmpresa", @"id" }),
                        },
                        new[]
                        {
                            new SchemaUniqueContract(@"UX_ProductosServiciosProductoAtributoValores_Empresa_ProductoAtributo_Valor", new[] { @"idEmpresa", @"idProductoAtributo", @"idAtributoValor" }, null),
                        },
                        Array.Empty<SchemaCheckContract>(),
                        new[]
                        {
                            new SchemaIndexContract(@"UX_ProductosServiciosProductoAtributoValores_Empresa_ProductoAtributo_Valor", true, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"idProductoAtributo", false), new SchemaIndexColumnContract(@"idAtributoValor", false) }, Array.Empty<string>(), null),
                        }),
                    new SchemaTableContract(
                        "dbo",
                        @"ProductosServiciosTags",
                        new[]
                        {
                            new SchemaColumnContract(@"id", @"UNIQUEIDENTIFIER", null, null, null, false, @"(NEWID())", false, false, null),
                            new SchemaColumnContract(@"idEmpresa", @"UNIQUEIDENTIFIER", null, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"identityKey", @"UNIQUEIDENTIFIER", null, null, null, false, @"(NEWID())", false, false, null),
                            new SchemaColumnContract(@"Nombre", @"NVARCHAR(100)", 100, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"Activo", @"BIT", null, null, null, false, @"((1))", false, false, null),
                            new SchemaColumnContract(@"FechaCreacion", @"DATETIME2(0)", null, null, 0, false, @"(SYSUTCDATETIME())", false, false, null),
                            new SchemaColumnContract(@"FechaActualizacion", @"DATETIME2(0)", null, null, 0, false, @"(SYSUTCDATETIME())", false, false, null),
                            new SchemaColumnContract(@"FechaArchivado", @"DATETIME2(0)", null, null, 0, true, null, false, false, null),
                            new SchemaColumnContract(@"NombreNormalizado", @"NVARCHAR(100)", 100, null, null, true, null, false, true, @"UPPER(LTRIM(RTRIM(Nombre)))"),
                        },
                        new SchemaPrimaryKeyContract(@"PK_ProductosServiciosTags", new[] { "id" }, true),
                        Array.Empty<SchemaForeignKeyContract>(),
                        new[]
                        {
                            new SchemaUniqueContract(@"UX_ProductosServiciosTags_Empresa_Id", new[] { @"idEmpresa", @"id" }, null),
                            new SchemaUniqueContract(@"UX_ProductosServiciosTags_Empresa_NombreNormalizado", new[] { @"idEmpresa", @"NombreNormalizado" }, null),
                        },
                        Array.Empty<SchemaCheckContract>(),
                        new[]
                        {
                            new SchemaIndexContract(@"UX_ProductosServiciosTags_Empresa_Id", true, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"id", false) }, Array.Empty<string>(), null),
                            new SchemaIndexContract(@"UX_ProductosServiciosTags_Empresa_NombreNormalizado", true, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"NombreNormalizado", false) }, Array.Empty<string>(), null),
                        }),
                    new SchemaTableContract(
                        "dbo",
                        @"ProductosServiciosProductoTags",
                        new[]
                        {
                            new SchemaColumnContract(@"id", @"UNIQUEIDENTIFIER", null, null, null, false, @"(NEWID())", false, false, null),
                            new SchemaColumnContract(@"idEmpresa", @"UNIQUEIDENTIFIER", null, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"identityKey", @"UNIQUEIDENTIFIER", null, null, null, false, @"(NEWID())", false, false, null),
                            new SchemaColumnContract(@"idProductoServicio", @"UNIQUEIDENTIFIER", null, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"idTag", @"UNIQUEIDENTIFIER", null, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"FechaCreacion", @"DATETIME2(0)", null, null, 0, false, @"(SYSUTCDATETIME())", false, false, null),
                        },
                        new SchemaPrimaryKeyContract(@"PK_ProductosServiciosProductoTags", new[] { "id" }, true),
                        new[]
                        {
                            new SchemaForeignKeyContract(@"FK_ProductosServiciosProductoTags_Productos_EmpresaId", new[] { @"idEmpresa", @"idProductoServicio" }, @"dbo", @"ProductosServicios", new[] { @"idEmpresa", @"id" }),
                            new SchemaForeignKeyContract(@"FK_ProductosServiciosProductoTags_Tags_EmpresaId", new[] { @"idEmpresa", @"idTag" }, @"dbo", @"ProductosServiciosTags", new[] { @"idEmpresa", @"id" }),
                        },
                        new[]
                        {
                            new SchemaUniqueContract(@"UX_ProductosServiciosProductoTags_Empresa_Producto_Tag", new[] { @"idEmpresa", @"idProductoServicio", @"idTag" }, null),
                        },
                        Array.Empty<SchemaCheckContract>(),
                        new[]
                        {
                            new SchemaIndexContract(@"UX_ProductosServiciosProductoTags_Empresa_Producto_Tag", true, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"idProductoServicio", false), new SchemaIndexColumnContract(@"idTag", false) }, Array.Empty<string>(), null),
                            new SchemaIndexContract(@"IX_ProductosServiciosProductoTags_Empresa_Tag", false, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"idTag", false), new SchemaIndexColumnContract(@"idProductoServicio", false) }, Array.Empty<string>(), null),
                        }),
                    new SchemaTableContract(
                        "dbo",
                        @"ProductosServiciosOpcionesVariante",
                        new[]
                        {
                            new SchemaColumnContract(@"id", @"UNIQUEIDENTIFIER", null, null, null, false, @"(NEWID())", false, false, null),
                            new SchemaColumnContract(@"idEmpresa", @"UNIQUEIDENTIFIER", null, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"identityKey", @"UNIQUEIDENTIFIER", null, null, null, false, @"(NEWID())", false, false, null),
                            new SchemaColumnContract(@"idProductoServicio", @"UNIQUEIDENTIFIER", null, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"Nombre", @"NVARCHAR(100)", 100, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"Orden", @"INT", null, null, null, false, @"((1))", false, false, null),
                            new SchemaColumnContract(@"Activo", @"BIT", null, null, null, false, @"((1))", false, false, null),
                            new SchemaColumnContract(@"FechaCreacion", @"DATETIME2(0)", null, null, 0, false, @"(SYSUTCDATETIME())", false, false, null),
                            new SchemaColumnContract(@"FechaActualizacion", @"DATETIME2(0)", null, null, 0, false, @"(SYSUTCDATETIME())", false, false, null),
                        },
                        new SchemaPrimaryKeyContract(@"PK_ProductosServiciosOpcionesVariante", new[] { "id" }, true),
                        new[]
                        {
                            new SchemaForeignKeyContract(@"FK_ProductosServiciosOpcionesVariante_Productos_EmpresaId", new[] { @"idEmpresa", @"idProductoServicio" }, @"dbo", @"ProductosServicios", new[] { @"idEmpresa", @"id" }),
                        },
                        new[]
                        {
                            new SchemaUniqueContract(@"UX_ProductosServiciosOpcionesVariante_Empresa_Id", new[] { @"idEmpresa", @"id" }, null),
                            new SchemaUniqueContract(@"UX_ProductosServiciosOpcionesVariante_Empresa_Producto_Nombre", new[] { @"idEmpresa", @"idProductoServicio", @"Nombre" }, null),
                        },
                        Array.Empty<SchemaCheckContract>(),
                        new[]
                        {
                            new SchemaIndexContract(@"UX_ProductosServiciosOpcionesVariante_Empresa_Id", true, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"id", false) }, Array.Empty<string>(), null),
                            new SchemaIndexContract(@"UX_ProductosServiciosOpcionesVariante_Empresa_Producto_Nombre", true, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"idProductoServicio", false), new SchemaIndexColumnContract(@"Nombre", false) }, Array.Empty<string>(), null),
                        }),
                    new SchemaTableContract(
                        "dbo",
                        @"ProductosServiciosOpcionesVarianteValores",
                        new[]
                        {
                            new SchemaColumnContract(@"id", @"UNIQUEIDENTIFIER", null, null, null, false, @"(NEWID())", false, false, null),
                            new SchemaColumnContract(@"idEmpresa", @"UNIQUEIDENTIFIER", null, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"identityKey", @"UNIQUEIDENTIFIER", null, null, null, false, @"(NEWID())", false, false, null),
                            new SchemaColumnContract(@"idOpcionVariante", @"UNIQUEIDENTIFIER", null, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"Valor", @"NVARCHAR(100)", 100, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"Orden", @"INT", null, null, null, false, @"((1))", false, false, null),
                            new SchemaColumnContract(@"Activo", @"BIT", null, null, null, false, @"((1))", false, false, null),
                            new SchemaColumnContract(@"FechaCreacion", @"DATETIME2(0)", null, null, 0, false, @"(SYSUTCDATETIME())", false, false, null),
                            new SchemaColumnContract(@"FechaActualizacion", @"DATETIME2(0)", null, null, 0, false, @"(SYSUTCDATETIME())", false, false, null),
                        },
                        new SchemaPrimaryKeyContract(@"PK_ProductosServiciosOpcionesVarianteValores", new[] { "id" }, true),
                        new[]
                        {
                            new SchemaForeignKeyContract(@"FK_ProductosServiciosOpcionesVarianteValores_Opciones_EmpresaId", new[] { @"idEmpresa", @"idOpcionVariante" }, @"dbo", @"ProductosServiciosOpcionesVariante", new[] { @"idEmpresa", @"id" }),
                        },
                        new[]
                        {
                            new SchemaUniqueContract(@"UX_ProductosServiciosOpcionesVarianteValores_Empresa_Id", new[] { @"idEmpresa", @"id" }, null),
                            new SchemaUniqueContract(@"UX_ProductosServiciosOpcionesVarianteValores_Empresa_Opcion_Valor", new[] { @"idEmpresa", @"idOpcionVariante", @"Valor" }, null),
                        },
                        Array.Empty<SchemaCheckContract>(),
                        new[]
                        {
                            new SchemaIndexContract(@"UX_ProductosServiciosOpcionesVarianteValores_Empresa_Id", true, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"id", false) }, Array.Empty<string>(), null),
                            new SchemaIndexContract(@"UX_ProductosServiciosOpcionesVarianteValores_Empresa_Opcion_Valor", true, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"idOpcionVariante", false), new SchemaIndexColumnContract(@"Valor", false) }, Array.Empty<string>(), null),
                        }),
                    new SchemaTableContract(
                        "dbo",
                        @"ProductosServiciosVariantes",
                        new[]
                        {
                            new SchemaColumnContract(@"id", @"UNIQUEIDENTIFIER", null, null, null, false, @"(NEWID())", false, false, null),
                            new SchemaColumnContract(@"idEmpresa", @"UNIQUEIDENTIFIER", null, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"identityKey", @"UNIQUEIDENTIFIER", null, null, null, false, @"(NEWID())", false, false, null),
                            new SchemaColumnContract(@"idProductoServicio", @"UNIQUEIDENTIFIER", null, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"Sku", @"NVARCHAR(100)", 100, null, null, true, null, false, false, null),
                            new SchemaColumnContract(@"Nombre", @"NVARCHAR(200)", 200, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"ClaveCombinacion", @"NVARCHAR(500)", 500, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"ImagenUrl", @"NVARCHAR(1000)", 1000, null, null, true, null, false, false, null),
                            new SchemaColumnContract(@"ImagenNombre", @"NVARCHAR(255)", 255, null, null, true, null, false, false, null),
                            new SchemaColumnContract(@"Costo", @"DECIMAL(18,2)", null, 18, 2, true, null, false, false, null),
                            new SchemaColumnContract(@"PrecioPublico", @"DECIMAL(18,2)", null, 18, 2, true, null, false, false, null),
                            new SchemaColumnContract(@"PrecioComparacion", @"DECIMAL(18,2)", null, 18, 2, true, null, false, false, null),
                            new SchemaColumnContract(@"PrecioUnitarioMonto", @"DECIMAL(18,6)", null, 18, 6, true, null, false, false, null),
                            new SchemaColumnContract(@"PrecioUnitarioBaseCantidad", @"DECIMAL(18,6)", null, 18, 6, true, null, false, false, null),
                            new SchemaColumnContract(@"PrecioUnitarioUnidad", @"NVARCHAR(20)", 20, null, null, true, null, false, false, null),
                            new SchemaColumnContract(@"Orden", @"INT", null, null, null, false, @"((1))", false, false, null),
                            new SchemaColumnContract(@"Activo", @"BIT", null, null, null, false, @"((1))", false, false, null),
                            new SchemaColumnContract(@"FechaCreacion", @"DATETIME2(0)", null, null, 0, false, @"(SYSUTCDATETIME())", false, false, null),
                            new SchemaColumnContract(@"FechaActualizacion", @"DATETIME2(0)", null, null, 0, false, @"(SYSUTCDATETIME())", false, false, null),
                        },
                        new SchemaPrimaryKeyContract(@"PK_ProductosServiciosVariantes", new[] { "id" }, true),
                        new[]
                        {
                            new SchemaForeignKeyContract(@"FK_ProductosServiciosVariantes_Productos_EmpresaId", new[] { @"idEmpresa", @"idProductoServicio" }, @"dbo", @"ProductosServicios", new[] { @"idEmpresa", @"id" }),
                        },
                        new[]
                        {
                            new SchemaUniqueContract(@"UX_ProductosServiciosVariantes_Empresa_Id", new[] { @"idEmpresa", @"id" }, null),
                            new SchemaUniqueContract(@"UX_ProductosServiciosVariantes_Empresa_Producto_ClaveCombinacion", new[] { @"idEmpresa", @"idProductoServicio", @"ClaveCombinacion" }, null),
                        },
                        Array.Empty<SchemaCheckContract>(),
                        new[]
                        {
                            new SchemaIndexContract(@"UX_ProductosServiciosVariantes_Empresa_Id", true, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"id", false) }, Array.Empty<string>(), null),
                            new SchemaIndexContract(@"UX_ProductosServiciosVariantes_Empresa_Producto_ClaveCombinacion", true, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"idProductoServicio", false), new SchemaIndexColumnContract(@"ClaveCombinacion", false) }, Array.Empty<string>(), null),
                        }),
                    new SchemaTableContract(
                        "dbo",
                        @"ProductosServiciosVarianteValores",
                        new[]
                        {
                            new SchemaColumnContract(@"id", @"UNIQUEIDENTIFIER", null, null, null, false, @"(NEWID())", false, false, null),
                            new SchemaColumnContract(@"idEmpresa", @"UNIQUEIDENTIFIER", null, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"identityKey", @"UNIQUEIDENTIFIER", null, null, null, false, @"(NEWID())", false, false, null),
                            new SchemaColumnContract(@"idVariante", @"UNIQUEIDENTIFIER", null, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"idAtributo", @"UNIQUEIDENTIFIER", null, null, null, true, null, false, false, null),
                            new SchemaColumnContract(@"idAtributoValor", @"UNIQUEIDENTIFIER", null, null, null, true, null, false, false, null),
                            new SchemaColumnContract(@"idOpcionVariante", @"UNIQUEIDENTIFIER", null, null, null, true, null, false, false, null),
                            new SchemaColumnContract(@"idOpcionVarianteValor", @"UNIQUEIDENTIFIER", null, null, null, true, null, false, false, null),
                            new SchemaColumnContract(@"Orden", @"INT", null, null, null, false, @"((1))", false, false, null),
                            new SchemaColumnContract(@"FechaCreacion", @"DATETIME2(0)", null, null, 0, false, @"(SYSUTCDATETIME())", false, false, null),
                        },
                        new SchemaPrimaryKeyContract(@"PK_ProductosServiciosVarianteValores", new[] { "id" }, true),
                        new[]
                        {
                            new SchemaForeignKeyContract(@"FK_ProductosServiciosVarianteValores_Variantes_EmpresaId", new[] { @"idEmpresa", @"idVariante" }, @"dbo", @"ProductosServiciosVariantes", new[] { @"idEmpresa", @"id" }),
                            new SchemaForeignKeyContract(@"FK_ProductosServiciosVarianteValores_Atributos_EmpresaId", new[] { @"idEmpresa", @"idAtributo" }, @"dbo", @"ProductosServiciosAtributos", new[] { @"idEmpresa", @"id" }),
                            new SchemaForeignKeyContract(@"FK_ProductosServiciosVarianteValores_AtributosValores_EmpresaId", new[] { @"idEmpresa", @"idAtributoValor" }, @"dbo", @"ProductosServiciosAtributosValores", new[] { @"idEmpresa", @"id" }),
                            new SchemaForeignKeyContract(@"FK_ProductosServiciosVarianteValores_Opciones_EmpresaId", new[] { @"idEmpresa", @"idOpcionVariante" }, @"dbo", @"ProductosServiciosOpcionesVariante", new[] { @"idEmpresa", @"id" }),
                            new SchemaForeignKeyContract(@"FK_ProductosServiciosVarianteValores_OpcionesValores_EmpresaId", new[] { @"idEmpresa", @"idOpcionVarianteValor" }, @"dbo", @"ProductosServiciosOpcionesVarianteValores", new[] { @"idEmpresa", @"id" }),
                        },
                        Array.Empty<SchemaUniqueContract>(),
                        Array.Empty<SchemaCheckContract>(),
                        new[]
                        {
                            new SchemaIndexContract(@"IX_ProductosServiciosVarianteValores_Empresa_Variante_Orden", false, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"idVariante", false), new SchemaIndexColumnContract(@"Orden", false) }, Array.Empty<string>(), null),
                        }),
                    new SchemaTableContract(
                        "dbo",
                        @"ProductosServiciosMultimedia",
                        new[]
                        {
                            new SchemaColumnContract(@"id", @"UNIQUEIDENTIFIER", null, null, null, false, @"(NEWID())", false, false, null),
                            new SchemaColumnContract(@"idEmpresa", @"UNIQUEIDENTIFIER", null, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"identityKey", @"UNIQUEIDENTIFIER", null, null, null, false, @"(NEWID())", false, false, null),
                            new SchemaColumnContract(@"idProductoServicio", @"UNIQUEIDENTIFIER", null, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"TipoMultimedia", @"NVARCHAR(20)", 20, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"Foto", @"BIT", null, null, null, false, @"((0))", false, false, null),
                            new SchemaColumnContract(@"Video", @"BIT", null, null, null, false, @"((0))", false, false, null),
                            new SchemaColumnContract(@"Documento", @"BIT", null, null, null, false, @"((0))", false, false, null),
                            new SchemaColumnContract(@"NombreOriginal", @"NVARCHAR(255)", 255, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"NombreAlmacenado", @"NVARCHAR(255)", 255, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"Extension", @"NVARCHAR(20)", 20, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"MimeType", @"NVARCHAR(120)", 120, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"UrlFirebase", @"NVARCHAR(1000)", 1000, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"PesoBytes", @"BIGINT", null, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"Orden", @"INT", null, null, null, false, @"((1))", false, false, null),
                            new SchemaColumnContract(@"Activo", @"BIT", null, null, null, false, @"((1))", false, false, null),
                            new SchemaColumnContract(@"FechaCreacion", @"DATETIME2(0)", null, null, 0, false, @"(SYSUTCDATETIME())", false, false, null),
                            new SchemaColumnContract(@"FechaActualizacion", @"DATETIME2(0)", null, null, 0, false, @"(SYSUTCDATETIME())", false, false, null),
                        },
                        new SchemaPrimaryKeyContract(@"PK_ProductosServiciosMultimedia", new[] { "id" }, true),
                        new[]
                        {
                            new SchemaForeignKeyContract(@"FK_ProductosServiciosMultimedia_Productos_EmpresaId", new[] { @"idEmpresa", @"idProductoServicio" }, @"dbo", @"ProductosServicios", new[] { @"idEmpresa", @"id" }),
                        },
                        Array.Empty<SchemaUniqueContract>(),
                        Array.Empty<SchemaCheckContract>(),
                        new[]
                        {
                            new SchemaIndexContract(@"IX_ProductosServiciosMultimedia_Empresa_Producto", false, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"idProductoServicio", false), new SchemaIndexColumnContract(@"Activo", false), new SchemaIndexColumnContract(@"TipoMultimedia", false), new SchemaIndexColumnContract(@"Orden", false) }, Array.Empty<string>(), null),
                        }),
                    new SchemaTableContract(
                        "dbo",
                        @"ProductosServiciosPresentacionesVenta",
                        new[]
                        {
                            new SchemaColumnContract(@"id", @"UNIQUEIDENTIFIER", null, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"idEmpresa", @"UNIQUEIDENTIFIER", null, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"identityKey", @"UNIQUEIDENTIFIER", null, null, null, false, @"(NEWID())", false, false, null),
                            new SchemaColumnContract(@"idProductoServicio", @"UNIQUEIDENTIFIER", null, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"Nombre", @"NVARCHAR(100)", 100, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"EquivalenciaBase", @"DECIMAL(18,4)", null, 18, 4, false, null, false, false, null),
                            new SchemaColumnContract(@"Precio", @"DECIMAL(18,2)", null, 18, 2, false, null, false, false, null),
                            new SchemaColumnContract(@"EsPredeterminada", @"BIT", null, null, null, false, @"((0))", false, false, null),
                            new SchemaColumnContract(@"Orden", @"INT", null, null, null, false, @"((0))", false, false, null),
                            new SchemaColumnContract(@"Activo", @"BIT", null, null, null, false, @"((1))", false, false, null),
                            new SchemaColumnContract(@"FechaCreacion", @"DATETIME2(0)", null, null, 0, false, @"(SYSUTCDATETIME())", false, false, null),
                            new SchemaColumnContract(@"FechaActualizacion", @"DATETIME2(0)", null, null, 0, false, @"(SYSUTCDATETIME())", false, false, null),
                            new SchemaColumnContract(@"FechaArchivado", @"DATETIME2(0)", null, null, 0, true, null, false, false, null),
                            new SchemaColumnContract(@"CantidadVenta", @"DECIMAL(18,4)", null, 18, 4, false, @"((1))", false, false, null),
                            new SchemaColumnContract(@"idUnidadVenta", @"UNIQUEIDENTIFIER", null, null, null, false, null, false, false, null),
                        },
                        new SchemaPrimaryKeyContract(@"PK_ProductosServiciosPresentacionesVenta", new[] { "id" }, true),
                        new[]
                        {
                            new SchemaForeignKeyContract(@"FK_ProductosServiciosPresentacionesVenta_Productos_EmpresaId", new[] { @"idEmpresa", @"idProductoServicio" }, @"dbo", @"ProductosServicios", new[] { @"idEmpresa", @"id" }),
                        },
                        new[]
                        {
                            new SchemaUniqueContract(@"UX_ProductosServiciosPresentacionesVenta_Empresa_Id", new[] { @"idEmpresa", @"id" }, null),
                            new SchemaUniqueContract(@"UX_ProductosServiciosPresentacionesVenta_PredeterminadaActiva", new[] { @"idEmpresa", @"idProductoServicio" }, @"Activo = 1 AND EsPredeterminada = 1"),
                        },
                        new[]
                        {
                            new SchemaCheckContract(@"CK_ProductosServiciosPresentacionesVenta_Equivalencia", @"CHECK (EquivalenciaBase > 0)"),
                            new SchemaCheckContract(@"CK_ProductosServiciosPresentacionesVenta_Precio", @"CHECK (Precio >= 0)"),
                            new SchemaCheckContract(@"CK_PSPresentaciones_CantidadVenta", @"CHECK (CantidadVenta > 0)"),
                        },
                        new[]
                        {
                            new SchemaIndexContract(@"UX_ProductosServiciosPresentacionesVenta_Empresa_Id", true, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"id", false) }, Array.Empty<string>(), null),
                            new SchemaIndexContract(@"IX_ProductosServiciosPresentacionesVenta_Empresa_Producto_Activo_Orden", false, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"idProductoServicio", false), new SchemaIndexColumnContract(@"Activo", false), new SchemaIndexColumnContract(@"Orden", false) }, Array.Empty<string>(), null),
                            new SchemaIndexContract(@"UX_ProductosServiciosPresentacionesVenta_PredeterminadaActiva", true, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"idProductoServicio", false) }, Array.Empty<string>(), @"Activo = 1 AND EsPredeterminada = 1"),
                        }),
                },
                new[]
                {
                    @"inspectorapi/checklistWs/Scripts/productos-servicios-up.sql",
                    @"inspectorapi/checklistWs/Scripts/ticket-09-presentaciones-venta-up.sql",
                    @"inspectorapi/checklistWs/Scripts/ticket-10-unidades-controladas-up.sql",
                    @"inspectorapi/checklistWs/Scripts/ticket-10-unidades-catalogo-tiempo-up.sql",
                    @"inspectorapi/checklistWs/Scripts/ticket-10-unidades-tiempo-no-convertible-up.sql",
                    @"inspectorapi/checklistWs/Scripts/ticket-10-catalogo-metrico-us-up.sql",
                    @"inspector/docs/productos-servicios/auditoria-20260909/ESQUEMA_DECLARADO_NO_CERTIFICADO.md",
                },
                new DateTime(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc));
        }

        private static SchemaContract GetSucursalesContract(int? version)
        {
            int contractVersion = version ?? SucursalesLatestVersion;
            if (contractVersion is not V1 and not V2)
            {
                throw new InvalidOperationException("SCHEMA_CONTRACT_VERSION_NOT_SUPPORTED");
            }

            SchemaColumnContract notasRazones = NotasSucursales(contractVersion, @"TEXT", 16);
            SchemaColumnContract notasZonas = NotasSucursales(contractVersion, @"VARCHAR(255)", 255);
            SchemaColumnContract notasTipos = NotasSucursales(contractVersion, @"VARCHAR(256)", 256);
            SchemaColumnContract notasSucursales = NotasSucursales(contractVersion, @"VARCHAR(255)", 255);

            return new SchemaContract(
                DatabaseScopes.Sucursales,
                contractVersion,
                "Sucursales",
                new[]
                {
                    new SchemaTableContract(
                        "dbo",
                        @"RazonesSociales",
                        new[]
                        {
                            new SchemaColumnContract(@"Id", @"UNIQUEIDENTIFIER", null, null, null, false, @"(NEWID())", false, false, null),
                            new SchemaColumnContract(@"IdEmpresa", @"UNIQUEIDENTIFIER", null, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"Nombre", @"VARCHAR(200)", 200, null, null, true, @"(NULL)", false, false, null),
                            new SchemaColumnContract(@"Representante", @"VARCHAR(255)", 255, null, null, true, @"(NULL)", false, false, null),
                            new SchemaColumnContract(@"RFC", @"VARCHAR(20)", 20, null, null, true, @"(NULL)", false, false, null),
                            new SchemaColumnContract(@"Direccion", @"VARCHAR(255)", 255, null, null, true, @"(NULL)", false, false, null),
                            new SchemaColumnContract(@"Colonia", @"VARCHAR(255)", 255, null, null, true, @"(NULL)", false, false, null),
                            new SchemaColumnContract(@"CodigoPostal", @"VARCHAR(20)", 20, null, null, true, @"(NULL)", false, false, null),
                            new SchemaColumnContract(@"Ciudad", @"VARCHAR(255)", 255, null, null, true, @"(NULL)", false, false, null),
                            new SchemaColumnContract(@"Estado", @"VARCHAR(255)", 255, null, null, true, @"(NULL)", false, false, null),
                            new SchemaColumnContract(@"Pais", @"VARCHAR(255)", 255, null, null, true, @"(NULL)", false, false, null),
                            new SchemaColumnContract(@"Telefono", @"NVARCHAR(13)", 13, null, null, true, @"(NULL)", false, false, null),
                            new SchemaColumnContract(@"Regimen1", @"VARCHAR(255)", 255, null, null, true, @"(NULL)", false, false, null),
                            new SchemaColumnContract(@"Fecha", @"DATETIME", null, null, null, true, @"(GETDATE())", false, false, null),
                            new SchemaColumnContract(@"usuario", @"VARCHAR(256)", 256, null, null, true, @"(NULL)", false, false, null),
                            new SchemaColumnContract(@"tipo", @"NUMERIC(1,0)", null, 1, 0, true, @"((1))", false, false, null),
                            new SchemaColumnContract(@"IMGFIREBASE", @"NVARCHAR(MAX)", -1, null, null, true, null, false, false, null),
                            new SchemaColumnContract(@"ContribEspecial", @"NVARCHAR(13)", 13, null, null, true, @"(NULL)", false, false, null),
                            new SchemaColumnContract(@"ObligaConta", @"BIT", null, null, null, true, @"((0))", false, false, null),
                            new SchemaColumnContract(@"NombreComercial", @"NVARCHAR(300)", 300, null, null, true, @"(NULL)", false, false, null),
                            new SchemaColumnContract(@"registroPatronal", @"NVARCHAR(50)", 50, null, null, true, null, false, false, null),
                            new SchemaColumnContract(@"SitioWeb", @"NVARCHAR(MAX)", -1, null, null, true, null, false, false, null),
                            new SchemaColumnContract(@"SitioEncuesta", @"NVARCHAR(MAX)", -1, null, null, true, null, false, false, null),
                            notasRazones,
                            new SchemaColumnContract(@"borrado", @"BIT", null, null, null, true, @"((0))", false, false, null),
                        },
                        new SchemaPrimaryKeyContract(@"PK_RazonesSociales", new[] { "Id" }, true),
                        Array.Empty<SchemaForeignKeyContract>(),
                        new[]
                        {
                            new SchemaUniqueContract(@"UX_RazonesSociales_Empresa_Id", new[] { @"IdEmpresa", @"Id" }, null),
                        },
                        Array.Empty<SchemaCheckContract>(),
                        new[]
                        {
                            new SchemaIndexContract(@"UX_RazonesSociales_Empresa_Id", true, false, new[] { new SchemaIndexColumnContract(@"IdEmpresa", false), new SchemaIndexColumnContract(@"Id", false) }, Array.Empty<string>(), null),
                            new SchemaIndexContract(@"IX_RazonesSociales_Empresa_Nombre", false, false, new[] { new SchemaIndexColumnContract(@"IdEmpresa", false), new SchemaIndexColumnContract(@"Nombre", false) }, Array.Empty<string>(), null),
                        }),
                    new SchemaTableContract(
                        "dbo",
                        @"Zonas",
                        new[]
                        {
                            new SchemaColumnContract(@"Id", @"UNIQUEIDENTIFIER", null, null, null, false, @"(NEWID())", false, false, null),
                            new SchemaColumnContract(@"Nombre", @"VARCHAR(255)", 255, null, null, false, null, false, false, null),
                            notasZonas,
                            new SchemaColumnContract(@"Fecha", @"DATETIME", null, null, null, true, @"(GETDATE())", false, false, null),
                            new SchemaColumnContract(@"IdEmpresa", @"UNIQUEIDENTIFIER", null, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"borrado", @"BIT", null, null, null, true, @"((0))", false, false, null),
                        },
                        new SchemaPrimaryKeyContract(@"PK_Zonas", new[] { "Id" }, true),
                        Array.Empty<SchemaForeignKeyContract>(),
                        new[]
                        {
                            new SchemaUniqueContract(@"UX_Zonas_Empresa_Id", new[] { @"IdEmpresa", @"Id" }, null),
                        },
                        Array.Empty<SchemaCheckContract>(),
                        new[]
                        {
                            new SchemaIndexContract(@"UX_Zonas_Empresa_Id", true, false, new[] { new SchemaIndexColumnContract(@"IdEmpresa", false), new SchemaIndexColumnContract(@"Id", false) }, Array.Empty<string>(), null),
                            new SchemaIndexContract(@"IX_Zonas_Empresa_Nombre", false, false, new[] { new SchemaIndexColumnContract(@"IdEmpresa", false), new SchemaIndexColumnContract(@"Nombre", false) }, Array.Empty<string>(), null),
                        }),
                    new SchemaTableContract(
                        "dbo",
                        @"SucursalesTipos",
                        new[]
                        {
                            new SchemaColumnContract(@"Nombre", @"NVARCHAR(MAX)", -1, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"Fecha", @"DATETIME", null, null, null, true, @"(GETDATE())", false, false, null),
                            new SchemaColumnContract(@"Id", @"UNIQUEIDENTIFIER", null, null, null, false, @"(NEWID())", false, false, null),
                            new SchemaColumnContract(@"IdEmpresa", @"UNIQUEIDENTIFIER", null, null, null, true, null, false, false, null),
                            new SchemaColumnContract(@"borrado", @"BIT", null, null, null, true, @"((0))", false, false, null),
                            new SchemaColumnContract(@"Virtual", @"BIT", null, null, null, true, @"((0))", false, false, null),
                            notasTipos,
                        },
                        new SchemaPrimaryKeyContract(@"PK_SucursalesTipos", new[] { "Id" }, true),
                        Array.Empty<SchemaForeignKeyContract>(),
                        Array.Empty<SchemaUniqueContract>(),
                        Array.Empty<SchemaCheckContract>(),
                        Array.Empty<SchemaIndexContract>()),
                    new SchemaTableContract(
                        "dbo",
                        @"Sucursales",
                        new[]
                        {
                            new SchemaColumnContract(@"Id", @"UNIQUEIDENTIFIER", null, null, null, false, @"(NEWID())", false, false, null),
                            new SchemaColumnContract(@"IdEmpresa", @"UNIQUEIDENTIFIER", null, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"Nombre", @"NVARCHAR(MAX)", -1, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"Direccion", @"NVARCHAR(MAX)", -1, null, null, true, null, false, false, null),
                            new SchemaColumnContract(@"Ciudad", @"NVARCHAR(MAX)", -1, null, null, true, null, false, false, null),
                            new SchemaColumnContract(@"latitud", @"NVARCHAR(MAX)", -1, null, null, true, null, false, false, null),
                            new SchemaColumnContract(@"longitud", @"NVARCHAR(MAX)", -1, null, null, true, null, false, false, null),
                            new SchemaColumnContract(@"Telefono", @"VARCHAR(255)", 255, null, null, true, null, false, false, null),
                            new SchemaColumnContract(@"Numero", @"VARCHAR(255)", 255, null, null, true, null, false, false, null),
                            new SchemaColumnContract(@"Correo", @"VARCHAR(255)", 255, null, null, true, null, false, false, null),
                            new SchemaColumnContract(@"Pais", @"VARCHAR(255)", 255, null, null, true, @"((0))", false, false, null),
                            new SchemaColumnContract(@"IdTitular", @"UNIQUEIDENTIFIER", null, null, null, true, null, false, false, null),
                            new SchemaColumnContract(@"IdRazonSocial", @"UNIQUEIDENTIFIER", null, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"IdZona", @"UNIQUEIDENTIFIER", null, null, null, true, null, false, false, null),
                            new SchemaColumnContract(@"IdSucursalTipo", @"UNIQUEIDENTIFIER", null, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"borrado", @"BIT", null, null, null, true, @"((0))", false, false, null),
                            new SchemaColumnContract(@"Fecha", @"DATETIME2(0)", null, null, 0, true, null, false, false, null),
                            notasSucursales,
                            new SchemaColumnContract(@"LinkImagen", @"VARCHAR(255)", 255, null, null, true, null, false, false, null),
                            new SchemaColumnContract(@"Tipo", @"VARCHAR(255)", 255, null, null, true, null, false, false, null),
                            new SchemaColumnContract(@"web", @"BIT", null, null, null, true, @"((0))", false, false, null),
                        },
                        new SchemaPrimaryKeyContract(@"PK_Sucursales", new[] { "Id" }, true),
                        Array.Empty<SchemaForeignKeyContract>(),
                        new[]
                        {
                            new SchemaUniqueContract(@"UX_Sucursales_Empresa_Id", new[] { @"IdEmpresa", @"Id" }, null),
                        },
                        Array.Empty<SchemaCheckContract>(),
                        new[]
                        {
                            new SchemaIndexContract(@"UX_Sucursales_Empresa_Id", true, false, new[] { new SchemaIndexColumnContract(@"IdEmpresa", false), new SchemaIndexColumnContract(@"Id", false) }, Array.Empty<string>(), null),
                            new SchemaIndexContract(@"IX_Sucursales_Empresa_RazonZona", false, false, new[] { new SchemaIndexColumnContract(@"IdEmpresa", false), new SchemaIndexColumnContract(@"IdRazonSocial", false), new SchemaIndexColumnContract(@"IdZona", false) }, Array.Empty<string>(), null),
                        }),
                },
                new[]
                {
                    @"inspectorapi/checklistWs/Controllers/Sucursal/SucursalController.cs",
                    @"inspectorapi/checklistWs/Controllers/RazonSocial/RazonSocialController.cs",
                    @"inspectorapi/checklistWs/Controllers/ZonaController1.cs",
                    @"inspector/docs/pattern/APLICACION_PATRON_CHECKAPP_SUCURSALES_RAZONES_REGIONES_20260916.md",
                },
                new DateTime(2026, 9, 17, 0, 0, 0, DateTimeKind.Utc));
        }

        private static SchemaColumnContract NotasSucursales(int contractVersion, string legacyType, int legacyLength)
        {
            return contractVersion >= V2
                ? new SchemaColumnContract(@"Notas", @"NVARCHAR(MAX)", -1, null, null, true, null, false, false, null)
                : new SchemaColumnContract(@"Notas", legacyType, legacyLength, null, null, true, null, false, false, null);
        }

        private static SchemaContract GetProveedoresContract(int? version)
        {
            int contractVersion = version ?? ProveedoresLatestVersion;
            if (contractVersion != V1)
            {
                throw new InvalidOperationException("SCHEMA_CONTRACT_VERSION_NOT_SUPPORTED");
            }

            return new SchemaContract(
                DatabaseScopes.Proveedores,
                contractVersion,
                "Proveedores",
                new[]
                {
                    new SchemaTableContract(
                        "dbo",
                        @"ActivosProveedores",
                        new[]
                        {
                            new SchemaColumnContract(@"id", @"UNIQUEIDENTIFIER", null, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"idEmpresa", @"UNIQUEIDENTIFIER", null, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"Codigo", @"NVARCHAR(64)", 64, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"Nombre", @"NVARCHAR(160)", 160, null, null, false, null, false, false, null),
                            new SchemaColumnContract(@"Descripcion", @"NVARCHAR(MAX)", -1, null, null, true, null, false, false, null),
                            new SchemaColumnContract(@"RazonSocial", @"NVARCHAR(250)", 250, null, null, true, null, false, false, null),
                            new SchemaColumnContract(@"RFC", @"NVARCHAR(15)", 15, null, null, true, null, false, false, null),
                            new SchemaColumnContract(@"Telefono", @"NVARCHAR(15)", 15, null, null, true, null, false, false, null),
                            new SchemaColumnContract(@"Telefono1", @"NVARCHAR(15)", 15, null, null, true, null, false, false, null),
                            new SchemaColumnContract(@"Email", @"NVARCHAR(50)", 50, null, null, true, null, false, false, null),
                            new SchemaColumnContract(@"Limite", @"DECIMAL(18,2)", null, 18, 2, false, @"((0))", false, false, null),
                            new SchemaColumnContract(@"ClasifContable", @"BIT", null, null, null, false, @"((0))", false, false, null),
                            new SchemaColumnContract(@"CuentaContable", @"NVARCHAR(255)", 255, null, null, true, null, false, false, null),
                            new SchemaColumnContract(@"Contacto", @"NVARCHAR(255)", 255, null, null, true, null, false, false, null),
                            new SchemaColumnContract(@"CuentaBancaria", @"NVARCHAR(255)", 255, null, null, true, null, false, false, null),
                            new SchemaColumnContract(@"Activo", @"BIT", null, null, null, false, @"((1))", false, false, null),
                            new SchemaColumnContract(@"FechaCreacion", @"DATETIME2(0)", null, null, 0, false, @"(SYSUTCDATETIME())", false, false, null),
                            new SchemaColumnContract(@"FechaActualizacion", @"DATETIME2(0)", null, null, 0, false, @"(SYSUTCDATETIME())", false, false, null),
                        },
                        new SchemaPrimaryKeyContract(@"PK_ActivosProveedores", new[] { "id" }, true),
                        Array.Empty<SchemaForeignKeyContract>(),
                        new[]
                        {
                            new SchemaUniqueContract(@"UX_ActivosProveedores_IdEmpresa_Codigo", new[] { @"idEmpresa", @"Codigo" }, null),
                        },
                        new[]
                        {
                            new SchemaCheckContract(@"CK_ActivosProveedores_Limite", @"CHECK (Limite >= 0)"),
                        },
                        new[]
                        {
                            new SchemaIndexContract(@"UX_ActivosProveedores_IdEmpresa_Codigo", true, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"Codigo", false) }, Array.Empty<string>(), null),
                            new SchemaIndexContract(@"IX_ActivosProveedores_EmpresaActivoNombre", false, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"Activo", false), new SchemaIndexColumnContract(@"Nombre", false), new SchemaIndexColumnContract(@"Codigo", false) }, Array.Empty<string>(), null),
                            new SchemaIndexContract(@"IX_ActivosProveedores_Empresa_Rfc", false, false, new[] { new SchemaIndexColumnContract(@"idEmpresa", false), new SchemaIndexColumnContract(@"RFC", false) }, Array.Empty<string>(), @"RFC IS NOT NULL"),
                        }),
                },
                new[]
                {
                    @"inspectorapi/checklistWs/Controllers/Activos/ActivosController.cs",
                    @"inspectorapi/checklistWs/Scripts/activos-proveedores-legacy-parity-up.sql",
                    @"inspector/docs/pattern/MOKA_PROVEEDORES_LEGACY_PATRON_CHECKAPP_20260917.md",
                },
                new DateTime(2026, 9, 17, 0, 0, 0, DateTimeKind.Utc));
        }

        private static SchemaColumnContract DescripcionCatalogo(int contractVersion)
        {
            return contractVersion >= V2
                ? new SchemaColumnContract(@"Descripcion", @"NVARCHAR(MAX)", -1, null, null, true, null, false, false, null)
                : new SchemaColumnContract(@"Descripcion", @"NVARCHAR(500)", 500, null, null, true, null, false, false, null);
        }
    }
}
