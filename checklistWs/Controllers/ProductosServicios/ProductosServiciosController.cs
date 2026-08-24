using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Net;
using System.Net.Http;
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
        private const int NumeroColeccionLength = 50;
        private const int ClaveSatProductoLength = 20;
        private const int ClaveSatUnidadLength = 10;
        private const int ObjetoImpuestoLength = 4;
        private const int PrecioUnitarioUnidadLength = 20;
        private const int TipoPaqueteLength = 30;
        private const int AtributoNombreLength = 100;
        private const int AtributoValorLength = 120;
        private const int OpcionVarianteNombreLength = 100;
        private const int NombreArchivoLength = 255;
        private const int MimeTypeLength = 120;
        private const int UrlLength = 1000;
        private const long ImagenMaxBytes = 10L * 1024L * 1024L;
        private const long VideoMaxBytes = 200L * 1024L * 1024L;
        private const long DocumentoMaxBytes = 25L * 1024L * 1024L;
        private const long UploadTemporalRequestLimitBytes = 12L * 1024L * 1024L;
        private const long UploadTemporalMultimediaRequestLimitBytes = 210L * 1024L * 1024L;
        private static readonly TimeSpan TemporalTokenLifetime = TimeSpan.FromHours(6);
        private static readonly TimeSpan ProxyHeaderTolerance = TimeSpan.FromMinutes(5);
        private static readonly string[] MimeTypesImagenPermitidos = new[] { "image/jpeg", "image/png", "image/webp" };
        private static readonly string[] ExtensionesImagenPermitidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        private static readonly string[] TiposMultimediaPermitidos = new[] { "foto", "video", "documento" };
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
    ps.idColeccion,
    ISNULL(col.Numero, '') AS ColeccionNumero,
    ISNULL(col.Nombre, '') AS ColeccionNombre,
    ps.idPaquete,
    ISNULL(pa.Nombre, '') AS PaqueteNombre,
    ps.Costo,
    ps.PrecioPublico,
    ps.PrecioComparacion,
    ps.PrecioUnitarioMonto,
    ps.PrecioUnitarioBaseCantidad,
    ISNULL(ps.PrecioUnitarioUnidad, '') AS PrecioUnitarioUnidad,
    ISNULL(ps.ObjetoImpuesto, '') AS ObjetoImpuesto,
    ISNULL(ps.ClaveProductoSat, '') AS ClaveProductoSat,
    ISNULL(ps.ClaveUnidadSat, '') AS ClaveUnidadSat,
    ps.EsProductoFisico,
    ps.PesoKg,
    ps.LargoCm,
    ps.AnchoCm,
    ps.AltoCm,
    ps.UsaNumeroSerie,
    ps.CausaInventario,
    ps.PermiteVentaSinExistencia,
    ex.id AS IdExistencia,
    ex.ExistenciaActual,
    ex.ExistenciaMinima,
    ex.CostoPromedio,
    ISNULL(ps.ImagenUrl, '') AS ImagenUrl,
    ISNULL(ps.ImagenNombre, '') AS ImagenNombre,
    ISNULL(mm.CantidadFotos, 0) AS CantidadFotos,
    ISNULL(mm.CantidadVideos, 0) AS CantidadVideos,
    ISNULL(mm.CantidadDocumentos, 0) AS CantidadDocumentos,
    ISNULL(vr.CantidadVariantes, 0) AS CantidadVariantes,
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
LEFT JOIN dbo.ProductosServiciosColecciones col
    ON col.idEmpresa = ps.idEmpresa AND col.id = ps.idColeccion
LEFT JOIN dbo.ProductosServiciosPaquetes pa
    ON pa.idEmpresa = ps.idEmpresa AND pa.id = ps.idPaquete
LEFT JOIN dbo.ProductosServiciosExistencias ex
    ON ex.idEmpresa = ps.idEmpresa AND ex.idProductoServicio = ps.id
OUTER APPLY (
    SELECT
        SUM(CASE WHEN pm.Activo = 1 AND pm.Foto = 1 THEN 1 ELSE 0 END) AS CantidadFotos,
        SUM(CASE WHEN pm.Activo = 1 AND pm.Video = 1 THEN 1 ELSE 0 END) AS CantidadVideos,
        SUM(CASE WHEN pm.Activo = 1 AND pm.Documento = 1 THEN 1 ELSE 0 END) AS CantidadDocumentos
    FROM dbo.ProductosServiciosMultimedia pm
    WHERE pm.idEmpresa = ps.idEmpresa AND pm.idProductoServicio = ps.id
) mm
OUTER APPLY (
    SELECT COUNT(1) AS CantidadVariantes
    FROM dbo.ProductosServiciosVariantes pv
    WHERE pv.idEmpresa = ps.idEmpresa AND pv.idProductoServicio = ps.id AND pv.Activo = 1
) vr
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
    ps.idColeccion,
    ISNULL(col.Numero, '') AS ColeccionNumero,
    ISNULL(col.Nombre, '') AS ColeccionNombre,
    ps.idPaquete,
    ISNULL(pa.Nombre, '') AS PaqueteNombre,
    ps.Costo,
    ps.PrecioPublico,
    ps.PrecioComparacion,
    ps.PrecioUnitarioMonto,
    ps.PrecioUnitarioBaseCantidad,
    ISNULL(ps.PrecioUnitarioUnidad, '') AS PrecioUnitarioUnidad,
    ISNULL(ps.ObjetoImpuesto, '') AS ObjetoImpuesto,
    ISNULL(ps.ClaveProductoSat, '') AS ClaveProductoSat,
    ISNULL(ps.ClaveUnidadSat, '') AS ClaveUnidadSat,
    ps.EsProductoFisico,
    ps.PesoKg,
    ps.LargoCm,
    ps.AnchoCm,
    ps.AltoCm,
    ps.UsaNumeroSerie,
    ps.CausaInventario,
    ps.PermiteVentaSinExistencia,
    ex.id AS IdExistencia,
    ex.ExistenciaActual,
    ex.ExistenciaMinima,
    ex.CostoPromedio,
    ISNULL(ps.ImagenUrl, '') AS ImagenUrl,
    ISNULL(ps.ImagenNombre, '') AS ImagenNombre,
    ISNULL(mm.CantidadFotos, 0) AS CantidadFotos,
    ISNULL(mm.CantidadVideos, 0) AS CantidadVideos,
    ISNULL(mm.CantidadDocumentos, 0) AS CantidadDocumentos,
    ISNULL(vr.CantidadVariantes, 0) AS CantidadVariantes,
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
LEFT JOIN dbo.ProductosServiciosColecciones col
    ON col.idEmpresa = ps.idEmpresa AND col.id = ps.idColeccion
LEFT JOIN dbo.ProductosServiciosPaquetes pa
    ON pa.idEmpresa = ps.idEmpresa AND pa.id = ps.idPaquete
LEFT JOIN dbo.ProductosServiciosExistencias ex
    ON ex.idEmpresa = ps.idEmpresa AND ex.idProductoServicio = ps.id
OUTER APPLY (
    SELECT
        SUM(CASE WHEN pm.Activo = 1 AND pm.Foto = 1 THEN 1 ELSE 0 END) AS CantidadFotos,
        SUM(CASE WHEN pm.Activo = 1 AND pm.Video = 1 THEN 1 ELSE 0 END) AS CantidadVideos,
        SUM(CASE WHEN pm.Activo = 1 AND pm.Documento = 1 THEN 1 ELSE 0 END) AS CantidadDocumentos
    FROM dbo.ProductosServiciosMultimedia pm
    WHERE pm.idEmpresa = ps.idEmpresa AND pm.idProductoServicio = ps.id
) mm
OUTER APPLY (
    SELECT COUNT(1) AS CantidadVariantes
    FROM dbo.ProductosServiciosVariantes pv
    WHERE pv.idEmpresa = ps.idEmpresa AND pv.idProductoServicio = ps.id AND pv.Activo = 1
) vr
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
                            IdColeccion = baseItem.IdColeccion,
                            ColeccionNumero = baseItem.ColeccionNumero,
                            ColeccionNombre = baseItem.ColeccionNombre,
                            IdPaquete = baseItem.IdPaquete,
                            PaqueteNombre = baseItem.PaqueteNombre,
                            Costo = baseItem.Costo,
                            PrecioPublico = baseItem.PrecioPublico,
                            PrecioComparacion = baseItem.PrecioComparacion,
                            PrecioUnitarioMonto = baseItem.PrecioUnitarioMonto,
                            PrecioUnitarioBaseCantidad = baseItem.PrecioUnitarioBaseCantidad,
                            PrecioUnitarioUnidad = baseItem.PrecioUnitarioUnidad,
                            ObjetoImpuesto = baseItem.ObjetoImpuesto,
                            ClaveProductoSat = baseItem.ClaveProductoSat,
                            ClaveUnidadSat = baseItem.ClaveUnidadSat,
                            EsProductoFisico = baseItem.EsProductoFisico,
                            PesoKg = baseItem.PesoKg,
                            LargoCm = baseItem.LargoCm,
                            AnchoCm = baseItem.AnchoCm,
                            AltoCm = baseItem.AltoCm,
                            UsaNumeroSerie = baseItem.UsaNumeroSerie,
                            CausaInventario = baseItem.CausaInventario,
                            PermiteVentaSinExistencia = baseItem.PermiteVentaSinExistencia,
                            ExistenciaActual = baseItem.ExistenciaActual,
                            ExistenciaMinima = baseItem.ExistenciaMinima,
                            CostoPromedio = baseItem.CostoPromedio,
                            ImagenUrl = baseItem.ImagenUrl,
                            ImagenNombre = baseItem.ImagenNombre,
                            CantidadFotos = baseItem.CantidadFotos,
                            CantidadVideos = baseItem.CantidadVideos,
                            CantidadDocumentos = baseItem.CantidadDocumentos,
                            CantidadVariantes = baseItem.CantidadVariantes,
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
                detalle.Atributos = await ObtenerAtributosProductoAsync(connection, context.IdEmpresa, idProductoServicio);
                detalle.OpcionesVariante = await ObtenerOpcionesVarianteProductoAsync(connection, context.IdEmpresa, idProductoServicio);
                detalle.Variantes = await ObtenerVariantesProductoAsync(connection, context.IdEmpresa, idProductoServicio);
                detalle.Multimedia = await ObtenerMultimediaProductoAsync(connection, context.IdEmpresa, idProductoServicio);
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

        [HttpPost("SubirMultimediaTemporal")]
        [RequestFormLimits(MultipartBodyLengthLimit = UploadTemporalMultimediaRequestLimitBytes)]
        [RequestSizeLimit(UploadTemporalMultimediaRequestLimitBytes)]
        public async Task<IActionResult> SubirMultimediaTemporal(Guid idEmpresa, [FromForm] string tipoMultimedia, [FromForm] string operacionCarga, IFormFile? archivo)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                string tipo = NormalizeTipoMultimedia(tipoMultimedia);
                if (string.IsNullOrWhiteSpace(tipo))
                {
                    return BadRequest(new ProductoServicioMultimediaTemporalResponse { Mensaje = "Selecciona un tipo de evidencia válido." });
                }

                if (archivo == null || archivo.Length <= 0)
                {
                    return BadRequest(new ProductoServicioMultimediaTemporalResponse { Mensaje = "Selecciona un archivo válido para cargar." });
                }

                string validation = ValidateTemporalMultimediaUpload(tipo, archivo);
                if (!string.IsNullOrWhiteSpace(validation))
                {
                    return BadRequest(new ProductoServicioMultimediaTemporalResponse { Mensaje = validation });
                }

                byte[] fileBytes = await ReadFileBytesAsync(archivo);
                string folderName = BuildTemporalMultimediaFolderName(context.EmpresaStorageKey, operacionCarga, tipo);
                UploadedImagePayload uploaded = await UploadImageToFirebaseAsync(
                    folderName,
                    BuildStoredFileName(archivo.FileName, archivo.ContentType),
                    fileBytes,
                    archivo.FileName,
                    archivo.ContentType,
                    archivo.Length);

                return Ok(new ProductoServicioMultimediaTemporalResponse
                {
                    Mensaje = "La evidencia temporal fue cargada.",
                    Archivo = new ProductoServicioMultimediaTemporalDto
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
                            TipoMultimedia = tipo,
                            ExpiraUtc = DateTime.UtcNow.Add(TemporalTokenLifetime)
                        }),
                        TipoMultimedia = tipo,
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
                return HandleException(ex, "SubirMultimediaTemporal", "No fue posible procesar la evidencia temporal.");
            }
        }

        [HttpPost("LimpiarMultimediaTemporal")]
        public async Task<IActionResult> LimpiarMultimediaTemporal(Guid idEmpresa, [FromBody] ProductoServicioMultimediaTemporalCleanupRequest? request)
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
                return HandleException(ex, "LimpiarMultimediaTemporal", "No fue posible limpiar la evidencia temporal.");
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
                PreparedMultimediaOperation preparedMultimedia = await PrepareMultimediaOperationAsync(context, productoId, normalized);
                PreparedVariantSyncResult variantSync = new PreparedVariantSyncResult();

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
    (id, idEmpresa, identityKey, Tipo, Codigo, Tag, Nombre, Descripcion, idCategoria, idMarca, idUnidadMedida, idColeccion, idPaquete, Costo, PrecioPublico, PrecioComparacion, PrecioUnitarioMonto, PrecioUnitarioBaseCantidad, PrecioUnitarioUnidad, ObjetoImpuesto, ClaveProductoSat, ClaveUnidadSat, EsProductoFisico, PesoKg, LargoCm, AnchoCm, AltoCm, UsaNumeroSerie, CausaInventario, PermiteVentaSinExistencia, ImagenUrl, ImagenNombre, Activo, FechaCreacion, FechaActualizacion, FechaArchivado)
