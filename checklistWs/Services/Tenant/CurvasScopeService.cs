using System.Data;
using System.Data.SqlClient;

namespace checklistWs.Services.Tenant
{
    public interface ICurvasScopeService
    {
        Task<Guid> CrearCurvaAsync(TenantDatabaseDescriptor descriptor, Guid idEmpresa, CurvaCrearRequest request, CancellationToken cancellationToken = default);
        Task<Guid> AgregarDetalleAsync(TenantDatabaseDescriptor descriptor, Guid idEmpresa, CurvaDetalleRequest request, CancellationToken cancellationToken = default);
        Task<Guid> SembrarAsync(TenantDatabaseDescriptor descriptor, Guid idEmpresa, CurvaSiembraRequest request, CancellationToken cancellationToken = default);
        Task<CurvaOperacionResult> CrearOperacionAsync(TenantDatabaseDescriptor descriptor, Guid idEmpresa, CurvaOperacionRequest request, CancellationToken cancellationToken = default);
        Task<Guid> RelacionarOrdenCompraAsync(TenantDatabaseDescriptor descriptor, Guid idEmpresa, CurvaOperacionOrdenCompraRequest request, CancellationToken cancellationToken = default);
        Task<Guid> CrearSnapshotAsync(TenantDatabaseDescriptor descriptor, Guid idEmpresa, CurvaSnapshotRequest request, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<CurvasCatalogoItemDto>> ListarCatalogoAsync(TenantDatabaseDescriptor descriptor, Guid idEmpresa, CurvasCatalogoListRequest request, CancellationToken cancellationToken = default);
        Task<CurvasCatalogoDetalleDto?> ObtenerCatalogoAsync(TenantDatabaseDescriptor descriptor, Guid idEmpresa, Guid idCurva, CancellationToken cancellationToken = default);
        Task<Guid> GuardarCatalogoAsync(TenantDatabaseDescriptor descriptor, Guid idEmpresa, CurvasCatalogoGuardarRequest request, CancellationToken cancellationToken = default);
        Task BajaCatalogoAsync(TenantDatabaseDescriptor descriptor, Guid idEmpresa, Guid idCurva, Guid? usuarioId, CancellationToken cancellationToken = default);
        Task ReactivarCatalogoAsync(TenantDatabaseDescriptor descriptor, Guid idEmpresa, Guid idCurva, Guid? usuarioId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<CurvasProductoElegibleDto>> BuscarProductosElegiblesAsync(TenantDatabaseDescriptor descriptor, Guid idEmpresa, string? busqueda, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<CurvasVarianteElegibleDto>> ObtenerVariantesAsync(TenantDatabaseDescriptor descriptor, Guid idEmpresa, Guid idProductoServicio, CancellationToken cancellationToken = default);
    }

    public sealed class CurvasScopeService : ICurvasScopeService
    {
        private readonly ITenantSqlConnectionFactory _connectionFactory;

        public CurvasScopeService(ITenantSqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<Guid> CrearCurvaAsync(TenantDatabaseDescriptor descriptor, Guid idEmpresa, CurvaCrearRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Nombre))
            {
                throw new InvalidOperationException("CURVA_NOMBRE_REQUERIDO");
            }

            await using SqlConnection connection = _connectionFactory.CreateConnection(descriptor);
            await connection.OpenAsync(cancellationToken);
            using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable);
            Guid id = Guid.NewGuid();
            await ExecuteAsync(connection, transaction, @"
INSERT INTO dbo.CurvasCatalogo
(id, idEmpresa, identityKey, Nombre, Codigo, Estado, Activo, FechaCreacion, FechaActualizacion, idUsuarioCreacion)
VALUES
(@Id, @IdEmpresa, NEWID(), @Nombre, @Codigo, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME(), @Usuario)", cancellationToken,
                ("@Id", id),
                ("@IdEmpresa", idEmpresa),
                ("@Nombre", request.Nombre.Trim()),
                ("@Codigo", Db(request.Codigo)),
                ("@Usuario", Db(request.UsuarioId)));
            transaction.Commit();
            return id;
        }

        public async Task<Guid> AgregarDetalleAsync(TenantDatabaseDescriptor descriptor, Guid idEmpresa, CurvaDetalleRequest request, CancellationToken cancellationToken = default)
        {
            if (request.CantidadBaseObjetivo < 0)
            {
                throw new InvalidOperationException("CURVA_OBJETIVO_INVALIDO");
            }

            await using SqlConnection connection = _connectionFactory.CreateConnection(descriptor);
            await connection.OpenAsync(cancellationToken);
            using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable);
            await EnsureCurvaActivaAsync(connection, transaction, idEmpresa, request.IdCurva, cancellationToken);
            await EnsureProductoAsync(connection, transaction, idEmpresa, request.IdProductoServicio, requireProducto: true, cancellationToken);
            if (request.IdVariante.HasValue)
            {
                await EnsureVarianteAsync(connection, transaction, idEmpresa, request.IdProductoServicio, request.IdVariante.Value, cancellationToken);
            }

            Guid id = Guid.NewGuid();
            await ExecuteAsync(connection, transaction, @"
INSERT INTO dbo.CurvasDetalle
(id, idEmpresa, identityKey, idCurva, idProductoServicio, idVariante, TipoProductoServicio, CantidadBaseObjetivo, Activo, FechaCreacion, FechaActualizacion, idUsuarioCreacion)
VALUES
(@Id, @IdEmpresa, NEWID(), @IdCurva, @IdProductoServicio, @IdVariante, 1, @Cantidad, 1, SYSUTCDATETIME(), SYSUTCDATETIME(), @Usuario)", cancellationToken,
                ("@Id", id),
                ("@IdEmpresa", idEmpresa),
                ("@IdCurva", request.IdCurva),
                ("@IdProductoServicio", request.IdProductoServicio),
                ("@IdVariante", Db(request.IdVariante)),
                ("@Cantidad", request.CantidadBaseObjetivo),
                ("@Usuario", Db(request.UsuarioId)));
            transaction.Commit();
            return id;
        }

