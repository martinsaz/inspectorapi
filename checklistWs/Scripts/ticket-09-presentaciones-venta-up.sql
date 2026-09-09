/*
    TICKET 09 - Presentaciones de venta
    Ejecutar despues de confirmar UX_ProductosServicios_Empresa_Id en la base destino.
*/
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes i
        INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
        INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
        WHERE i.object_id = OBJECT_ID(N'dbo.ProductosServicios')
          AND i.is_unique = 1
        GROUP BY i.index_id
        HAVING STRING_AGG(CASE WHEN ic.is_included_column = 0 THEN c.name END, ',')
               WITHIN GROUP (ORDER BY ic.key_ordinal) = N'idEmpresa,id'
    )
    BEGIN
        THROW 51009, 'No existe un indice UNIQUE compatible en dbo.ProductosServicios (idEmpresa, id).', 1;
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosPresentacionesVenta', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.ProductosServiciosPresentacionesVenta
        (
            id UNIQUEIDENTIFIER NOT NULL
                CONSTRAINT PK_ProductosServiciosPresentacionesVenta PRIMARY KEY,
            idEmpresa UNIQUEIDENTIFIER NOT NULL,
            identityKey UNIQUEIDENTIFIER NOT NULL
                CONSTRAINT DF_ProductosServiciosPresentacionesVenta_IdentityKey DEFAULT (NEWID()),
            idProductoServicio UNIQUEIDENTIFIER NOT NULL,
            Nombre NVARCHAR(100) NOT NULL,
            EquivalenciaBase DECIMAL(18, 4) NOT NULL,
            Precio DECIMAL(18, 2) NOT NULL,
            EsPredeterminada BIT NOT NULL
                CONSTRAINT DF_ProductosServiciosPresentacionesVenta_EsPredeterminada DEFAULT ((0)),
            Orden INT NOT NULL
                CONSTRAINT DF_ProductosServiciosPresentacionesVenta_Orden DEFAULT ((0)),
            Activo BIT NOT NULL
                CONSTRAINT DF_ProductosServiciosPresentacionesVenta_Activo DEFAULT ((1)),
            FechaCreacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_ProductosServiciosPresentacionesVenta_FechaCreacion DEFAULT (SYSUTCDATETIME()),
            FechaActualizacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_ProductosServiciosPresentacionesVenta_FechaActualizacion DEFAULT (SYSUTCDATETIME()),
            FechaArchivado DATETIME2(0) NULL,
            CONSTRAINT CK_ProductosServiciosPresentacionesVenta_Equivalencia
                CHECK (EquivalenciaBase > 0),
            CONSTRAINT CK_ProductosServiciosPresentacionesVenta_Precio
                CHECK (Precio >= 0),
            CONSTRAINT FK_ProductosServiciosPresentacionesVenta_Productos_EmpresaId
                FOREIGN KEY (idEmpresa, idProductoServicio)
                REFERENCES dbo.ProductosServicios (idEmpresa, id)
        );
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosPresentacionesVenta') AND name = N'UX_ProductosServiciosPresentacionesVenta_Empresa_Id')
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UX_ProductosServiciosPresentacionesVenta_Empresa_Id
            ON dbo.ProductosServiciosPresentacionesVenta (idEmpresa, id);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosPresentacionesVenta') AND name = N'IX_ProductosServiciosPresentacionesVenta_Empresa_Producto_Activo_Orden')
    BEGIN
        CREATE NONCLUSTERED INDEX IX_ProductosServiciosPresentacionesVenta_Empresa_Producto_Activo_Orden
            ON dbo.ProductosServiciosPresentacionesVenta (idEmpresa, idProductoServicio, Activo, Orden);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosPresentacionesVenta') AND name = N'UX_ProductosServiciosPresentacionesVenta_PredeterminadaActiva')
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UX_ProductosServiciosPresentacionesVenta_PredeterminadaActiva
            ON dbo.ProductosServiciosPresentacionesVenta (idEmpresa, idProductoServicio)
            WHERE Activo = 1 AND EsPredeterminada = 1;
    END;

    DECLARE @Candidatos INT = (
        SELECT COUNT(1)
        FROM dbo.ProductosServicios ps
        WHERE ps.Tipo = 1 AND ps.Activo = 1 AND ps.idUnidadMedida IS NOT NULL
          AND NOT EXISTS (
              SELECT 1
              FROM dbo.ProductosServiciosPresentacionesVenta pv
              WHERE pv.idEmpresa = ps.idEmpresa AND pv.idProductoServicio = ps.id
          )
    );

    INSERT INTO dbo.ProductosServiciosPresentacionesVenta
        (id, idEmpresa, identityKey, idProductoServicio, Nombre, EquivalenciaBase, Precio, EsPredeterminada, Orden, Activo, FechaCreacion, FechaActualizacion)
    SELECT NEWID(), ps.idEmpresa, NEWID(), ps.id, um.Nombre, 1, ps.PrecioPublico, 1, 0, 1, SYSUTCDATETIME(), SYSUTCDATETIME()
    FROM dbo.ProductosServicios ps
    INNER JOIN dbo.ProductosServiciosUnidadesMedida um
        ON um.idEmpresa = ps.idEmpresa AND um.id = ps.idUnidadMedida
    WHERE ps.Tipo = 1 AND ps.Activo = 1 AND ps.idUnidadMedida IS NOT NULL
      AND NOT EXISTS (
          SELECT 1
          FROM dbo.ProductosServiciosPresentacionesVenta pv
          WHERE pv.idEmpresa = ps.idEmpresa AND pv.idProductoServicio = ps.id
      );

    DECLARE @Creadas INT = @@ROWCOUNT;
    SELECT @Candidatos AS ProductosCandidatos, @Creadas AS PresentacionesInicialesCreadas;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