VALUES
    (@Id, @IdEmpresa, @IdentityKey, @Tipo, @Codigo, @Tag, @Nombre, @Descripcion, @IdCategoria, @IdMarca, @IdUnidadMedida, @IdColeccion, @IdPaquete, @Costo, @PrecioPublico, @PrecioComparacion, @PrecioUnitarioMonto, @PrecioUnitarioBaseCantidad, @PrecioUnitarioUnidad, @ObjetoImpuesto, @ClaveProductoSat, @ClaveUnidadSat, @EsProductoFisico, @PesoKg, @LargoCm, @AnchoCm, @AltoCm, @UsaNumeroSerie, @CausaInventario, @PermiteVentaSinExistencia, @ImagenUrl, @ImagenNombre, @Activo, @FechaCreacion, @FechaActualizacion, NULL)", connection, transaction);

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
    idColeccion = @IdColeccion,
    idPaquete = @IdPaquete,
    Costo = @Costo,
    PrecioPublico = @PrecioPublico,
    PrecioComparacion = @PrecioComparacion,
    PrecioUnitarioMonto = @PrecioUnitarioMonto,
    PrecioUnitarioBaseCantidad = @PrecioUnitarioBaseCantidad,
    PrecioUnitarioUnidad = @PrecioUnitarioUnidad,
    ObjetoImpuesto = @ObjetoImpuesto,
    ClaveProductoSat = @ClaveProductoSat,
    ClaveUnidadSat = @ClaveUnidadSat,
    EsProductoFisico = @EsProductoFisico,
    PesoKg = @PesoKg,
    LargoCm = @LargoCm,
    AnchoCm = @AnchoCm,
    AltoCm = @AltoCm,
    UsaNumeroSerie = @UsaNumeroSerie,
    CausaInventario = @CausaInventario,
    PermiteVentaSinExistencia = @PermiteVentaSinExistencia,
    ImagenUrl = @ImagenUrl,
    ImagenNombre = @ImagenNombre,
    Activo = @Activo,
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
                    await SynchronizeProductoAtributosAsync(connection, transaction, context.IdEmpresa, productoId, normalized.Atributos, ahora);
                    Dictionary<string, VariantOptionReference> optionReferences = await SynchronizeProductoOpcionesVarianteAsync(connection, transaction, context.IdEmpresa, productoId, normalized.OpcionesVariante, ahora);
                    variantSync = await SynchronizeProductoVariantesAsync(connection, transaction, context, productoId, normalized.Variantes, optionReferences, ahora);
                    await SynchronizeProductoMultimediaAsync(connection, transaction, context.IdEmpresa, productoId, preparedMultimedia.FinalItems, ahora);
                    transaction.Commit();

                    await FinalizeImageOperationAfterCommitAsync(preparedImage, imageMutation.PreviousImageCleanup);
                    await FinalizeVariantSyncAfterCommitAsync(variantSync);
                    await FinalizeMultimediaOperationAfterCommitAsync(preparedMultimedia);
                    return Ok(new ProductoServicioOperacionResponse
                    {
                        Mensaje = esNuevo ? "El producto o servicio fue registrado." : "El producto o servicio fue actualizado."
                    });
                }
                catch (ProductoServicioValidationException validationEx)
                {
                    await CompensatePreparedVariantSyncAsync(variantSync);
                    await CompensatePreparedImageAsync(preparedImage);
                    await CompensatePreparedMultimediaAsync(preparedMultimedia);
                    return BadRequest(new ProductoServicioOperacionResponse { Mensaje = validationEx.Message });
                }
                catch (Exception ex)
                {
                    await CompensatePreparedVariantSyncAsync(variantSync);
                    await CompensatePreparedImageAsync(preparedImage);
                    await CompensatePreparedMultimediaAsync(preparedMultimedia);
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
                    Colecciones = await ObtenerColeccionesComboAsync(context.IdEmpresa),
                    Paquetes = await ObtenerPaquetesComboAsync(context.IdEmpresa),
                    Atributos = await ObtenerAtributosComboAsync(context.IdEmpresa),
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
                    },
                    ObjetosImpuesto = new List<ProductoServicioOpcionDto>
                    {
                        new ProductoServicioOpcionDto { Clave = "01", Nombre = "01 · No objeto de impuesto" },
                        new ProductoServicioOpcionDto { Clave = "02", Nombre = "02 · Sí objeto de impuesto" },
                        new ProductoServicioOpcionDto { Clave = "03", Nombre = "03 · Sí objeto y no obligado al desglose" },
                        new ProductoServicioOpcionDto { Clave = "04", Nombre = "04 · Sí objeto y no causa impuesto" }
                    },
                    TiposPaquete = new List<ProductoServicioOpcionDto>
                    {
                        new ProductoServicioOpcionDto { Clave = "caja", Nombre = "Caja" },
                        new ProductoServicioOpcionDto { Clave = "sobre", Nombre = "Sobre" },
                        new ProductoServicioOpcionDto { Clave = "flexible", Nombre = "Paquete flexible" }
                    },
                    UnidadesPrecioUnitario = new List<ProductoServicioOpcionDto>
                    {
                        new ProductoServicioOpcionDto { Clave = "kg", Nombre = "kg" },
                        new ProductoServicioOpcionDto { Clave = "g", Nombre = "g" },
                        new ProductoServicioOpcionDto { Clave = "lb", Nombre = "lb" },
                        new ProductoServicioOpcionDto { Clave = "l", Nombre = "l" },
                        new ProductoServicioOpcionDto { Clave = "ml", Nombre = "ml" },
                        new ProductoServicioOpcionDto { Clave = "pz", Nombre = "pz" },
                        new ProductoServicioOpcionDto { Clave = "m", Nombre = "m" }
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerCombosProductosServicios", "No fue posible cargar los catálogos del módulo.");
            }
        }

        [HttpGet("BuscarCatalogosSatProductoServicio")]
        public async Task<IActionResult> BuscarCatalogosSatProductoServicio(Guid idEmpresa, string tipo = "", string q = "", int take = 40)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            _ = context;

            try
            {
                string tipoNormalizado = (tipo ?? string.Empty).Trim().ToLowerInvariant();
                if (tipoNormalizado != "producto" && tipoNormalizado != "unidad")
                {
                    return BadRequest(new ProductoServicioOperacionResponse { Mensaje = "Selecciona un catálogo SAT válido." });
                }

                int top = Math.Clamp(take, 20, 120);
                List<ProductoServicioOpcionDto> items;

                if (tipoNormalizado == "producto")
                {
                    string[] terms = ParseSatTerms(q);
                    if (terms.Length == 0)
                    {
                        return Ok(new ProductoServicioSatCatalogosResponseDto());
                    }

                    items = await BuscarClavesProductoSatAsync(terms, top);
                }
                else
                {
                    items = await BuscarClavesUnidadSatAsync(q, top);
                }

                return Ok(new ProductoServicioSatCatalogosResponseDto { Items = items });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "BuscarCatalogosSatProductoServicio", "No fue posible consultar el catálogo SAT.");
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
                    "Ya existe una marca con este código.",
                    request.Codigo,
                    request.Nombre,
                    request.Descripcion,
                    null,
                    null,
                    false,
                    duplicateNameMessage: "Ya existe una marca con este nombre.");
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
                    "Ya existe una unidad de medida con este código.",
                    request.Codigo,
                    request.Nombre,
                    request.Descripcion,
                    request.Abreviatura,
                    request.PermiteDecimales,
                    true,
                    duplicateNameMessage: "Ya existe una unidad de medida con este nombre.");
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

        [HttpPost("GuardarColeccionProductoServicio")]
        public async Task<IActionResult> GuardarColeccionProductoServicio([FromBody] ProductoServicioColeccionGuardarRequest request, Guid idEmpresa)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                string validation = ValidateColeccionRequest(request, context.IdEmpresa);
                if (!string.IsNullOrWhiteSpace(validation))
                {
                    return BadRequest(new ProductoServicioOperacionResponse { Mensaje = validation });
                }

                using SqlConnection connection = CreateConnection();
                await connection.OpenAsync();
                using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable);

                Guid id = request.Id ?? Guid.NewGuid();
                bool esNuevo = !request.Id.HasValue || request.Id.Value == Guid.Empty;
                if (await ExisteNumeroColeccionAsync(connection, transaction, context.IdEmpresa, request.Numero.Trim(), esNuevo ? null : id))
                {
                    transaction.Rollback();
                    return BadRequest(new ProductoServicioOperacionResponse { Mensaje = "Ya existe una colección con este número." });
                }

                if (await ExisteNombreColeccionAsync(connection, transaction, context.IdEmpresa, request.Nombre.Trim(), esNuevo ? null : id))
                {
                    transaction.Rollback();
                    return BadRequest(new ProductoServicioOperacionResponse { Mensaje = "Ya existe una colección con este nombre." });
                }

                DateTime ahora = DateTime.UtcNow;
                if (esNuevo)
                {
                    using SqlCommand insert = new SqlCommand(@"
INSERT INTO dbo.ProductosServiciosColecciones
    (id, idEmpresa, identityKey, Numero, Nombre, Descripcion, Activo, FechaCreacion, FechaActualizacion, FechaArchivado)
VALUES
    (@Id, @IdEmpresa, @IdentityKey, @Numero, @Nombre, @Descripcion, 1, @FechaCreacion, @FechaActualizacion, NULL)", connection, transaction);
                    insert.Parameters.AddWithValue("@Id", id);
                    insert.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);
                    insert.Parameters.AddWithValue("@IdentityKey", Guid.NewGuid());
                    insert.Parameters.AddWithValue("@Numero", request.Numero.Trim());
                    insert.Parameters.AddWithValue("@Nombre", request.Nombre.Trim());
                    insert.Parameters.AddWithValue("@Descripcion", string.IsNullOrWhiteSpace(request.Descripcion) ? DBNull.Value : request.Descripcion.Trim());
                    insert.Parameters.AddWithValue("@FechaCreacion", ahora);
                    insert.Parameters.AddWithValue("@FechaActualizacion", ahora);
                    await insert.ExecuteNonQueryAsync();
                }
                else
                {
                    using SqlCommand update = new SqlCommand(@"
UPDATE dbo.ProductosServiciosColecciones
SET Numero = @Numero,
    Nombre = @Nombre,
    Descripcion = @Descripcion,
    FechaActualizacion = @FechaActualizacion
WHERE idEmpresa = @IdEmpresa AND id = @Id", connection, transaction);
                    update.Parameters.AddWithValue("@Id", id);
                    update.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);
                    update.Parameters.AddWithValue("@Numero", request.Numero.Trim());
                    update.Parameters.AddWithValue("@Nombre", request.Nombre.Trim());
                    update.Parameters.AddWithValue("@Descripcion", string.IsNullOrWhiteSpace(request.Descripcion) ? DBNull.Value : request.Descripcion.Trim());
                    update.Parameters.AddWithValue("@FechaActualizacion", ahora);
                    await update.ExecuteNonQueryAsync();
                }

                transaction.Commit();
                return Ok(new ProductoServicioColeccionOperacionResponse
                {
                    Mensaje = esNuevo ? "Se registró la colección." : "Se actualizó la colección.",
                    Coleccion = new ProductoServicioCatalogoComboDto
                    {
                        Id = id,
                        Numero = request.Numero.Trim(),
                        Nombre = request.Nombre.Trim(),
                        Descripcion = string.IsNullOrWhiteSpace(request.Descripcion) ? string.Empty : request.Descripcion.Trim(),
                        Activo = true
                    }
                });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "GuardarColeccionProductoServicio", "No fue posible guardar la colección.");
            }
        }

        [HttpPost("GuardarPaqueteProductoServicio")]
        public async Task<IActionResult> GuardarPaqueteProductoServicio([FromBody] ProductoServicioPaqueteGuardarRequest request, Guid idEmpresa)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                string validation = ValidatePaqueteRequest(request, context.IdEmpresa);
                if (!string.IsNullOrWhiteSpace(validation))
                {
                    return BadRequest(new ProductoServicioOperacionResponse { Mensaje = validation });
                }

                using SqlConnection connection = CreateConnection();
                await connection.OpenAsync();
                using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable);

                Guid id = request.Id ?? Guid.NewGuid();
                bool esNuevo = !request.Id.HasValue || request.Id.Value == Guid.Empty;
                DateTime ahora = DateTime.UtcNow;

                if (await ExistePaqueteMismoNombreTipoAsync(connection, transaction, context.IdEmpresa, request.Nombre.Trim(), request.TipoPaquete.Trim(), esNuevo ? null : id))
                {
                    transaction.Rollback();
                    return BadRequest(new ProductoServicioOperacionResponse { Mensaje = "Ya existe un paquete con el mismo nombre y tipo." });
                }

                if (request.EsPredeterminado)
                {
                    using SqlCommand clearDefaults = new SqlCommand(@"
UPDATE dbo.ProductosServiciosPaquetes
SET EsPredeterminado = 0,
    FechaActualizacion = @FechaActualizacion
WHERE idEmpresa = @IdEmpresa AND EsPredeterminado = 1 AND (@IdActual IS NULL OR id <> @IdActual)", connection, transaction);
                    clearDefaults.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);
                    clearDefaults.Parameters.AddWithValue("@IdActual", esNuevo ? DBNull.Value : id);
                    clearDefaults.Parameters.AddWithValue("@FechaActualizacion", ahora);
                    await clearDefaults.ExecuteNonQueryAsync();
                }

                if (esNuevo)
                {
                    using SqlCommand insert = new SqlCommand(@"
INSERT INTO dbo.ProductosServiciosPaquetes
    (id, idEmpresa, identityKey, Nombre, TipoPaquete, LargoCm, AnchoCm, AltoCm, PesoEmpaqueVacioKg, EsPredeterminado, Activo, FechaCreacion, FechaActualizacion, FechaArchivado)
VALUES
    (@Id, @IdEmpresa, @IdentityKey, @Nombre, @TipoPaquete, @LargoCm, @AnchoCm, @AltoCm, @PesoEmpaqueVacioKg, @EsPredeterminado, 1, @FechaCreacion, @FechaActualizacion, NULL)", connection, transaction);
                    AddPaqueteParameters(insert, id, context.IdEmpresa, request, ahora, true);
                    await insert.ExecuteNonQueryAsync();
                }
                else
                {
                    using SqlCommand update = new SqlCommand(@"
UPDATE dbo.ProductosServiciosPaquetes
SET Nombre = @Nombre,
    TipoPaquete = @TipoPaquete,
    LargoCm = @LargoCm,
    AnchoCm = @AnchoCm,
    AltoCm = @AltoCm,
    PesoEmpaqueVacioKg = @PesoEmpaqueVacioKg,
    EsPredeterminado = @EsPredeterminado,
    FechaActualizacion = @FechaActualizacion
WHERE idEmpresa = @IdEmpresa AND id = @Id", connection, transaction);
                    AddPaqueteParameters(update, id, context.IdEmpresa, request, ahora, false);
                    await update.ExecuteNonQueryAsync();
                }

                transaction.Commit();
                return Ok(new ProductoServicioPaqueteOperacionResponse
                {
                    Mensaje = esNuevo ? "Se registró el paquete." : "Se actualizó el paquete.",
                    Paquete = new ProductoServicioCatalogoComboDto
                    {
                        Id = id,
                        Nombre = request.Nombre.Trim(),
                        TipoPaquete = request.TipoPaquete.Trim(),
                        LargoCm = request.LargoCm,
                        AnchoCm = request.AnchoCm,
                        AltoCm = request.AltoCm,
                        PesoEmpaqueVacioKg = request.PesoEmpaqueVacioKg,
                        EsPredeterminado = request.EsPredeterminado,
                        Descripcion = string.Empty,
                        Activo = true
                    }
                });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "GuardarPaqueteProductoServicio", "No fue posible guardar el paquete.");
            }
        }

        [HttpPost("GuardarAtributoProductoServicio")]
        public async Task<IActionResult> GuardarAtributoProductoServicio([FromBody] ProductoServicioAtributoCatalogoGuardarRequest request, Guid idEmpresa)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                string validation = ValidateAtributoCatalogoRequest(request, context.IdEmpresa);
                if (!string.IsNullOrWhiteSpace(validation))
                {
                    return BadRequest(new ProductoServicioOperacionResponse { Mensaje = validation });
                }

                using SqlConnection connection = CreateConnection();
                await connection.OpenAsync();
                using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable);
                Guid id = request.Id ?? Guid.NewGuid();
                bool esNuevo = !request.Id.HasValue || request.Id.Value == Guid.Empty;
                if (await ExisteNombreAtributoAsync(connection, transaction, context.IdEmpresa, request.Nombre.Trim(), esNuevo ? null : id))
                {
                    transaction.Rollback();
                    return BadRequest(new ProductoServicioOperacionResponse { Mensaje = "Ya existe un atributo con el mismo nombre." });
                }

                DateTime ahora = DateTime.UtcNow;
                if (esNuevo)
                {
                    using SqlCommand insert = new SqlCommand(@"
INSERT INTO dbo.ProductosServiciosAtributos
    (id, idEmpresa, identityKey, Nombre, Activo, FechaCreacion, FechaActualizacion, FechaArchivado)
VALUES
    (@Id, @IdEmpresa, @IdentityKey, @Nombre, 1, @FechaCreacion, @FechaActualizacion, NULL)", connection, transaction);
                    insert.Parameters.AddWithValue("@Id", id);
                    insert.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);
                    insert.Parameters.AddWithValue("@IdentityKey", Guid.NewGuid());
                    insert.Parameters.AddWithValue("@Nombre", request.Nombre.Trim());
                    insert.Parameters.AddWithValue("@FechaCreacion", ahora);
                    insert.Parameters.AddWithValue("@FechaActualizacion", ahora);
                    await insert.ExecuteNonQueryAsync();
                }
                else
                {
                    using SqlCommand update = new SqlCommand(@"
UPDATE dbo.ProductosServiciosAtributos
SET Nombre = @Nombre,
    FechaActualizacion = @FechaActualizacion
WHERE idEmpresa = @IdEmpresa AND id = @Id", connection, transaction);
                    update.Parameters.AddWithValue("@Id", id);
                    update.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);
                    update.Parameters.AddWithValue("@Nombre", request.Nombre.Trim());
                    update.Parameters.AddWithValue("@FechaActualizacion", ahora);
                    await update.ExecuteNonQueryAsync();
                }

                transaction.Commit();
                return Ok(new ProductoServicioAtributoOperacionResponse
                {
                    Mensaje = esNuevo ? "Se registró el atributo." : "Se actualizó el atributo.",
                    Atributo = new ProductoServicioCatalogoComboDto
                    {
                        Id = id,
                        Nombre = request.Nombre.Trim(),
                        Descripcion = string.Empty,
                        Activo = true
                    }
                });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "GuardarAtributoProductoServicio", "No fue posible guardar el atributo.");
            }
        }

        [HttpGet("ObtenerValoresAtributoProductoServicio")]
        public async Task<IActionResult> ObtenerValoresAtributoProductoServicio(Guid idEmpresa, Guid idAtributo)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            if (idAtributo == Guid.Empty)
            {
                return Ok(new List<ProductoServicioAtributoValorDto>());
            }

            try
            {
                using SqlConnection connection = CreateConnection();
                await connection.OpenAsync();

                using SqlCommand command = new SqlCommand(@"
SELECT id, idEmpresa, idAtributo, Valor, Orden, Activo
FROM dbo.ProductosServiciosAtributosValores
WHERE idEmpresa = @IdEmpresa AND idAtributo = @IdAtributo AND Activo = 1
ORDER BY Orden, Valor", connection);
                command.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);
                command.Parameters.AddWithValue("@IdAtributo", idAtributo);

                List<ProductoServicioAtributoValorDto> items = new List<ProductoServicioAtributoValorDto>();
                using SqlDataReader reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    items.Add(new ProductoServicioAtributoValorDto
                    {
                        Id = ReadGuid(reader, "id"),
                        IdEmpresa = ReadGuid(reader, "idEmpresa"),
                        IdAtributo = ReadGuid(reader, "idAtributo"),
                        Valor = ReadString(reader, "Valor"),
                        Orden = ReadInt(reader, "Orden"),
                        Activo = ReadBool(reader, "Activo")
                    });
                }

                return Ok(items);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerValoresAtributoProductoServicio", "No fue posible cargar los valores del atributo.");
            }
        }

        [HttpPost("GuardarValorAtributoProductoServicio")]
        public async Task<IActionResult> GuardarValorAtributoProductoServicio([FromBody] ProductoServicioAtributoValorCatalogoGuardarRequest request, Guid idEmpresa)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                string validation = ValidateAtributoValorCatalogoRequest(request, context.IdEmpresa);
                if (!string.IsNullOrWhiteSpace(validation))
                {
                    return BadRequest(new ProductoServicioOperacionResponse { Mensaje = validation });
                }

                using SqlConnection connection = CreateConnection();
                await connection.OpenAsync();
                using SqlTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable);

                if (!await ExisteAtributoActivoAsync(connection, transaction, context.IdEmpresa, request.IdAtributo))
                {
                    transaction.Rollback();
                    return BadRequest(new ProductoServicioOperacionResponse { Mensaje = "Selecciona un atributo válido para registrar el elemento." });
                }

                Guid id = request.Id ?? Guid.NewGuid();
                bool esNuevo = !request.Id.HasValue || request.Id.Value == Guid.Empty;
                if (await ExisteValorAtributoAsync(connection, transaction, context.IdEmpresa, request.IdAtributo, request.Valor.Trim(), esNuevo ? null : id))
                {
                    transaction.Rollback();
                    return BadRequest(new ProductoServicioOperacionResponse { Mensaje = "Ya existe un elemento con el mismo nombre dentro de este atributo." });
                }

                DateTime ahora = DateTime.UtcNow;
                if (esNuevo)
                {
                    using SqlCommand insert = new SqlCommand(@"
INSERT INTO dbo.ProductosServiciosAtributosValores
    (id, idEmpresa, identityKey, idAtributo, Valor, Orden, Activo, FechaCreacion, FechaActualizacion, FechaArchivado)
VALUES
    (@Id, @IdEmpresa, @IdentityKey, @IdAtributo, @Valor, @Orden, 1, @FechaCreacion, @FechaActualizacion, NULL)", connection, transaction);
                    insert.Parameters.AddWithValue("@Id", id);
                    insert.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);
                    insert.Parameters.AddWithValue("@IdentityKey", Guid.NewGuid());
                    insert.Parameters.AddWithValue("@IdAtributo", request.IdAtributo);
                    insert.Parameters.AddWithValue("@Valor", request.Valor.Trim());
                    insert.Parameters.AddWithValue("@Orden", request.Orden <= 0 ? 1 : request.Orden);
                    insert.Parameters.AddWithValue("@FechaCreacion", ahora);
                    insert.Parameters.AddWithValue("@FechaActualizacion", ahora);
                    await insert.ExecuteNonQueryAsync();
                }
                else
                {
                    using SqlCommand update = new SqlCommand(@"
UPDATE dbo.ProductosServiciosAtributosValores
SET Valor = @Valor,
    Orden = @Orden,
    FechaActualizacion = @FechaActualizacion
WHERE idEmpresa = @IdEmpresa AND id = @Id AND idAtributo = @IdAtributo", connection, transaction);
                    update.Parameters.AddWithValue("@Id", id);
                    update.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);
                    update.Parameters.AddWithValue("@IdAtributo", request.IdAtributo);
                    update.Parameters.AddWithValue("@Valor", request.Valor.Trim());
                    update.Parameters.AddWithValue("@Orden", request.Orden <= 0 ? 1 : request.Orden);
                    update.Parameters.AddWithValue("@FechaActualizacion", ahora);
                    await update.ExecuteNonQueryAsync();
                }

                transaction.Commit();
                return Ok(new ProductoServicioAtributoValorOperacionResponse
                {
                    Mensaje = esNuevo ? "Se registró el elemento del atributo." : "Se actualizó el elemento del atributo.",
                    Valor = new ProductoServicioAtributoValorDto
                    {
                        Id = id,
                        IdEmpresa = context.IdEmpresa,
                        IdAtributo = request.IdAtributo,
                        Valor = request.Valor.Trim(),
                        Orden = request.Orden <= 0 ? 1 : request.Orden,
                        Activo = true
                    }
                });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "GuardarValorAtributoProductoServicio", "No fue posible guardar el elemento del atributo.");
            }
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
                "Ya existe una categoría con este código.",
                request.Codigo,
                request.Nombre,
                request.Descripcion,
                null,
                null,
                false,
                request.AplicaA,
                duplicateNameMessage: "Ya existe una categoría con este nombre.");
        }

        private async Task<IActionResult> GuardarCatalogoBasicoAsync(
            Guid? id,
            Guid idEmpresa,
            string tableName,
            string label,
            string duplicateCodeMessage,
            string codigo,
            string nombre,
            string descripcion,
            string? abreviatura,
            bool? permiteDecimales,
            bool includeUnidadFields,
            byte? aplicaA = null,
            string? duplicateNameMessage = null)
        {
            using SqlConnection connection = CreateConnection();
            await connection.OpenAsync();
            using SqlTransaction transaction = connection.BeginTransaction();

            Guid itemId = id ?? Guid.NewGuid();
            bool esNuevo = !id.HasValue || id.Value == Guid.Empty;

            if (await ExisteCodigoCatalogoAsync(connection, transaction, idEmpresa, codigo, esNuevo ? null : itemId, tableName))
            {
                transaction.Rollback();
                return BadRequest(new ProductoServicioOperacionResponse { Mensaje = duplicateCodeMessage });
            }

            if (!string.IsNullOrWhiteSpace(duplicateNameMessage))
            {
                bool nombreDuplicado = aplicaA.HasValue
                    ? await ExisteNombreCategoriaAsync(connection, transaction, idEmpresa, nombre, aplicaA.Value, esNuevo ? null : itemId)
                    : await ExisteNombreCatalogoAsync(connection, transaction, idEmpresa, nombre, esNuevo ? null : itemId, tableName);

                if (nombreDuplicado)
                {
                    transaction.Rollback();
                    return BadRequest(new ProductoServicioOperacionResponse { Mensaje = duplicateNameMessage });
                }
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
    ps.idColeccion,
    ps.idPaquete,
    ps.EsProductoFisico,
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
                IdColeccion = ReadNullableGuid(reader, "idColeccion"),
                IdPaquete = ReadNullableGuid(reader, "idPaquete"),
                EsProductoFisico = ReadBool(reader, "EsProductoFisico"),
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

        private async Task<ProductoServicioMovimientoDto?> ObtenerMovimientoExistenciaInicialAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idProductoServicio)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT TOP (1)
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
WHERE idEmpresa = @IdEmpresa
  AND idProductoServicio = @IdProductoServicio
ORDER BY FechaMovimiento ASC, id ASC", connection, transaction);

            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);

            using SqlDataReader reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }

            return MapMovimiento(reader);
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

            bool puedeAjustarExistenciaInicial =
                request.ExistenciaInicial.HasValue &&
                (
                    movimientosHistoricos == 0 ||
                    (
                        movimientosHistoricos == 1 &&
                        existenciaActual.ExistenciaActual >= 0m
                    )
                );

            if (puedeAjustarExistenciaInicial)
            {
                bool aplicarAjuste = movimientosHistoricos == 0;

                if (!aplicarAjuste && movimientosHistoricos == 1)
                {
                    ProductoServicioMovimientoDto? movimientoInicial = await ObtenerMovimientoExistenciaInicialAsync(connection, transaction, idEmpresa, productoId);
                    aplicarAjuste = movimientoInicial != null &&
                        movimientoInicial.TipoMovimiento == MovimientoExistenciaInicial &&
                        movimientoInicial.ExistenciaAnterior == 0m &&
                        movimientoInicial.ExistenciaPosterior == existenciaActual.ExistenciaActual;

                    if (aplicarAjuste)
                    {
                        await ActualizarMovimientoExistenciaInicialAsync(connection, transaction, movimientoInicial!.Id, request.ExistenciaInicial!.Value, request.Costo, ahora);
                    }
                }

                if (aplicarAjuste)
                {
                    await ActualizarExistenciaAsync(connection, transaction, existenciaActual.Id, request.ExistenciaInicial!.Value, existenciaMinima, request.Costo, ahora);
                    return;
                }
            }

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

        private async Task ActualizarMovimientoExistenciaInicialAsync(SqlConnection connection, SqlTransaction transaction, Guid idMovimiento, decimal cantidad, decimal? costoUnitario, DateTime ahora)
        {
            using SqlCommand command = new SqlCommand(@"
UPDATE dbo.ProductosServiciosMovimientosInventario
SET
    Cantidad = @Cantidad,
    ExistenciaAnterior = 0,
    ExistenciaPosterior = @ExistenciaPosterior,
    CostoUnitario = @CostoUnitario,
    Observaciones = 'Movimiento inicial ajustado durante la edición del producto inventariable.'
WHERE id = @Id", connection, transaction);

            command.Parameters.AddWithValue("@Id", idMovimiento);
            command.Parameters.AddWithValue("@Cantidad", cantidad);
            command.Parameters.AddWithValue("@ExistenciaPosterior", cantidad);
            command.Parameters.AddWithValue("@CostoUnitario", costoUnitario.HasValue ? costoUnitario.Value : DBNull.Value);
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

        private async Task<bool> ExisteNombreCatalogoAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, string nombre, Guid? excludeId, string tableName)
        {
            using SqlCommand command = new SqlCommand($@"
SELECT COUNT(1)
FROM {tableName}
WHERE idEmpresa = @IdEmpresa
  AND LTRIM(RTRIM(Nombre)) = @Nombre
  AND (@ExcludeId IS NULL OR id <> @ExcludeId)", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@Nombre", nombre.Trim());
            command.Parameters.AddWithValue("@ExcludeId", excludeId.HasValue ? excludeId.Value : DBNull.Value);
            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        private async Task<bool> ExisteNombreCategoriaAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, string nombre, byte aplicaA, Guid? excludeId)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT COUNT(1)
FROM dbo.ProductosServiciosCategorias
WHERE idEmpresa = @IdEmpresa
  AND LTRIM(RTRIM(Nombre)) = @Nombre
  AND AplicaA = @AplicaA
  AND (@ExcludeId IS NULL OR id <> @ExcludeId)", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@Nombre", nombre.Trim());
            command.Parameters.AddWithValue("@AplicaA", aplicaA);
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

            if ((request.ClaveProductoSat ?? string.Empty).Trim().Length > ClaveSatProductoLength)
            {
                return $"La clave SAT del producto no puede exceder {ClaveSatProductoLength} caracteres.";
            }

            if ((request.ClaveUnidadSat ?? string.Empty).Trim().Length > ClaveSatUnidadLength)
            {
                return $"La clave SAT de la unidad no puede exceder {ClaveSatUnidadLength} caracteres.";
            }

            if ((request.ObjetoImpuesto ?? string.Empty).Trim().Length > ObjetoImpuestoLength)
            {
                return $"El objeto de impuesto no puede exceder {ObjetoImpuestoLength} caracteres.";
            }

            if (request.PrecioPublico < 0)
            {
                return "El precio público no puede ser negativo.";
            }

            if (request.Costo.HasValue && request.Costo.Value < 0)
            {
                return "El costo no puede ser negativo.";
            }

            if (request.PrecioComparacion.HasValue && request.PrecioComparacion.Value < 0)
            {
                return "El precio de comparación no puede ser negativo.";
            }

            if (request.PrecioUnitarioMonto.HasValue && request.PrecioUnitarioMonto.Value < 0)
            {
                return "El precio unitario no puede ser negativo.";
            }

            bool tieneMontoUnitario = request.PrecioUnitarioMonto.HasValue;
            bool tieneBaseUnitaria = request.PrecioUnitarioBaseCantidad.HasValue;
            bool tieneUnidadBase = !string.IsNullOrWhiteSpace(request.PrecioUnitarioUnidad);
            if (tieneMontoUnitario || tieneBaseUnitaria || tieneUnidadBase)
            {
                if (!tieneMontoUnitario)
                {
                    return "Captura el importe total del precio unitario.";
                }

                if (!tieneBaseUnitaria)
                {
                    return "Captura la medida base del precio unitario.";
                }

                if (!tieneUnidadBase)
                {
                    return "Selecciona la unidad base del precio unitario.";
                }
            }

            if (request.PrecioUnitarioBaseCantidad.HasValue && request.PrecioUnitarioBaseCantidad.Value <= 0)
            {
                return "La base del precio unitario debe ser mayor que cero.";
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

            if (!request.EsProductoFisico &&
                (request.PesoKg.HasValue || request.LargoCm.HasValue || request.AnchoCm.HasValue || request.AltoCm.HasValue || request.IdPaquete.HasValue))
            {
                return "No envíes logística física para registros marcados como no físicos.";
            }

            if (request.PesoKg.HasValue && request.PesoKg.Value < 0)
            {
                return "El peso no puede ser negativo.";
            }

            if ((request.LargoCm.HasValue && request.LargoCm.Value < 0) ||
                (request.AnchoCm.HasValue && request.AnchoCm.Value < 0) ||
                (request.AltoCm.HasValue && request.AltoCm.Value < 0))
            {
                return "Las dimensiones no pueden ser negativas.";
            }

            if (request.Atributos.Any(a => a.IdAtributo == Guid.Empty))
            {
                return "Todos los atributos deben estar definidos.";
            }

            if (request.Atributos.Any(a => a.Valores == null || !a.Valores.Any()))
            {
                return "Cada asociación de atributo debe incluir al menos un elemento seleccionado.";
            }

            if (request.OpcionesVariante.Any(o => string.IsNullOrWhiteSpace(o.Nombre)))
            {
                return "Todas las opciones de variante deben tener nombre.";
            }

            if (request.Variantes.Any(v => string.IsNullOrWhiteSpace(v.ClaveCombinacion)))
            {
                return "Todas las variantes deben incluir una clave de combinación.";
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

        private static string ValidateAtributoValorCatalogoRequest(ProductoServicioAtributoValorCatalogoGuardarRequest request, Guid idEmpresa)
        {
            if (request.IdEmpresa == Guid.Empty || request.IdEmpresa != idEmpresa)
            {
                return "No fue posible resolver la empresa activa.";
            }

            if (request.IdAtributo == Guid.Empty)
            {
                return "Selecciona el atributo al que pertenecerá el elemento.";
            }

            if (string.IsNullOrWhiteSpace(request.Valor) || request.Valor.Trim().Length > AtributoValorLength)
            {
                return $"Captura un elemento válido de hasta {AtributoValorLength} caracteres.";
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

        private static ResolvedImageMutation ResolveImageMutation(string imagenUrl, string imagenNombre, PreparedImageOperation preparedImage)
        {
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

        private static ResolvedImageMutation ResolveImageMutation(ProductoServicioSnapshot? existente, PreparedImageOperation preparedImage)
        {
            return ResolveImageMutation(existente?.ImagenUrl ?? string.Empty, existente?.ImagenNombre ?? string.Empty, preparedImage);
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
                IdColeccion = request.IdColeccion.HasValue && request.IdColeccion.Value != Guid.Empty ? request.IdColeccion : null,
                IdPaquete = request.IdPaquete.HasValue && request.IdPaquete.Value != Guid.Empty ? request.IdPaquete : null,
                Costo = request.Costo,
                PrecioPublico = request.PrecioPublico,
                PrecioComparacion = request.PrecioComparacion,
                PrecioUnitarioMonto = request.PrecioUnitarioMonto,
                PrecioUnitarioBaseCantidad = request.PrecioUnitarioBaseCantidad,
                PrecioUnitarioUnidad = Truncate(request.PrecioUnitarioUnidad ?? string.Empty, PrecioUnitarioUnidadLength),
                ObjetoImpuesto = Truncate(request.ObjetoImpuesto ?? string.Empty, ObjetoImpuestoLength),
                ClaveProductoSat = Truncate(request.ClaveProductoSat ?? string.Empty, ClaveSatProductoLength),
                ClaveUnidadSat = Truncate(request.ClaveUnidadSat ?? string.Empty, ClaveSatUnidadLength),
                EsProductoFisico = request.EsProductoFisico,
                PesoKg = request.PesoKg,
                LargoCm = request.LargoCm,
                AnchoCm = request.AnchoCm,
                AltoCm = request.AltoCm,
                UsaNumeroSerie = request.UsaNumeroSerie,
                CausaInventario = request.CausaInventario,
                PermiteVentaSinExistencia = request.PermiteVentaSinExistencia,
                ExistenciaInicial = NormalizeInventoryInput(request.ExistenciaInicial),
                ExistenciaMinima = NormalizeInventoryInput(request.ExistenciaMinima),
                Activo = request.Activo,
                ImagenPrincipal = request.ImagenPrincipal,
                EliminarImagenPrincipal = request.EliminarImagenPrincipal,
                Atributos = request.Atributos ?? new List<ProductoServicioAtributoGuardarRequest>(),
                OpcionesVariante = request.OpcionesVariante ?? new List<ProductoServicioOpcionVarianteGuardarRequest>(),
                Variantes = request.Variantes ?? new List<ProductoServicioVarianteGuardarRequest>(),
                Multimedia = request.Multimedia ?? new List<ProductoServicioMultimediaGuardarRequest>()
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

            if (!normalized.EsProductoFisico)
            {
                normalized.IdPaquete = null;
                normalized.PesoKg = null;
                normalized.LargoCm = null;
                normalized.AnchoCm = null;
                normalized.AltoCm = null;
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
            command.Parameters.AddWithValue("@IdColeccion", request.IdColeccion.HasValue ? request.IdColeccion.Value : DBNull.Value);
            command.Parameters.AddWithValue("@IdPaquete", request.IdPaquete.HasValue ? request.IdPaquete.Value : DBNull.Value);
            command.Parameters.AddWithValue("@Costo", request.Costo.HasValue ? request.Costo.Value : DBNull.Value);
            command.Parameters.AddWithValue("@PrecioPublico", request.PrecioPublico);
            command.Parameters.AddWithValue("@PrecioComparacion", request.PrecioComparacion.HasValue ? request.PrecioComparacion.Value : DBNull.Value);
            command.Parameters.AddWithValue("@PrecioUnitarioMonto", request.PrecioUnitarioMonto.HasValue ? request.PrecioUnitarioMonto.Value : DBNull.Value);
            command.Parameters.AddWithValue("@PrecioUnitarioBaseCantidad", request.PrecioUnitarioBaseCantidad.HasValue ? request.PrecioUnitarioBaseCantidad.Value : DBNull.Value);
            command.Parameters.AddWithValue("@PrecioUnitarioUnidad", string.IsNullOrWhiteSpace(request.PrecioUnitarioUnidad) ? DBNull.Value : request.PrecioUnitarioUnidad);
            command.Parameters.AddWithValue("@ObjetoImpuesto", string.IsNullOrWhiteSpace(request.ObjetoImpuesto) ? DBNull.Value : request.ObjetoImpuesto);
            command.Parameters.AddWithValue("@ClaveProductoSat", string.IsNullOrWhiteSpace(request.ClaveProductoSat) ? DBNull.Value : request.ClaveProductoSat);
            command.Parameters.AddWithValue("@ClaveUnidadSat", string.IsNullOrWhiteSpace(request.ClaveUnidadSat) ? DBNull.Value : request.ClaveUnidadSat);
            command.Parameters.AddWithValue("@EsProductoFisico", request.EsProductoFisico);
            command.Parameters.AddWithValue("@PesoKg", request.PesoKg.HasValue ? request.PesoKg.Value : DBNull.Value);
            command.Parameters.AddWithValue("@LargoCm", request.LargoCm.HasValue ? request.LargoCm.Value : DBNull.Value);
            command.Parameters.AddWithValue("@AnchoCm", request.AnchoCm.HasValue ? request.AnchoCm.Value : DBNull.Value);
            command.Parameters.AddWithValue("@AltoCm", request.AltoCm.HasValue ? request.AltoCm.Value : DBNull.Value);
            command.Parameters.AddWithValue("@UsaNumeroSerie", request.UsaNumeroSerie);
            command.Parameters.AddWithValue("@CausaInventario", request.CausaInventario);
            command.Parameters.AddWithValue("@PermiteVentaSinExistencia", request.PermiteVentaSinExistencia);
            command.Parameters.AddWithValue("@ImagenUrl", string.IsNullOrWhiteSpace(imageMutation.ImagenUrl) ? DBNull.Value : imageMutation.ImagenUrl);
            command.Parameters.AddWithValue("@ImagenNombre", string.IsNullOrWhiteSpace(imageMutation.ImagenNombre) ? DBNull.Value : imageMutation.ImagenNombre);
            command.Parameters.AddWithValue("@Activo", request.Activo);
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

        private static void AddPaqueteParameters(SqlCommand command, Guid id, Guid idEmpresa, ProductoServicioPaqueteGuardarRequest request, DateTime ahora, bool includeIdentityKey)
        {
            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            if (includeIdentityKey)
            {
                command.Parameters.AddWithValue("@IdentityKey", Guid.NewGuid());
                command.Parameters.AddWithValue("@FechaCreacion", ahora);
            }

            command.Parameters.AddWithValue("@Nombre", request.Nombre.Trim());
            command.Parameters.AddWithValue("@TipoPaquete", request.TipoPaquete.Trim().ToLowerInvariant());
            command.Parameters.AddWithValue("@LargoCm", request.LargoCm.HasValue ? request.LargoCm.Value : DBNull.Value);
            command.Parameters.AddWithValue("@AnchoCm", request.AnchoCm.HasValue ? request.AnchoCm.Value : DBNull.Value);
            command.Parameters.AddWithValue("@AltoCm", request.AltoCm.HasValue ? request.AltoCm.Value : DBNull.Value);
            command.Parameters.AddWithValue("@PesoEmpaqueVacioKg", request.PesoEmpaqueVacioKg.HasValue ? request.PesoEmpaqueVacioKg.Value : DBNull.Value);
            command.Parameters.AddWithValue("@EsPredeterminado", request.EsPredeterminado);
            command.Parameters.AddWithValue("@FechaActualizacion", ahora);
        }

        private async Task<List<ProductoServicioCatalogoComboDto>> ObtenerColeccionesComboAsync(Guid idEmpresa, string busqueda = "")
        {
            using SqlConnection connection = CreateConnection();
            await connection.OpenAsync();
            StringBuilder query = new StringBuilder(@"
SELECT id, Numero AS Codigo, Nombre, ISNULL(Descripcion, '') AS Descripcion, Activo, CAST(NULL AS tinyint) AS AplicaA, '' AS Abreviatura, CAST(NULL AS bit) AS PermiteDecimales, Numero, '' AS TipoPaquete
FROM dbo.ProductosServiciosColecciones
WHERE idEmpresa = @IdEmpresa AND Activo = 1");
            using SqlCommand command = new SqlCommand();
            command.Connection = connection;
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                query.Append(" AND (Numero LIKE @Busqueda OR Nombre LIKE @Busqueda OR ISNULL(Descripcion, '') LIKE @Busqueda)");
                command.Parameters.AddWithValue("@Busqueda", $"%{busqueda.Trim()}%");
            }

            query.Append(" ORDER BY Nombre, Numero");
            command.CommandText = query.ToString();
            List<ProductoServicioCatalogoComboDto> items = new List<ProductoServicioCatalogoComboDto>();
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(MapCatalogoCombo(reader));
            }

            return items;
        }

        private async Task<List<ProductoServicioCatalogoComboDto>> ObtenerPaquetesComboAsync(Guid idEmpresa, string busqueda = "")
        {
            using SqlConnection connection = CreateConnection();
            await connection.OpenAsync();
            StringBuilder query = new StringBuilder(@"
SELECT id, Nombre AS Codigo, Nombre, N'' AS Descripcion, Activo, CAST(NULL AS tinyint) AS AplicaA, '' AS Abreviatura, CAST(NULL AS bit) AS PermiteDecimales, '' AS Numero, TipoPaquete,
       LargoCm, AnchoCm, AltoCm, PesoEmpaqueVacioKg, EsPredeterminado
FROM dbo.ProductosServiciosPaquetes
WHERE idEmpresa = @IdEmpresa AND Activo = 1");
            using SqlCommand command = new SqlCommand();
            command.Connection = connection;
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                query.Append(" AND Nombre LIKE @Busqueda");
                command.Parameters.AddWithValue("@Busqueda", $"%{busqueda.Trim()}%");
            }

            query.Append(" ORDER BY EsPredeterminado DESC, Nombre");
            command.CommandText = query.ToString();
            List<ProductoServicioCatalogoComboDto> items = new List<ProductoServicioCatalogoComboDto>();
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(MapCatalogoCombo(reader));
            }

            return items;
        }

        private async Task<List<ProductoServicioCatalogoComboDto>> ObtenerAtributosComboAsync(Guid idEmpresa, string busqueda = "")
        {
            using SqlConnection connection = CreateConnection();
            await connection.OpenAsync();
            StringBuilder query = new StringBuilder(@"
SELECT id, Nombre AS Codigo, Nombre, N'' AS Descripcion, Activo, CAST(NULL AS tinyint) AS AplicaA, '' AS Abreviatura, CAST(NULL AS bit) AS PermiteDecimales, '' AS Numero, '' AS TipoPaquete
FROM dbo.ProductosServiciosAtributos
WHERE idEmpresa = @IdEmpresa AND Activo = 1");
            using SqlCommand command = new SqlCommand();
            command.Connection = connection;
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                query.Append(" AND Nombre LIKE @Busqueda");
                command.Parameters.AddWithValue("@Busqueda", $"%{busqueda.Trim()}%");
            }

            query.Append(" ORDER BY Nombre");
            command.CommandText = query.ToString();
            List<ProductoServicioCatalogoComboDto> items = new List<ProductoServicioCatalogoComboDto>();
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(MapCatalogoCombo(reader));
            }

            return items;
        }

        private async Task<bool> ExisteNumeroColeccionAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, string numero, Guid? excludeId)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT COUNT(1)
FROM dbo.ProductosServiciosColecciones
WHERE idEmpresa = @IdEmpresa AND Numero = @Numero AND (@ExcludeId IS NULL OR id <> @ExcludeId)", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@Numero", numero);
            command.Parameters.AddWithValue("@ExcludeId", excludeId.HasValue ? excludeId.Value : DBNull.Value);
            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        private async Task<bool> ExisteNombreColeccionAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, string nombre, Guid? excludeId)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT COUNT(1)
FROM dbo.ProductosServiciosColecciones
WHERE idEmpresa = @IdEmpresa
  AND LTRIM(RTRIM(Nombre)) = @Nombre
  AND (@ExcludeId IS NULL OR id <> @ExcludeId)", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@Nombre", nombre);
            command.Parameters.AddWithValue("@ExcludeId", excludeId.HasValue ? excludeId.Value : DBNull.Value);
            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        private async Task<bool> ExisteNombreAtributoAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, string nombre, Guid? excludeId)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT COUNT(1)
