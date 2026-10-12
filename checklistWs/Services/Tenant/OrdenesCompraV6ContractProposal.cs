namespace checklistWs.Services.Tenant
{
    public static class OrdenesCompraV6ContractProposal
    {
        public const int SourceVersion = ProductosServiciosSchemaContractProvider.V5;
        public const int TargetVersion = 6;
        public const string MigrationId = "OC-M20261009-V5-V6-CIERRE-IDEMPOTENTE";

        public static SchemaContract CreateTargetContract(SchemaContract source)
        {
            ArgumentNullException.ThrowIfNull(source);
            if (!string.Equals(source.Scope, DatabaseScopes.OrdenesCompra, StringComparison.OrdinalIgnoreCase) || source.ContractVersion != SourceVersion)
            {
                throw new InvalidOperationException("OC_V6_SOURCE_CONTRACT_MUST_BE_V5");
            }

            return new SchemaContract(
                DatabaseScopes.OrdenesCompra,
                TargetVersion,
                source.Product,
                source.Tables.Concat(new[] { OperacionesCierre(), Reemplazos() }).ToArray(),
                source.Sources.Concat(new[]
                {
                    "inspectorapi/checklistWs/Services/Tenant/OrdenesCompraV6ContractProposal.cs",
                    "inspectorapi/checklistWs/Controllers/OrdenesCompra/OrdenesCompraController.cs",
                }).ToArray(),
                new DateTime(2026, 10, 9, 0, 0, 0, DateTimeKind.Utc));
        }

        public static SchemaMigrationDefinition CreateMigration(ISchemaContractProvider contracts, ISchemaManifestProvider manifests)
        {
            SchemaContract source = contracts.GetContract(DatabaseScopes.OrdenesCompra, SourceVersion);
            SchemaContract target = CreateTargetContract(source);
            string sql = BuildV5ToV6Sql();
            return new SchemaMigrationDefinition(
                MigrationId,
                "OC-B20260921",
                DatabaseScopes.OrdenesCompra,
                SourceVersion,
                TargetVersion,
                6,
                new[] { "dbo.OrdenesCompraOperacionesCierre", "dbo.OrdenesCompraReemplazos" },
                new[]
                {
                    "SELECT CASE WHEN OBJECT_ID(N'dbo.OrdenesCompra',N'U') IS NOT NULL AND OBJECT_ID(N'dbo.OrdenesCompraDetalle',N'U') IS NOT NULL AND OBJECT_ID(N'dbo.OrdenesCompraSucursales',N'U') IS NOT NULL THEN 1 ELSE 0 END;",
                    "SELECT CASE WHEN OBJECT_ID(N'dbo.OrdenesCompraOperacionesCierre',N'U') IS NULL AND OBJECT_ID(N'dbo.OrdenesCompraReemplazos',N'U') IS NULL THEN 1 ELSE 0 END;",
                },
                new[] { "NO_BUSINESS_ROW_BACKFILL", "NO_ORDER_MUTATION", "NO_HARD_DELETE" },
                SchemaMigrationHash.Sha256(sql),
                manifests.CreateManifest(target).ManifestHash,
                "SingleTransaction",
                "Low",
                true,
                TimeSpan.FromMinutes(2),
                new[] { manifests.CreateManifest(source).ManifestHash },
                "ReconcileAfterUncertainCommit",
                sql,
                target,
                source);
        }

        public static string BuildV5ToV6Sql() => @"
SET XACT_ABORT ON;
SET NOCOUNT ON;

DECLARE @Complete bit = CASE WHEN
    OBJECT_ID(N'dbo.OrdenesCompraOperacionesCierre',N'U') IS NOT NULL
    AND OBJECT_ID(N'dbo.OrdenesCompraReemplazos',N'U') IS NOT NULL
    AND OBJECT_ID(N'dbo.UX_OCCierre_Empresa_IdempotencyKey',N'UQ') IS NOT NULL
    AND OBJECT_ID(N'dbo.UX_OCReemplazos_Empresa_Anterior',N'UQ') IS NOT NULL
    THEN 1 ELSE 0 END;
IF @Complete=1 RETURN;

IF OBJECT_ID(N'dbo.OrdenesCompraOperacionesCierre',N'U') IS NOT NULL
   OR OBJECT_ID(N'dbo.OrdenesCompraReemplazos',N'U') IS NOT NULL
    THROW 51070, 'OC_V6_PARTIAL_OR_DRIFTED_TARGET_REJECTED', 1;

CREATE TABLE dbo.OrdenesCompraOperacionesCierre
(
    id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_OCCierre_id DEFAULT (NEWID()),
    idEmpresa UNIQUEIDENTIFIER NOT NULL,
    Token UNIQUEIDENTIFIER NOT NULL,
    IdempotencyKey NVARCHAR(120) NOT NULL,
    PayloadHash CHAR(64) NOT NULL,
    PropuestaJson NVARCHAR(MAX) NOT NULL,
    ValidacionJson NVARCHAR(MAX) NOT NULL,
    Estado TINYINT NOT NULL CONSTRAINT DF_OCCierre_Estado DEFAULT ((1)),
    FechaExpiracion DATETIME2(0) NOT NULL,
    FechaConsumo DATETIME2(0) NULL,
    idOrdenCompraResultado UNIQUEIDENTIFIER NULL,
    ResultadoJson NVARCHAR(MAX) NULL,
    idUsuario UNIQUEIDENTIFIER NULL,
    FechaCreacion DATETIME2(0) NOT NULL CONSTRAINT DF_OCCierre_FechaCreacion DEFAULT (SYSUTCDATETIME()),
    FechaActualizacion DATETIME2(0) NOT NULL CONSTRAINT DF_OCCierre_FechaActualizacion DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_OrdenesCompraOperacionesCierre PRIMARY KEY CLUSTERED (id),
    CONSTRAINT UX_OCCierre_Empresa_Id UNIQUE (idEmpresa,id),
    CONSTRAINT UX_OCCierre_Empresa_Token UNIQUE (idEmpresa,Token),
    CONSTRAINT UX_OCCierre_Empresa_IdempotencyKey UNIQUE (idEmpresa,IdempotencyKey),
    CONSTRAINT FK_OCCierre_Resultado_EmpresaId FOREIGN KEY (idEmpresa,idOrdenCompraResultado) REFERENCES dbo.OrdenesCompra(idEmpresa,id),
    CONSTRAINT CK_OCCierre_Estado CHECK (Estado IN (1,2,3)),
    CONSTRAINT CK_OCCierre_Consumo CHECK ((Estado=1 AND FechaConsumo IS NULL AND idOrdenCompraResultado IS NULL AND ResultadoJson IS NULL) OR (Estado=2 AND FechaConsumo IS NOT NULL AND idOrdenCompraResultado IS NOT NULL AND ResultadoJson IS NOT NULL) OR (Estado=3 AND FechaConsumo IS NOT NULL AND idOrdenCompraResultado IS NULL))
);
CREATE INDEX IX_OCCierre_Empresa_Expiracion ON dbo.OrdenesCompraOperacionesCierre(idEmpresa,Estado,FechaExpiracion);

CREATE TABLE dbo.OrdenesCompraReemplazos
(
    id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_OCReemplazos_id DEFAULT (NEWID()),
    idEmpresa UNIQUEIDENTIFIER NOT NULL,
    idOperacionCierre UNIQUEIDENTIFIER NOT NULL,
    idOrdenCompraAnterior UNIQUEIDENTIFIER NOT NULL,
    idOrdenCompraNueva UNIQUEIDENTIFIER NOT NULL,
    Motivo NVARCHAR(250) NOT NULL,
    idUsuario UNIQUEIDENTIFIER NULL,
    FechaReemplazo DATETIME2(0) NOT NULL CONSTRAINT DF_OCReemplazos_Fecha DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_OrdenesCompraReemplazos PRIMARY KEY CLUSTERED (id),
    CONSTRAINT UX_OCReemplazos_Empresa_Id UNIQUE (idEmpresa,id),
    CONSTRAINT UX_OCReemplazos_Empresa_Anterior UNIQUE (idEmpresa,idOrdenCompraAnterior),
    CONSTRAINT FK_OCReemplazos_Operacion_EmpresaId FOREIGN KEY (idEmpresa,idOperacionCierre) REFERENCES dbo.OrdenesCompraOperacionesCierre(idEmpresa,id),
    CONSTRAINT FK_OCReemplazos_Anterior_EmpresaId FOREIGN KEY (idEmpresa,idOrdenCompraAnterior) REFERENCES dbo.OrdenesCompra(idEmpresa,id),
    CONSTRAINT FK_OCReemplazos_Nueva_EmpresaId FOREIGN KEY (idEmpresa,idOrdenCompraNueva) REFERENCES dbo.OrdenesCompra(idEmpresa,id),
    CONSTRAINT CK_OCReemplazos_Distintas CHECK (idOrdenCompraAnterior<>idOrdenCompraNueva),
    CONSTRAINT CK_OCReemplazos_Motivo CHECK (NULLIF(LTRIM(RTRIM(Motivo)),N'') IS NOT NULL)
);
CREATE INDEX IX_OCReemplazos_Empresa_Nueva ON dbo.OrdenesCompraReemplazos(idEmpresa,idOrdenCompraNueva);
";

        private static SchemaTableContract OperacionesCierre() => new(
            "dbo", "OrdenesCompraOperacionesCierre",
            new[]
            {
                Col("id","UNIQUEIDENTIFIER",false,def:"(NEWID())"), Col("idEmpresa","UNIQUEIDENTIFIER",false),
                Col("Token","UNIQUEIDENTIFIER",false), Col("IdempotencyKey","NVARCHAR(120)",false,max:120),
                Col("PayloadHash","CHAR(64)",false,max:64), Col("PropuestaJson","NVARCHAR(MAX)",false),
                Col("ValidacionJson","NVARCHAR(MAX)",false), Col("Estado","TINYINT",false,def:"((1))"),
                Col("FechaExpiracion","DATETIME2(0)",false,scale:0), Col("FechaConsumo","DATETIME2(0)",true,scale:0),
                Col("idOrdenCompraResultado","UNIQUEIDENTIFIER",true), Col("ResultadoJson","NVARCHAR(MAX)",true),
                Col("idUsuario","UNIQUEIDENTIFIER",true), Col("FechaCreacion","DATETIME2(0)",false,scale:0,def:"(SYSUTCDATETIME())"),
                Col("FechaActualizacion","DATETIME2(0)",false,scale:0,def:"(SYSUTCDATETIME())"),
            },
            new SchemaPrimaryKeyContract("PK_OrdenesCompraOperacionesCierre",new[]{"id"},true),
            new[]{new SchemaForeignKeyContract("FK_OCCierre_Resultado_EmpresaId",new[]{"idEmpresa","idOrdenCompraResultado"},"dbo","OrdenesCompra",new[]{"idEmpresa","id"})},
            new[]{new SchemaUniqueContract("UX_OCCierre_Empresa_Id",new[]{"idEmpresa","id"},null),new SchemaUniqueContract("UX_OCCierre_Empresa_Token",new[]{"idEmpresa","Token"},null),new SchemaUniqueContract("UX_OCCierre_Empresa_IdempotencyKey",new[]{"idEmpresa","IdempotencyKey"},null)},
            new[]{new SchemaCheckContract("CK_OCCierre_Estado","CHECK (Estado IN (1, 2, 3))"),new SchemaCheckContract("CK_OCCierre_Consumo","CHECK ((Estado = 1 AND FechaConsumo IS NULL AND idOrdenCompraResultado IS NULL AND ResultadoJson IS NULL) OR (Estado = 2 AND FechaConsumo IS NOT NULL AND idOrdenCompraResultado IS NOT NULL AND ResultadoJson IS NOT NULL) OR (Estado = 3 AND FechaConsumo IS NOT NULL AND idOrdenCompraResultado IS NULL))")},
            new[]{Ix("IX_OCCierre_Empresa_Expiracion",false,"idEmpresa","Estado","FechaExpiracion")});

        private static SchemaTableContract Reemplazos() => new(
            "dbo", "OrdenesCompraReemplazos",
            new[]{Col("id","UNIQUEIDENTIFIER",false,def:"(NEWID())"),Col("idEmpresa","UNIQUEIDENTIFIER",false),Col("idOperacionCierre","UNIQUEIDENTIFIER",false),Col("idOrdenCompraAnterior","UNIQUEIDENTIFIER",false),Col("idOrdenCompraNueva","UNIQUEIDENTIFIER",false),Col("Motivo","NVARCHAR(250)",false,max:250),Col("idUsuario","UNIQUEIDENTIFIER",true),Col("FechaReemplazo","DATETIME2(0)",false,scale:0,def:"(SYSUTCDATETIME())")},
            new SchemaPrimaryKeyContract("PK_OrdenesCompraReemplazos",new[]{"id"},true),
            new[]{new SchemaForeignKeyContract("FK_OCReemplazos_Operacion_EmpresaId",new[]{"idEmpresa","idOperacionCierre"},"dbo","OrdenesCompraOperacionesCierre",new[]{"idEmpresa","id"}),new SchemaForeignKeyContract("FK_OCReemplazos_Anterior_EmpresaId",new[]{"idEmpresa","idOrdenCompraAnterior"},"dbo","OrdenesCompra",new[]{"idEmpresa","id"}),new SchemaForeignKeyContract("FK_OCReemplazos_Nueva_EmpresaId",new[]{"idEmpresa","idOrdenCompraNueva"},"dbo","OrdenesCompra",new[]{"idEmpresa","id"})},
            new[]{new SchemaUniqueContract("UX_OCReemplazos_Empresa_Id",new[]{"idEmpresa","id"},null),new SchemaUniqueContract("UX_OCReemplazos_Empresa_Anterior",new[]{"idEmpresa","idOrdenCompraAnterior"},null)},
            new[]{new SchemaCheckContract("CK_OCReemplazos_Distintas","CHECK (idOrdenCompraAnterior <> idOrdenCompraNueva)"),new SchemaCheckContract("CK_OCReemplazos_Motivo","CHECK (NULLIF(LTRIM(RTRIM(Motivo)), N'') IS NOT NULL)")},
            new[]{Ix("IX_OCReemplazos_Empresa_Nueva",false,"idEmpresa","idOrdenCompraNueva")});

        private static SchemaColumnContract Col(string name,string type,bool nullable,int? max=null,byte? precision=null,int? scale=null,string? def=null)
            => new(name,type,max,precision,scale,nullable,def,false,false,null);
        private static SchemaIndexContract Ix(string name,bool unique,params string[] columns)
            => new(name,unique,false,columns.Select(x=>new SchemaIndexColumnContract(x,false)).ToArray(),Array.Empty<string>(),null);
    }
}
