using checklistWs.Models.Combo;
using checklistWs.Models.Usuario;
using checklistWs.Utiles;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Text;

namespace checklistWs.Controllers.Usuario
{
   
    public class UsuariosDepartamentosController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly SqlConnectionFactory _connectionFactory;

        public UsuariosDepartamentosController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionFactory = new SqlConnectionFactory(configuration);
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [Route("GetComboDepartamentos")]
        public async Task<IActionResult> GetComboDepartamentos(Guid idEmpresa, string empresa, string cadena, string nombre = null)
        {
            try
            {
                List<DataPair2> departamentos = new List<DataPair2>();

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
                    string sQuery = $"SELECT id, Nombre FROM [UsuariosDepartamentos] WHERE idEmpresa = '{idEmpresa}' {sComp} ORDER BY Nombre";
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(sQuery, connection))
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (reader.Read())
                            {
                                departamentos.Add(new DataPair2()
                                {
                                    value = reader["id"] != DBNull.Value ? reader["id"].ToString() : string.Empty,
                                    name = reader["Nombre"] != DBNull.Value ? reader["Nombre"].ToString().Trim() : string.Empty
                                });
                            }
                        }
                    }
                    return Ok(departamentos);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                // Retornar un código de error HTTP 500 (Internal Server Error)
                return StatusCode(500, $"Error interno del servidor {ex.Message}");
            }
        }

        [HttpPost("InsertarDepartamento")]
        public async Task<IActionResult> InsertarDepartamento([FromBody] UsuariosDepartamentos nuevoDepartamento, string empresa, string cadena)
        {
            try
            {
                string query = @"INSERT INTO UsuariosDepartamentos ( Nombre, notas, fecha, borrado, idEmpresa)
                                 VALUES ( @Nombre, @notas, @fecha, @borrado, @idEmpresa)";

                byte[] data = Convert.FromBase64String(cadena);

                // Si quieres convertir los bytes a una cadena original
                cadena = Encoding.UTF8.GetString(data);

                using (SqlConnection connection = new SqlConnection(cadena))
				{
                    await connection.OpenAsync();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                      
                        command.Parameters.AddWithValue("@Nombre", nuevoDepartamento.Nombre);
                        command.Parameters.AddWithValue("@notas", nuevoDepartamento.notas);
                        command.Parameters.AddWithValue("@fecha", nuevoDepartamento.fecha);
                        command.Parameters.AddWithValue("@borrado", nuevoDepartamento.borrado);
                        command.Parameters.AddWithValue("@idEmpresa", nuevoDepartamento.idEmpresa);

                        await command.ExecuteNonQueryAsync();
                    }
                }

                return Ok("Departamento insertado con éxito.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return StatusCode(500, $"Error interno del servidor: {e.Message}");
            }
        }

		[HttpPost("InsertarPrimerDepartamento")]
		public async Task<IActionResult> InsertarPrimerDepartamento([FromBody] UsuariosDepartamentos nuevoDepartamento, string empresa, string cadena)
		{
			try
			{
				string query = @"INSERT INTO UsuariosDepartamentos (id, Nombre, notas, fecha, borrado, idEmpresa)
                                 VALUES (@Id, @Nombre, @notas, @fecha, @borrado, @idEmpresa)";

				byte[] data = Convert.FromBase64String(cadena);

				// Si quieres convertir los bytes a una cadena original
				cadena = Encoding.UTF8.GetString(data);

				using (SqlConnection connection = new SqlConnection(cadena))
				{
					await connection.OpenAsync();

					using (SqlCommand command = new SqlCommand(query, connection))
					{

						command.Parameters.AddWithValue("@Id", nuevoDepartamento.id);
						command.Parameters.AddWithValue("@Nombre", nuevoDepartamento.Nombre);
						command.Parameters.AddWithValue("@notas", nuevoDepartamento.notas);
						command.Parameters.AddWithValue("@fecha", nuevoDepartamento.fecha);
						command.Parameters.AddWithValue("@borrado", nuevoDepartamento.borrado);
						command.Parameters.AddWithValue("@idEmpresa", nuevoDepartamento.idEmpresa);

						await command.ExecuteNonQueryAsync();
					}
				}

				return Ok("Departamento insertado con éxito.");
			}
			catch (Exception e)
			{
				Console.WriteLine($"Error: {e.Message}");
				return StatusCode(500, $"Error interno del servidor: {e.Message}");
			}
		}

		// Leer (Obtener uno)
		[HttpGet("ObtenerDepartamento")]
        public async Task<IActionResult> ObtenerDepartamento(Guid id, string empresa, string cadena)
        {
            try
            {
                string query = @"SELECT id, Nombre, notas, fecha, borrado, idEmpresa 
                                 FROM UsuariosDepartamentos 
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

                            var departamentos = new List<UsuariosDepartamentos>();
                            while (await reader.ReadAsync())
                            {
                                var departamento = new UsuariosDepartamentos
                                {
                                    id = reader.GetGuid(reader.GetOrdinal("id")),
                                    Nombre = reader.GetString(reader.GetOrdinal("Nombre")),
                                    notas = reader["notas"] != DBNull.Value ? reader["notas"].ToString() ?? string.Empty : string.Empty,
                                    fecha = reader.IsDBNull(reader.GetOrdinal("fecha")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("fecha")),
                                    borrado = reader.IsDBNull(reader.GetOrdinal("borrado")) ? (bool?)null : reader.GetBoolean(reader.GetOrdinal("borrado")),
                                    idEmpresa = reader.IsDBNull(reader.GetOrdinal("idEmpresa")) ? (Guid?)null : reader.GetGuid(reader.GetOrdinal("idEmpresa"))
                                };

                             
                                departamentos.Add(departamento);
                            }
                            return Ok(departamentos);

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

		[HttpGet("ObtenerPrimerDepartamento")]
		public async Task<IActionResult> ObtenerPrimerDepartamento(string nombre, string empresa, string cadena, Guid? idEmpresa = null)
		{
			try
			{
				string query = @"SELECT id, Nombre, notas, fecha, borrado, idEmpresa 
	                                 FROM UsuariosDepartamentos 
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

							var departamentos = new List<UsuariosDepartamentos>();
							while (await reader.ReadAsync())
							{
								var departamento = new UsuariosDepartamentos
								{
									id = reader.GetGuid(reader.GetOrdinal("id")),
									Nombre = reader.GetString(reader.GetOrdinal("Nombre")),
										notas = reader["notas"] != DBNull.Value ? reader["notas"].ToString() ?? string.Empty : string.Empty,
									fecha = reader.IsDBNull(reader.GetOrdinal("fecha")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("fecha")),
									borrado = reader.IsDBNull(reader.GetOrdinal("borrado")) ? (bool?)null : reader.GetBoolean(reader.GetOrdinal("borrado")),
									idEmpresa = reader.IsDBNull(reader.GetOrdinal("idEmpresa")) ? (Guid?)null : reader.GetGuid(reader.GetOrdinal("idEmpresa"))
								};


								departamentos.Add(departamento);
							}
							return Ok(departamentos);

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
		[HttpGet("ObtenerDepartamentos")]
        public async Task<IActionResult> ObtenerDepartamentos(string empresa, string idEmpresa, string cadena)
        {
            try
            {
                string query = @"SELECT id, Nombre, notas, fecha, borrado, idEmpresa 
                                 FROM UsuariosDepartamentos where idEmpresa = '"+idEmpresa+"'";

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
                            var departamentos = new List<UsuariosDepartamentos>();

                            while (await reader.ReadAsync())
                            {
                                var departamento = new UsuariosDepartamentos
                                {
                                    id = reader.GetGuid(reader.GetOrdinal("id")),
                                    Nombre = reader.GetString(reader.GetOrdinal("Nombre")),
                                    notas = reader["notas"] != DBNull.Value ? reader["notas"].ToString() ?? string.Empty : string.Empty,
                                    fecha = reader.IsDBNull(reader.GetOrdinal("fecha")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("fecha")),
                                    borrado = reader.IsDBNull(reader.GetOrdinal("borrado")) ? (bool?)null : reader.GetBoolean(reader.GetOrdinal("borrado")),
                                    idEmpresa = reader.IsDBNull(reader.GetOrdinal("idEmpresa")) ? (Guid?)null : reader.GetGuid(reader.GetOrdinal("idEmpresa"))
                                };

                                departamentos.Add(departamento);
                            }

                            return Ok(departamentos);
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
        [HttpPut("ActualizarDepartamento")]
        public async Task<IActionResult> ActualizarDepartamento(Guid id, [FromBody] UsuariosDepartamentos departamento, string empresa, string cadena)
        {
            if (id != departamento.id)
            {
                return BadRequest("El ID del departamento no coincide.");
            }

            try
            {
                string query = @"UPDATE UsuariosDepartamentos 
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
                        command.Parameters.AddWithValue("@id", departamento.id);
                        command.Parameters.AddWithValue("@Nombre", departamento.Nombre);
                        command.Parameters.AddWithValue("@notas", departamento.notas);
                        command.Parameters.AddWithValue("@fecha", departamento.fecha);
                        command.Parameters.AddWithValue("@borrado", departamento.borrado);
                        command.Parameters.AddWithValue("@idEmpresa", departamento.idEmpresa);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        if (rowsAffected == 0)
                        {
                            return NotFound("Departamento no encontrado.");
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
        [HttpDelete("EliminarDepartamento")]
        public async Task<IActionResult> EliminarDepartamento(Guid id, string empresa, string cadena)
        {
            try
            {
                string query = @"DELETE FROM UsuariosDepartamentos WHERE id = @id";

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
                            return NotFound("Departamento no encontrado.");
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