FROM dbo.ProductosServiciosAtributos
WHERE idEmpresa = @IdEmpresa AND Nombre = @Nombre AND (@ExcludeId IS NULL OR id <> @ExcludeId)", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@Nombre", nombre);
            command.Parameters.AddWithValue("@ExcludeId", excludeId.HasValue ? excludeId.Value : DBNull.Value);
            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        private async Task<bool> ExisteAtributoActivoAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idAtributo)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT COUNT(1)
FROM dbo.ProductosServiciosAtributos
WHERE idEmpresa = @IdEmpresa AND id = @Id AND Activo = 1", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@Id", idAtributo);
            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        private async Task<bool> ExisteValorAtributoAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idAtributo, string valor, Guid? excludeId)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT COUNT(1)
FROM dbo.ProductosServiciosAtributosValores
WHERE idEmpresa = @IdEmpresa
  AND idAtributo = @IdAtributo
  AND Valor = @Valor
  AND (@ExcludeId IS NULL OR id <> @ExcludeId)", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@IdAtributo", idAtributo);
            command.Parameters.AddWithValue("@Valor", valor);
            command.Parameters.AddWithValue("@ExcludeId", excludeId.HasValue ? excludeId.Value : DBNull.Value);
            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        private async Task<bool> ExistePaqueteMismoNombreTipoAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, string nombre, string tipoPaquete, Guid? excludeId)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT COUNT(1)
