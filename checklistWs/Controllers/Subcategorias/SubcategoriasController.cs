using checklistWs.Models.Categorias;
using checklistWs.Models.Subcategorias;
using checklistWs.Models.Sucursal;
using checklistWs.Utiles;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Text;
using System.Xml.Linq;

namespace checklistWs.Controllers.Subcategorias
{
    public class SubcategoriasController : Controller
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

        public SubcategoriasController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionFactory = new SqlConnectionFactory(configuration);
        }

        [HttpGet("ObtenerSubcategoria")]
        public async Task<IActionResult> ObtenerSucursal(Guid idEmpresa, Guid id, string empresa, string cadena)
        {
            try
            {

				string query = @$"SELECT Nombre, fecha, id, idEmpresa, borrado, notas FROM ListasPreguntasSubCategorias
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
                            var sucursales = new List<ListasPreguntasSubcategorias>();

                            while (await reader.ReadAsync())
                            {
                                var sucursal = new ListasPreguntasSubcategorias
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

        [HttpGet("ObtenerSubcategorias")]
        public async Task<IActionResult> ObtenerSucursales(Guid idEmpresa, string empresa, string cadena, string cualPrograma = "")
        {
            try
            {
                string sComp = string.Empty;
                if (!string.IsNullOrEmpty(cualPrograma))
                {
                    sComp = string.Format(" AND Nombre LIKE '%{0}%'", cualPrograma);
                }
                string query = @$"SELECT Nombre, fecha, id, idEmpresa, borrado, notas FROM ListasPreguntasSubCategorias
                                WHERE borrado = 0 AND idEmpresa = @IdEmpresa {sComp}
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
                            var sucursales = new List<ListasPreguntasSubcategorias>();

                            while (await reader.ReadAsync())
                            {
                                var sucursal = new ListasPreguntasSubcategorias
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

        [HttpPut("ActualizarSubcategoria")]
        public async Task<IActionResult> ActualizarSubcategoria(Guid id, [FromBody] ListasPreguntasSubcategorias subcategoria, string empresa, string cadena)
        {
            if (await ExisteActualiza(subcategoria.Nombre, subcategoria.IdEmpresa.ToString(), cadena, id))
            {
                return BadRequest("Ya existe un elemento con este nombre");
            }

            
            if (id != subcategoria.Id)
            {
                return BadRequest("El ID del elemento no coincide");
            }

            try
            {
                string query = @"UPDATE ListasPreguntasSubCategorias SET Nombre = @Nombre, IdEmpresa = @IdEmpresa, Borrado = 0, Notas = @Notas WHERE Id = @Id";

                byte[] data = Convert.FromBase64String(cadena);
                cadena = Encoding.UTF8.GetString(data);

                using (SqlConnection connection = new SqlConnection(cadena))
                {
                    await connection.OpenAsync();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", subcategoria.Id);
                        command.Parameters.AddWithValue("@IdEmpresa", subcategoria.IdEmpresa);
                        command.Parameters.AddWithValue("@Nombre", subcategoria.Nombre);
                        command.Parameters.AddWithValue("@Notas", subcategoria.Notas);

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
        [HttpDelete("EliminarSubcategoria")]
        public async Task<IActionResult> EliminarSucursal(Guid id, string empresa, string cadena)
        {
            try
            {

                string query = @"UPDATE FROM ListasPreguntasSubCategorias SET borrado = 1 WHERE Id = @Id";

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

        [HttpPost("InsertarSubcategoria")]
        public async Task<IActionResult> InsertarSubcategoria([FromBody] ListasPreguntasSubcategorias subcategoria, string empresa, string cadena)
        {
            try
            {
                if (await ExisteNueva(subcategoria.Nombre, subcategoria.IdEmpresa.ToString(), cadena))
                {
                    return Ok("Ya existe un elemento con este nombre");
                }

                string query = @"INSERT INTO ListasPreguntasSubCategorias (Nombre, Fecha, IdEmpresa, Borrado, Notas)
                         VALUES (@Nombre, getDate(), @IdEmpresa, 0, @Notas);";

                byte[] data = Convert.FromBase64String(cadena);
                cadena = Encoding.UTF8.GetString(data);

                using (SqlConnection connection = new SqlConnection(cadena))
                {
                    await connection.OpenAsync();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Nombre", subcategoria.Nombre);
                        command.Parameters.AddWithValue("@IdEmpresa", subcategoria.IdEmpresa);
                        command.Parameters.AddWithValue("@Borrado", subcategoria.Borrado);
                        command.Parameters.AddWithValue("@Notas", subcategoria.Notas);

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

        private async Task<bool> ExisteActualiza( string nombre, string idEmpresa, string cadena, Guid? idActual = null)
        {
            bool regresa = false; // Inicializar a true, luego lo cambiaremos a false si la categoría existe.

            try
            {
                byte[] data = Convert.FromBase64String(cadena);
                cadena = Encoding.UTF8.GetString(data);

                using (SqlConnection connection = new SqlConnection(cadena))
                {
                    await connection.OpenAsync();

                    // Modificar la consulta para insertar los valores literales en lugar de parámetros
                    string sQuery = $"SELECT COUNT(*) FROM [ListasPreguntasSubCategorias] WHERE nombre = '{nombre}' AND idEmpresa = '{idEmpresa}' AND borrado = 0";

                    // Si tenemos un ID actual, añadimos la condición para excluirlo
                    if (idActual != null)
                    {
                        sQuery += $" AND id != '{idActual.ToString()}'"; // Excluir el registro actual
                    }

                    using (SqlCommand command = new SqlCommand(sQuery, connection))
                    {
                        // Ejecutar la consulta y verificar si ya existe una categoría con el mismo nombre
                        int count = (int)await command.ExecuteScalarAsync();

                        // Si count es mayor que 0, la categoría ya existe, por lo que debemos devolver false
                        regresa = (count > 0);  // Esto es correcto ahora: regresa es false si count > 0, es decir, si ya existe
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
                    string sQuery = $"SELECT COUNT(*) FROM [ListasPreguntasSubCategorias] WHERE nombre = '{nombre}' AND idEmpresa = '{idEmpresa}' AND borrado = 0";

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
