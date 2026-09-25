using System.Data.SqlClient;

namespace checklistWs.Services.Tenant
{
    public enum ProductosServiciosPermissionRequirement
    {
        Read,
        Write
    }

    public sealed class ProductosServiciosAuthorizationRequest
    {
        public Guid IdEmpresa { get; init; }
        public string UserId { get; init; } = string.Empty;
        public string PermissionCode { get; init; } = ProductosServiciosAuthorizationDefaults.PermissionCode;
        public ProductosServiciosPermissionRequirement Requirement { get; init; }
        public TenantDatabaseDescriptor TenantDatabase { get; init; } = null!;
    }

    public sealed class ProductosServiciosAuthorizationDecision
    {
        public bool HasAccess { get; init; }
        public bool CanWrite { get; init; }
        public string PermissionCode { get; init; } = ProductosServiciosAuthorizationDefaults.PermissionCode;
        public string ReasonCode { get; init; } = string.Empty;
        public string ReferenceId { get; init; } = Guid.NewGuid().ToString("N");
        public bool IsAllowed(ProductosServiciosPermissionRequirement requirement)
            => HasAccess && (requirement == ProductosServiciosPermissionRequirement.Read || CanWrite);
    }

    public interface IProductosServiciosAuthorizationService
    {
        Task<ProductosServiciosAuthorizationDecision> AuthorizeAsync(
            ProductosServiciosAuthorizationRequest request,
            CancellationToken cancellationToken = default);
    }

    public static class ProductosServiciosAuthorizationDefaults
    {
        public const string PermissionCode = "05001000";
        public const string ModulePermissionCode = "05001000";
        public const string AbcPermissionCode = "05001001";
        public const string CatalogosPermissionCode = "05001002";
        public const string CategoriasPermissionCode = "05001003";
        public const string MarcasPermissionCode = "05001004";
        public const string UnidadesMedidaPermissionCode = "05001005";
        public const string ColeccionesPermissionCode = "05001006";
        public const string EtiquetasPermissionCode = "05001007";
        public const string PermissionCodeConfigurationKey = "ProductosServicios:PermissionCode";
        public const string AjustesPermissionCode = "04000000";
        public const string SucursalesPermissionCode = "04003000";
        public const string SucursalesAbcPermissionCode = "04003100";
        public const string RazonesSocialesPermissionCode = "04004000";
        public const string RegionesPermissionCode = "04005000";
        public const string ActivosCatalogosPermissionCode = "03506000";
        public const string ActivosProveedoresPermissionCode = "03506003";
        public const string OrdenesCompraPermissionCode = "05003000";
        public const string OrdenesCompraNuevaPermissionCode = "05003001";
        public const string OrdenesCompraReportePermissionCode = "05003002";
        public const string RecepcionPermissionCode = "05004000";
        public const string RecepcionNuevaPermissionCode = "05004001";
        public const string RecepcionReportePermissionCode = "05004002";
        public const string CurvasPermissionCode = "05005000";
        public const string CurvasCatalogoPermissionCode = "05005001";
    }
}
