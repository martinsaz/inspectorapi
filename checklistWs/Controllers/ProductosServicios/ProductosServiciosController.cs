using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using checklistWs.Models.ProductosServicios;
using checklistWs.Utiles;
using Firebase.Auth;
using Firebase.Auth.Providers;
using Firebase.Storage;
using Microsoft.AspNetCore.Mvc;

namespace checklistWs.Controllers.ProductosServicios
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductosServiciosController : ControllerBase
    {
        private const byte TipoProducto = 1;
        private const byte TipoServicio = 2;
        private const byte AplicaATodos = 0;
        private const byte AplicaAProductos = 1;
        private const byte AplicaAServicios = 2;
        private const byte MovimientoExistenciaInicial = 1;
        private const byte MovimientoEntrada = 2;
        private const byte MovimientoSalida = 3;
        private const byte MovimientoAjustePositivo = 4;
        private const byte MovimientoAjusteNegativo = 5;
        private const int CodigoLength = 50;
        private const int NombreLength = 150;
        private const int DescripcionLength = 1000;
        private const int DescripcionCatalogoLength = 500;
        private const int TagLength = 100;
        private const int UnidadCodigoLength = 30;
        private const int UnidadNombreLength = 100;
        private const int AbreviaturaLength = 20;
        private const int ReferenciaLength = 150;
        private const int NombreArchivoLength = 255;
        private const int MimeTypeLength = 120;
        private const int UrlLength = 1000;
        private const long ImagenMaxBytes = 10L * 1024L * 1024L;
        private const long UploadTemporalRequestLimitBytes = 12L * 1024L * 1024L;
        private static readonly TimeSpan TemporalTokenLifetime = TimeSpan.FromHours(6);
        private static readonly TimeSpan ProxyHeaderTolerance = TimeSpan.FromMinutes(5);
        private static readonly string[] MimeTypesImagenPermitidos = new[] { "image/jpeg", "image/png", "image/webp" };
        private static readonly string[] ExtensionesImagenPermitidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        private static readonly string[] EmpresaClaimKeys = new[] { "idEmpresa", "empresaId", "tenantId", "companyId", "tenant", "idempresa" };
        private static readonly string[] EmpresaNombreClaimKeys = new[] { "empresa", "empresaNombre", "tenantName", "companyName", "nombreEmpresa" };
        private static readonly string[] UsuarioClaimKeys = new[] { ClaimTypes.NameIdentifier, "sub", "idUsuario", "userid", "uid" };
        private const string ProxyEmpresaIdHeader = "X-ProductosServicios-Proxy-EmpresaId";
        private const string ProxyEmpresaKeyHeader = "X-ProductosServicios-Proxy-Empresa";
        private const string ProxyUsuarioIdHeader = "X-ProductosServicios-Proxy-UsuarioId";
        private const string ProxyTimestampHeader = "X-ProductosServicios-Proxy-Timestamp";
        private const string ProxySignatureHeader = "X-ProductosServicios-Proxy-Signature";
        private const string ProxyContextItemKey = "__ProductosServiciosProxyContext";

        private readonly IConfiguration _configuration;
        private readonly SqlConnectionFactory _connectionFactory;
        private readonly ILogger<ProductosServiciosController> _logger;

        public ProductosServiciosController(IConfiguration configuration, ILogger<ProductosServiciosController> logger)
        {
            _configuration = configuration;
            _connectionFactory = new SqlConnectionFactory(configuration);
            _logger = logger;
        }

        [HttpGet("ObtenerProductosServicios")]
        public async Task<IActionResult> ObtenerProductosServicios(
            Guid idEmpresa,
            string busqueda = "",
            byte? tipo = null,
            Guid? idCategoria = null,
            Guid? idMarca = null,
            Guid? idUnidadMedida = null,
            bool? causaInventario = null,
            string estatus = "")
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                using SqlConnection connection = CreateConnection();
                await connection.OpenAsync();

                StringBuilder query = new StringBuilder(@"
SELECT
    ps.id,
    ps.idEmpresa,
    ps.identityKey,
    ps.Tipo,
    CASE ps.Tipo WHEN 1 THEN 'Producto' ELSE 'Servicio' END AS TipoNombre,
    ps.Codigo,
    ISNULL(ps.Tag, '') AS Tag,
    ps.Nombre,
    ISNULL(ps.Descripcion, '') AS Descripcion,
    ps.idCategoria,
    cat.Nombre AS Categoria,
    cat.AplicaA AS CategoriaAplicaA,
    ps.idMarca,
    ISNULL(m.Nombre, '') AS Marca,
    ps.idUnidadMedida,
    um.Nombre AS UnidadMedida,
    um.Abreviatura AS UnidadAbreviatura,
    um.PermiteDecimales AS UnidadPermiteDecimales,
    ps.Costo,
    ps.PrecioPublico,
    ps.CausaInventario,
    ps.PermiteVentaSinExistencia,
    ex.id AS IdExistencia,
    ex.ExistenciaActual,
    ex.ExistenciaMinima,
    ex.CostoPromedio,
    ISNULL(ps.ImagenUrl, '') AS ImagenUrl,
    ISNULL(ps.ImagenNombre, '') AS ImagenNombre,
    ps.Activo,
    ps.FechaCreacion,
    ps.FechaActualizacion,
    ps.FechaArchivado
FROM dbo.ProductosServicios ps
INNER JOIN dbo.ProductosServiciosCategorias cat
    ON cat.idEmpresa = ps.idEmpresa AND cat.id = ps.idCategoria
INNER JOIN dbo.ProductosServiciosUnidadesMedida um
    ON um.idEmpresa = ps.idEmpresa AND um.id = ps.idUnidadMedida
LEFT JOIN dbo.ProductosServiciosMarcas m
    ON m.idEmpresa = ps.idEmpresa AND m.id = ps.idMarca
LEFT JOIN dbo.ProductosServiciosExistencias ex
    ON ex.idEmpresa = ps.idEmpresa AND ex.idProductoServicio = ps.id
WHERE ps.idEmpresa = @IdEmpresa");

                using SqlCommand command = new SqlCommand();
                command.Connection = connection;
                command.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);

                if (!string.IsNullOrWhiteSpace(busqueda))
                {
                    query.Append(@"
  AND (
      ps.Codigo LIKE @Busqueda
      OR ISNULL(ps.Tag, '') LIKE @Busqueda
      OR ps.Nombre LIKE @Busqueda
      OR ISNULL(ps.Descripcion, '') LIKE @Busqueda
  )");
                    command.Parameters.AddWithValue("@Busqueda", $"%{busqueda.Trim()}%");
                }

                AppendTinyIntFilter(query, command, "ps.Tipo", "@Tipo", tipo);
                AppendGuidFilter(query, command, "ps.idCategoria", "@IdCategoria", idCategoria);
                AppendGuidFilter(query, command, "ps.idMarca", "@IdMarca", idMarca);
                AppendGuidFilter(query, command, "ps.idUnidadMedida", "@IdUnidadMedida", idUnidadMedida);
                AppendBitFilter(query, command, "ps.CausaInventario", "@CausaInventario", causaInventario);
                AppendEstatusFilter(query, "ps.Activo", estatus);

                query.Append(" ORDER BY ps.Activo DESC, ps.Nombre, ps.Codigo");
                command.CommandText = query.ToString();

                List<ProductoServicioListadoDto> items = new List<ProductoServicioListadoDto>();
                using SqlDataReader reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    items.Add(MapProductoServicioListado(reader));
                }

                return Ok(items);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerProductosServicios", "No fue posible cargar los productos y servicios.");
            }
        }

        [HttpGet("ObtenerProductoServicio")]
        public async Task<IActionResult> ObtenerProductoServicio(Guid idEmpresa, Guid idProductoServicio)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                using SqlConnection connection = CreateConnection();
                await connection.OpenAsync();

                using SqlCommand command = new SqlCommand(@"
SELECT
    ps.id,
    ps.idEmpresa,
    ps.identityKey,
    ps.Tipo,
    CASE ps.Tipo WHEN 1 THEN 'Producto' ELSE 'Servicio' END AS TipoNombre,
    ps.Codigo,
    ISNULL(ps.Tag, '') AS Tag,
    ps.Nombre,
    ISNULL(ps.Descripcion, '') AS Descripcion,
    ps.idCategoria,
    cat.Nombre AS Categoria,
    cat.AplicaA AS CategoriaAplicaA,
    ps.idMarca,
    ISNULL(m.Nombre, '') AS Marca,
    ps.idUnidadMedida,
    um.Nombre AS UnidadMedida,
    um.Abreviatura AS UnidadAbreviatura,
    um.PermiteDecimales AS UnidadPermiteDecimales,
    ps.Costo,
    ps.PrecioPublico,
    ps.CausaInventario,
    ps.PermiteVentaSinExistencia,
    ex.id AS IdExistencia,
    ex.ExistenciaActual,
    ex.ExistenciaMinima,
    ex.CostoPromedio,
    ISNULL(ps.ImagenUrl, '') AS ImagenUrl,
    ISNULL(ps.ImagenNombre, '') AS ImagenNombre,
    ps.Activo,
    ps.FechaCreacion,
    ps.FechaActualizacion,
    ps.FechaArchivado
FROM dbo.ProductosServicios ps
INNER JOIN dbo.ProductosServiciosCategorias cat
    ON cat.idEmpresa = ps.idEmpresa AND cat.id = ps.idCategoria
INNER JOIN dbo.ProductosServiciosUnidadesMedida um
    ON um.idEmpresa = ps.idEmpresa AND um.id = ps.idUnidadMedida
LEFT JOIN dbo.ProductosServiciosMarcas m
    ON m.idEmpresa = ps.idEmpresa AND m.id = ps.idMarca
LEFT JOIN dbo.ProductosServiciosExistencias ex
    ON ex.idEmpresa = ps.idEmpresa AND ex.idProductoServicio = ps.id
WHERE ps.idEmpresa = @IdEmpresa AND ps.id = @IdProductoServicio", connection);

                command.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);
                command.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);

                ProductoServicioDetalleDto? detalle = null;
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        ProductoServicioListadoDto baseItem = MapProductoServicioListado(reader);
                        detalle = new ProductoServicioDetalleDto
                        {
                            Id = baseItem.Id,
                            IdEmpresa = baseItem.IdEmpresa,
                            IdentityKey = baseItem.IdentityKey,
                            Tipo = baseItem.Tipo,
                            TipoNombre = baseItem.TipoNombre,
                            Codigo = baseItem.Codigo,
                            Tag = baseItem.Tag,
                            Nombre = baseItem.Nombre,
                            Descripcion = baseItem.Descripcion,
                            IdCategoria = baseItem.IdCategoria,
                            Categoria = baseItem.Categoria,
                            CategoriaAplicaA = baseItem.CategoriaAplicaA,
                            IdMarca = baseItem.IdMarca,
                            Marca = baseItem.Marca,
                            IdUnidadMedida = baseItem.IdUnidadMedida,
                            UnidadMedida = baseItem.UnidadMedida,
                            UnidadAbreviatura = baseItem.UnidadAbreviatura,
                            UnidadPermiteDecimales = baseItem.UnidadPermiteDecimales,
                            Costo = baseItem.Costo,
                            PrecioPublico = baseItem.PrecioPublico,
                            CausaInventario = baseItem.CausaInventario,
                            PermiteVentaSinExistencia = baseItem.PermiteVentaSinExistencia,
                            ExistenciaActual = baseItem.ExistenciaActual,
                            ExistenciaMinima = baseItem.ExistenciaMinima,
                            CostoPromedio = baseItem.CostoPromedio,
                            ImagenUrl = baseItem.ImagenUrl,
                            ImagenNombre = baseItem.ImagenNombre,
                            Activo = baseItem.Activo,
                            FechaCreacion = baseItem.FechaCreacion,
                            FechaActualizacion = baseItem.FechaActualizacion,
                            FechaArchivado = baseItem.FechaArchivado,
                            IdExistencia = ReadNullableGuid(reader, "IdExistencia")
                        };
                    }
                }

                if (detalle == null)
                {
                    return NotFound(new ProductoServicioOperacionResponse { Mensaje = "El producto o servicio no está disponible." });
                }

                detalle.MovimientosRecientes = await ObtenerMovimientosInventarioInternoAsync(connection, context.IdEmpresa, idProductoServicio, 10);
                return Ok(detalle);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerProductoServicio", "No fue posible cargar el detalle del producto o servicio.");
            }
        }

        [HttpPost("SubirImagenTemporal")]
        [RequestFormLimits(MultipartBodyLengthLimit = UploadTemporalRequestLimitBytes)]
        [RequestSizeLimit(UploadTemporalRequestLimitBytes)]
        public async Task<IActionResult> SubirImagenTemporal(Guid idEmpresa, IFormFile? archivo)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                if (archivo == null || archivo.Length <= 0)
                {
                    return BadRequest(new ProductoServicioImagenTemporalResponse { Mensaje = "Selecciona una imagen válida para cargar." });
                }

                string validacion = ValidateImagenTemporalUpload(archivo);
                if (!string.IsNullOrWhiteSpace(validacion))
                {
                    return BadRequest(new ProductoServicioImagenTemporalResponse { Mensaje = validacion });
                }

                byte[] fileBytes = await ReadFileBytesAsync(archivo);
                validacion = ValidateImageSignature(archivo.FileName, archivo.ContentType, fileBytes);
                if (!string.IsNullOrWhiteSpace(validacion))
                {
                    return BadRequest(new ProductoServicioImagenTemporalResponse { Mensaje = validacion });
                }

                UploadedImagePayload uploaded = await UploadImageToFirebaseAsync(
                    BuildTemporalFolderName(context.EmpresaStorageKey),
                    BuildStoredFileName(archivo.FileName, archivo.ContentType),
                    fileBytes,
                    archivo.FileName,
                    archivo.ContentType,
                    archivo.Length);

                return Ok(new ProductoServicioImagenTemporalResponse
                {
                    Mensaje = "La imagen temporal fue cargada.",
                    Archivo = new ProductoServicioImagenTemporalDto
                    {
                        TemporalToken = CreateTemporalToken(new TemporalImageTokenPayload
                        {
                            NombreOriginal = uploaded.NombreOriginal,
                            NombreAlmacenado = uploaded.NombreAlmacenado,
                            Extension = uploaded.Extension,
                            MimeType = uploaded.MimeType,
                            UrlFirebase = uploaded.UrlFirebase,
                            FolderName = uploaded.FolderName,
                            PesoBytes = uploaded.PesoBytes,
                            ExpiraUtc = DateTime.UtcNow.Add(TemporalTokenLifetime)
                        }),
                        NombreOriginal = uploaded.NombreOriginal,
                        NombreAlmacenado = uploaded.NombreAlmacenado,
                        Extension = uploaded.Extension,
                        MimeType = uploaded.MimeType,
                        UrlFirebase = uploaded.UrlFirebase,
                        PesoBytes = uploaded.PesoBytes
                    }
                });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "SubirImagenTemporal", "No fue posible procesar la imagen temporal.");
            }
        }

        [HttpPost("LimpiarImagenTemporal")]
        public async Task<IActionResult> LimpiarImagenTemporal(Guid idEmpresa, [FromBody] ProductoServicioImagenTemporalCleanupRequest? request)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                List<FirebaseCleanupItem> items = new List<FirebaseCleanupItem>();
                foreach (string token in request?.Tokens ?? new List<string>())
                {
                    TemporalImageTokenPayload? payload = TryParseTemporalToken(token);
                    if (payload == null ||
                        string.IsNullOrWhiteSpace(payload.FolderName) ||
                        string.IsNullOrWhiteSpace(payload.NombreAlmacenado) ||
                        !FolderBelongsToEmpresa(payload.FolderName, context.EmpresaStorageKey))
                    {
                        continue;
                    }

                    items.Add(new FirebaseCleanupItem
                    {
                        FolderName = payload.FolderName,
                        StoredName = payload.NombreAlmacenado
                    });
                }

                await CleanupUploadedFirebaseFilesAsync(items);
                return Ok(new ProductoServicioOperacionResponse { Mensaje = "La limpieza temporal fue procesada." });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "LimpiarImagenTemporal", "No fue posible limpiar la imagen temporal.");
            }
        }

        [HttpPost("GuardarProductoServicio")]
        public async Task<IActionResult> GuardarProductoServicio([FromBody] ProductoServicioGuardarRequest request, Guid idEmpresa)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                string validacion = ValidateProductoServicioRequest(request, context.IdEmpresa);
                if (!string.IsNullOrWhiteSpace(validacion))
                {
                    return BadRequest(new ProductoServicioOperacionResponse { Mensaje = validacion });
                }

                NormalizedProductoServicioRequest normalized = NormalizeRequest(request);
                Guid productoId = normalized.Id ?? Guid.NewGuid();
                Guid? usuarioId = TryResolveUsuarioId();
                PreparedImageOperation preparedImage = await PrepareImageOperationAsync(context, productoId, normalized);

                try
                {
                    using SqlConnection connection = CreateConnection();
                    await connection.OpenAsync();
                    using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable);

                    bool esNuevo = !normalized.Id.HasValue || normalized.Id.Value == Guid.Empty;
                    ProductoServicioSnapshot? existente = esNuevo
                        ? null
                        : await ObtenerProductoServicioSnapshotAsync(connection, transaction, context.IdEmpresa, productoId);

                    if (!esNuevo && existente == null)
                    {
                        transaction.Rollback();
                        return NotFound(new ProductoServicioOperacionResponse { Mensaje = "El producto o servicio no está disponible para actualizar." });
                    }

                    if (await ExisteCodigoProductoServicioAsync(connection, transaction, context.IdEmpresa, normalized.Codigo, esNuevo ? null : productoId))
                    {
                        transaction.Rollback();
                        return BadRequest(new ProductoServicioOperacionResponse { Mensaje = "Ya existe un producto o servicio con el mismo código." });
                    }

                    string catalogoValidation = await ValidateCatalogReferencesAsync(connection, transaction, context.IdEmpresa, normalized, esNuevo, existente);
                    if (!string.IsNullOrWhiteSpace(catalogoValidation))
                    {
                        transaction.Rollback();
                        return BadRequest(new ProductoServicioOperacionResponse { Mensaje = catalogoValidation });
                    }

                    ProductoServicioExistenciaDto? existenciaActual = existente == null || existente.IdExistencia == null
                        ? null
                        : await ObtenerExistenciaInternaAsync(connection, transaction, context.IdEmpresa, productoId);
                    int movimientosHistoricos = await ContarMovimientosInventarioAsync(connection, transaction, context.IdEmpresa, productoId);

                    if (existente != null)
                    {
                        string transitionValidation = ValidateInventoryTransition(existente, normalized, existenciaActual, movimientosHistoricos);
                        if (!string.IsNullOrWhiteSpace(transitionValidation))
                        {
                            transaction.Rollback();
                            return BadRequest(new ProductoServicioOperacionResponse { Mensaje = transitionValidation });
                        }
                    }

                    ResolvedImageMutation imageMutation = ResolveImageMutation(existente, preparedImage);
                    DateTime ahora = DateTime.UtcNow;

                    if (esNuevo)
                    {
                        using SqlCommand insert = new SqlCommand(@"
INSERT INTO dbo.ProductosServicios
    (id, idEmpresa, identityKey, Tipo, Codigo, Tag, Nombre, Descripcion, idCategoria, idMarca, idUnidadMedida, Costo, PrecioPublico, CausaInventario, PermiteVentaSinExistencia, ImagenUrl, ImagenNombre, Activo, FechaCreacion, FechaActualizacion, FechaArchivado)
VALUES
    (@Id, @IdEmpresa, @IdentityKey, @Tipo, @Codigo, @Tag, @Nombre, @Descripcion, @IdCategoria, @IdMarca, @IdUnidadMedida, @Costo, @PrecioPublico, @CausaInventario, @PermiteVentaSinExistencia, @ImagenUrl, @ImagenNombre, 1, @FechaCreacion, @FechaActualizacion, NULL)", connection, transaction);

                        AddProductoServicioParameters(insert, productoId, context.IdEmpresa, normalized, imageMutation, ahora, true);
                        await insert.ExecuteNonQueryAsync();
                    }
                    else
                    {
                        using SqlCommand update = new SqlCommand(@"
UPDATE dbo.ProductosServicios
SET
    Tipo = @Tipo,
    Codigo = @Codigo,
    Tag = @Tag,
    Nombre = @Nombre,
    Descripcion = @Descripcion,
    idCategoria = @IdCategoria,
    idMarca = @IdMarca,
    idUnidadMedida = @IdUnidadMedida,
    Costo = @Costo,
    PrecioPublico = @PrecioPublico,
    CausaInventario = @CausaInventario,
    PermiteVentaSinExistencia = @PermiteVentaSinExistencia,
    ImagenUrl = @ImagenUrl,
    ImagenNombre = @ImagenNombre,
    FechaActualizacion = @FechaActualizacion
WHERE idEmpresa = @IdEmpresa AND id = @Id", connection, transaction);

                        AddProductoServicioParameters(update, productoId, context.IdEmpresa, normalized, imageMutation, ahora, false);
                        int rowsAffected = await update.ExecuteNonQueryAsync();
                        if (rowsAffected == 0)
                        {
                            transaction.Rollback();
                            return NotFound(new ProductoServicioOperacionResponse { Mensaje = "El producto o servicio no está disponible para actualizar." });
                        }
                    }

                    await SynchronizeInventoryForSaveAsync(connection, transaction, productoId, context.IdEmpresa, normalized, existente, existenciaActual, movimientosHistoricos, usuarioId, ahora);
                    transaction.Commit();

                    await FinalizeImageOperationAfterCommitAsync(preparedImage, imageMutation.PreviousImageCleanup);
                    return Ok(new ProductoServicioOperacionResponse
                    {
                        Mensaje = esNuevo ? "El producto o servicio fue registrado." : "El producto o servicio fue actualizado."
                    });
                }
                catch (Exception ex)
                {
                    await CompensatePreparedImageAsync(preparedImage);
                    return HandleException(ex, "GuardarProductoServicio", "No fue posible completar el guardado del producto o servicio.");
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex, "GuardarProductoServicio_Preparacion", "No fue posible completar el guardado del producto o servicio.");
            }
        }

        [HttpPost("BajaProductoServicio")]
        public async Task<IActionResult> BajaProductoServicio(Guid idEmpresa, Guid idProductoServicio)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            return await CambiarEstatusProductoServicioAsync(context.IdEmpresa, idProductoServicio, false);
        }

        [HttpPost("ActivarProductoServicio")]
        public async Task<IActionResult> ActivarProductoServicio(Guid idEmpresa, Guid idProductoServicio)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            return await CambiarEstatusProductoServicioAsync(context.IdEmpresa, idProductoServicio, true);
        }

        [HttpGet("ObtenerCombosProductosServicios")]
        public async Task<IActionResult> ObtenerCombosProductosServicios(Guid idEmpresa)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                ProductoServicioCombosDto response = new ProductoServicioCombosDto
                {
                    Categorias = await ObtenerCategoriasComboAsync(context.IdEmpresa, null),
                    Marcas = await ObtenerCatalogoBasicoComboAsync(context.IdEmpresa, "dbo.ProductosServiciosMarcas"),
                    UnidadesMedida = await ObtenerUnidadesComboAsync(context.IdEmpresa),
                    Tipos = new List<ProductoServicioOpcionDto>
                    {
                        new ProductoServicioOpcionDto { Clave = TipoProducto.ToString(), Nombre = "Producto" },
                        new ProductoServicioOpcionDto { Clave = TipoServicio.ToString(), Nombre = "Servicio" }
                    },
                    Estatus = new List<ProductoServicioOpcionDto>
                    {
                        new ProductoServicioOpcionDto { Clave = "activos", Nombre = "Activos" },
                        new ProductoServicioOpcionDto { Clave = "inactivos", Nombre = "Inactivos" },
                        new ProductoServicioOpcionDto { Clave = "todos", Nombre = "Todos" }
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerCombosProductosServicios", "No fue posible cargar los catálogos del módulo.");
            }
        }

        [HttpGet("ObtenerResumenProductosServicios")]
        public async Task<IActionResult> ObtenerResumenProductosServicios(Guid idEmpresa)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                using SqlConnection connection = CreateConnection();
                await connection.OpenAsync();

                using SqlCommand command = new SqlCommand(@"
SELECT
    COUNT(1) AS TotalRegistros,
    SUM(CASE WHEN ps.Activo = 1 THEN 1 ELSE 0 END) AS TotalActivos,
    SUM(CASE WHEN ps.Activo = 0 THEN 1 ELSE 0 END) AS TotalInactivos,
    SUM(CASE WHEN ps.Tipo = 1 THEN 1 ELSE 0 END) AS TotalProductos,
    SUM(CASE WHEN ps.Tipo = 2 THEN 1 ELSE 0 END) AS TotalServicios,
    SUM(CASE WHEN ps.CausaInventario = 1 THEN 1 ELSE 0 END) AS TotalConInventario,
    SUM(CASE WHEN ps.CausaInventario = 0 THEN 1 ELSE 0 END) AS TotalSinInventario,
    SUM(CASE WHEN ps.PermiteVentaSinExistencia = 1 THEN 1 ELSE 0 END) AS TotalInventarioNegativoPermitido,
    SUM(CASE WHEN ps.CausaInventario = 1 AND ex.id IS NOT NULL AND ex.ExistenciaActual <= ex.ExistenciaMinima THEN 1 ELSE 0 END) AS TotalBajoMinimo,
    SUM(CASE WHEN ps.CausaInventario = 1 AND ex.id IS NOT NULL THEN ISNULL(ex.ExistenciaActual, 0) * ISNULL(ps.Costo, 0) ELSE 0 END) AS ValorInventarioEstimado
FROM dbo.ProductosServicios ps
LEFT JOIN dbo.ProductosServiciosExistencias ex
    ON ex.idEmpresa = ps.idEmpresa AND ex.idProductoServicio = ps.id
WHERE ps.idEmpresa = @IdEmpresa", connection);

                command.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);

                using SqlDataReader reader = await command.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    return Ok(new ProductoServicioKpiResumenDto());
                }

                return Ok(new ProductoServicioKpiResumenDto
                {
                    TotalRegistros = ReadInt(reader, "TotalRegistros"),
                    TotalActivos = ReadInt(reader, "TotalActivos"),
                    TotalInactivos = ReadInt(reader, "TotalInactivos"),
                    TotalProductos = ReadInt(reader, "TotalProductos"),
                    TotalServicios = ReadInt(reader, "TotalServicios"),
                    TotalConInventario = ReadInt(reader, "TotalConInventario"),
                    TotalSinInventario = ReadInt(reader, "TotalSinInventario"),
                    TotalInventarioNegativoPermitido = ReadInt(reader, "TotalInventarioNegativoPermitido"),
                    TotalBajoMinimo = ReadInt(reader, "TotalBajoMinimo"),
                    ValorInventarioEstimado = ReadDecimal(reader, "ValorInventarioEstimado")
                });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerResumenProductosServicios", "No fue posible cargar el resumen del módulo.");
            }
        }

        [HttpGet("ExportarProductosServicios")]
        public async Task<IActionResult> ExportarProductosServicios(
            Guid idEmpresa,
            string busqueda = "",
            byte? tipo = null,
            Guid? idCategoria = null,
            Guid? idMarca = null,
            Guid? idUnidadMedida = null,
            bool? causaInventario = null,
            string estatus = "")
        {
            IActionResult listadoResult = await ObtenerProductosServicios(idEmpresa, busqueda, tipo, idCategoria, idMarca, idUnidadMedida, causaInventario, estatus);
            if (listadoResult is not OkObjectResult ok || ok.Value is not List<ProductoServicioListadoDto> items)
            {
                return listadoResult;
            }

            return Ok(items.Select(item => new ProductoServicioExportacionDto
            {
                Tipo = item.TipoNombre,
                Codigo = item.Codigo,
                Tag = item.Tag,
                Nombre = item.Nombre,
                Descripcion = item.Descripcion,
                Categoria = item.Categoria,
                Marca = item.Marca,
                UnidadMedida = item.UnidadMedida,
                UnidadAbreviatura = item.UnidadAbreviatura,
                Costo = item.Costo,
                PrecioPublico = item.PrecioPublico,
                CausaInventario = item.CausaInventario,
                PermiteVentaSinExistencia = item.PermiteVentaSinExistencia,
                ExistenciaActual = item.ExistenciaActual,
                ExistenciaMinima = item.ExistenciaMinima,
                Activo = item.Activo,
                FechaCreacion = item.FechaCreacion,
                FechaActualizacion = item.FechaActualizacion
            }).ToList());
        }

        [HttpGet("ObtenerCategoriasProductosServicios")]
        public async Task<IActionResult> ObtenerCategoriasProductosServicios(Guid idEmpresa, string busqueda = "", string estatus = "")
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                return Ok(await ObtenerCategoriasListadoAsync(context.IdEmpresa, busqueda, estatus));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerCategoriasProductosServicios", "No fue posible cargar las categorías.");
            }
        }

        [HttpGet("ObtenerCategoriaProductoServicio")]
        public async Task<IActionResult> ObtenerCategoriaProductoServicio(Guid idEmpresa, Guid idCategoria)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                ProductoServicioCategoriaDto? item = await ObtenerCategoriaAsync(context.IdEmpresa, idCategoria);
                if (item == null)
                {
                    return NotFound(new ProductoServicioOperacionResponse { Mensaje = "La categoría no está disponible." });
                }

                return Ok(item);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerCategoriaProductoServicio", "No fue posible cargar la categoría.");
            }
        }

        [HttpPost("GuardarCategoriaProductoServicio")]
        public async Task<IActionResult> GuardarCategoriaProductoServicio([FromBody] ProductoServicioCategoriaGuardarRequest request, Guid idEmpresa)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                string validacion = ValidateCategoriaRequest(request, context.IdEmpresa);
                if (!string.IsNullOrWhiteSpace(validacion))
                {
                    return BadRequest(new ProductoServicioOperacionResponse { Mensaje = validacion });
                }

                return await GuardarCategoriaAsync(request, context.IdEmpresa);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "GuardarCategoriaProductoServicio", "No fue posible guardar la categoría.");
            }
        }

        [HttpPost("BajaCategoriaProductoServicio")]
        public async Task<IActionResult> BajaCategoriaProductoServicio(Guid idEmpresa, Guid idCategoria)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            return await CambiarEstatusCatalogoBasicoAsync(context.IdEmpresa, idCategoria, "dbo.ProductosServiciosCategorias", "la categoría", false);
        }

        [HttpPost("ActivarCategoriaProductoServicio")]
        public async Task<IActionResult> ActivarCategoriaProductoServicio(Guid idEmpresa, Guid idCategoria)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            return await CambiarEstatusCatalogoBasicoAsync(context.IdEmpresa, idCategoria, "dbo.ProductosServiciosCategorias", "la categoría", true);
        }

        [HttpGet("ObtenerCatalogoCategoriasProductosServicios")]
        public async Task<IActionResult> ObtenerCatalogoCategoriasProductosServicios(Guid idEmpresa, byte? tipo = null, string busqueda = "")
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                return Ok(await ObtenerCategoriasComboAsync(context.IdEmpresa, tipo, busqueda));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerCatalogoCategoriasProductosServicios", "No fue posible cargar el catálogo de categorías.");
            }
        }

        [HttpGet("ExportarCategoriasProductosServicios")]
        public async Task<IActionResult> ExportarCategoriasProductosServicios(Guid idEmpresa, string busqueda = "", string estatus = "")
        {
            return await ObtenerCategoriasProductosServicios(idEmpresa, busqueda, estatus);
        }

        [HttpGet("ObtenerMarcasProductosServicios")]
        public async Task<IActionResult> ObtenerMarcasProductosServicios(Guid idEmpresa, string busqueda = "", string estatus = "")
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                return Ok(await ObtenerMarcasListadoAsync(context.IdEmpresa, busqueda, estatus));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerMarcasProductosServicios", "No fue posible cargar las marcas.");
            }
        }

        [HttpGet("ObtenerMarcaProductoServicio")]
        public async Task<IActionResult> ObtenerMarcaProductoServicio(Guid idEmpresa, Guid idMarca)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                ProductoServicioMarcaDto? item = await ObtenerMarcaAsync(context.IdEmpresa, idMarca);
                if (item == null)
                {
                    return NotFound(new ProductoServicioOperacionResponse { Mensaje = "La marca no está disponible." });
                }

                return Ok(item);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerMarcaProductoServicio", "No fue posible cargar la marca.");
            }
        }

        [HttpPost("GuardarMarcaProductoServicio")]
        public async Task<IActionResult> GuardarMarcaProductoServicio([FromBody] ProductoServicioMarcaGuardarRequest request, Guid idEmpresa)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                string validacion = ValidateMarcaRequest(request, context.IdEmpresa);
                if (!string.IsNullOrWhiteSpace(validacion))
                {
                    return BadRequest(new ProductoServicioOperacionResponse { Mensaje = validacion });
                }

                return await GuardarCatalogoBasicoAsync(
                    request.Id,
                    context.IdEmpresa,
                    "dbo.ProductosServiciosMarcas",
                    "la marca",
                    request.Codigo,
                    request.Nombre,
                    request.Descripcion,
                    null,
                    null,
                    false);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "GuardarMarcaProductoServicio", "No fue posible guardar la marca.");
            }
        }

        [HttpPost("BajaMarcaProductoServicio")]
        public async Task<IActionResult> BajaMarcaProductoServicio(Guid idEmpresa, Guid idMarca)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            return await CambiarEstatusCatalogoBasicoAsync(context.IdEmpresa, idMarca, "dbo.ProductosServiciosMarcas", "la marca", false);
        }

        [HttpPost("ActivarMarcaProductoServicio")]
        public async Task<IActionResult> ActivarMarcaProductoServicio(Guid idEmpresa, Guid idMarca)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            return await CambiarEstatusCatalogoBasicoAsync(context.IdEmpresa, idMarca, "dbo.ProductosServiciosMarcas", "la marca", true);
        }

        [HttpGet("ObtenerCatalogoMarcasProductosServicios")]
        public async Task<IActionResult> ObtenerCatalogoMarcasProductosServicios(Guid idEmpresa, string busqueda = "")
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                return Ok(await ObtenerCatalogoBasicoComboAsync(context.IdEmpresa, "dbo.ProductosServiciosMarcas", busqueda));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerCatalogoMarcasProductosServicios", "No fue posible cargar el catálogo de marcas.");
            }
        }

        [HttpGet("ExportarMarcasProductosServicios")]
        public async Task<IActionResult> ExportarMarcasProductosServicios(Guid idEmpresa, string busqueda = "", string estatus = "")
        {
            return await ObtenerMarcasProductosServicios(idEmpresa, busqueda, estatus);
        }

        [HttpGet("ObtenerUnidadesMedidaProductosServicios")]
        public async Task<IActionResult> ObtenerUnidadesMedidaProductosServicios(Guid idEmpresa, string busqueda = "", string estatus = "")
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                return Ok(await ObtenerUnidadesListadoAsync(context.IdEmpresa, busqueda, estatus));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerUnidadesMedidaProductosServicios", "No fue posible cargar las unidades de medida.");
            }
        }

        [HttpGet("ObtenerUnidadMedidaProductoServicio")]
        public async Task<IActionResult> ObtenerUnidadMedidaProductoServicio(Guid idEmpresa, Guid idUnidadMedida)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                ProductoServicioUnidadMedidaDto? item = await ObtenerUnidadAsync(context.IdEmpresa, idUnidadMedida);
                if (item == null)
                {
                    return NotFound(new ProductoServicioOperacionResponse { Mensaje = "La unidad de medida no está disponible." });
                }

                return Ok(item);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerUnidadMedidaProductoServicio", "No fue posible cargar la unidad de medida.");
            }
        }

        [HttpPost("GuardarUnidadMedidaProductoServicio")]
        public async Task<IActionResult> GuardarUnidadMedidaProductoServicio([FromBody] ProductoServicioUnidadMedidaGuardarRequest request, Guid idEmpresa)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                string validacion = ValidateUnidadRequest(request, context.IdEmpresa);
                if (!string.IsNullOrWhiteSpace(validacion))
                {
                    return BadRequest(new ProductoServicioOperacionResponse { Mensaje = validacion });
                }

                return await GuardarCatalogoBasicoAsync(
                    request.Id,
                    context.IdEmpresa,
                    "dbo.ProductosServiciosUnidadesMedida",
                    "la unidad de medida",
                    request.Codigo,
                    request.Nombre,
                    request.Descripcion,
                    request.Abreviatura,
                    request.PermiteDecimales,
                    true);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "GuardarUnidadMedidaProductoServicio", "No fue posible guardar la unidad de medida.");
            }
        }

        [HttpPost("BajaUnidadMedidaProductoServicio")]
        public async Task<IActionResult> BajaUnidadMedidaProductoServicio(Guid idEmpresa, Guid idUnidadMedida)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            return await CambiarEstatusCatalogoBasicoAsync(context.IdEmpresa, idUnidadMedida, "dbo.ProductosServiciosUnidadesMedida", "la unidad de medida", false);
        }

        [HttpPost("ActivarUnidadMedidaProductoServicio")]
        public async Task<IActionResult> ActivarUnidadMedidaProductoServicio(Guid idEmpresa, Guid idUnidadMedida)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            return await CambiarEstatusCatalogoBasicoAsync(context.IdEmpresa, idUnidadMedida, "dbo.ProductosServiciosUnidadesMedida", "la unidad de medida", true);
        }

        [HttpGet("ObtenerCatalogoUnidadesMedidaProductosServicios")]
        public async Task<IActionResult> ObtenerCatalogoUnidadesMedidaProductosServicios(Guid idEmpresa, string busqueda = "")
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                return Ok(await ObtenerUnidadesComboAsync(context.IdEmpresa, busqueda));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerCatalogoUnidadesMedidaProductosServicios", "No fue posible cargar el catálogo de unidades de medida.");
            }
        }

        [HttpGet("ExportarUnidadesMedidaProductosServicios")]
        public async Task<IActionResult> ExportarUnidadesMedidaProductosServicios(Guid idEmpresa, string busqueda = "", string estatus = "")
        {
            return await ObtenerUnidadesMedidaProductosServicios(idEmpresa, busqueda, estatus);
        }

        [HttpGet("ObtenerExistenciaProductoServicio")]
        public async Task<IActionResult> ObtenerExistenciaProductoServicio(Guid idEmpresa, Guid idProductoServicio)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                using SqlConnection connection = CreateConnection();
                await connection.OpenAsync();

                ProductoServicioExistenciaDto? item = await ObtenerExistenciaInternaAsync(connection, null, context.IdEmpresa, idProductoServicio);
                if (item == null)
                {
                    return NotFound(new ProductoServicioOperacionResponse { Mensaje = "La existencia no está disponible para el producto solicitado." });
                }

                return Ok(item);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerExistenciaProductoServicio", "No fue posible cargar la existencia del producto.");
            }
        }

        [HttpGet("ObtenerMovimientosInventarioProductoServicio")]
        public async Task<IActionResult> ObtenerMovimientosInventarioProductoServicio(Guid idEmpresa, Guid idProductoServicio)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                using SqlConnection connection = CreateConnection();
                await connection.OpenAsync();

                return Ok(await ObtenerMovimientosInventarioInternoAsync(connection, context.IdEmpresa, idProductoServicio, null));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerMovimientosInventarioProductoServicio", "No fue posible cargar los movimientos del producto.");
            }
        }

        [HttpPost("RegistrarEntradaInventarioProductoServicio")]
        public async Task<IActionResult> RegistrarEntradaInventarioProductoServicio([FromBody] ProductoServicioMovimientoGuardarRequest request, Guid idEmpresa)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            return await RegistrarMovimientoInventarioAsync(request, context.IdEmpresa, MovimientoEntrada);
        }

        [HttpPost("RegistrarSalidaInventarioProductoServicio")]
        public async Task<IActionResult> RegistrarSalidaInventarioProductoServicio([FromBody] ProductoServicioMovimientoGuardarRequest request, Guid idEmpresa)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            return await RegistrarMovimientoInventarioAsync(request, context.IdEmpresa, MovimientoSalida);
        }

        [HttpPost("RegistrarAjustePositivoInventarioProductoServicio")]
        public async Task<IActionResult> RegistrarAjustePositivoInventarioProductoServicio([FromBody] ProductoServicioMovimientoGuardarRequest request, Guid idEmpresa)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            return await RegistrarMovimientoInventarioAsync(request, context.IdEmpresa, MovimientoAjustePositivo);
        }

        [HttpPost("RegistrarAjusteNegativoInventarioProductoServicio")]
        public async Task<IActionResult> RegistrarAjusteNegativoInventarioProductoServicio([FromBody] ProductoServicioMovimientoGuardarRequest request, Guid idEmpresa)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            return await RegistrarMovimientoInventarioAsync(request, context.IdEmpresa, MovimientoAjusteNegativo);
        }

        private async Task<IActionResult> RegistrarMovimientoInventarioAsync(ProductoServicioMovimientoGuardarRequest request, Guid effectiveEmpresaId, byte tipoMovimiento)
        {
            try
            {
                string validacion = ValidateMovimientoRequest(request, effectiveEmpresaId);
                if (!string.IsNullOrWhiteSpace(validacion))
                {
                    return BadRequest(new ProductoServicioOperacionResponse { Mensaje = validacion });
                }

                using SqlConnection connection = CreateConnection();
                await connection.OpenAsync();
                using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable);

                ProductoServicioSnapshot? producto = await ObtenerProductoServicioSnapshotAsync(connection, transaction, effectiveEmpresaId, request.IdProductoServicio);
                if (producto == null || !producto.Activo)
                {
                    transaction.Rollback();
                    return NotFound(new ProductoServicioOperacionResponse { Mensaje = "El producto no está disponible para inventario." });
                }

                string inventoryValidation = ValidateProductoInventariable(producto);
                if (!string.IsNullOrWhiteSpace(inventoryValidation))
                {
                    transaction.Rollback();
                    return BadRequest(new ProductoServicioOperacionResponse { Mensaje = inventoryValidation });
                }

                ProductoServicioExistenciaDto? existencia = await ObtenerExistenciaInternaAsync(connection, transaction, effectiveEmpresaId, request.IdProductoServicio, true);
                if (existencia == null)
                {
                    transaction.Rollback();
                    return BadRequest(new ProductoServicioOperacionResponse { Mensaje = "El producto no cuenta con una existencia inicial para operar inventario." });
                }

                Guid? usuarioId = TryResolveUsuarioId();
                DateTime ahora = DateTime.UtcNow;
                string movimientoValidation = ValidateMovimientoAgainstExistencia(producto, existencia, request, tipoMovimiento);
                if (!string.IsNullOrWhiteSpace(movimientoValidation))
                {
                    transaction.Rollback();
                    return BadRequest(new ProductoServicioOperacionResponse { Mensaje = movimientoValidation });
                }

                decimal existenciaPosterior = CalcularExistenciaPosterior(existencia.ExistenciaActual, request.Cantidad, tipoMovimiento);
                await ActualizarExistenciaAsync(connection, transaction, existencia.Id, existenciaPosterior, existencia.ExistenciaMinima, request.CostoUnitario, ahora);
                await InsertarMovimientoInventarioAsync(connection, transaction, effectiveEmpresaId, request.IdProductoServicio, tipoMovimiento, request.Cantidad, existencia.ExistenciaActual, existenciaPosterior, request.CostoUnitario, request.Referencia, request.Observaciones, usuarioId, ahora);

                transaction.Commit();
                return Ok(new ProductoServicioOperacionResponse { Mensaje = $"El movimiento de inventario '{GetMovimientoNombre(tipoMovimiento)}' fue registrado." });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "RegistrarMovimientoInventario", "No fue posible registrar el movimiento de inventario.");
            }
        }

        private async Task<IActionResult> CambiarEstatusProductoServicioAsync(Guid effectiveEmpresaId, Guid idProductoServicio, bool activar)
        {
            try
            {
                using SqlConnection connection = CreateConnection();
                await connection.OpenAsync();

                using SqlCommand command = new SqlCommand(@"
UPDATE dbo.ProductosServicios
SET
    Activo = @Activo,
    FechaActualizacion = @FechaActualizacion,
    FechaArchivado = @FechaArchivado
WHERE idEmpresa = @IdEmpresa AND id = @Id AND Activo <> @Activo", connection);

                command.Parameters.AddWithValue("@IdEmpresa", effectiveEmpresaId);
                command.Parameters.AddWithValue("@Id", idProductoServicio);
                command.Parameters.AddWithValue("@Activo", activar);
                command.Parameters.AddWithValue("@FechaActualizacion", DateTime.UtcNow);
                command.Parameters.AddWithValue("@FechaArchivado", activar ? DBNull.Value : DateTime.UtcNow);

                int rowsAffected = await command.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    return NotFound(new ProductoServicioOperacionResponse { Mensaje = "El producto o servicio no está disponible para actualizar su estatus." });
                }

                return Ok(new ProductoServicioOperacionResponse { Mensaje = activar ? "El producto o servicio fue activado." : "El producto o servicio fue dado de baja." });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "CambiarEstatusProductoServicio", "No fue posible actualizar el estatus del producto o servicio.");
            }
        }

        private async Task<List<ProductoServicioCategoriaDto>> ObtenerCategoriasListadoAsync(Guid idEmpresa, string busqueda, string estatus)
        {
            using SqlConnection connection = CreateConnection();
            await connection.OpenAsync();

            StringBuilder query = new StringBuilder(@"
SELECT id, idEmpresa, identityKey, Codigo, Nombre, ISNULL(Descripcion, '') AS Descripcion, AplicaA, Activo, FechaCreacion, FechaActualizacion, FechaArchivado
FROM dbo.ProductosServiciosCategorias
WHERE idEmpresa = @IdEmpresa");

            using SqlCommand command = new SqlCommand();
            command.Connection = connection;
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            AppendBusquedaCatalogo(query, command, busqueda);
            AppendEstatusFilter(query, "Activo", estatus);
            query.Append(" ORDER BY Activo DESC, Nombre, Codigo");
            command.CommandText = query.ToString();

            List<ProductoServicioCategoriaDto> items = new List<ProductoServicioCategoriaDto>();
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(MapCategoria(reader));
            }

            return items;
        }

        private async Task<ProductoServicioCategoriaDto?> ObtenerCategoriaAsync(Guid idEmpresa, Guid idCategoria)
        {
            using SqlConnection connection = CreateConnection();
            await connection.OpenAsync();

            using SqlCommand command = new SqlCommand(@"
SELECT id, idEmpresa, identityKey, Codigo, Nombre, ISNULL(Descripcion, '') AS Descripcion, AplicaA, Activo, FechaCreacion, FechaActualizacion, FechaArchivado
FROM dbo.ProductosServiciosCategorias
WHERE idEmpresa = @IdEmpresa AND id = @Id", connection);

            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@Id", idCategoria);
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            return await reader.ReadAsync() ? MapCategoria(reader) : null;
        }

        private async Task<List<ProductoServicioMarcaDto>> ObtenerMarcasListadoAsync(Guid idEmpresa, string busqueda, string estatus)
        {
            using SqlConnection connection = CreateConnection();
            await connection.OpenAsync();

            StringBuilder query = new StringBuilder(@"
SELECT id, idEmpresa, identityKey, Codigo, Nombre, ISNULL(Descripcion, '') AS Descripcion, Activo, FechaCreacion, FechaActualizacion, FechaArchivado
FROM dbo.ProductosServiciosMarcas
WHERE idEmpresa = @IdEmpresa");

            using SqlCommand command = new SqlCommand();
            command.Connection = connection;
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            AppendBusquedaCatalogo(query, command, busqueda);
            AppendEstatusFilter(query, "Activo", estatus);
            query.Append(" ORDER BY Activo DESC, Nombre, Codigo");
            command.CommandText = query.ToString();

            List<ProductoServicioMarcaDto> items = new List<ProductoServicioMarcaDto>();
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(MapMarca(reader));
            }

            return items;
        }

        private async Task<ProductoServicioMarcaDto?> ObtenerMarcaAsync(Guid idEmpresa, Guid idMarca)
        {
            using SqlConnection connection = CreateConnection();
            await connection.OpenAsync();

            using SqlCommand command = new SqlCommand(@"
SELECT id, idEmpresa, identityKey, Codigo, Nombre, ISNULL(Descripcion, '') AS Descripcion, Activo, FechaCreacion, FechaActualizacion, FechaArchivado
FROM dbo.ProductosServiciosMarcas
WHERE idEmpresa = @IdEmpresa AND id = @Id", connection);

            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@Id", idMarca);
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            return await reader.ReadAsync() ? MapMarca(reader) : null;
        }

        private async Task<List<ProductoServicioUnidadMedidaDto>> ObtenerUnidadesListadoAsync(Guid idEmpresa, string busqueda, string estatus)
        {
            using SqlConnection connection = CreateConnection();
            await connection.OpenAsync();

            StringBuilder query = new StringBuilder(@"
SELECT id, idEmpresa, identityKey, Codigo, Nombre, N'' AS Descripcion, Abreviatura, PermiteDecimales, Activo, FechaCreacion, FechaActualizacion, FechaArchivado
FROM dbo.ProductosServiciosUnidadesMedida
WHERE idEmpresa = @IdEmpresa");

            using SqlCommand command = new SqlCommand();
            command.Connection = connection;
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                query.Append(" AND (Codigo LIKE @Busqueda OR Nombre LIKE @Busqueda OR Abreviatura LIKE @Busqueda)");
                command.Parameters.AddWithValue("@Busqueda", $"%{busqueda.Trim()}%");
            }
            AppendEstatusFilter(query, "Activo", estatus);
            query.Append(" ORDER BY Activo DESC, Nombre, Codigo");
            command.CommandText = query.ToString();

            List<ProductoServicioUnidadMedidaDto> items = new List<ProductoServicioUnidadMedidaDto>();
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(MapUnidad(reader));
            }

            return items;
        }

        private async Task<ProductoServicioUnidadMedidaDto?> ObtenerUnidadAsync(Guid idEmpresa, Guid idUnidadMedida)
        {
            using SqlConnection connection = CreateConnection();
            await connection.OpenAsync();

            using SqlCommand command = new SqlCommand(@"
SELECT id, idEmpresa, identityKey, Codigo, Nombre, N'' AS Descripcion, Abreviatura, PermiteDecimales, Activo, FechaCreacion, FechaActualizacion, FechaArchivado
FROM dbo.ProductosServiciosUnidadesMedida
WHERE idEmpresa = @IdEmpresa AND id = @Id", connection);

            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@Id", idUnidadMedida);
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            return await reader.ReadAsync() ? MapUnidad(reader) : null;
        }

        private async Task<IActionResult> GuardarCategoriaAsync(ProductoServicioCategoriaGuardarRequest request, Guid idEmpresa)
        {
            return await GuardarCatalogoBasicoAsync(
                request.Id,
                idEmpresa,
                "dbo.ProductosServiciosCategorias",
                "la categoría",
                request.Codigo,
                request.Nombre,
                request.Descripcion,
                null,
                null,
                false,
                request.AplicaA);
        }

        private async Task<IActionResult> GuardarCatalogoBasicoAsync(
            Guid? id,
            Guid idEmpresa,
            string tableName,
            string label,
            string codigo,
            string nombre,
            string descripcion,
            string? abreviatura,
            bool? permiteDecimales,
            bool includeUnidadFields,
            byte? aplicaA = null)
        {
            using SqlConnection connection = CreateConnection();
            await connection.OpenAsync();
            using SqlTransaction transaction = connection.BeginTransaction();

            Guid itemId = id ?? Guid.NewGuid();
            bool esNuevo = !id.HasValue || id.Value == Guid.Empty;

            if (await ExisteCodigoCatalogoAsync(connection, transaction, idEmpresa, codigo, esNuevo ? null : itemId, tableName))
            {
                transaction.Rollback();
                return BadRequest(new ProductoServicioOperacionResponse { Mensaje = $"Ya existe {label} con el mismo código." });
            }

            DateTime ahora = DateTime.UtcNow;
            if (esNuevo)
            {
                string insertSql = includeUnidadFields
                    ? $@"
INSERT INTO {tableName}
    (id, idEmpresa, identityKey, Codigo, Nombre, Abreviatura, PermiteDecimales, Activo, FechaCreacion, FechaActualizacion, FechaArchivado)
VALUES
    (@Id, @IdEmpresa, @IdentityKey, @Codigo, @Nombre, @Abreviatura, @PermiteDecimales, 1, @FechaCreacion, @FechaActualizacion, NULL)"
                    : aplicaA.HasValue
                        ? $@"
INSERT INTO {tableName}
    (id, idEmpresa, identityKey, Codigo, Nombre, Descripcion, AplicaA, Activo, FechaCreacion, FechaActualizacion, FechaArchivado)
VALUES
    (@Id, @IdEmpresa, @IdentityKey, @Codigo, @Nombre, @Descripcion, @AplicaA, 1, @FechaCreacion, @FechaActualizacion, NULL)"
                        : $@"
INSERT INTO {tableName}
    (id, idEmpresa, identityKey, Codigo, Nombre, Descripcion, Activo, FechaCreacion, FechaActualizacion, FechaArchivado)
VALUES
    (@Id, @IdEmpresa, @IdentityKey, @Codigo, @Nombre, @Descripcion, 1, @FechaCreacion, @FechaActualizacion, NULL)";

                using SqlCommand insert = new SqlCommand(insertSql, connection, transaction);
                AddCatalogoParameters(insert, itemId, idEmpresa, codigo, nombre, descripcion, ahora, abreviatura, permiteDecimales, aplicaA);
                await insert.ExecuteNonQueryAsync();
            }
            else
            {
                string updateSql = includeUnidadFields
                    ? $@"
UPDATE {tableName}
SET
    Codigo = @Codigo,
    Nombre = @Nombre,
    Abreviatura = @Abreviatura,
    PermiteDecimales = @PermiteDecimales,
    FechaActualizacion = @FechaActualizacion
WHERE idEmpresa = @IdEmpresa AND id = @Id"
                    : aplicaA.HasValue
                        ? $@"
UPDATE {tableName}
SET
    Codigo = @Codigo,
    Nombre = @Nombre,
    Descripcion = @Descripcion,
    AplicaA = @AplicaA,
    FechaActualizacion = @FechaActualizacion
WHERE idEmpresa = @IdEmpresa AND id = @Id"
                        : $@"
UPDATE {tableName}
SET
    Codigo = @Codigo,
    Nombre = @Nombre,
    Descripcion = @Descripcion,
    FechaActualizacion = @FechaActualizacion
WHERE idEmpresa = @IdEmpresa AND id = @Id";

                using SqlCommand update = new SqlCommand(updateSql, connection, transaction);
                AddCatalogoParameters(update, itemId, idEmpresa, codigo, nombre, descripcion, ahora, abreviatura, permiteDecimales, aplicaA);
                int rowsAffected = await update.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    transaction.Rollback();
                    return NotFound(new ProductoServicioOperacionResponse { Mensaje = $"No fue posible actualizar {label}." });
                }
            }

            transaction.Commit();
            return Ok(new ProductoServicioOperacionResponse { Mensaje = esNuevo ? $"Se registró {label}." : $"Se actualizó {label}." });
        }

        private async Task<IActionResult> CambiarEstatusCatalogoBasicoAsync(Guid idEmpresa, Guid id, string tableName, string label, bool activar)
        {
            try
            {
                using SqlConnection connection = CreateConnection();
                await connection.OpenAsync();

                using SqlCommand command = new SqlCommand($@"
UPDATE {tableName}
SET
    Activo = @Activo,
    FechaActualizacion = @FechaActualizacion,
    FechaArchivado = @FechaArchivado
WHERE idEmpresa = @IdEmpresa AND id = @Id AND Activo <> @Activo", connection);

                command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                command.Parameters.AddWithValue("@Id", id);
                command.Parameters.AddWithValue("@Activo", activar);
                command.Parameters.AddWithValue("@FechaActualizacion", DateTime.UtcNow);
                command.Parameters.AddWithValue("@FechaArchivado", activar ? DBNull.Value : DateTime.UtcNow);

                int rowsAffected = await command.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    return NotFound(new ProductoServicioOperacionResponse { Mensaje = $"No fue posible actualizar el estatus de {label}." });
                }

                return Ok(new ProductoServicioOperacionResponse { Mensaje = activar ? $"Se activó {label}." : $"Se dio de baja {label}." });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "CambiarEstatusCatalogoBasico", "No fue posible actualizar el estatus del catálogo.");
            }
        }

        private async Task<List<ProductoServicioCatalogoComboDto>> ObtenerCategoriasComboAsync(Guid idEmpresa, byte? tipo, string busqueda = "")
        {
            using SqlConnection connection = CreateConnection();
            await connection.OpenAsync();

            StringBuilder query = new StringBuilder(@"
SELECT id, Codigo, Nombre, ISNULL(Descripcion, '') AS Descripcion, Activo, AplicaA, '' AS Abreviatura, CAST(NULL AS bit) AS PermiteDecimales
FROM dbo.ProductosServiciosCategorias
WHERE idEmpresa = @IdEmpresa AND Activo = 1");

            using SqlCommand command = new SqlCommand();
            command.Connection = connection;
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);

            if (tipo.HasValue)
            {
                query.Append(" AND (AplicaA = @AplicaATodos OR AplicaA = @AplicaAEspecifico)");
                command.Parameters.AddWithValue("@AplicaATodos", AplicaATodos);
                command.Parameters.AddWithValue("@AplicaAEspecifico", tipo.Value == TipoProducto ? AplicaAProductos : AplicaAServicios);
            }

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                query.Append(" AND (Codigo LIKE @Busqueda OR Nombre LIKE @Busqueda OR ISNULL(Descripcion, '') LIKE @Busqueda)");
                command.Parameters.AddWithValue("@Busqueda", $"%{busqueda.Trim()}%");
            }

            query.Append(" ORDER BY Nombre, Codigo");
            command.CommandText = query.ToString();

            List<ProductoServicioCatalogoComboDto> items = new List<ProductoServicioCatalogoComboDto>();
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(MapCatalogoCombo(reader));
            }

            return items;
        }

        private async Task<List<ProductoServicioCatalogoComboDto>> ObtenerCatalogoBasicoComboAsync(Guid idEmpresa, string tableName, string busqueda = "")
        {
            using SqlConnection connection = CreateConnection();
            await connection.OpenAsync();

            StringBuilder query = new StringBuilder($@"
SELECT id, Codigo, Nombre, ISNULL(Descripcion, '') AS Descripcion, Activo, CAST(NULL AS tinyint) AS AplicaA, '' AS Abreviatura, CAST(NULL AS bit) AS PermiteDecimales
FROM {tableName}
WHERE idEmpresa = @IdEmpresa AND Activo = 1");

            using SqlCommand command = new SqlCommand();
            command.Connection = connection;
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                query.Append(" AND (Codigo LIKE @Busqueda OR Nombre LIKE @Busqueda OR ISNULL(Descripcion, '') LIKE @Busqueda)");
                command.Parameters.AddWithValue("@Busqueda", $"%{busqueda.Trim()}%");
            }

            query.Append(" ORDER BY Nombre, Codigo");
            command.CommandText = query.ToString();

            List<ProductoServicioCatalogoComboDto> items = new List<ProductoServicioCatalogoComboDto>();
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(MapCatalogoCombo(reader));
            }

            return items;
        }

        private async Task<List<ProductoServicioCatalogoComboDto>> ObtenerUnidadesComboAsync(Guid idEmpresa, string busqueda = "")
        {
            using SqlConnection connection = CreateConnection();
            await connection.OpenAsync();

            StringBuilder query = new StringBuilder(@"
SELECT id, Codigo, Nombre, N'' AS Descripcion, Activo, CAST(NULL AS tinyint) AS AplicaA, Abreviatura, PermiteDecimales
FROM dbo.ProductosServiciosUnidadesMedida
WHERE idEmpresa = @IdEmpresa AND Activo = 1");

            using SqlCommand command = new SqlCommand();
            command.Connection = connection;
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                query.Append(" AND (Codigo LIKE @Busqueda OR Nombre LIKE @Busqueda OR Abreviatura LIKE @Busqueda)");
                command.Parameters.AddWithValue("@Busqueda", $"%{busqueda.Trim()}%");
            }

            query.Append(" ORDER BY Nombre, Codigo");
            command.CommandText = query.ToString();

            List<ProductoServicioCatalogoComboDto> items = new List<ProductoServicioCatalogoComboDto>();
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(MapCatalogoCombo(reader));
            }

            return items;
        }

        private async Task<List<ProductoServicioMovimientoDto>> ObtenerMovimientosInventarioInternoAsync(SqlConnection connection, Guid idEmpresa, Guid idProductoServicio, int? take)
        {
            StringBuilder query = new StringBuilder(@"
SELECT ");
            if (take.HasValue)
            {
                query.Append("TOP (@Take) ");
            }

            query.Append(@"
    id,
    idEmpresa,
    identityKey,
    idProductoServicio,
    TipoMovimiento,
    Cantidad,
    ExistenciaAnterior,
    ExistenciaPosterior,
    CostoUnitario,
    ISNULL(Referencia, '') AS Referencia,
    ISNULL(Observaciones, '') AS Observaciones,
    idUsuario,
    FechaMovimiento
FROM dbo.ProductosServiciosMovimientosInventario
WHERE idEmpresa = @IdEmpresa AND idProductoServicio = @IdProductoServicio
ORDER BY FechaMovimiento DESC, id DESC");

            using SqlCommand command = new SqlCommand(query.ToString(), connection);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);
            if (take.HasValue)
            {
                command.Parameters.AddWithValue("@Take", take.Value);
            }

            List<ProductoServicioMovimientoDto> items = new List<ProductoServicioMovimientoDto>();
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(MapMovimiento(reader));
            }

            return items;
        }

        private async Task<ProductoServicioSnapshot?> ObtenerProductoServicioSnapshotAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idProductoServicio)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT
    ps.id,
    ps.idEmpresa,
    ps.Tipo,
    ps.Codigo,
    ps.idCategoria,
    ps.idMarca,
    ps.idUnidadMedida,
    ps.CausaInventario,
    ps.PermiteVentaSinExistencia,
    ps.Activo,
    ISNULL(ps.ImagenUrl, '') AS ImagenUrl,
    ISNULL(ps.ImagenNombre, '') AS ImagenNombre,
    ex.id AS IdExistencia