FROM dbo.ProductosServiciosPaquetes
WHERE idEmpresa = @IdEmpresa
  AND Nombre = @Nombre
  AND TipoPaquete = @TipoPaquete
  AND (@ExcludeId IS NULL OR id <> @ExcludeId)", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@Nombre", nombre);
            command.Parameters.AddWithValue("@TipoPaquete", tipoPaquete.Trim().ToLowerInvariant());
            command.Parameters.AddWithValue("@ExcludeId", excludeId.HasValue ? excludeId.Value : DBNull.Value);
            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        private static string ValidateColeccionRequest(ProductoServicioColeccionGuardarRequest request, Guid idEmpresa)
        {
            if (request.IdEmpresa == Guid.Empty || request.IdEmpresa != idEmpresa)
            {
                return "No fue posible resolver la empresa activa.";
            }

            if (string.IsNullOrWhiteSpace(request.Numero) || request.Numero.Trim().Length > NumeroColeccionLength)
            {
                return $"Captura un número válido de hasta {NumeroColeccionLength} caracteres.";
            }

            if (string.IsNullOrWhiteSpace(request.Nombre) || request.Nombre.Trim().Length > NombreLength)
            {
                return $"Captura un nombre válido de hasta {NombreLength} caracteres.";
            }

            if ((request.Descripcion ?? string.Empty).Trim().Length > DescripcionCatalogoLength)
            {
                return $"La descripción no puede exceder {DescripcionCatalogoLength} caracteres.";
            }

            return string.Empty;
        }

        private static string ValidatePaqueteRequest(ProductoServicioPaqueteGuardarRequest request, Guid idEmpresa)
        {
            if (request.IdEmpresa == Guid.Empty || request.IdEmpresa != idEmpresa)
            {
                return "No fue posible resolver la empresa activa.";
            }

            if (string.IsNullOrWhiteSpace(request.Nombre) || request.Nombre.Trim().Length > NombreLength)
            {
                return $"Captura un nombre válido de hasta {NombreLength} caracteres.";
            }

            string tipo = (request.TipoPaquete ?? string.Empty).Trim().ToLowerInvariant();
            if (tipo != "caja" && tipo != "sobre" && tipo != "flexible")
            {
                return "Selecciona un tipo de paquete válido.";
            }

            if ((request.LargoCm.HasValue && request.LargoCm.Value < 0) ||
                (request.AnchoCm.HasValue && request.AnchoCm.Value < 0) ||
                (request.AltoCm.HasValue && request.AltoCm.Value < 0) ||
                (request.PesoEmpaqueVacioKg.HasValue && request.PesoEmpaqueVacioKg.Value < 0))
            {
                return "Las medidas y el peso del paquete no pueden ser negativos.";
            }

            return string.Empty;
        }

        private static string ValidateAtributoCatalogoRequest(ProductoServicioAtributoCatalogoGuardarRequest request, Guid idEmpresa)
        {
            if (request.IdEmpresa == Guid.Empty || request.IdEmpresa != idEmpresa)
            {
                return "No fue posible resolver la empresa activa.";
            }

            if (string.IsNullOrWhiteSpace(request.Nombre) || request.Nombre.Trim().Length > AtributoNombreLength)
            {
                return $"Captura un atributo válido de hasta {AtributoNombreLength} caracteres.";
            }

            return string.Empty;
        }

        private async Task<List<ProductoServicioOpcionDto>> BuscarClavesProductoSatAsync(string[] terms, int top)
        {
            string baseUrl = GetSatCatalogosApiBaseUrl();
            return await GetSatCatalogOptionsFromExternalApiByTermsAsync(
                baseUrl,
                "GetClaveProdServ4",
                terms,
                new[] { "ClaveProdServ", "c_ClaveProdServ", "claveprodserv", "clave" },
                new[] { "Descripción", "descripcion", "Nombre", "nombre", "desc" },
                top);
        }

        private async Task<List<ProductoServicioOpcionDto>> BuscarClavesUnidadSatAsync(string q, int top)
        {
            string baseUrl = GetSatCatalogosApiBaseUrl();
            List<ProductoServicioOpcionDto> items = await GetSatCatalogOptionsFromExternalApiAsync(
                baseUrl,
                "GetTodoClaveUnidad",
                null,
                new[] { "ClaveUnidad", "Nombre", "c_ClaveUnidad", "claveunidad", "clave" },
                new[] { "Valor", "Descripción", "descripcion", "desc", "Nombre" },
                Math.Max(top * 3, 120));

            if (!items.Any(item => string.Equals(item.Clave, "H87", StringComparison.OrdinalIgnoreCase)))
            {
                items.Insert(0, new ProductoServicioOpcionDto
                {
                    Clave = "H87",
                    Nombre = "H87 - Pieza"
                });
            }

            string filtro = (q ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(filtro))
            {
                items = items
                    .Where(item => item.Clave.Contains(filtro, StringComparison.OrdinalIgnoreCase)
                        || item.Nombre.Contains(filtro, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return items
                .GroupBy(item => item.Clave, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .Take(top)
                .ToList();
        }

        private string GetSatCatalogosApiBaseUrl()
        {
            string raw = _configuration["SatCatalogosApi:BaseUrl"] ?? string.Empty;
            if (string.IsNullOrWhiteSpace(raw))
            {
                raw = "http://checkapp-001-site10.htempurl.com";
            }

            return raw.Trim().TrimEnd('/');
        }

        private static string[] ParseSatTerms(string q)
        {
            return (q ?? string.Empty)
                .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .SelectMany(fragment => fragment.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .Select(fragment => fragment.Trim())
                .Where(fragment => !string.IsNullOrWhiteSpace(fragment))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(12)
                .ToArray();
        }

        private static async Task<List<ProductoServicioOpcionDto>> GetSatCatalogOptionsFromExternalApiByTermsAsync(
            string baseUrl,
            string route,
            string[] terms,
            string[] keyCandidates,
            string[] descCandidates,
            int top)
        {
            List<ProductoServicioOpcionDto> merged = new List<ProductoServicioOpcionDto>();
            if (terms == null || terms.Length == 0)
            {
                return merged;
            }

            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string term in terms)
            {
                List<ProductoServicioOpcionDto> partial = await GetSatCatalogOptionsFromExternalApiAsync(baseUrl, route, term, keyCandidates, descCandidates, top);
                foreach (ProductoServicioOpcionDto item in partial)
                {
                    if (string.IsNullOrWhiteSpace(item.Clave) || !seen.Add(item.Clave))
                    {
                        continue;
                    }

                    merged.Add(item);
                    if (merged.Count >= top)
                    {
                        return merged;
                    }
                }
            }

            return merged;
        }

        private static async Task<List<ProductoServicioOpcionDto>> GetSatCatalogOptionsFromExternalApiAsync(
            string baseUrl,
            string route,
            string? termino,
            string[] keyCandidates,
            string[] descCandidates,
            int top)
        {
            List<ProductoServicioOpcionDto> list = new List<ProductoServicioOpcionDto>();
            if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(route))
            {
                return list;
            }

            string safeRoute = route.Trim().Trim('/');
            string url = string.IsNullOrWhiteSpace(termino)
                ? $"{baseUrl}/api/Catalogos/{safeRoute}"
                : $"{baseUrl}/api/Catalogos/{safeRoute}/{Uri.EscapeDataString(termino.Trim())}";

            using HttpClient http = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            using HttpResponseMessage response = await http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return list;
                }

                string detail = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"SAT API {safeRoute} respondió {(int)response.StatusCode}: {detail}");
            }

            string json = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(json))
            {
                return list;
            }

            using JsonDocument document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return list;
            }

            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (JsonElement row in document.RootElement.EnumerateArray())
            {
                if (row.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                string clave = GetJsonValueByCandidates(row, keyCandidates);
                if (string.IsNullOrWhiteSpace(clave) || !seen.Add(clave))
                {
                    continue;
                }

                string descripcion = GetJsonValueByCandidates(row, descCandidates);
                list.Add(new ProductoServicioOpcionDto
                {
                    Clave = clave,
                    Nombre = string.IsNullOrWhiteSpace(descripcion) ? clave : $"{clave} - {descripcion}"
                });

                if (list.Count >= top)
                {
                    break;
                }
            }

            return list;
        }

        private static string GetJsonValueByCandidates(JsonElement obj, string[] candidates)
        {
            if (obj.ValueKind != JsonValueKind.Object || candidates == null || candidates.Length == 0)
            {
                return string.Empty;
            }

            foreach (string candidate in candidates)
            {
                if (string.IsNullOrWhiteSpace(candidate) || !TryGetPropertyIgnoreCase(obj, candidate, out JsonElement value))
                {
                    continue;
                }

                string text = JsonElementToString(value);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text.Trim();
                }
            }

            return string.Empty;
        }

        private static bool TryGetPropertyIgnoreCase(JsonElement obj, string name, out JsonElement value)
        {
            if (obj.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty property in obj.EnumerateObject())
                {
                    if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        value = property.Value;
                        return true;
                    }
                }
            }

            value = default;
            return false;
        }

        private static string JsonElementToString(JsonElement value)
        {
            return value.ValueKind switch
            {
                JsonValueKind.String => value.GetString() ?? string.Empty,
                JsonValueKind.Number => value.ToString(),
                JsonValueKind.True => bool.TrueString,
                JsonValueKind.False => bool.FalseString,
                _ => string.Empty
            };
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

        private async Task<UploadedImagePayload> MoveTemporalImageToFinalAsync(string empresaStorageKey, Guid productoId, TemporalImageTokenPayload temporal, string finalFolderName)
        {
            byte[] fileBytes = await DownloadFirebaseFileAsync(temporal.FolderName, temporal.NombreAlmacenado);
            return await UploadImageToFirebaseAsync(
                finalFolderName,
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

        private async Task<List<ProductoServicioAtributoSeleccionDto>> ObtenerAtributosProductoAsync(SqlConnection connection, Guid idEmpresa, Guid idProductoServicio)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT
    ppa.id,
    ppa.idAtributo,
    a.Nombre,
    ppa.Orden,
    av.id AS idAtributoValor,
    av.Valor,
    pav.Orden AS OrdenValor
FROM dbo.ProductosServiciosProductoAtributos ppa
INNER JOIN dbo.ProductosServiciosAtributos a
    ON a.idEmpresa = ppa.idEmpresa AND a.id = ppa.idAtributo
LEFT JOIN dbo.ProductosServiciosProductoAtributoValores pav
    ON pav.idEmpresa = ppa.idEmpresa AND pav.idProductoAtributo = ppa.id AND pav.Activo = 1
LEFT JOIN dbo.ProductosServiciosAtributosValores av
    ON av.idEmpresa = pav.idEmpresa AND av.id = pav.idAtributoValor
WHERE ppa.idEmpresa = @IdEmpresa AND ppa.idProductoServicio = @IdProductoServicio AND ppa.Activo = 1
ORDER BY ppa.Orden, a.Nombre, pav.Orden, av.Valor", connection);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);

            Dictionary<Guid, ProductoServicioAtributoSeleccionDto> lookup = new Dictionary<Guid, ProductoServicioAtributoSeleccionDto>();
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                Guid idProductoAtributo = ReadGuid(reader, "id");
                if (!lookup.TryGetValue(idProductoAtributo, out ProductoServicioAtributoSeleccionDto? current))
                {
                    current = new ProductoServicioAtributoSeleccionDto
                    {
                        IdProductoAtributo = idProductoAtributo,
                        IdAtributo = ReadGuid(reader, "idAtributo"),
                        Nombre = ReadString(reader, "Nombre"),
                        Orden = ReadInt(reader, "Orden")
                    };
                    lookup[idProductoAtributo] = current;
                }

                Guid idAtributoValor = ReadGuid(reader, "idAtributoValor");
                if (idAtributoValor != Guid.Empty)
                {
                    current.Valores.Add(new ProductoServicioAtributoValorSeleccionDto
                    {
                        IdAtributoValor = idAtributoValor,
                        Valor = ReadString(reader, "Valor"),
                        Orden = ReadInt(reader, "OrdenValor")
                    });
                }
            }

            return lookup.Values.OrderBy(x => x.Orden).ToList();
        }

        private async Task<List<ProductoServicioOpcionVarianteDto>> ObtenerOpcionesVarianteProductoAsync(SqlConnection connection, Guid idEmpresa, Guid idProductoServicio)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT
    ov.id,
    ov.idProductoServicio,
    ov.Nombre,
    ov.Orden,
    ov.Activo,
    ovv.id AS idOpcionVarianteValor,
    ovv.Valor,
    ovv.Orden AS OrdenValor,
    ovv.Activo AS ValorActivo
FROM dbo.ProductosServiciosOpcionesVariante ov
LEFT JOIN dbo.ProductosServiciosOpcionesVarianteValores ovv
    ON ovv.idEmpresa = ov.idEmpresa AND ovv.idOpcionVariante = ov.id AND ovv.Activo = 1
WHERE ov.idEmpresa = @IdEmpresa AND ov.idProductoServicio = @IdProductoServicio AND ov.Activo = 1
ORDER BY ov.Orden, ov.Nombre, ovv.Orden, ovv.Valor", connection);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);

            Dictionary<Guid, ProductoServicioOpcionVarianteDto> lookup = new Dictionary<Guid, ProductoServicioOpcionVarianteDto>();
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                Guid idOpcion = ReadGuid(reader, "id");
                if (!lookup.TryGetValue(idOpcion, out ProductoServicioOpcionVarianteDto? current))
                {
                    current = new ProductoServicioOpcionVarianteDto
                    {
                        Id = idOpcion,
                        IdProductoServicio = ReadGuid(reader, "idProductoServicio"),
                        Nombre = ReadString(reader, "Nombre"),
                        Orden = ReadInt(reader, "Orden"),
                        Activo = ReadBool(reader, "Activo")
                    };
                    lookup[idOpcion] = current;
                }

                Guid idValor = ReadGuid(reader, "idOpcionVarianteValor");
                if (idValor != Guid.Empty)
                {
                    current.Valores.Add(new ProductoServicioOpcionVarianteValorDto
                    {
                        Id = idValor,
                        IdOpcionVariante = idOpcion,
                        Valor = ReadString(reader, "Valor"),
                        Orden = ReadInt(reader, "OrdenValor"),
                        Activo = ReadBool(reader, "ValorActivo")
                    });
                }
            }

            return lookup.Values.OrderBy(x => x.Orden).ToList();
        }

        private async Task<List<ProductoServicioVarianteDto>> ObtenerVariantesProductoAsync(SqlConnection connection, Guid idEmpresa, Guid idProductoServicio, SqlTransaction? transaction = null)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT
    pv.id,
    pv.idProductoServicio,
    ISNULL(pv.Sku, '') AS Sku,
    pv.Nombre,
    pv.ClaveCombinacion,
    ISNULL(pv.ImagenUrl, '') AS ImagenUrl,
    ISNULL(pv.ImagenNombre, '') AS ImagenNombre,
    pv.PrecioPublico,
    pv.PrecioComparacion,
    pv.PrecioUnitarioMonto,
    pv.PrecioUnitarioBaseCantidad,
    ISNULL(pv.PrecioUnitarioUnidad, '') AS PrecioUnitarioUnidad,
    pv.Orden,
    pv.Activo,
    vv.idOpcionVariante,
    ov.Nombre AS Opcion,
    vv.idOpcionVarianteValor,
    av.Valor,
    vv.Orden AS OrdenValor
