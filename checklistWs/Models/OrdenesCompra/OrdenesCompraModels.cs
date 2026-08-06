namespace checklistWs.Models.OrdenesCompra
{
    public class OrdenCompraGuardarRequest
    {
        public Guid? Id { get; set; }
        public Guid IdEmpresa { get; set; }
        public Guid IdRazonSocial { get; set; }
        public Guid IdSucursal { get; set; }
        public Guid IdProveedor { get; set; }
        public DateTime FechaOrden { get; set; }
        public DateTime? FechaLlegada { get; set; }
        public string Observaciones { get; set; } = string.Empty;
        public List<OrdenCompraPartidaGuardarRequest> Partidas { get; set; } = new List<OrdenCompraPartidaGuardarRequest>();
    }

    public class OrdenCompraPartidaGuardarRequest
    {
        public Guid IdProductoServicio { get; set; }
        public decimal Cantidad { get; set; }
        public decimal CostoUnitario { get; set; }
    }

    public class OrdenCompraGenerarRequest
    {
        public Guid IdEmpresa { get; set; }
        public Guid IdOrdenCompra { get; set; }
    }

    public class OrdenCompraPendientesRequest
    {
        public Guid IdEmpresa { get; set; }
        public Guid IdOrdenCompra { get; set; }
    }

    public class OrdenCompraCancelarRequest
    {
        public Guid IdEmpresa { get; set; }
        public Guid IdOrdenCompra { get; set; }
        public string MotivoCancelacion { get; set; } = string.Empty;
    }

    public class OrdenCompraOperacionResponse
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public Guid? IdOrdenCompra { get; set; }
        public string Folio { get; set; } = string.Empty;
        public byte Estado { get; set; }
        public string EstadoNombre { get; set; } = string.Empty;
        public decimal Subtotal { get; set; }
        public decimal Total { get; set; }
    }

    public class OrdenCompraPendientesResponse
    {
        public bool TienePendientes { get; set; }
        public int TotalOrdenesCoincidentes { get; set; }
        public int TotalPartidasCoincidentes { get; set; }
        public List<OrdenCompraPendienteItemDto> Ordenes { get; set; } = new List<OrdenCompraPendienteItemDto>();
    }

    public class OrdenCompraPendienteItemDto
    {
        public Guid IdOrdenCompra { get; set; }
        public string Folio { get; set; } = string.Empty;
        public byte Estado { get; set; }
        public string EstadoNombre { get; set; } = string.Empty;
        public DateTime FechaOrden { get; set; }
        public int PartidasCoincidentes { get; set; }
        public decimal TotalCoincidente { get; set; }
        public List<string> Productos { get; set; } = new List<string>();
    }

    public class OrdenCompraListadoDto
    {
        public Guid Id { get; set; }
        public string Folio { get; set; } = string.Empty;
        public DateTime FechaOrden { get; set; }
        public DateTime? FechaLlegada { get; set; }
        public Guid IdRazonSocial { get; set; }
        public string RazonSocial { get; set; } = string.Empty;
        public Guid IdSucursal { get; set; }
        public string Sucursal { get; set; } = string.Empty;
        public Guid IdProveedor { get; set; }
        public string Proveedor { get; set; } = string.Empty;
        public byte Estado { get; set; }
        public string EstadoNombre { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public DateTime FechaCreacion { get; set; }
        public bool PuedeEditar { get; set; }
        public bool PuedeGenerar { get; set; }
        public bool PuedeCancelar { get; set; }
    }

    public class OrdenCompraDetalleDto
    {
        public Guid Id { get; set; }
        public Guid IdEmpresa { get; set; }
        public Guid IdentityKey { get; set; }
        public string Folio { get; set; } = string.Empty;
        public Guid IdRazonSocial { get; set; }
        public string RazonSocial { get; set; } = string.Empty;
        public Guid IdSucursal { get; set; }
        public string Sucursal { get; set; } = string.Empty;
        public Guid IdProveedor { get; set; }
        public string Proveedor { get; set; } = string.Empty;
        public DateTime FechaOrden { get; set; }
        public DateTime? FechaLlegada { get; set; }
        public byte Estado { get; set; }
        public string EstadoNombre { get; set; } = string.Empty;
        public string Observaciones { get; set; } = string.Empty;
        public decimal Subtotal { get; set; }
        public decimal Total { get; set; }
        public string MotivoCancelacion { get; set; } = string.Empty;
        public DateTime? FechaCancelacion { get; set; }
        public Guid? IdUsuarioCreacion { get; set; }
        public Guid? IdUsuarioActualizacion { get; set; }
        public Guid? IdUsuarioCancelacion { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaActualizacion { get; set; }
        public List<OrdenCompraPartidaDetalleDto> Partidas { get; set; } = new List<OrdenCompraPartidaDetalleDto>();
    }

    public class OrdenCompraPartidaDetalleDto
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
        public decimal Cantidad { get; set; }
        public decimal CostoUnitario { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Total { get; set; }
    }

    public class OrdenCompraResumenDto
    {
        public int Total { get; set; }
        public int Borradores { get; set; }
        public int Generadas { get; set; }
        public int Canceladas { get; set; }
    }

    public class OrdenCompraComboDto
    {
        public Guid Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }

    public class OrdenCompraEstadoOpcionDto
    {
        public byte Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
    }

    public class OrdenCompraCombosDto
    {
        public List<OrdenCompraComboDto> RazonesSociales { get; set; } = new List<OrdenCompraComboDto>();
        public List<OrdenCompraComboDto> Sucursales { get; set; } = new List<OrdenCompraComboDto>();
        public List<OrdenCompraComboDto> Proveedores { get; set; } = new List<OrdenCompraComboDto>();
        public List<OrdenCompraEstadoOpcionDto> Estados { get; set; } = new List<OrdenCompraEstadoOpcionDto>();
    }

    public class OrdenCompraBusquedaProductoServicioDto
    {
        public Guid Id { get; set; }
        public byte Tipo { get; set; }
        public string TipoNombre { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public Guid IdUnidadMedida { get; set; }
        public string Unidad { get; set; } = string.Empty;
        public string Abreviatura { get; set; } = string.Empty;
        public decimal? CostoActual { get; set; }
        public bool CausaInventario { get; set; }
    }

    public class OrdenCompraExportacionDto
    {
        public string Folio { get; set; } = string.Empty;
        public DateTime FechaOrden { get; set; }
        public DateTime? FechaLlegada { get; set; }
        public string RazonSocial { get; set; } = string.Empty;
        public string Sucursal { get; set; } = string.Empty;
        public string Proveedor { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public DateTime FechaCreacion { get; set; }
    }

    public class OrdenCompraDocumentoExportDto
    {
        public Guid IdOrdenCompra { get; set; }
        public string Folio { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaOrden { get; set; }
        public DateTime? FechaLlegada { get; set; }
        public string RazonSocial { get; set; } = string.Empty;
        public string Sucursal { get; set; } = string.Empty;
        public string Proveedor { get; set; } = string.Empty;
        public string Observaciones { get; set; } = string.Empty;
        public decimal Subtotal { get; set; }
        public decimal Total { get; set; }
        public List<OrdenCompraDocumentoPartidaDto> Partidas { get; set; } = new List<OrdenCompraDocumentoPartidaDto>();
    }

    public class OrdenCompraDocumentoPartidaDto
    {
        public int NumeroPartida { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Unidad { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public decimal CostoUnitario { get; set; }
        public decimal Subtotal { get; set; }
    }
}
