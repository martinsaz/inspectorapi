/* TICKET 10 - Unidades de medida controladas. Ejecutar despues de ticket-09. */
SET XACT_ABORT ON;
    IF OBJECT_ID(N'dbo.ProductosServiciosUnidadesMedida', N'U') IS NULL
        THROW 51010, 'No existe dbo.ProductosServiciosUnidadesMedida.', 1;

    /* Pre-check y ALTER compatible: no se reemplazan ni eliminan datos legacy. */
    IF COL_LENGTH(N'dbo.ProductosServiciosUnidadesMedida', N'TipoUnidad') IS NULL
        ALTER TABLE dbo.ProductosServiciosUnidadesMedida ADD TipoUnidad NVARCHAR(20) NULL;
    IF COL_LENGTH(N'dbo.ProductosServiciosUnidadesMedida', N'EsSistema') IS NULL
        ALTER TABLE dbo.ProductosServiciosUnidadesMedida ADD EsSistema BIT NOT NULL CONSTRAINT DF_PSUnidades_EsSistema DEFAULT ((0));
    IF COL_LENGTH(N'dbo.ProductosServiciosUnidadesMedida', N'EsPersonalizada') IS NULL
        ALTER TABLE dbo.ProductosServiciosUnidadesMedida ADD EsPersonalizada BIT NOT NULL CONSTRAINT DF_PSUnidades_EsPersonalizada DEFAULT ((1));
    IF COL_LENGTH(N'dbo.ProductosServiciosUnidadesMedida', N'FactorConversion') IS NULL
        ALTER TABLE dbo.ProductosServiciosUnidadesMedida ADD FactorConversion DECIMAL(28,12) NULL;
    IF COL_LENGTH(N'dbo.ProductosServiciosUnidadesMedida', N'Convertible') IS NULL
        ALTER TABLE dbo.ProductosServiciosUnidadesMedida ADD Convertible BIT NOT NULL CONSTRAINT DF_PSUnidades_Convertible DEFAULT ((0));
    IF COL_LENGTH(N'dbo.ProductosServiciosUnidadesMedida', N'ClaveSistema') IS NULL
        ALTER TABLE dbo.ProductosServiciosUnidadesMedida ADD ClaveSistema NVARCHAR(30) NULL;

    IF OBJECT_ID(N'dbo.ProductosServiciosPresentacionesVenta', N'U') IS NOT NULL
    BEGIN
        IF COL_LENGTH(N'dbo.ProductosServiciosPresentacionesVenta', N'CantidadVenta') IS NULL
            ALTER TABLE dbo.ProductosServiciosPresentacionesVenta ADD CantidadVenta DECIMAL(18,4) NOT NULL CONSTRAINT DF_PSPresentaciones_CantidadVenta DEFAULT ((1));
        IF COL_LENGTH(N'dbo.ProductosServiciosPresentacionesVenta', N'idUnidadVenta') IS NULL
            ALTER TABLE dbo.ProductosServiciosPresentacionesVenta ADD idUnidadVenta UNIQUEIDENTIFIER NULL;
    END;

GO

