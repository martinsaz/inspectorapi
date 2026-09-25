namespace checklistWs.Services.Tenant
{
    public enum CurvasModoCaptura : byte
    {
        Manual = 1,
        PedidoInicial = 2,
        RellenarCurva = 3,
        NoPedir = 4
    }

    public sealed class CurvaCrearRequest
    {
        public string Nombre { get; set; } = string.Empty;
        public string? Codigo { get; set; }
        public Guid? UsuarioId { get; set; }
    }

    public sealed class CurvaDetalleRequest
    {
        public Guid IdCurva { get; set; }
        public Guid IdProductoServicio { get; set; }
        public Guid? IdVariante { get; set; }
        public decimal CantidadBaseObjetivo { get; set; }
        public Guid? UsuarioId { get; set; }
    }

    public sealed class CurvaSiembraRequest
    {
        public Guid IdSucursal { get; set; }
        public Guid IdProductoServicio { get; set; }
        public Guid? IdVariante { get; set; }
        public Guid IdCurva { get; set; }
        public Guid? UsuarioId { get; set; }
    }

    public sealed class CurvaOperacionRequest
    {
        public string OperationKey { get; set; } = string.Empty;
        public Guid IdProveedor { get; set; }
        public int SucursalCount { get; set; }
        public string? ContextoJson { get; set; }
        public Guid? UsuarioId { get; set; }
    }

    public sealed class CurvaOperacionOrdenCompraRequest
    {
        public Guid IdOperacionCurva { get; set; }
        public Guid IdOrdenCompra { get; set; }
        public Guid IdSucursal { get; set; }
    }

    public sealed class CurvaSnapshotRequest
    {
        public Guid IdOperacionCurva { get; set; }
        public Guid? IdOrdenCompra { get; set; }
        public Guid? IdOrdenCompraDetalle { get; set; }
        public Guid? IdCurva { get; set; }
        public Guid? IdSiembra { get; set; }
        public Guid IdSucursal { get; set; }
        public Guid IdProductoServicio { get; set; }
        public Guid? IdVariante { get; set; }
        public CurvasModoCaptura Modo { get; set; }
        public decimal CurvaObjetivoBase { get; set; }
        public decimal ExistenciaSnapshotBase { get; set; }
        public decimal TransitoSnapshotBase { get; set; }
        public decimal HuecoSnapshotBase { get; set; }
        public decimal CopeteSnapshotBase { get; set; }
        public decimal CantidadPropuestaBase { get; set; }
        public decimal CantidadFinalBase { get; set; }
        public bool OverrideManual { get; set; }
        public bool NoPedir { get; set; }
        public Guid? IdPresentacionCompra { get; set; }
        public bool PermiteCantidadBaseSnapshot { get; set; }
        public string? ContextoJson { get; set; }
        public Guid? UsuarioId { get; set; }
    }

    public sealed class CurvaOperacionResult
    {
        public Guid Id { get; set; }
        public bool AlreadyApplied { get; set; }
    }

    public enum CurvasSugerenciaEstado : byte
    {
        SinCurva = 0,
        Hueco = 1,
        Copete = 2,
        Completa = 3,
        Manual = 4,
        NoPedir = 5
    }

    public sealed class CurvasSugerenciaPreviewRequest
    {
        public Guid IdEmpresa { get; set; }
        public string EmpresaKey { get; set; } = string.Empty;
        public IReadOnlyList<CurvasSugerenciaItemRequest> Items { get; set; } = Array.Empty<CurvasSugerenciaItemRequest>();
    }

    public sealed class CurvasSugerenciaItemRequest
    {
        public Guid IdSucursal { get; set; }
        public Guid IdProductoServicio { get; set; }
        public Guid? IdVariante { get; set; }
        public Guid? IdPresentacionCompra { get; set; }
        public CurvasModoCaptura Modo { get; set; } = CurvasModoCaptura.RellenarCurva;
        public decimal? CantidadManualBase { get; set; }
        public string? ContextoJson { get; set; }
    }

    public sealed class CurvasSugerenciaPreviewResponse
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public IReadOnlyList<CurvasSugerenciaResult> Items { get; set; } = Array.Empty<CurvasSugerenciaResult>();
    }

    public sealed class CurvasSugerenciaResult
    {
        public Guid IdSucursal { get; set; }
        public Guid IdProductoServicio { get; set; }
        public Guid? IdVariante { get; set; }
        public Guid? IdCurva { get; set; }
        public Guid? IdSiembra { get; set; }
        public CurvasModoCaptura Modo { get; set; }
        public CurvasSugerenciaEstado Estado { get; set; }
        public decimal CurvaObjetivoBase { get; set; }
        public decimal ExistenciaBase { get; set; }
        public decimal TransitoBase { get; set; }
        public decimal CoberturaBase { get; set; }
        public decimal HuecoBase { get; set; }
        public decimal CopeteBase { get; set; }
        public decimal CantidadPropuestaBase { get; set; }
        public decimal CantidadFinalBase { get; set; }
        public Guid? IdPresentacionCompra { get; set; }
        public bool PermiteCantidadBase { get; set; }
        public decimal? FactorConversionBase { get; set; }
        public decimal CantidadCompraSugerida { get; set; }
        public decimal CantidadBaseConvertida { get; set; }
        public decimal ExcedenteRedondeoBase { get; set; }
        public bool OverrideManual { get; set; }
        public bool NoPedir { get; set; }
        public bool PreviewReadOnly { get; set; } = true;
        public CurvaSnapshotPreviewPayload SnapshotPayload { get; set; } = new();
    }

    public sealed class CurvaSnapshotPreviewPayload
    {
        public Guid? IdOperacionCurva { get; set; }
        public Guid? IdOrdenCompra { get; set; }
        public Guid? IdOrdenCompraDetalle { get; set; }
        public Guid? IdCurva { get; set; }
        public Guid? IdSiembra { get; set; }
        public Guid IdSucursal { get; set; }
        public Guid IdProductoServicio { get; set; }
        public Guid? IdVariante { get; set; }
        public CurvasModoCaptura Modo { get; set; }
        public decimal CurvaObjetivoBase { get; set; }
        public decimal ExistenciaSnapshotBase { get; set; }
        public decimal TransitoSnapshotBase { get; set; }
        public decimal HuecoSnapshotBase { get; set; }
        public decimal CopeteSnapshotBase { get; set; }
        public decimal CantidadPropuestaBase { get; set; }
        public decimal CantidadFinalBase { get; set; }
        public bool OverrideManual { get; set; }
        public bool NoPedir { get; set; }
        public Guid? IdPresentacionCompra { get; set; }
        public bool PermiteCantidadBaseSnapshot { get; set; }
        public string? ContextoJson { get; set; }
    }

    public sealed class CurvasCatalogoListRequest
    {
        public string? Busqueda { get; set; }
        public string Estatus { get; set; } = "activos";
    }

    public class CurvasCatalogoItemDto
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public byte Estado { get; set; }
        public bool Activo { get; set; }
        public int Renglones { get; set; }
        public decimal PiezasObjetivo { get; set; }
        public DateTime FechaActualizacion { get; set; }
    }

    public sealed class CurvasCatalogoDetalleDto : CurvasCatalogoItemDto
    {
        public IReadOnlyList<CurvasCatalogoDetalleRenglonDto> Detalles { get; set; } = Array.Empty<CurvasCatalogoDetalleRenglonDto>();
    }

    public sealed class CurvasCatalogoDetalleRenglonDto
    {
        public Guid Id { get; set; }
        public Guid IdProductoServicio { get; set; }
        public Guid? IdVariante { get; set; }
        public string Producto { get; set; } = string.Empty;
        public string CodigoProducto { get; set; } = string.Empty;
        public string Variante { get; set; } = string.Empty;
        public string UnidadBase { get; set; } = string.Empty;
        public decimal CantidadBaseObjetivo { get; set; }
    }

    public sealed class CurvasCatalogoGuardarRequest
    {
        public Guid? Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Codigo { get; set; }
        public bool Activo { get; set; } = true;
        public Guid? UsuarioId { get; set; }
        public IReadOnlyList<CurvasCatalogoGuardarDetalleRequest> Detalles { get; set; } = Array.Empty<CurvasCatalogoGuardarDetalleRequest>();
    }

    public sealed class CurvasCatalogoGuardarDetalleRequest
    {
        public Guid IdProductoServicio { get; set; }
        public Guid? IdVariante { get; set; }
        public decimal CantidadBaseObjetivo { get; set; }
    }

    public sealed class CurvasProductoElegibleDto
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public string UnidadBase { get; set; } = string.Empty;
        public int Variantes { get; set; }
    }

    public sealed class CurvasVarianteElegibleDto
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
    }

    public sealed class CurvasCatalogoOperacionResponse
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public Guid? Id { get; set; }
    }
}
