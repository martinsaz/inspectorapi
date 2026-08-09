IF EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_ClientesNotas_Clientes'
)
BEGIN
    ALTER TABLE dbo.ClientesNotas DROP CONSTRAINT FK_ClientesNotas_Clientes;
END;
GO

IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_ClientesNotas_idEmpresa_idCliente_Activo_FechaCreacion'
      AND object_id = OBJECT_ID('dbo.ClientesNotas')
)
BEGIN
    DROP INDEX IX_ClientesNotas_idEmpresa_idCliente_Activo_FechaCreacion ON dbo.ClientesNotas;
END;
GO

IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_ClientesNotas_identityKey'
      AND object_id = OBJECT_ID('dbo.ClientesNotas')
)
BEGIN
    DROP INDEX UX_ClientesNotas_identityKey ON dbo.ClientesNotas;
END;
GO

IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_Clientes_idEmpresa_Correo'
      AND object_id = OBJECT_ID('dbo.Clientes')
)
BEGIN
    DROP INDEX IX_Clientes_idEmpresa_Correo ON dbo.Clientes;
END;
GO

IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_Clientes_idEmpresa_Telefono'
      AND object_id = OBJECT_ID('dbo.Clientes')
)
BEGIN
    DROP INDEX IX_Clientes_idEmpresa_Telefono ON dbo.Clientes;
END;
GO

IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_Clientes_idEmpresa_Activo_Nombre'
      AND object_id = OBJECT_ID('dbo.Clientes')
)
BEGIN
    DROP INDEX IX_Clientes_idEmpresa_Activo_Nombre ON dbo.Clientes;
END;
GO

IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_Clientes_identityKey'
      AND object_id = OBJECT_ID('dbo.Clientes')
)
BEGIN
    DROP INDEX UX_Clientes_identityKey ON dbo.Clientes;
END;
GO

IF OBJECT_ID('dbo.ClientesNotas', 'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.ClientesNotas;
END;
GO

IF OBJECT_ID('dbo.Clientes', 'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.Clientes;
END;
GO
