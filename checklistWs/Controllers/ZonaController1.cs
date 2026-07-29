using checklistWs.Models.Zonas;
using checklistWs.Utiles;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Data;
using System.Text;

namespace checklistWs.Controllers
{
    public class ZonaController1 : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly SqlConnectionFactory _connectionFactory;

        public ZonaController1(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionFactory = new SqlConnectionFactory(configuration);
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("ObtenerZona")]
        public async Task<IActionResult> ObtenerZona(Guid idEmpresa, Guid id)
        {
            try
            {
                string query = @"SELECT Id, Nombre, Notas, Fecha, IdEmpresa, borrado 
                                FROM Zonas 
                                WHERE borrado = 0 AND idEmpresa = @IdEmpresa AND id = @Id 
                                ORDER BY Nombre";

                using (SqlConnection connection = _connectionFactory.CreateConnection())
                {
                    await connection.OpenAsync();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                        command.Parameters.AddWithValue("@Id", id);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            var zonas = new List<Zona>();

                            while (await reader.ReadAsync())
                            {
                                var zona = new Zona
                                {
                                    Id = reader.GetGuid(reader.GetOrdinal("Id")),
                                    Nombre = reader.GetString(reader.GetOrdinal("Nombre")),
                                    Notas = reader.GetString(reader.GetOrdinal("Notas")),
                                    Fecha = reader.IsDBNull(reader.GetOrdinal("Fecha")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("Fecha")),
                                    IdEmpresa = reader.GetGuid(reader.GetOrdinal("IdEmpresa")),
                                    borrado = reader.GetBoolean(reader.GetOrdinal("borrado"))
                                };

                                zonas.Add(zona);
                            }

                            return Ok(zonas);
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

        [HttpGet("ObtenerZonas")]
        public async Task<IActionResult> ObtenerZonas(Guid idEmpresa)
        {
            try
            {
                string query = @"SELECT Id, Nombre, Notas, Fecha, IdEmpresa, borrado 
                                FROM Zonas 
                                WHERE borrado = 0 AND idEmpresa = @IdEmpresa 
                                ORDER BY Nombre";

                using (SqlConnection connection = _connectionFactory.CreateConnection())
                {
                    await connection.OpenAsync();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            var zonas = new List<Zona>();

                            while (await reader.ReadAsync())
                            {
                                var zona = new Zona
                                {
                                    Id = reader.GetGuid(reader.GetOrdinal("Id")),
                                    Nombre = reader.GetString(reader.GetOrdinal("Nombre")),
                                    Notas = reader.GetString(reader.GetOrdinal("Notas")),
                                    Fecha = reader.IsDBNull(reader.GetOrdinal("Fecha")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("Fecha")),
                                    IdEmpresa = reader.GetGuid(reader.GetOrdinal("IdEmpresa")),
                                    borrado = reader.GetBoolean(reader.GetOrdinal("borrado"))
                                };

                                zonas.Add(zona);
                            }

                            return Ok(zonas);
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

        [HttpPut("ActualizarZona")]
        public async Task<IActionResult> ActualizarZona(Guid id, [FromBody] Zona zona, string cadena)
        {
            if (await ExisteActualiza(zona.Nombre, zona.IdEmpresa.ToString(),  cadena, id))
            {
                return BadRequest("Ya existe un elemento con este nombre");
            }

            if (id != zona.Id)
            {
                return BadRequest("El ID del elemento no coincide");
            }

            try
            {
                string query = @"UPDATE Zonas 
                                 SET Nombre = @Nombre, Notas = @Notas 
                                 WHERE Id = @Id AND borrado = 0";

                byte[] data = Convert.FromBase64String(cadena);
                cadena = Encoding.UTF8.GetString(data);

                using (SqlConnection connection = new SqlConnection(cadena))
                {
                    await connection.OpenAsync();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        command.Parameters.AddWithValue("@Nombre", zona.Nombre);
                        command.Parameters.AddWithValue("@Notas", zona.Notas);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        if (rowsAffected == 0)
                        {
                            return NotFound("El elemento no fue encontrado");
                        }
                        else
                        {
                            return Ok("Ok");
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

        [HttpDelete("EliminarZona")]
        public async Task<IActionResult> EliminarZona(Guid id)
        {
            try
            {
                string query = @"UPDATE Zonas 
                                 SET borrado = 1 
                                 WHERE Id = @Id AND borrado = 0";

                using (SqlConnection connection = _connectionFactory.CreateConnection())
                {
                    await connection.OpenAsync();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        if (rowsAffected == 0)
                        {
                            return NotFound("La zona no fue encontrada.");
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

        [HttpPost("InsertarZona")]
        public async Task<IActionResult> InsertarZona([FromBody] Zona zona, string cadena)
        {
            try
            {
                if (await ExisteNueva(zona.Nombre, zona.IdEmpresa.ToString(), cadena))
                {
                    return Ok("Ya existe un elemento con este nombre");
                }

                string query = @"INSERT INTO Zonas (Id, Nombre, Notas, Fecha, IdEmpresa, borrado) 
                                 VALUES (NEWID(), @Nombre, @Notas, GETDATE(), @IdEmpresa, 0)";

                byte[] data = Convert.FromBase64String(cadena);
                cadena = Encoding.UTF8.GetString(data);

                using (SqlConnection connection = new SqlConnection(cadena))
                {
                    await connection.OpenAsync();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Nombre", zona.Nombre);
                        command.Parameters.AddWithValue("@Notas", zona.Notas);
                        command.Parameters.AddWithValue("@IdEmpresa", zona.IdEmpresa);

                        await command.ExecuteNonQueryAsync();
                    }
                }

                return Ok("Ok");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return StatusCode(500, $"Error interno del servidor: {e.Message}");
            }
        }


        [HttpPost("InsertarPrimerZona")]
        public async Task<IActionResult> InsertarPrimerZona([FromBody] Zona nuevoRegistro)
        {
            try
            {
                string query = @"INSERT INTO Zonas (Id, Nombre, Notas, Fecha, IdEmpresa, borrado) 
                                 VALUES (@Id, @Nombre, @Notas, GETDATE(), @IdEmpresa, 0)";

                using (SqlConnection connection = _connectionFactory.CreateConnection())
                {
                    await connection.OpenAsync();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", nuevoRegistro.Id);
                        command.Parameters.AddWithValue("@Nombre", nuevoRegistro.Nombre);
                        command.Parameters.AddWithValue("@Notas", nuevoRegistro.Notas);
                        command.Parameters.AddWithValue("@IdEmpresa", nuevoRegistro.IdEmpresa);

                        await command.ExecuteNonQueryAsync();
                    }
                }

                return Ok("Zona insertada con éxito.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return StatusCode(500, $"Error interno del servidor: {e.Message}");
            }
        }

        [HttpPost("GuardarZona")]
    /*    public async Task<IActionResult> GuardarZona([FromBody] Zona zona, string cadena)
        {
            try
            {
                // Validaciones de campos requeridos
                if (zona == null)
                {
                    return BadRequest(new { message = "Los datos son requeridos." });
                }

                if (string.IsNullOrEmpty(zona.Nombre?.Trim()))
                {
                    return BadRequest(new { message = "El campo 'Nombre' es requerido." });
                }

            
                // Verificar si el nombre de la zona ya existe en la misma empresa
                bool existe = await Existe(zona.Nombre, zona.IdEmpresa.ToString(), cadena, zona.Id);
                if (existe)
                {
                    // Si ya existe, no permitimos insertar ni actualizar
                    return BadRequest(new { message = "El elemento ya existe en esta empresa." });
                }

                // Si el Id de la zona es vacío, se trata de una inserción
                if (zona.Id == Guid.Empty)
                {
                    // Inserción de nuevo elemento
                    string queryInsertar = @"INSERT INTO Zonas (Nombre, Notas, Fecha, IdEmpresa, borrado) 
                                     VALUES (@Nombre, @Notas, GETDATE(), @IdEmpresa, 0)";

                    using (SqlConnection connection = _connectionFactory.CreateConnection())
                    {
                        await connection.OpenAsync();

                        using (SqlCommand command = new SqlCommand(queryInsertar, connection))
                        {
                            command.Parameters.AddWithValue("@Nombre", zona.Nombre);
                            command.Parameters.AddWithValue("@Notas", zona.Notas);
                            command.Parameters.AddWithValue("@IdEmpresa", zona.IdEmpresa);

                            await command.ExecuteNonQueryAsync();
                        }
                    }

                    return Ok(new { message = "Ok" });
                }
                else // Si el Id no es vacío, es una actualización
                {
                    // Actualización del elemento existente
                    string queryActualizar = @"UPDATE Zonas 
                                      SET Nombre = @Nombre, Notas = @Notas 
                                      WHERE Id = @Id AND borrado = 0";

                    using (SqlConnection connection = _connectionFactory.CreateConnection())
                    {
                        await connection.OpenAsync();

                        using (SqlCommand command = new SqlCommand(queryActualizar, connection))
                        {
                            command.Parameters.AddWithValue("@Id", zona.Id);
                            command.Parameters.AddWithValue("@Nombre", zona.Nombre);
                            command.Parameters.AddWithValue("@Notas", zona.Notas);

                            int rowsAffected = await command.ExecuteNonQueryAsync();
                            if (rowsAffected == 0)
                            {
                                return NotFound(new { message = "El elemento no fue encontrado o ya fue eliminado." });
                            }
                        }
                    }

                    return Ok(new { message = "Ok" });
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return StatusCode(500, new { message = "Error interno del servidor.", error = e.Message });
            }
        }*/



        private async Task<bool> ExisteActualiza(string nombre, string idEmpresa, string cadena, Guid? idActual = null)
        {
            bool regresa = false;

            try
            {
                byte[] data = Convert.FromBase64String(cadena);
                cadena = Encoding.UTF8.GetString(data);

                using (SqlConnection connection = new SqlConnection(cadena))
                {
                    await connection.OpenAsync();

                    string sQuery = $"SELECT COUNT(*) FROM [Zonas] WHERE nombre = '{nombre}' AND idEmpresa = '{idEmpresa}' AND borrado = 0";

                    
                    if (idActual != null)
                    {
                        sQuery += $" AND id != '{idActual.ToString()}'"; 
                    }

                    using (SqlCommand command = new SqlCommand(sQuery, connection))
                    {
                       
                        int count = (int)await command.ExecuteScalarAsync();

                       
                        regresa = (count > 0);  
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            return regresa;
        }

        private async Task<bool> ExisteNueva(string nombre, string idEmpresa, string cadena)
        {
            bool regresa = false;

            try
            {
                byte[] data = Convert.FromBase64String(cadena);
                cadena = Encoding.UTF8.GetString(data);

                using (SqlConnection connection = new SqlConnection(cadena))
                {
                    await connection.OpenAsync();

                   
                    string sQuery = $"SELECT COUNT(*) FROM [Zonas] WHERE nombre = '{nombre}' AND idEmpresa = '{idEmpresa}' AND borrado = 0";

                    using (SqlCommand command = new SqlCommand(sQuery, connection))
                    {
                       
                        int count = (int)await command.ExecuteScalarAsync();

                      
                        regresa = (count > 0);  
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            return regresa;
        }



    }
}
