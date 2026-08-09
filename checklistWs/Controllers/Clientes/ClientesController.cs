using System.Data.SqlClient;
using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using checklistWs.Models.Clientes;
using checklistWs.Utiles;
using Microsoft.AspNetCore.Mvc;

namespace checklistWs.Controllers.Clientes
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientesController : ControllerBase
    {
        private static readonly TimeSpan ProxyHeaderTolerance = TimeSpan.FromMinutes(5);
        private static readonly string[] EmpresaClaimKeys = new[] { "idEmpresa", "empresaId", "tenantId", "companyId", "tenant", "idempresa" };
        private static readonly string[] EmpresaNombreClaimKeys = new[] { "empresa", "empresaNombre", "tenantName", "companyName", "nombreEmpresa" };
        private static readonly string[] UsuarioClaimKeys = new[] { ClaimTypes.NameIdentifier, "sub", "idUsuario", "userid", "uid" };
        private const int NombreLength = 200;
        private const int TelefonoLength = 30;
        private const int CorreoLength = 200;
        private const int EmpresaLength = 200;
        private const int CbarrasLength = 80;
        private const int CalleLength = 200;
        private const int NumeroLength = 40;
        private const int ColoniaLength = 150;
        private const int CiudadLength = 150;
        private const int MunicipioLength = 150;
        private const int EstadoLength = 150;
        private const int CodigoPostalLength = 12;
        private const int RfcLength = 20;
        private const int RegimenFiscalLength = 40;
        private const int EntreCallesLength = 300;
        private const int ReferenciaLength = 300;
        private const int NombreAvalLength = 200;
        private const int DireccionAvalLength = 300;
        private const int ObservacionesLength = 2000;
        private const int TextoNotaLength = 2000;
        private const string ProxyEmpresaIdHeader = "X-ProductosServicios-Proxy-EmpresaId";
        private const string ProxyEmpresaKeyHeader = "X-ProductosServicios-Proxy-Empresa";
        private const string ProxyUsuarioIdHeader = "X-ProductosServicios-Proxy-UsuarioId";
        private const string ProxyTimestampHeader = "X-ProductosServicios-Proxy-Timestamp";
        private const string ProxySignatureHeader = "X-ProductosServicios-Proxy-Signature";
        private const string ProxyContextItemKey = "__ClientesProxyContext";

        private readonly IConfiguration _configuration;
        private readonly SqlConnectionFactory _connectionFactory;
        private readonly ILogger<ClientesController> _logger;

        public ClientesController(IConfiguration configuration, ILogger<ClientesController> logger)
        {
            _configuration = configuration;
            _connectionFactory = new SqlConnectionFactory(configuration);
            _logger = logger;
        }

        [HttpGet("ObtenerClientes")]
        public async Task<IActionResult> ObtenerClientes(Guid idEmpresa, string busqueda = "", byte? tipoCliente = null)
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
    c.id,
    c.idEmpresa,
    c.identityKey,
    c.TipoCliente,
    c.Nombre,
    ISNULL(c.Telefono, '') AS Telefono,
    ISNULL(c.Correo, '') AS Correo,
    ISNULL(c.Empresa, '') AS Empresa,
    c.Activo,
    c.FechaCreacion,
    c.FechaActualizacion,
    c.FechaArchivado
FROM dbo.Clientes c
WHERE c.idEmpresa = @IdEmpresa
  AND c.Activo = 1");

                using SqlCommand command = new SqlCommand();
                command.Connection = connection;
                command.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);

                if (!string.IsNullOrWhiteSpace(busqueda))
                {
                    query.Append(@"
  AND (
      c.Nombre LIKE @Busqueda
      OR ISNULL(c.Telefono, '') LIKE @Busqueda
      OR ISNULL(c.Correo, '') LIKE @Busqueda
      OR ISNULL(c.Empresa, '') LIKE @Busqueda
  )");
                    command.Parameters.AddWithValue("@Busqueda", $"%{busqueda.Trim()}%");
                }

                if (tipoCliente.HasValue && (tipoCliente == ClienteTipos.Particular || tipoCliente == ClienteTipos.Empresa))
                {
                    query.Append(" AND c.TipoCliente = @TipoCliente");
                    command.Parameters.AddWithValue("@TipoCliente", tipoCliente.Value);
                }

                query.Append(" ORDER BY c.Nombre, c.FechaCreacion DESC");
                command.CommandText = query.ToString();

                List<ClienteListadoItemDto> items = new List<ClienteListadoItemDto>();
                using SqlDataReader reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    items.Add(MapCliente(reader));
                }

                ClienteResumenDto resumen = BuildResumen(items);
                return Ok(new ClienteListadoResponse { Resumen = resumen, Items = items });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerClientes", "No fue posible cargar los clientes.");
            }
        }

        [HttpGet("ObtenerCliente")]
        public async Task<IActionResult> ObtenerCliente(Guid idEmpresa, Guid idCliente)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            if (idCliente == Guid.Empty)
            {
                return BadRequest(new ClienteOperacionResponse { Mensaje = "El cliente solicitado no es válido." });
            }

            try
            {
                using SqlConnection connection = CreateConnection();
                await connection.OpenAsync();

                using SqlCommand command = new SqlCommand(@"
SELECT
    c.id,
    c.idEmpresa,
    c.identityKey,
    c.TipoCliente,
    c.Nombre,
    ISNULL(c.Telefono, '') AS Telefono,
    ISNULL(c.Correo, '') AS Correo,
    ISNULL(c.Empresa, '') AS Empresa,
    c.Activo,
    c.FechaCreacion,
    c.FechaActualizacion,
    c.FechaArchivado
FROM dbo.Clientes c
WHERE c.idEmpresa = @IdEmpresa
  AND c.id = @IdCliente
  AND c.Activo = 1", connection);

                command.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);
                command.Parameters.AddWithValue("@IdCliente", idCliente);

                using SqlDataReader reader = await command.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    return NotFound(new ClienteOperacionResponse { Mensaje = "El cliente no está disponible." });
                }

                return Ok(MapCliente(reader));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerCliente", "No fue posible cargar la ficha del cliente.");
            }
        }

        [HttpGet("ObtenerClienteAvanzado")]
        public async Task<IActionResult> ObtenerClienteAvanzado(Guid idEmpresa, Guid idCliente)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            if (idCliente == Guid.Empty)
            {
                return BadRequest(new ClienteOperacionResponse { Mensaje = "El cliente solicitado no es válido." });
            }

            try
            {
                using SqlConnection connection = CreateConnection();
                await connection.OpenAsync();

                using SqlCommand command = new SqlCommand(@"
SELECT
    c.id,
    c.idEmpresa,
    c.identityKey,
    c.TipoCliente,
    c.Nombre,
    ISNULL(c.Telefono, '') AS Telefono,
    ISNULL(c.Correo, '') AS Correo,
    ISNULL(c.Empresa, '') AS Empresa,
    ISNULL(c.Celular, '') AS Celular,
    ISNULL(c.TelefonoFijo, '') AS TelefonoFijo,
    c.FechaNacimiento,
    ISNULL(c.Cbarras, '') AS Cbarras,
    ISNULL(c.Calle, '') AS Calle,
    ISNULL(c.NumeroExt, '') AS NumeroExt,
    ISNULL(c.NumeroInt, '') AS NumeroInt,
    ISNULL(c.Colonia, '') AS Colonia,
    ISNULL(c.Ciudad, '') AS Ciudad,
    ISNULL(c.Municipio, '') AS Municipio,
    ISNULL(c.Estado, '') AS Estado,
    ISNULL(c.CodigoPostal, '') AS CodigoPostal,
    ISNULL(c.Rfc, '') AS Rfc,
    ISNULL(c.RegimenFiscal, '') AS RegimenFiscal,
    ISNULL(c.EntreCalles, '') AS EntreCalles,
    ISNULL(c.Referencia, '') AS Referencia,
    ISNULL(c.NombreAval, '') AS NombreAval,
    ISNULL(c.DireccionAval, '') AS DireccionAval,
    ISNULL(c.LimiteCredito, 0) AS LimiteCredito,
    ISNULL(c.PlazoDias, 0) AS PlazoDias,
    ISNULL(c.Descuento, 0) AS Descuento,
    ISNULL(c.Pagos, 0) AS Pagos,
    ISNULL(c.Interes, 0) AS Interes,
    ISNULL(c.Observaciones, '') AS Observaciones,
    ISNULL(c.IdNivel, 1) AS IdNivel,
    c.Activo,
    c.FechaCreacion,
    c.FechaActualizacion,
    c.FechaArchivado
FROM dbo.Clientes c
WHERE c.idEmpresa = @IdEmpresa
  AND c.id = @IdCliente
  AND c.Activo = 1", connection);

                command.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);
                command.Parameters.AddWithValue("@IdCliente", idCliente);

                using SqlDataReader reader = await command.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    return NotFound(new ClienteOperacionResponse { Mensaje = "El cliente no está disponible." });
                }

                return Ok(MapClienteAvanzado(reader));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerClienteAvanzado", "No fue posible cargar la edición avanzada del cliente.");
            }
        }

        [HttpGet("ObtenerListasPrecioCliente")]
        public IActionResult ObtenerListasPrecioCliente(Guid idEmpresa)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out _, out IActionResult? error))
            {
                return error!;
            }

            return Ok(BuildListasPrecioFallback());
        }

        [HttpGet("ObtenerRegimenesFiscalesCliente")]
        public async Task<IActionResult> ObtenerRegimenesFiscalesCliente(Guid idEmpresa)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                using SqlConnection connection = CreateConnection();
                await connection.OpenAsync();

                if (!await TableExistsAsync(connection, "CatalogoClientesRegimenFiscal"))
                {
                    return Ok(new List<ClienteCatalogoItemDto>
                    {
                        new ClienteCatalogoItemDto { Id = 0, Clave = string.Empty, Nombre = "Sin régimen fiscal" }
                    });
                }

                using SqlCommand command = new SqlCommand(@"
SELECT
    ISNULL(c_RegimenFiscal, '') AS Clave,
    ISNULL(Descripcion, '') AS Descripcion
FROM dbo.CatalogoClientesRegimenFiscal
WHERE ISNULL(Activo, 1) = 1
ORDER BY c_RegimenFiscal", connection);

                List<ClienteCatalogoItemDto> items = new List<ClienteCatalogoItemDto>
                {
                    new ClienteCatalogoItemDto { Id = 0, Clave = string.Empty, Nombre = "Sin régimen fiscal" }
                };

                using SqlDataReader reader = await command.ExecuteReaderAsync();
                int order = 1;
                while (await reader.ReadAsync())
                {
                    string clave = ReadString(reader, "Clave");
                    if (string.IsNullOrWhiteSpace(clave))
                    {
                        continue;
                    }

                    string descripcion = ReadString(reader, "Descripcion");
                    items.Add(new ClienteCatalogoItemDto
                    {
                        Id = order++,
                        Clave = clave,
                        Nombre = string.IsNullOrWhiteSpace(descripcion) ? clave : $"{descripcion} [{clave}]"
                    });
                }

                return Ok(items);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerRegimenesFiscalesCliente", "No fue posible cargar el catálogo de regímenes fiscales.");
            }
        }

        [HttpPost("ValidarDuplicadosCliente")]
        public async Task<IActionResult> ValidarDuplicadosCliente(Guid idEmpresa, [FromBody] ClienteDuplicadosRequest? request)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            request ??= new ClienteDuplicadosRequest();

            try
            {
                using SqlConnection connection = CreateConnection();
                await connection.OpenAsync();

                List<ClienteDuplicadoItemDto> coincidencias = await BuscarDuplicadosAsync(connection, context.IdEmpresa, request.IdCliente, request.Nombre, request.Telefono, request.Correo);

                return Ok(new ClienteDuplicadosResponse
                {
                    HayCoincidencias = coincidencias.Count > 0,
                    Mensaje = coincidencias.Count > 0
                        ? "Encontramos clientes que podrían coincidir con este registro."
                        : string.Empty,
                    Coincidencias = coincidencias
                });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ValidarDuplicadosCliente", "No fue posible validar posibles coincidencias.");
            }
        }

        [HttpPost("GuardarCliente")]
        public async Task<IActionResult> GuardarCliente(Guid idEmpresa, [FromBody] ClienteGuardarRequest? request)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            request ??= new ClienteGuardarRequest();

            try
            {
                string validation = ValidateClienteRequest(request);
                if (!string.IsNullOrWhiteSpace(validation))
                {
                    return BadRequest(new ClienteOperacionResponse { Mensaje = validation });
                }

                using SqlConnection connection = CreateConnection();
                await connection.OpenAsync();

                List<ClienteDuplicadoItemDto> coincidencias = await BuscarDuplicadosAsync(connection, context.IdEmpresa, request.Id, request.Nombre, request.Telefono, request.Correo);
                if (coincidencias.Count > 0 && !request.OmitirAdvertenciaDuplicados)
                {
                    return Conflict(new ClienteOperacionResponse
                    {
                        Mensaje = "Encontramos clientes que podrían coincidir con este registro.",
                        RequiereConfirmacionDuplicados = true,
                        Coincidencias = coincidencias
                    });
                }

                using SqlTransaction transaction = connection.BeginTransaction();
                DateTime now = DateTime.UtcNow;
                Guid clienteId = request.Id.GetValueOrDefault();
                bool esNuevo = clienteId == Guid.Empty;

                if (esNuevo)
                {
                    clienteId = Guid.NewGuid();
                    using SqlCommand insert = new SqlCommand(@"
INSERT INTO dbo.Clientes
    (id, idEmpresa, identityKey, TipoCliente, Nombre, Telefono, Correo, Empresa, Activo, FechaCreacion, FechaActualizacion, FechaArchivado)
VALUES
    (@Id, @IdEmpresa, @IdentityKey, @TipoCliente, @Nombre, @Telefono, @Correo, @Empresa, 1, @FechaCreacion, NULL, NULL)", connection, transaction);

                    AddClienteParameters(insert, clienteId, context.IdEmpresa, request, now, true);
                    await insert.ExecuteNonQueryAsync();
                }
                else
                {
                    using SqlCommand update = new SqlCommand(@"
UPDATE dbo.Clientes
SET
    TipoCliente = @TipoCliente,
    Nombre = @Nombre,
    Telefono = @Telefono,
    Correo = @Correo,
    Empresa = @Empresa,
    FechaActualizacion = @FechaActualizacion
WHERE idEmpresa = @IdEmpresa
  AND id = @Id
  AND Activo = 1", connection, transaction);

                    AddClienteParameters(update, clienteId, context.IdEmpresa, request, now, false);
                    int rows = await update.ExecuteNonQueryAsync();
                    if (rows == 0)
                    {
                        transaction.Rollback();
                        return NotFound(new ClienteOperacionResponse { Mensaje = "El cliente no está disponible para actualizar." });
                    }
                }

                transaction.Commit();

                return Ok(new ClienteOperacionResponse
                {
                    Mensaje = esNuevo ? "El cliente fue registrado." : "El cliente fue actualizado.",
                    IdCliente = clienteId
                });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "GuardarCliente", "No fue posible guardar el cliente.");
            }
        }

        [HttpPost("GuardarClienteAvanzado")]
        public async Task<IActionResult> GuardarClienteAvanzado(Guid idEmpresa, [FromBody] ClienteAvanzadoGuardarRequest? request)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            request ??= new ClienteAvanzadoGuardarRequest();
            if (request.Id == Guid.Empty)
            {
                return BadRequest(new ClienteOperacionResponse { Mensaje = "El cliente solicitado no es válido." });
            }

            try
            {
                string validation = ValidateClienteAvanzadoRequest(request, out DateTime? fechaNacimiento);
                if (!string.IsNullOrWhiteSpace(validation))
                {
                    return BadRequest(new ClienteOperacionResponse { Mensaje = validation });
                }

                using SqlConnection connection = CreateConnection();
                await connection.OpenAsync();
                using SqlTransaction transaction = connection.BeginTransaction();

                if (!await ClienteExisteAsync(connection, transaction, context.IdEmpresa, request.Id))
                {
                    transaction.Rollback();
                    return NotFound(new ClienteOperacionResponse { Mensaje = "El cliente no está disponible para actualizar." });
                }

                using SqlCommand update = new SqlCommand(@"
UPDATE dbo.Clientes
SET
    Nombre = @Nombre,
    Telefono = @Telefono,
    Correo = @Correo,
    Celular = @Celular,
    TelefonoFijo = @TelefonoFijo,
    FechaNacimiento = @FechaNacimiento,
    Cbarras = @Cbarras,
    Calle = @Calle,
    NumeroExt = @NumeroExt,
    NumeroInt = @NumeroInt,
    Colonia = @Colonia,
    Ciudad = @Ciudad,
    Municipio = @Municipio,
    Estado = @Estado,
    CodigoPostal = @CodigoPostal,
    Rfc = @Rfc,
    RegimenFiscal = @RegimenFiscal,
    EntreCalles = @EntreCalles,
    Referencia = @Referencia,
    NombreAval = @NombreAval,
    DireccionAval = @DireccionAval,
    LimiteCredito = @LimiteCredito,
    PlazoDias = @PlazoDias,
    Descuento = @Descuento,
    Pagos = @Pagos,
    Interes = @Interes,
    Observaciones = @Observaciones,
    IdNivel = @IdNivel,
    FechaActualizacion = @FechaActualizacion
WHERE idEmpresa = @IdEmpresa
  AND id = @Id
  AND Activo = 1", connection, transaction);

                AddClienteAvanzadoParameters(update, request, context.IdEmpresa, fechaNacimiento, DateTime.UtcNow);
                int rows = await update.ExecuteNonQueryAsync();
                if (rows == 0)
                {
                    transaction.Rollback();
                    return NotFound(new ClienteOperacionResponse { Mensaje = "El cliente no está disponible para actualizar." });
                }

                transaction.Commit();
                return Ok(new ClienteOperacionResponse
                {
                    Mensaje = "La edición avanzada del cliente fue actualizada.",
                    IdCliente = request.Id
                });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "GuardarClienteAvanzado", "No fue posible guardar la edición avanzada del cliente.");
            }
        }

        [HttpGet("ObtenerNotasCliente")]
        public async Task<IActionResult> ObtenerNotasCliente(Guid idEmpresa, Guid idCliente)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            if (idCliente == Guid.Empty)
            {
                return BadRequest(new ClienteOperacionResponse { Mensaje = "El cliente solicitado no es válido." });
            }

            try
            {
                using SqlConnection connection = CreateConnection();
                await connection.OpenAsync();

                if (!await ClienteExisteAsync(connection, null, context.IdEmpresa, idCliente))
                {
                    return NotFound(new ClienteOperacionResponse { Mensaje = "El cliente no está disponible." });
                }

                using SqlCommand command = new SqlCommand(@"
SELECT
    n.id,
    n.idCliente,
    n.idEmpresa,
    n.identityKey,
    n.Texto,
    n.EsTarea,
    n.FechaTarea,
    n.HoraTarea,
    n.Completada,
    n.FechaCompletada,
    n.FechaCreacion,
    n.Activo
FROM dbo.ClientesNotas n
WHERE n.idEmpresa = @IdEmpresa
  AND n.idCliente = @IdCliente
  AND n.Activo = 1
ORDER BY n.FechaCreacion DESC", connection);

                command.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);
                command.Parameters.AddWithValue("@IdCliente", idCliente);

                List<ClienteNotaItemDto> notas = new List<ClienteNotaItemDto>();
                using SqlDataReader reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    notas.Add(MapNota(reader));
                }

                return Ok(notas);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerNotasCliente", "No fue posible cargar las notas del cliente.");
            }
        }

        [HttpPost("GuardarNotaCliente")]
        public async Task<IActionResult> GuardarNotaCliente(Guid idEmpresa, [FromBody] ClienteNotaGuardarRequest? request)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            request ??= new ClienteNotaGuardarRequest();

            try
            {
                string validation = ValidateNotaRequest(request, out DateTime? fechaTarea, out TimeSpan? horaTarea);
                if (!string.IsNullOrWhiteSpace(validation))
                {
                    return BadRequest(new ClienteOperacionResponse { Mensaje = validation });
                }

                using SqlConnection connection = CreateConnection();
                await connection.OpenAsync();
                using SqlTransaction transaction = connection.BeginTransaction();

                if (!await ClienteExisteAsync(connection, transaction, context.IdEmpresa, request.IdCliente))
                {
                    transaction.Rollback();
                    return NotFound(new ClienteOperacionResponse { Mensaje = "El cliente no está disponible." });
                }

                Guid notaId = Guid.NewGuid();
                DateTime now = DateTime.UtcNow;

                using SqlCommand insert = new SqlCommand(@"
INSERT INTO dbo.ClientesNotas
    (id, idEmpresa, identityKey, idCliente, Texto, EsTarea, FechaTarea, HoraTarea, Completada, FechaCompletada, Activo, FechaCreacion, FechaActualizacion, FechaArchivado)
VALUES
    (@Id, @IdEmpresa, @IdentityKey, @IdCliente, @Texto, @EsTarea, @FechaTarea, @HoraTarea, 0, NULL, 1, @FechaCreacion, NULL, NULL)", connection, transaction);

                insert.Parameters.AddWithValue("@Id", notaId);
                insert.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);
                insert.Parameters.AddWithValue("@IdentityKey", Guid.NewGuid());
                insert.Parameters.AddWithValue("@IdCliente", request.IdCliente);
                insert.Parameters.AddWithValue("@Texto", Truncate(request.Texto, TextoNotaLength));
                insert.Parameters.AddWithValue("@EsTarea", request.EsTarea);
                insert.Parameters.AddWithValue("@FechaTarea", (object?)fechaTarea?.Date ?? DBNull.Value);
                insert.Parameters.AddWithValue("@HoraTarea", (object?)horaTarea ?? DBNull.Value);
                insert.Parameters.AddWithValue("@FechaCreacion", now);

                await insert.ExecuteNonQueryAsync();
                transaction.Commit();

                return Ok(new ClienteOperacionResponse { Mensaje = request.EsTarea ? "La tarea fue registrada." : "La nota fue registrada." });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "GuardarNotaCliente", "No fue posible guardar la nota del cliente.");
            }
        }

        [HttpPost("CompletarTareaCliente")]
        public async Task<IActionResult> CompletarTareaCliente(Guid idEmpresa, [FromBody] ClienteCompletarTareaRequest? request)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            request ??= new ClienteCompletarTareaRequest();
            if (request.IdNota == Guid.Empty)
            {
                return BadRequest(new ClienteOperacionResponse { Mensaje = "La tarea solicitada no es válida." });
            }

            try
            {
                using SqlConnection connection = CreateConnection();
                await connection.OpenAsync();
                using SqlTransaction transaction = connection.BeginTransaction();

                using SqlCommand update = new SqlCommand(@"
UPDATE dbo.ClientesNotas
SET
    Completada = @Completada,
    FechaCompletada = @FechaCompletada,
    FechaActualizacion = @FechaActualizacion
WHERE idEmpresa = @IdEmpresa
  AND id = @IdNota
  AND Activo = 1
  AND EsTarea = 1", connection, transaction);

                DateTime now = DateTime.UtcNow;
                update.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);
                update.Parameters.AddWithValue("@IdNota", request.IdNota);
                update.Parameters.AddWithValue("@Completada", request.Completada);
                update.Parameters.AddWithValue("@FechaCompletada", request.Completada ? now : DBNull.Value);
                update.Parameters.AddWithValue("@FechaActualizacion", now);

                int rows = await update.ExecuteNonQueryAsync();
                if (rows == 0)
                {
                    transaction.Rollback();
                    return NotFound(new ClienteOperacionResponse { Mensaje = "La tarea no está disponible." });
                }

                transaction.Commit();
                return Ok(new ClienteOperacionResponse
                {
                    Mensaje = request.Completada ? "La tarea fue marcada como completada." : "La tarea volvió a pendiente."
                });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "CompletarTareaCliente", "No fue posible actualizar la tarea.");
            }
        }

        private static ClienteResumenDto BuildResumen(List<ClienteListadoItemDto> items)
        {
            return new ClienteResumenDto
            {
                Total = items.Count,
                Particulares = items.Count(x => x.TipoCliente == ClienteTipos.Particular),
                Empresas = items.Count(x => x.TipoCliente == ClienteTipos.Empresa),
                ConTelefono = items.Count(x => !string.IsNullOrWhiteSpace(x.Telefono)),
                ConCorreo = items.Count(x => !string.IsNullOrWhiteSpace(x.Correo))
            };
        }

        private static ClienteListadoItemDto MapCliente(SqlDataReader reader)
        {
            byte tipoCliente = ReadByte(reader, "TipoCliente");
            return new ClienteListadoItemDto
            {
                Id = ReadGuid(reader, "id"),
                IdEmpresa = ReadGuid(reader, "idEmpresa"),
                IdentityKey = ReadGuid(reader, "identityKey"),
                TipoCliente = tipoCliente,
                TipoClienteNombre = GetTipoClienteNombre(tipoCliente),
                Nombre = ReadString(reader, "Nombre"),
                Telefono = ReadString(reader, "Telefono"),
                Correo = ReadString(reader, "Correo"),
                Empresa = ReadString(reader, "Empresa"),
                Activo = ReadBool(reader, "Activo"),
                FechaCreacion = ReadDateTime(reader, "FechaCreacion"),
                FechaActualizacion = ReadNullableDateTime(reader, "FechaActualizacion"),
                FechaArchivado = ReadNullableDateTime(reader, "FechaArchivado")
            };
        }

        private static ClienteAvanzadoDto MapClienteAvanzado(SqlDataReader reader)
        {
            byte tipoCliente = ReadByte(reader, "TipoCliente");
            DateTime? fechaNacimiento = ReadNullableDateTime(reader, "FechaNacimiento");

            return new ClienteAvanzadoDto
            {
                Id = ReadGuid(reader, "id"),
                IdEmpresa = ReadGuid(reader, "idEmpresa"),
                IdentityKey = ReadGuid(reader, "identityKey"),
                TipoCliente = tipoCliente,
                TipoClienteNombre = GetTipoClienteNombre(tipoCliente),
                Nombre = ReadString(reader, "Nombre"),
                Telefono = ReadString(reader, "Telefono"),
                Correo = ReadString(reader, "Correo"),
                Empresa = ReadString(reader, "Empresa"),
                Celular = ReadString(reader, "Celular"),
                TelefonoFijo = ReadString(reader, "TelefonoFijo"),
                FechaNacimiento = fechaNacimiento.HasValue ? fechaNacimiento.Value.ToString("yyyy-MM-dd") : string.Empty,
                Cbarras = ReadString(reader, "Cbarras"),
                Calle = ReadString(reader, "Calle"),
                NumeroExt = ReadString(reader, "NumeroExt"),
                NumeroInt = ReadString(reader, "NumeroInt"),
                Colonia = ReadString(reader, "Colonia"),
                Ciudad = ReadString(reader, "Ciudad"),
                Municipio = ReadString(reader, "Municipio"),
                Estado = ReadString(reader, "Estado"),
                CodigoPostal = ReadString(reader, "CodigoPostal"),
                Rfc = ReadString(reader, "Rfc"),
                RegimenFiscal = ReadString(reader, "RegimenFiscal"),
                EntreCalles = ReadString(reader, "EntreCalles"),
                Referencia = ReadString(reader, "Referencia"),
                NombreAval = ReadString(reader, "NombreAval"),
                DireccionAval = ReadString(reader, "DireccionAval"),
                LimiteCredito = ReadDecimal(reader, "LimiteCredito"),
                PlazoDias = ReadInt(reader, "PlazoDias"),
                Descuento = ReadDecimal(reader, "Descuento"),
                Pagos = ReadInt(reader, "Pagos"),
                Interes = ReadDecimal(reader, "Interes"),
                Observaciones = ReadString(reader, "Observaciones"),
                IdNivel = Math.Max(1, ReadInt(reader, "IdNivel")),
                Activo = ReadBool(reader, "Activo"),
                FechaCreacion = ReadDateTime(reader, "FechaCreacion"),
                FechaActualizacion = ReadNullableDateTime(reader, "FechaActualizacion"),
                FechaArchivado = ReadNullableDateTime(reader, "FechaArchivado")
            };
        }

        private static ClienteNotaItemDto MapNota(SqlDataReader reader)
        {
            DateTime? fechaTarea = ReadNullableDateTime(reader, "FechaTarea");
            TimeSpan? horaTarea = ReadNullableTime(reader, "HoraTarea");
            DateTime? fechaCompletada = ReadNullableDateTime(reader, "FechaCompletada");
            DateTime fechaCreacion = ReadDateTime(reader, "FechaCreacion");

            return new ClienteNotaItemDto
            {
                Id = ReadGuid(reader, "id"),
                IdCliente = ReadGuid(reader, "idCliente"),
                IdEmpresa = ReadGuid(reader, "idEmpresa"),
                IdentityKey = ReadGuid(reader, "identityKey"),
                Texto = ReadString(reader, "Texto"),
                EsTarea = ReadBool(reader, "EsTarea"),
                FechaTarea = fechaTarea.HasValue ? fechaTarea.Value.ToString("yyyy-MM-dd") : string.Empty,
                HoraTarea = horaTarea.HasValue ? horaTarea.Value.ToString(@"hh\:mm") : string.Empty,
                Completada = ReadBool(reader, "Completada"),
                FechaCompletada = fechaCompletada.HasValue ? fechaCompletada.Value.ToString("yyyy-MM-ddTHH:mm:ss") : string.Empty,
                FechaCreacion = fechaCreacion.ToString("yyyy-MM-ddTHH:mm:ss"),
                Activo = ReadBool(reader, "Activo")
            };
        }

        private static string ValidateClienteRequest(ClienteGuardarRequest request)
        {
            string nombre = (request.Nombre ?? string.Empty).Trim();
            string telefono = (request.Telefono ?? string.Empty).Trim();
            string correo = (request.Correo ?? string.Empty).Trim();
            string empresa = (request.Empresa ?? string.Empty).Trim();

            if (request.TipoCliente != ClienteTipos.Particular && request.TipoCliente != ClienteTipos.Empresa)
            {
                return "Selecciona un tipo de cliente válido.";
            }

            if (string.IsNullOrWhiteSpace(nombre))
            {
                return "Captura el nombre del cliente.";
            }

            if (string.IsNullOrWhiteSpace(telefono) && string.IsNullOrWhiteSpace(correo))
            {
                return "Captura al menos un teléfono o un correo.";
            }

            if (!string.IsNullOrWhiteSpace(correo) && !IsValidEmail(correo))
            {
                return "Captura un correo con formato válido.";
            }

            if (request.TipoCliente == ClienteTipos.Empresa && string.IsNullOrWhiteSpace(empresa))
            {
                return "Captura la empresa del cliente.";
            }

            return string.Empty;
        }

        private static string ValidateClienteAvanzadoRequest(ClienteAvanzadoGuardarRequest request, out DateTime? fechaNacimiento)
        {
            fechaNacimiento = null;

            string nombre = (request.Nombre ?? string.Empty).Trim();
            string telefono = (request.Telefono ?? string.Empty).Trim();
            string correo = (request.Correo ?? string.Empty).Trim();
            string celular = (request.Celular ?? string.Empty).Trim();
            string telefonoFijo = (request.TelefonoFijo ?? string.Empty).Trim();
            string codigoPostal = (request.CodigoPostal ?? string.Empty).Trim();
            string rfc = (request.Rfc ?? string.Empty).Trim().ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(nombre))
            {
                return "Captura el nombre del cliente.";
            }

            if (string.IsNullOrWhiteSpace(telefono) && string.IsNullOrWhiteSpace(correo))
            {
                return "Captura al menos un teléfono o un correo.";
            }

            if (!string.IsNullOrWhiteSpace(correo) && !IsValidEmail(correo))
            {
                return "Captura un correo con formato válido.";
            }

            if (!string.IsNullOrWhiteSpace(request.FechaNacimiento))
            {
                if (!DateTime.TryParseExact(request.FechaNacimiento.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
                {
                    return "Captura una fecha de nacimiento válida.";
                }

                fechaNacimiento = parsedDate.Date;
            }

            if (!string.IsNullOrWhiteSpace(codigoPostal) && codigoPostal.Length > CodigoPostalLength)
            {
                return "El código postal excede la longitud permitida.";
            }

            if (!string.IsNullOrWhiteSpace(rfc) && rfc.Length > RfcLength)
            {
                return "El RFC excede la longitud permitida.";
            }

            if (request.LimiteCredito < 0 || request.PlazoDias < 0 || request.Descuento < 0 || request.Pagos < 0 || request.Interes < 0)
            {
                return "Los valores comerciales no pueden ser negativos.";
            }

            return string.Empty;
        }

        private static string ValidateNotaRequest(ClienteNotaGuardarRequest request, out DateTime? fechaTarea, out TimeSpan? horaTarea)
        {
            fechaTarea = null;
            horaTarea = null;

            string texto = (request.Texto ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(texto))
            {
                return "Captura el texto de la nota.";
            }

            if (request.IdCliente == Guid.Empty)
            {
                return "Selecciona un cliente válido.";
            }

            if (!request.EsTarea)
            {
                return string.Empty;
            }

            if (!DateTime.TryParseExact(request.FechaTarea?.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
            {
                return "Captura una fecha válida para la tarea.";
            }

            if (!TimeSpan.TryParseExact(request.HoraTarea?.Trim(), @"hh\:mm", CultureInfo.InvariantCulture, out TimeSpan parsedTime))
            {
                return "Captura una hora válida para la tarea.";
            }

            fechaTarea = parsedDate.Date;
            horaTarea = parsedTime;
            return string.Empty;
        }

        private static bool IsValidEmail(string email)
        {
            string value = (email ?? string.Empty).Trim();
            int at = value.IndexOf('@');
            int dot = value.LastIndexOf('.');
            return at > 0 && dot > at + 1 && dot < value.Length - 1;
        }

        private async Task<List<ClienteDuplicadoItemDto>> BuscarDuplicadosAsync(SqlConnection connection, Guid idEmpresa, Guid? idCliente, string nombre, string telefono, string correo)
        {
            List<ClienteDuplicadoItemDto> coincidencias = new List<ClienteDuplicadoItemDto>();
            List<string> condiciones = new List<string>();
            using SqlCommand command = new SqlCommand();
            command.Connection = connection;
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);

            string telefonoNormalizado = NormalizeTelefono(telefono);
            string correoNormalizado = (correo ?? string.Empty).Trim().ToLowerInvariant();
            string nombreNormalizado = (nombre ?? string.Empty).Trim();

            if (!string.IsNullOrWhiteSpace(telefonoNormalizado))
            {
                condiciones.Add("REPLACE(REPLACE(REPLACE(ISNULL(c.Telefono, ''), ' ', ''), '-', ''), '(', '') LIKE @TelefonoBusqueda");
                command.Parameters.AddWithValue("@TelefonoBusqueda", $"%{telefonoNormalizado}%");
            }

            if (!string.IsNullOrWhiteSpace(correoNormalizado))
            {
                condiciones.Add("LOWER(ISNULL(c.Correo, '')) = @Correo");
                command.Parameters.AddWithValue("@Correo", correoNormalizado);
            }

            if (!string.IsNullOrWhiteSpace(nombreNormalizado))
            {
                condiciones.Add("LOWER(c.Nombre) = @Nombre");
                command.Parameters.AddWithValue("@Nombre", nombreNormalizado.ToLowerInvariant());
            }

            if (condiciones.Count == 0)
            {
                return coincidencias;
            }

            StringBuilder query = new StringBuilder(@"
SELECT TOP 10
    c.id,
    c.TipoCliente,
    c.Nombre,
    ISNULL(c.Telefono, '') AS Telefono,
    ISNULL(c.Correo, '') AS Correo,
    ISNULL(c.Empresa, '') AS Empresa
FROM dbo.Clientes c
WHERE c.idEmpresa = @IdEmpresa
  AND c.Activo = 1");

            if (idCliente.HasValue && idCliente.Value != Guid.Empty)
            {
                query.Append(" AND c.id <> @IdCliente");
                command.Parameters.AddWithValue("@IdCliente", idCliente.Value);
            }

            query.Append(" AND (");
            query.Append(string.Join(" OR ", condiciones));
            query.Append(") ORDER BY c.Nombre");
            command.CommandText = query.ToString();

            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                string[] motivos = BuildCoincidenciaMotivos(
                    ReadString(reader, "Nombre"),
                    ReadString(reader, "Telefono"),
                    ReadString(reader, "Correo"),
                    nombreNormalizado,
                    telefonoNormalizado,
                    correoNormalizado);

                coincidencias.Add(new ClienteDuplicadoItemDto
                {
                    Id = ReadGuid(reader, "id"),
                    Nombre = ReadString(reader, "Nombre"),
                    Telefono = ReadString(reader, "Telefono"),
                    Correo = ReadString(reader, "Correo"),
                    Empresa = ReadString(reader, "Empresa"),
                    TipoClienteNombre = GetTipoClienteNombre(ReadByte(reader, "TipoCliente")),
                    CoincidenciaEn = string.Join(", ", motivos.Where(x => !string.IsNullOrWhiteSpace(x)))
                });
            }

            return coincidencias;
        }

        private static string[] BuildCoincidenciaMotivos(string nombreActual, string telefonoActual, string correoActual, string nombreBuscado, string telefonoBuscado, string correoBuscado)
        {
            List<string> motivos = new List<string>();

            if (!string.IsNullOrWhiteSpace(nombreBuscado) &&
                string.Equals((nombreActual ?? string.Empty).Trim(), nombreBuscado, StringComparison.OrdinalIgnoreCase))
            {
                motivos.Add("Nombre");
            }

            if (!string.IsNullOrWhiteSpace(telefonoBuscado) &&
                NormalizeTelefono(telefonoActual) == telefonoBuscado)
            {
                motivos.Add("Teléfono");
            }

            if (!string.IsNullOrWhiteSpace(correoBuscado) &&
                string.Equals((correoActual ?? string.Empty).Trim(), correoBuscado, StringComparison.OrdinalIgnoreCase))
            {
                motivos.Add("Correo");
            }

            return motivos.ToArray();
        }

        private async Task<bool> ClienteExisteAsync(SqlConnection connection, SqlTransaction? transaction, Guid idEmpresa, Guid idCliente)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT COUNT(1)
FROM dbo.Clientes
WHERE idEmpresa = @IdEmpresa
  AND id = @IdCliente
  AND Activo = 1", connection, transaction);

            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@IdCliente", idCliente);

            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        private static void AddClienteParameters(SqlCommand command, Guid clienteId, Guid idEmpresa, ClienteGuardarRequest request, DateTime now, bool includeCreation)
        {
            command.Parameters.AddWithValue("@Id", clienteId);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            if (includeCreation)
            {
                command.Parameters.AddWithValue("@IdentityKey", Guid.NewGuid());
                command.Parameters.AddWithValue("@FechaCreacion", now);
            }

            if (!includeCreation)
            {
                command.Parameters.AddWithValue("@FechaActualizacion", now);
            }

            command.Parameters.AddWithValue("@TipoCliente", request.TipoCliente);
            command.Parameters.AddWithValue("@Nombre", Truncate(request.Nombre, NombreLength));
            command.Parameters.AddWithValue("@Telefono", string.IsNullOrWhiteSpace(request.Telefono) ? DBNull.Value : Truncate(request.Telefono, TelefonoLength));
            command.Parameters.AddWithValue("@Correo", string.IsNullOrWhiteSpace(request.Correo) ? DBNull.Value : Truncate(request.Correo.Trim().ToLowerInvariant(), CorreoLength));
            command.Parameters.AddWithValue("@Empresa", request.TipoCliente == ClienteTipos.Empresa && !string.IsNullOrWhiteSpace(request.Empresa)
                ? Truncate(request.Empresa, EmpresaLength)
                : DBNull.Value);
        }

        private static void AddClienteAvanzadoParameters(SqlCommand command, ClienteAvanzadoGuardarRequest request, Guid idEmpresa, DateTime? fechaNacimiento, DateTime now)
        {
            command.Parameters.AddWithValue("@Id", request.Id);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@FechaActualizacion", now);
            command.Parameters.AddWithValue("@Nombre", Truncate(request.Nombre, NombreLength));
            command.Parameters.AddWithValue("@Telefono", ToDbNullable(request.Telefono, TelefonoLength));
            command.Parameters.AddWithValue("@Correo", string.IsNullOrWhiteSpace(request.Correo)
                ? DBNull.Value
                : Truncate(request.Correo.Trim().ToLowerInvariant(), CorreoLength));
            command.Parameters.AddWithValue("@Celular", ToDbNullable(request.Celular, TelefonoLength));
            command.Parameters.AddWithValue("@TelefonoFijo", ToDbNullable(request.TelefonoFijo, TelefonoLength));
            command.Parameters.AddWithValue("@FechaNacimiento", (object?)fechaNacimiento ?? DBNull.Value);
            command.Parameters.AddWithValue("@Cbarras", ToDbNullable(request.Cbarras, CbarrasLength));
            command.Parameters.AddWithValue("@Calle", ToDbNullable(request.Calle, CalleLength));
            command.Parameters.AddWithValue("@NumeroExt", ToDbNullable(request.NumeroExt, NumeroLength));
            command.Parameters.AddWithValue("@NumeroInt", ToDbNullable(request.NumeroInt, NumeroLength));
            command.Parameters.AddWithValue("@Colonia", ToDbNullable(request.Colonia, ColoniaLength));
            command.Parameters.AddWithValue("@Ciudad", ToDbNullable(request.Ciudad, CiudadLength));
            command.Parameters.AddWithValue("@Municipio", ToDbNullable(request.Municipio, MunicipioLength));
            command.Parameters.AddWithValue("@Estado", ToDbNullable(request.Estado, EstadoLength));
            command.Parameters.AddWithValue("@CodigoPostal", ToDbNullable(request.CodigoPostal, CodigoPostalLength));
            command.Parameters.AddWithValue("@Rfc", ToDbNullable((request.Rfc ?? string.Empty).Trim().ToUpperInvariant(), RfcLength));
            command.Parameters.AddWithValue("@RegimenFiscal", ToDbNullable(request.RegimenFiscal, RegimenFiscalLength));
            command.Parameters.AddWithValue("@EntreCalles", ToDbNullable(request.EntreCalles, EntreCallesLength));
            command.Parameters.AddWithValue("@Referencia", ToDbNullable(request.Referencia, ReferenciaLength));
            command.Parameters.AddWithValue("@NombreAval", ToDbNullable(request.NombreAval, NombreAvalLength));
            command.Parameters.AddWithValue("@DireccionAval", ToDbNullable(request.DireccionAval, DireccionAvalLength));
            command.Parameters.AddWithValue("@LimiteCredito", request.LimiteCredito);
            command.Parameters.AddWithValue("@PlazoDias", Math.Max(0, request.PlazoDias));
            command.Parameters.AddWithValue("@Descuento", request.Descuento);
            command.Parameters.AddWithValue("@Pagos", Math.Max(0, request.Pagos));
            command.Parameters.AddWithValue("@Interes", request.Interes);
            command.Parameters.AddWithValue("@Observaciones", ToDbNullable(request.Observaciones, ObservacionesLength));
            command.Parameters.AddWithValue("@IdNivel", request.IdNivel <= 0 ? 1 : request.IdNivel);
        }

        private static object ToDbNullable(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return DBNull.Value;
            }

            return Truncate(value, maxLength);
        }

        private static List<ClienteCatalogoItemDto> BuildListasPrecioFallback()
        {
            return new List<ClienteCatalogoItemDto>
            {
                new ClienteCatalogoItemDto { Id = 1, Clave = "1", Nombre = "Precio 1" },
                new ClienteCatalogoItemDto { Id = 2, Clave = "2", Nombre = "Precio 2" },
                new ClienteCatalogoItemDto { Id = 3, Clave = "3", Nombre = "Precio 3" },
                new ClienteCatalogoItemDto { Id = 4, Clave = "4", Nombre = "Mayoreo 1" },
                new ClienteCatalogoItemDto { Id = 5, Clave = "5", Nombre = "Mayoreo 2" },
                new ClienteCatalogoItemDto { Id = 6, Clave = "6", Nombre = "Mayoreo 3" },
                new ClienteCatalogoItemDto { Id = 7, Clave = "7", Nombre = "Precio 7" },
                new ClienteCatalogoItemDto { Id = 8, Clave = "8", Nombre = "Precio 8" },
                new ClienteCatalogoItemDto { Id = 9, Clave = "9", Nombre = "Precio 9" },
                new ClienteCatalogoItemDto { Id = 10, Clave = "10", Nombre = "Precio 10" }
            };
        }

        private static async Task<bool> TableExistsAsync(SqlConnection connection, string tableName)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT COUNT(1)
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'dbo'
  AND TABLE_NAME = @TableName", connection);

            command.Parameters.AddWithValue("@TableName", tableName);
            return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
        }

        private bool TryResolveRequestContext(Guid? clientEmpresaId, string? clientEmpresaKey, out RequestContext context, out IActionResult? error)
        {
            context = null!;
            error = null;

            Guid? effectiveEmpresaId = TryResolveEmpresaId(out string? proxyEmpresaKey);
            if (!effectiveEmpresaId.HasValue || effectiveEmpresaId.Value == Guid.Empty)
            {
                error = Unauthorized(new ClienteOperacionResponse { Mensaje = "No fue posible resolver la empresa activa." });
                return false;
            }

            if (clientEmpresaId.HasValue && clientEmpresaId.Value != Guid.Empty && clientEmpresaId.Value != effectiveEmpresaId.Value)
            {
                error = BadRequest(new ClienteOperacionResponse { Mensaje = "La empresa solicitada no coincide con la sesión activa." });
                return false;
            }

            string empresaStorageKey = TryResolveEmpresaStorageKey(effectiveEmpresaId.Value, proxyEmpresaKey);
            if (!string.IsNullOrWhiteSpace(clientEmpresaKey) &&
                !string.Equals(clientEmpresaKey.Trim(), empresaStorageKey, StringComparison.OrdinalIgnoreCase))
            {
                error = BadRequest(new ClienteOperacionResponse { Mensaje = "La empresa solicitada no coincide con la sesión activa." });
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
                _logger.LogWarning("Clientes proxy headers recibidos sin secreto compartido configurado.");
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
                _logger.LogWarning("Clientes proxy headers expirados o fuera de tolerancia para empresa {EmpresaId}.", empresaId);
                return false;
            }

            string payload = BuildProxySignaturePayload(empresaIdRaw, empresaKeyRaw, usuarioIdRaw, timestampRaw);
            string expectedSignature = ComputeProxySignature(secret, payload);

            if (!SignaturesMatch(expectedSignature, signatureRaw))
            {
                _logger.LogWarning("Clientes proxy headers con firma inválida para empresa {EmpresaId}.", empresaId);
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

        private static string GetTipoClienteNombre(byte tipoCliente)
        {
            return tipoCliente == ClienteTipos.Empresa ? "Empresa" : "Particular";
        }

        private static string NormalizeTelefono(string telefono)
        {
            if (string.IsNullOrWhiteSpace(telefono))
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder();
            foreach (char character in telefono)
            {
                if (char.IsDigit(character))
                {
                    builder.Append(character);
                }
            }

            return builder.ToString();
        }

        private static string Truncate(string value, int maxLength)
        {
            string normalized = (value ?? string.Empty).Trim();
            return normalized.Length > maxLength ? normalized[..maxLength] : normalized;
        }

        private SqlConnection CreateConnection()
        {
            return _connectionFactory.CreateConnection();
        }

        private IActionResult HandleException(Exception ex, string operation, string safeMessage)
        {
            _logger.LogError(ex, "Error en Clientes durante {Operation}.", operation);
            return StatusCode(500, new ClienteOperacionResponse { Mensaje = safeMessage });
        }

        private static Guid ReadGuid(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return !reader.IsDBNull(ordinal) ? reader.GetGuid(ordinal) : Guid.Empty;
        }

        private static byte ReadByte(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return !reader.IsDBNull(ordinal) ? reader.GetByte(ordinal) : (byte)0;
        }

        private static bool ReadBool(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return !reader.IsDBNull(ordinal) && reader.GetBoolean(ordinal);
        }

        private static string ReadString(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
        }

        private static int ReadInt(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal))
            {
                return 0;
            }

            object value = reader.GetValue(ordinal);
            return value switch
            {
                int direct => direct,
                short shortValue => shortValue,
                long longValue => (int)longValue,
                byte byteValue => byteValue,
                decimal decimalValue => (int)decimalValue,
                _ => Convert.ToInt32(value, CultureInfo.InvariantCulture)
            };
        }

        private static decimal ReadDecimal(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal))
            {
                return 0m;
            }

            object value = reader.GetValue(ordinal);
            return value switch
            {
                decimal direct => direct,
                double doubleValue => Convert.ToDecimal(doubleValue, CultureInfo.InvariantCulture),
                float floatValue => Convert.ToDecimal(floatValue, CultureInfo.InvariantCulture),
                int intValue => intValue,
                long longValue => longValue,
                _ => Convert.ToDecimal(value, CultureInfo.InvariantCulture)
            };
        }

        private static DateTime ReadDateTime(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? DateTime.MinValue : reader.GetDateTime(ordinal);
        }

        private static DateTime? ReadNullableDateTime(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal))
            {
                return null;
            }

            object value = reader.GetValue(ordinal);
            return value is DateTime dateTime ? dateTime : Convert.ToDateTime(value);
        }

        private static TimeSpan? ReadNullableTime(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? null : (TimeSpan?)reader.GetValue(ordinal);
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
    }
}
