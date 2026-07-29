using checklistWs.Models.Mislistas;
using checklistWs.Models.Reportes;
using checklistWs.Utiles;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace checklistWs.Controllers.Reportes
{
    public class ReportesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        private readonly IConfiguration _configuration;
        private readonly SqlConnectionFactory _connectionFactory;

        public ReportesController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionFactory = new SqlConnectionFactory(configuration);
        }


        [HttpGet]
        [Route("GetReporteEstrellas")]
        public async Task<IActionResult> GetReporteEstrellas(Guid idEmpresa, string empresa, Guid idLista, string tipoPregunta, string cadena)
        {
            try
            {
                List<ReporteEstrellas1> ListaRoles = new List<ReporteEstrellas1>();

                byte[] data = Convert.FromBase64String(cadena);


                cadena = Encoding.UTF8.GetString(data);
                using (SqlConnection connection = new SqlConnection(cadena))
                {

                    string sQuery = $"WITH RankedEvents AS ( SELECT idSucursal, idLista, idEmpresa, evento,  FechaRespuesta," +
                        $" ROW_NUMBER() OVER (PARTITION BY idSucursal ORDER BY FechaRespuesta DESC) AS rn FROM ListasRespuestas  " +
                        $" WHERE idLista = '{idLista}'), CTE_UltimaEvaluacion AS ( SELECT idSucursal, idLista," +
                        $" idEmpresa, evento, MAX(CAST(FechaRespuesta AS DATE)) AS UltimaFechaEvaluacion FROM RankedEvents WHERE rn = 1" +
                        $" GROUP BY idSucursal, idLista, idEmpresa, evento), CTE_RespuestasRecientes AS ( SELECT lr.idSucursal, lr.idLista, " +
                        $" lr.idEmpresa, lr.evento, CAST(CAST(lr.respuestavalor AS VARCHAR(50)) AS DECIMAL(10, 2)) AS respuestavalor, " +
                        $" CAST(lr.FechaRespuesta AS DATE) AS FechaRespuesta FROM ListasRespuestas lr INNER JOIN CTE_UltimaEvaluacion ue" +
                        $" ON lr.idSucursal = ue.idSucursal AND lr.idLista = ue.idLista AND lr.idEmpresa = ue.idEmpresa AND lr.evento = ue.evento" +
                        $" AND CAST(lr.FechaRespuesta AS DATE) = ue.UltimaFechaEvaluacion WHERE lr.idTipoPregunta = {tipoPregunta} AND " +
                        $" lr.idLista = '{idLista}'), CTE_Respuestas12Meses AS (SELECT lr.idSucursal, lr.idLista," +
                        $" lr.idEmpresa, AVG(CAST(CAST(lr.respuestavalor AS VARCHAR(50)) AS DECIMAL(10, 2))) AS respuestavalor" +
                        $" FROM ListasRespuestas lr WHERE lr.idTipoPregunta = {tipoPregunta} AND lr.idLista = '{idLista}'" +
                        $" AND CAST(lr.FechaRespuesta AS DATE) >= DATEADD(MONTH, -12, CAST(GETDATE() AS DATE)) GROUP BY lr.idSucursal, " +
                        $" lr.idLista, lr.idEmpresa ) SELECT s.nombre AS Sucursal, l.nombre AS NombreLista, ue.UltimaFechaEvaluacion," +
                        $" AVG(rr.respuestavalor) AS PromedioUltimaEvaluacion, (SELECT AVG(r12.respuestavalor) FROM CTE_Respuestas12Meses r12" +
                        $" WHERE r12.idSucursal = ue.idSucursal AND r12.idLista = ue.idLista AND r12.idEmpresa = ue.idEmpresa) AS PromedioUltimos12Meses" +
                        $" FROM CTE_UltimaEvaluacion ue LEFT JOIN CTE_RespuestasRecientes rr ON ue.idSucursal = rr.idSucursal AND ue.idLista = rr.idLista" +
                        $" AND ue.idEmpresa = rr.idEmpresa AND ue.evento = rr.evento INNER JOIN dbo.Sucursales s ON ue.idSucursal = s.id " +
                        $" INNER JOIN dbo.Listas l ON ue.idLista = l.id GROUP BY s.nombre, l.nombre, ue.idSucursal, ue.idLista, ue.idEmpresa, " +
                        $" ue.UltimaFechaEvaluacion ORDER BY ue.UltimaFechaEvaluacion DESC;";
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(sQuery, connection))
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (reader.Read())
                            {
                                ListaRoles.Add(new ReporteEstrellas1()
                                {
                                    Sucursal = reader["Sucursal"] != DBNull.Value ? (reader["Sucursal"].ToString()) : string.Empty,
                                    NombreLista = reader["NombreLista"] != DBNull.Value ? (reader["NombreLista"].ToString()) : string.Empty,
                                    UltimaFechaEvaluacion = reader["UltimaFechaEvaluacion"] != DBNull.Value ? reader["UltimaFechaEvaluacion"].ToString().Trim() : string.Empty,
                                    PromedioUltimaEvaluacion = reader["PromedioUltimaEvaluacion"] != DBNull.Value ? reader["PromedioUltimaEvaluacion"].ToString().Trim() : string.Empty,
                                    PromedioUltimos12Meses = reader["PromedioUltimos12Meses"] != DBNull.Value ? reader["PromedioUltimos12Meses"].ToString().Trim() : string.Empty
                                });
                            }
                        }
                    }
                    return Ok(ListaRoles);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                // Retornar un código de error HTTP 500 (Internal Server Error)
                return StatusCode(500, $"Error interno del servidor {ex.Message}");
            }
        }


        [HttpGet]
        [Route("GetReporteListado")]
        public async Task<IActionResult> GetReporteListado(Guid idEmpresa, string empresa, Guid idLista, string evento, Guid idSucursal, string cadena)
        {
            try
            {
                List<ReporteListado> ListaRoles = new List<ReporteListado>();

                byte[] data = Convert.FromBase64String(cadena);


                cadena = Encoding.UTF8.GetString(data);

                using (SqlConnection connection = new SqlConnection(cadena))
                {
                    string sQuery = $" SELECT \r\n    ListasPreguntas.Pregunta, \r\n    ListasPreguntasCategorias.Nombre AS Categoria,  \r\n    ListasPreguntasSubCategorias.Nombre AS Subcategoria, \r\n    ListasRespuestas.RespuestaValor, \r\n    ListasRespuestas.ValorCorrecto, \r\n    ListasRespuestas.FechaRespuesta, \r\n    ListasRespuestas.notas, \r\n    ListasRespuestas.explicacion, \r\n    Usuarios.Nombre AS Usuario, \r\n    CASE \r\n        WHEN ListasRespuestas.idTipoPregunta = 1 THEN 'Calificacion' \r\n        WHEN ListasRespuestas.idTipoPregunta = 2 THEN 'Opción simple' \r\n        WHEN ListasRespuestas.idTipoPregunta = 3 THEN 'Opción Múltiple' \r\n        WHEN ListasRespuestas.idTipoPregunta = 4 THEN 'Texto comentarios' \r\n        WHEN ListasRespuestas.idTipoPregunta = 5 THEN 'Valor númerico' \r\n        WHEN ListasRespuestas.idTipoPregunta = 6 THEN 'Fechas' \r\n    END AS TipoDeTarea, \r\n    CASE \r\n        WHEN TRY_CONVERT(FLOAT, CAST(ListasRespuestas.RespuestaValor AS VARCHAR(MAX))) IS NOT NULL \r\n             AND ListasRespuestas.ValorCorrecto IS NOT NULL \r\n             AND ListasRespuestas.ValorCorrecto <> 0 THEN \r\n            (TRY_CONVERT(FLOAT, CAST(ListasRespuestas.RespuestaValor AS VARCHAR(MAX))) * 100.0) / ListasRespuestas.ValorCorrecto \r\n        ELSE \r\n            0 \r\n    END AS Ponderacion \r\nFROM \r\n    dbo.ListasRespuestas \r\nINNER JOIN \r\n    dbo.ListasPreguntas ON ListasRespuestas.idPregunta = ListasPreguntas.id \r\nINNER JOIN \r\n    dbo.Usuarios ON ListasRespuestas.idUsuario = Usuarios.id \r\nINNER JOIN \r\n    dbo.ListasPreguntasCategorias ON ListasPreguntas.idCategoria = ListasPreguntasCategorias.id \r\nINNER JOIN \r\n    dbo.ListasPreguntasSubCategorias ON ListasPreguntas.idSubCategoria = ListasPreguntasSubCategorias.id  \r\nWHERE  ListasRespuestas.idlista = '{idLista}'  AND ListasRespuestas.evento = '{evento}' AND " +
                        $" ListasRespuestas.idSucursal = '{idSucursal}' ORDER BY 1 ASC;";
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(sQuery, connection))
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (reader.Read())
                            {
                                ListaRoles.Add(new ReporteListado()
                                {
                                    Pregunta = reader["Pregunta"] != DBNull.Value ? (reader["Pregunta"].ToString()) : string.Empty,
                                    Categoria = reader["Categoria"] != DBNull.Value ? (reader["Categoria"].ToString()) : string.Empty,
                                    Subcategoria = reader["Subcategoria"] != DBNull.Value ? reader["Subcategoria"].ToString().Trim() : string.Empty,
                                    RespuestaValor = reader["RespuestaValor"] != DBNull.Value ? reader["RespuestaValor"].ToString().Trim() : string.Empty,
                                    ValorCorrecto = reader["ValorCorrecto"] != DBNull.Value ? reader["ValorCorrecto"].ToString().Trim() : string.Empty,
                                    FechaRespuesta = reader["FechaRespuesta"] != DBNull.Value ? reader["FechaRespuesta"].ToString().Trim() : string.Empty,
                                    Notas = reader["Notas"] != DBNull.Value ? reader["Notas"].ToString().Trim() : string.Empty,
                                    Explicacion = reader["Explicacion"] != DBNull.Value ? reader["Explicacion"].ToString().Trim() : string.Empty,
                                    Usuario = reader["Usuario"] != DBNull.Value ? reader["Usuario"].ToString().Trim() : string.Empty,
                                    TipoDeTarea = reader["TipoDeTarea"] != DBNull.Value ? reader["TipoDeTarea"].ToString().Trim() : string.Empty,
                                    Ponderacion = reader["Ponderacion"] != DBNull.Value ? reader["Ponderacion"].ToString().Trim() : string.Empty

                                });
                            }
                        }
                    }
                    return Ok(ListaRoles);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                // Retornar un código de error HTTP 500 (Internal Server Error)
                return StatusCode(500, $"Error interno del servidor {ex.Message}");
            }
        }

        [HttpGet]
        [Route("ReporteDinamico")]

        public async Task<IActionResult> ReporteDinamico(string empresa, Guid idLista, string tipoPregunta, string cadena)
        {
            try
            {
                DataTable dataTable = new DataTable();

                byte[] data = Convert.FromBase64String(cadena);


                cadena = Encoding.UTF8.GetString(data);

                string sQuery = $"DECLARE @columns NVARCHAR(MAX);" +
                    $" SELECT @columns = STRING_AGG(QUOTENAME(nombre), ', ') FROM ( SELECT DISTINCT lpc.nombre FROM ListasPreguntasCategorias lpc" +
                    $" INNER JOIN ListasPreguntas lp ON lpc.id = lp.idCategoria INNER JOIN ListasRespuestas lr ON lp.id = lr.idPregunta" +
                    $" WHERE lr.idLista = '{idLista}' AND lr.idTipoPregunta = {tipoPregunta}) AS distinct_categories;  " +
                    $" DECLARE @query NVARCHAR(MAX); SET @query = 'WITH RankedEvents AS (SELECT idSucursal, idLista, idEmpresa,   " +
                    $" evento, FechaRespuesta, ROW_NUMBER() OVER (PARTITION BY idSucursal ORDER BY FechaRespuesta DESC) AS rn FROM ListasRespuestas" +
                    $" WHERE idLista = ''{idLista}'' AND idTipoPregunta = {tipoPregunta}), CTE_UltimaEvaluacion AS ( SELECT idSucursal, idLista,  idEmpresa, " +
                    $" evento, MAX(CAST(FechaRespuesta AS DATE)) AS UltimaFechaEvaluacion FROM RankedEvents WHERE rn = 1 GROUP BY idSucursal," +
                    $" idLista, idEmpresa, evento), FilteredRespuestas AS (SELECT lr.idSucursal, lr.idLista, lr.idEmpresa, lr.evento, lr.idPregunta," +
                    $" lr.FechaRespuesta, TRY_CONVERT(DECIMAL(10, 2), CAST(lr.respuestavalor AS VARCHAR(MAX))) AS respuestavalor  FROM ListasRespuestas lr" +
                    $" WHERE lr.idLista = ''{idLista}'' AND lr.idTipoPregunta = {tipoPregunta} AND TRY_CONVERT(DECIMAL(10, 2), CAST(lr.respuestavalor AS VARCHAR(MAX))) " +
                    $" IS NOT NULL), CTE_RespuestasRecientes AS ( SELECT fr.idSucursal, fr.idLista, fr.idEmpresa, fr.evento, fr.idPregunta, fr.respuestavalor," +
                    $" CAST(fr.FechaRespuesta AS DATE) AS FechaRespuesta FROM FilteredRespuestas fr INNER JOIN CTE_UltimaEvaluacion ue ON fr.idSucursal = ue.idSucursal " +
                    $" AND fr.idLista = ue.idLista AND fr.idEmpresa = ue.idEmpresa AND fr.evento = ue.evento AND CAST(fr.FechaRespuesta AS DATE) = ue.UltimaFechaEvaluacion)," +
                    $" CategoriaRespuestas AS ( SELECT fr.idSucursal, fr.idLista, fr.idEmpresa, lpc.nombre AS CategoriaNombre, AVG(fr.respuestavalor) " +
                    $" AS PromedioCategoria FROM CTE_RespuestasRecientes fr INNER JOIN ListasPreguntas lp ON fr.idPregunta = lp.id INNER JOIN ListasPreguntasCategorias" +
                    $" lpc ON lp.idCategoria = lpc.id WHERE fr.idLista = ''{idLista}'' GROUP BY fr.idSucursal, fr.idLista, fr.idEmpresa, lpc.nombre)," +
                    $" PivotedCategorias AS (SELECT idSucursal, ' + @columns + ' FROM (SELECT idSucursal, CategoriaNombre, PromedioCategoria FROM CategoriaRespuestas) " +
                    $" AS SourceTable PIVOT (AVG(PromedioCategoria) FOR CategoriaNombre IN (' + @columns + ') ) AS PivotTable), CTE_Respuestas12Meses " +
                    $" AS ( SELECT fr.idSucursal,   fr.idLista, fr.idEmpresa, AVG(fr.respuestavalor) AS respuestavalor FROM FilteredRespuestas fr WHERE " +
                    $" CAST(fr.FechaRespuesta AS DATE) >= DATEADD(MONTH, -12, CAST(GETDATE() AS DATE)) GROUP BY fr.idSucursal, fr.idLista, fr.idEmpresa) " +
                    $" SELECT s.nombre AS Sucursal, l.nombre AS NombreLista, ue.UltimaFechaEvaluacion, (SELECT AVG(r12.respuestavalor) FROM CTE_Respuestas12Meses" +
                    $" r12 WHERE r12.idSucursal = ue.idSucursal AND r12.idLista = ue.idLista AND r12.idEmpresa = ue.idEmpresa) AS PromedioUltimos12Meses, AVG(rr.respuestavalor) AS PromedioUltimaEvaluacion  " +
                    $" , pc.' + @columns + 'FROM CTE_UltimaEvaluacion ue LEFT JOIN" +
                    $" CTE_RespuestasRecientes rr ON ue.idSucursal = rr.idSucursal AND ue.idLista = rr.idLista AND ue.idEmpresa = rr.idEmpresa " +
                    $" AND ue.evento = rr.evento LEFT JOIN PivotedCategorias pc ON ue.idSucursal = pc.idSucursal INNER JOIN dbo.Sucursales s " +
                    $" ON ue.idSucursal = s.id INNER JOIN dbo.Listas l ON ue.idLista = l.id GROUP BY s.nombre, l.nombre, ue.idSucursal, " +
                    $" ue.idLista, ue.idEmpresa, ue.UltimaFechaEvaluacion, pc.' + @columns + 'ORDER BY ue.UltimaFechaEvaluacion DESC; '" +
                    $" ; EXEC sp_executesql @query;";


                DataTable dt = Sergio.Utiles.Comandos.MyDataTable(sQuery, cadena);
                string json = JsonConvert.SerializeObject(dt);
                return Ok(json);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                // Retornar un código de error HTTP 500 (Internal Server Error)
                return StatusCode(500, $"Error interno del servidor {ex.Message}");
            }
        }


		[HttpGet]
		[Route("MisListas")]
		public async Task<IActionResult> MisListas(Guid idEmpresa, string empresa, string cadena)
		{
			try
			{
				List<Mislistas> ListaRoles = new List<Mislistas>();

				byte[] data = Convert.FromBase64String(cadena);
				cadena = Encoding.UTF8.GetString(data);

				using (SqlConnection connection = new SqlConnection(cadena))
				{
					string sQuery = $@"
                SELECT 
                    Listas.id,  
                    Listas.nombre, 
                    Listas.fechacreacion,  
                    Listas.notas, 
                    Listas.status,  
                    Listas.latitud,  
                    Listas.longitud, 
                    UPPER(ISNULL(Usuarios.Nombre, '') + ' ' + ISNULL(Usuarios.ApellidoPaterno, '') + ' ' + ISNULL(Usuarios.ApellidoMaterno, '')) AS Creador,  
                    COUNT(ListasPreguntas.Pregunta) AS preguntas,  
                    COUNT(DISTINCT ListasRespuestas.evento) AS veces 
                FROM 
                    dbo.Listas 
                    LEFT JOIN dbo.Usuarios ON Listas.idusuario = Usuarios.id 
                    LEFT JOIN dbo.ListasPreguntas ON Listas.id = ListasPreguntas.idLista 
                    LEFT JOIN dbo.ListasRespuestas ON Listas.id = ListasRespuestas.idLista 
                WHERE 
                    Listas.idEmpresa = '{idEmpresa}' 
                    AND Listas.Estado = 2 
                GROUP BY  
                    Listas.id,   
                    Listas.nombre,  
                    Listas.fechacreacion, 
                    Listas.notas, 
                    Listas.status,  
                    Listas.latitud,  
                    Listas.longitud,  
                    Usuarios.Nombre,  
                    Usuarios.ApellidoPaterno,  
                    Usuarios.ApellidoMaterno 
                ORDER BY 
                    Listas.id";

					connection.Open();
					using (SqlCommand command = new SqlCommand(sQuery, connection))
					{
						using (SqlDataReader reader = await command.ExecuteReaderAsync())
						{
							while (reader.Read())
							{
								ListaRoles.Add(new Mislistas()
								{
									Id = reader["id"] != DBNull.Value ? Guid.Parse(reader["id"].ToString()) : Guid.Empty,
									Lista = reader["nombre"] != DBNull.Value ? reader["nombre"].ToString() : string.Empty,
									FechaCreacion = reader["fechacreacion"] != DBNull.Value ? reader["fechacreacion"].ToString().Trim() : string.Empty,
									Notas = reader["notas"] != DBNull.Value ? reader["notas"].ToString().Trim() : string.Empty,
									Status = reader["status"] != DBNull.Value ? reader["status"].ToString().Trim() : string.Empty,
									latitud = reader["latitud"] != DBNull.Value ? reader["latitud"].ToString().Trim() : string.Empty,
									longitud = reader["longitud"] != DBNull.Value ? reader["longitud"].ToString().Trim() : string.Empty,
									Creador = reader["Creador"] != DBNull.Value ? reader["Creador"].ToString().Trim() : string.Empty,
									Preguntas = reader["preguntas"] != DBNull.Value ? reader["preguntas"].ToString().Trim() : string.Empty,
									Veces = reader["veces"] != DBNull.Value ? reader["veces"].ToString().Trim() : string.Empty
								});
							}
						}
					}
					return Ok(ListaRoles);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error: {ex.Message}");
				// Retornar un código de error HTTP 500 (Internal Server Error)
				return StatusCode(500, $"Error interno del servidor {ex.Message}");
			}
		}


	}
}
