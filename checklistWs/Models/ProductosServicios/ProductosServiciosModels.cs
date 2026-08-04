namespace checklistWs.Models.ProductosServicios
{
    public class ProductoServicioListadoDto
    {
        public Guid Id { get; set; }
        public Guid IdEmpresa { get; set; }
        public Guid IdentityKey { get; set; }
        public byte Tipo { get; set; }
        public string TipoNombre { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public Guid IdCategoria { get; set; }
        public string Categoria { get; set; } = string.Empty;
        public byte CategoriaAplicaA { get; set; }
        public Guid? IdMarca { get; set; }
        public string Marca { get; set; } = string.Empty;
        public Guid IdUnidadMedida { get; set; }
        public string UnidadMedida { get; set; } = string.Empty;
        public string UnidadAbreviatura { get; set; } = string.Empty;
        public bool UnidadPermiteDecimales { get; set; }
        public decimal? Costo { get; set; }
        public decimal PrecioPublico { get; set; }
        public bool CausaInventario { get; set; }
        public bool PermiteVentaSinExistencia { get; set; }
        public decimal? ExistenciaActual { get; set; }
        public decimal? ExistenciaMinima { get; set; }
        public decimal? CostoPromedio { get; set; }
        public string ImagenUrl { get; set; } = string.Empty;
        public string ImagenNombre { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaActualizacion { get; set; }
        public DateTime? FechaArchivado { get; set; }
    }

    public class ProductoServicioDetalleDto : ProductoServicioListadoDto
    {
        public Guid? IdExistencia { get; set; }
        public List<ProductoServicioMovimientoDto> MovimientosRecientes { get; set; } = new List<ProductoServicioMovimientoDto>();
    }

    public class ProductoServicioGuardarRequest
    {
        public Guid? Id { get; set; }
        public Guid IdEmpresa { get; set; }
        public byte Tipo { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public Guid IdCategoria { get; set; }
        public Guid? IdMarca { get; set; }
        public Guid IdUnidadMedida { get; set; }
        public decimal? Costo { get; set; }
        public decimal PrecioPublico { get; set; }
        public bool CausaInventario { get; set; }
        public bool PermiteVentaSinExistencia { get; set; }
        public decimal? ExistenciaInicial { get; set; }
        public decimal? ExistenciaMinima { get; set; }
        public ProductoServicioImagenGuardarRequest? ImagenPrincipal { get; set; }
        public bool EliminarImagenPrincipal { get; set; }
    }

    public class ProductoServicioImagenGuardarRequest
    {
        public string TemporalToken { get; set; } = string.Empty;
        public string NombreOriginal { get; set; } = string.Empty;
        public string NombreAlmacenado { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public string UrlFirebase { get; set; } = string.Empty;
        public long PesoBytes { get; set; }
    }

    public class ProductoServicioImagenTemporalResponse : ProductoServicioOperacionResponse
    {
        public ProductoServicioImagenTemporalDto? Archivo { get; set; }
    }

    public class ProductoServicioImagenTemporalDto
    {
        public string TemporalToken { get; set; } = string.Empty;
        public string NombreOriginal { get; set; } = string.Empty;
        public string NombreAlmacenado { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public string UrlFirebase { get; set; } = string.Empty;
        public long PesoBytes { get; set; }
    }

    public class ProductoServicioImagenTemporalCleanupRequest
    {
        public List<string> Tokens { get; set; } = new List<string>();
    }

    public class ProductoServicioCatalogoBasicoDto
    {
        public Guid Id { get; set; }
        public Guid IdEmpresa { get; set; }
        public Guid IdentityKey { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaActualizacion { get; set; }
        public DateTime? FechaArchivado { get; set; }
    }

    public class ProductoServicioCategoriaDto : ProductoServicioCatalogoBasicoDto
    {
        public byte AplicaA { get; set; }
        public string AplicaANombre { get; set; } = string.Empty;
    }

    public class ProductoServicioMarcaDto : ProductoServicioCatalogoBasicoDto
    {
    }

    public class ProductoServicioUnidadMedidaDto : ProductoServicioCatalogoBasicoDto
    {
        public string Abreviatura { get; set; } = string.Empty;
        public bool PermiteDecimales { get; set; }
    }

    public class ProductoServicioCategoriaGuardarRequest
    {
        public Guid? Id { get; set; }
        public Guid IdEmpresa { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public byte AplicaA { get; set; }
    }

    public class ProductoServicioMarcaGuardarRequest
    {
        public Guid? Id { get; set; }
        public Guid IdEmpresa { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
    }

    public class ProductoServicioUnidadMedidaGuardarRequest
    {
        public Guid? Id { get; set; }
        public Guid IdEmpresa { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Abreviatura { get; set; } = string.Empty;
        public bool PermiteDecimales { get; set; }
    }

    public class ProductoServicioCatalogoComboDto
    {
        public Guid Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public byte? AplicaA { get; set; }
        public string Abreviatura { get; set; } = string.Empty;
        public bool? PermiteDecimales { get; set; }
    }

    public class ProductoServicioCombosDto
    {
        public List<ProductoServicioCatalogoComboDto> Categorias { get; set; } = new List<ProductoServicioCatalogoComboDto>();
        public List<ProductoServicioCatalogoComboDto> Marcas { get; set; } = new List<ProductoServicioCatalogoComboDto>();
        public List<ProductoServicioCatalogoComboDto> UnidadesMedida { get; set; } = new List<ProductoServicioCatalogoComboDto>();
        public List<ProductoServicioOpcionDto> Tipos { get; set; } = new List<ProductoServicioOpcionDto>();
        public List<ProductoServicioOpcionDto> Estatus { get; set; } = new List<ProductoServicioOpcionDto>();
    }

    public class ProductoServicioOpcionDto
    {
        public string Clave { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
    }

    public class ProductoServicioKpiResumenDto
    {
        public int TotalRegistros { get; set; }
        public int TotalActivos { get; set; }
        public int TotalInactivos { get; set; }
        public int TotalProductos { get; set; }
        public int TotalServicios { get; set; }
        public int TotalConInventario { get; set; }
        public int TotalSinInventario { get; set; }
        public int TotalInventarioNegativoPermitido { get; set; }
        public int TotalBajoMinimo { get; set; }
        public decimal ValorInventarioEstimado { get; set; }
    }

    public class ProductoServicioExportacionDto
    {
        public string Tipo { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public string Tag { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Categoria { get; set; } = string.Empty;
        public string Marca { get; set; } = string.Empty;
        public string UnidadMedida { get; set; } = string.Empty;
        public string UnidadAbreviatura { get; set; } = string.Empty;
        public decimal? Costo { get; set; }
        public decimal PrecioPublico { get; set; }
        public bool CausaInventario { get; set; }
        public bool PermiteVentaSinExistencia { get; set; }
        public decimal? ExistenciaActual { get; set; }
        public decimal? ExistenciaMinima { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaActualizacion { get; set; }
    }

    public class ProductoServicioExistenciaDto
    {
        public Guid Id { get; set; }
        public Guid IdEmpresa { get; set; }
        public Guid IdentityKey { get; set; }
        public Guid IdProductoServicio { get; set; }
        public decimal ExistenciaActual { get; set; }
        public decimal ExistenciaMinima { get; set; }
        public decimal? CostoPromedio { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaActualizacion { get; set; }
    }

    public class ProductoServicioMovimientoDto
    {
        public Guid Id { get; set; }
        public Guid IdEmpresa { get; set; }
        public Guid IdentityKey { get; set; }
        public Guid IdProductoServicio { get; set; }
        public byte TipoMovimiento { get; set; }
        public string TipoMovimientoNombre { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public decimal ExistenciaAnterior { get; set; }
        public decimal ExistenciaPosterior { get; set; }
        public decimal? CostoUnitario { get; set; }
        public string Referencia { get; set; } = string.Empty;
        public string Observaciones { get; set; } = string.Empty;
        public Guid? IdUsuario { get; set; }
        public DateTime FechaMovimiento { get; set; }
    }

    public class ProductoServicioMovimientoGuardarRequest
    {
        public Guid IdEmpresa { get; set; }
        public Guid IdProductoServicio { get; set; }
        public decimal Cantidad { get; set; }
        public decimal? CostoUnitario { get; set; }
        public string Referencia { get; set; } = string.Empty;
        public string Observaciones { get; set; } = string.Empty;
    }

    public class ProductoServicioOperacionResponse
    {
        public string Mensaje { get; set; } = string.Empty;
    }
}
