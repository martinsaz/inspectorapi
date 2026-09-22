/*
    MODULO: Ordenes de compra
    FASE: Modelo de datos
    SCRIPT: UP
    ADVERTENCIA:
    - No ejecutar automaticamente.
    - No inserta datos semilla.
    - No altera tablas existentes fuera del modulo.
    - No crea relaciones fisicas hacia catalogos externos no controlados por este script.
*/

SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.OrdenesCompraFolios', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.OrdenesCompraFolios
        (
            id UNIQUEIDENTIFIER NOT NULL
                CONSTRAINT PK_OrdenesCompraFolios PRIMARY KEY CLUSTERED
                CONSTRAINT DF_OrdenesCompraFolios_id DEFAULT (NEWID()),
            idEmpresa UNIQUEIDENTIFIER NOT NULL,
            identityKey UNIQUEIDENTIFIER NOT NULL
                CONSTRAINT DF_OrdenesCompraFolios_identityKey DEFAULT (NEWID()),
            UltimoConsecutivo BIGINT NOT NULL
                CONSTRAINT DF_OrdenesCompraFolios_UltimoConsecutivo DEFAULT ((0)),
            FechaCreacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_OrdenesCompraFolios_FechaCreacion DEFAULT (SYSUTCDATETIME()),
            FechaActualizacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_OrdenesCompraFolios_FechaActualizacion DEFAULT (SYSUTCDATETIME()),
            CONSTRAINT CK_OrdenesCompraFolios_UltimoConsecutivo
                CHECK (UltimoConsecutivo >= 0)
        );
    END;

    IF OBJECT_ID(N'dbo.OrdenesCompra', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.OrdenesCompra
        (
            id UNIQUEIDENTIFIER NOT NULL
                CONSTRAINT PK_OrdenesCompra PRIMARY KEY CLUSTERED
                CONSTRAINT DF_OrdenesCompra_id DEFAULT (NEWID()),
            idEmpresa UNIQUEIDENTIFIER NOT NULL,
            identityKey UNIQUEIDENTIFIER NOT NULL
                CONSTRAINT DF_OrdenesCompra_identityKey DEFAULT (NEWID()),
            Folio NVARCHAR(30) NULL,
            idRazonSocial UNIQUEIDENTIFIER NOT NULL,
            idSucursal UNIQUEIDENTIFIER NOT NULL,
            idProveedor UNIQUEIDENTIFIER NOT NULL,
            FechaOrden DATETIME2(0) NOT NULL,
            FechaLlegada DATETIME2(0) NULL,
            Estado TINYINT NOT NULL
                CONSTRAINT DF_OrdenesCompra_Estado DEFAULT ((1)),
            Subtotal DECIMAL(18, 2) NOT NULL
                CONSTRAINT DF_OrdenesCompra_Subtotal DEFAULT ((0)),
            Total DECIMAL(18, 2) NOT NULL
                CONSTRAINT DF_OrdenesCompra_Total DEFAULT ((0)),
            Observaciones NVARCHAR(1000) NULL,
            MotivoCancelacion NVARCHAR(500) NULL,
            FechaCancelacion DATETIME2(0) NULL,
            Activo BIT NOT NULL
                CONSTRAINT DF_OrdenesCompra_Activo DEFAULT ((1)),
            FechaCreacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_OrdenesCompra_FechaCreacion DEFAULT (SYSUTCDATETIME()),
            FechaActualizacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_OrdenesCompra_FechaActualizacion DEFAULT (SYSUTCDATETIME()),
            FechaArchivado DATETIME2(0) NULL,
            idUsuarioCreacion UNIQUEIDENTIFIER NULL,
            idUsuarioActualizacion UNIQUEIDENTIFIER NULL,
            idUsuarioCancelacion UNIQUEIDENTIFIER NULL,
            CONSTRAINT CK_OrdenesCompra_Estado
                CHECK (Estado IN (1, 2, 3)),
            CONSTRAINT CK_OrdenesCompra_ImportesNoNegativos
                CHECK (Subtotal >= 0 AND Total >= 0),
            CONSTRAINT CK_OrdenesCompra_TotalIgualSubtotal
                CHECK (Subtotal = Total),
            CONSTRAINT CK_OrdenesCompra_TotalPorEstado
                CHECK (
                    (Estado = 2 AND Total > 0)
                    OR (Estado IN (1, 3) AND Total >= 0)
                ),
            CONSTRAINT CK_OrdenesCompra_FechaLlegada
                CHECK (FechaLlegada IS NULL OR FechaLlegada >= FechaOrden),
            CONSTRAINT CK_OrdenesCompra_Cancelacion
                CHECK (
                    (Estado = 3
                        AND FechaCancelacion IS NOT NULL
                        AND NULLIF(LTRIM(RTRIM(MotivoCancelacion)), N'') IS NOT NULL)
                    OR
                    (Estado IN (1, 2)
                        AND FechaCancelacion IS NULL
                        AND MotivoCancelacion IS NULL
                        AND idUsuarioCancelacion IS NULL)
                ),
            CONSTRAINT CK_OrdenesCompra_FolioGenerada
                CHECK (
                    (Estado = 2 AND NULLIF(LTRIM(RTRIM(Folio)), N'') IS NOT NULL)
                    OR (Estado IN (1, 3))
                ),
            CONSTRAINT CK_OrdenesCompra_Archivado
                CHECK (
                    (Activo = 1 AND FechaArchivado IS NULL)
                    OR (Activo = 0 AND FechaArchivado IS NOT NULL)
                )
        );
    END;

    IF OBJECT_ID(N'dbo.OrdenesCompraDetalle', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.OrdenesCompraDetalle
        (
            id UNIQUEIDENTIFIER NOT NULL
                CONSTRAINT PK_OrdenesCompraDetalle PRIMARY KEY CLUSTERED
                CONSTRAINT DF_OrdenesCompraDetalle_id DEFAULT (NEWID()),
            idEmpresa UNIQUEIDENTIFIER NOT NULL,
            identityKey UNIQUEIDENTIFIER NOT NULL
                CONSTRAINT DF_OrdenesCompraDetalle_identityKey DEFAULT (NEWID()),
            idOrdenCompra UNIQUEIDENTIFIER NOT NULL,
            NumeroPartida INT NOT NULL,
            idProductoServicio UNIQUEIDENTIFIER NOT NULL,
            TipoProductoServicio TINYINT NOT NULL,
            Codigo NVARCHAR(50) NOT NULL,
            Nombre NVARCHAR(150) NOT NULL,
            Descripcion NVARCHAR(1000) NULL,
            idUnidadMedida UNIQUEIDENTIFIER NOT NULL,
            UnidadMedida NVARCHAR(100) NOT NULL,
            UnidadAbreviatura NVARCHAR(20) NOT NULL,
            Cantidad DECIMAL(18, 4) NOT NULL,
            CostoUnitario DECIMAL(18, 2) NOT NULL
                CONSTRAINT DF_OrdenesCompraDetalle_CostoUnitario DEFAULT ((0)),
            Subtotal DECIMAL(18, 2) NOT NULL
                CONSTRAINT DF_OrdenesCompraDetalle_Subtotal DEFAULT ((0)),
            Total DECIMAL(18, 2) NOT NULL
                CONSTRAINT DF_OrdenesCompraDetalle_Total DEFAULT ((0)),
            Activo BIT NOT NULL
                CONSTRAINT DF_OrdenesCompraDetalle_Activo DEFAULT ((1)),
            FechaCreacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_OrdenesCompraDetalle_FechaCreacion DEFAULT (SYSUTCDATETIME()),
            FechaActualizacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_OrdenesCompraDetalle_FechaActualizacion DEFAULT (SYSUTCDATETIME()),
            FechaArchivado DATETIME2(0) NULL,
            CONSTRAINT CK_OrdenesCompraDetalle_NumeroPartida
                CHECK (NumeroPartida > 0),
            CONSTRAINT CK_OrdenesCompraDetalle_TipoProductoServicio
                CHECK (TipoProductoServicio IN (1, 2)),
            CONSTRAINT CK_OrdenesCompraDetalle_Cantidad
                CHECK (Cantidad > 0),
            CONSTRAINT CK_OrdenesCompraDetalle_ImportesNoNegativos
                CHECK (CostoUnitario >= 0 AND Subtotal >= 0 AND Total >= 0),
            CONSTRAINT CK_OrdenesCompraDetalle_Calculo
                CHECK (
                    Subtotal = ROUND(Cantidad * CostoUnitario, 2)
                    AND Total = Subtotal
                ),
            CONSTRAINT CK_OrdenesCompraDetalle_Archivado
                CHECK (
                    (Activo = 1 AND FechaArchivado IS NULL)
                    OR (Activo = 0 AND FechaArchivado IS NOT NULL)
                )
        );
    END;

    IF OBJECT_ID(N'dbo.OrdenesCompraFolios', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.OrdenesCompraFolios')
              AND name = N'UX_OrdenesCompraFolios_Empresa_Id'
       )
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UX_OrdenesCompraFolios_Empresa_Id
            ON dbo.OrdenesCompraFolios (idEmpresa, id);
    END;

    IF OBJECT_ID(N'dbo.OrdenesCompraFolios', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.OrdenesCompraFolios')
              AND name = N'UX_OrdenesCompraFolios_Empresa'
       )
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UX_OrdenesCompraFolios_Empresa
            ON dbo.OrdenesCompraFolios (idEmpresa);
    END;

    IF OBJECT_ID(N'dbo.OrdenesCompra', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.OrdenesCompra')
              AND name = N'UX_OrdenesCompra_Empresa_Id'
       )
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UX_OrdenesCompra_Empresa_Id
            ON dbo.OrdenesCompra (idEmpresa, id);
    END;

    IF OBJECT_ID(N'dbo.OrdenesCompra', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.OrdenesCompra')
              AND name = N'UX_OrdenesCompra_Empresa_Folio'
       )
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UX_OrdenesCompra_Empresa_Folio
            ON dbo.OrdenesCompra (idEmpresa, Folio)
            WHERE Folio IS NOT NULL;
    END;

    IF OBJECT_ID(N'dbo.OrdenesCompra', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.OrdenesCompra')
              AND name = N'IX_OrdenesCompra_Empresa_Estado_FechaOrden'
       )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_OrdenesCompra_Empresa_Estado_FechaOrden
            ON dbo.OrdenesCompra (idEmpresa, Estado, FechaOrden DESC);
    END;

    IF OBJECT_ID(N'dbo.OrdenesCompra', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.OrdenesCompra')
              AND name = N'IX_OrdenesCompra_Empresa_Proveedor'
       )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_OrdenesCompra_Empresa_Proveedor
            ON dbo.OrdenesCompra (idEmpresa, idProveedor, FechaOrden DESC);
    END;

    IF OBJECT_ID(N'dbo.OrdenesCompra', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.OrdenesCompra')
              AND name = N'IX_OrdenesCompra_Empresa_Sucursal'
       )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_OrdenesCompra_Empresa_Sucursal
            ON dbo.OrdenesCompra (idEmpresa, idSucursal, FechaOrden DESC);
    END;

    IF OBJECT_ID(N'dbo.OrdenesCompra', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.OrdenesCompra')
              AND name = N'IX_OrdenesCompra_Empresa_RazonSocial'
       )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_OrdenesCompra_Empresa_RazonSocial
            ON dbo.OrdenesCompra (idEmpresa, idRazonSocial, FechaOrden DESC);
    END;

    IF OBJECT_ID(N'dbo.OrdenesCompraDetalle', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.OrdenesCompraDetalle')
              AND name = N'UX_OrdenesCompraDetalle_Empresa_Id'
       )
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UX_OrdenesCompraDetalle_Empresa_Id
            ON dbo.OrdenesCompraDetalle (idEmpresa, id);
    END;

    IF OBJECT_ID(N'dbo.OrdenesCompraDetalle', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.OrdenesCompraDetalle')
              AND name = N'IX_OrdenesCompraDetalle_Empresa_Orden'
       )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_OrdenesCompraDetalle_Empresa_Orden
            ON dbo.OrdenesCompraDetalle (idEmpresa, idOrdenCompra);
    END;

    IF OBJECT_ID(N'dbo.OrdenesCompraDetalle', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.OrdenesCompraDetalle')
              AND name = N'UX_OrdenesCompraDetalle_Empresa_Orden_NumeroPartida'
       )
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UX_OrdenesCompraDetalle_Empresa_Orden_NumeroPartida
            ON dbo.OrdenesCompraDetalle (idEmpresa, idOrdenCompra, NumeroPartida)
            WHERE Activo = 1 AND FechaArchivado IS NULL;
    END;

    IF OBJECT_ID(N'dbo.OrdenesCompraDetalle', N'U') IS NOT NULL
       AND EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.OrdenesCompraDetalle')
              AND name = N'UX_OrdenesCompraDetalle_Empresa_Orden_ProductoServicio_Activo'
       )
    BEGIN
        DROP INDEX UX_OrdenesCompraDetalle_Empresa_Orden_ProductoServicio_Activo
            ON dbo.OrdenesCompraDetalle;
    END;

    IF OBJECT_ID(N'dbo.OrdenesCompraDetalle', N'U') IS NOT NULL
       AND OBJECT_ID(N'dbo.OrdenesCompra', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.foreign_keys
            WHERE name = N'FK_OrdenesCompraDetalle_OrdenesCompra_EmpresaId'
       )
    BEGIN
        ALTER TABLE dbo.OrdenesCompraDetalle WITH CHECK
        ADD CONSTRAINT FK_OrdenesCompraDetalle_OrdenesCompra_EmpresaId
            FOREIGN KEY (idEmpresa, idOrdenCompra)
            REFERENCES dbo.OrdenesCompra (idEmpresa, id);
    END;

    IF OBJECT_ID(N'dbo.OrdenesCompraPresentacionesCompra', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.OrdenesCompraPresentacionesCompra
        (
            id UNIQUEIDENTIFIER NOT NULL
                CONSTRAINT PK_OrdenesCompraPresentacionesCompra PRIMARY KEY CLUSTERED
                CONSTRAINT DF_OrdenesCompraPresentacionesCompra_id DEFAULT (NEWID()),
            idEmpresa UNIQUEIDENTIFIER NOT NULL,
            identityKey UNIQUEIDENTIFIER NOT NULL
                CONSTRAINT DF_OrdenesCompraPresentacionesCompra_identityKey DEFAULT (NEWID()),
            idProductoServicio UNIQUEIDENTIFIER NOT NULL,
            idVariante UNIQUEIDENTIFIER NULL,
            Nombre NVARCHAR(150) NOT NULL,
            idUnidadCompra UNIQUEIDENTIFIER NOT NULL,
            UnidadCompra NVARCHAR(100) NOT NULL,
            UnidadCompraAbreviatura NVARCHAR(20) NOT NULL,
            FactorConversionBase DECIMAL(28, 12) NOT NULL,
            Activo BIT NOT NULL
                CONSTRAINT DF_OrdenesCompraPresentacionesCompra_Activo DEFAULT ((1)),
            FechaCreacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_OrdenesCompraPresentacionesCompra_FechaCreacion DEFAULT (SYSUTCDATETIME()),
            FechaActualizacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_OrdenesCompraPresentacionesCompra_FechaActualizacion DEFAULT (SYSUTCDATETIME()),
            FechaArchivado DATETIME2(0) NULL,
            CONSTRAINT CK_OCPresentacionesCompra_Factor CHECK (FactorConversionBase > 0),
            CONSTRAINT CK_OCPresentacionesCompra_Archivado CHECK ((Activo = 1 AND FechaArchivado IS NULL) OR (Activo = 0 AND FechaArchivado IS NOT NULL))
        );
    END;

    IF COL_LENGTH('dbo.OrdenesCompraDetalle', 'idVariante') IS NULL
        ALTER TABLE dbo.OrdenesCompraDetalle ADD idVariante UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH('dbo.OrdenesCompraDetalle', 'idPresentacionCompra') IS NULL
        ALTER TABLE dbo.OrdenesCompraDetalle ADD idPresentacionCompra UNIQUEIDENTIFIER NULL;
    IF COL_LENGTH('dbo.OrdenesCompraDetalle', 'VarianteSnapshot') IS NULL
        ALTER TABLE dbo.OrdenesCompraDetalle ADD VarianteSnapshot NVARCHAR(200) NULL;
    IF COL_LENGTH('dbo.OrdenesCompraDetalle', 'PresentacionCompraSnapshot') IS NULL
        ALTER TABLE dbo.OrdenesCompraDetalle ADD PresentacionCompraSnapshot NVARCHAR(150) NULL;
    IF COL_LENGTH('dbo.OrdenesCompraDetalle', 'UnidadCompraSnapshot') IS NULL
        ALTER TABLE dbo.OrdenesCompraDetalle ADD UnidadCompraSnapshot NVARCHAR(100) NULL;
    IF COL_LENGTH('dbo.OrdenesCompraDetalle', 'UnidadCompraAbreviaturaSnapshot') IS NULL
        ALTER TABLE dbo.OrdenesCompraDetalle ADD UnidadCompraAbreviaturaSnapshot NVARCHAR(20) NULL;
    IF COL_LENGTH('dbo.OrdenesCompraDetalle', 'CantidadCompra') IS NULL
        ALTER TABLE dbo.OrdenesCompraDetalle ADD CantidadCompra DECIMAL(18, 4) NULL;
    IF COL_LENGTH('dbo.OrdenesCompraDetalle', 'FactorConversionSnapshot') IS NULL
        ALTER TABLE dbo.OrdenesCompraDetalle ADD FactorConversionSnapshot DECIMAL(28, 12) NULL;
    IF COL_LENGTH('dbo.OrdenesCompraDetalle', 'CantidadBaseOrdenada') IS NULL
        ALTER TABLE dbo.OrdenesCompraDetalle ADD CantidadBaseOrdenada DECIMAL(18, 4) NULL;
    IF COL_LENGTH('dbo.OrdenesCompraDetalle', 'CantidadBaseRecibidaAcumulada') IS NULL
        ALTER TABLE dbo.OrdenesCompraDetalle ADD CantidadBaseRecibidaAcumulada DECIMAL(18, 4) NULL;
    IF COL_LENGTH('dbo.OrdenesCompraDetalle', 'CantidadBasePendiente') IS NULL
        ALTER TABLE dbo.OrdenesCompraDetalle ADD CantidadBasePendiente DECIMAL(18, 4) NULL;
    IF COL_LENGTH('dbo.OrdenesCompraDetalle', 'EstadoPartida') IS NULL
        ALTER TABLE dbo.OrdenesCompraDetalle ADD EstadoPartida TINYINT NULL;

    EXEC(N'UPDATE dbo.OrdenesCompraDetalle
    SET
        CantidadCompra = ISNULL(CantidadCompra, Cantidad),
        FactorConversionSnapshot = ISNULL(FactorConversionSnapshot, 1),
        CantidadBaseOrdenada = ISNULL(CantidadBaseOrdenada, Cantidad),
        CantidadBaseRecibidaAcumulada = ISNULL(CantidadBaseRecibidaAcumulada, 0),
        CantidadBasePendiente = ISNULL(CantidadBasePendiente, Cantidad),
        EstadoPartida = ISNULL(EstadoPartida, 1)
    WHERE CantidadCompra IS NULL
       OR FactorConversionSnapshot IS NULL
       OR CantidadBaseOrdenada IS NULL
       OR CantidadBaseRecibidaAcumulada IS NULL
       OR CantidadBasePendiente IS NULL
       OR EstadoPartida IS NULL;');

    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_OrdenesCompra_Estado')
        ALTER TABLE dbo.OrdenesCompra DROP CONSTRAINT CK_OrdenesCompra_Estado;
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_OrdenesCompra_TotalPorEstado')
        ALTER TABLE dbo.OrdenesCompra DROP CONSTRAINT CK_OrdenesCompra_TotalPorEstado;
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_OrdenesCompra_Cancelacion')
        ALTER TABLE dbo.OrdenesCompra DROP CONSTRAINT CK_OrdenesCompra_Cancelacion;
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_OrdenesCompra_FolioGenerada')
        ALTER TABLE dbo.OrdenesCompra DROP CONSTRAINT CK_OrdenesCompra_FolioGenerada;

    ALTER TABLE dbo.OrdenesCompra WITH CHECK ADD CONSTRAINT CK_OrdenesCompra_Estado CHECK (Estado IN (1, 2, 3, 4, 5));
    ALTER TABLE dbo.OrdenesCompra WITH CHECK ADD CONSTRAINT CK_OrdenesCompra_TotalPorEstado CHECK ((Estado IN (2, 4, 5) AND Total > 0) OR (Estado IN (1, 3) AND Total >= 0));
    ALTER TABLE dbo.OrdenesCompra WITH CHECK ADD CONSTRAINT CK_OrdenesCompra_Cancelacion CHECK ((Estado = 3 AND FechaCancelacion IS NOT NULL AND NULLIF(LTRIM(RTRIM(MotivoCancelacion)), N'') IS NOT NULL) OR (Estado IN (1, 2, 4, 5) AND FechaCancelacion IS NULL AND MotivoCancelacion IS NULL AND idUsuarioCancelacion IS NULL));
    ALTER TABLE dbo.OrdenesCompra WITH CHECK ADD CONSTRAINT CK_OrdenesCompra_FolioGenerada CHECK ((Estado IN (2, 4, 5) AND NULLIF(LTRIM(RTRIM(Folio)), N'') IS NOT NULL) OR (Estado IN (1, 3)));

    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_OrdenesCompraDetalle_Cantidad')
        ALTER TABLE dbo.OrdenesCompraDetalle DROP CONSTRAINT CK_OrdenesCompraDetalle_Cantidad;
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_OrdenesCompraDetalle_Calculo')
        ALTER TABLE dbo.OrdenesCompraDetalle DROP CONSTRAINT CK_OrdenesCompraDetalle_Calculo;
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_OrdenesCompraDetalle_Base')
        ALTER TABLE dbo.OrdenesCompraDetalle DROP CONSTRAINT CK_OrdenesCompraDetalle_Base;
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_OrdenesCompraDetalle_EstadoPartida')
        ALTER TABLE dbo.OrdenesCompraDetalle DROP CONSTRAINT CK_OrdenesCompraDetalle_EstadoPartida;

    EXEC(N'ALTER TABLE dbo.OrdenesCompraDetalle ALTER COLUMN CantidadCompra DECIMAL(18, 4) NOT NULL;');
    EXEC(N'ALTER TABLE dbo.OrdenesCompraDetalle ALTER COLUMN FactorConversionSnapshot DECIMAL(28, 12) NOT NULL;');
    EXEC(N'ALTER TABLE dbo.OrdenesCompraDetalle ALTER COLUMN CantidadBaseOrdenada DECIMAL(18, 4) NOT NULL;');
    EXEC(N'ALTER TABLE dbo.OrdenesCompraDetalle ALTER COLUMN CantidadBaseRecibidaAcumulada DECIMAL(18, 4) NOT NULL;');
    EXEC(N'ALTER TABLE dbo.OrdenesCompraDetalle ALTER COLUMN CantidadBasePendiente DECIMAL(18, 4) NOT NULL;');
    EXEC(N'ALTER TABLE dbo.OrdenesCompraDetalle ALTER COLUMN EstadoPartida TINYINT NOT NULL;');

    EXEC(N'ALTER TABLE dbo.OrdenesCompraDetalle WITH CHECK ADD CONSTRAINT CK_OrdenesCompraDetalle_Cantidad CHECK (Cantidad > 0 AND CantidadCompra > 0 AND FactorConversionSnapshot > 0);');
    EXEC(N'ALTER TABLE dbo.OrdenesCompraDetalle WITH CHECK ADD CONSTRAINT CK_OrdenesCompraDetalle_Base CHECK (CantidadBaseOrdenada > 0 AND CantidadBaseRecibidaAcumulada >= 0 AND CantidadBasePendiente >= 0 AND CantidadBaseOrdenada = CantidadBaseRecibidaAcumulada + CantidadBasePendiente);');
    EXEC(N'ALTER TABLE dbo.OrdenesCompraDetalle WITH CHECK ADD CONSTRAINT CK_OrdenesCompraDetalle_EstadoPartida CHECK (EstadoPartida IN (1, 2, 3, 4));');
    EXEC(N'ALTER TABLE dbo.OrdenesCompraDetalle WITH CHECK ADD CONSTRAINT CK_OrdenesCompraDetalle_Calculo CHECK (Subtotal = ROUND(CantidadCompra * CostoUnitario, 2) AND Total = Subtotal);');

    IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = N'DF_OrdenesCompraDetalle_FactorConversionSnapshot')
        EXEC(N'ALTER TABLE dbo.OrdenesCompraDetalle ADD CONSTRAINT DF_OrdenesCompraDetalle_FactorConversionSnapshot DEFAULT ((1)) FOR FactorConversionSnapshot;');
    IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = N'DF_OrdenesCompraDetalle_CantidadBaseRecibida')
        EXEC(N'ALTER TABLE dbo.OrdenesCompraDetalle ADD CONSTRAINT DF_OrdenesCompraDetalle_CantidadBaseRecibida DEFAULT ((0)) FOR CantidadBaseRecibidaAcumulada;');
    IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = N'DF_OrdenesCompraDetalle_EstadoPartida')
        EXEC(N'ALTER TABLE dbo.OrdenesCompraDetalle ADD CONSTRAINT DF_OrdenesCompraDetalle_EstadoPartida DEFAULT ((1)) FOR EstadoPartida;');

    IF OBJECT_ID(N'dbo.OrdenesCompraDetalle', N'U') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.OrdenesCompraDetalle') AND name = N'IX_OrdenesCompraDetalle_Empresa_Producto_Variante')
        EXEC(N'CREATE NONCLUSTERED INDEX IX_OrdenesCompraDetalle_Empresa_Producto_Variante
            ON dbo.OrdenesCompraDetalle (idEmpresa, idProductoServicio, idVariante);');

    IF OBJECT_ID(N'dbo.OrdenesCompraDetalle', N'U') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.OrdenesCompraDetalle') AND name = N'IX_OrdenesCompraDetalle_Empresa_PresentacionCompra')
        EXEC(N'CREATE NONCLUSTERED INDEX IX_OrdenesCompraDetalle_Empresa_PresentacionCompra
            ON dbo.OrdenesCompraDetalle (idEmpresa, idPresentacionCompra);');

    IF OBJECT_ID(N'dbo.OrdenesCompraDetalle', N'U') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.OrdenesCompraDetalle') AND name = N'IX_OrdenesCompraDetalle_Empresa_EstadoPartida')
        EXEC(N'CREATE NONCLUSTERED INDEX IX_OrdenesCompraDetalle_Empresa_EstadoPartida
            ON dbo.OrdenesCompraDetalle (idEmpresa, EstadoPartida);');

    IF OBJECT_ID(N'dbo.OrdenesCompraPresentacionesCompra', N'U') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.OrdenesCompraPresentacionesCompra') AND name = N'UX_OCPresentacionesCompra_Empresa_Id')
        CREATE UNIQUE NONCLUSTERED INDEX UX_OCPresentacionesCompra_Empresa_Id
            ON dbo.OrdenesCompraPresentacionesCompra (idEmpresa, id);

    IF OBJECT_ID(N'dbo.OrdenesCompraPresentacionesCompra', N'U') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.OrdenesCompraPresentacionesCompra') AND name = N'IX_OCPresentacionesCompra_Empresa_Producto_Variante')
        CREATE NONCLUSTERED INDEX IX_OCPresentacionesCompra_Empresa_Producto_Variante
            ON dbo.OrdenesCompraPresentacionesCompra (idEmpresa, idProductoServicio, idVariante, Activo);

    IF OBJECT_ID(N'dbo.OrdenesCompraPresentacionesCompra', N'U') IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.OrdenesCompraPresentacionesCompra') AND name = N'IX_OCPresentacionesCompra_Empresa_Unidad')
        CREATE NONCLUSTERED INDEX IX_OCPresentacionesCompra_Empresa_Unidad
            ON dbo.OrdenesCompraPresentacionesCompra (idEmpresa, idUnidadCompra, Activo);

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_OCPresentacionesCompra_ProductosServicios_EmpresaId')
        ALTER TABLE dbo.OrdenesCompraPresentacionesCompra WITH CHECK ADD CONSTRAINT FK_OCPresentacionesCompra_ProductosServicios_EmpresaId FOREIGN KEY (idEmpresa, idProductoServicio) REFERENCES dbo.ProductosServicios (idEmpresa, id);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_OCPresentacionesCompra_Variantes_EmpresaId')
        ALTER TABLE dbo.OrdenesCompraPresentacionesCompra WITH CHECK ADD CONSTRAINT FK_OCPresentacionesCompra_Variantes_EmpresaId FOREIGN KEY (idEmpresa, idVariante) REFERENCES dbo.ProductosServiciosVariantes (idEmpresa, id);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_OCPresentacionesCompra_Unidades_EmpresaId')
        ALTER TABLE dbo.OrdenesCompraPresentacionesCompra WITH CHECK ADD CONSTRAINT FK_OCPresentacionesCompra_Unidades_EmpresaId FOREIGN KEY (idEmpresa, idUnidadCompra) REFERENCES dbo.ProductosServiciosUnidadesMedida (idEmpresa, id);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_OrdenesCompraDetalle_Variantes_EmpresaId')
        EXEC(N'ALTER TABLE dbo.OrdenesCompraDetalle WITH CHECK ADD CONSTRAINT FK_OrdenesCompraDetalle_Variantes_EmpresaId FOREIGN KEY (idEmpresa, idVariante) REFERENCES dbo.ProductosServiciosVariantes (idEmpresa, id);');
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_OrdenesCompraDetalle_PresentacionCompra_EmpresaId')
        EXEC(N'ALTER TABLE dbo.OrdenesCompraDetalle WITH CHECK ADD CONSTRAINT FK_OrdenesCompraDetalle_PresentacionCompra_EmpresaId FOREIGN KEY (idEmpresa, idPresentacionCompra) REFERENCES dbo.OrdenesCompraPresentacionesCompra (idEmpresa, id);');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;
    END;

    THROW;
END CATCH;
