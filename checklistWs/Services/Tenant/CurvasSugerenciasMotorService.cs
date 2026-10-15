using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;

namespace checklistWs.Services.Tenant
{
    public interface ICurvasSugerenciasMotorService
    {
        Task<CurvasSugerenciaPreviewResponse> PreviewAsync(
            TenantDatabaseDescriptor descriptor,
            Guid idEmpresa,
            IReadOnlyList<CurvasSugerenciaItemRequest> items,
            CancellationToken cancellationToken = default);
        Task<IReadOnlyList<CurvasAplicableDto>> ListarCurvasAplicablesAsync(
            TenantDatabaseDescriptor descriptor,
            Guid idEmpresa,
            Guid idProductoServicio,
            CancellationToken cancellationToken = default);
        Task<IReadOnlyList<CurvasSiembraSucursalDto>> ListarSucursalesAnalisisAsync(
            TenantDatabaseDescriptor descriptor,
            Guid idEmpresa,
            CancellationToken cancellationToken = default);
        Task<CurvasAnalisisResponse> AnalizarAsync(
            TenantDatabaseDescriptor descriptor,
            Guid idEmpresa,
            CurvasAnalisisRequest request,
            CancellationToken cancellationToken = default);
    }

    public sealed class CurvasSugerenciasMotorService : ICurvasSugerenciasMotorService
    {
        private const byte TipoProducto = 1;
        private const byte OcEstadoGenerada = 2;
        private const byte OcEstadoParcialmenteRecibida = 4;
        private const byte PartidaPendiente = 1;
        private const byte PartidaParcial = 2;

        private readonly ITenantSqlConnectionFactory _connectionFactory;

