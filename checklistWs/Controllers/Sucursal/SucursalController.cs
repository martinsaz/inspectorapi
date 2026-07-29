using checklistWs.Models.Combo;
using checklistWs.Models.Lista;
using checklistWs.Models.Sucursal;
using checklistWs.Utiles;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace checklistWs.Controllers.Sucursal
{
    [Route("api/[controller]")]
    [ApiController]
    public class SucursalController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly SqlConnectionFactory _connectionFactory;

        public SucursalController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionFactory = new SqlConnectionFactory(configuration);
        }

        [HttpGet]
        [Route("GetComboSucursales")]
        public async Task<IActionResult> GetComboSucursales(Guid idEmpresa, string empresa, string cadena, string nombre = null)
        {
            try
            {
                List<DataPair2> sucursales = new List<DataPair2>();

                byte[] data = Convert.FromBase64String(cadena);

                // Si quieres convertir los bytes a una cadena original
                cadena = Encoding.UTF8.GetString(data);

                using (SqlConnection connection = new SqlConnection(cadena))
                {
                    string sComp = string.Empty;
                    if (nombre != null)
                    {
                        sComp = $"AND nombre LIKE '%{nombre}%'";
                    }
                    string sQuery = $"SELECT id, nombre FROM [Sucursales] WHERE idEmpresa = '{idEmpresa}' {sComp} ORDER BY nombre";
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(sQuery, connection))
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (reader.Read())
                            {
                                sucursales.Add(new DataPair2()
                                {
                                    value = reader["id"] != DBNull.Value ? reader["id"].ToString() : string.Empty,
                                    name = reader["nombre"] != DBNull.Value ? reader["nombre"].ToString().Trim() : string.Empty
                                });
                            }
                        }
                    }
                    return Ok(sucursales);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                // Retornar un código de error HTTP 500 (Internal Server Error)
                return StatusCode(500, $"Error interno del servidor {ex.Message}");
            }
        }

        [HttpGet("ObtenerPrimerSucursal")]
		public async Task<IActionResult> ObtenerPrimerSucursal(Guid idEmpresa, string nombre, string empresa, string cadena)
		{
			try
			{

				string query = @$"SELECT su.Id, su.Nombre, su.Direccion, su.Ciudad, su.Telefono, su.Numero, su.Correo, su.Pais, su.IdTitular,
                                us.CorreoInstitucional as 'usuario', su.idRazonSocial, rs.nombre AS 'nombreRazonSocial', su.idZona, 
                                zo.Nombre as 'nombreZona', su.idSucursalTipo, suti.Nombre as 'nombreSucursalTipo', su.Notas, su.borrado, 
                                su.fecha, su.linkimagen, su.idEmpresa 
                                FROM sucursales su 
                                LEFT JOIN Usuarios us ON su.idTitular = us.id 
                                LEFT JOIN RazonesSociales rs ON rs.id = su.idRazonSocial 
                                LEFT JOIN Zonas zo ON zo.id = su.idZona 
                                LEFT JOIN SucursalesTipos suti ON su.idSucursalTipo = suti.id 
                                WHERE su.borrado = 0 AND su.nombre = '{nombre}' and su.idEmpresa = '{idEmpresa}'
                                ORDER BY su.Nombre";

				byte[] data = Convert.FromBase64String(cadena);


				cadena = Encoding.UTF8.GetString(data);

				using (SqlConnection connection = new SqlConnection(cadena))
				{
					await connection.OpenAsync();

					using (SqlCommand command = new SqlCommand(query, connection))
					{
				

						using (SqlDataReader reader = await command.ExecuteReaderAsync())
						{
							var sucursales = new List<SucursalWeb>();

							while (await reader.ReadAsync())
							{
								var sucursal = new SucursalWeb
								{
									Id = reader.GetGuid(reader.GetOrdinal("Id")),
									//IdEmpresa = reader.GetGuid(reader.GetOrdinal("idEmpresa")),
									//Nombre = reader.GetString(reader.GetOrdinal("Nombre")),
									//Direccion = reader.GetString(reader.GetOrdinal("Direccion")),
									//Ciudad = reader.GetString(reader.GetOrdinal("Ciudad")),
									//Telefono = reader.GetString(reader.GetOrdinal("Telefono")),
									//Numero = reader.GetString(reader.GetOrdinal("Numero")),
									//Correo = reader.GetString(reader.GetOrdinal("Correo")),
									//Pais = reader.GetString(reader.GetOrdinal("Pais")),
									//// IdTitular = reader.GetGuid(reader.GetOrdinal("IdTitular")),
									//// usuario = reader.GetString(reader.GetOrdinal("usuario")),
									//IdRazonSocial = reader.GetGuid(reader.GetOrdinal("idRazonSocial")),
									//NombreRzonSocial = reader.GetString(reader.GetOrdinal("nombreRazonSocial")),
									//IdZona = reader.GetGuid(reader.GetOrdinal("idZona")),
									//NombreZona = reader.GetString(reader.GetOrdinal("nombreZona")),
									////IdSucursalTipo = reader.GetGuid(reader.GetOrdinal("idSucursalTipo")),
									//// NombreSucursalTipo = reader.GetString(reader.GetOrdinal("nombreSucursalTipo")),
									//borrado = reader.GetBoolean(reader.GetOrdinal("borrado")),
									//Fecha = reader.GetDateTime(reader.GetOrdinal("fecha")),
									//Notas = reader.GetString(reader.GetOrdinal("Notas")),
									//// LinkImagen = reader.GetString(reader.GetOrdinal("linkimagen"))
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

		[HttpGet("ObtenerSucursal")]
        public async Task<IActionResult> ObtenerSucursal(Guid idEmpresa, Guid id, string empresa, string cadena)
        {
            try
            {
              
                string query = @"SELECT su.Id, su.Nombre, su.Direccion, su.Ciudad, su.Telefono, su.Numero, su.Correo, su.Pais, su.IdTitular,
                                us.CorreoInstitucional as 'usuario', su.idRazonSocial, rs.nombre AS 'nombreRazonSocial', su.idZona, 
                                zo.Nombre as 'nombreZona', su.idSucursalTipo, suti.Nombre as 'nombreSucursalTipo', su.Notas, su.borrado, 
                                su.fecha, su.linkimagen, su.idEmpresa 
                                FROM sucursales su 
                                LEFT JOIN Usuarios us ON su.idTitular = us.id 
                                LEFT JOIN RazonesSociales rs ON rs.id = su.idRazonSocial 
                                LEFT JOIN Zonas zo ON zo.id = su.idZona 
                                LEFT JOIN SucursalesTipos suti ON su.idSucursalTipo = suti.id 
                                WHERE su.borrado = 0 AND su.idEmpresa = @IdEmpresa AND su.id = @Id 
                                ORDER BY su.Nombre";

                byte[] data = Convert.FromBase64String(cadena);


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
                            var sucursales = new List<SucursalWeb>();

                            while (await reader.ReadAsync())
                            {
                                var sucursal = new SucursalWeb
                                {
                                    Id = reader.IsDBNull(reader.GetOrdinal("Id")) ? Guid.Empty : reader.GetGuid(reader.GetOrdinal("Id")),
                                    IdEmpresa = reader.IsDBNull(reader.GetOrdinal("idEmpresa")) ? Guid.Empty : reader.GetGuid(reader.GetOrdinal("idEmpresa")),
                                    Nombre = reader.IsDBNull(reader.GetOrdinal("Nombre")) ? null : reader.GetString(reader.GetOrdinal("Nombre")),
                                    Direccion = reader.IsDBNull(reader.GetOrdinal("Direccion")) ? null : reader.GetString(reader.GetOrdinal("Direccion")),
                                    Ciudad = reader.IsDBNull(reader.GetOrdinal("Ciudad")) ? null : reader.GetString(reader.GetOrdinal("Ciudad")),
                                    Telefono = reader.IsDBNull(reader.GetOrdinal("Telefono")) ? null : reader.GetString(reader.GetOrdinal("Telefono")),
                                    Numero = reader.IsDBNull(reader.GetOrdinal("Numero")) ? null : reader.GetString(reader.GetOrdinal("Numero")),
                                    Correo = reader.IsDBNull(reader.GetOrdinal("Correo")) ? null : reader.GetString(reader.GetOrdinal("Correo")),
                                    Pais = reader.IsDBNull(reader.GetOrdinal("Pais")) ? null : reader.GetString(reader.GetOrdinal("Pais")),
                                    // IdTitular = reader.IsDBNull(reader.GetOrdinal("IdTitular")) ? Guid.Empty : reader.GetGuid(reader.GetOrdinal("IdTitular")),
                                    // usuario = reader.IsDBNull(reader.GetOrdinal("usuario")) ? null : reader.GetString(reader.GetOrdinal("usuario")),
                                    IdRazonSocial = reader.IsDBNull(reader.GetOrdinal("idRazonSocial")) ? Guid.Empty : reader.GetGuid(reader.GetOrdinal("idRazonSocial")),
                                    NombreRzonSocial = reader.IsDBNull(reader.GetOrdinal("nombreRazonSocial")) ? null : reader.GetString(reader.GetOrdinal("nombreRazonSocial")),
                                    IdZona = reader.IsDBNull(reader.GetOrdinal("idZona")) ? Guid.Empty : reader.GetGuid(reader.GetOrdinal("idZona")),
                                    NombreZona = reader.IsDBNull(reader.GetOrdinal("nombreZona")) ? null : reader.GetString(reader.GetOrdinal("nombreZona")),
                                    //IdSucursalTipo = reader.IsDBNull(reader.GetOrdinal("idSucursalTipo")) ? Guid.Empty : reader.GetGuid(reader.GetOrdinal("idSucursalTipo")),
                                    //NombreSucursalTipo = reader.IsDBNull(reader.GetOrdinal("nombreSucursalTipo")) ? null : reader.GetString(reader.GetOrdinal("nombreSucursalTipo")),
                                    borrado = reader.GetBoolean(reader.GetOrdinal("borrado")),
                                    Fecha = reader.GetDateTime(reader.GetOrdinal("fecha")),
                                    Notas = reader.IsDBNull(reader.GetOrdinal("Notas")) ? null : reader.GetString(reader.GetOrdinal("Notas")),
                                    // LinkImagen = reader.IsDBNull(reader.GetOrdinal("linkimagen")) ? null : reader.GetString(reader.GetOrdinal("linkimagen"))

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

        [HttpGet("ObtenerSucursales")]
        public async Task<IActionResult> ObtenerSucursales(Guid idEmpresa, string empresa, string cadena)
        {
            try
            {
               
                string query = @"SELECT Id, IdEmpresa, Nombre, Direccion, Ciudad, Telefono, Numero, Correo, Pais, IdTitular, 
                                IdRazonSocial, IdZona, IdSucursaltipo, borrado, Fecha, Notas, LinkImagen 
                                FROM Sucursales 
                                WHERE  idEmpresa = @IdEmpresa 
                                ORDER BY Nombre";

                byte[] data = Convert.FromBase64String(cadena);


                cadena = Encoding.UTF8.GetString(data);

                using (SqlConnection connection = new SqlConnection(cadena))
				{
                    await connection.OpenAsync();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            var sucursales = new List<Sucursales>();

                            while (await reader.ReadAsync())
                            {
                                var sucursal = new Sucursales
                                {
                                    Id = reader.GetGuid(reader.GetOrdinal("Id")),
                                    IdEmpresa = reader.GetGuid(reader.GetOrdinal("IdEmpresa")),
                                    Nombre = reader.GetString(reader.GetOrdinal("Nombre")),
                                    Direccion = reader.GetString(reader.GetOrdinal("Direccion")),
                                    Ciudad = reader.GetString(reader.GetOrdinal("Ciudad")),
                                    Telefono = reader.GetString(reader.GetOrdinal("Telefono")),
                                    Numero = reader.GetString(reader.GetOrdinal("Numero")),
                                    Correo = reader.GetString(reader.GetOrdinal("Correo")),
                                    Pais = reader.GetString(reader.GetOrdinal("Pais")),
                                    IdTitular = reader.GetGuid(reader.GetOrdinal("IdTitular")),
                                    IdRazonSocial = reader.GetGuid(reader.GetOrdinal("IdRazonSocial")),
                                    IdZona = reader.GetGuid(reader.GetOrdinal("IdZona")),
                                    IdSucursalTipo = reader.GetGuid(reader.GetOrdinal("IdSucursaltipo")),
                                    borrado = reader.GetBoolean(reader.GetOrdinal("borrado")),
                                    Fecha = reader.GetDateTime(reader.GetOrdinal("Fecha")),
                                    Notas = reader.GetString(reader.GetOrdinal("Notas")),
                                    LinkImagen = reader.GetString(reader.GetOrdinal("LinkImagen"))
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

        [HttpGet("ObtenerSucursalesPorUsuario")]
        public async Task<IActionResult> ObtenerSucursalesPorUsuario(Guid idEmpresa, string cadena, string correo)
        {
            try
            {

                string query = @"SELECT s.id AS SucursalId, s.nombre AS SucursalNombre FROM Usuarios u
                                LEFT JOIN Roles r ON u.idRol = r.id
                                LEFT JOIN UsuariosPuestos p ON u.idPuesto = p.id
                                INNER JOIN Sucursales s ON (ISNULL(r.NombreRol, '') = 'SuperAdmin' OR s.id = u.idSucursal OR ISNULL(p.Nombre, '') = 'Supervisor')
                        WHERE (u.CorreoPersonal = @Correo OR u.CorreoInstitucional = @Correo)
                          AND u.idEmpresa = @IdEmpresa
                          AND s.idEmpresa = u.idEmpresa
                          AND ISNULL(s.borrado, 0) = 0
                        ORDER BY s.nombre;";

                byte[] data = Convert.FromBase64String(cadena);


                cadena = Encoding.UTF8.GetString(data);

                using (SqlConnection connection = new SqlConnection(cadena))
                {
                    await connection.OpenAsync();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Correo", correo);
                        command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            var sucursales = new List<Sucursalx>();

                            while (await reader.ReadAsync())
                            {
                                var sucursal = new Sucursalx
                                {
                                    Id = reader.GetGuid(reader.GetOrdinal("SucursalId")),
                                    Nombre = reader.GetString(reader.GetOrdinal("SucursalNombre"))
                                   
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

        [HttpGet("ObtenerSucursalesCompleta")]
        public async Task<IActionResult> ObtenerSucursalesCompleta(Guid idEmpresa,string empresa, string mailUsuario = "", string cadena = "")
        {
            try
            {
                
                string sComp = string.Empty;
                if (!string.IsNullOrEmpty(mailUsuario))
                {
                    sComp = $"AND su.id IN (SELECT idSucursal FROM Usuarios WHERE CorreoPersonal = '{mailUsuario}' OR CorreoInstitucional = '{mailUsuario}')";
                }

                string query = $@"SELECT su.Id, su.Nombre, su.Direccion, su.Ciudad, su.Telefono, su.Numero, su.Correo, su.Pais, su.IdTitular,
                                us.CorreoInstitucional as 'usuario', su.idRazonSocial, rs.nombre AS 'nombreRazonSocial', su.idZona, 
                                zo.Nombre as 'nombreZona', su.idSucursalTipo, suti.Nombre as 'nombreSucursalTipo', su.Notas, su.borrado, 
                                su.fecha, su.linkimagen, su.idEmpresa 
                                FROM sucursales su 
                                LEFT JOIN Usuarios us ON su.idTitular = us.id 
                                LEFT JOIN RazonesSociales rs ON rs.id = su.idRazonSocial 
                                LEFT JOIN Zonas zo ON zo.id = su.idZona 
                                LEFT JOIN SucursalesTipos suti ON su.idSucursalTipo = suti.id 
                                WHERE su.borrado = 0 AND su.idEmpresa = @IdEmpresa {sComp} 
                                ORDER BY su.Nombre";

                byte[] data = Convert.FromBase64String(cadena);


                cadena = Encoding.UTF8.GetString(data);

                using (SqlConnection connection = new SqlConnection(cadena))
				{
                    await connection.OpenAsync();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            var sucursales = new List<SucursalWeb>();

                            while (await reader.ReadAsync())
                            {
                                var sucursal = new SucursalWeb
                                {
                                    Id = reader.IsDBNull(reader.GetOrdinal("Id")) ? Guid.Empty : reader.GetGuid(reader.GetOrdinal("Id")),
                                    IdEmpresa = reader.IsDBNull(reader.GetOrdinal("idEmpresa")) ? Guid.Empty : reader.GetGuid(reader.GetOrdinal("idEmpresa")),
                                    Nombre = reader.IsDBNull(reader.GetOrdinal("Nombre")) ? null : reader.GetString(reader.GetOrdinal("Nombre")),
                                    Direccion = reader.IsDBNull(reader.GetOrdinal("Direccion")) ? null : reader.GetString(reader.GetOrdinal("Direccion")),
                                    Ciudad = reader.IsDBNull(reader.GetOrdinal("Ciudad")) ? null : reader.GetString(reader.GetOrdinal("Ciudad")),
                                    Telefono = reader.IsDBNull(reader.GetOrdinal("Telefono")) ? null : reader.GetString(reader.GetOrdinal("Telefono")),
                                    Numero = reader.IsDBNull(reader.GetOrdinal("Numero")) ? null : reader.GetString(reader.GetOrdinal("Numero")),
                                    Correo = reader.IsDBNull(reader.GetOrdinal("Correo")) ? null : reader.GetString(reader.GetOrdinal("Correo")),
                                    Pais = reader.IsDBNull(reader.GetOrdinal("Pais")) ? null : reader.GetString(reader.GetOrdinal("Pais")),
                                    IdRazonSocial = reader.IsDBNull(reader.GetOrdinal("idRazonSocial")) ? Guid.Empty : reader.GetGuid(reader.GetOrdinal("idRazonSocial")),
                                    NombreRzonSocial = reader.IsDBNull(reader.GetOrdinal("nombreRazonSocial")) ? null : reader.GetString(reader.GetOrdinal("nombreRazonSocial")),
                                    IdZona = reader.IsDBNull(reader.GetOrdinal("idZona")) ? Guid.Empty : reader.GetGuid(reader.GetOrdinal("idZona")),
                                    NombreZona = reader.IsDBNull(reader.GetOrdinal("nombreZona")) ? null : reader.GetString(reader.GetOrdinal("nombreZona")),
                                    borrado = reader.GetBoolean(reader.GetOrdinal("borrado")),
                                    Fecha = reader.GetDateTime(reader.GetOrdinal("fecha")),
                                    Notas = reader.IsDBNull(reader.GetOrdinal("Notas")) ? null : reader.GetString(reader.GetOrdinal("Notas")),

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

        [HttpPut("ActualizarSucursal")]
        public async Task<IActionResult> ActualizarSucursal(Guid id, [FromBody] Sucursales sucursal, string empresa, string cadena)
        {
         

            try
            {
               
                string query = @"UPDATE Sucursales 
                         SET Nombre = @Nombre, 
                             Direccion = @Direccion, 
                             Ciudad = @Ciudad, 
                             Telefono = @Telefono, 
                             Numero = @Numero, 
                             Correo = @Correo, 
                             Pais = @Pais, 
                             IdTitular = @IdTitular, 
                             IdRazonSocial = @IdRazonSocial, 
                             IdZona = @IdZona, 
                             IdSucursalTipo = @IdSucursalTipo, 
                             Notas = @Notas, 
                             LinkImagen = @LinkImagen 
                         WHERE Id = @Id";

                byte[] data = Convert.FromBase64String(cadena);


                cadena = Encoding.UTF8.GetString(data);

                using (SqlConnection connection = new SqlConnection(cadena))
				{
                    await connection.OpenAsync();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        command.Parameters.AddWithValue("@Nombre", sucursal.Nombre);
                        command.Parameters.AddWithValue("@Direccion", sucursal.Direccion);
                        command.Parameters.AddWithValue("@Ciudad", sucursal.Ciudad);
                        command.Parameters.AddWithValue("@Telefono", sucursal.Telefono);
                        command.Parameters.AddWithValue("@Numero", sucursal.Numero);
                        command.Parameters.AddWithValue("@Correo", sucursal.Correo);
                        command.Parameters.AddWithValue("@Pais", sucursal.Pais);
                        command.Parameters.AddWithValue("@IdTitular", sucursal.IdTitular);
                        command.Parameters.AddWithValue("@IdRazonSocial", sucursal.IdRazonSocial);
                        command.Parameters.AddWithValue("@IdZona", sucursal.IdZona);
                        command.Parameters.AddWithValue("@IdSucursalTipo", sucursal.IdSucursalTipo);
                        command.Parameters.AddWithValue("@Notas", sucursal.Notas);
                        command.Parameters.AddWithValue("@LinkImagen", sucursal.LinkImagen);

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

        [HttpDelete("EliminarSucursal")]
        public async Task<IActionResult> EliminarSucursal(Guid id, string empresa, string cadena)
        {
            try
            {
              
                string query = @"DELETE FROM Sucursales WHERE Id = @Id";

                byte[] data = Convert.FromBase64String(cadena);


                cadena = Encoding.UTF8.GetString(data);

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

        [HttpPost("InsertarSucursal")]
        public async Task<IActionResult> InsertarSucursal([FromBody] Sucursales nuevaSucursal, string empresa, string cadena)
        {
            try
            {
                Guid idSucursal = nuevaSucursal.Id ?? Guid.NewGuid();
                string query = @"INSERT INTO Sucursales ( Id, IdEmpresa, Nombre, Direccion, Ciudad, Telefono, Numero, Correo, Pais, IdTitular, IdRazonSocial, IdZona, IdSucursaltipo, borrado, Fecha, Notas, LinkImagen)
            VALUES 
            ( @Id, @IdEmpresa, @Nombre, @Direccion, @Ciudad, @Telefono, '0', @Correo, @Pais, newid(), @IdRazonSocial, @IdZona, newid(), 0, GETDATE(), @Notas, 'N/A')";

                byte[] data = Convert.FromBase64String(cadena);


                cadena = Encoding.UTF8.GetString(data);

                using (SqlConnection connection = new SqlConnection(cadena))
				{
                    await connection.OpenAsync();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", idSucursal);
                        command.Parameters.AddWithValue("@IdEmpresa", nuevaSucursal.IdEmpresa);
                        command.Parameters.AddWithValue("@Nombre", nuevaSucursal.Nombre);
                        command.Parameters.AddWithValue("@Direccion", nuevaSucursal.Direccion);
                        command.Parameters.AddWithValue("@Ciudad", nuevaSucursal.Ciudad);
                        command.Parameters.AddWithValue("@Telefono", nuevaSucursal.Telefono);
                        //command.Parameters.AddWithValue("@Numero", nuevaSucursal.Numero);
                        command.Parameters.AddWithValue("@Correo", nuevaSucursal.Correo);
                        command.Parameters.AddWithValue("@Pais", nuevaSucursal.Pais);
                        //command.Parameters.AddWithValue("@IdTitular", nuevaSucursal.IdTitular);
                        command.Parameters.AddWithValue("@IdRazonSocial", nuevaSucursal.IdRazonSocial);
                        command.Parameters.AddWithValue("@IdZona", nuevaSucursal.IdZona);
                        //command.Parameters.AddWithValue("@IdSucursalTipo", nuevaSucursal.IdSucursalTipo);
                        command.Parameters.AddWithValue("@borrado", nuevaSucursal.borrado);
                        command.Parameters.AddWithValue("@Fecha", nuevaSucursal.Fecha);
                        command.Parameters.AddWithValue("@Notas", nuevaSucursal.Notas);
                       // command.Parameters.AddWithValue("@LinkImagen", nuevaSucursal.LinkImagen);

                        await command.ExecuteNonQueryAsync();
                    }
                }

                return Ok("Sucursal insertada con éxito.");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return StatusCode(500, $"Error interno del servidor: {e.Message}");
            }
        }
    }
}
