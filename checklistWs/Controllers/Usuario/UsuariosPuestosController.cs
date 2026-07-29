using checklistWs.Models.Combo;
using checklistWs.Models.Usuario;
using checklistWs.Utiles;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Text;

namespace checklistWs.Controllers.Usuario
{
    public class UsuariosPuestosController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        private readonly IConfiguration _configuration;
        private readonly SqlConnectionFactory _connectionFactory;

        public UsuariosPuestosController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionFactory = new SqlConnectionFactory(configuration);
        }

        [HttpGet]
        [Route("GetComboPuestos")]
        public async Task<IActionResult> GetComboPuestos(Guid idEmpresa, string empresa, string cadena, string nombre = null)
        {
            try
            {
                List<DataPair2> puestos = new List<DataPair2>();

                byte[] data = Convert.FromBase64String(cadena);

                // Si quieres convertir los bytes a una cadena original
                cadena = Encoding.UTF8.GetString(data);

                using (SqlConnection connection = new SqlConnection(cadena))
                {
                    string sComp = string.Empty;
                    if (nombre != null)
                    {
                        sComp = $"AND Nombre LIKE '%{nombre}%'";
                    }
                    string sQuery = $"SELECT id, Nombre FROM [UsuariosPuestos] WHERE idEmpresa = '{idEmpresa}' {sComp} ORDER BY Nombre";
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(sQuery, connection))
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (reader.Read())
                            {
                                puestos.Add(new DataPair2()
                                {
                                    value = reader["id"] != DBNull.Value ? reader["id"].ToString() : string.Empty,
                                    name = reader["Nombre"] != DBNull.Value ? reader["Nombre"].ToString().Trim() : string.Empty
                                });
                            }
                        }
                    }
                    return Ok(puestos);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                // Retornar un código de error HTTP 500 (Internal Server Error)
                return StatusCode(500, $"Error interno del servidor {ex.Message}");
            }
        }

        // Crear
        [HttpPost("InsertarPuesto")]
        public async Task<IActionResult> InsertarPuesto([FromBody] UsuariosPuestos nuevoPuesto, string empresa, string cadena)
        {
            try
            {
                string query = @"INSERT INTO UsuariosPuestos ( Nombre, notas, fecha, borrado, idEmpresa)
                                 VALUES ( @Nombre, @notas, @fecha, @borrado, @idEmpresa)";

				byte[] data = Convert.FromBase64String(cadena);

				// Si quieres convertir los bytes a una cadena original
				cadena = Encoding.UTF8.GetString(data);

				using (SqlConnection connection = new SqlConnection(cadena))
				{
                    await connection.OpenAsync();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                      
                        command.Parameters.AddWithValue("@Nombre", nuevoPuesto.Nombre);
                        command.Parameters.AddWithValue("@notas", nuevoPuesto.notas);
                        command.Parameters.AddWithValue("@fecha", nuevoPuesto.fecha);
                        command.Parameters.AddWithValue("@borrado", nuevoPuesto.borrado);
                        command.Parameters.AddWithValue("@idEmpresa", nuevoPuesto.idEmpresa);

                        await command.ExecuteNonQueryAsync();
                    }
                }

                return Ok("Puesto insertado con éxito.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return StatusCode(500, $"Error interno del servidor: {e.Message}");
            }
        }

		[HttpPost("InsertarPrimerPuesto")]
		public async Task<IActionResult> InsertarPrimerPuesto([FromBody] UsuariosPuestos nuevoPuesto, string empresa, string cadena)
		{
			try
			{
				string query = @"INSERT INTO UsuariosPuestos (id, Nombre, notas, fecha, borrado, idEmpresa)
                                 VALUES (@Id, @Nombre, @notas, @fecha, @borrado, @idEmpresa)";

				byte[] data = Convert.FromBase64String(cadena);

				// Si quieres convertir los bytes a una cadena original
				cadena = Encoding.UTF8.GetString(data);

				using (SqlConnection connection = new SqlConnection(cadena))
				{
					await connection.OpenAsync();

					using (SqlCommand command = new SqlCommand(query, connection))
					{
						command.Parameters.AddWithValue("@Id", nuevoPuesto.id);
						command.Parameters.AddWithValue("@Nombre", nuevoPuesto.Nombre);
						command.Parameters.AddWithValue("@notas", nuevoPuesto.notas);
						command.Parameters.AddWithValue("@fecha", nuevoPuesto.fecha);
						command.Parameters.AddWithValue("@borrado", nuevoPuesto.borrado);
						command.Parameters.AddWithValue("@idEmpresa", nuevoPuesto.idEmpresa);

						await command.ExecuteNonQueryAsync();
					}
				}

				return Ok("Puesto insertado con éxito.");
			}
			catch (Exception e)
			{
				Console.WriteLine($"Error: {e.Message}");
				return StatusCode(500, $"Error interno del servidor: {e.Message}");
			}
		}

		// Leer (Obtener uno)
		[HttpGet("ObtenerPuesto")]
        public async Task<IActionResult> ObtenerPuesto(Guid id, string empresa, string cadena)
        {
            try
            {
                string query = @"SELECT id, Nombre, notas, fecha, borrado, idEmpresa 
                                 FROM UsuariosPuestos 
                                 WHERE id = @id";


				byte[] data = Convert.FromBase64String(cadena);

				// Si quieres convertir los bytes a una cadena original
				cadena = Encoding.UTF8.GetString(data);


				using (SqlConnection connection = new SqlConnection(cadena))
				{
                    await connection.OpenAsync();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            var puestos = new List<UsuariosPuestos>();
                            while (await reader.ReadAsync())
                            {
                                var puesto = new UsuariosPuestos
                                {
                                    id = reader.GetGuid(reader.GetOrdinal("id")),
                                    Nombre = reader.GetString(reader.GetOrdinal("Nombre")),
                                    notas = reader["notas"] != DBNull.Value ? reader["notas"].ToString() ?? string.Empty : string.Empty,
                                    fecha = reader.IsDBNull(reader.GetOrdinal("fecha")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("fecha")),
                                    borrado = reader.IsDBNull(reader.GetOrdinal("borrado")) ? (bool?)null : reader.GetBoolean(reader.GetOrdinal("borrado")),
                                    idEmpresa = reader.IsDBNull(reader.GetOrdinal("idEmpresa")) ? (Guid?)null : reader.GetGuid(reader.GetOrdinal("idEmpresa"))
                                };
                                puestos.Add(puesto);
                             
                            }
                            return Ok(puestos);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return StatusCode(500, $"Error interno del servidor: {e.Message}");
            }
        }

		[HttpGet("ObtenerPrimerPuesto")]
		public async Task<IActionResult> ObtenerPrimerPuesto(string nombre, string empresa, string cadena, Guid? idEmpresa = null)
		{
			try
			{
				string query = @"SELECT id, Nombre, notas, fecha, borrado, idEmpresa 
	                                 FROM UsuariosPuestos 
	                                 WHERE nombre = @Nombre";

				if (idEmpresa.HasValue && idEmpresa.Value != Guid.Empty)
				{
					query += " AND idEmpresa = @IdEmpresa";
				}


				byte[] data = Convert.FromBase64String(cadena);

				// Si quieres convertir los bytes a una cadena original
				cadena = Encoding.UTF8.GetString(data);


				using (SqlConnection connection = new SqlConnection(cadena))
				{
					await connection.OpenAsync();

						using (SqlCommand command = new SqlCommand(query, connection))
						{
							command.Parameters.AddWithValue("@Nombre", nombre);
							if (idEmpresa.HasValue && idEmpresa.Value != Guid.Empty)
							{
								command.Parameters.AddWithValue("@IdEmpresa", idEmpresa.Value);
							}

						using (SqlDataReader reader = await command.ExecuteReaderAsync())
						{
							var puestos = new List<UsuariosPuestos>();
							while (await reader.ReadAsync())
							{
								var puesto = new UsuariosPuestos
								{
									id = reader.GetGuid(reader.GetOrdinal("id")),
									Nombre = reader.GetString(reader.GetOrdinal("Nombre")),
										notas = reader["notas"] != DBNull.Value ? reader["notas"].ToString() ?? string.Empty : string.Empty,
									fecha = reader.IsDBNull(reader.GetOrdinal("fecha")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("fecha")),
									borrado = reader.IsDBNull(reader.GetOrdinal("borrado")) ? (bool?)null : reader.GetBoolean(reader.GetOrdinal("borrado")),
									idEmpresa = reader.IsDBNull(reader.GetOrdinal("idEmpresa")) ? (Guid?)null : reader.GetGuid(reader.GetOrdinal("idEmpresa"))
								};
								puestos.Add(puesto);

							}
							return Ok(puestos);
						}
					}
				}
			}
			catch (Exception e)
			{
				Console.WriteLine($"Error: {e.Message}");
				return StatusCode(500, $"Error interno del servidor: {e.Message}");
			}
		}

		// Leer (Obtener todos)
		[HttpGet("ObtenerPuestos")]
        public async Task<IActionResult> ObtenerPuestos(string empresa, string idEmpresa, string cadena)
        {
            try
            {
                string query = @"SELECT id, Nombre, notas, fecha, borrado, idEmpresa 
                                 FROM UsuariosPuestos where idEmpresa = '"+idEmpresa+"' ";

			

				byte[] data = Convert.FromBase64String(cadena);

				// Si quieres convertir los bytes a una cadena original
				cadena = Encoding.UTF8.GetString(data);


				using (SqlConnection connection = new SqlConnection(cadena))
				{
                    await connection.OpenAsync();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            var puestos = new List<UsuariosPuestos>();

                            while (await reader.ReadAsync())
                            {
                                var puesto = new UsuariosPuestos
                                {
                                    id = reader.GetGuid(reader.GetOrdinal("id")),
                                    Nombre = reader.GetString(reader.GetOrdinal("Nombre")),
                                    notas = reader["notas"] != DBNull.Value ? reader["notas"].ToString() ?? string.Empty : string.Empty,
                                    fecha = reader.IsDBNull(reader.GetOrdinal("fecha")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("fecha")),
                                    borrado = reader.IsDBNull(reader.GetOrdinal("borrado")) ? (bool?)null : reader.GetBoolean(reader.GetOrdinal("borrado")),
                                    idEmpresa = reader.IsDBNull(reader.GetOrdinal("idEmpresa")) ? (Guid?)null : reader.GetGuid(reader.GetOrdinal("idEmpresa"))
                                };

                                puestos.Add(puesto);
                            }

                            return Ok(puestos);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return StatusCode(500, $"Error interno del servidor: {e.Message}");
            }
        }

        // Actualizar
        [HttpPut("ActualizarPuesto")]
        public async Task<IActionResult> ActualizarPuesto(Guid id, [FromBody] UsuariosPuestos puesto, string empresa, string cadena)
        {
            if (id != puesto.id)
            {
                return BadRequest("El ID del puesto no coincide.");
            }

            try
            {
                string query = @"UPDATE UsuariosPuestos 
                                 SET Nombre = @Nombre, 
                                     notas = @notas, 
                                     fecha = @fecha, 
                                     borrado = @borrado, 
                                     idEmpresa = @idEmpresa 
                                 WHERE id = @id";


				byte[] data = Convert.FromBase64String(cadena);

				// Si quieres convertir los bytes a una cadena original
				cadena = Encoding.UTF8.GetString(data);



				using (SqlConnection connection = new SqlConnection(cadena))
				{
                    await connection.OpenAsync();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", puesto.id);
                        command.Parameters.AddWithValue("@Nombre", puesto.Nombre);
                        command.Parameters.AddWithValue("@notas", puesto.notas);
                        command.Parameters.AddWithValue("@fecha", puesto.fecha);
                        command.Parameters.AddWithValue("@borrado", puesto.borrado);
                        command.Parameters.AddWithValue("@idEmpresa", puesto.idEmpresa);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        if (rowsAffected == 0)
                        {
                            return NotFound("Puesto no encontrado.");
                        }
                    }
                }

                return NoContent();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return StatusCode(500, $"Error interno del servidor: {e.Message}");
            }
        }

        // Eliminar
        [HttpDelete("EliminarPuesto")]
        public async Task<IActionResult> EliminarPuesto(Guid id, string empresa, string cadena)
        {
            try
            {
                string query = @"DELETE FROM UsuariosPuestos WHERE id = @id";


				byte[] data = Convert.FromBase64String(cadena);

				// Si quieres convertir los bytes a una cadena original
				cadena = Encoding.UTF8.GetString(data);


				using (SqlConnection connection = new SqlConnection(cadena))
				{
                    await connection.OpenAsync();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        if (rowsAffected == 0)
                        {
                            return NotFound("Puesto no encontrado.");
                        }
                    }
                }

                return NoContent();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return StatusCode(500, $"Error interno del servidor: {e.Message}");
            }
        }
    }
}
