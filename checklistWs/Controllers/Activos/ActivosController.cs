using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using checklistWs.Models.Activos;
using Firebase.Auth;
using Firebase.Auth.Providers;
using Firebase.Storage;
using Microsoft.AspNetCore.Mvc;

namespace checklistWs.Controllers.Activos
{
    [Route("api/[controller]")]
    [ApiController]
    public class ActivosController : ControllerBase
    {
        private const int CodigoActivoLength = 64;
        private const int NombreActivoLength = 200;
        private const int TagLength = 80;
        private const int NumeroSerieLength = 120;
        private const int DescripcionActivoLength = 500;
        private const int CodigoCatalogoLength = 64;
        private const int NombreCatalogoLength = 160;
        private const int DescripcionCatalogoLength = 400;
        private const int UrlFirebaseLength = 1024;
        private const int NombreArchivoLength = 255;
        private const int ExtensionLength = 20;
        private const int MimeTypeLength = 120;
        private const long GuardarActivoRequestLimitBytes = 20L * 1024L * 1024L;
        private const long FotoMaxBytes = 10L * 1024L * 1024L;
        private const long VideoMaxBytes = 200L * 1024L * 1024L;
        private const long DocumentoMaxBytes = 25L * 1024L * 1024L;
        private const long UploadTemporalRequestLimitBytes = 210L * 1024L * 1024L;
        private static readonly TimeSpan TemporalTokenLifetime = TimeSpan.FromHours(6);
        private static readonly string[] TiposPermitidos = new[] { "foto", "video", "documento" };

        private readonly IConfiguration _configuration;

        public ActivosController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("ObtenerActivos")]
        public async Task<IActionResult> ObtenerActivos(
            Guid idEmpresa,
            string cadena,
            string busqueda = "",
            Guid? idTipoActivo = null,
            Guid? idEstadoOperativo = null,
            Guid? idSucursal = null,
            Guid? idMarca = null,
            Guid? idProveedor = null,
            string estatus = "")
        {
            try
            {
                using SqlConnection connection = CreateConnection(cadena);
                await connection.OpenAsync();

                StringBuilder query = new StringBuilder(@"
SELECT
    a.id,
    a.idEmpresa,
    a.identityKey,
    a.Codigo,
    a.Nombre,
    a.idTipoActivo,
    t.Nombre AS tipoActivo,
    a.idEstadoOperativo,
    eo.Nombre AS estadoOperativo,
    a.idSucursal,
    s.nombre AS sucursal,
    a.idMarca,
    ISNULL(m.Nombre, '') AS marca,
    a.idProveedor,
    ISNULL(p.Nombre, '') AS proveedor,
    a.Tag,
    a.NumeroSerie,
    a.Descripcion,
    ISNULL(mm.CantidadFotos, 0) AS CantidadFotos,
    ISNULL(mm.CantidadVideos, 0) AS CantidadVideos,
    ISNULL(mm.CantidadDocumentos, 0) AS CantidadDocumentos,
    a.Activo,
    a.FechaArchivado,
    a.FechaCreacion,
    a.FechaActualizacion
FROM dbo.Activos a
INNER JOIN dbo.ActivosTipos t ON t.id = a.idTipoActivo AND t.idEmpresa = a.idEmpresa
INNER JOIN dbo.ActivosEstadosOperativos eo ON eo.id = a.idEstadoOperativo AND eo.idEmpresa = a.idEmpresa
INNER JOIN dbo.Sucursales s ON s.id = a.idSucursal AND s.idEmpresa = a.idEmpresa
LEFT JOIN dbo.ActivosMarcas m ON m.id = a.idMarca AND m.idEmpresa = a.idEmpresa
LEFT JOIN dbo.ActivosProveedores p ON p.id = a.idProveedor AND p.idEmpresa = a.idEmpresa
OUTER APPLY (
    SELECT
        SUM(CASE WHEN am.Activo = 1 AND am.Foto = 1 THEN 1 ELSE 0 END) AS CantidadFotos,
        SUM(CASE WHEN am.Activo = 1 AND am.Video = 1 THEN 1 ELSE 0 END) AS CantidadVideos,
        SUM(CASE WHEN am.Activo = 1 AND am.Documento = 1 THEN 1 ELSE 0 END) AS CantidadDocumentos
    FROM dbo.ActivosMultimedia am
    WHERE am.idActivo = a.id
) mm
WHERE a.idEmpresa = @IdEmpresa");

                using SqlCommand command = new SqlCommand();
                command.Connection = connection;
                command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);

                if (!string.IsNullOrWhiteSpace(busqueda))
                {
                    query.Append(@"
  AND (
        a.Codigo LIKE @Busqueda
        OR a.Nombre LIKE @Busqueda
        OR ISNULL(a.Tag, '') LIKE @Busqueda
        OR ISNULL(a.NumeroSerie, '') LIKE @Busqueda
        OR ISNULL(a.Descripcion, '') LIKE @Busqueda
        OR t.Nombre LIKE @Busqueda
        OR eo.Nombre LIKE @Busqueda
        OR s.nombre LIKE @Busqueda
        OR ISNULL(m.Nombre, '') LIKE @Busqueda
        OR ISNULL(p.Nombre, '') LIKE @Busqueda
      )");
                    command.Parameters.AddWithValue("@Busqueda", $"%{busqueda.Trim()}%");
                }

                AppendGuidFilter(query, command, "a.idTipoActivo", "@IdTipoActivo", idTipoActivo);
                AppendGuidFilter(query, command, "a.idEstadoOperativo", "@IdEstadoOperativo", idEstadoOperativo);
                AppendGuidFilter(query, command, "a.idSucursal", "@IdSucursal", idSucursal);
                AppendGuidFilter(query, command, "a.idMarca", "@IdMarca", idMarca);
                AppendGuidFilter(query, command, "a.idProveedor", "@IdProveedor", idProveedor);

                switch ((estatus ?? string.Empty).Trim().ToLowerInvariant())
                {
                    case "activos":
                        query.Append(" AND a.Activo = 1");
                        break;
                    case "inactivos":
                        query.Append(" AND a.Activo = 0");
                        break;
                }

                query.Append(" ORDER BY a.Activo DESC, a.Nombre, a.Codigo");
                command.CommandText = query.ToString();

                List<ActivoListadoDto> activos = new List<ActivoListadoDto>();
                using SqlDataReader reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    activos.Add(MapActivoListado(reader));
                }

                return Ok(activos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ActivoOperacionResponse { Mensaje = $"Error interno del servidor: {ex.Message}" });
            }
        }

        [HttpGet("ObtenerActivo")]
        public async Task<IActionResult> ObtenerActivo(Guid idEmpresa, Guid idActivo, string cadena)
        {
            try
            {
                using SqlConnection connection = CreateConnection(cadena);
                await connection.OpenAsync();

                using SqlCommand command = new SqlCommand(@"
SELECT
    a.id,
    a.idEmpresa,
    a.identityKey,
    a.Codigo,
    a.Nombre,
    a.idTipoActivo,
    t.Nombre AS tipoActivo,
    a.idEstadoOperativo,
    eo.Nombre AS estadoOperativo,
    a.idSucursal,
    s.nombre AS sucursal,
    a.idMarca,
    ISNULL(m.Nombre, '') AS marca,
    a.idProveedor,
    ISNULL(p.Nombre, '') AS proveedor,
    a.Tag,
    a.NumeroSerie,
    a.Descripcion,
    ISNULL(mm.CantidadFotos, 0) AS CantidadFotos,
    ISNULL(mm.CantidadVideos, 0) AS CantidadVideos,
    ISNULL(mm.CantidadDocumentos, 0) AS CantidadDocumentos,
    a.Activo,
    a.FechaArchivado,
    a.FechaCreacion,
    a.FechaActualizacion
FROM dbo.Activos a
INNER JOIN dbo.ActivosTipos t ON t.id = a.idTipoActivo AND t.idEmpresa = a.idEmpresa
INNER JOIN dbo.ActivosEstadosOperativos eo ON eo.id = a.idEstadoOperativo AND eo.idEmpresa = a.idEmpresa
INNER JOIN dbo.Sucursales s ON s.id = a.idSucursal AND s.idEmpresa = a.idEmpresa
LEFT JOIN dbo.ActivosMarcas m ON m.id = a.idMarca AND m.idEmpresa = a.idEmpresa
LEFT JOIN dbo.ActivosProveedores p ON p.id = a.idProveedor AND p.idEmpresa = a.idEmpresa
OUTER APPLY (
    SELECT
        SUM(CASE WHEN am.Activo = 1 AND am.Foto = 1 THEN 1 ELSE 0 END) AS CantidadFotos,
        SUM(CASE WHEN am.Activo = 1 AND am.Video = 1 THEN 1 ELSE 0 END) AS CantidadVideos,
        SUM(CASE WHEN am.Activo = 1 AND am.Documento = 1 THEN 1 ELSE 0 END) AS CantidadDocumentos
    FROM dbo.ActivosMultimedia am
    WHERE am.idActivo = a.id
) mm
WHERE a.idEmpresa = @IdEmpresa AND a.id = @IdActivo", connection);

                command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                command.Parameters.AddWithValue("@IdActivo", idActivo);

                ActivoDetalleDto activo;
                using (SqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    if (!await reader.ReadAsync())
                    {
                        return NotFound(new ActivoOperacionResponse { Mensaje = "El activo no está disponible." });
                    }

                    activo = MapActivoDetalle(reader);
                }

                activo.Multimedia = await ObtenerMultimediaActivaAsync(connection, idActivo);
                return Ok(activo);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ActivoOperacionResponse { Mensaje = $"Error interno del servidor: {ex.Message}" });
            }
        }

