using System.Data.SqlClient;

namespace checklistWs.Services.Tenant
{
    public interface ICurvasSugerenciasMotorService
    {
        Task<CurvasSugerenciaPreviewResponse> PreviewAsync(
            TenantDatabaseDescriptor descriptor,
            Guid idEmpresa,
            IReadOnlyList<CurvasSugerenciaItemRequest> items,
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

                CurvaSiembraSnapshot? siembra = await LoadSiembraAsync(connection, idEmpresa, item, cancellationToken);
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
SELECT s.id AS IdSiembra, s.idCurva, d.CantidadBaseObjetivo
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
                    reader.GetDecimal(reader.GetOrdinal("CantidadBaseObjetivo")));
            }

            return result;
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
  AND oc.idSucursal = @IdSucursal
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

    public sealed record CurvaSiembraSnapshot(Guid IdCurva, Guid IdSiembra, decimal CurvaObjetivoBase);
    public sealed record PresentacionCompraSnapshot(Guid Id, decimal FactorConversionBase, bool PermiteCantidadBase);
    public sealed record PresentationConversion(decimal CantidadCompraSugerida, decimal CantidadBaseConvertida, decimal ExcedenteRedondeoBase);
}
