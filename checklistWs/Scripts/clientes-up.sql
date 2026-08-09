IF OBJECT_ID('dbo.ClientesNotas', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ClientesNotas
    (
        id uniqueidentifier NOT NULL,
        idEmpresa uniqueidentifier NOT NULL,
        identityKey uniqueidentifier NOT NULL,
        idCliente uniqueidentifier NOT NULL,
        Texto nvarchar(2000) NOT NULL,
        EsTarea bit NOT NULL,
        FechaTarea date NULL,
        HoraTarea time(0) NULL,
        Completada bit NOT NULL,
        FechaCompletada datetime2 NULL,
        Activo bit NOT NULL,
        FechaCreacion datetime2 NOT NULL,
        FechaActualizacion datetime2 NULL,
        FechaArchivado datetime2 NULL,
        CONSTRAINT PK_ClientesNotas PRIMARY KEY CLUSTERED (id),
        CONSTRAINT CK_ClientesNotas_TareaFechas
            CHECK ((EsTarea = 0) OR (FechaTarea IS NOT NULL AND HoraTarea IS NOT NULL))
    );
END;
GO

IF OBJECT_ID('dbo.Clientes', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Clientes
    (
        id uniqueidentifier NOT NULL,
        idEmpresa uniqueidentifier NOT NULL,
        identityKey uniqueidentifier NOT NULL,
        TipoCliente tinyint NOT NULL,
        Nombre nvarchar(200) NOT NULL,
        Telefono nvarchar(30) NULL,
        Correo nvarchar(200) NULL,
        Empresa nvarchar(200) NULL,
        Activo bit NOT NULL,
        FechaCreacion datetime2 NOT NULL,
        FechaActualizacion datetime2 NULL,
        FechaArchivado datetime2 NULL,
        Celular nvarchar(30) NULL,
        TelefonoFijo nvarchar(30) NULL,
        FechaNacimiento date NULL,
        Cbarras nvarchar(80) NULL,
        Calle nvarchar(200) NULL,
        NumeroExt nvarchar(40) NULL,
        NumeroInt nvarchar(40) NULL,
        Colonia nvarchar(150) NULL,
        Ciudad nvarchar(150) NULL,
        Municipio nvarchar(150) NULL,
        Estado nvarchar(150) NULL,
        CodigoPostal nvarchar(12) NULL,
        Rfc nvarchar(20) NULL,
        RegimenFiscal nvarchar(40) NULL,
        EntreCalles nvarchar(300) NULL,
        Referencia nvarchar(300) NULL,
        NombreAval nvarchar(200) NULL,
        DireccionAval nvarchar(300) NULL,
        LimiteCredito decimal(18, 2) NOT NULL CONSTRAINT DF_Clientes_LimiteCredito DEFAULT (0),
        PlazoDias int NOT NULL CONSTRAINT DF_Clientes_PlazoDias DEFAULT (0),
        Descuento decimal(18, 2) NOT NULL CONSTRAINT DF_Clientes_Descuento DEFAULT (0),
        Pagos int NOT NULL CONSTRAINT DF_Clientes_Pagos DEFAULT (0),
        Interes decimal(18, 2) NOT NULL CONSTRAINT DF_Clientes_Interes DEFAULT (0),
        Observaciones nvarchar(2000) NULL,
        IdNivel int NOT NULL CONSTRAINT DF_Clientes_IdNivel DEFAULT (1),
        CONSTRAINT PK_Clientes PRIMARY KEY CLUSTERED (id),
        CONSTRAINT CK_Clientes_TipoCliente CHECK (TipoCliente IN (1, 2))
    );
END;
GO

IF COL_LENGTH('dbo.Clientes', 'Celular') IS NULL
BEGIN
    ALTER TABLE dbo.Clientes ADD Celular nvarchar(30) NULL;
END;
GO

IF COL_LENGTH('dbo.Clientes', 'TelefonoFijo') IS NULL
BEGIN
    ALTER TABLE dbo.Clientes ADD TelefonoFijo nvarchar(30) NULL;
END;
GO

IF COL_LENGTH('dbo.Clientes', 'FechaNacimiento') IS NULL
BEGIN
    ALTER TABLE dbo.Clientes ADD FechaNacimiento date NULL;
END;
GO

IF COL_LENGTH('dbo.Clientes', 'Cbarras') IS NULL
BEGIN
    ALTER TABLE dbo.Clientes ADD Cbarras nvarchar(80) NULL;
END;
GO

IF COL_LENGTH('dbo.Clientes', 'Calle') IS NULL
BEGIN
    ALTER TABLE dbo.Clientes ADD Calle nvarchar(200) NULL;
END;
GO

IF COL_LENGTH('dbo.Clientes', 'NumeroExt') IS NULL
BEGIN
    ALTER TABLE dbo.Clientes ADD NumeroExt nvarchar(40) NULL;
END;
GO

IF COL_LENGTH('dbo.Clientes', 'NumeroInt') IS NULL
BEGIN
    ALTER TABLE dbo.Clientes ADD NumeroInt nvarchar(40) NULL;
END;
GO

IF COL_LENGTH('dbo.Clientes', 'Colonia') IS NULL
BEGIN
    ALTER TABLE dbo.Clientes ADD Colonia nvarchar(150) NULL;
END;
GO

IF COL_LENGTH('dbo.Clientes', 'Ciudad') IS NULL
BEGIN
    ALTER TABLE dbo.Clientes ADD Ciudad nvarchar(150) NULL;
END;
GO

IF COL_LENGTH('dbo.Clientes', 'Municipio') IS NULL
BEGIN
    ALTER TABLE dbo.Clientes ADD Municipio nvarchar(150) NULL;
END;
GO

IF COL_LENGTH('dbo.Clientes', 'Estado') IS NULL
BEGIN
    ALTER TABLE dbo.Clientes ADD Estado nvarchar(150) NULL;
END;
GO

IF COL_LENGTH('dbo.Clientes', 'CodigoPostal') IS NULL
BEGIN
    ALTER TABLE dbo.Clientes ADD CodigoPostal nvarchar(12) NULL;
END;
GO

IF COL_LENGTH('dbo.Clientes', 'Rfc') IS NULL
BEGIN
    ALTER TABLE dbo.Clientes ADD Rfc nvarchar(20) NULL;
END;
GO

IF COL_LENGTH('dbo.Clientes', 'RegimenFiscal') IS NULL
BEGIN
    ALTER TABLE dbo.Clientes ADD RegimenFiscal nvarchar(40) NULL;
END;
GO

IF COL_LENGTH('dbo.Clientes', 'EntreCalles') IS NULL
BEGIN
    ALTER TABLE dbo.Clientes ADD EntreCalles nvarchar(300) NULL;
END;
GO

IF COL_LENGTH('dbo.Clientes', 'Referencia') IS NULL
BEGIN
    ALTER TABLE dbo.Clientes ADD Referencia nvarchar(300) NULL;
END;
GO

IF COL_LENGTH('dbo.Clientes', 'NombreAval') IS NULL
BEGIN
    ALTER TABLE dbo.Clientes ADD NombreAval nvarchar(200) NULL;
END;
GO

IF COL_LENGTH('dbo.Clientes', 'DireccionAval') IS NULL
BEGIN
    ALTER TABLE dbo.Clientes ADD DireccionAval nvarchar(300) NULL;
END;
GO

IF COL_LENGTH('dbo.Clientes', 'LimiteCredito') IS NULL
BEGIN
    ALTER TABLE dbo.Clientes ADD LimiteCredito decimal(18, 2) NOT NULL CONSTRAINT DF_Clientes_LimiteCredito DEFAULT (0);
END;
GO

IF COL_LENGTH('dbo.Clientes', 'PlazoDias') IS NULL
BEGIN
    ALTER TABLE dbo.Clientes ADD PlazoDias int NOT NULL CONSTRAINT DF_Clientes_PlazoDias DEFAULT (0);
END;
GO

IF COL_LENGTH('dbo.Clientes', 'Descuento') IS NULL
BEGIN
    ALTER TABLE dbo.Clientes ADD Descuento decimal(18, 2) NOT NULL CONSTRAINT DF_Clientes_Descuento DEFAULT (0);
END;
GO

IF COL_LENGTH('dbo.Clientes', 'Pagos') IS NULL
BEGIN
    ALTER TABLE dbo.Clientes ADD Pagos int NOT NULL CONSTRAINT DF_Clientes_Pagos DEFAULT (0);
END;
GO

IF COL_LENGTH('dbo.Clientes', 'Interes') IS NULL
BEGIN
    ALTER TABLE dbo.Clientes ADD Interes decimal(18, 2) NOT NULL CONSTRAINT DF_Clientes_Interes DEFAULT (0);
END;
GO

IF COL_LENGTH('dbo.Clientes', 'Observaciones') IS NULL
BEGIN
    ALTER TABLE dbo.Clientes ADD Observaciones nvarchar(2000) NULL;
END;
GO

IF COL_LENGTH('dbo.Clientes', 'IdNivel') IS NULL
BEGIN
    ALTER TABLE dbo.Clientes ADD IdNivel int NOT NULL CONSTRAINT DF_Clientes_IdNivel DEFAULT (1);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_ClientesNotas_Clientes'
)
BEGIN
    ALTER TABLE dbo.ClientesNotas
    ADD CONSTRAINT FK_ClientesNotas_Clientes
        FOREIGN KEY (idCliente) REFERENCES dbo.Clientes(id);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_Clientes_identityKey'
      AND object_id = OBJECT_ID('dbo.Clientes')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_Clientes_identityKey
        ON dbo.Clientes(identityKey);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_Clientes_idEmpresa_Activo_Nombre'
      AND object_id = OBJECT_ID('dbo.Clientes')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Clientes_idEmpresa_Activo_Nombre
        ON dbo.Clientes(idEmpresa, Activo, Nombre)
        INCLUDE (TipoCliente, Telefono, Correo, Empresa, FechaCreacion, FechaActualizacion);
END;
GO

IF OBJECT_ID('dbo.CatalogoClientesRegimenFiscal', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CatalogoClientesRegimenFiscal
    (
        Id varchar(255) NOT NULL,
        c_RegimenFiscal varchar(10) NOT NULL,
        Descripcion nvarchar(255) NOT NULL,
        Activo bit NOT NULL CONSTRAINT DF_CatalogoClientesRegimenFiscal_Activo DEFAULT (1),
        CONSTRAINT PK_CatalogoClientesRegimenFiscal PRIMARY KEY CLUSTERED (Id)
    );
END;
GO

IF COL_LENGTH('dbo.CatalogoClientesRegimenFiscal', 'Activo') IS NULL
BEGIN
    ALTER TABLE dbo.CatalogoClientesRegimenFiscal
    ADD Activo bit NOT NULL CONSTRAINT DF_CatalogoClientesRegimenFiscal_Activo DEFAULT (1);
END;
GO

MERGE dbo.CatalogoClientesRegimenFiscal AS target
USING (VALUES
    ('General de Ley Personas Morales', '601', N'General de Ley Personas Morales', 1),
    ('Personas Morales con Fines no Lucrativos', '603', N'Personas Morales con Fines no Lucrativos', 1),
    ('Sueldos y Salarios e Ingresos Asimilados a Salarios', '605', N'Sueldos y Salarios e Ingresos Asimilados a Salarios', 1),
    ('Arrendamiento', '606', N'Arrendamiento', 1),
    ('Régimen de Enajenación o Adquisición de Bienes', '607', N'Régimen de Enajenación o Adquisición de Bienes', 1),
    ('Demás ingresos', '608', N'Demás ingresos', 1),
    ('Residentes en el Extranjero sin Establecimiento Permanente en México', '610', N'Residentes en el Extranjero sin Establecimiento Permanente en México', 1),
    ('Ingresos por Dividendos (socios y accionistas)', '611', N'Ingresos por Dividendos (socios y accionistas)', 1),
    ('Personas Físicas con Actividades Empresariales y Profesionales', '612', N'Personas Físicas con Actividades Empresariales y Profesionales', 1),
    ('Ingresos por intereses', '614', N'Ingresos por intereses', 1),
    ('Régimen de los ingresos por obtención de premios', '615', N'Régimen de los ingresos por obtención de premios', 1),
    ('Sin obligaciones fiscales', '616', N'Sin obligaciones fiscales', 1),
    ('Sociedades Cooperativas de Producción que optan por diferir sus ingresos', '620', N'Sociedades Cooperativas de Producción que optan por diferir sus ingresos', 1),
    ('Incorporación Fiscal', '621', N'Incorporación Fiscal', 1),
    ('Actividades Agrícolas, Ganaderas, Silvícolas y Pesqueras', '622', N'Actividades Agrícolas, Ganaderas, Silvícolas y Pesqueras', 1),
    ('Opcional para Grupos de Sociedades', '623', N'Opcional para Grupos de Sociedades', 1),
    ('Coordinados', '624', N'Coordinados', 1),
    ('Régimen de las Actividades Empresariales con ingresos a través de Plataformas Tecnológicas', '625', N'Régimen de las Actividades Empresariales con ingresos a través de Plataformas Tecnológicas', 1),
    ('Régimen Simplificado de Confianza', '626', N'Régimen Simplificado de Confianza', 1)
) AS source (Id, c_RegimenFiscal, Descripcion, Activo)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        target.c_RegimenFiscal = source.c_RegimenFiscal,
        target.Descripcion = source.Descripcion,
        target.Activo = source.Activo
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, c_RegimenFiscal, Descripcion, Activo)
    VALUES (source.Id, source.c_RegimenFiscal, source.Descripcion, source.Activo);
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_CatalogoClientesRegimenFiscal_Clave'
      AND object_id = OBJECT_ID('dbo.CatalogoClientesRegimenFiscal')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_CatalogoClientesRegimenFiscal_Clave
        ON dbo.CatalogoClientesRegimenFiscal(c_RegimenFiscal);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_Clientes_idEmpresa_Telefono'
      AND object_id = OBJECT_ID('dbo.Clientes')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Clientes_idEmpresa_Telefono
        ON dbo.Clientes(idEmpresa, Telefono)
        INCLUDE (Nombre, Correo, Empresa, TipoCliente, Activo);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_Clientes_idEmpresa_Correo'
      AND object_id = OBJECT_ID('dbo.Clientes')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Clientes_idEmpresa_Correo
        ON dbo.Clientes(idEmpresa, Correo)
        INCLUDE (Nombre, Telefono, Empresa, TipoCliente, Activo);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UX_ClientesNotas_identityKey'
      AND object_id = OBJECT_ID('dbo.ClientesNotas')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_ClientesNotas_identityKey
        ON dbo.ClientesNotas(identityKey);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_ClientesNotas_idEmpresa_idCliente_Activo_FechaCreacion'
      AND object_id = OBJECT_ID('dbo.ClientesNotas')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ClientesNotas_idEmpresa_idCliente_Activo_FechaCreacion
        ON dbo.ClientesNotas(idEmpresa, idCliente, Activo, FechaCreacion DESC)
        INCLUDE (EsTarea, FechaTarea, HoraTarea, Completada, FechaCompletada, Texto, FechaActualizacion);
END;
GO
