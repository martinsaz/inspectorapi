using System.Data.SqlClient;
using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using checklistWs.Models.Clientes;
using checklistWs.Utiles;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Mvc;

namespace checklistWs.Controllers.Clientes
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientesReporteController : ControllerBase
    {
        private static readonly TimeSpan ProxyHeaderTolerance = TimeSpan.FromMinutes(5);
        private static readonly string[] EmpresaClaimKeys = new[] { "idEmpresa", "empresaId", "tenantId", "companyId", "tenant", "idempresa" };
        private static readonly string[] EmpresaNombreClaimKeys = new[] { "empresa", "empresaNombre", "tenantName", "companyName", "nombreEmpresa" };
        private const string ProxyEmpresaIdHeader = "X-ProductosServicios-Proxy-EmpresaId";
        private const string ProxyEmpresaKeyHeader = "X-ProductosServicios-Proxy-Empresa";
        private const string ProxyUsuarioIdHeader = "X-ProductosServicios-Proxy-UsuarioId";
        private const string ProxyTimestampHeader = "X-ProductosServicios-Proxy-Timestamp";
        private const string ProxySignatureHeader = "X-ProductosServicios-Proxy-Signature";
        private const string ProxyContextItemKey = "__ClientesReporteProxyContext";

        private readonly IConfiguration _configuration;
        private readonly SqlConnectionFactory _connectionFactory;
        private readonly ILogger<ClientesReporteController> _logger;

        public ClientesReporteController(IConfiguration configuration, ILogger<ClientesReporteController> logger)
        {
            _configuration = configuration;
            _connectionFactory = new SqlConnectionFactory(configuration);
            _logger = logger;
        }

        [HttpGet("ObtenerConfiguracion")]
        public IActionResult ObtenerConfiguracion(Guid idEmpresa)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out _, out IActionResult? error))
            {
                return error!;
            }

            return Ok(new ClienteReporteConfiguracionResponse
            {
                Reportes = BuildCatalogoReportes(),
                Clasificaciones = new List<ClienteReporteClasificacionDto>
                {
                    new ClienteReporteClasificacionDto { Id = string.Empty, Nombre = "Todas" },
                    new ClienteReporteClasificacionDto { Id = ClienteTipos.Particular.ToString(CultureInfo.InvariantCulture), Nombre = "Particular" },
                    new ClienteReporteClasificacionDto { Id = ClienteTipos.Empresa.ToString(CultureInfo.InvariantCulture), Nombre = "Empresa" }
                }
            });
        }

        [HttpGet("Generar")]
        public async Task<IActionResult> Generar(
            Guid idEmpresa,
            string reporte = "",
            string busqueda = "",
            byte? clasificacion = null,
            int? top = null,
            string? fechaInicial = null,
            string? fechaFinal = null,
            string? fechaCorte = null,
            string? periodo1Inicial = null,
            string? periodo1Final = null,
            string? periodo2Inicial = null,
            string? periodo2Final = null)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                ClienteReporteQuery query = CreateQuery(
                    reporte,
                    busqueda,
                    clasificacion,
                    top,
                    fechaInicial,
                    fechaFinal,
                    fechaCorte,
                    periodo1Inicial,
                    periodo1Final,
                    periodo2Inicial,
                    periodo2Final);

                ClienteReporteResponse response = await BuildReportAsync(context.IdEmpresa, query);
                return Ok(response);
            }
            catch (ReportValidationException ex)
            {
                return BadRequest(new ClienteOperacionResponse { Mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Generar", "No fue posible generar el reporte de clientes.");
            }
        }

        [HttpGet("ExportarExcel")]
        public async Task<IActionResult> ExportarExcel(
            Guid idEmpresa,
            string reporte = "",
            string busqueda = "",
            byte? clasificacion = null,
            int? top = null,
            string? fechaInicial = null,
            string? fechaFinal = null,
            string? fechaCorte = null,
            string? periodo1Inicial = null,
            string? periodo1Final = null,
            string? periodo2Inicial = null,
            string? periodo2Final = null)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                ClienteReporteQuery query = CreateQuery(
                    reporte,
                    busqueda,
                    clasificacion,
                    top,
                    fechaInicial,
                    fechaFinal,
                    fechaCorte,
                    periodo1Inicial,
                    periodo1Final,
                    periodo2Inicial,
                    periodo2Final);

                ClienteReporteResponse response = await BuildReportAsync(context.IdEmpresa, query);
                if (!string.Equals(response.Estado, "ready", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new ClienteOperacionResponse
                    {
                        Mensaje = string.IsNullOrWhiteSpace(response.Mensaje)
                            ? "El reporte seleccionado no tiene una base de datos suficiente para exportarse."
                            : response.Mensaje
                    });
                }

                byte[] bytes = BuildExcelDocument(response);
                string safeName = SanitizeFileName(response.Titulo);
                string fileName = $"{safeName}-{DateTime.Now:yyyyMMdd-HHmm}.xlsx";
                return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (ReportValidationException ex)
            {
                return BadRequest(new ClienteOperacionResponse { Mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ExportarExcel", "No fue posible exportar el reporte de clientes.");
            }
        }

        private async Task<ClienteReporteResponse> BuildReportAsync(Guid idEmpresa, ClienteReporteQuery query)
        {
            ClienteReporteDefinicionDto definicion = BuildCatalogoReportes()
                .FirstOrDefault(item => string.Equals(item.Id, query.ReporteId, StringComparison.OrdinalIgnoreCase))
                ?? throw new ReportValidationException("Selecciona un reporte válido.");

            if (!string.Equals(definicion.Estado, "ready", StringComparison.OrdinalIgnoreCase))
            {
                return new ClienteReporteResponse
                {
                    ReporteId = definicion.Id,
                    Titulo = definicion.Nombre,
                    Descripcion = definicion.Descripcion,
                    Estado = "gap",
                    Mensaje = definicion.Motivo,
                    Indicadores = new List<ClienteReporteIndicadorDto>
                    {
                        new ClienteReporteIndicadorDto { Etiqueta = "Estado", Valor = "Brecha real de datos" }
                    }
                };
            }

            using SqlConnection connection = CreateConnection();
            await connection.OpenAsync();

            return query.ReporteId switch
            {
                ClienteReporteIds.RankingFrecuencia => await BuildRankingFrecuenciaAsync(connection, idEmpresa, query, definicion),
                ClienteReporteIds.AntiguedadRecencia => await BuildAntiguedadRecenciaAsync(connection, idEmpresa, query, definicion),
                ClienteReporteIds.NuevosPeriodo => await BuildNuevosPeriodoAsync(connection, idEmpresa, query, definicion),
                ClienteReporteIds.ComparativoPeriodos => await BuildComparativoPeriodosAsync(connection, idEmpresa, query, definicion),
                _ => throw new ReportValidationException("Selecciona un reporte válido.")
            };
        }

        private async Task<ClienteReporteResponse> BuildRankingFrecuenciaAsync(SqlConnection connection, Guid idEmpresa, ClienteReporteQuery query, ClienteReporteDefinicionDto definicion)
        {
            EnsureDateRange(query.FechaInicial, query.FechaFinal, "Captura un rango de fechas válido para el ranking de frecuencia.");

            using SqlCommand command = new SqlCommand(@"
SELECT TOP (@Top)
    c.id,
    c.Nombre,
    c.TipoCliente,
    ISNULL(c.Telefono, '') AS Telefono,
    ISNULL(c.Correo, '') AS Correo,
    COUNT(n.id) AS Interacciones,
    SUM(CASE WHEN ISNULL(n.EsTarea, 0) = 1 THEN 1 ELSE 0 END) AS Tareas,
    SUM(CASE WHEN ISNULL(n.EsTarea, 0) = 1 AND ISNULL(n.Completada, 0) = 1 THEN 1 ELSE 0 END) AS TareasCompletadas,
    MAX(COALESCE(n.FechaCompletada, n.FechaActualizacion, n.FechaCreacion)) AS UltimaActividad
FROM dbo.Clientes c
LEFT JOIN dbo.ClientesNotas n
    ON n.idEmpresa = c.idEmpresa
   AND n.idCliente = c.id
   AND n.Activo = 1
   AND n.FechaArchivado IS NULL
   AND n.FechaCreacion >= @FechaInicial
   AND n.FechaCreacion < @FechaFinalExclusiva
WHERE c.idEmpresa = @IdEmpresa
  AND c.Activo = 1
  AND c.FechaArchivado IS NULL
  /**filtro-busqueda**/
  /**filtro-clasificacion**/
GROUP BY c.id, c.Nombre, c.TipoCliente, c.Telefono, c.Correo
HAVING COUNT(n.id) > 0
ORDER BY COUNT(n.id) DESC, MAX(COALESCE(n.FechaCompletada, n.FechaActualizacion, n.FechaCreacion)) DESC, c.Nombre ASC", connection);

            string sql = ApplyClientFilters(command, query, command.CommandText);
            command.CommandText = sql;
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@Top", query.Top);
            command.Parameters.AddWithValue("@FechaInicial", query.FechaInicial!.Value.Date);
            command.Parameters.AddWithValue("@FechaFinalExclusiva", query.FechaFinal!.Value.Date.AddDays(1));

            List<ClienteReporteFilaDto> filas = new List<ClienteReporteFilaDto>();
            int totalInteracciones = 0;
            int totalTareas = 0;
            int totalCompletadas = 0;

            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                int interacciones = ReadInt(reader, "Interacciones");
                int tareas = ReadInt(reader, "Tareas");
                int tareasCompletadas = ReadInt(reader, "TareasCompletadas");
                DateTime? ultimaActividad = ReadNullableDateTime(reader, "UltimaActividad");

                totalInteracciones += interacciones;
                totalTareas += tareas;
                totalCompletadas += tareasCompletadas;

                filas.Add(BuildRow(
                    ReadGuid(reader, "id"),
                    ReadString(reader, "Nombre"),
                    $"{ReadString(reader, "Telefono")} · {ReadString(reader, "Correo")}".Trim(' ', '·'),
                    new[]
                    {
                        ReadString(reader, "Nombre"),
                        GetTipoClienteNombre(ReadByte(reader, "TipoCliente")),
                        ReadString(reader, "Telefono"),
                        ReadString(reader, "Correo"),
                        interacciones.ToString(CultureInfo.InvariantCulture),
                        tareas.ToString(CultureInfo.InvariantCulture),
                        tareasCompletadas.ToString(CultureInfo.InvariantCulture),
                        ultimaActividad.HasValue ? ultimaActividad.Value.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture) : "Sin actividad"
                    }));
            }

            return new ClienteReporteResponse
            {
                ReporteId = definicion.Id,
                Titulo = "Ranking de seguimiento por frecuencia",
                Descripcion = "Conteo de notas y tareas registradas por cliente dentro del periodo consultado.",
                Estado = "ready",
                Rango = BuildSingleRangeLabel(query.FechaInicial!.Value, query.FechaFinal!.Value),
                Indicadores = new List<ClienteReporteIndicadorDto>
                {
                    new ClienteReporteIndicadorDto { Etiqueta = "Clientes", Valor = filas.Count.ToString(CultureInfo.InvariantCulture) },
                    new ClienteReporteIndicadorDto { Etiqueta = "Interacciones", Valor = totalInteracciones.ToString(CultureInfo.InvariantCulture) },
                    new ClienteReporteIndicadorDto { Etiqueta = "Tareas", Valor = totalTareas.ToString(CultureInfo.InvariantCulture) },
                    new ClienteReporteIndicadorDto { Etiqueta = "Tareas completadas", Valor = totalCompletadas.ToString(CultureInfo.InvariantCulture) }
                },
                Columnas = BuildColumns("Cliente", "Clasificación", "Teléfono", "Correo", "Interacciones", "Tareas", "Tareas completadas", "Última actividad"),
                Filas = filas
            };
        }

        private async Task<ClienteReporteResponse> BuildAntiguedadRecenciaAsync(SqlConnection connection, Guid idEmpresa, ClienteReporteQuery query, ClienteReporteDefinicionDto definicion)
        {
            DateTime fechaCorte = query.FechaCorte?.Date ?? DateTime.Today;

            using SqlCommand command = new SqlCommand(@"
SELECT
    TOP (@Top)
    c.id,
    c.Nombre,
    c.TipoCliente,
    ISNULL(c.Telefono, '') AS Telefono,
    ISNULL(c.Correo, '') AS Correo,
    c.FechaCreacion,
    MAX(COALESCE(n.FechaCompletada, n.FechaActualizacion, n.FechaCreacion, c.FechaActualizacion, c.FechaCreacion)) AS UltimaActividad,
    COUNT(n.id) AS Notas,
    SUM(CASE WHEN ISNULL(n.EsTarea, 0) = 1 AND ISNULL(n.Completada, 0) = 0 THEN 1 ELSE 0 END) AS TareasPendientes
FROM dbo.Clientes c
LEFT JOIN dbo.ClientesNotas n
    ON n.idEmpresa = c.idEmpresa
   AND n.idCliente = c.id
   AND n.Activo = 1
   AND n.FechaArchivado IS NULL
   AND n.FechaCreacion < @FechaCorteExclusiva
WHERE c.idEmpresa = @IdEmpresa
  AND c.Activo = 1
  AND c.FechaArchivado IS NULL
  AND c.FechaCreacion < @FechaCorteExclusiva
  /**filtro-busqueda**/
  /**filtro-clasificacion**/
GROUP BY c.id, c.Nombre, c.TipoCliente, c.Telefono, c.Correo, c.FechaCreacion
ORDER BY DATEDIFF(DAY, MAX(COALESCE(n.FechaCompletada, n.FechaActualizacion, n.FechaCreacion, c.FechaActualizacion, c.FechaCreacion)), @FechaCorte) DESC,
         c.FechaCreacion ASC", connection);

            string sql = ApplyClientFilters(command, query, command.CommandText);
            command.CommandText = sql;
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@Top", query.Top);
            command.Parameters.AddWithValue("@FechaCorte", fechaCorte);
            command.Parameters.AddWithValue("@FechaCorteExclusiva", fechaCorte.AddDays(1));

            List<ClienteReporteFilaDto> filas = new List<ClienteReporteFilaDto>();
            int carteraActiva = 0;
            int pendientes = 0;

            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                DateTime fechaCreacion = ReadDateTime(reader, "FechaCreacion");
                DateTime? ultimaActividad = ReadNullableDateTime(reader, "UltimaActividad");
                int notas = ReadInt(reader, "Notas");
                int tareasPendientes = ReadInt(reader, "TareasPendientes");
                int diasComoCliente = Math.Max(0, (fechaCorte - fechaCreacion.Date).Days);
                int diasSinSeguimiento = ultimaActividad.HasValue
                    ? Math.Max(0, (fechaCorte - ultimaActividad.Value.Date).Days)
                    : diasComoCliente;

                carteraActiva++;
                pendientes += tareasPendientes;

                filas.Add(BuildRow(
                    ReadGuid(reader, "id"),
                    ReadString(reader, "Nombre"),
                    $"{ReadString(reader, "Telefono")} · {ReadString(reader, "Correo")}".Trim(' ', '·'),
                    new[]
                    {
                        ReadString(reader, "Nombre"),
                        GetTipoClienteNombre(ReadByte(reader, "TipoCliente")),
                        fechaCreacion.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                        diasComoCliente.ToString(CultureInfo.InvariantCulture),
                        ultimaActividad.HasValue ? ultimaActividad.Value.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture) : "Sin seguimiento",
                        diasSinSeguimiento.ToString(CultureInfo.InvariantCulture),
                        notas.ToString(CultureInfo.InvariantCulture),
                        tareasPendientes.ToString(CultureInfo.InvariantCulture)
                    }));
            }

            return new ClienteReporteResponse
            {
                ReporteId = definicion.Id,
                Titulo = "Antigüedad y recencia del cliente",
                Descripcion = "Muestra la antigüedad del cliente y su actividad más reciente registrada en CheckApp.",
                Estado = "ready",
                Rango = $"Corte al {fechaCorte:dd/MM/yyyy}",
                Indicadores = new List<ClienteReporteIndicadorDto>
                {
                    new ClienteReporteIndicadorDto { Etiqueta = "Clientes", Valor = carteraActiva.ToString(CultureInfo.InvariantCulture) },
                    new ClienteReporteIndicadorDto { Etiqueta = "Pendientes", Valor = pendientes.ToString(CultureInfo.InvariantCulture) },
                    new ClienteReporteIndicadorDto { Etiqueta = "Corte", Valor = fechaCorte.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) }
                },
                Columnas = BuildColumns("Cliente", "Clasificación", "Fecha de alta", "Días como cliente", "Última actividad", "Días desde actividad", "Notas", "Tareas pendientes"),
                Filas = filas
            };
        }

        private async Task<ClienteReporteResponse> BuildNuevosPeriodoAsync(SqlConnection connection, Guid idEmpresa, ClienteReporteQuery query, ClienteReporteDefinicionDto definicion)
        {
            EnsureDateRange(query.FechaInicial, query.FechaFinal, "Captura un rango de fechas válido para el reporte de nuevos.");

            using SqlCommand command = new SqlCommand(@"
SELECT TOP (@Top)
    c.id,
    c.Nombre,
    c.TipoCliente,
    ISNULL(c.Telefono, '') AS Telefono,
    ISNULL(c.Correo, '') AS Correo,
    c.FechaCreacion,
    COUNT(n.id) AS Notas,
    SUM(CASE WHEN ISNULL(n.EsTarea, 0) = 1 AND ISNULL(n.Completada, 0) = 0 THEN 1 ELSE 0 END) AS TareasPendientes
FROM dbo.Clientes c
LEFT JOIN dbo.ClientesNotas n
    ON n.idEmpresa = c.idEmpresa
   AND n.idCliente = c.id
   AND n.Activo = 1
   AND n.FechaArchivado IS NULL
WHERE c.idEmpresa = @IdEmpresa
  AND c.Activo = 1
  AND c.FechaArchivado IS NULL
  AND c.FechaCreacion >= @FechaInicial
  AND c.FechaCreacion < @FechaFinalExclusiva
  /**filtro-busqueda**/
  /**filtro-clasificacion**/
GROUP BY c.id, c.Nombre, c.TipoCliente, c.Telefono, c.Correo, c.FechaCreacion
ORDER BY c.FechaCreacion DESC, c.Nombre ASC", connection);

            string sql = ApplyClientFilters(command, query, command.CommandText);
            command.CommandText = sql;
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@Top", query.Top);
            command.Parameters.AddWithValue("@FechaInicial", query.FechaInicial!.Value.Date);
            command.Parameters.AddWithValue("@FechaFinalExclusiva", query.FechaFinal!.Value.Date.AddDays(1));

            List<ClienteReporteFilaDto> filas = new List<ClienteReporteFilaDto>();

            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                filas.Add(BuildRow(
                    ReadGuid(reader, "id"),
                    ReadString(reader, "Nombre"),
                    $"{ReadString(reader, "Telefono")} · {ReadString(reader, "Correo")}".Trim(' ', '·'),
                    new[]
                    {
                        ReadDateTime(reader, "FechaCreacion").ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
                        ReadString(reader, "Nombre"),
                        GetTipoClienteNombre(ReadByte(reader, "TipoCliente")),
                        ReadString(reader, "Telefono"),
                        ReadString(reader, "Correo"),
                        ReadInt(reader, "Notas").ToString(CultureInfo.InvariantCulture),
                        ReadInt(reader, "TareasPendientes").ToString(CultureInfo.InvariantCulture),
                        "Alta en periodo"
                    }));
            }

            return new ClienteReporteResponse
            {
                ReporteId = definicion.Id,
                Titulo = "Clientes registrados por periodo",
                Descripcion = "Clientes dados de alta dentro del periodo seleccionado, con su seguimiento actual.",
                Estado = "ready",
                Rango = BuildSingleRangeLabel(query.FechaInicial!.Value, query.FechaFinal!.Value),
                Indicadores = new List<ClienteReporteIndicadorDto>
                {
                    new ClienteReporteIndicadorDto { Etiqueta = "Altas", Valor = filas.Count.ToString(CultureInfo.InvariantCulture) },
                    new ClienteReporteIndicadorDto { Etiqueta = "Particulares", Valor = filas.Count(x => x.Celdas.Count > 2 && x.Celdas[2] == "Particular").ToString(CultureInfo.InvariantCulture) },
                    new ClienteReporteIndicadorDto { Etiqueta = "Empresas", Valor = filas.Count(x => x.Celdas.Count > 2 && x.Celdas[2] == "Empresa").ToString(CultureInfo.InvariantCulture) }
                },
                Columnas = BuildColumns("Fecha de alta", "Cliente", "Clasificación", "Teléfono", "Correo", "Notas", "Pendientes", "Lectura"),
                Filas = filas
            };
        }

        private async Task<ClienteReporteResponse> BuildComparativoPeriodosAsync(SqlConnection connection, Guid idEmpresa, ClienteReporteQuery query, ClienteReporteDefinicionDto definicion)
        {
            EnsureDateRange(query.Periodo1Inicial, query.Periodo1Final, "Captura un primer periodo válido para el comparativo.");
            EnsureDateRange(query.Periodo2Inicial, query.Periodo2Final, "Captura un segundo periodo válido para el comparativo.");

            using SqlCommand command = new SqlCommand(@"
SELECT TOP (@Top)
    c.id,
    c.Nombre,
    c.TipoCliente,
    ISNULL(c.Telefono, '') AS Telefono,
    ISNULL(c.Correo, '') AS Correo,
    SUM(CASE WHEN n.FechaCreacion >= @Periodo1Inicial AND n.FechaCreacion < @Periodo1FinalExclusiva THEN 1 ELSE 0 END) AS SeguimientoP1,
    SUM(CASE WHEN n.FechaCreacion >= @Periodo2Inicial AND n.FechaCreacion < @Periodo2FinalExclusiva THEN 1 ELSE 0 END) AS SeguimientoP2,
    SUM(CASE WHEN ISNULL(n.EsTarea, 0) = 1 AND n.FechaCreacion >= @Periodo1Inicial AND n.FechaCreacion < @Periodo1FinalExclusiva THEN 1 ELSE 0 END) AS TareasP1,
    SUM(CASE WHEN ISNULL(n.EsTarea, 0) = 1 AND n.FechaCreacion >= @Periodo2Inicial AND n.FechaCreacion < @Periodo2FinalExclusiva THEN 1 ELSE 0 END) AS TareasP2,
    MAX(COALESCE(n.FechaCompletada, n.FechaActualizacion, n.FechaCreacion, c.FechaActualizacion, c.FechaCreacion)) AS UltimaActividad
FROM dbo.Clientes c
LEFT JOIN dbo.ClientesNotas n
    ON n.idEmpresa = c.idEmpresa
   AND n.idCliente = c.id
   AND n.Activo = 1
   AND n.FechaArchivado IS NULL
WHERE c.idEmpresa = @IdEmpresa
  AND c.Activo = 1
  AND c.FechaArchivado IS NULL
  /**filtro-busqueda**/
  /**filtro-clasificacion**/
GROUP BY c.id, c.Nombre, c.TipoCliente, c.Telefono, c.Correo
HAVING
    SUM(CASE WHEN n.FechaCreacion >= @Periodo1Inicial AND n.FechaCreacion < @Periodo1FinalExclusiva THEN 1 ELSE 0 END) > 0
    OR SUM(CASE WHEN n.FechaCreacion >= @Periodo2Inicial AND n.FechaCreacion < @Periodo2FinalExclusiva THEN 1 ELSE 0 END) > 0
ORDER BY
    (SUM(CASE WHEN n.FechaCreacion >= @Periodo2Inicial AND n.FechaCreacion < @Periodo2FinalExclusiva THEN 1 ELSE 0 END)
     - SUM(CASE WHEN n.FechaCreacion >= @Periodo1Inicial AND n.FechaCreacion < @Periodo1FinalExclusiva THEN 1 ELSE 0 END)) DESC,
    c.Nombre ASC", connection);

            string sql = ApplyClientFilters(command, query, command.CommandText);
            command.CommandText = sql;
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@Top", query.Top);
            command.Parameters.AddWithValue("@Periodo1Inicial", query.Periodo1Inicial!.Value.Date);
            command.Parameters.AddWithValue("@Periodo1FinalExclusiva", query.Periodo1Final!.Value.Date.AddDays(1));
            command.Parameters.AddWithValue("@Periodo2Inicial", query.Periodo2Inicial!.Value.Date);
            command.Parameters.AddWithValue("@Periodo2FinalExclusiva", query.Periodo2Final!.Value.Date.AddDays(1));

            List<ClienteReporteFilaDto> filas = new List<ClienteReporteFilaDto>();
            int totalP1 = 0;
            int totalP2 = 0;

            using SqlDataReader reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                int seguimientoP1 = ReadInt(reader, "SeguimientoP1");
                int seguimientoP2 = ReadInt(reader, "SeguimientoP2");
                int tareasP1 = ReadInt(reader, "TareasP1");
                int tareasP2 = ReadInt(reader, "TareasP2");
                int variacion = seguimientoP2 - seguimientoP1;
                DateTime? ultimaActividad = ReadNullableDateTime(reader, "UltimaActividad");

                totalP1 += seguimientoP1;
                totalP2 += seguimientoP2;

                filas.Add(BuildRow(
                    ReadGuid(reader, "id"),
                    ReadString(reader, "Nombre"),
                    $"{ReadString(reader, "Telefono")} · {ReadString(reader, "Correo")}".Trim(' ', '·'),
                    new[]
                    {
                        ReadString(reader, "Nombre"),
                        GetTipoClienteNombre(ReadByte(reader, "TipoCliente")),
                        seguimientoP1.ToString(CultureInfo.InvariantCulture),
                        seguimientoP2.ToString(CultureInfo.InvariantCulture),
                        variacion >= 0 ? $"+{variacion}" : variacion.ToString(CultureInfo.InvariantCulture),
                        $"{tareasP1} / {tareasP2}",
                        ultimaActividad.HasValue ? ultimaActividad.Value.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture) : "Sin actividad",
                        variacion > 0 ? "Creció" : variacion < 0 ? "Disminuyó" : "Sin cambio"
                    }));
            }

            return new ClienteReporteResponse
            {
                ReporteId = definicion.Id,
                Titulo = "Comparativo de seguimiento entre periodos",
                Descripcion = "Comparación de notas y tareas registradas por cliente entre dos periodos reales.",
                Estado = "ready",
                Rango = $"P1 {BuildShortRangeLabel(query.Periodo1Inicial!.Value, query.Periodo1Final!.Value)} · P2 {BuildShortRangeLabel(query.Periodo2Inicial!.Value, query.Periodo2Final!.Value)}",
                Indicadores = new List<ClienteReporteIndicadorDto>
                {
                    new ClienteReporteIndicadorDto { Etiqueta = "Clientes", Valor = filas.Count.ToString(CultureInfo.InvariantCulture) },
                    new ClienteReporteIndicadorDto { Etiqueta = "Interacciones P1", Valor = totalP1.ToString(CultureInfo.InvariantCulture) },
                    new ClienteReporteIndicadorDto { Etiqueta = "Interacciones P2", Valor = totalP2.ToString(CultureInfo.InvariantCulture) },
                    new ClienteReporteIndicadorDto { Etiqueta = "Variación", Valor = (totalP2 - totalP1).ToString("+0;-0;0", CultureInfo.InvariantCulture) }
                },
                Columnas = BuildColumns("Cliente", "Clasificación", "Interacciones P1", "Interacciones P2", "Variación", "Tareas P1 / P2", "Última actividad", "Lectura"),
                Filas = filas
            };
        }

        private static List<ClienteReporteDefinicionDto> BuildCatalogoReportes()
        {
            return new List<ClienteReporteDefinicionDto>
            {
                new ClienteReporteDefinicionDto
                {
                    Id = ClienteReporteIds.RankingFrecuencia,
                    Nombre = "Ranking de seguimiento por frecuencia",
                    Descripcion = "Frecuencia de notas y tareas registradas por cliente dentro de un periodo.",
                    Estado = "ready"
                },
                new ClienteReporteDefinicionDto
                {
                    Id = ClienteReporteIds.AntiguedadRecencia,
                    Nombre = "Antigüedad y recencia del cliente",
                    Descripcion = "Muestra la antigüedad del cliente y su actividad más reciente registrada en CheckApp.",
                    Estado = "ready"
                },
                new ClienteReporteDefinicionDto
                {
                    Id = ClienteReporteIds.NuevosPeriodo,
                    Nombre = "Clientes registrados por periodo",
                    Descripcion = "Clientes dados de alta dentro del periodo consultado.",
                    Estado = "ready"
                },
                new ClienteReporteDefinicionDto
                {
                    Id = ClienteReporteIds.ComparativoPeriodos,
                    Nombre = "Comparativo de seguimiento entre periodos",
                    Descripcion = "Comparación de notas y tareas registradas por cliente entre dos periodos.",
                    Estado = "ready"
                }
            };
        }

        private static ClienteReporteQuery CreateQuery(
            string reporte,
            string busqueda,
            byte? clasificacion,
            int? top,
            string? fechaInicial,
            string? fechaFinal,
            string? fechaCorte,
            string? periodo1Inicial,
            string? periodo1Final,
            string? periodo2Inicial,
            string? periodo2Final)
        {
            return new ClienteReporteQuery
            {
                ReporteId = (reporte ?? string.Empty).Trim().ToLowerInvariant(),
                Busqueda = (busqueda ?? string.Empty).Trim(),
                Clasificacion = clasificacion == ClienteTipos.Particular || clasificacion == ClienteTipos.Empresa ? clasificacion : null,
                Top = top.HasValue && top.Value > 0 ? Math.Min(top.Value, 200) : 100,
                FechaInicial = TryParseDate(fechaInicial),
                FechaFinal = TryParseDate(fechaFinal),
                FechaCorte = TryParseDate(fechaCorte),
                Periodo1Inicial = TryParseDate(periodo1Inicial),
                Periodo1Final = TryParseDate(periodo1Final),
                Periodo2Inicial = TryParseDate(periodo2Inicial),
                Periodo2Final = TryParseDate(periodo2Final)
            };
        }

        private static void EnsureDateRange(DateTime? inicio, DateTime? fin, string message)
        {
            if (!inicio.HasValue || !fin.HasValue || fin.Value.Date < inicio.Value.Date)
            {
                throw new ReportValidationException(message);
            }
        }

        private static DateTime? TryParseDate(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            return DateTime.TryParse(raw.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out DateTime parsed)
                ? parsed.Date
                : null;
        }

        private static string ApplyClientFilters(SqlCommand command, ClienteReporteQuery query, string sql)
        {
            if (!string.IsNullOrWhiteSpace(query.Busqueda))
            {
                sql = sql.Replace("/**filtro-busqueda**/", @"
  AND (
      c.Nombre LIKE @Busqueda
      OR ISNULL(c.Telefono, '') LIKE @Busqueda
      OR ISNULL(c.Correo, '') LIKE @Busqueda
      OR ISNULL(c.Empresa, '') LIKE @Busqueda
  )");
                command.Parameters.AddWithValue("@Busqueda", $"%{query.Busqueda}%");
            }
            else
            {
                sql = sql.Replace("/**filtro-busqueda**/", string.Empty);
            }

            if (query.Clasificacion.HasValue)
            {
                sql = sql.Replace("/**filtro-clasificacion**/", " AND c.TipoCliente = @Clasificacion");
                command.Parameters.AddWithValue("@Clasificacion", query.Clasificacion.Value);
            }
            else
            {
                sql = sql.Replace("/**filtro-clasificacion**/", string.Empty);
            }

            return sql;
        }

        private static List<ClienteReporteColumnaDto> BuildColumns(params string[] titles)
        {
            return titles.Select(title => new ClienteReporteColumnaDto { Titulo = title }).ToList();
        }

        private static ClienteReporteFilaDto BuildRow(Guid idCliente, string principal, string secundario, IEnumerable<string> cells)
        {
            return new ClienteReporteFilaDto
            {
                IdCliente = idCliente,
                Principal = principal,
                Secundario = secundario,
                AccionTexto = "Abrir cliente",
                AccionUrl = $"/Clientes/EdicionAvanzada?idCliente={idCliente}&returnUrl=%2FClientes%2FReporte",
                Celdas = cells.Select(value => value ?? string.Empty).ToList()
            };
        }

        private static string BuildSingleRangeLabel(DateTime fechaInicial, DateTime fechaFinal)
        {
            return $"{fechaInicial:dd/MM/yyyy} al {fechaFinal:dd/MM/yyyy}";
        }

        private static string BuildShortRangeLabel(DateTime fechaInicial, DateTime fechaFinal)
        {
            return $"{fechaInicial:dd/MM} al {fechaFinal:dd/MM}";
        }

        private static string SanitizeFileName(string title)
        {
            StringBuilder builder = new StringBuilder();
            foreach (char character in title.ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(character);
                }
                else if (character == ' ' || character == '-' || character == '_')
                {
                    builder.Append('-');
                }
            }

            string normalized = builder.ToString().Trim('-');
            return string.IsNullOrWhiteSpace(normalized) ? "reporte-clientes" : normalized;
        }

        private static byte[] BuildExcelDocument(ClienteReporteResponse reporte)
        {
            using MemoryStream stream = new MemoryStream();
            using (SpreadsheetDocument spreadsheet = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook, true))
            {
                WorkbookPart workbookPart = spreadsheet.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();

                WorkbookStylesPart stylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
                stylesPart.Stylesheet = BuildWorkbookStylesheet();
                stylesPart.Stylesheet.Save();

                WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                SheetData sheetData = new SheetData();
                worksheetPart.Worksheet = new Worksheet(
                    new SheetViews(new SheetView { WorkbookViewId = 0U }),
                    new SheetFormatProperties { DefaultRowHeight = 15D },
                    BuildReportColumns(Math.Max(9, reporte.Columnas.Count + 1)),
                    sheetData);

                Sheets sheets = workbookPart.Workbook.AppendChild(new Sheets());
                sheets.Append(new Sheet
                {
                    Id = workbookPart.GetIdOfPart(worksheetPart),
                    SheetId = 1U,
                    Name = "Reporte"
                });

                uint rowIndex = 1;
                sheetData.Append(
                    BuildTextRow(rowIndex++, 1U, reporte.Titulo),
                    BuildTextRow(rowIndex++, 0U, reporte.Descripcion),
                    BuildTextRow(rowIndex++, 0U, $"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}"),
                    BuildTextRow(rowIndex++, 0U, $"Rango: {reporte.Rango}"),
                    new Row { RowIndex = rowIndex++ });

                List<string> headers = new List<string> { "Acción" };
                headers.AddRange(reporte.Columnas.Select(item => item.Titulo));
                sheetData.Append(BuildHeaderRow(rowIndex++, headers));

                foreach (ClienteReporteFilaDto fila in reporte.Filas)
                {
                    List<string> values = new List<string> { fila.AccionTexto };
                    values.AddRange(fila.Celdas);
                    sheetData.Append(BuildValueRow(rowIndex++, values));
                }

                workbookPart.Workbook.Save();
            }

            return stream.ToArray();
        }

        private static Columns BuildReportColumns(int totalColumns)
        {
            Columns columns = new Columns();
            for (uint index = 1; index <= totalColumns; index++)
            {
                columns.Append(new Column
                {
                    Min = index,
                    Max = index,
                    Width = index == 1 ? 18D : 24D,
                    CustomWidth = true
                });
            }

            return columns;
        }

        private static Row BuildHeaderRow(uint rowIndex, IReadOnlyList<string> headers)
        {
            Row row = new Row { RowIndex = rowIndex };
            for (int index = 0; index < headers.Count; index++)
            {
                row.Append(BuildTextCell(GetColumnName(index + 1), rowIndex, headers[index], 1U));
            }

            return row;
        }

        private static Row BuildValueRow(uint rowIndex, IReadOnlyList<string> values)
        {
            Row row = new Row { RowIndex = rowIndex };
            for (int index = 0; index < values.Count; index++)
            {
                row.Append(BuildTextCell(GetColumnName(index + 1), rowIndex, values[index], 0U));
            }

            return row;
        }

        private static Row BuildTextRow(uint rowIndex, uint styleIndex, string value)
        {
            return new Row(
                BuildTextCell("A", rowIndex, value, styleIndex))
            { RowIndex = rowIndex };
        }

        private static Cell BuildTextCell(string columnName, uint rowIndex, string value, uint styleIndex)
        {
            return new Cell
            {
                CellReference = $"{columnName}{rowIndex}",
                StyleIndex = styleIndex,
                DataType = CellValues.InlineString,
                InlineString = new InlineString(new Text(value ?? string.Empty))
            };
        }

        private static Stylesheet BuildWorkbookStylesheet()
        {
            return new Stylesheet(
                new Fonts(
                    new Font(
                        new FontSize { Val = 11D },
                        new Color { Rgb = new HexBinaryValue("1F2937") },
                        new FontName { Val = "Arial" }),
                    new Font(
                        new Bold(),
                        new FontSize { Val = 11D },
                        new Color { Rgb = new HexBinaryValue("FFFFFF") },
                        new FontName { Val = "Arial" })),
                new Fills(
                    new Fill(new PatternFill { PatternType = PatternValues.None }),
                    new Fill(new PatternFill { PatternType = PatternValues.Gray125 }),
                    new Fill(new PatternFill(
                        new ForegroundColor { Rgb = new HexBinaryValue("D41010") },
                        new BackgroundColor { Indexed = 64U })
                    { PatternType = PatternValues.Solid })),
                new Borders(
                    new Border(
                        new LeftBorder(),
                        new RightBorder(),
                        new TopBorder(),
                        new BottomBorder(),
                        new DiagonalBorder())),
                new CellFormats(
                    new CellFormat(),
                    new CellFormat
                    {
                        FontId = 1U,
                        FillId = 2U,
                        BorderId = 0U,
                        ApplyFill = true,
                        ApplyFont = true
                    }));
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
                _logger.LogWarning("ClientesReporte proxy headers recibidos sin secreto compartido configurado.");
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

            if ((DateTimeOffset.UtcNow - timestamp.ToUniversalTime()).Duration() > ProxyHeaderTolerance)
            {
                _logger.LogWarning("ClientesReporte proxy headers expirados o fuera de tolerancia para empresa {EmpresaId}.", empresaId);
                return false;
            }

            string payload = string.Join('\n', empresaIdRaw.Trim(), empresaKeyRaw.Trim().ToUpperInvariant(), usuarioIdRaw.Trim(), timestampRaw.Trim());
            string expectedSignature = ComputeProxySignature(secret, payload);

            if (!SignaturesMatch(expectedSignature, signatureRaw))
            {
                _logger.LogWarning("ClientesReporte proxy headers con firma inválida para empresa {EmpresaId}.", empresaId);
                return false;
            }

            context = new SignedProxyContext
            {
                IdEmpresa = empresaId,
                EmpresaStorageKey = empresaKeyRaw.ToUpperInvariant()
            };

            HttpContext.Items[ProxyContextItemKey] = context;
            return true;
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
            _logger.LogError(ex, "Error en ClientesReporte durante {Operation}.", operation);
            return StatusCode(500, new ClienteOperacionResponse { Mensaje = safeMessage });
        }

        private static Guid ReadGuid(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? Guid.Empty : reader.GetGuid(ordinal);
        }

        private static byte ReadByte(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? (byte)0 : reader.GetByte(ordinal);
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
            return value is DateTime dateTime ? dateTime : Convert.ToDateTime(value, CultureInfo.InvariantCulture);
        }

        private static string GetTipoClienteNombre(byte tipoCliente)
        {
            return tipoCliente == ClienteTipos.Empresa ? "Empresa" : "Particular";
        }

        private static string GetColumnName(int columnIndex)
        {
            int dividend = columnIndex;
            string columnName = string.Empty;
            while (dividend > 0)
            {
                int modulo = (dividend - 1) % 26;
                columnName = Convert.ToChar(65 + modulo) + columnName;
                dividend = (dividend - modulo) / 26;
            }

            return columnName;
        }

        private sealed class ClienteReporteQuery
        {
            public string ReporteId { get; set; } = string.Empty;
            public string Busqueda { get; set; } = string.Empty;
            public byte? Clasificacion { get; set; }
            public int Top { get; set; } = 100;
            public DateTime? FechaInicial { get; set; }
            public DateTime? FechaFinal { get; set; }
            public DateTime? FechaCorte { get; set; }
            public DateTime? Periodo1Inicial { get; set; }
            public DateTime? Periodo1Final { get; set; }
            public DateTime? Periodo2Inicial { get; set; }
            public DateTime? Periodo2Final { get; set; }
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
        }

        private sealed class ReportValidationException : Exception
        {
            public ReportValidationException(string message) : base(message)
            {
            }
        }
    }
}