        public async Task<IReadOnlyList<CurvasCatalogoItemDto>> ListarCatalogoAsync(TenantDatabaseDescriptor descriptor, Guid idEmpresa, CurvasCatalogoListRequest request, CancellationToken cancellationToken = default)
        {
            string estatus = NormalizeEstatus(request?.Estatus);
            string search = (request?.Busqueda ?? string.Empty).Trim();
            await using SqlConnection connection = _connectionFactory.CreateConnection(descriptor);
            await connection.OpenAsync(cancellationToken);

            string statusFilter = estatus switch
            {
                "inactivos" => "AND c.Activo = 0",
                "todos" => string.Empty,
                _ => "AND c.Activo = 1"
            };

            using SqlCommand command = new($@"
SELECT
    c.id,
    c.Nombre,
    ISNULL(c.Codigo, '') AS Codigo,
    c.Estado,
    c.Activo,
    c.FechaActualizacion,
    COUNT(d.id) AS Renglones,
    COALESCE(SUM(d.CantidadBaseObjetivo), 0) AS PiezasObjetivo
FROM dbo.CurvasCatalogo c
LEFT JOIN dbo.CurvasDetalle d
    ON d.idEmpresa = c.idEmpresa
   AND d.idCurva = c.id
   AND d.Activo = 1
   AND d.FechaArchivado IS NULL
WHERE c.idEmpresa = @IdEmpresa
  {statusFilter}
  AND (@Busqueda = '' OR c.Nombre LIKE @BusquedaLike OR c.Codigo LIKE @BusquedaLike)
GROUP BY c.id, c.Nombre, c.Codigo, c.Estado, c.Activo, c.FechaActualizacion
ORDER BY c.Activo DESC, c.Nombre ASC", connection);
            command.Parameters.Add("@IdEmpresa", SqlDbType.UniqueIdentifier).Value = idEmpresa;
            command.Parameters.Add("@Busqueda", SqlDbType.NVarChar, 150).Value = search;
            command.Parameters.Add("@BusquedaLike", SqlDbType.NVarChar, 160).Value = $"%{search}%";

            List<CurvasCatalogoItemDto> result = new();
            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(new CurvasCatalogoItemDto
                {
                    Id = reader.GetGuid(reader.GetOrdinal("id")),
                    Nombre = ReadString(reader, "Nombre"),
                    Codigo = ReadString(reader, "Codigo"),
                    Estado = Convert.ToByte(reader["Estado"]),
                    Activo = Convert.ToBoolean(reader["Activo"]),
                    FechaActualizacion = Convert.ToDateTime(reader["FechaActualizacion"]),
                    Renglones = Convert.ToInt32(reader["Renglones"]),
                    PiezasObjetivo = Convert.ToDecimal(reader["PiezasObjetivo"])
                });
            }

            return result;
        }

