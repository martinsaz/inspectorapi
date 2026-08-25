SET NOCOUNT ON;

IF EXISTS (
    SELECT 1
    FROM dbo.ActivosMarcas
    GROUP BY idEmpresa, Codigo
    HAVING COUNT(*) > 1
)
BEGIN
    THROW 51000, 'No se puede crear UX_ActivosMarcas_IdEmpresa_Codigo porque existen códigos duplicados en dbo.ActivosMarcas.', 1;
END;

IF EXISTS (
    SELECT 1
    FROM dbo.ActivosProveedores
    GROUP BY idEmpresa, Codigo
    HAVING COUNT(*) > 1
)
BEGIN
    THROW 51001, 'No se puede crear UX_ActivosProveedores_IdEmpresa_Codigo porque existen códigos duplicados en dbo.ActivosProveedores.', 1;
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.ActivosMarcas')
      AND name = N'UX_ActivosMarcas_IdEmpresa_Codigo'
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_ActivosMarcas_IdEmpresa_Codigo
        ON dbo.ActivosMarcas(idEmpresa, Codigo);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.ActivosProveedores')
      AND name = N'UX_ActivosProveedores_IdEmpresa_Codigo'
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_ActivosProveedores_IdEmpresa_Codigo
        ON dbo.ActivosProveedores(idEmpresa, Codigo);
END;
