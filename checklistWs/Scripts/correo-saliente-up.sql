IF OBJECT_ID('dbo.ConfiguracionCorreoSaliente', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConfiguracionCorreoSaliente
    (
        id uniqueidentifier NOT NULL,
        idEmpresa uniqueidentifier NOT NULL,
        identityKey uniqueidentifier NOT NULL,
        Cuenta nvarchar(200) NOT NULL,
        ServidorSmtp nvarchar(200) NOT NULL,
        Puerto int NOT NULL,
        Seguridad nvarchar(20) NOT NULL,
        CredencialProtegida nvarchar(max) NOT NULL,
        DestinatarioPrueba nvarchar(200) NULL,
        ConfiguracionVerificada bit NOT NULL,
        FechaUltimaPrueba datetime2 NULL,
        FechaCreacion datetime2 NOT NULL,
        FechaActualizacion datetime2 NULL,
        FechaArchivado datetime2 NULL,
        Activo bit NOT NULL,
        CONSTRAINT PK_ConfiguracionCorreoSaliente PRIMARY KEY CLUSTERED (id),
        CONSTRAINT CK_ConfiguracionCorreoSaliente_Puerto CHECK (Puerto > 0 AND Puerto <= 65535),
        CONSTRAINT CK_ConfiguracionCorreoSaliente_Seguridad CHECK (Seguridad IN ('SSL_TLS', 'STARTTLS'))
    );
END;
GO

IF COL_LENGTH('dbo.ConfiguracionCorreoSaliente', 'DestinatarioPrueba') IS NULL
BEGIN
    ALTER TABLE dbo.ConfiguracionCorreoSaliente ADD DestinatarioPrueba nvarchar(200) NULL;
END;
GO

IF COL_LENGTH('dbo.ConfiguracionCorreoSaliente', 'Seguridad') IS NULL
BEGIN
    ALTER TABLE dbo.ConfiguracionCorreoSaliente ADD Seguridad nvarchar(20) NOT NULL CONSTRAINT DF_ConfiguracionCorreoSaliente_Seguridad DEFAULT ('SSL_TLS');
END;
GO

IF COL_LENGTH('dbo.ConfiguracionCorreoSaliente', 'ConfiguracionVerificada') IS NULL
BEGIN
    ALTER TABLE dbo.ConfiguracionCorreoSaliente ADD ConfiguracionVerificada bit NOT NULL CONSTRAINT DF_ConfiguracionCorreoSaliente_Verificada DEFAULT (0);
END;
GO

IF COL_LENGTH('dbo.ConfiguracionCorreoSaliente', 'FechaUltimaPrueba') IS NULL
BEGIN
    ALTER TABLE dbo.ConfiguracionCorreoSaliente ADD FechaUltimaPrueba datetime2 NULL;
END;
GO

IF COL_LENGTH('dbo.ConfiguracionCorreoSaliente', 'FechaActualizacion') IS NULL
BEGIN
    ALTER TABLE dbo.ConfiguracionCorreoSaliente ADD FechaActualizacion datetime2 NULL;
END;
GO

IF COL_LENGTH('dbo.ConfiguracionCorreoSaliente', 'FechaArchivado') IS NULL
BEGIN
    ALTER TABLE dbo.ConfiguracionCorreoSaliente ADD FechaArchivado datetime2 NULL;
END;
GO

IF COL_LENGTH('dbo.ConfiguracionCorreoSaliente', 'Activo') IS NULL
BEGIN
    ALTER TABLE dbo.ConfiguracionCorreoSaliente ADD Activo bit NOT NULL CONSTRAINT DF_ConfiguracionCorreoSaliente_Activo DEFAULT (1);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_ConfiguracionCorreoSaliente_identityKey'
      AND object_id = OBJECT_ID('dbo.ConfiguracionCorreoSaliente')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_ConfiguracionCorreoSaliente_identityKey
        ON dbo.ConfiguracionCorreoSaliente(identityKey);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_ConfiguracionCorreoSaliente_idEmpresa'
      AND object_id = OBJECT_ID('dbo.ConfiguracionCorreoSaliente')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_ConfiguracionCorreoSaliente_idEmpresa
        ON dbo.ConfiguracionCorreoSaliente(idEmpresa)
        WHERE FechaArchivado IS NULL;
END;
GO
