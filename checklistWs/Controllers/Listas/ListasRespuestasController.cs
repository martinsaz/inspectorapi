using checklistWs.Models.Lista;
using checklistWs.Utiles;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Text;

namespace checklistWs.Controllers.Listas
{
	public class ListasRespuestasController : Controller
	{
		public IActionResult Index()
		{
			return View();
		}

		private readonly IConfiguration _configuration;
		private readonly SqlConnectionFactory _connectionFactory;

		public ListasRespuestasController(IConfiguration configuration)
		{
			_configuration = configuration;
			_connectionFactory = new SqlConnectionFactory(configuration);
		}



		[HttpGet]
		[Route("ListasRespuestas/GetElemento")]
		public async Task<IActionResult> GetElemento(Guid id, string empresa, string cadena)
		{
			try
			{
				
				List<ListasRespuestas2> regresa = new List<ListasRespuestas2>();
                byte[] data = Convert.FromBase64String(cadena);

                // Si quieres convertir los bytes a una cadena original
                cadena = Encoding.UTF8.GetString(data);

                using (SqlConnection connection = new SqlConnection(cadena))
				{
					connection.Open();
					string sQuery = string.Format("SELECT lr.id, lr.idEmpresa, lr.idLista, lr.idPregunta, lr.RespuestaValor, lr.Notas, lr.idAlumno, lr.idPrograma, lr.idTipoPregunta, lr.Explicacion, lr.Valor, lr.Calificacion, lr.obligatoria, l.Nombre as Lista, lp.Pregunta,  cl.Nombre + ' ' + cca1.apellido + ' ' + cca2.apellido as Alumno  from ListasRespuestas lr LEFT JOIN Listas l on lr.idLista = l.id LEFT JOIN ListasPreguntas lp on lr.idPregunta = lp.id LEFT JOIN clientes cl on lr.idAlumno = cl.id LEFT JOIN CatalogoClientesApellidos cca1 on cl.idApellidoPaterno = cca1.id LEFT JOIN CatalogoClientesApellidos cca2 on cl.idApellidoMaterno = cca2.id where lr.id = '{0}'", id);
					using (SqlCommand command = new SqlCommand(sQuery, connection))
					{
						using (SqlDataReader reader = await command.ExecuteReaderAsync())
						{
							while (reader.Read())
							{
								ListasRespuestas2 item = new ListasRespuestas2();
								item.id = reader["id"] != DBNull.Value ? Guid.Parse(reader["id"].ToString()) : Guid.Empty;
								item.idEmpresa = reader["idEmpresa"] != DBNull.Value ? Guid.Parse(reader["idEmpresa"].ToString()) : Guid.Empty;
								item.idLista = reader["idLista"] != DBNull.Value ? Guid.Parse(reader["idLista"].ToString()) : Guid.Empty;
								item.idPregunta = reader["idPregunta"] != DBNull.Value ? Guid.Parse(reader["idPregunta"].ToString()) : Guid.Empty;
								item.RespuestaValor = reader["RespuestaValor"] != DBNull.Value ? reader["RespuestaValor"].ToString().Trim() : string.Empty;
								item.Notas = reader["Notas"] != DBNull.Value ? reader["Notas"].ToString().Trim() : string.Empty;
								item.idAlumno = reader["idAlumno"] != DBNull.Value ? Guid.Parse(reader["idAlumno"].ToString()) : Guid.Empty;
								item.idPrograma = reader["idPrograma"] != DBNull.Value ? Guid.Parse(reader["idPrograma"].ToString()) : Guid.Empty;
								item.idTipoPregunta = reader["idTipoPregunta"] != DBNull.Value ? Convert.ToDecimal(reader["idTipoPregunta"]) : 0;
								item.Explicacion = reader["Explicacion"] != DBNull.Value ? reader["Explicacion"].ToString().Trim() : string.Empty;
								item.Valor = reader["Valor"] != DBNull.Value ? Convert.ToDecimal(reader["Valor"]) : 0;
								item.Calificacion = reader["Calificacion"] != DBNull.Value ? Convert.ToDecimal(reader["Calificacion"]) : 0;
								item.obligatoria = reader["obligatoria"] != DBNull.Value ? bool.Parse(reader["obligatoria"].ToString()) : false;
								item.Pregunta = reader["Pregunta"] != DBNull.Value ? reader["Pregunta"].ToString().Trim() : string.Empty;
								item.Lista = reader["Lista"] != DBNull.Value ? reader["Lista"].ToString().Trim() : string.Empty;
								item.Alumno = reader["Alumno"] != DBNull.Value ? reader["Alumno"].ToString().Trim() : string.Empty;
								//
								regresa.Add(item);
							}
						}
					}
					return Ok(regresa);
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
		[Route("ListasRespuestas/GetTodos")]
		public async Task<IActionResult> GetTodos(Guid idEmpresa, string empresa, string cadena)
		{
			try
			{
				
				List<ListasRespuestas2> regresa = new List<ListasRespuestas2>();
                byte[] data = Convert.FromBase64String(cadena);

                // Si quieres convertir los bytes a una cadena original
                cadena = Encoding.UTF8.GetString(data);

                using (SqlConnection connection = new SqlConnection(cadena))
				{
					connection.Open();
					string sQuery = string.Format(" SELECT lr.id, lr.idEmpresa, lr.idLista, lr.idPregunta , lr.RespuestaValor, lr.Notas, lr.idAlumno, lr.idPrograma, lr.idTipoPregunta, lr.Explicacion, lr.Valor, lr.Calificacion, lr.obligatoria, l.Nombre as Lista, lp.Pregunta,  cl.Nombre + ' ' + cca1.apellido + ' ' + cca2.apellido as Alumno  from ListasRespuestas lr LEFT JOIN Listas l on lr.idLista = l.id LEFT JOIN ListasPreguntas lp on lr.idPregunta = lp.id LEFT JOIN clientes cl on lr.idAlumno = cl.id LEFT JOIN CatalogoClientesApellidos cca1 on cl.idApellidoPaterno = cca1.id LEFT JOIN CatalogoClientesApellidos cca2 on cl.idApellidoMaterno = cca2.id where lr.idEmpresa = '{0}'", idEmpresa);
					using (SqlCommand command = new SqlCommand(sQuery, connection))
					{
						using (SqlDataReader reader = await command.ExecuteReaderAsync())
						{
							while (reader.Read())
							{

								ListasRespuestas2 item = new ListasRespuestas2();
								item.id = reader["id"] != DBNull.Value ? Guid.Parse(reader["id"].ToString()) : Guid.Empty;
								item.idEmpresa = reader["idEmpresa"] != DBNull.Value ? Guid.Parse(reader["idEmpresa"].ToString()) : Guid.Empty;
								item.idLista = reader["idLista"] != DBNull.Value ? Guid.Parse(reader["idLista"].ToString()) : Guid.Empty;
								item.idPregunta = reader["idPregunta"] != DBNull.Value ? Guid.Parse(reader["idPregunta"].ToString()) : Guid.Empty;
								item.RespuestaValor = reader["RespuestaValor"] != DBNull.Value ? reader["RespuestaValor"].ToString().Trim() : string.Empty;
								item.Notas = reader["Notas"] != DBNull.Value ? reader["Notas"].ToString().Trim() : string.Empty;
								item.idAlumno = reader["idAlumno"] != DBNull.Value ? Guid.Parse(reader["idAlumno"].ToString()) : Guid.Empty;
								item.idPrograma = reader["idPrograma"] != DBNull.Value ? Guid.Parse(reader["idPrograma"].ToString()) : Guid.Empty;
								item.idTipoPregunta = reader["idTipoPregunta"] != DBNull.Value ? Convert.ToDecimal(reader["idTipoPregunta"]) : 0;
								item.Explicacion = reader["Explicacion"] != DBNull.Value ? reader["Explicacion"].ToString().Trim() : string.Empty;
								item.Valor = reader["Valor"] != DBNull.Value ? Convert.ToDecimal(reader["Valor"]) : 0;
								item.Calificacion = reader["Calificacion"] != DBNull.Value ? Convert.ToDecimal(reader["Calificacion"]) : 0;
								item.obligatoria = reader["obligatoria"] != DBNull.Value ? bool.Parse(reader["obligatoria"].ToString()) : false;
								item.Pregunta = reader["Pregunta"] != DBNull.Value ? reader["Pregunta"].ToString().Trim() : string.Empty;
								item.Lista = reader["Lista"] != DBNull.Value ? reader["Lista"].ToString().Trim() : string.Empty;
								item.Alumno = reader["Alumno"] != DBNull.Value ? reader["Alumno"].ToString().Trim() : string.Empty;
								//
								regresa.Add(item);
							}
						}
					}
					return Ok(regresa);
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
		[Route("ListasRespuestas/GetLista")]
		public async Task<IActionResult> GetLista(Guid idLista, Guid idEmpresa, Guid idUsuario, string empresa, string cadena)
		{
			try
			{
				
				List<ListasRespuestasDetalle> regresa = new List<ListasRespuestasDetalle>();
                byte[] data = Convert.FromBase64String(cadena);

          
                cadena = Encoding.UTF8.GetString(data);

                using (SqlConnection connection = new SqlConnection(cadena))
				{
					connection.Open();
					

					string sQuery = string.Format("WITH DistinctOpciones AS (\r\n    -- Opciones únicas por pregunta\r\n    SELECT DISTINCT idPregunta, idLista, opcion \r\n    FROM dbo.ListasPreguntasOpciones\r\n),\r\nRespuestaUnica AS (\r\n    -- Selecciona una única respuesta por pregunta\r\n    SELECT\r\n        idPregunta,\r\n        respuestavalor,\r\n        ROW_NUMBER() OVER (PARTITION BY idPregunta ORDER BY (SELECT NULL)) AS rn \r\n    FROM dbo.ListasRespuestas \r\n    WHERE evento= '{0}' \r\n        AND idEmpresa = '{1}' \r\n        AND idUsuario = '{2}'\r\n),\r\nRespuestasAgrupadas AS (\r\n    -- Genera la tabla temporal de respuestas\r\n    SELECT\r\n        LR.id,\r\n        LR.idPregunta,\r\n        LR.idLista,\r\n        L.Nombre AS Evaluacion,\r\n        LP.Pregunta,\r\n        LR.notas, -- Se agrega el campo notas\r\n        ISNULL(\r\n            (\r\n                SELECT STRING_AGG(CAST(DO.opcion AS VARCHAR(MAX)), ', ') \r\n                FROM DistinctOpciones DO \r\n                WHERE DO.idPregunta = LR.idPregunta \r\n                AND DO.idLista = LR.idLista\r\n            ), '') AS RespuestaOpciones,\r\n        CASE\r\n            WHEN LP.Tipo = 2 THEN \r\n                (SELECT TOP 1 respuestavalor FROM RespuestaUnica WHERE RespuestaUnica.idPregunta = LR.idPregunta AND rn = 1)\r\n            ELSE \r\n                (SELECT STRING_AGG(CAST(LR2.respuestavalor AS VARCHAR(MAX)), ', ') \r\n                 FROM dbo.ListasRespuestas AS LR2 \r\n                 WHERE LR2.idPregunta = LR.idPregunta \r\n                 AND LR2.evento = LR.evento \r\n                 AND LR2.idEmpresa = LR.idEmpresa)\r\n        END AS Respuesta,\r\n        CASE\r\n            WHEN LP.Tipo = 1 THEN 'Calificacion' \r\n            WHEN LP.Tipo = 2 THEN 'Opción simple' \r\n            WHEN LP.Tipo = 3 THEN 'Opción Múltiple' \r\n            WHEN LP.Tipo = 4 THEN 'Texto comentarios' \r\n            WHEN LP.Tipo = 5 THEN 'Valor númerico' \r\n            WHEN LP.Tipo = 6 THEN 'Fechas' \r\n        END AS Tipo,\r\n        LPC.Nombre AS NombreCategoria,\r\n        LPSC.Nombre AS NombreSubCategoria \r\n    FROM dbo.ListasRespuestas LR\r\n    INNER JOIN dbo.Listas L ON LR.idLista = L.id\r\n    INNER JOIN dbo.ListasPreguntas LP ON LR.idPregunta = LP.id \r\n        AND L.id = LP.idLista\r\n    LEFT JOIN dbo.ListasPreguntasCategorias LPC ON LP.idCategoria = LPC.id\r\n    LEFT JOIN dbo.ListasPreguntasSubCategorias LPSC ON LP.idSubcategoria = LPSC.id \r\n    WHERE evento= '{0}' \r\n        AND LR.idEmpresa = '{1}' \r\n        AND LR.idUsuario = '{2}'\r\n),\r\nAnexos AS (\r\n    -- Selecciona una única URL por idListaRespuesta\r\n    SELECT DISTINCT idListaRespuesta, \r\n           (SELECT TOP 1 url FROM dbo.AnexoPregunta WHERE AnexoPregunta.idListaRespuesta = AR.idListaRespuesta ORDER BY id) AS url\r\n    FROM dbo.AnexoPregunta AS AR\r\n)\r\nSELECT\r\n    Evaluacion,\r\n    Subquery.id,\r\n    Pregunta,\r\n    RespuestaOpciones,\r\n    Respuesta,\r\n    Tipo,\r\n    idPregunta,\r\n    NombreCategoria,\r\n    NombreSubCategoria,\r\n    Subquery.notas, -- Se agrega el campo notas en el SELECT final\r\n    Anexos.url -- Solo incluimos la URL única\r\nFROM\r\n    (\r\n        SELECT\r\n            Evaluacion,\r\n            id,\r\n            Pregunta,\r\n            RespuestaOpciones,\r\n            Respuesta,\r\n            Tipo,\r\n            idPregunta,\r\n            NombreCategoria,\r\n            NombreSubCategoria,\r\n            notas, -- Se pasa el campo notas\r\n            ROW_NUMBER() OVER (PARTITION BY Pregunta ORDER BY id) AS rn \r\n        FROM RespuestasAgrupadas\r\n    ) AS Subquery\r\nLEFT JOIN Anexos ON Subquery.id = Anexos.idListaRespuesta\r\nWHERE rn = 1\r\nORDER BY\r\n    CASE\r\n        WHEN PATINDEX('[0-9]%.%', Pregunta) = 1 THEN CAST(SUBSTRING(Pregunta, 1, CHARINDEX('.', Pregunta) - 1) AS INT)\r\n        WHEN PATINDEX('[0-9]%', Pregunta) = 1 THEN CAST(SUBSTRING(Pregunta, 1, PATINDEX('%[^0-9]%', Pregunta + ' ') - 1) AS INT)\r\n        ELSE NULL\r\n    END,\r\n    Pregunta;", idLista, idEmpresa, idUsuario);



                    using (SqlCommand command = new SqlCommand(sQuery, connection))
					{
						using (SqlDataReader reader = await command.ExecuteReaderAsync())
						{
							while (reader.Read())
							{

								ListasRespuestasDetalle item = new ListasRespuestasDetalle();
								item.idPregunta = reader["idPregunta"] != DBNull.Value ? Guid.Parse(reader["idPregunta"].ToString()) : Guid.Empty;
                                item.id = reader["id"] != DBNull.Value ? Guid.Parse(reader["id"].ToString()) : Guid.Empty;
                                item.Evaluacion = reader["Evaluacion"] != DBNull.Value ? (reader["Evaluacion"].ToString()) : string.Empty;
								item.Pregunta = reader["Pregunta"] != DBNull.Value ? (reader["Pregunta"].ToString()) : string.Empty;
								item.RespuestaOpciones = reader["RespuestaOpciones"] != DBNull.Value ? (reader["RespuestaOpciones"].ToString()) : string.Empty;
								item.Respuesta = reader["Respuesta"] != DBNull.Value ? (reader["Respuesta"].ToString()) : string.Empty;
								item.Tipo = reader["Tipo"] != DBNull.Value ? (reader["Tipo"].ToString()) : string.Empty;
								item.categoria = reader["NombreCategoria"] != DBNull.Value ? (reader["NombreCategoria"].ToString()) : string.Empty;
								item.subcategoria = reader["NombreSubCategoria"] != DBNull.Value ? (reader["NombreSubCategoria"].ToString()) : string.Empty;
                                item.urlAnexo = reader["url"] != DBNull.Value ? (reader["url"].ToString()) : string.Empty;
                                item.notas = reader["notas"] != DBNull.Value ? reader["notas"].ToString().Trim() : string.Empty;



                                regresa.Add(item);
							}
						}
					}
					return Ok(regresa);
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
        [Route("ListasRespuestas/GetAnexo")]
        public async Task<IActionResult> GetAnexo(Guid idListaRespuesta, string empresa, string cadena)
        {
            try
            {

                List<AnexoPregunta> regresa = new List<AnexoPregunta>();
                byte[] data = Convert.FromBase64String(cadena);

               
                cadena = Encoding.UTF8.GetString(data);

                using (SqlConnection connection = new SqlConnection(cadena))
				{
                    connection.Open();
                    string sQuery = string.Format("select * from AnexoPregunta where idListaRespuesta = '{0}'", idListaRespuesta);
                    using (SqlCommand command = new SqlCommand(sQuery, connection))
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (reader.Read())
                            {

                                AnexoPregunta item = new AnexoPregunta();
                                item.id = reader["id"] != DBNull.Value ? Guid.Parse(reader["id"].ToString()) : Guid.Empty;
                                item.url = reader["url"] != DBNull.Value ? (reader["url"].ToString()) : string.Empty;
                                item.tipo_anexo = reader["tipo_anexo"] != DBNull.Value ? Convert.ToInt32(reader["tipo_anexo"]) : 0;
                                regresa.Add(item);
                            }
                        }
                    }
                    return Ok(regresa);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                // Retornar un código de error HTTP 500 (Internal Server Error)
                return StatusCode(500, $"Error interno del servidor {ex.Message}");
            }
        }


        [HttpPost]
		[Route("ListasRespuestas/Guardar")]
		public async Task<IActionResult> Guardar(ListasRespuestas2 datos, string empresa, string cadena)
		{
			try
			{

                byte[] data = Convert.FromBase64String(cadena);

                // Si quieres convertir los bytes a una cadena original
                cadena = Encoding.UTF8.GetString(data);

                using (SqlConnection connection = new SqlConnection(cadena))
				{
					connection.Open();
					string sQuery = string.Empty;
					Guid insertado = Guid.NewGuid();
					if (await Existe((Guid)datos.id, empresa, cadena))
					{
						sQuery = string.Format("UPDATE ListasRespuestas SET  idEmpresa = @idEmpresa, idLista = @idLista, idPregunta = @idPregunta, RespuestaValor = @RespuestaValor, Notas = @Notas, idAlumno = @idAlumno, idPrograma = @idPrograma, idTipoPregunta = @idTipoPregunta, Explicacion = @Explicacion, Valor = @Valor, Calificacion = @Calificacion, obligatoria = obligatoria WHERE id = '{0}'", datos.id);
					}
					else
					{
						sQuery = string.Format("INSERT INTO ListasRespuestas (id, idEmpresa, idLista, idPregunta, RespuestaValor, Notas, idAlumno, idPrograma, idTipoPregunta, Explicacion, Valor, Calificacion, obligatoria) VALUES ('{0}', @idEmpresa, @idLista, @idPregunta, @RespuestaValor, @Notas, @idAlumno, @idPrograma, @idTipoPregunta, @Explicacion, @Valor, @Calificacion, @obligatoria)", insertado);
					}
					using (SqlCommand command = new SqlCommand(sQuery, connection))
					{

						if (datos.idEmpresa != null) command.Parameters.AddWithValue("@idEmpresa", datos.idEmpresa); else command.Parameters.AddWithValue("@idEmpresa", DBNull.Value);
						if (datos.idLista != null) command.Parameters.AddWithValue("@idLista", datos.idLista); else command.Parameters.AddWithValue("@idLista", DBNull.Value);
						if (datos.idPregunta != null) command.Parameters.AddWithValue("@idPregunta", datos.idPregunta); else command.Parameters.AddWithValue("@idPregunta", DBNull.Value);
						if (datos.RespuestaValor != null) command.Parameters.AddWithValue("@RespuestaValor", datos.RespuestaValor); else command.Parameters.AddWithValue("@RespuestaValor", DBNull.Value);
						if (datos.Notas != null) command.Parameters.AddWithValue("@Notas", datos.Notas); else command.Parameters.AddWithValue("@Notas", DBNull.Value);
						if (datos.idAlumno != null) command.Parameters.AddWithValue("@idAlumno", datos.idAlumno); else command.Parameters.AddWithValue("@idAlumno", DBNull.Value);
						if (datos.idPrograma != null) command.Parameters.AddWithValue("@idPrograma", datos.idPrograma); else command.Parameters.AddWithValue("@idPrograma", DBNull.Value);
						if (datos.idTipoPregunta != null) command.Parameters.AddWithValue("@idTipoPregunta", datos.idTipoPregunta); else command.Parameters.AddWithValue("@idTipoPregunta", DBNull.Value);
						if (datos.Explicacion != null) command.Parameters.AddWithValue("@Explicacion", datos.Explicacion); else command.Parameters.AddWithValue("@Explicacion", DBNull.Value);
						if (datos.Valor != null) command.Parameters.AddWithValue("@Valor", datos.Valor); else command.Parameters.AddWithValue("@Valor", DBNull.Value);
						if (datos.Calificacion != null) command.Parameters.AddWithValue("@Calificacion", datos.Calificacion); else command.Parameters.AddWithValue("@Calificacion", DBNull.Value);
						if (datos.obligatoria != null) command.Parameters.AddWithValue("@obligatoria", datos.obligatoria); else command.Parameters.AddWithValue("@obligatoria", DBNull.Value);

						await command.ExecuteNonQueryAsync();
					}
				}
				return Ok("Ok");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error: {ex.Message}");
				// Retornar un código de error HTTP 500 (Internal Server Error)
				return StatusCode(500, $"Error interno del servidor {ex.Message}");
			}
		}


		//[HttpDelete]
		//[Route("ListasPreguntas/Borrar")]
		//public async Task<IHttpActionResult> Borrar(Guid id)
		//{
		//    try
		//    {
		//        string cadena = SqlConnectionFactory.ObtenerCadenaConexion();
		//        using (SqlConnection connection = new SqlConnection(cadena))
		//        {
		//            connection.Open();
		//            string sQuery = $@"UPDATE ListasPreguntas SET Status = '0' WHERE id = '{id}'";
		//            using (SqlCommand command = new SqlCommand(sQuery, connection))
		//            {
		//                await command.ExecuteNonQueryAsync();
		//            }
		//        }
		//        return Ok("Ok");
		//    }
		//    catch (Exception ex)
		//    {
		//        return InternalServerError(ex);
		//    }
		//}

		//Utilerias

		private async Task<bool> Existe(Guid cualId, string empresa, string cadena)
		{
			bool regresa = false;
			if (cualId != Guid.Empty)
			{
				try
				{

                  



                    using (SqlConnection connection = new SqlConnection(cadena))
					{
						connection.Open();
						string sQuery = string.Format("SELECT COUNT(*) FROM ListasRespuestas WHERE id = '{0}'", cualId);
						using (SqlCommand command = new SqlCommand(sQuery, connection))
						{
							using (SqlDataReader reader = await command.ExecuteReaderAsync())
							{
								if (reader.HasRows)
								{
									reader.Read();
									if (Convert.ToInt32(reader[0]) > 0) regresa = true;
								}
							}
						}
					}
				}
				catch (Exception ex)
				{
				}
			}
			return regresa;
		}
	}
}