FROM dbo.ProductosServicios ps
LEFT JOIN dbo.ProductosServiciosExistencias ex
    ON ex.idEmpresa = ps.idEmpresa AND ex.idProductoServicio = ps.id
WHERE ps.idEmpresa = @IdEmpresa AND ps.id = @Id", connection, transaction);

            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@Id", idProductoServicio);
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }

            return new ProductoServicioSnapshot
            {
                Id = ReadGuid(reader, "id"),
                IdEmpresa = ReadGuid(reader, "idEmpresa"),
                Tipo = ReadByte(reader, "Tipo"),
                Codigo = ReadString(reader, "Codigo"),
                IdCategoria = ReadGuid(reader, "idCategoria"),
                IdMarca = ReadNullableGuid(reader, "idMarca"),
                IdUnidadMedida = ReadGuid(reader, "idUnidadMedida"),
                CausaInventario = ReadBool(reader, "CausaInventario"),
                PermiteVentaSinExistencia = ReadBool(reader, "PermiteVentaSinExistencia"),
                Activo = ReadBool(reader, "Activo"),
                ImagenUrl = ReadString(reader, "ImagenUrl"),
                ImagenNombre = ReadString(reader, "ImagenNombre"),
                IdExistencia = ReadNullableGuid(reader, "IdExistencia")
            };
        }

        private async Task<ProductoServicioExistenciaDto?> ObtenerExistenciaInternaAsync(SqlConnection connection, SqlTransaction? transaction, Guid idEmpresa, Guid idProductoServicio, bool lockRow = false)
        {
            string lockHint = lockRow ? " WITH (UPDLOCK, HOLDLOCK)" : string.Empty;
            using SqlCommand command = new SqlCommand($@"
SELECT id, idEmpresa, identityKey, idProductoServicio, ExistenciaActual, ExistenciaMinima, CostoPromedio, FechaCreacion, FechaActualizacion
FROM dbo.ProductosServiciosExistencias{lockHint}
WHERE idEmpresa = @IdEmpresa AND idProductoServicio = @IdProductoServicio", connection, transaction);

            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);

            using SqlDataReader reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }

            return new ProductoServicioExistenciaDto
            {
                Id = ReadGuid(reader, "id"),
                IdEmpresa = ReadGuid(reader, "idEmpresa"),
                IdentityKey = ReadGuid(reader, "identityKey"),
                IdProductoServicio = ReadGuid(reader, "idProductoServicio"),
                ExistenciaActual = ReadDecimal(reader, "ExistenciaActual"),
                ExistenciaMinima = ReadDecimal(reader, "ExistenciaMinima"),
                CostoPromedio = ReadNullableDecimal(reader, "CostoPromedio"),
                FechaCreacion = ReadDateTime(reader, "FechaCreacion"),
                FechaActualizacion = ReadDateTime(reader, "FechaActualizacion")
            };
        }

        private async Task<int> ContarMovimientosInventarioAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idProductoServicio)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT COUNT(1)
