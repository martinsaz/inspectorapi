using System.Data;
using System.Data.SqlClient;
using checklistWs.Models.Recepcion;

namespace checklistWs.Services.Tenant
{
    public sealed class RecepcionScopeService : IRecepcionScopeService
    {
        private const byte OcEstadoGenerada = 2;
        private const byte OcEstadoCancelada = 3;
        private const byte OcEstadoParcialmenteRecibida = 4;
        private const byte OcEstadoRecibida = 5;
        private const byte TipoProducto = 1;
        private const byte TipoServicio = 2;
        private const byte PartidaPendiente = 1;
        private const byte PartidaParcial = 2;
        private const byte PartidaRecibida = 3;
        private const int OperationKeyMaxLength = 120;
        private const int ObservacionesMaxLength = 1000;

        private readonly ITenantSqlConnectionFactory _connectionFactory;
        private readonly IInventarioScopeLedgerService _ledgerService;

        public RecepcionScopeService(
            ITenantSqlConnectionFactory connectionFactory,
            IInventarioScopeLedgerService ledgerService)
        {
            _connectionFactory = connectionFactory;
            _ledgerService = ledgerService;
        }

        public async Task<RecepcionOperacionResponse> ConfirmarAsync(
            TenantDatabaseDescriptor descriptor,
            RecepcionConfirmarRequest request,
            Guid? idUsuario,
            CancellationToken cancellationToken = default)
        {
            ValidateRequest(descriptor, request);

            await using SqlConnection connection = _connectionFactory.CreateConnection(descriptor);
            await connection.OpenAsync(cancellationToken);
            await using SqlTransaction transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

            try
            {
                ExistingRecepcion? existing = await FindExistingAsync(connection, transaction, request.IdEmpresa, request.OperationKey, cancellationToken);
                if (existing != null)
                {
                    await transaction.CommitAsync(cancellationToken);
                    return new RecepcionOperacionResponse
                    {
                        Exito = true,
                        Mensaje = "La recepción ya estaba confirmada.",
                        IdRecepcion = existing.Id,
                        FolioRecepcion = existing.Folio,
                        AlreadyApplied = true
                    };
                }

                await AcquireOcLockAsync(connection, transaction, request.IdEmpresa, request.IdOrdenCompra, cancellationToken);
                existing = await FindExistingAsync(connection, transaction, request.IdEmpresa, request.OperationKey, cancellationToken);
                if (existing != null)
                {
                    await transaction.CommitAsync(cancellationToken);
                    return new RecepcionOperacionResponse
                    {
                        Exito = true,
                        Mensaje = "La recepción ya estaba confirmada.",
                        IdRecepcion = existing.Id,
                        FolioRecepcion = existing.Folio,
                        AlreadyApplied = true
                    };
                }

                OrdenSnapshot orden = await LoadOrdenAsync(connection, transaction, request, cancellationToken);
                string folio = await NextFolioAsync(connection, transaction, request.IdEmpresa, cancellationToken);
                Guid recepcionId = Guid.NewGuid();
                DateTime fecha = request.FechaRecepcion ?? DateTime.UtcNow;

                await InsertRecepcionAsync(connection, transaction, recepcionId, request, folio, fecha, idUsuario, cancellationToken);

                foreach (RecepcionPartidaConfirmarRequest partidaRequest in request.Partidas)
                {
                    PartidaSnapshot partida = await LoadPartidaAsync(connection, transaction, request.IdEmpresa, request.IdOrdenCompra, partidaRequest.IdOrdenCompraDetalle, cancellationToken);
                    await ApplyPartidaAsync(connection, transaction, recepcionId, request, partidaRequest, partida, idUsuario, cancellationToken);
                }

                await RecalculateOrdenStateAsync(connection, transaction, request.IdEmpresa, request.IdOrdenCompra, cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return new RecepcionOperacionResponse
                {
                    Exito = true,
                    Mensaje = "La recepción fue confirmada.",
                    IdRecepcion = recepcionId,
                    FolioRecepcion = folio,
                    AlreadyApplied = false
                };
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None);
                throw;
            }
        }

