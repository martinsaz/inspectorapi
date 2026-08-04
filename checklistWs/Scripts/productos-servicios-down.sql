/*
    MODULO: Productos y servicios
    FASE: Modelo de datos
    SCRIPT: DOWN
    ADVERTENCIA:
    - Este script elimina fisicamente las tablas del modulo.
    - No ejecutar automaticamente.
    - No toca tablas externas al modulo.
*/

SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = N'FK_ProductosServiciosMovimientos_ProductosServicios_EmpresaId'
    )
    BEGIN
        ALTER TABLE dbo.ProductosServiciosMovimientosInventario
        DROP CONSTRAINT FK_ProductosServiciosMovimientos_ProductosServicios_EmpresaId;
    END;

    IF EXISTS (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = N'FK_ProductosServiciosExistencias_ProductosServicios_EmpresaId'
    )
    BEGIN
        ALTER TABLE dbo.ProductosServiciosExistencias
        DROP CONSTRAINT FK_ProductosServiciosExistencias_ProductosServicios_EmpresaId;
    END;

    IF EXISTS (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = N'FK_ProductosServicios_Unidades_EmpresaId'
    )
    BEGIN
        ALTER TABLE dbo.ProductosServicios
        DROP CONSTRAINT FK_ProductosServicios_Unidades_EmpresaId;
    END;

    IF EXISTS (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = N'FK_ProductosServicios_Marcas_EmpresaId'
    )
    BEGIN
        ALTER TABLE dbo.ProductosServicios
        DROP CONSTRAINT FK_ProductosServicios_Marcas_EmpresaId;
    END;

    IF EXISTS (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = N'FK_ProductosServicios_Categorias_EmpresaId'
    )
    BEGIN
        ALTER TABLE dbo.ProductosServicios
        DROP CONSTRAINT FK_ProductosServicios_Categorias_EmpresaId;
    END;

    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosMovimientosInventario')
          AND name = N'IX_ProductosServiciosMovimientos_Empresa_FechaMovimiento'
    )
    BEGIN
        DROP INDEX IX_ProductosServiciosMovimientos_Empresa_FechaMovimiento
            ON dbo.ProductosServiciosMovimientosInventario;
    END;

    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosMovimientosInventario')
          AND name = N'IX_ProductosServiciosMovimientos_Empresa_ProductoServicio_FechaMovimiento'
    )
    BEGIN
        DROP INDEX IX_ProductosServiciosMovimientos_Empresa_ProductoServicio_FechaMovimiento
            ON dbo.ProductosServiciosMovimientosInventario;
    END;

    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosExistencias')
          AND name = N'UX_ProductosServiciosExistencias_Empresa_ProductoServicio'
    )
    BEGIN
        DROP INDEX UX_ProductosServiciosExistencias_Empresa_ProductoServicio
            ON dbo.ProductosServiciosExistencias;
    END;

    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.ProductosServicios')
          AND name = N'IX_ProductosServicios_Empresa_Tag'
    )
    BEGIN
        DROP INDEX IX_ProductosServicios_Empresa_Tag
            ON dbo.ProductosServicios;
    END;

    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.ProductosServicios')
          AND name = N'IX_ProductosServicios_Empresa_Unidad_Activo'
    )
    BEGIN
        DROP INDEX IX_ProductosServicios_Empresa_Unidad_Activo
            ON dbo.ProductosServicios;
    END;

    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.ProductosServicios')
          AND name = N'IX_ProductosServicios_Empresa_Marca_Activo'
    )
    BEGIN
        DROP INDEX IX_ProductosServicios_Empresa_Marca_Activo
            ON dbo.ProductosServicios;
    END;

    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.ProductosServicios')
          AND name = N'IX_ProductosServicios_Empresa_Categoria_Activo'
    )
    BEGIN
        DROP INDEX IX_ProductosServicios_Empresa_Categoria_Activo
            ON dbo.ProductosServicios;
    END;

    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.ProductosServicios')
          AND name = N'IX_ProductosServicios_Empresa_Tipo_Activo'
    )
    BEGIN
        DROP INDEX IX_ProductosServicios_Empresa_Tipo_Activo
            ON dbo.ProductosServicios;
    END;

    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.ProductosServicios')
          AND name = N'UX_ProductosServicios_Empresa_Id'
    )
    BEGIN
        DROP INDEX UX_ProductosServicios_Empresa_Id
            ON dbo.ProductosServicios;
    END;

    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.ProductosServicios')
          AND name = N'UX_ProductosServicios_Empresa_Codigo'
    )
    BEGIN
        DROP INDEX UX_ProductosServicios_Empresa_Codigo
            ON dbo.ProductosServicios;
    END;

    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosUnidadesMedida')
          AND name = N'IX_ProductosServiciosUnidadesMedida_Empresa_Activo'
    )
    BEGIN
        DROP INDEX IX_ProductosServiciosUnidadesMedida_Empresa_Activo
            ON dbo.ProductosServiciosUnidadesMedida;
    END;

    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosUnidadesMedida')
          AND name = N'IX_ProductosServiciosUnidadesMedida_Empresa_Nombre'
    )
    BEGIN
        DROP INDEX IX_ProductosServiciosUnidadesMedida_Empresa_Nombre
            ON dbo.ProductosServiciosUnidadesMedida;
    END;

    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosUnidadesMedida')
          AND name = N'UX_ProductosServiciosUnidadesMedida_Empresa_Id'
    )
    BEGIN
        DROP INDEX UX_ProductosServiciosUnidadesMedida_Empresa_Id
            ON dbo.ProductosServiciosUnidadesMedida;
    END;

    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosUnidadesMedida')
          AND name = N'UX_ProductosServiciosUnidadesMedida_Empresa_Codigo'
    )
    BEGIN
        DROP INDEX UX_ProductosServiciosUnidadesMedida_Empresa_Codigo
            ON dbo.ProductosServiciosUnidadesMedida;
    END;

    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosMarcas')
          AND name = N'IX_ProductosServiciosMarcas_Empresa_Activo'
    )
    BEGIN
        DROP INDEX IX_ProductosServiciosMarcas_Empresa_Activo
            ON dbo.ProductosServiciosMarcas;
    END;

    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosMarcas')
          AND name = N'IX_ProductosServiciosMarcas_Empresa_Nombre'
    )
    BEGIN
        DROP INDEX IX_ProductosServiciosMarcas_Empresa_Nombre
            ON dbo.ProductosServiciosMarcas;
    END;

    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosMarcas')
          AND name = N'UX_ProductosServiciosMarcas_Empresa_Id'
    )
    BEGIN
        DROP INDEX UX_ProductosServiciosMarcas_Empresa_Id
            ON dbo.ProductosServiciosMarcas;
    END;

    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosMarcas')
          AND name = N'UX_ProductosServiciosMarcas_Empresa_Codigo'
    )
    BEGIN
        DROP INDEX UX_ProductosServiciosMarcas_Empresa_Codigo
            ON dbo.ProductosServiciosMarcas;
    END;

    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosCategorias')
          AND name = N'IX_ProductosServiciosCategorias_Empresa_Activo'
    )
    BEGIN
        DROP INDEX IX_ProductosServiciosCategorias_Empresa_Activo
            ON dbo.ProductosServiciosCategorias;
    END;

    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosCategorias')
          AND name = N'IX_ProductosServiciosCategorias_Empresa_Nombre'
    )
    BEGIN
        DROP INDEX IX_ProductosServiciosCategorias_Empresa_Nombre
            ON dbo.ProductosServiciosCategorias;
    END;

    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosCategorias')
          AND name = N'UX_ProductosServiciosCategorias_Empresa_Id'
    )
    BEGIN
        DROP INDEX UX_ProductosServiciosCategorias_Empresa_Id
            ON dbo.ProductosServiciosCategorias;
    END;

    IF EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosCategorias')
          AND name = N'UX_ProductosServiciosCategorias_Empresa_Codigo'
    )
    BEGIN
        DROP INDEX UX_ProductosServiciosCategorias_Empresa_Codigo
            ON dbo.ProductosServiciosCategorias;
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosMovimientosInventario', N'U') IS NOT NULL
    BEGIN
        DROP TABLE dbo.ProductosServiciosMovimientosInventario;
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosExistencias', N'U') IS NOT NULL
    BEGIN
        DROP TABLE dbo.ProductosServiciosExistencias;
    END;

    IF OBJECT_ID(N'dbo.ProductosServicios', N'U') IS NOT NULL
    BEGIN
        DROP TABLE dbo.ProductosServicios;
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosUnidadesMedida', N'U') IS NOT NULL
    BEGIN
        DROP TABLE dbo.ProductosServiciosUnidadesMedida;
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosMarcas', N'U') IS NOT NULL
    BEGIN
        DROP TABLE dbo.ProductosServiciosMarcas;
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosCategorias', N'U') IS NOT NULL
    BEGIN
        DROP TABLE dbo.ProductosServiciosCategorias;
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
