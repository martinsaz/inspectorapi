namespace checklistWs.Services.Tenant;

public static class KitsSchemaContractFactory
{
    public const string BaselineId = "KITS-B20261011";

    public static SchemaContract GetContract(int version)
    {
        if (version != ProductosServiciosSchemaContractProvider.V1)
        {
            throw new InvalidOperationException("SCHEMA_CONTRACT_VERSION_NOT_SUPPORTED");
        }

        return new SchemaContract(
            DatabaseScopes.Kits,
            version,
            "Kits",
            new[] { Kits(), Componentes(), Historial() },
            new[]
            {
                "KITS-PO01 modelo final aprobado",
                "KITS-01 schema y contratos V1"
            },
            new DateTime(2026, 10, 11, 0, 0, 0, DateTimeKind.Utc));
    }

    private static SchemaTableContract Kits() => new(
        "dbo",
        "Kits",
        new[]
        {
            GuidColumn("id", false, "(NEWID())"),
            GuidColumn("idEmpresa"),
            GuidColumn("identityKey", false, "(NEWID())"),
            TextColumn("Nombre", 150),
            TinyIntColumn("Tipo"),
            DecimalColumn("PrecioKit", 18, 2),
            TextColumn("ImagenUrl", 1000, true),
            TextColumn("ImagenNombre", 255, true),
            BitColumn("Activo", "((1))"),
            DateColumn("FechaCreacion", false, "(SYSUTCDATETIME())"),
            DateColumn("FechaActualizacion", false, "(SYSUTCDATETIME())"),
            DateColumn("FechaArchivado", true),
            GuidColumn("idUsuarioCreacion", true),
            GuidColumn("idUsuarioActualizacion", true),
            GuidColumn("idUsuarioArchivado", true),
        },
        new SchemaPrimaryKeyContract("PK_Kits", new[] { "id" }, true),
        Array.Empty<SchemaForeignKeyContract>(),
        new[]
        {
            new SchemaUniqueContract("UX_Kits_Empresa_Id", new[] { "idEmpresa", "id" }, null),
            new SchemaUniqueContract("UX_Kits_Empresa_IdentityKey", new[] { "idEmpresa", "identityKey" }, null),
            new SchemaUniqueContract("UX_Kits_Empresa_Nombre_Activo", new[] { "idEmpresa", "Nombre" }, "Activo = 1 AND FechaArchivado IS NULL"),
        },
        new[]
        {
            new SchemaCheckContract("CK_Kits_Tipo", "CHECK (Tipo IN (1, 2, 3))"),
            new SchemaCheckContract("CK_Kits_PrecioKit", "CHECK (PrecioKit >= 0)"),
            new SchemaCheckContract("CK_Kits_BajaLogica", "CHECK ((Activo = 1 AND FechaArchivado IS NULL AND idUsuarioArchivado IS NULL) OR (Activo = 0 AND FechaArchivado IS NOT NULL))"),
        },
        new[]
        {
            Unique("UX_Kits_Empresa_Id", "idEmpresa", "id"),
            Unique("UX_Kits_Empresa_IdentityKey", "idEmpresa", "identityKey"),
            UniqueFiltered("UX_Kits_Empresa_Nombre_Activo", "Activo = 1 AND FechaArchivado IS NULL", "idEmpresa", "Nombre"),
            Index("IX_Kits_Empresa_Activo_Tipo_Nombre", "idEmpresa", "Activo", "Tipo", "Nombre"),
        });

