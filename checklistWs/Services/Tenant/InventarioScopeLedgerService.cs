using System.Data;
using System.Data.SqlClient;

namespace checklistWs.Services.Tenant
{
    public sealed class InventarioScopeLedgerService : IInventarioScopeLedgerService
    {
        private const int OriginTypeMaxLength = 40;
        private const int OperationKeyMaxLength = 120;
        private const int ObservationsMaxLength = 500;

        public async Task<InventarioMovimientoResult> ApplyMovementAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            InventarioMovimientoRequest request,
            CancellationToken cancellationToken = default)
        {
            Validate(request);

            InventarioMovimientoResult? existing = await FindExistingMovementAsync(connection, transaction, request, cancellationToken);
            if (existing != null)
            {
                return existing;
            }

            await ValidateCatalogsAsync(connection, transaction, request, cancellationToken);
            await AcquireGranularityLockAsync(connection, transaction, request, cancellationToken);

            existing = await FindExistingMovementAsync(connection, transaction, request, cancellationToken);
            if (existing != null)
            {
                return existing;
            }

            (Guid saldoId, decimal saldoAnterior) = await EnsureSaldoAsync(connection, transaction, request, cancellationToken);
            decimal signedQuantity = IsPositive(request.TipoMovimiento) ? request.CantidadBase : -request.CantidadBase;
            decimal saldoPosterior = saldoAnterior + signedQuantity;
            if (saldoPosterior < 0)
            {
                throw new InvalidOperationException("INVENTARIO_SALDO_INSUFICIENTE");
            }

            Guid movimientoId = Guid.NewGuid();
            DateTime fechaMovimiento = request.FechaMovimiento ?? DateTime.UtcNow;

            using (SqlCommand insert = new SqlCommand(@"
INSERT INTO dbo.InventarioMovimientos
    (id, idEmpresa, identityKey, idSucursal, idProductoServicio, idVariante, TipoMovimiento, CantidadBase, SaldoAnterior, SaldoPosterior, OrigenTipo, OrigenId, OrigenPartidaId, OperationKey, Observaciones, idUsuario, FechaMovimiento, FechaCreacion)
VALUES
    (@Id, @IdEmpresa, NEWID(), @IdSucursal, @IdProductoServicio, @IdVariante, @TipoMovimiento, @CantidadBase, @SaldoAnterior, @SaldoPosterior, @OrigenTipo, @OrigenId, @OrigenPartidaId, @OperationKey, @Observaciones, @IdUsuario, @FechaMovimiento, SYSUTCDATETIME())", connection, transaction))
            {
                AddMovementParameters(insert, movimientoId, request, saldoAnterior, saldoPosterior, fechaMovimiento);
                await insert.ExecuteNonQueryAsync(cancellationToken);
            }

            using (SqlCommand update = new SqlCommand(@"
UPDATE dbo.InventarioSaldos
SET CantidadBaseActual = @SaldoPosterior,
    FechaActualizacion = SYSUTCDATETIME()
WHERE idEmpresa = @IdEmpresa AND id = @SaldoId", connection, transaction))
            {
                update.Parameters.AddWithValue("@SaldoPosterior", saldoPosterior);
                update.Parameters.AddWithValue("@IdEmpresa", request.IdEmpresa);
                update.Parameters.AddWithValue("@SaldoId", saldoId);
                await update.ExecuteNonQueryAsync(cancellationToken);
            }

            return new InventarioMovimientoResult
            {
                MovimientoId = movimientoId,
                SaldoId = saldoId,
                AlreadyApplied = false,
                SaldoAnterior = saldoAnterior,
                SaldoPosterior = saldoPosterior
            };
        }

        private static void Validate(InventarioMovimientoRequest request)
        {
            if (request.IdEmpresa == Guid.Empty ||
                request.IdSucursal == Guid.Empty ||
                request.IdProductoServicio == Guid.Empty)
            {
                throw new ArgumentException("INVENTARIO_REQUEST_INVALID");
            }

            if (request.CantidadBase <= 0)
            {
                throw new ArgumentException("INVENTARIO_CANTIDAD_BASE_INVALIDA");
            }

            if (!Enum.IsDefined(typeof(InventarioTipoMovimiento), request.TipoMovimiento))
            {
                throw new ArgumentException("INVENTARIO_TIPO_MOVIMIENTO_INVALIDO");
            }

            if (string.IsNullOrWhiteSpace(request.OrigenTipo) ||
                string.IsNullOrWhiteSpace(request.OperationKey))
            {
                throw new ArgumentException("INVENTARIO_ORIGEN_OPERATIONKEY_REQUERIDOS");
            }
        }

        private static async Task ValidateCatalogsAsync(SqlConnection connection, SqlTransaction transaction, InventarioMovimientoRequest request, CancellationToken cancellationToken)
        {
            using (SqlCommand command = new SqlCommand(@"
SELECT
    CASE WHEN EXISTS (SELECT 1 FROM dbo.Sucursales WHERE idEmpresa = @IdEmpresa AND id = @IdSucursal) THEN 1 ELSE 0 END AS SucursalOk,
    ps.Tipo,
    ps.CausaInventario,
    CASE WHEN @IdVariante IS NULL THEN 1
         WHEN EXISTS (SELECT 1 FROM dbo.ProductosServiciosVariantes WHERE idEmpresa = @IdEmpresa AND id = @IdVariante AND idProductoServicio = @IdProductoServicio) THEN 1
         ELSE 0 END AS VarianteOk
FROM dbo.ProductosServicios ps
WHERE ps.idEmpresa = @IdEmpresa AND ps.id = @IdProductoServicio AND ps.Activo = 1", connection, transaction))
            {
                command.Parameters.AddWithValue("@IdEmpresa", request.IdEmpresa);
                command.Parameters.AddWithValue("@IdSucursal", request.IdSucursal);
                command.Parameters.AddWithValue("@IdProductoServicio", request.IdProductoServicio);
                command.Parameters.AddWithValue("@IdVariante", (object?)request.IdVariante ?? DBNull.Value);

                using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                {
                    throw new InvalidOperationException("INVENTARIO_PRODUCTO_NO_DISPONIBLE");
                }

                bool sucursalOk = reader.GetInt32(reader.GetOrdinal("SucursalOk")) == 1;
                byte tipo = reader.GetByte(reader.GetOrdinal("Tipo"));
                bool causaInventario = reader.GetBoolean(reader.GetOrdinal("CausaInventario"));
                bool varianteOk = reader.GetInt32(reader.GetOrdinal("VarianteOk")) == 1;
                if (!sucursalOk) throw new InvalidOperationException("INVENTARIO_SUCURSAL_NO_DISPONIBLE");
                if (tipo != 1 || !causaInventario) throw new InvalidOperationException("INVENTARIO_PRODUCTO_NO_INVENTARIABLE");
                if (!varianteOk) throw new InvalidOperationException("INVENTARIO_VARIANTE_INVALIDA");
            }
        }

        private static async Task AcquireGranularityLockAsync(SqlConnection connection, SqlTransaction transaction, InventarioMovimientoRequest request, CancellationToken cancellationToken)
        {
            string variantSegment = request.IdVariante?.ToString("N") ?? "BASE";
            string resource = $"Inventario:{request.IdEmpresa:N}:{request.IdSucursal:N}:{request.IdProductoServicio:N}:{variantSegment}";
            using SqlCommand command = new SqlCommand("EXEC @Result = sp_getapplock @Resource = @Resource, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 15000;", connection, transaction);
            SqlParameter result = command.Parameters.Add("@Result", SqlDbType.Int);
            result.Direction = ParameterDirection.Output;
            command.Parameters.AddWithValue("@Resource", resource);
            await command.ExecuteNonQueryAsync(cancellationToken);
            int lockResult = result.Value is int value ? value : -999;
            if (lockResult < 0)
            {
                throw new TimeoutException("INVENTARIO_LOCK_TIMEOUT");
            }
        }

        private static async Task<(Guid SaldoId, decimal Cantidad)> EnsureSaldoAsync(SqlConnection connection, SqlTransaction transaction, InventarioMovimientoRequest request, CancellationToken cancellationToken)
        {
            using (SqlCommand select = new SqlCommand(@"
SELECT id, CantidadBaseActual
FROM dbo.InventarioSaldos WITH (UPDLOCK, HOLDLOCK)
WHERE idEmpresa = @IdEmpresa
  AND idSucursal = @IdSucursal
  AND idProductoServicio = @IdProductoServicio
  AND ((idVariante = @IdVariante) OR (idVariante IS NULL AND @IdVariante IS NULL))", connection, transaction))
            {
                AddGranularityParameters(select, request);
                using SqlDataReader reader = await select.ExecuteReaderAsync(cancellationToken);
                if (await reader.ReadAsync(cancellationToken))
                {
                    return (reader.GetGuid(reader.GetOrdinal("id")), reader.GetDecimal(reader.GetOrdinal("CantidadBaseActual")));
                }
            }

            Guid saldoId = Guid.NewGuid();
            using (SqlCommand insert = new SqlCommand(@"
INSERT INTO dbo.InventarioSaldos
    (id, idEmpresa, identityKey, idSucursal, idProductoServicio, idVariante, CantidadBaseActual, FechaCreacion, FechaActualizacion)
VALUES
    (@Id, @IdEmpresa, NEWID(), @IdSucursal, @IdProductoServicio, @IdVariante, 0, SYSUTCDATETIME(), SYSUTCDATETIME())", connection, transaction))
            {
                insert.Parameters.AddWithValue("@Id", saldoId);
                AddGranularityParameters(insert, request);
                await insert.ExecuteNonQueryAsync(cancellationToken);
            }

            return (saldoId, 0m);
        }

        private static async Task<InventarioMovimientoResult?> FindExistingMovementAsync(SqlConnection connection, SqlTransaction transaction, InventarioMovimientoRequest request, CancellationToken cancellationToken)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT TOP (1)
    m.id,
    s.id AS SaldoId,
    m.SaldoAnterior,
    m.SaldoPosterior
FROM dbo.InventarioMovimientos m
LEFT JOIN dbo.InventarioSaldos s
    ON s.idEmpresa = m.idEmpresa
   AND s.idSucursal = m.idSucursal
   AND s.idProductoServicio = m.idProductoServicio
   AND ((s.idVariante = m.idVariante) OR (s.idVariante IS NULL AND m.idVariante IS NULL))
WHERE m.idEmpresa = @IdEmpresa AND m.OperationKey = @OperationKey
ORDER BY m.FechaMovimiento DESC, m.id DESC", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", request.IdEmpresa);
            command.Parameters.AddWithValue("@OperationKey", Truncate(request.OperationKey, OperationKeyMaxLength));

            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return new InventarioMovimientoResult
            {
                MovimientoId = reader.GetGuid(reader.GetOrdinal("id")),
                SaldoId = reader.IsDBNull(reader.GetOrdinal("SaldoId")) ? Guid.Empty : reader.GetGuid(reader.GetOrdinal("SaldoId")),
                AlreadyApplied = true,
                SaldoAnterior = reader.GetDecimal(reader.GetOrdinal("SaldoAnterior")),
                SaldoPosterior = reader.GetDecimal(reader.GetOrdinal("SaldoPosterior"))
            };
        }

        private static void AddMovementParameters(SqlCommand command, Guid movimientoId, InventarioMovimientoRequest request, decimal saldoAnterior, decimal saldoPosterior, DateTime fechaMovimiento)
        {
            command.Parameters.AddWithValue("@Id", movimientoId);
            AddGranularityParameters(command, request);
            command.Parameters.AddWithValue("@TipoMovimiento", (byte)request.TipoMovimiento);
            command.Parameters.AddWithValue("@CantidadBase", request.CantidadBase);
            command.Parameters.AddWithValue("@SaldoAnterior", saldoAnterior);
            command.Parameters.AddWithValue("@SaldoPosterior", saldoPosterior);
            command.Parameters.AddWithValue("@OrigenTipo", Truncate(request.OrigenTipo, OriginTypeMaxLength));
            command.Parameters.AddWithValue("@OrigenId", (object?)request.OrigenId ?? DBNull.Value);
            command.Parameters.AddWithValue("@OrigenPartidaId", (object?)request.OrigenPartidaId ?? DBNull.Value);
            command.Parameters.AddWithValue("@OperationKey", Truncate(request.OperationKey, OperationKeyMaxLength));
            command.Parameters.AddWithValue("@Observaciones", string.IsNullOrWhiteSpace(request.Observaciones) ? DBNull.Value : Truncate(request.Observaciones!, ObservationsMaxLength));
            command.Parameters.AddWithValue("@IdUsuario", (object?)request.IdUsuario ?? DBNull.Value);
            command.Parameters.AddWithValue("@FechaMovimiento", fechaMovimiento);
        }

        private static void AddGranularityParameters(SqlCommand command, InventarioMovimientoRequest request)
        {
            command.Parameters.AddWithValue("@IdEmpresa", request.IdEmpresa);
            command.Parameters.AddWithValue("@IdSucursal", request.IdSucursal);
            command.Parameters.AddWithValue("@IdProductoServicio", request.IdProductoServicio);
            command.Parameters.AddWithValue("@IdVariante", (object?)request.IdVariante ?? DBNull.Value);
        }

        private static bool IsPositive(InventarioTipoMovimiento movement)
            => movement is InventarioTipoMovimiento.Entrada or InventarioTipoMovimiento.AjustePositivo;

        private static string Truncate(string value, int maxLength)
        {
            string clean = value.Trim();
            return clean.Length <= maxLength ? clean : clean[..maxLength];
        }
    }
}
