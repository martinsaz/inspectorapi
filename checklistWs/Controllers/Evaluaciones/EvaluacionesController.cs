
using checklistWs.Models.Combo;
using checklistWs.Models.Lista;
using checklistWs.Models.ListaParaReporteListado;
using checklistWs.Utiles;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace checklistWs.Controllers.Evaluaciones
{
    [Route("api/[controller]")]
    [ApiController]
    public class EvaluacionesController : ControllerBase
    {

        private readonly IConfiguration _configuration;
        private readonly SqlConnectionFactory _connectionFactory;

        public EvaluacionesController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionFactory = new SqlConnectionFactory(configuration);
        }


		[HttpGet]
		[Route("Evaluacion/ObtenerPreguntasXPrograma")]
		public async Task<IActionResult> ObtenerPreguntasXProgramaAsync(Guid idPrograma, Guid idLista, string empresa, string cadena)
		{
			try
			{
				List<PreguntasXResponder> BusquedaDeCursos = new List<PreguntasXResponder>();

				byte[] data = Convert.FromBase64String(cadena);

				// Si quieres convertir los bytes a una cadena original
				cadena = Encoding.UTF8.GetString(data);

				using (SqlConnection connection = new SqlConnection(cadena))
				{
					connection.Open();

					string query = @"             SELECT 
    LP.idLista, 
    LP.id, 
    LP.pregunta, 
    LP.Explicacion, 
    LP.Tipo, 
    LP.valor, 
    LP.Obligatorio,
lp.valorcorrecto,	lp.idCategoria,
	lpc.Nombre as 'categoria',
	lp.idSubCategoria,
	lps.nombre as 'subcategoria',
	lp.Explicacion
FROM 
    ListasPreguntas LP 
INNER JOIN 
    Listas L ON LP.idLista = L.id INNER JOIN ListasPreguntasCategorias lpc ON lpc.id = lp.idCategoria INNER JOIN ListasPreguntasSubCategorias lps ON  lps.id = lp.idSubCategoria
 
 
WHERE 
     LP.idLista = @idLista  AND lp.status = 1
   
     
ORDER BY 
        CASE 
  
        WHEN PATINDEX('[0-9]%.%', lp.Pregunta) = 1 
        THEN CAST(SUBSTRING(lp.Pregunta, 1, CHARINDEX('.', lp.Pregunta) - 1) AS INT)
        
        WHEN PATINDEX('[0-9]%', lp.Pregunta) = 1 
        THEN CAST(SUBSTRING(lp.Pregunta, 1, PATINDEX('%[^0-9]%', lp.Pregunta + ' ') - 1) AS INT)
        
        ELSE NULL
    END,
    lp.Pregunta;";

					using (SqlCommand command = new SqlCommand(query, connection))
					{
						// Agregar parámetros
						command.Parameters.AddWithValue("@idLista", idLista);
						

						using (SqlDataReader reader = command.ExecuteReader())
						{
							while (reader.Read())
							{
								PreguntasXResponder BusquedaDeCurso = new PreguntasXResponder
								{
									id = reader["id"] != DBNull.Value ? Guid.Parse(reader["id"].ToString()) : Guid.Empty,
									idLista = reader["idLista"] != DBNull.Value ? Guid.Parse(reader["idLista"].ToString()) : Guid.Empty,
									pregunta = reader["pregunta"].ToString(),
									explicacion = reader["explicacion"].ToString(),
									tipo = reader["tipo"].ToString(),
									valor = reader["valor"].ToString(),
									obligatorio = reader["obligatorio"] != DBNull.Value ? bool.Parse(reader["obligatorio"].ToString()) : false,
                                    RespuestaCorrecta = reader["valorcorrecto"] != DBNull.Value ? reader["valorcorrecto"].ToString() : string.Empty,
                                    idCategoria = reader["idCategoria"] != DBNull.Value ? Guid.Parse(reader["idCategoria"].ToString()) : Guid.Empty,
                                    idSubcategoria = reader["idSubcategoria"] != DBNull.Value ? Guid.Parse(reader["idSubcategoria"].ToString()) : Guid.Empty,
                                    categoria = reader["categoria"] != DBNull.Value ? reader["categoria"].ToString() : string.Empty,
                                    subcategoria = reader["subcategoria"] != DBNull.Value ? reader["subcategoria"].ToString() : string.Empty,
                                    notas = reader["Explicacion"] != DBNull.Value ? reader["Explicacion"].ToString() : string.Empty

                                };
								BusquedaDeCursos.Add(BusquedaDeCurso);
							}
						}
					}
				}
				return Ok(BusquedaDeCursos);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error: {ex.Message}");
				// Retornar un código de error HTTP 500 (Internal Server Error)
				return StatusCode(500, $"Error interno del servidor {ex.Message}");
			}
		}


		[HttpGet]
		[Route("ObtenerComboProgramasXAlumno")]
		public async Task<IActionResult> ObtenerComboProgramasXAlumno(string empresa, string idEmpresa, string cualPrograma = "", string cadena = "")
		{
			List<DataPair2> regresa = new List<DataPair2>();
			try
			{
				byte[] data = Convert.FromBase64String(cadena);

				// Si quieres convertir los bytes a una cadena original
				cadena = Encoding.UTF8.GetString(data);

				using (SqlConnection connection = new SqlConnection(cadena))
				{
					connection.Open();
					string sComp = string.Empty;
					if (!string.IsNullOrEmpty(cualPrograma))
					{
						sComp = string.Format(" AND c.Nombre LIKE '%{0}%'", cualPrograma);
					}
					string query = string.Format("SELECT Nombre, id as 'idLista' FROM listas where Estado = 2 {0} AND idEmpresa = '{1}' Order by Nombre", sComp, idEmpresa);
					using (SqlCommand command = new SqlCommand(query, connection))
					{
						using (SqlDataReader reader = command.ExecuteReader())
						{
							while (reader.Read())
							{
								regresa.Add(new DataPair2()
								{
									name = reader["Nombre"] != DBNull.Value ? reader["Nombre"].ToString().Trim() : string.Empty,
									//value = reader["id"] != DBNull.Value ? reader["id"].ToString() : string.Empty,
									idLista = reader["idLista"] != DBNull.Value ? reader["idLista"].ToString() : string.Empty
								});
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error: {ex.Message}");
				// Retornar un código de error HTTP 500 (Internal Server Error)
				return StatusCode(500, $"Error interno del servidor {ex.Message}");
			}
			return Ok(regresa);
		}

		[HttpGet]
		[Route("ObtenerComboProgramasEjecutablesXAlumno")]
		public async Task<IActionResult> ObtenerComboProgramasEjecutablesXAlumno(string empresa, string idEmpresa, string idAlumno = "", string cualPrograma = "", string cadena = "")
		{
			List<DataPair2> regresa = new List<DataPair2>();
			try
			{
				_ = empresa;
				byte[] data = Convert.FromBase64String(cadena);
				cadena = Encoding.UTF8.GetString(data);

				using (SqlConnection connection = new SqlConnection(cadena))
				{
					await connection.OpenAsync();

					string query = @"
SELECT DISTINCT
    l.Nombre,
    l.id AS idLista,
    ISNULL(l.UsaActivos, 0) AS usaActivos,
    ISNULL(CONVERT(varchar(36), l.idTipoActivo), '') AS idTipoActivo,
    ISNULL(at.Nombre, '') AS tipoActivo
FROM Listas l
LEFT JOIN dbo.ActivosTipos at ON l.idTipoActivo = at.id AND at.idEmpresa = l.idEmpresa
WHERE l.idEmpresa = @IdEmpresa
  AND l.Estado = 2
  AND ISNULL(l.[Status], 0) = 1
  AND ISNULL(l.Activo, 0) = 1
  AND EXISTS (
      SELECT 1
      FROM ListasPreguntas lp
      WHERE lp.idLista = l.id
        AND ISNULL(lp.[Status], 0) = 1
  )
  AND (@Nombre = '' OR l.Nombre LIKE '%' + @Nombre + '%')
ORDER BY l.Nombre";

					using (SqlCommand command = new SqlCommand(query, connection))
					{
						command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
						command.Parameters.AddWithValue("@Nombre", cualPrograma ?? string.Empty);

						using (SqlDataReader reader = await command.ExecuteReaderAsync())
						{
							while (await reader.ReadAsync())
							{
								regresa.Add(new DataPair2()
								{
									name = reader["Nombre"] != DBNull.Value ? reader["Nombre"].ToString().Trim() : string.Empty,
									idLista = reader["idLista"] != DBNull.Value ? reader["idLista"].ToString() : string.Empty,
									usaActivos = reader["usaActivos"] != DBNull.Value && bool.Parse(reader["usaActivos"].ToString()),
									idTipoActivo = reader["idTipoActivo"] != DBNull.Value ? reader["idTipoActivo"].ToString() : string.Empty,
									tipoActivo = reader["tipoActivo"] != DBNull.Value ? reader["tipoActivo"].ToString().Trim() : string.Empty
								});
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error: {ex.Message}");
				return StatusCode(500, $"Error interno del servidor {ex.Message}");
			}

			return Ok(regresa);
		}

        [HttpPost]
        [Route("GuardarInspeccionBL26")]
        public async Task<IActionResult> GuardarInspeccionBL26([FromBody] GuardarInspeccionBl26Request request, string empresa, string cadena)
        {
            byte[] data;
            try
            {
                data = Convert.FromBase64String(cadena);
            }
            catch (FormatException ex)
            {
                return BadRequest($"Error en el formato de la cadena base64: {ex.Message}");
            }

            try
            {
                cadena = Encoding.UTF8.GetString(data);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error al convertir bytes a cadena: {ex.Message}");
            }

            if (request == null || request.idEmpresa == Guid.Empty || request.idLista == Guid.Empty || request.idSucursal == Guid.Empty || request.idUsuarioResponsable == Guid.Empty || request.idAlumno == Guid.Empty)
            {
                return BadRequest("La inspección no contiene el contexto mínimo requerido.");
            }

            if (request.respuestas == null || request.respuestas.Count == 0)
            {
                return BadRequest("La inspección no contiene respuestas para guardar.");
            }

            Guid? eventoLegacy = request.eventoLegacy.HasValue && request.eventoLegacy.Value != Guid.Empty
                ? request.eventoLegacy.Value
                : Guid.NewGuid();

            using (SqlConnection connection = new SqlConnection(cadena))
            {
                await connection.OpenAsync();
                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        ListaConfigBl26 lista = await ObtenerConfigListaBl26Async(connection, transaction, request.idLista);
                        if (lista == null)
                        {
                            transaction.Rollback();
                            return NotFound("La lista no existe.");
                        }

                        if (lista.idEmpresa != request.idEmpresa)
                        {
                            transaction.Rollback();
                            return BadRequest("La lista no corresponde a la empresa actual.");
                        }

                        if (!lista.Activo || !lista.Status || lista.Estado != 2)
                        {
                            transaction.Rollback();
                            return BadRequest("La lista no está disponible para inspección.");
                        }

                        Guid? idActivoCabecera = null;
                        if (lista.UsaActivos)
                        {
                            if (!request.idActivo.HasValue || request.idActivo.Value == Guid.Empty)
                            {
                                transaction.Rollback();
                                return BadRequest("La lista requiere un activo válido antes de iniciar la inspección.");
                            }

                            ActivoValidacionBl26 activo = await ObtenerActivoValidoBl26Async(connection, transaction, request.idActivo.Value);
                            if (activo == null)
                            {
                                transaction.Rollback();
                                return BadRequest("El activo seleccionado no existe.");
                            }

                            if (activo.idEmpresa != request.idEmpresa)
                            {
                                transaction.Rollback();
                                return BadRequest("El activo no pertenece a la empresa actual.");
                            }

                            if (!activo.Activo)
                            {
                                transaction.Rollback();
                                return BadRequest("El activo seleccionado está inactivo.");
                            }

                            if (lista.idTipoActivo.HasValue && activo.idTipoActivo != lista.idTipoActivo.Value)
                            {
                                transaction.Rollback();
                                return BadRequest("El activo seleccionado no corresponde al tipo configurado en la lista.");
                            }

                            idActivoCabecera = request.idActivo.Value;
                        }

                        Guid idInspeccion = await CrearCabeceraInspeccionBl26Async(connection, transaction, request, idActivoCabecera, eventoLegacy);
                        int respuestasGuardadas = 0;

                        foreach (GuardarInspeccionBl26RespuestaItem respuesta in request.respuestas)
                        {
                            Guid idListaRespuesta = await InsertarRespuestaBl26Async(
                                connection,
                                transaction,
                                request,
                                respuesta,
                                idInspeccion,
                                eventoLegacy);

                            await InsertarAnexosBl26Async(connection, transaction, idListaRespuesta, respuesta.urlVideos, 2);
                            await InsertarAnexosBl26Async(connection, transaction, idListaRespuesta, respuesta.urlFotos, 1);
                            respuestasGuardadas++;
                        }

                        transaction.Commit();

                        return Ok(new GuardarInspeccionBl26Response
                        {
                            idInspeccion = idInspeccion,
                            eventoLegacy = eventoLegacy,
                            respuestasGuardadas = respuestasGuardadas
                        });
                    }
                    catch (SqlException ex)
                    {
                        transaction.Rollback();
                        return StatusCode(500, $"Error en la base de datos: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return StatusCode(500, $"Error interno del servidor: {ex.Message}");
                    }
                }
            }
        }



		[HttpGet]
		[Route("Evaluacion/ObtenerPreguntasXResponder")]
		public async Task<IActionResult> ObtenerPreguntasXResponder(Guid idLista, Guid idPlantel, Guid idPrograma, Guid idCliente, string fechaInicia, string fechaFin, string empresa, string cadena)
		{
			try
			{
				List<PreguntasXResponder> BusquedaDeCursos = new List<PreguntasXResponder>();

				byte[] data = Convert.FromBase64String(cadena);

				// Si quieres convertir los bytes a una cadena original
				cadena = Encoding.UTF8.GetString(data);

				using (SqlConnection connection = new SqlConnection(cadena))
				{
					connection.Open();

				

					string query = $@"  SELECT  
     LP.idLista, 
     LP.id, 
     LP.pregunta, 
     LP.Explicacion, 
     LP.Tipo, 
     LP.valor, 
     LP.Obligatorio 
 FROM 
     ListasPreguntas LP 
 INNER JOIN 
     Listas L ON LP.idLista = L.id

WHERE 
     LP.idLista = @idLista
   AND
	 CAST(L.fechaCreacion AS DATE) BETWEEN '{fechaInicia}' AND '{fechaFin}';
    
;";

					using (SqlCommand command = new SqlCommand(query, connection))
					{
						// Agregar parámetros
						command.Parameters.AddWithValue("@idLista", idLista);
						command.Parameters.AddWithValue("@idPlantel", idPlantel);
						command.Parameters.AddWithValue("@idPrograma", idPrograma);
						command.Parameters.AddWithValue("@idCliente", idCliente);

						using (SqlDataReader reader = command.ExecuteReader())
						{
							while (reader.Read())
							{
								PreguntasXResponder BusquedaDeCurso = new PreguntasXResponder
								{
									id = reader["id"] != DBNull.Value ? Guid.Parse(reader["id"].ToString()) : Guid.Empty,
									idLista = reader["idLista"] != DBNull.Value ? Guid.Parse(reader["idLista"].ToString()) : Guid.Empty,
									pregunta = reader["pregunta"].ToString(),
									explicacion = reader["explicacion"].ToString(),
									tipo = reader["tipo"].ToString(),
									valor = reader["valor"].ToString(),
									obligatorio = reader["obligatorio"] != DBNull.Value ? bool.Parse(reader["obligatorio"].ToString()) : false,
								};
								BusquedaDeCursos.Add(BusquedaDeCurso);
							}
						}
					}
				}
				return Ok(BusquedaDeCursos);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error: {ex.Message}");
				// Retornar un código de error HTTP 500 (Internal Server Error)
				return StatusCode(500, $"Error interno del servidor {ex.Message}");
			}
		}




        [HttpGet]
        [Route("Evaluacion/ObtenerConsultaEvaluacion")]
        public async Task<IActionResult> ObtenerConsultaEvaluacion(string fechaInicia, string fechaFin,string empresa, string idSucursal = null, string idUsuario = null, string idLista = null, string cadena = "")
        {
            try
            {
                List<ConsultaEvaluacion> BusquedaDeCursos = new List<ConsultaEvaluacion>();

				byte[] data = Convert.FromBase64String(cadena);

				// Si quieres convertir los bytes a una cadena original
				cadena = Encoding.UTF8.GetString(data);

				using (SqlConnection connection = new SqlConnection(cadena))
				{
                    connection.Open();
                    //string query

                    string filtroSucursal = string.Empty;
                    string filtroUsuarios = string.Empty;
                    string compPlantel = string.Empty;
                    string compEspecialidad = string.Empty;

                    if (!string.IsNullOrEmpty(idSucursal) && Guid.TryParse(idSucursal, out Guid id))
                    {
                        filtroSucursal = $" AND dbo.ListasRespuestas.idsucursal = '{idSucursal}' ";
                    }

                    if (!string.IsNullOrEmpty(idUsuario) && Guid.TryParse(idUsuario, out Guid id2))
                    {
                        filtroSucursal = $" AND dbo.ListasRespuestas.idUsuario = '{idUsuario}' ";
                    }

                    /*if (!string.IsNullOrEmpty(idUsuario) && Guid.TryParse(idUsuario, out Guid usuarioGuid))
                    {
                        compEspecialidad = $" AND Usuarios.id = '{usuarioGuid}'";
                    }*/


                                        string query = $@"SELECT
	                    Listas.Nombre AS Lista,
	                    MIN(listas.fechacreacion) AS Periodo, -- Usar función de agregación
	                    'as' AS Instructor,
	                    CONVERT(DATE, ListasRespuestas.FechaRespuesta) AS Fecha,
	                    ListasRespuestas.evento,
	                    CONCAT(u.Nombre, ' ', u.ApellidoPaterno, ' ', u.ApellidoMaterno) AS 'nombreUsuario',
	                    s.nombre AS 'nombreSucursal',
	                    u.id AS 'idUsuario', ListasRespuestas.Latitud as latitud, ListasRespuestas.Longitud as longitud
                    FROM
	                    dbo.ListasRespuestas
	                    INNER JOIN dbo.Listas ON ListasRespuestas.idLista = Listas.id inner join usuarios u ON dbo.ListasRespuestas.idUsuario = u.id 
                    INNER JOIN sucursales s ON dbo.ListasRespuestas.idSucursal = s.id  
                    WHERE
	                    CAST(ListasRespuestas.Fecha AS DATE) BETWEEN '{fechaInicia}' AND '{fechaFin}' {filtroSucursal} {filtroUsuarios} AND listas.id = '{idLista}'  
                    GROUP BY
	                    Listas.Nombre,
	                    CONVERT(DATE, ListasRespuestas.FechaRespuesta),
	                    ListasRespuestas.evento,
	                    CONCAT(u.Nombre, ' ', u.ApellidoPaterno, ' ', u.ApellidoMaterno),
	                    s.nombre, u.id , ListasRespuestas.Latitud, ListasRespuestas.Longitud";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ConsultaEvaluacion BusquedaDeCurso = new ConsultaEvaluacion
                                {
                                    Evento = reader["Evento"] != DBNull.Value ? Guid.Parse(reader["Evento"].ToString()) : Guid.Empty,
                                    Lista = reader["Lista"].ToString(),
                                    Periodo = reader["Periodo"].ToString(),
                                    Instructor = reader["Instructor"].ToString(),
                                    Fecha = reader["Fecha"].ToString(),
                                    nombreSucursal = reader["nombreSucursal"].ToString(),
                                    nombreUsuario = reader["nombreUsuario"].ToString(),
                                    idUsuario = reader["idUsuario"].ToString(),
                                    latitud = reader["latitud"].ToString(),
                                    longitud = reader["longitud"].ToString()

                                };
                                BusquedaDeCursos.Add(BusquedaDeCurso);
                            }
                        }
                    }
                }
                return Ok(BusquedaDeCursos);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                // Retornar un código de error HTTP 500 (Internal Server Error)
                return StatusCode(500, $"Error interno del servidor {e.Message}");
            }
        }


        [HttpGet]
        [Route("Evaluacion/ObtenerListasReporte")]
        public async Task<IActionResult> ObtenerListasReporte(string fechaInicia, string fechaFin, string empresa, string idSucursal = null, string idUsuario = null, string idLista = null, string cadena = "", string idEmpresa = "")
        {
            try
            {
                List<ListaParaReporteListado> BusquedaDeCursos = new List<ListaParaReporteListado>();

				byte[] data = Convert.FromBase64String(cadena);

				// Si quieres convertir los bytes a una cadena original
				cadena = Encoding.UTF8.GetString(data);

				using (SqlConnection connection = new SqlConnection(cadena))
                {
                    connection.Open();
                    //string query

                    string filtroSucursal = string.Empty;
                    string filtroUsuarios = string.Empty;
                    string compPlantel = string.Empty;
                    string compEspecialidad = string.Empty;
                    string filtroLista = string.Empty;

                    if (!string.IsNullOrEmpty(idSucursal) && Guid.TryParse(idSucursal, out Guid id))
                    {
                        filtroSucursal = $" AND dbo.ListasRespuestas.idsucursal = '{idSucursal}' ";
                    }

                    if (!string.IsNullOrEmpty(idUsuario) && Guid.TryParse(idUsuario, out Guid id2))
                    {
                        filtroSucursal = $" AND dbo.ListasRespuestas.idUsuario = '{idUsuario}' ";
                    }

                    if (!string.IsNullOrEmpty(idLista) && Guid.TryParse(idLista, out Guid id3))
                    {
                        filtroLista = $" AND listas.id = '{idLista}' ";
                    }


                    

                    /*if (!string.IsNullOrEmpty(idUsuario) && Guid.TryParse(idUsuario, out Guid usuarioGuid))
                    {
                        compEspecialidad = $" AND Usuarios.id = '{usuarioGuid}'";
                    }*/


                    string query = $@"SELECT
	Listas.Nombre AS Evaluacion,
	MIN(listas.fechacreacion) AS Periodo, -- Usar función de agregación
	'as' AS Instructor,
	CONVERT(DATE, ListasRespuestas.FechaRespuesta) AS Fecha,
	ListasRespuestas.evento,
	CONCAT(u.Nombre, ' ', u.ApellidoPaterno, ' ', u.ApellidoMaterno) AS 'nombreUsuario',
	s.nombre AS 'nombreSucursal',
	u.id AS 'idUsuario' , dbo.Listas.id,  dbo.ListasRespuestas.idSucursal
FROM
	dbo.ListasRespuestas
	INNER JOIN dbo.Listas ON ListasRespuestas.idLista = Listas.id inner join usuarios u ON dbo.ListasRespuestas.idUsuario = u.id 
INNER JOIN sucursales s ON dbo.ListasRespuestas.idSucursal = s.id  
WHERE
	CAST(ListasRespuestas.Fecha AS DATE) BETWEEN '{fechaInicia}' AND '{fechaFin}' AND listas.idEmpresa = '{idEmpresa}' {filtroSucursal} {filtroUsuarios} {filtroLista} 
GROUP BY
	Listas.Nombre,
	CONVERT(DATE, ListasRespuestas.FechaRespuesta),
	ListasRespuestas.evento,
	CONCAT(u.Nombre, ' ', u.ApellidoPaterno, ' ', u.ApellidoMaterno),
	s.nombre, u.id , dbo.Listas.id,  dbo.ListasRespuestas.idSucursal";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ListaParaReporteListado BusquedaDeCurso = new ListaParaReporteListado
                                {
                                    Evento = reader["Evento"] != DBNull.Value ? Guid.Parse(reader["Evento"].ToString()) : Guid.Empty,
                                    Evaluacion = reader["Evaluacion"].ToString(),
                                    Periodo = reader["Periodo"].ToString(),
                                    Instructor = reader["Instructor"].ToString(),
                                    Fecha = reader["Fecha"].ToString(),
                                    nombreSucursal = reader["nombreSucursal"].ToString(),
                                    nombreUsuario = reader["nombreUsuario"].ToString(),
                                    idUsuario = reader["idUsuario"].ToString(),
                                    idLista = reader["id"] != DBNull.Value ? Guid.Parse(reader["id"].ToString()) : Guid.Empty,
                                    idSucursal = reader["idSucursal"] != DBNull.Value ? Guid.Parse(reader["idSucursal"].ToString()) : Guid.Empty,

                                };
                                BusquedaDeCursos.Add(BusquedaDeCurso);
                            }
                        }
                    }
                }
                return Ok(BusquedaDeCursos);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                // Retornar un código de error HTTP 500 (Internal Server Error)
                return StatusCode(500, $"Error interno del servidor {e.Message}");
            }
        }



        [HttpGet]
        [Route("ObtenerDetalleEvaluacion")]
        public async Task<IActionResult> ObtenerBusquedaDeCursos(string fechaInicia, string fechaFin,string empresaa,  string idLista = null, string cadena = "")
        {
            try
            {
				byte[] data = Convert.FromBase64String(cadena);

				// Si quieres convertir los bytes a una cadena original
				cadena = Encoding.UTF8.GetString(data);

				using (SqlConnection connection = new SqlConnection(cadena))
				{
                    connection.Open();


                    string compArea = string.Empty;
                    string compEspecialidad = string.Empty;

                    if (!string.IsNullOrEmpty(idLista) && Guid.TryParse(idLista, out Guid listaGuid))
                    {
                        compArea = $" AND Listas.id = '{listaGuid}'";
                    }
                  

                    string query = "SELECT Listas.Nombre AS [Evaluacion], " +
                        " ISNULL(Usuarios.Nombre, 'Sin usuario') AS Usuario, " +
                        " ListasPreguntas.Pregunta, ListasPreguntas.Explicacion, " +
                        " CASE WHEN ListasPreguntas.Tipo = 1 THEN 'Estrellas'" +
                        " WHEN ListasPreguntas.Tipo = 2 THEN 'Opción simple'" +
                        " WHEN ListasPreguntas.Tipo = 3 THEN 'Opción Múltiple'" +
                        " WHEN ListasPreguntas.Tipo = 4 THEN 'Texto libre'" +
                        " WHEN ListasPreguntas.Tipo = 5 THEN 'Númerico'" +
                        " WHEN ListasPreguntas.Tipo = 6 THEN 'Fecha'" +
                        " ELSE 'otro' END AS Tipo, ListasPreguntas.Valor,  " +
                        " CASE WHEN ListasPreguntas.Obligatorio = 0 THEN 'NO'  " +
                        " WHEN ListasPreguntas.Obligatorio = 1 THEN 'SI' ELSE 'otro' " +
                        " END AS Obligatorio, Listas.id, Listas.Status AS StatusLista                     " +
                        " FROM dbo.Listas LEFT JOIN dbo.Usuarios ON Listas.idusuario = Usuarios.id" +
                        " INNER JOIN dbo.ListasPreguntas ON Listas.id = ListasPreguntas.idLista" +
                        $" WHERE CAST(Listas.fechaCreacion AS DATE) BETWEEN '{fechaInicia}' AND '{fechaFin}' {compArea} {compEspecialidad}";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            List<DetalleEvaluacion> empresas = new List<DetalleEvaluacion>();

                            while (reader.Read())
                            {
                                DetalleEvaluacion empresa = new DetalleEvaluacion
                                {
                                    id = reader["Id"] != DBNull.Value ? Guid.Parse(reader["id"].ToString()) : Guid.Empty,
                                    Evaluacion = reader["Evaluacion"] != DBNull.Value ? reader["Evaluacion"].ToString() : string.Empty,
                                    Usuario = reader["Usuario"] != DBNull.Value ? (reader["Usuario"].ToString()) : string.Empty,                                 
                                    Pregunta = reader["Pregunta"] != DBNull.Value ? reader["Pregunta"].ToString() : string.Empty,
                                    Explicacion = reader["Explicacion"] != DBNull.Value ? reader["Explicacion"].ToString() : string.Empty,
                                    Tipo = reader["Tipo"] != DBNull.Value ? reader["Tipo"].ToString() : string.Empty,
                                    Valor = reader["Valor"] != DBNull.Value ? decimal.Parse(reader["Valor"].ToString()) : 0,
                                    Obligatorio = reader["Obligatorio"] != DBNull.Value ? (reader["Obligatorio"].ToString()) : string.Empty,
                                    StatusLista = reader["StatusLista"] != DBNull.Value ? bool.Parse(reader["StatusLista"].ToString()) : false,
                                   
                                };

                                empresas.Add(empresa);
                            }

                            return Ok(empresas);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                // Retornar un código de error HTTP 500 (Internal Server Error)
                return StatusCode(500, $"Error interno del servidor {e.Message}");
            }
        }



        [HttpGet]
        public async Task<IActionResult> ObtenerEmpresas(string empresaa, string cadena )
        {
            try
            {
				byte[] data = Convert.FromBase64String(cadena);

				// Si quieres convertir los bytes a una cadena original
				cadena = Encoding.UTF8.GetString(data);

				using (SqlConnection connection = new SqlConnection(cadena))
				{
                    connection.Open();

                    string query = "SELECT Id, idEmpresa, idusuario, nombre, Notas, Activo, [Status], estado FROM listas";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            List<Lista> empresas = new List<Lista>();

                            while (reader.Read())
                            {
                                Lista empresa = new Lista
                                {
                                    id = reader["Id"] != DBNull.Value ? Guid.Parse(reader["id"].ToString()) : Guid.Empty,
                                    idEmpresa = reader["idEmpresa"] != DBNull.Value ? Guid.Parse(reader["idEmpresa"].ToString()) : Guid.Empty,
                                    idusuario = reader["idusuario"] != DBNull.Value ? Guid.Parse(reader["idusuario"].ToString()) : Guid.Empty,
                                    Nombre = reader["nombre"] != DBNull.Value ? reader["nombre"].ToString() : string.Empty,
                                    Notas = reader["Notas"] != DBNull.Value ? reader["Notas"].ToString() : string.Empty,
                                    Activo = reader["Activo"] != DBNull.Value ? bool.Parse(reader["Activo"].ToString()) : false,
                                    Status = reader["Status"] != DBNull.Value ? bool.Parse(reader["Status"].ToString()) : false,
                                    Estado = reader["estado"] != DBNull.Value ? decimal.Parse(reader["estado"].ToString()) : 0
                            };

                                empresas.Add(empresa);
                            }

                            return Ok(empresas);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                // Retornar un código de error HTTP 500 (Internal Server Error)
                return StatusCode(500, $"Error interno del servidor {e.Message}");
            }
        }

        //[HttpPost]
        //public IActionResult InsertarLista([FromBody] Lista nuevaLista)
        //{
        //    try
        //    {
        //        using (SqlConnection connection = _connectionFactory.CreateConnection())
        //        {
        //            connection.Open();

        //            string query = "INSERT INTO listas ( idEmpresa,idInstructor, idPrograma, idusuario, nombre, Notas, Activo, [Status], estado) " +
        //                           "VALUES ( @IdEmpresa, newid(),newid(), @IdUsuario, @Nombre, @Notas, @Activo, @Status, @Estado)";

        //            using (SqlCommand command = new SqlCommand(query, connection))
        //            {

        //                command.Parameters.AddWithValue("@IdEmpresa", nuevaLista.idEmpresa);
        //                command.Parameters.AddWithValue("@IdUsuario", nuevaLista.idusuario);
        //                command.Parameters.AddWithValue("@Nombre", nuevaLista.Nombre);
        //                command.Parameters.AddWithValue("@Notas", nuevaLista.Notas);
        //                command.Parameters.AddWithValue("@Activo", nuevaLista.Activo);
        //                command.Parameters.AddWithValue("@Status", nuevaLista.Status);
        //                command.Parameters.AddWithValue("@Estado", nuevaLista.Estado);

        //                int rowsAffected = command.ExecuteNonQuery();
        //                if (rowsAffected > 0)
        //                {
        //                    return Ok("Lista insertada correctamente.");
        //                }
        //                else
        //                {
        //                    return StatusCode(500, "Error al insertar la lista.");
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine($"Error: {e.Message}");
        //        return StatusCode(500, $"Error interno del servidor {e.Message}");
        //    }
        //}

        [HttpPost]
        public async Task<IActionResult> Guardar(listasRespuestas datos, string evento, string empresa, string cadena)
        {
            byte[] data;
            try
            {
                data = Convert.FromBase64String(cadena);
            }
            catch (FormatException ex)
            {
                return BadRequest($"Error en el formato de la cadena base64: {ex.Message}");
            }

            try
            {
                cadena = Encoding.UTF8.GetString(data);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error al convertir bytes a cadena: {ex.Message}");
            }

            using (SqlConnection connection = new SqlConnection(cadena))
            {
                try
                {
                    await connection.OpenAsync();
                }
                catch (SqlException ex)
                {
                    return StatusCode(500, $"Error al abrir la conexión a la base de datos: {ex.Message}");
                }

                try
                {
                    // Generar un nuevo GUID para ListasRespuestas
                    Guid idListasRespuestas = Guid.NewGuid();

                    Guid? eventoLegacy = Guid.TryParse(evento, out Guid eventoGuid) ? eventoGuid : null;

                    // Insertar en la tabla ListasRespuestas
                    string sQueryListasRespuestas = @"
                INSERT INTO ListasRespuestas (
                    id, idEmpresa, idLista, idPregunta, RespuestaValor, Notas, idAlumno, 
                    idPrograma, idTipoPregunta, Explicacion, Valor, Calificacion, obligatoria, evento, valorCorrecto, idSucursal, idUsuario, latitud, longitud, stamp
                ) 
                VALUES (
                    @id, @idEmpresa, @idLista, @idPregunta, @RespuestaValor, @Notas, @idAlumno, 
                    @idPrograma, @idTipoPregunta, @Explicacion, @Valor, @Calificacion, @obligatoria, @evento, @respuestaCorrecta, @idSucursal, @idUsuario, @latitud, @longitud, @stamp 
                );";

                    using (SqlCommand commandListasRespuestas = new SqlCommand(sQueryListasRespuestas, connection))
                    {
                        commandListasRespuestas.Parameters.AddWithValue("@id", idListasRespuestas);
                        if (eventoLegacy.HasValue) commandListasRespuestas.Parameters.AddWithValue("@evento", eventoLegacy.Value); else commandListasRespuestas.Parameters.AddWithValue("@evento", DBNull.Value);
                        commandListasRespuestas.Parameters.AddWithValue("@idEmpresa", (object)datos.idEmpresa ?? DBNull.Value);
                        commandListasRespuestas.Parameters.AddWithValue("@idLista", (object)datos.idLista ?? DBNull.Value);
                        commandListasRespuestas.Parameters.AddWithValue("@idPregunta", (object)datos.idPregunta ?? DBNull.Value);
                        commandListasRespuestas.Parameters.AddWithValue("@RespuestaValor", (object)datos.RespuestaValor ?? DBNull.Value);
                        commandListasRespuestas.Parameters.AddWithValue("@Notas", (object)datos.Notas ?? DBNull.Value);
                        commandListasRespuestas.Parameters.AddWithValue("@idAlumno", (object)datos.idAlumno ?? DBNull.Value);
                        commandListasRespuestas.Parameters.AddWithValue("@idPrograma", (object)datos.idPrograma ?? DBNull.Value);
                        commandListasRespuestas.Parameters.AddWithValue("@idTipoPregunta", (object)datos.idTipoPregunta ?? DBNull.Value);
                        commandListasRespuestas.Parameters.AddWithValue("@Explicacion", (object)datos.Explicacion ?? DBNull.Value);
                        commandListasRespuestas.Parameters.AddWithValue("@Valor", (object)datos.Valor ?? DBNull.Value);
                        commandListasRespuestas.Parameters.AddWithValue("@Calificacion", (object)datos.Calificacion ?? DBNull.Value);
                        commandListasRespuestas.Parameters.AddWithValue("@obligatoria", (object)datos.obligatoria ?? DBNull.Value);
                        commandListasRespuestas.Parameters.AddWithValue("@respuestaCorrecta", (object)datos.RespuestaCorrecta ?? DBNull.Value);
                        commandListasRespuestas.Parameters.AddWithValue("@idSucursal", (object)datos.idSucursal ?? DBNull.Value);
                        commandListasRespuestas.Parameters.AddWithValue("@idUsuario", (object)datos.idUsuario ?? DBNull.Value);
                        commandListasRespuestas.Parameters.AddWithValue("@latitud", (object)datos.latitud ?? DBNull.Value);
                        commandListasRespuestas.Parameters.AddWithValue("@longitud", (object)datos.longitud ?? DBNull.Value);
                        commandListasRespuestas.Parameters.AddWithValue("@stamp", (object)datos.stamp ?? DBNull.Value);

                        await commandListasRespuestas.ExecuteNonQueryAsync();
                    }

                    // Insertar en la tabla AnexoPregunta para videos
                    string sQueryAnexoPregunta = @"
                INSERT INTO AnexoPregunta (id, url, tipo_anexo, fecha, idListaRespuesta)
                VALUES (@id, @url, @tipo_anexo, @fecha, @idListaRespuesta);";

                    foreach (var urlVideo in datos.urlVideos)
                    {
                        using (SqlCommand commandAnexoPregunta = new SqlCommand(sQueryAnexoPregunta, connection))
                        {
                            Guid idAnexoPregunta = Guid.NewGuid();
                            commandAnexoPregunta.Parameters.AddWithValue("@id", idAnexoPregunta);
                            commandAnexoPregunta.Parameters.AddWithValue("@url", urlVideo);
                            commandAnexoPregunta.Parameters.AddWithValue("@tipo_anexo", 2);
                            commandAnexoPregunta.Parameters.AddWithValue("@fecha", DateTime.Now);
                            commandAnexoPregunta.Parameters.AddWithValue("@idListaRespuesta", idListasRespuestas);

                            await commandAnexoPregunta.ExecuteNonQueryAsync();
                        }
                    }

                    // Insertar en la tabla AnexoPregunta para fotos
                    foreach (var urlFoto in datos.urlFotos)
                    {
                        using (SqlCommand commandAnexoPregunta = new SqlCommand(sQueryAnexoPregunta, connection))
                        {
                            Guid idAnexoPregunta = Guid.NewGuid();
                            commandAnexoPregunta.Parameters.AddWithValue("@id", idAnexoPregunta);
                            commandAnexoPregunta.Parameters.AddWithValue("@url", urlFoto);
                            commandAnexoPregunta.Parameters.AddWithValue("@tipo_anexo", 1);
                            commandAnexoPregunta.Parameters.AddWithValue("@fecha", DateTime.Now);
                            commandAnexoPregunta.Parameters.AddWithValue("@idListaRespuesta", idListasRespuestas);

                            await commandAnexoPregunta.ExecuteNonQueryAsync();
                        }
                    }

                    return Ok("Ok");
                }
                catch (SqlException ex)
                {
                    return StatusCode(500, $"Error en la base de datos: {ex.Message}");
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Error interno del servidor: {ex.Message}");
                }
            }
        }

        private sealed class ListaConfigBl26
        {
            public Guid idEmpresa { get; init; }
            public bool UsaActivos { get; init; }
            public Guid? idTipoActivo { get; init; }
            public bool Activo { get; init; }
            public bool Status { get; init; }
            public decimal Estado { get; init; }
        }

        private sealed class ActivoValidacionBl26
        {
            public Guid idEmpresa { get; init; }
            public Guid idTipoActivo { get; init; }
            public bool Activo { get; init; }
        }

        private static async Task<ListaConfigBl26?> ObtenerConfigListaBl26Async(SqlConnection connection, SqlTransaction transaction, Guid idLista)
        {
            const string sql = @"
SELECT TOP 1
    idEmpresa,
    ISNULL(UsaActivos, 0) AS UsaActivos,
    idTipoActivo,
    ISNULL(Activo, 1) AS Activo,
    ISNULL([Status], 1) AS [Status],
    ISNULL(Estado, 0) AS Estado
FROM dbo.Listas
WHERE id = @IdLista;";

            using SqlCommand command = new SqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("@IdLista", idLista);

            using SqlDataReader reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }

            return new ListaConfigBl26
            {
                idEmpresa = reader["idEmpresa"] != DBNull.Value ? Guid.Parse(reader["idEmpresa"].ToString()) : Guid.Empty,
                UsaActivos = reader["UsaActivos"] != DBNull.Value && bool.Parse(reader["UsaActivos"].ToString()),
                idTipoActivo = reader["idTipoActivo"] != DBNull.Value ? Guid.Parse(reader["idTipoActivo"].ToString()) : null,
                Activo = reader["Activo"] != DBNull.Value && bool.Parse(reader["Activo"].ToString()),
                Status = reader["Status"] != DBNull.Value && bool.Parse(reader["Status"].ToString()),
                Estado = reader["Estado"] != DBNull.Value ? Convert.ToDecimal(reader["Estado"]) : 0m
            };
        }

        private static async Task<ActivoValidacionBl26?> ObtenerActivoValidoBl26Async(SqlConnection connection, SqlTransaction transaction, Guid idActivo)
        {
            const string sql = @"
SELECT TOP 1
    idEmpresa,
    idTipoActivo,
    ISNULL(Activo, 0) AS Activo
FROM dbo.Activos
WHERE id = @IdActivo;";

            using SqlCommand command = new SqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("@IdActivo", idActivo);

            using SqlDataReader reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }

            return new ActivoValidacionBl26
            {
                idEmpresa = reader["idEmpresa"] != DBNull.Value ? Guid.Parse(reader["idEmpresa"].ToString()) : Guid.Empty,
                idTipoActivo = reader["idTipoActivo"] != DBNull.Value ? Guid.Parse(reader["idTipoActivo"].ToString()) : Guid.Empty,
                Activo = reader["Activo"] != DBNull.Value && bool.Parse(reader["Activo"].ToString())
            };
        }

        private static async Task<Guid> CrearCabeceraInspeccionBl26Async(
            SqlConnection connection,
            SqlTransaction transaction,
            GuardarInspeccionBl26Request request,
            Guid? idActivo,
            Guid? eventoLegacy)
        {
            const string sql = @"
INSERT INTO dbo.ListasInspecciones
(
    idEmpresa,
    idLista,
    idActivo,
    idProgramacion,
    eventoLegacy,
    idSucursal,
    idUsuarioResponsable,
    FechaInicio,
    FechaFin,
    Estado,
    FechaCreacion,
    FechaActualizacion
)
OUTPUT inserted.id
VALUES
(
    @idEmpresa,
    @idLista,
    @idActivo,
    @idProgramacion,
    @eventoLegacy,
    @idSucursal,
    @idUsuarioResponsable,
    SYSDATETIME(),
    SYSDATETIME(),
    2,
    SYSDATETIME(),
    SYSDATETIME()
);";

            using SqlCommand command = new SqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("@idEmpresa", request.idEmpresa);
            command.Parameters.AddWithValue("@idLista", request.idLista);
            if (idActivo.HasValue) command.Parameters.AddWithValue("@idActivo", idActivo.Value); else command.Parameters.AddWithValue("@idActivo", DBNull.Value);
            if (request.idProgramacion.HasValue) command.Parameters.AddWithValue("@idProgramacion", request.idProgramacion.Value); else command.Parameters.AddWithValue("@idProgramacion", DBNull.Value);
            if (eventoLegacy.HasValue) command.Parameters.AddWithValue("@eventoLegacy", eventoLegacy.Value); else command.Parameters.AddWithValue("@eventoLegacy", DBNull.Value);
            command.Parameters.AddWithValue("@idSucursal", request.idSucursal);
            command.Parameters.AddWithValue("@idUsuarioResponsable", request.idUsuarioResponsable);

            object result = await command.ExecuteScalarAsync();
            return result != null ? Guid.Parse(result.ToString()) : Guid.Empty;
        }

        private static async Task<Guid> InsertarRespuestaBl26Async(
            SqlConnection connection,
            SqlTransaction transaction,
            GuardarInspeccionBl26Request request,
            GuardarInspeccionBl26RespuestaItem respuesta,
            Guid idInspeccion,
            Guid? eventoLegacy)
        {
            Guid idListaRespuesta = Guid.NewGuid();
            const string sql = @"
INSERT INTO dbo.ListasRespuestas
(
    id,
    idEmpresa,
    idLista,
    idPregunta,
    RespuestaValor,
    Notas,
    idAlumno,
    idPrograma,
    idTipoPregunta,
    Explicacion,
    Valor,
    Calificacion,
    obligatoria,
    evento,
    valorCorrecto,
    idSucursal,
    idUsuario,
    idInspeccion,
    latitud,
    longitud,
    stamp
)
VALUES
(
    @id,
    @idEmpresa,
    @idLista,
    @idPregunta,
    @RespuestaValor,
    @Notas,
    @idAlumno,
    @idPrograma,
    @idTipoPregunta,
    @Explicacion,
    @Valor,
    @Calificacion,
    @obligatoria,
    @evento,
    @valorCorrecto,
    @idSucursal,
    @idUsuario,
    @idInspeccion,
    @latitud,
    @longitud,
    @stamp
);";

            using SqlCommand command = new SqlCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("@id", idListaRespuesta);
            command.Parameters.AddWithValue("@idEmpresa", request.idEmpresa);
            command.Parameters.AddWithValue("@idLista", request.idLista);
            command.Parameters.AddWithValue("@idPregunta", respuesta.idPregunta);
            command.Parameters.AddWithValue("@RespuestaValor", (object?)respuesta.RespuestaValor ?? DBNull.Value);
            command.Parameters.AddWithValue("@Notas", (object?)respuesta.Notas ?? DBNull.Value);
            command.Parameters.AddWithValue("@idAlumno", request.idAlumno);
            if (respuesta.idPrograma.HasValue) command.Parameters.AddWithValue("@idPrograma", respuesta.idPrograma.Value); else command.Parameters.AddWithValue("@idPrograma", DBNull.Value);
            if (respuesta.idTipoPregunta.HasValue) command.Parameters.AddWithValue("@idTipoPregunta", respuesta.idTipoPregunta.Value); else command.Parameters.AddWithValue("@idTipoPregunta", DBNull.Value);
            command.Parameters.AddWithValue("@Explicacion", (object?)respuesta.Explicacion ?? DBNull.Value);
            if (respuesta.Valor.HasValue) command.Parameters.AddWithValue("@Valor", respuesta.Valor.Value); else command.Parameters.AddWithValue("@Valor", DBNull.Value);
            if (respuesta.Calificacion.HasValue) command.Parameters.AddWithValue("@Calificacion", respuesta.Calificacion.Value); else command.Parameters.AddWithValue("@Calificacion", DBNull.Value);
            if (respuesta.obligatoria.HasValue) command.Parameters.AddWithValue("@obligatoria", respuesta.obligatoria.Value); else command.Parameters.AddWithValue("@obligatoria", DBNull.Value);
            if (eventoLegacy.HasValue) command.Parameters.AddWithValue("@evento", eventoLegacy.Value); else command.Parameters.AddWithValue("@evento", DBNull.Value);
            command.Parameters.AddWithValue("@valorCorrecto", (object?)respuesta.RespuestaCorrecta ?? DBNull.Value);
            command.Parameters.AddWithValue("@idSucursal", request.idSucursal);
            command.Parameters.AddWithValue("@idUsuario", request.idUsuarioResponsable);
            command.Parameters.AddWithValue("@idInspeccion", idInspeccion);
            command.Parameters.AddWithValue("@latitud", (object?)respuesta.latitud ?? DBNull.Value);
            command.Parameters.AddWithValue("@longitud", (object?)respuesta.longitud ?? DBNull.Value);
            command.Parameters.AddWithValue("@stamp", string.IsNullOrWhiteSpace(respuesta.stamp) ? DBNull.Value : respuesta.stamp);

            await command.ExecuteNonQueryAsync();
            return idListaRespuesta;
        }

        private static async Task InsertarAnexosBl26Async(SqlConnection connection, SqlTransaction transaction, Guid idListaRespuesta, List<string> urls, int tipoAnexo)
        {
            if (urls == null || urls.Count == 0)
            {
                return;
            }

            const string sql = @"
INSERT INTO dbo.AnexoPregunta (id, url, tipo_anexo, fecha, idListaRespuesta)
VALUES (@id, @url, @tipo_anexo, @fecha, @idListaRespuesta);";

            foreach (string url in urls.Where(item => !string.IsNullOrWhiteSpace(item)))
            {
                using SqlCommand command = new SqlCommand(sql, connection, transaction);
                command.Parameters.AddWithValue("@id", Guid.NewGuid());
                command.Parameters.AddWithValue("@url", url);
                command.Parameters.AddWithValue("@tipo_anexo", tipoAnexo);
                command.Parameters.AddWithValue("@fecha", DateTime.Now);
                command.Parameters.AddWithValue("@idListaRespuesta", idListaRespuesta);
                await command.ExecuteNonQueryAsync();
            }
        }









        [HttpPut]
        public async Task<IActionResult> ActualizarLista([FromBody] Lista listaActualizada, string empresa, string cadena)
        {
            try
            {
				byte[] data = Convert.FromBase64String(cadena);

				// Si quieres convertir los bytes a una cadena original
				cadena = Encoding.UTF8.GetString(data);

				using (SqlConnection connection = new SqlConnection(cadena))
				{
                    connection.Open();

                    string query = "UPDATE listas SET idEmpresa = @IdEmpresa, idusuario = @IdUsuario, nombre = @Nombre, " +
                                   "Notas = @Notas, Activo = @Activo, [Status] = @Status, estado = @Estado " +
                                   "WHERE Id = @Id";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", listaActualizada.id);
                        command.Parameters.AddWithValue("@IdEmpresa", listaActualizada.idEmpresa);
                        command.Parameters.AddWithValue("@IdUsuario", listaActualizada.idusuario);
                        command.Parameters.AddWithValue("@Nombre", listaActualizada.Nombre);
                        command.Parameters.AddWithValue("@Notas", listaActualizada.Notas);
                        command.Parameters.AddWithValue("@Activo", listaActualizada.Activo);
                        command.Parameters.AddWithValue("@Status", listaActualizada.Status);
                        command.Parameters.AddWithValue("@Estado", listaActualizada.Estado);

                        int rowsAffected = command.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            return Ok("Lista actualizada correctamente.");
                        }
                        else
                        {
                            return StatusCode(500, "Error al actualizar la lista.");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return StatusCode(500, $"Error interno del servidor {e.Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarLista(Guid id, string empresa, string cadena)
        {
            try
            {
				byte[] data = Convert.FromBase64String(cadena);

				// Si quieres convertir los bytes a una cadena original
				cadena = Encoding.UTF8.GetString(data);

				using (SqlConnection connection = new SqlConnection(cadena))
				{
                    connection.Open();

                    string query = "DELETE FROM listas WHERE Id = @Id";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);

                        int rowsAffected = command.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            return Ok("Lista eliminada correctamente.");
                        }
                        else
                        {
                            return StatusCode(404, "Lista no encontrada.");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return StatusCode(500, $"Error interno del servidor {e.Message}");
            }
        }

    }
}
