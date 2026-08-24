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

    IF COL_LENGTH('dbo.ProductosServicios', 'PrecioComparacion') IS NULL
    BEGIN
        ALTER TABLE dbo.ProductosServicios
        ADD PrecioComparacion DECIMAL(18, 2) NULL;
    END;

    IF COL_LENGTH('dbo.ProductosServicios', 'PrecioUnitarioMonto') IS NULL
    BEGIN
        ALTER TABLE dbo.ProductosServicios
        ADD PrecioUnitarioMonto DECIMAL(18, 6) NULL;
    END;

    IF COL_LENGTH('dbo.ProductosServicios', 'PrecioUnitarioBaseCantidad') IS NULL
    BEGIN
        ALTER TABLE dbo.ProductosServicios
        ADD PrecioUnitarioBaseCantidad DECIMAL(18, 6) NULL;
    END;

    IF COL_LENGTH('dbo.ProductosServicios', 'PrecioUnitarioUnidad') IS NULL
    BEGIN
        ALTER TABLE dbo.ProductosServicios
        ADD PrecioUnitarioUnidad NVARCHAR(20) NULL;
    END;

    IF COL_LENGTH('dbo.ProductosServicios', 'ObjetoImpuesto') IS NULL
    BEGIN
        ALTER TABLE dbo.ProductosServicios
        ADD ObjetoImpuesto NVARCHAR(4) NULL;
    END;

    IF COL_LENGTH('dbo.ProductosServicios', 'ClaveProductoSat') IS NULL
    BEGIN
        ALTER TABLE dbo.ProductosServicios
        ADD ClaveProductoSat NVARCHAR(20) NULL;
    END;

    IF COL_LENGTH('dbo.ProductosServicios', 'ClaveUnidadSat') IS NULL
    BEGIN
        ALTER TABLE dbo.ProductosServicios
        ADD ClaveUnidadSat NVARCHAR(10) NULL;
    END;

    IF COL_LENGTH('dbo.ProductosServicios', 'EsProductoFisico') IS NULL
    BEGIN
        ALTER TABLE dbo.ProductosServicios
        ADD EsProductoFisico BIT NOT NULL
            CONSTRAINT DF_ProductosServicios_EsProductoFisico DEFAULT ((0));
    END;

    IF COL_LENGTH('dbo.ProductosServicios', 'PesoKg') IS NULL
    BEGIN
        ALTER TABLE dbo.ProductosServicios
        ADD PesoKg DECIMAL(18, 5) NULL;
    END;

    IF COL_LENGTH('dbo.ProductosServicios', 'LargoCm') IS NULL
    BEGIN
        ALTER TABLE dbo.ProductosServicios
        ADD LargoCm DECIMAL(18, 2) NULL;
    END;

    IF COL_LENGTH('dbo.ProductosServicios', 'AnchoCm') IS NULL
    BEGIN
        ALTER TABLE dbo.ProductosServicios
        ADD AnchoCm DECIMAL(18, 2) NULL;
    END;

    IF COL_LENGTH('dbo.ProductosServicios', 'AltoCm') IS NULL
    BEGIN
        ALTER TABLE dbo.ProductosServicios
        ADD AltoCm DECIMAL(18, 2) NULL;
    END;

    IF COL_LENGTH('dbo.ProductosServicios', 'UsaNumeroSerie') IS NULL
    BEGIN
        ALTER TABLE dbo.ProductosServicios
        ADD UsaNumeroSerie BIT NOT NULL
            CONSTRAINT DF_ProductosServicios_UsaNumeroSerie DEFAULT ((0));
    END;

    IF COL_LENGTH('dbo.ProductosServicios', 'idColeccion') IS NULL
    BEGIN
        ALTER TABLE dbo.ProductosServicios
        ADD idColeccion UNIQUEIDENTIFIER NULL;
    END;

    IF COL_LENGTH('dbo.ProductosServicios', 'idPaquete') IS NULL
    BEGIN
        ALTER TABLE dbo.ProductosServicios
        ADD idPaquete UNIQUEIDENTIFIER NULL;
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosColecciones', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.ProductosServiciosColecciones
        (
            id UNIQUEIDENTIFIER NOT NULL
                CONSTRAINT PK_ProductosServiciosColecciones PRIMARY KEY CLUSTERED
                CONSTRAINT DF_ProductosServiciosColecciones_id DEFAULT (NEWID()),
            idEmpresa UNIQUEIDENTIFIER NOT NULL,
            identityKey UNIQUEIDENTIFIER NOT NULL
                CONSTRAINT DF_ProductosServiciosColecciones_identityKey DEFAULT (NEWID()),
            Numero NVARCHAR(50) NOT NULL,
            Nombre NVARCHAR(150) NOT NULL,
            Descripcion NVARCHAR(500) NULL,
            Activo BIT NOT NULL
                CONSTRAINT DF_ProductosServiciosColecciones_Activo DEFAULT ((1)),
            FechaCreacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_ProductosServiciosColecciones_FechaCreacion DEFAULT (SYSUTCDATETIME()),
            FechaActualizacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_ProductosServiciosColecciones_FechaActualizacion DEFAULT (SYSUTCDATETIME()),
            FechaArchivado DATETIME2(0) NULL
        );
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosPaquetes', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.ProductosServiciosPaquetes
        (
            id UNIQUEIDENTIFIER NOT NULL
                CONSTRAINT PK_ProductosServiciosPaquetes PRIMARY KEY CLUSTERED
                CONSTRAINT DF_ProductosServiciosPaquetes_id DEFAULT (NEWID()),
            idEmpresa UNIQUEIDENTIFIER NOT NULL,
            identityKey UNIQUEIDENTIFIER NOT NULL
                CONSTRAINT DF_ProductosServiciosPaquetes_identityKey DEFAULT (NEWID()),
            Nombre NVARCHAR(150) NOT NULL,
            TipoPaquete NVARCHAR(30) NOT NULL,
            LargoCm DECIMAL(18, 2) NULL,
            AnchoCm DECIMAL(18, 2) NULL,
            AltoCm DECIMAL(18, 2) NULL,
            PesoEmpaqueVacioKg DECIMAL(18, 5) NULL,
            EsPredeterminado BIT NOT NULL
                CONSTRAINT DF_ProductosServiciosPaquetes_EsPredeterminado DEFAULT ((0)),
            Activo BIT NOT NULL
                CONSTRAINT DF_ProductosServiciosPaquetes_Activo DEFAULT ((1)),
            FechaCreacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_ProductosServiciosPaquetes_FechaCreacion DEFAULT (SYSUTCDATETIME()),
            FechaActualizacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_ProductosServiciosPaquetes_FechaActualizacion DEFAULT (SYSUTCDATETIME()),
            FechaArchivado DATETIME2(0) NULL,
            CONSTRAINT CK_ProductosServiciosPaquetes_Tipo
                CHECK (TipoPaquete IN (N'caja', N'sobre', N'flexible'))
        );
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosAtributos', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.ProductosServiciosAtributos
        (
            id UNIQUEIDENTIFIER NOT NULL
                CONSTRAINT PK_ProductosServiciosAtributos PRIMARY KEY CLUSTERED
                CONSTRAINT DF_ProductosServiciosAtributos_id DEFAULT (NEWID()),
            idEmpresa UNIQUEIDENTIFIER NOT NULL,
            identityKey UNIQUEIDENTIFIER NOT NULL
                CONSTRAINT DF_ProductosServiciosAtributos_identityKey DEFAULT (NEWID()),
            Nombre NVARCHAR(100) NOT NULL,
            Activo BIT NOT NULL
                CONSTRAINT DF_ProductosServiciosAtributos_Activo DEFAULT ((1)),
            FechaCreacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_ProductosServiciosAtributos_FechaCreacion DEFAULT (SYSUTCDATETIME()),
            FechaActualizacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_ProductosServiciosAtributos_FechaActualizacion DEFAULT (SYSUTCDATETIME()),
            FechaArchivado DATETIME2(0) NULL
        );
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosAtributosValores', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.ProductosServiciosAtributosValores
        (
            id UNIQUEIDENTIFIER NOT NULL
                CONSTRAINT PK_ProductosServiciosAtributosValores PRIMARY KEY CLUSTERED
                CONSTRAINT DF_ProductosServiciosAtributosValores_id DEFAULT (NEWID()),
            idEmpresa UNIQUEIDENTIFIER NOT NULL,
            identityKey UNIQUEIDENTIFIER NOT NULL
                CONSTRAINT DF_ProductosServiciosAtributosValores_identityKey DEFAULT (NEWID()),
            idAtributo UNIQUEIDENTIFIER NOT NULL,
            Valor NVARCHAR(100) NOT NULL,
            Orden INT NOT NULL
                CONSTRAINT DF_ProductosServiciosAtributosValores_Orden DEFAULT ((1)),
            Activo BIT NOT NULL
                CONSTRAINT DF_ProductosServiciosAtributosValores_Activo DEFAULT ((1)),
            FechaCreacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_ProductosServiciosAtributosValores_FechaCreacion DEFAULT (SYSUTCDATETIME()),
            FechaActualizacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_ProductosServiciosAtributosValores_FechaActualizacion DEFAULT (SYSUTCDATETIME()),
            FechaArchivado DATETIME2(0) NULL
        );
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosProductoAtributos', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.ProductosServiciosProductoAtributos
        (
            id UNIQUEIDENTIFIER NOT NULL
                CONSTRAINT PK_ProductosServiciosProductoAtributos PRIMARY KEY CLUSTERED
                CONSTRAINT DF_ProductosServiciosProductoAtributos_id DEFAULT (NEWID()),
            idEmpresa UNIQUEIDENTIFIER NOT NULL,
            identityKey UNIQUEIDENTIFIER NOT NULL
                CONSTRAINT DF_ProductosServiciosProductoAtributos_identityKey DEFAULT (NEWID()),
            idProductoServicio UNIQUEIDENTIFIER NOT NULL,
            idAtributo UNIQUEIDENTIFIER NOT NULL,
            Orden INT NOT NULL
                CONSTRAINT DF_ProductosServiciosProductoAtributos_Orden DEFAULT ((1)),
            Activo BIT NOT NULL
                CONSTRAINT DF_ProductosServiciosProductoAtributos_Activo DEFAULT ((1)),
            FechaCreacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_ProductosServiciosProductoAtributos_FechaCreacion DEFAULT (SYSUTCDATETIME()),
            FechaActualizacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_ProductosServiciosProductoAtributos_FechaActualizacion DEFAULT (SYSUTCDATETIME())
        );
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosProductoAtributoValores', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.ProductosServiciosProductoAtributoValores
        (
            id UNIQUEIDENTIFIER NOT NULL
                CONSTRAINT PK_ProductosServiciosProductoAtributoValores PRIMARY KEY CLUSTERED
                CONSTRAINT DF_ProductosServiciosProductoAtributoValores_id DEFAULT (NEWID()),
            idEmpresa UNIQUEIDENTIFIER NOT NULL,
            identityKey UNIQUEIDENTIFIER NOT NULL
                CONSTRAINT DF_ProductosServiciosProductoAtributoValores_identityKey DEFAULT (NEWID()),
            idProductoAtributo UNIQUEIDENTIFIER NOT NULL,
            idAtributoValor UNIQUEIDENTIFIER NOT NULL,
            Orden INT NOT NULL
                CONSTRAINT DF_ProductosServiciosProductoAtributoValores_Orden DEFAULT ((1)),
            Activo BIT NOT NULL
                CONSTRAINT DF_ProductosServiciosProductoAtributoValores_Activo DEFAULT ((1)),
            FechaCreacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_ProductosServiciosProductoAtributoValores_FechaCreacion DEFAULT (SYSUTCDATETIME()),
            FechaActualizacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_ProductosServiciosProductoAtributoValores_FechaActualizacion DEFAULT (SYSUTCDATETIME())
        );
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosOpcionesVariante', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.ProductosServiciosOpcionesVariante
        (
            id UNIQUEIDENTIFIER NOT NULL
                CONSTRAINT PK_ProductosServiciosOpcionesVariante PRIMARY KEY CLUSTERED
                CONSTRAINT DF_ProductosServiciosOpcionesVariante_id DEFAULT (NEWID()),
            idEmpresa UNIQUEIDENTIFIER NOT NULL,
            identityKey UNIQUEIDENTIFIER NOT NULL
                CONSTRAINT DF_ProductosServiciosOpcionesVariante_identityKey DEFAULT (NEWID()),
            idProductoServicio UNIQUEIDENTIFIER NOT NULL,
            Nombre NVARCHAR(100) NOT NULL,
            Orden INT NOT NULL
                CONSTRAINT DF_ProductosServiciosOpcionesVariante_Orden DEFAULT ((1)),
            Activo BIT NOT NULL
                CONSTRAINT DF_ProductosServiciosOpcionesVariante_Activo DEFAULT ((1)),
            FechaCreacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_ProductosServiciosOpcionesVariante_FechaCreacion DEFAULT (SYSUTCDATETIME()),
            FechaActualizacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_ProductosServiciosOpcionesVariante_FechaActualizacion DEFAULT (SYSUTCDATETIME())
        );
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosOpcionesVarianteValores', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.ProductosServiciosOpcionesVarianteValores
        (
            id UNIQUEIDENTIFIER NOT NULL
                CONSTRAINT PK_ProductosServiciosOpcionesVarianteValores PRIMARY KEY CLUSTERED
                CONSTRAINT DF_ProductosServiciosOpcionesVarianteValores_id DEFAULT (NEWID()),
            idEmpresa UNIQUEIDENTIFIER NOT NULL,
            identityKey UNIQUEIDENTIFIER NOT NULL
                CONSTRAINT DF_ProductosServiciosOpcionesVarianteValores_identityKey DEFAULT (NEWID()),
            idOpcionVariante UNIQUEIDENTIFIER NOT NULL,
            Valor NVARCHAR(100) NOT NULL,
            Orden INT NOT NULL
                CONSTRAINT DF_ProductosServiciosOpcionesVarianteValores_Orden DEFAULT ((1)),
            Activo BIT NOT NULL
                CONSTRAINT DF_ProductosServiciosOpcionesVarianteValores_Activo DEFAULT ((1)),
            FechaCreacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_ProductosServiciosOpcionesVarianteValores_FechaCreacion DEFAULT (SYSUTCDATETIME()),
            FechaActualizacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_ProductosServiciosOpcionesVarianteValores_FechaActualizacion DEFAULT (SYSUTCDATETIME())
        );
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosVariantes', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.ProductosServiciosVariantes
        (
            id UNIQUEIDENTIFIER NOT NULL
                CONSTRAINT PK_ProductosServiciosVariantes PRIMARY KEY CLUSTERED
                CONSTRAINT DF_ProductosServiciosVariantes_id DEFAULT (NEWID()),
            idEmpresa UNIQUEIDENTIFIER NOT NULL,
            identityKey UNIQUEIDENTIFIER NOT NULL
                CONSTRAINT DF_ProductosServiciosVariantes_identityKey DEFAULT (NEWID()),
            idProductoServicio UNIQUEIDENTIFIER NOT NULL,
            Sku NVARCHAR(100) NULL,
            Nombre NVARCHAR(200) NOT NULL,
            ClaveCombinacion NVARCHAR(500) NOT NULL,
            ImagenUrl NVARCHAR(1000) NULL,
            ImagenNombre NVARCHAR(255) NULL,
            PrecioPublico DECIMAL(18, 2) NULL,
            PrecioComparacion DECIMAL(18, 2) NULL,
            PrecioUnitarioMonto DECIMAL(18, 6) NULL,
            PrecioUnitarioBaseCantidad DECIMAL(18, 6) NULL,
            PrecioUnitarioUnidad NVARCHAR(20) NULL,
            Orden INT NOT NULL
                CONSTRAINT DF_ProductosServiciosVariantes_Orden DEFAULT ((1)),
            Activo BIT NOT NULL
                CONSTRAINT DF_ProductosServiciosVariantes_Activo DEFAULT ((1)),
            FechaCreacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_ProductosServiciosVariantes_FechaCreacion DEFAULT (SYSUTCDATETIME()),
            FechaActualizacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_ProductosServiciosVariantes_FechaActualizacion DEFAULT (SYSUTCDATETIME())
        );
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosVariantes', N'U') IS NOT NULL
       AND COL_LENGTH(N'dbo.ProductosServiciosVariantes', N'ImagenUrl') IS NULL
    BEGIN
        ALTER TABLE dbo.ProductosServiciosVariantes
        ADD ImagenUrl NVARCHAR(1000) NULL;
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosVariantes', N'U') IS NOT NULL
       AND COL_LENGTH(N'dbo.ProductosServiciosVariantes', N'ImagenNombre') IS NULL
    BEGIN
        ALTER TABLE dbo.ProductosServiciosVariantes
        ADD ImagenNombre NVARCHAR(255) NULL;
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosVarianteValores', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.ProductosServiciosVarianteValores
        (
            id UNIQUEIDENTIFIER NOT NULL
                CONSTRAINT PK_ProductosServiciosVarianteValores PRIMARY KEY CLUSTERED
                CONSTRAINT DF_ProductosServiciosVarianteValores_id DEFAULT (NEWID()),
            idEmpresa UNIQUEIDENTIFIER NOT NULL,
            identityKey UNIQUEIDENTIFIER NOT NULL
                CONSTRAINT DF_ProductosServiciosVarianteValores_identityKey DEFAULT (NEWID()),
            idVariante UNIQUEIDENTIFIER NOT NULL,
            idAtributo UNIQUEIDENTIFIER NULL,
            idAtributoValor UNIQUEIDENTIFIER NULL,
            idOpcionVariante UNIQUEIDENTIFIER NULL,
            idOpcionVarianteValor UNIQUEIDENTIFIER NULL,
            Orden INT NOT NULL
                CONSTRAINT DF_ProductosServiciosVarianteValores_Orden DEFAULT ((1)),
            FechaCreacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_ProductosServiciosVarianteValores_FechaCreacion DEFAULT (SYSUTCDATETIME())
        );
    END;

    IF COL_LENGTH('dbo.ProductosServiciosVarianteValores', 'idOpcionVariante') IS NULL
    BEGIN
        ALTER TABLE dbo.ProductosServiciosVarianteValores
        ADD idOpcionVariante UNIQUEIDENTIFIER NULL;
    END;

    IF COL_LENGTH('dbo.ProductosServiciosVarianteValores', 'idOpcionVarianteValor') IS NULL
    BEGIN
        ALTER TABLE dbo.ProductosServiciosVarianteValores
        ADD idOpcionVarianteValor UNIQUEIDENTIFIER NULL;
    END;

    IF COL_LENGTH('dbo.ProductosServiciosVarianteValores', 'idAtributo') IS NOT NULL
    BEGIN
        ALTER TABLE dbo.ProductosServiciosVarianteValores
        ALTER COLUMN idAtributo UNIQUEIDENTIFIER NULL;
    END;

    IF COL_LENGTH('dbo.ProductosServiciosVarianteValores', 'idAtributoValor') IS NOT NULL
    BEGIN
        ALTER TABLE dbo.ProductosServiciosVarianteValores
        ALTER COLUMN idAtributoValor UNIQUEIDENTIFIER NULL;
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosMultimedia', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.ProductosServiciosMultimedia
        (
            id UNIQUEIDENTIFIER NOT NULL
                CONSTRAINT PK_ProductosServiciosMultimedia PRIMARY KEY CLUSTERED
                CONSTRAINT DF_ProductosServiciosMultimedia_id DEFAULT (NEWID()),
            idEmpresa UNIQUEIDENTIFIER NOT NULL,
            identityKey UNIQUEIDENTIFIER NOT NULL
                CONSTRAINT DF_ProductosServiciosMultimedia_identityKey DEFAULT (NEWID()),
            idProductoServicio UNIQUEIDENTIFIER NOT NULL,
            TipoMultimedia NVARCHAR(20) NOT NULL,
            Foto BIT NOT NULL
                CONSTRAINT DF_ProductosServiciosMultimedia_Foto DEFAULT ((0)),
            Video BIT NOT NULL
                CONSTRAINT DF_ProductosServiciosMultimedia_Video DEFAULT ((0)),
            Documento BIT NOT NULL
                CONSTRAINT DF_ProductosServiciosMultimedia_Documento DEFAULT ((0)),
            NombreOriginal NVARCHAR(255) NOT NULL,
            NombreAlmacenado NVARCHAR(255) NOT NULL,
            Extension NVARCHAR(20) NOT NULL,
            MimeType NVARCHAR(120) NOT NULL,
            UrlFirebase NVARCHAR(1000) NOT NULL,
            PesoBytes BIGINT NOT NULL,
            Orden INT NOT NULL
                CONSTRAINT DF_ProductosServiciosMultimedia_Orden DEFAULT ((1)),
            Activo BIT NOT NULL
                CONSTRAINT DF_ProductosServiciosMultimedia_Activo DEFAULT ((1)),
            FechaCreacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_ProductosServiciosMultimedia_FechaCreacion DEFAULT (SYSUTCDATETIME()),
            FechaActualizacion DATETIME2(0) NOT NULL
                CONSTRAINT DF_ProductosServiciosMultimedia_FechaActualizacion DEFAULT (SYSUTCDATETIME())
        );
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosColecciones', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosColecciones')
              AND name = N'UX_ProductosServiciosColecciones_Empresa_Id'
       )
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UX_ProductosServiciosColecciones_Empresa_Id
            ON dbo.ProductosServiciosColecciones (idEmpresa, id);
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosColecciones', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosColecciones')
              AND name = N'UX_ProductosServiciosColecciones_Empresa_Numero'
       )
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UX_ProductosServiciosColecciones_Empresa_Numero
            ON dbo.ProductosServiciosColecciones (idEmpresa, Numero);
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosColecciones', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosColecciones')
              AND name = N'IX_ProductosServiciosColecciones_Empresa_Nombre'
       )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_ProductosServiciosColecciones_Empresa_Nombre
            ON dbo.ProductosServiciosColecciones (idEmpresa, Nombre);
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosPaquetes', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosPaquetes')
              AND name = N'UX_ProductosServiciosPaquetes_Empresa_Id'
       )
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UX_ProductosServiciosPaquetes_Empresa_Id
            ON dbo.ProductosServiciosPaquetes (idEmpresa, id);
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosPaquetes', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosPaquetes')
              AND name = N'IX_ProductosServiciosPaquetes_Empresa_Nombre'
       )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_ProductosServiciosPaquetes_Empresa_Nombre
            ON dbo.ProductosServiciosPaquetes (idEmpresa, Nombre);
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosAtributos', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosAtributos')
              AND name = N'UX_ProductosServiciosAtributos_Empresa_Id'
       )
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UX_ProductosServiciosAtributos_Empresa_Id
            ON dbo.ProductosServiciosAtributos (idEmpresa, id);
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosAtributos', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosAtributos')
              AND name = N'UX_ProductosServiciosAtributos_Empresa_Nombre'
       )
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UX_ProductosServiciosAtributos_Empresa_Nombre
            ON dbo.ProductosServiciosAtributos (idEmpresa, Nombre);
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosAtributosValores', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosAtributosValores')
              AND name = N'UX_ProductosServiciosAtributosValores_Empresa_Id'
       )
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UX_ProductosServiciosAtributosValores_Empresa_Id
            ON dbo.ProductosServiciosAtributosValores (idEmpresa, id);
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosAtributosValores', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosAtributosValores')
              AND name = N'UX_ProductosServiciosAtributosValores_Empresa_Atributo_Valor'
       )
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UX_ProductosServiciosAtributosValores_Empresa_Atributo_Valor
            ON dbo.ProductosServiciosAtributosValores (idEmpresa, idAtributo, Valor);
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosProductoAtributos', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosProductoAtributos')
              AND name = N'UX_ProductosServiciosProductoAtributos_Empresa_Id'
       )
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UX_ProductosServiciosProductoAtributos_Empresa_Id
            ON dbo.ProductosServiciosProductoAtributos (idEmpresa, id);
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosProductoAtributos', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosProductoAtributos')
              AND name = N'UX_ProductosServiciosProductoAtributos_Empresa_Producto_Atributo'
       )
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UX_ProductosServiciosProductoAtributos_Empresa_Producto_Atributo
            ON dbo.ProductosServiciosProductoAtributos (idEmpresa, idProductoServicio, idAtributo);
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosProductoAtributoValores', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosProductoAtributoValores')
              AND name = N'UX_ProductosServiciosProductoAtributoValores_Empresa_ProductoAtributo_Valor'
       )
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UX_ProductosServiciosProductoAtributoValores_Empresa_ProductoAtributo_Valor
            ON dbo.ProductosServiciosProductoAtributoValores (idEmpresa, idProductoAtributo, idAtributoValor);
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosOpcionesVariante', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosOpcionesVariante')
              AND name = N'UX_ProductosServiciosOpcionesVariante_Empresa_Id'
       )
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UX_ProductosServiciosOpcionesVariante_Empresa_Id
            ON dbo.ProductosServiciosOpcionesVariante (idEmpresa, id);
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosOpcionesVariante', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosOpcionesVariante')
              AND name = N'UX_ProductosServiciosOpcionesVariante_Empresa_Producto_Nombre'
       )
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UX_ProductosServiciosOpcionesVariante_Empresa_Producto_Nombre
            ON dbo.ProductosServiciosOpcionesVariante (idEmpresa, idProductoServicio, Nombre);
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosOpcionesVarianteValores', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosOpcionesVarianteValores')
              AND name = N'UX_ProductosServiciosOpcionesVarianteValores_Empresa_Id'
       )
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UX_ProductosServiciosOpcionesVarianteValores_Empresa_Id
            ON dbo.ProductosServiciosOpcionesVarianteValores (idEmpresa, id);
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosOpcionesVarianteValores', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosOpcionesVarianteValores')
              AND name = N'UX_ProductosServiciosOpcionesVarianteValores_Empresa_Opcion_Valor'
       )
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UX_ProductosServiciosOpcionesVarianteValores_Empresa_Opcion_Valor
            ON dbo.ProductosServiciosOpcionesVarianteValores (idEmpresa, idOpcionVariante, Valor);
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosVariantes', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosVariantes')
              AND name = N'UX_ProductosServiciosVariantes_Empresa_Id'
       )
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UX_ProductosServiciosVariantes_Empresa_Id
            ON dbo.ProductosServiciosVariantes (idEmpresa, id);
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosVariantes', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosVariantes')
              AND name = N'UX_ProductosServiciosVariantes_Empresa_Producto_ClaveCombinacion'
       )
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX UX_ProductosServiciosVariantes_Empresa_Producto_ClaveCombinacion
            ON dbo.ProductosServiciosVariantes (idEmpresa, idProductoServicio, ClaveCombinacion);
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosVarianteValores', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosVarianteValores')
              AND name = N'IX_ProductosServiciosVarianteValores_Empresa_Variante_Orden'
       )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_ProductosServiciosVarianteValores_Empresa_Variante_Orden
            ON dbo.ProductosServiciosVarianteValores (idEmpresa, idVariante, Orden);
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosMultimedia', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.ProductosServiciosMultimedia')
              AND name = N'IX_ProductosServiciosMultimedia_Empresa_Producto'
       )
    BEGIN
        CREATE NONCLUSTERED INDEX IX_ProductosServiciosMultimedia_Empresa_Producto
            ON dbo.ProductosServiciosMultimedia (idEmpresa, idProductoServicio, Activo, TipoMultimedia, Orden);
    END;

    IF OBJECT_ID(N'dbo.ProductosServicios', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1 FROM sys.foreign_keys
            WHERE name = N'FK_ProductosServicios_Colecciones_EmpresaId'
       )
    BEGIN
        ALTER TABLE dbo.ProductosServicios WITH CHECK
        ADD CONSTRAINT FK_ProductosServicios_Colecciones_EmpresaId
            FOREIGN KEY (idEmpresa, idColeccion)
            REFERENCES dbo.ProductosServiciosColecciones (idEmpresa, id);
    END;

    IF OBJECT_ID(N'dbo.ProductosServicios', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1 FROM sys.foreign_keys
            WHERE name = N'FK_ProductosServicios_Paquetes_EmpresaId'
       )
    BEGIN
        ALTER TABLE dbo.ProductosServicios WITH CHECK
        ADD CONSTRAINT FK_ProductosServicios_Paquetes_EmpresaId
            FOREIGN KEY (idEmpresa, idPaquete)
            REFERENCES dbo.ProductosServiciosPaquetes (idEmpresa, id);
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosAtributosValores', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1 FROM sys.foreign_keys
            WHERE name = N'FK_ProductosServiciosAtributosValores_Atributos_EmpresaId'
       )
    BEGIN
        ALTER TABLE dbo.ProductosServiciosAtributosValores WITH CHECK
        ADD CONSTRAINT FK_ProductosServiciosAtributosValores_Atributos_EmpresaId
            FOREIGN KEY (idEmpresa, idAtributo)
            REFERENCES dbo.ProductosServiciosAtributos (idEmpresa, id);
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosProductoAtributos', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1 FROM sys.foreign_keys
            WHERE name = N'FK_ProductosServiciosProductoAtributos_Productos_EmpresaId'
       )
    BEGIN
        ALTER TABLE dbo.ProductosServiciosProductoAtributos WITH CHECK
        ADD CONSTRAINT FK_ProductosServiciosProductoAtributos_Productos_EmpresaId
            FOREIGN KEY (idEmpresa, idProductoServicio)
            REFERENCES dbo.ProductosServicios (idEmpresa, id);
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosProductoAtributos', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1 FROM sys.foreign_keys
            WHERE name = N'FK_ProductosServiciosProductoAtributos_Atributos_EmpresaId'
       )
    BEGIN
        ALTER TABLE dbo.ProductosServiciosProductoAtributos WITH CHECK
        ADD CONSTRAINT FK_ProductosServiciosProductoAtributos_Atributos_EmpresaId
            FOREIGN KEY (idEmpresa, idAtributo)
            REFERENCES dbo.ProductosServiciosAtributos (idEmpresa, id);
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosProductoAtributoValores', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1 FROM sys.foreign_keys
            WHERE name = N'FK_ProductosServiciosProductoAtributoValores_ProductoAtributos_EmpresaId'
       )
    BEGIN
        ALTER TABLE dbo.ProductosServiciosProductoAtributoValores WITH CHECK
        ADD CONSTRAINT FK_ProductosServiciosProductoAtributoValores_ProductoAtributos_EmpresaId
            FOREIGN KEY (idEmpresa, idProductoAtributo)
            REFERENCES dbo.ProductosServiciosProductoAtributos (idEmpresa, id);
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosProductoAtributoValores', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1 FROM sys.foreign_keys
            WHERE name = N'FK_ProductosServiciosProductoAtributoValores_AtributosValores_EmpresaId'
       )
    BEGIN
        ALTER TABLE dbo.ProductosServiciosProductoAtributoValores WITH CHECK
        ADD CONSTRAINT FK_ProductosServiciosProductoAtributoValores_AtributosValores_EmpresaId
            FOREIGN KEY (idEmpresa, idAtributoValor)
            REFERENCES dbo.ProductosServiciosAtributosValores (idEmpresa, id);
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosVariantes', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1 FROM sys.foreign_keys
            WHERE name = N'FK_ProductosServiciosVariantes_Productos_EmpresaId'
       )
    BEGIN
        ALTER TABLE dbo.ProductosServiciosVariantes WITH CHECK
        ADD CONSTRAINT FK_ProductosServiciosVariantes_Productos_EmpresaId
            FOREIGN KEY (idEmpresa, idProductoServicio)
            REFERENCES dbo.ProductosServicios (idEmpresa, id);
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosOpcionesVariante', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1 FROM sys.foreign_keys
            WHERE name = N'FK_ProductosServiciosOpcionesVariante_Productos_EmpresaId'
       )
    BEGIN
        ALTER TABLE dbo.ProductosServiciosOpcionesVariante WITH CHECK
        ADD CONSTRAINT FK_ProductosServiciosOpcionesVariante_Productos_EmpresaId
            FOREIGN KEY (idEmpresa, idProductoServicio)
            REFERENCES dbo.ProductosServicios (idEmpresa, id);
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosOpcionesVarianteValores', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1 FROM sys.foreign_keys
            WHERE name = N'FK_ProductosServiciosOpcionesVarianteValores_Opciones_EmpresaId'
       )
    BEGIN
        ALTER TABLE dbo.ProductosServiciosOpcionesVarianteValores WITH CHECK
        ADD CONSTRAINT FK_ProductosServiciosOpcionesVarianteValores_Opciones_EmpresaId
            FOREIGN KEY (idEmpresa, idOpcionVariante)
            REFERENCES dbo.ProductosServiciosOpcionesVariante (idEmpresa, id);
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosVarianteValores', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1 FROM sys.foreign_keys
            WHERE name = N'FK_ProductosServiciosVarianteValores_Variantes_EmpresaId'
       )
    BEGIN
        ALTER TABLE dbo.ProductosServiciosVarianteValores WITH CHECK
        ADD CONSTRAINT FK_ProductosServiciosVarianteValores_Variantes_EmpresaId
            FOREIGN KEY (idEmpresa, idVariante)
            REFERENCES dbo.ProductosServiciosVariantes (idEmpresa, id);
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosVarianteValores', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1 FROM sys.foreign_keys
            WHERE name = N'FK_ProductosServiciosVarianteValores_Atributos_EmpresaId'
       )
    BEGIN
        ALTER TABLE dbo.ProductosServiciosVarianteValores WITH CHECK
        ADD CONSTRAINT FK_ProductosServiciosVarianteValores_Atributos_EmpresaId
            FOREIGN KEY (idEmpresa, idAtributo)
            REFERENCES dbo.ProductosServiciosAtributos (idEmpresa, id);
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosVarianteValores', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1 FROM sys.foreign_keys
            WHERE name = N'FK_ProductosServiciosVarianteValores_AtributosValores_EmpresaId'
       )
    BEGIN
        ALTER TABLE dbo.ProductosServiciosVarianteValores WITH CHECK
        ADD CONSTRAINT FK_ProductosServiciosVarianteValores_AtributosValores_EmpresaId
            FOREIGN KEY (idEmpresa, idAtributoValor)
            REFERENCES dbo.ProductosServiciosAtributosValores (idEmpresa, id);
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosVarianteValores', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1 FROM sys.foreign_keys
            WHERE name = N'FK_ProductosServiciosVarianteValores_Opciones_EmpresaId'
       )
    BEGIN
        ALTER TABLE dbo.ProductosServiciosVarianteValores WITH CHECK
        ADD CONSTRAINT FK_ProductosServiciosVarianteValores_Opciones_EmpresaId
            FOREIGN KEY (idEmpresa, idOpcionVariante)
            REFERENCES dbo.ProductosServiciosOpcionesVariante (idEmpresa, id);
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosVarianteValores', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1 FROM sys.foreign_keys
            WHERE name = N'FK_ProductosServiciosVarianteValores_OpcionesValores_EmpresaId'
       )
    BEGIN
        ALTER TABLE dbo.ProductosServiciosVarianteValores WITH CHECK
        ADD CONSTRAINT FK_ProductosServiciosVarianteValores_OpcionesValores_EmpresaId
            FOREIGN KEY (idEmpresa, idOpcionVarianteValor)
            REFERENCES dbo.ProductosServiciosOpcionesVarianteValores (idEmpresa, id);
    END;

    IF OBJECT_ID(N'dbo.ProductosServiciosMultimedia', N'U') IS NOT NULL
       AND NOT EXISTS (
            SELECT 1 FROM sys.foreign_keys
            WHERE name = N'FK_ProductosServiciosMultimedia_Productos_EmpresaId'
       )
    BEGIN
        ALTER TABLE dbo.ProductosServiciosMultimedia WITH CHECK
        ADD CONSTRAINT FK_ProductosServiciosMultimedia_Productos_EmpresaId
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
