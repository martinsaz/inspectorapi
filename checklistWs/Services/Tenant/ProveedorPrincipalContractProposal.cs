namespace checklistWs.Services.Tenant
{
    public static class ProveedorPrincipalContractProposal
    {
        public const int ProveedoresSourceVersion = ProductosServiciosSchemaContractProvider.V1;
        public const int ProveedoresTargetVersion = ProductosServiciosSchemaContractProvider.V2;
        public const int ProductosServiciosSourceVersion = ProductosServiciosSchemaContractProvider.V3;
        public const int ProductosServiciosTargetVersion = ProductosServiciosSchemaContractProvider.V4;
        public const string ProveedoresMigrationId = "PROV-M20261008-V1-V2-EMPRESA-ID-CANDIDATE";
        public const string ProductosServiciosMigrationId = "PS-M20261008-V3-V4-PROVEEDOR-PRINCIPAL";

        public static SchemaContract CreateProveedoresV2Contract(ISchemaContractProvider provider)
        {
            SchemaContract source = provider.GetContract(DatabaseScopes.Proveedores, ProveedoresSourceVersion);
            SchemaTableContract table = source.Tables.Single(item => item.Name == "ActivosProveedores");
            SchemaTableContract updated = table with
            {
                UniqueConstraints = table.UniqueConstraints.Concat(new[]
                {
                    new SchemaUniqueContract("UX_ActivosProveedores_Empresa_Id", new[] { "idEmpresa", "id" }, null)
                }).ToArray(),
                Indexes = table.Indexes.Concat(new[]
                {
                    new SchemaIndexContract(
                        "UX_ActivosProveedores_Empresa_Id",
                        true,
                        false,
                        new[] { new SchemaIndexColumnContract("idEmpresa", false), new SchemaIndexColumnContract("id", false) },
                        Array.Empty<string>(),
                        null)
                }).ToArray()
            };

            return source with
            {
                ContractVersion = ProveedoresTargetVersion,
                Tables = source.Tables.Select(item => item.Name == table.Name ? updated : item).ToArray(),
                Sources = source.Sources.Concat(new[] { "OC-QA11B" }).ToArray(),
                PublishedAtUtc = new DateTime(2026, 10, 8, 0, 0, 0, DateTimeKind.Utc)
            };
        }

        public static SchemaContract CreateProductosServiciosV4Contract(ISchemaContractProvider provider)
        {
            SchemaContract source = provider.GetContract(DatabaseScopes.ProductosServicios, ProductosServiciosSourceVersion);
            SchemaTableContract table = source.Tables.Single(item => item.Name == "ProductosServicios");
            SchemaTableContract updated = table with
            {
                Columns = table.Columns.Concat(new[]
                {
                    new SchemaColumnContract("idProveedorPrincipal", "UNIQUEIDENTIFIER", null, null, null, true, null, false, false, null)
                }).ToArray(),
                ForeignKeys = table.ForeignKeys.Concat(new[]
                {
                    new SchemaForeignKeyContract(
                        "FK_ProductosServicios_ProveedorPrincipal_EmpresaId",
                        new[] { "idEmpresa", "idProveedorPrincipal" },
                        "dbo",
                        "ActivosProveedores",
                        new[] { "idEmpresa", "id" })
                }).ToArray(),
                Indexes = table.Indexes.Concat(new[]
                {
                    new SchemaIndexContract(
                        "IX_ProductosServicios_Empresa_ProveedorPrincipal_Activo_Tipo",
                        false,
                        false,
                        new[]
                        {
                            new SchemaIndexColumnContract("idEmpresa", false),
                            new SchemaIndexColumnContract("idProveedorPrincipal", false),
                            new SchemaIndexColumnContract("Activo", false),
                            new SchemaIndexColumnContract("Tipo", false)
                        },
                        Array.Empty<string>(),
                        null)
                }).ToArray()
            };

            return source with
            {
                ContractVersion = ProductosServiciosTargetVersion,
                Tables = source.Tables.Select(item => item.Name == table.Name ? updated : item).ToArray(),
                Sources = source.Sources.Concat(new[] { "OC-QA11B" }).ToArray(),
                PublishedAtUtc = new DateTime(2026, 10, 8, 0, 0, 0, DateTimeKind.Utc)
            };
        }

        public static string BuildProveedoresV1ToV2Sql() => @"
SET XACT_ABORT ON;

IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.ActivosProveedores') AND name = N'UX_ActivosProveedores_Empresa_Id')
BEGIN
    RETURN;
END;

CREATE UNIQUE NONCLUSTERED INDEX UX_ActivosProveedores_Empresa_Id
    ON dbo.ActivosProveedores (idEmpresa, id);";

        public static string BuildProductosServiciosV3ToV4Sql() => @"
SET XACT_ABORT ON;

DECLARE @ColumnExists bit = CASE WHEN COL_LENGTH('dbo.ProductosServicios', 'idProveedorPrincipal') IS NOT NULL THEN 1 ELSE 0 END;
DECLARE @FkExists bit = CASE WHEN OBJECT_ID(N'dbo.FK_ProductosServicios_ProveedorPrincipal_EmpresaId', N'F') IS NOT NULL THEN 1 ELSE 0 END;
DECLARE @IndexExists bit = CASE WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.ProductosServicios') AND name = N'IX_ProductosServicios_Empresa_ProveedorPrincipal_Activo_Tipo') THEN 1 ELSE 0 END;

IF @ColumnExists = 1 AND @FkExists = 1 AND @IndexExists = 1
BEGIN
    RETURN;
END;

IF @ColumnExists = 1 OR @FkExists = 1 OR @IndexExists = 1
BEGIN
    THROW 51000, 'PRODUCTOSSERVICIOS_V4_PARTIAL_TARGET_REQUIRES_REVIEW', 1;
END;

ALTER TABLE dbo.ProductosServicios ADD idProveedorPrincipal UNIQUEIDENTIFIER NULL;

EXEC(N'ALTER TABLE dbo.ProductosServicios WITH CHECK
    ADD CONSTRAINT FK_ProductosServicios_ProveedorPrincipal_EmpresaId
    FOREIGN KEY (idEmpresa, idProveedorPrincipal)
    REFERENCES dbo.ActivosProveedores (idEmpresa, id)
    ON DELETE NO ACTION;');

ALTER TABLE dbo.ProductosServicios CHECK CONSTRAINT FK_ProductosServicios_ProveedorPrincipal_EmpresaId;

EXEC(N'CREATE NONCLUSTERED INDEX IX_ProductosServicios_Empresa_ProveedorPrincipal_Activo_Tipo
    ON dbo.ProductosServicios (idEmpresa, idProveedorPrincipal, Activo, Tipo);');";
    }
}
