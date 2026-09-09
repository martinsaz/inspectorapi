/* TICKET 10 - Ampliación de unidades Sistema, TIME y depuración de legacy 004/005. */
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.ProductosServiciosUnidadesMedida', N'U') IS NULL
    THROW 51020, 'No existe dbo.ProductosServiciosUnidadesMedida.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    /* TIME es un tipo controlado; sólo el catálogo Sistema puede crear unidades físicas. */
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.ProductosServiciosUnidadesMedida') AND name = N'CK_PSUnidades_TipoUnidad')
        ALTER TABLE dbo.ProductosServiciosUnidadesMedida DROP CONSTRAINT CK_PSUnidades_TipoUnidad;
    ALTER TABLE dbo.ProductosServiciosUnidadesMedida ADD CONSTRAINT CK_PSUnidades_TipoUnidad
        CHECK (TipoUnidad IN (N'WEIGHT', N'VOLUME', N'LENGTH', N'AREA', N'ITEM', N'TIME', N'OTHER'));

    DECLARE @Empresas TABLE (idEmpresa UNIQUEIDENTIFIER PRIMARY KEY);
    INSERT @Empresas (idEmpresa)
    SELECT DISTINCT idEmpresa FROM dbo.ProductosServiciosUnidadesMedida;

    /* GAL ya equivale exactamente al galón líquido estadounidense y no tiene referencias que preservar. */
    IF EXISTS (
        SELECT 1 FROM dbo.ProductosServiciosUnidadesMedida u
        WHERE u.EsSistema = 1 AND u.ClaveSistema = N'GAL'
          AND (EXISTS (SELECT 1 FROM dbo.ProductosServicios p WHERE p.idUnidadMedida = u.id)
            OR EXISTS (SELECT 1 FROM dbo.ProductosServiciosPresentacionesVenta pv WHERE pv.idUnidadVenta = u.id)
            OR EXISTS (SELECT 1 FROM dbo.CotizacionesPartidas cp WHERE cp.idUnidadMedida = u.id)
            OR EXISTS (SELECT 1 FROM dbo.OrdenesCompraDetalle oc WHERE oc.idUnidadMedida = u.id)))
        THROW 51021, 'GAL tiene referencias; no se puede normalizar su estándar sin una migración histórica explícita.', 1;

    UPDATE dbo.ProductosServiciosUnidadesMedida
    SET Nombre = N'Galón USA', Abreviatura = N'US gal', FechaActualizacion = SYSUTCDATETIME()
    WHERE EsSistema = 1 AND ClaveSistema = N'GAL' AND FactorConversion = CAST(3.785411784 AS DECIMAL(28,12));

    INSERT dbo.ProductosServiciosUnidadesMedida
        (id, idEmpresa, identityKey, Codigo, Nombre, Abreviatura, PermiteDecimales, TipoUnidad, EsSistema, EsPersonalizada, FactorConversion, Convertible, ClaveSistema, Activo, FechaCreacion, FechaActualizacion)
    SELECT NEWID(), e.idEmpresa, NEWID(), s.Clave, s.Nombre, s.Abreviatura, s.Decimales, s.Tipo, 1, 0, s.Factor, 1, s.Clave, 1, SYSUTCDATETIME(), SYSUTCDATETIME()
    FROM @Empresas e
    CROSS JOIN (VALUES
        (N'ANGSTROM', N'Ångström', N'Å', CAST(1 AS bit), N'LENGTH', CAST(0.000000000100 AS DECIMAL(28,12))),
        (N'MI', N'Milla terrestre', N'mi', CAST(1 AS bit), N'LENGTH', CAST(1609.344 AS DECIMAL(28,12))),
        (N'NMI', N'Milla marítima', N'nmi', CAST(1 AS bit), N'LENGTH', CAST(1852 AS DECIMAL(28,12))),
        (N'FATHOM', N'Braza', N'ftm', CAST(1 AS bit), N'LENGTH', CAST(1.8288 AS DECIMAL(28,12))),
        (N'LY', N'Año luz', N'ly', CAST(1 AS bit), N'LENGTH', CAST(9460730472580800 AS DECIMAL(28,12))),
        (N'T', N'Tonelada métrica', N't', CAST(1 AS bit), N'WEIGHT', CAST(1000 AS DECIMAL(28,12))),
        (N'CT', N'Quilate métrico', N'ct', CAST(1 AS bit), N'WEIGHT', CAST(0.0002 AS DECIMAL(28,12))),
        (N'IN3', N'Pulgada cúbica', N'in3', CAST(1 AS bit), N'VOLUME', CAST(0.016387064 AS DECIMAL(28,12))),
        (N'FT3', N'Pie cúbico', N'ft3', CAST(1 AS bit), N'VOLUME', CAST(28.316846592 AS DECIMAL(28,12))),
        (N'GAL_IMP', N'Galón inglés', N'imp gal', CAST(1 AS bit), N'VOLUME', CAST(4.54609 AS DECIMAL(28,12))),
        (N'HA', N'Hectárea', N'ha', CAST(1 AS bit), N'AREA', CAST(10000 AS DECIMAL(28,12))),
        (N'IN2', N'Pulgada cuadrada', N'in2', CAST(1 AS bit), N'AREA', CAST(0.00064516 AS DECIMAL(28,12))),
        (N'AC', N'Acre', N'ac', CAST(1 AS bit), N'AREA', CAST(4046.8564224 AS DECIMAL(28,12))),
        (N'MI2', N'Milla cuadrada', N'mi2', CAST(1 AS bit), N'AREA', CAST(2589988.110336 AS DECIMAL(28,12))),
        (N'MIN', N'Minuto', N'min', CAST(1 AS bit), N'TIME', CAST(1 AS DECIMAL(28,12))),
        (N'H', N'Hora', N'h', CAST(1 AS bit), N'TIME', CAST(60 AS DECIMAL(28,12))),
        (N'D', N'Día', N'd', CAST(1 AS bit), N'TIME', CAST(1440 AS DECIMAL(28,12)))
    ) s(Clave, Nombre, Abreviatura, Decimales, Tipo, Factor)
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.ProductosServiciosUnidadesMedida u
        WHERE u.idEmpresa = e.idEmpresa AND u.EsSistema = 1 AND u.ClaveSistema = s.Clave);

    DECLARE @PiezaSistema TABLE (idEmpresa UNIQUEIDENTIFIER PRIMARY KEY, id UNIQUEIDENTIFIER NOT NULL);
    DECLARE @HoraSistema TABLE (idEmpresa UNIQUEIDENTIFIER PRIMARY KEY, id UNIQUEIDENTIFIER NOT NULL);
    INSERT @PiezaSistema SELECT idEmpresa, id FROM dbo.ProductosServiciosUnidadesMedida WHERE EsSistema = 1 AND ClaveSistema = N'PZ';
    INSERT @HoraSistema SELECT idEmpresa, id FROM dbo.ProductosServiciosUnidadesMedida WHERE EsSistema = 1 AND ClaveSistema = N'H';

    IF EXISTS (SELECT 1 FROM @Empresas e WHERE NOT EXISTS (SELECT 1 FROM @PiezaSistema p WHERE p.idEmpresa = e.idEmpresa))
        THROW 51022, 'Falta Pieza Sistema PZ para una empresa.', 1;
    IF EXISTS (SELECT 1 FROM @Empresas e WHERE NOT EXISTS (SELECT 1 FROM @HoraSistema h WHERE h.idEmpresa = e.idEmpresa))
        THROW 51023, 'Falta Hora Sistema H para una empresa.', 1;

    /* Sólo se cambian identificadores de unidad; snapshots textuales y financieros permanecen intactos. */
    UPDATE p SET idUnidadMedida = s.id, FechaActualizacion = SYSUTCDATETIME()
    FROM dbo.ProductosServicios p
    JOIN dbo.ProductosServiciosUnidadesMedida u ON u.id = p.idUnidadMedida
    JOIN @PiezaSistema s ON s.idEmpresa = p.idEmpresa
    WHERE u.EsSistema = 0 AND u.Codigo = N'004' AND u.Nombre = N'Pieza' AND u.Abreviatura = N'PZA';
    UPDATE pv SET idUnidadVenta = s.id, FechaActualizacion = SYSUTCDATETIME()
    FROM dbo.ProductosServiciosPresentacionesVenta pv
    JOIN dbo.ProductosServiciosUnidadesMedida u ON u.id = pv.idUnidadVenta
    JOIN @PiezaSistema s ON s.idEmpresa = pv.idEmpresa
    WHERE u.EsSistema = 0 AND u.Codigo = N'004' AND u.Nombre = N'Pieza' AND u.Abreviatura = N'PZA';
    UPDATE cp SET idUnidadMedida = s.id
    FROM dbo.CotizacionesPartidas cp
    JOIN dbo.ProductosServiciosUnidadesMedida u ON u.id = cp.idUnidadMedida
    JOIN @PiezaSistema s ON s.idEmpresa = cp.idEmpresa
    WHERE u.EsSistema = 0 AND u.Codigo = N'004' AND u.Nombre = N'Pieza' AND u.Abreviatura = N'PZA';
    UPDATE oc SET idUnidadMedida = s.id
    FROM dbo.OrdenesCompraDetalle oc
    JOIN dbo.ProductosServiciosUnidadesMedida u ON u.id = oc.idUnidadMedida
    JOIN @PiezaSistema s ON s.idEmpresa = oc.idEmpresa
    WHERE u.EsSistema = 0 AND u.Codigo = N'004' AND u.Nombre = N'Pieza' AND u.Abreviatura = N'PZA';

    UPDATE p SET idUnidadMedida = s.id, FechaActualizacion = SYSUTCDATETIME()
    FROM dbo.ProductosServicios p
    JOIN dbo.ProductosServiciosUnidadesMedida u ON u.id = p.idUnidadMedida
    JOIN @HoraSistema s ON s.idEmpresa = p.idEmpresa
    WHERE u.EsSistema = 0 AND u.Codigo = N'005' AND u.Nombre = N'Hora' AND u.Abreviatura = N'HR';
    UPDATE pv SET idUnidadVenta = s.id, FechaActualizacion = SYSUTCDATETIME()
    FROM dbo.ProductosServiciosPresentacionesVenta pv
    JOIN dbo.ProductosServiciosUnidadesMedida u ON u.id = pv.idUnidadVenta
    JOIN @HoraSistema s ON s.idEmpresa = pv.idEmpresa
    WHERE u.EsSistema = 0 AND u.Codigo = N'005' AND u.Nombre = N'Hora' AND u.Abreviatura = N'HR';
    UPDATE cp SET idUnidadMedida = s.id
    FROM dbo.CotizacionesPartidas cp
    JOIN dbo.ProductosServiciosUnidadesMedida u ON u.id = cp.idUnidadMedida
    JOIN @HoraSistema s ON s.idEmpresa = cp.idEmpresa
    WHERE u.EsSistema = 0 AND u.Codigo = N'005' AND u.Nombre = N'Hora' AND u.Abreviatura = N'HR';
    UPDATE oc SET idUnidadMedida = s.id
    FROM dbo.OrdenesCompraDetalle oc
    JOIN dbo.ProductosServiciosUnidadesMedida u ON u.id = oc.idUnidadMedida
    JOIN @HoraSistema s ON s.idEmpresa = oc.idEmpresa
    WHERE u.EsSistema = 0 AND u.Codigo = N'005' AND u.Nombre = N'Hora' AND u.Abreviatura = N'HR';

    DELETE u FROM dbo.ProductosServiciosUnidadesMedida u
    WHERE u.EsSistema = 0 AND u.Codigo = N'004' AND u.Nombre = N'Pieza' AND u.Abreviatura = N'PZA'
      AND NOT EXISTS (SELECT 1 FROM dbo.ProductosServicios p WHERE p.idUnidadMedida = u.id)
      AND NOT EXISTS (SELECT 1 FROM dbo.ProductosServiciosPresentacionesVenta pv WHERE pv.idUnidadVenta = u.id)
      AND NOT EXISTS (SELECT 1 FROM dbo.CotizacionesPartidas cp WHERE cp.idUnidadMedida = u.id)
      AND NOT EXISTS (SELECT 1 FROM dbo.OrdenesCompraDetalle oc WHERE oc.idUnidadMedida = u.id);
    DELETE u FROM dbo.ProductosServiciosUnidadesMedida u
    WHERE u.EsSistema = 0 AND u.Codigo = N'005' AND u.Nombre = N'Hora' AND u.Abreviatura = N'HR'
      AND NOT EXISTS (SELECT 1 FROM dbo.ProductosServicios p WHERE p.idUnidadMedida = u.id)
      AND NOT EXISTS (SELECT 1 FROM dbo.ProductosServiciosPresentacionesVenta pv WHERE pv.idUnidadVenta = u.id)
      AND NOT EXISTS (SELECT 1 FROM dbo.CotizacionesPartidas cp WHERE cp.idUnidadMedida = u.id)
      AND NOT EXISTS (SELECT 1 FROM dbo.OrdenesCompraDetalle oc WHERE oc.idUnidadMedida = u.id);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
