/* TICKET 10 - Catálogo definitivo métrico + inglés/US. Factores hacia la unidad canónica de cada tipo. */
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.ProductosServiciosUnidadesMedida', N'U') IS NULL
    THROW 51040, 'No existe dbo.ProductosServiciosUnidadesMedida.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Empresas TABLE (idEmpresa UNIQUEIDENTIFIER PRIMARY KEY);
    INSERT @Empresas SELECT DISTINCT idEmpresa FROM dbo.ProductosServiciosUnidadesMedida;

    DECLARE @Catalogo TABLE (
        Clave NVARCHAR(30) PRIMARY KEY, Nombre NVARCHAR(100), Abreviatura NVARCHAR(20),
        Decimales BIT, Tipo NVARCHAR(20), Factor DECIMAL(28,12) NULL, Convertible BIT);

    INSERT @Catalogo VALUES
    (N'UG',N'Microgramo',N'µg',1,N'WEIGHT',0.000000001,1),(N'MG',N'Miligramo',N'mg',1,N'WEIGHT',0.000001,1),(N'G',N'Gramo',N'g',1,N'WEIGHT',0.001,1),(N'KG',N'Kilogramo',N'kg',1,N'WEIGHT',1,1),(N'T',N'Tonelada métrica',N't',1,N'WEIGHT',1000,1),(N'OZ',N'Onza',N'oz',1,N'WEIGHT',0.028349523125,1),(N'LB',N'Libra',N'lb',1,N'WEIGHT',0.45359237,1),(N'SHORT_TON',N'Tonelada corta',N'short ton',1,N'WEIGHT',907.18474,1),
    (N'UL',N'Microlitro',N'µL',1,N'VOLUME',0.000001,1),(N'ML',N'Mililitro',N'mL',1,N'VOLUME',0.001,1),(N'CL',N'Centilitro',N'cL',1,N'VOLUME',0.01,1),(N'DL',N'Decilitro',N'dL',1,N'VOLUME',0.1,1),(N'L',N'Litro',N'L',1,N'VOLUME',1,1),(N'MM3',N'Milímetro cúbico',N'mm³',1,N'VOLUME',0.000001,1),(N'CM3',N'Centímetro cúbico',N'cm³',1,N'VOLUME',0.001,1),(N'DM3',N'Decímetro cúbico',N'dm³',1,N'VOLUME',1,1),(N'M3',N'Metro cúbico',N'm³',1,N'VOLUME',1000,1),(N'FL_OZ',N'Onza líquida US',N'fl oz',1,N'VOLUME',0.029573529562,1),(N'CUP',N'Taza US',N'cup',1,N'VOLUME',0.2365882365,1),(N'PT',N'Pinta US',N'pt',1,N'VOLUME',0.473176473,1),(N'QT',N'Cuarto US',N'qt',1,N'VOLUME',0.946352946,1),(N'GAL',N'Galón US',N'US gal',1,N'VOLUME',3.785411784,1),
    (N'UM',N'Micrómetro',N'µm',1,N'LENGTH',0.000001,1),(N'MM',N'Milímetro',N'mm',1,N'LENGTH',0.001,1),(N'CM',N'Centímetro',N'cm',1,N'LENGTH',0.01,1),(N'DM',N'Decímetro',N'dm',1,N'LENGTH',0.1,1),(N'M',N'Metro',N'm',1,N'LENGTH',1,1),(N'KM',N'Kilómetro',N'km',1,N'LENGTH',1000,1),(N'IN',N'Pulgada',N'in',1,N'LENGTH',0.0254,1),(N'FT',N'Pie',N'ft',1,N'LENGTH',0.3048,1),(N'YD',N'Yarda',N'yd',1,N'LENGTH',0.9144,1),(N'MI',N'Milla',N'mi',1,N'LENGTH',1609.344,1),
    (N'MM2',N'Milímetro cuadrado',N'mm²',1,N'AREA',0.000001,1),(N'CM2',N'Centímetro cuadrado',N'cm²',1,N'AREA',0.0001,1),(N'DM2',N'Decímetro cuadrado',N'dm²',1,N'AREA',0.01,1),(N'M2',N'Metro cuadrado',N'm²',1,N'AREA',1,1),(N'HA',N'Hectárea',N'ha',1,N'AREA',10000,1),(N'KM2',N'Kilómetro cuadrado',N'km²',1,N'AREA',1000000,1),(N'IN2',N'Pulgada cuadrada',N'in²',1,N'AREA',0.00064516,1),(N'FT2',N'Pie cuadrado',N'ft²',1,N'AREA',0.09290304,1),(N'YD2',N'Yarda cuadrada',N'yd²',1,N'AREA',0.83612736,1),(N'AC',N'Acre',N'ac',1,N'AREA',4046.8564224,1),(N'MI2',N'Milla cuadrada',N'mi²',1,N'AREA',2589988.110336,1),
    (N'PZ',N'Pieza',N'pz',0,N'ITEM',NULL,0),
    (N'MS',N'Milisegundo',N'ms',1,N'TIME',0.000016666667,1),(N'S',N'Segundo',N's',1,N'TIME',0.016666666667,1),(N'MIN',N'Minuto',N'min',1,N'TIME',1,1),(N'H',N'Hora',N'h',1,N'TIME',60,1),(N'D',N'Día',N'd',1,N'TIME',1440,1),
    (N'YEAR',N'Año',N'año',0,N'TIME',NULL,0),(N'LUSTRUM',N'Lustro',N'lustro',0,N'TIME',NULL,0),(N'DECADE',N'Década',N'década',0,N'TIME',NULL,0),(N'CENTURY',N'Siglo',N'siglo',0,N'TIME',NULL,0);

    UPDATE u SET Nombre=c.Nombre, Abreviatura=c.Abreviatura, PermiteDecimales=c.Decimales, TipoUnidad=c.Tipo, FactorConversion=c.Factor, Convertible=c.Convertible, EsSistema=1, EsPersonalizada=0, Activo=1, FechaActualizacion=SYSUTCDATETIME()
    FROM dbo.ProductosServiciosUnidadesMedida u JOIN @Catalogo c ON c.Clave=u.ClaveSistema WHERE u.EsSistema=1;

    INSERT dbo.ProductosServiciosUnidadesMedida (id,idEmpresa,identityKey,Codigo,Nombre,Abreviatura,PermiteDecimales,TipoUnidad,EsSistema,EsPersonalizada,FactorConversion,Convertible,ClaveSistema,Activo,FechaCreacion,FechaActualizacion)
    SELECT NEWID(),e.idEmpresa,NEWID(),c.Clave,c.Nombre,c.Abreviatura,c.Decimales,c.Tipo,1,0,c.Factor,c.Convertible,c.Clave,1,SYSUTCDATETIME(),SYSUTCDATETIME()
    FROM @Empresas e CROSS JOIN @Catalogo c
    WHERE NOT EXISTS (SELECT 1 FROM dbo.ProductosServiciosUnidadesMedida u WHERE u.idEmpresa=e.idEmpresa AND u.EsSistema=1 AND u.ClaveSistema=c.Clave);

    /* Sólo permanecen unidades Sistema del catálogo definitivo; las históricas con referencias se preservan. */
    DELETE u FROM dbo.ProductosServiciosUnidadesMedida u
    WHERE u.EsSistema=1
      AND NOT EXISTS (SELECT 1 FROM @Catalogo c WHERE c.Clave=u.ClaveSistema)
      AND NOT EXISTS (SELECT 1 FROM dbo.ProductosServicios p WHERE p.idUnidadMedida=u.id)
      AND NOT EXISTS (SELECT 1 FROM dbo.ProductosServiciosPresentacionesVenta pv WHERE pv.idUnidadVenta=u.id)
      AND NOT EXISTS (SELECT 1 FROM dbo.CotizacionesPartidas cp WHERE cp.idUnidadMedida=u.id)
      AND NOT EXISTS (SELECT 1 FROM dbo.OrdenesCompraDetalle oc WHERE oc.idUnidadMedida=u.id);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