FROM dbo.ProductosServiciosVariantes pv
LEFT JOIN dbo.ProductosServiciosVarianteValores vv
    ON vv.idEmpresa = pv.idEmpresa AND vv.idVariante = pv.id
LEFT JOIN dbo.ProductosServiciosOpcionesVariante ov
    ON ov.idEmpresa = vv.idEmpresa AND ov.id = vv.idOpcionVariante
LEFT JOIN dbo.ProductosServiciosOpcionesVarianteValores av
    ON av.idEmpresa = vv.idEmpresa AND av.id = vv.idOpcionVarianteValor
WHERE pv.idEmpresa = @IdEmpresa AND pv.idProductoServicio = @IdProductoServicio AND pv.Activo = 1
ORDER BY pv.Orden, pv.Nombre, vv.Orden", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);

            Dictionary<Guid, ProductoServicioVarianteDto> lookup = new Dictionary<Guid, ProductoServicioVarianteDto>();
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                Guid idVariante = ReadGuid(reader, "id");
                if (!lookup.TryGetValue(idVariante, out ProductoServicioVarianteDto? current))
                {
                    current = new ProductoServicioVarianteDto
                    {
                        Id = idVariante,
                        IdProductoServicio = ReadGuid(reader, "idProductoServicio"),
                        Sku = ReadString(reader, "Sku"),
                        Nombre = ReadString(reader, "Nombre"),
                        ClaveCombinacion = ReadString(reader, "ClaveCombinacion"),
                        ImagenUrl = ReadString(reader, "ImagenUrl"),
                        ImagenNombre = ReadString(reader, "ImagenNombre"),
                        PrecioPublico = ReadNullableDecimal(reader, "PrecioPublico"),
                        PrecioComparacion = ReadNullableDecimal(reader, "PrecioComparacion"),
                        PrecioUnitarioMonto = ReadNullableDecimal(reader, "PrecioUnitarioMonto"),
                        PrecioUnitarioBaseCantidad = ReadNullableDecimal(reader, "PrecioUnitarioBaseCantidad"),
                        PrecioUnitarioUnidad = ReadString(reader, "PrecioUnitarioUnidad"),
                        Orden = ReadInt(reader, "Orden"),
                        Activo = ReadBool(reader, "Activo")
                    };
                    lookup[idVariante] = current;
                }

                Guid idOpcionVariante = ReadGuid(reader, "idOpcionVariante");
                Guid idOpcionVarianteValor = ReadGuid(reader, "idOpcionVarianteValor");
                if (idOpcionVariante != Guid.Empty && idOpcionVarianteValor != Guid.Empty)
                {
                    current.Valores.Add(new ProductoServicioVarianteValorDto
                    {
                        IdOpcionVariante = idOpcionVariante,
                        Opcion = ReadString(reader, "Opcion"),
                        IdOpcionVarianteValor = idOpcionVarianteValor,
                        Valor = ReadString(reader, "Valor"),
                        Orden = ReadInt(reader, "OrdenValor")
                    });
                }
            }

            return lookup.Values.OrderBy(x => x.Orden).ToList();
        }

        private async Task<List<ProductoServicioMultimediaDto>> ObtenerMultimediaProductoAsync(SqlConnection connection, Guid idEmpresa, Guid idProductoServicio, SqlTransaction? transaction = null)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT id, idProductoServicio, TipoMultimedia, Foto, Video, Documento, NombreOriginal, NombreAlmacenado, Extension, MimeType, UrlFirebase, PesoBytes, Orden, Activo, FechaCreacion, FechaActualizacion