        private async Task ApplyPartidaAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            Guid recepcionId,
            RecepcionConfirmarRequest request,
            RecepcionPartidaConfirmarRequest partidaRequest,
            PartidaSnapshot partida,
            Guid? idUsuario,
            CancellationToken cancellationToken)
        {
            decimal cantidadCompra = decimal.Round(partidaRequest.CantidadCompraRecibida, 4, MidpointRounding.AwayFromZero);
            if (cantidadCompra <= 0)
            {
                throw new InvalidOperationException("RECEPCION_CANTIDAD_INVALIDA");
            }

            decimal cantidadBase = decimal.Round(cantidadCompra * partida.FactorConversionSnapshot, 4, MidpointRounding.AwayFromZero);
            decimal pendienteAntes = partida.CantidadBaseOrdenada - partida.CantidadBaseRecibidaAcumulada;
            if (cantidadBase <= 0 || cantidadBase > pendienteAntes)
            {
                throw new InvalidOperationException("RECEPCION_SOBRE_RECEPCION_NO_PERMITIDA");
            }

            decimal nuevoAcumulado = partida.CantidadBaseRecibidaAcumulada + cantidadBase;
            decimal nuevoPendiente = partida.CantidadBaseOrdenada - nuevoAcumulado;
            byte nuevoEstadoPartida = nuevoPendiente == 0 ? PartidaRecibida : PartidaParcial;
            bool controlSerie = partida.TipoPartida == TipoProducto && partida.UsaNumeroSerie;
            ValidateNoDuplicateSeries(partidaRequest.Series);
            string[] series = NormalizeSeries(partidaRequest.Series);

            if (partida.TipoPartida == TipoServicio && series.Length > 0)
            {
                throw new InvalidOperationException("RECEPCION_SERVICIO_NO_ADMITE_SERIES");
            }

            if (controlSerie)
            {
                if (cantidadBase != decimal.Truncate(cantidadBase) || series.Length != Convert.ToInt32(cantidadBase))
                {
                    throw new InvalidOperationException("RECEPCION_SERIES_INCONSISTENTES");
                }
            }
            else if (series.Length > 0)
            {
                throw new InvalidOperationException("RECEPCION_SERIES_NO_APLICAN");
            }

            Guid? movimientoId = null;
            if (partida.TipoPartida == TipoProducto && partida.CausaInventario)
            {
                InventarioMovimientoResult movimiento = await _ledgerService.ApplyMovementAsync(connection, transaction, new InventarioMovimientoRequest
                {
                    IdEmpresa = request.IdEmpresa,
                    IdSucursal = request.IdSucursal,
                    IdProductoServicio = partida.IdProductoServicio,
                    IdVariante = partida.IdVariante,
                    TipoMovimiento = InventarioTipoMovimiento.Entrada,
                    CantidadBase = cantidadBase,
                    OrigenTipo = "RecepcionOC",
                    OrigenId = recepcionId,
                    OrigenPartidaId = partida.Id,
                    OperationKey = BuildPartidaOperationKey(request.OperationKey, partida.Id),
                    Observaciones = request.Observaciones,
                    IdUsuario = idUsuario,
                    FechaMovimiento = request.FechaRecepcion ?? DateTime.UtcNow
                }, cancellationToken);
                movimientoId = movimiento.MovimientoId;
            }

            Guid recepcionPartidaId = Guid.NewGuid();
            await InsertPartidaAsync(connection, transaction, recepcionPartidaId, recepcionId, partida, cantidadCompra, cantidadBase, nuevoAcumulado, nuevoPendiente, nuevoEstadoPartida, controlSerie, movimientoId, cancellationToken);

            foreach (string numeroSerie in series)
            {
                Guid inventarioSerieId = await InsertInventarioSerieAsync(connection, transaction, request, partida, numeroSerie, movimientoId, cancellationToken);
                await InsertRecepcionSerieAsync(connection, transaction, recepcionId, recepcionPartidaId, partida, numeroSerie, inventarioSerieId, cancellationToken);
            }

            using SqlCommand updateDetalle = new(@"
UPDATE dbo.OrdenesCompraDetalle
SET CantidadBaseRecibidaAcumulada = @Acumulado,
    CantidadBasePendiente = @Pendiente,
    EstadoPartida = @EstadoPartida,
    FechaActualizacion = SYSUTCDATETIME()
WHERE idEmpresa = @IdEmpresa AND id = @IdDetalle", connection, transaction);
            updateDetalle.Parameters.AddWithValue("@IdEmpresa", request.IdEmpresa);
            updateDetalle.Parameters.AddWithValue("@IdDetalle", partida.Id);
            updateDetalle.Parameters.AddWithValue("@Acumulado", nuevoAcumulado);
            updateDetalle.Parameters.AddWithValue("@Pendiente", nuevoPendiente);
            updateDetalle.Parameters.AddWithValue("@EstadoPartida", nuevoEstadoPartida);
            if (await updateDetalle.ExecuteNonQueryAsync(cancellationToken) != 1)
            {
                throw new InvalidOperationException("RECEPCION_OC_DETALLE_UPDATE_FAILED");
            }
        }

        private static void ValidateRequest(TenantDatabaseDescriptor descriptor, RecepcionConfirmarRequest request)
        {
            if (descriptor == null ||
                request == null ||
                request.IdEmpresa == Guid.Empty ||
                request.IdEmpresa != descriptor.IdEmpresa ||
                request.IdOrdenCompra == Guid.Empty ||
                request.IdSucursal == Guid.Empty ||
                string.IsNullOrWhiteSpace(request.OperationKey) ||
                request.Partidas.Count == 0)
            {
                throw new InvalidOperationException("RECEPCION_REQUEST_INVALID");
            }

            if (request.OperationKey.Trim().Length > OperationKeyMaxLength)
            {
                throw new InvalidOperationException("RECEPCION_OPERATIONKEY_INVALID");
            }

            if (request.Partidas.Select(p => p.IdOrdenCompraDetalle).Distinct().Count() != request.Partidas.Count)
            {
                throw new InvalidOperationException("RECEPCION_PARTIDAS_DUPLICADAS");
            }
        }

        private static async Task AcquireOcLockAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idOrdenCompra, CancellationToken cancellationToken)
        {
            using SqlCommand command = new("EXEC @Result = sp_getapplock @Resource = @Resource, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 15000;", connection, transaction);
            SqlParameter result = command.Parameters.Add("@Result", SqlDbType.Int);
            result.Direction = ParameterDirection.Output;
            command.Parameters.AddWithValue("@Resource", $"RecepcionOC:{idEmpresa:N}:{idOrdenCompra:N}");
            await command.ExecuteNonQueryAsync(cancellationToken);
            if ((result.Value as int? ?? -999) < 0)
            {
                throw new TimeoutException("RECEPCION_LOCK_TIMEOUT");
            }
        }

