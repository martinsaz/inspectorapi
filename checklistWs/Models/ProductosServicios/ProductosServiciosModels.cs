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
        public Guid? IdColeccion { get; set; }
        public string ColeccionNumero { get; set; } = string.Empty;
        public string ColeccionNombre { get; set; } = string.Empty;
        public Guid? IdPaquete { get; set; }
        public string PaqueteNombre { get; set; } = string.Empty;
        public decimal? Costo { get; set; }
        public decimal PrecioPublico { get; set; }
        public decimal? PrecioComparacion { get; set; }
        public decimal? PrecioUnitarioMonto { get; set; }
        public decimal? PrecioUnitarioBaseCantidad { get; set; }
        public string PrecioUnitarioUnidad { get; set; } = string.Empty;
        public string ObjetoImpuesto { get; set; } = string.Empty;
        public string ClaveProductoSat { get; set; } = string.Empty;
        public string ClaveUnidadSat { get; set; } = string.Empty;
        public bool EsProductoFisico { get; set; }
        public decimal? PesoKg { get; set; }
        public decimal? LargoCm { get; set; }
        public decimal? AnchoCm { get; set; }
        public decimal? AltoCm { get; set; }
        public bool UsaNumeroSerie { get; set; }
        public bool CausaInventario { get; set; }
        public bool PermiteVentaSinExistencia { get; set; }
        public decimal? ExistenciaActual { get; set; }
        public decimal? ExistenciaMinima { get; set; }
        public decimal? CostoPromedio { get; set; }
        public string ImagenUrl { get; set; } = string.Empty;
        public string ImagenNombre { get; set; } = string.Empty;
        public int CantidadFotos { get; set; }
        public int CantidadVideos { get; set; }
        public int CantidadDocumentos { get; set; }
        public int CantidadVariantes { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaActualizacion { get; set; }
        public DateTime? FechaArchivado { get; set; }
    }

    public class ProductoServicioDetalleDto : ProductoServicioListadoDto
    {
        public Guid? IdExistencia { get; set; }
        public List<ProductoServicioMovimientoDto> MovimientosRecientes { get; set; } = new List<ProductoServicioMovimientoDto>();
        public List<ProductoServicioAtributoSeleccionDto> Atributos { get; set; } = new List<ProductoServicioAtributoSeleccionDto>();
        public List<ProductoServicioOpcionVarianteDto> OpcionesVariante { get; set; } = new List<ProductoServicioOpcionVarianteDto>();
        public List<ProductoServicioVarianteDto> Variantes { get; set; } = new List<ProductoServicioVarianteDto>();
        public List<ProductoServicioMultimediaDto> Multimedia { get; set; } = new List<ProductoServicioMultimediaDto>();
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
        public Guid? IdColeccion { get; set; }
        public Guid? IdPaquete { get; set; }
        public decimal? Costo { get; set; }
        public decimal PrecioPublico { get; set; }
        public decimal? PrecioComparacion { get; set; }
        public decimal? PrecioUnitarioMonto { get; set; }
        public decimal? PrecioUnitarioBaseCantidad { get; set; }
        public string PrecioUnitarioUnidad { get; set; } = string.Empty;
        public string ObjetoImpuesto { get; set; } = string.Empty;
        public string ClaveProductoSat { get; set; } = string.Empty;
        public string ClaveUnidadSat { get; set; } = string.Empty;
        public bool EsProductoFisico { get; set; }
        public decimal? PesoKg { get; set; }
        public decimal? LargoCm { get; set; }
        public decimal? AnchoCm { get; set; }
        public decimal? AltoCm { get; set; }
        public bool UsaNumeroSerie { get; set; }
        public bool CausaInventario { get; set; }
        public bool PermiteVentaSinExistencia { get; set; }
        public decimal? ExistenciaInicial { get; set; }
        public decimal? ExistenciaMinima { get; set; }
        public bool Activo { get; set; } = true;
        public ProductoServicioImagenGuardarRequest? ImagenPrincipal { get; set; }
        public bool EliminarImagenPrincipal { get; set; }
        public List<ProductoServicioAtributoGuardarRequest> Atributos { get; set; } = new List<ProductoServicioAtributoGuardarRequest>();
        public List<ProductoServicioOpcionVarianteGuardarRequest> OpcionesVariante { get; set; } = new List<ProductoServicioOpcionVarianteGuardarRequest>();
        public List<ProductoServicioVarianteGuardarRequest> Variantes { get; set; } = new List<ProductoServicioVarianteGuardarRequest>();
        public List<ProductoServicioMultimediaGuardarRequest> Multimedia { get; set; } = new List<ProductoServicioMultimediaGuardarRequest>();
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

    public class ProductoServicioColeccionDto : ProductoServicioCatalogoBasicoDto
    {
        public string Numero { get; set; } = string.Empty;
    }

    public class ProductoServicioPaqueteDto : ProductoServicioCatalogoBasicoDto
    {
        public string TipoPaquete { get; set; } = string.Empty;
        public decimal? LargoCm { get; set; }
        public decimal? AnchoCm { get; set; }
        public decimal? AltoCm { get; set; }
        public decimal? PesoEmpaqueVacioKg { get; set; }
        public bool EsPredeterminado { get; set; }
    }

    public class ProductoServicioAtributoDto : ProductoServicioCatalogoBasicoDto
    {
        public List<ProductoServicioAtributoValorDto> Valores { get; set; } = new List<ProductoServicioAtributoValorDto>();
    }

    public class ProductoServicioAtributoValorDto
    {
        public Guid Id { get; set; }
        public Guid IdEmpresa { get; set; }
        public Guid IdAtributo { get; set; }
        public string Valor { get; set; } = string.Empty;
        public int Orden { get; set; }
        public bool Activo { get; set; }
    }

    public class ProductoServicioAtributoValorOperacionResponse : ProductoServicioOperacionResponse
    {
        public ProductoServicioAtributoValorDto? Valor { get; set; }
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

    public class ProductoServicioColeccionGuardarRequest
    {
        public Guid? Id { get; set; }
        public Guid IdEmpresa { get; set; }
        public string Numero { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
    }

    public class ProductoServicioPaqueteGuardarRequest
    {
        public Guid? Id { get; set; }
        public Guid IdEmpresa { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string TipoPaquete { get; set; } = string.Empty;
        public decimal? LargoCm { get; set; }
        public decimal? AnchoCm { get; set; }
        public decimal? AltoCm { get; set; }
        public decimal? PesoEmpaqueVacioKg { get; set; }
        public bool EsPredeterminado { get; set; }
    }

    public class ProductoServicioAtributoCatalogoGuardarRequest
    {
        public Guid? Id { get; set; }
        public Guid IdEmpresa { get; set; }
        public string Nombre { get; set; } = string.Empty;
    }

    public class ProductoServicioAtributoValorCatalogoGuardarRequest
    {
        public Guid? Id { get; set; }
        public Guid IdEmpresa { get; set; }
        public Guid IdAtributo { get; set; }
        public string Valor { get; set; } = string.Empty;
        public int Orden { get; set; }
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
        public string Numero { get; set; } = string.Empty;
        public string TipoPaquete { get; set; } = string.Empty;
        public decimal? LargoCm { get; set; }
        public decimal? AnchoCm { get; set; }
        public decimal? AltoCm { get; set; }
        public decimal? PesoEmpaqueVacioKg { get; set; }
        public bool? EsPredeterminado { get; set; }
    }

    public class ProductoServicioCombosDto
    {
        public List<ProductoServicioCatalogoComboDto> Categorias { get; set; } = new List<ProductoServicioCatalogoComboDto>();
        public List<ProductoServicioCatalogoComboDto> Marcas { get; set; } = new List<ProductoServicioCatalogoComboDto>();
        public List<ProductoServicioCatalogoComboDto> UnidadesMedida { get; set; } = new List<ProductoServicioCatalogoComboDto>();
        public List<ProductoServicioCatalogoComboDto> Colecciones { get; set; } = new List<ProductoServicioCatalogoComboDto>();
        public List<ProductoServicioCatalogoComboDto> Paquetes { get; set; } = new List<ProductoServicioCatalogoComboDto>();
        public List<ProductoServicioCatalogoComboDto> Atributos { get; set; } = new List<ProductoServicioCatalogoComboDto>();
        public List<ProductoServicioOpcionDto> Tipos { get; set; } = new List<ProductoServicioOpcionDto>();
        public List<ProductoServicioOpcionDto> Estatus { get; set; } = new List<ProductoServicioOpcionDto>();
        public List<ProductoServicioOpcionDto> ObjetosImpuesto { get; set; } = new List<ProductoServicioOpcionDto>();
        public List<ProductoServicioOpcionDto> TiposPaquete { get; set; } = new List<ProductoServicioOpcionDto>();
        public List<ProductoServicioOpcionDto> UnidadesPrecioUnitario { get; set; } = new List<ProductoServicioOpcionDto>();
    }

    public class ProductoServicioOpcionDto
    {
        public string Clave { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
    }

    public class ProductoServicioSatCatalogosResponseDto
    {
        public List<ProductoServicioOpcionDto> Items { get; set; } = new List<ProductoServicioOpcionDto>();
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
        public string Coleccion { get; set; } = string.Empty;
        public string UnidadMedida { get; set; } = string.Empty;
        public string UnidadAbreviatura { get; set; } = string.Empty;
        public decimal? Costo { get; set; }
        public decimal PrecioPublico { get; set; }
        public decimal? PrecioComparacion { get; set; }
        public string ObjetoImpuesto { get; set; } = string.Empty;
        public string ClaveProductoSat { get; set; } = string.Empty;
        public string ClaveUnidadSat { get; set; } = string.Empty;
        public bool EsProductoFisico { get; set; }
        public bool UsaNumeroSerie { get; set; }
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

    public class ProductoServicioAtributoSeleccionDto
    {
        public Guid? IdProductoAtributo { get; set; }
        public Guid IdAtributo { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int Orden { get; set; }
        public List<ProductoServicioAtributoValorSeleccionDto> Valores { get; set; } = new List<ProductoServicioAtributoValorSeleccionDto>();
    }

    public class ProductoServicioAtributoValorSeleccionDto
    {
        public Guid IdAtributoValor { get; set; }
        public string Valor { get; set; } = string.Empty;
        public int Orden { get; set; }
    }

    public class ProductoServicioAtributoGuardarRequest
    {
        public Guid? IdProductoAtributo { get; set; }
        public Guid IdAtributo { get; set; }
        public int Orden { get; set; }
        public List<ProductoServicioAtributoValorGuardarRequest> Valores { get; set; } = new List<ProductoServicioAtributoValorGuardarRequest>();
    }

    public class ProductoServicioAtributoValorGuardarRequest
    {
        public Guid? IdAtributoValor { get; set; }
        public string Valor { get; set; } = string.Empty;
        public int Orden { get; set; }
    }

    public class ProductoServicioVarianteDto
    {
        public Guid Id { get; set; }
        public Guid IdProductoServicio { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string ClaveCombinacion { get; set; } = string.Empty;
        public string ImagenUrl { get; set; } = string.Empty;
        public string ImagenNombre { get; set; } = string.Empty;
        public decimal? PrecioPublico { get; set; }
        public decimal? PrecioComparacion { get; set; }
        public decimal? PrecioUnitarioMonto { get; set; }
        public decimal? PrecioUnitarioBaseCantidad { get; set; }
        public string PrecioUnitarioUnidad { get; set; } = string.Empty;
        public int Orden { get; set; }
        public bool Activo { get; set; }
        public List<ProductoServicioVarianteValorDto> Valores { get; set; } = new List<ProductoServicioVarianteValorDto>();
    }

    public class ProductoServicioOpcionVarianteDto
    {
        public Guid Id { get; set; }
        public Guid IdProductoServicio { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int Orden { get; set; }
        public bool Activo { get; set; }
        public List<ProductoServicioOpcionVarianteValorDto> Valores { get; set; } = new List<ProductoServicioOpcionVarianteValorDto>();
    }

    public class ProductoServicioOpcionVarianteValorDto
    {
        public Guid Id { get; set; }
        public Guid IdOpcionVariante { get; set; }
        public string Valor { get; set; } = string.Empty;
        public int Orden { get; set; }
        public bool Activo { get; set; }
    }

    public class ProductoServicioVarianteValorDto
    {
        public Guid IdOpcionVariante { get; set; }
        public string Opcion { get; set; } = string.Empty;
        public Guid IdOpcionVarianteValor { get; set; }
        public string Valor { get; set; } = string.Empty;
        public int Orden { get; set; }
    }

    public class ProductoServicioOpcionVarianteGuardarRequest
    {
        public Guid? Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int Orden { get; set; }
        public List<ProductoServicioOpcionVarianteValorGuardarRequest> Valores { get; set; } = new List<ProductoServicioOpcionVarianteValorGuardarRequest>();
    }

    public class ProductoServicioOpcionVarianteValorGuardarRequest
    {
        public Guid? Id { get; set; }
        public string Valor { get; set; } = string.Empty;
        public int Orden { get; set; }
    }

    public class ProductoServicioVarianteGuardarRequest
    {
        public Guid? Id { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string ClaveCombinacion { get; set; } = string.Empty;
        public ProductoServicioImagenGuardarRequest? Imagen { get; set; }
        public bool EliminarImagen { get; set; }
        public decimal? PrecioPublico { get; set; }
        public decimal? PrecioComparacion { get; set; }
        public decimal? PrecioUnitarioMonto { get; set; }
        public decimal? PrecioUnitarioBaseCantidad { get; set; }
        public string PrecioUnitarioUnidad { get; set; } = string.Empty;
        public int Orden { get; set; }
        public List<ProductoServicioVarianteValorGuardarRequest> Valores { get; set; } = new List<ProductoServicioVarianteValorGuardarRequest>();
    }

    public class ProductoServicioVarianteValorGuardarRequest
    {
        public Guid? IdOpcionVariante { get; set; }
        public Guid? IdOpcionVarianteValor { get; set; }
        public string Opcion { get; set; } = string.Empty;
        public string Valor { get; set; } = string.Empty;
        public int Orden { get; set; }
    }

    public class ProductoServicioMultimediaDto
    {
        public Guid Id { get; set; }
        public Guid IdProductoServicio { get; set; }
        public string TipoMultimedia { get; set; } = string.Empty;
        public bool Foto { get; set; }
        public bool Video { get; set; }
        public bool Documento { get; set; }
        public string NombreOriginal { get; set; } = string.Empty;
        public string NombreAlmacenado { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public string UrlFirebase { get; set; } = string.Empty;
        public long PesoBytes { get; set; }
        public int Orden { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaActualizacion { get; set; }
    }

    public class ProductoServicioMultimediaGuardarRequest
    {
        public Guid? Id { get; set; }
        public string TipoMultimedia { get; set; } = string.Empty;
        public string NombreOriginal { get; set; } = string.Empty;
        public string NombreAlmacenado { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public string UrlFirebase { get; set; } = string.Empty;
        public long PesoBytes { get; set; }
        public int Orden { get; set; }
        public string TemporalToken { get; set; } = string.Empty;
    }

    public class ProductoServicioMultimediaTemporalDto
    {
        public string TemporalToken { get; set; } = string.Empty;
        public string TipoMultimedia { get; set; } = string.Empty;
        public string NombreOriginal { get; set; } = string.Empty;
        public string NombreAlmacenado { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public string UrlFirebase { get; set; } = string.Empty;
        public long PesoBytes { get; set; }
    }

    public class ProductoServicioMultimediaTemporalResponse : ProductoServicioOperacionResponse
    {
        public ProductoServicioMultimediaTemporalDto? Archivo { get; set; }
    }

    public class ProductoServicioMultimediaTemporalCleanupRequest
    {
        public List<string> Tokens { get; set; } = new List<string>();
    }

    public class ProductoServicioOperacionResponse
    {
        public string Mensaje { get; set; } = string.Empty;
    }

    public class ProductoServicioColeccionOperacionResponse : ProductoServicioOperacionResponse
    {
        public ProductoServicioCatalogoComboDto? Coleccion { get; set; }
    }

    public class ProductoServicioPaqueteOperacionResponse : ProductoServicioOperacionResponse
    {
        public ProductoServicioCatalogoComboDto? Paquete { get; set; }
    }

    public class ProductoServicioAtributoOperacionResponse : ProductoServicioOperacionResponse
    {
        public ProductoServicioCatalogoComboDto? Atributo { get; set; }
    }
}
