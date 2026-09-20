SET XACT_ABORT ON;

BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.ActivosProveedores', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ActivosProveedores
    (
        id uniqueidentifier NOT NULL,
        idEmpresa uniqueidentifier NOT NULL,
        Codigo nvarchar(64) NOT NULL,
        Nombre nvarchar(160) NOT NULL,
        Descripcion nvarchar(max) NULL,
        RazonSocial nvarchar(250) NULL,
        RFC nvarchar(15) NULL,
        Telefono nvarchar(15) NULL,
        Telefono1 nvarchar(15) NULL,
        Email nvarchar(50) NULL,
        Limite decimal(18,2) NOT NULL CONSTRAINT DF_ActivosProveedores_Limite DEFAULT (0),
        ClasifContable bit NOT NULL CONSTRAINT DF_ActivosProveedores_ClasifContable DEFAULT (0),
        CuentaContable nvarchar(255) NULL,
        Contacto nvarchar(255) NULL,
        CuentaBancaria nvarchar(255) NULL,
        Activo bit NOT NULL CONSTRAINT DF_ActivosProveedores_Activo DEFAULT (1),
        FechaCreacion datetime2(0) NOT NULL CONSTRAINT DF_ActivosProveedores_FechaCreacion DEFAULT (SYSUTCDATETIME()),
        FechaActualizacion datetime2(0) NOT NULL CONSTRAINT DF_ActivosProveedores_FechaActualizacion DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_ActivosProveedores PRIMARY KEY CLUSTERED (id)
    );
END;

IF COL_LENGTH('dbo.ActivosProveedores', 'RazonSocial') IS NULL
    ALTER TABLE dbo.ActivosProveedores ADD RazonSocial nvarchar(250) NULL;

IF COL_LENGTH('dbo.ActivosProveedores', 'RFC') IS NULL
    ALTER TABLE dbo.ActivosProveedores ADD RFC nvarchar(15) NULL;

IF COL_LENGTH('dbo.ActivosProveedores', 'Telefono') IS NULL
    ALTER TABLE dbo.ActivosProveedores ADD Telefono nvarchar(15) NULL;

IF COL_LENGTH('dbo.ActivosProveedores', 'Telefono1') IS NULL
    ALTER TABLE dbo.ActivosProveedores ADD Telefono1 nvarchar(15) NULL;

IF COL_LENGTH('dbo.ActivosProveedores', 'Email') IS NULL
    ALTER TABLE dbo.ActivosProveedores ADD Email nvarchar(50) NULL;

IF COL_LENGTH('dbo.ActivosProveedores', 'Limite') IS NULL
    ALTER TABLE dbo.ActivosProveedores ADD Limite decimal(18,2) NOT NULL CONSTRAINT DF_ActivosProveedores_Limite DEFAULT (0);

IF COL_LENGTH('dbo.ActivosProveedores', 'ClasifContable') IS NULL
    ALTER TABLE dbo.ActivosProveedores ADD ClasifContable bit NOT NULL CONSTRAINT DF_ActivosProveedores_ClasifContable DEFAULT (0);

IF COL_LENGTH('dbo.ActivosProveedores', 'CuentaContable') IS NULL
    ALTER TABLE dbo.ActivosProveedores ADD CuentaContable nvarchar(255) NULL;

IF COL_LENGTH('dbo.ActivosProveedores', 'Contacto') IS NULL
    ALTER TABLE dbo.ActivosProveedores ADD Contacto nvarchar(255) NULL;

IF COL_LENGTH('dbo.ActivosProveedores', 'CuentaBancaria') IS NULL
    ALTER TABLE dbo.ActivosProveedores ADD CuentaBancaria nvarchar(255) NULL;

IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.ActivosProveedores')
      AND name = N'Descripcion'
      AND max_length <> -1
)
    ALTER TABLE dbo.ActivosProveedores ALTER COLUMN Descripcion nvarchar(max) NULL;

IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.ActivosProveedores')
      AND name = N'CK_ActivosProveedores_Limite'
)
    ALTER TABLE dbo.ActivosProveedores ADD CONSTRAINT CK_ActivosProveedores_Limite CHECK (Limite >= 0);

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.ActivosProveedores')
      AND name = N'IX_ActivosProveedores_Empresa_Rfc'
)
    CREATE NONCLUSTERED INDEX IX_ActivosProveedores_Empresa_Rfc
        ON dbo.ActivosProveedores(idEmpresa, RFC)
        WHERE RFC IS NOT NULL;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.ActivosProveedores')
      AND name = N'UX_ActivosProveedores_IdEmpresa_Codigo'
)
    CREATE UNIQUE NONCLUSTERED INDEX UX_ActivosProveedores_IdEmpresa_Codigo
        ON dbo.ActivosProveedores(idEmpresa, Codigo);

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.ActivosProveedores')
      AND name = N'IX_ActivosProveedores_EmpresaActivoNombre'
)
    CREATE NONCLUSTERED INDEX IX_ActivosProveedores_EmpresaActivoNombre
        ON dbo.ActivosProveedores(idEmpresa, Activo, Nombre, Codigo);

COMMIT TRANSACTION;
