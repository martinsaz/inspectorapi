/* TICKET 10 - Cierre PO: periodos calendarios sin conversión automática. */
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.ProductosServiciosUnidadesMedida', N'U') IS NULL
    THROW 51030, 'No existe dbo.ProductosServiciosUnidadesMedida.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Empresas TABLE (idEmpresa UNIQUEIDENTIFIER PRIMARY KEY);
    INSERT @Empresas (idEmpresa)
    SELECT DISTINCT idEmpresa FROM dbo.ProductosServiciosUnidadesMedida;

    /* No se define factor: año y periodos derivados no representan una duración calendaria fija. */
    INSERT dbo.ProductosServiciosUnidadesMedida
        (id, idEmpresa, identityKey, Codigo, Nombre, Abreviatura, PermiteDecimales, TipoUnidad, EsSistema, EsPersonalizada, FactorConversion, Convertible, ClaveSistema, Activo, FechaCreacion, FechaActualizacion)
    SELECT NEWID(), e.idEmpresa, NEWID(), s.Clave, s.Nombre, s.Abreviatura, 1, N'TIME', 1, 0, NULL, 0, s.Clave, 1, SYSUTCDATETIME(), SYSUTCDATETIME()
    FROM @Empresas e
    CROSS JOIN (VALUES
        (N'YEAR', N'Año', N'a'),
        (N'LUSTRUM', N'Lustro', N'lustro'),
        (N'DECADE', N'Década', N'década'),
        (N'CENTURY', N'Siglo', N'siglo')
    ) s(Clave, Nombre, Abreviatura)
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.ProductosServiciosUnidadesMedida u
        WHERE u.idEmpresa = e.idEmpresa AND u.EsSistema = 1 AND u.ClaveSistema = s.Clave);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
