using checklistWs.Models.Combo;
using checklistWs.Utiles;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Text;

namespace checklistWs.Controllers.Roles
{
	public class RolesController : Controller
	{

		private readonly IConfiguration _configuration;
		private readonly SqlConnectionFactory _connectionFactory;

		public RolesController(IConfiguration configuration)
		{
			_configuration = configuration;
			_connectionFactory = new SqlConnectionFactory(configuration);
		}
		public IActionResult Index()
		{
			return View();
		}

		[HttpGet]
		[Route("GetRoles")]
		public async Task<IActionResult> GetRoles(Guid idEmpresa,string empresa, string cadena,  Guid? id = null)
		{
			try
			{
				List<Models.Roles.Roles> ListaRoles = new List<Models.Roles.Roles>();


				byte[] data = Convert.FromBase64String(cadena);

				// Si quieres convertir los bytes a una cadena original
				cadena = Encoding.UTF8.GetString(data);

				using (SqlConnection connection = new SqlConnection(cadena))
				{
					string sComp = string.Empty;
					if (id != null)
					{
						if (id != Guid.Empty)
						{
							sComp = $"AND id = '{id}'";
						}
					}
					string sQuery = $"SELECT id, idEmpresa, NombreRol, Permisos FROM [Roles] WHERE idEmpresa = '{idEmpresa}' {sComp} ORDER BY NombreRol";
					connection.Open();
					using (SqlCommand command = new SqlCommand(sQuery, connection))
					{
						using (SqlDataReader reader = await command.ExecuteReaderAsync())
						{
							while (reader.Read())
							{
								ListaRoles.Add(new Models.Roles.Roles()
								{
									id = reader["id"] != DBNull.Value ? Guid.Parse(reader["id"].ToString()) : Guid.Empty,
									idEmpresa = reader["idEmpresa"] != DBNull.Value ? Guid.Parse(reader["idEmpresa"].ToString()) : Guid.Empty,
									NombreRol = reader["NombreRol"] != DBNull.Value ? reader["NombreRol"].ToString().Trim() : string.Empty,
									Permisos = reader["Permisos"] != DBNull.Value ? reader["Permisos"].ToString().Trim() : string.Empty
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
		[Route("GetRoll")]
		public async Task<IActionResult> GetRoll(string cadena, string idEmpresa, string nombreRol)
		{
			try
			{
				List<Models.Roles.Roles> ListaRoles = new List<Models.Roles.Roles>();


				byte[] data = Convert.FromBase64String(cadena);

				// Si quieres convertir los bytes a una cadena original
				cadena = Encoding.UTF8.GetString(data);

				using (SqlConnection connection = new SqlConnection(cadena))
				{
					
						string nombreRolNormalizado = string.IsNullOrWhiteSpace(nombreRol) ? "SuperAdmin" : nombreRol.Trim();
						string sQuery = "SELECT id, idEmpresa, NombreRol, Permisos FROM [Roles] WHERE idEmpresa = @IdEmpresa AND NombreRol = @NombreRol";
						connection.Open();
						using (SqlCommand command = new SqlCommand(sQuery, connection))
						{
							command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
							command.Parameters.AddWithValue("@NombreRol", nombreRolNormalizado);
							using (SqlDataReader reader = await command.ExecuteReaderAsync())
						{
							while (reader.Read())
							{
								ListaRoles.Add(new Models.Roles.Roles()
								{
									id = reader["id"] != DBNull.Value ? Guid.Parse(reader["id"].ToString()) : Guid.Empty,
									idEmpresa = reader["idEmpresa"] != DBNull.Value ? Guid.Parse(reader["idEmpresa"].ToString()) : Guid.Empty,
									NombreRol = reader["NombreRol"] != DBNull.Value ? reader["NombreRol"].ToString().Trim() : string.Empty,
									Permisos = reader["Permisos"] != DBNull.Value ? reader["Permisos"].ToString().Trim() : string.Empty
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
		[Route("GetComboRoles")]
		public async Task<IActionResult> GetComboRoles(Guid idEmpresa, string empresa,string cadena, string rol = null)
		{
			try
			{
				List<DataPair2> ListaRoles = new List<DataPair2>();

				byte[] data = Convert.FromBase64String(cadena);

				// Si quieres convertir los bytes a una cadena original
				cadena = Encoding.UTF8.GetString(data);

				using (SqlConnection connection = new SqlConnection(cadena))
				{
					string sComp = string.Empty;
					if (rol != null)
					{
						sComp = $"AND NombreRol LIKE '%{rol}%'";
					}
					string sQuery = $"SELECT id, NombreRol FROM [Roles] WHERE idEmpresa = '{idEmpresa}' {sComp} ORDER BY NombreRol";
					connection.Open();
					using (SqlCommand command = new SqlCommand(sQuery, connection))
					{
						using (SqlDataReader reader = await command.ExecuteReaderAsync())
						{
							while (reader.Read())
							{
								ListaRoles.Add(new DataPair2()
								{
									value = reader["id"] != DBNull.Value ? reader["id"].ToString() : string.Empty,
									name = reader["NombreRol"] != DBNull.Value ? reader["NombreRol"].ToString().Trim() : string.Empty
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

		private async Task<bool> ExisteAsync(Guid? cualId, Guid? idEmpresa, string nombreRol, string cadena)
		{
			try
			{
				using (SqlConnection connection = new SqlConnection(cadena))
				{
					await connection.OpenAsync();

						// Verificar si existe un registro con el mismo nombreRol dentro de la misma empresa y con un ID diferente al actual
						string sQuery = "SELECT COUNT(*) FROM [Roles] WHERE idEmpresa = @IdEmpresa AND nombreRol = @NombreRol AND id != @Id";
						using (SqlCommand command = new SqlCommand(sQuery, connection))
						{
							command.Parameters.AddWithValue("@Id", cualId.HasValue ? (object)cualId.Value : DBNull.Value);
							command.Parameters.AddWithValue("@IdEmpresa", idEmpresa.HasValue ? (object)idEmpresa.Value : DBNull.Value);
							command.Parameters.AddWithValue("@NombreRol", nombreRol);

						int count = (int)await command.ExecuteScalarAsync();

						// Retorna true si existe un registro con el mismo nombreRol pero diferente ID
						return count > 0;
					}
				}
			}
			catch (Exception ex)
			{
				// Manejo de la excepción, se puede registrar aquí
				Console.WriteLine($"Error al verificar la existencia del registro: {ex.Message}");
				return false;
			}
		}

		[HttpPut]
		[Route("Guardar")]
		public async Task<IActionResult> Guardar([FromBody] Models.Roles.Roles item, string empresa, string cadena)
		{
			try
			{
				// Decodifica la cadena de conexión
				byte[] data = Convert.FromBase64String(cadena);
				cadena = Encoding.UTF8.GetString(data);

				using (SqlConnection connection = new SqlConnection(cadena))
				{
					await connection.OpenAsync();

					if (item.id == null) item.id = Guid.NewGuid(); // Genera un nuevo ID si es nulo

					// Verificar si ya existe un registro con el mismo nombreRol y un ID diferente
						if (await ExisteAsync(item.id, item.idEmpresa, item.NombreRol, cadena))
					{
						// Si existe un registro con el mismo nombreRol y diferente ID, no se permite la operación
						return BadRequest("Ya existe un registro con el mismo nombreRol.");
					}

					// Verificar si el registro actual existe para determinar si es actualización o inserción
					string selectQuery = "SELECT COUNT(*) FROM [Roles] WHERE id = @Id";
					using (SqlCommand selectCommand = new SqlCommand(selectQuery, connection))
					{
						selectCommand.Parameters.AddWithValue("@Id", item.id);

						int recordExists = (int)await selectCommand.ExecuteScalarAsync();

						if (recordExists > 0)
						{
							// Actualizar el registro existente
							string updateQuery = "UPDATE [Roles] SET [NombreRol] = @NombreRol, [Permisos] = @Permisos WHERE [id] = @Id";
							using (SqlCommand updateCommand = new SqlCommand(updateQuery, connection))
							{
								updateCommand.Parameters.AddWithValue("@Id", item.id);
								updateCommand.Parameters.AddWithValue("@NombreRol", item.NombreRol?.Trim() ?? (object)DBNull.Value);
								updateCommand.Parameters.AddWithValue("@Permisos", item.Permisos?.Trim() ?? (object)DBNull.Value);

								int rowsAffected = await updateCommand.ExecuteNonQueryAsync();
								if (rowsAffected > 0)
								{
									return Ok("Registro actualizado correctamente");
								}
								else
								{
									return Ok("No se pudo actualizar el registro");
								}
							}
						}
						else
						{
							// Insertar un nuevo registro
								string insertQuery = "INSERT INTO [Roles] ( [id], [idEmpresa], [NombreRol], [Permisos]) VALUES ( @Id, @IdEmpresa, @NombreRol, @Permisos)";
								using (SqlCommand insertCommand = new SqlCommand(insertQuery, connection))
								{
									insertCommand.Parameters.AddWithValue("@Id", item.id ?? Guid.NewGuid());
									insertCommand.Parameters.AddWithValue("@IdEmpresa", item.idEmpresa ?? (object)DBNull.Value);
									insertCommand.Parameters.AddWithValue("@NombreRol", item.NombreRol?.Trim() ?? (object)DBNull.Value);
								insertCommand.Parameters.AddWithValue("@Permisos", item.Permisos?.Trim() ?? (object)DBNull.Value);

								int rowsAffected = await insertCommand.ExecuteNonQueryAsync();
								if (rowsAffected > 0)
								{
									return Ok("Registro insertado correctamente");
								}
								else
								{
									return Ok("No se pudo insertar el registro");
								}
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error: {ex.Message}");
				// Retornar un código de error HTTP 500 (Internal Server Error)
				return StatusCode(500, $"Error interno del servidor: {ex.Message}");
			}
		}


	}
}