BEGIN TRY
    BEGIN TRANSACTION;

    UPDATE dbo.ProductosServiciosUnidadesMedida
    SET TipoUnidad = CASE
            WHEN Nombre IN (N'Miligramo', N'Gramo', N'Kilogramo') THEN N'WEIGHT'
            WHEN Nombre = N'Litro' THEN N'VOLUME'
            WHEN Nombre = N'Pieza' THEN N'ITEM'
            ELSE N'OTHER' END,
        FactorConversion = CASE
            WHEN Nombre = N'Miligramo' THEN CAST(0.000001 AS DECIMAL(28,12))
            WHEN Nombre = N'Gramo' THEN CAST(0.001 AS DECIMAL(28,12))
            WHEN Nombre = N'Litro' THEN CAST(1 AS DECIMAL(28,12))
            WHEN Nombre = N'Pieza' THEN CAST(1 AS DECIMAL(28,12))
            WHEN Nombre = N'Kilogramo' AND Abreviatura = N'KM' THEN CAST(1 AS DECIMAL(28,12))
            ELSE NULL END,
        Convertible = CASE WHEN Nombre IN (N'Miligramo', N'Gramo', N'Kilogramo', N'Litro', N'Pieza') THEN 1 ELSE 0 END,
        EsPersonalizada = 1
    WHERE TipoUnidad IS NULL;

    ALTER TABLE dbo.ProductosServiciosUnidadesMedida ALTER COLUMN TipoUnidad NVARCHAR(20) NOT NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_PSUnidades_TipoUnidad')
        ALTER TABLE dbo.ProductosServiciosUnidadesMedida ADD CONSTRAINT CK_PSUnidades_TipoUnidad CHECK (TipoUnidad IN (N'WEIGHT',N'VOLUME',N'LENGTH',N'AREA',N'ITEM',N'OTHER'));
    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_PSUnidades_Conversion')
        ALTER TABLE dbo.ProductosServiciosUnidadesMedida ADD CONSTRAINT CK_PSUnidades_Conversion CHECK ((Convertible = 0 AND FactorConversion IS NULL) OR (Convertible = 1 AND FactorConversion > 0));

    /* La presentación persiste la unidad de venta; EquivalenciaBase continúa siendo la entrada del motor certificado. */
    IF OBJECT_ID(N'dbo.ProductosServiciosPresentacionesVenta', N'U') IS NOT NULL
    BEGIN
        UPDATE pv SET idUnidadVenta = ps.idUnidadMedida
        FROM dbo.ProductosServiciosPresentacionesVenta pv
        INNER JOIN dbo.ProductosServicios ps ON ps.idEmpresa = pv.idEmpresa AND ps.id = pv.idProductoServicio
        WHERE pv.idUnidadVenta IS NULL;
        ALTER TABLE dbo.ProductosServiciosPresentacionesVenta ALTER COLUMN idUnidadVenta UNIQUEIDENTIFIER NOT NULL;
        IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_PSPresentaciones_CantidadVenta')
            ALTER TABLE dbo.ProductosServiciosPresentacionesVenta ADD CONSTRAINT CK_PSPresentaciones_CantidadVenta CHECK (CantidadVenta > 0);
    END;

    /* Catálogo estándar por empresa; ClaveSistema evita duplicados sin reescribir aliases históricos. */
    DECLARE @E TABLE (idEmpresa UNIQUEIDENTIFIER PRIMARY KEY);
    INSERT @E SELECT DISTINCT idEmpresa FROM dbo.ProductosServiciosUnidadesMedida;
    INSERT dbo.ProductosServiciosUnidadesMedida
        (id,idEmpresa,identityKey,Codigo,Nombre,Abreviatura,PermiteDecimales,TipoUnidad,EsSistema,EsPersonalizada,FactorConversion,Convertible,ClaveSistema,Activo,FechaCreacion,FechaActualizacion)
    SELECT NEWID(), e.idEmpresa, NEWID(), s.Clave, s.Nombre, s.Abreviatura, s.Decimales, s.Tipo, 1, 0, s.Factor, 1, s.Clave, 1, SYSUTCDATETIME(), SYSUTCDATETIME()
    FROM @E e CROSS JOIN (VALUES
        (N'MG',N'Miligramo',N'mg',CAST(1 AS bit),N'WEIGHT',CAST(0.000001 AS DECIMAL(28,12))), (N'G',N'Gramo',N'g',CAST(1 AS bit),N'WEIGHT',CAST(0.001 AS DECIMAL(28,12))), (N'KG',N'Kilogramo',N'kg',CAST(1 AS bit),N'WEIGHT',CAST(1 AS DECIMAL(28,12))), (N'OZ',N'Onza',N'oz',CAST(1 AS bit),N'WEIGHT',CAST(0.028349523125 AS DECIMAL(28,12))), (N'LB',N'Libra',N'lb',CAST(1 AS bit),N'WEIGHT',CAST(0.45359237 AS DECIMAL(28,12))),
        (N'ML',N'Mililitro',N'ml',CAST(1 AS bit),N'VOLUME',CAST(0.001 AS DECIMAL(28,12))), (N'L',N'Litro',N'L',CAST(1 AS bit),N'VOLUME',CAST(1 AS DECIMAL(28,12))), (N'FLOZ',N'Onza líquida',N'fl oz',CAST(1 AS bit),N'VOLUME',CAST(0.029573529562 AS DECIMAL(28,12))), (N'PT',N'Pinta',N'pt',CAST(1 AS bit),N'VOLUME',CAST(0.473176473 AS DECIMAL(28,12))), (N'QT',N'Cuarto',N'qt',CAST(1 AS bit),N'VOLUME',CAST(0.946352946 AS DECIMAL(28,12))), (N'GAL',N'Galón',N'gal',CAST(1 AS bit),N'VOLUME',CAST(3.785411784 AS DECIMAL(28,12))),
        (N'MM',N'Milímetro',N'mm',CAST(1 AS bit),N'LENGTH',CAST(0.001 AS DECIMAL(28,12))), (N'CM',N'Centímetro',N'cm',CAST(1 AS bit),N'LENGTH',CAST(0.01 AS DECIMAL(28,12))), (N'M',N'Metro',N'm',CAST(1 AS bit),N'LENGTH',CAST(1 AS DECIMAL(28,12))), (N'KM',N'Kilómetro',N'km',CAST(1 AS bit),N'LENGTH',CAST(1000 AS DECIMAL(28,12))), (N'IN',N'Pulgada',N'in',CAST(1 AS bit),N'LENGTH',CAST(0.0254 AS DECIMAL(28,12))), (N'FT',N'Pie',N'ft',CAST(1 AS bit),N'LENGTH',CAST(0.3048 AS DECIMAL(28,12))), (N'YD',N'Yarda',N'yd',CAST(1 AS bit),N'LENGTH',CAST(0.9144 AS DECIMAL(28,12))),
        (N'CM2',N'Centímetro cuadrado',N'cm²',CAST(1 AS bit),N'AREA',CAST(0.0001 AS DECIMAL(28,12))), (N'M2',N'Metro cuadrado',N'm²',CAST(1 AS bit),N'AREA',CAST(1 AS DECIMAL(28,12))), (N'FT2',N'Pie cuadrado',N'ft²',CAST(1 AS bit),N'AREA',CAST(0.09290304 AS DECIMAL(28,12))), (N'PZ',N'Pieza',N'pz',CAST(0 AS bit),N'ITEM',CAST(1 AS DECIMAL(28,12)))
    ) s(Clave,Nombre,Abreviatura,Decimales,Tipo,Factor)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.ProductosServiciosUnidadesMedida u WHERE u.idEmpresa=e.idEmpresa AND u.EsSistema=1 AND u.ClaveSistema=s.Clave);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.ProductosServiciosUnidadesMedida') AND name=N'UX_PSUnidades_Empresa_ClaveSistema')
        CREATE UNIQUE NONCLUSTERED INDEX UX_PSUnidades_Empresa_ClaveSistema ON dbo.ProductosServiciosUnidadesMedida(idEmpresa,ClaveSistema) WHERE ClaveSistema IS NOT NULL;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
