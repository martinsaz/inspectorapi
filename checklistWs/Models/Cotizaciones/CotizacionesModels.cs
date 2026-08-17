namespace checklistWs.Models.Cotizaciones
{
    public static class CotizacionEstados
    {
        public const byte Borrador = 1;
        public const byte Cancelada = 2;
        public const byte Autorizada = 3;
    }

    public sealed class CotizacionGuardarRequest
    {
        public Guid? Id { get; set; }
        public Guid IdEmpresa { get; set; }
        public Guid IdCliente { get; set; }
        public Guid? IdSucursal { get; set; }
        public string Observaciones { get; set; } = string.Empty;
        public int? VigenciaDias { get; set; }
        public string Caja { get; set; } = string.Empty;
        public List<CotizacionPartidaGuardarRequest> Partidas { get; set; } = new();
    }

    public sealed class CotizacionPartidaGuardarRequest
    {
        public Guid IdProductoServicio { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal DescuentoPct { get; set; }
    }

    public sealed class CotizacionCancelarRequest
    {
        public Guid IdEmpresa { get; set; }
        public Guid IdCotizacion { get; set; }
        public string MotivoCancelacion { get; set; } = string.Empty;
    }

    public sealed class CotizacionAutorizarRequest
    {
        public Guid IdEmpresa { get; set; }
        public Guid IdCotizacion { get; set; }
    }

    public sealed class CotizacionCorreoRequest
    {
        public Guid IdCotizacion { get; set; }
        public string Correo { get; set; } = string.Empty;
        public string Asunto { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public string Folio { get; set; } = string.Empty;
        public string ClienteNombre { get; set; } = string.Empty;
    }

    public sealed class CotizacionOperacionResponse
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public Guid? IdCotizacion { get; set; }
        public string Folio { get; set; } = string.Empty;
        public byte Estado { get; set; }
        public string EstadoNombre { get; set; } = string.Empty;
        public decimal Subtotal { get; set; }
        public decimal DescuentoTotal { get; set; }
        public decimal Total { get; set; }
    }

    public sealed class CotizacionListadoDto
    {
        public Guid Id { get; set; }
        public string Folio { get; set; } = string.Empty;
        public DateTime FechaCotizacion { get; set; }
        public DateTime? FechaVigencia { get; set; }
        public Guid IdCliente { get; set; }
        public string Cliente { get; set; } = string.Empty;
        public Guid? IdSucursal { get; set; }
        public string Sucursal { get; set; } = string.Empty;
        public string Vendedor { get; set; } = string.Empty;
        public byte Estado { get; set; }
        public string EstadoNombre { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public decimal TotalPiezas { get; set; }
        public DateTime FechaCreacion { get; set; }
        public bool PuedeEditar { get; set; }
        public bool PuedeCancelar { get; set; }
        public bool PuedeClonar { get; set; }
        public bool PuedeExportarPdf { get; set; }
        public bool PuedeAutorizar { get; set; }
    }

    public sealed class CotizacionDetalleDto
    {
        public Guid Id { get; set; }
        public Guid IdEmpresa { get; set; }
        public Guid IdentityKey { get; set; }
        public string Folio { get; set; } = string.Empty;
        public DateTime FechaCotizacion { get; set; }
        public int VigenciaDias { get; set; }
        public DateTime? FechaVigencia { get; set; }
        public Guid IdCliente { get; set; }
        public string Cliente { get; set; } = string.Empty;
        public string ClienteTelefono { get; set; } = string.Empty;
        public string ClienteCorreo { get; set; } = string.Empty;
        public decimal ClienteDescuento { get; set; }
        public Guid? IdSucursal { get; set; }
        public string Sucursal { get; set; } = string.Empty;
        public string Vendedor { get; set; } = string.Empty;
        public string Caja { get; set; } = string.Empty;
        public byte Estado { get; set; }
        public string EstadoNombre { get; set; } = string.Empty;
        public string Observaciones { get; set; } = string.Empty;
        public decimal Subtotal { get; set; }
        public decimal DescuentoTotal { get; set; }
        public decimal Total { get; set; }
        public decimal TotalPiezas { get; set; }
        public string MotivoCancelacion { get; set; } = string.Empty;
        public DateTime? FechaCancelacion { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaActualizacion { get; set; }
        public List<CotizacionPartidaDetalleDto> Partidas { get; set; } = new();
    }

    public sealed class CotizacionPartidaDetalleDto
    {
        public Guid Id { get; set; }
        public int NumeroPartida { get; set; }
        public Guid IdProductoServicio { get; set; }
        public byte TipoProductoServicio { get; set; }
        public string TipoProductoServicioNombre { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public Guid IdUnidadMedida { get; set; }
        public string UnidadMedida { get; set; } = string.Empty;
        public string UnidadAbreviatura { get; set; } = string.Empty;
        public bool UnidadPermiteDecimales { get; set; }
        public bool PermiteVentaSinExistencia { get; set; }
        public decimal? ExistenciaActual { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal DescuentoPct { get; set; }
        public decimal ImporteBruto { get; set; }
        public decimal DescuentoImporte { get; set; }
        public decimal Total { get; set; }
    }

    public sealed class CotizacionResumenDto
    {
        public int Total { get; set; }
        public int Borradores { get; set; }
        public int Canceladas { get; set; }
        public decimal ImporteTotal { get; set; }
    }

    public sealed class CotizacionDocumentoExportDto
    {
        public Guid IdCotizacion { get; set; }
        public string Folio { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaCotizacion { get; set; }
        public DateTime? FechaVigencia { get; set; }
        public string Cliente { get; set; } = string.Empty;
        public string ClienteTelefono { get; set; } = string.Empty;
        public string ClienteCorreo { get; set; } = string.Empty;
        public string Sucursal { get; set; } = string.Empty;
        public string Vendedor { get; set; } = string.Empty;
        public string Caja { get; set; } = string.Empty;
        public string Observaciones { get; set; } = string.Empty;
        public decimal Subtotal { get; set; }
        public decimal DescuentoTotal { get; set; }
        public decimal Total { get; set; }
        public decimal TotalPiezas { get; set; }
        public List<CotizacionDocumentoPartidaDto> Partidas { get; set; } = new();
    }

    public sealed class CotizacionDocumentoPartidaDto
    {
        public int NumeroPartida { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Unidad { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal DescuentoPct { get; set; }
        public decimal Total { get; set; }
        public decimal? ExistenciaActual { get; set; }
    }
}
