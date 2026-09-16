namespace checklistWs.Services.Tenant
{
    public sealed class ProductosServiciosSchemaContractProvider : ISchemaContractProvider
    {
        public SchemaContract GetContract(string scope, int? version = null)
        {
            if (!string.Equals(scope, DatabaseScopes.ProductosServicios, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("SCHEMA_CONTRACT_SCOPE_NOT_SUPPORTED");
            }
            if (version.HasValue && version.Value != 1)
            {
                throw new InvalidOperationException("SCHEMA_CONTRACT_VERSION_NOT_SUPPORTED");
            }
            return new SchemaContract(
                DatabaseScopes.ProductosServicios,
                1,
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
                            new SchemaColumnContract(@"Descripcion", @"NVARCHAR(500)", 500, null, null, true, null, false, false, null),
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
                            new SchemaColumnContract(@"Descripcion", @"NVARCHAR(500)", 500, null, null, true, null, false, false, null),
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
                            new SchemaColumnContract(@"Descripcion", @"NVARCHAR(500)", 500, null, null, true, null, false, false, null),
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
    }
}
