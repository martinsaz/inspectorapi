using checklistWs.Models.Recepcion;

namespace checklistWs.Services.Tenant
{
    public interface IRecepcionScopeService
    {
        Task<RecepcionOperacionResponse> ConfirmarAsync(
            TenantDatabaseDescriptor descriptor,
            RecepcionConfirmarRequest request,
            Guid? idUsuario,
            CancellationToken cancellationToken = default);
    }
}