FROM dbo.ProductosServiciosMovimientosInventario
WHERE idEmpresa = @IdEmpresa AND idProductoServicio = @IdProductoServicio", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);
            return Convert.ToInt32(await command.ExecuteScalarAsync());
        }

        private async Task SynchronizeInventoryForSaveAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            Guid productoId,
            Guid idEmpresa,
            NormalizedProductoServicioRequest request,
            ProductoServicioSnapshot? existente,
            ProductoServicioExistenciaDto? existenciaActual,
            int movimientosHistoricos,
            Guid? usuarioId,
            DateTime ahora)
        {
            bool targetInventariable = request.Tipo == TipoProducto && request.CausaInventario;

            if (!targetInventariable)
            {
                if (existenciaActual != null)
                {
                    await EnsureExistenciaRemovableAsync(connection, transaction, existenciaActual, idEmpresa, productoId, movimientosHistoricos);
                    await EliminarExistenciaAsync(connection, transaction, idEmpresa, productoId);
                }

                return;
            }

            decimal existenciaInicial = request.ExistenciaInicial ?? (existenciaActual?.ExistenciaActual ?? 0m);
            decimal existenciaMinima = request.ExistenciaMinima ?? (existenciaActual?.ExistenciaMinima ?? 0m);
            decimal? costoPromedio = request.Costo ?? existenciaActual?.CostoPromedio;

            if (existenciaActual == null)
            {
                Guid existenciaId = Guid.NewGuid();
                using SqlCommand insert = new SqlCommand(@"
INSERT INTO dbo.ProductosServiciosExistencias
    (id, idEmpresa, identityKey, idProductoServicio, ExistenciaActual, ExistenciaMinima, CostoPromedio, FechaCreacion, FechaActualizacion)
VALUES
    (@Id, @IdEmpresa, @IdentityKey, @IdProductoServicio, @ExistenciaActual, @ExistenciaMinima, @CostoPromedio, @FechaCreacion, @FechaActualizacion)", connection, transaction);

                insert.Parameters.AddWithValue("@Id", existenciaId);
                insert.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                insert.Parameters.AddWithValue("@IdentityKey", Guid.NewGuid());
                insert.Parameters.AddWithValue("@IdProductoServicio", productoId);
                insert.Parameters.AddWithValue("@ExistenciaActual", existenciaInicial);
                insert.Parameters.AddWithValue("@ExistenciaMinima", existenciaMinima);
                insert.Parameters.AddWithValue("@CostoPromedio", costoPromedio.HasValue ? costoPromedio.Value : DBNull.Value);
                insert.Parameters.AddWithValue("@FechaCreacion", ahora);
                insert.Parameters.AddWithValue("@FechaActualizacion", ahora);
                await insert.ExecuteNonQueryAsync();

                if (existenciaInicial > 0)
                {
                    await InsertarMovimientoInventarioAsync(connection, transaction, idEmpresa, productoId, MovimientoExistenciaInicial, existenciaInicial, 0m, existenciaInicial, request.Costo, "Alta inicial", "Movimiento inicial generado durante el alta o activación de inventario.", usuarioId, ahora);
                }

                return;
            }

            using SqlCommand update = new SqlCommand(@"
UPDATE dbo.ProductosServiciosExistencias
SET
    ExistenciaMinima = @ExistenciaMinima,
    CostoPromedio = @CostoPromedio,
    FechaActualizacion = @FechaActualizacion
WHERE idEmpresa = @IdEmpresa AND idProductoServicio = @IdProductoServicio", connection, transaction);

            update.Parameters.AddWithValue("@ExistenciaMinima", existenciaMinima);
            update.Parameters.AddWithValue("@CostoPromedio", costoPromedio.HasValue ? costoPromedio.Value : DBNull.Value);
            update.Parameters.AddWithValue("@FechaActualizacion", ahora);
            update.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            update.Parameters.AddWithValue("@IdProductoServicio", productoId);
            await update.ExecuteNonQueryAsync();

            if (existente != null &&
                !existente.CausaInventario &&
                request.CausaInventario &&
                movimientosHistoricos == 0 &&
                existenciaInicial > 0 &&
                existenciaActual.ExistenciaActual == 0)
            {
                await ActualizarExistenciaAsync(connection, transaction, existenciaActual.Id, existenciaInicial, existenciaMinima, request.Costo, ahora);
                await InsertarMovimientoInventarioAsync(connection, transaction, idEmpresa, productoId, MovimientoExistenciaInicial, existenciaInicial, 0m, existenciaInicial, request.Costo, "Activación inventario", "Movimiento inicial generado al convertir el registro en inventariable.", usuarioId, ahora);
            }
        }

        private async Task EnsureExistenciaRemovableAsync(SqlConnection connection, SqlTransaction transaction, ProductoServicioExistenciaDto existenciaActual, Guid idEmpresa, Guid idProductoServicio, int movimientosHistoricos)
        {
            int movimientosConfirmados = movimientosHistoricos;
            if (movimientosConfirmados == 0)
            {
                movimientosConfirmados = await ContarMovimientosInventarioAsync(connection, transaction, idEmpresa, idProductoServicio);
            }

            if (movimientosConfirmados > 0 || existenciaActual.ExistenciaActual != 0m)
            {
                throw new InvalidOperationException("No es posible convertir a no inventariable un producto con historial o existencia distinta de cero.");
            }
        }

        private async Task EliminarExistenciaAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idProductoServicio)
        {
            using SqlCommand delete = new SqlCommand(@"
DELETE FROM dbo.ProductosServiciosExistencias
WHERE idEmpresa = @IdEmpresa AND idProductoServicio = @IdProductoServicio", connection, transaction);
            delete.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            delete.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);
            await delete.ExecuteNonQueryAsync();
        }

        private async Task ActualizarExistenciaAsync(SqlConnection connection, SqlTransaction transaction, Guid idExistencia, decimal existenciaPosterior, decimal existenciaMinima, decimal? costoPromedio, DateTime ahora)
        {
            using SqlCommand command = new SqlCommand(@"
UPDATE dbo.ProductosServiciosExistencias
SET
    ExistenciaActual = @ExistenciaActual,
    ExistenciaMinima = @ExistenciaMinima,
    CostoPromedio = COALESCE(@CostoPromedio, CostoPromedio),
    FechaActualizacion = @FechaActualizacion
WHERE id = @Id", connection, transaction);

            command.Parameters.AddWithValue("@Id", idExistencia);
            command.Parameters.AddWithValue("@ExistenciaActual", existenciaPosterior);
            command.Parameters.AddWithValue("@ExistenciaMinima", existenciaMinima);
            command.Parameters.AddWithValue("@CostoPromedio", costoPromedio.HasValue ? costoPromedio.Value : DBNull.Value);
            command.Parameters.AddWithValue("@FechaActualizacion", ahora);
            await command.ExecuteNonQueryAsync();
        }

        private async Task InsertarMovimientoInventarioAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            Guid idEmpresa,
            Guid idProductoServicio,
            byte tipoMovimiento,
            decimal cantidad,
            decimal existenciaAnterior,
            decimal existenciaPosterior,
            decimal? costoUnitario,
            string referencia,
            string observaciones,
            Guid? idUsuario,
            DateTime fechaMovimiento)
        {
            using SqlCommand command = new SqlCommand(@"
INSERT INTO dbo.ProductosServiciosMovimientosInventario
    (id, idEmpresa, identityKey, idProductoServicio, TipoMovimiento, Cantidad, ExistenciaAnterior, ExistenciaPosterior, CostoUnitario, Referencia, Observaciones, idUsuario, FechaMovimiento)
VALUES
    (@Id, @IdEmpresa, @IdentityKey, @IdProductoServicio, @TipoMovimiento, @Cantidad, @ExistenciaAnterior, @ExistenciaPosterior, @CostoUnitario, @Referencia, @Observaciones, @IdUsuario, @FechaMovimiento)", connection, transaction);

            command.Parameters.AddWithValue("@Id", Guid.NewGuid());
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@IdentityKey", Guid.NewGuid());
            command.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);
            command.Parameters.AddWithValue("@TipoMovimiento", tipoMovimiento);
            command.Parameters.AddWithValue("@Cantidad", cantidad);
            command.Parameters.AddWithValue("@ExistenciaAnterior", existenciaAnterior);
            command.Parameters.AddWithValue("@ExistenciaPosterior", existenciaPosterior);
            command.Parameters.AddWithValue("@CostoUnitario", costoUnitario.HasValue ? costoUnitario.Value : DBNull.Value);
            command.Parameters.AddWithValue("@Referencia", Truncate(referencia, ReferenciaLength));
            command.Parameters.AddWithValue("@Observaciones", Truncate(observaciones, DescripcionLength));
            command.Parameters.AddWithValue("@IdUsuario", idUsuario.HasValue ? idUsuario.Value : DBNull.Value);
            command.Parameters.AddWithValue("@FechaMovimiento", fechaMovimiento);
            await command.ExecuteNonQueryAsync();
        }

        private async Task<string> ValidateCatalogReferencesAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, NormalizedProductoServicioRequest request, bool esNuevo, ProductoServicioSnapshot? existente)
        {
            ProductoServicioCategoriaDto? categoria = await ObtenerCategoriaInternaAsync(connection, transaction, idEmpresa, request.IdCategoria);
            if (categoria == null || (esNuevo && !categoria.Activo))
            {
                return "Selecciona una categoría válida de la empresa activa.";
            }

            if (!CategoriaAplicaATipo(categoria.AplicaA, request.Tipo))
            {
                return request.Tipo == TipoProducto
                    ? "La categoría seleccionada no aplica a productos."
                    : "La categoría seleccionada no aplica a servicios.";
            }

            ProductoServicioUnidadMedidaDto? unidad = await ObtenerUnidadInternaAsync(connection, transaction, idEmpresa, request.IdUnidadMedida);
            if (unidad == null || (esNuevo && !unidad.Activo))
            {
                return "Selecciona una unidad de medida válida de la empresa activa.";
            }

            if (request.Tipo == TipoProducto && request.IdMarca.HasValue)
            {
                ProductoServicioMarcaDto? marca = await ObtenerMarcaInternaAsync(connection, transaction, idEmpresa, request.IdMarca.Value);
                if (marca == null || (esNuevo && !marca.Activo))
                {
                    return "Selecciona una marca válida de la empresa activa.";
                }
            }

            if (request.Tipo == TipoServicio && request.IdMarca.HasValue)
            {
                return "Los servicios no pueden asociarse a una marca.";
            }

            if (!esNuevo && existente != null && existente.IdEmpresa != idEmpresa)
            {
                return "El contexto de empresa no coincide con el registro solicitado.";
            }

            return string.Empty;
        }

        private async Task<ProductoServicioCategoriaDto?> ObtenerCategoriaInternaAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idCategoria)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT id, idEmpresa, identityKey, Codigo, Nombre, ISNULL(Descripcion, '') AS Descripcion, AplicaA, Activo, FechaCreacion, FechaActualizacion, FechaArchivado
