using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace checklistWs.Services.Tenant
{
    public interface IListaPreciosRepository
    {
        Task<ListaPreciosProductoRecord?> ObtenerProductoAsync(Guid idEmpresa, Guid idProductoServicio, CancellationToken cancellationToken = default);
        Task<ListaPreciosVarianteRecord?> ObtenerVarianteAsync(Guid idEmpresa, Guid idVariante, CancellationToken cancellationToken = default);
        Task<ListaPreciosPresentacionVentaRecord?> ObtenerPresentacionVentaAsync(Guid idEmpresa, Guid idPresentacionVenta, CancellationToken cancellationToken = default);
        Task<ListaPreciosListaRecord?> ObtenerListaPorNivelAsync(Guid idEmpresa, int nivel, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ListaPreciosListaDto>> ObtenerListasAsync(Guid idEmpresa, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ListaPreciosComboItemDto>> ObtenerCategoriasAsync(Guid idEmpresa, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ListaPreciosComboItemDto>> ObtenerMarcasAsync(Guid idEmpresa, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ListaPreciosComboItemDto>> ObtenerColeccionesAsync(Guid idEmpresa, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ListaPreciosComboItemDto>> ObtenerEtiquetasAsync(Guid idEmpresa, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ListaPreciosComboItemDto>> ObtenerAtributosAsync(Guid idEmpresa, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ListaPreciosComboItemDto>> ObtenerValoresAtributosAsync(Guid idEmpresa, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ListaPreciosComboItemDto>> ObtenerVariantesAsync(Guid idEmpresa, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ListaPreciosComboItemDto>> ObtenerPresentacionesVentaAsync(Guid idEmpresa, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ListaPreciosComboItemDto>> ObtenerSucursalesAsync(Guid idEmpresa, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ListaPreciosConsultaIdentityRecord>> ConsultarIdentidadesAsync(Guid idEmpresa, ListaPreciosConsultaRequest request, CancellationToken cancellationToken = default);
        Task<ListaPreciosInventarioDetalleDto> ObtenerInventarioDetalleAsync(Guid idEmpresa, ListaPreciosIdentityKey identity, CancellationToken cancellationToken = default);
        Task<ListaPreciosDetalleRecord?> ObtenerPrecioActivoAsync(Guid idEmpresa, Guid idListaPrecio, ListaPreciosIdentityKey identity, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ListaPreciosDetalleRecord>> ObtenerDetallesPorNivelAsync(Guid idEmpresa, int nivel, bool soloActivos, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ListaPreciosPrecioDto>> ObtenerPreciosPorProductoAsync(Guid idEmpresa, Guid idProductoServicio, CancellationToken cancellationToken = default);
        Task<ListaPreciosComercialDto> ObtenerComercialAsync(Guid idEmpresa, ListaPreciosIdentityKey identity, CancellationToken cancellationToken = default);
        Task<Guid> EnsureListaAsync(Guid idEmpresa, int nivel, Guid? usuarioId, CancellationToken cancellationToken = default);
        Task<Guid> GuardarPrecioAsync(Guid idEmpresa, ListaPreciosGuardarPrecioCommand command, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Guid>> GuardarPreciosMasivosAsync(Guid idEmpresa, IReadOnlyList<ListaPreciosGuardarPrecioCommand> commands, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Guid>> GuardarMatrizConComercialAsync(Guid idEmpresa, IReadOnlyList<ListaPreciosGuardarPrecioCommand> commands, ListaPreciosComercialCommand? comercial, CancellationToken cancellationToken = default);
        Task BajaPrecioAsync(Guid idEmpresa, Guid idPrecio, Guid? usuarioId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ListaPreciosHistorialDto>> ObtenerHistorialAsync(Guid idEmpresa, ListaPreciosIdentityKey identity, int? nivel, CancellationToken cancellationToken = default);
        Task<ListaPreciosHistorialPaginaDto> ConsultarHistorialAsync(Guid idEmpresa, ListaPreciosHistorialConsultaRequest request, CancellationToken cancellationToken = default);
    }

    public interface IListaPreciosService
    {
        Task<ListaPreciosResolutionResult> ResolverPrecioAsync(Guid idEmpresa, ListaPreciosResolverRequest request, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ListaPreciosListaDto>> ObtenerListasAsync(Guid idEmpresa, CancellationToken cancellationToken = default);
        Task<ListaPreciosCombosDto> ObtenerCombosAsync(Guid idEmpresa, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ListaPreciosConsultaRowDto>> ConsultarAsync(Guid idEmpresa, ListaPreciosConsultaRequest request, CancellationToken cancellationToken = default);
        Task<ListaPreciosInventarioDetalleDto> ObtenerInventarioDetalleAsync(Guid idEmpresa, ListaPreciosResolverRequest request, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ListaPreciosPrecioDto>> ObtenerPreciosPorProductoAsync(Guid idEmpresa, Guid idProductoServicio, CancellationToken cancellationToken = default);
        Task<ListaPreciosEditorDto> ObtenerEditorAsync(Guid idEmpresa, ListaPreciosResolverRequest request, CancellationToken cancellationToken = default);
        Task<Guid> EnsureListaAsync(Guid idEmpresa, int nivel, Guid? usuarioId, CancellationToken cancellationToken = default);
        Task<Guid> GuardarPrecioAsync(Guid idEmpresa, ListaPreciosGuardarPrecioRequest request, CancellationToken cancellationToken = default);
        Task BajaPrecioAsync(Guid idEmpresa, Guid idPrecio, Guid? usuarioId, CancellationToken cancellationToken = default);
        Task<ListaPreciosResolutionResult> PreviewAsync(Guid idEmpresa, ListaPreciosPreviewRequest request, CancellationToken cancellationToken = default);
        Task<ListaPreciosMatrizPreviewDto> PreviewMatrizAsync(Guid idEmpresa, ListaPreciosMatrizRequest request, CancellationToken cancellationToken = default);
        Task<ListaPreciosMatrizPreviewDto> GuardarMatrizAsync(Guid idEmpresa, ListaPreciosMatrizRequest request, CancellationToken cancellationToken = default);
        Task<ListaPreciosAjusteMasivoResultadoDto> PreviewAjusteMasivoAsync(Guid idEmpresa, ListaPreciosAjusteMasivoRequest request, CancellationToken cancellationToken = default);
        Task<ListaPreciosAjusteMasivoResultadoDto> EjecutarAjusteMasivoAsync(Guid idEmpresa, ListaPreciosAjusteMasivoRequest request, CancellationToken cancellationToken = default);
        Task<ListaPreciosCopiarListaResultadoDto> PreviewCopiarListaAsync(Guid idEmpresa, ListaPreciosCopiarListaRequest request, CancellationToken cancellationToken = default);
        Task<ListaPreciosCopiarListaResultadoDto> EjecutarCopiarListaAsync(Guid idEmpresa, ListaPreciosCopiarListaRequest request, CancellationToken cancellationToken = default);
        Task<ListaPreciosDescuentoMarcaResultadoDto> PreviewDescuentoMarcaAsync(Guid idEmpresa, ListaPreciosDescuentoMarcaRequest request, CancellationToken cancellationToken = default);
        Task<ListaPreciosDescuentoMarcaResultadoDto> EjecutarDescuentoMarcaAsync(Guid idEmpresa, ListaPreciosDescuentoMarcaRequest request, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ListaPreciosHistorialDto>> ObtenerHistorialAsync(Guid idEmpresa, ListaPreciosResolverRequest request, CancellationToken cancellationToken = default);
        Task<ListaPreciosHistorialPaginaDto> ConsultarHistorialAsync(Guid idEmpresa, ListaPreciosHistorialConsultaRequest request, CancellationToken cancellationToken = default);
    }

    public sealed record ListaPreciosIdentityKey(byte TipoIdentidad, Guid IdProductoServicio, Guid? IdVariante, Guid? IdPresentacionVenta);
    public sealed record ListaPreciosGuardarPrecioCommand(
        int Nivel,
        byte TipoProductoServicio,
        ListaPreciosIdentityKey Identity,
        decimal Precio,
        decimal? DescuentoPct,
        byte RedondeoModo,
        DateTime? VigenciaInicio,
        DateTime? VigenciaFin,
        Guid? UsuarioId,
        string Usuario,
        string Motivo,
        Guid CorrelationId,
        string Origen,
        string OperacionHistorial = "");

    public static class ListaPreciosHistorialPolicy
    {
        public static bool RequiresReactivationHistory(ListaPreciosDetalleRecord previous, ListaPreciosGuardarPrecioCommand command)
        {
            if (previous.Activo)
            {
                return false;
            }

            return FormatDecimal(previous.Precio) == FormatDecimal(command.Precio)
                && FormatDecimal(previous.DescuentoPct) == FormatDecimal(command.DescuentoPct)
                && previous.RedondeoModo == command.RedondeoModo
                && FormatDate(previous.VigenciaInicio) == FormatDate(command.VigenciaInicio)
                && FormatDate(previous.VigenciaFin) == FormatDate(command.VigenciaFin);
        }

        private static string? FormatDecimal(decimal? value)
            => value?.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

        private static string? FormatDate(DateTime? value)
            => value?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
    }

    public enum ListaPreciosHeaderAction
    {
        Reuse,
        Reactivate,
        Create
    }

    public sealed record ListaPreciosHeaderCandidate(
        Guid Id,
        Guid IdEmpresa,
        int Nivel,
        bool Activo,
        DateTime? FechaArchivado);

    public sealed record ListaPreciosHeaderDecision(ListaPreciosHeaderAction Action, Guid? Id);

    public static class ListaPreciosHeaderPolicy
    {
        public const string InconsistentHeader = "CABECERA_LISTA_INCONSISTENTE";

        public static ListaPreciosHeaderDecision Resolve(
            Guid idEmpresa,
            int nivel,
            IReadOnlyCollection<ListaPreciosHeaderCandidate> candidates)
        {
            if (idEmpresa == Guid.Empty || nivel < ListaPreciosConstants.NivelMinimo || nivel > ListaPreciosConstants.NivelMaximo)
            {
                throw new InvalidOperationException(ListaPreciosResolutionCodes.ListaInvalida);
            }

            List<ListaPreciosHeaderCandidate> matches = (candidates ?? Array.Empty<ListaPreciosHeaderCandidate>())
                .Where(candidate => candidate.IdEmpresa == idEmpresa && candidate.Nivel == nivel)
                .ToList();
            if (matches.Count == 0)
            {
                return new ListaPreciosHeaderDecision(ListaPreciosHeaderAction.Create, null);
            }

            if (matches.Count != 1)
            {
                throw new InvalidOperationException(InconsistentHeader);
            }

            ListaPreciosHeaderCandidate match = matches[0];
            if (match.Activo && !match.FechaArchivado.HasValue)
            {
                return new ListaPreciosHeaderDecision(ListaPreciosHeaderAction.Reuse, match.Id);
            }

            if (!match.Activo && match.FechaArchivado.HasValue)
            {
                return new ListaPreciosHeaderDecision(ListaPreciosHeaderAction.Reactivate, match.Id);
            }

            throw new InvalidOperationException(InconsistentHeader);
        }
    }

    public sealed class ListaPreciosService : IListaPreciosService
    {
        private readonly IListaPreciosRepository _repository;

        public ListaPreciosService(IListaPreciosRepository repository)
        {
            _repository = repository;
        }

        public Task<IReadOnlyList<ListaPreciosListaDto>> ObtenerListasAsync(Guid idEmpresa, CancellationToken cancellationToken = default)
        {
            return _repository.ObtenerListasAsync(idEmpresa, cancellationToken);
        }

        public async Task<ListaPreciosCombosDto> ObtenerCombosAsync(Guid idEmpresa, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<ListaPreciosListaDto> listas = await _repository.ObtenerListasAsync(idEmpresa, cancellationToken);
            Dictionary<int, ListaPreciosListaDto> listasExistentes = listas.ToDictionary(x => x.Nivel);
            List<ListaPreciosComboItemDto> listasUi = Enumerable.Range(ListaPreciosConstants.NivelMinimo, ListaPreciosConstants.NivelMaximo)
                .Select(nivel => new ListaPreciosComboItemDto
                {
                    Id = listasExistentes.TryGetValue(nivel, out ListaPreciosListaDto? lista) ? lista.Id : null,
                    Clave = nivel.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    Nombre = lista?.Nombre ?? $"Lista {nivel}"
                })
                .ToList();

            return new ListaPreciosCombosDto
            {
                Listas = listasUi,
                Tipos = new[]
                {
                    new ListaPreciosComboItemDto { Clave = string.Empty, Nombre = "Todos" },
                    new ListaPreciosComboItemDto { Clave = ListaPreciosConstants.TipoProducto.ToString(System.Globalization.CultureInfo.InvariantCulture), Nombre = "Producto" },
                    new ListaPreciosComboItemDto { Clave = ListaPreciosConstants.TipoServicio.ToString(System.Globalization.CultureInfo.InvariantCulture), Nombre = "Servicio" }
                },
                Categorias = await _repository.ObtenerCategoriasAsync(idEmpresa, cancellationToken),
                Marcas = await _repository.ObtenerMarcasAsync(idEmpresa, cancellationToken),
                Colecciones = await _repository.ObtenerColeccionesAsync(idEmpresa, cancellationToken),
                Etiquetas = await _repository.ObtenerEtiquetasAsync(idEmpresa, cancellationToken),
                Atributos = await _repository.ObtenerAtributosAsync(idEmpresa, cancellationToken),
                ValoresAtributos = await _repository.ObtenerValoresAtributosAsync(idEmpresa, cancellationToken),
                Variantes = await _repository.ObtenerVariantesAsync(idEmpresa, cancellationToken),
                PresentacionesVenta = await _repository.ObtenerPresentacionesVentaAsync(idEmpresa, cancellationToken),
                Sucursales = await _repository.ObtenerSucursalesAsync(idEmpresa, cancellationToken),
                Estatus = new[]
                {
                    new ListaPreciosComboItemDto { Clave = "activos", Nombre = "Activos" },
                    new ListaPreciosComboItemDto { Clave = "inactivos", Nombre = "Inactivos" },
                    new ListaPreciosComboItemDto { Clave = "todos", Nombre = "Todos" }
                }
            };
        }

        public async Task<IReadOnlyList<ListaPreciosConsultaRowDto>> ConsultarAsync(Guid idEmpresa, ListaPreciosConsultaRequest request, CancellationToken cancellationToken = default)
        {
            request ??= new ListaPreciosConsultaRequest();
            if (!TryGetValidLevel(request.Nivel, out int validLevel))
            {
                throw new InvalidOperationException(ListaPreciosResolutionCodes.ListaInvalida);
            }

            ValidateConsultaInput(request);
            request.Nivel = validLevel;
            IReadOnlyList<ListaPreciosConsultaIdentityRecord> identities = await _repository.ConsultarIdentidadesAsync(idEmpresa, request, cancellationToken);
            List<ListaPreciosConsultaRowDto> result = new();

            foreach (ListaPreciosConsultaIdentityRecord identity in identities)
            {
                // LP-QA01: the identity, selected-list detail and P1..P10 matrix are
                // projected by one tenant-scoped SQL query. Do not resolve once per row.
                bool selectedIsCurrent = identity.IdDetalleSeleccionado.HasValue
                    && (!identity.VigenciaInicioSeleccionada.HasValue || identity.VigenciaInicioSeleccionada.Value.Date <= DateTime.UtcNow.Date)
                    && (!identity.VigenciaFinSeleccionada.HasValue || identity.VigenciaFinSeleccionada.Value.Date >= DateTime.UtcNow.Date);
                decimal reference = selectedIsCurrent ? identity.PrecioSeleccionado!.Value : identity.PrecioBase;
                ListaPreciosResolutionResult resolution = BuildResolvedResult(
                    idEmpresa,
                    IdentityValidation.Pass(
                        new ListaPreciosIdentityKey(identity.TipoIdentidad, identity.IdProductoServicio, identity.IdVariante, identity.IdPresentacionVenta),
                        new ListaPreciosProductoRecord(identity.IdProductoServicio, identity.Tipo, identity.ProductoActivo, identity.PrecioBase, identity.InventarioAplicable),
                        null,
                        null),
                    validLevel,
                    validLevel,
                    null,
                    selectedIsCurrent ? identity.IdDetalleSeleccionado : null,
                    reference,
                    selectedIsCurrent ? identity.PrecioSeleccionado : null,
                    selectedIsCurrent ? ListaPreciosResolutionCodes.PrecioLista : ListaPreciosResolutionCodes.FallbackPrecioPublico,
                    selectedIsCurrent ? ListaPreciosResolutionCodes.PrecioLista : ListaPreciosResolutionCodes.FallbackPrecioPublico,
                    selectedIsCurrent ? identity.DescuentoSeleccionado : null,
                    selectedIsCurrent ? identity.RedondeoSeleccionado : ListaPreciosConstants.RedondeoSinRedondeo,
                    selectedIsCurrent ? identity.VigenciaInicioSeleccionada : null,
                    selectedIsCurrent ? identity.VigenciaFinSeleccionada : null,
                    selectedIsCurrent ? "Precio configurado." : "Precio base por fallback.");

                bool precioLista = string.Equals(resolution.CodigoResolucion, ListaPreciosResolutionCodes.PrecioLista, StringComparison.OrdinalIgnoreCase);
                ListaPreciosConsultaRowDto row = new()
                {
                    IdProductoServicio = identity.IdProductoServicio,
                    IdVariante = identity.IdVariante,
                    IdPresentacionVenta = identity.IdPresentacionVenta,
                    TipoIdentidad = identity.TipoIdentidad,
                    TipoIdentidadNombre = identity.TipoIdentidadNombre,
                    Tipo = identity.Tipo,
                    TipoNombre = identity.TipoNombre,
                    Codigo = identity.Codigo,
                    Nombre = identity.Nombre,
                    ProductoPadre = identity.ProductoPadre,
                    Descripcion = identity.Descripcion,
                    IdCategoria = identity.IdCategoria,
                    Categoria = identity.Categoria,
                    IdMarca = identity.IdMarca,
                    Marca = identity.Marca,
                    IdColeccion = identity.IdColeccion,
                    Coleccion = identity.Coleccion,
                    IdentidadVendible = identity.IdentidadVendible,
                    ImagenUrl = identity.ImagenUrl,
                    ImagenNombre = identity.ImagenNombre,
                    ImagenOrigen = identity.ImagenOrigen,
                    Lista = resolution.ListaEfectiva,
                    IdDetallePrecio = resolution.IdDetallePrecio,
                    PrecioLista = resolution.PrecioLista,
                    PrecioEfectivo = resolution.PrecioEfectivo,
                    PrecioBase = resolution.PrecioBase,
                    Costo = identity.Costo,
                    MargenPct = identity.Costo.HasValue && resolution.PrecioFinal.HasValue && resolution.PrecioFinal.Value > 0m
                        ? decimal.Round((resolution.PrecioFinal.Value - identity.Costo.Value) / resolution.PrecioFinal.Value * 100m, 2, MidpointRounding.AwayFromZero)
                        : null,
                    OrigenPrecio = resolution.OrigenPrecio,
                    DescuentoPct = resolution.DescuentoPct,
                    SubtotalAntesRedondeo = resolution.SubtotalAntesRedondeo,
                    RedondeoModo = resolution.RedondeoModo,
                    PrecioFinal = resolution.PrecioFinal,
                    VigenciaInicio = resolution.VigenciaInicio,
                    VigenciaFin = resolution.VigenciaFin,
                    CodigoResolucion = resolution.CodigoResolucion,
                    Origen = resolution.Origen,
                    OrigenNombre = precioLista ? "Precio de lista" : "Precio base",
                    Resuelto = resolution.Resuelto,
                    Activo = identity.Activo,
                    ProductoActivo = identity.ProductoActivo,
                    InventarioAplicable = identity.InventarioAplicable,
                    Existencia = identity.Existencia,
                    Estatus = identity.Activo && identity.ProductoActivo ? "Activo" : "Inactivo"
                    ,P1 = identity.P1, D1 = identity.D1, F1 = CalculateMatrixFinal(identity.P1, identity.D1, identity.R1)
                    ,P2 = identity.P2, D2 = identity.D2, F2 = CalculateMatrixFinal(identity.P2, identity.D2, identity.R2)
                    ,P3 = identity.P3, D3 = identity.D3, F3 = CalculateMatrixFinal(identity.P3, identity.D3, identity.R3)
                    ,P4 = identity.P4, D4 = identity.D4, F4 = CalculateMatrixFinal(identity.P4, identity.D4, identity.R4)
                    ,P5 = identity.P5, D5 = identity.D5, F5 = CalculateMatrixFinal(identity.P5, identity.D5, identity.R5)
                    ,P6 = identity.P6, D6 = identity.D6, F6 = CalculateMatrixFinal(identity.P6, identity.D6, identity.R6)
                    ,P7 = identity.P7, D7 = identity.D7, F7 = CalculateMatrixFinal(identity.P7, identity.D7, identity.R7)
                    ,P8 = identity.P8, D8 = identity.D8, F8 = CalculateMatrixFinal(identity.P8, identity.D8, identity.R8)
                    ,P9 = identity.P9, D9 = identity.D9, F9 = CalculateMatrixFinal(identity.P9, identity.D9, identity.R9)
                    ,P10 = identity.P10, D10 = identity.D10, F10 = CalculateMatrixFinal(identity.P10, identity.D10, identity.R10)
                };

                if (MatchesCommercialFilters(row, request))
                {
                    result.Add(row);
                }
            }

            return result;
        }

        public async Task<ListaPreciosMatrizPreviewDto> PreviewMatrizAsync(Guid idEmpresa, ListaPreciosMatrizRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null || request.Listas == null || request.Listas.Count < 1 || request.Listas.Count > 10 ||
                request.Listas.Select(x => x.Nivel).Distinct().Count() != request.Listas.Count ||
                request.Listas.Any(x => x.Nivel < ListaPreciosConstants.NivelMinimo || x.Nivel > ListaPreciosConstants.NivelMaximo))
            {
                throw new InvalidOperationException("MATRIZ_LISTAS_INVALIDA");
            }

            Guid correlationId = request.CorrelationId.GetValueOrDefault(Guid.NewGuid());
            List<ListaPreciosResolutionResult> previews = new(10);
            foreach (ListaPreciosMatrizItemRequest item in request.Listas.OrderBy(x => x.Nivel))
            {
                previews.Add(await PreviewAsync(idEmpresa, new ListaPreciosPreviewRequest
                {
                    Nivel = item.Nivel,
                    TipoIdentidad = request.TipoIdentidad,
                    IdProductoServicio = request.IdProductoServicio,
                    IdVariante = request.IdVariante,
                    IdPresentacionVenta = request.IdPresentacionVenta,
                    Precio = item.Precio,
                    DescuentoPct = item.DescuentoPct,
                    RedondeoModo = item.RedondeoModo,
                    VigenciaInicio = item.VigenciaInicio,
                    VigenciaFin = item.VigenciaFin,
                    Motivo = request.Motivo,
                    Usuario = request.Usuario,
                    UsuarioId = request.UsuarioId,
                    CorrelationId = correlationId
                }, cancellationToken));
            }

            return new ListaPreciosMatrizPreviewDto { Persistido = false, CorrelationId = correlationId, Listas = previews };
        }

        public async Task<ListaPreciosMatrizPreviewDto> GuardarMatrizAsync(Guid idEmpresa, ListaPreciosMatrizRequest request, CancellationToken cancellationToken = default)
        {
            if ((request.Listas == null || request.Listas.Count == 0) && request.Comercial?.Modificado != true)
                throw new InvalidOperationException("SIN_CAMBIOS");
            ListaPreciosMatrizPreviewDto preview = request.Listas != null && request.Listas.Count > 0
                ? await PreviewMatrizAsync(idEmpresa, request, cancellationToken)
                : new ListaPreciosMatrizPreviewDto { CorrelationId = request.CorrelationId.GetValueOrDefault(Guid.NewGuid()), Listas = Array.Empty<ListaPreciosResolutionResult>() };
            Guid correlationId = preview.CorrelationId;
            IdentityValidation validation = await ValidateIdentityAsync(idEmpresa, new ListaPreciosResolverRequest
            {
                TipoIdentidad = request.TipoIdentidad,
                IdProductoServicio = request.IdProductoServicio,
                IdVariante = request.IdVariante,
                IdPresentacionVenta = request.IdPresentacionVenta,
                RequiereActivo = true
            }, cancellationToken);
            if (!validation.IsValid) throw new InvalidOperationException(validation.Code);
            List<ListaPreciosGuardarPrecioCommand> commands = (request.Listas ?? Array.Empty<ListaPreciosMatrizItemRequest>()).OrderBy(x => x.Nivel).Select(item =>
                new ListaPreciosGuardarPrecioCommand(
                    item.Nivel,
                    validation.Product!.Tipo,
                    validation.Identity!,
                    decimal.Round(item.Precio, 2, MidpointRounding.AwayFromZero),
                    item.DescuentoPct.HasValue ? decimal.Round(item.DescuentoPct.Value, 2, MidpointRounding.AwayFromZero) : null,
                    item.RedondeoModo,
                    item.VigenciaInicio,
                    item.VigenciaFin,
                    request.UsuarioId,
                    request.Usuario,
                    request.Motivo,
                    correlationId,
                    ListaPreciosConstants.OrigenHistorialIndividual)).ToList();
            ListaPreciosComercialCommand? comercial = null;
            if (request.Comercial?.Modificado == true)
            {
                ListaPreciosComercialRequest value = request.Comercial;
                if ((value.Web?.Trim().Length ?? 0) > 250) throw new InvalidOperationException("WEB_LONGITUD_INVALIDA");
                if (validation.Identity!.TipoIdentidad is ListaPreciosConstants.IdentidadVariante or ListaPreciosConstants.IdentidadPresentacionVenta)
                {
                    ListaPreciosComercialDto current = await _repository.ObtenerComercialAsync(idEmpresa, validation.Identity, cancellationToken);
                    if (!string.Equals(NormalizeText(value.Descripcion), NormalizeText(current.Descripcion), StringComparison.Ordinal))
                        throw new InvalidOperationException("DESCRIPCION_SOLO_LECTURA");
                }
                comercial = new ListaPreciosComercialCommand(validation.Product!.Tipo, validation.Identity!, NormalizeText(value.Descripcion), NormalizeText(value.Web), NormalizeText(value.Liverpool), NormalizeText(value.MercadoLibre), NormalizeText(value.Observaciones), value.DosPorUno, value.TresPorDos, value.DescuentoSegundo, value.Monedero, request.UsuarioId, request.Usuario, correlationId);
            }
            await _repository.GuardarMatrizConComercialAsync(idEmpresa, commands, comercial, cancellationToken);
            preview.Persistido = true;
            return preview;
        }

        public async Task<ListaPreciosEditorDto> ObtenerEditorAsync(Guid idEmpresa, ListaPreciosResolverRequest request, CancellationToken cancellationToken = default)
        {
            IdentityValidation validation = await ValidateIdentityAsync(idEmpresa, request, cancellationToken);
            if (!validation.IsValid) throw new InvalidOperationException(validation.Code);
            return new ListaPreciosEditorDto
            {
                Listas = await _repository.ObtenerPreciosPorProductoAsync(idEmpresa, request.IdProductoServicio, cancellationToken),
                Comercial = await _repository.ObtenerComercialAsync(idEmpresa, validation.Identity!, cancellationToken)
            };
        }

        private static string? NormalizeText(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        public async Task<ListaPreciosInventarioDetalleDto> ObtenerInventarioDetalleAsync(Guid idEmpresa, ListaPreciosResolverRequest request, CancellationToken cancellationToken = default)
        {
            IdentityValidation validation = await ValidateIdentityAsync(idEmpresa, request, cancellationToken);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException(validation.Code);
            }

            if (validation.Product!.Tipo != ListaPreciosConstants.TipoProducto || !validation.Product.CausaInventario)
            {
                return new ListaPreciosInventarioDetalleDto { Aplicable = false, Mensaje = "Inventario no aplica para esta identidad." };
            }

            return await _repository.ObtenerInventarioDetalleAsync(idEmpresa, validation.Identity!, cancellationToken);
        }

        public Task<IReadOnlyList<ListaPreciosPrecioDto>> ObtenerPreciosPorProductoAsync(Guid idEmpresa, Guid idProductoServicio, CancellationToken cancellationToken = default)
        {
            if (idEmpresa == Guid.Empty || idProductoServicio == Guid.Empty)
            {
                throw new InvalidOperationException("IDENTIDAD_INVALIDA");
            }

            return _repository.ObtenerPreciosPorProductoAsync(idEmpresa, idProductoServicio, cancellationToken);
        }

        public async Task<ListaPreciosResolutionResult> ResolverPrecioAsync(Guid idEmpresa, ListaPreciosResolverRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null || idEmpresa == Guid.Empty || request.IdProductoServicio == Guid.Empty)
            {
                return Invalid(request, ListaPreciosResolutionCodes.IdentidadInvalida, "La identidad solicitada no es válida.");
            }

            int requestedLevel = request.Nivel.GetValueOrDefault(ListaPreciosConstants.NivelDefault);
            if (!TryGetValidLevel(request.Nivel, out int effectiveLevel))
            {
                return Invalid(request, ListaPreciosResolutionCodes.ListaInvalida, "La lista solicitada no es válida.", idEmpresa);
            }

            IdentityValidation validation = await ValidateIdentityAsync(idEmpresa, request, cancellationToken);
            if (!validation.IsValid)
            {
                return new ListaPreciosResolutionResult
                {
                    Resuelto = false,
                    CodigoResolucion = validation.Code,
                    Origen = validation.Code,
                    TenantId = idEmpresa,
                    ListaSolicitada = requestedLevel,
                    ListaEfectiva = effectiveLevel,
                    TipoIdentidad = validation.Identity?.TipoIdentidad ?? 0,
                    IdProductoServicio = request.IdProductoServicio,
                    IdVariante = request.IdVariante,
                    IdPresentacionVenta = request.IdPresentacionVenta,
                    Mensaje = validation.Message
                };
            }

            ListaPreciosListaRecord? list = await _repository.ObtenerListaPorNivelAsync(idEmpresa, effectiveLevel, cancellationToken);
            if (list != null)
            {
                ListaPreciosDetalleRecord? detail = await _repository.ObtenerPrecioActivoAsync(idEmpresa, list.Id, validation.Identity!, cancellationToken);
                if (detail != null)
                {
                    return BuildResolvedResult(
                        idEmpresa,
                        validation,
                        requestedLevel,
                        effectiveLevel,
                        list.Id,
                        detail.Id,
                        detail.Precio,
                        detail.Precio,
                        ListaPreciosResolutionCodes.PrecioLista,
                        ListaPreciosResolutionCodes.PrecioLista,
                        detail.DescuentoPct,
                        detail.RedondeoModo,
                        detail.VigenciaInicio,
                        detail.VigenciaFin,
                        "Precio específico configurado.");
                }
            }

            decimal? fallback = ResolveFallback(validation);
            if (fallback.HasValue)
            {
                return BuildResolvedResult(
                    idEmpresa,
                    validation,
                    requestedLevel,
                    effectiveLevel,
                    list?.Id,
                    null,
                    fallback.Value,
                    null,
                    ListaPreciosResolutionCodes.FallbackPrecioPublico,
                    ListaPreciosResolutionCodes.FallbackPrecioPublico,
                    null,
                    ListaPreciosConstants.RedondeoSinRedondeo,
                    null,
                    null,
                    "No existe precio específico activo; se aplicó fallback.");
            }

            return new ListaPreciosResolutionResult
            {
                Resuelto = false,
                CodigoResolucion = ListaPreciosResolutionCodes.SinPrecioResoluble,
                Origen = ListaPreciosResolutionCodes.SinPrecioResoluble,
                TenantId = idEmpresa,
                ListaSolicitada = requestedLevel,
                ListaEfectiva = effectiveLevel,
                IdListaPrecio = list?.Id,
                TipoIdentidad = validation.Identity!.TipoIdentidad,
                IdProductoServicio = validation.Identity.IdProductoServicio,
                IdVariante = validation.Identity.IdVariante,
                IdPresentacionVenta = validation.Identity.IdPresentacionVenta,
                Mensaje = "No existe precio resoluble para la identidad solicitada.",
                FechaResolucionUtc = DateTime.UtcNow,
                CorrelationId = Guid.NewGuid()
            };
        }

        public async Task<Guid> GuardarPrecioAsync(Guid idEmpresa, ListaPreciosGuardarPrecioRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null || request.Precio < 0)
            {
                throw new InvalidOperationException("PRECIO_NEGATIVO");
            }

            ValidateCommercialInput(request.DescuentoPct, request.RedondeoModo, request.VigenciaInicio, request.VigenciaFin);

            IdentityValidation validation = await ValidateIdentityAsync(idEmpresa, new ListaPreciosResolverRequest
            {
                Nivel = request.Nivel,
                TipoIdentidad = request.TipoIdentidad,
                IdProductoServicio = request.IdProductoServicio,
                IdVariante = request.IdVariante,
                IdPresentacionVenta = request.IdPresentacionVenta,
                RequiereActivo = true
            }, cancellationToken);

            if (!validation.IsValid)
            {
                throw new InvalidOperationException(validation.Code);
            }

            if (!TryGetValidLevel(request.Nivel, out int level))
            {
                throw new InvalidOperationException(ListaPreciosResolutionCodes.ListaInvalida);
            }

            return await _repository.GuardarPrecioAsync(idEmpresa, new ListaPreciosGuardarPrecioCommand(
                level,
                validation.Product!.Tipo,
                validation.Identity!,
                decimal.Round(request.Precio, 2, MidpointRounding.AwayFromZero),
                request.DescuentoPct.HasValue ? decimal.Round(request.DescuentoPct.Value, 2, MidpointRounding.AwayFromZero) : null,
                request.RedondeoModo,
                request.VigenciaInicio?.Date,
                request.VigenciaFin?.Date,
                request.UsuarioId,
                request.Usuario,
                request.Motivo,
                request.CorrelationId.GetValueOrDefault(Guid.NewGuid()),
                ListaPreciosConstants.OrigenHistorialIndividual), cancellationToken);
        }

        public Task<Guid> EnsureListaAsync(Guid idEmpresa, int nivel, Guid? usuarioId, CancellationToken cancellationToken = default)
        {
            if (idEmpresa == Guid.Empty || !TryGetValidLevel(nivel, out int validLevel))
            {
                throw new InvalidOperationException(ListaPreciosResolutionCodes.ListaInvalida);
            }

            return _repository.EnsureListaAsync(idEmpresa, validLevel, usuarioId, cancellationToken);
        }

        public Task BajaPrecioAsync(Guid idEmpresa, Guid idPrecio, Guid? usuarioId, CancellationToken cancellationToken = default)
        {
            if (idEmpresa == Guid.Empty || idPrecio == Guid.Empty)
            {
                throw new InvalidOperationException("IDENTIDAD_INVALIDA");
            }

            return _repository.BajaPrecioAsync(idEmpresa, idPrecio, usuarioId, cancellationToken);
        }

        public async Task<ListaPreciosResolutionResult> PreviewAsync(Guid idEmpresa, ListaPreciosPreviewRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null || request.Precio < 0)
            {
                throw new InvalidOperationException("PRECIO_NEGATIVO");
            }

            ValidateCommercialInput(request.DescuentoPct, request.RedondeoModo, request.VigenciaInicio, request.VigenciaFin);
            if (!TryGetValidLevel(request.Nivel, out int level))
            {
                throw new InvalidOperationException(ListaPreciosResolutionCodes.ListaInvalida);
            }

            IdentityValidation validation = await ValidateIdentityAsync(idEmpresa, new ListaPreciosResolverRequest
            {
                Nivel = level,
                TipoIdentidad = request.TipoIdentidad,
                IdProductoServicio = request.IdProductoServicio,
                IdVariante = request.IdVariante,
                IdPresentacionVenta = request.IdPresentacionVenta,
                RequiereActivo = true
            }, cancellationToken);

            if (!validation.IsValid)
            {
                throw new InvalidOperationException(validation.Code);
            }

            ListaPreciosListaRecord? list = await _repository.ObtenerListaPorNivelAsync(idEmpresa, level, cancellationToken);
            return BuildResolvedResult(
                idEmpresa,
                validation,
                level,
                level,
                list?.Id,
                null,
                decimal.Round(request.Precio, 2, MidpointRounding.AwayFromZero),
                decimal.Round(request.Precio, 2, MidpointRounding.AwayFromZero),
                ListaPreciosResolutionCodes.PrecioLista,
                ListaPreciosResolutionCodes.PrecioLista,
                request.DescuentoPct.HasValue ? decimal.Round(request.DescuentoPct.Value, 2, MidpointRounding.AwayFromZero) : null,
                request.RedondeoModo,
                request.VigenciaInicio?.Date,
                request.VigenciaFin?.Date,
                "Preview comercial sin persistencia.");
        }

        public async Task<ListaPreciosAjusteMasivoResultadoDto> PreviewAjusteMasivoAsync(Guid idEmpresa, ListaPreciosAjusteMasivoRequest request, CancellationToken cancellationToken = default)
        {
            BulkPreparation preparation = await PrepareBulkAsync(idEmpresa, request, cancellationToken);
            return preparation.Result;
        }

        public async Task<ListaPreciosAjusteMasivoResultadoDto> EjecutarAjusteMasivoAsync(Guid idEmpresa, ListaPreciosAjusteMasivoRequest request, CancellationToken cancellationToken = default)
        {
            BulkPreparation preparation = await PrepareBulkAsync(idEmpresa, request, cancellationToken);
            ListaPreciosAjusteMasivoItemDto? rejected = preparation.Result.Items.FirstOrDefault(item => !item.Aceptado);
            if (rejected != null)
            {
                throw new InvalidOperationException(rejected.Codigo);
            }

            await _repository.GuardarPreciosMasivosAsync(idEmpresa, preparation.Commands, cancellationToken);
            preparation.Result.Persistido = true;
            return preparation.Result;
        }

        private async Task<BulkPreparation> PrepareBulkAsync(Guid idEmpresa, ListaPreciosAjusteMasivoRequest request, CancellationToken cancellationToken)
        {
            if (request == null || request.Identidades == null || request.Identidades.Count == 0)
            {
                throw new InvalidOperationException("SELECCION_VACIA");
            }

            if (!TryGetValidLevel(request.Nivel, out int level))
            {
                throw new InvalidOperationException(ListaPreciosResolutionCodes.ListaInvalida);
            }

            string field = (request.Campo ?? string.Empty).Trim().ToUpperInvariant();
            string adjustmentType = (request.TipoAjuste ?? string.Empty).Trim().ToUpperInvariant();
            string operation = (request.Operacion ?? string.Empty).Trim().ToUpperInvariant();
            if (field is not ("PRECIO" or "DESCUENTO")) throw new InvalidOperationException("CAMPO_INVALIDO");
            if (adjustmentType is not ("VALOR" or "MONTO" or "PORCENTAJE")) throw new InvalidOperationException("TIPO_AJUSTE_INVALIDO");
            if (request.Valor < 0m) throw new InvalidOperationException("VALOR_AJUSTE_INVALIDO");
            if (adjustmentType == "VALOR" && operation != "ASIGNAR") throw new InvalidOperationException("OPERACION_INVALIDA");
            if (adjustmentType != "VALOR" && operation is not ("SUMAR" or "RESTAR")) throw new InvalidOperationException("OPERACION_INVALIDA");

            Guid correlationId = request.CorrelationId.GetValueOrDefault(Guid.NewGuid());
            List<ListaPreciosAjusteMasivoItemDto> items = new();
            List<ListaPreciosGuardarPrecioCommand> commands = new();
            HashSet<ListaPreciosIdentityKey> seen = new();

            foreach (ListaPreciosAjusteMasivoIdentidadRequest identityRequest in request.Identidades)
            {
                ListaPreciosAjusteMasivoItemDto item = new()
                {
                    TipoIdentidad = identityRequest.TipoIdentidad.GetValueOrDefault(),
                    IdProductoServicio = identityRequest.IdProductoServicio,
                    IdVariante = identityRequest.IdVariante,
                    IdPresentacionVenta = identityRequest.IdPresentacionVenta
                };
                items.Add(item);

                try
                {
                    IdentityValidation validation = await ValidateIdentityAsync(idEmpresa, new ListaPreciosResolverRequest
                    {
                        Nivel = level,
                        TipoIdentidad = identityRequest.TipoIdentidad,
                        IdProductoServicio = identityRequest.IdProductoServicio,
                        IdVariante = identityRequest.IdVariante,
                        IdPresentacionVenta = identityRequest.IdPresentacionVenta,
                        RequiereActivo = true
                    }, cancellationToken);
                    if (!validation.IsValid) throw new InvalidOperationException(validation.Code);
                    if (!seen.Add(validation.Identity!)) throw new InvalidOperationException(ListaPreciosResolutionCodes.Duplicado);

                    ListaPreciosResolutionResult current = await ResolverPrecioAsync(idEmpresa, new ListaPreciosResolverRequest
                    {
                        Nivel = level,
                        TipoIdentidad = validation.Identity!.TipoIdentidad,
                        IdProductoServicio = validation.Identity.IdProductoServicio,
                        IdVariante = validation.Identity.IdVariante,
                        IdPresentacionVenta = validation.Identity.IdPresentacionVenta
                    }, cancellationToken);
                    if (!current.Resuelto) throw new InvalidOperationException(current.CodigoResolucion);

                    decimal currentPrice = current.PrecioLista ?? current.PrecioBase ?? current.PrecioEfectivo.GetValueOrDefault();
                    decimal currentDiscount = current.DescuentoPct.GetValueOrDefault();
                    decimal currentValue = field == "PRECIO" ? currentPrice : currentDiscount;
                    decimal proposedValue = CalculateBulkValue(currentValue, request.Valor, adjustmentType, operation);
                    decimal proposedPrice = field == "PRECIO" ? proposedValue : currentPrice;
                    decimal? proposedDiscount = field == "DESCUENTO" ? proposedValue : current.DescuentoPct;
                    if (proposedPrice < 0m) throw new InvalidOperationException("PRECIO_NEGATIVO");
                    ValidateCommercialInput(proposedDiscount, current.RedondeoModo, current.VigenciaInicio, current.VigenciaFin);

                    ListaPreciosResolutionResult preview = await PreviewAsync(idEmpresa, new ListaPreciosPreviewRequest
                    {
                        Nivel = level,
                        TipoIdentidad = validation.Identity.TipoIdentidad,
                        IdProductoServicio = validation.Identity.IdProductoServicio,
                        IdVariante = validation.Identity.IdVariante,
                        IdPresentacionVenta = validation.Identity.IdPresentacionVenta,
                        Precio = proposedPrice,
                        DescuentoPct = proposedDiscount,
                        RedondeoModo = current.RedondeoModo,
                        VigenciaInicio = current.VigenciaInicio,
                        VigenciaFin = current.VigenciaFin
                    }, cancellationToken);

                    item.TipoIdentidad = validation.Identity.TipoIdentidad;
                    item.ValorActual = decimal.Round(currentValue, 2, MidpointRounding.AwayFromZero);
                    item.ValorPropuesto = decimal.Round(proposedValue, 2, MidpointRounding.AwayFromZero);
                    item.Diferencia = item.ValorPropuesto - item.ValorActual;
                    item.PrecioLista = preview.PrecioLista;
                    item.DescuentoPct = preview.DescuentoPct;
                    item.RedondeoModo = preview.RedondeoModo;
                    item.PrecioFinal = preview.PrecioFinal;
                    item.Aceptado = true;
                    item.Codigo = "OK";
                    commands.Add(new ListaPreciosGuardarPrecioCommand(
                        level, validation.Product!.Tipo, validation.Identity, proposedPrice, proposedDiscount,
                        current.RedondeoModo, current.VigenciaInicio?.Date, current.VigenciaFin?.Date,
                        request.UsuarioId, request.Usuario, request.Motivo, correlationId, ListaPreciosConstants.OrigenHistorialMasivo));
                }
                catch (InvalidOperationException ex)
                {
                    item.Aceptado = false;
                    item.Codigo = ex.Message;
                    item.MotivoRechazo = ex.Message;
                }
            }

            return new BulkPreparation(new ListaPreciosAjusteMasivoResultadoDto
            {
                Seleccionados = items.Count,
                Aceptados = items.Count(item => item.Aceptado),
                Rechazados = items.Count(item => !item.Aceptado),
                Nivel = level,
                Campo = field,
                TipoAjuste = adjustmentType,
                Operacion = operation,
                Valor = request.Valor,
                Persistido = false,
                CorrelationId = correlationId,
                Items = items
            }, commands);
        }

        private static decimal CalculateBulkValue(decimal current, decimal value, string adjustmentType, string operation)
        {
            decimal result = adjustmentType switch
            {
                "VALOR" => value,
                "MONTO" => operation == "SUMAR" ? current + value : current - value,
                "PORCENTAJE" => operation == "SUMAR" ? current + (current * value / 100m) : current - (current * value / 100m),
                _ => throw new InvalidOperationException("TIPO_AJUSTE_INVALIDO")
            };
            return decimal.Round(result, 2, MidpointRounding.AwayFromZero);
        }

        private sealed record BulkPreparation(ListaPreciosAjusteMasivoResultadoDto Result, IReadOnlyList<ListaPreciosGuardarPrecioCommand> Commands);

        public async Task<ListaPreciosCopiarListaResultadoDto> PreviewCopiarListaAsync(Guid idEmpresa, ListaPreciosCopiarListaRequest request, CancellationToken cancellationToken = default)
        {
            CopyPreparation preparation = await PrepareCopyAsync(idEmpresa, request, cancellationToken);
            return preparation.Result;
        }

        public async Task<ListaPreciosCopiarListaResultadoDto> EjecutarCopiarListaAsync(Guid idEmpresa, ListaPreciosCopiarListaRequest request, CancellationToken cancellationToken = default)
        {
            CopyPreparation preparation = await PrepareCopyAsync(idEmpresa, request, cancellationToken);
            ListaPreciosCopiarListaItemDto? rejected = preparation.Result.Items.FirstOrDefault(item => !item.Aceptado);
            if (rejected != null) throw new InvalidOperationException(rejected.Codigo);

            if (preparation.Commands.Count > 0)
            {
                await _repository.GuardarPreciosMasivosAsync(idEmpresa, preparation.Commands, cancellationToken);
            }

            preparation.Result.Persistido = true;
            return preparation.Result;
        }

        private async Task<CopyPreparation> PrepareCopyAsync(Guid idEmpresa, ListaPreciosCopiarListaRequest request, CancellationToken cancellationToken)
        {
            if (request == null
                || !TryGetValidLevel(request.NivelOrigen, out int sourceLevel)
                || !TryGetValidLevel(request.NivelDestino, out int targetLevel))
            {
                throw new InvalidOperationException(ListaPreciosResolutionCodes.ListaInvalida);
            }
            if (sourceLevel == targetLevel) throw new InvalidOperationException("ORIGEN_DESTINO_IGUALES");

            string mode = (request.Modo ?? string.Empty).Trim().ToUpperInvariant();
            if (mode is not ("SOBRESCRIBIR" or "MERGE")) throw new InvalidOperationException("MODO_INVALIDO");

            ListaPreciosListaRecord? sourceList = await _repository.ObtenerListaPorNivelAsync(idEmpresa, sourceLevel, cancellationToken);
            if (sourceList == null) throw new InvalidOperationException(ListaPreciosResolutionCodes.ListaInvalida);

            IReadOnlyList<ListaPreciosDetalleRecord> sources = await _repository.ObtenerDetallesPorNivelAsync(idEmpresa, sourceLevel, true, cancellationToken);
            IReadOnlyList<ListaPreciosDetalleRecord> targets = await _repository.ObtenerDetallesPorNivelAsync(idEmpresa, targetLevel, false, cancellationToken);
            Dictionary<ListaPreciosIdentityKey, List<ListaPreciosDetalleRecord>> targetsByIdentity = targets
                .GroupBy(ToIdentity)
                .ToDictionary(group => group.Key, group => group.OrderByDescending(x => x.Activo).ThenByDescending(x => x.FechaActualizacion).ToList());

            Guid correlationId = request.CorrelationId.GetValueOrDefault(Guid.NewGuid());
            List<ListaPreciosCopiarListaItemDto> items = new(sources.Count);
            List<ListaPreciosGuardarPrecioCommand> commands = new(sources.Count);

            foreach (ListaPreciosDetalleRecord source in sources)
            {
                ListaPreciosIdentityKey identity = ToIdentity(source);
                ListaPreciosCopiarListaItemDto item = CreateCopyItem(source);
                items.Add(item);
                try
                {
                    ValidateCommercialInput(source.DescuentoPct, source.RedondeoModo, source.VigenciaInicio, source.VigenciaFin);
                    IdentityValidation validation = await ValidateIdentityAsync(idEmpresa, new ListaPreciosResolverRequest
                    {
                        Nivel = sourceLevel,
                        TipoIdentidad = identity.TipoIdentidad,
                        IdProductoServicio = identity.IdProductoServicio,
                        IdVariante = identity.IdVariante,
                        IdPresentacionVenta = identity.IdPresentacionVenta,
                        RequiereActivo = true
                    }, cancellationToken);
                    if (!validation.IsValid) throw new InvalidOperationException(validation.Code);

                    targetsByIdentity.TryGetValue(identity, out List<ListaPreciosDetalleRecord>? matches);
                    matches ??= new List<ListaPreciosDetalleRecord>();
                    List<ListaPreciosDetalleRecord> activeTargets = matches.Where(x => x.Activo).ToList();
                    if (activeTargets.Count > 1) throw new InvalidOperationException(ListaPreciosResolutionCodes.Duplicado);

                    ListaPreciosDetalleRecord? activeTarget = activeTargets.FirstOrDefault();
                    ListaPreciosDetalleRecord? inactiveTarget = activeTarget == null ? matches.FirstOrDefault(x => !x.Activo) : null;
                    ListaPreciosDetalleRecord? before = activeTarget ?? inactiveTarget;
                    ApplyCopyBefore(item, before);

                    bool copyDiscounts = request.CopiarDescuentos != false;
                    decimal? targetDiscount = copyDiscounts ? source.DescuentoPct : before?.DescuentoPct;
                    byte targetRounding = copyDiscounts ? source.RedondeoModo : before?.RedondeoModo ?? ListaPreciosConstants.RedondeoSinRedondeo;
                    DateTime? targetStart = copyDiscounts ? source.VigenciaInicio?.Date : before?.VigenciaInicio?.Date;
                    DateTime? targetEnd = copyDiscounts ? source.VigenciaFin?.Date : before?.VigenciaFin?.Date;
                    item.DescuentoAfter = targetDiscount;
                    item.RedondeoAfter = targetRounding;
                    item.VigenciaInicioAfter = targetStart;
                    item.VigenciaFinAfter = targetEnd;

                    if (activeTarget != null && mode == "MERGE")
                    {
                        ApplyCopyAfter(item, activeTarget);
                        item.Accion = "OMITIDO";
                        item.Codigo = "DESTINO_EXISTENTE";
                        item.Motivo = "MERGE preserva la configuración activa del destino.";
                        item.Aceptado = true;
                        continue;
                    }
                    if (activeTarget != null
                        && decimal.Round(source.Precio, 2, MidpointRounding.AwayFromZero) == decimal.Round(activeTarget.Precio, 2, MidpointRounding.AwayFromZero)
                        && Nullable.Equals(targetDiscount, activeTarget.DescuentoPct)
                        && targetRounding == activeTarget.RedondeoModo
                        && targetStart == activeTarget.VigenciaInicio?.Date
                        && targetEnd == activeTarget.VigenciaFin?.Date)
                    {
                        item.Accion = "OMITIDO";
                        item.Codigo = "SIN_CAMBIO";
                        item.Motivo = "El destino activo ya contiene los mismos valores comerciales.";
                        item.Aceptado = true;
                        continue;
                    }

                    item.Accion = activeTarget != null ? "SOBRESCRIBIR" : inactiveTarget != null ? "REACTIVAR" : "NUEVO";
                    item.Codigo = "OK";
                    item.Aceptado = true;
                    commands.Add(new ListaPreciosGuardarPrecioCommand(
                        targetLevel,
                        validation.Product!.Tipo,
                        identity,
                        source.Precio,
                        targetDiscount,
                        targetRounding,
                        targetStart,
                        targetEnd,
                        request.UsuarioId,
                        request.Usuario,
                        request.Motivo,
                        correlationId,
                        ListaPreciosConstants.OrigenHistorialCopiaLista,
                        item.Accion switch { "NUEVO" => "NUEVO", "REACTIVAR" => "REACTIVACION", _ => "SOBRESCRITURA" }));
                }
                catch (InvalidOperationException ex)
                {
                    item.Accion = "RECHAZADO";
                    item.Aceptado = false;
                    item.Codigo = ex.Message;
                    item.Motivo = ex.Message;
                }
            }

            return new CopyPreparation(new ListaPreciosCopiarListaResultadoDto
            {
                NivelOrigen = sourceLevel,
                NivelDestino = targetLevel,
                Modo = mode,
                TotalOrigenActivo = sources.Count,
                Nuevos = items.Count(x => x.Accion == "NUEVO"),
                Reactivados = items.Count(x => x.Accion == "REACTIVAR"),
                Sobrescritos = items.Count(x => x.Accion == "SOBRESCRIBIR"),
                Omitidos = items.Count(x => x.Accion == "OMITIDO"),
                Rechazados = items.Count(x => !x.Aceptado),
                Persistido = false,
                CorrelationId = correlationId,
                Items = items
            }, commands);
        }

        private static ListaPreciosCopiarListaItemDto CreateCopyItem(ListaPreciosDetalleRecord source) => new()
        {
            TipoIdentidad = source.TipoIdentidad,
            IdProductoServicio = source.IdProductoServicio,
            IdVariante = source.IdVariante,
            IdPresentacionVenta = source.IdPresentacionVenta,
            EstadoDestino = "SIN_CONFIGURACION",
            PrecioOrigen = source.Precio,
            PrecioAfter = source.Precio,
            DescuentoOrigen = source.DescuentoPct,
            DescuentoAfter = source.DescuentoPct,
            RedondeoOrigen = source.RedondeoModo,
            RedondeoAfter = source.RedondeoModo,
            VigenciaInicioOrigen = source.VigenciaInicio,
            VigenciaInicioAfter = source.VigenciaInicio,
            VigenciaFinOrigen = source.VigenciaFin,
            VigenciaFinAfter = source.VigenciaFin
        };

        private static void ApplyCopyBefore(ListaPreciosCopiarListaItemDto item, ListaPreciosDetalleRecord? before)
        {
            if (before == null) return;
            item.EstadoDestino = before.Activo ? "ACTIVO" : "INACTIVO";
            item.PrecioDestinoBefore = before.Precio;
            item.DescuentoDestinoBefore = before.DescuentoPct;
            item.RedondeoDestinoBefore = before.RedondeoModo;
            item.VigenciaInicioDestinoBefore = before.VigenciaInicio;
            item.VigenciaFinDestinoBefore = before.VigenciaFin;
        }

        private static void ApplyCopyAfter(ListaPreciosCopiarListaItemDto item, ListaPreciosDetalleRecord detail)
        {
            item.PrecioAfter = detail.Precio;
            item.DescuentoAfter = detail.DescuentoPct;
            item.RedondeoAfter = detail.RedondeoModo;
            item.VigenciaInicioAfter = detail.VigenciaInicio;
            item.VigenciaFinAfter = detail.VigenciaFin;
        }

        private static ListaPreciosIdentityKey ToIdentity(ListaPreciosDetalleRecord detail)
            => new(detail.TipoIdentidad, detail.IdProductoServicio, detail.IdVariante, detail.IdPresentacionVenta);

        private static bool SameCommercialValues(ListaPreciosDetalleRecord left, ListaPreciosDetalleRecord right)
            => decimal.Round(left.Precio, 2, MidpointRounding.AwayFromZero) == decimal.Round(right.Precio, 2, MidpointRounding.AwayFromZero)
                && Nullable.Equals(left.DescuentoPct, right.DescuentoPct)
                && left.RedondeoModo == right.RedondeoModo
                && left.VigenciaInicio?.Date == right.VigenciaInicio?.Date
                && left.VigenciaFin?.Date == right.VigenciaFin?.Date;

        private sealed record CopyPreparation(ListaPreciosCopiarListaResultadoDto Result, IReadOnlyList<ListaPreciosGuardarPrecioCommand> Commands);

        public async Task<ListaPreciosDescuentoMarcaResultadoDto> PreviewDescuentoMarcaAsync(Guid idEmpresa, ListaPreciosDescuentoMarcaRequest request, CancellationToken cancellationToken = default)
        {
            BrandDiscountPreparation preparation = await PrepareBrandDiscountAsync(idEmpresa, request, cancellationToken);
            return preparation.Result;
        }

        public async Task<ListaPreciosDescuentoMarcaResultadoDto> EjecutarDescuentoMarcaAsync(Guid idEmpresa, ListaPreciosDescuentoMarcaRequest request, CancellationToken cancellationToken = default)
        {
            BrandDiscountPreparation preparation = await PrepareBrandDiscountAsync(idEmpresa, request, cancellationToken);
            ListaPreciosDescuentoMarcaItemDto? rejected = preparation.Result.Items.FirstOrDefault(item => !item.Aceptado);
            if (rejected != null) throw new InvalidOperationException(rejected.Codigo);

            if (preparation.Commands.Count > 0)
            {
                await _repository.GuardarPreciosMasivosAsync(idEmpresa, preparation.Commands, cancellationToken);
            }

            preparation.Result.Persistido = true;
            return preparation.Result;
        }

        private async Task<BrandDiscountPreparation> PrepareBrandDiscountAsync(Guid idEmpresa, ListaPreciosDescuentoMarcaRequest request, CancellationToken cancellationToken)
        {
            if (request == null || request.IdMarca == Guid.Empty) throw new InvalidOperationException("MARCA_INVALIDA");
            if (!TryGetValidLevel(request.Nivel, out int level)) throw new InvalidOperationException(ListaPreciosResolutionCodes.ListaInvalida);
            ValidateCommercialInput(request.DescuentoPct, ListaPreciosConstants.RedondeoSinRedondeo, null, null);

            ListaPreciosComboItemDto? brand = (await _repository.ObtenerMarcasAsync(idEmpresa, cancellationToken))
                .FirstOrDefault(item => item.Id == request.IdMarca);
            if (brand == null) throw new InvalidOperationException("MARCA_INVALIDA");

            IReadOnlyList<ListaPreciosConsultaIdentityRecord> identities = await _repository.ConsultarIdentidadesAsync(idEmpresa, new ListaPreciosConsultaRequest
            {
                Nivel = level,
                IdMarca = request.IdMarca,
                Estatus = "activos"
            }, cancellationToken);
            IReadOnlyList<ListaPreciosDetalleRecord> details = await _repository.ObtenerDetallesPorNivelAsync(idEmpresa, level, false, cancellationToken);
            Guid correlationId = request.CorrelationId.GetValueOrDefault(Guid.NewGuid());
            List<ListaPreciosDescuentoMarcaItemDto> items = new();
            List<ListaPreciosGuardarPrecioCommand> commands = new();

            foreach (ListaPreciosConsultaIdentityRecord identityRecord in identities)
            {
                ListaPreciosIdentityKey identity = new(identityRecord.TipoIdentidad, identityRecord.IdProductoServicio, identityRecord.IdVariante, identityRecord.IdPresentacionVenta);
                ListaPreciosDescuentoMarcaItemDto item = new()
                {
                    TipoIdentidad = identity.TipoIdentidad,
                    IdProductoServicio = identity.IdProductoServicio,
                    IdVariante = identity.IdVariante,
                    IdPresentacionVenta = identity.IdPresentacionVenta,
                    Identidad = identityRecord.Nombre
                };
                items.Add(item);

                try
                {
                    IdentityValidation validation = await ValidateIdentityAsync(idEmpresa, new ListaPreciosResolverRequest
                    {
                        Nivel = level,
                        TipoIdentidad = identity.TipoIdentidad,
                        IdProductoServicio = identity.IdProductoServicio,
                        IdVariante = identity.IdVariante,
                        IdPresentacionVenta = identity.IdPresentacionVenta,
                        RequiereActivo = true
                    }, cancellationToken);
                    if (!validation.IsValid) throw new InvalidOperationException(validation.Code);

                    ListaPreciosResolutionResult current = await ResolverPrecioAsync(idEmpresa, new ListaPreciosResolverRequest
                    {
                        Nivel = level,
                        TipoIdentidad = identity.TipoIdentidad,
                        IdProductoServicio = identity.IdProductoServicio,
                        IdVariante = identity.IdVariante,
                        IdPresentacionVenta = identity.IdPresentacionVenta
                    }, cancellationToken);
                    if (!current.Resuelto) throw new InvalidOperationException(current.CodigoResolucion);

                    ListaPreciosDetalleRecord? previous = details
                        .Where(detail => SameIdentity(detail, identity))
                        .OrderByDescending(detail => detail.Activo)
                        .ThenByDescending(detail => detail.FechaActualizacion)
                        .FirstOrDefault();
                    decimal price = previous?.Precio ?? current.PrecioLista ?? current.PrecioBase ?? current.PrecioEfectivo.GetValueOrDefault();
                    decimal? discountBefore = previous?.DescuentoPct ?? current.DescuentoPct;
                    byte rounding = previous?.RedondeoModo ?? current.RedondeoModo;
                    DateTime? start = previous?.VigenciaInicio?.Date ?? current.VigenciaInicio?.Date;
                    DateTime? end = previous?.VigenciaFin?.Date ?? current.VigenciaFin?.Date;
                    ListaPreciosResolutionResult preview = await PreviewAsync(idEmpresa, new ListaPreciosPreviewRequest
                    {
                        Nivel = level,
                        TipoIdentidad = identity.TipoIdentidad,
                        IdProductoServicio = identity.IdProductoServicio,
                        IdVariante = identity.IdVariante,
                        IdPresentacionVenta = identity.IdPresentacionVenta,
                        Precio = price,
                        DescuentoPct = request.DescuentoPct,
                        RedondeoModo = rounding,
                        VigenciaInicio = start,
                        VigenciaFin = end
                    }, cancellationToken);

                    item.PrecioListaBefore = previous?.Precio ?? current.PrecioLista ?? current.PrecioBase;
                    item.DescuentoBefore = discountBefore;
                    item.PrecioFinalBefore = current.PrecioFinal ?? current.PrecioEfectivo;
                    item.DescuentoAfter = preview.DescuentoPct;
                    item.PrecioFinalAfter = preview.PrecioFinal;
                    item.Aceptado = true;
                    item.Codigo = "OK";

                    bool noChange = previous?.Activo == true && Nullable.Equals(previous.DescuentoPct, preview.DescuentoPct);
                    if (noChange)
                    {
                        item.Accion = "OMITIR";
                        item.Motivo = "El descuento activo ya coincide con el valor solicitado.";
                        continue;
                    }

                    item.Accion = previous == null ? "CREAR" : previous.Activo ? "ACTUALIZAR" : "REACTIVAR";
                    commands.Add(new ListaPreciosGuardarPrecioCommand(
                        level, validation.Product!.Tipo, identity, price, preview.DescuentoPct, rounding, start, end,
                        request.UsuarioId, request.Usuario, request.Motivo, correlationId, ListaPreciosConstants.OrigenHistorialDescuentoMarca));
                }
                catch (InvalidOperationException ex)
                {
                    item.Aceptado = false;
                    item.Codigo = ex.Message;
                    item.Motivo = ex.Message;
                    item.Accion = "RECHAZAR";
                }
            }

            return new BrandDiscountPreparation(new ListaPreciosDescuentoMarcaResultadoDto
            {
                IdMarca = request.IdMarca,
                Marca = brand.Nombre,
                Nivel = level,
                DescuentoPct = decimal.Round(request.DescuentoPct, 2, MidpointRounding.AwayFromZero),
                Evaluados = items.Count,
                Afectados = items.Count(item => item.Aceptado && item.Accion != "OMITIR"),
                Omitidos = items.Count(item => item.Aceptado && item.Accion == "OMITIR"),
                Rechazados = items.Count(item => !item.Aceptado),
                Persistido = false,
                CorrelationId = correlationId,
                Items = items
            }, commands);
        }

        private static bool SameIdentity(ListaPreciosDetalleRecord detail, ListaPreciosIdentityKey identity)
        {
            return detail.TipoIdentidad == identity.TipoIdentidad
                && detail.IdProductoServicio == identity.IdProductoServicio
                && detail.IdVariante == identity.IdVariante
                && detail.IdPresentacionVenta == identity.IdPresentacionVenta;
        }

        private sealed record BrandDiscountPreparation(ListaPreciosDescuentoMarcaResultadoDto Result, IReadOnlyList<ListaPreciosGuardarPrecioCommand> Commands);

        public async Task<IReadOnlyList<ListaPreciosHistorialDto>> ObtenerHistorialAsync(Guid idEmpresa, ListaPreciosResolverRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null || !TryGetValidLevel(request.Nivel, out int level))
            {
                throw new InvalidOperationException(ListaPreciosResolutionCodes.ListaInvalida);
            }

            IdentityValidation validation = await ValidateIdentityAsync(idEmpresa, request, cancellationToken);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException(validation.Code);
            }

            return await _repository.ObtenerHistorialAsync(idEmpresa, validation.Identity!, level, cancellationToken);
        }

        public Task<ListaPreciosHistorialPaginaDto> ConsultarHistorialAsync(Guid idEmpresa, ListaPreciosHistorialConsultaRequest request, CancellationToken cancellationToken = default)
        {
            if (request.FechaDesde.HasValue && request.FechaHasta.HasValue && request.FechaDesde.Value.Date > request.FechaHasta.Value.Date)
            {
                throw new InvalidOperationException("RANGO_FECHAS_INVALIDO");
            }

            if (request.Nivel.HasValue && !TryGetValidLevel(request.Nivel, out _))
            {
                throw new InvalidOperationException(ListaPreciosResolutionCodes.ListaInvalida);
            }

            if (request.TipoIdentidad.HasValue && !IsKnownIdentityType(request.TipoIdentidad.Value))
            {
                throw new InvalidOperationException(ListaPreciosResolutionCodes.IdentidadInvalida);
            }

            request.Pagina = Math.Max(1, request.Pagina);
            request.TamanoPagina = request.TamanoPagina is 25 or 50 or 100 ? request.TamanoPagina : 25;
            request.Busqueda = TrimToLength(request.Busqueda, 240);
            request.Origen = TrimToLength(request.Origen, 40);
            request.Operacion = TrimToLength(request.Operacion, 40);
            request.Usuario = TrimToLength(request.Usuario, 240);
            return _repository.ConsultarHistorialAsync(idEmpresa, request, cancellationToken);
        }

        private static string TrimToLength(string? value, int maximumLength)
        {
            string normalized = (value ?? string.Empty).Trim();
            return normalized.Length <= maximumLength ? normalized : normalized[..maximumLength];
        }

        private async Task<IdentityValidation> ValidateIdentityAsync(Guid idEmpresa, ListaPreciosResolverRequest request, CancellationToken cancellationToken)
        {
            if (request.IdVariante.HasValue && request.IdPresentacionVenta.HasValue)
            {
                return IdentityValidation.Fail(ListaPreciosResolutionCodes.FueraDeV1, "Variante + Presentación está fuera de V1.");
            }

            if (request.TipoIdentidad.HasValue && !IsKnownIdentityType(request.TipoIdentidad.Value))
            {
                return IdentityValidation.Fail(ListaPreciosResolutionCodes.IdentidadInvalida, "El tipo de identidad solicitado no es válido.");
            }

            if (request.TipoIdentidad.HasValue && !IdentityShapeMatches(request.TipoIdentidad.Value, request))
            {
                return IdentityValidation.Fail(ListaPreciosResolutionCodes.IdentidadInvalida, "La identidad solicitada no coincide con sus llaves.");
            }

            ListaPreciosProductoRecord? product = await _repository.ObtenerProductoAsync(idEmpresa, request.IdProductoServicio, cancellationToken);
            if (product == null)
            {
                return IdentityValidation.Fail(ListaPreciosResolutionCodes.IdentidadInvalida, "El producto o servicio no existe para la empresa activa.");
            }

            if (!product.Activo)
            {
                return IdentityValidation.Fail(ListaPreciosResolutionCodes.IdentidadInactiva, "El producto o servicio está inactivo.");
            }

            byte identityType = ResolveIdentityType(request, product);
            if (identityType == ListaPreciosConstants.IdentidadServicio && product.Tipo != ListaPreciosConstants.TipoServicio)
            {
                return IdentityValidation.Fail(ListaPreciosResolutionCodes.IdentidadInvalida, "La identidad de servicio no corresponde al registro solicitado.");
            }

            if (identityType == ListaPreciosConstants.IdentidadProducto && product.Tipo != ListaPreciosConstants.TipoProducto)
            {
                return IdentityValidation.Fail(ListaPreciosResolutionCodes.IdentidadInvalida, "La identidad de producto no corresponde al registro solicitado.");
            }

            ListaPreciosVarianteRecord? variant = null;
            ListaPreciosPresentacionVentaRecord? presentation = null;
            if (identityType == ListaPreciosConstants.IdentidadVariante)
            {
                if (!request.IdVariante.HasValue || product.Tipo != ListaPreciosConstants.TipoProducto)
                {
                    return IdentityValidation.Fail(ListaPreciosResolutionCodes.IdentidadInvalida, "La variante requiere un producto válido.");
                }

                variant = await _repository.ObtenerVarianteAsync(idEmpresa, request.IdVariante.Value, cancellationToken);
                if (variant == null || variant.IdProductoServicio != request.IdProductoServicio)
                {
                    return IdentityValidation.Fail(ListaPreciosResolutionCodes.IdentidadInvalida, "La variante no pertenece al producto solicitado.");
                }

                if (!variant.Activo)
                {
                    return IdentityValidation.Fail(ListaPreciosResolutionCodes.IdentidadInactiva, "La variante está inactiva.");
                }
            }

            if (identityType == ListaPreciosConstants.IdentidadPresentacionVenta)
            {
                if (!request.IdPresentacionVenta.HasValue || product.Tipo != ListaPreciosConstants.TipoProducto)
                {
                    return IdentityValidation.Fail(ListaPreciosResolutionCodes.IdentidadInvalida, "La presentación de venta requiere un producto válido.");
                }

                presentation = await _repository.ObtenerPresentacionVentaAsync(idEmpresa, request.IdPresentacionVenta.Value, cancellationToken);
                if (presentation == null || presentation.IdProductoServicio != request.IdProductoServicio)
                {
                    return IdentityValidation.Fail(ListaPreciosResolutionCodes.IdentidadInvalida, "La presentación no pertenece al producto solicitado.");
                }

                if (!presentation.Activo)
                {
                    return IdentityValidation.Fail(ListaPreciosResolutionCodes.IdentidadInactiva, "La presentación está inactiva.");
                }
            }

            ListaPreciosIdentityKey identity = new(
                identityType,
                request.IdProductoServicio,
                identityType == ListaPreciosConstants.IdentidadVariante ? request.IdVariante : null,
                identityType == ListaPreciosConstants.IdentidadPresentacionVenta ? request.IdPresentacionVenta : null);

            return IdentityValidation.Pass(identity, product, variant, presentation);
        }

        private static bool IsKnownIdentityType(byte tipoIdentidad)
        {
            return tipoIdentidad is ListaPreciosConstants.IdentidadProducto
                or ListaPreciosConstants.IdentidadServicio
                or ListaPreciosConstants.IdentidadVariante
                or ListaPreciosConstants.IdentidadPresentacionVenta;
        }

        private static bool IdentityShapeMatches(byte tipoIdentidad, ListaPreciosResolverRequest request)
        {
            return tipoIdentidad switch
            {
                ListaPreciosConstants.IdentidadProducto => !request.IdVariante.HasValue && !request.IdPresentacionVenta.HasValue,
                ListaPreciosConstants.IdentidadServicio => !request.IdVariante.HasValue && !request.IdPresentacionVenta.HasValue,
                ListaPreciosConstants.IdentidadVariante => request.IdVariante.HasValue && !request.IdPresentacionVenta.HasValue,
                ListaPreciosConstants.IdentidadPresentacionVenta => !request.IdVariante.HasValue && request.IdPresentacionVenta.HasValue,
                _ => false
            };
        }

        private static byte ResolveIdentityType(ListaPreciosResolverRequest request, ListaPreciosProductoRecord product)
        {
            if (request.TipoIdentidad.HasValue)
            {
                return request.TipoIdentidad.Value;
            }

            if (request.IdVariante.HasValue) return ListaPreciosConstants.IdentidadVariante;
            if (request.IdPresentacionVenta.HasValue) return ListaPreciosConstants.IdentidadPresentacionVenta;
            return product.Tipo == ListaPreciosConstants.TipoServicio
                ? ListaPreciosConstants.IdentidadServicio
                : ListaPreciosConstants.IdentidadProducto;
        }

        private static decimal? ResolveFallback(IdentityValidation validation)
        {
            return validation.Identity!.TipoIdentidad switch
            {
                ListaPreciosConstants.IdentidadProducto => validation.Product!.PrecioPublico,
                ListaPreciosConstants.IdentidadServicio => validation.Product!.PrecioPublico,
                ListaPreciosConstants.IdentidadVariante => validation.Variante!.PrecioPublico ?? validation.Product!.PrecioPublico,
                ListaPreciosConstants.IdentidadPresentacionVenta => validation.PresentacionVenta!.Precio,
                _ => null
            };
        }

        private static bool TryGetValidLevel(int? level, out int validLevel)
        {
            validLevel = level.GetValueOrDefault(ListaPreciosConstants.NivelDefault);
            return validLevel >= ListaPreciosConstants.NivelMinimo && validLevel <= ListaPreciosConstants.NivelMaximo;
        }

        private static void ValidateCommercialInput(decimal? descuentoPct, byte redondeoModo, DateTime? vigenciaInicio, DateTime? vigenciaFin)
        {
            if (descuentoPct.HasValue && (descuentoPct.Value < 0m || descuentoPct.Value > 100m))
            {
                throw new InvalidOperationException(ListaPreciosResolutionCodes.DescuentoInvalido);
            }

            if (redondeoModo is not (ListaPreciosConstants.RedondeoSinRedondeo or ListaPreciosConstants.RedondeoA49 or ListaPreciosConstants.RedondeoSolo9))
            {
                throw new InvalidOperationException(ListaPreciosResolutionCodes.RedondeoInvalido);
            }

            if (vigenciaInicio.HasValue && vigenciaFin.HasValue && vigenciaInicio.Value.Date > vigenciaFin.Value.Date)
            {
                throw new InvalidOperationException(ListaPreciosResolutionCodes.VigenciaInvalida);
            }
        }

        private static void ValidateConsultaInput(ListaPreciosConsultaRequest request)
        {
            if (request.IdsValoresAtributo.Count > 0 && (!request.IdAtributo.HasValue || request.IdAtributo.Value == Guid.Empty))
            {
                throw new InvalidOperationException("ATRIBUTO_REQUERIDO");
            }

            if (request.PrecioMinimo.HasValue && request.PrecioMinimo.Value < 0m)
            {
                throw new InvalidOperationException("PRECIO_MINIMO_INVALIDO");
            }

            if (request.PrecioMaximo.HasValue && request.PrecioMaximo.Value < 0m)
            {
                throw new InvalidOperationException("PRECIO_MAXIMO_INVALIDO");
            }

            if (request.PrecioMinimo.HasValue && request.PrecioMaximo.HasValue && request.PrecioMinimo.Value > request.PrecioMaximo.Value)
            {
                throw new InvalidOperationException("RANGO_PRECIO_INVALIDO");
            }

            if (request.DescuentoPct.HasValue && (request.DescuentoPct.Value < 0m || request.DescuentoPct.Value > 100m))
            {
                throw new InvalidOperationException(ListaPreciosResolutionCodes.DescuentoInvalido);
            }

            string descuento = (request.Descuento ?? string.Empty).Trim().ToLowerInvariant();
            if (descuento is not ("" or "con" or "sin"))
            {
                throw new InvalidOperationException(ListaPreciosResolutionCodes.DescuentoInvalido);
            }

            if (request.CantidadMenorA.HasValue && request.CantidadMenorA.Value < 0m)
            {
                throw new InvalidOperationException("CANTIDAD_INVENTARIO_INVALIDA");
            }

            string existencia = (request.Existencia ?? string.Empty).Trim().ToLowerInvariant();
            if (existencia is not ("" or "con" or "sin"))
            {
                throw new InvalidOperationException("EXISTENCIA_FILTRO_INVALIDO");
            }
        }

        private static bool MatchesCommercialFilters(ListaPreciosConsultaRowDto row, ListaPreciosConsultaRequest request)
        {
            decimal? price = row.PrecioFinal ?? row.PrecioEfectivo;
            if (request.PrecioMinimo.HasValue && (!price.HasValue || price.Value < request.PrecioMinimo.Value))
            {
                return false;
            }

            if (request.PrecioMaximo.HasValue && (!price.HasValue || price.Value > request.PrecioMaximo.Value))
            {
                return false;
            }

            if (request.DescuentoPct.HasValue)
            {
                decimal discount = row.DescuentoPct.GetValueOrDefault(0m);
                if (decimal.Round(discount, 2, MidpointRounding.AwayFromZero) != decimal.Round(request.DescuentoPct.Value, 2, MidpointRounding.AwayFromZero))
                {
                    return false;
                }
            }

            string discountFilter = (request.Descuento ?? string.Empty).Trim().ToLowerInvariant();
            if (discountFilter == "con" && row.DescuentoPct.GetValueOrDefault() <= 0m)
            {
                return false;
            }

            if (discountFilter == "sin" && row.DescuentoPct.GetValueOrDefault() > 0m)
            {
                return false;
            }

            return true;
        }

        private static ListaPreciosResolutionResult BuildResolvedResult(
            Guid idEmpresa,
            IdentityValidation validation,
            int requestedLevel,
            int effectiveLevel,
            Guid? idLista,
            Guid? idDetalle,
            decimal precioReferencia,
            decimal? precioLista,
            string codigo,
            string origenPrecio,
            decimal? descuentoPct,
            byte redondeoModo,
            DateTime? vigenciaInicio,
            DateTime? vigenciaFin,
            string mensaje)
        {
            decimal roundedReference = decimal.Round(precioReferencia, 2, MidpointRounding.AwayFromZero);
            decimal effectiveDiscount = descuentoPct.GetValueOrDefault(0m);
            decimal subtotal = decimal.Round(roundedReference * (1m - effectiveDiscount / 100m), 2, MidpointRounding.AwayFromZero);
            decimal final = ApplyRounding(subtotal, roundedReference, redondeoModo);
            DateTime now = DateTime.UtcNow;
            return new ListaPreciosResolutionResult
            {
                Resuelto = true,
                TenantId = idEmpresa,
                PrecioEfectivo = final,
                PrecioBase = roundedReference,
                PrecioLista = precioLista,
                OrigenPrecio = origenPrecio,
                DescuentoPct = descuentoPct,
                SubtotalAntesRedondeo = subtotal,
                RedondeoModo = redondeoModo,
                PrecioFinal = final,
                VigenciaInicio = vigenciaInicio,
                VigenciaFin = vigenciaFin,
                CodigoResolucion = codigo,
                Origen = origenPrecio,
                ListaSolicitada = requestedLevel,
                ListaEfectiva = effectiveLevel,
                IdListaPrecio = idLista,
                TipoIdentidad = validation.Identity!.TipoIdentidad,
                IdProductoServicio = validation.Identity.IdProductoServicio,
                IdVariante = validation.Identity.IdVariante,
                IdPresentacionVenta = validation.Identity.IdPresentacionVenta,
                IdDetallePrecio = idDetalle,
                Mensaje = mensaje,
                PromocionEvaluada = false,
                PromocionAplicada = false,
                PromocionCodigo = string.Empty,
                FechaResolucionUtc = now,
                CorrelationId = Guid.NewGuid(),
                ReglaVersion = ListaPreciosConstants.ReglaVersionLp08
            };
        }

        private static decimal ApplyRounding(decimal subtotal, decimal precioBase, byte redondeoModo)
        {
            decimal result = redondeoModo switch
            {
                ListaPreciosConstants.RedondeoSinRedondeo => decimal.Round(subtotal, 2, MidpointRounding.AwayFromZero),
                ListaPreciosConstants.RedondeoA49 => RoundUpToCommercial49(subtotal),
                ListaPreciosConstants.RedondeoSolo9 => RoundUpTo9(subtotal),
                _ => throw new InvalidOperationException(ListaPreciosResolutionCodes.RedondeoInvalido)
            };

            return result > precioBase ? decimal.Round(precioBase, 2, MidpointRounding.AwayFromZero) : result;
        }

        private static decimal RoundUpToCommercial49(decimal value)
        {
            int entero = (int)Math.Ceiling(value);
            int ultimo = entero % 10;
            int rounded = ultimo <= 4 ? entero + (4 - ultimo) : entero + (9 - ultimo);
            return decimal.Round(rounded, 2, MidpointRounding.AwayFromZero);
        }

        private static decimal RoundUpTo9(decimal value)
        {
            int entero = (int)Math.Ceiling(value);
            int ultimo = entero % 10;
            int rounded = ultimo == 9 ? entero : entero + (9 - ultimo);
            return decimal.Round(rounded, 2, MidpointRounding.AwayFromZero);
        }

        private static ListaPreciosResolutionResult Invalid(ListaPreciosResolverRequest? request, string code, string message, Guid idEmpresa = default)
        {
            int requested = request?.Nivel ?? ListaPreciosConstants.NivelDefault;
            int effective = TryGetValidLevel(request?.Nivel, out int validLevel) ? validLevel : ListaPreciosConstants.NivelDefault;
            return new ListaPreciosResolutionResult
            {
                Resuelto = false,
                CodigoResolucion = code,
                Origen = code,
                TenantId = idEmpresa,
                ListaSolicitada = requested,
                ListaEfectiva = effective,
                IdProductoServicio = request?.IdProductoServicio ?? Guid.Empty,
                IdVariante = request?.IdVariante,
                IdPresentacionVenta = request?.IdPresentacionVenta,
                Mensaje = message,
                FechaResolucionUtc = DateTime.UtcNow,
                CorrelationId = Guid.NewGuid()
            };
        }

        private static decimal? CalculateMatrixFinal(decimal? price, decimal? discountPct, byte roundingMode)
        {
            if (!price.HasValue)
                return null;

            decimal roundedPrice = decimal.Round(price.Value, 2, MidpointRounding.AwayFromZero);
            decimal subtotal = decimal.Round(roundedPrice * (1m - discountPct.GetValueOrDefault() / 100m), 2, MidpointRounding.AwayFromZero);
            return ApplyRounding(subtotal, roundedPrice, roundingMode);
        }

        private sealed class IdentityValidation
        {
            public bool IsValid { get; init; }
            public string Code { get; init; } = string.Empty;
            public string Message { get; init; } = string.Empty;
            public ListaPreciosIdentityKey? Identity { get; init; }
            public ListaPreciosProductoRecord? Product { get; init; }
            public ListaPreciosVarianteRecord? Variante { get; init; }
            public ListaPreciosPresentacionVentaRecord? PresentacionVenta { get; init; }

            public static IdentityValidation Fail(string code, string message) => new() { IsValid = false, Code = code, Message = message };

            public static IdentityValidation Pass(
                ListaPreciosIdentityKey identity,
                ListaPreciosProductoRecord product,
                ListaPreciosVarianteRecord? variant,
                ListaPreciosPresentacionVentaRecord? presentation) =>
                new() { IsValid = true, Identity = identity, Product = product, Variante = variant, PresentacionVenta = presentation };
        }
    }

    public sealed class SqlListaPreciosRepository : IListaPreciosRepository
    {
        private readonly ITenantSqlConnectionFactory _connectionFactory;
        private readonly TenantDatabaseDescriptor _descriptor;

        public SqlListaPreciosRepository(ITenantSqlConnectionFactory connectionFactory, TenantDatabaseDescriptor descriptor)
        {
            _connectionFactory = connectionFactory;
            _descriptor = descriptor;
        }

        public async Task<ListaPreciosProductoRecord?> ObtenerProductoAsync(Guid idEmpresa, Guid idProductoServicio, CancellationToken cancellationToken = default)
        {
            await using SqlConnection connection = _connectionFactory.CreateConnection(_descriptor);
            await connection.OpenAsync(cancellationToken);
            using SqlCommand command = new("SELECT id, Tipo, Activo, PrecioPublico, CausaInventario FROM dbo.ProductosServicios WHERE idEmpresa=@IdEmpresa AND id=@Id", connection);
            command.Parameters.Add("@IdEmpresa", SqlDbType.UniqueIdentifier).Value = idEmpresa;
            command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = idProductoServicio;
            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken)
                ? new ListaPreciosProductoRecord(reader.GetGuid(0), Convert.ToByte(reader["Tipo"]), Convert.ToBoolean(reader["Activo"]), Convert.ToDecimal(reader["PrecioPublico"]), Convert.ToBoolean(reader["CausaInventario"]))
                : null;
        }

        public async Task<ListaPreciosVarianteRecord?> ObtenerVarianteAsync(Guid idEmpresa, Guid idVariante, CancellationToken cancellationToken = default)
        {
            await using SqlConnection connection = _connectionFactory.CreateConnection(_descriptor);
            await connection.OpenAsync(cancellationToken);
            using SqlCommand command = new("SELECT id, idProductoServicio, Activo, PrecioPublico FROM dbo.ProductosServiciosVariantes WHERE idEmpresa=@IdEmpresa AND id=@Id", connection);
            command.Parameters.Add("@IdEmpresa", SqlDbType.UniqueIdentifier).Value = idEmpresa;
            command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = idVariante;
            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken)
                ? new ListaPreciosVarianteRecord(reader.GetGuid(0), reader.GetGuid(1), Convert.ToBoolean(reader["Activo"]), reader["PrecioPublico"] == DBNull.Value ? null : Convert.ToDecimal(reader["PrecioPublico"]))
                : null;
        }

        public async Task<ListaPreciosPresentacionVentaRecord?> ObtenerPresentacionVentaAsync(Guid idEmpresa, Guid idPresentacionVenta, CancellationToken cancellationToken = default)
        {
            await using SqlConnection connection = _connectionFactory.CreateConnection(_descriptor);
            await connection.OpenAsync(cancellationToken);
            using SqlCommand command = new("SELECT id, idProductoServicio, Activo, Precio FROM dbo.ProductosServiciosPresentacionesVenta WHERE idEmpresa=@IdEmpresa AND id=@Id", connection);
            command.Parameters.Add("@IdEmpresa", SqlDbType.UniqueIdentifier).Value = idEmpresa;
            command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = idPresentacionVenta;
            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken)
                ? new ListaPreciosPresentacionVentaRecord(reader.GetGuid(0), reader.GetGuid(1), Convert.ToBoolean(reader["Activo"]), Convert.ToDecimal(reader["Precio"]))
                : null;
        }

        public async Task<ListaPreciosListaRecord?> ObtenerListaPorNivelAsync(Guid idEmpresa, int nivel, CancellationToken cancellationToken = default)
        {
            await using SqlConnection connection = _connectionFactory.CreateConnection(_descriptor);
            await connection.OpenAsync(cancellationToken);
            using SqlCommand command = new(@"
SELECT id, Nivel, Nombre, EsDefault, Activo
FROM dbo.ListaPreciosListas
WHERE idEmpresa=@IdEmpresa AND Nivel=@Nivel AND Activo=1 AND FechaArchivado IS NULL", connection);
            command.Parameters.Add("@IdEmpresa", SqlDbType.UniqueIdentifier).Value = idEmpresa;
            command.Parameters.Add("@Nivel", SqlDbType.TinyInt).Value = nivel;
            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken)
                ? new ListaPreciosListaRecord(reader.GetGuid(0), Convert.ToInt32(reader["Nivel"]), Convert.ToString(reader["Nombre"]) ?? string.Empty, Convert.ToBoolean(reader["EsDefault"]), Convert.ToBoolean(reader["Activo"]))
                : null;
        }

        public async Task<IReadOnlyList<ListaPreciosListaDto>> ObtenerListasAsync(Guid idEmpresa, CancellationToken cancellationToken = default)
        {
            await using SqlConnection connection = _connectionFactory.CreateConnection(_descriptor);
            await connection.OpenAsync(cancellationToken);
            using SqlCommand command = new(@"
SELECT id, Nivel, Nombre, EsDefault, Activo
FROM dbo.ListaPreciosListas
WHERE idEmpresa=@IdEmpresa AND FechaArchivado IS NULL
ORDER BY Nivel", connection);
            command.Parameters.Add("@IdEmpresa", SqlDbType.UniqueIdentifier).Value = idEmpresa;
            List<ListaPreciosListaDto> result = new();
            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(new ListaPreciosListaDto
                {
                    Id = reader.GetGuid(0),
                    Nivel = Convert.ToInt32(reader["Nivel"]),
                    Nombre = Convert.ToString(reader["Nombre"]) ?? string.Empty,
                    EsDefault = Convert.ToBoolean(reader["EsDefault"]),
                    Activo = Convert.ToBoolean(reader["Activo"])
                });
            }

            return result;
        }

        public async Task<IReadOnlyList<ListaPreciosComboItemDto>> ObtenerCategoriasAsync(Guid idEmpresa, CancellationToken cancellationToken = default)
        {
            return await ObtenerCatalogoComboAsync(idEmpresa, "dbo.ProductosServiciosCategorias", cancellationToken);
        }

        public async Task<IReadOnlyList<ListaPreciosComboItemDto>> ObtenerMarcasAsync(Guid idEmpresa, CancellationToken cancellationToken = default)
        {
            return await ObtenerCatalogoComboAsync(idEmpresa, "dbo.ProductosServiciosMarcas", cancellationToken);
        }

        public async Task<IReadOnlyList<ListaPreciosComboItemDto>> ObtenerColeccionesAsync(Guid idEmpresa, CancellationToken cancellationToken = default)
        {
            return await ObtenerCatalogoComboAsync(idEmpresa, "dbo.ProductosServiciosColecciones", cancellationToken);
        }

        public async Task<IReadOnlyList<ListaPreciosComboItemDto>> ObtenerEtiquetasAsync(Guid idEmpresa, CancellationToken cancellationToken = default)
        {
            return await ObtenerCatalogoComboAsync(idEmpresa, "dbo.ProductosServiciosTags", cancellationToken);
        }

        public async Task<IReadOnlyList<ListaPreciosComboItemDto>> ObtenerAtributosAsync(Guid idEmpresa, CancellationToken cancellationToken = default)
        {
            return await ObtenerCatalogoComboAsync(idEmpresa, "dbo.ProductosServiciosAtributos", cancellationToken);
        }

        public async Task<IReadOnlyList<ListaPreciosComboItemDto>> ObtenerValoresAtributosAsync(Guid idEmpresa, CancellationToken cancellationToken = default)
        {
            await using SqlConnection connection = _connectionFactory.CreateConnection(_descriptor);
            await connection.OpenAsync(cancellationToken);
            using SqlCommand command = new(@"
SELECT av.id, av.idAtributo, av.Valor
FROM dbo.ProductosServiciosAtributosValores av
INNER JOIN dbo.ProductosServiciosAtributos a ON a.idEmpresa=av.idEmpresa AND a.id=av.idAtributo AND a.Activo=1
WHERE av.idEmpresa=@IdEmpresa AND av.Activo=1
ORDER BY av.idAtributo, av.Orden, av.Valor", connection);
            command.Parameters.Add("@IdEmpresa", SqlDbType.UniqueIdentifier).Value = idEmpresa;
            List<ListaPreciosComboItemDto> result = new();
            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                Guid id = reader.GetGuid(0);
                result.Add(new ListaPreciosComboItemDto
                {
                    Id = id,
                    ParentId = reader.GetGuid(1),
                    Clave = id.ToString(),
                    Nombre = Convert.ToString(reader["Valor"]) ?? string.Empty
                });
            }
            return result;
        }

        public async Task<IReadOnlyList<ListaPreciosComboItemDto>> ObtenerVariantesAsync(Guid idEmpresa, CancellationToken cancellationToken = default)
        {
            return await ObtenerIdentidadComboAsync(
                idEmpresa,
                "dbo.ProductosServiciosVariantes",
                "CONCAT(ps.Nombre, N' · ', src.Nombre)",
                "src.idProductoServicio=ps.id",
                cancellationToken);
        }

        public async Task<IReadOnlyList<ListaPreciosComboItemDto>> ObtenerPresentacionesVentaAsync(Guid idEmpresa, CancellationToken cancellationToken = default)
        {
            await using SqlConnection connection = _connectionFactory.CreateConnection(_descriptor);
            await connection.OpenAsync(cancellationToken);
            using SqlCommand command = new(@"
WITH Presentaciones AS
(
    SELECT pv.id,
           ps.Nombre AS Producto,
           pv.Nombre,
           pv.CantidadVenta,
           COALESCE(NULLIF(um.Abreviatura, N''), um.Nombre) AS Unidad,
           pv.Precio,
           pv.Orden,
           COUNT(1) OVER
           (
               PARTITION BY pv.idProductoServicio, pv.Nombre, pv.CantidadVenta, pv.idUnidadVenta
           ) AS Coincidencias
    FROM dbo.ProductosServiciosPresentacionesVenta pv
    INNER JOIN dbo.ProductosServicios ps ON ps.idEmpresa=pv.idEmpresa AND ps.id=pv.idProductoServicio
    INNER JOIN dbo.ProductosServiciosUnidadesMedida um ON um.idEmpresa=pv.idEmpresa AND um.id=pv.idUnidadVenta
    WHERE pv.idEmpresa=@IdEmpresa AND pv.Activo=1 AND ps.Tipo=1 AND ps.Activo=1
)
SELECT id,
       CONCAT(
           Producto, N' · ', Nombre, N' · ', CONVERT(nvarchar(32), CAST(CantidadVenta AS float)), N' ', Unidad,
           CASE WHEN Coincidencias > 1
                THEN CONCAT(N' · Precio base ', CONVERT(nvarchar(32), CAST(Precio AS decimal(18, 2))),
                            N' · Orden ', CONVERT(nvarchar(16), Orden),
                            N' · Ref ', LEFT(CONVERT(nvarchar(36), id), 8))
                ELSE N'' END
       ) AS Nombre
FROM Presentaciones
ORDER BY Producto, Nombre, CantidadVenta, Unidad, Orden, id", connection);
            command.Parameters.Add("@IdEmpresa", SqlDbType.UniqueIdentifier).Value = idEmpresa;
            List<ListaPreciosComboItemDto> result = new();
            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                Guid id = reader.GetGuid(0);
                result.Add(new ListaPreciosComboItemDto { Id = id, Clave = id.ToString(), Nombre = Convert.ToString(reader["Nombre"]) ?? string.Empty });
            }
            return result;
        }

        public async Task<IReadOnlyList<ListaPreciosComboItemDto>> ObtenerSucursalesAsync(Guid idEmpresa, CancellationToken cancellationToken = default)
        {
            await using SqlConnection connection = _connectionFactory.CreateConnection(_descriptor);
            await connection.OpenAsync(cancellationToken);
            using SqlCommand command = new(@"
SELECT id, Nombre
FROM dbo.Sucursales
WHERE idEmpresa=@IdEmpresa AND ISNULL(borrado, 0)=0
ORDER BY Nombre", connection);
            command.Parameters.Add("@IdEmpresa", SqlDbType.UniqueIdentifier).Value = idEmpresa;
            List<ListaPreciosComboItemDto> result = new();
            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                Guid id = reader.GetGuid(0);
                result.Add(new ListaPreciosComboItemDto { Id = id, Clave = id.ToString(), Nombre = Convert.ToString(reader["Nombre"]) ?? string.Empty });
            }

            return result;
        }

        public async Task<IReadOnlyList<ListaPreciosConsultaIdentityRecord>> ConsultarIdentidadesAsync(Guid idEmpresa, ListaPreciosConsultaRequest request, CancellationToken cancellationToken = default)
        {
            await using SqlConnection connection = _connectionFactory.CreateConnection(_descriptor);
            await connection.OpenAsync(cancellationToken);

            List<Guid> sucursales = request.IdsSucursales
                .Where(x => x != Guid.Empty)
                .Distinct()
                .ToList();
            if (sucursales.Count == 0 && request.IdSucursal.HasValue && request.IdSucursal.Value != Guid.Empty)
                sucursales.Add(request.IdSucursal.Value);
            string sucursalesSql = sucursales.Count == 0
                ? string.Empty
                : " AND idSucursal IN (" + string.Join(",", sucursales.Select((_, index) => $"@Sucursal{index}")) + ")";

            StringBuilder query = new($@"
WITH Identidades AS (
    SELECT
        ps.id AS IdProductoServicio,
        CAST(NULL AS uniqueidentifier) AS IdVariante,
        CAST(NULL AS uniqueidentifier) AS IdPresentacionVenta,
        CASE ps.Tipo WHEN 2 THEN CAST(2 AS tinyint) ELSE CAST(1 AS tinyint) END AS TipoIdentidad,
        CASE ps.Tipo WHEN 2 THEN N'Servicio' ELSE N'Producto base' END AS TipoIdentidadNombre,
        ps.Tipo,
        CASE ps.Tipo WHEN 2 THEN N'Servicio' ELSE N'Producto' END AS TipoNombre,
        ISNULL(ps.Codigo, N'') AS Codigo,
        ps.Nombre AS Nombre,
        ps.Nombre AS ProductoPadre,
        ps.idCategoria,
        cat.Nombre AS Categoria,
        ps.idMarca,
        ISNULL(m.Nombre, N'') AS Marca,
        ps.idColeccion,
        ISNULL(col.Nombre, N'') AS Coleccion,
        CASE ps.Tipo WHEN 2 THEN N'Servicio' ELSE N'Producto base' END AS IdentidadVendible,
        ISNULL(ps.ImagenUrl, N'') AS ImagenUrl,
        ISNULL(ps.ImagenNombre, N'') AS ImagenNombre,
        CASE WHEN ISNULL(ps.ImagenUrl, N'') <> N'' THEN CASE ps.Tipo WHEN 2 THEN N'Servicio' ELSE N'Producto' END ELSE N'' END AS ImagenOrigen,
        ISNULL(ps.Descripcion, N'') AS Descripcion,
        ps.Activo,
        ps.Activo AS ProductoActivo,
        ps.CausaInventario,
        CONVERT(decimal(18,2), ps.PrecioPublico) AS PrecioBase,
        CONVERT(decimal(18,2), ps.Costo) AS Costo,
        ps.idEmpresa
    FROM dbo.ProductosServicios ps
    INNER JOIN dbo.ProductosServiciosCategorias cat ON cat.idEmpresa=ps.idEmpresa AND cat.id=ps.idCategoria
    LEFT JOIN dbo.ProductosServiciosMarcas m ON m.idEmpresa=ps.idEmpresa AND m.id=ps.idMarca
    LEFT JOIN dbo.ProductosServiciosColecciones col ON col.idEmpresa=ps.idEmpresa AND col.id=ps.idColeccion
    WHERE ps.idEmpresa=@IdEmpresa

    UNION ALL

    SELECT
        ps.id AS IdProductoServicio,
        pv.id AS IdVariante,
        CAST(NULL AS uniqueidentifier) AS IdPresentacionVenta,
        CAST(3 AS tinyint) AS TipoIdentidad,
        N'Variante' AS TipoIdentidadNombre,
        ps.Tipo,
        N'Producto' AS TipoNombre,
        ISNULL(NULLIF(pv.Sku, N''), ISNULL(ps.Codigo, N'')) AS Codigo,
        CONCAT(ps.Nombre, N' · ', pv.Nombre) AS Nombre,
        ps.Nombre AS ProductoPadre,
        ps.idCategoria,
        cat.Nombre AS Categoria,
        ps.idMarca,
        ISNULL(m.Nombre, N'') AS Marca,
        ps.idColeccion,
        ISNULL(col.Nombre, N'') AS Coleccion,
        CONCAT(N'Variante · ', pv.Nombre) AS IdentidadVendible,
        COALESCE(NULLIF(pv.ImagenUrl, N''), NULLIF(ps.ImagenUrl, N''), N'') AS ImagenUrl,
        COALESCE(NULLIF(pv.ImagenNombre, N''), NULLIF(ps.ImagenNombre, N''), N'') AS ImagenNombre,
        CASE
            WHEN NULLIF(pv.ImagenUrl, N'') IS NOT NULL THEN N'Variante'
            WHEN NULLIF(ps.ImagenUrl, N'') IS NOT NULL THEN N'Producto'
            ELSE N''
        END AS ImagenOrigen,
        ISNULL(ps.Descripcion, N'') AS Descripcion,
        pv.Activo,
        ps.Activo AS ProductoActivo,
        ps.CausaInventario,
        CONVERT(decimal(18,2), COALESCE(pv.PrecioPublico, ps.PrecioPublico)) AS PrecioBase,
        CONVERT(decimal(18,2), ps.Costo) AS Costo,
        ps.idEmpresa
    FROM dbo.ProductosServiciosVariantes pv
    INNER JOIN dbo.ProductosServicios ps ON ps.idEmpresa=pv.idEmpresa AND ps.id=pv.idProductoServicio
    INNER JOIN dbo.ProductosServiciosCategorias cat ON cat.idEmpresa=ps.idEmpresa AND cat.id=ps.idCategoria
    LEFT JOIN dbo.ProductosServiciosMarcas m ON m.idEmpresa=ps.idEmpresa AND m.id=ps.idMarca
    LEFT JOIN dbo.ProductosServiciosColecciones col ON col.idEmpresa=ps.idEmpresa AND col.id=ps.idColeccion
    WHERE pv.idEmpresa=@IdEmpresa AND ps.Tipo=1

    UNION ALL

    SELECT
        ps.id AS IdProductoServicio,
        CAST(NULL AS uniqueidentifier) AS IdVariante,
        pv.id AS IdPresentacionVenta,
        CAST(4 AS tinyint) AS TipoIdentidad,
        N'Presentación venta' AS TipoIdentidadNombre,
        ps.Tipo,
        N'Producto' AS TipoNombre,
        ISNULL(ps.Codigo, N'') AS Codigo,
        CONCAT(ps.Nombre, N' · ', pv.Nombre) AS Nombre,
        ps.Nombre AS ProductoPadre,
        ps.idCategoria,
        cat.Nombre AS Categoria,
        ps.idMarca,
        ISNULL(m.Nombre, N'') AS Marca,
        ps.idColeccion,
        ISNULL(col.Nombre, N'') AS Coleccion,
        CONCAT(N'Presentación · ', pv.Nombre) AS IdentidadVendible,
        ISNULL(ps.ImagenUrl, N'') AS ImagenUrl,
        ISNULL(ps.ImagenNombre, N'') AS ImagenNombre,
        CASE WHEN ISNULL(ps.ImagenUrl, N'') <> N'' THEN N'Producto' ELSE N'' END AS ImagenOrigen,
        ISNULL(ps.Descripcion, N'') AS Descripcion,
        pv.Activo,
        ps.Activo AS ProductoActivo,
        ps.CausaInventario,
        CONVERT(decimal(18,2), pv.Precio) AS PrecioBase,
        CONVERT(decimal(18,2), ps.Costo) AS Costo,
        ps.idEmpresa
    FROM dbo.ProductosServiciosPresentacionesVenta pv
    INNER JOIN dbo.ProductosServicios ps ON ps.idEmpresa=pv.idEmpresa AND ps.id=pv.idProductoServicio
    INNER JOIN dbo.ProductosServiciosCategorias cat ON cat.idEmpresa=ps.idEmpresa AND cat.id=ps.idCategoria
    LEFT JOIN dbo.ProductosServiciosMarcas m ON m.idEmpresa=ps.idEmpresa AND m.id=ps.idMarca
    LEFT JOIN dbo.ProductosServiciosColecciones col ON col.idEmpresa=ps.idEmpresa AND col.id=ps.idColeccion
    WHERE pv.idEmpresa=@IdEmpresa AND ps.Tipo=1
), Inventario AS (
    SELECT idProductoServicio, idVariante, SUM(CantidadBaseActual) AS Existencia
    FROM dbo.InventarioSaldos
    WHERE idEmpresa=@IdEmpresa{sucursalesSql}
    GROUP BY idProductoServicio, idVariante
), Matriz AS (
    SELECT
        d.TipoIdentidad, d.idProductoServicio, d.idVariante, d.idPresentacionVenta,
        MAX(CASE WHEN l.Nivel=@Nivel THEN d.id END) AS IdDetalleSeleccionado,
        MAX(CASE WHEN l.Nivel=@Nivel THEN d.Precio END) AS PrecioSeleccionado,
        MAX(CASE WHEN l.Nivel=@Nivel THEN d.DescuentoPct END) AS DescuentoSeleccionado,
        MAX(CASE WHEN l.Nivel=@Nivel THEN d.RedondeoModo END) AS RedondeoSeleccionado,
        MAX(CASE WHEN l.Nivel=@Nivel THEN d.VigenciaInicio END) AS VigenciaInicioSeleccionada,
        MAX(CASE WHEN l.Nivel=@Nivel THEN d.VigenciaFin END) AS VigenciaFinSeleccionada,
        MAX(CASE WHEN l.Nivel=1 THEN d.Precio END) P1, MAX(CASE WHEN l.Nivel=1 THEN d.DescuentoPct END) D1, ISNULL(MAX(CASE WHEN l.Nivel=1 THEN d.RedondeoModo END), 0) R1,
        MAX(CASE WHEN l.Nivel=2 THEN d.Precio END) P2, MAX(CASE WHEN l.Nivel=2 THEN d.DescuentoPct END) D2, ISNULL(MAX(CASE WHEN l.Nivel=2 THEN d.RedondeoModo END), 0) R2,
        MAX(CASE WHEN l.Nivel=3 THEN d.Precio END) P3, MAX(CASE WHEN l.Nivel=3 THEN d.DescuentoPct END) D3, ISNULL(MAX(CASE WHEN l.Nivel=3 THEN d.RedondeoModo END), 0) R3,
        MAX(CASE WHEN l.Nivel=4 THEN d.Precio END) P4, MAX(CASE WHEN l.Nivel=4 THEN d.DescuentoPct END) D4, ISNULL(MAX(CASE WHEN l.Nivel=4 THEN d.RedondeoModo END), 0) R4,
        MAX(CASE WHEN l.Nivel=5 THEN d.Precio END) P5, MAX(CASE WHEN l.Nivel=5 THEN d.DescuentoPct END) D5, ISNULL(MAX(CASE WHEN l.Nivel=5 THEN d.RedondeoModo END), 0) R5,
        MAX(CASE WHEN l.Nivel=6 THEN d.Precio END) P6, MAX(CASE WHEN l.Nivel=6 THEN d.DescuentoPct END) D6, ISNULL(MAX(CASE WHEN l.Nivel=6 THEN d.RedondeoModo END), 0) R6,
        MAX(CASE WHEN l.Nivel=7 THEN d.Precio END) P7, MAX(CASE WHEN l.Nivel=7 THEN d.DescuentoPct END) D7, ISNULL(MAX(CASE WHEN l.Nivel=7 THEN d.RedondeoModo END), 0) R7,
        MAX(CASE WHEN l.Nivel=8 THEN d.Precio END) P8, MAX(CASE WHEN l.Nivel=8 THEN d.DescuentoPct END) D8, ISNULL(MAX(CASE WHEN l.Nivel=8 THEN d.RedondeoModo END), 0) R8,
        MAX(CASE WHEN l.Nivel=9 THEN d.Precio END) P9, MAX(CASE WHEN l.Nivel=9 THEN d.DescuentoPct END) D9, ISNULL(MAX(CASE WHEN l.Nivel=9 THEN d.RedondeoModo END), 0) R9,
        MAX(CASE WHEN l.Nivel=10 THEN d.Precio END) P10, MAX(CASE WHEN l.Nivel=10 THEN d.DescuentoPct END) D10, ISNULL(MAX(CASE WHEN l.Nivel=10 THEN d.RedondeoModo END), 0) R10
    FROM dbo.ListaPreciosDetalle d
    INNER JOIN dbo.ListaPreciosListas l ON l.idEmpresa=d.idEmpresa AND l.id=d.idListaPrecio AND l.Activo=1 AND l.FechaArchivado IS NULL
    WHERE d.idEmpresa=@IdEmpresa AND d.Activo=1 AND d.FechaArchivado IS NULL
      AND (d.VigenciaInicio IS NULL OR d.VigenciaInicio <= CONVERT(date, SYSUTCDATETIME()))
      AND (d.VigenciaFin IS NULL OR d.VigenciaFin >= CONVERT(date, SYSUTCDATETIME()))
    GROUP BY d.TipoIdentidad, d.idProductoServicio, d.idVariante, d.idPresentacionVenta
)
SELECT *
FROM (
SELECT
    i.IdProductoServicio,
    i.IdVariante,
    i.IdPresentacionVenta,
    i.TipoIdentidad,
    TipoIdentidadNombre,
    Tipo,
    TipoNombre,
    Codigo,
    Nombre,
    ProductoPadre,
    idCategoria,
    Categoria,
    idMarca,
    Marca,
    idColeccion,
    Coleccion,
    IdentidadVendible,
    ImagenUrl,
    ImagenNombre,
    ImagenOrigen,
    Descripcion,
    Activo,
    ProductoActivo,
    i.PrecioBase,
    i.Costo,
    CAST(CASE WHEN i.Tipo=1 AND i.CausaInventario=1 THEN 1 ELSE 0 END AS bit) AS InventarioAplicable,
    CASE WHEN i.Tipo=1 AND i.CausaInventario=1 THEN ISNULL(inv.Existencia, 0) ELSE NULL END AS Existencia,
    i.idEmpresa,
    mx.IdDetalleSeleccionado, mx.PrecioSeleccionado, mx.DescuentoSeleccionado,
    ISNULL(mx.RedondeoSeleccionado, 0) AS RedondeoSeleccionado,
    mx.VigenciaInicioSeleccionada, mx.VigenciaFinSeleccionada,
    mx.P1, mx.D1, ISNULL(mx.R1, 0) R1, mx.P2, mx.D2, ISNULL(mx.R2, 0) R2,
    mx.P3, mx.D3, ISNULL(mx.R3, 0) R3, mx.P4, mx.D4, ISNULL(mx.R4, 0) R4,
    mx.P5, mx.D5, ISNULL(mx.R5, 0) R5, mx.P6, mx.D6, ISNULL(mx.R6, 0) R6,
    mx.P7, mx.D7, ISNULL(mx.R7, 0) R7, mx.P8, mx.D8, ISNULL(mx.R8, 0) R8,
    mx.P9, mx.D9, ISNULL(mx.R9, 0) R9, mx.P10, mx.D10, ISNULL(mx.R10, 0) R10
FROM Identidades i
LEFT JOIN Inventario inv
   ON inv.idProductoServicio=i.IdProductoServicio
   AND ((inv.idVariante IS NULL AND i.IdVariante IS NULL) OR inv.idVariante=i.IdVariante)
LEFT JOIN Matriz mx
    ON mx.TipoIdentidad=i.TipoIdentidad AND mx.idProductoServicio=i.IdProductoServicio
   AND ((mx.idVariante IS NULL AND i.IdVariante IS NULL) OR mx.idVariante=i.IdVariante)
   AND ((mx.idPresentacionVenta IS NULL AND i.IdPresentacionVenta IS NULL) OR mx.idPresentacionVenta=i.IdPresentacionVenta)
) Consulta
WHERE idEmpresa=@IdEmpresa");

            using SqlCommand command = new() { Connection = connection };
            command.Parameters.Add("@IdEmpresa", SqlDbType.UniqueIdentifier).Value = idEmpresa;
            command.Parameters.Add("@Nivel", SqlDbType.TinyInt).Value = request.Nivel.GetValueOrDefault(ListaPreciosConstants.NivelDefault);
            for (int index = 0; index < sucursales.Count; index++)
                command.Parameters.Add($"@Sucursal{index}", SqlDbType.UniqueIdentifier).Value = sucursales[index];

            if (!string.IsNullOrWhiteSpace(request.Busqueda))
            {
                query.Append(@"
  AND (Codigo LIKE @Busqueda OR Nombre LIKE @Busqueda OR ProductoPadre LIKE @Busqueda OR Categoria LIKE @Busqueda OR Marca LIKE @Busqueda OR Coleccion LIKE @Busqueda OR IdentidadVendible LIKE @Busqueda)");
                command.Parameters.Add("@Busqueda", SqlDbType.NVarChar, 250).Value = $"%{request.Busqueda.Trim()}%";
            }

            if (request.Tipo.HasValue && request.Tipo.Value is ListaPreciosConstants.TipoProducto or ListaPreciosConstants.TipoServicio)
            {
                query.Append(" AND Tipo=@Tipo");
                command.Parameters.Add("@Tipo", SqlDbType.TinyInt).Value = request.Tipo.Value;
            }

            if (request.IdCategoria.HasValue && request.IdCategoria.Value != Guid.Empty)
            {
                query.Append(" AND idCategoria=@IdCategoria");
                command.Parameters.Add("@IdCategoria", SqlDbType.UniqueIdentifier).Value = request.IdCategoria.Value;
            }

            if (request.IdMarca.HasValue && request.IdMarca.Value != Guid.Empty)
            {
                query.Append(" AND idMarca=@IdMarca");
                command.Parameters.Add("@IdMarca", SqlDbType.UniqueIdentifier).Value = request.IdMarca.Value;
            }

            if (request.IdColeccion.HasValue && request.IdColeccion.Value != Guid.Empty)
            {
                query.Append(" AND idColeccion=@IdColeccion");
                command.Parameters.Add("@IdColeccion", SqlDbType.UniqueIdentifier).Value = request.IdColeccion.Value;
            }

            if (request.IdEtiqueta.HasValue && request.IdEtiqueta.Value != Guid.Empty)
            {
                query.Append(@"
  AND EXISTS (
      SELECT 1
      FROM dbo.ProductosServiciosProductoTags pt
      INNER JOIN dbo.ProductosServiciosTags t ON t.idEmpresa=pt.idEmpresa AND t.id=pt.idTag AND t.Activo=1
      WHERE pt.idEmpresa=@IdEmpresa AND pt.idProductoServicio=Consulta.IdProductoServicio AND pt.idTag=@IdEtiqueta
  )");
                command.Parameters.Add("@IdEtiqueta", SqlDbType.UniqueIdentifier).Value = request.IdEtiqueta.Value;
            }

            if (request.IdAtributo.HasValue && request.IdAtributo.Value != Guid.Empty)
            {
                query.Append(@"
  AND EXISTS (
      SELECT 1
      FROM dbo.ProductosServiciosProductoAtributos pa
      INNER JOIN dbo.ProductosServiciosAtributos a ON a.idEmpresa=pa.idEmpresa AND a.id=pa.idAtributo AND a.Activo=1
      WHERE pa.idEmpresa=@IdEmpresa AND pa.idProductoServicio=Consulta.IdProductoServicio AND pa.idAtributo=@IdAtributo AND pa.Activo=1
  )");
                command.Parameters.Add("@IdAtributo", SqlDbType.UniqueIdentifier).Value = request.IdAtributo.Value;
            }

            List<Guid> valoresAtributo = request.IdsValoresAtributo.Where(x => x != Guid.Empty).Distinct().ToList();
            if (valoresAtributo.Count > 0)
            {
                query.Append(@"
  AND EXISTS (
      SELECT 1
      FROM dbo.ProductosServiciosProductoAtributos pa
      INNER JOIN dbo.ProductosServiciosProductoAtributoValores pav
        ON pav.idEmpresa=pa.idEmpresa AND pav.idProductoAtributo=pa.id AND pav.Activo=1
      INNER JOIN dbo.ProductosServiciosAtributosValores av
        ON av.idEmpresa=pav.idEmpresa AND av.id=pav.idAtributoValor AND av.Activo=1
      WHERE pa.idEmpresa=@IdEmpresa AND pa.idProductoServicio=Consulta.IdProductoServicio AND pa.Activo=1
        AND pa.idAtributo=@IdAtributoValorFiltro
        AND pav.idAtributoValor IN (" + string.Join(",", valoresAtributo.Select((_, index) => $"@AtributoValor{index}")) + @")
  )");
                command.Parameters.Add("@IdAtributoValorFiltro", SqlDbType.UniqueIdentifier).Value = request.IdAtributo!.Value;
                for (int index = 0; index < valoresAtributo.Count; index++)
                    command.Parameters.Add($"@AtributoValor{index}", SqlDbType.UniqueIdentifier).Value = valoresAtributo[index];
            }

            if (request.IdVariante.HasValue && request.IdVariante.Value != Guid.Empty)
            {
                query.Append(" AND IdVariante=@IdVariante");
                command.Parameters.Add("@IdVariante", SqlDbType.UniqueIdentifier).Value = request.IdVariante.Value;
            }

            if (request.IdPresentacionVenta.HasValue && request.IdPresentacionVenta.Value != Guid.Empty)
            {
                query.Append(" AND IdPresentacionVenta=@IdPresentacionVenta");
                command.Parameters.Add("@IdPresentacionVenta", SqlDbType.UniqueIdentifier).Value = request.IdPresentacionVenta.Value;
            }

            string existencia = (request.Existencia ?? string.Empty).Trim().ToLowerInvariant();
            if (existencia == "con")
            {
                query.Append(" AND InventarioAplicable=1 AND Existencia>0");
            }
            else if (existencia == "sin")
            {
                query.Append(" AND InventarioAplicable=1 AND Existencia=0");
            }

            if (request.CantidadMenorA.HasValue)
            {
                query.Append(" AND InventarioAplicable=1 AND Existencia<@CantidadMenorA");
                command.Parameters.Add("@CantidadMenorA", SqlDbType.Decimal).Value = request.CantidadMenorA.Value;
                command.Parameters["@CantidadMenorA"].Precision = 18;
                command.Parameters["@CantidadMenorA"].Scale = 6;
            }

            string estatus = string.IsNullOrWhiteSpace(request.Estatus) ? "activos" : request.Estatus.Trim().ToLowerInvariant();
            if (estatus == "activos")
            {
                query.Append(" AND Activo=1 AND ProductoActivo=1");
            }
            else if (estatus == "inactivos")
            {
                query.Append(" AND (Activo=0 OR ProductoActivo=0)");
            }

            query.Append(" ORDER BY ProductoPadre, TipoIdentidad, Nombre");
            command.CommandText = query.ToString();

            List<ListaPreciosConsultaIdentityRecord> result = new();
            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(new ListaPreciosConsultaIdentityRecord(
                    reader.GetGuid(0),
                    reader.IsDBNull(1) ? null : reader.GetGuid(1),
                    reader.IsDBNull(2) ? null : reader.GetGuid(2),
                    Convert.ToByte(reader["TipoIdentidad"]),
                    Convert.ToString(reader["TipoIdentidadNombre"]) ?? string.Empty,
                    Convert.ToByte(reader["Tipo"]),
                    Convert.ToString(reader["TipoNombre"]) ?? string.Empty,
                    Convert.ToString(reader["Codigo"]) ?? string.Empty,
                    Convert.ToString(reader["Nombre"]) ?? string.Empty,
                    Convert.ToString(reader["ProductoPadre"]) ?? string.Empty,
                    reader.IsDBNull(10) ? null : reader.GetGuid(10),
                    Convert.ToString(reader["Categoria"]) ?? string.Empty,
                    reader.IsDBNull(12) ? null : reader.GetGuid(12),
                    Convert.ToString(reader["Marca"]) ?? string.Empty,
                    reader.IsDBNull(14) ? null : reader.GetGuid(14),
                    Convert.ToString(reader["Coleccion"]) ?? string.Empty,
                    Convert.ToString(reader["IdentidadVendible"]) ?? string.Empty,
                    Convert.ToString(reader["ImagenUrl"]) ?? string.Empty,
                    Convert.ToString(reader["ImagenNombre"]) ?? string.Empty,
                    Convert.ToString(reader["ImagenOrigen"]) ?? string.Empty,
                    Convert.ToBoolean(reader["Activo"]),
                    Convert.ToBoolean(reader["ProductoActivo"]),
                    Convert.ToBoolean(reader["InventarioAplicable"]),
                    reader["Existencia"] == DBNull.Value ? null : Convert.ToDecimal(reader["Existencia"]),
                    Convert.ToDecimal(reader["PrecioBase"]),
                    DecimalOrNull(reader, "Costo"),
                    reader["IdDetalleSeleccionado"] == DBNull.Value ? null : reader.GetGuid(reader.GetOrdinal("IdDetalleSeleccionado")),
                    reader["PrecioSeleccionado"] == DBNull.Value ? null : Convert.ToDecimal(reader["PrecioSeleccionado"]),
                    reader["DescuentoSeleccionado"] == DBNull.Value ? null : Convert.ToDecimal(reader["DescuentoSeleccionado"]),
                    Convert.ToByte(reader["RedondeoSeleccionado"]),
                    reader["VigenciaInicioSeleccionada"] == DBNull.Value ? null : Convert.ToDateTime(reader["VigenciaInicioSeleccionada"]),
                    reader["VigenciaFinSeleccionada"] == DBNull.Value ? null : Convert.ToDateTime(reader["VigenciaFinSeleccionada"]),
                    DecimalOrNull(reader, "P1"), DecimalOrNull(reader, "D1"), Convert.ToByte(reader["R1"]),
                    DecimalOrNull(reader, "P2"), DecimalOrNull(reader, "D2"), Convert.ToByte(reader["R2"]),
                    DecimalOrNull(reader, "P3"), DecimalOrNull(reader, "D3"), Convert.ToByte(reader["R3"]),
                    DecimalOrNull(reader, "P4"), DecimalOrNull(reader, "D4"), Convert.ToByte(reader["R4"]),
                    DecimalOrNull(reader, "P5"), DecimalOrNull(reader, "D5"), Convert.ToByte(reader["R5"]),
                    DecimalOrNull(reader, "P6"), DecimalOrNull(reader, "D6"), Convert.ToByte(reader["R6"]),
                    DecimalOrNull(reader, "P7"), DecimalOrNull(reader, "D7"), Convert.ToByte(reader["R7"]),
                    DecimalOrNull(reader, "P8"), DecimalOrNull(reader, "D8"), Convert.ToByte(reader["R8"]),
                    DecimalOrNull(reader, "P9"), DecimalOrNull(reader, "D9"), Convert.ToByte(reader["R9"]),
                    DecimalOrNull(reader, "P10"), DecimalOrNull(reader, "D10"), Convert.ToByte(reader["R10"]),
                    Convert.ToString(reader["Descripcion"]) ?? string.Empty));
            }

            return result;
        }

        private static decimal? DecimalOrNull(SqlDataReader reader, string name)
            => reader[name] == DBNull.Value ? null : Convert.ToDecimal(reader[name]);

        public async Task<ListaPreciosInventarioDetalleDto> ObtenerInventarioDetalleAsync(Guid idEmpresa, ListaPreciosIdentityKey identity, CancellationToken cancellationToken = default)
        {
            Guid? inventarioVariante = identity.TipoIdentidad == ListaPreciosConstants.IdentidadVariante ? identity.IdVariante : null;
            await using SqlConnection connection = _connectionFactory.CreateConnection(_descriptor);
            await connection.OpenAsync(cancellationToken);

            List<ListaPreciosInventarioSaldoDto> saldos = new();
            using (SqlCommand saldosCommand = new(@"
SELECT s.idSucursal, ISNULL(su.Nombre, N'') AS Sucursal, SUM(s.CantidadBaseActual) AS Existencia
FROM dbo.InventarioSaldos s
INNER JOIN dbo.Sucursales su ON su.idEmpresa=s.idEmpresa AND su.id=s.idSucursal
WHERE s.idEmpresa=@IdEmpresa AND s.idProductoServicio=@IdProductoServicio
  AND ((s.idVariante IS NULL AND @IdVariante IS NULL) OR s.idVariante=@IdVariante)
GROUP BY s.idSucursal, su.Nombre
ORDER BY su.Nombre", connection))
            {
                AddInventoryParameters(saldosCommand, idEmpresa, identity.IdProductoServicio, inventarioVariante);
                using SqlDataReader reader = await saldosCommand.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    saldos.Add(new ListaPreciosInventarioSaldoDto(reader.GetGuid(0), Convert.ToString(reader["Sucursal"]) ?? string.Empty, Convert.ToDecimal(reader["Existencia"])));
                }
            }

            List<ListaPreciosInventarioMovimientoDto> movimientos = new();
            using (SqlCommand movimientosCommand = new(@"
SELECT TOP (100)
    m.id, m.idSucursal, ISNULL(su.Nombre, N'') AS Sucursal, m.TipoMovimiento, m.CantidadBase,
    m.SaldoAnterior, m.SaldoPosterior,
    CONCAT(m.OrigenTipo, CASE WHEN m.OrigenId IS NULL THEN N'' ELSE CONCAT(N' · ', CONVERT(nvarchar(36), m.OrigenId)) END) AS Referencia,
    ISNULL(m.Observaciones, N'') AS Observaciones, m.FechaMovimiento
FROM dbo.InventarioMovimientos m
INNER JOIN dbo.Sucursales su ON su.idEmpresa=m.idEmpresa AND su.id=m.idSucursal
WHERE m.idEmpresa=@IdEmpresa AND m.idProductoServicio=@IdProductoServicio
  AND ((m.idVariante IS NULL AND @IdVariante IS NULL) OR m.idVariante=@IdVariante)
ORDER BY m.FechaMovimiento DESC, m.id DESC", connection))
            {
                AddInventoryParameters(movimientosCommand, idEmpresa, identity.IdProductoServicio, inventarioVariante);
                using SqlDataReader reader = await movimientosCommand.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    byte tipo = Convert.ToByte(reader["TipoMovimiento"]);
                    movimientos.Add(new ListaPreciosInventarioMovimientoDto(
                        reader.GetGuid(0),
                        reader.GetGuid(1),
                        Convert.ToString(reader["Sucursal"]) ?? string.Empty,
                        tipo,
                        InventoryMovementName(tipo),
                        Convert.ToDecimal(reader["CantidadBase"]),
                        Convert.ToDecimal(reader["SaldoAnterior"]),
                        Convert.ToDecimal(reader["SaldoPosterior"]),
                        Convert.ToString(reader["Referencia"]) ?? string.Empty,
                        Convert.ToString(reader["Observaciones"]) ?? string.Empty,
                        Convert.ToDateTime(reader["FechaMovimiento"])));
                }
            }

            return new ListaPreciosInventarioDetalleDto
            {
                Aplicable = true,
                Mensaje = saldos.Count == 0 ? "Sin existencias registradas." : string.Empty,
                ExistenciaTotal = saldos.Sum(x => x.Existencia),
                Saldos = saldos,
                Movimientos = movimientos
            };
        }

        private static void AddInventoryParameters(SqlCommand command, Guid idEmpresa, Guid idProductoServicio, Guid? idVariante)
        {
            command.Parameters.Add("@IdEmpresa", SqlDbType.UniqueIdentifier).Value = idEmpresa;
            command.Parameters.Add("@IdProductoServicio", SqlDbType.UniqueIdentifier).Value = idProductoServicio;
            command.Parameters.Add("@IdVariante", SqlDbType.UniqueIdentifier).Value = Db(idVariante);
        }

        private static string InventoryMovementName(byte tipo) => tipo switch
        {
            1 => "Entrada",
            2 => "Salida",
            3 => "Ajuste positivo",
            4 => "Ajuste negativo",
            5 => "Reserva",
            _ => "Movimiento"
        };

        public async Task<ListaPreciosDetalleRecord?> ObtenerPrecioActivoAsync(Guid idEmpresa, Guid idListaPrecio, ListaPreciosIdentityKey identity, CancellationToken cancellationToken = default)
        {
            await using SqlConnection connection = _connectionFactory.CreateConnection(_descriptor);
            await connection.OpenAsync(cancellationToken);
            using SqlCommand command = new(@"
SELECT TOP (1) d.id, d.idListaPrecio, l.Nivel, d.TipoIdentidad, d.idProductoServicio, d.idVariante, d.idPresentacionVenta,
       d.Precio, d.DescuentoPct, d.RedondeoModo, d.VigenciaInicio, d.VigenciaFin, d.Activo, d.FechaActualizacion
FROM dbo.ListaPreciosDetalle d
INNER JOIN dbo.ListaPreciosListas l ON l.idEmpresa=d.idEmpresa AND l.id=d.idListaPrecio
WHERE d.idEmpresa=@IdEmpresa
  AND d.idListaPrecio=@Lista
  AND d.TipoIdentidad=@Tipo
  AND d.idProductoServicio=@Producto
  AND ((d.idVariante IS NULL AND @Variante IS NULL) OR d.idVariante=@Variante)
  AND ((d.idPresentacionVenta IS NULL AND @Presentacion IS NULL) OR d.idPresentacionVenta=@Presentacion)
  AND d.Activo=1
  AND d.FechaArchivado IS NULL
  AND (d.VigenciaInicio IS NULL OR d.VigenciaInicio <= CONVERT(date, SYSUTCDATETIME()))
  AND (d.VigenciaFin IS NULL OR d.VigenciaFin >= CONVERT(date, SYSUTCDATETIME()))", connection);
            AddIdentityParameters(command, idEmpresa, idListaPrecio, identity);
            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            return await reader.ReadAsync(cancellationToken) ? ReadDetalle(reader) : null;
        }

        public async Task<IReadOnlyList<ListaPreciosDetalleRecord>> ObtenerDetallesPorNivelAsync(Guid idEmpresa, int nivel, bool soloActivos, CancellationToken cancellationToken = default)
        {
            await using SqlConnection connection = _connectionFactory.CreateConnection(_descriptor);
            await connection.OpenAsync(cancellationToken);
            using SqlCommand command = new($@"
SELECT d.id, d.idListaPrecio, l.Nivel, d.TipoIdentidad, d.idProductoServicio, d.idVariante, d.idPresentacionVenta,
       d.Precio, d.DescuentoPct, d.RedondeoModo, d.VigenciaInicio, d.VigenciaFin, d.Activo, d.FechaActualizacion
FROM dbo.ListaPreciosDetalle d
INNER JOIN dbo.ListaPreciosListas l ON l.idEmpresa=d.idEmpresa AND l.id=d.idListaPrecio
WHERE d.idEmpresa=@IdEmpresa AND l.Nivel=@Nivel{(soloActivos ? " AND d.Activo=1 AND d.FechaArchivado IS NULL" : string.Empty)}
ORDER BY d.TipoIdentidad, d.idProductoServicio, d.idVariante, d.idPresentacionVenta, d.Activo DESC, d.FechaActualizacion DESC", connection);
            command.Parameters.Add("@IdEmpresa", SqlDbType.UniqueIdentifier).Value = idEmpresa;
            command.Parameters.Add("@Nivel", SqlDbType.TinyInt).Value = nivel;
            List<ListaPreciosDetalleRecord> result = new();
            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken)) result.Add(ReadDetalle(reader));
            return result;
        }

        public async Task<IReadOnlyList<ListaPreciosPrecioDto>> ObtenerPreciosPorProductoAsync(Guid idEmpresa, Guid idProductoServicio, CancellationToken cancellationToken = default)
        {
            await using SqlConnection connection = _connectionFactory.CreateConnection(_descriptor);
            await connection.OpenAsync(cancellationToken);
            using SqlCommand command = new(@"
SELECT d.id, d.idListaPrecio, l.Nivel, d.TipoIdentidad, d.idProductoServicio, d.idVariante, d.idPresentacionVenta,
       d.Precio, d.DescuentoPct, d.RedondeoModo, d.VigenciaInicio, d.VigenciaFin, d.Activo, d.FechaActualizacion
FROM dbo.ListaPreciosDetalle d
INNER JOIN dbo.ListaPreciosListas l ON l.idEmpresa=d.idEmpresa AND l.id=d.idListaPrecio
WHERE d.idEmpresa=@IdEmpresa
  AND d.idProductoServicio=@Producto
ORDER BY l.Nivel, d.TipoIdentidad", connection);
            command.Parameters.Add("@IdEmpresa", SqlDbType.UniqueIdentifier).Value = idEmpresa;
            command.Parameters.Add("@Producto", SqlDbType.UniqueIdentifier).Value = idProductoServicio;
            List<ListaPreciosPrecioDto> result = new();
            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                ListaPreciosDetalleRecord detail = ReadDetalle(reader);
                result.Add(ToDto(detail));
            }

            return result;
        }

        private async Task<IReadOnlyList<ListaPreciosComboItemDto>> ObtenerCatalogoComboAsync(Guid idEmpresa, string tableName, CancellationToken cancellationToken)
        {
            await using SqlConnection connection = _connectionFactory.CreateConnection(_descriptor);
            await connection.OpenAsync(cancellationToken);
            using SqlCommand command = new($@"
SELECT id, Nombre
FROM {tableName}
WHERE idEmpresa=@IdEmpresa AND Activo=1
ORDER BY Nombre", connection);
            command.Parameters.Add("@IdEmpresa", SqlDbType.UniqueIdentifier).Value = idEmpresa;
            List<ListaPreciosComboItemDto> result = new();
            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                Guid id = reader.GetGuid(0);
                result.Add(new ListaPreciosComboItemDto
                {
                    Id = id,
                    Clave = id.ToString(),
                    Nombre = Convert.ToString(reader["Nombre"]) ?? string.Empty
                });
            }

            return result;
        }

        private async Task<IReadOnlyList<ListaPreciosComboItemDto>> ObtenerIdentidadComboAsync(
            Guid idEmpresa,
            string tableName,
            string displayExpression,
            string joinPredicate,
            CancellationToken cancellationToken)
        {
            await using SqlConnection connection = _connectionFactory.CreateConnection(_descriptor);
            await connection.OpenAsync(cancellationToken);
            using SqlCommand command = new($@"
SELECT src.id, {displayExpression} AS Nombre
FROM {tableName} src
INNER JOIN dbo.ProductosServicios ps ON ps.idEmpresa=src.idEmpresa AND {joinPredicate}
WHERE src.idEmpresa=@IdEmpresa AND src.Activo=1 AND ps.Tipo=1 AND ps.Activo=1
ORDER BY ps.Nombre, src.Nombre", connection);
            command.Parameters.Add("@IdEmpresa", SqlDbType.UniqueIdentifier).Value = idEmpresa;
            List<ListaPreciosComboItemDto> result = new();
            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                Guid id = reader.GetGuid(0);
                result.Add(new ListaPreciosComboItemDto
                {
                    Id = id,
                    Clave = id.ToString(),
                    Nombre = Convert.ToString(reader["Nombre"]) ?? string.Empty
                });
            }

            return result;
        }

        public async Task<Guid> GuardarPrecioAsync(Guid idEmpresa, ListaPreciosGuardarPrecioCommand command, CancellationToken cancellationToken = default)
        {
            await using SqlConnection connection = _connectionFactory.CreateConnection(_descriptor);
            await connection.OpenAsync(cancellationToken);
            using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable);
            Guid id = await GuardarPrecioInTransactionAsync(connection, transaction, idEmpresa, command, cancellationToken);
            transaction.Commit();
            return id;
        }

        public async Task<Guid> EnsureListaAsync(Guid idEmpresa, int nivel, Guid? usuarioId, CancellationToken cancellationToken = default)
        {
            await using SqlConnection connection = _connectionFactory.CreateConnection(_descriptor);
            await connection.OpenAsync(cancellationToken);
            using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable);
            Guid id = await EnsureListaAsync(connection, transaction, idEmpresa, nivel, usuarioId, cancellationToken);
            transaction.Commit();
            return id;
        }

        public async Task<IReadOnlyList<Guid>> GuardarPreciosMasivosAsync(Guid idEmpresa, IReadOnlyList<ListaPreciosGuardarPrecioCommand> commands, CancellationToken cancellationToken = default)
        {
            if (commands == null || commands.Count == 0)
            {
                throw new InvalidOperationException("SELECCION_VACIA");
            }

            await using SqlConnection connection = _connectionFactory.CreateConnection(_descriptor);
            await connection.OpenAsync(cancellationToken);
            using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable);
            List<Guid> ids = new(commands.Count);
            foreach (ListaPreciosGuardarPrecioCommand command in commands)
            {
                ids.Add(await GuardarPrecioInTransactionAsync(connection, transaction, idEmpresa, command, cancellationToken));
            }

            transaction.Commit();
            return ids;
        }

        public async Task<ListaPreciosComercialDto> ObtenerComercialAsync(Guid idEmpresa, ListaPreciosIdentityKey identity, CancellationToken cancellationToken = default)
        {
            await using SqlConnection connection = _connectionFactory.CreateConnection(_descriptor);
            await connection.OpenAsync(cancellationToken);
            using SqlCommand command = new(@"
SELECT ps.Descripcion, c.Web, c.Liverpool, c.MercadoLibre, c.Observaciones,
       ISNULL(c.DosPorUno,0) DosPorUno, ISNULL(c.TresPorDos,0) TresPorDos,
       ISNULL(c.DescuentoSegundo,0) DescuentoSegundo, ISNULL(c.Monedero,0) Monedero,
       CASE WHEN c.id IS NULL THEN 0 ELSE 1 END Materializada
FROM dbo.ProductosServicios ps
LEFT JOIN dbo.ProductosServiciosIdentidadComercial c
  ON c.idEmpresa=ps.idEmpresa AND c.idProductoServicio=ps.id AND c.TipoIdentidad=@Tipo
 AND ((c.idVariante IS NULL AND @Variante IS NULL) OR c.idVariante=@Variante)
 AND ((c.idPresentacionVenta IS NULL AND @Presentacion IS NULL) OR c.idPresentacionVenta=@Presentacion)
 AND c.Activo=1 AND c.FechaArchivado IS NULL
WHERE ps.idEmpresa=@IdEmpresa AND ps.id=@Producto", connection);
            AddCommercialIdentityParameters(command, idEmpresa, identity);
            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) throw new InvalidOperationException(ListaPreciosResolutionCodes.IdentidadInvalida);
            return new ListaPreciosComercialDto
            {
                Descripcion = Convert.ToString(reader["Descripcion"]) ?? string.Empty,
                DescripcionReadOnly = identity.TipoIdentidad is ListaPreciosConstants.IdentidadVariante or ListaPreciosConstants.IdentidadPresentacionVenta,
                Web = Convert.ToString(reader["Web"]) ?? string.Empty,
                Liverpool = Convert.ToString(reader["Liverpool"]) ?? string.Empty,
                MercadoLibre = Convert.ToString(reader["MercadoLibre"]) ?? string.Empty,
                Observaciones = Convert.ToString(reader["Observaciones"]) ?? string.Empty,
                DosPorUno = Convert.ToBoolean(reader["DosPorUno"]), TresPorDos = Convert.ToBoolean(reader["TresPorDos"]),
                DescuentoSegundo = Convert.ToBoolean(reader["DescuentoSegundo"]), Monedero = Convert.ToBoolean(reader["Monedero"]),
                Materializada = Convert.ToBoolean(reader["Materializada"])
            };
        }

        public async Task<IReadOnlyList<Guid>> GuardarMatrizConComercialAsync(Guid idEmpresa, IReadOnlyList<ListaPreciosGuardarPrecioCommand> commands, ListaPreciosComercialCommand? comercial, CancellationToken cancellationToken = default)
        {
            await using SqlConnection connection = _connectionFactory.CreateConnection(_descriptor);
            await connection.OpenAsync(cancellationToken);
            using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable);
            try
            {
                ListaPreciosIdentityKey identity = comercial?.Identity ?? commands.First().Identity;
                await ValidateIdentityInTransactionAsync(connection, transaction, idEmpresa, identity, cancellationToken);
                List<Guid> ids = new(commands.Count);
                foreach (ListaPreciosGuardarPrecioCommand command in commands)
                    ids.Add(await GuardarPrecioInTransactionAsync(connection, transaction, idEmpresa, command, cancellationToken));
                if (comercial != null)
                    await GuardarComercialInTransactionAsync(connection, transaction, idEmpresa, comercial, cancellationToken);
                transaction.Commit();
                return ids;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        private static async Task GuardarComercialInTransactionAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, ListaPreciosComercialCommand value, CancellationToken cancellationToken)
        {
            using SqlCommand read = new(@"
SELECT TOP (1) id, identityKey, Web, Liverpool, MercadoLibre, Observaciones, DosPorUno, TresPorDos, DescuentoSegundo, Monedero, Activo
FROM dbo.ProductosServiciosIdentidadComercial WITH (UPDLOCK,HOLDLOCK)
WHERE idEmpresa=@IdEmpresa AND TipoIdentidad=@Tipo AND idProductoServicio=@Producto
 AND ((idVariante IS NULL AND @Variante IS NULL) OR idVariante=@Variante)
 AND ((idPresentacionVenta IS NULL AND @Presentacion IS NULL) OR idPresentacionVenta=@Presentacion)
ORDER BY Activo DESC, FechaActualizacion DESC", connection, transaction);
            AddCommercialIdentityParameters(read, idEmpresa, value.Identity);
            Guid? rowId = null; bool wasActive = false; string? oldWeb = null, oldLiverpool = null, oldMl = null, oldObs = null;
            bool old21 = false, old32 = false, oldSecond = false, oldWallet = false;
            using (SqlDataReader reader = await read.ExecuteReaderAsync(cancellationToken))
            {
                if (await reader.ReadAsync(cancellationToken))
                {
                    rowId = reader.GetGuid(0); wasActive = Convert.ToBoolean(reader["Activo"]);
                    oldWeb = DbText(reader["Web"]); oldLiverpool = DbText(reader["Liverpool"]); oldMl = DbText(reader["MercadoLibre"]); oldObs = DbText(reader["Observaciones"]);
                    old21 = Convert.ToBoolean(reader["DosPorUno"]); old32 = Convert.ToBoolean(reader["TresPorDos"]); oldSecond = Convert.ToBoolean(reader["DescuentoSegundo"]); oldWallet = Convert.ToBoolean(reader["Monedero"]);
                }
            }
            if (!wasActive)
            {
                oldWeb = oldLiverpool = oldMl = oldObs = null;
                old21 = old32 = oldSecond = oldWallet = false;
            }

            string? oldDescription;
            using (SqlCommand description = new("SELECT Descripcion FROM dbo.ProductosServicios WITH (UPDLOCK,HOLDLOCK) WHERE idEmpresa=@IdEmpresa AND id=@Producto", connection, transaction))
            {
                description.Parameters.Add("@IdEmpresa", SqlDbType.UniqueIdentifier).Value = idEmpresa;
                description.Parameters.Add("@Producto", SqlDbType.UniqueIdentifier).Value = value.Identity.IdProductoServicio;
                object? result = await description.ExecuteScalarAsync(cancellationToken);
                if (result == null) throw new InvalidOperationException(ListaPreciosResolutionCodes.IdentidadInvalida);
                oldDescription = result == DBNull.Value ? null : Convert.ToString(result);
            }

            bool descriptionChanged = (value.Identity.TipoIdentidad is ListaPreciosConstants.IdentidadProducto or ListaPreciosConstants.IdentidadServicio)
                && !TextEqual(oldDescription, value.Descripcion);
            bool extensionChanged = !TextEqual(oldWeb, value.Web) || !TextEqual(oldLiverpool, value.Liverpool) || !TextEqual(oldMl, value.MercadoLibre) || !TextEqual(oldObs, value.Observaciones)
                || old21 != value.DosPorUno || old32 != value.TresPorDos || oldSecond != value.DescuentoSegundo || oldWallet != value.Monedero;
            if (!descriptionChanged && !extensionChanged) return;

            if (descriptionChanged)
            {
                using SqlCommand updateDescription = new("UPDATE dbo.ProductosServicios SET Descripcion=@Value, FechaActualizacion=SYSUTCDATETIME() WHERE idEmpresa=@IdEmpresa AND id=@Producto", connection, transaction);
                updateDescription.Parameters.Add("@Value", SqlDbType.NVarChar, -1).Value = (object?)value.Descripcion ?? DBNull.Value;
                updateDescription.Parameters.Add("@IdEmpresa", SqlDbType.UniqueIdentifier).Value = idEmpresa;
                updateDescription.Parameters.Add("@Producto", SqlDbType.UniqueIdentifier).Value = value.Identity.IdProductoServicio;
                await updateDescription.ExecuteNonQueryAsync(cancellationToken);
                await InsertCommercialHistoryAsync(connection, transaction, idEmpresa, value, "Descripcion", "UPDATE", oldDescription, value.Descripcion, cancellationToken);
            }

            if (!extensionChanged && rowId == null) return;
            bool archiveExtension = IsCommercialExtensionEmpty(value);
            if (archiveExtension)
            {
                if (rowId == null || !wasActive) return;
                using SqlCommand archive = new(@"UPDATE dbo.ProductosServiciosIdentidadComercial
SET Activo=0, FechaArchivado=SYSUTCDATETIME(), idUsuarioArchivado=@UsuarioId,
    FechaActualizacion=SYSUTCDATETIME(), idUsuarioActualizacion=@UsuarioId
WHERE idEmpresa=@IdEmpresa AND id=@Id AND Activo=1", connection, transaction);
                archive.Parameters.Add("@IdEmpresa", SqlDbType.UniqueIdentifier).Value = idEmpresa;
                archive.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = rowId.Value;
                archive.Parameters.Add("@UsuarioId", SqlDbType.UniqueIdentifier).Value = (object?)value.UsuarioId ?? DBNull.Value;
                await archive.ExecuteNonQueryAsync(cancellationToken);
                await HistoryIfChanged("Web", oldWeb, value.Web, "UPDATE");
                await HistoryIfChanged("Liverpool", oldLiverpool, value.Liverpool, "UPDATE");
                await HistoryIfChanged("MercadoLibre", oldMl, value.MercadoLibre, "UPDATE");
                await HistoryIfChanged("Observaciones", oldObs, value.Observaciones, "UPDATE");
                await HistoryIfChanged("DosPorUno", old21 ? "1" : "0", "0", "UPDATE");
                await HistoryIfChanged("TresPorDos", old32 ? "1" : "0", "0", "UPDATE");
                await HistoryIfChanged("DescuentoSegundo", oldSecond ? "1" : "0", "0", "UPDATE");
                await HistoryIfChanged("Monedero", oldWallet ? "1" : "0", "0", "UPDATE");
                await InsertCommercialHistoryAsync(connection, transaction, idEmpresa, value, "Activo", "ARCHIVE", "1", "0", cancellationToken);
                return;
            }
            if (rowId == null)
            {
                rowId = Guid.NewGuid();
                using SqlCommand insert = new(@"INSERT dbo.ProductosServiciosIdentidadComercial
(id,idEmpresa,identityKey,TipoIdentidad,idProductoServicio,TipoProductoServicio,idVariante,idPresentacionVenta,Web,Liverpool,MercadoLibre,Observaciones,DosPorUno,TresPorDos,DescuentoSegundo,Monedero,Activo,FechaCreacion,FechaActualizacion,idUsuarioCreacion,idUsuarioActualizacion)
VALUES(@Id,@IdEmpresa,NEWID(),@Tipo,@Producto,@TipoProducto,@Variante,@Presentacion,@Web,@Liverpool,@Ml,@Obs,@P21,@P32,@Segundo,@Monedero,1,SYSUTCDATETIME(),SYSUTCDATETIME(),@UsuarioId,@UsuarioId)", connection, transaction);
                AddCommercialWriteParameters(insert, idEmpresa, rowId.Value, value);
                await insert.ExecuteNonQueryAsync(cancellationToken);
            }
            else
            {
                using SqlCommand update = new(@"UPDATE dbo.ProductosServiciosIdentidadComercial SET Web=@Web,Liverpool=@Liverpool,MercadoLibre=@Ml,Observaciones=@Obs,DosPorUno=@P21,TresPorDos=@P32,DescuentoSegundo=@Segundo,Monedero=@Monedero,Activo=1,FechaArchivado=NULL,idUsuarioArchivado=NULL,FechaActualizacion=SYSUTCDATETIME(),idUsuarioActualizacion=@UsuarioId WHERE idEmpresa=@IdEmpresa AND id=@Id", connection, transaction);
                AddCommercialWriteParameters(update, idEmpresa, rowId.Value, value);
                await update.ExecuteNonQueryAsync(cancellationToken);
                if (!wasActive) await InsertCommercialHistoryAsync(connection, transaction, idEmpresa, value, "Activo", "REACTIVATE", "0", "1", cancellationToken);
            }
            await HistoryIfChanged("Web", oldWeb, value.Web); await HistoryIfChanged("Liverpool", oldLiverpool, value.Liverpool); await HistoryIfChanged("MercadoLibre", oldMl, value.MercadoLibre); await HistoryIfChanged("Observaciones", oldObs, value.Observaciones);
            await HistoryIfChanged("DosPorUno", old21 ? "1" : "0", value.DosPorUno ? "1" : "0"); await HistoryIfChanged("TresPorDos", old32 ? "1" : "0", value.TresPorDos ? "1" : "0"); await HistoryIfChanged("DescuentoSegundo", oldSecond ? "1" : "0", value.DescuentoSegundo ? "1" : "0"); await HistoryIfChanged("Monedero", oldWallet ? "1" : "0", value.Monedero ? "1" : "0");
            async Task HistoryIfChanged(string field, string? before, string? after, string? operation = null) { if (!TextEqual(before, after)) await InsertCommercialHistoryAsync(connection, transaction, idEmpresa, value, field, operation ?? (rowId.HasValue && wasActive ? "UPDATE" : "INSERT"), before, after, cancellationToken); }
        }

        private static bool IsCommercialExtensionEmpty(ListaPreciosComercialCommand value) =>
            string.IsNullOrWhiteSpace(value.Web)
            && string.IsNullOrWhiteSpace(value.Liverpool)
            && string.IsNullOrWhiteSpace(value.MercadoLibre)
            && string.IsNullOrWhiteSpace(value.Observaciones)
            && !value.DosPorUno
            && !value.TresPorDos
            && !value.DescuentoSegundo
            && !value.Monedero;

        private static async Task InsertCommercialHistoryAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, ListaPreciosComercialCommand value, string field, string operation, string? before, string? after, CancellationToken cancellationToken)
        {
            using SqlCommand command = new(@"INSERT dbo.ProductosServiciosIdentidadComercialHistorial
(id,idEmpresa,TipoIdentidad,idProductoServicio,TipoProductoServicio,idVariante,idPresentacionVenta,Campo,Operacion,ValorAnterior,ValorNuevo,idUsuario,Usuario,CorrelationId,Origen,FechaUtc)
VALUES(NEWID(),@IdEmpresa,@Tipo,@Producto,@TipoProducto,@Variante,@Presentacion,@Campo,@Operacion,@Antes,@Despues,@UsuarioId,@Usuario,@Correlation,N'LISTA_PRECIOS',SYSUTCDATETIME())", connection, transaction);
            AddCommercialIdentityParameters(command, idEmpresa, value.Identity);
            command.Parameters.Add("@TipoProducto", SqlDbType.TinyInt).Value = value.TipoProductoServicio; command.Parameters.Add("@Campo", SqlDbType.NVarChar, 60).Value = field; command.Parameters.Add("@Operacion", SqlDbType.NVarChar, 20).Value = operation;
            command.Parameters.Add("@Antes", SqlDbType.NVarChar, -1).Value = (object?)before ?? DBNull.Value; command.Parameters.Add("@Despues", SqlDbType.NVarChar, -1).Value = (object?)after ?? DBNull.Value; command.Parameters.Add("@UsuarioId", SqlDbType.UniqueIdentifier).Value = (object?)value.UsuarioId ?? DBNull.Value; command.Parameters.Add("@Usuario", SqlDbType.NVarChar, 256).Value = (object?)value.Usuario ?? DBNull.Value; command.Parameters.Add("@Correlation", SqlDbType.UniqueIdentifier).Value = value.CorrelationId;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task ValidateIdentityInTransactionAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, ListaPreciosIdentityKey identity, CancellationToken cancellationToken)
        {
            string join = identity.TipoIdentidad switch
            {
                ListaPreciosConstants.IdentidadVariante => "INNER JOIN dbo.ProductosServiciosVariantes x ON x.idEmpresa=ps.idEmpresa AND x.id=@Child AND x.idProductoServicio=ps.id AND x.Activo=1",
                ListaPreciosConstants.IdentidadPresentacionVenta => "INNER JOIN dbo.ProductosServiciosPresentacionesVenta x ON x.idEmpresa=ps.idEmpresa AND x.id=@Child AND x.idProductoServicio=ps.id AND x.Activo=1",
                _ => string.Empty
            };
            using SqlCommand command = new($"SELECT COUNT_BIG(1) FROM dbo.ProductosServicios ps WITH (UPDLOCK,HOLDLOCK) {join} WHERE ps.idEmpresa=@IdEmpresa AND ps.id=@Producto AND ps.Activo=1 AND ps.Tipo=@TipoProducto", connection, transaction);
            command.Parameters.Add("@IdEmpresa", SqlDbType.UniqueIdentifier).Value = idEmpresa;
            command.Parameters.Add("@Producto", SqlDbType.UniqueIdentifier).Value = identity.IdProductoServicio;
            command.Parameters.Add("@TipoProducto", SqlDbType.TinyInt).Value = identity.TipoIdentidad == ListaPreciosConstants.IdentidadServicio ? ListaPreciosConstants.TipoServicio : ListaPreciosConstants.TipoProducto;
            if (identity.TipoIdentidad is ListaPreciosConstants.IdentidadVariante or ListaPreciosConstants.IdentidadPresentacionVenta)
                command.Parameters.Add("@Child", SqlDbType.UniqueIdentifier).Value = identity.TipoIdentidad == ListaPreciosConstants.IdentidadVariante ? identity.IdVariante!.Value : identity.IdPresentacionVenta!.Value;
            if (Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken)) != 1L) throw new InvalidOperationException(ListaPreciosResolutionCodes.IdentidadInvalida);
        }

        private static void AddCommercialIdentityParameters(SqlCommand command, Guid idEmpresa, ListaPreciosIdentityKey identity)
        { command.Parameters.Add("@IdEmpresa", SqlDbType.UniqueIdentifier).Value=idEmpresa; command.Parameters.Add("@Tipo", SqlDbType.TinyInt).Value=identity.TipoIdentidad; command.Parameters.Add("@Producto", SqlDbType.UniqueIdentifier).Value=identity.IdProductoServicio; command.Parameters.Add("@Variante", SqlDbType.UniqueIdentifier).Value=(object?)identity.IdVariante??DBNull.Value; command.Parameters.Add("@Presentacion", SqlDbType.UniqueIdentifier).Value=(object?)identity.IdPresentacionVenta??DBNull.Value; }
        private static void AddCommercialWriteParameters(SqlCommand command, Guid idEmpresa, Guid id, ListaPreciosComercialCommand value)
        { AddCommercialIdentityParameters(command,idEmpresa,value.Identity); command.Parameters.Add("@Id",SqlDbType.UniqueIdentifier).Value=id; command.Parameters.Add("@TipoProducto",SqlDbType.TinyInt).Value=value.TipoProductoServicio; command.Parameters.Add("@Web",SqlDbType.NVarChar,250).Value=(object?)value.Web??DBNull.Value; command.Parameters.Add("@Liverpool",SqlDbType.NVarChar,-1).Value=(object?)value.Liverpool??DBNull.Value; command.Parameters.Add("@Ml",SqlDbType.NVarChar,-1).Value=(object?)value.MercadoLibre??DBNull.Value; command.Parameters.Add("@Obs",SqlDbType.NVarChar,-1).Value=(object?)value.Observaciones??DBNull.Value; command.Parameters.Add("@P21",SqlDbType.Bit).Value=value.DosPorUno; command.Parameters.Add("@P32",SqlDbType.Bit).Value=value.TresPorDos; command.Parameters.Add("@Segundo",SqlDbType.Bit).Value=value.DescuentoSegundo; command.Parameters.Add("@Monedero",SqlDbType.Bit).Value=value.Monedero; command.Parameters.Add("@UsuarioId",SqlDbType.UniqueIdentifier).Value=(object?)value.UsuarioId??DBNull.Value; }
        private static string? DbText(object value) => value == DBNull.Value ? null : Convert.ToString(value);
        private static bool TextEqual(string? left, string? right) => string.Equals(string.IsNullOrWhiteSpace(left)?null:left.Trim(), string.IsNullOrWhiteSpace(right)?null:right.Trim(), StringComparison.Ordinal);

        private static async Task<Guid> GuardarPrecioInTransactionAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            Guid idEmpresa,
            ListaPreciosGuardarPrecioCommand command,
            CancellationToken cancellationToken)
        {
            Guid idLista = await EnsureListaAsync(connection, transaction, idEmpresa, command.Nivel, command.UsuarioId, cancellationToken);

            using SqlCommand duplicateCommand = new(@"
SELECT id, Activo, Precio, DescuentoPct, RedondeoModo, VigenciaInicio, VigenciaFin
FROM dbo.ListaPreciosDetalle
WHERE idEmpresa=@IdEmpresa
  AND idListaPrecio=@Lista
  AND TipoIdentidad=@Tipo
  AND idProductoServicio=@Producto
  AND ((idVariante IS NULL AND @Variante IS NULL) OR idVariante=@Variante)
  AND ((idPresentacionVenta IS NULL AND @Presentacion IS NULL) OR idPresentacionVenta=@Presentacion)
ORDER BY Activo DESC, FechaActualizacion DESC", connection, transaction);
            AddIdentityParameters(duplicateCommand, idEmpresa, idLista, command.Identity);
            List<ListaPreciosDetalleRecord> matches = new();
            using (SqlDataReader reader = await duplicateCommand.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    matches.Add(new ListaPreciosDetalleRecord(
                        reader.GetGuid(reader.GetOrdinal("id")),
                        idLista,
                        command.Nivel,
                        command.Identity.TipoIdentidad,
                        command.Identity.IdProductoServicio,
                        command.Identity.IdVariante,
                        command.Identity.IdPresentacionVenta,
                        Convert.ToDecimal(reader["Precio"]),
                        reader["DescuentoPct"] == DBNull.Value ? null : Convert.ToDecimal(reader["DescuentoPct"]),
                        Convert.ToByte(reader["RedondeoModo"]),
                        reader["VigenciaInicio"] == DBNull.Value ? null : Convert.ToDateTime(reader["VigenciaInicio"]),
                        reader["VigenciaFin"] == DBNull.Value ? null : Convert.ToDateTime(reader["VigenciaFin"]),
                        Convert.ToBoolean(reader["Activo"]),
                        DateTime.UtcNow));
                }
            }

            if (matches.Count(x => x.Activo) > 1)
            {
                throw new InvalidOperationException(ListaPreciosResolutionCodes.Duplicado);
            }

            ListaPreciosDetalleRecord? previous = matches.FirstOrDefault();
            Guid id = previous?.Id ?? Guid.Empty;
            if (id == Guid.Empty)
            {
                id = Guid.NewGuid();
                using SqlCommand insert = new(@"
INSERT INTO dbo.ListaPreciosDetalle
(id, idEmpresa, identityKey, idListaPrecio, TipoIdentidad, idProductoServicio, TipoProductoServicio, idVariante, idPresentacionVenta, Precio, DescuentoPct, RedondeoModo, VigenciaInicio, VigenciaFin, Activo, FechaCreacion, FechaActualizacion, idUsuarioCreacion)
VALUES
(@Id, @IdEmpresa, NEWID(), @Lista, @Tipo, @Producto, @TipoProductoServicio, @Variante, @Presentacion, @Precio, @DescuentoPct, @RedondeoModo, @VigenciaInicio, @VigenciaFin, 1, SYSUTCDATETIME(), SYSUTCDATETIME(), @Usuario)", connection, transaction);
                AddWriteParameters(insert, idEmpresa, idLista, command);
                insert.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = id;
                await insert.ExecuteNonQueryAsync(cancellationToken);
                string insertOperation = string.IsNullOrWhiteSpace(command.OperacionHistorial) ? "INSERT" : command.OperacionHistorial;
                await InsertHistoryAsync(connection, transaction, idEmpresa, idLista, command, "Precio", null, command.Precio.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), insertOperation, cancellationToken);
                await InsertHistoryAsync(connection, transaction, idEmpresa, idLista, command, "DescuentoPct", null, command.DescuentoPct?.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), insertOperation, cancellationToken);
                await InsertHistoryAsync(connection, transaction, idEmpresa, idLista, command, "RedondeoModo", null, command.RedondeoModo.ToString(System.Globalization.CultureInfo.InvariantCulture), insertOperation, cancellationToken);
                await InsertHistoryAsync(connection, transaction, idEmpresa, idLista, command, "VigenciaInicio", null, command.VigenciaInicio?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), insertOperation, cancellationToken);
                await InsertHistoryAsync(connection, transaction, idEmpresa, idLista, command, "VigenciaFin", null, command.VigenciaFin?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), insertOperation, cancellationToken);
            }
            else
            {
                using SqlCommand update = new(@"
UPDATE dbo.ListaPreciosDetalle
SET Precio=@Precio,
    DescuentoPct=@DescuentoPct,
    RedondeoModo=@RedondeoModo,
    VigenciaInicio=@VigenciaInicio,
    VigenciaFin=@VigenciaFin,
    Activo=1,
    FechaArchivado=NULL,
    idUsuarioArchivado=NULL,
    FechaActualizacion=SYSUTCDATETIME(),
    idUsuarioActualizacion=@Usuario
WHERE idEmpresa=@IdEmpresa AND id=@Id", connection, transaction);
                update.Parameters.Add("@IdEmpresa", SqlDbType.UniqueIdentifier).Value = idEmpresa;
                update.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = id;
                update.Parameters.Add("@Precio", SqlDbType.Decimal).Value = command.Precio;
                update.Parameters["@Precio"].Precision = 18;
                update.Parameters["@Precio"].Scale = 2;
                update.Parameters.Add("@DescuentoPct", SqlDbType.Decimal).Value = Db(command.DescuentoPct);
                update.Parameters["@DescuentoPct"].Precision = 5;
                update.Parameters["@DescuentoPct"].Scale = 2;
                update.Parameters.Add("@RedondeoModo", SqlDbType.TinyInt).Value = command.RedondeoModo;
                update.Parameters.Add("@VigenciaInicio", SqlDbType.Date).Value = Db(command.VigenciaInicio);
                update.Parameters.Add("@VigenciaFin", SqlDbType.Date).Value = Db(command.VigenciaFin);
                update.Parameters.Add("@Usuario", SqlDbType.UniqueIdentifier).Value = Db(command.UsuarioId);
                await update.ExecuteNonQueryAsync(cancellationToken);
                string updateOperation = string.IsNullOrWhiteSpace(command.OperacionHistorial) ? "UPDATE" : command.OperacionHistorial;
                await InsertHistoryIfChangedAsync(connection, transaction, idEmpresa, idLista, command, "Precio", previous?.Precio.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), command.Precio.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), updateOperation, cancellationToken);
                await InsertHistoryIfChangedAsync(connection, transaction, idEmpresa, idLista, command, "DescuentoPct", previous?.DescuentoPct?.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), command.DescuentoPct?.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), updateOperation, cancellationToken);
                await InsertHistoryIfChangedAsync(connection, transaction, idEmpresa, idLista, command, "RedondeoModo", previous == null ? null : previous.RedondeoModo.ToString(System.Globalization.CultureInfo.InvariantCulture), command.RedondeoModo.ToString(System.Globalization.CultureInfo.InvariantCulture), updateOperation, cancellationToken);
                await InsertHistoryIfChangedAsync(connection, transaction, idEmpresa, idLista, command, "VigenciaInicio", previous?.VigenciaInicio?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), command.VigenciaInicio?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), updateOperation, cancellationToken);
                await InsertHistoryIfChangedAsync(connection, transaction, idEmpresa, idLista, command, "VigenciaFin", previous?.VigenciaFin?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), command.VigenciaFin?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), updateOperation, cancellationToken);
                bool copyReactivation = previous != null && !previous.Activo && command.OperacionHistorial == "REACTIVACION";
                if (previous != null && (copyReactivation || ListaPreciosHistorialPolicy.RequiresReactivationHistory(previous, command)))
                {
                    await InsertHistoryAsync(connection, transaction, idEmpresa, idLista, command, "Activo", "0", "1", "REACTIVACION", cancellationToken);
                }
            }

            return id;
        }

        public async Task BajaPrecioAsync(Guid idEmpresa, Guid idPrecio, Guid? usuarioId, CancellationToken cancellationToken = default)
        {
            await using SqlConnection connection = _connectionFactory.CreateConnection(_descriptor);
            await connection.OpenAsync(cancellationToken);
            using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
            ListaPreciosDetalleRecord? previous = null;
            using (SqlCommand select = new(@"
SELECT d.id, d.idListaPrecio, l.Nivel, d.TipoIdentidad, d.idProductoServicio, d.idVariante, d.idPresentacionVenta,
       d.Precio, d.DescuentoPct, d.RedondeoModo, d.VigenciaInicio, d.VigenciaFin, d.Activo, d.FechaActualizacion
FROM dbo.ListaPreciosDetalle d
INNER JOIN dbo.ListaPreciosListas l ON l.idEmpresa=d.idEmpresa AND l.id=d.idListaPrecio
WHERE d.idEmpresa=@IdEmpresa AND d.id=@Id", connection, transaction))
            {
                select.Parameters.Add("@IdEmpresa", SqlDbType.UniqueIdentifier).Value = idEmpresa;
                select.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = idPrecio;
                using SqlDataReader reader = await select.ExecuteReaderAsync(cancellationToken);
                if (await reader.ReadAsync(cancellationToken))
                {
                    previous = ReadDetalle(reader);
                }
            }

            using SqlCommand command = new(@"
UPDATE dbo.ListaPreciosDetalle
SET Activo=0,
    FechaArchivado=COALESCE(FechaArchivado, SYSUTCDATETIME()),
    idUsuarioArchivado=@Usuario,
    FechaActualizacion=SYSUTCDATETIME(),
    idUsuarioActualizacion=@Usuario
WHERE idEmpresa=@IdEmpresa AND id=@Id AND Activo=1", connection, transaction);
            command.Parameters.Add("@IdEmpresa", SqlDbType.UniqueIdentifier).Value = idEmpresa;
            command.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = idPrecio;
            command.Parameters.Add("@Usuario", SqlDbType.UniqueIdentifier).Value = Db(usuarioId);
            int affected = await command.ExecuteNonQueryAsync(cancellationToken);
            if (affected > 0 && previous != null)
            {
                ListaPreciosGuardarPrecioCommand historyCommand = new(
                    previous.Nivel,
                    0,
                    new ListaPreciosIdentityKey(previous.TipoIdentidad, previous.IdProductoServicio, previous.IdVariante, previous.IdPresentacionVenta),
                    previous.Precio,
                    previous.DescuentoPct,
                    previous.RedondeoModo,
                    previous.VigenciaInicio,
                    previous.VigenciaFin,
                    usuarioId,
                    string.Empty,
                    "Baja lógica",
                    Guid.NewGuid(),
                    ListaPreciosConstants.OrigenHistorialIndividual);
                await InsertHistoryAsync(connection, transaction, idEmpresa, previous.IdListaPrecio, historyCommand, "Activo", "1", "0", "BAJA", cancellationToken);
            }

            transaction.Commit();
        }

        private static async Task<Guid> EnsureListaAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, int nivel, Guid? usuarioId, CancellationToken cancellationToken)
        {
            ListaPreciosHeaderDecision decision = await ReadHeaderDecisionAsync(connection, transaction, idEmpresa, nivel, cancellationToken);
            if (decision.Action == ListaPreciosHeaderAction.Reuse)
            {
                return decision.Id!.Value;
            }

            if (decision.Action == ListaPreciosHeaderAction.Reactivate)
            {
                using SqlCommand reactivate = new(@"
UPDATE dbo.ListaPreciosListas
SET Activo=1,
    FechaArchivado=NULL,
    idUsuarioArchivado=NULL,
    FechaActualizacion=SYSUTCDATETIME(),
    idUsuarioActualizacion=@Usuario
WHERE idEmpresa=@IdEmpresa AND Nivel=@Nivel AND id=@Id AND Activo=0 AND FechaArchivado IS NOT NULL", connection, transaction);
                reactivate.Parameters.Add("@IdEmpresa", SqlDbType.UniqueIdentifier).Value = idEmpresa;
                reactivate.Parameters.Add("@Nivel", SqlDbType.TinyInt).Value = nivel;
                reactivate.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = decision.Id!.Value;
                reactivate.Parameters.Add("@Usuario", SqlDbType.UniqueIdentifier).Value = Db(usuarioId);
                int affected = await reactivate.ExecuteNonQueryAsync(cancellationToken);
                if (affected != 1)
                {
                    throw new InvalidOperationException(ListaPreciosHeaderPolicy.InconsistentHeader);
                }

                return decision.Id.Value;
            }

            Guid id = Guid.NewGuid();
            using SqlCommand insert = new(@"
INSERT INTO dbo.ListaPreciosListas
(id, idEmpresa, identityKey, Nivel, Nombre, EsDefault, Activo, FechaCreacion, FechaActualizacion, idUsuarioCreacion)
VALUES
(@Id, @IdEmpresa, NEWID(), @Nivel, @Nombre, @Default, 1, SYSUTCDATETIME(), SYSUTCDATETIME(), @Usuario)", connection, transaction);
            insert.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = id;
            insert.Parameters.Add("@IdEmpresa", SqlDbType.UniqueIdentifier).Value = idEmpresa;
            insert.Parameters.Add("@Nivel", SqlDbType.TinyInt).Value = nivel;
            insert.Parameters.Add("@Nombre", SqlDbType.NVarChar, 100).Value = nivel == ListaPreciosConstants.NivelDefault ? "Lista 1" : $"Lista {nivel}";
            insert.Parameters.Add("@Default", SqlDbType.Bit).Value = nivel == ListaPreciosConstants.NivelDefault;
            insert.Parameters.Add("@Usuario", SqlDbType.UniqueIdentifier).Value = Db(usuarioId);
            try
            {
                await insert.ExecuteNonQueryAsync(cancellationToken);
                return id;
            }
            catch (SqlException ex) when (ex.Number is 2601 or 2627)
            {
                ListaPreciosHeaderDecision raced = await ReadHeaderDecisionAsync(connection, transaction, idEmpresa, nivel, cancellationToken);
                if (raced.Action == ListaPreciosHeaderAction.Reuse && raced.Id.HasValue)
                {
                    return raced.Id.Value;
                }

                throw;
            }
        }

        private static async Task<ListaPreciosHeaderDecision> ReadHeaderDecisionAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            Guid idEmpresa,
            int nivel,
            CancellationToken cancellationToken)
        {
            List<ListaPreciosHeaderCandidate> candidates = new();
            using SqlCommand select = new(@"
SELECT id, idEmpresa, Nivel, Activo, FechaArchivado
FROM dbo.ListaPreciosListas WITH (UPDLOCK, HOLDLOCK)
WHERE idEmpresa=@IdEmpresa AND Nivel=@Nivel", connection, transaction);
            select.Parameters.Add("@IdEmpresa", SqlDbType.UniqueIdentifier).Value = idEmpresa;
            select.Parameters.Add("@Nivel", SqlDbType.TinyInt).Value = nivel;
            using SqlDataReader reader = await select.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                candidates.Add(new ListaPreciosHeaderCandidate(
                    reader.GetGuid(reader.GetOrdinal("id")),
                    reader.GetGuid(reader.GetOrdinal("idEmpresa")),
                    Convert.ToInt32(reader["Nivel"]),
                    Convert.ToBoolean(reader["Activo"]),
                    reader["FechaArchivado"] == DBNull.Value ? null : Convert.ToDateTime(reader["FechaArchivado"])));
            }

            return ListaPreciosHeaderPolicy.Resolve(idEmpresa, nivel, candidates);
        }

        private static void AddIdentityParameters(SqlCommand command, Guid idEmpresa, Guid idLista, ListaPreciosIdentityKey identity)
        {
            command.Parameters.Add("@IdEmpresa", SqlDbType.UniqueIdentifier).Value = idEmpresa;
            command.Parameters.Add("@Lista", SqlDbType.UniqueIdentifier).Value = idLista;
            command.Parameters.Add("@Tipo", SqlDbType.TinyInt).Value = identity.TipoIdentidad;
            command.Parameters.Add("@Producto", SqlDbType.UniqueIdentifier).Value = identity.IdProductoServicio;
            command.Parameters.Add("@Variante", SqlDbType.UniqueIdentifier).Value = Db(identity.IdVariante);
            command.Parameters.Add("@Presentacion", SqlDbType.UniqueIdentifier).Value = Db(identity.IdPresentacionVenta);
        }

        private static void AddWriteParameters(SqlCommand command, Guid idEmpresa, Guid idLista, ListaPreciosGuardarPrecioCommand write)
        {
            AddIdentityParameters(command, idEmpresa, idLista, write.Identity);
            command.Parameters.Add("@TipoProductoServicio", SqlDbType.TinyInt).Value = write.TipoProductoServicio;
            command.Parameters.Add("@Precio", SqlDbType.Decimal).Value = write.Precio;
            command.Parameters["@Precio"].Precision = 18;
            command.Parameters["@Precio"].Scale = 2;
            command.Parameters.Add("@DescuentoPct", SqlDbType.Decimal).Value = Db(write.DescuentoPct);
            command.Parameters["@DescuentoPct"].Precision = 5;
            command.Parameters["@DescuentoPct"].Scale = 2;
            command.Parameters.Add("@RedondeoModo", SqlDbType.TinyInt).Value = write.RedondeoModo;
            command.Parameters.Add("@VigenciaInicio", SqlDbType.Date).Value = Db(write.VigenciaInicio);
            command.Parameters.Add("@VigenciaFin", SqlDbType.Date).Value = Db(write.VigenciaFin);
            command.Parameters.Add("@Usuario", SqlDbType.UniqueIdentifier).Value = Db(write.UsuarioId);
        }

        public async Task<ListaPreciosHistorialPaginaDto> ConsultarHistorialAsync(Guid idEmpresa, ListaPreciosHistorialConsultaRequest request, CancellationToken cancellationToken = default)
        {
            await using SqlConnection connection = _connectionFactory.CreateConnection(_descriptor);
            await connection.OpenAsync(cancellationToken);
            const string fromSql = @"
FROM dbo.ListaPreciosHistorial h
LEFT JOIN dbo.ListaPreciosListas l ON l.idEmpresa=h.idEmpresa AND l.id=h.idListaPrecio
LEFT JOIN dbo.ProductosServicios ps ON ps.idEmpresa=h.idEmpresa AND ps.id=h.idProductoServicio
LEFT JOIN dbo.ProductosServiciosVariantes v ON v.idEmpresa=h.idEmpresa AND v.id=h.idVariante
LEFT JOIN dbo.ProductosServiciosPresentacionesVenta pv ON pv.idEmpresa=h.idEmpresa AND pv.id=h.idPresentacionVenta";
            StringBuilder where = new(" WHERE h.idEmpresa=@IdEmpresa");
            using SqlCommand command = new() { Connection = connection };
            command.Parameters.Add("@IdEmpresa", SqlDbType.UniqueIdentifier).Value = idEmpresa;

            if (request.FechaDesde.HasValue)
            {
                where.Append(" AND h.FechaUtc>=@FechaDesde");
                command.Parameters.Add("@FechaDesde", SqlDbType.DateTime2).Value = request.FechaDesde.Value.Date;
            }

            if (request.FechaHasta.HasValue)
            {
                where.Append(" AND h.FechaUtc<@FechaHastaExclusiva");
                command.Parameters.Add("@FechaHastaExclusiva", SqlDbType.DateTime2).Value = request.FechaHasta.Value.Date.AddDays(1);
            }

            if (request.Nivel.HasValue)
            {
                where.Append(" AND l.Nivel=@Nivel");
                command.Parameters.Add("@Nivel", SqlDbType.TinyInt).Value = request.Nivel.Value;
            }

            if (request.TipoIdentidad.HasValue)
            {
                where.Append(" AND h.TipoIdentidad=@TipoIdentidad");
                command.Parameters.Add("@TipoIdentidad", SqlDbType.TinyInt).Value = request.TipoIdentidad.Value;
            }

            if (!string.IsNullOrWhiteSpace(request.Busqueda))
            {
                where.Append(@" AND (
                    ISNULL(ps.Codigo, N'') LIKE @Busqueda OR
                    ISNULL(v.Sku, N'') LIKE @Busqueda OR
                    ISNULL(ps.Nombre, N'') LIKE @Busqueda OR
                    ISNULL(v.Nombre, N'') LIKE @Busqueda OR
                    ISNULL(pv.Nombre, N'') LIKE @Busqueda)");
                command.Parameters.Add("@Busqueda", SqlDbType.NVarChar, 260).Value = "%" + request.Busqueda + "%";
            }

            if (!string.IsNullOrWhiteSpace(request.Origen))
            {
                where.Append(" AND h.Origen=@Origen");
                command.Parameters.Add("@Origen", SqlDbType.NVarChar, 40).Value = request.Origen;
            }

            if (!string.IsNullOrWhiteSpace(request.Operacion))
            {
                where.Append(" AND h.Operacion=@Operacion");
                command.Parameters.Add("@Operacion", SqlDbType.NVarChar, 40).Value = request.Operacion;
            }

            if (request.CorrelationId.HasValue && request.CorrelationId.Value != Guid.Empty)
            {
                where.Append(" AND h.CorrelationId=@CorrelationId");
                command.Parameters.Add("@CorrelationId", SqlDbType.UniqueIdentifier).Value = request.CorrelationId.Value;
            }

            if (!string.IsNullOrWhiteSpace(request.Usuario))
            {
                where.Append(" AND ISNULL(h.Usuario, N'') LIKE @Usuario");
                command.Parameters.Add("@Usuario", SqlDbType.NVarChar, 260).Value = "%" + request.Usuario + "%";
            }

            int offset = (request.Pagina - 1) * request.TamanoPagina;
            command.Parameters.Add("@Offset", SqlDbType.Int).Value = offset;
            command.Parameters.Add("@TamanoPagina", SqlDbType.Int).Value = request.TamanoPagina;
            command.CommandText = $@"
SELECT COUNT(1) {fromSql} {where};
SELECT
    h.id, h.FechaUtc, l.Nivel,
    COALESCE(NULLIF(l.Nombre, N''), CASE WHEN l.Nivel IS NULL THEN N'Lista no disponible' ELSE CONCAT(N'Lista ', l.Nivel) END) AS Lista,
    h.TipoIdentidad,
    CASE h.TipoIdentidad WHEN 1 THEN N'Producto base' WHEN 2 THEN N'Servicio' WHEN 3 THEN N'Variante' WHEN 4 THEN N'Presentación venta' ELSE N'Identidad no disponible' END AS TipoIdentidadNombre,
    CASE WHEN h.TipoIdentidad=3 THEN COALESCE(NULLIF(v.Sku, N''), ps.Codigo, N'') ELSE COALESCE(ps.Codigo, N'') END AS Codigo,
    CASE
        WHEN h.TipoIdentidad=3 AND ps.Nombre IS NOT NULL THEN CONCAT(ps.Nombre, N' · ', COALESCE(v.Nombre, N'Identidad no disponible'))
        WHEN h.TipoIdentidad=4 AND ps.Nombre IS NOT NULL THEN CONCAT(ps.Nombre, N' · ', COALESCE(pv.Nombre, N'Identidad no disponible'))
        ELSE COALESCE(ps.Nombre, N'Identidad no disponible')
    END AS Identidad,
    h.Campo, h.Operacion, h.ValorAnterior, h.ValorNuevo, h.Usuario, h.Origen, h.CorrelationId, h.Motivo
{fromSql} {where}
ORDER BY h.FechaUtc DESC, h.id DESC
OFFSET @Offset ROWS FETCH NEXT @TamanoPagina ROWS ONLY;";

            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            int total = await reader.ReadAsync(cancellationToken) ? reader.GetInt32(0) : 0;
            await reader.NextResultAsync(cancellationToken);
            List<ListaPreciosHistorialConsultaItemDto> items = new();
            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(new ListaPreciosHistorialConsultaItemDto(
                    reader.GetGuid(reader.GetOrdinal("id")),
                    Convert.ToDateTime(reader["FechaUtc"]),
                    reader["Nivel"] == DBNull.Value ? null : Convert.ToInt32(reader["Nivel"]),
                    Convert.ToString(reader["Lista"]) ?? string.Empty,
                    reader["TipoIdentidad"] == DBNull.Value ? null : Convert.ToByte(reader["TipoIdentidad"]),
                    Convert.ToString(reader["TipoIdentidadNombre"]) ?? string.Empty,
                    Convert.ToString(reader["Codigo"]) ?? string.Empty,
                    Convert.ToString(reader["Identidad"]) ?? string.Empty,
                    Convert.ToString(reader["Campo"]) ?? string.Empty,
                    Convert.ToString(reader["Operacion"]) ?? string.Empty,
                    Convert.ToString(reader["ValorAnterior"]) ?? string.Empty,
                    Convert.ToString(reader["ValorNuevo"]) ?? string.Empty,
                    Convert.ToString(reader["Usuario"]) ?? string.Empty,
                    Convert.ToString(reader["Origen"]) ?? string.Empty,
                    reader["CorrelationId"] == DBNull.Value ? Guid.Empty : reader.GetGuid(reader.GetOrdinal("CorrelationId")),
                    Convert.ToString(reader["Motivo"]) ?? string.Empty));
            }

            return new ListaPreciosHistorialPaginaDto
            {
                Items = items,
                Pagina = request.Pagina,
                TamanoPagina = request.TamanoPagina,
                Total = total,
                TotalPaginas = total == 0 ? 0 : (int)Math.Ceiling(total / (double)request.TamanoPagina)
            };
        }

        public async Task<IReadOnlyList<ListaPreciosHistorialDto>> ObtenerHistorialAsync(Guid idEmpresa, ListaPreciosIdentityKey identity, int? nivel, CancellationToken cancellationToken = default)
        {
            await using SqlConnection connection = _connectionFactory.CreateConnection(_descriptor);
            await connection.OpenAsync(cancellationToken);
            StringBuilder query = new(@"
SELECT TOP (200)
    h.id, h.idEmpresa, h.idListaPrecio, l.Nivel, h.TipoIdentidad, h.idProductoServicio, h.idVariante, h.idPresentacionVenta,
    h.Campo, h.Operacion, h.ValorAnterior, h.ValorNuevo, h.idUsuario, h.Usuario, h.Origen, h.CorrelationId, h.Motivo, h.FechaUtc
FROM dbo.ListaPreciosHistorial h
LEFT JOIN dbo.ListaPreciosListas l ON l.idEmpresa=h.idEmpresa AND l.id=h.idListaPrecio
WHERE h.idEmpresa=@IdEmpresa
  AND h.TipoIdentidad=@Tipo
  AND h.idProductoServicio=@Producto
  AND ((h.idVariante IS NULL AND @Variante IS NULL) OR h.idVariante=@Variante)
  AND ((h.idPresentacionVenta IS NULL AND @Presentacion IS NULL) OR h.idPresentacionVenta=@Presentacion)");
            using SqlCommand command = new() { Connection = connection };
            AddIdentityParameters(command, idEmpresa, Guid.Empty, identity);
            if (nivel.HasValue)
            {
                query.Append(" AND l.Nivel=@Nivel");
                command.Parameters.Add("@Nivel", SqlDbType.TinyInt).Value = nivel.Value;
            }

            query.Append(" ORDER BY h.FechaUtc DESC");
            command.CommandText = query.ToString();
            List<ListaPreciosHistorialDto> result = new();
            using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(new ListaPreciosHistorialDto(
                    reader.GetGuid(reader.GetOrdinal("id")),
                    reader.GetGuid(reader.GetOrdinal("idEmpresa")),
                    reader["idListaPrecio"] == DBNull.Value ? null : reader.GetGuid(reader.GetOrdinal("idListaPrecio")),
                    reader["Nivel"] == DBNull.Value ? null : Convert.ToInt32(reader["Nivel"]),
                    reader["TipoIdentidad"] == DBNull.Value ? null : Convert.ToByte(reader["TipoIdentidad"]),
                    reader["idProductoServicio"] == DBNull.Value ? null : reader.GetGuid(reader.GetOrdinal("idProductoServicio")),
                    reader["idVariante"] == DBNull.Value ? null : reader.GetGuid(reader.GetOrdinal("idVariante")),
                    reader["idPresentacionVenta"] == DBNull.Value ? null : reader.GetGuid(reader.GetOrdinal("idPresentacionVenta")),
                    Convert.ToString(reader["Campo"]) ?? string.Empty,
                    Convert.ToString(reader["Operacion"]) ?? string.Empty,
                    Convert.ToString(reader["ValorAnterior"]) ?? string.Empty,
                    Convert.ToString(reader["ValorNuevo"]) ?? string.Empty,
                    reader["idUsuario"] == DBNull.Value ? null : reader.GetGuid(reader.GetOrdinal("idUsuario")),
                    Convert.ToString(reader["Usuario"]) ?? string.Empty,
                    Convert.ToString(reader["Origen"]) ?? string.Empty,
                    reader["CorrelationId"] == DBNull.Value ? Guid.Empty : reader.GetGuid(reader.GetOrdinal("CorrelationId")),
                    Convert.ToString(reader["Motivo"]) ?? string.Empty,
                    Convert.ToDateTime(reader["FechaUtc"])));
            }

            return result;
        }

        private static Task InsertHistoryIfChangedAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            Guid idEmpresa,
            Guid idLista,
            ListaPreciosGuardarPrecioCommand command,
            string campo,
            string? previous,
            string? current,
            string operation,
            CancellationToken cancellationToken)
        {
            return string.Equals(previous ?? string.Empty, current ?? string.Empty, StringComparison.Ordinal)
                ? Task.CompletedTask
                : InsertHistoryAsync(connection, transaction, idEmpresa, idLista, command, campo, previous, current, operation, cancellationToken);
        }

        private static async Task InsertHistoryAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            Guid idEmpresa,
            Guid idLista,
            ListaPreciosGuardarPrecioCommand command,
            string campo,
            string? previous,
            string? current,
            string operation,
            CancellationToken cancellationToken)
        {
            using SqlCommand history = new(@"
INSERT INTO dbo.ListaPreciosHistorial
(id, idEmpresa, idListaPrecio, TipoIdentidad, idProductoServicio, idVariante, idPresentacionVenta, Campo, Operacion, ValorAnterior, ValorNuevo, idUsuario, Usuario, Origen, CorrelationId, Motivo, FechaUtc)
VALUES
(@Id, @IdEmpresa, @Lista, @Tipo, @Producto, @Variante, @Presentacion, @Campo, @Operacion, @ValorAnterior, @ValorNuevo, @UsuarioId, @Usuario, @Origen, @CorrelationId, @Motivo, SYSUTCDATETIME())", connection, transaction);
            history.Parameters.Add("@Id", SqlDbType.UniqueIdentifier).Value = Guid.NewGuid();
            AddIdentityParameters(history, idEmpresa, idLista, command.Identity);
            history.Parameters.Add("@Campo", SqlDbType.NVarChar, 60).Value = campo;
            history.Parameters.Add("@Operacion", SqlDbType.NVarChar, 40).Value = operation;
            history.Parameters.Add("@ValorAnterior", SqlDbType.NVarChar, 4000).Value = Db(previous);
            history.Parameters.Add("@ValorNuevo", SqlDbType.NVarChar, 4000).Value = Db(current);
            history.Parameters.Add("@UsuarioId", SqlDbType.UniqueIdentifier).Value = Db(command.UsuarioId);
            history.Parameters.Add("@Usuario", SqlDbType.NVarChar, 256).Value = Db(command.Usuario);
            history.Parameters.Add("@Origen", SqlDbType.NVarChar, 40).Value = command.Origen;
            history.Parameters.Add("@CorrelationId", SqlDbType.UniqueIdentifier).Value = command.CorrelationId == Guid.Empty ? Guid.NewGuid() : command.CorrelationId;
            history.Parameters.Add("@Motivo", SqlDbType.NVarChar, 500).Value = Db(command.Motivo);
            await history.ExecuteNonQueryAsync(cancellationToken);
        }

        private static ListaPreciosDetalleRecord ReadDetalle(SqlDataReader reader)
        {
            return new(
                reader.GetGuid(reader.GetOrdinal("id")),
                reader.GetGuid(reader.GetOrdinal("idListaPrecio")),
                Convert.ToInt32(reader["Nivel"]),
                Convert.ToByte(reader["TipoIdentidad"]),
                reader.GetGuid(reader.GetOrdinal("idProductoServicio")),
                reader["idVariante"] == DBNull.Value ? null : reader.GetGuid(reader.GetOrdinal("idVariante")),
                reader["idPresentacionVenta"] == DBNull.Value ? null : reader.GetGuid(reader.GetOrdinal("idPresentacionVenta")),
                Convert.ToDecimal(reader["Precio"]),
                reader["DescuentoPct"] == DBNull.Value ? null : Convert.ToDecimal(reader["DescuentoPct"]),
                Convert.ToByte(reader["RedondeoModo"]),
                reader["VigenciaInicio"] == DBNull.Value ? null : Convert.ToDateTime(reader["VigenciaInicio"]),
                reader["VigenciaFin"] == DBNull.Value ? null : Convert.ToDateTime(reader["VigenciaFin"]),
                Convert.ToBoolean(reader["Activo"]),
                Convert.ToDateTime(reader["FechaActualizacion"]));
        }

        private static ListaPreciosPrecioDto ToDto(ListaPreciosDetalleRecord detail) => new()
        {
            Id = detail.Id,
            IdListaPrecio = detail.IdListaPrecio,
            Nivel = detail.Nivel,
            TipoIdentidad = detail.TipoIdentidad,
            IdProductoServicio = detail.IdProductoServicio,
            IdVariante = detail.IdVariante,
            IdPresentacionVenta = detail.IdPresentacionVenta,
            Precio = detail.Precio,
            DescuentoPct = detail.DescuentoPct,
            RedondeoModo = detail.RedondeoModo,
            VigenciaInicio = detail.VigenciaInicio,
            VigenciaFin = detail.VigenciaFin,
            Activo = detail.Activo,
            FechaActualizacion = detail.FechaActualizacion
        };

        private static object Db(Guid? value) => value.HasValue && value.Value != Guid.Empty ? value.Value : DBNull.Value;
        private static object Db(decimal? value) => value.HasValue ? value.Value : DBNull.Value;
        private static object Db(DateTime? value) => value.HasValue ? value.Value.Date : DBNull.Value;
        private static object Db(string? value) => string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
    }
}
