/*
    #MOKA - Bootstrap de permiso propio para Activos / Proveedores
    Scope funcional: Proveedores
    Permiso operativo: 03506003

    Idempotente. Sólo opera en bases que contienen dbo.Roles con JSON legacy
    de permisos. Las bases de alcance puramente transaccional pueden no tener
    Roles/Usuarios; en ese caso el AuthZ API valida contra la conexión de
    autorización configurada.
*/

SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.Roles', N'U') IS NULL
BEGIN
    PRINT 'dbo.Roles no existe en esta base; bootstrap de permiso omitido.';
    RETURN;
END;

DECLARE @Permiso nvarchar(16) = N'03506003';
DECLARE @Nodo nvarchar(max) = N'{"Opcion":"03506003","Texto":"Proveedores","Permisos":{"Acceso":1,"Escritura":1},"Hijos":[]}';

DECLARE @Roles table
(
    id uniqueidentifier NOT NULL,
    Permisos nvarchar(max) NOT NULL
);

INSERT INTO @Roles (id, Permisos)
SELECT id, Permisos
FROM dbo.Roles
WHERE ISJSON(Permisos) = 1
  AND Permisos NOT LIKE N'%' + @Permiso + N'%'
  AND Permisos LIKE N'%03506000%';

DECLARE @id uniqueidentifier;
DECLARE @permisos nvarchar(max);
DECLARE permisos_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT id, Permisos FROM @Roles;

OPEN permisos_cursor;
FETCH NEXT FROM permisos_cursor INTO @id, @permisos;

WHILE @@FETCH_STATUS = 0
BEGIN
    DECLARE @path nvarchar(256) = NULL;

    SELECT TOP (1) @path = CONCAT(N'$[', [key], N'].Hijos')
    FROM OPENJSON(@permisos)
    WHERE JSON_VALUE(value, N'$.Opcion') = N'03506000';

    IF @path IS NOT NULL
    BEGIN
        SET @permisos = JSON_MODIFY(@permisos, N'append ' + @path, JSON_QUERY(@Nodo));

        UPDATE dbo.Roles
        SET Permisos = @permisos
        WHERE id = @id;
    END;

    FETCH NEXT FROM permisos_cursor INTO @id, @permisos;
END;

CLOSE permisos_cursor;
DEALLOCATE permisos_cursor;
