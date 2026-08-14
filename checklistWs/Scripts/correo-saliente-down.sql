IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_ConfiguracionCorreoSaliente_idEmpresa'
      AND object_id = OBJECT_ID('dbo.ConfiguracionCorreoSaliente')
)
BEGIN
    DROP INDEX UX_ConfiguracionCorreoSaliente_idEmpresa ON dbo.ConfiguracionCorreoSaliente;
END;
GO

IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_ConfiguracionCorreoSaliente_identityKey'
      AND object_id = OBJECT_ID('dbo.ConfiguracionCorreoSaliente')
)
BEGIN
    DROP INDEX UX_ConfiguracionCorreoSaliente_identityKey ON dbo.ConfiguracionCorreoSaliente;
END;
GO

IF OBJECT_ID('dbo.ConfiguracionCorreoSaliente', 'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.ConfiguracionCorreoSaliente;
END;
GO