FROM dbo.ProductosServiciosCategorias
WHERE idEmpresa = @IdEmpresa AND id = @Id", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@Id", idCategoria);
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            return await reader.ReadAsync() ? MapCategoria(reader) : null;
        }

        private async Task<ProductoServicioMarcaDto?> ObtenerMarcaInternaAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idMarca)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT id, idEmpresa, identityKey, Codigo, Nombre, ISNULL(Descripcion, '') AS Descripcion, Activo, FechaCreacion, FechaActualizacion, FechaArchivado
FROM dbo.ProductosServiciosMarcas
WHERE idEmpresa = @IdEmpresa AND id = @Id", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@Id", idMarca);
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            return await reader.ReadAsync() ? MapMarca(reader) : null;
        }

        private async Task<ProductoServicioUnidadMedidaDto?> ObtenerUnidadInternaAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idUnidadMedida)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT id, idEmpresa, identityKey, Codigo, Nombre, N'' AS Descripcion, Abreviatura, PermiteDecimales, Activo, FechaCreacion, FechaActualizacion, FechaArchivado
FROM dbo.ProductosServiciosUnidadesMedida
WHERE idEmpresa = @IdEmpresa AND id = @Id", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@Id", idUnidadMedida);
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            return await reader.ReadAsync() ? MapUnidad(reader) : null;
        }

        private async Task<bool> ExisteCodigoProductoServicioAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, string codigo, Guid? excludeId)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT COUNT(1)
