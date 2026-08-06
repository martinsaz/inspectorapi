/*
    MODULO: Ordenes de compra
    FASE: Modelo de datos
    SCRIPT: DOWN
    ADVERTENCIA:
    - Este script elimina fisicamente los objetos del modulo.
    - No ejecutar automaticamente.
    - No usar como rollback automatico.
    - No toca tablas externas al modulo.
*/

SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = N'FK_OrdenesCompraDetalle_OrdenesCompra_EmpresaId'
    )
    BEGIN
        ALTER TABLE dbo.OrdenesCompraDetalle
        DROP CONSTRAINT FK_OrdenesCompraDetalle_OrdenesCompra_EmpresaId;
    END;

    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.OrdenesCompraDetalle')
          AND name = N'UX_OrdenesCompraDetalle_Empresa_Orden_ProductoServicio_Activo'
    )
    BEGIN
        DROP INDEX UX_OrdenesCompraDetalle_Empresa_Orden_ProductoServicio_Activo
            ON dbo.OrdenesCompraDetalle;
    END;

    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.OrdenesCompraDetalle')
          AND name = N'UX_OrdenesCompraDetalle_Empresa_Orden_NumeroPartida'
    )
    BEGIN
        DROP INDEX UX_OrdenesCompraDetalle_Empresa_Orden_NumeroPartida
            ON dbo.OrdenesCompraDetalle;
    END;

    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.OrdenesCompraDetalle')
          AND name = N'IX_OrdenesCompraDetalle_Empresa_Orden'
    )
    BEGIN
        DROP INDEX IX_OrdenesCompraDetalle_Empresa_Orden
            ON dbo.OrdenesCompraDetalle;
    END;

    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.OrdenesCompraDetalle')
          AND name = N'UX_OrdenesCompraDetalle_Empresa_Id'
    )
    BEGIN
        DROP INDEX UX_OrdenesCompraDetalle_Empresa_Id
            ON dbo.OrdenesCompraDetalle;
    END;

    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.OrdenesCompra')
          AND name = N'IX_OrdenesCompra_Empresa_RazonSocial'
    )
    BEGIN
        DROP INDEX IX_OrdenesCompra_Empresa_RazonSocial
            ON dbo.OrdenesCompra;
    END;

    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.OrdenesCompra')
          AND name = N'IX_OrdenesCompra_Empresa_Sucursal'
    )
    BEGIN
        DROP INDEX IX_OrdenesCompra_Empresa_Sucursal
            ON dbo.OrdenesCompra;
    END;

    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.OrdenesCompra')
          AND name = N'IX_OrdenesCompra_Empresa_Proveedor'
    )
    BEGIN
        DROP INDEX IX_OrdenesCompra_Empresa_Proveedor
            ON dbo.OrdenesCompra;
    END;

    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.OrdenesCompra')
          AND name = N'IX_OrdenesCompra_Empresa_Estado_FechaOrden'
    )
    BEGIN
        DROP INDEX IX_OrdenesCompra_Empresa_Estado_FechaOrden
            ON dbo.OrdenesCompra;
    END;

    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.OrdenesCompra')
          AND name = N'UX_OrdenesCompra_Empresa_Folio'
    )
    BEGIN
        DROP INDEX UX_OrdenesCompra_Empresa_Folio
            ON dbo.OrdenesCompra;
    END;

    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.OrdenesCompra')
          AND name = N'UX_OrdenesCompra_Empresa_Id'
    )
    BEGIN
        DROP INDEX UX_OrdenesCompra_Empresa_Id
            ON dbo.OrdenesCompra;
    END;

    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.OrdenesCompraFolios')
          AND name = N'UX_OrdenesCompraFolios_Empresa'
    )
    BEGIN
        DROP INDEX UX_OrdenesCompraFolios_Empresa
            ON dbo.OrdenesCompraFolios;
    END;

    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.OrdenesCompraFolios')
          AND name = N'UX_OrdenesCompraFolios_Empresa_Id'
    )
    BEGIN
        DROP INDEX UX_OrdenesCompraFolios_Empresa_Id
            ON dbo.OrdenesCompraFolios;
    END;

    IF OBJECT_ID(N'dbo.OrdenesCompraDetalle', N'U') IS NOT NULL
    BEGIN
        DROP TABLE dbo.OrdenesCompraDetalle;
    END;

    IF OBJECT_ID(N'dbo.OrdenesCompra', N'U') IS NOT NULL
    BEGIN
        DROP TABLE dbo.OrdenesCompra;
    END;

    IF OBJECT_ID(N'dbo.OrdenesCompraFolios', N'U') IS NOT NULL
    BEGIN
        DROP TABLE dbo.OrdenesCompraFolios;
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;
    END;

    THROW;
END CATCH;
