using System.Text.Json.Serialization;

namespace checklistWs.Models.Kits;

public static class KitsConstants
{
    public const byte TipoProducto = 1;
    public const byte TipoServicio = 2;
    public const byte TipoMixto = 3;
    public const byte IdentidadProducto = 1;
    public const byte IdentidadServicio = 2;
    public const byte IdentidadVariante = 3;
    public const byte IdentidadPresentacionVenta = 4;
}

// API request contracts. Tenant and audit fields are deliberately absent.
public sealed class KitCreateRequest
{
    public string OperationKey { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public byte Tipo { get; set; }
    public decimal PrecioKit { get; set; }
    public bool Activo { get; set; } = true;
    public string? ImagenTemporalToken { get; set; }
    public string? ImagenUrl { get; set; }
    public string? ImagenNombre { get; set; }
    public List<KitComponentCreateRequest> Componentes { get; set; } = new();

    [JsonIgnore]
    public Guid? PreparedId { get; set; }
}

public sealed class KitImageTemporaryResponse
{
    public string Message { get; set; } = string.Empty;
    public string TemporalToken { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

public sealed class KitImageCleanupRequest
{
    public string? TemporalToken { get; set; }
}

public sealed class KitUpdateRequest
{
    public string Nombre { get; set; } = string.Empty;
    public byte Tipo { get; set; }
    public decimal PrecioKit { get; set; }
    public bool Activo { get; set; } = true;
    public string? ImagenTemporalToken { get; set; }
    public string? ImagenUrl { get; set; }
    public string? ImagenNombre { get; set; }
    public List<KitComponentEditRequest> Componentes { get; set; } = new();
}

public sealed class KitComponentEditRequest
{
    public Guid? Id { get; set; }
    public string OperationKey { get; set; } = string.Empty;
    public byte TipoIdentidad { get; set; }
    public Guid IdProductoServicio { get; set; }
    public Guid? IdVariante { get; set; }
    public Guid? IdPresentacionVenta { get; set; }
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
}

public sealed class KitComponentCreateRequest
{
    public string OperationKey { get; set; } = string.Empty;
    public byte TipoIdentidad { get; set; }
    public Guid IdProductoServicio { get; set; }
    public Guid? IdVariante { get; set; }
    public Guid? IdPresentacionVenta { get; set; }
    public decimal Cantidad { get; set; } = 1m;
    public decimal? PrecioUnitario { get; set; }
}

public sealed class KitComponentUpdateRequest
{
    public byte TipoIdentidad { get; set; }
    public Guid IdProductoServicio { get; set; }
    public Guid? IdVariante { get; set; }
    public Guid? IdPresentacionVenta { get; set; }
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
}

public sealed class KitPriceResolutionRequest
{
    public byte TipoIdentidad { get; set; }
    public Guid IdProductoServicio { get; set; }
    public Guid? IdVariante { get; set; }
    public Guid? IdPresentacionVenta { get; set; }
}

public sealed class KitListQuery
{
    public string Busqueda { get; set; } = string.Empty;
    public byte? Tipo { get; set; }
    public bool? Activo { get; set; }
}

// API response contracts.
public sealed class KitOperationResponse
{
    public string Code { get; set; } = "OK";
    public string Message { get; set; } = string.Empty;
    public Guid? Id { get; set; }
    public Guid CorrelationId { get; set; }
    public bool Replayed { get; set; }
    public object? Details { get; set; }
}

public class KitListItemResponse
{
    public Guid Id { get; set; }
    public Guid IdentityKey { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public byte Tipo { get; set; }
    public string TipoNombre { get; set; } = string.Empty;
    public decimal PrecioKit { get; set; }
    public string? ImagenUrl { get; set; }
    public string? ImagenNombre { get; set; }
    public bool Activo { get; set; }
    public int NumeroComponentes { get; set; }
    public decimal ValorComponentes { get; set; }
    public decimal Diferencia { get; set; }
    public DateTime FechaActualizacion { get; set; }
    public bool EsUtilizable { get; set; }
    public IReadOnlyList<string> MotivosNoUtilizable { get; set; } = Array.Empty<string>();
}

public sealed class KitDetailResponse : KitListItemResponse
{
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaArchivado { get; set; }
    public IReadOnlyList<KitComponentResponse> Componentes { get; set; } = Array.Empty<KitComponentResponse>();
}

public sealed class KitComponentResponse
{
    public Guid Id { get; set; }
    public Guid IdentityKey { get; set; }
    public byte TipoIdentidad { get; set; }
    public string TipoIdentidadNombre { get; set; } = string.Empty;
    public Guid IdProductoServicio { get; set; }
    public Guid? IdVariante { get; set; }
    public Guid? IdPresentacionVenta { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string IdentidadVisible { get; set; } = string.Empty;
    public bool UnidadPermiteDecimales { get; set; }
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Importe { get; set; }
    public bool FuenteActiva { get; set; }
    public string? MotivoFuenteInactiva { get; set; }
    public bool Activo { get; set; }
    public DateTime? FechaArchivado { get; set; }
}

public sealed class KitOptionResponse
{
    public byte TipoIdentidad { get; set; }
    public Guid IdProductoServicio { get; set; }
    public Guid? IdVariante { get; set; }
    public Guid? IdPresentacionVenta { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string IdentidadVisible { get; set; } = string.Empty;
    public bool UnidadPermiteDecimales { get; set; }
    public bool FuenteActiva { get; set; }
}

public sealed class KitPriceResolutionResponse
{
    public bool Resuelto { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public decimal? PrecioPropuesto { get; set; }
    public string Origen { get; set; } = string.Empty;
    public int Nivel { get; set; } = 1;
    public Guid CorrelationId { get; set; }
}

public sealed class KitHistoryResponse
{
    public Guid Id { get; set; }
    public Guid IdKit { get; set; }
    public Guid? IdKitComponente { get; set; }
    public byte TipoEntidad { get; set; }
    public string Operacion { get; set; } = string.Empty;
    public string? DatosAntes { get; set; }
    public string? DatosDespues { get; set; }
    public Guid? IdUsuario { get; set; }
    public Guid CorrelationId { get; set; }
    public DateTime FechaUtc { get; set; }
}

// Service/domain models are separate from transport requests and SQL rows.
public sealed record KitActor(Guid IdEmpresa, Guid? IdUsuario);
public sealed record KitIdentity(byte TipoIdentidad, Guid IdProductoServicio, Guid? IdVariante, Guid? IdPresentacionVenta);
public sealed record KitSource(
    KitIdentity Identity,
    byte TipoProductoServicio,
    string Codigo,
    string Nombre,
    string IdentidadVisible,
    bool UnidadPermiteDecimales,
    bool ProductoActivo,
    bool IdentidadActiva)
{
    public bool FuenteActiva => ProductoActivo && IdentidadActiva;
    public string? MotivoInactivo => FuenteActiva ? null : !ProductoActivo ? "PRODUCTO_SERVICIO_INACTIVO" : "IDENTIDAD_ESPECIFICA_INACTIVA";
}