    private static SchemaTableContract Componentes() => new(
        "dbo",
        "KitsComponentes",
        new[]
        {
            GuidColumn("id", false, "(NEWID())"),
            GuidColumn("idEmpresa"),
            GuidColumn("identityKey", false, "(NEWID())"),
            GuidColumn("idKit"),
            TinyIntColumn("TipoIdentidad"),
            GuidColumn("idProductoServicio"),
            GuidColumn("idVariante", true),
            GuidColumn("idPresentacionVenta", true),
            DecimalColumn("Cantidad", 18, 4, false, "((1))"),
            DecimalColumn("PrecioUnitario", 18, 2),
            BitColumn("Activo", "((1))"),
            DateColumn("FechaCreacion", false, "(SYSUTCDATETIME())"),
            DateColumn("FechaActualizacion", false, "(SYSUTCDATETIME())"),
            DateColumn("FechaArchivado", true),
            GuidColumn("idUsuarioCreacion", true),
            GuidColumn("idUsuarioActualizacion", true),
            GuidColumn("idUsuarioArchivado", true),
        },
        new SchemaPrimaryKeyContract("PK_KitsComponentes", new[] { "id" }, true),
        new[]
        {
            new SchemaForeignKeyContract("FK_KitsComponentes_Kits_EmpresaId", new[] { "idEmpresa", "idKit" }, "dbo", "Kits", new[] { "idEmpresa", "id" }),
            new SchemaForeignKeyContract("FK_KitsComponentes_ProductosServicios_EmpresaId", new[] { "idEmpresa", "idProductoServicio" }, "dbo", "ProductosServicios", new[] { "idEmpresa", "id" }),
            new SchemaForeignKeyContract("FK_KitsComponentes_Variantes_EmpresaId", new[] { "idEmpresa", "idVariante" }, "dbo", "ProductosServiciosVariantes", new[] { "idEmpresa", "id" }),
            new SchemaForeignKeyContract("FK_KitsComponentes_PresentacionesVenta_EmpresaId", new[] { "idEmpresa", "idPresentacionVenta" }, "dbo", "ProductosServiciosPresentacionesVenta", new[] { "idEmpresa", "id" }),
        },
        new[]
        {
            new SchemaUniqueContract("UX_KitsComponentes_Empresa_Id", new[] { "idEmpresa", "id" }, null),
            new SchemaUniqueContract("UX_KitsComponentes_Empresa_IdentityKey", new[] { "idEmpresa", "identityKey" }, null),
        },
        new[]
        {
            new SchemaCheckContract("CK_KitsComponentes_TipoIdentidad", "CHECK (TipoIdentidad IN (1, 2, 3, 4))"),
            new SchemaCheckContract("CK_KitsComponentes_Identidad", "CHECK ((TipoIdentidad IN (1, 2) AND idVariante IS NULL AND idPresentacionVenta IS NULL) OR (TipoIdentidad = 3 AND idVariante IS NOT NULL AND idPresentacionVenta IS NULL) OR (TipoIdentidad = 4 AND idVariante IS NULL AND idPresentacionVenta IS NOT NULL))"),
            new SchemaCheckContract("CK_KitsComponentes_Cantidad", "CHECK (Cantidad > 0)"),
            new SchemaCheckContract("CK_KitsComponentes_PrecioUnitario", "CHECK (PrecioUnitario >= 0)"),
            new SchemaCheckContract("CK_KitsComponentes_BajaLogica", "CHECK ((Activo = 1 AND FechaArchivado IS NULL AND idUsuarioArchivado IS NULL) OR (Activo = 0 AND FechaArchivado IS NOT NULL))"),
        },
        new[]
        {
            Unique("UX_KitsComponentes_Empresa_Id", "idEmpresa", "id"),
            Unique("UX_KitsComponentes_Empresa_IdentityKey", "idEmpresa", "identityKey"),
            UniqueFiltered("UX_KitsComponentes_Base_Activo", "Activo = 1 AND FechaArchivado IS NULL AND TipoIdentidad IN (1, 2) AND idVariante IS NULL AND idPresentacionVenta IS NULL", "idEmpresa", "idKit", "idProductoServicio"),
            UniqueFiltered("UX_KitsComponentes_Variante_Activo", "Activo = 1 AND FechaArchivado IS NULL AND TipoIdentidad = 3 AND idVariante IS NOT NULL", "idEmpresa", "idKit", "idProductoServicio", "idVariante"),
            UniqueFiltered("UX_KitsComponentes_Presentacion_Activo", "Activo = 1 AND FechaArchivado IS NULL AND TipoIdentidad = 4 AND idPresentacionVenta IS NOT NULL", "idEmpresa", "idKit", "idProductoServicio", "idPresentacionVenta"),
            Index("IX_KitsComponentes_Empresa_Kit_Activo", "idEmpresa", "idKit", "Activo"),
        });

