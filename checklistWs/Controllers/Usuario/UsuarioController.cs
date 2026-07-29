using System.Data.SqlClient;
using System.Text;
using checklistWs.Models.Usuario;
using checklistWs.Utiles;
using Microsoft.AspNetCore.Mvc;

namespace checklistWs.Controllers.Usuario
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuarioController : ControllerBase
    {

        private readonly IConfiguration _configuration;
        private readonly SqlConnectionFactory _connectionFactory;

        public UsuarioController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionFactory = new SqlConnectionFactory(configuration);
        }


        [HttpGet]
        [Route("ObtenerUsuario")]
        public async Task<IActionResult> ObtenerUsuario(Guid idEmpresa, Guid id, string empresa, string cadena)
        {
            try
            {
                List<UsuarioWeb> usuarios = new List<UsuarioWeb>();

                byte[] data = Convert.FromBase64String(cadena);

                // Si quieres convertir los bytes a una cadena original
                cadena = Encoding.UTF8.GetString(data);

                using (SqlConnection connection = new SqlConnection(cadena))
                {
                    connection.Open();

                    string query = $"SELECT us.Id, us.Nombre, us.apellidoPaterno, us.apellidoMaterno, us.FechaNacimiento, us.Numero, us.telefonoMovil, us.telefonoCasa, us.CorreoInstitucional, us.CorreoPersonal, us.idSucursal, su.nombre as 'nombreSucursal', us.idDepartamento, de.Nombre as 'nombreDepartamento', us.idPuesto, pu.Nombre as 'nombrePuesto', us.Estado, us.FechaIngreso, us.Estatus, us.notas, us.borrado, us.fechaAlta, us.FotoLink, us.idFirebase, us.idEmpresa, r.NombreRol as 'nombreRol', r.id as 'idRol' FROM Usuarios us  LEFT JOIN Sucursales su ON us.idSucursal = su.id LEFT JOIN UsuariosDepartamentos de ON us.idDepartamento = de.id LEFT JOIN UsuariosPuestos pu ON us.idPuesto = pu.id LEFT JOIN Roles r ON us.idRol = r.id WHERE us.idEmpresa = '{idEmpresa}' AND us.id = '{id}' ORDER BY Nombre";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                UsuarioWeb usuario = new UsuarioWeb
                                {
                                    Id = reader["Id"] != DBNull.Value ? Guid.Parse(reader["Id"].ToString()) : Guid.Empty,
                                    Nombre = reader["Nombre"] != DBNull.Value ? reader["Nombre"].ToString() : string.Empty,
                                    // ApellidoPaterno = reader["IdApellidoPaterno"] != DBNull.Value ? Guid.Parse(reader["IdApellidoPaterno"].ToString()) : Guid.Empty,
                                    APaterno = reader["apellidoPaterno"] != DBNull.Value ? reader["apellidoPaterno"].ToString() : string.Empty,
                                    IdEmpresa = reader["IdEmpresa"] != DBNull.Value ? Guid.Parse(reader["IdEmpresa"].ToString()) : Guid.Empty,
                                    //  ApellidoMaterno = reader["IdApellidoMaterno"] != DBNull.Value ? Guid.Parse(reader["IdApellidoMaterno"].ToString()) : Guid.Empty,
                                    AMaterno = reader["apellidoMaterno"] != DBNull.Value ? reader["apellidoMaterno"].ToString() : string.Empty,
                                    FechaNacimiento = reader["FechaNacimiento"] != DBNull.Value ? Convert.ToDateTime(reader["FechaNacimiento"]) : DateTime.MinValue,
                                    Numero = reader["Numero"] != DBNull.Value ? reader["Numero"].ToString() : string.Empty,
                                    TelefonoMovil = reader["TelefonoMovil"] != DBNull.Value ? reader["TelefonoMovil"].ToString() : string.Empty,
                                    TelefonoFijo = reader["TelefonoCasa"] != DBNull.Value ? reader["TelefonoCasa"].ToString() : string.Empty,
                                    CorreoInstitucional = reader["CorreoInstitucional"] != DBNull.Value ? reader["CorreoInstitucional"].ToString() : string.Empty,
                                    CorreoPersonal = reader["CorreoPersonal"] != DBNull.Value ? reader["CorreoPersonal"].ToString() : string.Empty,
                                    IdSucursal = reader["IdSucursal"] != DBNull.Value ? (Guid?)reader["IdSucursal"] : Guid.Empty,
                                    NombreSucursal = reader["nombreSucursal"] != DBNull.Value ? reader["nombreSucursal"].ToString() : string.Empty,
                                    IdDepartamento = reader["IdDepartamento"] != DBNull.Value ? (Guid?)reader["IdDepartamento"] : Guid.Empty,
                                    NombreDepartamento = reader["nombreDepartamento"] != DBNull.Value ? reader["nombreDepartamento"].ToString() : string.Empty,
                                    IdPuesto = reader["IdPuesto"] != DBNull.Value ? (Guid?)reader["IdPuesto"] : Guid.Empty,
                                    NombrePuesto = reader["nombrePuesto"] != DBNull.Value ? reader["nombrePuesto"].ToString() : string.Empty,
                                    Estado = reader["Estado"] != DBNull.Value ? bool.Parse(reader["Estado"].ToString()) : false,
                                    FechaIngreso = reader["FechaIngreso"] != DBNull.Value ? Convert.ToDateTime(reader["FechaIngreso"]) : DateTime.MinValue,
                                    Estatus = reader["Estatus"] != DBNull.Value ? bool.Parse(reader["Estatus"].ToString()) : false,
                                    Notas = reader["Notas"] != DBNull.Value ? reader["Notas"].ToString() : string.Empty,
                                    FotoLink = reader["FotoLink"] != DBNull.Value ? reader["FotoLink"].ToString() : string.Empty,
                                    borrado = reader["borrado"] != DBNull.Value ? bool.Parse(reader["borrado"].ToString()) : false,
                                    FechaAlta = reader["FechaAlta"] != DBNull.Value ? Convert.ToDateTime(reader["FechaAlta"].ToString()) : DateTime.MinValue,
                                    IdFirebase = reader["IdFirebase"] != DBNull.Value ? reader["IdFirebase"].ToString() : string.Empty,
                                    idRol = reader["idRol"] != DBNull.Value ? (Guid?)reader["idRol"] : Guid.Empty,
                                    NombreRol = reader["NombreRol"] != DBNull.Value ? reader["NombreRol"].ToString() : string.Empty
                                };

                                usuarios.Add(usuario);
                            }
                        }
                    }
                }

                return Ok(usuarios);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return StatusCode(500, $"Error interno del servidor: {e.Message}");
            }
        }

        [HttpGet]
        [Route("ObtenerSuperAdminId")]
        public async Task<IActionResult> ObtenerSuperAdminId(Guid idEmpresa, string cadena)
        {
            try
            {
                Guid idSadmin = Guid.Empty;
                byte[] data = Convert.FromBase64String(cadena);
                cadena = Encoding.UTF8.GetString(data);
                string sQuery = $"SELECT top 1 id, fechaAlta FROM Usuarios WHERE idEmpresa = '{idEmpresa}' GROUP BY id, fechaAlta ORDER BY fechaAlta";
                using (SqlConnection connection = new SqlConnection(cadena))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(sQuery, connection))
                    {
                        using (SqlDataReader dr = await command.ExecuteReaderAsync())
                        {
                            while (dr.Read())
                            {
                                idSadmin = dr["id"] != DBNull.Value ? Guid.Parse(dr["id"].ToString()) : Guid.Empty;
                            }
                        }
                    }
                }
                return Ok(idSadmin);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return StatusCode(500, $"Error interno del servidor: {e.Message}");
            }
        }

        [HttpGet]
        [Route("ObtenerUsuarioPorEmail")]
        public async Task<IActionResult> ObtenerUsuarioPorEmailO(Guid idEmpresa, string email, string empresa, string cadena)
        {
            try
            {
                List<UsuarioWeb> usuarios = new List<UsuarioWeb>();
                byte[] data = Convert.FromBase64String(cadena);
                cadena = Encoding.UTF8.GetString(data);
                using (SqlConnection connection = new SqlConnection(cadena))
                {
                    connection.Open();
                    string query = string.Empty;
                    if (email != "soporte@secuencia.com")
                    {
                        query = $"SELECT * FROM Usuarios where CorreoInstitucional = '{email}' AND idEmpresa = '{idEmpresa}'";
                    }
                    else
                    {
                        query = $"SELECT TOP 1 * FROM Usuarios u WHERE u.idRol = (SELECT id FROM Roles r WHERE r.idEmpresa = '{idEmpresa}' AND r.NombreRol = 'SuperAdmin') ORDER BY u.fechaAlta ASC";
                    }
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (reader.Read())
                            {
                                UsuarioWeb usuario = new UsuarioWeb
                                {
                                    Id = reader["Id"] != DBNull.Value ? Guid.Parse(reader["Id"].ToString()) : Guid.Empty,
                                    Nombre = reader["Nombre"] != DBNull.Value ? reader["Nombre"].ToString() : string.Empty,
                                    //  ApellidoPaterno = reader["IdApellidoPaterno"] != DBNull.Value ? Guid.Parse(reader["IdApellidoPaterno"].ToString()) : Guid.Empty,
                                    // APaterno = reader["ApellidoPaterno"] != DBNull.Value ? reader["ApellidoPaterno"].ToString() : string.Empty,
                                    // IdEmpresa = reader["IdEmpresa"] != DBNull.Value ? Guid.Parse(reader["IdEmpresa"].ToString()) : Guid.Empty,
                                    //ApellidoMaterno = reader["IdApellidoMaterno"] != DBNull.Value ? Guid.Parse(reader["IdApellidoMaterno"].ToString()) : Guid.Empty,
                                    // AMaterno = reader["ApellidoMaterno"] != DBNull.Value ? reader["ApellidoMaterno"].ToString() : string.Empty,
                                    // FechaNacimiento = reader["FechaNacimiento"] != DBNull.Value ? Convert.ToDateTime(reader["FechaNacimiento"]) : DateTime.MinValue,
                                    // Numero = reader["Numero"] != DBNull.Value ? reader["Numero"].ToString() : string.Empty,
                                    // TelefonoMovil = reader["TelefonoMovil"] != DBNull.Value ? reader["TelefonoMovil"].ToString() : string.Empty,
                                    //TelefonoFijo = reader["TelefonoCasa"] != DBNull.Value ? reader["TelefonoCasa"].ToString() : string.Empty,
                                    //CorreoInstitucional = reader["CorreoInstitucional"] != DBNull.Value ? reader["CorreoInstitucional"].ToString() : string.Empty,
                                    //CorreoPersonal = reader["CorreoPersonal"] != DBNull.Value ? reader["CorreoPersonal"].ToString() : string.Empty,
                                    //IdSucursal = reader["IdSucursal"] != DBNull.Value ? (Guid?)reader["IdSucursal"] : Guid.Empty,
                                    // NombreSucursal = reader["nombreSucursal"] != DBNull.Value ? reader["nombreSucursal"].ToString() : string.Empty,
                                    // IdDepartamento = reader["IdDepartamento"] != DBNull.Value ? (Guid?)reader["IdDepartamento"] : Guid.Empty,
                                    // NombreDepartamento = reader["nombreDepartamento"] != DBNull.Value ? reader["nombreDepartamento"].ToString() : string.Empty,
                                    // IdPuesto = reader["IdPuesto"] != DBNull.Value ? (Guid?)reader["IdPuesto"] : Guid.Empty,
                                    // NombrePuesto = reader["nombrePuesto"] != DBNull.Value ? reader["nombrePuesto"].ToString() : string.Empty,
                                    //Estado = reader["Estado"] != DBNull.Value ? bool.Parse(reader["Estado"].ToString()) : false,
                                    //FechaIngreso = reader["FechaIngreso"] != DBNull.Value ? Convert.ToDateTime(reader["FechaIngreso"]) : DateTime.MinValue,
                                    //Estatus = reader["Estatus"] != DBNull.Value ? bool.Parse(reader["Estatus"].ToString()) : false,
                                    //Notas = reader["Notas"] != DBNull.Value ? reader["Notas"].ToString() : string.Empty,
                                    //FotoLink = reader["FotoLink"] != DBNull.Value ? reader["FotoLink"].ToString() : string.Empty,
                                    //borrado = reader["borrado"] != DBNull.Value ? bool.Parse(reader["borrado"].ToString()) : false,
                                    //FechaAlta = reader["FechaAlta"] != DBNull.Value ? Convert.ToDateTime(reader["FechaAlta"].ToString()) : DateTime.MinValue,
                                    //IdFirebase = reader["IdFirebase"] != DBNull.Value ? reader["IdFirebase"].ToString() : string.Empty,
                                    //   idRol = reader["idRol"] != DBNull.Value ? (Guid?)reader["idRol"] : Guid.Empty,
                                    //  NombreRol = reader["NombreRol"] != DBNull.Value ? reader["NombreRol"].ToString() : string.Empty
                                };

                                usuarios.Add(usuario);
                            }
                        }
                    }
                }

                return Ok(usuarios);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return StatusCode(500, $"Error interno del servidor: {e.Message}");
            }
        }

        [HttpGet]
        [Route("ObtenerUsuariosCompleto")]
        public async Task<IActionResult> ObtenerUsuariosCompleto(Guid idEmpresa, string empresa, string cadena)
        {
            try
            {
                List<UsuarioWeb> usuarios = new List<UsuarioWeb>();


                byte[] data = Convert.FromBase64String(cadena);

                // Si quieres convertir los bytes a una cadena original
                cadena = Encoding.UTF8.GetString(data);

                using (SqlConnection connection = new SqlConnection(cadena))
                {
                    connection.Open();

                    string query = $@"SELECT us.Id, us.Nombre, us.apellidoPaterno, us.apellidoMaterno, us.FechaNacimiento, us.Numero, us.telefonoMovil, us.telefonoCasa, us.CorreoInstitucional, us.CorreoPersonal, us.idSucursal, su.nombre as 'nombreSucursal', us.idDepartamento, de.Nombre as 'nombreDepartamento', us.idPuesto, pu.Nombre as 'nombrePuesto', us.Estado, us.FechaIngreso, us.Estatus, us.notas, us.borrado, us.fechaAlta, us.FotoLink, us.idFirebase, us.idEmpresa, r.NombreRol as 'nombreRol', r.id as 'idRol' FROM Usuarios us  LEFT JOIN Sucursales su ON us.idSucursal = su.id LEFT JOIN UsuariosDepartamentos de ON us.idDepartamento = de.id LEFT JOIN UsuariosPuestos pu ON us.idPuesto = pu.id LEFT JOIN Roles r ON us.idRol = r.id WHERE us.idEmpresa = '{idEmpresa}' ORDER BY us.Nombre";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                UsuarioWeb usuario = new UsuarioWeb
                                {
                                    Id = reader["Id"] != DBNull.Value ? Guid.Parse(reader["Id"].ToString()) : Guid.Empty,
                                    Nombre = reader["Nombre"] != DBNull.Value ? reader["Nombre"].ToString() : string.Empty,
                                    // ApellidoPaterno = reader["IdApellidoPaterno"] != DBNull.Value ? Guid.Parse(reader["IdApellidoPaterno"].ToString()) : Guid.Empty,
                                    APaterno = reader["ApellidoPaterno"] != DBNull.Value ? reader["ApellidoPaterno"].ToString() : string.Empty,
                                    IdEmpresa = reader["IdEmpresa"] != DBNull.Value ? Guid.Parse(reader["IdEmpresa"].ToString()) : Guid.Empty,
                                    // ApellidoMaterno = reader["IdApellidoMaterno"] != DBNull.Value ? Guid.Parse(reader["IdApellidoMaterno"].ToString()) : Guid.Empty,
                                    AMaterno = reader["ApellidoMaterno"] != DBNull.Value ? reader["ApellidoMaterno"].ToString() : string.Empty,
                                    FechaNacimiento = reader["FechaNacimiento"] != DBNull.Value ? Convert.ToDateTime(reader["FechaNacimiento"]) : DateTime.MinValue,
                                    Numero = reader["Numero"] != DBNull.Value ? reader["Numero"].ToString() : string.Empty,
                                    TelefonoMovil = reader["TelefonoMovil"] != DBNull.Value ? reader["TelefonoMovil"].ToString() : string.Empty,
                                    TelefonoFijo = reader["TelefonoCasa"] != DBNull.Value ? reader["TelefonoCasa"].ToString() : string.Empty,
                                    CorreoInstitucional = reader["CorreoInstitucional"] != DBNull.Value ? reader["CorreoInstitucional"].ToString() : string.Empty,
                                    CorreoPersonal = reader["CorreoPersonal"] != DBNull.Value ? reader["CorreoPersonal"].ToString() : string.Empty,
                                    IdSucursal = reader["IdSucursal"] != DBNull.Value ? (Guid?)reader["IdSucursal"] : Guid.Empty,
                                    NombreSucursal = reader["nombreSucursal"] != DBNull.Value ? reader["nombreSucursal"].ToString() : string.Empty,
                                    IdDepartamento = reader["IdDepartamento"] != DBNull.Value ? (Guid?)reader["IdDepartamento"] : Guid.Empty,
                                    NombreDepartamento = reader["nombreDepartamento"] != DBNull.Value ? reader["nombreDepartamento"].ToString() : string.Empty,
                                    IdPuesto = reader["IdPuesto"] != DBNull.Value ? (Guid?)reader["IdPuesto"] : Guid.Empty,
                                    NombrePuesto = reader["nombrePuesto"] != DBNull.Value ? reader["nombrePuesto"].ToString() : string.Empty,
                                    Estado = reader["Estado"] != DBNull.Value ? bool.Parse(reader["Estado"].ToString()) : false,
                                    FechaIngreso = reader["FechaIngreso"] != DBNull.Value ? Convert.ToDateTime(reader["FechaIngreso"]) : DateTime.MinValue,
                                    Estatus = reader["Estatus"] != DBNull.Value ? bool.Parse(reader["Estatus"].ToString()) : false,
                                    Notas = reader["Notas"] != DBNull.Value ? reader["Notas"].ToString() : string.Empty,
                                    FotoLink = reader["FotoLink"] != DBNull.Value ? reader["FotoLink"].ToString() : string.Empty,
                                    borrado = reader["borrado"] != DBNull.Value ? bool.Parse(reader["borrado"].ToString()) : false,
                                    FechaAlta = reader["FechaAlta"] != DBNull.Value ? Convert.ToDateTime(reader["FechaAlta"].ToString()) : DateTime.MinValue,
                                    IdFirebase = reader["IdFirebase"] != DBNull.Value ? reader["IdFirebase"].ToString() : string.Empty,
                                    idRol = reader["idRol"] != DBNull.Value ? (Guid?)reader["idRol"] : Guid.Empty,
                                    NombreRol = reader["NombreRol"] != DBNull.Value ? reader["NombreRol"].ToString() : string.Empty
                                };

                                usuarios.Add(usuario);
                            }
                        }
                    }
                }

                return Ok(usuarios);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return StatusCode(500, $"Error interno del servidor: {e.Message}");
            }
        }


        [HttpGet]
        [Route("ObtenerUsuariosCompletoXSucursal")]
        public async Task<IActionResult> ObtenerUsuariosCompletoXSucursal(Guid idEmpresa, Guid idSucursal, string empresa, string cadena)
        {
            try
            {
                List<UsuarioWeb> usuarios = new List<UsuarioWeb>();


                byte[] data = Convert.FromBase64String(cadena);

                // Si quieres convertir los bytes a una cadena original
                cadena = Encoding.UTF8.GetString(data);

                using (SqlConnection connection = new SqlConnection(cadena))
                {
                    connection.Open();

                    string query = $@"SELECT us.Id, us.Nombre, us.apellidoPaterno, us.apellidoMaterno, us.FechaNacimiento, us.Numero, us.telefonoMovil, us.telefonoCasa, us.CorreoInstitucional, us.CorreoPersonal, us.idSucursal, su.nombre as 'nombreSucursal', us.idDepartamento, de.Nombre as 'nombreDepartamento', us.idPuesto, pu.Nombre as 'nombrePuesto', us.Estado, us.FechaIngreso, us.Estatus, us.notas, us.borrado, us.fechaAlta, us.FotoLink, us.idFirebase, us.idEmpresa, r.NombreRol as 'nombreRol', r.id as 'idRol' FROM Usuarios us  LEFT JOIN Sucursales su ON us.idSucursal = su.id LEFT JOIN UsuariosDepartamentos de ON us.idDepartamento = de.id LEFT JOIN UsuariosPuestos pu ON us.idPuesto = pu.id LEFT JOIN Roles r ON us.idRol = r.id WHERE us.idEmpresa = '{idEmpresa}' AND us.idSucursal = '{idSucursal}' ORDER BY us.Nombre";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                UsuarioWeb usuario = new UsuarioWeb
                                {
                                    Id = reader["Id"] != DBNull.Value ? Guid.Parse(reader["Id"].ToString()) : Guid.Empty,
                                    Nombre = reader["Nombre"] != DBNull.Value ? reader["Nombre"].ToString() : string.Empty,
                                    // ApellidoPaterno = reader["IdApellidoPaterno"] != DBNull.Value ? Guid.Parse(reader["IdApellidoPaterno"].ToString()) : Guid.Empty,
                                    APaterno = reader["ApellidoPaterno"] != DBNull.Value ? reader["ApellidoPaterno"].ToString() : string.Empty,
                                    IdEmpresa = reader["IdEmpresa"] != DBNull.Value ? Guid.Parse(reader["IdEmpresa"].ToString()) : Guid.Empty,
                                    // ApellidoMaterno = reader["IdApellidoMaterno"] != DBNull.Value ? Guid.Parse(reader["IdApellidoMaterno"].ToString()) : Guid.Empty,
                                    AMaterno = reader["ApellidoMaterno"] != DBNull.Value ? reader["ApellidoMaterno"].ToString() : string.Empty,
                                    FechaNacimiento = reader["FechaNacimiento"] != DBNull.Value ? Convert.ToDateTime(reader["FechaNacimiento"]) : DateTime.MinValue,
                                    Numero = reader["Numero"] != DBNull.Value ? reader["Numero"].ToString() : string.Empty,
                                    TelefonoMovil = reader["TelefonoMovil"] != DBNull.Value ? reader["TelefonoMovil"].ToString() : string.Empty,
                                    TelefonoFijo = reader["TelefonoCasa"] != DBNull.Value ? reader["TelefonoCasa"].ToString() : string.Empty,
                                    CorreoInstitucional = reader["CorreoInstitucional"] != DBNull.Value ? reader["CorreoInstitucional"].ToString() : string.Empty,
                                    CorreoPersonal = reader["CorreoPersonal"] != DBNull.Value ? reader["CorreoPersonal"].ToString() : string.Empty,
                                    IdSucursal = reader["IdSucursal"] != DBNull.Value ? (Guid?)reader["IdSucursal"] : Guid.Empty,
                                    NombreSucursal = reader["nombreSucursal"] != DBNull.Value ? reader["nombreSucursal"].ToString() : string.Empty,
                                    IdDepartamento = reader["IdDepartamento"] != DBNull.Value ? (Guid?)reader["IdDepartamento"] : Guid.Empty,
                                    NombreDepartamento = reader["nombreDepartamento"] != DBNull.Value ? reader["nombreDepartamento"].ToString() : string.Empty,
                                    IdPuesto = reader["IdPuesto"] != DBNull.Value ? (Guid?)reader["IdPuesto"] : Guid.Empty,
                                    NombrePuesto = reader["nombrePuesto"] != DBNull.Value ? reader["nombrePuesto"].ToString() : string.Empty,
                                    Estado = reader["Estado"] != DBNull.Value ? bool.Parse(reader["Estado"].ToString()) : false,
                                    FechaIngreso = reader["FechaIngreso"] != DBNull.Value ? Convert.ToDateTime(reader["FechaIngreso"]) : DateTime.MinValue,
                                    Estatus = reader["Estatus"] != DBNull.Value ? bool.Parse(reader["Estatus"].ToString()) : false,
                                    Notas = reader["Notas"] != DBNull.Value ? reader["Notas"].ToString() : string.Empty,
                                    FotoLink = reader["FotoLink"] != DBNull.Value ? reader["FotoLink"].ToString() : string.Empty,
                                    borrado = reader["borrado"] != DBNull.Value ? bool.Parse(reader["borrado"].ToString()) : false,
                                    FechaAlta = reader["FechaAlta"] != DBNull.Value ? Convert.ToDateTime(reader["FechaAlta"].ToString()) : DateTime.MinValue,
                                    IdFirebase = reader["IdFirebase"] != DBNull.Value ? reader["IdFirebase"].ToString() : string.Empty,
                                    idRol = reader["idRol"] != DBNull.Value ? (Guid?)reader["idRol"] : Guid.Empty,
                                    NombreRol = reader["NombreRol"] != DBNull.Value ? reader["NombreRol"].ToString() : string.Empty
                                };

                                usuarios.Add(usuario);
                            }
                        }
                    }
                }

                return Ok(usuarios);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return StatusCode(500, $"Error interno del servidor: {e.Message}");
            }
        }

        [HttpGet]
        [Route("ObtenerUsuarios")]
        public async Task<IActionResult> ObtenerUsuarios(string idEmpresa, string empresa, string cadena)
        {
            try
            {
                List<UsuarioWeb> usuarios = new List<UsuarioWeb>();


                byte[] data = Convert.FromBase64String(cadena);

                // Si quieres convertir los bytes a una cadena original
                cadena = Encoding.UTF8.GetString(data);

                using (SqlConnection connection = new SqlConnection(cadena))
                {
                    connection.Open();

                    string query = @"SELECT us.Id, us.Nombre, us.apellidoPaterno, us.apellidoMaterno, us.FechaNacimiento, us.Numero, us.telefonoMovil, us.telefonoCasa, us.CorreoInstitucional, us.CorreoPersonal, us.idSucursal, su.nombre as 'nombreSucursal', us.idDepartamento, de.Nombre as 'nombreDepartamento', us.idPuesto, pu.Nombre as 'nombrePuesto', us.Estado, us.FechaIngreso, us.Estatus, us.notas, us.borrado, us.fechaAlta, us.FotoLink, us.idFirebase, us.idEmpresa, r.NombreRol as 'nombreRol', r.id as 'idRol' FROM Usuarios us  LEFT JOIN Sucursales su ON us.idSucursal = su.id LEFT JOIN UsuariosDepartamentos de ON us.idDepartamento = de.id LEFT JOIN UsuariosPuestos pu ON us.idPuesto = pu.id LEFT JOIN Roles r ON us.idRol = r.id where us.idEmpresa = '" + idEmpresa + "'  ORDER BY us.Nombre";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                UsuarioWeb usuario = new UsuarioWeb
                                {
                                    Id = reader["Id"] != DBNull.Value ? Guid.Parse(reader["Id"].ToString()) : Guid.Empty,
                                    Nombre = reader["Nombre"] != DBNull.Value ? reader["Nombre"].ToString() : string.Empty,
                                    //ApellidoPaterno = reader["IdApellidoPaterno"] != DBNull.Value ? Guid.Parse(reader["IdApellidoPaterno"].ToString()) : Guid.Empty,
                                    APaterno = reader["ApellidoPaterno"] != DBNull.Value ? reader["ApellidoPaterno"].ToString() : string.Empty,
                                    IdEmpresa = reader["IdEmpresa"] != DBNull.Value ? Guid.Parse(reader["IdEmpresa"].ToString()) : Guid.Empty,
                                    // ApellidoMaterno = reader["IdApellidoMaterno"] != DBNull.Value ? Guid.Parse(reader["IdApellidoMaterno"].ToString()) : Guid.Empty,
                                    AMaterno = reader["ApellidoMaterno"] != DBNull.Value ? reader["ApellidoMaterno"].ToString() : string.Empty,
                                    FechaNacimiento = reader["FechaNacimiento"] != DBNull.Value ? Convert.ToDateTime(reader["FechaNacimiento"]) : DateTime.MinValue,
                                    Numero = reader["Numero"] != DBNull.Value ? reader["Numero"].ToString() : string.Empty,
                                    TelefonoMovil = reader["TelefonoMovil"] != DBNull.Value ? reader["TelefonoMovil"].ToString() : string.Empty,
                                    TelefonoFijo = reader["TelefonoCasa"] != DBNull.Value ? reader["TelefonoCasa"].ToString() : string.Empty,
                                    CorreoInstitucional = reader["CorreoInstitucional"] != DBNull.Value ? reader["CorreoInstitucional"].ToString() : string.Empty,
                                    CorreoPersonal = reader["CorreoPersonal"] != DBNull.Value ? reader["CorreoPersonal"].ToString() : string.Empty,
                                    IdSucursal = reader["IdSucursal"] != DBNull.Value ? (Guid?)reader["IdSucursal"] : Guid.Empty,
                                    NombreSucursal = reader["nombreSucursal"] != DBNull.Value ? reader["nombreSucursal"].ToString() : string.Empty,
                                    IdDepartamento = reader["IdDepartamento"] != DBNull.Value ? (Guid?)reader["IdDepartamento"] : Guid.Empty,
                                    NombreDepartamento = reader["nombreDepartamento"] != DBNull.Value ? reader["nombreDepartamento"].ToString() : string.Empty,
                                    IdPuesto = reader["IdPuesto"] != DBNull.Value ? (Guid?)reader["IdPuesto"] : Guid.Empty,
                                    NombrePuesto = reader["nombrePuesto"] != DBNull.Value ? reader["nombrePuesto"].ToString() : string.Empty,
                                    Estado = reader["Estado"] != DBNull.Value ? bool.Parse(reader["Estado"].ToString()) : false,
                                    FechaIngreso = reader["FechaIngreso"] != DBNull.Value ? Convert.ToDateTime(reader["FechaIngreso"]) : DateTime.MinValue,
                                    Estatus = reader["Estatus"] != DBNull.Value ? bool.Parse(reader["Estatus"].ToString()) : false,
                                    Notas = reader["Notas"] != DBNull.Value ? reader["Notas"].ToString() : string.Empty,
                                    FotoLink = reader["FotoLink"] != DBNull.Value ? reader["FotoLink"].ToString() : string.Empty,
                                    borrado = reader["borrado"] != DBNull.Value ? bool.Parse(reader["borrado"].ToString()) : false,
                                    FechaAlta = reader["FechaAlta"] != DBNull.Value ? Convert.ToDateTime(reader["FechaAlta"].ToString()) : DateTime.MinValue,
                                    IdFirebase = reader["IdFirebase"] != DBNull.Value ? reader["IdFirebase"].ToString() : string.Empty,
                                    idRol = reader["idRol"] != DBNull.Value ? (Guid?)reader["idRol"] : Guid.Empty,
                                    NombreRol = reader["NombreRol"] != DBNull.Value ? reader["NombreRol"].ToString() : string.Empty
                                };

                                usuarios.Add(usuario);
                            }
                        }
                    }
                }

                return Ok(usuarios);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return StatusCode(500, $"Error interno del servidor: {e.Message}");
            }
        }

        [HttpPost]
        [Route("InsertarUsuario")]
        public async Task<IActionResult> InsertarUsuario(Usuarios usuario, string empresa, string cadena)
        {
            try
            {

                byte[] data = Convert.FromBase64String(cadena);

                // Si quieres convertir los bytes a una cadena original
                cadena = Encoding.UTF8.GetString(data);

                using (SqlConnection connection = new SqlConnection(cadena))
                {
                    connection.Open();

                    string query = @"INSERT INTO Usuarios (Nombre, ApellidoPaterno, ApellidoMaterno, FechaNacimiento, Numero, TelefonoMovil, TelefonoCasa, CorreoInstitucional, CorreoPersonal, IdSucursal, IdDepartamento, IdPuesto, Estado, Estatus, Notas, borrado, FotoLink, idEmpresa, idFirebase, idRol) 
                                     VALUES (@Nombre, @ApellidoPaterno, @ApellidoMaterno, @FechaNacimiento, @Numero, @TelefonoMovil, @TelefonoFijo, @CorreoInstitucional, @CorreoPersonal, @IdSucursal, @IdDepartamento, @IdPuesto, @Estado, @Estatus, @Notas, @Borrado, @FotoLink, @IdEmpresa, @idFireBase, @idRol)";
                    SqlCommand command = new SqlCommand(query, connection);
                    if (!string.IsNullOrEmpty(usuario.Nombre)) command.Parameters.AddWithValue("@Nombre", usuario.Nombre); else command.Parameters.AddWithValue("@Nombre", DBNull.Value);
                    if (usuario.APaterno != null) command.Parameters.AddWithValue("@ApellidoPaterno", usuario.APaterno); else command.Parameters.AddWithValue("@ApellidoPaterno", DBNull.Value);
                    if (usuario.AMaterno != null) command.Parameters.AddWithValue("@ApellidoMaterno", usuario.AMaterno); else command.Parameters.AddWithValue("@ApellidoMaterno", DBNull.Value);
                    if (usuario.FechaNacimiento != null) command.Parameters.AddWithValue("@FechaNacimiento", usuario.FechaNacimiento); else command.Parameters.AddWithValue("@FechaNacimiento", DBNull.Value);
                    if (!string.IsNullOrEmpty(usuario.Numero)) command.Parameters.AddWithValue("@Numero", usuario.Numero); else command.Parameters.AddWithValue("@Numero", DBNull.Value);
                    if (!string.IsNullOrEmpty(usuario.TelefonoMovil)) command.Parameters.AddWithValue("@TelefonoMovil", usuario.TelefonoMovil); else command.Parameters.AddWithValue("@TelefonoMovil", DBNull.Value);
                    if (!string.IsNullOrEmpty(usuario.TelefonoFijo)) command.Parameters.AddWithValue("@TelefonoFijo", usuario.TelefonoFijo); else command.Parameters.AddWithValue("@TelefonoFijo", DBNull.Value);
                    if (!string.IsNullOrEmpty(usuario.CorreoInstitucional)) command.Parameters.AddWithValue("@CorreoInstitucional", usuario.CorreoInstitucional); else command.Parameters.AddWithValue("@CorreoInstitucional", DBNull.Value);
                    if (!string.IsNullOrEmpty(usuario.CorreoPersonal)) command.Parameters.AddWithValue("@CorreoPersonal", usuario.CorreoPersonal); else command.Parameters.AddWithValue("@CorreoPersonal", DBNull.Value);
                    if (usuario.IdSucursal != null) command.Parameters.AddWithValue("@IdSucursal", usuario.IdSucursal); else command.Parameters.AddWithValue("@IdSucursal", DBNull.Value);
                    if (usuario.IdDepartamento != null) command.Parameters.AddWithValue("@IdDepartamento", usuario.IdDepartamento); else command.Parameters.AddWithValue("@IdDepartamento", DBNull.Value);
                    if (usuario.IdPuesto != null) command.Parameters.AddWithValue("@IdPuesto", usuario.IdPuesto); else command.Parameters.AddWithValue("@IdPuesto", DBNull.Value);
                    if (usuario.Estado != null) command.Parameters.AddWithValue("@Estado", usuario.Estado); else command.Parameters.AddWithValue("@Estado", DBNull.Value);
                    //if (usuario.FechaIngreso != null) command.Parameters.AddWithValue("@FechaIngreso", usuario.FechaIngreso); else command.Parameters.AddWithValue("@FechaIngreso", DBNull.Value);
                    if (usuario.Estatus != null) command.Parameters.AddWithValue("@Estatus", usuario.Estatus); else command.Parameters.AddWithValue("@Estatus", DBNull.Value);
                    if (usuario.IdEmpresa != null) command.Parameters.AddWithValue("@IdEmpresa", usuario.IdEmpresa); else command.Parameters.AddWithValue("@IdEmpresa", DBNull.Value);
                    if (!string.IsNullOrEmpty(usuario.Notas)) command.Parameters.AddWithValue("@Notas", usuario.Notas); else command.Parameters.AddWithValue("@Notas", DBNull.Value);
                    if (usuario.borrado != null) command.Parameters.AddWithValue("@Borrado", usuario.borrado); else command.Parameters.AddWithValue("@borrado", DBNull.Value);
                    //if (usuario.FechaAlta != null) command.Parameters.AddWithValue("@FechaAlta", usuario.FechaAlta); else command.Parameters.AddWithValue("@FechaAlta", DBNull.Value);
                    if (!string.IsNullOrEmpty(usuario.FotoLink)) command.Parameters.AddWithValue("@FotoLink", usuario.FotoLink); else command.Parameters.AddWithValue("@FotoLink", DBNull.Value);
                    if (!string.IsNullOrEmpty(usuario.IdFirebase)) command.Parameters.AddWithValue("@idFireBase", usuario.IdFirebase); else command.Parameters.AddWithValue("@idFireBase", DBNull.Value);
                    if (usuario.idRol != null) command.Parameters.AddWithValue("@idRol", usuario.idRol); else command.Parameters.AddWithValue("@idRol", DBNull.Value);
                    command.ExecuteNonQuery();
                }

                return Ok("Usuario insertado correctamente");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return StatusCode(500, $"Error interno del servidor: {e.Message}");
            }
        }

        [HttpPut]
        [Route("ActualizarUsuario")]
        public async Task<IActionResult> ActualizarUsuario(Usuarios usuario, Guid idEmpresa, string empresa, string cadena)
        {
            try
            {
                byte[] data = Convert.FromBase64String(cadena);
                cadena = Encoding.UTF8.GetString(data);

                // id superadmin
                Guid idSadmin = Guid.Empty;
                Guid tmpRolSadmin = Guid.Empty;
                string sQuery = $"SELECT top 1 id, fechaAlta, idRol FROM Usuarios WHERE idEmpresa = '{idEmpresa}' GROUP BY id, fechaAlta, idRol ORDER BY fechaAlta";
                using (SqlConnection connection = new SqlConnection(cadena))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(sQuery, connection))
                    {
                        using (SqlDataReader dr = await command.ExecuteReaderAsync())
                        {
                            while (dr.Read())
                            {
                                idSadmin = dr["id"] != DBNull.Value ? Guid.Parse(dr["id"].ToString()) : Guid.Empty;
                                tmpRolSadmin = dr["idRol"] != DBNull.Value ? Guid.Parse(dr["idRol"].ToString()) : Guid.Empty;
                            }
                        }
                    }
                }

                // proceso normal
                using (SqlConnection connection = new SqlConnection(cadena))
                {
                    connection.Open();
                    string query = string.Empty;
                    if (usuario.Id == idSadmin) { usuario.idRol = tmpRolSadmin; }
                    query = $"UPDATE Usuarios SET Nombre = @Nombre, ApellidoPaterno = @ApellidoPaterno, ApellidoMaterno = @ApellidoMaterno, FechaNacimiento = @FechaNacimiento, Numero = @Numero, TelefonoMovil = @TelefonoMovil, TelefonoCasa = @TelefonoFijo, CorreoInstitucional = @CorreoInstitucional, CorreoPersonal = @CorreoPersonal, IdSucursal = @IdSucursal, IdDepartamento = @IdDepartamento, IdPuesto = @IdPuesto, Estado = @Estado, Estatus = @Estatus, FotoLink = @FotoLink, Notas = @Notas, idRol = @idRol WHERE Id = @Id AND idEmpresa = '{idEmpresa}'";
                    SqlCommand command = new SqlCommand(query, connection);
                    if (!string.IsNullOrEmpty(usuario.Nombre)) command.Parameters.AddWithValue("@Nombre", usuario.Nombre); else command.Parameters.AddWithValue("@Nombre", DBNull.Value);
                    if (usuario.APaterno != null) command.Parameters.AddWithValue("@ApellidoPaterno", usuario.APaterno); else command.Parameters.AddWithValue("@ApellidoPaterno", DBNull.Value);
                    if (usuario.AMaterno != null) command.Parameters.AddWithValue("@ApellidoMaterno", usuario.AMaterno); else command.Parameters.AddWithValue("@ApellidoMaterno", DBNull.Value);
                    if (usuario.FechaNacimiento != null) command.Parameters.AddWithValue("@FechaNacimiento", usuario.FechaNacimiento); else command.Parameters.AddWithValue("@FechaNacimiento", DBNull.Value);
                    if (!string.IsNullOrEmpty(usuario.Numero)) command.Parameters.AddWithValue("@Numero", usuario.Numero); else command.Parameters.AddWithValue("@Numero", DBNull.Value);
                    if (!string.IsNullOrEmpty(usuario.TelefonoMovil)) command.Parameters.AddWithValue("@TelefonoMovil", usuario.TelefonoMovil); else command.Parameters.AddWithValue("@TelefonoMovil", DBNull.Value);
                    if (!string.IsNullOrEmpty(usuario.TelefonoFijo)) command.Parameters.AddWithValue("@TelefonoFijo", usuario.TelefonoFijo); else command.Parameters.AddWithValue("@TelefonoFijo", DBNull.Value);
                    if (!string.IsNullOrEmpty(usuario.CorreoInstitucional)) command.Parameters.AddWithValue("@CorreoInstitucional", usuario.CorreoInstitucional); else command.Parameters.AddWithValue("@CorreoInstitucional", DBNull.Value);
                    if (!string.IsNullOrEmpty(usuario.CorreoPersonal)) command.Parameters.AddWithValue("@CorreoPersonal", usuario.CorreoPersonal); else command.Parameters.AddWithValue("@CorreoPersonal", DBNull.Value);
                    if (usuario.IdSucursal != null) command.Parameters.AddWithValue("@IdSucursal", usuario.IdSucursal); else command.Parameters.AddWithValue("@IdSucursal", DBNull.Value);
                    if (usuario.IdDepartamento != null) command.Parameters.AddWithValue("@IdDepartamento", usuario.IdDepartamento); else command.Parameters.AddWithValue("@IdDepartamento", DBNull.Value);
                    if (usuario.IdPuesto != null) command.Parameters.AddWithValue("@IdPuesto", usuario.IdPuesto); else command.Parameters.AddWithValue("@IdPuesto", DBNull.Value);
                    if (usuario.Estado != null) command.Parameters.AddWithValue("@Estado", usuario.Estado); else command.Parameters.AddWithValue("@Estado", DBNull.Value);
                    //if (usuario.FechaIngreso != null) command.Parameters.AddWithValue("@FechaIngreso", usuario.FechaIngreso); else command.Parameters.AddWithValue("@FechaIngreso", DBNull.Value);
                    if (usuario.Estatus != null) command.Parameters.AddWithValue("@Estatus", usuario.Estatus); else command.Parameters.AddWithValue("@Estatus", DBNull.Value);
                    if (!string.IsNullOrEmpty(usuario.FotoLink)) command.Parameters.AddWithValue("@FotoLink", usuario.FotoLink); else command.Parameters.AddWithValue("@FotoLink", DBNull.Value);
                    if (!string.IsNullOrEmpty(usuario.Notas)) command.Parameters.AddWithValue("@Notas", usuario.Notas); else command.Parameters.AddWithValue("@Notas", DBNull.Value);
                    if (usuario.idRol != null) command.Parameters.AddWithValue("@idRol", usuario.idRol); else command.Parameters.AddWithValue("@idRol", DBNull.Value);
                    command.Parameters.AddWithValue("@Id", usuario.Id);
                    command.ExecuteNonQuery();
                }
                return Ok("Usuario actualizado correctamente");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return StatusCode(500, $"Error interno del servidor: {e.Message}");
            }
        }

        [HttpDelete]
        [Route("EliminarUsuario")]
        public async Task<IActionResult> EliminarUsuario(Guid id, string empresa, string cadena)
        {
            try
            {

                byte[] data = Convert.FromBase64String(cadena);

                // Si quieres convertir los bytes a una cadena original
                cadena = Encoding.UTF8.GetString(data);

                using (SqlConnection connection = new SqlConnection(cadena))
                {
                    connection.Open();

                    string query = "UPDATE Usuarios set borrado='1' WHERE Id = @Id";
                    SqlCommand command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@Id", id);
                    command.ExecuteNonQuery();
                }

                return Ok("Usuario eliminado correctamente");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return StatusCode(500, $"Error interno del servidor: {e.Message}");
            }
        }

        [HttpPut]
        [Route("AsignaRol")]
        public async Task<IActionResult> AsignaRol(Guid idUsuario, Guid idRol, string empresa, string cadena)
        {
            try
            {

                byte[] data = Convert.FromBase64String(cadena);

                // Si quieres convertir los bytes a una cadena original
                cadena = Encoding.UTF8.GetString(data);

                using (SqlConnection connection = new SqlConnection(cadena))
                {
                    connection.Open();

                    string query = @"UPDATE Usuarios SET idRol = @idRol WHERE Id = @Id";
                    SqlCommand command = new SqlCommand(query, connection);
                    if (idRol != null) command.Parameters.AddWithValue("@idRol", idRol); else command.Parameters.AddWithValue("@idRol", DBNull.Value);
                    command.Parameters.AddWithValue("@Id", idUsuario);
                    command.ExecuteNonQuery();
                }
                return Ok("Ok");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return StatusCode(500, $"Error interno del servidor: {e.Message}");
            }
        }

    }
}
