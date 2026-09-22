using System.Data.SqlClient;

namespace checklistWs.Services.Tenant
{
    public enum InventarioTipoMovimiento : byte
    {
        Entrada = 1,
        Salida = 2,
        AjustePositivo = 3,
        AjusteNegativo = 4,
        Reserva = 5
    }

    public sealed class InventarioMovimientoRequest
    {
        public Guid IdEmpresa { get; init; }
        public Guid IdSucursal { get; init; }
        public Guid IdProductoServicio { get; init; }
        public Guid? IdVariante { get; init; }
        public InventarioTipoMovimiento TipoMovimiento { get; init; }
        public decimal CantidadBase { get; init; }
        public string OrigenTipo { get; init; } = string.Empty;
        public Guid? OrigenId { get; init; }
        public Guid? OrigenPartidaId { get; init; }
        public string OperationKey { get; init; } = string.Empty;
        public string? Observaciones { get; init; }
        public Guid? IdUsuario { get; init; }
        public DateTime? FechaMovimiento { get; init; }
    }

    public sealed class InventarioMovimientoResult
    {
        public Guid MovimientoId { get; init; }
        public Guid SaldoId { get; init; }
        public bool AlreadyApplied { get; init; }
        public decimal SaldoAnterior { get; init; }
        public decimal SaldoPosterior { get; init; }
    }

    public interface IInventarioScopeLedgerService
    {
        Task<InventarioMovimientoResult> ApplyMovementAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            InventarioMovimientoRequest request,
            CancellationToken cancellationToken = default);
    }
}