FROM dbo.ProductosServicios
WHERE idEmpresa = @IdEmpresa AND Codigo = @Codigo AND (@ExcludeId IS NULL OR id <> @ExcludeId)", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@Codigo", codigo.Trim());
            command.Parameters.AddWithValue("@ExcludeId", excludeId.HasValue ? excludeId.Value : DBNull.Value);
            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        private async Task<bool> ExisteCodigoCatalogoAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, string codigo, Guid? excludeId, string tableName)
        {
            using SqlCommand command = new SqlCommand($@"
SELECT COUNT(1)
FROM {tableName}
WHERE idEmpresa = @IdEmpresa AND Codigo = @Codigo AND (@ExcludeId IS NULL OR id <> @ExcludeId)", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@Codigo", codigo.Trim());
            command.Parameters.AddWithValue("@ExcludeId", excludeId.HasValue ? excludeId.Value : DBNull.Value);
            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        private static string ValidateProductoServicioRequest(ProductoServicioGuardarRequest request, Guid idEmpresa)
        {
            if (request.IdEmpresa == Guid.Empty || request.IdEmpresa != idEmpresa)
            {
                return "No fue posible resolver la empresa activa.";
            }

            if (request.Tipo != TipoProducto && request.Tipo != TipoServicio)
            {
                return "Selecciona un tipo válido: Producto o Servicio.";
            }

            if (string.IsNullOrWhiteSpace(request.Codigo) || request.Codigo.Trim().Length > CodigoLength)
            {
                return $"Captura un código válido de hasta {CodigoLength} caracteres.";
            }

            if (string.IsNullOrWhiteSpace(request.Nombre) || request.Nombre.Trim().Length > NombreLength)
            {
                return $"Captura un nombre válido de hasta {NombreLength} caracteres.";
            }

            if (request.IdCategoria == Guid.Empty)
            {
                return "Selecciona una categoría.";
            }

            if (request.IdUnidadMedida == Guid.Empty)
            {
                return "Selecciona una unidad de medida.";
            }

            if ((request.Tag ?? string.Empty).Trim().Length > TagLength)
            {
                return $"La etiqueta no puede exceder {TagLength} caracteres.";
            }

            if ((request.Descripcion ?? string.Empty).Trim().Length > DescripcionLength)
            {
                return $"La descripción no puede exceder {DescripcionLength} caracteres.";
            }

            if (request.PrecioPublico < 0)
            {
                return "El precio público no puede ser negativo.";
            }

            if (request.Costo.HasValue && request.Costo.Value < 0)
            {
                return "El costo no puede ser negativo.";
            }

            if (request.ExistenciaInicial.HasValue && request.ExistenciaInicial.Value < 0)
            {
                return "La existencia inicial no puede ser negativa.";
            }

            if (request.ExistenciaMinima.HasValue && request.ExistenciaMinima.Value < 0)
            {
                return "La existencia mínima no puede ser negativa.";
            }

            if (request.Tipo == TipoServicio && request.IdMarca.HasValue && request.IdMarca.Value != Guid.Empty)
            {
                return "Los servicios no pueden asociarse a una marca.";
            }

            if (request.Tipo == TipoProducto && request.CausaInventario == false && request.PermiteVentaSinExistencia)
            {
                return "La venta sin existencia solo aplica a productos inventariables.";
            }

            if ((request.Tipo == TipoServicio || !request.CausaInventario) &&
                HasContradictoryInventoryValues(request.ExistenciaInicial, request.ExistenciaMinima))
            {
                return "No envíes existencias para servicios o productos sin inventario.";
            }

            return string.Empty;
        }

        private static string ValidateCategoriaRequest(ProductoServicioCategoriaGuardarRequest request, Guid idEmpresa)
        {
            string baseValidation = ValidateCatalogoBasico(idEmpresa, request.IdEmpresa, request.Codigo, request.Nombre, request.Descripcion);
            if (!string.IsNullOrWhiteSpace(baseValidation))
            {
                return baseValidation;
            }

            if (request.AplicaA != AplicaATodos && request.AplicaA != AplicaAProductos && request.AplicaA != AplicaAServicios)
            {
                return "Selecciona un valor válido para 'Aplica a'.";
            }

            return string.Empty;
        }

        private static string ValidateMarcaRequest(ProductoServicioMarcaGuardarRequest request, Guid idEmpresa)
        {
            return ValidateCatalogoBasico(idEmpresa, request.IdEmpresa, request.Codigo, request.Nombre, request.Descripcion);
        }

        private static string ValidateUnidadRequest(ProductoServicioUnidadMedidaGuardarRequest request, Guid idEmpresa)
        {
            if (request.IdEmpresa == Guid.Empty || request.IdEmpresa != idEmpresa)
            {
                return "No fue posible resolver la empresa activa.";
            }

            if (string.IsNullOrWhiteSpace(request.Codigo) || request.Codigo.Trim().Length > UnidadCodigoLength)
            {
                return $"Captura un código válido de hasta {UnidadCodigoLength} caracteres.";
            }

            if (string.IsNullOrWhiteSpace(request.Nombre) || request.Nombre.Trim().Length > UnidadNombreLength)
            {
                return $"Captura un nombre válido de hasta {UnidadNombreLength} caracteres.";
            }

            if ((request.Descripcion ?? string.Empty).Trim().Length > DescripcionCatalogoLength)
            {
                return $"La descripción no puede exceder {DescripcionCatalogoLength} caracteres.";
            }

            if (string.IsNullOrWhiteSpace(request.Abreviatura) || request.Abreviatura.Trim().Length > AbreviaturaLength)
            {
                return $"Captura una abreviatura válida de hasta {AbreviaturaLength} caracteres.";
            }

            return string.Empty;
        }

        private static string ValidateCatalogoBasico(Guid idEmpresaEsperado, Guid idEmpresaRequest, string codigo, string nombre, string descripcion)
        {
            if (idEmpresaRequest == Guid.Empty || idEmpresaEsperado != idEmpresaRequest)
            {
                return "No fue posible resolver la empresa activa.";
            }

            if (string.IsNullOrWhiteSpace(codigo) || codigo.Trim().Length > CodigoLength)
            {
                return $"Captura un código válido de hasta {CodigoLength} caracteres.";
            }

            if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length > NombreLength)
            {
                return $"Captura un nombre válido de hasta {NombreLength} caracteres.";
            }

            if ((descripcion ?? string.Empty).Trim().Length > DescripcionCatalogoLength)
            {
                return $"La descripción no puede exceder {DescripcionCatalogoLength} caracteres.";
            }

            return string.Empty;
        }

        private static string ValidateMovimientoRequest(ProductoServicioMovimientoGuardarRequest request, Guid idEmpresa)
        {
            if (request.IdEmpresa == Guid.Empty || request.IdEmpresa != idEmpresa)
            {
                return "No fue posible resolver la empresa activa.";
            }

            if (request.IdProductoServicio == Guid.Empty)
            {
                return "Selecciona un producto válido para inventario.";
            }

            if (request.Cantidad <= 0)
            {
                return "La cantidad del movimiento debe ser mayor que cero.";
            }

            if (request.CostoUnitario.HasValue && request.CostoUnitario.Value < 0)
            {
                return "El costo unitario no puede ser negativo.";
            }

            if ((request.Referencia ?? string.Empty).Trim().Length > ReferenciaLength)
            {
                return $"La referencia no puede exceder {ReferenciaLength} caracteres.";
            }

            if ((request.Observaciones ?? string.Empty).Trim().Length > DescripcionLength)
            {
                return $"Las observaciones no pueden exceder {DescripcionLength} caracteres.";
            }

            return string.Empty;
        }

        private static string ValidateInventoryTransition(ProductoServicioSnapshot existente, NormalizedProductoServicioRequest request, ProductoServicioExistenciaDto? existenciaActual, int movimientosHistoricos)
        {
            bool targetInventariable = request.Tipo == TipoProducto && request.CausaInventario;
            if (existente.CausaInventario && !targetInventariable)
            {
                bool tieneExistenciaDistintaDeCero = existenciaActual != null && existenciaActual.ExistenciaActual != 0m;
                if (movimientosHistoricos > 0 || tieneExistenciaDistintaDeCero)
                {
                    return "No es posible convertir a no inventariable un producto con historial o existencia distinta de cero.";
                }
            }

            return string.Empty;
        }

        private static string ValidateProductoInventariable(ProductoServicioSnapshot producto)
        {
            if (producto.Tipo != TipoProducto)
            {
                return "Los servicios no pueden generar movimientos de inventario.";
            }

            if (!producto.CausaInventario)
            {
                return "El producto seleccionado no está configurado para inventario.";
            }

            return string.Empty;
        }

        private static string ValidateMovimientoAgainstExistencia(ProductoServicioSnapshot producto, ProductoServicioExistenciaDto existencia, ProductoServicioMovimientoGuardarRequest request, byte tipoMovimiento)
        {
            decimal existenciaPosterior = CalcularExistenciaPosterior(existencia.ExistenciaActual, request.Cantidad, tipoMovimiento);
            if (existenciaPosterior < 0 && !producto.PermiteVentaSinExistencia)
            {
                return "La operación dejaría la existencia en negativo y el producto no permite venta sin existencia.";
            }

            return string.Empty;
        }

        private static string ValidateImagenTemporalUpload(IFormFile archivo)
        {
            if (archivo.Length > ImagenMaxBytes)
            {
                return "La imagen excede el tamaño máximo permitido de 10 MB.";
            }

            string extension = (Path.GetExtension(archivo.FileName) ?? string.Empty).Trim().ToLowerInvariant();
            if (!ExtensionesImagenPermitidas.Contains(extension))
            {
                return "La imagen debe estar en formato JPG, PNG o WEBP.";
            }

            if (!MimeTypesImagenPermitidos.Contains((archivo.ContentType ?? string.Empty).Trim().ToLowerInvariant()))
            {
                return "El tipo MIME de la imagen no está soportado.";
            }

            return string.Empty;
        }

        private static string ValidateImageSignature(string fileName, string contentType, byte[] fileBytes)
        {
            if (fileBytes.Length < 12)
            {
                return "La imagen cargada no contiene una firma válida.";
            }

            string extension = (Path.GetExtension(fileName) ?? string.Empty).Trim().ToLowerInvariant();
            bool isJpeg = fileBytes[0] == 0xFF && fileBytes[1] == 0xD8;
            bool isPng = fileBytes[0] == 0x89 && fileBytes[1] == 0x50 && fileBytes[2] == 0x4E && fileBytes[3] == 0x47;
            bool isWebp = Encoding.ASCII.GetString(fileBytes, 0, 4) == "RIFF" && Encoding.ASCII.GetString(fileBytes, 8, 4) == "WEBP";

            if (extension is ".jpg" or ".jpeg")
            {
                return isJpeg ? string.Empty : "La firma del archivo no corresponde a una imagen JPG.";
            }

            if (extension == ".png")
            {
                return isPng ? string.Empty : "La firma del archivo no corresponde a una imagen PNG.";
            }

            if (extension == ".webp")
            {
                return isWebp ? string.Empty : "La firma del archivo no corresponde a una imagen WEBP.";
            }

            return $"La extensión '{extension}' no está soportada para imágenes.";
        }

        private async Task<PreparedImageOperation> PrepareImageOperationAsync(RequestContext context, Guid productoId, NormalizedProductoServicioRequest request)
        {
            if (request.EliminarImagenPrincipal)
            {
                return PreparedImageOperation.ForRemove();
            }

            if (request.ImagenPrincipal == null || string.IsNullOrWhiteSpace(request.ImagenPrincipal.TemporalToken))
            {
                return PreparedImageOperation.None();
            }

            TemporalImageTokenPayload temporal = TryParseTemporalToken(request.ImagenPrincipal.TemporalToken)
                ?? throw new InvalidOperationException("La referencia temporal de la imagen principal es inválida o expiró.");

            if (!FolderBelongsToEmpresa(temporal.FolderName, context.EmpresaStorageKey))
            {
                throw new InvalidOperationException("La imagen temporal no pertenece a la empresa activa.");
            }

            UploadedImagePayload uploaded = await MoveTemporalImageToFinalAsync(context.EmpresaStorageKey, productoId, temporal);
            return PreparedImageOperation.ForNewImage(uploaded, new FirebaseCleanupItem
            {
                FolderName = temporal.FolderName,
                StoredName = temporal.NombreAlmacenado
            });
        }

        private async Task FinalizeImageOperationAfterCommitAsync(PreparedImageOperation preparedImage, FirebaseCleanupItem? previousImageCleanup)
        {
            List<FirebaseCleanupItem> cleanupItems = new List<FirebaseCleanupItem>();
            if (preparedImage.TemporalCleanup != null)
            {
                cleanupItems.Add(preparedImage.TemporalCleanup);
            }

            if (previousImageCleanup != null)
            {
                cleanupItems.Add(previousImageCleanup);
            }

            await CleanupUploadedFirebaseFilesAsync(cleanupItems);
        }

        private async Task CompensatePreparedImageAsync(PreparedImageOperation preparedImage)
        {
            if (preparedImage.NewImageCleanup == null)
            {
                return;
            }

            try
            {
                await CleanupUploadedFirebaseFilesAsync(new List<FirebaseCleanupItem> { preparedImage.NewImageCleanup });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fallo la compensacion de imagen final para productos y servicios.");
            }
        }

        private static ResolvedImageMutation ResolveImageMutation(ProductoServicioSnapshot? existente, PreparedImageOperation preparedImage)
        {
            string imagenUrl = existente?.ImagenUrl ?? string.Empty;
            string imagenNombre = existente?.ImagenNombre ?? string.Empty;
            FirebaseCleanupItem? previousCleanup = null;

            if (preparedImage.Mode == ImageOperationMode.Remove)
            {
                if (!string.IsNullOrWhiteSpace(imagenUrl))
                {
                    previousCleanup = TryBuildCleanupItemFromUrl(imagenUrl);
                }

                return new ResolvedImageMutation
                {
                    ImagenUrl = string.Empty,
                    ImagenNombre = string.Empty,
                    PreviousImageCleanup = previousCleanup
                };
            }

            if (preparedImage.Mode == ImageOperationMode.NewImage && preparedImage.UploadedImage != null)
            {
                if (!string.IsNullOrWhiteSpace(imagenUrl))
                {
                    previousCleanup = TryBuildCleanupItemFromUrl(imagenUrl);
                }

                return new ResolvedImageMutation
                {
                    ImagenUrl = preparedImage.UploadedImage.UrlFirebase,
                    ImagenNombre = preparedImage.UploadedImage.NombreOriginal,
                    PreviousImageCleanup = previousCleanup
                };
            }

            return new ResolvedImageMutation
            {
                ImagenUrl = imagenUrl,
                ImagenNombre = imagenNombre
            };
        }

        private static NormalizedProductoServicioRequest NormalizeRequest(ProductoServicioGuardarRequest request)
        {
            NormalizedProductoServicioRequest normalized = new NormalizedProductoServicioRequest
            {
                Id = request.Id,
                Tipo = request.Tipo,
                Codigo = request.Codigo.Trim(),
                Tag = (request.Tag ?? string.Empty).Trim(),
                Nombre = request.Nombre.Trim(),
                Descripcion = (request.Descripcion ?? string.Empty).Trim(),
                IdCategoria = request.IdCategoria,
                IdMarca = request.IdMarca.HasValue && request.IdMarca.Value != Guid.Empty ? request.IdMarca : null,
                IdUnidadMedida = request.IdUnidadMedida,
                Costo = request.Costo,
                PrecioPublico = request.PrecioPublico,
                CausaInventario = request.CausaInventario,
                PermiteVentaSinExistencia = request.PermiteVentaSinExistencia,
                ExistenciaInicial = NormalizeInventoryInput(request.ExistenciaInicial),
                ExistenciaMinima = NormalizeInventoryInput(request.ExistenciaMinima),
                ImagenPrincipal = request.ImagenPrincipal,
                EliminarImagenPrincipal = request.EliminarImagenPrincipal
            };

            if (normalized.Tipo == TipoServicio)
            {
                normalized.IdMarca = null;
                normalized.CausaInventario = false;
                normalized.PermiteVentaSinExistencia = false;
                normalized.ExistenciaInicial = null;
                normalized.ExistenciaMinima = null;
            }
            else if (!normalized.CausaInventario)
            {
                normalized.PermiteVentaSinExistencia = false;
                normalized.ExistenciaInicial = null;
                normalized.ExistenciaMinima = null;
            }

            return normalized;
        }

        private static void AddProductoServicioParameters(SqlCommand command, Guid productoId, Guid idEmpresa, NormalizedProductoServicioRequest request, ResolvedImageMutation imageMutation, DateTime ahora, bool includeIdentityKey)
        {
            command.Parameters.AddWithValue("@Id", productoId);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            if (includeIdentityKey)
            {
                command.Parameters.AddWithValue("@IdentityKey", Guid.NewGuid());
                command.Parameters.AddWithValue("@FechaCreacion", ahora);
            }

            command.Parameters.AddWithValue("@Tipo", request.Tipo);
            command.Parameters.AddWithValue("@Codigo", request.Codigo);
            command.Parameters.AddWithValue("@Tag", string.IsNullOrWhiteSpace(request.Tag) ? DBNull.Value : request.Tag);
            command.Parameters.AddWithValue("@Nombre", request.Nombre);
            command.Parameters.AddWithValue("@Descripcion", string.IsNullOrWhiteSpace(request.Descripcion) ? DBNull.Value : request.Descripcion);
            command.Parameters.AddWithValue("@IdCategoria", request.IdCategoria);
            command.Parameters.AddWithValue("@IdMarca", request.IdMarca.HasValue ? request.IdMarca.Value : DBNull.Value);
            command.Parameters.AddWithValue("@IdUnidadMedida", request.IdUnidadMedida);
            command.Parameters.AddWithValue("@Costo", request.Costo.HasValue ? request.Costo.Value : DBNull.Value);
            command.Parameters.AddWithValue("@PrecioPublico", request.PrecioPublico);
            command.Parameters.AddWithValue("@CausaInventario", request.CausaInventario);
            command.Parameters.AddWithValue("@PermiteVentaSinExistencia", request.PermiteVentaSinExistencia);
            command.Parameters.AddWithValue("@ImagenUrl", string.IsNullOrWhiteSpace(imageMutation.ImagenUrl) ? DBNull.Value : imageMutation.ImagenUrl);
            command.Parameters.AddWithValue("@ImagenNombre", string.IsNullOrWhiteSpace(imageMutation.ImagenNombre) ? DBNull.Value : imageMutation.ImagenNombre);
            command.Parameters.AddWithValue("@FechaActualizacion", ahora);
        }

        private static void AddCatalogoParameters(SqlCommand command, Guid id, Guid idEmpresa, string codigo, string nombre, string descripcion, DateTime ahora, string? abreviatura, bool? permiteDecimales, byte? aplicaA)
        {
            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@IdentityKey", Guid.NewGuid());
            command.Parameters.AddWithValue("@Codigo", codigo.Trim());
            command.Parameters.AddWithValue("@Nombre", nombre.Trim());
            command.Parameters.AddWithValue("@Descripcion", string.IsNullOrWhiteSpace(descripcion) ? DBNull.Value : descripcion.Trim());
            command.Parameters.AddWithValue("@FechaCreacion", ahora);
            command.Parameters.AddWithValue("@FechaActualizacion", ahora);

            if (abreviatura != null)
            {
                command.Parameters.AddWithValue("@Abreviatura", abreviatura.Trim());
            }

            if (permiteDecimales.HasValue)
            {
                command.Parameters.AddWithValue("@PermiteDecimales", permiteDecimales.Value);
            }

            if (aplicaA.HasValue)
            {
                command.Parameters.AddWithValue("@AplicaA", aplicaA.Value);
            }
        }

        private async Task<UploadedImagePayload> UploadImageToFirebaseAsync(string folderName, string storedName, byte[] fileBytes, string nombreOriginal, string mimeType, long pesoBytes)
        {
            string extension = NormalizeExtension(Path.GetExtension(nombreOriginal), mimeType);
            var config = new FirebaseAuthConfig
            {
                ApiKey = _configuration.GetValue<string>("fireBdata:fireApiKey"),
                AuthDomain = _configuration.GetValue<string>("fireBdata:fireAuthDomain"),
                Providers = new FirebaseAuthProvider[] { new EmailProvider() }
            };

            var authClient = new FirebaseAuthClient(config);
            var userCredential = await authClient.SignInWithEmailAndPasswordAsync(
                _configuration.GetValue<string>("fireBdata:fireUser"),
                _configuration.GetValue<string>("fireBdata:fireClave"));
            string token = await userCredential.User.GetIdTokenAsync();

            using MemoryStream stream = new MemoryStream(fileBytes);
            var storage = new FirebaseStorage(
                _configuration.GetValue<string>("fireBdata:fireStorage"),
                new FirebaseStorageOptions
                {
                    AuthTokenAsyncFactory = () => Task.FromResult(token),
                    ThrowOnCancel = true
                });

            string downloadUrl = await storage.Child(folderName).Child(storedName).PutAsync(stream);
            authClient.SignOut();

            return new UploadedImagePayload
            {
                FolderName = folderName,
                NombreOriginal = NormalizeArchivoText(nombreOriginal, NombreArchivoLength, storedName),
                NombreAlmacenado = storedName,
                Extension = extension,
                MimeType = NormalizeArchivoText(mimeType, MimeTypeLength, "image/jpeg"),
                UrlFirebase = NormalizeArchivoText(downloadUrl, UrlLength, string.Empty),
                PesoBytes = pesoBytes > 0 ? pesoBytes : fileBytes.LongLength
            };
        }

        private async Task<UploadedImagePayload> MoveTemporalImageToFinalAsync(string empresaStorageKey, Guid productoId, TemporalImageTokenPayload temporal)
        {
            byte[] fileBytes = await DownloadFirebaseFileAsync(temporal.FolderName, temporal.NombreAlmacenado);
            return await UploadImageToFirebaseAsync(
                BuildFinalFolderName(empresaStorageKey, productoId),
                BuildStoredFileName(temporal.NombreOriginal, temporal.MimeType),
                fileBytes,
                temporal.NombreOriginal,
                temporal.MimeType,
                temporal.PesoBytes);
        }

        private async Task<byte[]> DownloadFirebaseFileAsync(string folderName, string storedName)
        {
            var config = new FirebaseAuthConfig
            {
                ApiKey = _configuration.GetValue<string>("fireBdata:fireApiKey"),
                AuthDomain = _configuration.GetValue<string>("fireBdata:fireAuthDomain"),
                Providers = new FirebaseAuthProvider[] { new EmailProvider() }
            };

            var authClient = new FirebaseAuthClient(config);
            var userCredential = await authClient.SignInWithEmailAndPasswordAsync(
                _configuration.GetValue<string>("fireBdata:fireUser"),
                _configuration.GetValue<string>("fireBdata:fireClave"));
            string token = await userCredential.User.GetIdTokenAsync();

            var storage = new FirebaseStorage(
                _configuration.GetValue<string>("fireBdata:fireStorage"),
                new FirebaseStorageOptions
                {
                    AuthTokenAsyncFactory = () => Task.FromResult(token),
                    ThrowOnCancel = true
                });

            string url = await storage.Child(folderName).Child(storedName).GetDownloadUrlAsync();
            using HttpClient httpClient = new HttpClient();
            byte[] fileBytes = await httpClient.GetByteArrayAsync(url);
            authClient.SignOut();
            return fileBytes;
        }

        private async Task CleanupUploadedFirebaseFilesAsync(List<FirebaseCleanupItem> items)
        {
            List<FirebaseCleanupItem> filesToDelete = items
                .Where(item => !string.IsNullOrWhiteSpace(item.FolderName) && !string.IsNullOrWhiteSpace(item.StoredName))
                .GroupBy(item => $"{item.FolderName}/{item.StoredName}")
                .Select(group => group.First())
                .ToList();

            if (filesToDelete.Count == 0)
            {
                return;
            }

            try
            {
                var config = new FirebaseAuthConfig
                {
                    ApiKey = _configuration.GetValue<string>("fireBdata:fireApiKey"),
                    AuthDomain = _configuration.GetValue<string>("fireBdata:fireAuthDomain"),
                    Providers = new FirebaseAuthProvider[] { new EmailProvider() }
                };

                var authClient = new FirebaseAuthClient(config);
                var userCredential = await authClient.SignInWithEmailAndPasswordAsync(
                    _configuration.GetValue<string>("fireBdata:fireUser"),
                    _configuration.GetValue<string>("fireBdata:fireClave"));
                string token = await userCredential.User.GetIdTokenAsync();

                var storage = new FirebaseStorage(
                    _configuration.GetValue<string>("fireBdata:fireStorage"),
                    new FirebaseStorageOptions
                    {
                        AuthTokenAsyncFactory = () => Task.FromResult(token),
                        ThrowOnCancel = true
                    });

                foreach (FirebaseCleanupItem item in filesToDelete)
                {
                    await storage.Child(item.FolderName).Child(item.StoredName).DeleteAsync();
                }

                authClient.SignOut();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fallo la limpieza de archivos en Firebase para productos y servicios.");
            }
        }

        private static string BuildTemporalFolderName(string empresaStorageKey)
        {
            return $"{empresaStorageKey}/ProductosServicios/Temporal/Imagen";
        }

        private static string BuildFinalFolderName(string empresaStorageKey, Guid productoId)
        {
            return $"{empresaStorageKey}/ProductosServicios/{productoId:N}/Imagen";
        }

        private static string BuildStoredFileName(string fileName, string mimeType)
        {
            string extension = NormalizeExtension(Path.GetExtension(fileName), mimeType);
            return $"{Guid.NewGuid():N}{extension}";
        }

        private static string NormalizeExtension(string? extension, string mimeType)
        {
            string normalized = (extension ?? string.Empty).Trim().ToLowerInvariant();
            if (ExtensionesImagenPermitidas.Contains(normalized))
            {
                return normalized;
            }

            return (mimeType ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "image/png" => ".png",
                "image/webp" => ".webp",
                _ => ".jpg"
            };
        }

        private static string NormalizeArchivoText(string value, int maxLength, string fallback)
        {
            string normalized = (value ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                normalized = fallback;
            }

            return normalized.Length > maxLength ? normalized[..maxLength] : normalized;
        }

        private static string CreateTemporalToken(TemporalImageTokenPayload payload)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)));
        }

        private static TemporalImageTokenPayload? TryParseTemporalToken(string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    return null;
                }

                byte[] bytes = Convert.FromBase64String(token);
                TemporalImageTokenPayload? payload = JsonSerializer.Deserialize<TemporalImageTokenPayload>(Encoding.UTF8.GetString(bytes));
                if (payload == null || payload.ExpiraUtc < DateTime.UtcNow)
                {
                    return null;
                }

                return payload;
            }
            catch
            {
                return null;
            }
        }

        private static FirebaseCleanupItem? TryBuildCleanupItemFromUrl(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl) || !Uri.TryCreate(imageUrl, UriKind.Absolute, out Uri? uri))
            {
                return null;
            }

            string path = Uri.UnescapeDataString(uri.AbsolutePath);
            int bucketMarker = path.IndexOf("/o/", StringComparison.OrdinalIgnoreCase);
            if (bucketMarker < 0)
            {
                return null;
            }

            string filePath = path[(bucketMarker + 3)..];
            int slashIndex = filePath.LastIndexOf('/');
            if (slashIndex <= 0 || slashIndex >= filePath.Length - 1)
            {
                return null;
            }

            return new FirebaseCleanupItem
            {
                FolderName = filePath[..slashIndex],
                StoredName = filePath[(slashIndex + 1)..]
            };
        }

        private static bool FolderBelongsToEmpresa(string folderName, string empresaStorageKey)
        {
            return !string.IsNullOrWhiteSpace(folderName) &&
                   folderName.StartsWith($"{empresaStorageKey}/", StringComparison.OrdinalIgnoreCase);
        }

        private static async Task<byte[]> ReadFileBytesAsync(IFormFile archivo)
        {
            using MemoryStream stream = new MemoryStream();
            await archivo.CopyToAsync(stream);
            return stream.ToArray();
        }

        private bool TryResolveRequestContext(Guid? clientEmpresaId, string? clientEmpresaKey, out RequestContext context, out IActionResult? error)
        {
            context = null!;
            error = null;

            Guid? effectiveEmpresaId = TryResolveEmpresaId(out string? proxyEmpresaKey);
            if (!effectiveEmpresaId.HasValue || effectiveEmpresaId.Value == Guid.Empty)
            {
                error = Unauthorized(new ProductoServicioOperacionResponse { Mensaje = "No fue posible resolver la empresa activa." });
                return false;
            }

            if (clientEmpresaId.HasValue && clientEmpresaId.Value != Guid.Empty && clientEmpresaId.Value != effectiveEmpresaId.Value)
            {
                error = BadRequest(new ProductoServicioOperacionResponse { Mensaje = "La empresa solicitada no coincide con la sesión activa." });
                return false;
            }

            string empresaStorageKey = TryResolveEmpresaStorageKey(effectiveEmpresaId.Value, proxyEmpresaKey);
            if (!string.IsNullOrWhiteSpace(clientEmpresaKey) &&
                !string.Equals(clientEmpresaKey.Trim(), empresaStorageKey, StringComparison.OrdinalIgnoreCase))
            {
                error = BadRequest(new ProductoServicioOperacionResponse { Mensaje = "La empresa solicitada no coincide con la sesión activa." });
                return false;
            }

            context = new RequestContext
            {
                IdEmpresa = effectiveEmpresaId.Value,
                EmpresaStorageKey = empresaStorageKey
            };
            return true;
        }

        private Guid? TryResolveEmpresaId(out string? proxyEmpresaKey)
        {
            proxyEmpresaKey = null;

            foreach (string claimKey in EmpresaClaimKeys)
            {
                string? value = User.FindFirstValue(claimKey);
                if (Guid.TryParse(value, out Guid parsed) && parsed != Guid.Empty)
                {
                    return parsed;
                }
            }

            if (TryResolveSignedProxyContext(out SignedProxyContext? proxyContext) && proxyContext != null)
            {
                proxyEmpresaKey = proxyContext.EmpresaStorageKey;
                return proxyContext.IdEmpresa;
            }

            return null;
        }

        private string TryResolveEmpresaStorageKey(Guid empresaId, string? proxyEmpresaKey = null)
        {
            foreach (string claimKey in EmpresaNombreClaimKeys)
            {
                string? value = User.FindFirstValue(claimKey);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim().ToUpperInvariant();
                }
            }

            if (!string.IsNullOrWhiteSpace(proxyEmpresaKey))
            {
                return proxyEmpresaKey.Trim().ToUpperInvariant();
            }

            return empresaId.ToString("N").ToUpperInvariant();
        }

        private Guid? TryResolveUsuarioId()
        {
            foreach (string claimKey in UsuarioClaimKeys)
            {
                string? value = User.FindFirstValue(claimKey);
                if (Guid.TryParse(value, out Guid parsed) && parsed != Guid.Empty)
                {
                    return parsed;
                }
            }

            if (TryResolveSignedProxyContext(out SignedProxyContext? proxyContext) &&
                proxyContext != null &&
                proxyContext.UsuarioId.HasValue)
            {
                return proxyContext.UsuarioId.Value;
            }

            return null;
        }

        private bool TryResolveSignedProxyContext(out SignedProxyContext? context)
        {
            if (HttpContext.Items.TryGetValue(ProxyContextItemKey, out object? cached))
            {
                context = cached as SignedProxyContext;
                return context != null;
            }

            context = null;

            if (!Request.Headers.TryGetValue(ProxyEmpresaIdHeader, out var empresaIdHeader) ||
                !Request.Headers.TryGetValue(ProxyEmpresaKeyHeader, out var empresaKeyHeader) ||
                !Request.Headers.TryGetValue(ProxyTimestampHeader, out var timestampHeader) ||
                !Request.Headers.TryGetValue(ProxySignatureHeader, out var signatureHeader))
            {
                return false;
            }

            string secret = _configuration["fireBdata:fireClave"] ?? string.Empty;
            if (string.IsNullOrWhiteSpace(secret))
            {
                _logger.LogWarning("ProductosServicios proxy headers recibidos sin secreto compartido configurado.");
                return false;
            }

            string empresaIdRaw = empresaIdHeader.ToString().Trim();
            string empresaKeyRaw = empresaKeyHeader.ToString().Trim();
            string usuarioIdRaw = Request.Headers.TryGetValue(ProxyUsuarioIdHeader, out var usuarioIdHeader)
                ? usuarioIdHeader.ToString().Trim()
                : string.Empty;
            string timestampRaw = timestampHeader.ToString().Trim();
            string signatureRaw = signatureHeader.ToString().Trim();

            if (!Guid.TryParse(empresaIdRaw, out Guid empresaId) || empresaId == Guid.Empty)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(empresaKeyRaw) ||
                !DateTimeOffset.TryParseExact(timestampRaw, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset timestamp))
            {
                return false;
            }

            TimeSpan age = DateTimeOffset.UtcNow - timestamp.ToUniversalTime();
            if (age.Duration() > ProxyHeaderTolerance)
            {
                _logger.LogWarning("ProductosServicios proxy headers expirados o fuera de tolerancia para empresa {EmpresaId}.", empresaId);
                return false;
            }

            string payload = BuildProxySignaturePayload(empresaIdRaw, empresaKeyRaw, usuarioIdRaw, timestampRaw);
            string expectedSignature = ComputeProxySignature(secret, payload);

            if (!SignaturesMatch(expectedSignature, signatureRaw))
            {
                _logger.LogWarning("ProductosServicios proxy headers con firma invalida para empresa {EmpresaId}.", empresaId);
                return false;
            }

            context = new SignedProxyContext
            {
                IdEmpresa = empresaId,
                EmpresaStorageKey = empresaKeyRaw.ToUpperInvariant(),
                UsuarioId = Guid.TryParse(usuarioIdRaw, out Guid usuarioId) && usuarioId != Guid.Empty ? usuarioId : null
            };

            HttpContext.Items[ProxyContextItemKey] = context;
            return true;
        }

        private static string BuildProxySignaturePayload(string empresaId, string empresaKey, string usuarioId, string timestamp)
        {
            return string.Join('\n', empresaId.Trim(), empresaKey.Trim().ToUpperInvariant(), usuarioId.Trim(), timestamp.Trim());
        }

        private static string ComputeProxySignature(string secret, string payload)
        {
            using HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToBase64String(hash);
        }

        private static bool SignaturesMatch(string expectedSignature, string providedSignature)
        {
            byte[] expectedBytes = Encoding.UTF8.GetBytes(expectedSignature);
            byte[] providedBytes = Encoding.UTF8.GetBytes(providedSignature);
            return CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
        }

        private SqlConnection CreateConnection()
        {
            return _connectionFactory.CreateConnection();
        }

        private IActionResult HandleException(Exception ex, string operation, string safeMessage)
        {
            _logger.LogError(ex, "Error en ProductosServicios durante {Operation}.", operation);
            return StatusCode(500, new ProductoServicioOperacionResponse { Mensaje = safeMessage });
        }

        private static void AppendBusquedaCatalogo(StringBuilder query, SqlCommand command, string busqueda)
        {
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                query.Append(" AND (Codigo LIKE @Busqueda OR Nombre LIKE @Busqueda OR ISNULL(Descripcion, '') LIKE @Busqueda)");
                command.Parameters.AddWithValue("@Busqueda", $"%{busqueda.Trim()}%");
            }
        }

        private static void AppendGuidFilter(StringBuilder query, SqlCommand command, string columnName, string parameterName, Guid? value)
        {
            if (value.HasValue && value.Value != Guid.Empty)
            {
                query.Append($" AND {columnName} = {parameterName}");
                command.Parameters.AddWithValue(parameterName, value.Value);
            }
        }

        private static void AppendTinyIntFilter(StringBuilder query, SqlCommand command, string columnName, string parameterName, byte? value)
        {
            if (value.HasValue)
            {
                query.Append($" AND {columnName} = {parameterName}");
                command.Parameters.AddWithValue(parameterName, value.Value);
            }
        }

        private static void AppendBitFilter(StringBuilder query, SqlCommand command, string columnName, string parameterName, bool? value)
        {
            if (value.HasValue)
            {
                query.Append($" AND {columnName} = {parameterName}");
                command.Parameters.AddWithValue(parameterName, value.Value);
            }
        }

        private static void AppendEstatusFilter(StringBuilder query, string columnName, string estatus)
        {
            switch ((estatus ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "activos":
                    query.Append($" AND {columnName} = 1");
                    break;
                case "inactivos":
                    query.Append($" AND {columnName} = 0");
                    break;
            }
        }

        private static bool CategoriaAplicaATipo(byte aplicaA, byte tipo)
        {
            return aplicaA == AplicaATodos ||
                   (aplicaA == AplicaAProductos && tipo == TipoProducto) ||
                   (aplicaA == AplicaAServicios && tipo == TipoServicio);
        }

        private static bool HasContradictoryInventoryValues(decimal? existenciaInicial, decimal? existenciaMinima)
        {
            return HasMeaningfulInventoryValue(existenciaInicial) || HasMeaningfulInventoryValue(existenciaMinima);
        }

        private static bool HasMeaningfulInventoryValue(decimal? value)
        {
            return value.HasValue && value.Value != 0m;
        }

        private static decimal? NormalizeInventoryInput(decimal? value)
        {
            return value.HasValue && value.Value == 0m ? null : value;
        }

        private static decimal CalcularExistenciaPosterior(decimal existenciaActual, decimal cantidad, byte tipoMovimiento)
        {
            return tipoMovimiento switch
            {
                MovimientoEntrada => existenciaActual + cantidad,
                MovimientoAjustePositivo => existenciaActual + cantidad,
                MovimientoSalida => existenciaActual - cantidad,
                MovimientoAjusteNegativo => existenciaActual - cantidad,
                MovimientoExistenciaInicial => cantidad,
                _ => existenciaActual
            };
        }

        private static string GetMovimientoNombre(byte tipoMovimiento)
        {
            return tipoMovimiento switch
            {
                MovimientoExistenciaInicial => "Existencia inicial",
                MovimientoEntrada => "Entrada",
                MovimientoSalida => "Salida",
                MovimientoAjustePositivo => "Ajuste positivo",
                MovimientoAjusteNegativo => "Ajuste negativo",
                _ => "Movimiento"
            };
        }

        private static string GetAplicaANombre(byte aplicaA)
        {
            return aplicaA switch
            {
                AplicaAProductos => "Productos",
                AplicaAServicios => "Servicios",
                _ => "Todos"
            };
        }

        private static string Truncate(string value, int maxLength)
        {
            string normalized = (value ?? string.Empty).Trim();
            return normalized.Length > maxLength ? normalized[..maxLength] : normalized;
        }

        private static ProductoServicioListadoDto MapProductoServicioListado(SqlDataReader reader)
        {
            return new ProductoServicioListadoDto
            {
                Id = ReadGuid(reader, "id"),
                IdEmpresa = ReadGuid(reader, "idEmpresa"),
                IdentityKey = ReadGuid(reader, "identityKey"),
                Tipo = ReadByte(reader, "Tipo"),
                TipoNombre = ReadString(reader, "TipoNombre"),
                Codigo = ReadString(reader, "Codigo"),
                Tag = ReadString(reader, "Tag"),
                Nombre = ReadString(reader, "Nombre"),
                Descripcion = ReadString(reader, "Descripcion"),
                IdCategoria = ReadGuid(reader, "idCategoria"),
                Categoria = ReadString(reader, "Categoria"),
                CategoriaAplicaA = ReadByte(reader, "CategoriaAplicaA"),
                IdMarca = ReadNullableGuid(reader, "idMarca"),
                Marca = ReadString(reader, "Marca"),
                IdUnidadMedida = ReadGuid(reader, "idUnidadMedida"),
                UnidadMedida = ReadString(reader, "UnidadMedida"),
                UnidadAbreviatura = ReadString(reader, "UnidadAbreviatura"),
                UnidadPermiteDecimales = ReadBool(reader, "UnidadPermiteDecimales"),
                Costo = ReadNullableDecimal(reader, "Costo"),
                PrecioPublico = ReadDecimal(reader, "PrecioPublico"),
                CausaInventario = ReadBool(reader, "CausaInventario"),
                PermiteVentaSinExistencia = ReadBool(reader, "PermiteVentaSinExistencia"),
                ExistenciaActual = ReadNullableDecimal(reader, "ExistenciaActual"),
                ExistenciaMinima = ReadNullableDecimal(reader, "ExistenciaMinima"),
                CostoPromedio = ReadNullableDecimal(reader, "CostoPromedio"),
                ImagenUrl = ReadString(reader, "ImagenUrl"),
                ImagenNombre = ReadString(reader, "ImagenNombre"),
                Activo = ReadBool(reader, "Activo"),
                FechaCreacion = ReadDateTime(reader, "FechaCreacion"),
                FechaActualizacion = ReadNullableDateTime(reader, "FechaActualizacion"),
                FechaArchivado = ReadNullableDateTime(reader, "FechaArchivado")
            };
        }

        private static ProductoServicioCategoriaDto MapCategoria(SqlDataReader reader)
        {
            byte aplicaA = ReadByte(reader, "AplicaA");
            return new ProductoServicioCategoriaDto
            {
                Id = ReadGuid(reader, "id"),
                IdEmpresa = ReadGuid(reader, "idEmpresa"),
                IdentityKey = ReadGuid(reader, "identityKey"),
                Codigo = ReadString(reader, "Codigo"),
                Nombre = ReadString(reader, "Nombre"),
                Descripcion = ReadString(reader, "Descripcion"),
                AplicaA = aplicaA,
                AplicaANombre = GetAplicaANombre(aplicaA),
                Activo = ReadBool(reader, "Activo"),
                FechaCreacion = ReadDateTime(reader, "FechaCreacion"),
                FechaActualizacion = ReadDateTime(reader, "FechaActualizacion"),
                FechaArchivado = ReadNullableDateTime(reader, "FechaArchivado")
            };
        }

        private static ProductoServicioMarcaDto MapMarca(SqlDataReader reader)
        {
            return new ProductoServicioMarcaDto
            {
                Id = ReadGuid(reader, "id"),
                IdEmpresa = ReadGuid(reader, "idEmpresa"),
                IdentityKey = ReadGuid(reader, "identityKey"),
                Codigo = ReadString(reader, "Codigo"),
                Nombre = ReadString(reader, "Nombre"),
                Descripcion = ReadString(reader, "Descripcion"),
                Activo = ReadBool(reader, "Activo"),
                FechaCreacion = ReadDateTime(reader, "FechaCreacion"),
                FechaActualizacion = ReadDateTime(reader, "FechaActualizacion"),
                FechaArchivado = ReadNullableDateTime(reader, "FechaArchivado")
            };
        }

        private static ProductoServicioUnidadMedidaDto MapUnidad(SqlDataReader reader)
        {
            return new ProductoServicioUnidadMedidaDto
            {
                Id = ReadGuid(reader, "id"),
                IdEmpresa = ReadGuid(reader, "idEmpresa"),
                IdentityKey = ReadGuid(reader, "identityKey"),
                Codigo = ReadString(reader, "Codigo"),
                Nombre = ReadString(reader, "Nombre"),
                Descripcion = ReadString(reader, "Descripcion"),
                Abreviatura = ReadString(reader, "Abreviatura"),
                PermiteDecimales = ReadBool(reader, "PermiteDecimales"),
                Activo = ReadBool(reader, "Activo"),
                FechaCreacion = ReadDateTime(reader, "FechaCreacion"),
                FechaActualizacion = ReadDateTime(reader, "FechaActualizacion"),
                FechaArchivado = ReadNullableDateTime(reader, "FechaArchivado")
            };
        }

        private static ProductoServicioCatalogoComboDto MapCatalogoCombo(SqlDataReader reader)
        {
            return new ProductoServicioCatalogoComboDto
            {
                Id = ReadGuid(reader, "id"),
                Codigo = ReadString(reader, "Codigo"),
                Nombre = ReadString(reader, "Nombre"),
                Descripcion = ReadString(reader, "Descripcion"),
                Activo = ReadBool(reader, "Activo"),
                AplicaA = ReadNullableByte(reader, "AplicaA"),
                Abreviatura = ReadString(reader, "Abreviatura"),
                PermiteDecimales = ReadNullableBool(reader, "PermiteDecimales")
            };
        }

        private static ProductoServicioMovimientoDto MapMovimiento(SqlDataReader reader)
        {
            byte tipoMovimiento = ReadByte(reader, "TipoMovimiento");
            return new ProductoServicioMovimientoDto
            {
                Id = ReadGuid(reader, "id"),
                IdEmpresa = ReadGuid(reader, "idEmpresa"),
                IdentityKey = ReadGuid(reader, "identityKey"),
                IdProductoServicio = ReadGuid(reader, "idProductoServicio"),
                TipoMovimiento = tipoMovimiento,
                TipoMovimientoNombre = GetMovimientoNombre(tipoMovimiento),
                Cantidad = ReadDecimal(reader, "Cantidad"),
                ExistenciaAnterior = ReadDecimal(reader, "ExistenciaAnterior"),
                ExistenciaPosterior = ReadDecimal(reader, "ExistenciaPosterior"),
                CostoUnitario = ReadNullableDecimal(reader, "CostoUnitario"),
                Referencia = ReadString(reader, "Referencia"),
                Observaciones = ReadString(reader, "Observaciones"),
                IdUsuario = ReadNullableGuid(reader, "idUsuario"),
                FechaMovimiento = ReadDateTime(reader, "FechaMovimiento")
            };
        }

        private static Guid ReadGuid(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return !reader.IsDBNull(ordinal) ? reader.GetGuid(ordinal) : Guid.Empty;
        }

        private static Guid? ReadNullableGuid(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetGuid(ordinal);
        }

        private static byte ReadByte(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return !reader.IsDBNull(ordinal) ? reader.GetByte(ordinal) : (byte)0;
        }

        private static byte? ReadNullableByte(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetByte(ordinal);
        }

        private static bool ReadBool(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return !reader.IsDBNull(ordinal) && reader.GetBoolean(ordinal);
        }

        private static bool? ReadNullableBool(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetBoolean(ordinal);
        }

        private static string ReadString(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
        }

        private static decimal ReadDecimal(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? 0m : reader.GetDecimal(ordinal);
        }

        private static decimal? ReadNullableDecimal(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
        }

        private static int ReadInt(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? 0 : Convert.ToInt32(reader.GetValue(ordinal));
        }

        private static DateTime ReadDateTime(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? DateTime.MinValue : reader.GetDateTime(ordinal);
        }

        private static DateTime? ReadNullableDateTime(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
        }

        private sealed class RequestContext
        {
            public Guid IdEmpresa { get; set; }
            public string EmpresaStorageKey { get; set; } = string.Empty;
        }

        private sealed class SignedProxyContext
        {
            public Guid IdEmpresa { get; set; }
            public string EmpresaStorageKey { get; set; } = string.Empty;
            public Guid? UsuarioId { get; set; }
        }

        private sealed class NormalizedProductoServicioRequest
        {
            public Guid? Id { get; set; }
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

        private sealed class ProductoServicioSnapshot
        {
            public Guid Id { get; set; }
            public Guid IdEmpresa { get; set; }
            public byte Tipo { get; set; }
            public string Codigo { get; set; } = string.Empty;
            public Guid IdCategoria { get; set; }
            public Guid? IdMarca { get; set; }
            public Guid IdUnidadMedida { get; set; }
            public bool CausaInventario { get; set; }
            public bool PermiteVentaSinExistencia { get; set; }
            public bool Activo { get; set; }
            public string ImagenUrl { get; set; } = string.Empty;
            public string ImagenNombre { get; set; } = string.Empty;
            public Guid? IdExistencia { get; set; }
        }

        private sealed class TemporalImageTokenPayload
        {
            public string NombreOriginal { get; set; } = string.Empty;
            public string NombreAlmacenado { get; set; } = string.Empty;
            public string Extension { get; set; } = string.Empty;
            public string MimeType { get; set; } = string.Empty;
            public string UrlFirebase { get; set; } = string.Empty;
            public string FolderName { get; set; } = string.Empty;
            public long PesoBytes { get; set; }
            public DateTime ExpiraUtc { get; set; }
        }

        private sealed class UploadedImagePayload
        {
            public string FolderName { get; set; } = string.Empty;
            public string NombreOriginal { get; set; } = string.Empty;
            public string NombreAlmacenado { get; set; } = string.Empty;
            public string Extension { get; set; } = string.Empty;
            public string MimeType { get; set; } = string.Empty;
            public string UrlFirebase { get; set; } = string.Empty;
            public long PesoBytes { get; set; }
        }

        private sealed class FirebaseCleanupItem
        {
            public string FolderName { get; set; } = string.Empty;
            public string StoredName { get; set; } = string.Empty;
        }

        private enum ImageOperationMode
        {
            None,
            NewImage,
            Remove
        }

        private sealed class PreparedImageOperation
        {
            public ImageOperationMode Mode { get; set; }
            public UploadedImagePayload? UploadedImage { get; set; }
            public FirebaseCleanupItem? TemporalCleanup { get; set; }
            public FirebaseCleanupItem? NewImageCleanup { get; set; }

            public static PreparedImageOperation None() => new PreparedImageOperation { Mode = ImageOperationMode.None };

            public static PreparedImageOperation ForRemove() => new PreparedImageOperation { Mode = ImageOperationMode.Remove };

            public static PreparedImageOperation ForNewImage(UploadedImagePayload uploadedImage, FirebaseCleanupItem temporalCleanup)
            {
                return new PreparedImageOperation
                {
                    Mode = ImageOperationMode.NewImage,
                    UploadedImage = uploadedImage,
                    TemporalCleanup = temporalCleanup,
                    NewImageCleanup = new FirebaseCleanupItem
                    {
                        FolderName = uploadedImage.FolderName,
                        StoredName = uploadedImage.NombreAlmacenado
                    }
                };
            }
        }

        private sealed class ResolvedImageMutation
        {
            public string ImagenUrl { get; set; } = string.Empty;
            public string ImagenNombre { get; set; } = string.Empty;
            public FirebaseCleanupItem? PreviousImageCleanup { get; set; }
        }
    }
}