        [HttpPost("SubirMultimediaTemporal")]
        [RequestFormLimits(MultipartBodyLengthLimit = UploadTemporalRequestLimitBytes)]
        [RequestSizeLimit(UploadTemporalRequestLimitBytes)]
        public async Task<IActionResult> SubirMultimediaTemporal(Guid idEmpresa, string cadena, string empresa, [FromForm] string tipoMultimedia, [FromForm] string operacionCarga, IFormFile? archivo)
        {
            try
            {
                string tipo = NormalizeTipoMultimedia(tipoMultimedia);
                if (idEmpresa == Guid.Empty)
                {
                    return BadRequest(new ActivoMultimediaTemporalResponse { Mensaje = "No fue posible resolver la empresa activa." });
                }

                if (string.IsNullOrWhiteSpace(tipo))
                {
                    return BadRequest(new ActivoMultimediaTemporalResponse { Mensaje = "Selecciona un tipo de evidencia válido." });
                }

                if (archivo == null || archivo.Length <= 0)
                {
                    return BadRequest(new ActivoMultimediaTemporalResponse { Mensaje = "Selecciona un archivo válido para cargar." });
                }

                string validation = ValidateTemporalUpload(tipo, archivo);
                if (!string.IsNullOrWhiteSpace(validation))
                {
                    return BadRequest(new ActivoMultimediaTemporalResponse { Mensaje = validation });
                }

                byte[] fileBytes = await ReadFileBytesAsync(archivo);
                validation = ValidateFileSignature(tipo, archivo.FileName, archivo.ContentType, fileBytes);
                if (!string.IsNullOrWhiteSpace(validation))
                {
                    return BadRequest(new ActivoMultimediaTemporalResponse { Mensaje = validation });
                }

                string empresaNormalizada = string.IsNullOrWhiteSpace(empresa) ? idEmpresa.ToString("N").ToUpperInvariant() : empresa.Trim().ToUpperInvariant();
                string operationKey = NormalizeOperationKey(operacionCarga);
                UploadedMultimediaPayload uploaded = await UploadMediaToFirebaseAsync(
                    folderName: BuildTemporalFolderName(empresaNormalizada, operationKey, tipo),
                    storedName: BuildStoredName(tipo, archivo.FileName, archivo.ContentType),
                    fileBytes: fileBytes,
                    tipoMultimedia: tipo,
                    nombreOriginal: archivo.FileName,
                    extension: Path.GetExtension(archivo.FileName),
                    mimeType: archivo.ContentType,
                    pesoBytes: archivo.Length);

                return Ok(new ActivoMultimediaTemporalResponse
                {
                    Mensaje = "La evidencia temporal fue cargada.",
                    Archivo = new ActivoMultimediaTemporalDto
                    {
                        TemporalToken = CreateTemporalToken(new TemporalMultimediaTokenPayload
                        {
                            TipoMultimedia = tipo,
                            NombreOriginal = uploaded.NombreOriginal,
                            NombreAlmacenado = uploaded.NombreAlmacenado,
                            Extension = uploaded.Extension,
                            MimeType = uploaded.MimeType,
                            UrlFirebase = uploaded.UrlFirebase,
                            FolderName = uploaded.FolderName,
                            PesoBytes = uploaded.PesoBytes,
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
                Console.WriteLine($"[Activos][SubirMultimediaTemporal] {ex}");
                return StatusCode(500, new ActivoMultimediaTemporalResponse { Mensaje = ResolveTemporalUploadErrorMessage(ex) });
            }
        }

        [HttpPost("LimpiarMultimediaTemporal")]
        public async Task<IActionResult> LimpiarMultimediaTemporal(Guid idEmpresa, string cadena, [FromBody] ActivoMultimediaTemporalCleanupRequest? request)
        {
            try
            {
                List<FirebaseCleanupItem> files = (request?.Tokens ?? new List<string>())
                    .Select(TryParseTemporalToken)
                    .Where(item => item != null)
                    .Select(item => new FirebaseCleanupItem
                    {
                        FolderName = item!.FolderName,
                        StoredName = item.NombreAlmacenado
                    })
                    .ToList();

                await CleanupUploadedFirebaseFilesAsync(files);
                return Ok(new ActivoOperacionResponse { Mensaje = "La multimedia temporal fue liberada." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Activos][LimpiarMultimediaTemporal] {ex}");
                return StatusCode(500, new ActivoOperacionResponse { Mensaje = "No fue posible liberar la multimedia temporal." });
            }
        }

        [HttpPost("GuardarActivo")]
        [RequestSizeLimit(GuardarActivoRequestLimitBytes)]
        public async Task<IActionResult> GuardarActivo([FromBody] ActivoGuardarRequest request, Guid idEmpresa, string cadena, string empresa = "")
        {
            List<FirebaseCleanupItem> movedTempFiles = new List<FirebaseCleanupItem>();
            try
            {
                request.IdEmpresa = idEmpresa;
                string validacion = ValidateActivoRequest(request);
                if (!string.IsNullOrEmpty(validacion))
                {
                    return BadRequest(new ActivoOperacionResponse { Mensaje = validacion });
                }

                using SqlConnection connection = CreateConnection(cadena);
                await connection.OpenAsync();
                using SqlTransaction transaction = connection.BeginTransaction();

                if (!await ExisteTipoActivoAsync(connection, transaction, idEmpresa, request.IdTipoActivo))
                {
                    return BadRequest(new ActivoOperacionResponse { Mensaje = "Selecciona un tipo de activo vigente." });
                }

                if (!await ExisteEstadoOperativoAsync(connection, transaction, idEmpresa, request.IdEstadoOperativo))
                {
                    return BadRequest(new ActivoOperacionResponse { Mensaje = "Selecciona un estado operativo vigente." });
                }

                if (!await ExisteSucursalAsync(connection, transaction, idEmpresa, request.IdSucursal))
                {
                    return BadRequest(new ActivoOperacionResponse { Mensaje = "Selecciona una sucursal vigente." });
                }

                if (!await ExisteMarcaAsync(connection, transaction, idEmpresa, request.IdMarca))
                {
                    return BadRequest(new ActivoOperacionResponse { Mensaje = "Selecciona una marca vigente." });
                }

                if (!await ExisteProveedorAsync(connection, transaction, idEmpresa, request.IdProveedor))
                {
                    return BadRequest(new ActivoOperacionResponse { Mensaje = "Selecciona un proveedor vigente." });
                }

                Guid idActivo = request.Id ?? Guid.Empty;
                bool esNuevo = idActivo == Guid.Empty;
                if (esNuevo)
                {
                    idActivo = Guid.NewGuid();
                }

                if (await ExisteCodigoActivoAsync(connection, transaction, idEmpresa, request.Codigo, esNuevo ? null : idActivo))
                {
                    return BadRequest(new ActivoOperacionResponse { Mensaje = "Ya existe un activo con el mismo código." });
                }

                List<ActivoMultimediaDto> multimediaActual = esNuevo
                    ? new List<ActivoMultimediaDto>()
                    : await ObtenerMultimediaActivaAsync(connection, transaction, idActivo);

                string empresaNormalizada = string.IsNullOrWhiteSpace(empresa) ? idEmpresa.ToString("N").ToUpperInvariant() : empresa.Trim().ToUpperInvariant();
                List<ActivoMultimediaDto> multimediaFinal = await ResolverMultimediaFinalAsync(multimediaActual, request.Multimedia, empresaNormalizada, idActivo, movedTempFiles);
                string validacionMultimedia = ValidateMultimedia(multimediaFinal);
                if (!string.IsNullOrEmpty(validacionMultimedia))
                {
                    await CleanupUploadedFirebaseFilesAsync(movedTempFiles);
                    return BadRequest(new ActivoOperacionResponse { Mensaje = validacionMultimedia });
                }

                DateTime ahora = DateTime.UtcNow;
                if (esNuevo)
                {
                    using SqlCommand insert = new SqlCommand(@"
INSERT INTO dbo.Activos
    (id, idEmpresa, identityKey, Codigo, Nombre, idTipoActivo, idEstadoOperativo, idSucursal, idMarca, idProveedor, Tag, NumeroSerie, Descripcion, Activo, FechaArchivado, FechaCreacion, FechaActualizacion)
VALUES
    (@Id, @IdEmpresa, @IdentityKey, @Codigo, @Nombre, @IdTipoActivo, @IdEstadoOperativo, @IdSucursal, @IdMarca, @IdProveedor, @Tag, @NumeroSerie, @Descripcion, 1, NULL, @FechaCreacion, @FechaActualizacion)", connection, transaction);

                    insert.Parameters.AddWithValue("@Id", idActivo);
                    insert.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                    insert.Parameters.AddWithValue("@IdentityKey", BuildIdentityKey(idActivo));
                    insert.Parameters.AddWithValue("@Codigo", request.Codigo.Trim());
                    insert.Parameters.AddWithValue("@Nombre", request.Nombre.Trim());
                    insert.Parameters.AddWithValue("@IdTipoActivo", request.IdTipoActivo);
                    insert.Parameters.AddWithValue("@IdEstadoOperativo", request.IdEstadoOperativo);
                    insert.Parameters.AddWithValue("@IdSucursal", request.IdSucursal);
                    insert.Parameters.AddWithValue("@IdMarca", request.IdMarca);
                    insert.Parameters.AddWithValue("@IdProveedor", request.IdProveedor);
                    insert.Parameters.AddWithValue("@Tag", request.Tag.Trim());
                    insert.Parameters.AddWithValue("@NumeroSerie", request.NumeroSerie.Trim());
                    insert.Parameters.AddWithValue("@Descripcion", request.Descripcion.Trim());
                    insert.Parameters.AddWithValue("@FechaCreacion", ahora);
                    insert.Parameters.AddWithValue("@FechaActualizacion", ahora);
                    await insert.ExecuteNonQueryAsync();
                }
                else
                {
                    using SqlCommand update = new SqlCommand(@"
UPDATE dbo.Activos
SET
    Codigo = @Codigo,
    Nombre = @Nombre,
    idTipoActivo = @IdTipoActivo,
    idEstadoOperativo = @IdEstadoOperativo,
    idSucursal = @IdSucursal,
    idMarca = @IdMarca,
    idProveedor = @IdProveedor,
    Tag = @Tag,
    NumeroSerie = @NumeroSerie,
    Descripcion = @Descripcion,
    FechaActualizacion = @FechaActualizacion
WHERE idEmpresa = @IdEmpresa AND id = @Id", connection, transaction);

                    update.Parameters.AddWithValue("@Id", idActivo);
                    update.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                    update.Parameters.AddWithValue("@Codigo", request.Codigo.Trim());
                    update.Parameters.AddWithValue("@Nombre", request.Nombre.Trim());
                    update.Parameters.AddWithValue("@IdTipoActivo", request.IdTipoActivo);
                    update.Parameters.AddWithValue("@IdEstadoOperativo", request.IdEstadoOperativo);
                    update.Parameters.AddWithValue("@IdSucursal", request.IdSucursal);
                    update.Parameters.AddWithValue("@IdMarca", request.IdMarca);
                    update.Parameters.AddWithValue("@IdProveedor", request.IdProveedor);
                    update.Parameters.AddWithValue("@Tag", request.Tag.Trim());
                    update.Parameters.AddWithValue("@NumeroSerie", request.NumeroSerie.Trim());
                    update.Parameters.AddWithValue("@Descripcion", request.Descripcion.Trim());
                    update.Parameters.AddWithValue("@FechaActualizacion", ahora);

                    int rowsAffected = await update.ExecuteNonQueryAsync();
                    if (rowsAffected == 0)
                    {
                        transaction.Rollback();
                        return NotFound(new ActivoOperacionResponse { Mensaje = "El activo no está disponible." });
                    }
                }

                await SincronizarMultimediaAsync(connection, transaction, idActivo, multimediaActual, multimediaFinal, ahora);
                transaction.Commit();
                await CleanupUploadedFirebaseFilesAsync(movedTempFiles);
                return Ok(new ActivoOperacionResponse { Mensaje = esNuevo ? "El activo fue registrado." : "El activo fue actualizado." });
            }
            catch (Exception ex)
            {
                await CleanupUploadedFirebaseFilesAsync(movedTempFiles);
                Console.WriteLine($"[Activos][GuardarActivo] {ex}");
                return StatusCode(500, new ActivoOperacionResponse { Mensaje = ResolveGuardarActivoErrorMessage(ex) });
            }
        }

        [HttpPut("BajaActivo")]
        public async Task<IActionResult> BajaActivo(Guid idEmpresa, Guid idActivo, string cadena)
        {
            try
            {
                using SqlConnection connection = CreateConnection(cadena);
                await connection.OpenAsync();

                using SqlCommand command = new SqlCommand(@"
UPDATE dbo.Activos
SET
    Activo = 0,
    FechaArchivado = @FechaArchivado,
    FechaActualizacion = @FechaActualizacion
WHERE idEmpresa = @IdEmpresa AND id = @IdActivo AND Activo = 1", connection);

                DateTime ahora = DateTime.UtcNow;
                command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                command.Parameters.AddWithValue("@IdActivo", idActivo);
                command.Parameters.AddWithValue("@FechaArchivado", ahora);
                command.Parameters.AddWithValue("@FechaActualizacion", ahora);

                int rowsAffected = await command.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    return NotFound(new ActivoOperacionResponse { Mensaje = "El activo no está disponible para baja." });
                }

                return Ok(new ActivoOperacionResponse { Mensaje = "El activo fue dado de baja." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ActivoOperacionResponse { Mensaje = $"Error interno del servidor: {ex.Message}" });
            }
        }

        [HttpGet("ObtenerTiposActivos")]
        public async Task<IActionResult> ObtenerTiposActivos(Guid idEmpresa, string cadena, string busqueda = "", string estatus = "")
        {
            return Ok(await ObtenerCatalogosBasicosAsync<TipoActivoDto>(cadena, idEmpresa, busqueda, estatus, "dbo.ActivosTipos"));
        }

        [HttpGet("ObtenerTipoActivo")]
        public async Task<IActionResult> ObtenerTipoActivo(Guid idEmpresa, Guid idTipoActivo, string cadena)
        {
            TipoActivoDto? item = await ObtenerCatalogoBasicoAsync<TipoActivoDto>(cadena, idEmpresa, idTipoActivo, "dbo.ActivosTipos");
            return item == null
                ? NotFound(new ActivoOperacionResponse { Mensaje = "El tipo de activo no está disponible." })
                : Ok(item);
        }

        [HttpPost("GuardarTipoActivo")]
        public async Task<IActionResult> GuardarTipoActivo([FromBody] TipoActivoGuardarRequest request, Guid idEmpresa, string cadena)
        {
            request.IdEmpresa = idEmpresa;
            return await GuardarCatalogoBasicoAsync(request.Codigo, request.Nombre, request.Descripcion, request.Id, idEmpresa, cadena, "dbo.ActivosTipos", "tipo de activo");
        }

        [HttpPut("BajaTipoActivo")]
        public async Task<IActionResult> BajaTipoActivo(Guid idEmpresa, Guid idTipoActivo, string cadena)
        {
            return await CambiarEstatusCatalogoBasicoAsync(idEmpresa, idTipoActivo, cadena, "dbo.ActivosTipos", "tipo de activo", false);
        }

        [HttpPut("ActivarTipoActivo")]
        public async Task<IActionResult> ActivarTipoActivo(Guid idEmpresa, Guid idTipoActivo, string cadena)
        {
            return await CambiarEstatusCatalogoBasicoAsync(idEmpresa, idTipoActivo, cadena, "dbo.ActivosTipos", "tipo de activo", true);
        }

        [HttpGet("ObtenerMarcasActivos")]
        public async Task<IActionResult> ObtenerMarcasActivos(Guid idEmpresa, string cadena, string busqueda = "", string estatus = "")
        {
            return Ok(await ObtenerCatalogosBasicosAsync<MarcaActivoDto>(cadena, idEmpresa, busqueda, estatus, "dbo.ActivosMarcas"));
        }

        [HttpGet("ObtenerMarcaActivo")]
        public async Task<IActionResult> ObtenerMarcaActivo(Guid idEmpresa, Guid idMarca, string cadena)
        {
            MarcaActivoDto? item = await ObtenerCatalogoBasicoAsync<MarcaActivoDto>(cadena, idEmpresa, idMarca, "dbo.ActivosMarcas");
            return item == null
                ? NotFound(new ActivoOperacionResponse { Mensaje = "La marca no está disponible." })
                : Ok(item);
        }

        [HttpPost("GuardarMarcaActivo")]
        public async Task<IActionResult> GuardarMarcaActivo([FromBody] MarcaActivoGuardarRequest request, Guid idEmpresa, string cadena)
        {
            request.IdEmpresa = idEmpresa;
            return await GuardarCatalogoBasicoAsync(request.Codigo, request.Nombre, request.Descripcion, request.Id, idEmpresa, cadena, "dbo.ActivosMarcas", "marca");
        }

        [HttpPut("BajaMarcaActivo")]
        public async Task<IActionResult> BajaMarcaActivo(Guid idEmpresa, Guid idMarca, string cadena)
        {
            return await CambiarEstatusCatalogoBasicoAsync(idEmpresa, idMarca, cadena, "dbo.ActivosMarcas", "marca", false);
        }

        [HttpPut("ActivarMarcaActivo")]
        public async Task<IActionResult> ActivarMarcaActivo(Guid idEmpresa, Guid idMarca, string cadena)
        {
            return await CambiarEstatusCatalogoBasicoAsync(idEmpresa, idMarca, cadena, "dbo.ActivosMarcas", "marca", true);
        }

        [HttpGet("ObtenerProveedoresActivos")]
        public async Task<IActionResult> ObtenerProveedoresActivos(Guid idEmpresa, string cadena, string busqueda = "", string estatus = "")
        {
            return Ok(await ObtenerCatalogosBasicosAsync<ProveedorActivoDto>(cadena, idEmpresa, busqueda, estatus, "dbo.ActivosProveedores"));
        }

        [HttpGet("ObtenerProveedorActivo")]
        public async Task<IActionResult> ObtenerProveedorActivo(Guid idEmpresa, Guid idProveedor, string cadena)
        {
            ProveedorActivoDto? item = await ObtenerCatalogoBasicoAsync<ProveedorActivoDto>(cadena, idEmpresa, idProveedor, "dbo.ActivosProveedores");
            return item == null
                ? NotFound(new ActivoOperacionResponse { Mensaje = "El proveedor no está disponible." })
                : Ok(item);
        }

        [HttpPost("GuardarProveedorActivo")]
        public async Task<IActionResult> GuardarProveedorActivo([FromBody] ProveedorActivoGuardarRequest request, Guid idEmpresa, string cadena)
        {
            request.IdEmpresa = idEmpresa;
            return await GuardarCatalogoBasicoAsync(request.Codigo, request.Nombre, request.Descripcion, request.Id, idEmpresa, cadena, "dbo.ActivosProveedores", "proveedor");
        }

        [HttpPut("BajaProveedorActivo")]
        public async Task<IActionResult> BajaProveedorActivo(Guid idEmpresa, Guid idProveedor, string cadena)
        {
            return await CambiarEstatusCatalogoBasicoAsync(idEmpresa, idProveedor, cadena, "dbo.ActivosProveedores", "proveedor", false);
        }

        [HttpPut("ActivarProveedorActivo")]
        public async Task<IActionResult> ActivarProveedorActivo(Guid idEmpresa, Guid idProveedor, string cadena)
        {
            return await CambiarEstatusCatalogoBasicoAsync(idEmpresa, idProveedor, cadena, "dbo.ActivosProveedores", "proveedor", true);
        }

        [HttpGet("ObtenerEstadosOperativos")]
        public async Task<IActionResult> ObtenerEstadosOperativos(Guid idEmpresa, string cadena, string busqueda = "", string estatus = "")
        {
            try
            {
                using SqlConnection connection = CreateConnection(cadena);
                await connection.OpenAsync();

                StringBuilder query = new StringBuilder(@"
SELECT id, idEmpresa, Codigo, Nombre, Descripcion, PermiteOperacion, Orden, Activo, FechaCreacion, FechaActualizacion
FROM dbo.ActivosEstadosOperativos
WHERE idEmpresa = @IdEmpresa");

                using SqlCommand command = new SqlCommand();
                command.Connection = connection;
                command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);

                if (!string.IsNullOrWhiteSpace(busqueda))
                {
                    query.Append(" AND (Codigo LIKE @Busqueda OR Nombre LIKE @Busqueda OR ISNULL(Descripcion, '') LIKE @Busqueda)");
                    command.Parameters.AddWithValue("@Busqueda", $"%{busqueda.Trim()}%");
                }

                AppendEstatusFilter(query, estatus);
                query.Append(" ORDER BY Activo DESC, Orden, Nombre, Codigo");
                command.CommandText = query.ToString();

                List<EstadoOperativoDto> estados = new List<EstadoOperativoDto>();
                using SqlDataReader reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    estados.Add(new EstadoOperativoDto
                    {
                        Id = ReadGuid(reader, "id"),
                        IdEmpresa = ReadGuid(reader, "idEmpresa"),
                        Codigo = ReadString(reader, "Codigo"),
                        Nombre = ReadString(reader, "Nombre"),
                        Descripcion = ReadString(reader, "Descripcion"),
                        PermiteOperacion = ReadBool(reader, "PermiteOperacion"),
                        Orden = ReadInt(reader, "Orden"),
                        Activo = ReadBool(reader, "Activo"),
                        FechaCreacion = ReadDateTime(reader, "FechaCreacion"),
                        FechaActualizacion = ReadDateTime(reader, "FechaActualizacion")
                    });
                }

                return Ok(estados);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ActivoOperacionResponse { Mensaje = $"Error interno del servidor: {ex.Message}" });
            }
        }

        [HttpGet("ObtenerEstadoOperativo")]
        public async Task<IActionResult> ObtenerEstadoOperativo(Guid idEmpresa, Guid idEstadoOperativo, string cadena)
        {
            try
            {
                using SqlConnection connection = CreateConnection(cadena);
                await connection.OpenAsync();

                using SqlCommand command = new SqlCommand(@"
SELECT id, idEmpresa, Codigo, Nombre, Descripcion, PermiteOperacion, Orden, Activo, FechaCreacion, FechaActualizacion
FROM dbo.ActivosEstadosOperativos
WHERE idEmpresa = @IdEmpresa AND id = @IdEstadoOperativo", connection);

                command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                command.Parameters.AddWithValue("@IdEstadoOperativo", idEstadoOperativo);

                using SqlDataReader reader = await command.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    return NotFound(new ActivoOperacionResponse { Mensaje = "El estado operativo no está disponible." });
                }

                return Ok(new EstadoOperativoDto
                {
                    Id = ReadGuid(reader, "id"),
                    IdEmpresa = ReadGuid(reader, "idEmpresa"),
                    Codigo = ReadString(reader, "Codigo"),
                    Nombre = ReadString(reader, "Nombre"),
                    Descripcion = ReadString(reader, "Descripcion"),
                    PermiteOperacion = ReadBool(reader, "PermiteOperacion"),
                    Orden = ReadInt(reader, "Orden"),
                    Activo = ReadBool(reader, "Activo"),
                    FechaCreacion = ReadDateTime(reader, "FechaCreacion"),
                    FechaActualizacion = ReadDateTime(reader, "FechaActualizacion")
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ActivoOperacionResponse { Mensaje = $"Error interno del servidor: {ex.Message}" });
            }
        }

        [HttpPost("GuardarEstadoOperativo")]
        public async Task<IActionResult> GuardarEstadoOperativo([FromBody] EstadoOperativoGuardarRequest request, Guid idEmpresa, string cadena)
        {
            try
            {
                request.IdEmpresa = idEmpresa;
                string validacion = ValidateEstadoOperativoRequest(request);
                if (!string.IsNullOrEmpty(validacion))
                {
                    return BadRequest(new ActivoOperacionResponse { Mensaje = validacion });
                }

                using SqlConnection connection = CreateConnection(cadena);
                await connection.OpenAsync();
                using SqlTransaction transaction = connection.BeginTransaction();

                Guid idEstadoOperativo = request.Id ?? Guid.Empty;
                bool esNuevo = idEstadoOperativo == Guid.Empty;
                if (esNuevo)
                {
                    idEstadoOperativo = Guid.NewGuid();
                }

                if (await ExisteCodigoCatalogoAsync(connection, transaction, idEmpresa, request.Codigo, esNuevo ? null : idEstadoOperativo, "dbo.ActivosEstadosOperativos"))
                {
                    return BadRequest(new ActivoOperacionResponse { Mensaje = "Ya existe un estado operativo con el mismo código." });
                }

                DateTime ahora = DateTime.UtcNow;
                if (esNuevo)
                {
                    using SqlCommand insert = new SqlCommand(@"
INSERT INTO dbo.ActivosEstadosOperativos
    (id, idEmpresa, Codigo, Nombre, Descripcion, PermiteOperacion, Orden, Activo, FechaCreacion, FechaActualizacion)
VALUES
    (@Id, @IdEmpresa, @Codigo, @Nombre, @Descripcion, @PermiteOperacion, @Orden, 1, @FechaCreacion, @FechaActualizacion)", connection, transaction);

                    insert.Parameters.AddWithValue("@Id", idEstadoOperativo);
                    insert.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                    insert.Parameters.AddWithValue("@Codigo", request.Codigo.Trim());
                    insert.Parameters.AddWithValue("@Nombre", request.Nombre.Trim());
                    insert.Parameters.AddWithValue("@Descripcion", request.Descripcion.Trim());
                    insert.Parameters.AddWithValue("@PermiteOperacion", request.PermiteOperacion);
                    insert.Parameters.AddWithValue("@Orden", request.Orden!.Value);
                    insert.Parameters.AddWithValue("@FechaCreacion", ahora);
                    insert.Parameters.AddWithValue("@FechaActualizacion", ahora);
                    await insert.ExecuteNonQueryAsync();
                }
                else
                {
                    using SqlCommand update = new SqlCommand(@"
UPDATE dbo.ActivosEstadosOperativos
SET
    Codigo = @Codigo,
    Nombre = @Nombre,
    Descripcion = @Descripcion,
    PermiteOperacion = @PermiteOperacion,
    Orden = @Orden,
    FechaActualizacion = @FechaActualizacion
WHERE idEmpresa = @IdEmpresa AND id = @Id", connection, transaction);

                    update.Parameters.AddWithValue("@Id", idEstadoOperativo);
                    update.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                    update.Parameters.AddWithValue("@Codigo", request.Codigo.Trim());
                    update.Parameters.AddWithValue("@Nombre", request.Nombre.Trim());
                    update.Parameters.AddWithValue("@Descripcion", request.Descripcion.Trim());
                    update.Parameters.AddWithValue("@PermiteOperacion", request.PermiteOperacion);
                    update.Parameters.AddWithValue("@Orden", request.Orden!.Value);
                    update.Parameters.AddWithValue("@FechaActualizacion", ahora);

                    int rowsAffected = await update.ExecuteNonQueryAsync();
                    if (rowsAffected == 0)
                    {
                        transaction.Rollback();
                        return NotFound(new ActivoOperacionResponse { Mensaje = "El estado operativo no está disponible." });
                    }
                }

                transaction.Commit();
                return Ok(new ActivoOperacionResponse { Mensaje = esNuevo ? "El estado operativo fue registrado." : "El estado operativo fue actualizado." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ActivoOperacionResponse { Mensaje = $"Error interno del servidor: {ex.Message}" });
            }
        }

        [HttpPut("BajaEstadoOperativo")]
        public async Task<IActionResult> BajaEstadoOperativo(Guid idEmpresa, Guid idEstadoOperativo, string cadena)
        {
            return await CambiarEstatusCatalogoBasicoAsync(idEmpresa, idEstadoOperativo, cadena, "dbo.ActivosEstadosOperativos", "estado operativo", false);
        }

        [HttpPut("ActivarEstadoOperativo")]
        public async Task<IActionResult> ActivarEstadoOperativo(Guid idEmpresa, Guid idEstadoOperativo, string cadena)
        {
            return await CambiarEstatusCatalogoBasicoAsync(idEmpresa, idEstadoOperativo, cadena, "dbo.ActivosEstadosOperativos", "estado operativo", true);
        }

        [HttpGet("ObtenerCatalogoTiposActivos")]
        public async Task<IActionResult> ObtenerCatalogoTiposActivos(Guid idEmpresa, string cadena, string busqueda = "")
        {
            return Ok(await GetCatalogoAsync(
                cadena,
                @"
SELECT id, Codigo, Nombre, Descripcion, Activo, CAST(NULL AS uniqueidentifier) AS RelacionId
FROM dbo.ActivosTipos
WHERE idEmpresa = @IdEmpresa AND Activo = 1",
                idEmpresa,
                busqueda,
                "Nombre"));
        }

        [HttpGet("ObtenerCatalogoMarcasActivos")]
        public async Task<IActionResult> ObtenerCatalogoMarcasActivos(Guid idEmpresa, string cadena, string busqueda = "")
        {
            return Ok(await GetCatalogoAsync(
                cadena,
                @"
SELECT id, Codigo, Nombre, Descripcion, Activo, CAST(NULL AS uniqueidentifier) AS RelacionId
FROM dbo.ActivosMarcas
WHERE idEmpresa = @IdEmpresa AND Activo = 1",
                idEmpresa,
                busqueda,
                "Nombre"));
        }

        [HttpGet("ObtenerCatalogoProveedoresActivos")]
        public async Task<IActionResult> ObtenerCatalogoProveedoresActivos(Guid idEmpresa, string cadena, string busqueda = "")
        {
            return Ok(await GetCatalogoAsync(
                cadena,
                @"
SELECT id, Codigo, Nombre, Descripcion, Activo, CAST(NULL AS uniqueidentifier) AS RelacionId
FROM dbo.ActivosProveedores
WHERE idEmpresa = @IdEmpresa AND Activo = 1",
                idEmpresa,
                busqueda,
                "Nombre"));
        }

        [HttpGet("ObtenerCatalogoEstadosOperativos")]
        public async Task<IActionResult> ObtenerCatalogoEstadosOperativos(Guid idEmpresa, string cadena, string busqueda = "")
        {
            return Ok(await GetCatalogoAsync(
                cadena,
                @"
SELECT id, Codigo, Nombre, Descripcion, Activo, CAST(NULL AS uniqueidentifier) AS RelacionId
FROM dbo.ActivosEstadosOperativos
WHERE idEmpresa = @IdEmpresa AND Activo = 1",
                idEmpresa,
                busqueda,
                "Nombre"));
        }

        [HttpGet("ObtenerCatalogoSucursales")]
        public async Task<IActionResult> ObtenerCatalogoSucursales(Guid idEmpresa, string cadena, string busqueda = "")
        {
            return Ok(await GetCatalogoAsync(
                cadena,
                @"
SELECT *
FROM (
    SELECT
        id,
        '' AS Codigo,
        nombre AS Nombre,
        ISNULL(direccion, '') AS Descripcion,
        CAST(CASE WHEN ISNULL(borrado, 0) = 0 THEN 1 ELSE 0 END AS bit) AS Activo,
        CAST(NULL AS uniqueidentifier) AS RelacionId
    FROM dbo.Sucursales
    WHERE idEmpresa = @IdEmpresa AND ISNULL(borrado, 0) = 0
) sucursales",
                idEmpresa,
                busqueda,
                "Nombre"));
        }

        private async Task<List<TDto>> ObtenerCatalogosBasicosAsync<TDto>(string cadena, Guid idEmpresa, string busqueda, string estatus, string tableName)
            where TDto : TipoActivoDto, new()
        {
            using SqlConnection connection = CreateConnection(cadena);
            await connection.OpenAsync();

            StringBuilder query = new StringBuilder($@"
SELECT id, idEmpresa, Codigo, Nombre, Descripcion, Activo, FechaCreacion, FechaActualizacion
FROM {tableName}
WHERE idEmpresa = @IdEmpresa");

            using SqlCommand command = new SqlCommand();
            command.Connection = connection;
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                query.Append(" AND (Codigo LIKE @Busqueda OR Nombre LIKE @Busqueda OR ISNULL(Descripcion, '') LIKE @Busqueda)");
                command.Parameters.AddWithValue("@Busqueda", $"%{busqueda.Trim()}%");
            }

            AppendEstatusFilter(query, estatus);
            query.Append(" ORDER BY Activo DESC, Nombre, Codigo");
            command.CommandText = query.ToString();

            List<TDto> items = new List<TDto>();
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new TDto
                {
                    Id = ReadGuid(reader, "id"),
                    IdEmpresa = ReadGuid(reader, "idEmpresa"),
                    Codigo = ReadString(reader, "Codigo"),
                    Nombre = ReadString(reader, "Nombre"),
                    Descripcion = ReadString(reader, "Descripcion"),
                    Activo = ReadBool(reader, "Activo"),
                    FechaCreacion = ReadDateTime(reader, "FechaCreacion"),
                    FechaActualizacion = ReadDateTime(reader, "FechaActualizacion")
                });
            }

            return items;
        }

        private async Task<TDto?> ObtenerCatalogoBasicoAsync<TDto>(string cadena, Guid idEmpresa, Guid id, string tableName)
            where TDto : TipoActivoDto, new()
        {
            using SqlConnection connection = CreateConnection(cadena);
            await connection.OpenAsync();

            using SqlCommand command = new SqlCommand($@"
SELECT id, idEmpresa, Codigo, Nombre, Descripcion, Activo, FechaCreacion, FechaActualizacion
FROM {tableName}
WHERE idEmpresa = @IdEmpresa AND id = @Id", connection);

            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@Id", id);

            using SqlDataReader reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }

            return new TDto
            {
                Id = ReadGuid(reader, "id"),
                IdEmpresa = ReadGuid(reader, "idEmpresa"),
                Codigo = ReadString(reader, "Codigo"),
                Nombre = ReadString(reader, "Nombre"),
                Descripcion = ReadString(reader, "Descripcion"),
                Activo = ReadBool(reader, "Activo"),
                FechaCreacion = ReadDateTime(reader, "FechaCreacion"),
                FechaActualizacion = ReadDateTime(reader, "FechaActualizacion")
            };
        }

        private async Task<IActionResult> GuardarCatalogoBasicoAsync(string codigo, string nombre, string descripcion, Guid? id, Guid idEmpresa, string cadena, string tableName, string label)
        {
            try
            {
                string validacion = ValidateCatalogoBasico(idEmpresa, codigo, nombre, descripcion);
                if (!string.IsNullOrEmpty(validacion))
                {
                    return BadRequest(new ActivoOperacionResponse { Mensaje = validacion });
                }

                using SqlConnection connection = CreateConnection(cadena);
                await connection.OpenAsync();
                using SqlTransaction transaction = connection.BeginTransaction();

                Guid itemId = id ?? Guid.Empty;
                bool esNuevo = itemId == Guid.Empty;
                if (esNuevo)
                {
                    itemId = Guid.NewGuid();
                }

                if (await ExisteCodigoCatalogoAsync(connection, transaction, idEmpresa, codigo, esNuevo ? null : itemId, tableName))
                {
                    return BadRequest(new ActivoOperacionResponse { Mensaje = $"Ya existe un {label} con el mismo código." });
                }

                DateTime ahora = DateTime.UtcNow;
                if (esNuevo)
                {
                    using SqlCommand insert = new SqlCommand($@"
INSERT INTO {tableName}
    (id, idEmpresa, Codigo, Nombre, Descripcion, Activo, FechaCreacion, FechaActualizacion)
VALUES
    (@Id, @IdEmpresa, @Codigo, @Nombre, @Descripcion, 1, @FechaCreacion, @FechaActualizacion)", connection, transaction);

                    insert.Parameters.AddWithValue("@Id", itemId);
                    insert.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                    insert.Parameters.AddWithValue("@Codigo", codigo.Trim());
                    insert.Parameters.AddWithValue("@Nombre", nombre.Trim());
                    insert.Parameters.AddWithValue("@Descripcion", (descripcion ?? string.Empty).Trim());
                    insert.Parameters.AddWithValue("@FechaCreacion", ahora);
                    insert.Parameters.AddWithValue("@FechaActualizacion", ahora);
                    await insert.ExecuteNonQueryAsync();
                }
                else
                {
                    using SqlCommand update = new SqlCommand($@"
UPDATE {tableName}
SET
    Codigo = @Codigo,
    Nombre = @Nombre,
    Descripcion = @Descripcion,
    FechaActualizacion = @FechaActualizacion
WHERE idEmpresa = @IdEmpresa AND id = @Id", connection, transaction);

                    update.Parameters.AddWithValue("@Id", itemId);
                    update.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                    update.Parameters.AddWithValue("@Codigo", codigo.Trim());
                    update.Parameters.AddWithValue("@Nombre", nombre.Trim());
                    update.Parameters.AddWithValue("@Descripcion", (descripcion ?? string.Empty).Trim());
                    update.Parameters.AddWithValue("@FechaActualizacion", ahora);

                    int rowsAffected = await update.ExecuteNonQueryAsync();
                    if (rowsAffected == 0)
                    {
                        transaction.Rollback();
                        return NotFound(new ActivoOperacionResponse { Mensaje = $"El {label} no está disponible." });
                    }
                }

                transaction.Commit();
                return Ok(new ActivoOperacionResponse { Mensaje = esNuevo ? $"El {label} fue registrado." : $"El {label} fue actualizado." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ActivoOperacionResponse { Mensaje = $"Error interno del servidor: {ex.Message}" });
            }
        }

        private async Task<IActionResult> CambiarEstatusCatalogoBasicoAsync(Guid idEmpresa, Guid id, string cadena, string tableName, string label, bool activar)
        {
            try
            {
                using SqlConnection connection = CreateConnection(cadena);
                await connection.OpenAsync();

                using SqlCommand command = new SqlCommand($@"
UPDATE {tableName}
SET
    Activo = @Activo,
    FechaActualizacion = @FechaActualizacion
WHERE idEmpresa = @IdEmpresa AND id = @Id AND Activo <> @Activo", connection);

                command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                command.Parameters.AddWithValue("@Id", id);
                command.Parameters.AddWithValue("@Activo", activar);
                command.Parameters.AddWithValue("@FechaActualizacion", DateTime.UtcNow);

                int rowsAffected = await command.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    return NotFound(new ActivoOperacionResponse { Mensaje = $"El {label} no está disponible para actualizar su estatus." });
                }

                return Ok(new ActivoOperacionResponse { Mensaje = activar ? $"El {label} fue activado." : $"El {label} fue dado de baja." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ActivoOperacionResponse { Mensaje = $"Error interno del servidor: {ex.Message}" });
            }
        }

        private async Task<List<ActivoMultimediaDto>> ObtenerMultimediaActivaAsync(SqlConnection connection, Guid idActivo)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT id, idActivo, TipoMultimedia, Foto, Video, Documento, NombreOriginal, NombreAlmacenado, Extension, MimeType, UrlFirebase, PesoBytes, Orden, Activo, FechaCreacion, FechaActualizacion
FROM dbo.ActivosMultimedia
WHERE idActivo = @IdActivo AND Activo = 1
ORDER BY
    CASE
        WHEN Foto = 1 THEN 1
        WHEN Video = 1 THEN 2
        WHEN Documento = 1 THEN 3
        ELSE 4
    END,
    Orden,
    FechaCreacion", connection);

            command.Parameters.AddWithValue("@IdActivo", idActivo);
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            return await ReadMultimediaAsync(reader);
        }

        private async Task<List<ActivoMultimediaDto>> ObtenerMultimediaActivaAsync(SqlConnection connection, SqlTransaction transaction, Guid idActivo)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT id, idActivo, TipoMultimedia, Foto, Video, Documento, NombreOriginal, NombreAlmacenado, Extension, MimeType, UrlFirebase, PesoBytes, Orden, Activo, FechaCreacion, FechaActualizacion
FROM dbo.ActivosMultimedia
WHERE idActivo = @IdActivo AND Activo = 1
ORDER BY
    CASE
        WHEN Foto = 1 THEN 1
        WHEN Video = 1 THEN 2
        WHEN Documento = 1 THEN 3
        ELSE 4
    END,
    Orden,
    FechaCreacion", connection, transaction);

            command.Parameters.AddWithValue("@IdActivo", idActivo);
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            return await ReadMultimediaAsync(reader);
        }

        private static async Task<List<ActivoMultimediaDto>> ReadMultimediaAsync(SqlDataReader reader)
        {
            List<ActivoMultimediaDto> items = new List<ActivoMultimediaDto>();
            while (await reader.ReadAsync())
            {
                items.Add(MapMultimedia(reader));
            }

            return items;
        }

        private async Task<List<ActivoMultimediaDto>> ResolverMultimediaFinalAsync(
            List<ActivoMultimediaDto> multimediaActual,
            List<ActivoMultimediaGuardarRequest>? multimediaSolicitada,
            string empresa,
            Guid idActivo,
            List<FirebaseCleanupItem> movedTempFiles)
        {
            Dictionary<Guid, ActivoMultimediaDto> existentes = multimediaActual.ToDictionary(item => item.Id);
            List<ActivoMultimediaDto> resultado = new List<ActivoMultimediaDto>();

            foreach (ActivoMultimediaGuardarRequest item in multimediaSolicitada ?? new List<ActivoMultimediaGuardarRequest>())
            {
                string tipo = NormalizeTipoMultimedia(item.TipoMultimedia);
                if (string.IsNullOrWhiteSpace(tipo))
                {
                    continue;
                }

                if (item.Id.HasValue && item.Id.Value != Guid.Empty && existentes.TryGetValue(item.Id.Value, out ActivoMultimediaDto? existente))
                {
                    resultado.Add(new ActivoMultimediaDto
                    {
                        Id = existente.Id,
                        IdActivo = existente.IdActivo,
                        TipoMultimedia = existente.TipoMultimedia,
                        Foto = existente.Foto,
                        Video = existente.Video,
                        Documento = existente.Documento,
                        NombreOriginal = existente.NombreOriginal,
                        NombreAlmacenado = existente.NombreAlmacenado,
                        Extension = existente.Extension,
                        MimeType = existente.MimeType,
                        UrlFirebase = existente.UrlFirebase,
                        PesoBytes = existente.PesoBytes,
                        Orden = item.Orden,
                        Activo = true,
                        FechaCreacion = existente.FechaCreacion,
                        FechaActualizacion = DateTime.UtcNow
                    });

                    continue;
                }

                if (string.IsNullOrWhiteSpace(item.TemporalToken))
                {
                    continue;
                }

                TemporalMultimediaTokenPayload temporal = TryParseTemporalToken(item.TemporalToken)
                    ?? throw new InvalidOperationException("Se detectó una referencia temporal inválida.");
                if (!string.Equals(temporal.TipoMultimedia, tipo, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("La referencia temporal no coincide con el tipo de evidencia.");
                }

                UploadedMultimediaPayload uploaded = await MoveTemporalMediaToFinalAsync(empresa, idActivo, temporal);
                movedTempFiles.Add(new FirebaseCleanupItem
                {
                    FolderName = temporal.FolderName,
                    StoredName = temporal.NombreAlmacenado
                });

                resultado.Add(new ActivoMultimediaDto
                {
                    Id = Guid.NewGuid(),
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
            }

            return resultado
                .OrderBy(item => item.Foto ? 1 : item.Video ? 2 : 3)
                .ThenBy(item => item.Orden)
                .ThenBy(item => item.NombreOriginal)
                .ToList();
        }

        private async Task CleanupUploadedFirebaseFilesAsync(List<FirebaseCleanupItem> uploadedFiles)
        {
            if (uploadedFiles == null || uploadedFiles.Count == 0)
            {
                return;
            }

            List<FirebaseCleanupItem> filesToDelete = uploadedFiles
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
                    Providers = new FirebaseAuthProvider[]
                    {
                        new EmailProvider()
                    }
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

                foreach (FirebaseCleanupItem file in filesToDelete)
                {
                    await storage.Child(file.FolderName).Child(file.StoredName).DeleteAsync();
                }

                authClient.SignOut();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Activos][CleanupFirebase] {ex}");
            }
        }

        private async Task SincronizarMultimediaAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            Guid idActivo,
            List<ActivoMultimediaDto> multimediaActual,
            List<ActivoMultimediaDto> multimediaFinal,
            DateTime ahora)
        {
            HashSet<Guid> idsFinales = multimediaFinal
                .Where(item => item.Id != Guid.Empty)
                .Select(item => item.Id)
                .ToHashSet();

            foreach (ActivoMultimediaDto existente in multimediaActual.Where(item => !idsFinales.Contains(item.Id)))
            {
                using SqlCommand deactivate = new SqlCommand(@"
UPDATE dbo.ActivosMultimedia
SET
    Activo = 0,
    FechaActualizacion = @FechaActualizacion
WHERE id = @Id", connection, transaction);

                deactivate.Parameters.AddWithValue("@Id", existente.Id);
                deactivate.Parameters.AddWithValue("@FechaActualizacion", ahora);
                await deactivate.ExecuteNonQueryAsync();
            }

            foreach (ActivoMultimediaDto item in multimediaFinal)
            {
                bool yaExiste = multimediaActual.Any(actual => actual.Id == item.Id);
                if (yaExiste)
                {
                    using SqlCommand update = new SqlCommand(@"
UPDATE dbo.ActivosMultimedia
SET
    Orden = @Orden,
    Activo = 1,
    FechaActualizacion = @FechaActualizacion
WHERE id = @Id", connection, transaction);

                    update.Parameters.AddWithValue("@Id", item.Id);
                    update.Parameters.AddWithValue("@Orden", item.Orden);
                    update.Parameters.AddWithValue("@FechaActualizacion", ahora);
                    await update.ExecuteNonQueryAsync();
                }
                else
                {
                    using SqlCommand insert = new SqlCommand(@"
INSERT INTO dbo.ActivosMultimedia
    (id, idActivo, TipoMultimedia, Foto, Video, Documento, NombreOriginal, NombreAlmacenado, Extension, MimeType, UrlFirebase, PesoBytes, Orden, Activo, FechaCreacion, FechaActualizacion)
VALUES
    (@Id, @IdActivo, @TipoMultimedia, @Foto, @Video, @Documento, @NombreOriginal, @NombreAlmacenado, @Extension, @MimeType, @UrlFirebase, @PesoBytes, @Orden, 1, @FechaCreacion, @FechaActualizacion)", connection, transaction);

                    insert.Parameters.AddWithValue("@Id", item.Id);
                    insert.Parameters.AddWithValue("@IdActivo", idActivo);
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

        private async Task<UploadedMultimediaPayload> UploadMediaToFirebaseAsync(
            string folderName,
            string storedName,
            byte[] fileBytes,
            string tipoMultimedia,
            string nombreOriginal,
            string extension,
            string mimeType,
            long pesoBytes)
        {
            string normalizedExtension = NormalizeExtension(extension, nombreOriginal, mimeType, tipoMultimedia);
            var config = new FirebaseAuthConfig
            {
                ApiKey = _configuration.GetValue<string>("fireBdata:fireApiKey"),
                AuthDomain = _configuration.GetValue<string>("fireBdata:fireAuthDomain"),
                Providers = new FirebaseAuthProvider[]
                {
                    new EmailProvider()
                }
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

            string downloadUrl = await storage
                .Child(folderName)
                .Child(storedName)
                .PutAsync(stream);

            authClient.SignOut();

            return new UploadedMultimediaPayload
            {
                FolderName = folderName,
                NombreOriginal = NormalizeArchivoText(nombreOriginal, NombreArchivoLength, storedName),
                NombreAlmacenado = storedName,
                Extension = normalizedExtension,
                MimeType = NormalizeArchivoText(mimeType, MimeTypeLength, ResolveMimeTypeByTipo(tipoMultimedia)),
                UrlFirebase = NormalizeArchivoText(downloadUrl, UrlFirebaseLength, string.Empty),
                PesoBytes = pesoBytes > 0 ? pesoBytes : fileBytes.LongLength
            };
        }

        private async Task<List<CatalogoActivoDto>> GetCatalogoAsync(string cadena, string baseQuery, Guid idEmpresa, string busqueda, string orderBy)
        {
            using SqlConnection connection = CreateConnection(cadena);
            await connection.OpenAsync();

            StringBuilder query = new StringBuilder($@"
SELECT id, Codigo, Nombre, Descripcion, Activo, RelacionId
FROM (
{baseQuery}
) catalogo
WHERE 1 = 1");
            using SqlCommand command = new SqlCommand();
            command.Connection = connection;
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                query.Append(" AND (Codigo LIKE @Busqueda OR Nombre LIKE @Busqueda OR ISNULL(Descripcion, '') LIKE @Busqueda)");
                command.Parameters.AddWithValue("@Busqueda", $"%{busqueda.Trim()}%");
            }

            query.Append($" ORDER BY {orderBy}, Codigo");
            command.CommandText = query.ToString();

            List<CatalogoActivoDto> result = new List<CatalogoActivoDto>();
            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new CatalogoActivoDto
                {
                    Id = ReadGuid(reader, "id"),
                    Codigo = ReadString(reader, "Codigo"),
                    Nombre = ReadString(reader, "Nombre"),
                    Descripcion = ReadString(reader, "Descripcion"),
                    Activo = ReadBool(reader, "Activo"),
                    RelacionId = ReadNullableGuid(reader, "RelacionId")
                });
            }

            return result;
        }

        private static SqlConnection CreateConnection(string cadena)
        {
            byte[] data = Convert.FromBase64String(cadena);
            string decoded = Encoding.UTF8.GetString(data);
            return new SqlConnection(decoded);
        }

        private static string ValidateActivoRequest(ActivoGuardarRequest request)
        {
            if (request.IdEmpresa == Guid.Empty)
            {
                return "No fue posible resolver la empresa activa.";
            }

            if (request.IdTipoActivo == Guid.Empty)
            {
                return "Selecciona un tipo de activo.";
            }

            if (request.IdEstadoOperativo == Guid.Empty)
            {
                return "Selecciona un estado operativo.";
            }

            if (request.IdSucursal == Guid.Empty)
            {
                return "Selecciona una sucursal.";
            }

            if (request.IdMarca == Guid.Empty)
            {
                return "Selecciona una marca.";
            }

            if (request.IdProveedor == Guid.Empty)
            {
                return "Selecciona un proveedor.";
            }

            if (string.IsNullOrWhiteSpace(request.Codigo) || request.Codigo.Trim().Length > CodigoActivoLength)
            {
                return $"Captura un código válido de hasta {CodigoActivoLength} caracteres.";
            }

            if (string.IsNullOrWhiteSpace(request.Nombre) || request.Nombre.Trim().Length > NombreActivoLength)
            {
                return $"Captura un nombre válido de hasta {NombreActivoLength} caracteres.";
            }

            if ((request.Tag ?? string.Empty).Trim().Length > TagLength)
            {
                return $"La etiqueta no puede exceder {TagLength} caracteres.";
            }

            if ((request.NumeroSerie ?? string.Empty).Trim().Length > NumeroSerieLength)
            {
                return $"El número de serie no puede exceder {NumeroSerieLength} caracteres.";
            }

            if ((request.Descripcion ?? string.Empty).Trim().Length > DescripcionActivoLength)
            {
                return $"La descripción no puede exceder {DescripcionActivoLength} caracteres.";
            }

            return string.Empty;
        }

        private static string ValidateCatalogoBasico(Guid idEmpresa, string codigo, string nombre, string descripcion)
        {
            if (idEmpresa == Guid.Empty)
            {
                return "No fue posible resolver la empresa activa.";
            }

            if (string.IsNullOrWhiteSpace(codigo) || codigo.Trim().Length > CodigoCatalogoLength)
            {
                return $"Captura un código válido de hasta {CodigoCatalogoLength} caracteres.";
            }

            if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length > NombreCatalogoLength)
            {
                return $"Captura un nombre válido de hasta {NombreCatalogoLength} caracteres.";
            }

            if ((descripcion ?? string.Empty).Trim().Length > DescripcionCatalogoLength)
            {
                return $"La descripción no puede exceder {DescripcionCatalogoLength} caracteres.";
            }

            return string.Empty;
        }

        private static string ValidateEstadoOperativoRequest(EstadoOperativoGuardarRequest request)
        {
            string baseValidation = ValidateCatalogoBasico(request.IdEmpresa, request.Codigo, request.Nombre, request.Descripcion);
            if (!string.IsNullOrEmpty(baseValidation))
            {
                return baseValidation;
            }

            if (!request.Orden.HasValue || request.Orden.Value <= 0)
            {
                return "Captura un orden entero mayor que cero.";
            }

            return string.Empty;
        }

        private static string ValidateMultimedia(List<ActivoMultimediaDto> multimedia)
        {
            int fotos = multimedia.Count(item => item.Foto);
            int videos = multimedia.Count(item => item.Video);
            int documentos = multimedia.Count(item => item.Documento);

            if (fotos < 1 || fotos > 3)
            {
                return "Captura entre 1 y 3 fotos.";
            }

            if (videos > 1)
            {
                return "Solo se permite 1 video por activo.";
            }

            if (documentos < 1 || documentos > 3)
            {
                return "Captura entre 1 y 3 documentos.";
            }

            if (multimedia.Any(item => !TiposPermitidos.Contains(NormalizeTipoMultimedia(item.TipoMultimedia))))
            {
                return "Se detectó un tipo de multimedia no soportado.";
            }

            return string.Empty;
        }

        private static void AppendGuidFilter(StringBuilder query, SqlCommand command, string columnName, string parameterName, Guid? value)
        {
            if (value.HasValue && value.Value != Guid.Empty)
            {
                query.Append($" AND {columnName} = {parameterName}");
                command.Parameters.AddWithValue(parameterName, value.Value);
            }
        }

        private static void AppendEstatusFilter(StringBuilder query, string estatus)
        {
            switch ((estatus ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "activos":
                    query.Append(" AND Activo = 1");
                    break;
                case "inactivos":
                    query.Append(" AND Activo = 0");
                    break;
            }
        }

        private static string BuildIdentityKey(Guid idActivo)
        {
            return $"AST-{idActivo:N}".ToUpperInvariant();
        }

        private static async Task<bool> ExisteCodigoActivoAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, string codigo, Guid? excludeId)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT COUNT(1)
FROM dbo.Activos
WHERE idEmpresa = @IdEmpresa AND Codigo = @Codigo AND (@ExcludeId IS NULL OR id <> @ExcludeId)", connection, transaction);

            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@Codigo", codigo.Trim());
            command.Parameters.AddWithValue("@ExcludeId", excludeId.HasValue ? excludeId.Value : DBNull.Value);
            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        private static async Task<bool> ExisteCodigoCatalogoAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, string codigo, Guid? excludeId, string tableName)
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

        private static async Task<bool> ExisteTipoActivoAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idTipoActivo)
        {
            return await ExisteCatalogoActivoAsync(connection, transaction, idEmpresa, idTipoActivo, "dbo.ActivosTipos");
        }

        private static async Task<bool> ExisteEstadoOperativoAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idEstadoOperativo)
        {
            return await ExisteCatalogoActivoAsync(connection, transaction, idEmpresa, idEstadoOperativo, "dbo.ActivosEstadosOperativos");
        }

        private static async Task<bool> ExisteMarcaAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idMarca)
        {
            return await ExisteCatalogoActivoAsync(connection, transaction, idEmpresa, idMarca, "dbo.ActivosMarcas");
        }

        private static async Task<bool> ExisteProveedorAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idProveedor)
        {
            return await ExisteCatalogoActivoAsync(connection, transaction, idEmpresa, idProveedor, "dbo.ActivosProveedores");
        }

        private static async Task<bool> ExisteCatalogoActivoAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid id, string tableName)
        {
            using SqlCommand command = new SqlCommand($"SELECT COUNT(1) FROM {tableName} WHERE idEmpresa = @IdEmpresa AND id = @Id AND Activo = 1", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@Id", id);
            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        private static async Task<bool> ExisteSucursalAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, Guid idSucursal)
        {
            using SqlCommand command = new SqlCommand("SELECT COUNT(1) FROM dbo.Sucursales WHERE idEmpresa = @IdEmpresa AND id = @Id AND ISNULL(borrado, 0) = 0", connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@Id", idSucursal);
            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        private static ActivoListadoDto MapActivoListado(SqlDataReader reader)
        {
            return new ActivoListadoDto
            {
                Id = ReadGuid(reader, "id"),
                IdEmpresa = ReadGuid(reader, "idEmpresa"),
                IdentityKey = ReadString(reader, "identityKey"),
                Codigo = ReadString(reader, "Codigo"),
                Nombre = ReadString(reader, "Nombre"),
                IdTipoActivo = ReadGuid(reader, "idTipoActivo"),
                TipoActivo = ReadString(reader, "tipoActivo"),
                IdEstadoOperativo = ReadGuid(reader, "idEstadoOperativo"),
                EstadoOperativo = ReadString(reader, "estadoOperativo"),
                IdSucursal = ReadGuid(reader, "idSucursal"),
                Sucursal = ReadString(reader, "sucursal"),
                IdMarca = ReadNullableGuid(reader, "idMarca"),
                Marca = ReadString(reader, "marca"),
                IdProveedor = ReadNullableGuid(reader, "idProveedor"),
                Proveedor = ReadString(reader, "proveedor"),
                Tag = ReadString(reader, "Tag"),
                NumeroSerie = ReadString(reader, "NumeroSerie"),
                Descripcion = ReadString(reader, "Descripcion"),
                CantidadFotos = ReadInt(reader, "CantidadFotos"),
                CantidadVideos = ReadInt(reader, "CantidadVideos"),
                CantidadDocumentos = ReadInt(reader, "CantidadDocumentos"),
                Activo = ReadBool(reader, "Activo"),
                FechaArchivado = ReadNullableDateTime(reader, "FechaArchivado"),
                FechaCreacion = ReadDateTime(reader, "FechaCreacion"),
                FechaActualizacion = ReadDateTime(reader, "FechaActualizacion")
            };
        }

        private static ActivoDetalleDto MapActivoDetalle(SqlDataReader reader)
        {
            ActivoListadoDto baseDto = MapActivoListado(reader);
            return new ActivoDetalleDto
            {
                Id = baseDto.Id,
                IdEmpresa = baseDto.IdEmpresa,
                IdentityKey = baseDto.IdentityKey,
                Codigo = baseDto.Codigo,
                Nombre = baseDto.Nombre,
                IdTipoActivo = baseDto.IdTipoActivo,
                TipoActivo = baseDto.TipoActivo,
                IdEstadoOperativo = baseDto.IdEstadoOperativo,
                EstadoOperativo = baseDto.EstadoOperativo,
                IdSucursal = baseDto.IdSucursal,
                Sucursal = baseDto.Sucursal,
                IdMarca = baseDto.IdMarca,
                Marca = baseDto.Marca,
                IdProveedor = baseDto.IdProveedor,
                Proveedor = baseDto.Proveedor,
                Tag = baseDto.Tag,
                NumeroSerie = baseDto.NumeroSerie,
                Descripcion = baseDto.Descripcion,
                CantidadFotos = baseDto.CantidadFotos,
                CantidadVideos = baseDto.CantidadVideos,
                CantidadDocumentos = baseDto.CantidadDocumentos,
                Activo = baseDto.Activo,
                FechaArchivado = baseDto.FechaArchivado,
                FechaCreacion = baseDto.FechaCreacion,
                FechaActualizacion = baseDto.FechaActualizacion
            };
        }

        private static ActivoMultimediaDto MapMultimedia(SqlDataReader reader)
        {
            return new ActivoMultimediaDto
            {
                Id = ReadGuid(reader, "id"),
                IdActivo = ReadGuid(reader, "idActivo"),
                TipoMultimedia = NormalizeTipoMultimedia(ReadString(reader, "TipoMultimedia")),
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
            };
        }

        private static string ValidateTemporalUpload(string tipoMultimedia, IFormFile archivo)
        {
            long maxBytes = ResolveMaxBytesByTipo(tipoMultimedia);
            if (archivo.Length <= 0)
            {
                return "Selecciona un archivo válido para cargar.";
            }

            if (archivo.Length > maxBytes)
            {
                return tipoMultimedia switch
                {
                    "foto" => "La foto excede el máximo permitido de 10 MB.",
                    "video" => "El video excede el máximo permitido de 200 MB.",
                    "documento" => "El documento excede el máximo permitido de 25 MB.",
                    _ => "El archivo excede el tamaño permitido."
                };
            }

            string extension = NormalizeExtension(Path.GetExtension(archivo.FileName), archivo.FileName, archivo.ContentType, tipoMultimedia);
            string mimeType = (archivo.ContentType ?? string.Empty).Trim().ToLowerInvariant();
            if (tipoMultimedia == "foto")
            {
                if (!new[] { ".jpg", ".jpeg", ".png", ".webp", ".heic" }.Contains(extension))
                {
                    return "Selecciona una foto válida para el activo.";
                }

                if (!string.IsNullOrWhiteSpace(mimeType) && !mimeType.StartsWith("image/", StringComparison.Ordinal))
                {
                    return "Selecciona una foto válida para el activo.";
                }
            }

            if (tipoMultimedia == "video")
            {
                if (!new[] { ".webm", ".mp4", ".mov" }.Contains(extension))
                {
                    return "Selecciona un video válido para el activo.";
                }

                if (!string.IsNullOrWhiteSpace(mimeType) && !mimeType.StartsWith("video/", StringComparison.Ordinal))
                {
                    return "Selecciona un video válido para el activo.";
                }
            }

            if (tipoMultimedia == "documento" && !new[] { ".pdf", ".doc", ".docx" }.Contains(extension))
            {
                return "Selecciona un documento PDF o Word válido.";
            }

            return string.Empty;
        }

        private static async Task<byte[]> ReadFileBytesAsync(IFormFile archivo)
        {
            using MemoryStream memory = new MemoryStream();
            await archivo.CopyToAsync(memory);
            return memory.ToArray();
        }

        private static string ValidateFileSignature(string tipoMultimedia, string fileName, string mimeType, byte[] fileBytes)
        {
            if (fileBytes == null || fileBytes.Length == 0)
            {
                return "Se detectó un archivo vacío. Selecciona una evidencia válida.";
            }

            string extension = NormalizeExtension(Path.GetExtension(fileName), fileName, mimeType, tipoMultimedia);
            bool isValid = tipoMultimedia switch
            {
                "foto" => IsValidPhoto(fileBytes, extension),
                "video" => IsValidVideo(fileBytes, extension),
                "documento" => IsValidDocument(fileBytes, extension),
                _ => false
            };

            return isValid ? string.Empty : "El archivo seleccionado no coincide con el formato permitido.";
        }

        private static bool IsValidPhoto(byte[] fileBytes, string extension)
        {
            if (extension == ".jpg" || extension == ".jpeg")
            {
                return fileBytes.Length > 3 && fileBytes[0] == 0xFF && fileBytes[1] == 0xD8 && fileBytes[2] == 0xFF;
            }

            if (extension == ".png")
            {
                return fileBytes.Length > 8
                    && fileBytes[0] == 0x89
                    && fileBytes[1] == 0x50
                    && fileBytes[2] == 0x4E
                    && fileBytes[3] == 0x47;
            }

            if (extension == ".webp")
            {
                return fileBytes.Length > 12
                    && Encoding.ASCII.GetString(fileBytes, 0, 4) == "RIFF"
                    && Encoding.ASCII.GetString(fileBytes, 8, 4) == "WEBP";
            }

            if (extension == ".heic")
            {
                return fileBytes.Length > 12
                    && Encoding.ASCII.GetString(fileBytes, 4, 4) == "ftyp"
                    && new[] { "heic", "heix", "hevc", "heim", "mif1", "msf1" }
                        .Contains(Encoding.ASCII.GetString(fileBytes, 8, 4));
            }

            return false;
        }

        private static bool IsValidVideo(byte[] fileBytes, string extension)
        {
            if (extension == ".webm")
            {
                return fileBytes.Length > 4
                    && fileBytes[0] == 0x1A
                    && fileBytes[1] == 0x45
                    && fileBytes[2] == 0xDF
                    && fileBytes[3] == 0xA3;
            }

            if (extension == ".mp4" || extension == ".mov")
            {
                return fileBytes.Length > 12
                    && Encoding.ASCII.GetString(fileBytes, 4, 4) == "ftyp";
            }

            return false;
        }

        private static bool IsValidDocument(byte[] fileBytes, string extension)
        {
            if (extension == ".pdf")
            {
                return fileBytes.Length > 4
                    && fileBytes[0] == 0x25
                    && fileBytes[1] == 0x50
                    && fileBytes[2] == 0x44
                    && fileBytes[3] == 0x46;
            }

            if (extension == ".doc")
            {
                return fileBytes.Length > 8
                    && fileBytes[0] == 0xD0
                    && fileBytes[1] == 0xCF
                    && fileBytes[2] == 0x11
                    && fileBytes[3] == 0xE0;
            }

            if (extension == ".docx")
            {
                return fileBytes.Length > 4
                    && fileBytes[0] == 0x50
                    && fileBytes[1] == 0x4B
                    && (fileBytes[2] == 0x03 || fileBytes[2] == 0x05 || fileBytes[2] == 0x07)
                    && (fileBytes[3] == 0x04 || fileBytes[3] == 0x06 || fileBytes[3] == 0x08);
            }

            return false;
        }

        private static long ResolveMaxBytesByTipo(string tipoMultimedia)
        {
            return tipoMultimedia switch
            {
                "foto" => FotoMaxBytes,
                "video" => VideoMaxBytes,
                "documento" => DocumentoMaxBytes,
                _ => FotoMaxBytes
            };
        }

        private static string NormalizeOperationKey(string value)
        {
            string raw = string.IsNullOrWhiteSpace(value) ? Guid.NewGuid().ToString("N") : value.Trim();
            StringBuilder builder = new StringBuilder(raw.Length);
            foreach (char character in raw)
            {
                if (char.IsLetterOrDigit(character) || character == '-' || character == '_')
                {
                    builder.Append(character);
                }
            }

            string result = builder.ToString();
            return string.IsNullOrWhiteSpace(result) ? Guid.NewGuid().ToString("N") : result[..Math.Min(result.Length, 80)];
        }

        private static string BuildTemporalFolderName(string empresa, string operationKey, string tipoMultimedia)
        {
            return $"{empresa}/Activos/Temporal/{operationKey}/{ResolveFolderSegmentByTipo(tipoMultimedia)}";
        }

        private static string BuildFinalFolderName(string empresa, Guid idActivo, string tipoMultimedia)
        {
            return $"{empresa}/Activos/{idActivo:N}/{ResolveFolderSegmentByTipo(tipoMultimedia)}";
        }

        private static string ResolveFolderSegmentByTipo(string tipoMultimedia)
        {
            return tipoMultimedia switch
            {
                "foto" => "Fotos",
                "video" => "Video",
                "documento" => "Documentos",
                _ => "Archivos"
            };
        }

        private static string BuildStoredName(string tipoMultimedia, string fileName, string mimeType)
        {
            string extension = NormalizeExtension(Path.GetExtension(fileName), fileName, mimeType, tipoMultimedia);
            return $"{Guid.NewGuid():N}{extension}";
        }

        private async Task<UploadedMultimediaPayload> MoveTemporalMediaToFinalAsync(string empresa, Guid idActivo, TemporalMultimediaTokenPayload temporal)
        {
            byte[] fileBytes = await DownloadFileBytesAsync(temporal.UrlFirebase);
            string validation = ValidateFileSignature(temporal.TipoMultimedia, temporal.NombreOriginal, temporal.MimeType, fileBytes);
            if (!string.IsNullOrWhiteSpace(validation))
            {
                throw new InvalidOperationException(validation);
            }

            return await UploadMediaToFirebaseAsync(
                folderName: BuildFinalFolderName(empresa, idActivo, temporal.TipoMultimedia),
                storedName: BuildStoredName(temporal.TipoMultimedia, temporal.NombreOriginal, temporal.MimeType),
                fileBytes: fileBytes,
                tipoMultimedia: temporal.TipoMultimedia,
                nombreOriginal: temporal.NombreOriginal,
                extension: temporal.Extension,
                mimeType: temporal.MimeType,
                pesoBytes: temporal.PesoBytes);
        }

        private static async Task<byte[]> DownloadFileBytesAsync(string url)
        {
            using HttpClient client = new HttpClient();
            using HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync();
        }

        private string CreateTemporalToken(TemporalMultimediaTokenPayload payload)
        {
            string json = JsonSerializer.Serialize(payload);
            string payloadPart = ToBase64Url(Encoding.UTF8.GetBytes(json));
            string signaturePart = ToBase64Url(SignToken(payloadPart));
            return $"{payloadPart}.{signaturePart}";
        }

        private TemporalMultimediaTokenPayload? TryParseTemporalToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            string[] parts = token.Split('.', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                return null;
            }

            byte[] expectedSignature = SignToken(parts[0]);
            byte[] providedSignature;
            try
            {
                providedSignature = FromBase64Url(parts[1]);
            }
            catch
            {
                return null;
            }

            if (!CryptographicOperations.FixedTimeEquals(expectedSignature, providedSignature))
            {
                return null;
            }

            try
            {
                TemporalMultimediaTokenPayload? payload = JsonSerializer.Deserialize<TemporalMultimediaTokenPayload>(Encoding.UTF8.GetString(FromBase64Url(parts[0])));
                if (payload == null || payload.ExpiraUtc < DateTime.UtcNow || string.IsNullOrWhiteSpace(payload.FolderName) || string.IsNullOrWhiteSpace(payload.NombreAlmacenado))
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

        private byte[] SignToken(string payloadPart)
        {
            using HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(ResolveTemporalTokenSecret()));
            return hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadPart));
        }

        private string ResolveTemporalTokenSecret()
        {
            return _configuration.GetValue<string>("Activos:MultimediaTokenSecret")
                ?? _configuration.GetValue<string>("fireBdata:fireApiKey")
                ?? "ACTIVOS_MULTIMEDIA_TEMP";
        }

        private static string ToBase64Url(byte[] input)
        {
            return Convert.ToBase64String(input).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static byte[] FromBase64Url(string input)
        {
            string normalized = input.Replace('-', '+').Replace('_', '/');
            switch (normalized.Length % 4)
            {
                case 2:
                    normalized += "==";
                    break;
                case 3:
                    normalized += "=";
                    break;
            }

            return Convert.FromBase64String(normalized);
        }

        private static string NormalizeTipoMultimedia(string value)
        {
            string normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
            return normalized switch
            {
                "foto" => "foto",
                "video" => "video",
                "documento" => "documento",
                "archivo" => "documento",
                "pdf" => "documento",
                "word" => "documento",
                _ => string.Empty
            };
        }

        private static string NormalizeExtension(string extension, string nombreOriginal, string mimeType, string tipoMultimedia)
        {
            string normalized = (extension ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                normalized = Path.GetExtension(nombreOriginal ?? string.Empty);
            }

            if (string.IsNullOrWhiteSpace(normalized))
            {
                normalized = tipoMultimedia switch
                {
                    "foto" when mimeType.Contains("png", StringComparison.OrdinalIgnoreCase) => ".png",
                    "foto" => ".jpg",
                    "video" => ".mp4",
                    "documento" when mimeType.Contains("word", StringComparison.OrdinalIgnoreCase) => ".docx",
                    "documento" => ".pdf",
                    _ => ".bin"
                };
            }

            normalized = normalized.StartsWith(".") ? normalized : "." + normalized;
            normalized = normalized.ToLowerInvariant();
            if (normalized.Length > ExtensionLength)
            {
                normalized = normalized[..ExtensionLength];
            }

            return normalized;
        }

        private static string NormalizeArchivoText(string value, int maxLength, string fallback)
        {
            string normalized = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
            if (normalized.Length > maxLength)
            {
                normalized = normalized[..maxLength];
            }

            return normalized;
        }

        private static string ResolveMimeTypeByTipo(string tipoMultimedia)
        {
            return tipoMultimedia switch
            {
                "foto" => "image/jpeg",
                "video" => "video/webm",
                "documento" => "application/pdf",
                _ => "application/octet-stream"
            };
        }

        private static string ResolveTemporalUploadErrorMessage(Exception ex)
        {
            string message = ex.Message ?? string.Empty;
            if (message.Contains("Request body too large", StringComparison.OrdinalIgnoreCase)
                || message.Contains("request body", StringComparison.OrdinalIgnoreCase)
                || message.Contains("413", StringComparison.OrdinalIgnoreCase))
            {
                return "No fue posible cargar la evidencia porque excede el límite permitido.";
            }

            return "No fue posible cargar temporalmente la evidencia seleccionada.";
        }

        private static string ResolveGuardarActivoErrorMessage(Exception ex)
        {
            string message = ex.Message ?? string.Empty;
            if (message.Contains("Request body too large", StringComparison.OrdinalIgnoreCase)
                || message.Contains("request body", StringComparison.OrdinalIgnoreCase)
                || message.Contains("413", StringComparison.OrdinalIgnoreCase))
            {
                return "No fue posible completar el registro porque la carga de evidencias excedió el límite permitido.";
            }

            if (message.Contains("base64", StringComparison.OrdinalIgnoreCase)
                || message.Contains("invalid character", StringComparison.OrdinalIgnoreCase)
                || message.Contains("input is not a valid", StringComparison.OrdinalIgnoreCase))
            {
                return "No fue posible procesar una de las evidencias seleccionadas.";
            }

            return "No fue posible completar el registro del activo con sus evidencias.";
        }

        private static string ReadString(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? string.Empty : Convert.ToString(reader.GetValue(ordinal))?.Trim() ?? string.Empty;
        }

        private static Guid ReadGuid(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? Guid.Empty : reader.GetGuid(ordinal);
        }

        private static Guid? ReadNullableGuid(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetGuid(ordinal);
        }

        private static bool ReadBool(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return !reader.IsDBNull(ordinal) && reader.GetBoolean(ordinal);
        }

        private static DateTime ReadDateTime(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.GetDateTime(ordinal);
        }

        private static DateTime? ReadNullableDateTime(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
        }

        private static int ReadInt(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? 0 : Convert.ToInt32(reader.GetValue(ordinal));
        }

        private static long ReadLong(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? 0 : Convert.ToInt64(reader.GetValue(ordinal));
        }

        private sealed class UploadedMultimediaPayload
        {
            public string FolderName { get; set; } = string.Empty;
            public string NombreOriginal { get; set; } = string.Empty;
            public string NombreAlmacenado { get; set; } = string.Empty;
            public string Extension { get; set; } = string.Empty;
            public string MimeType { get; set; } = string.Empty;
            public string UrlFirebase { get; set; } = string.Empty;
            public long PesoBytes { get; set; }
        }

        private sealed class TemporalMultimediaTokenPayload
        {
            public string TipoMultimedia { get; set; } = string.Empty;
            public string NombreOriginal { get; set; } = string.Empty;
            public string NombreAlmacenado { get; set; } = string.Empty;
            public string Extension { get; set; } = string.Empty;
            public string MimeType { get; set; } = string.Empty;
            public string UrlFirebase { get; set; } = string.Empty;
            public string FolderName { get; set; } = string.Empty;
            public long PesoBytes { get; set; }
            public DateTime ExpiraUtc { get; set; }
        }

        private sealed class FirebaseCleanupItem
        {
            public string FolderName { get; set; } = string.Empty;
            public string StoredName { get; set; } = string.Empty;
        }
    }
}