FROM dbo.ProductosServiciosMultimedia
WHERE idEmpresa = @IdEmpresa AND idProductoServicio = @IdProductoServicio AND Activo = 1
ORDER BY CASE WHEN Foto = 1 THEN 1 WHEN Video = 1 THEN 2 ELSE 3 END, Orden, FechaCreacion", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);

            List<ProductoServicioMultimediaDto> items = new List<ProductoServicioMultimediaDto>();
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new ProductoServicioMultimediaDto
                {
                    Id = ReadGuid(reader, "id"),
                    IdProductoServicio = ReadGuid(reader, "idProductoServicio"),
                    TipoMultimedia = ReadString(reader, "TipoMultimedia"),
                    Foto = ReadBool(reader, "Foto"),
                    Video = ReadBool(reader, "Video"),
                    Documento = ReadBool(reader, "Documento"),
                    NombreOriginal = ReadString(reader, "NombreOriginal"),
                    NombreAlmacenado = ReadString(reader, "NombreAlmacenado"),
                    Extension = ReadString(reader, "Extension"),
                    MimeType = ReadString(reader, "MimeType"),
                    UrlFirebase = ReadString(reader, "UrlFirebase"),
                    PesoBytes = ReadLong(reader, "PesoBytes"),
                    Orden = ReadInt(reader, "Orden"),
                    Activo = ReadBool(reader, "Activo"),
                    FechaCreacion = ReadDateTime(reader, "FechaCreacion"),
                    FechaActualizacion = ReadDateTime(reader, "FechaActualizacion")
                });
            }

            return items;
        }

        private async Task<PreparedMultimediaOperation> PrepareMultimediaOperationAsync(RequestContext context, Guid productoId, NormalizedProductoServicioRequest request)
        {
            PreparedMultimediaOperation operation = new PreparedMultimediaOperation();
            foreach (ProductoServicioMultimediaGuardarRequest item in request.Multimedia ?? new List<ProductoServicioMultimediaGuardarRequest>())
            {
                string tipo = NormalizeTipoMultimedia(item.TipoMultimedia);
                if (string.IsNullOrWhiteSpace(tipo))
                {
                    continue;
                }

                if (item.Id.HasValue && item.Id.Value != Guid.Empty)
                {
                    operation.FinalItems.Add(new ProductoServicioMultimediaDto
                    {
                        Id = item.Id.Value,
                        IdProductoServicio = productoId,
                        TipoMultimedia = tipo,
                        Foto = tipo == "foto",
                        Video = tipo == "video",
                        Documento = tipo == "documento",
                        NombreOriginal = item.NombreOriginal,
                        NombreAlmacenado = item.NombreAlmacenado,
                        Extension = item.Extension,
                        MimeType = item.MimeType,
                        UrlFirebase = item.UrlFirebase,
                        PesoBytes = item.PesoBytes,
                        Orden = item.Orden,
                        Activo = true,
                        FechaCreacion = DateTime.UtcNow,
                        FechaActualizacion = DateTime.UtcNow
                    });
                    continue;
                }

                if (string.IsNullOrWhiteSpace(item.TemporalToken))
                {
                    continue;
                }

                TemporalImageTokenPayload temporal = TryParseTemporalToken(item.TemporalToken)
                    ?? throw new InvalidOperationException("Se detectó una referencia temporal inválida.");
                if (!FolderBelongsToEmpresa(temporal.FolderName, context.EmpresaStorageKey))
                {
                    throw new InvalidOperationException("La evidencia temporal no pertenece a la empresa activa.");
                }

                UploadedImagePayload uploaded = await MoveTemporalImageToFinalAsync(
                    context.EmpresaStorageKey,
                    productoId,
                    temporal,
                    BuildFinalMultimediaFolderName(context.EmpresaStorageKey, productoId, tipo));

                operation.FinalItems.Add(new ProductoServicioMultimediaDto
                {
                    Id = Guid.NewGuid(),
                    IdProductoServicio = productoId,
                    TipoMultimedia = tipo,
                    Foto = tipo == "foto",
                    Video = tipo == "video",
                    Documento = tipo == "documento",
                    NombreOriginal = uploaded.NombreOriginal,
                    NombreAlmacenado = uploaded.NombreAlmacenado,
                    Extension = uploaded.Extension,
                    MimeType = uploaded.MimeType,
                    UrlFirebase = uploaded.UrlFirebase,
                    PesoBytes = uploaded.PesoBytes,
                    Orden = item.Orden,
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow,
                    FechaActualizacion = DateTime.UtcNow
                });

                operation.TemporalCleanups.Add(new FirebaseCleanupItem
                {
                    FolderName = temporal.FolderName,
                    StoredName = temporal.NombreAlmacenado
                });

                operation.NewFileCompensations.Add(new FirebaseCleanupItem
                {
                    FolderName = uploaded.FolderName,
                    StoredName = uploaded.NombreAlmacenado
                });
            }

            return operation;
        }

        private async Task FinalizeMultimediaOperationAfterCommitAsync(PreparedMultimediaOperation operation)
        {
            await CleanupUploadedFirebaseFilesAsync(operation.TemporalCleanups);
        }

        private async Task CompensatePreparedMultimediaAsync(PreparedMultimediaOperation operation)
        {
            await CleanupUploadedFirebaseFilesAsync(operation.NewFileCompensations);
        }

        private async Task SynchronizeProductoMultimediaAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idProductoServicio, List<ProductoServicioMultimediaDto> multimediaFinal, DateTime ahora)
        {
            List<ProductoServicioMultimediaDto> actual = await ObtenerMultimediaProductoAsync(connection, idEmpresa, idProductoServicio, transaction);
            HashSet<Guid> finalIds = multimediaFinal.Select(x => x.Id).ToHashSet();
            if (finalIds.Count == 0)
            {
                using SqlCommand deactivateAll = new SqlCommand(@"
UPDATE dbo.ProductosServiciosMultimedia
SET Activo = 0, FechaActualizacion = @FechaActualizacion
WHERE idEmpresa = @IdEmpresa AND idProductoServicio = @IdProductoServicio", connection, transaction);
                deactivateAll.Parameters.AddWithValue("@FechaActualizacion", ahora);
                deactivateAll.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                deactivateAll.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);
                await deactivateAll.ExecuteNonQueryAsync();
            }
            else
            {
                using SqlCommand deactivate = new SqlCommand(@"
UPDATE dbo.ProductosServiciosMultimedia
SET Activo = 0, FechaActualizacion = @FechaActualizacion
WHERE idEmpresa = @IdEmpresa AND idProductoServicio = @IdProductoServicio AND id NOT IN (SELECT TRY_CONVERT(uniqueidentifier, value) FROM STRING_SPLIT(@IdsCsv, ',') WHERE TRY_CONVERT(uniqueidentifier, value) IS NOT NULL)", connection, transaction);
                deactivate.Parameters.AddWithValue("@FechaActualizacion", ahora);
                deactivate.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                deactivate.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);
                deactivate.Parameters.AddWithValue("@IdsCsv", string.Join(",", finalIds.Select(x => x.ToString())));
                await deactivate.ExecuteNonQueryAsync();
            }

            foreach (ProductoServicioMultimediaDto item in multimediaFinal)
            {
                bool exists = actual.Any(x => x.Id == item.Id);
                if (exists)
                {
                    using SqlCommand update = new SqlCommand(@"
UPDATE dbo.ProductosServiciosMultimedia
SET Orden = @Orden, Activo = 1, FechaActualizacion = @FechaActualizacion
WHERE idEmpresa = @IdEmpresa AND id = @Id", connection, transaction);
                    update.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                    update.Parameters.AddWithValue("@Id", item.Id);
                    update.Parameters.AddWithValue("@Orden", item.Orden);
                    update.Parameters.AddWithValue("@FechaActualizacion", ahora);
                    await update.ExecuteNonQueryAsync();
                }
                else
                {
                    using SqlCommand insert = new SqlCommand(@"
INSERT INTO dbo.ProductosServiciosMultimedia
    (id, idEmpresa, identityKey, idProductoServicio, TipoMultimedia, Foto, Video, Documento, NombreOriginal, NombreAlmacenado, Extension, MimeType, UrlFirebase, PesoBytes, Orden, Activo, FechaCreacion, FechaActualizacion)
VALUES
    (@Id, @IdEmpresa, @IdentityKey, @IdProductoServicio, @TipoMultimedia, @Foto, @Video, @Documento, @NombreOriginal, @NombreAlmacenado, @Extension, @MimeType, @UrlFirebase, @PesoBytes, @Orden, 1, @FechaCreacion, @FechaActualizacion)", connection, transaction);
                    insert.Parameters.AddWithValue("@Id", item.Id);
                    insert.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                    insert.Parameters.AddWithValue("@IdentityKey", Guid.NewGuid());
                    insert.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);
                    insert.Parameters.AddWithValue("@TipoMultimedia", item.TipoMultimedia);
                    insert.Parameters.AddWithValue("@Foto", item.Foto);
                    insert.Parameters.AddWithValue("@Video", item.Video);
                    insert.Parameters.AddWithValue("@Documento", item.Documento);
                    insert.Parameters.AddWithValue("@NombreOriginal", item.NombreOriginal);
                    insert.Parameters.AddWithValue("@NombreAlmacenado", item.NombreAlmacenado);
                    insert.Parameters.AddWithValue("@Extension", item.Extension);
                    insert.Parameters.AddWithValue("@MimeType", item.MimeType);
                    insert.Parameters.AddWithValue("@UrlFirebase", item.UrlFirebase);
                    insert.Parameters.AddWithValue("@PesoBytes", item.PesoBytes);
                    insert.Parameters.AddWithValue("@Orden", item.Orden);
                    insert.Parameters.AddWithValue("@FechaCreacion", ahora);
                    insert.Parameters.AddWithValue("@FechaActualizacion", ahora);
                    await insert.ExecuteNonQueryAsync();
                }
            }
        }

        private async Task SynchronizeProductoAtributosAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idProductoServicio, List<ProductoServicioAtributoGuardarRequest> atributos, DateTime ahora)
        {
            using SqlCommand deleteValores = new SqlCommand(@"
DELETE pav
FROM dbo.ProductosServiciosProductoAtributoValores pav
INNER JOIN dbo.ProductosServiciosProductoAtributos ppa ON ppa.idEmpresa = pav.idEmpresa AND ppa.id = pav.idProductoAtributo
WHERE pav.idEmpresa = @IdEmpresa AND ppa.idProductoServicio = @IdProductoServicio", connection, transaction);
            deleteValores.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            deleteValores.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);
            await deleteValores.ExecuteNonQueryAsync();

            using SqlCommand deleteAtributos = new SqlCommand(@"
DELETE FROM dbo.ProductosServiciosProductoAtributos
WHERE idEmpresa = @IdEmpresa AND idProductoServicio = @IdProductoServicio", connection, transaction);
            deleteAtributos.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            deleteAtributos.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);
            await deleteAtributos.ExecuteNonQueryAsync();

            foreach (ProductoServicioAtributoGuardarRequest atributo in atributos.OrderBy(x => x.Orden))
            {
                Guid idProductoAtributo = Guid.NewGuid();
                using SqlCommand insert = new SqlCommand(@"
INSERT INTO dbo.ProductosServiciosProductoAtributos
    (id, idEmpresa, identityKey, idProductoServicio, idAtributo, Orden, Activo, FechaCreacion, FechaActualizacion)
VALUES
    (@Id, @IdEmpresa, @IdentityKey, @IdProductoServicio, @IdAtributo, @Orden, 1, @FechaCreacion, @FechaActualizacion)", connection, transaction);
                insert.Parameters.AddWithValue("@Id", idProductoAtributo);
                insert.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                insert.Parameters.AddWithValue("@IdentityKey", Guid.NewGuid());
                insert.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);
                insert.Parameters.AddWithValue("@IdAtributo", atributo.IdAtributo);
                insert.Parameters.AddWithValue("@Orden", atributo.Orden);
                insert.Parameters.AddWithValue("@FechaCreacion", ahora);
                insert.Parameters.AddWithValue("@FechaActualizacion", ahora);
                await insert.ExecuteNonQueryAsync();

                foreach (ProductoServicioAtributoValorGuardarRequest valor in atributo.Valores.OrderBy(x => x.Orden))
                {
                    Guid idValor = await EnsureAtributoValorAsync(connection, transaction, idEmpresa, atributo.IdAtributo, valor, ahora);
                    using SqlCommand insertValor = new SqlCommand(@"
INSERT INTO dbo.ProductosServiciosProductoAtributoValores
    (id, idEmpresa, identityKey, idProductoAtributo, idAtributoValor, Orden, Activo, FechaCreacion, FechaActualizacion)
VALUES
    (@Id, @IdEmpresa, @IdentityKey, @IdProductoAtributo, @IdAtributoValor, @Orden, 1, @FechaCreacion, @FechaActualizacion)", connection, transaction);
                    insertValor.Parameters.AddWithValue("@Id", Guid.NewGuid());
                    insertValor.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                    insertValor.Parameters.AddWithValue("@IdentityKey", Guid.NewGuid());
                    insertValor.Parameters.AddWithValue("@IdProductoAtributo", idProductoAtributo);
                    insertValor.Parameters.AddWithValue("@IdAtributoValor", idValor);
                    insertValor.Parameters.AddWithValue("@Orden", valor.Orden);
                    insertValor.Parameters.AddWithValue("@FechaCreacion", ahora);
                    insertValor.Parameters.AddWithValue("@FechaActualizacion", ahora);
                    await insertValor.ExecuteNonQueryAsync();
                }
            }
        }

        private async Task<Dictionary<string, VariantOptionReference>> SynchronizeProductoOpcionesVarianteAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idProductoServicio, List<ProductoServicioOpcionVarianteGuardarRequest> opciones, DateTime ahora)
        {
            using SqlCommand deleteVariantValues = new SqlCommand(@"
DELETE vv
FROM dbo.ProductosServiciosVarianteValores vv
INNER JOIN dbo.ProductosServiciosVariantes pv ON pv.idEmpresa = vv.idEmpresa AND pv.id = vv.idVariante
WHERE vv.idEmpresa = @IdEmpresa AND pv.idProductoServicio = @IdProductoServicio", connection, transaction);
            deleteVariantValues.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            deleteVariantValues.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);
            await deleteVariantValues.ExecuteNonQueryAsync();

            using SqlCommand deleteValores = new SqlCommand(@"
DELETE ovv
FROM dbo.ProductosServiciosOpcionesVarianteValores ovv
INNER JOIN dbo.ProductosServiciosOpcionesVariante ov ON ov.idEmpresa = ovv.idEmpresa AND ov.id = ovv.idOpcionVariante
WHERE ovv.idEmpresa = @IdEmpresa AND ov.idProductoServicio = @IdProductoServicio", connection, transaction);
            deleteValores.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            deleteValores.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);
            await deleteValores.ExecuteNonQueryAsync();

            using SqlCommand deleteOpciones = new SqlCommand(@"
DELETE FROM dbo.ProductosServiciosOpcionesVariante
WHERE idEmpresa = @IdEmpresa AND idProductoServicio = @IdProductoServicio", connection, transaction);
            deleteOpciones.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            deleteOpciones.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);
            await deleteOpciones.ExecuteNonQueryAsync();

            Dictionary<string, VariantOptionReference> references = new Dictionary<string, VariantOptionReference>(StringComparer.OrdinalIgnoreCase);
            foreach (ProductoServicioOpcionVarianteGuardarRequest opcion in opciones.OrderBy(x => x.Orden))
            {
                Guid idOpcion = opcion.Id ?? Guid.NewGuid();
                using SqlCommand insert = new SqlCommand(@"
INSERT INTO dbo.ProductosServiciosOpcionesVariante
    (id, idEmpresa, identityKey, idProductoServicio, Nombre, Orden, Activo, FechaCreacion, FechaActualizacion)
VALUES
    (@Id, @IdEmpresa, @IdentityKey, @IdProductoServicio, @Nombre, @Orden, 1, @FechaCreacion, @FechaActualizacion)", connection, transaction);
                insert.Parameters.AddWithValue("@Id", idOpcion);
                insert.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                insert.Parameters.AddWithValue("@IdentityKey", Guid.NewGuid());
                insert.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);
                insert.Parameters.AddWithValue("@Nombre", opcion.Nombre.Trim());
                insert.Parameters.AddWithValue("@Orden", opcion.Orden);
                insert.Parameters.AddWithValue("@FechaCreacion", ahora);
                insert.Parameters.AddWithValue("@FechaActualizacion", ahora);
                await insert.ExecuteNonQueryAsync();

                VariantOptionReference current = new VariantOptionReference { Id = idOpcion, Nombre = opcion.Nombre.Trim() };
                foreach (ProductoServicioOpcionVarianteValorGuardarRequest valor in opcion.Valores.OrderBy(x => x.Orden))
                {
                    Guid idValor = valor.Id ?? Guid.NewGuid();
                    using SqlCommand insertValor = new SqlCommand(@"
INSERT INTO dbo.ProductosServiciosOpcionesVarianteValores
    (id, idEmpresa, identityKey, idOpcionVariante, Valor, Orden, Activo, FechaCreacion, FechaActualizacion)
VALUES
    (@Id, @IdEmpresa, @IdentityKey, @IdOpcionVariante, @Valor, @Orden, 1, @FechaCreacion, @FechaActualizacion)", connection, transaction);
                    insertValor.Parameters.AddWithValue("@Id", idValor);
                    insertValor.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                    insertValor.Parameters.AddWithValue("@IdentityKey", Guid.NewGuid());
                    insertValor.Parameters.AddWithValue("@IdOpcionVariante", idOpcion);
                    insertValor.Parameters.AddWithValue("@Valor", valor.Valor.Trim());
                    insertValor.Parameters.AddWithValue("@Orden", valor.Orden);
                    insertValor.Parameters.AddWithValue("@FechaCreacion", ahora);
                    insertValor.Parameters.AddWithValue("@FechaActualizacion", ahora);
                    await insertValor.ExecuteNonQueryAsync();

                    current.Valores[NormalizeCatalogKey(valor.Valor)] = idValor;
                }

                references[NormalizeCatalogKey(opcion.Nombre)] = current;
            }

            return references;
        }

        private async Task<PreparedVariantSyncResult> SynchronizeProductoVariantesAsync(SqlConnection connection, SqlTransaction transaction, RequestContext context, Guid idProductoServicio, List<ProductoServicioVarianteGuardarRequest> variantes, Dictionary<string, VariantOptionReference> optionReferences, DateTime ahora)
        {
            List<ProductoServicioVarianteDto> variantesActuales = await ObtenerVariantesProductoAsync(connection, context.IdEmpresa, idProductoServicio, transaction);
            Dictionary<Guid, ProductoServicioVarianteDto> variantesActualesPorId = variantesActuales.ToDictionary(x => x.Id);
            Dictionary<string, ProductoServicioVarianteDto> variantesActualesPorClave = variantesActuales
                .Where(x => !string.IsNullOrWhiteSpace(x.ClaveCombinacion))
                .GroupBy(x => x.ClaveCombinacion, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
            PreparedVariantSyncResult result = new PreparedVariantSyncResult();

            using SqlCommand deleteValores = new SqlCommand(@"
DELETE vv
FROM dbo.ProductosServiciosVarianteValores vv
INNER JOIN dbo.ProductosServiciosVariantes pv ON pv.idEmpresa = vv.idEmpresa AND pv.id = vv.idVariante
WHERE vv.idEmpresa = @IdEmpresa AND pv.idProductoServicio = @IdProductoServicio", connection, transaction);
            deleteValores.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);
            deleteValores.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);
            await deleteValores.ExecuteNonQueryAsync();

            using SqlCommand deleteVariantes = new SqlCommand(@"
DELETE FROM dbo.ProductosServiciosVariantes
WHERE idEmpresa = @IdEmpresa AND idProductoServicio = @IdProductoServicio", connection, transaction);
            deleteVariantes.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);
            deleteVariantes.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);
            await deleteVariantes.ExecuteNonQueryAsync();

            HashSet<Guid> finalVariantIds = new HashSet<Guid>();
            foreach (ProductoServicioVarianteGuardarRequest variante in variantes.OrderBy(x => x.Orden))
            {
                ProductoServicioVarianteDto? existente = null;
                if (variante.Id.HasValue && variante.Id.Value != Guid.Empty)
                {
                    variantesActualesPorId.TryGetValue(variante.Id.Value, out existente);
                }

                if (existente == null && !string.IsNullOrWhiteSpace(variante.ClaveCombinacion))
                {
                    variantesActualesPorClave.TryGetValue(variante.ClaveCombinacion.Trim(), out existente);
                }

                Guid idVariante = variante.Id.HasValue && variante.Id.Value != Guid.Empty
                    ? variante.Id.Value
                    : existente?.Id ?? Guid.NewGuid();
                finalVariantIds.Add(idVariante);

                PreparedImageOperation preparedImage = await PrepareVariantImageOperationAsync(context, idProductoServicio, idVariante, variante);
                ResolvedImageMutation imageMutation = ResolveImageMutation(existente?.ImagenUrl ?? string.Empty, existente?.ImagenNombre ?? string.Empty, preparedImage);

                if (preparedImage.TemporalCleanup != null)
                {
                    result.TemporalCleanups.Add(preparedImage.TemporalCleanup);
                }

                if (preparedImage.NewImageCleanup != null)
                {
                    result.NewFileCompensations.Add(preparedImage.NewImageCleanup);
                }

                if (imageMutation.PreviousImageCleanup != null)
                {
                    result.FinalCleanups.Add(imageMutation.PreviousImageCleanup);
                }

                using SqlCommand insert = new SqlCommand(@"
INSERT INTO dbo.ProductosServiciosVariantes
    (id, idEmpresa, identityKey, idProductoServicio, Sku, Nombre, ClaveCombinacion, ImagenUrl, ImagenNombre, PrecioPublico, PrecioComparacion, PrecioUnitarioMonto, PrecioUnitarioBaseCantidad, PrecioUnitarioUnidad, Orden, Activo, FechaCreacion, FechaActualizacion)
VALUES
    (@Id, @IdEmpresa, @IdentityKey, @IdProductoServicio, @Sku, @Nombre, @ClaveCombinacion, @ImagenUrl, @ImagenNombre, @PrecioPublico, @PrecioComparacion, @PrecioUnitarioMonto, @PrecioUnitarioBaseCantidad, @PrecioUnitarioUnidad, @Orden, 1, @FechaCreacion, @FechaActualizacion)", connection, transaction);
                insert.Parameters.AddWithValue("@Id", idVariante);
                insert.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);
                insert.Parameters.AddWithValue("@IdentityKey", Guid.NewGuid());
                insert.Parameters.AddWithValue("@IdProductoServicio", idProductoServicio);
                insert.Parameters.AddWithValue("@Sku", string.IsNullOrWhiteSpace(variante.Sku) ? DBNull.Value : variante.Sku.Trim());
                insert.Parameters.AddWithValue("@Nombre", variante.Nombre.Trim());
                insert.Parameters.AddWithValue("@ClaveCombinacion", variante.ClaveCombinacion.Trim());
                insert.Parameters.AddWithValue("@ImagenUrl", string.IsNullOrWhiteSpace(imageMutation.ImagenUrl) ? DBNull.Value : imageMutation.ImagenUrl);
                insert.Parameters.AddWithValue("@ImagenNombre", string.IsNullOrWhiteSpace(imageMutation.ImagenNombre) ? DBNull.Value : imageMutation.ImagenNombre);
                insert.Parameters.AddWithValue("@PrecioPublico", variante.PrecioPublico.HasValue ? variante.PrecioPublico.Value : DBNull.Value);
                insert.Parameters.AddWithValue("@PrecioComparacion", variante.PrecioComparacion.HasValue ? variante.PrecioComparacion.Value : DBNull.Value);
                insert.Parameters.AddWithValue("@PrecioUnitarioMonto", variante.PrecioUnitarioMonto.HasValue ? variante.PrecioUnitarioMonto.Value : DBNull.Value);
                insert.Parameters.AddWithValue("@PrecioUnitarioBaseCantidad", variante.PrecioUnitarioBaseCantidad.HasValue ? variante.PrecioUnitarioBaseCantidad.Value : DBNull.Value);
                insert.Parameters.AddWithValue("@PrecioUnitarioUnidad", string.IsNullOrWhiteSpace(variante.PrecioUnitarioUnidad) ? DBNull.Value : variante.PrecioUnitarioUnidad.Trim());
                insert.Parameters.AddWithValue("@Orden", variante.Orden);
                insert.Parameters.AddWithValue("@FechaCreacion", ahora);
                insert.Parameters.AddWithValue("@FechaActualizacion", ahora);
                await insert.ExecuteNonQueryAsync();

                foreach (ProductoServicioVarianteValorGuardarRequest valor in variante.Valores.OrderBy(x => x.Orden))
                {
                    VariantOptionReference option = ResolveVariantOptionReference(optionReferences, valor.IdOpcionVariante ?? Guid.Empty, valor.Opcion);
                    Guid idValor = ResolveVariantOptionValueId(option, valor.IdOpcionVarianteValor, valor.Valor);

                    using SqlCommand insertValor = new SqlCommand(@"
INSERT INTO dbo.ProductosServiciosVarianteValores
    (id, idEmpresa, identityKey, idVariante, idOpcionVariante, idOpcionVarianteValor, Orden, FechaCreacion)
VALUES
    (@Id, @IdEmpresa, @IdentityKey, @IdVariante, @IdOpcionVariante, @IdOpcionVarianteValor, @Orden, @FechaCreacion)", connection, transaction);
                    insertValor.Parameters.AddWithValue("@Id", Guid.NewGuid());
                    insertValor.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);
                    insertValor.Parameters.AddWithValue("@IdentityKey", Guid.NewGuid());
                    insertValor.Parameters.AddWithValue("@IdVariante", idVariante);
                    insertValor.Parameters.AddWithValue("@IdOpcionVariante", option.Id);
                    insertValor.Parameters.AddWithValue("@IdOpcionVarianteValor", idValor);
                    insertValor.Parameters.AddWithValue("@Orden", valor.Orden);
                    insertValor.Parameters.AddWithValue("@FechaCreacion", ahora);
                    await insertValor.ExecuteNonQueryAsync();
                }
            }

            foreach (ProductoServicioVarianteDto varianteEliminada in variantesActuales.Where(x => !finalVariantIds.Contains(x.Id)))
            {
                FirebaseCleanupItem? cleanup = TryBuildCleanupItemFromUrl(varianteEliminada.ImagenUrl);
                if (cleanup != null)
                {
                    result.FinalCleanups.Add(cleanup);
                }
            }

            return result;
        }

        private async Task<PreparedImageOperation> PrepareVariantImageOperationAsync(RequestContext context, Guid productoId, Guid varianteId, ProductoServicioVarianteGuardarRequest request)
        {
            if (request.EliminarImagen)
            {
                return PreparedImageOperation.ForRemove();
            }

            if (request.Imagen == null || string.IsNullOrWhiteSpace(request.Imagen.TemporalToken))
            {
                return PreparedImageOperation.None();
            }

            TemporalImageTokenPayload temporal = TryParseTemporalToken(request.Imagen.TemporalToken)
                ?? throw new InvalidOperationException("La referencia temporal de la imagen de la variante es inválida o expiró.");

            if (!FolderBelongsToEmpresa(temporal.FolderName, context.EmpresaStorageKey))
            {
                throw new InvalidOperationException("La imagen temporal de la variante no pertenece a la empresa activa.");
            }

            UploadedImagePayload uploaded = await MoveTemporalImageToFinalAsync(
                context.EmpresaStorageKey,
                productoId,
                temporal,
                BuildFinalVariantImageFolderName(context.EmpresaStorageKey, productoId, varianteId));

            return PreparedImageOperation.ForNewImage(uploaded, new FirebaseCleanupItem
            {
                FolderName = temporal.FolderName,
                StoredName = temporal.NombreAlmacenado
            });
        }

        private async Task FinalizeVariantSyncAfterCommitAsync(PreparedVariantSyncResult result)
        {
            await CleanupUploadedFirebaseFilesAsync(result.TemporalCleanups.Concat(result.FinalCleanups).ToList());
        }

        private async Task CompensatePreparedVariantSyncAsync(PreparedVariantSyncResult result)
        {
            try
            {
                await CleanupUploadedFirebaseFilesAsync(result.NewFileCompensations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fallo la compensacion de imagen final para variantes de productos y servicios.");
            }
        }

        private async Task<Guid> EnsureAtributoValorAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idAtributo, ProductoServicioAtributoValorGuardarRequest valor, DateTime ahora)
        {
            if (valor.IdAtributoValor.HasValue && valor.IdAtributoValor.Value != Guid.Empty)
            {
                using SqlCommand validateExisting = new SqlCommand(@"
SELECT TOP 1 id
FROM dbo.ProductosServiciosAtributosValores
WHERE idEmpresa = @IdEmpresa AND idAtributo = @IdAtributo AND id = @IdAtributoValor", connection, transaction);
                validateExisting.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                validateExisting.Parameters.AddWithValue("@IdAtributo", idAtributo);
                validateExisting.Parameters.AddWithValue("@IdAtributoValor", valor.IdAtributoValor.Value);
                object? validExisting = await validateExisting.ExecuteScalarAsync();
                if (validExisting != null && validExisting != DBNull.Value)
                {
                    return valor.IdAtributoValor.Value;
                }

                throw new ProductoServicioValidationException("Se detectó un valor de atributo inválido para la variante.");
            }

            using SqlCommand find = new SqlCommand(@"
SELECT TOP 1 id
FROM dbo.ProductosServiciosAtributosValores
WHERE idEmpresa = @IdEmpresa AND idAtributo = @IdAtributo AND Valor = @Valor", connection, transaction);
            find.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            find.Parameters.AddWithValue("@IdAtributo", idAtributo);
            find.Parameters.AddWithValue("@Valor", valor.Valor.Trim());
            object? existing = await find.ExecuteScalarAsync();
            if (existing != null && existing != DBNull.Value)
            {
                return (Guid)existing;
            }

            Guid id = Guid.NewGuid();
            using SqlCommand insert = new SqlCommand(@"
INSERT INTO dbo.ProductosServiciosAtributosValores
    (id, idEmpresa, identityKey, idAtributo, Valor, Orden, Activo, FechaCreacion, FechaActualizacion, FechaArchivado)
VALUES
    (@Id, @IdEmpresa, @IdentityKey, @IdAtributo, @Valor, @Orden, 1, @FechaCreacion, @FechaActualizacion, NULL)", connection, transaction);
            insert.Parameters.AddWithValue("@Id", id);
            insert.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            insert.Parameters.AddWithValue("@IdentityKey", Guid.NewGuid());
            insert.Parameters.AddWithValue("@IdAtributo", idAtributo);
            insert.Parameters.AddWithValue("@Valor", valor.Valor.Trim());
            insert.Parameters.AddWithValue("@Orden", valor.Orden);
            insert.Parameters.AddWithValue("@FechaCreacion", ahora);
            insert.Parameters.AddWithValue("@FechaActualizacion", ahora);
            await insert.ExecuteNonQueryAsync();
            return id;
        }

        private static VariantOptionReference ResolveVariantOptionReference(Dictionary<string, VariantOptionReference> optionReferences, Guid idOpcionVariante, string opcion)
        {
            if (idOpcionVariante != Guid.Empty)
            {
                VariantOptionReference? matchById = optionReferences.Values.FirstOrDefault(item => item.Id == idOpcionVariante);
                if (matchById != null)
                {
                    return matchById;
                }
            }

            string key = NormalizeCatalogKey(opcion);
            if (!string.IsNullOrWhiteSpace(key) && optionReferences.TryGetValue(key, out VariantOptionReference? current))
            {
                return current;
            }

            throw new ProductoServicioValidationException("Se detectó una opción de variante inválida para una combinación.");
        }

        private static Guid ResolveVariantOptionValueId(VariantOptionReference option, Guid? idOpcionVarianteValor, string valor)
        {
            if (idOpcionVarianteValor.HasValue && idOpcionVarianteValor.Value != Guid.Empty && option.Valores.Values.Contains(idOpcionVarianteValor.Value))
            {
                return idOpcionVarianteValor.Value;
            }

            string key = NormalizeCatalogKey(valor);
            if (!string.IsNullOrWhiteSpace(key) && option.Valores.TryGetValue(key, out Guid idValor))
            {
                return idValor;
            }

            throw new ProductoServicioValidationException("Se detectó un valor de opción inválido para una variante.");
        }

        private static string NormalizeCatalogKey(string value)
        {
            return string.Concat((value ?? string.Empty)
                .Trim()
                .Normalize(NormalizationForm.FormD)
                .Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark))
                .Trim()
                .ToLowerInvariant();
        }

        private static string BuildTemporalFolderName(string empresaStorageKey)
        {
            return $"{empresaStorageKey}/ProductosServicios/Temporal/Imagen";
        }

        private static string BuildTemporalMultimediaFolderName(string empresaStorageKey, string operacionCarga, string tipoMultimedia)
        {
            string operacion = string.IsNullOrWhiteSpace(operacionCarga) ? Guid.NewGuid().ToString("N") : operacionCarga.Trim();
            return $"{empresaStorageKey}/ProductosServicios/Temporal/{operacion}/{tipoMultimedia}";
        }

        private static string BuildFinalFolderName(string empresaStorageKey, Guid productoId)
        {
            return $"{empresaStorageKey}/ProductosServicios/{productoId:N}/Imagen";
        }

        private static string BuildFinalVariantImageFolderName(string empresaStorageKey, Guid productoId, Guid varianteId)
        {
            return $"{empresaStorageKey}/ProductosServicios/{productoId:N}/Variantes/{varianteId:N}/Imagen";
        }

        private static string BuildFinalMultimediaFolderName(string empresaStorageKey, Guid productoId, string tipoMultimedia)
        {
            string folder = tipoMultimedia switch
            {
                "foto" => "Fotos",
                "video" => "Video",
                _ => "Documentos"
            };

            return $"{empresaStorageKey}/ProductosServicios/{productoId:N}/{folder}";
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

            if (normalized is ".pdf" or ".doc" or ".docx" or ".mp4" or ".mov" or ".webm")
            {
                return normalized;
            }

            return (mimeType ?? string.Empty).Trim().ToLowerInvariant() switch
            {
                "application/pdf" => ".pdf",
                "application/msword" => ".doc",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ".docx",
                "video/mp4" => ".mp4",
                "video/quicktime" => ".mov",
                "video/webm" => ".webm",
                "image/png" => ".png",
                "image/webp" => ".webp",
                _ => ".jpg"
            };
        }

        private static string NormalizeTipoMultimedia(string value)
        {
            string normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
            return TiposMultimediaPermitidos.Contains(normalized) ? normalized : string.Empty;
        }

        private static string ValidateTemporalMultimediaUpload(string tipoMultimedia, IFormFile archivo)
        {
            if (archivo.Length <= 0)
            {
                return "Selecciona un archivo válido para cargar.";
            }

            string extension = NormalizeExtension(Path.GetExtension(archivo.FileName), archivo.ContentType ?? string.Empty);
            string mimeType = (archivo.ContentType ?? string.Empty).Trim().ToLowerInvariant();
            return tipoMultimedia switch
            {
                "foto" when archivo.Length > ImagenMaxBytes => "La foto excede el tamaño máximo permitido de 10 MB.",
                "foto" when !(new[] { ".jpg", ".jpeg", ".png", ".webp", ".heic" }.Contains(extension)) => "Selecciona una foto válida.",
                "foto" when !string.IsNullOrWhiteSpace(mimeType) && !mimeType.StartsWith("image/", StringComparison.Ordinal) => "Selecciona una foto válida.",
                "video" when archivo.Length > VideoMaxBytes => "El video excede el tamaño máximo permitido de 200 MB.",
                "video" when !(new[] { ".mp4", ".mov", ".webm" }.Contains(extension)) => "Selecciona un video válido.",
                "video" when !string.IsNullOrWhiteSpace(mimeType) && !mimeType.StartsWith("video/", StringComparison.Ordinal) => "Selecciona un video válido.",
                "documento" when archivo.Length > DocumentoMaxBytes => "El documento excede el tamaño máximo permitido de 25 MB.",
                "documento" when !(new[] { ".pdf", ".doc", ".docx" }.Contains(extension)) => "Selecciona un documento PDF o Word válido.",
                _ => string.Empty
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
                IdColeccion = ReadNullableGuid(reader, "idColeccion"),
                ColeccionNumero = ReadString(reader, "ColeccionNumero"),
                ColeccionNombre = ReadString(reader, "ColeccionNombre"),
                IdPaquete = ReadNullableGuid(reader, "idPaquete"),
                PaqueteNombre = ReadString(reader, "PaqueteNombre"),
                Costo = ReadNullableDecimal(reader, "Costo"),
                PrecioPublico = ReadDecimal(reader, "PrecioPublico"),
                PrecioComparacion = ReadNullableDecimal(reader, "PrecioComparacion"),
                PrecioUnitarioMonto = ReadNullableDecimal(reader, "PrecioUnitarioMonto"),
                PrecioUnitarioBaseCantidad = ReadNullableDecimal(reader, "PrecioUnitarioBaseCantidad"),
                PrecioUnitarioUnidad = ReadString(reader, "PrecioUnitarioUnidad"),
                ObjetoImpuesto = ReadString(reader, "ObjetoImpuesto"),
                ClaveProductoSat = ReadString(reader, "ClaveProductoSat"),
                ClaveUnidadSat = ReadString(reader, "ClaveUnidadSat"),
                EsProductoFisico = ReadBool(reader, "EsProductoFisico"),
                PesoKg = ReadNullableDecimal(reader, "PesoKg"),
                LargoCm = ReadNullableDecimal(reader, "LargoCm"),
                AnchoCm = ReadNullableDecimal(reader, "AnchoCm"),
                AltoCm = ReadNullableDecimal(reader, "AltoCm"),
                UsaNumeroSerie = ReadBool(reader, "UsaNumeroSerie"),
                CausaInventario = ReadBool(reader, "CausaInventario"),
                PermiteVentaSinExistencia = ReadBool(reader, "PermiteVentaSinExistencia"),
                ExistenciaActual = ReadNullableDecimal(reader, "ExistenciaActual"),
                ExistenciaMinima = ReadNullableDecimal(reader, "ExistenciaMinima"),
                CostoPromedio = ReadNullableDecimal(reader, "CostoPromedio"),
                ImagenUrl = ReadString(reader, "ImagenUrl"),
                ImagenNombre = ReadString(reader, "ImagenNombre"),
                CantidadFotos = ReadInt(reader, "CantidadFotos"),
                CantidadVideos = ReadInt(reader, "CantidadVideos"),
                CantidadDocumentos = ReadInt(reader, "CantidadDocumentos"),
                CantidadVariantes = ReadInt(reader, "CantidadVariantes"),
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
                PermiteDecimales = ReadNullableBool(reader, "PermiteDecimales"),
                Numero = HasColumn(reader, "Numero") ? ReadString(reader, "Numero") : string.Empty,
                TipoPaquete = HasColumn(reader, "TipoPaquete") ? ReadString(reader, "TipoPaquete") : string.Empty,
                LargoCm = HasColumn(reader, "LargoCm") ? ReadNullableDecimal(reader, "LargoCm") : null,
                AnchoCm = HasColumn(reader, "AnchoCm") ? ReadNullableDecimal(reader, "AnchoCm") : null,
                AltoCm = HasColumn(reader, "AltoCm") ? ReadNullableDecimal(reader, "AltoCm") : null,
                PesoEmpaqueVacioKg = HasColumn(reader, "PesoEmpaqueVacioKg") ? ReadNullableDecimal(reader, "PesoEmpaqueVacioKg") : null,
                EsPredeterminado = HasColumn(reader, "EsPredeterminado") ? ReadNullableBool(reader, "EsPredeterminado") : null
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

        private static bool HasColumn(SqlDataReader reader, string columnName)
        {
            for (int index = 0; index < reader.FieldCount; index++)
            {
                if (string.Equals(reader.GetName(index), columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
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

        private static long ReadLong(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? 0L : Convert.ToInt64(reader.GetValue(ordinal));
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

        private sealed class ProductoServicioValidationException : Exception
        {
            public ProductoServicioValidationException(string message) : base(message)
            {
            }
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
            public bool Activo { get; set; }
            public ProductoServicioImagenGuardarRequest? ImagenPrincipal { get; set; }
            public bool EliminarImagenPrincipal { get; set; }
            public List<ProductoServicioAtributoGuardarRequest> Atributos { get; set; } = new List<ProductoServicioAtributoGuardarRequest>();
            public List<ProductoServicioOpcionVarianteGuardarRequest> OpcionesVariante { get; set; } = new List<ProductoServicioOpcionVarianteGuardarRequest>();
            public List<ProductoServicioVarianteGuardarRequest> Variantes { get; set; } = new List<ProductoServicioVarianteGuardarRequest>();
            public List<ProductoServicioMultimediaGuardarRequest> Multimedia { get; set; } = new List<ProductoServicioMultimediaGuardarRequest>();
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
            public Guid? IdColeccion { get; set; }
            public Guid? IdPaquete { get; set; }
            public bool EsProductoFisico { get; set; }
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
            public string TipoMultimedia { get; set; } = string.Empty;
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

        private sealed class VariantOptionReference
        {
            public Guid Id { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public Dictionary<string, Guid> Valores { get; set; } = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
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

        private sealed class PreparedMultimediaOperation
        {
            public List<ProductoServicioMultimediaDto> FinalItems { get; set; } = new List<ProductoServicioMultimediaDto>();
            public List<FirebaseCleanupItem> TemporalCleanups { get; set; } = new List<FirebaseCleanupItem>();
            public List<FirebaseCleanupItem> NewFileCompensations { get; set; } = new List<FirebaseCleanupItem>();
        }

        private sealed class PreparedVariantSyncResult
        {
            public List<FirebaseCleanupItem> TemporalCleanups { get; set; } = new List<FirebaseCleanupItem>();
            public List<FirebaseCleanupItem> FinalCleanups { get; set; } = new List<FirebaseCleanupItem>();
            public List<FirebaseCleanupItem> NewFileCompensations { get; set; } = new List<FirebaseCleanupItem>();
        }
    }
}