        private static async Task<ExistingRecepcion?> FindExistingAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, string operationKey, CancellationToken cancellationToken)
        {
            using SqlCommand command = new("SELECT TOP (1) id, FolioRecepcion FROM dbo.Recepciones WHERE idEmpresa = @IdEmpresa AND OperationKey = @OperationKey", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@OperationKey", operationKey.Trim());
            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken)
                ? new ExistingRecepcion(reader.GetGuid(reader.GetOrdinal("id")), reader.GetString(reader.GetOrdinal("FolioRecepcion")))
                : null;
        }

        private static async Task<OrdenSnapshot> LoadOrdenAsync(SqlConnection connection, SqlTransaction transaction, RecepcionConfirmarRequest request, CancellationToken cancellationToken)
        {
            using SqlCommand command = new(@"
SELECT oc.id, oc.Estado, oc.idSucursal
FROM dbo.OrdenesCompra oc WITH (UPDLOCK, HOLDLOCK)
INNER JOIN dbo.Sucursales s ON s.idEmpresa = oc.idEmpresa AND s.id = @IdSucursal
WHERE oc.idEmpresa = @IdEmpresa
  AND oc.id = @IdOrdenCompra
  AND oc.idSucursal = @IdSucursal
  AND oc.Activo = 1
  AND oc.FechaArchivado IS NULL", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", request.IdEmpresa);
            command.Parameters.AddWithValue("@IdOrdenCompra", request.IdOrdenCompra);
            command.Parameters.AddWithValue("@IdSucursal", request.IdSucursal);
            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException("RECEPCION_OC_NO_DISPONIBLE");
            }

            byte estado = reader.GetByte(reader.GetOrdinal("Estado"));
            if (estado == OcEstadoCancelada || estado == OcEstadoRecibida || estado < OcEstadoGenerada)
            {
                throw new InvalidOperationException("RECEPCION_OC_ESTADO_INVALIDO");
            }

            return new OrdenSnapshot(reader.GetGuid(reader.GetOrdinal("id")), estado);
        }

        private static async Task<PartidaSnapshot> LoadPartidaAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idOrdenCompra, Guid idDetalle, CancellationToken cancellationToken)
        {
            using SqlCommand command = new(@"
SELECT
    d.id, d.idEmpresa, d.idOrdenCompra, d.NumeroPartida, d.TipoProductoServicio, d.idProductoServicio, d.idVariante,
    d.idPresentacionCompra, ISNULL(d.PresentacionCompraSnapshot, '') AS PresentacionCompraSnapshot,
    ISNULL(d.UnidadCompraSnapshot, '') AS UnidadCompraSnapshot,
    ISNULL(d.UnidadCompraAbreviaturaSnapshot, '') AS UnidadCompraAbreviaturaSnapshot,
    d.FactorConversionSnapshot, d.CantidadBaseOrdenada, d.CantidadBaseRecibidaAcumulada,
    d.CantidadBasePendiente, ps.CausaInventario, ps.UsaNumeroSerie,
    CASE WHEN d.idVariante IS NULL THEN 1
         WHEN EXISTS (SELECT 1 FROM dbo.ProductosServiciosVariantes v WHERE v.idEmpresa = d.idEmpresa AND v.id = d.idVariante AND v.idProductoServicio = d.idProductoServicio) THEN 1
         ELSE 0 END AS VarianteOk
FROM dbo.OrdenesCompraDetalle d WITH (UPDLOCK, HOLDLOCK)
INNER JOIN dbo.ProductosServicios ps ON ps.idEmpresa = d.idEmpresa AND ps.id = d.idProductoServicio
WHERE d.idEmpresa = @IdEmpresa AND d.idOrdenCompra = @IdOrdenCompra AND d.id = @IdDetalle AND d.Activo = 1 AND d.FechaArchivado IS NULL", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@IdOrdenCompra", idOrdenCompra);
            command.Parameters.AddWithValue("@IdDetalle", idDetalle);
            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException("RECEPCION_PARTIDA_NO_DISPONIBLE");
            }

            if (reader.GetInt32(reader.GetOrdinal("VarianteOk")) != 1)
            {
                throw new InvalidOperationException("RECEPCION_VARIANTE_INVALIDA");
            }

            return new PartidaSnapshot(
                reader.GetGuid(reader.GetOrdinal("id")),
                reader.GetGuid(reader.GetOrdinal("idEmpresa")),
                reader.GetGuid(reader.GetOrdinal("idOrdenCompra")),
                reader.GetInt32(reader.GetOrdinal("NumeroPartida")),
                reader.GetByte(reader.GetOrdinal("TipoProductoServicio")),
                reader.GetGuid(reader.GetOrdinal("idProductoServicio")),
                reader.IsDBNull(reader.GetOrdinal("idVariante")) ? null : reader.GetGuid(reader.GetOrdinal("idVariante")),
                reader.IsDBNull(reader.GetOrdinal("idPresentacionCompra")) ? null : reader.GetGuid(reader.GetOrdinal("idPresentacionCompra")),
                reader.GetString(reader.GetOrdinal("PresentacionCompraSnapshot")),
                reader.GetString(reader.GetOrdinal("UnidadCompraSnapshot")),
                reader.GetString(reader.GetOrdinal("UnidadCompraAbreviaturaSnapshot")),
                reader.GetDecimal(reader.GetOrdinal("FactorConversionSnapshot")),
                reader.GetDecimal(reader.GetOrdinal("CantidadBaseOrdenada")),
                reader.GetDecimal(reader.GetOrdinal("CantidadBaseRecibidaAcumulada")),
                reader.GetDecimal(reader.GetOrdinal("CantidadBasePendiente")),
                reader.GetBoolean(reader.GetOrdinal("CausaInventario")),
                reader.GetBoolean(reader.GetOrdinal("UsaNumeroSerie")));
        }

        private static async Task<string> NextFolioAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, CancellationToken cancellationToken)
        {
            using (SqlCommand ensure = new(@"
IF NOT EXISTS (SELECT 1 FROM dbo.RecepcionFolios WITH (UPDLOCK, HOLDLOCK) WHERE idEmpresa = @IdEmpresa)
    INSERT INTO dbo.RecepcionFolios (id, idEmpresa, identityKey, UltimoConsecutivo, FechaCreacion, FechaActualizacion)
    VALUES (NEWID(), @IdEmpresa, NEWID(), 0, SYSUTCDATETIME(), SYSUTCDATETIME());", connection, transaction))
            {
                ensure.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                await ensure.ExecuteNonQueryAsync(cancellationToken);
            }

            using SqlCommand update = new(@"
UPDATE dbo.RecepcionFolios
SET UltimoConsecutivo = UltimoConsecutivo + 1,
    FechaActualizacion = SYSUTCDATETIME()
OUTPUT inserted.UltimoConsecutivo
WHERE idEmpresa = @IdEmpresa", connection, transaction);
            update.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            long next = Convert.ToInt64(await update.ExecuteScalarAsync(cancellationToken));
            return $"REC-{next:000000}";
        }

        private static async Task InsertRecepcionAsync(SqlConnection connection, SqlTransaction transaction, Guid id, RecepcionConfirmarRequest request, string folio, DateTime fecha, Guid? idUsuario, CancellationToken cancellationToken)
        {
            using SqlCommand command = new(@"
INSERT INTO dbo.Recepciones
    (id, idEmpresa, identityKey, idOrdenCompra, FolioRecepcion, idSucursal, FechaRecepcion, Estado, OperationKey, Observaciones, idUsuarioCreacion, idUsuarioConfirmacion, FechaCreacion, FechaActualizacion)
VALUES
    (@Id, @IdEmpresa, NEWID(), @IdOrdenCompra, @Folio, @IdSucursal, @Fecha, 2, @OperationKey, @Observaciones, @IdUsuario, @IdUsuario, SYSUTCDATETIME(), SYSUTCDATETIME())", connection, transaction);
            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@IdEmpresa", request.IdEmpresa);
            command.Parameters.AddWithValue("@IdOrdenCompra", request.IdOrdenCompra);
            command.Parameters.AddWithValue("@Folio", folio);
            command.Parameters.AddWithValue("@IdSucursal", request.IdSucursal);
            command.Parameters.AddWithValue("@Fecha", fecha);
            command.Parameters.AddWithValue("@OperationKey", request.OperationKey.Trim());
            command.Parameters.AddWithValue("@Observaciones", string.IsNullOrWhiteSpace(request.Observaciones) ? DBNull.Value : Truncate(request.Observaciones, ObservacionesMaxLength));
            command.Parameters.AddWithValue("@IdUsuario", (object?)idUsuario ?? DBNull.Value);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task InsertPartidaAsync(SqlConnection connection, SqlTransaction transaction, Guid id, Guid recepcionId, PartidaSnapshot partida, decimal cantidadCompra, decimal cantidadBase, decimal acumulado, decimal pendiente, byte estado, bool controlSerie, Guid? movimientoId, CancellationToken cancellationToken)
        {
            using SqlCommand command = new(@"
INSERT INTO dbo.RecepcionPartidas
    (id, idEmpresa, identityKey, idRecepcion, idOrdenCompra, idOrdenCompraDetalle, NumeroPartida, TipoPartida, idProductoServicio, idVariante, idPresentacionCompra, PresentacionCompraSnapshot, UnidadCompraSnapshot, UnidadCompraAbreviaturaSnapshot, FactorConversionSnapshot, CantidadCompraRecibida, CantidadBaseOrdenada, CantidadBaseRecibidaAnterior, CantidadBaseEstaRecepcion, CantidadBaseRecibidaAcumulada, CantidadBasePendiente, EstadoPartidaRecepcion, ControlSerie, idInventarioMovimiento, FechaCreacion)
VALUES
    (@Id, @IdEmpresa, NEWID(), @IdRecepcion, @IdOrdenCompra, @IdOrdenCompraDetalle, @NumeroPartida, @TipoPartida, @IdProductoServicio, @IdVariante, @IdPresentacionCompra, @PresentacionCompra, @UnidadCompra, @UnidadCompraAbreviatura, @Factor, @CantidadCompra, @Ordenada, @Anterior, @Esta, @Acumulada, @Pendiente, @Estado, @ControlSerie, @MovimientoId, SYSUTCDATETIME())", connection, transaction);
            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@IdEmpresa", partida.IdEmpresa);
            command.Parameters.AddWithValue("@IdRecepcion", recepcionId);
            command.Parameters.AddWithValue("@IdOrdenCompra", partida.IdOrdenCompra);
            command.Parameters.AddWithValue("@IdOrdenCompraDetalle", partida.Id);
            command.Parameters.AddWithValue("@NumeroPartida", partida.NumeroPartida);
            command.Parameters.AddWithValue("@TipoPartida", partida.TipoPartida);
            command.Parameters.AddWithValue("@IdProductoServicio", partida.IdProductoServicio);
            command.Parameters.AddWithValue("@IdVariante", (object?)partida.IdVariante ?? DBNull.Value);
            command.Parameters.AddWithValue("@IdPresentacionCompra", (object?)partida.IdPresentacionCompra ?? DBNull.Value);
            command.Parameters.AddWithValue("@PresentacionCompra", string.IsNullOrWhiteSpace(partida.PresentacionCompraSnapshot) ? DBNull.Value : partida.PresentacionCompraSnapshot);
            command.Parameters.AddWithValue("@UnidadCompra", string.IsNullOrWhiteSpace(partida.UnidadCompraSnapshot) ? DBNull.Value : partida.UnidadCompraSnapshot);
            command.Parameters.AddWithValue("@UnidadCompraAbreviatura", string.IsNullOrWhiteSpace(partida.UnidadCompraAbreviaturaSnapshot) ? DBNull.Value : partida.UnidadCompraAbreviaturaSnapshot);
            command.Parameters.AddWithValue("@Factor", partida.FactorConversionSnapshot);
            command.Parameters.AddWithValue("@CantidadCompra", cantidadCompra);
            command.Parameters.AddWithValue("@Ordenada", partida.CantidadBaseOrdenada);
            command.Parameters.AddWithValue("@Anterior", partida.CantidadBaseRecibidaAcumulada);
            command.Parameters.AddWithValue("@Esta", cantidadBase);
            command.Parameters.AddWithValue("@Acumulada", acumulado);
            command.Parameters.AddWithValue("@Pendiente", pendiente);
            command.Parameters.AddWithValue("@Estado", estado);
            command.Parameters.AddWithValue("@ControlSerie", controlSerie);
            command.Parameters.AddWithValue("@MovimientoId", (object?)movimientoId ?? DBNull.Value);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task<Guid> InsertInventarioSerieAsync(SqlConnection connection, SqlTransaction transaction, RecepcionConfirmarRequest request, PartidaSnapshot partida, string numeroSerie, Guid? movimientoId, CancellationToken cancellationToken)
        {
            Guid id = Guid.NewGuid();
            using SqlCommand command = new(@"
INSERT INTO dbo.InventarioSeries
    (id, idEmpresa, identityKey, idSucursalActual, idProductoServicio, idVariante, NumeroSerie, Estado, OrigenMovimientoId, FechaCreacion, FechaActualizacion)
VALUES
    (@Id, @IdEmpresa, NEWID(), @IdSucursal, @IdProductoServicio, @IdVariante, @NumeroSerie, 1, @MovimientoId, SYSUTCDATETIME(), SYSUTCDATETIME())", connection, transaction);
            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@IdEmpresa", request.IdEmpresa);
            command.Parameters.AddWithValue("@IdSucursal", request.IdSucursal);
            command.Parameters.AddWithValue("@IdProductoServicio", partida.IdProductoServicio);
            command.Parameters.AddWithValue("@IdVariante", (object?)partida.IdVariante ?? DBNull.Value);
            command.Parameters.AddWithValue("@NumeroSerie", numeroSerie);
            command.Parameters.AddWithValue("@MovimientoId", (object?)movimientoId ?? DBNull.Value);
            await command.ExecuteNonQueryAsync(cancellationToken);
            return id;
        }

        private static async Task InsertRecepcionSerieAsync(SqlConnection connection, SqlTransaction transaction, Guid recepcionId, Guid partidaId, PartidaSnapshot partida, string numeroSerie, Guid inventarioSerieId, CancellationToken cancellationToken)
        {
            using SqlCommand command = new(@"
INSERT INTO dbo.RecepcionSeries
    (id, idEmpresa, identityKey, idRecepcion, idRecepcionPartida, idProductoServicio, idVariante, NumeroSerie, idInventarioSerie, FechaCreacion)
VALUES
    (NEWID(), @IdEmpresa, NEWID(), @IdRecepcion, @IdPartida, @IdProductoServicio, @IdVariante, @NumeroSerie, @IdInventarioSerie, SYSUTCDATETIME())", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", partida.IdEmpresa);
            command.Parameters.AddWithValue("@IdRecepcion", recepcionId);
            command.Parameters.AddWithValue("@IdPartida", partidaId);
            command.Parameters.AddWithValue("@IdProductoServicio", partida.IdProductoServicio);
            command.Parameters.AddWithValue("@IdVariante", (object?)partida.IdVariante ?? DBNull.Value);
            command.Parameters.AddWithValue("@NumeroSerie", numeroSerie);
            command.Parameters.AddWithValue("@IdInventarioSerie", inventarioSerieId);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task RecalculateOrdenStateAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idOrdenCompra, CancellationToken cancellationToken)
        {
            using SqlCommand summary = new(@"
SELECT
    SUM(CASE WHEN CantidadBasePendiente = 0 THEN 1 ELSE 0 END) AS Completas,
    SUM(CASE WHEN CantidadBaseRecibidaAcumulada > 0 THEN 1 ELSE 0 END) AS ConRecepcion,
    COUNT(1) AS Total
FROM dbo.OrdenesCompraDetalle WITH (UPDLOCK, HOLDLOCK)
WHERE idEmpresa = @IdEmpresa AND idOrdenCompra = @IdOrdenCompra AND Activo = 1 AND FechaArchivado IS NULL", connection, transaction);
            summary.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            summary.Parameters.AddWithValue("@IdOrdenCompra", idOrdenCompra);
            using SqlDataReader reader = await summary.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new InvalidOperationException("RECEPCION_OC_SIN_PARTIDAS");
            }

            int completas = reader.IsDBNull(reader.GetOrdinal("Completas")) ? 0 : Convert.ToInt32(reader["Completas"]);
            int conRecepcion = reader.IsDBNull(reader.GetOrdinal("ConRecepcion")) ? 0 : Convert.ToInt32(reader["ConRecepcion"]);
            int total = Convert.ToInt32(reader["Total"]);
            await reader.CloseAsync();

            byte estado = completas == total ? OcEstadoRecibida : conRecepcion > 0 ? OcEstadoParcialmenteRecibida : OcEstadoGenerada;
            using SqlCommand update = new("UPDATE dbo.OrdenesCompra SET Estado = @Estado, FechaActualizacion = SYSUTCDATETIME() WHERE idEmpresa = @IdEmpresa AND id = @IdOrdenCompra", connection, transaction);
            update.Parameters.AddWithValue("@Estado", estado);
            update.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            update.Parameters.AddWithValue("@IdOrdenCompra", idOrdenCompra);
            await update.ExecuteNonQueryAsync(cancellationToken);
        }

        private static string[] NormalizeSeries(IEnumerable<string> series)
            => series
                .Select(item => (item ?? string.Empty).Trim())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

        private static void ValidateNoDuplicateSeries(IEnumerable<string> series)
        {
            string[] normalized = series
                .Select(item => (item ?? string.Empty).Trim())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToArray();

            if (normalized.Length != normalized.Distinct(StringComparer.OrdinalIgnoreCase).Count())
            {
                throw new InvalidOperationException("RECEPCION_SERIES_DUPLICADAS");
            }
        }

        private static string BuildPartidaOperationKey(string operationKey, Guid partidaId)
        {
            string key = $"REC:{operationKey.Trim()}:{partidaId:N}";
            return key.Length <= OperationKeyMaxLength ? key : key[..OperationKeyMaxLength];
        }

        private static string Truncate(string value, int maxLength)
        {
            string clean = value.Trim();
            return clean.Length <= maxLength ? clean : clean[..maxLength];
        }

        private sealed record ExistingRecepcion(Guid Id, string Folio);
        private sealed record OrdenSnapshot(Guid Id, byte Estado);

        private sealed record PartidaSnapshot(
            Guid Id,
            Guid IdEmpresa,
            Guid IdOrdenCompra,
            int NumeroPartida,
            byte TipoPartida,
            Guid IdProductoServicio,
            Guid? IdVariante,
            Guid? IdPresentacionCompra,
            string PresentacionCompraSnapshot,
            string UnidadCompraSnapshot,
            string UnidadCompraAbreviaturaSnapshot,
            decimal FactorConversionSnapshot,
            decimal CantidadBaseOrdenada,
            decimal CantidadBaseRecibidaAcumulada,
            decimal CantidadBasePendiente,
            bool CausaInventario,
            bool UsaNumeroSerie);
    }
}
