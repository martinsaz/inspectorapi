/*
    MODULO: Productos y servicios
    FASE: Modelo de datos
    SCRIPT: UP
    ADVERTENCIA:
    - No ejecutar automaticamente.
    - No inserta datos semilla.
    - No altera tablas existentes fuera del modulo.
*/

SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.ProductosServiciosCategorias', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.ProductosServiciosCategorias
        (
            id UNIQUEIDENTIFIER NOT NULL
                CONSTRAINT PK_ProductosServiciosCategorias PRIMARY KEY CLUSTERED
                CONSTRAINT DF_ProductosServiciosCategorias_id DEFAULT (NEWID()),
            idEmpresa UNIQUEIDENTIFIER NOT NULL,
            identityKey UNIQUEIDENTIFIER NOT NULL
                CONSTRAINT DF_ProductosServiciosCategorias_identityKey DEFAULT (NEWID()),
            Codigo NVARCHAR(50) NOT NULL,
            Nombre NVARCHAR(150) NOT NULL,
            Descripcion NVARCHAR(500) NULL,
            AplicaA TINYINT NOT NULL
                CONSTRAINT DF_ProductosServiciosCategorias_AplicaA DEFAULT ((0)),
            Activo BIT NOT NULL
                CONSTRAINT DF_ProductosServiciosCategorias_Activo DEFAULT ((1)),
            FechaCreacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_ProductosServiciosCategorias_FechaCreacion DEFAULT (SYSUTCDATETIME()),
            FechaActualizacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_ProductosServiciosCategorias_FechaActualizacion DEFAULT (SYSUTCDATETIME()),
            FechaArchivado DATETIME2(0) NULL,
            CONSTRAINT CK_ProductosServiciosCategorias_AplicaA
                CHECK (AplicaA IN (0, 1, 2))
        );
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosMarcas', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.ProductosServiciosMarcas
        (
            id UNIQUEIDENTIFIER NOT NULL
                CONSTRAINT PK_ProductosServiciosMarcas PRIMARY KEY CLUSTERED
                CONSTRAINT DF_ProductosServiciosMarcas_id DEFAULT (NEWID()),
            idEmpresa UNIQUEIDENTIFIER NOT NULL,
            identityKey UNIQUEIDENTIFIER NOT NULL
                CONSTRAINT DF_ProductosServiciosMarcas_identityKey DEFAULT (NEWID()),
            Codigo NVARCHAR(50) NOT NULL,
            Nombre NVARCHAR(150) NOT NULL,
            Descripcion NVARCHAR(500) NULL,
            Activo BIT NOT NULL
                CONSTRAINT DF_ProductosServiciosMarcas_Activo DEFAULT ((1)),
            FechaCreacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_ProductosServiciosMarcas_FechaCreacion DEFAULT (SYSUTCDATETIME()),
            FechaActualizacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_ProductosServiciosMarcas_FechaActualizacion DEFAULT (SYSUTCDATETIME()),
            FechaArchivado DATETIME2(0) NULL
        );
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosUnidadesMedida', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.ProductosServiciosUnidadesMedida
        (
            id UNIQUEIDENTIFIER NOT NULL
                CONSTRAINT PK_ProductosServiciosUnidadesMedida PRIMARY KEY CLUSTERED
                CONSTRAINT DF_ProductosServiciosUnidadesMedida_id DEFAULT (NEWID()),
            idEmpresa UNIQUEIDENTIFIER NOT NULL,
            identityKey UNIQUEIDENTIFIER NOT NULL
                CONSTRAINT DF_ProductosServiciosUnidadesMedida_identityKey DEFAULT (NEWID()),
            Codigo NVARCHAR(30) NOT NULL,
            Nombre NVARCHAR(100) NOT NULL,
            Abreviatura NVARCHAR(20) NOT NULL,
            PermiteDecimales BIT NOT NULL
                CONSTRAINT DF_ProductosServiciosUnidadesMedida_PermiteDecimales DEFAULT ((0)),
            Activo BIT NOT NULL
                CONSTRAINT DF_ProductosServiciosUnidadesMedida_Activo DEFAULT ((1)),
            FechaCreacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_ProductosServiciosUnidadesMedida_FechaCreacion DEFAULT (SYSUTCDATETIME()),
            FechaActualizacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_ProductosServiciosUnidadesMedida_FechaActualizacion DEFAULT (SYSUTCDATETIME()),
            FechaArchivado DATETIME2(0) NULL
        );
    END;

    IF OBJECT_ID(N'dbo.ProductosServicios', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.ProductosServicios
        (
            id UNIQUEIDENTIFIER NOT NULL
                CONSTRAINT PK_ProductosServicios PRIMARY KEY CLUSTERED
                CONSTRAINT DF_ProductosServicios_id DEFAULT (NEWID()),
            idEmpresa UNIQUEIDENTIFIER NOT NULL,
            identityKey UNIQUEIDENTIFIER NOT NULL
                CONSTRAINT DF_ProductosServicios_identityKey DEFAULT (NEWID()),
            Tipo TINYINT NOT NULL,
            Codigo NVARCHAR(50) NOT NULL,
            Tag NVARCHAR(100) NULL,
            Nombre NVARCHAR(150) NOT NULL,
            Descripcion NVARCHAR(1000) NULL,
            idCategoria UNIQUEIDENTIFIER NOT NULL,
            idMarca UNIQUEIDENTIFIER NULL,
            idUnidadMedida UNIQUEIDENTIFIER NOT NULL,
            Costo DECIMAL(18, 2) NULL,
            PrecioPublico DECIMAL(18, 2) NOT NULL,
            CausaInventario BIT NOT NULL
                CONSTRAINT DF_ProductosServicios_CausaInventario DEFAULT ((0)),
            PermiteVentaSinExistencia BIT NOT NULL
                CONSTRAINT DF_ProductosServicios_PermiteVentaSinExistencia DEFAULT ((0)),
            ImagenUrl NVARCHAR(1000) NULL,
            ImagenNombre NVARCHAR(255) NULL,
            Activo BIT NOT NULL
                CONSTRAINT DF_ProductosServicios_Activo DEFAULT ((1)),
            FechaCreacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_ProductosServicios_FechaCreacion DEFAULT (SYSUTCDATETIME()),
            FechaActualizacion DATETIME2(0) NULL,
            FechaArchivado DATETIME2(0) NULL,
            CONSTRAINT CK_ProductosServicios_Tipo
                CHECK (Tipo IN (1, 2)),
            CONSTRAINT CK_ProductosServicios_ValoresMonetarios
                CHECK (
                    PrecioPublico >= 0
                    AND (Costo IS NULL OR Costo >= 0)
                ),
            CONSTRAINT CK_ProductosServicios_ServicioSinInventario
                CHECK (
                    Tipo = 1
                    OR (
                        idMarca IS NULL
                        AND CausaInventario = 0
                        AND PermiteVentaSinExistencia = 0
                    )
                )
        );
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosExistencias', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.ProductosServiciosExistencias
        (
            id UNIQUEIDENTIFIER NOT NULL
                CONSTRAINT PK_ProductosServiciosExistencias PRIMARY KEY CLUSTERED
                CONSTRAINT DF_ProductosServiciosExistencias_id DEFAULT (NEWID()),
            idEmpresa UNIQUEIDENTIFIER NOT NULL,
            identityKey UNIQUEIDENTIFIER NOT NULL
                CONSTRAINT DF_ProductosServiciosExistencias_identityKey DEFAULT (NEWID()),
            idProductoServicio UNIQUEIDENTIFIER NOT NULL,
            ExistenciaActual DECIMAL(18, 4) NOT NULL
                CONSTRAINT DF_ProductosServiciosExistencias_ExistenciaActual DEFAULT ((0)),
            ExistenciaMinima DECIMAL(18, 4) NOT NULL
                CONSTRAINT DF_ProductosServiciosExistencias_ExistenciaMinima DEFAULT ((0)),
            CostoPromedio DECIMAL(18, 2) NULL,
            FechaCreacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_ProductosServiciosExistencias_FechaCreacion DEFAULT (SYSUTCDATETIME()),
            FechaActualizacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_ProductosServiciosExistencias_FechaActualizacion DEFAULT (SYSUTCDATETIME()),
            CONSTRAINT CK_ProductosServiciosExistencias_Valores
                CHECK (
                    ExistenciaMinima >= 0
                    AND (CostoPromedio IS NULL OR CostoPromedio >= 0)
                )
        );
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosMovimientosInventario', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.ProductosServiciosMovimientosInventario
        (
            id UNIQUEIDENTIFIER NOT NULL
                CONSTRAINT PK_ProductosServiciosMovimientosInventario PRIMARY KEY CLUSTERED
                CONSTRAINT DF_ProductosServiciosMovimientosInventario_id DEFAULT (NEWID()),
            idEmpresa UNIQUEIDENTIFIER NOT NULL,
            identityKey UNIQUEIDENTIFIER NOT NULL
                CONSTRAINT DF_ProductosServiciosMovimientosInventario_identityKey DEFAULT (NEWID()),
            idProductoServicio UNIQUEIDENTIFIER NOT NULL,
            TipoMovimiento TINYINT NOT NULL,
            Cantidad DECIMAL(18, 4) NOT NULL,
            ExistenciaAnterior DECIMAL(18, 4) NOT NULL,
            ExistenciaPosterior DECIMAL(18, 4) NOT NULL,
            CostoUnitario DECIMAL(18, 2) NULL,
            Referencia NVARCHAR(150) NULL,
            Observaciones NVARCHAR(1000) NULL,
            idUsuario UNIQUEIDENTIFIER NULL,
            FechaMovimiento DATETIME2(0) NOT NULL
                CONSTRAINT DF_ProductosServiciosMovimientosInventario_FechaMovimiento DEFAULT (SYSUTCDATETIME()),
            CONSTRAINT CK_ProductosServiciosMovimientos_Tipo
                CHECK (TipoMovimiento IN (1, 2, 3, 4, 5)),
            CONSTRAINT CK_ProductosServiciosMovimientos_Cantidad
                CHECK (Cantidad > 0),
            CONSTRAINT CK_ProductosServiciosMovimientos_ValoresMonetarios
                CHECK (CostoUnitario IS NULL OR CostoUnitario >= 0)
        );
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosCategorias', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosCategorias')
              AND name = N'UX_ProductosServiciosCategorias_Empresa_Codigo'
       )
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UX_ProductosServiciosCategorias_Empresa_Codigo
            ON dbo.ProductosServiciosCategorias (idEmpresa, Codigo);
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosCategorias', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosCategorias')
              AND name = N'UX_ProductosServiciosCategorias_Empresa_Id'
       )
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UX_ProductosServiciosCategorias_Empresa_Id
            ON dbo.ProductosServiciosCategorias (idEmpresa, id);
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosCategorias', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosCategorias')
              AND name = N'IX_ProductosServiciosCategorias_Empresa_Nombre'
       )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_ProductosServiciosCategorias_Empresa_Nombre
            ON dbo.ProductosServiciosCategorias (idEmpresa, Nombre);
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosCategorias', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosCategorias')
              AND name = N'IX_ProductosServiciosCategorias_Empresa_Activo'
       )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_ProductosServiciosCategorias_Empresa_Activo
            ON dbo.ProductosServiciosCategorias (idEmpresa, Activo);
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosMarcas', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosMarcas')
              AND name = N'UX_ProductosServiciosMarcas_Empresa_Codigo'
       )
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UX_ProductosServiciosMarcas_Empresa_Codigo
            ON dbo.ProductosServiciosMarcas (idEmpresa, Codigo);
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosMarcas', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosMarcas')
              AND name = N'UX_ProductosServiciosMarcas_Empresa_Id'
       )
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UX_ProductosServiciosMarcas_Empresa_Id
            ON dbo.ProductosServiciosMarcas (idEmpresa, id);
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosMarcas', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosMarcas')
              AND name = N'IX_ProductosServiciosMarcas_Empresa_Nombre'
       )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_ProductosServiciosMarcas_Empresa_Nombre
            ON dbo.ProductosServiciosMarcas (idEmpresa, Nombre);
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosMarcas', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosMarcas')
              AND name = N'IX_ProductosServiciosMarcas_Empresa_Activo'
       )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_ProductosServiciosMarcas_Empresa_Activo
            ON dbo.ProductosServiciosMarcas (idEmpresa, Activo);
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosUnidadesMedida', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosUnidadesMedida')
              AND name = N'UX_ProductosServiciosUnidadesMedida_Empresa_Codigo'
       )
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UX_ProductosServiciosUnidadesMedida_Empresa_Codigo
            ON dbo.ProductosServiciosUnidadesMedida (idEmpresa, Codigo);
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosUnidadesMedida', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosUnidadesMedida')
              AND name = N'UX_ProductosServiciosUnidadesMedida_Empresa_Id'
       )
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UX_ProductosServiciosUnidadesMedida_Empresa_Id
            ON dbo.ProductosServiciosUnidadesMedida (idEmpresa, id);
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosUnidadesMedida', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosUnidadesMedida')
              AND name = N'IX_ProductosServiciosUnidadesMedida_Empresa_Nombre'
       )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_ProductosServiciosUnidadesMedida_Empresa_Nombre
            ON dbo.ProductosServiciosUnidadesMedida (idEmpresa, Nombre);
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosUnidadesMedida', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosUnidadesMedida')
              AND name = N'IX_ProductosServiciosUnidadesMedida_Empresa_Activo'
       )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_ProductosServiciosUnidadesMedida_Empresa_Activo
            ON dbo.ProductosServiciosUnidadesMedida (idEmpresa, Activo);
    END;

    IF OBJECT_ID(N'dbo.ProductosServicios', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.ProductosServicios')
              AND name = N'UX_ProductosServicios_Empresa_Codigo'
       )
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UX_ProductosServicios_Empresa_Codigo
            ON dbo.ProductosServicios (idEmpresa, Codigo);
    END;

    IF OBJECT_ID(N'dbo.ProductosServicios', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.ProductosServicios')
              AND name = N'UX_ProductosServicios_Empresa_Id'
       )
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UX_ProductosServicios_Empresa_Id
            ON dbo.ProductosServicios (idEmpresa, id);
    END;

    IF OBJECT_ID(N'dbo.ProductosServicios', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.ProductosServicios')
              AND name = N'IX_ProductosServicios_Empresa_Tipo_Activo'
       )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_ProductosServicios_Empresa_Tipo_Activo
            ON dbo.ProductosServicios (idEmpresa, Tipo, Activo);
    END;

    IF OBJECT_ID(N'dbo.ProductosServicios', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.ProductosServicios')
              AND name = N'IX_ProductosServicios_Empresa_Categoria_Activo'
       )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_ProductosServicios_Empresa_Categoria_Activo
            ON dbo.ProductosServicios (idEmpresa, idCategoria, Activo);
    END;

    IF OBJECT_ID(N'dbo.ProductosServicios', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.ProductosServicios')
              AND name = N'IX_ProductosServicios_Empresa_Marca_Activo'
       )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_ProductosServicios_Empresa_Marca_Activo
            ON dbo.ProductosServicios (idEmpresa, idMarca, Activo);
    END;

    IF OBJECT_ID(N'dbo.ProductosServicios', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.ProductosServicios')
              AND name = N'IX_ProductosServicios_Empresa_Unidad_Activo'
       )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_ProductosServicios_Empresa_Unidad_Activo
            ON dbo.ProductosServicios (idEmpresa, idUnidadMedida, Activo);
    END;

    IF OBJECT_ID(N'dbo.ProductosServicios', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.ProductosServicios')
              AND name = N'IX_ProductosServicios_Empresa_Tag'
       )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_ProductosServicios_Empresa_Tag
            ON dbo.ProductosServicios (idEmpresa, Tag);
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosExistencias', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosExistencias')
              AND name = N'UX_ProductosServiciosExistencias_Empresa_ProductoServicio'
       )
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UX_ProductosServiciosExistencias_Empresa_ProductoServicio
            ON dbo.ProductosServiciosExistencias (idEmpresa, idProductoServicio);
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosMovimientosInventario', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosMovimientosInventario')
              AND name = N'IX_ProductosServiciosMovimientos_Empresa_ProductoServicio_FechaMovimiento'
       )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_ProductosServiciosMovimientos_Empresa_ProductoServicio_FechaMovimiento
            ON dbo.ProductosServiciosMovimientosInventario (idEmpresa, idProductoServicio, FechaMovimiento);
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosMovimientosInventario', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosMovimientosInventario')
              AND name = N'IX_ProductosServiciosMovimientos_Empresa_FechaMovimiento'
       )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_ProductosServiciosMovimientos_Empresa_FechaMovimiento
            ON dbo.ProductosServiciosMovimientosInventario (idEmpresa, FechaMovimiento);
    END;

    IF OBJECT_ID(N'dbo.ProductosServicios', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.foreign_keys
            WHERE name = N'FK_ProductosServicios_Categorias_EmpresaId'
       )
    BEGIN
        ALTER TABLE dbo.ProductosServicios WITH CHECK
        ADD CONSTRAINT FK_ProductosServicios_Categorias_EmpresaId
            FOREIGN KEY (idEmpresa, idCategoria)
            REFERENCES dbo.ProductosServiciosCategorias (idEmpresa, id);
    END;

    IF OBJECT_ID(N'dbo.ProductosServicios', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.foreign_keys
            WHERE name = N'FK_ProductosServicios_Marcas_EmpresaId'
       )
    BEGIN
        ALTER TABLE dbo.ProductosServicios WITH CHECK
        ADD CONSTRAINT FK_ProductosServicios_Marcas_EmpresaId
            FOREIGN KEY (idEmpresa, idMarca)
            REFERENCES dbo.ProductosServiciosMarcas (idEmpresa, id);
    END;

    IF OBJECT_ID(N'dbo.ProductosServicios', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.foreign_keys
            WHERE name = N'FK_ProductosServicios_Unidades_EmpresaId'
       )
    BEGIN
        ALTER TABLE dbo.ProductosServicios WITH CHECK
        ADD CONSTRAINT FK_ProductosServicios_Unidades_EmpresaId
            FOREIGN KEY (idEmpresa, idUnidadMedida)
            REFERENCES dbo.ProductosServiciosUnidadesMedida (idEmpresa, id);
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosExistencias', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.foreign_keys
            WHERE name = N'FK_ProductosServiciosExistencias_ProductosServicios_EmpresaId'
       )
    BEGIN
        ALTER TABLE dbo.ProductosServiciosExistencias WITH CHECK
        ADD CONSTRAINT FK_ProductosServiciosExistencias_ProductosServicios_EmpresaId
            FOREIGN KEY (idEmpresa, idProductoServicio)
            REFERENCES dbo.ProductosServicios (idEmpresa, id);
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosMovimientosInventario', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1
            FROM sys.foreign_keys
            WHERE name = N'FK_ProductosServiciosMovimientos_ProductosServicios_EmpresaId'
       )
    BEGIN
        ALTER TABLE dbo.ProductosServiciosMovimientosInventario WITH CHECK
        ADD CONSTRAINT FK_ProductosServiciosMovimientos_ProductosServicios_EmpresaId
            FOREIGN KEY (idEmpresa, idProductoServicio)
            REFERENCES dbo.ProductosServicios (idEmpresa, id);
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
