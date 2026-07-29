using checklistWs.Models.Categorias;
using checklistWs.Models.Sucursal;
using checklistWs.Utiles;
using LiteDB;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace checklistWs.Controllers.Categorias
{
    public class CategoriasController : Controller
    {
        private static string ReadNullableString(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
        }

        public IActionResult Index()
        {
            return View();
        }

        private readonly IConfiguration _configuration;
        private readonly SqlConnectionFactory _connectionFactory;

        public CategoriasController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionFactory = new SqlConnectionFactory(configuration);
        }

        [HttpGet("ObtenerCategoria")]

        public async Task<IActionResult> ObtenerSucursal(Guid idEmpresa, Guid id, string empresa, string cadena)
        {
            try
            {

                string query = @"SELECT Nombre, fecha, id, idEmpresa, borrado, notas FROM ListasPreguntasCategorias
                                WHERE borrado = 0 AND idEmpresa = @IdEmpresa AND id = @Id 
                                ORDER BY Nombre";


                byte[] data = Convert.FromBase64String(cadena);

                // Si quieres convertir los bytes a una cadena original
                cadena = Encoding.UTF8.GetString(data);

                using (SqlConnection connection = new SqlConnection(cadena))
                {
                    await connection.OpenAsync();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                        command.Parameters.AddWithValue("@Id", id);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            var sucursales = new List<ListasPreguntasCategorias>();

                            while (await reader.ReadAsync())
                            {
                                var sucursal = new ListasPreguntasCategorias
                                {
                                    Id = reader.GetGuid(reader.GetOrdinal("Id")),
                                    IdEmpresa = reader.GetGuid(reader.GetOrdinal("idEmpresa")),
                                    Nombre = reader.GetString(reader.GetOrdinal("Nombre")),
                                    Borrado = reader.GetBoolean(reader.GetOrdinal("borrado")),
                                    Fecha = reader.GetDateTime(reader.GetOrdinal("fecha")),
                                    Notas = ReadNullableString(reader, "Notas"),

                                };

                                sucursales.Add(sucursal);
                            }

                            return Ok(sucursales);
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

        [HttpGet("ObtenerCategorias")]
        public async Task<IActionResult> ObtenerSucursales(Guid idEmpresa, string empresa, string cadena, string cualPrograma = "")
        {
            try
            {

                string sComp = string.Empty;
                if (!string.IsNullOrEmpty(cualPrograma))
                {
                    sComp = string.Format(" AND Nombre LIKE '%{0}%'", cualPrograma);
                }
                string query = @$"SELECT Nombre, fecha, id, idEmpresa, borrado, notas FROM ListasPreguntasCategorias
                                WHERE borrado = 0 AND idEmpresa = @IdEmpresa  {sComp}
                                ORDER BY Nombre";


                byte[] data = Convert.FromBase64String(cadena);

                // Si quieres convertir los bytes a una cadena original
                cadena = Encoding.UTF8.GetString(data);

                using (SqlConnection connection = new SqlConnection(cadena))
                {
                    await connection.OpenAsync();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            var sucursales = new List<ListasPreguntasCategorias>();

                            while (await reader.ReadAsync())
                            {
                                var sucursal = new ListasPreguntasCategorias
                                {
                                    Id = reader.GetGuid(reader.GetOrdinal("Id")),
                                    IdEmpresa = reader.GetGuid(reader.GetOrdinal("idEmpresa")),
                                    Nombre = reader.GetString(reader.GetOrdinal("Nombre")),
                                    Borrado = reader.GetBoolean(reader.GetOrdinal("borrado")),
                                    Fecha = reader.GetDateTime(reader.GetOrdinal("fecha")),
                                    Notas = ReadNullableString(reader, "Notas"),
                                };

                                sucursales.Add(sucursal);
                            }

                            return Ok(sucursales);
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



        [HttpPut("ActualizarCategoria")]
        public async Task<IActionResult> ActualizarCategoria(Guid id, [FromBody] ListasPreguntasCategorias categoria, string empresa, string cadena)
        {
            if (await ExisteActualiza(categoria.Nombre, categoria.IdEmpresa.ToString(), cadena, id))
            {
                return BadRequest("Ya existe un elemento con este nombre");
            }

            // Verificar que el ID coincida
            if (id != categoria.Id)
            {
                return BadRequest("El ID del elemento no coincide");
            }

            try
            {
                string query = @"UPDATE ListasPreguntasCategorias SET Nombre = @Nombre, IdEmpresa = @IdEmpresa, Borrado = 0, Notas = @Notas WHERE Id = @Id AND borrado = 0";

                byte[] data = Convert.FromBase64String(cadena);
                cadena = Encoding.UTF8.GetString(data);

                using (SqlConnection connection = new SqlConnection(cadena))
                {
                    await connection.OpenAsync();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", categoria.Id);
                        command.Parameters.AddWithValue("@IdEmpresa", categoria.IdEmpresa);
                        command.Parameters.AddWithValue("@Nombre", categoria.Nombre);
                        command.Parameters.AddWithValue("@Notas", categoria.Notas);

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



        [HttpDelete("EliminarCategoria")]
        public async Task<IActionResult> EliminarSucursal(Guid id, string empresa, string cadena)
        {
            try
            {

                byte[] data = Convert.FromBase64String(cadena);

                // Si quieres convertir los bytes a una cadena original
                cadena = Encoding.UTF8.GetString(data);
                string query = @"UPDATE FROM ListasPreguntasCategorias SET borrado = 1 WHERE Id = @Id";
                using (SqlConnection connection = new SqlConnection(cadena))
                {
                    await connection.OpenAsync();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        if (rowsAffected == 0)
                        {
                            return NotFound("La sucursal no fue encontrada.");
                        }
                        else
                        {
                            return Ok("Ok.");
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

        [HttpPost("InsertarCategoria")]
        public async Task<IActionResult> InsertarCategoria([FromBody] ListasPreguntasCategorias categoria, string empresa, string cadena)
        {
            try
            {
                if (await ExisteNueva(categoria.Nombre, categoria.IdEmpresa.ToString(), cadena))
                {
                    return Ok("Ya existe un elemento con este nombre");
                }

                string query = @"INSERT INTO ListasPreguntasCategorias (Nombre, Fecha, IdEmpresa, Borrado, Notas)
                         VALUES (@Nombre, getDate(), @IdEmpresa, 0, @Notas);";

                byte[] data = Convert.FromBase64String(cadena);
                cadena = Encoding.UTF8.GetString(data);

                using (SqlConnection connection = new SqlConnection(cadena))
                {
                    await connection.OpenAsync();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Nombre", categoria.Nombre);
                        command.Parameters.AddWithValue("@IdEmpresa", categoria.IdEmpresa);
                        command.Parameters.AddWithValue("@Borrado", categoria.Borrado);
                        command.Parameters.AddWithValue("@Notas", categoria.Notas);

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

                    string sQuery = $"SELECT COUNT(*) FROM [ListasPreguntasCategorias] WHERE nombre = '{nombre}' AND idEmpresa = '{idEmpresa}' AND borrado = 0";

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
            bool regresa = false; // Inicializar a false, ya que la categoría no existe al principio.

            try
            {
                byte[] data = Convert.FromBase64String(cadena);
                cadena = Encoding.UTF8.GetString(data);

                using (SqlConnection connection = new SqlConnection(cadena))
                {
                    await connection.OpenAsync();

                    // Crear la consulta para verificar si la categoría ya existe
                    string sQuery = $"SELECT COUNT(*) FROM [ListasPreguntasCategorias] WHERE nombre = '{nombre}' AND idEmpresa = '{idEmpresa}' AND borrado = 0";

                    using (SqlCommand command = new SqlCommand(sQuery, connection))
                    {
                        // Ejecutar la consulta y verificar si ya existe una categoría con el mismo nombre
                        int count = (int)await command.ExecuteScalarAsync();

                        // Si count es mayor que 0, la categoría ya existe, por lo que debemos devolver true
                        regresa = (count > 0);  // regresa será true si ya existe
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