    private static SchemaTableContract Historial() => new(
        "dbo",
        "KitsHistorial",
        new[]
        {
            GuidColumn("id", false, "(NEWID())"),
            GuidColumn("idEmpresa"),
            GuidColumn("idKit"),
            GuidColumn("idKitComponente", true),
            TinyIntColumn("TipoEntidad"),
            TextColumn("Operacion", 40),
            MaxTextColumn("DatosAntes", true),
            MaxTextColumn("DatosDespues", true),
            GuidColumn("idUsuario", true),
            GuidColumn("CorrelationId"),
            DateColumn("FechaUtc", false, "(SYSUTCDATETIME())"),
        },
        new SchemaPrimaryKeyContract("PK_KitsHistorial", new[] { "id" }, true),
        new[]
        {
            new SchemaForeignKeyContract("FK_KitsHistorial_Kits_EmpresaId", new[] { "idEmpresa", "idKit" }, "dbo", "Kits", new[] { "idEmpresa", "id" }),
            new SchemaForeignKeyContract("FK_KitsHistorial_Componentes_EmpresaId", new[] { "idEmpresa", "idKitComponente" }, "dbo", "KitsComponentes", new[] { "idEmpresa", "id" }),
        },
        Array.Empty<SchemaUniqueContract>(),
        new[]
        {
            new SchemaCheckContract("CK_KitsHistorial_TipoEntidad", "CHECK ((TipoEntidad = 1 AND idKitComponente IS NULL) OR (TipoEntidad = 2 AND idKitComponente IS NOT NULL))"),
        },
        new[]
        {
            IndexDescendingLast("IX_KitsHistorial_Empresa_Kit_Fecha", "idEmpresa", "idKit", "FechaUtc"),
            Index("IX_KitsHistorial_Empresa_CorrelationId", "idEmpresa", "CorrelationId"),
        });

    private static SchemaColumnContract GuidColumn(string name, bool nullable = false, string? defaultDefinition = null) => new(name, "UNIQUEIDENTIFIER", null, null, null, nullable, defaultDefinition, false, false, null);
    private static SchemaColumnContract TextColumn(string name, int length, bool nullable = false) => new(name, $"NVARCHAR({length})", length, null, null, nullable, null, false, false, null);
    private static SchemaColumnContract MaxTextColumn(string name, bool nullable) => new(name, "NVARCHAR(MAX)", -1, null, null, nullable, null, false, false, null);
    private static SchemaColumnContract TinyIntColumn(string name) => new(name, "TINYINT", null, null, null, false, null, false, false, null);
    private static SchemaColumnContract BitColumn(string name, string defaultDefinition) => new(name, "BIT", null, null, null, false, defaultDefinition, false, false, null);
    private static SchemaColumnContract DecimalColumn(string name, byte precision, int scale, bool nullable = false, string? defaultDefinition = null) => new(name, $"DECIMAL({precision},{scale})", null, precision, scale, nullable, defaultDefinition, false, false, null);
    private static SchemaColumnContract DateColumn(string name, bool nullable, string? defaultDefinition = null) => new(name, "DATETIME2(0)", null, null, 0, nullable, defaultDefinition, false, false, null);
    private static SchemaIndexContract Unique(string name, params string[] columns) => new(name, true, false, columns.Select(column => new SchemaIndexColumnContract(column, false)).ToArray(), Array.Empty<string>(), null);
    private static SchemaIndexContract UniqueFiltered(string name, string filter, params string[] columns) => new(name, true, false, columns.Select(column => new SchemaIndexColumnContract(column, false)).ToArray(), Array.Empty<string>(), filter);
    private static SchemaIndexContract Index(string name, params string[] columns) => new(name, false, false, columns.Select(column => new SchemaIndexColumnContract(column, false)).ToArray(), Array.Empty<string>(), null);
    private static SchemaIndexContract IndexDescendingLast(string name, string first, string second, string last) => new(name, false, false, new[] { new SchemaIndexColumnContract(first, false), new SchemaIndexColumnContract(second, false), new SchemaIndexColumnContract(last, true) }, Array.Empty<string>(), null);
}
