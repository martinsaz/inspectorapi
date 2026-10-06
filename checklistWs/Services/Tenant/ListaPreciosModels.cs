namespace checklistWs.Services.Tenant
{
    public static class ListaPreciosConstants
    {
        public const byte TipoProducto = 1;
        public const byte TipoServicio = 2;
        public const byte IdentidadProducto = 1;
        public const byte IdentidadServicio = 2;
        public const byte IdentidadVariante = 3;
        public const byte IdentidadPresentacionVenta = 4;
        public const int NivelDefault = 1;
        public const int NivelMinimo = 1;
        public const int NivelMaximo = 10;
        public const byte RedondeoSinRedondeo = 0;
        public const byte RedondeoA49 = 1;
        public const byte RedondeoSolo9 = 2;
        public const string ReglaVersionLp08 = "LP-08-V2";
        public const string OrigenHistorialIndividual = "INDIVIDUAL";
        public const string OrigenHistorialMasivo = "MASIVO";
        public const string OrigenHistorialCopiaLista = "COPIA_LISTA";
        public const string OrigenHistorialDescuentoMarca = "DESCUENTO_MARCA";
    }

    public static class ListaPreciosResolutionCodes
    {
        public const string PrecioLista = "PRECIO_LISTA";
        public const string FallbackPrecioPublico = "FALLBACK_PRECIO_PUBLICO";
        public const string ListaDefault = "LISTA_DEFAULT";
        public const string ListaInvalida = "LISTA_INVALIDA";
        public const string IdentidadInactiva = "IDENTIDAD_INACTIVA";
        public const string IdentidadInvalida = "IDENTIDAD_INVALIDA";
        public const string SinPrecioResoluble = "SIN_PRECIO_RESOLUBLE";
        public const string FueraDeV1 = "FUERA_DE_V1";
        public const string DescuentoInvalido = "DESCUENTO_INVALIDO";
        public const string RedondeoInvalido = "REDONDEO_INVALIDO";
        public const string VigenciaInvalida = "VIGENCIA_INVALIDA";
        public const string Duplicado = "DUPLICADO";
        public const string SchemaIncompatible = "SCHEMA_INCOMPATIBLE";
        public const string TenantMismatch = "TENANT_MISMATCH";
        public const string PromocionNoDisponiblePorContrato = "PROMOCION_NO_DISPONIBLE_POR_CONTRATO";
        public const string NoImplementado = "NO_IMPLEMENTADO";
    }

    public sealed class ListaPreciosResolverRequest
    {
        public int? Nivel { get; set; }
        public byte? TipoIdentidad { get; set; }
        public Guid IdProductoServicio { get; set; }
        public Guid? IdVariante { get; set; }
        public Guid? IdPresentacionVenta { get; set; }
        public bool RequiereActivo { get; set; } = true;
    }

    public sealed class ListaPreciosResolutionResult
    {
        public bool Resuelto { get; set; }
        public decimal? PrecioEfectivo { get; set; }
        public string CodigoResolucion { get; set; } = string.Empty;
        public string Origen { get; set; } = string.Empty;
        public int ListaSolicitada { get; set; }
        public int ListaEfectiva { get; set; }
        public Guid? IdListaPrecio { get; set; }
        public byte TipoIdentidad { get; set; }
        public Guid IdProductoServicio { get; set; }
        public Guid? IdVariante { get; set; }
        public Guid? IdPresentacionVenta { get; set; }
        public Guid? IdDetallePrecio { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public Guid TenantId { get; set; }
        public decimal? PrecioBase { get; set; }
        public decimal? Costo { get; set; }
        public decimal? MargenPct { get; set; }
        public decimal? PrecioLista { get; set; }
        public string OrigenPrecio { get; set; } = string.Empty;
        public decimal? DescuentoPct { get; set; }
        public decimal? SubtotalAntesRedondeo { get; set; }
        public byte RedondeoModo { get; set; }
        public decimal? PrecioFinal { get; set; }
        public DateTime? VigenciaInicio { get; set; }
        public DateTime? VigenciaFin { get; set; }
        public bool PromocionEvaluada { get; set; }
        public bool PromocionAplicada { get; set; }
        public string PromocionCodigo { get; set; } = string.Empty;
        public string ReglaVersion { get; set; } = ListaPreciosConstants.ReglaVersionLp08;
        public DateTime FechaResolucionUtc { get; set; }
        public Guid CorrelationId { get; set; }
    }

    public class ListaPreciosGuardarPrecioRequest
    {
        public int? Nivel { get; set; }
        public byte? TipoIdentidad { get; set; }
        public Guid IdProductoServicio { get; set; }
        public Guid? IdVariante { get; set; }
        public Guid? IdPresentacionVenta { get; set; }
        public decimal Precio { get; set; }
        public decimal? DescuentoPct { get; set; }
        public byte RedondeoModo { get; set; }
        public DateTime? VigenciaInicio { get; set; }
        public DateTime? VigenciaFin { get; set; }
        public string Motivo { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;
        public Guid? CorrelationId { get; set; }
        public Guid? UsuarioId { get; set; }
    }

    public sealed class ListaPreciosPreviewRequest : ListaPreciosGuardarPrecioRequest
    {
    }

    public sealed class ListaPreciosAjusteMasivoIdentidadRequest
    {
        public byte? TipoIdentidad { get; set; }
        public Guid IdProductoServicio { get; set; }
        public Guid? IdVariante { get; set; }
        public Guid? IdPresentacionVenta { get; set; }
    }

    public sealed class ListaPreciosAjusteMasivoRequest
    {
        public int? Nivel { get; set; }
        public string Campo { get; set; } = string.Empty;
        public string TipoAjuste { get; set; } = string.Empty;
        public string Operacion { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public IReadOnlyList<ListaPreciosAjusteMasivoIdentidadRequest> Identidades { get; set; } = Array.Empty<ListaPreciosAjusteMasivoIdentidadRequest>();
        public string Motivo { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;
        public Guid? UsuarioId { get; set; }
        public Guid? CorrelationId { get; set; }
    }

    public sealed class ListaPreciosAjusteMasivoItemDto
    {
        public byte TipoIdentidad { get; set; }
        public Guid IdProductoServicio { get; set; }
        public Guid? IdVariante { get; set; }
        public Guid? IdPresentacionVenta { get; set; }
        public decimal? ValorActual { get; set; }
        public decimal? ValorPropuesto { get; set; }
        public decimal? Diferencia { get; set; }
        public decimal? PrecioLista { get; set; }
        public decimal? DescuentoPct { get; set; }
        public byte RedondeoModo { get; set; }
        public decimal? PrecioFinal { get; set; }
        public bool Aceptado { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string MotivoRechazo { get; set; } = string.Empty;
    }

    public sealed class ListaPreciosAjusteMasivoResultadoDto
    {
        public int Seleccionados { get; set; }
        public int Aceptados { get; set; }
        public int Rechazados { get; set; }
        public int Nivel { get; set; }
        public string Campo { get; set; } = string.Empty;
        public string TipoAjuste { get; set; } = string.Empty;
        public string Operacion { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public bool Persistido { get; set; }
        public Guid CorrelationId { get; set; }
        public IReadOnlyList<ListaPreciosAjusteMasivoItemDto> Items { get; set; } = Array.Empty<ListaPreciosAjusteMasivoItemDto>();
    }

    public sealed class ListaPreciosCopiarListaRequest
    {
        public int? NivelOrigen { get; set; }
        public int? NivelDestino { get; set; }
        public string Modo { get; set; } = string.Empty;
        public bool? CopiarDescuentos { get; set; }
        public string Motivo { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;
        public Guid? UsuarioId { get; set; }
        public Guid? CorrelationId { get; set; }
    }

    public sealed class ListaPreciosCopiarListaItemDto
    {
        public byte TipoIdentidad { get; set; }
        public Guid IdProductoServicio { get; set; }
        public Guid? IdVariante { get; set; }
        public Guid? IdPresentacionVenta { get; set; }
        public string Accion { get; set; } = string.Empty;
        public string EstadoDestino { get; set; } = string.Empty;
        public bool Aceptado { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Motivo { get; set; } = string.Empty;
        public decimal PrecioOrigen { get; set; }
        public decimal? PrecioDestinoBefore { get; set; }
        public decimal PrecioAfter { get; set; }
        public decimal? DescuentoOrigen { get; set; }
        public decimal? DescuentoDestinoBefore { get; set; }
        public decimal? DescuentoAfter { get; set; }
        public byte RedondeoOrigen { get; set; }
        public byte? RedondeoDestinoBefore { get; set; }
        public byte RedondeoAfter { get; set; }
        public DateTime? VigenciaInicioOrigen { get; set; }
        public DateTime? VigenciaInicioDestinoBefore { get; set; }
        public DateTime? VigenciaInicioAfter { get; set; }
        public DateTime? VigenciaFinOrigen { get; set; }
        public DateTime? VigenciaFinDestinoBefore { get; set; }
        public DateTime? VigenciaFinAfter { get; set; }
    }

    public sealed class ListaPreciosCopiarListaResultadoDto
    {
        public int NivelOrigen { get; set; }
        public int NivelDestino { get; set; }
        public string Modo { get; set; } = string.Empty;
        public int TotalOrigenActivo { get; set; }
        public int Nuevos { get; set; }
        public int Reactivados { get; set; }
        public int Sobrescritos { get; set; }
        public int Omitidos { get; set; }
        public int Rechazados { get; set; }
        public bool Persistido { get; set; }
        public Guid CorrelationId { get; set; }
        public IReadOnlyList<ListaPreciosCopiarListaItemDto> Items { get; set; } = Array.Empty<ListaPreciosCopiarListaItemDto>();
    }

    public sealed class ListaPreciosDescuentoMarcaRequest
    {
        public Guid IdMarca { get; set; }
        public int? Nivel { get; set; }
        public decimal DescuentoPct { get; set; }
        public string Motivo { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;
        public Guid? UsuarioId { get; set; }
        public Guid? CorrelationId { get; set; }
    }

    public sealed class ListaPreciosDescuentoMarcaItemDto
    {
        public byte TipoIdentidad { get; set; }
        public Guid IdProductoServicio { get; set; }
        public Guid? IdVariante { get; set; }
        public Guid? IdPresentacionVenta { get; set; }
        public string Identidad { get; set; } = string.Empty;
        public string Accion { get; set; } = string.Empty;
        public decimal? PrecioListaBefore { get; set; }
        public decimal? DescuentoBefore { get; set; }
        public decimal? PrecioFinalBefore { get; set; }
        public decimal? DescuentoAfter { get; set; }
        public decimal? PrecioFinalAfter { get; set; }
        public bool Aceptado { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Motivo { get; set; } = string.Empty;
    }

    public sealed class ListaPreciosDescuentoMarcaResultadoDto
    {
        public Guid IdMarca { get; set; }
        public string Marca { get; set; } = string.Empty;
        public int Nivel { get; set; }
        public decimal DescuentoPct { get; set; }
        public int Evaluados { get; set; }
        public int Afectados { get; set; }
        public int Omitidos { get; set; }
        public int Rechazados { get; set; }
        public bool Persistido { get; set; }
        public Guid CorrelationId { get; set; }
        public IReadOnlyList<ListaPreciosDescuentoMarcaItemDto> Items { get; set; } = Array.Empty<ListaPreciosDescuentoMarcaItemDto>();
    }

    public sealed class ListaPreciosPrecioDto
    {
        public Guid Id { get; set; }
        public Guid IdListaPrecio { get; set; }
        public int Nivel { get; set; }
        public byte TipoIdentidad { get; set; }
        public Guid IdProductoServicio { get; set; }
        public Guid? IdVariante { get; set; }
        public Guid? IdPresentacionVenta { get; set; }
        public decimal Precio { get; set; }
        public decimal? DescuentoPct { get; set; }
        public byte RedondeoModo { get; set; }
        public DateTime? VigenciaInicio { get; set; }
        public DateTime? VigenciaFin { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaActualizacion { get; set; }
    }

    public sealed class ListaPreciosListaDto
    {
        public Guid Id { get; set; }
        public int Nivel { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public bool EsDefault { get; set; }
        public bool Activo { get; set; }
    }

    public sealed class ListaPreciosOperacionResponse
    {
        public string Mensaje { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public Guid? Id { get; set; }
    }

    public sealed class ListaPreciosConsultaRequest
    {
        public int? Nivel { get; set; }
        public string Busqueda { get; set; } = string.Empty;
        public byte? Tipo { get; set; }
        public Guid? IdCategoria { get; set; }
        public Guid? IdMarca { get; set; }
        public Guid? IdColeccion { get; set; }
        public Guid? IdEtiqueta { get; set; }
        public Guid? IdAtributo { get; set; }
        public IReadOnlyList<Guid> IdsValoresAtributo { get; set; } = Array.Empty<Guid>();
        public Guid? IdVariante { get; set; }
        public Guid? IdPresentacionVenta { get; set; }
        public decimal? PrecioMinimo { get; set; }
        public decimal? PrecioMaximo { get; set; }
        // LP-QA01: tri-state filter over the selected canonical list.
        // Empty = all, "con" = DescuentoPct > 0, "sin" = NULL/0.
        public string Descuento { get; set; } = string.Empty;
        // Kept for backwards compatibility with already deployed clients.
        public decimal? DescuentoPct { get; set; }
        public Guid? IdSucursal { get; set; }
        public IReadOnlyList<Guid> IdsSucursales { get; set; } = Array.Empty<Guid>();
        public string Existencia { get; set; } = string.Empty;
        public decimal? CantidadMenorA { get; set; }
        public string Estatus { get; set; } = "activos";
    }

    public sealed class ListaPreciosMatrizItemRequest
    {
        public int Nivel { get; set; }
        public decimal Precio { get; set; }
        public decimal? DescuentoPct { get; set; }
        public byte RedondeoModo { get; set; }
        public DateTime? VigenciaInicio { get; set; }
        public DateTime? VigenciaFin { get; set; }
    }

    public sealed class ListaPreciosMatrizRequest
    {
        public byte? TipoIdentidad { get; set; }
        public Guid IdProductoServicio { get; set; }
        public Guid? IdVariante { get; set; }
        public Guid? IdPresentacionVenta { get; set; }
        public IReadOnlyList<ListaPreciosMatrizItemRequest> Listas { get; set; } = Array.Empty<ListaPreciosMatrizItemRequest>();
        public string Motivo { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;
        public Guid? UsuarioId { get; set; }
        public Guid? CorrelationId { get; set; }
        public ListaPreciosComercialRequest? Comercial { get; set; }
    }

    public sealed class ListaPreciosComercialRequest
    {
        public bool Modificado { get; set; }
        public string? Descripcion { get; set; }
        public string? Web { get; set; }
        public string? Liverpool { get; set; }
        public string? MercadoLibre { get; set; }
        public string? Observaciones { get; set; }
        public bool DosPorUno { get; set; }
        public bool TresPorDos { get; set; }
        public bool DescuentoSegundo { get; set; }
        public bool Monedero { get; set; }
    }

    public sealed class ListaPreciosComercialDto
    {
        public string Descripcion { get; set; } = string.Empty;
        public bool DescripcionReadOnly { get; set; }
        public string Web { get; set; } = string.Empty;
        public string Liverpool { get; set; } = string.Empty;
        public string MercadoLibre { get; set; } = string.Empty;
        public string Observaciones { get; set; } = string.Empty;
        public bool DosPorUno { get; set; }
        public bool TresPorDos { get; set; }
        public bool DescuentoSegundo { get; set; }
        public bool Monedero { get; set; }
        public bool Materializada { get; set; }
    }

    public sealed class ListaPreciosEditorDto
    {
        public IReadOnlyList<ListaPreciosPrecioDto> Listas { get; set; } = Array.Empty<ListaPreciosPrecioDto>();
        public ListaPreciosComercialDto Comercial { get; set; } = new();
    }

    public sealed class ListaPreciosMatrizPreviewDto
    {
        public bool Persistido { get; set; }
        public Guid CorrelationId { get; set; }
        public IReadOnlyList<ListaPreciosResolutionResult> Listas { get; set; } = Array.Empty<ListaPreciosResolutionResult>();
    }

    public sealed record ListaPreciosComercialCommand(
        byte TipoProductoServicio,
        ListaPreciosIdentityKey Identity,
        string? Descripcion,
        string? Web,
        string? Liverpool,
        string? MercadoLibre,
        string? Observaciones,
        bool DosPorUno,
        bool TresPorDos,
        bool DescuentoSegundo,
        bool Monedero,
        Guid? UsuarioId,
        string Usuario,
        Guid CorrelationId);

    public sealed class ListaPreciosConsultaRowDto
    {
        public Guid IdProductoServicio { get; set; }
        public Guid? IdVariante { get; set; }
        public Guid? IdPresentacionVenta { get; set; }
        public byte TipoIdentidad { get; set; }
        public string TipoIdentidadNombre { get; set; } = string.Empty;
        public byte Tipo { get; set; }
        public string TipoNombre { get; set; } = string.Empty;
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string ProductoPadre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public Guid? IdCategoria { get; set; }
        public string Categoria { get; set; } = string.Empty;
        public Guid? IdMarca { get; set; }
        public string Marca { get; set; } = string.Empty;
        public Guid? IdColeccion { get; set; }
        public string Coleccion { get; set; } = string.Empty;
        public string IdentidadVendible { get; set; } = string.Empty;
        public string ImagenUrl { get; set; } = string.Empty;
        public string ImagenNombre { get; set; } = string.Empty;
        public string ImagenOrigen { get; set; } = string.Empty;
        public int Lista { get; set; }
        public Guid? IdDetallePrecio { get; set; }
        public decimal? PrecioLista { get; set; }
        public decimal? PrecioEfectivo { get; set; }
        public decimal? PrecioBase { get; set; }
        public decimal? Costo { get; set; }
        public decimal? MargenPct { get; set; }
        public string OrigenPrecio { get; set; } = string.Empty;
        public decimal? DescuentoPct { get; set; }
        public decimal? SubtotalAntesRedondeo { get; set; }
        public byte RedondeoModo { get; set; }
        public decimal? PrecioFinal { get; set; }
        public DateTime? VigenciaInicio { get; set; }
        public DateTime? VigenciaFin { get; set; }
        public string CodigoResolucion { get; set; } = string.Empty;
        public string Origen { get; set; } = string.Empty;
        public string OrigenNombre { get; set; } = string.Empty;
        public bool Resuelto { get; set; }
        public bool Activo { get; set; }
        public bool ProductoActivo { get; set; }
        public bool InventarioAplicable { get; set; }
        public decimal? Existencia { get; set; }
        public string Estatus { get; set; } = string.Empty;
        public decimal? P1 { get; set; }
        public decimal? D1 { get; set; }
        public decimal? F1 { get; set; }
        public decimal? P2 { get; set; }
        public decimal? D2 { get; set; }
        public decimal? F2 { get; set; }
        public decimal? P3 { get; set; }
        public decimal? D3 { get; set; }
        public decimal? F3 { get; set; }
        public decimal? P4 { get; set; }
        public decimal? D4 { get; set; }
        public decimal? F4 { get; set; }
        public decimal? P5 { get; set; }
        public decimal? D5 { get; set; }
        public decimal? F5 { get; set; }
        public decimal? P6 { get; set; }
        public decimal? D6 { get; set; }
        public decimal? F6 { get; set; }
        public decimal? P7 { get; set; }
        public decimal? D7 { get; set; }
        public decimal? F7 { get; set; }
        public decimal? P8 { get; set; }
        public decimal? D8 { get; set; }
        public decimal? F8 { get; set; }
        public decimal? P9 { get; set; }
        public decimal? D9 { get; set; }
        public decimal? F9 { get; set; }
        public decimal? P10 { get; set; }
        public decimal? D10 { get; set; }
        public decimal? F10 { get; set; }
    }

    public sealed class ListaPreciosComboItemDto
    {
        public Guid? Id { get; set; }
        public Guid? ParentId { get; set; }
        public string Clave { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
    }

    public sealed class ListaPreciosCombosDto
    {
        public IReadOnlyList<ListaPreciosComboItemDto> Listas { get; set; } = Array.Empty<ListaPreciosComboItemDto>();
        public IReadOnlyList<ListaPreciosComboItemDto> Tipos { get; set; } = Array.Empty<ListaPreciosComboItemDto>();
        public IReadOnlyList<ListaPreciosComboItemDto> Categorias { get; set; } = Array.Empty<ListaPreciosComboItemDto>();
        public IReadOnlyList<ListaPreciosComboItemDto> Marcas { get; set; } = Array.Empty<ListaPreciosComboItemDto>();
        public IReadOnlyList<ListaPreciosComboItemDto> Colecciones { get; set; } = Array.Empty<ListaPreciosComboItemDto>();
        public IReadOnlyList<ListaPreciosComboItemDto> Etiquetas { get; set; } = Array.Empty<ListaPreciosComboItemDto>();
        public IReadOnlyList<ListaPreciosComboItemDto> Atributos { get; set; } = Array.Empty<ListaPreciosComboItemDto>();
        public IReadOnlyList<ListaPreciosComboItemDto> ValoresAtributos { get; set; } = Array.Empty<ListaPreciosComboItemDto>();
        public IReadOnlyList<ListaPreciosComboItemDto> Variantes { get; set; } = Array.Empty<ListaPreciosComboItemDto>();
        public IReadOnlyList<ListaPreciosComboItemDto> PresentacionesVenta { get; set; } = Array.Empty<ListaPreciosComboItemDto>();
        public IReadOnlyList<ListaPreciosComboItemDto> Sucursales { get; set; } = Array.Empty<ListaPreciosComboItemDto>();
        public IReadOnlyList<ListaPreciosComboItemDto> Estatus { get; set; } = Array.Empty<ListaPreciosComboItemDto>();
    }

    public sealed class ListaPreciosInventarioDetalleDto
    {
        public bool Aplicable { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public decimal? ExistenciaTotal { get; set; }
        public IReadOnlyList<ListaPreciosInventarioSaldoDto> Saldos { get; set; } = Array.Empty<ListaPreciosInventarioSaldoDto>();
        public IReadOnlyList<ListaPreciosInventarioMovimientoDto> Movimientos { get; set; } = Array.Empty<ListaPreciosInventarioMovimientoDto>();
    }

    public sealed record ListaPreciosInventarioSaldoDto(Guid IdSucursal, string Sucursal, decimal Existencia);
    public sealed record ListaPreciosInventarioMovimientoDto(
        Guid Id,
        Guid IdSucursal,
        string Sucursal,
        byte TipoMovimiento,
        string TipoMovimientoNombre,
        decimal Cantidad,
        decimal SaldoAnterior,
        decimal SaldoPosterior,
        string Referencia,
        string Observaciones,
        DateTime FechaMovimiento);

    public sealed record ListaPreciosProductoRecord(Guid Id, byte Tipo, bool Activo, decimal PrecioPublico, bool CausaInventario = true);
    public sealed record ListaPreciosVarianteRecord(Guid Id, Guid IdProductoServicio, bool Activo, decimal? PrecioPublico);
    public sealed record ListaPreciosPresentacionVentaRecord(Guid Id, Guid IdProductoServicio, bool Activo, decimal Precio);
    public sealed record ListaPreciosListaRecord(Guid Id, int Nivel, string Nombre, bool EsDefault, bool Activo);
    public sealed record ListaPreciosDetalleRecord(
        Guid Id,
        Guid IdListaPrecio,
        int Nivel,
        byte TipoIdentidad,
        Guid IdProductoServicio,
        Guid? IdVariante,
        Guid? IdPresentacionVenta,
        decimal Precio,
        decimal? DescuentoPct,
        byte RedondeoModo,
        DateTime? VigenciaInicio,
        DateTime? VigenciaFin,
        bool Activo,
        DateTime FechaActualizacion);
    public sealed record ListaPreciosHistorialDto(
        Guid Id,
        Guid IdEmpresa,
        Guid? IdListaPrecio,
        int? Nivel,
        byte? TipoIdentidad,
        Guid? IdProductoServicio,
        Guid? IdVariante,
        Guid? IdPresentacionVenta,
        string Campo,
        string Operacion,
        string ValorAnterior,
        string ValorNuevo,
        Guid? IdUsuario,
        string Usuario,
        string Origen,
        Guid CorrelationId,
        string Motivo,
        DateTime FechaUtc);

    public sealed class ListaPreciosHistorialConsultaRequest
    {
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
        public int? Nivel { get; set; }
        public byte? TipoIdentidad { get; set; }
        public string Busqueda { get; set; } = string.Empty;
        public string Origen { get; set; } = string.Empty;
        public string Operacion { get; set; } = string.Empty;
        public Guid? CorrelationId { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public int Pagina { get; set; } = 1;
        public int TamanoPagina { get; set; } = 25;
    }

    public sealed record ListaPreciosHistorialConsultaItemDto(
        Guid Id,
        DateTime FechaUtc,
        int? Nivel,
        string Lista,
        byte? TipoIdentidad,
        string TipoIdentidadNombre,
        string Codigo,
        string Identidad,
        string Campo,
        string Operacion,
        string ValorAnterior,
        string ValorNuevo,
        string Usuario,
        string Origen,
        Guid CorrelationId,
        string Motivo);

    public sealed class ListaPreciosHistorialPaginaDto
    {
        public IReadOnlyList<ListaPreciosHistorialConsultaItemDto> Items { get; set; } = Array.Empty<ListaPreciosHistorialConsultaItemDto>();
        public int Pagina { get; set; }
        public int TamanoPagina { get; set; }
        public int Total { get; set; }
        public int TotalPaginas { get; set; }
    }
    public sealed record ListaPreciosConsultaIdentityRecord(
        Guid IdProductoServicio,
        Guid? IdVariante,
        Guid? IdPresentacionVenta,
        byte TipoIdentidad,
        string TipoIdentidadNombre,
        byte Tipo,
        string TipoNombre,
        string Codigo,
        string Nombre,
        string ProductoPadre,
        Guid? IdCategoria,
        string Categoria,
        Guid? IdMarca,
        string Marca,
        Guid? IdColeccion,
        string Coleccion,
        string IdentidadVendible,
        string ImagenUrl,
        string ImagenNombre,
        string ImagenOrigen,
        bool Activo,
        bool ProductoActivo,
        bool InventarioAplicable,
        decimal? Existencia,
        decimal PrecioBase = 0m,
        decimal? Costo = null,
        Guid? IdDetalleSeleccionado = null,
        decimal? PrecioSeleccionado = null,
        decimal? DescuentoSeleccionado = null,
        byte RedondeoSeleccionado = 0,
        DateTime? VigenciaInicioSeleccionada = null,
        DateTime? VigenciaFinSeleccionada = null,
        decimal? P1 = null, decimal? D1 = null, byte R1 = 0,
        decimal? P2 = null, decimal? D2 = null, byte R2 = 0,
        decimal? P3 = null, decimal? D3 = null, byte R3 = 0,
        decimal? P4 = null, decimal? D4 = null, byte R4 = 0,
        decimal? P5 = null, decimal? D5 = null, byte R5 = 0,
        decimal? P6 = null, decimal? D6 = null, byte R6 = 0,
        decimal? P7 = null, decimal? D7 = null, byte R7 = 0,
        decimal? P8 = null, decimal? D8 = null, byte R8 = 0,
        decimal? P9 = null, decimal? D9 = null, byte R9 = 0,
        decimal? P10 = null, decimal? D10 = null, byte R10 = 0,
        string Descripcion = "");
}
