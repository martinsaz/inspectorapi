namespace checklistWs.Models.Recepcion
{
    public sealed class RecepcionConfirmarRequest
    {
        public Guid IdEmpresa { get; set; }
        public Guid IdOrdenCompra { get; set; }
        public Guid IdSucursal { get; set; }
        public DateTime? FechaRecepcion { get; set; }
        public string OperationKey { get; set; } = string.Empty;
        public string Observaciones { get; set; } = string.Empty;
        public List<RecepcionPartidaConfirmarRequest> Partidas { get; set; } = new();
    }

    public sealed class RecepcionPartidaConfirmarRequest
    {
        public Guid IdOrdenCompraDetalle { get; set; }
        public decimal CantidadCompraRecibida { get; set; }
        public List<string> Series { get; set; } = new();
    }

    public sealed class RecepcionOperacionResponse
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public Guid? IdRecepcion { get; set; }
        public string FolioRecepcion { get; set; } = string.Empty;
        public bool AlreadyApplied { get; set; }
    }

    public class RecepcionListadoDto
    {
        public Guid Id { get; set; }
        public string FolioRecepcion { get; set; } = string.Empty;
        public Guid IdOrdenCompra { get; set; }
        public string FolioOrdenCompra { get; set; } = string.Empty;
        public Guid IdSucursal { get; set; }
        public string Sucursal { get; set; } = string.Empty;
        public DateTime FechaRecepcion { get; set; }
        public byte Estado { get; set; }
        public string EstadoNombre { get; set; } = string.Empty;
    }

    public sealed class RecepcionDetalleDto : RecepcionListadoDto
    {
        public string OperationKey { get; set; } = string.Empty;
        public string Observaciones { get; set; } = string.Empty;
        public List<RecepcionPartidaDto> Partidas { get; set; } = new();
    }

    public sealed class RecepcionPartidaDto
    {
        public Guid Id { get; set; }
        public Guid IdOrdenCompraDetalle { get; set; }
        public int NumeroPartida { get; set; }
        public byte TipoPartida { get; set; }
        public string TipoPartidaNombre { get; set; } = string.Empty;
        public Guid IdProductoServicio { get; set; }
        public Guid? IdVariante { get; set; }
        public decimal CantidadCompraRecibida { get; set; }
        public decimal CantidadBaseEstaRecepcion { get; set; }
        public decimal CantidadBaseRecibidaAnterior { get; set; }
        public decimal CantidadBaseRecibidaAcumulada { get; set; }
        public decimal CantidadBasePendiente { get; set; }
        public bool ControlSerie { get; set; }
        public List<string> Series { get; set; } = new();
    }

    public sealed class OrdenCompraRecepcionPendienteDto
    {
        public Guid IdOrdenCompra { get; set; }
        public string Folio { get; set; } = string.Empty;
        public Guid IdSucursal { get; set; }
        public string Sucursal { get; set; } = string.Empty;
        public byte Estado { get; set; }
        public string EstadoNombre { get; set; } = string.Empty;
        public List<OrdenCompraRecepcionPartidaPendienteDto> Partidas { get; set; } = new();
    }

    public sealed class OrdenCompraRecepcionPartidaPendienteDto
    {
        public Guid IdOrdenCompraDetalle { get; set; }
        public int NumeroPartida { get; set; }
        public byte TipoPartida { get; set; }
        public string TipoPartidaNombre { get; set; } = string.Empty;
        public Guid IdProductoServicio { get; set; }
        public Guid? IdVariante { get; set; }
        public Guid? IdPresentacionCompra { get; set; }
        public string Producto { get; set; } = string.Empty;
        public string Variante { get; set; } = string.Empty;
        public string PresentacionCompra { get; set; } = string.Empty;
        public decimal FactorConversionSnapshot { get; set; }
        public decimal CantidadBaseOrdenada { get; set; }
        public decimal CantidadBaseRecibidaAcumulada { get; set; }
        public decimal CantidadBasePendiente { get; set; }
        public bool CausaInventario { get; set; }
        public bool UsaNumeroSerie { get; set; }
    }
}
