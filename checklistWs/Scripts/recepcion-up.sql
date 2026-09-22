SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.RecepcionFolios', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RecepcionFolios
    (
        id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_RecepcionFolios_id DEFAULT (NEWID()),
        idEmpresa UNIQUEIDENTIFIER NOT NULL,
        identityKey UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_RecepcionFolios_identityKey DEFAULT (NEWID()),
        UltimoConsecutivo BIGINT NOT NULL CONSTRAINT DF_RecepcionFolios_UltimoConsecutivo DEFAULT ((0)),
        FechaCreacion DATETIME2(0) NOT NULL CONSTRAINT DF_RecepcionFolios_FechaCreacion DEFAULT (SYSUTCDATETIME()),
        FechaActualizacion DATETIME2(0) NOT NULL CONSTRAINT DF_RecepcionFolios_FechaActualizacion DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_RecepcionFolios PRIMARY KEY CLUSTERED (id),
        CONSTRAINT CK_RecepcionFolios_UltimoConsecutivo CHECK (UltimoConsecutivo >= 0)
    );
END;

IF OBJECT_ID(N'dbo.Recepciones', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Recepciones
    (
        id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Recepciones_id DEFAULT (NEWID()),
        idEmpresa UNIQUEIDENTIFIER NOT NULL,
        identityKey UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Recepciones_identityKey DEFAULT (NEWID()),
        idOrdenCompra UNIQUEIDENTIFIER NOT NULL,
        FolioRecepcion NVARCHAR(30) NOT NULL,
        idSucursal UNIQUEIDENTIFIER NOT NULL,
        FechaRecepcion DATETIME2(0) NOT NULL,
        Estado TINYINT NOT NULL CONSTRAINT DF_Recepciones_Estado DEFAULT ((2)),
        OperationKey NVARCHAR(120) NOT NULL,
        Observaciones NVARCHAR(1000) NULL,
        FechaCancelacion DATETIME2(0) NULL,
        MotivoCancelacion NVARCHAR(500) NULL,
        idUsuarioCreacion UNIQUEIDENTIFIER NULL,
        idUsuarioConfirmacion UNIQUEIDENTIFIER NULL,
        FechaCreacion DATETIME2(0) NOT NULL CONSTRAINT DF_Recepciones_FechaCreacion DEFAULT (SYSUTCDATETIME()),
        FechaActualizacion DATETIME2(0) NOT NULL CONSTRAINT DF_Recepciones_FechaActualizacion DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_Recepciones PRIMARY KEY CLUSTERED (id),
        CONSTRAINT CK_Recepciones_Estado CHECK (Estado IN (1, 2, 3)),
        CONSTRAINT CK_Recepciones_Cancelacion CHECK ([Estado]=(3) AND [FechaCancelacion] IS NOT NULL AND NULLIF(LTRIM(RTRIM([MotivoCancelacion])), N'') IS NOT NULL OR ([Estado]=(2) OR [Estado]=(1)) AND [FechaCancelacion] IS NULL AND [MotivoCancelacion] IS NULL)
    );
END;

IF OBJECT_ID(N'dbo.RecepcionPartidas', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RecepcionPartidas
    (
        id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_RecepcionPartidas_id DEFAULT (NEWID()),
        idEmpresa UNIQUEIDENTIFIER NOT NULL,
        identityKey UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_RecepcionPartidas_identityKey DEFAULT (NEWID()),
        idRecepcion UNIQUEIDENTIFIER NOT NULL,
        idOrdenCompra UNIQUEIDENTIFIER NOT NULL,
        idOrdenCompraDetalle UNIQUEIDENTIFIER NOT NULL,
        NumeroPartida INT NOT NULL,
        TipoPartida TINYINT NOT NULL,
        idProductoServicio UNIQUEIDENTIFIER NOT NULL,
        idVariante UNIQUEIDENTIFIER NULL,
        idPresentacionCompra UNIQUEIDENTIFIER NULL,
        PresentacionCompraSnapshot NVARCHAR(150) NULL,
        UnidadCompraSnapshot NVARCHAR(100) NULL,
        UnidadCompraAbreviaturaSnapshot NVARCHAR(20) NULL,
        FactorConversionSnapshot DECIMAL(28,12) NOT NULL,
        CantidadCompraRecibida DECIMAL(18,4) NOT NULL,
        CantidadBaseOrdenada DECIMAL(18,4) NOT NULL,
        CantidadBaseRecibidaAnterior DECIMAL(18,4) NOT NULL,
        CantidadBaseEstaRecepcion DECIMAL(18,4) NOT NULL,
        CantidadBaseRecibidaAcumulada DECIMAL(18,4) NOT NULL,
        CantidadBasePendiente DECIMAL(18,4) NOT NULL,
        EstadoPartidaRecepcion TINYINT NOT NULL,
        ControlSerie BIT NOT NULL CONSTRAINT DF_RecepcionPartidas_ControlSerie DEFAULT ((0)),
        idInventarioMovimiento UNIQUEIDENTIFIER NULL,
        FechaCreacion DATETIME2(0) NOT NULL CONSTRAINT DF_RecepcionPartidas_FechaCreacion DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_RecepcionPartidas PRIMARY KEY CLUSTERED (id),
        CONSTRAINT CK_RecepcionPartidas_Tipo CHECK (TipoPartida IN (1, 2)),
        CONSTRAINT CK_RecepcionPartidas_Cantidades CHECK (CantidadCompraRecibida > 0 AND FactorConversionSnapshot > 0 AND CantidadBaseOrdenada > 0 AND CantidadBaseRecibidaAnterior >= 0 AND CantidadBaseEstaRecepcion > 0 AND CantidadBaseRecibidaAcumulada >= CantidadBaseRecibidaAnterior AND CantidadBasePendiente >= 0 AND CantidadBaseOrdenada = CantidadBaseRecibidaAcumulada + CantidadBasePendiente),
        CONSTRAINT CK_RecepcionPartidas_NoSobreRecepcion CHECK (CantidadBaseEstaRecepcion <= (CantidadBaseOrdenada - CantidadBaseRecibidaAnterior)),
        CONSTRAINT CK_RecepcionPartidas_Estado CHECK (EstadoPartidaRecepcion IN (1, 2, 3))
    );
END;

IF OBJECT_ID(N'dbo.RecepcionSeries', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RecepcionSeries
    (
        id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_RecepcionSeries_id DEFAULT (NEWID()),
        idEmpresa UNIQUEIDENTIFIER NOT NULL,
        identityKey UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_RecepcionSeries_identityKey DEFAULT (NEWID()),
        idRecepcion UNIQUEIDENTIFIER NOT NULL,
        idRecepcionPartida UNIQUEIDENTIFIER NOT NULL,
        idProductoServicio UNIQUEIDENTIFIER NOT NULL,
        idVariante UNIQUEIDENTIFIER NULL,
        NumeroSerie NVARCHAR(120) NOT NULL,
        idInventarioSerie UNIQUEIDENTIFIER NULL,
        FechaCreacion DATETIME2(0) NOT NULL CONSTRAINT DF_RecepcionSeries_FechaCreacion DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_RecepcionSeries PRIMARY KEY CLUSTERED (id)
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.RecepcionFolios') AND name = N'UX_RecepcionFolios_Empresa_Id')
    CREATE UNIQUE NONCLUSTERED INDEX UX_RecepcionFolios_Empresa_Id ON dbo.RecepcionFolios (idEmpresa, id);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.RecepcionFolios') AND name = N'UX_RecepcionFolios_Empresa')
    CREATE UNIQUE NONCLUSTERED INDEX UX_RecepcionFolios_Empresa ON dbo.RecepcionFolios (idEmpresa);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Recepciones') AND name = N'UX_Recepciones_Empresa_Id')
    CREATE UNIQUE NONCLUSTERED INDEX UX_Recepciones_Empresa_Id ON dbo.Recepciones (idEmpresa, id);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Recepciones') AND name = N'UX_Recepciones_Empresa_Folio')
    CREATE UNIQUE NONCLUSTERED INDEX UX_Recepciones_Empresa_Folio ON dbo.Recepciones (idEmpresa, FolioRecepcion);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Recepciones') AND name = N'UX_Recepciones_Empresa_OperationKey')
    CREATE UNIQUE NONCLUSTERED INDEX UX_Recepciones_Empresa_OperationKey ON dbo.Recepciones (idEmpresa, OperationKey);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Recepciones') AND name = N'IX_Recepciones_Empresa_OC')
    CREATE NONCLUSTERED INDEX IX_Recepciones_Empresa_OC ON dbo.Recepciones (idEmpresa, idOrdenCompra);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Recepciones') AND name = N'IX_Recepciones_Empresa_Fecha')
    CREATE NONCLUSTERED INDEX IX_Recepciones_Empresa_Fecha ON dbo.Recepciones (idEmpresa, FechaRecepcion);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Recepciones') AND name = N'IX_Recepciones_Empresa_Estado')
    CREATE NONCLUSTERED INDEX IX_Recepciones_Empresa_Estado ON dbo.Recepciones (idEmpresa, Estado);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Recepciones') AND name = N'IX_Recepciones_Empresa_Sucursal')
    CREATE NONCLUSTERED INDEX IX_Recepciones_Empresa_Sucursal ON dbo.Recepciones (idEmpresa, idSucursal);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.RecepcionPartidas') AND name = N'UX_RecepcionPartidas_Empresa_Id')
    CREATE UNIQUE NONCLUSTERED INDEX UX_RecepcionPartidas_Empresa_Id ON dbo.RecepcionPartidas (idEmpresa, id);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.RecepcionPartidas') AND name = N'IX_RecepcionPartidas_Empresa_Recepcion')
    CREATE NONCLUSTERED INDEX IX_RecepcionPartidas_Empresa_Recepcion ON dbo.RecepcionPartidas (idEmpresa, idRecepcion);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.RecepcionPartidas') AND name = N'IX_RecepcionPartidas_Empresa_OCDetalle')
    CREATE NONCLUSTERED INDEX IX_RecepcionPartidas_Empresa_OCDetalle ON dbo.RecepcionPartidas (idEmpresa, idOrdenCompraDetalle);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.RecepcionPartidas') AND name = N'IX_RecepcionPartidas_Empresa_Producto_Variante')
    CREATE NONCLUSTERED INDEX IX_RecepcionPartidas_Empresa_Producto_Variante ON dbo.RecepcionPartidas (idEmpresa, idProductoServicio, idVariante);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.RecepcionSeries') AND name = N'UX_RecepcionSeries_Empresa_Id')
    CREATE UNIQUE NONCLUSTERED INDEX UX_RecepcionSeries_Empresa_Id ON dbo.RecepcionSeries (idEmpresa, id);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.RecepcionSeries') AND name = N'UX_RecepcionSeries_Empresa_Partida_Numero')
    CREATE UNIQUE NONCLUSTERED INDEX UX_RecepcionSeries_Empresa_Partida_Numero ON dbo.RecepcionSeries (idEmpresa, idRecepcionPartida, NumeroSerie);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.RecepcionSeries') AND name = N'IX_RecepcionSeries_Empresa_Producto_Variante')
    CREATE NONCLUSTERED INDEX IX_RecepcionSeries_Empresa_Producto_Variante ON dbo.RecepcionSeries (idEmpresa, idProductoServicio, idVariante, NumeroSerie);

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Recepciones_OrdenesCompra_EmpresaId')
    ALTER TABLE dbo.Recepciones WITH CHECK ADD CONSTRAINT FK_Recepciones_OrdenesCompra_EmpresaId FOREIGN KEY (idEmpresa, idOrdenCompra) REFERENCES dbo.OrdenesCompra (idEmpresa, id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Recepciones_Sucursales_EmpresaId')
    ALTER TABLE dbo.Recepciones WITH CHECK ADD CONSTRAINT FK_Recepciones_Sucursales_EmpresaId FOREIGN KEY (idEmpresa, idSucursal) REFERENCES dbo.Sucursales (idEmpresa, id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_RecepcionPartidas_Recepciones_EmpresaId')
    ALTER TABLE dbo.RecepcionPartidas WITH CHECK ADD CONSTRAINT FK_RecepcionPartidas_Recepciones_EmpresaId FOREIGN KEY (idEmpresa, idRecepcion) REFERENCES dbo.Recepciones (idEmpresa, id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_RecepcionPartidas_OrdenesCompra_EmpresaId')
    ALTER TABLE dbo.RecepcionPartidas WITH CHECK ADD CONSTRAINT FK_RecepcionPartidas_OrdenesCompra_EmpresaId FOREIGN KEY (idEmpresa, idOrdenCompra) REFERENCES dbo.OrdenesCompra (idEmpresa, id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_RecepcionPartidas_OrdenesCompraDetalle_EmpresaId')
    ALTER TABLE dbo.RecepcionPartidas WITH CHECK ADD CONSTRAINT FK_RecepcionPartidas_OrdenesCompraDetalle_EmpresaId FOREIGN KEY (idEmpresa, idOrdenCompraDetalle) REFERENCES dbo.OrdenesCompraDetalle (idEmpresa, id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_RecepcionPartidas_ProductosServicios_EmpresaId')
    ALTER TABLE dbo.RecepcionPartidas WITH CHECK ADD CONSTRAINT FK_RecepcionPartidas_ProductosServicios_EmpresaId FOREIGN KEY (idEmpresa, idProductoServicio) REFERENCES dbo.ProductosServicios (idEmpresa, id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_RecepcionPartidas_Variantes_EmpresaId')
    ALTER TABLE dbo.RecepcionPartidas WITH CHECK ADD CONSTRAINT FK_RecepcionPartidas_Variantes_EmpresaId FOREIGN KEY (idEmpresa, idVariante) REFERENCES dbo.ProductosServiciosVariantes (idEmpresa, id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_RecepcionPartidas_InventarioMovimiento_EmpresaId')
    ALTER TABLE dbo.RecepcionPartidas WITH CHECK ADD CONSTRAINT FK_RecepcionPartidas_InventarioMovimiento_EmpresaId FOREIGN KEY (idEmpresa, idInventarioMovimiento) REFERENCES dbo.InventarioMovimientos (idEmpresa, id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_RecepcionSeries_Recepciones_EmpresaId')
    ALTER TABLE dbo.RecepcionSeries WITH CHECK ADD CONSTRAINT FK_RecepcionSeries_Recepciones_EmpresaId FOREIGN KEY (idEmpresa, idRecepcion) REFERENCES dbo.Recepciones (idEmpresa, id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_RecepcionSeries_RecepcionPartidas_EmpresaId')
    ALTER TABLE dbo.RecepcionSeries WITH CHECK ADD CONSTRAINT FK_RecepcionSeries_RecepcionPartidas_EmpresaId FOREIGN KEY (idEmpresa, idRecepcionPartida) REFERENCES dbo.RecepcionPartidas (idEmpresa, id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_RecepcionSeries_ProductosServicios_EmpresaId')
    ALTER TABLE dbo.RecepcionSeries WITH CHECK ADD CONSTRAINT FK_RecepcionSeries_ProductosServicios_EmpresaId FOREIGN KEY (idEmpresa, idProductoServicio) REFERENCES dbo.ProductosServicios (idEmpresa, id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_RecepcionSeries_Variantes_EmpresaId')
    ALTER TABLE dbo.RecepcionSeries WITH CHECK ADD CONSTRAINT FK_RecepcionSeries_Variantes_EmpresaId FOREIGN KEY (idEmpresa, idVariante) REFERENCES dbo.ProductosServiciosVariantes (idEmpresa, id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_RecepcionSeries_InventarioSeries_EmpresaId')
    ALTER TABLE dbo.RecepcionSeries WITH CHECK ADD CONSTRAINT FK_RecepcionSeries_InventarioSeries_EmpresaId FOREIGN KEY (idEmpresa, idInventarioSerie) REFERENCES dbo.InventarioSeries (idEmpresa, id);

COMMIT TRANSACTION;