        public CurvasSugerenciasMotorService(ITenantSqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<CurvasSugerenciaPreviewResponse> PreviewAsync(
            TenantDatabaseDescriptor descriptor,
            Guid idEmpresa,
            IReadOnlyList<CurvasSugerenciaItemRequest> items,
            CancellationToken cancellationToken = default)
        {
            if (idEmpresa == Guid.Empty || descriptor.IdEmpresa != idEmpresa)
            {
                throw new InvalidOperationException("CURVA_TENANT_INVALIDO");
            }

            if (items == null || items.Count == 0)
            {
                throw new InvalidOperationException("CURVA_SUGERENCIA_ITEMS_REQUERIDOS");
            }

            await using SqlConnection connection = _connectionFactory.CreateConnection(descriptor);
            await connection.OpenAsync(cancellationToken);

            List<CurvasSugerenciaResult> results = new(items.Count);
            foreach (CurvasSugerenciaItemRequest item in items)
            {
                ValidateItem(item);
                await EnsureSucursalAsync(connection, idEmpresa, item.IdSucursal, cancellationToken);
                await EnsureProductoAsync(connection, idEmpresa, item.IdProductoServicio, cancellationToken);
                if (item.IdVariante.HasValue)
                {
                    await EnsureVarianteAsync(connection, idEmpresa, item.IdProductoServicio, item.IdVariante.Value, cancellationToken);
                }

                CurvaSiembraSnapshot? siembra = item.IdCurvaTemporal.HasValue
                    ? await LoadCurvaTemporalAsync(connection, idEmpresa, item, cancellationToken)
                    : await LoadSiembraAsync(connection, idEmpresa, item, cancellationToken);
                decimal existencia = await LoadExistenciaAsync(connection, idEmpresa, item, cancellationToken);
                decimal transito = await LoadTransitoAsync(connection, idEmpresa, item, cancellationToken);
                PresentacionCompraSnapshot? presentacion = item.IdPresentacionCompra.HasValue
                    ? await LoadPresentacionAsync(connection, idEmpresa, item, cancellationToken)
                    : null;

                CurvasSugerenciaResult result = CurvasSugerenciasCalculator.Calculate(
                    item,
                    siembra,
                    existencia,
                    transito,
                    presentacion);
                results.Add(result);
            }

            return new CurvasSugerenciaPreviewResponse
            {
                Exito = true,
                Mensaje = "Preview de sugerencias calculado sin persistir snapshots.",
                Items = results
            };
        }

        public async Task<IReadOnlyList<CurvasAplicableDto>> ListarCurvasAplicablesAsync(
            TenantDatabaseDescriptor descriptor,
            Guid idEmpresa,
            Guid idProductoServicio,
            CancellationToken cancellationToken = default)
        {
            if (idEmpresa == Guid.Empty || descriptor.IdEmpresa != idEmpresa || idProductoServicio == Guid.Empty)
            {
                throw new InvalidOperationException("CURVA_TENANT_INVALIDO");
            }

            await using SqlConnection connection = _connectionFactory.CreateConnection(descriptor);
            await connection.OpenAsync(cancellationToken);
            await EnsureProductoAsync(connection, idEmpresa, idProductoServicio, cancellationToken);

            using SqlCommand command = Command(connection, @"
SELECT c.id, c.Nombre, COALESCE(c.Codigo, '') AS Codigo,
       COUNT(DISTINCT d.idVariante) AS VariantesConfiguradas
FROM dbo.CurvasCatalogo c
INNER JOIN dbo.CurvasDetalle d
    ON d.idEmpresa = c.idEmpresa
   AND d.idCurva = c.id
   AND d.idProductoServicio = @IdProductoServicio
   AND d.Activo = 1
   AND d.FechaArchivado IS NULL
WHERE c.idEmpresa = @IdEmpresa
  AND c.Estado = 1
  AND c.Activo = 1
  AND c.FechaArchivado IS NULL
GROUP BY c.id, c.Nombre, c.Codigo
ORDER BY c.Nombre, c.Codigo", ("@IdEmpresa", idEmpresa), ("@IdProductoServicio", idProductoServicio));
            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            List<CurvasAplicableDto> result = new();
            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(new CurvasAplicableDto
                {
                    Id = reader.GetGuid(reader.GetOrdinal("id")),
                    Nombre = reader.GetString(reader.GetOrdinal("Nombre")),
                    Codigo = reader.GetString(reader.GetOrdinal("Codigo")),
                    VariantesConfiguradas = reader.GetInt32(reader.GetOrdinal("VariantesConfiguradas"))
                });
            }

            return result;
        }

        public async Task<IReadOnlyList<CurvasSiembraSucursalDto>> ListarSucursalesAnalisisAsync(
            TenantDatabaseDescriptor descriptor,
            Guid idEmpresa,
            CancellationToken cancellationToken = default)
        {
            EnsureTenant(descriptor, idEmpresa);
            await using SqlConnection connection = _connectionFactory.CreateConnection(descriptor);
            await connection.OpenAsync(cancellationToken);
            using SqlCommand command = Command(connection, @"
SELECT id, ISNULL(NULLIF(LTRIM(RTRIM(Nombre)), ''), 'Sucursal') AS Nombre
FROM dbo.Sucursales
WHERE idEmpresa = @IdEmpresa AND ISNULL(borrado, 0) = 0
ORDER BY Nombre", ("@IdEmpresa", idEmpresa));
            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            List<CurvasSiembraSucursalDto> result = new();
            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(new CurvasSiembraSucursalDto
                {
                    Id = reader.GetGuid(reader.GetOrdinal("id")),
                    Nombre = reader.GetString(reader.GetOrdinal("Nombre"))
                });
            }
            return result;
        }

        public async Task<CurvasAnalisisResponse> AnalizarAsync(
            TenantDatabaseDescriptor descriptor,
            Guid idEmpresa,
            CurvasAnalisisRequest request,
            CancellationToken cancellationToken = default)
        {
            EnsureTenant(descriptor, idEmpresa);
            if (request == null) throw new InvalidOperationException("CURVA_ANALISIS_REQUEST_REQUERIDO");

            int page = Math.Max(request.Pagina, 1);
            int pageSize = request.TamanoPagina is 25 or 50 or 100 ? request.TamanoPagina : 25;
            string status = NormalizeAnalysisStatus(request.Estado);
            string product = (request.Producto ?? string.Empty).Trim();
            Stopwatch stopwatch = Stopwatch.StartNew();

            await using SqlConnection connection = _connectionFactory.CreateConnection(descriptor);
            await connection.OpenAsync(cancellationToken);
            using SqlCommand command = new(@"
;WITH SucursalesFiltradas AS (
    SELECT s.id, ISNULL(NULLIF(LTRIM(RTRIM(s.Nombre)), ''), 'Sucursal') AS Nombre
    FROM dbo.Sucursales s
    WHERE s.idEmpresa = @IdEmpresa
      AND ISNULL(s.borrado, 0) = 0
      AND (@IdSucursal IS NULL OR s.id = @IdSucursal)
), Identidades AS (
    SELECT p.id AS IdProductoServicio, ISNULL(p.Codigo, '') AS Codigo, p.Nombre AS Producto,
           v.id AS IdVariante, ISNULL(v.Nombre, '') AS Variante
    FROM dbo.ProductosServicios p
    LEFT JOIN dbo.ProductosServiciosVariantes v
      ON v.idEmpresa = p.idEmpresa AND v.idProductoServicio = p.id AND v.Activo = 1
    WHERE p.idEmpresa = @IdEmpresa AND p.Tipo = @TipoProducto AND p.Activo = 1
      AND (@Producto = '' OR p.Nombre LIKE @ProductoLike OR ISNULL(p.Codigo, '') LIKE @ProductoLike OR ISNULL(v.Nombre, '') LIKE @ProductoLike)
      AND (v.id IS NOT NULL OR NOT EXISTS (
          SELECT 1 FROM dbo.ProductosServiciosVariantes vx
          WHERE vx.idEmpresa = p.idEmpresa AND vx.idProductoServicio = p.id AND vx.Activo = 1))
), Existencias AS (
    SELECT i.idSucursal, i.idProductoServicio, i.idVariante, SUM(i.CantidadBaseActual) AS Existencia
    FROM dbo.InventarioSaldos i
    WHERE i.idEmpresa = @IdEmpresa
    GROUP BY i.idSucursal, i.idProductoServicio, i.idVariante
), Transitos AS (
    SELECT d.idSucursal, d.idProductoServicio, d.idVariante, SUM(d.CantidadBasePendiente) AS Transito
    FROM dbo.OrdenesCompraDetalle d
    INNER JOIN dbo.OrdenesCompra oc ON oc.idEmpresa = d.idEmpresa AND oc.id = d.idOrdenCompra
    WHERE d.idEmpresa = @IdEmpresa
      AND oc.Estado IN (@EstadoGenerada, @EstadoParcial)
      AND oc.Activo = 1 AND oc.FechaArchivado IS NULL
      AND d.Activo = 1 AND d.FechaArchivado IS NULL
      AND d.TipoProductoServicio = @TipoProducto
      AND d.EstadoPartida IN (@PartidaPendiente, @PartidaParcial)
      AND d.CantidadBasePendiente > 0
    GROUP BY d.idSucursal, d.idProductoServicio, d.idVariante
), Base AS (
    SELECT su.id AS IdSucursal, su.Nombre AS Sucursal,
           i.IdProductoServicio, i.Codigo, i.Producto, i.IdVariante, i.Variante,
           CASE WHEN cd.id IS NULL THEN NULL ELSE c.id END AS IdCurva,
           CASE WHEN cd.id IS NULL THEN '' ELSE ISNULL(c.Nombre, '') END AS Curva,
           CAST(ISNULL(cd.CantidadBaseObjetivo, 0) AS decimal(18,6)) AS Objetivo,
           CAST(ISNULL(ex.Existencia, 0) AS decimal(18,6)) AS Existencia,
           CAST(ISNULL(tr.Transito, 0) AS decimal(18,6)) AS Transito
    FROM SucursalesFiltradas su
    CROSS JOIN Identidades i
    LEFT JOIN dbo.CurvasSiembra si
      ON si.idEmpresa = @IdEmpresa AND si.idSucursal = su.id
     AND si.idProductoServicio = i.IdProductoServicio
     AND ((si.idVariante IS NULL AND i.IdVariante IS NULL) OR si.idVariante = i.IdVariante)
     AND si.Estado = 1 AND si.FechaVigenciaFin IS NULL
    LEFT JOIN dbo.CurvasCatalogo c
      ON c.idEmpresa = si.idEmpresa AND c.id = si.idCurva
     AND c.Estado = 1 AND c.Activo = 1 AND c.FechaArchivado IS NULL
    LEFT JOIN dbo.CurvasDetalle cd
      ON cd.idEmpresa = si.idEmpresa AND cd.idCurva = si.idCurva
     AND cd.idProductoServicio = i.IdProductoServicio
     AND ((cd.idVariante IS NULL AND i.IdVariante IS NULL) OR cd.idVariante = i.IdVariante)
     AND cd.Activo = 1 AND cd.FechaArchivado IS NULL
    LEFT JOIN Existencias ex
      ON ex.idSucursal = su.id AND ex.idProductoServicio = i.IdProductoServicio
     AND ((ex.idVariante IS NULL AND i.IdVariante IS NULL) OR ex.idVariante = i.IdVariante)
    LEFT JOIN Transitos tr
      ON tr.idSucursal = su.id AND tr.idProductoServicio = i.IdProductoServicio
     AND ((tr.idVariante IS NULL AND i.IdVariante IS NULL) OR tr.idVariante = i.IdVariante)
), Calculado AS (
    SELECT *, CAST(Existencia + Transito AS decimal(18,6)) AS Cobertura,
           CAST(CASE WHEN IdCurva IS NOT NULL AND Objetivo > Existencia + Transito THEN Objetivo - (Existencia + Transito) ELSE 0 END AS decimal(18,6)) AS Hueco,
           CAST(CASE WHEN IdCurva IS NOT NULL AND Existencia + Transito > Objetivo THEN (Existencia + Transito) - Objetivo ELSE 0 END AS decimal(18,6)) AS Copete,
           CASE WHEN IdCurva IS NULL THEN 'SIN_CURVA'
                WHEN Objetivo > Existencia + Transito THEN 'HUECO'
                WHEN Existencia + Transito > Objetivo THEN 'COPETE'
                ELSE 'EN_OBJETIVO' END AS Estado
    FROM Base
), Filtrado AS (
    SELECT * FROM Calculado WHERE @Estado = 'TODOS' OR Estado = @Estado
)
SELECT IdSucursal, Sucursal, IdProductoServicio, Codigo, Producto, IdVariante, Variante,
       IdCurva, Curva, Objetivo, Existencia, Transito, Cobertura, Hueco, Copete, Estado,
       COUNT(1) OVER() AS TotalRegistros
FROM Filtrado
ORDER BY Sucursal, Producto, Variante, IdProductoServicio, IdVariante
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY", connection);
            command.Parameters.Add("@IdEmpresa", SqlDbType.UniqueIdentifier).Value = idEmpresa;
            command.Parameters.Add("@IdSucursal", SqlDbType.UniqueIdentifier).Value = (object?)request.IdSucursal ?? DBNull.Value;
            command.Parameters.Add("@Producto", SqlDbType.NVarChar, 150).Value = product;
            command.Parameters.Add("@ProductoLike", SqlDbType.NVarChar, 160).Value = $"%{product}%";
            command.Parameters.Add("@Estado", SqlDbType.VarChar, 20).Value = status;
            command.Parameters.Add("@TipoProducto", SqlDbType.TinyInt).Value = TipoProducto;
            command.Parameters.Add("@EstadoGenerada", SqlDbType.TinyInt).Value = OcEstadoGenerada;
            command.Parameters.Add("@EstadoParcial", SqlDbType.TinyInt).Value = OcEstadoParcialmenteRecibida;
            command.Parameters.Add("@PartidaPendiente", SqlDbType.TinyInt).Value = PartidaPendiente;
            command.Parameters.Add("@PartidaParcial", SqlDbType.TinyInt).Value = PartidaParcial;
            command.Parameters.Add("@Offset", SqlDbType.Int).Value = (page - 1) * pageSize;
            command.Parameters.Add("@PageSize", SqlDbType.Int).Value = pageSize;

            List<CurvasAnalisisItemDto> items = new();
            int total = 0;
            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                total = reader.GetInt32(reader.GetOrdinal("TotalRegistros"));
                items.Add(new CurvasAnalisisItemDto
                {
                    IdSucursal = reader.GetGuid(reader.GetOrdinal("IdSucursal")),
                    Sucursal = reader.GetString(reader.GetOrdinal("Sucursal")),
                    IdProductoServicio = reader.GetGuid(reader.GetOrdinal("IdProductoServicio")),
                    Codigo = reader.GetString(reader.GetOrdinal("Codigo")),
                    Producto = reader.GetString(reader.GetOrdinal("Producto")),
                    IdVariante = reader.IsDBNull(reader.GetOrdinal("IdVariante")) ? null : reader.GetGuid(reader.GetOrdinal("IdVariante")),
                    Variante = reader.GetString(reader.GetOrdinal("Variante")),
                    IdCurva = reader.IsDBNull(reader.GetOrdinal("IdCurva")) ? null : reader.GetGuid(reader.GetOrdinal("IdCurva")),
                    Curva = reader.GetString(reader.GetOrdinal("Curva")),
                    Objetivo = reader.GetDecimal(reader.GetOrdinal("Objetivo")),
                    Existencia = reader.GetDecimal(reader.GetOrdinal("Existencia")),
                    Transito = reader.GetDecimal(reader.GetOrdinal("Transito")),
                    Cobertura = reader.GetDecimal(reader.GetOrdinal("Cobertura")),
                    Hueco = reader.GetDecimal(reader.GetOrdinal("Hueco")),
                    Copete = reader.GetDecimal(reader.GetOrdinal("Copete")),
                    Estado = reader.GetString(reader.GetOrdinal("Estado"))
                });
            }
            stopwatch.Stop();
            return new CurvasAnalisisResponse
            {
                Pagina = page,
                TamanoPagina = pageSize,
                TotalRegistros = total,
                DuracionMilisegundos = stopwatch.ElapsedMilliseconds,
                Items = items
            };
        }

        private static void EnsureTenant(TenantDatabaseDescriptor descriptor, Guid idEmpresa)
        {
            if (idEmpresa == Guid.Empty || descriptor.IdEmpresa != idEmpresa)
                throw new InvalidOperationException("CURVA_TENANT_INVALIDO");
        }

        private static string NormalizeAnalysisStatus(string? value)
        {
            string status = (value ?? "TODOS").Trim().ToUpperInvariant();
            return status is "TODOS" or "HUECO" or "COPETE" or "EN_OBJETIVO" or "SIN_CURVA"
                ? status
                : throw new InvalidOperationException("CURVA_ANALISIS_ESTADO_INVALIDO");
        }

        private static void ValidateItem(CurvasSugerenciaItemRequest item)
        {
            if (item == null || item.IdSucursal == Guid.Empty || item.IdProductoServicio == Guid.Empty)
            {
                throw new InvalidOperationException("CURVA_SUGERENCIA_ITEM_INVALIDO");
            }

            if (!Enum.IsDefined(typeof(CurvasModoCaptura), item.Modo))
            {
                throw new InvalidOperationException("CURVA_MODO_INVALIDO");
            }

            if (item.CantidadManualBase.HasValue && item.CantidadManualBase.Value < 0)
            {
                throw new InvalidOperationException("CURVA_MANUAL_INVALIDO");
            }
        }

        private static async Task EnsureSucursalAsync(SqlConnection connection, Guid idEmpresa, Guid idSucursal, CancellationToken cancellationToken)
        {
            if (!await ExistsAsync(connection, "SELECT 1 FROM dbo.Sucursales WHERE idEmpresa=@IdEmpresa AND id=@Id", cancellationToken, ("@IdEmpresa", idEmpresa), ("@Id", idSucursal)))
            {
                throw new InvalidOperationException("CURVA_SUCURSAL_NO_DISPONIBLE");
            }
        }

        private static async Task EnsureProductoAsync(SqlConnection connection, Guid idEmpresa, Guid idProducto, CancellationToken cancellationToken)
        {
            byte? tipo = await ScalarByteAsync(connection, "SELECT Tipo FROM dbo.ProductosServicios WHERE idEmpresa=@IdEmpresa AND id=@Id AND Activo=1", cancellationToken, ("@IdEmpresa", idEmpresa), ("@Id", idProducto));
            if (!tipo.HasValue)
            {
                throw new InvalidOperationException("CURVA_PRODUCTO_NO_DISPONIBLE");
            }

            if (tipo.Value != TipoProducto)
            {
                throw new InvalidOperationException("CURVA_SERVICIO_RECHAZADO");
            }
        }

        private static async Task EnsureVarianteAsync(SqlConnection connection, Guid idEmpresa, Guid idProducto, Guid idVariante, CancellationToken cancellationToken)
        {
            if (!await ExistsAsync(connection, "SELECT 1 FROM dbo.ProductosServiciosVariantes WHERE idEmpresa=@IdEmpresa AND idProductoServicio=@Producto AND id=@Variante AND Activo=1", cancellationToken, ("@IdEmpresa", idEmpresa), ("@Producto", idProducto), ("@Variante", idVariante)))
            {
                throw new InvalidOperationException("CURVA_VARIANTE_NO_DISPONIBLE");
            }
        }

        private static async Task<CurvaSiembraSnapshot?> LoadSiembraAsync(SqlConnection connection, Guid idEmpresa, CurvasSugerenciaItemRequest item, CancellationToken cancellationToken)
        {
            using SqlCommand command = Command(connection, @"
SELECT s.id AS IdSiembra, s.idCurva, c.Nombre AS CurvaNombre, d.CantidadBaseObjetivo
FROM dbo.CurvasSiembra s
INNER JOIN dbo.CurvasCatalogo c
    ON c.idEmpresa = s.idEmpresa
   AND c.id = s.idCurva
   AND c.Estado = 1
   AND c.Activo = 1
   AND c.FechaArchivado IS NULL
INNER JOIN dbo.CurvasDetalle d
    ON d.idEmpresa = s.idEmpresa
   AND d.idCurva = s.idCurva
   AND d.idProductoServicio = s.idProductoServicio
   AND ((d.idVariante IS NULL AND s.idVariante IS NULL) OR d.idVariante = s.idVariante)
   AND d.Activo = 1
   AND d.FechaArchivado IS NULL
WHERE s.idEmpresa = @IdEmpresa
  AND s.idSucursal = @IdSucursal
  AND s.idProductoServicio = @IdProductoServicio
  AND ((s.idVariante IS NULL AND @IdVariante IS NULL) OR s.idVariante = @IdVariante)
  AND s.Estado = 1
  AND s.FechaVigenciaFin IS NULL", ("@IdEmpresa", idEmpresa), ("@IdSucursal", item.IdSucursal), ("@IdProductoServicio", item.IdProductoServicio), ("@IdVariante", Db(item.IdVariante)));
            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

            CurvaSiembraSnapshot? result = null;
            while (await reader.ReadAsync(cancellationToken))
            {
                if (result != null)
                {
                    throw new InvalidOperationException("CURVA_SIEMBRA_AMBIGUA");
                }

                result = new CurvaSiembraSnapshot(
                    reader.GetGuid(reader.GetOrdinal("idCurva")),
                    reader.GetGuid(reader.GetOrdinal("IdSiembra")),
                    reader.GetDecimal(reader.GetOrdinal("CantidadBaseObjetivo")),
                    reader.GetString(reader.GetOrdinal("CurvaNombre")));
            }

            return result;
        }

        private static async Task<CurvaSiembraSnapshot?> LoadCurvaTemporalAsync(SqlConnection connection, Guid idEmpresa, CurvasSugerenciaItemRequest item, CancellationToken cancellationToken)
        {
            using SqlCommand command = Command(connection, @"
SELECT TOP (1) c.id AS idCurva, c.Nombre AS CurvaNombre, d.CantidadBaseObjetivo
FROM dbo.CurvasCatalogo c
INNER JOIN dbo.CurvasDetalle d
    ON d.idEmpresa = c.idEmpresa
   AND d.idCurva = c.id
   AND d.idProductoServicio = @IdProductoServicio
   AND (d.idVariante = @IdVariante OR d.idVariante IS NULL)
   AND d.Activo = 1
   AND d.FechaArchivado IS NULL
WHERE c.idEmpresa = @IdEmpresa
  AND c.id = @IdCurva
  AND c.Estado = 1
  AND c.Activo = 1
  AND c.FechaArchivado IS NULL
ORDER BY CASE WHEN d.idVariante = @IdVariante THEN 0 ELSE 1 END", ("@IdEmpresa", idEmpresa), ("@IdCurva", item.IdCurvaTemporal), ("@IdProductoServicio", item.IdProductoServicio), ("@IdVariante", Db(item.IdVariante)));
            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return new CurvaSiembraSnapshot(
                reader.GetGuid(reader.GetOrdinal("idCurva")),
                null,
                reader.GetDecimal(reader.GetOrdinal("CantidadBaseObjetivo")),
                reader.GetString(reader.GetOrdinal("CurvaNombre")));
        }

        private static async Task<decimal> LoadExistenciaAsync(SqlConnection connection, Guid idEmpresa, CurvasSugerenciaItemRequest item, CancellationToken cancellationToken)
        {
            object? value = await ScalarAsync(connection, @"
SELECT COALESCE(SUM(CantidadBaseActual), 0)
FROM dbo.InventarioSaldos
WHERE idEmpresa = @IdEmpresa
  AND idSucursal = @IdSucursal
  AND idProductoServicio = @IdProductoServicio
  AND ((idVariante IS NULL AND @IdVariante IS NULL) OR idVariante = @IdVariante)", cancellationToken, ("@IdEmpresa", idEmpresa), ("@IdSucursal", item.IdSucursal), ("@IdProductoServicio", item.IdProductoServicio), ("@IdVariante", Db(item.IdVariante)));
            return ToDecimal(value);
        }

        private static async Task<decimal> LoadTransitoAsync(SqlConnection connection, Guid idEmpresa, CurvasSugerenciaItemRequest item, CancellationToken cancellationToken)
        {
            object? value = await ScalarAsync(connection, @"
SELECT COALESCE(SUM(d.CantidadBasePendiente), 0)
FROM dbo.OrdenesCompraDetalle d
INNER JOIN dbo.OrdenesCompra oc
    ON oc.idEmpresa = d.idEmpresa
   AND oc.id = d.idOrdenCompra
WHERE d.idEmpresa = @IdEmpresa
  AND d.idSucursal = @IdSucursal
  AND oc.Estado IN (@EstadoGenerada, @EstadoParcial)
  AND oc.Activo = 1
  AND oc.FechaArchivado IS NULL
  AND d.Activo = 1
  AND d.FechaArchivado IS NULL
  AND d.TipoProductoServicio = @TipoProducto
  AND d.EstadoPartida IN (@PartidaPendiente, @PartidaParcial)
  AND d.CantidadBasePendiente > 0
  AND d.idProductoServicio = @IdProductoServicio
  AND ((d.idVariante IS NULL AND @IdVariante IS NULL) OR d.idVariante = @IdVariante)", cancellationToken,
                ("@IdEmpresa", idEmpresa),
                ("@IdSucursal", item.IdSucursal),
                ("@EstadoGenerada", OcEstadoGenerada),
                ("@EstadoParcial", OcEstadoParcialmenteRecibida),
                ("@TipoProducto", TipoProducto),
                ("@PartidaPendiente", PartidaPendiente),
                ("@PartidaParcial", PartidaParcial),
                ("@IdProductoServicio", item.IdProductoServicio),
                ("@IdVariante", Db(item.IdVariante)));
            return ToDecimal(value);
        }

        private static async Task<PresentacionCompraSnapshot> LoadPresentacionAsync(SqlConnection connection, Guid idEmpresa, CurvasSugerenciaItemRequest item, CancellationToken cancellationToken)
        {
            using SqlCommand command = Command(connection, @"
SELECT id, FactorConversionBase, PermiteCantidadBase
FROM dbo.OrdenesCompraPresentacionesCompra
WHERE idEmpresa = @IdEmpresa
  AND id = @Id
  AND idProductoServicio = @IdProductoServicio
  AND ((idVariante IS NULL AND @IdVariante IS NULL) OR idVariante = @IdVariante)
  AND Activo = 1
  AND FechaArchivado IS NULL", ("@IdEmpresa", idEmpresa), ("@Id", item.IdPresentacionCompra!.Value), ("@IdProductoServicio", item.IdProductoServicio), ("@IdVariante", Db(item.IdVariante)));
            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException("CURVA_PRESENTACION_COMPRA_NO_DISPONIBLE");
            }

            return new PresentacionCompraSnapshot(
                reader.GetGuid(reader.GetOrdinal("id")),
                reader.GetDecimal(reader.GetOrdinal("FactorConversionBase")),
                reader.GetBoolean(reader.GetOrdinal("PermiteCantidadBase")));
        }

        private static async Task<bool> ExistsAsync(SqlConnection connection, string sql, CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
            => await ScalarAsync(connection, sql, cancellationToken, parameters) is not null;

        private static async Task<byte?> ScalarByteAsync(SqlConnection connection, string sql, CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
        {
            object? value = await ScalarAsync(connection, sql, cancellationToken, parameters);
            return value == null ? null : Convert.ToByte(value);
        }

        private static async Task<object?> ScalarAsync(SqlConnection connection, string sql, CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
        {
            using SqlCommand command = Command(connection, sql, parameters);
            object? value = await command.ExecuteScalarAsync(cancellationToken);
            return value == null || value == DBNull.Value ? null : value;
        }

        private static SqlCommand Command(SqlConnection connection, string sql, params (string Name, object? Value)[] parameters)
        {
            SqlCommand command = new(sql, connection);
            foreach ((string name, object? value) in parameters)
            {
                command.Parameters.AddWithValue(name, value ?? DBNull.Value);
            }

            return command;
        }

        private static decimal ToDecimal(object? value) => value == null || value == DBNull.Value ? 0 : Convert.ToDecimal(value);
        private static object Db(Guid? value) => value.HasValue && value.Value != Guid.Empty ? value.Value : DBNull.Value;
    }

    public static class CurvasSugerenciasCalculator
    {
        public static CurvasSugerenciaResult Calculate(
            CurvasSugerenciaItemRequest item,
            CurvaSiembraSnapshot? siembra,
            decimal existenciaBase,
            decimal transitoBase,
            PresentacionCompraSnapshot? presentacion)
        {
            decimal objetivo = siembra?.CurvaObjetivoBase ?? 0;
            decimal cobertura = existenciaBase + transitoBase;
            decimal hueco = Math.Max(objetivo - cobertura, 0);
            decimal copete = Math.Max(cobertura - objetivo, 0);
            bool tieneCurva = siembra != null;

            CurvasSugerenciaEstado estadoBase = !tieneCurva
                ? CurvasSugerenciaEstado.SinCurva
                : hueco > 0
                    ? CurvasSugerenciaEstado.Hueco
                    : copete > 0
                        ? CurvasSugerenciaEstado.Copete
                        : CurvasSugerenciaEstado.Completa;

            bool overrideManual = item.Modo == CurvasModoCaptura.Manual;
            bool noPedir = item.Modo == CurvasModoCaptura.NoPedir;
            CurvasSugerenciaEstado estado = overrideManual
                ? CurvasSugerenciaEstado.Manual
                : noPedir
                    ? CurvasSugerenciaEstado.NoPedir
                    : estadoBase;

            decimal propuesta = item.Modo switch
            {
                CurvasModoCaptura.PedidoInicial => Math.Max(objetivo, 0),
                CurvasModoCaptura.RellenarCurva => tieneCurva ? hueco : 0,
                CurvasModoCaptura.Manual => Math.Max(item.CantidadManualBase ?? 0, 0),
                CurvasModoCaptura.NoPedir => 0,
                _ => 0
            };

            PresentationConversion conversion = ConvertPresentation(propuesta, presentacion);

            CurvasSugerenciaResult result = new()
            {
                IdSucursal = item.IdSucursal,
                IdProductoServicio = item.IdProductoServicio,
                IdVariante = item.IdVariante,
                IdCurva = siembra?.IdCurva,
                CurvaNombre = siembra?.CurvaNombre ?? string.Empty,
                IdSiembra = siembra?.IdSiembra,
                Modo = item.Modo,
                Estado = estado,
                CurvaObjetivoBase = objetivo,
                ExistenciaBase = existenciaBase,
                TransitoBase = transitoBase,
                CoberturaBase = cobertura,
                HuecoBase = hueco,
                CopeteBase = copete,
                CantidadPropuestaBase = propuesta,
                CantidadFinalBase = conversion.CantidadBaseConvertida,
                IdPresentacionCompra = presentacion?.Id,
                PermiteCantidadBase = presentacion?.PermiteCantidadBase ?? true,
                FactorConversionBase = presentacion?.FactorConversionBase,
                CantidadCompraSugerida = conversion.CantidadCompraSugerida,
                CantidadBaseConvertida = conversion.CantidadBaseConvertida,
                ExcedenteRedondeoBase = conversion.ExcedenteRedondeoBase,
                OverrideManual = overrideManual,
                NoPedir = noPedir,
                PreviewReadOnly = true
            };

            result.SnapshotPayload = new CurvaSnapshotPreviewPayload
            {
                IdCurva = result.IdCurva,
                IdSiembra = result.IdSiembra,
                IdSucursal = result.IdSucursal,
                IdProductoServicio = result.IdProductoServicio,
                IdVariante = result.IdVariante,
                Modo = result.Modo,
                CurvaObjetivoBase = result.CurvaObjetivoBase,
                ExistenciaSnapshotBase = result.ExistenciaBase,
                TransitoSnapshotBase = result.TransitoBase,
                HuecoSnapshotBase = result.HuecoBase,
                CopeteSnapshotBase = result.CopeteBase,
                CantidadPropuestaBase = result.CantidadPropuestaBase,
                CantidadFinalBase = result.CantidadFinalBase,
                OverrideManual = result.OverrideManual,
                NoPedir = result.NoPedir,
                IdPresentacionCompra = result.IdPresentacionCompra,
                PermiteCantidadBaseSnapshot = result.PermiteCantidadBase,
                ContextoJson = item.ContextoJson
            };

            return result;
        }

        private static PresentationConversion ConvertPresentation(decimal cantidadPropuestaBase, PresentacionCompraSnapshot? presentacion)
        {
            if (presentacion == null)
            {
                return new PresentationConversion(cantidadPropuestaBase, cantidadPropuestaBase, 0);
            }

            if (presentacion.FactorConversionBase <= 0)
            {
                throw new InvalidOperationException("CURVA_PRESENTACION_FACTOR_INVALIDO");
            }

            if (presentacion.PermiteCantidadBase)
            {
                return new PresentationConversion(cantidadPropuestaBase, cantidadPropuestaBase, 0);
            }

            decimal cantidadCompra = cantidadPropuestaBase <= 0 ? 0 : Math.Ceiling(cantidadPropuestaBase / presentacion.FactorConversionBase);
            decimal cantidadBaseConvertida = cantidadCompra * presentacion.FactorConversionBase;
            decimal excedente = Math.Max(cantidadBaseConvertida - cantidadPropuestaBase, 0);
            return new PresentationConversion(cantidadCompra, cantidadBaseConvertida, excedente);
        }
    }

    public sealed record CurvaSiembraSnapshot(Guid IdCurva, Guid? IdSiembra, decimal CurvaObjetivoBase, string CurvaNombre = "");
    public sealed record PresentacionCompraSnapshot(Guid Id, decimal FactorConversionBase, bool PermiteCantidadBase);
    public sealed record PresentationConversion(decimal CantidadCompraSugerida, decimal CantidadBaseConvertida, decimal ExcedenteRedondeoBase);
}
