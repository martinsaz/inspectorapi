using checklistWs.Models.Opciones;
using checklistWs.Utiles;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Text;

namespace checklistWs.Controllers.OpcionesPreguntas
{
    public class PreguntasController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly SqlConnectionFactory _connectionFactory;

        public PreguntasController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionFactory = new SqlConnectionFactory(configuration);
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [Route("ListasPreguntasOpciones/GetElemento")]
        public async Task<IActionResult> GetElemento(Guid id, string empresa, string cadena)
        {
            try
            {
              
                List<ListasPreguntasOpciones> regresa = new List<ListasPreguntasOpciones>();
				byte[] data = Convert.FromBase64String(cadena);

				// Si quieres convertir los bytes a una cadena original
				cadena = Encoding.UTF8.GetString(data);

				using (SqlConnection connection = new SqlConnection(cadena))
				{
                    connection.Open();
                    string sQuery = string.Format("SELECT lpo.id, lpo.idEmpresa, lpo.idLista, lpo.opcion, lpo.idPregunta, l.Nombre as Lista, lp.Pregunta FROM ListasPreguntasOpciones lpo LEFT JOIN Listas l on lpo.idLista = l.id LEFT JOIN ListasPreguntas lp on lpo.idPregunta = lp.id  where lpo.id = '{0}'", id);
                    using (SqlCommand command = new SqlCommand(sQuery, connection))
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (reader.Read())
                            {
                                ListasPreguntasOpciones item = new ListasPreguntasOpciones();
                                item.id = reader["id"] != DBNull.Value ? Guid.Parse(reader["id"].ToString()) : Guid.Empty;
                                item.idEmpresa = reader["idEmpresa"] != DBNull.Value ? Guid.Parse(reader["idEmpresa"].ToString()) : Guid.Empty;
                                item.idLista = reader["idLista"] != DBNull.Value ? Guid.Parse(reader["idLista"].ToString()) : Guid.Empty;
                                item.opcion = reader["opcion"] != DBNull.Value ? reader["opcion"].ToString().Trim() : string.Empty;
                                item.idPregunta = reader["idPregunta"] != DBNull.Value ? Guid.Parse(reader["idPregunta"].ToString()) : Guid.Empty;
                                item.Lista = reader["Lista"] != DBNull.Value ? reader["Lista"].ToString().Trim() : string.Empty;
                                item.Pregunta = reader["Pregunta"] != DBNull.Value ? reader["Pregunta"].ToString().Trim() : string.Empty;


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
        [Route("ListasPreguntasOpciones/GetLista")]
        public async Task<IActionResult> GetLista(Guid idLista, string empresa, string cadena, string cualPrograma = "")
        {
            try
            {
               
                List<ListasPreguntasOpciones> regresa = new List<ListasPreguntasOpciones>();
				byte[] data = Convert.FromBase64String(cadena);

				// Si quieres convertir los bytes a una cadena original
				cadena = Encoding.UTF8.GetString(data);

				using (SqlConnection connection = new SqlConnection(cadena))
				{
                    connection.Open();
					string sComp = string.Empty;
					if (!string.IsNullOrEmpty(cualPrograma))
					{
						sComp = string.Format(" AND lp.Pregunta LIKE '%{0}%'", cualPrograma);
					}
					string sQuery = string.Format("SELECT lpo.id, lp.idEmpresa, lp.idLista, lpo.opcion, lpo.idPregunta, l.Nombre as Lista, lp.Pregunta \r\nFROM listas l left JOIN ListasPreguntas lp on lp.idLista = l.id left JOIN ListasPreguntasOpciones lpo  \r\non lpo.idPregunta = lp.id  where lp.idLista = '{0}' {1}", idLista, sComp);
                    using (SqlCommand command = new SqlCommand(sQuery, connection))
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (reader.Read())
                            {
                                ListasPreguntasOpciones item = new ListasPreguntasOpciones();
                                item.id = reader["id"] != DBNull.Value ? Guid.Parse(reader["id"].ToString()) : Guid.Empty;
                                item.idEmpresa = reader["idEmpresa"] != DBNull.Value ? Guid.Parse(reader["idEmpresa"].ToString()) : Guid.Empty;
                                item.idLista = reader["idLista"] != DBNull.Value ? Guid.Parse(reader["idLista"].ToString()) : Guid.Empty;
                                item.opcion = reader["opcion"] != DBNull.Value ? reader["opcion"].ToString().Trim() : string.Empty;
                                item.idPregunta = reader["idPregunta"] != DBNull.Value ? Guid.Parse(reader["idPregunta"].ToString()) : Guid.Empty;
                                item.Lista = reader["Lista"] != DBNull.Value ? reader["Lista"].ToString().Trim() : string.Empty;
                                item.Pregunta = reader["Pregunta"] != DBNull.Value ? reader["Pregunta"].ToString().Trim() : string.Empty;


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
        [Route("ListasPreguntasOpciones/GetPregunta")]
        public async Task<IActionResult> GetPregunta(Guid idPregunta, string empresa, string cadena, string tipoPregunta)
        {
            try
            {
               
                List<ListasPreguntasOpciones> regresa = new List<ListasPreguntasOpciones>();
				byte[] data = Convert.FromBase64String(cadena);

				// Si quieres convertir los bytes a una cadena original
				cadena = Encoding.UTF8.GetString(data);

				using (SqlConnection connection = new SqlConnection(cadena))
				{
                    connection.Open();
                    string sQuery = string.Format("SELECT lpo.id, lpo.idEmpresa, lpo.idLista, lpo.opcion, lpo.idPregunta, l.Nombre as Lista, lp.Pregunta FROM ListasPreguntasOpciones lpo LEFT JOIN Listas l on lpo.idLista = l.id LEFT JOIN ListasPreguntas lp on lpo.idPregunta = lp.id  where lpo.idPregunta = '{0}' and lp.Tipo = {1}", idPregunta, tipoPregunta);
                    using (SqlCommand command = new SqlCommand(sQuery, connection))
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (reader.Read())
                            {
                                ListasPreguntasOpciones item = new ListasPreguntasOpciones();
                                item.id = reader["id"] != DBNull.Value ? Guid.Parse(reader["id"].ToString()) : Guid.Empty;
                                item.idEmpresa = reader["idEmpresa"] != DBNull.Value ? Guid.Parse(reader["idEmpresa"].ToString()) : Guid.Empty;
                                item.idLista = reader["idLista"] != DBNull.Value ? Guid.Parse(reader["idLista"].ToString()) : Guid.Empty;
                                item.opcion = reader["opcion"] != DBNull.Value ? reader["opcion"].ToString().Trim() : string.Empty;
                                item.idPregunta = reader["idPregunta"] != DBNull.Value ? Guid.Parse(reader["idPregunta"].ToString()) : Guid.Empty;
                                item.Lista = reader["Lista"] != DBNull.Value ? reader["Lista"].ToString().Trim() : string.Empty;
                                item.Pregunta = reader["Pregunta"] != DBNull.Value ? reader["Pregunta"].ToString().Trim() : string.Empty;


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
        [Route("ListasPreguntasOpciones/GetTodos")]
        public async Task<IActionResult> GetTodos(Guid idEmpresa, string empresa, string cadena)
        {
            try
            {
                
                List<ListasPreguntasOpciones> regresa = new List<ListasPreguntasOpciones>();
				byte[] data = Convert.FromBase64String(cadena);

				// Si quieres convertir los bytes a una cadena original
				cadena = Encoding.UTF8.GetString(data);

				using (SqlConnection connection = new SqlConnection(cadena))
				{
                    connection.Open();
                    string sQuery = string.Format("SELECT lpo.id, lpo.idEmpresa, lpo.idLista, lpo.opcion, lpo.idPregunta, l.Nombre as Lista, lp.Pregunta FROM ListasPreguntasOpciones lpo LEFT JOIN Listas l on lpo.idLista = l.id LEFT JOIN ListasPreguntas lp on lpo.idPregunta = lp.id where lpo.idEmpresa = '{0}'", idEmpresa);
                    using (SqlCommand command = new SqlCommand(sQuery, connection))
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (reader.Read())
                            {
                                ListasPreguntasOpciones item = new ListasPreguntasOpciones();
                                item.id = reader["id"] != DBNull.Value ? Guid.Parse(reader["id"].ToString()) : Guid.Empty;
                                item.idEmpresa = reader["idEmpresa"] != DBNull.Value ? Guid.Parse(reader["idEmpresa"].ToString()) : Guid.Empty;
                                item.idLista = reader["idLista"] != DBNull.Value ? Guid.Parse(reader["idLista"].ToString()) : Guid.Empty;
                                item.opcion = reader["opcion"] != DBNull.Value ? reader["opcion"].ToString().Trim() : string.Empty;
                                item.idPregunta = reader["idPregunta"] != DBNull.Value ? Guid.Parse(reader["idPregunta"].ToString()) : Guid.Empty;
                                item.Lista = reader["Lista"] != DBNull.Value ? reader["Lista"].ToString().Trim() : string.Empty;
                                item.Pregunta = reader["Pregunta"] != DBNull.Value ? reader["Pregunta"].ToString().Trim() : string.Empty;
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


        [HttpPost]
        [Route("ListasPreguntasOpciones/Guardar")]
        public async Task<IActionResult> Guardar([FromBody] ListasPreguntasOpciones datos, string empresa, string cadena)
        {
            try
            {
                byte[] data = Convert.FromBase64String(cadena);

                // Convertir los bytes a la cadena original
                cadena = Encoding.UTF8.GetString(data);

                using (SqlConnection connection = new SqlConnection(cadena))
                {
                    connection.Open();
                    string sQuery = string.Empty;
                    Guid insertado = Guid.NewGuid();

                    // Consulta para ListasPreguntasOpciones
                    if (await Existe((Guid)datos.idPregunta, empresa, cadena, datos.opcion))
                    {
                        sQuery = string.Format("INSERT INTO ListasPreguntasOpciones (id, idEmpresa, idLista, opcion, idPregunta) VALUES ('{0}',@idEmpresa, @idLista, @opcion, @idPregunta)", insertado);
                    }
                    else
                    {
                        return Ok("Ya existe el elemento");
                    }

                    using (SqlCommand command = new SqlCommand(sQuery, connection))
                    {
                        // Parámetros para ListasPreguntasOpciones
                        if (datos.idEmpresa != null) command.Parameters.AddWithValue("@idEmpresa", datos.idEmpresa); else command.Parameters.AddWithValue("@idEmpresa", DBNull.Value);
                        if (datos.idLista != null) command.Parameters.AddWithValue("@idLista", datos.idLista); else command.Parameters.AddWithValue("@idLista", DBNull.Value);
                        if (datos.opcion != null) command.Parameters.AddWithValue("@opcion", datos.opcion); else command.Parameters.AddWithValue("@opcion", DBNull.Value);
                        if (datos.idPregunta != null) command.Parameters.AddWithValue("@idPregunta", datos.idPregunta); else command.Parameters.AddWithValue("@idPregunta", DBNull.Value);

                        await command.ExecuteNonQueryAsync();
                    }

                    // Nueva consulta para actualizar ListasPreguntas
                    string sQueryUpdateListasPreguntas = "UPDATE ListasPreguntas SET tipo = @tipo WHERE id = @idPregunta";
                    using (SqlCommand commandUpdate = new SqlCommand(sQueryUpdateListasPreguntas, connection))
                    {
                        commandUpdate.Parameters.AddWithValue("@tipo", datos.tipoPregunta);
                        commandUpdate.Parameters.AddWithValue("@idPregunta", datos.idPregunta);

                        await commandUpdate.ExecuteNonQueryAsync();
                    }
                }
                return Ok("Ok");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, $"Error interno del servidor {ex.Message}");
            }
        }



        [HttpPost]
        [Route("ListasPreguntasOpciones/Eliminar")]
        public async Task<IActionResult> Eliminar(string id, string empresa, string cadena)
        {
            try
            {
                byte[] data = Convert.FromBase64String(cadena);
                cadena = Encoding.UTF8.GetString(data);

                using (SqlConnection connection = new SqlConnection(cadena))
                {
                    // Abrir la conexión aquí puede no ser necesario ya que el `SqlCommand` abrirá la conexión si no está abierta
                    await connection.OpenAsync();
                    string sQuery = "DELETE FROM ListasPreguntasOpciones WHERE id = @Id";

                    using (SqlCommand command = new SqlCommand(sQuery, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);

                        await command.ExecuteNonQueryAsync();
                    }
                }
                return Ok("Ok");
            }
            catch (Exception ex)
            {
                // Es mejor usar logging en lugar de Console.WriteLine para manejar los errores en un ambiente de producción
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }



        /*  [HttpDelete]
          [Route("ListasPreguntasOpciones/Borrar")]
          public async Task<IHttpActionResult> Borrar(Guid id)
          {
              try
              {
                  string cadena = SqlConnectionFactory.ObtenerCadenaConexion();
                  using (SqlConnection connection = new SqlConnection(cadena))
                  {
                      connection.Open();
                      string sQuery = $@"UPDATE ListasPreguntasOpciones SET Status = '0' WHERE id = '{id}'";
                      using (SqlCommand command = new SqlCommand(sQuery, connection))
                      {
                          await command.ExecuteNonQueryAsync();
                      }
                  }
                  return Ok("Ok");
              }
              catch (Exception ex)
              {
                  return InternalServerError(ex);
              }
          }*/

        //Utilerias

        private async Task<bool> Existe(Guid cualId, string empresa, string cadena, string opcion)
        {
            bool regresa = false;
            if (cualId != Guid.Empty)
            {
                try
                {

					

					using (SqlConnection connection = new SqlConnection(cadena))
					{
                        connection.Open();
                        string sQuery = string.Format("SELECT COUNT(*) FROM ListasPreguntasOpciones WHERE idPregunta = '{0}' and opcion = '{1}'", cualId, opcion);
                        using (SqlCommand command = new SqlCommand(sQuery, connection))
                        {
                            using (SqlDataReader reader = await command.ExecuteReaderAsync())
                            {
                                if (reader.HasRows)
                                {
                                    reader.Read();
                                    if (Convert.ToInt32(reader[0]) == 0) regresa = true;
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