        public async Task<CurvasCatalogoDetalleDto?> ObtenerCatalogoAsync(TenantDatabaseDescriptor descriptor, Guid idEmpresa, Guid idCurva, CancellationToken cancellationToken = default)
        {
            await using SqlConnection connection = _connectionFactory.CreateConnection(descriptor);
            await connection.OpenAsync(cancellationToken);

            CurvasCatalogoDetalleDto? curva = null;
            using (SqlCommand command = new(@"
SELECT c.id, c.Nombre, ISNULL(c.Codigo, '') AS Codigo, c.Estado, c.Activo, c.FechaActualizacion
FROM dbo.CurvasCatalogo c
WHERE c.idEmpresa = @IdEmpresa AND c.id = @Id", connection))
            {
                command.Parameters.Add("@IdEmpresa", SqlDbType.UniqueIdentifier).Value = idEmpresa;
                command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = idCurva;
                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
                if (await reader.ReadAsync(cancellationToken))
                {
                    curva = new CurvasCatalogoDetalleDto
                    {
                        Id = reader.GetGuid(reader.GetOrdinal("id")),
                        Nombre = ReadString(reader, "Nombre"),
                        Codigo = ReadString(reader, "Codigo"),
                        Estado = Convert.ToByte(reader["Estado"]),
                        Activo = Convert.ToBoolean(reader["Activo"]),
                        FechaActualizacion = Convert.ToDateTime(reader["FechaActualizacion"])
                    };
                }
            }

            if (curva == null)
            {
                return null;
            }

            List<CurvasCatalogoDetalleRenglonDto> detalles = new();
            using (SqlCommand command = new(@"
SELECT
    d.id,
    d.idProductoServicio,
    d.idVariante,
    ps.Nombre AS Producto,
    ISNULL(ps.Codigo, '') AS CodigoProducto,
    ISNULL(pv.Nombre, '') AS Variante,
    ISNULL(um.Abreviatura, um.Nombre) AS UnidadBase,
    d.CantidadBaseObjetivo
FROM dbo.CurvasDetalle d
INNER JOIN dbo.ProductosServicios ps
    ON ps.idEmpresa = d.idEmpresa
   AND ps.id = d.idProductoServicio
LEFT JOIN dbo.ProductosServiciosVariantes pv
    ON pv.idEmpresa = d.idEmpresa
   AND pv.id = d.idVariante
LEFT JOIN dbo.ProductosServiciosUnidadesMedida um
    ON um.idEmpresa = ps.idEmpresa
   AND um.id = ps.idUnidadMedida
WHERE d.idEmpresa = @IdEmpresa
  AND d.idCurva = @IdCurva
  AND d.Activo = 1
  AND d.FechaArchivado IS NULL
ORDER BY ps.Nombre, pv.Nombre", connection))
            {
                command.Parameters.Add("@IdEmpresa", SqlDbType.UniqueIdentifier).Value = idEmpresa;
                command.Parameters.Add("@IdCurva", SqlDbType.UniqueIdentifier).Value = idCurva;
                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    detalles.Add(new CurvasCatalogoDetalleRenglonDto
                    {
                        Id = reader.GetGuid(reader.GetOrdinal("id")),
                        IdProductoServicio = reader.GetGuid(reader.GetOrdinal("idProductoServicio")),
                        IdVariante = ReadNullableGuid(reader, "idVariante"),
                        Producto = ReadString(reader, "Producto"),
                        CodigoProducto = ReadString(reader, "CodigoProducto"),
                        Variante = ReadString(reader, "Variante"),
                        UnidadBase = ReadString(reader, "UnidadBase"),
                        CantidadBaseObjetivo = Convert.ToDecimal(reader["CantidadBaseObjetivo"])
                    });
                }
            }

            curva.Detalles = detalles;
            curva.Renglones = detalles.Count;
            curva.PiezasObjetivo = detalles.Sum(item => item.CantidadBaseObjetivo);
            return curva;
        }

        public async Task<Guid> GuardarCatalogoAsync(TenantDatabaseDescriptor descriptor, Guid idEmpresa, CurvasCatalogoGuardarRequest request, CancellationToken cancellationToken = default)
        {
            ValidateCatalogoRequest(request);
            await using SqlConnection connection = _connectionFactory.CreateConnection(descriptor);
            await connection.OpenAsync(cancellationToken);
            using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable);

            Guid idCurva = request.Id.HasValue && request.Id.Value != Guid.Empty ? request.Id.Value : Guid.NewGuid();
            bool exists = await ExistsAsync(connection, transaction, "SELECT 1 FROM dbo.CurvasCatalogo WHERE idEmpresa=@IdEmpresa AND id=@Id", cancellationToken, ("@IdEmpresa", idEmpresa), ("@Id", idCurva));
            string? existingCodigo = exists ? await ScalarStringAsync(connection, transaction, "SELECT Codigo FROM dbo.CurvasCatalogo WHERE idEmpresa=@IdEmpresa AND id=@Id", cancellationToken, ("@IdEmpresa", idEmpresa), ("@Id", idCurva)) : null;
            string codigo = exists && !string.IsNullOrWhiteSpace(existingCodigo) ? existingCodigo : await GenerateCatalogoCodeAsync(connection, transaction, idEmpresa, cancellationToken);
            await EnsureCatalogoUniqueAsync(connection, transaction, idEmpresa, idCurva, request.Nombre.Trim(), codigo, cancellationToken);

            foreach (CurvasCatalogoGuardarDetalleRequest detalle in request.Detalles)
            {
                await EnsureProductoAsync(connection, transaction, idEmpresa, detalle.IdProductoServicio, requireProducto: true, cancellationToken);
                if (detalle.IdVariante.HasValue)
                {
                    await EnsureVarianteAsync(connection, transaction, idEmpresa, detalle.IdProductoServicio, detalle.IdVariante.Value, cancellationToken);
                }
            }

            if (exists)
            {
                string updateSql = request.Activo ? @"
UPDATE dbo.CurvasCatalogo
SET Nombre=@Nombre, Codigo=@Codigo, Estado=1, Activo=1, FechaActualizacion=SYSUTCDATETIME(), FechaArchivado=NULL, idUsuarioActualizacion=@Usuario, idUsuarioArchivado=NULL
WHERE idEmpresa=@IdEmpresa AND id=@Id" : @"
UPDATE dbo.CurvasCatalogo
SET Nombre=@Nombre, Codigo=@Codigo, Estado=2, Activo=0, FechaActualizacion=SYSUTCDATETIME(), FechaArchivado=COALESCE(FechaArchivado, SYSUTCDATETIME()), idUsuarioActualizacion=@Usuario, idUsuarioArchivado=@Usuario
WHERE idEmpresa=@IdEmpresa AND id=@Id";
                await ExecuteAsync(connection, transaction, updateSql, cancellationToken,
                    ("@IdEmpresa", idEmpresa), ("@Id", idCurva), ("@Nombre", request.Nombre.Trim()), ("@Codigo", Db(codigo)), ("@Usuario", Db(request.UsuarioId)));
                await ExecuteAsync(connection, transaction, @"
UPDATE dbo.CurvasDetalle
SET Activo=0, FechaArchivado=SYSUTCDATETIME(), FechaActualizacion=SYSUTCDATETIME(), idUsuarioActualizacion=@Usuario
WHERE idEmpresa=@IdEmpresa AND idCurva=@IdCurva AND Activo=1 AND FechaArchivado IS NULL", cancellationToken,
                    ("@IdEmpresa", idEmpresa), ("@IdCurva", idCurva), ("@Usuario", Db(request.UsuarioId)));
            }
            else
            {
                string insertSql = request.Activo ? @"
INSERT INTO dbo.CurvasCatalogo
(id, idEmpresa, identityKey, Nombre, Codigo, Estado, Activo, FechaCreacion, FechaActualizacion, idUsuarioCreacion)
VALUES
(@Id, @IdEmpresa, NEWID(), @Nombre, @Codigo, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME(), @Usuario)" : @"
INSERT INTO dbo.CurvasCatalogo
(id, idEmpresa, identityKey, Nombre, Codigo, Estado, Activo, FechaCreacion, FechaActualizacion, FechaArchivado, idUsuarioCreacion, idUsuarioArchivado)
VALUES
(@Id, @IdEmpresa, NEWID(), @Nombre, @Codigo, 2, 0, SYSUTCDATETIME(), SYSUTCDATETIME(), SYSUTCDATETIME(), @Usuario, @Usuario)";
                await ExecuteAsync(connection, transaction, insertSql, cancellationToken,
                    ("@Id", idCurva), ("@IdEmpresa", idEmpresa), ("@Nombre", request.Nombre.Trim()), ("@Codigo", Db(codigo)), ("@Usuario", Db(request.UsuarioId)));
            }

            foreach (CurvasCatalogoGuardarDetalleRequest detalle in request.Detalles)
            {
                await ExecuteAsync(connection, transaction, @"
INSERT INTO dbo.CurvasDetalle
(id, idEmpresa, identityKey, idCurva, idProductoServicio, idVariante, TipoProductoServicio, CantidadBaseObjetivo, Activo, FechaCreacion, FechaActualizacion, idUsuarioCreacion)
VALUES
(@Id, @IdEmpresa, NEWID(), @IdCurva, @Producto, @Variante, 1, @Cantidad, 1, SYSUTCDATETIME(), SYSUTCDATETIME(), @Usuario)", cancellationToken,
                    ("@Id", Guid.NewGuid()), ("@IdEmpresa", idEmpresa), ("@IdCurva", idCurva), ("@Producto", detalle.IdProductoServicio), ("@Variante", Db(detalle.IdVariante)), ("@Cantidad", detalle.CantidadBaseObjetivo), ("@Usuario", Db(request.UsuarioId)));
            }

            transaction.Commit();
            return idCurva;
        }

        public async Task BajaCatalogoAsync(TenantDatabaseDescriptor descriptor, Guid idEmpresa, Guid idCurva, Guid? usuarioId, CancellationToken cancellationToken = default)
        {
            await CambiarEstadoCatalogoAsync(descriptor, idEmpresa, idCurva, activo: false, usuarioId, cancellationToken);
        }

        public async Task ReactivarCatalogoAsync(TenantDatabaseDescriptor descriptor, Guid idEmpresa, Guid idCurva, Guid? usuarioId, CancellationToken cancellationToken = default)
        {
            await CambiarEstadoCatalogoAsync(descriptor, idEmpresa, idCurva, activo: true, usuarioId, cancellationToken);
        }

        public async Task<IReadOnlyList<CurvasProductoElegibleDto>> BuscarProductosElegiblesAsync(TenantDatabaseDescriptor descriptor, Guid idEmpresa, string? busqueda, CancellationToken cancellationToken = default)
        {
            string search = (busqueda ?? string.Empty).Trim();
            await using SqlConnection connection = _connectionFactory.CreateConnection(descriptor);
            await connection.OpenAsync(cancellationToken);
            using SqlCommand command = new(@"
SELECT TOP (25)
    ps.id,
    ISNULL(ps.Codigo, '') AS Codigo,
    ps.Nombre,
    ISNULL(um.Abreviatura, um.Nombre) AS UnidadBase,
    COUNT(pv.id) AS Variantes
FROM dbo.ProductosServicios ps
LEFT JOIN dbo.ProductosServiciosUnidadesMedida um
    ON um.idEmpresa = ps.idEmpresa
   AND um.id = ps.idUnidadMedida
LEFT JOIN dbo.ProductosServiciosVariantes pv
    ON pv.idEmpresa = ps.idEmpresa
   AND pv.idProductoServicio = ps.id
   AND pv.Activo = 1
WHERE ps.idEmpresa = @IdEmpresa
  AND ps.Tipo = 1
  AND ps.Activo = 1
  AND (@Busqueda = '' OR ps.Nombre LIKE @BusquedaLike OR ps.Codigo LIKE @BusquedaLike)
GROUP BY ps.id, ps.Codigo, ps.Nombre, um.Abreviatura, um.Nombre
ORDER BY ps.Nombre", connection);
            command.Parameters.Add("@IdEmpresa", SqlDbType.UniqueIdentifier).Value = idEmpresa;
            command.Parameters.Add("@Busqueda", SqlDbType.NVarChar, 150).Value = search;
            command.Parameters.Add("@BusquedaLike", SqlDbType.NVarChar, 160).Value = $"%{search}%";

            List<CurvasProductoElegibleDto> result = new();
            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(new CurvasProductoElegibleDto
                {
                    Id = reader.GetGuid(reader.GetOrdinal("id")),
                    Codigo = ReadString(reader, "Codigo"),
                    Nombre = ReadString(reader, "Nombre"),
                    UnidadBase = ReadString(reader, "UnidadBase"),
                    Variantes = Convert.ToInt32(reader["Variantes"])
                });
            }

            return result;
        }

        public async Task<IReadOnlyList<CurvasVarianteElegibleDto>> ObtenerVariantesAsync(TenantDatabaseDescriptor descriptor, Guid idEmpresa, Guid idProductoServicio, CancellationToken cancellationToken = default)
        {
            await using SqlConnection connection = _connectionFactory.CreateConnection(descriptor);
            await connection.OpenAsync(cancellationToken);
            using (SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                await EnsureProductoAsync(connection, transaction, idEmpresa, idProductoServicio, requireProducto: true, cancellationToken);
                transaction.Commit();
            }
            using SqlCommand command = new(@"
SELECT id, CAST('' AS nvarchar(50)) AS Codigo, Nombre
FROM dbo.ProductosServiciosVariantes
WHERE idEmpresa = @IdEmpresa
  AND idProductoServicio = @Producto
  AND Activo = 1
ORDER BY Nombre", connection);
            command.Parameters.Add("@IdEmpresa", SqlDbType.UniqueIdentifier).Value = idEmpresa;
            command.Parameters.Add("@Producto", SqlDbType.UniqueIdentifier).Value = idProductoServicio;

            List<CurvasVarianteElegibleDto> result = new();
            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(new CurvasVarianteElegibleDto
                {
                    Id = reader.GetGuid(reader.GetOrdinal("id")),
                    Codigo = ReadString(reader, "Codigo"),
                    Nombre = ReadString(reader, "Nombre")
                });
            }

            return result;
        }

        public async Task<Guid> SembrarAsync(TenantDatabaseDescriptor descriptor, Guid idEmpresa, CurvaSiembraRequest request, CancellationToken cancellationToken = default)
        {
            await using SqlConnection connection = _connectionFactory.CreateConnection(descriptor);
            await connection.OpenAsync(cancellationToken);
            using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable);
            await EnsureSucursalAsync(connection, transaction, idEmpresa, request.IdSucursal, cancellationToken);
            await EnsureCurvaActivaAsync(connection, transaction, idEmpresa, request.IdCurva, cancellationToken);
            await EnsureProductoAsync(connection, transaction, idEmpresa, request.IdProductoServicio, requireProducto: true, cancellationToken);
            if (request.IdVariante.HasValue)
            {
                await EnsureVarianteAsync(connection, transaction, idEmpresa, request.IdProductoServicio, request.IdVariante.Value, cancellationToken);
            }

            await ExecuteAsync(connection, transaction, @"
UPDATE dbo.CurvasSiembra
SET Estado = 3, FechaVigenciaFin = SYSUTCDATETIME(), FechaActualizacion = SYSUTCDATETIME(), idUsuarioActualizacion = @Usuario
WHERE idEmpresa = @IdEmpresa
  AND idSucursal = @IdSucursal
  AND idProductoServicio = @IdProductoServicio
  AND ((idVariante IS NULL AND @IdVariante IS NULL) OR idVariante = @IdVariante)
  AND Estado = 1
  AND FechaVigenciaFin IS NULL", cancellationToken,
                ("@IdEmpresa", idEmpresa),
                ("@IdSucursal", request.IdSucursal),
                ("@IdProductoServicio", request.IdProductoServicio),
                ("@IdVariante", Db(request.IdVariante)),
                ("@Usuario", Db(request.UsuarioId)));

            Guid id = Guid.NewGuid();
            await ExecuteAsync(connection, transaction, @"
INSERT INTO dbo.CurvasSiembra
(id, idEmpresa, identityKey, idSucursal, idProductoServicio, idVariante, idCurva, TipoProductoServicio, Estado, FechaVigenciaInicio, FechaCreacion, FechaActualizacion, idUsuarioCreacion)
VALUES
(@Id, @IdEmpresa, NEWID(), @IdSucursal, @IdProductoServicio, @IdVariante, @IdCurva, 1, 1, SYSUTCDATETIME(), SYSUTCDATETIME(), SYSUTCDATETIME(), @Usuario)", cancellationToken,
                ("@Id", id),
                ("@IdEmpresa", idEmpresa),
                ("@IdSucursal", request.IdSucursal),
                ("@IdProductoServicio", request.IdProductoServicio),
                ("@IdVariante", Db(request.IdVariante)),
                ("@IdCurva", request.IdCurva),
                ("@Usuario", Db(request.UsuarioId)));
            transaction.Commit();
            return id;
        }

        public async Task<CurvaOperacionResult> CrearOperacionAsync(TenantDatabaseDescriptor descriptor, Guid idEmpresa, CurvaOperacionRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.OperationKey) || request.OperationKey.Length > 120)
            {
                throw new InvalidOperationException("CURVA_OPERATIONKEY_INVALIDA");
            }

