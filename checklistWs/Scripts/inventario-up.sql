SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.InventarioSaldos', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.InventarioSaldos
        (
            id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_InventarioSaldos_id DEFAULT (NEWID()),
            idEmpresa UNIQUEIDENTIFIER NOT NULL,
            identityKey UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_InventarioSaldos_identityKey DEFAULT (NEWID()),
            idSucursal UNIQUEIDENTIFIER NOT NULL,
            idProductoServicio UNIQUEIDENTIFIER NOT NULL,
            idVariante UNIQUEIDENTIFIER NULL,
            CantidadBaseActual DECIMAL(28,12) NOT NULL CONSTRAINT DF_InventarioSaldos_CantidadBaseActual DEFAULT ((0)),
            FechaCreacion DATETIME2(0) NOT NULL CONSTRAINT DF_InventarioSaldos_FechaCreacion DEFAULT (SYSUTCDATETIME()),
            FechaActualizacion DATETIME2(0) NOT NULL CONSTRAINT DF_InventarioSaldos_FechaActualizacion DEFAULT (SYSUTCDATETIME()),
            CONSTRAINT PK_InventarioSaldos PRIMARY KEY CLUSTERED (id)
        );
    END

    IF OBJECT_ID(N'dbo.InventarioMovimientos', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.InventarioMovimientos
        (
            id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_InventarioMovimientos_id DEFAULT (NEWID()),
            idEmpresa UNIQUEIDENTIFIER NOT NULL,
            identityKey UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_InventarioMovimientos_identityKey DEFAULT (NEWID()),
            idSucursal UNIQUEIDENTIFIER NOT NULL,
            idProductoServicio UNIQUEIDENTIFIER NOT NULL,
            idVariante UNIQUEIDENTIFIER NULL,
            TipoMovimiento TINYINT NOT NULL,
            CantidadBase DECIMAL(28,12) NOT NULL,
            SaldoAnterior DECIMAL(28,12) NOT NULL,
            SaldoPosterior DECIMAL(28,12) NOT NULL,
            OrigenTipo NVARCHAR(40) NOT NULL,
            OrigenId UNIQUEIDENTIFIER NULL,
            OrigenPartidaId UNIQUEIDENTIFIER NULL,
            OperationKey NVARCHAR(120) NOT NULL,
            Observaciones NVARCHAR(500) NULL,
            idUsuario UNIQUEIDENTIFIER NULL,
            FechaMovimiento DATETIME2(0) NOT NULL CONSTRAINT DF_InventarioMovimientos_FechaMovimiento DEFAULT (SYSUTCDATETIME()),
            FechaCreacion DATETIME2(0) NOT NULL CONSTRAINT DF_InventarioMovimientos_FechaCreacion DEFAULT (SYSUTCDATETIME()),
            CONSTRAINT PK_InventarioMovimientos PRIMARY KEY CLUSTERED (id)
        );
    END

    IF OBJECT_ID(N'dbo.InventarioSeries', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.InventarioSeries
        (
            id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_InventarioSeries_id DEFAULT (NEWID()),
            idEmpresa UNIQUEIDENTIFIER NOT NULL,
            identityKey UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_InventarioSeries_identityKey DEFAULT (NEWID()),
            idProductoServicio UNIQUEIDENTIFIER NOT NULL,
            idVariante UNIQUEIDENTIFIER NULL,
            NumeroSerie NVARCHAR(120) NOT NULL,
            idSucursalActual UNIQUEIDENTIFIER NOT NULL,
            Estado TINYINT NOT NULL CONSTRAINT DF_InventarioSeries_Estado DEFAULT ((1)),
            OrigenMovimientoId UNIQUEIDENTIFIER NULL,
            FechaCreacion DATETIME2(0) NOT NULL CONSTRAINT DF_InventarioSeries_FechaCreacion DEFAULT (SYSUTCDATETIME()),
            FechaActualizacion DATETIME2(0) NOT NULL CONSTRAINT DF_InventarioSeries_FechaActualizacion DEFAULT (SYSUTCDATETIME()),
            FechaArchivado DATETIME2(0) NULL,
            CONSTRAINT PK_InventarioSeries PRIMARY KEY CLUSTERED (id)
        );
    END

    IF OBJECT_ID(N'dbo.InventarioSaldos', N'U') IS NOT NULL
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.InventarioSaldos') AND name = N'UX_InventarioSaldos_Empresa_Id')
            CREATE UNIQUE NONCLUSTERED INDEX UX_InventarioSaldos_Empresa_Id ON dbo.InventarioSaldos (idEmpresa, id);
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.InventarioSaldos') AND name = N'UX_InventarioSaldos_Empresa_Sucursal_Producto_Variante')
            CREATE UNIQUE NONCLUSTERED INDEX UX_InventarioSaldos_Empresa_Sucursal_Producto_Variante ON dbo.InventarioSaldos (idEmpresa, idSucursal, idProductoServicio, idVariante);
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.InventarioSaldos') AND name = N'IX_InventarioSaldos_Empresa_Producto_Variante')
            CREATE NONCLUSTERED INDEX IX_InventarioSaldos_Empresa_Producto_Variante ON dbo.InventarioSaldos (idEmpresa, idProductoServicio, idVariante);
    END

    IF OBJECT_ID(N'dbo.InventarioMovimientos', N'U') IS NOT NULL
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.InventarioMovimientos') AND name = N'UX_InventarioMovimientos_Empresa_Id')
            CREATE UNIQUE NONCLUSTERED INDEX UX_InventarioMovimientos_Empresa_Id ON dbo.InventarioMovimientos (idEmpresa, id);
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.InventarioMovimientos') AND name = N'UX_InventarioMovimientos_Empresa_OperationKey')
            CREATE UNIQUE NONCLUSTERED INDEX UX_InventarioMovimientos_Empresa_OperationKey ON dbo.InventarioMovimientos (idEmpresa, OperationKey);
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.InventarioMovimientos') AND name = N'IX_InventarioMovimientos_Empresa_Sucursal_Producto_Variante_Fecha')
            CREATE NONCLUSTERED INDEX IX_InventarioMovimientos_Empresa_Sucursal_Producto_Variante_Fecha ON dbo.InventarioMovimientos (idEmpresa, idSucursal, idProductoServicio, idVariante, FechaMovimiento);
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.InventarioMovimientos') AND name = N'IX_InventarioMovimientos_Empresa_Origen')
            CREATE NONCLUSTERED INDEX IX_InventarioMovimientos_Empresa_Origen ON dbo.InventarioMovimientos (idEmpresa, OrigenTipo, OrigenId, OrigenPartidaId);
    END

    IF OBJECT_ID(N'dbo.InventarioSeries', N'U') IS NOT NULL
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.InventarioSeries') AND name = N'UX_InventarioSeries_Empresa_Id')
            CREATE UNIQUE NONCLUSTERED INDEX UX_InventarioSeries_Empresa_Id ON dbo.InventarioSeries (idEmpresa, id);
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.InventarioSeries') AND name = N'UX_InventarioSeries_Empresa_Producto_Variante_Numero_Activo')
            CREATE UNIQUE NONCLUSTERED INDEX UX_InventarioSeries_Empresa_Producto_Variante_Numero_Activo ON dbo.InventarioSeries (idEmpresa, idProductoServicio, idVariante, NumeroSerie) WHERE Estado = 1 AND FechaArchivado IS NULL;
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.InventarioSeries') AND name = N'IX_InventarioSeries_Empresa_Sucursal')
            CREATE NONCLUSTERED INDEX IX_InventarioSeries_Empresa_Sucursal ON dbo.InventarioSeries (idEmpresa, idSucursalActual, Estado);
    END

    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_InventarioSaldos_Cantidad')
        ALTER TABLE dbo.InventarioSaldos WITH CHECK ADD CONSTRAINT CK_InventarioSaldos_Cantidad CHECK (CantidadBaseActual >= 0);
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_InventarioMovimientos_Tipo')
        ALTER TABLE dbo.InventarioMovimientos WITH CHECK ADD CONSTRAINT CK_InventarioMovimientos_Tipo CHECK (TipoMovimiento IN (1, 2, 3, 4, 5));
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_InventarioMovimientos_Cantidad')
        ALTER TABLE dbo.InventarioMovimientos WITH CHECK ADD CONSTRAINT CK_InventarioMovimientos_Cantidad CHECK (CantidadBase > 0);
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_InventarioMovimientos_Saldos')
        ALTER TABLE dbo.InventarioMovimientos WITH CHECK ADD CONSTRAINT CK_InventarioMovimientos_Saldos CHECK (SaldoAnterior >= 0 AND SaldoPosterior >= 0);
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_InventarioSeries_Estado')
        ALTER TABLE dbo.InventarioSeries WITH CHECK ADD CONSTRAINT CK_InventarioSeries_Estado CHECK (Estado IN (1, 2, 3));
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_InventarioSeries_Archivado')
        ALTER TABLE dbo.InventarioSeries WITH CHECK ADD CONSTRAINT CK_InventarioSeries_Archivado CHECK ((Estado = 1 AND FechaArchivado IS NULL) OR (Estado IN (2, 3)));

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_InventarioSaldos_Sucursales_EmpresaId')
        ALTER TABLE dbo.InventarioSaldos WITH CHECK ADD CONSTRAINT FK_InventarioSaldos_Sucursales_EmpresaId FOREIGN KEY (idEmpresa, idSucursal) REFERENCES dbo.Sucursales (idEmpresa, id);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_InventarioSaldos_ProductosServicios_EmpresaId')
        ALTER TABLE dbo.InventarioSaldos WITH CHECK ADD CONSTRAINT FK_InventarioSaldos_ProductosServicios_EmpresaId FOREIGN KEY (idEmpresa, idProductoServicio) REFERENCES dbo.ProductosServicios (idEmpresa, id);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_InventarioSaldos_Variantes_EmpresaId')
        ALTER TABLE dbo.InventarioSaldos WITH CHECK ADD CONSTRAINT FK_InventarioSaldos_Variantes_EmpresaId FOREIGN KEY (idEmpresa, idVariante) REFERENCES dbo.ProductosServiciosVariantes (idEmpresa, id);

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_InventarioMovimientos_Sucursales_EmpresaId')
        ALTER TABLE dbo.InventarioMovimientos WITH CHECK ADD CONSTRAINT FK_InventarioMovimientos_Sucursales_EmpresaId FOREIGN KEY (idEmpresa, idSucursal) REFERENCES dbo.Sucursales (idEmpresa, id);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_InventarioMovimientos_ProductosServicios_EmpresaId')
        ALTER TABLE dbo.InventarioMovimientos WITH CHECK ADD CONSTRAINT FK_InventarioMovimientos_ProductosServicios_EmpresaId FOREIGN KEY (idEmpresa, idProductoServicio) REFERENCES dbo.ProductosServicios (idEmpresa, id);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_InventarioMovimientos_Variantes_EmpresaId')
        ALTER TABLE dbo.InventarioMovimientos WITH CHECK ADD CONSTRAINT FK_InventarioMovimientos_Variantes_EmpresaId FOREIGN KEY (idEmpresa, idVariante) REFERENCES dbo.ProductosServiciosVariantes (idEmpresa, id);

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_InventarioSeries_ProductosServicios_EmpresaId')
        ALTER TABLE dbo.InventarioSeries WITH CHECK ADD CONSTRAINT FK_InventarioSeries_ProductosServicios_EmpresaId FOREIGN KEY (idEmpresa, idProductoServicio) REFERENCES dbo.ProductosServicios (idEmpresa, id);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_InventarioSeries_Variantes_EmpresaId')
        ALTER TABLE dbo.InventarioSeries WITH CHECK ADD CONSTRAINT FK_InventarioSeries_Variantes_EmpresaId FOREIGN KEY (idEmpresa, idVariante) REFERENCES dbo.ProductosServiciosVariantes (idEmpresa, id);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_InventarioSeries_Sucursales_EmpresaId')
        ALTER TABLE dbo.InventarioSeries WITH CHECK ADD CONSTRAINT FK_InventarioSeries_Sucursales_EmpresaId FOREIGN KEY (idEmpresa, idSucursalActual) REFERENCES dbo.Sucursales (idEmpresa, id);
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_InventarioSeries_Movimiento_EmpresaId')
        ALTER TABLE dbo.InventarioSeries WITH CHECK ADD CONSTRAINT FK_InventarioSeries_Movimiento_EmpresaId FOREIGN KEY (idEmpresa, OrigenMovimientoId) REFERENCES dbo.InventarioMovimientos (idEmpresa, id);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;