            await using SqlConnection connection = _connectionFactory.CreateConnection(descriptor);
            await connection.OpenAsync(cancellationToken);
            using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable);
            await EnsureProveedorAsync(connection, transaction, idEmpresa, request.IdProveedor, cancellationToken);

            Guid? existing = await ScalarGuidAsync(connection, transaction, @"
SELECT id FROM dbo.CurvasOperacionesCompra
WHERE idEmpresa = @IdEmpresa AND OperationKey = @OperationKey", cancellationToken,
                ("@IdEmpresa", idEmpresa),
                ("@OperationKey", request.OperationKey.Trim()));
            if (existing.HasValue)
            {
                transaction.Commit();
                return new CurvaOperacionResult { Id = existing.Value, AlreadyApplied = true };
            }

            Guid id = Guid.NewGuid();
            await ExecuteAsync(connection, transaction, @"
INSERT INTO dbo.CurvasOperacionesCompra
(id, idEmpresa, identityKey, OperationKey, idProveedor, Estado, SucursalCount, OrdenCompraCount, FechaOperacion, ContextoJson, idUsuarioCreacion)
VALUES
(@Id, @IdEmpresa, NEWID(), @OperationKey, @IdProveedor, 1, @SucursalCount, 0, SYSUTCDATETIME(), @ContextoJson, @Usuario)", cancellationToken,
                ("@Id", id),
                ("@IdEmpresa", idEmpresa),
                ("@OperationKey", request.OperationKey.Trim()),
                ("@IdProveedor", request.IdProveedor),
                ("@SucursalCount", request.SucursalCount),
                ("@ContextoJson", Db(request.ContextoJson)),
                ("@Usuario", Db(request.UsuarioId)));
            transaction.Commit();
            return new CurvaOperacionResult { Id = id, AlreadyApplied = false };
        }

        public async Task<Guid> RelacionarOrdenCompraAsync(TenantDatabaseDescriptor descriptor, Guid idEmpresa, CurvaOperacionOrdenCompraRequest request, CancellationToken cancellationToken = default)
        {
            await using SqlConnection connection = _connectionFactory.CreateConnection(descriptor);
            await connection.OpenAsync(cancellationToken);
            using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable);
            await EnsureOperacionAsync(connection, transaction, idEmpresa, request.IdOperacionCurva, cancellationToken);
            await EnsureOrdenCompraAsync(connection, transaction, idEmpresa, request.IdOrdenCompra, request.IdSucursal, cancellationToken);

            Guid? existing = await ScalarGuidAsync(connection, transaction, @"
SELECT id FROM dbo.CurvasOperacionOrdenesCompra
WHERE idEmpresa = @IdEmpresa AND idOperacionCurva = @Operacion AND idOrdenCompra = @Orden", cancellationToken,
                ("@IdEmpresa", idEmpresa),
                ("@Operacion", request.IdOperacionCurva),
                ("@Orden", request.IdOrdenCompra));
            if (existing.HasValue)
            {
                transaction.Commit();
                return existing.Value;
            }

            Guid id = Guid.NewGuid();
            await ExecuteAsync(connection, transaction, @"
INSERT INTO dbo.CurvasOperacionOrdenesCompra
(id, idEmpresa, identityKey, idOperacionCurva, idOrdenCompra, idSucursal, Estado, FechaCreacion)
VALUES
(@Id, @IdEmpresa, NEWID(), @Operacion, @Orden, @Sucursal, 1, SYSUTCDATETIME())", cancellationToken,
                ("@Id", id),
                ("@IdEmpresa", idEmpresa),
                ("@Operacion", request.IdOperacionCurva),
                ("@Orden", request.IdOrdenCompra),
                ("@Sucursal", request.IdSucursal));
            await ExecuteAsync(connection, transaction, @"
UPDATE dbo.CurvasOperacionesCompra
SET OrdenCompraCount = OrdenCompraCount + 1
WHERE idEmpresa = @IdEmpresa AND id = @Operacion", cancellationToken,
                ("@IdEmpresa", idEmpresa),
                ("@Operacion", request.IdOperacionCurva));
            transaction.Commit();
            return id;
        }

        public async Task<Guid> CrearSnapshotAsync(TenantDatabaseDescriptor descriptor, Guid idEmpresa, CurvaSnapshotRequest request, CancellationToken cancellationToken = default)
        {
            if (request.NoPedir && request.CantidadFinalBase != 0)
            {
                throw new InvalidOperationException("CURVA_SNAPSHOT_NOPEDIR_INVALIDO");
            }

            decimal cobertura = request.ExistenciaSnapshotBase + request.TransitoSnapshotBase;

            await using SqlConnection connection = _connectionFactory.CreateConnection(descriptor);
            await connection.OpenAsync(cancellationToken);
            using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable);
            await EnsureOperacionAsync(connection, transaction, idEmpresa, request.IdOperacionCurva, cancellationToken);
            await EnsureSucursalAsync(connection, transaction, idEmpresa, request.IdSucursal, cancellationToken);
            await EnsureProductoAsync(connection, transaction, idEmpresa, request.IdProductoServicio, requireProducto: true, cancellationToken);
            if (request.IdVariante.HasValue)
            {
                await EnsureVarianteAsync(connection, transaction, idEmpresa, request.IdProductoServicio, request.IdVariante.Value, cancellationToken);
            }
            if (request.IdOrdenCompra.HasValue)
            {
                await EnsureOrdenCompraExistsAsync(connection, transaction, idEmpresa, request.IdOrdenCompra.Value, cancellationToken);
            }
            if (request.IdOrdenCompraDetalle.HasValue)
            {
                await EnsureOrdenCompraDetalleAsync(connection, transaction, idEmpresa, request.IdOrdenCompraDetalle.Value, request.IdOrdenCompra, cancellationToken);
            }
            if (request.IdCurva.HasValue)
            {
                await EnsureCurvaExistsAsync(connection, transaction, idEmpresa, request.IdCurva.Value, cancellationToken);
            }
            if (request.IdSiembra.HasValue)
            {
                await EnsureSiembraAsync(connection, transaction, idEmpresa, request.IdSiembra.Value, cancellationToken);
            }
            if (request.IdPresentacionCompra.HasValue)
            {
                await EnsurePresentacionCompraAsync(connection, transaction, idEmpresa, request.IdProductoServicio, request.IdVariante, request.IdPresentacionCompra.Value, cancellationToken);
            }

            Guid id = Guid.NewGuid();
            await ExecuteAsync(connection, transaction, @"
INSERT INTO dbo.CurvasSugerenciasSnapshot
(id, idEmpresa, identityKey, idOperacionCurva, idOrdenCompra, idOrdenCompraDetalle, idCurva, idSiembra, idSucursal, idProductoServicio, idVariante, TipoProductoServicio, Modo, CurvaObjetivoBase, ExistenciaSnapshotBase, TransitoSnapshotBase, CoberturaSnapshotBase, HuecoSnapshotBase, CopeteSnapshotBase, CantidadPropuestaBase, CantidadFinalBase, OverrideManual, NoPedir, idPresentacionCompra, PermiteCantidadBaseSnapshot, ContextoJson, FechaSnapshot, idUsuarioCreacion)
VALUES
(@Id, @IdEmpresa, NEWID(), @Operacion, @Orden, @Detalle, @Curva, @Siembra, @Sucursal, @Producto, @Variante, 1, @Modo, @Objetivo, @Existencia, @Transito, @Cobertura, @Hueco, @Copete, @Propuesta, @Final, @Override, @NoPedir, @Presentacion, @PermiteBase, @Contexto, SYSUTCDATETIME(), @Usuario)", cancellationToken,
                ("@Id", id),
                ("@IdEmpresa", idEmpresa),
                ("@Operacion", request.IdOperacionCurva),
                ("@Orden", Db(request.IdOrdenCompra)),
                ("@Detalle", Db(request.IdOrdenCompraDetalle)),
                ("@Curva", Db(request.IdCurva)),
                ("@Siembra", Db(request.IdSiembra)),
                ("@Sucursal", request.IdSucursal),
                ("@Producto", request.IdProductoServicio),
                ("@Variante", Db(request.IdVariante)),
                ("@Modo", (byte)request.Modo),
                ("@Objetivo", request.CurvaObjetivoBase),
                ("@Existencia", request.ExistenciaSnapshotBase),
                ("@Transito", request.TransitoSnapshotBase),
                ("@Cobertura", cobertura),
                ("@Hueco", request.HuecoSnapshotBase),
                ("@Copete", request.CopeteSnapshotBase),
                ("@Propuesta", request.CantidadPropuestaBase),
                ("@Final", request.CantidadFinalBase),
                ("@Override", request.OverrideManual),
                ("@NoPedir", request.NoPedir),
                ("@Presentacion", Db(request.IdPresentacionCompra)),
                ("@PermiteBase", request.PermiteCantidadBaseSnapshot),
                ("@Contexto", Db(request.ContextoJson)),
                ("@Usuario", Db(request.UsuarioId)));
            transaction.Commit();
            return id;
        }

        private async Task CambiarEstadoCatalogoAsync(TenantDatabaseDescriptor descriptor, Guid idEmpresa, Guid idCurva, bool activo, Guid? usuarioId, CancellationToken cancellationToken)
        {
            await using SqlConnection connection = _connectionFactory.CreateConnection(descriptor);
            await connection.OpenAsync(cancellationToken);
            using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable);
            if (!await ExistsAsync(connection, transaction, "SELECT 1 FROM dbo.CurvasCatalogo WHERE idEmpresa=@IdEmpresa AND id=@Id", cancellationToken, ("@IdEmpresa", idEmpresa), ("@Id", idCurva)))
            {
                throw new InvalidOperationException("CURVA_NO_DISPONIBLE");
            }

            if (activo)
            {
                await ExecuteAsync(connection, transaction, @"
UPDATE dbo.CurvasCatalogo
SET Estado=1, Activo=1, FechaActualizacion=SYSUTCDATETIME(), FechaArchivado=NULL, idUsuarioActualizacion=@Usuario, idUsuarioArchivado=NULL
WHERE idEmpresa=@IdEmpresa AND id=@Id", cancellationToken, ("@IdEmpresa", idEmpresa), ("@Id", idCurva), ("@Usuario", Db(usuarioId)));
            }
            else
            {
                await ExecuteAsync(connection, transaction, @"
UPDATE dbo.CurvasCatalogo
SET Estado=2, Activo=0, FechaActualizacion=SYSUTCDATETIME(), FechaArchivado=COALESCE(FechaArchivado, SYSUTCDATETIME()), idUsuarioActualizacion=@Usuario, idUsuarioArchivado=@Usuario
WHERE idEmpresa=@IdEmpresa AND id=@Id", cancellationToken, ("@IdEmpresa", idEmpresa), ("@Id", idCurva), ("@Usuario", Db(usuarioId)));
            }

            transaction.Commit();
        }

        private static void ValidateCatalogoRequest(CurvasCatalogoGuardarRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Nombre))
            {
                throw new InvalidOperationException("CURVA_NOMBRE_REQUERIDO");
            }

            if (request.Nombre.Trim().Length > 150 || (!string.IsNullOrWhiteSpace(request.Codigo) && request.Codigo.Trim().Length > 50))
            {
                throw new InvalidOperationException("CURVA_DATOS_INVALIDOS");
            }

            if (request.Detalles == null || request.Detalles.Count == 0)
            {
                throw new InvalidOperationException("CURVA_DETALLE_REQUERIDO");
            }

            HashSet<string> keys = new(StringComparer.OrdinalIgnoreCase);
            foreach (CurvasCatalogoGuardarDetalleRequest detalle in request.Detalles)
            {
                if (detalle.IdProductoServicio == Guid.Empty || detalle.CantidadBaseObjetivo < 0)
                {
                    throw new InvalidOperationException("CURVA_DETALLE_INVALIDO");
                }

                string key = $"{detalle.IdProductoServicio:N}:{(detalle.IdVariante.HasValue ? detalle.IdVariante.Value.ToString("N") : "BASE")}";
                if (!keys.Add(key))
                {
                    throw new InvalidOperationException("CURVA_DETALLE_DUPLICADO");
                }
            }
        }

        private static async Task EnsureCatalogoUniqueAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idCurva, string nombre, string codigo, CancellationToken cancellationToken)
        {
            if (await ExistsAsync(connection, transaction, @"
SELECT 1
FROM dbo.CurvasCatalogo
WHERE idEmpresa=@IdEmpresa
  AND id<>@Id
  AND Activo=1
  AND FechaArchivado IS NULL
  AND (Nombre=@Nombre OR Codigo=@Codigo)", cancellationToken, ("@IdEmpresa", idEmpresa), ("@Id", idCurva), ("@Nombre", nombre), ("@Codigo", codigo)))
            {
                throw new InvalidOperationException("CURVA_DUPLICADA");
            }
        }

        private static async Task<string> GenerateCatalogoCodeAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, CancellationToken cancellationToken)
        {
            for (int attempt = 0; attempt < 5; attempt++)
            {
                string code = $"CUR-{Guid.NewGuid():N}"[..12].ToUpperInvariant();
                if (!await ExistsAsync(connection, transaction, "SELECT 1 FROM dbo.CurvasCatalogo WHERE idEmpresa=@IdEmpresa AND Codigo=@Codigo", cancellationToken, ("@IdEmpresa", idEmpresa), ("@Codigo", code)))
                {
                    return code;
                }
            }

            throw new InvalidOperationException("CURVA_CODIGO_NO_DISPONIBLE");
        }

        private static string NormalizeEstatus(string? value)
        {
            string normalized = (value ?? "activos").Trim().ToLowerInvariant();
            return normalized is "todos" or "inactivos" ? normalized : "activos";
        }

        private static async Task EnsureCurvaActivaAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idCurva, CancellationToken cancellationToken)
        {
            if (!await ExistsAsync(connection, transaction, "SELECT 1 FROM dbo.CurvasCatalogo WHERE idEmpresa=@IdEmpresa AND id=@Id AND Estado=1 AND Activo=1 AND FechaArchivado IS NULL", cancellationToken, ("@IdEmpresa", idEmpresa), ("@Id", idCurva)))
            {
                throw new InvalidOperationException("CURVA_NO_DISPONIBLE");
            }
        }

        private static async Task EnsureCurvaExistsAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idCurva, CancellationToken cancellationToken)
        {
            if (!await ExistsAsync(connection, transaction, "SELECT 1 FROM dbo.CurvasCatalogo WHERE idEmpresa=@IdEmpresa AND id=@Id", cancellationToken, ("@IdEmpresa", idEmpresa), ("@Id", idCurva)))
            {
                throw new InvalidOperationException("CURVA_NO_DISPONIBLE");
            }
        }

        private static async Task EnsureProveedorAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idProveedor, CancellationToken cancellationToken)
        {
            if (!await ExistsAsync(connection, transaction, "SELECT 1 FROM dbo.ActivosProveedores WHERE idEmpresa=@IdEmpresa AND id=@Id AND Activo=1", cancellationToken, ("@IdEmpresa", idEmpresa), ("@Id", idProveedor)))
            {
                throw new InvalidOperationException("CURVA_PROVEEDOR_NO_DISPONIBLE");
            }
        }

        private static async Task EnsureOperacionAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idOperacion, CancellationToken cancellationToken)
        {
            if (!await ExistsAsync(connection, transaction, "SELECT 1 FROM dbo.CurvasOperacionesCompra WHERE idEmpresa=@IdEmpresa AND id=@Id", cancellationToken, ("@IdEmpresa", idEmpresa), ("@Id", idOperacion)))
            {
                throw new InvalidOperationException("CURVA_OPERACION_NO_DISPONIBLE");
            }
        }

        private static async Task EnsureSucursalAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idSucursal, CancellationToken cancellationToken)
        {
            if (!await ExistsAsync(connection, transaction, "SELECT 1 FROM dbo.Sucursales WHERE idEmpresa=@IdEmpresa AND id=@Id", cancellationToken, ("@IdEmpresa", idEmpresa), ("@Id", idSucursal)))
            {
                throw new InvalidOperationException("CURVA_SUCURSAL_NO_DISPONIBLE");
            }
        }

        private static async Task EnsureProductoAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idProducto, bool requireProducto, CancellationToken cancellationToken)
        {
            byte? tipo = await ScalarByteAsync(connection, transaction, "SELECT Tipo FROM dbo.ProductosServicios WHERE idEmpresa=@IdEmpresa AND id=@Id AND Activo=1", cancellationToken, ("@IdEmpresa", idEmpresa), ("@Id", idProducto));
            if (!tipo.HasValue)
            {
                throw new InvalidOperationException("CURVA_PRODUCTO_NO_DISPONIBLE");
            }

            if (requireProducto && tipo.Value != 1)
            {
                throw new InvalidOperationException("CURVA_SERVICIO_RECHAZADO");
            }
        }

        private static async Task EnsureVarianteAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idProducto, Guid idVariante, CancellationToken cancellationToken)
        {
            if (!await ExistsAsync(connection, transaction, "SELECT 1 FROM dbo.ProductosServiciosVariantes WHERE idEmpresa=@IdEmpresa AND idProductoServicio=@Producto AND id=@Variante AND Activo=1", cancellationToken, ("@IdEmpresa", idEmpresa), ("@Producto", idProducto), ("@Variante", idVariante)))
            {
                throw new InvalidOperationException("CURVA_VARIANTE_NO_DISPONIBLE");
            }
        }

        private static async Task EnsureOrdenCompraAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idOrden, Guid idSucursal, CancellationToken cancellationToken)
        {
            if (!await ExistsAsync(connection, transaction, "SELECT 1 FROM dbo.OrdenesCompra WHERE idEmpresa=@IdEmpresa AND id=@Id AND idSucursal=@Sucursal", cancellationToken, ("@IdEmpresa", idEmpresa), ("@Id", idOrden), ("@Sucursal", idSucursal)))
            {
                throw new InvalidOperationException("CURVA_OC_NO_DISPONIBLE");
            }
        }

        private static async Task EnsureOrdenCompraExistsAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idOrden, CancellationToken cancellationToken)
        {
            if (!await ExistsAsync(connection, transaction, "SELECT 1 FROM dbo.OrdenesCompra WHERE idEmpresa=@IdEmpresa AND id=@Id", cancellationToken, ("@IdEmpresa", idEmpresa), ("@Id", idOrden)))
            {
                throw new InvalidOperationException("CURVA_OC_NO_DISPONIBLE");
            }
        }

        private static async Task EnsureOrdenCompraDetalleAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idDetalle, Guid? idOrden, CancellationToken cancellationToken)
        {
            string sql = idOrden.HasValue
                ? "SELECT 1 FROM dbo.OrdenesCompraDetalle WHERE idEmpresa=@IdEmpresa AND id=@Id AND idOrdenCompra=@Orden"
                : "SELECT 1 FROM dbo.OrdenesCompraDetalle WHERE idEmpresa=@IdEmpresa AND id=@Id";
            if (!await ExistsAsync(connection, transaction, sql, cancellationToken, ("@IdEmpresa", idEmpresa), ("@Id", idDetalle), ("@Orden", Db(idOrden))))
            {
                throw new InvalidOperationException("CURVA_OC_DETALLE_NO_DISPONIBLE");
            }
        }

        private static async Task EnsureSiembraAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idSiembra, CancellationToken cancellationToken)
        {
            if (!await ExistsAsync(connection, transaction, "SELECT 1 FROM dbo.CurvasSiembra WHERE idEmpresa=@IdEmpresa AND id=@Id", cancellationToken, ("@IdEmpresa", idEmpresa), ("@Id", idSiembra)))
            {
                throw new InvalidOperationException("CURVA_SIEMBRA_NO_DISPONIBLE");
            }
        }

        private static async Task EnsurePresentacionCompraAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idProducto, Guid? idVariante, Guid idPresentacion, CancellationToken cancellationToken)
        {
            if (!await ExistsAsync(connection, transaction, @"
SELECT 1
FROM dbo.OrdenesCompraPresentacionesCompra
WHERE idEmpresa = @IdEmpresa
  AND id = @Id
  AND idProductoServicio = @Producto
  AND ((idVariante IS NULL AND @Variante IS NULL) OR idVariante = @Variante)
  AND Activo = 1
  AND FechaArchivado IS NULL", cancellationToken, ("@IdEmpresa", idEmpresa), ("@Id", idPresentacion), ("@Producto", idProducto), ("@Variante", Db(idVariante))))
            {
                throw new InvalidOperationException("CURVA_PRESENTACION_COMPRA_NO_DISPONIBLE");
            }
        }

        private static async Task<bool> ExistsAsync(SqlConnection connection, SqlTransaction transaction, string sql, CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
        {
            using SqlCommand command = Command(connection, transaction, sql, parameters);
            object? value = await command.ExecuteScalarAsync(cancellationToken);
            return value != null && value != DBNull.Value;
        }

        private static async Task<Guid?> ScalarGuidAsync(SqlConnection connection, SqlTransaction transaction, string sql, CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
        {
            using SqlCommand command = Command(connection, transaction, sql, parameters);
            object? value = await command.ExecuteScalarAsync(cancellationToken);
            return value == null || value == DBNull.Value ? null : (Guid)value;
        }

        private static async Task<string?> ScalarStringAsync(SqlConnection connection, SqlTransaction transaction, string sql, CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
        {
            using SqlCommand command = Command(connection, transaction, sql, parameters);
            object? value = await command.ExecuteScalarAsync(cancellationToken);
            return value == null || value == DBNull.Value ? null : value.ToString()?.Trim();
        }

        private static async Task<byte?> ScalarByteAsync(SqlConnection connection, SqlTransaction transaction, string sql, CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
        {
            using SqlCommand command = Command(connection, transaction, sql, parameters);
            object? value = await command.ExecuteScalarAsync(cancellationToken);
            return value == null || value == DBNull.Value ? null : Convert.ToByte(value);
        }

        private static async Task ExecuteAsync(SqlConnection connection, SqlTransaction transaction, string sql, CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
        {
            using SqlCommand command = Command(connection, transaction, sql, parameters);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static SqlCommand Command(SqlConnection connection, SqlTransaction transaction, string sql, params (string Name, object? Value)[] parameters)
        {
            SqlCommand command = new(sql, connection, transaction);
            foreach ((string name, object? value) in parameters)
            {
                command.Parameters.AddWithValue(name, value ?? DBNull.Value);
            }

            return command;
        }

        private static object Db(Guid? value) => value.HasValue && value.Value != Guid.Empty ? value.Value : DBNull.Value;
        private static object Db(string? value) => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();

        private static Guid? ReadNullableGuid(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetGuid(ordinal);
        }

        private static string ReadString(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? string.Empty : reader.GetValue(ordinal).ToString()?.Trim() ?? string.Empty;
        }
    }
}
