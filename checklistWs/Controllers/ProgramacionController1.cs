using checklistWs.Models.Programacion;
using checklistWs.Utiles;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;

namespace checklistWs.Controllers
{
    public class ProgramacionController1 : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly SqlConnectionFactory _connectionFactory;

        public ProgramacionController1(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionFactory = new SqlConnectionFactory(configuration);
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [Route("ListasProgramacion/GetElemento")]
        public async Task<IActionResult> GetElemento(Guid id)
        {
            try
            {
               
                List<ListasProgramacion> regresa = new List<ListasProgramacion>();
                using (SqlConnection connection = _connectionFactory.CreateConnection())
                {
                    connection.Open();
                    string sQuery = string.Format("SELECT lpr.id,lpr.idEmpresa, lpr.idPrograma, lpr.idUsuario, lpr.Nombre, lpr. fechaProgramacion, lpr.idLista, lpr.FechaInicio, lpr.FechaFin, lpr.HoraInicio, lpr.HoraFin, u.Nombre + ' ' + cca1.apellido + ' ' + cca2.apellido as Usuario, l.Nombre as Lista FROM ListasProgramacion lpr LEFT JOIN Listas l on lpr.idLista = l.id LEFT JOIN usuarios u on l.idUsuario = u.id LEFT JOIN CatalogoClientesApellidos cca1 on u.idApellidoPaterno = cca1.id LEFT JOIN CatalogoClientesApellidos cca2 on u.idApellidoMaterno = cca2.id where lpr.id = '{0}'", id);
                    using (SqlCommand command = new SqlCommand(sQuery, connection))
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (reader.Read())
                            {
                                ListasProgramacion item = new ListasProgramacion();
                                item.id = reader["id"] != DBNull.Value ? Guid.Parse(reader["id"].ToString()) : Guid.Empty;
                                item.idEmpresa = reader["idEmpresa"] != DBNull.Value ? Guid.Parse(reader["idEmpresa"].ToString()) : Guid.Empty;
                                item.idPrograma = reader["idPrograma"] != DBNull.Value ? Guid.Parse(reader["idPrograma"].ToString()) : Guid.Empty;
                                item.idusuario = reader["idUsuario"] != DBNull.Value ? Guid.Parse(reader["idUsuario"].ToString()) : Guid.Empty;
                                item.Nombre = reader["Nombre"] != DBNull.Value ? reader["Nombre"].ToString().Trim() : string.Empty;
                                item.fechaProgramacion = reader["fechaProgramacion"] != DBNull.Value ? DateTime.Parse(reader["fechaProgramacion"].ToString()) : DateTime.MinValue;
                                item.idLista = reader["idLista"] != DBNull.Value ? Guid.Parse(reader["idLista"].ToString()) : Guid.Empty;
                                item.FechaInicio = reader["FechaInicio"] != DBNull.Value ? DateTime.Parse(reader["FechaInicio"].ToString()) : DateTime.MinValue;
                                item.FechaFin = reader["FechaFin"] != DBNull.Value ? DateTime.Parse(reader["FechaFin"].ToString()) : DateTime.MinValue;
                                item.HoraInicio = reader["HoraInicio"] != DBNull.Value ? TimeSpan.Parse(reader["HoraInicio"].ToString()) : TimeSpan.MinValue;
                                item.HoraFin = reader["HoraFin"] != DBNull.Value ? TimeSpan.Parse(reader["HoraFin"].ToString()) : TimeSpan.MinValue;
                                item.Usuario = reader["Usuario"] != DBNull.Value ? reader["Usuario"].ToString().Trim() : string.Empty;
                                item.Lista = reader["Lista"] != DBNull.Value ? reader["Lista"].ToString().Trim() : string.Empty;
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
        [Route("ListasProgramacion/GetLista")]
        public async Task<IActionResult> GetLista(Guid idLista)
        {
            try
            {
               
                List<ListasProgramacion> regresa = new List<ListasProgramacion>();
                using (SqlConnection connection = _connectionFactory.CreateConnection())
                {
                    connection.Open();
                    string sQuery = string.Format("SELECT lpr.id,lpr.idEmpresa, lpr.idPrograma, lpr.idUsuario, lpr.Nombre, lpr. fechaProgramacion, lpr.idLista, lpr.FechaInicio, lpr.FechaFin, lpr.HoraInicio, lpr.HoraFin, u.Nombre + ' ' + cca1.apellido + ' ' + cca2.apellido as Usuario, l.Nombre as Lista FROM ListasProgramacion lpr LEFT JOIN Listas l on lpr.idLista = l.id LEFT JOIN usuarios u on l.idUsuario = u.id LEFT JOIN CatalogoClientesApellidos cca1 on u.idApellidoPaterno = cca1.id LEFT JOIN CatalogoClientesApellidos cca2 on u.idApellidoMaterno = cca2.id where lpr.idLista = '{0}'", idLista);
                    using (SqlCommand command = new SqlCommand(sQuery, connection))
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (reader.Read())
                            {
                                ListasProgramacion item = new ListasProgramacion();
                                item.id = reader["id"] != DBNull.Value ? Guid.Parse(reader["id"].ToString()) : Guid.Empty;
                                item.idEmpresa = reader["idEmpresa"] != DBNull.Value ? Guid.Parse(reader["idEmpresa"].ToString()) : Guid.Empty;
                                item.idPrograma = reader["idPrograma"] != DBNull.Value ? Guid.Parse(reader["idPrograma"].ToString()) : Guid.Empty;
                                item.idusuario = reader["idUsuario"] != DBNull.Value ? Guid.Parse(reader["idUsuario"].ToString()) : Guid.Empty;
                                item.idusuario = reader["idUsuario"] != DBNull.Value ? Guid.Parse(reader["idUsuario"].ToString()) : Guid.Empty;

                                item.Nombre = reader["Nombre"] != DBNull.Value ? reader["Nombre"].ToString().Trim() : string.Empty;
                                item.fechaProgramacion = reader["fechaProgramacion"] != DBNull.Value ? DateTime.Parse(reader["fechaProgramacion"].ToString()) : DateTime.MinValue;
                                item.idLista = reader["idLista"] != DBNull.Value ? Guid.Parse(reader["idLista"].ToString()) : Guid.Empty;
                                item.FechaInicio = reader["FechaInicio"] != DBNull.Value ? DateTime.Parse(reader["FechaInicio"].ToString()) : DateTime.MinValue;
                                item.FechaFin = reader["FechaFin"] != DBNull.Value ? DateTime.Parse(reader["FechaFin"].ToString()) : DateTime.MinValue;
                                item.HoraInicio = reader["HoraInicio"] != DBNull.Value ? TimeSpan.Parse(reader["HoraInicio"].ToString()) : TimeSpan.MinValue;
                                item.HoraFin = reader["HoraFin"] != DBNull.Value ? TimeSpan.Parse(reader["HoraFin"].ToString()) : TimeSpan.MinValue;
                                item.Usuario = reader["Usuario"] != DBNull.Value ? reader["Usuario"].ToString().Trim() : string.Empty;
                                item.Lista = reader["Lista"] != DBNull.Value ? reader["Lista"].ToString().Trim() : string.Empty;
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
        [Route("ListasProgramacion/GetTodos")]
        public async Task<IActionResult> GetTodos(Guid idEmpresa)
        {
            try
            {
              
                List<ListasProgramacion> regresa = new List<ListasProgramacion>();
                using (SqlConnection connection = _connectionFactory.CreateConnection())
                {
                    connection.Open();
                    string sQuery = string.Format("SELECT lpr.id,lpr.idEmpresa, lpr.idPrograma, lpr.idUsuario, lpr.Nombre, lpr. fechaProgramacion, lpr.idLista, lpr.FechaInicio, lpr.FechaFin, lpr.HoraInicio, lpr.HoraFin, u.Nombre + ' ' + cca1.apellido + ' ' + cca2.apellido as Usuario, l.Nombre as Lista FROM ListasProgramacion lpr LEFT JOIN Listas l on lpr.idLista = l.id LEFT JOIN usuarios u on l.idUsuario = u.id LEFT JOIN CatalogoClientesApellidos cca1 on u.idApellidoPaterno = cca1.id LEFT JOIN CatalogoClientesApellidos cca2 on u.idApellidoMaterno = cca2.id where lpr.idEmpresa = '{0}'", idEmpresa);
                    using (SqlCommand command = new SqlCommand(sQuery, connection))
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (reader.Read())
                            {
                                ListasProgramacion item = new ListasProgramacion();
                                item.id = reader["id"] != DBNull.Value ? Guid.Parse(reader["id"].ToString()) : Guid.Empty;
                                item.idEmpresa = reader["idEmpresa"] != DBNull.Value ? Guid.Parse(reader["idEmpresa"].ToString()) : Guid.Empty;
                                item.idPrograma = reader["idPrograma"] != DBNull.Value ? Guid.Parse(reader["idPrograma"].ToString()) : Guid.Empty;
                                item.idusuario = reader["idUsuario"] != DBNull.Value ? Guid.Parse(reader["idUsuario"].ToString()) : Guid.Empty;
                                item.Nombre = reader["Nombre"] != DBNull.Value ? reader["Nombre"].ToString().Trim() : string.Empty;
                                item.fechaProgramacion = reader["fechaProgramacion"] != DBNull.Value ? DateTime.Parse(reader["fechaProgramacion"].ToString()) : DateTime.MinValue;
                                item.idLista = reader["idLista"] != DBNull.Value ? Guid.Parse(reader["idLista"].ToString()) : Guid.Empty;
                                item.FechaInicio = reader["FechaInicio"] != DBNull.Value ? DateTime.Parse(reader["FechaInicio"].ToString()) : DateTime.MinValue;
                                item.FechaFin = reader["FechaFin"] != DBNull.Value ? DateTime.Parse(reader["FechaFin"].ToString()) : DateTime.MinValue;
                                //item.HoraInicio = reader.GetTimeSpan(reader.GetOrdinal("HoraInicio"));
                                //item.HoraFin = reader.GetTimeSpan(reader.GetOrdinal("HoraFin"));
                                item.HoraInicio = reader["HoraInicio"] != DBNull.Value ? TimeSpan.Parse(reader["HoraInicio"].ToString()) : TimeSpan.MinValue;
                                item.HoraFin = reader["HoraFin"] != DBNull.Value ? TimeSpan.Parse(reader["HoraFin"].ToString()) : TimeSpan.MinValue;
                                item.Usuario = reader["Usuario"] != DBNull.Value ? reader["Usuario"].ToString().Trim() : string.Empty;
                                item.Lista = reader["Lista"] != DBNull.Value ? reader["Lista"].ToString().Trim() : string.Empty;
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
        [Route("ListasProgramacion/Guardar")]
        public async Task<IActionResult> Guardar([FromBody] ListasProgramacion datos)
        {
            try
            {
               
                using (SqlConnection connection = _connectionFactory.CreateConnection())
                {
                    connection.Open();
                    string sQuery = string.Empty;
                    bool actualiza = false;
                    Guid insertado = Guid.NewGuid();
                    if (await Existe((Guid)datos.id))
                    {
                        sQuery = string.Format("UPDATE ListasProgramacion SET idEmpresa = @idEmpresa, idPrograma = @idPrograma, idUsuario = @idUsuario, Nombre = @Nombre, fechaProgramacion = fechaProgramacion, idLista = @idLista, FechaInicio = @FechaInicio, FechaFin = @FechaFin, HoraInicio = @HoraInicio, HoraFin = @HoraFin  where id = '{0}'", datos.id);
                        actualiza = true;
                    }
                    else
                    {
                        sQuery = string.Format("INSERT INTO ListasProgramacion (id, idEmpresa, idPrograma, idUsuario, Nombre, fechaProgramacion, idLista, FechaInicio, FechaFin, HoraInicio, HoraFin) VALUES ('{0}',@idEmpresa,@idPrograma,@idUsuario,@Nombre,@fechaProgramacion, @idLista, @FechaInicio, @FechaFin, @HoraInicio, @HoraFin)", insertado);
                    }
                    using (SqlCommand command = new SqlCommand(sQuery, connection))
                    {

                        if (datos.idEmpresa != null) command.Parameters.AddWithValue("@idEmpresa", datos.idEmpresa); else command.Parameters.AddWithValue("@idEmpresa", DBNull.Value);
                        if (datos.idPrograma != null) command.Parameters.AddWithValue("@idPrograma", datos.idPrograma); else command.Parameters.AddWithValue("@idPrograma", DBNull.Value);
                        if (datos.idusuario != null) command.Parameters.AddWithValue("@idUsuario", datos.idusuario); else command.Parameters.AddWithValue("@idUsuario", DBNull.Value);
                        if (datos.Nombre != null) command.Parameters.AddWithValue("@Nombre", datos.Nombre); else command.Parameters.AddWithValue("@Nombre", DBNull.Value);
                        if (datos.fechaProgramacion != null) command.Parameters.AddWithValue("@fechaProgramacion", datos.fechaProgramacion); else command.Parameters.AddWithValue("@fechaProgramacion", DBNull.Value);
                        if (datos.idLista != null) command.Parameters.AddWithValue("@idLista", datos.idLista); else command.Parameters.AddWithValue("@idLista", DBNull.Value);
                        if (datos.FechaInicio != null) command.Parameters.AddWithValue("@FechaInicio", datos.FechaInicio); else command.Parameters.AddWithValue("@FechaInicio", DBNull.Value);
                        if (datos.FechaFin != null) command.Parameters.AddWithValue("@FechaFin", datos.FechaFin); else command.Parameters.AddWithValue("@FechaFin", DBNull.Value);
                        if (datos.HoraInicio != null) command.Parameters.AddWithValue("@HoraInicio", datos.HoraInicio); else command.Parameters.AddWithValue("@HoraInicio", DBNull.Value);
                        if (datos.HoraFin != null) command.Parameters.AddWithValue("@HoraFin", datos.HoraFin); else command.Parameters.AddWithValue("@HoraFin", DBNull.Value);


                        await command.ExecuteNonQueryAsync();
                    }
                }
                return Ok("Ok");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                // Retornar un código de error HTTP 500 (Internal Server Error)
                return StatusCode(500, $"Error interno del servidor {ex.Message}");
            }
        }


        /* [HttpDelete]
         [Route("ListasProgramacion/Borrar")]
         public async Task<IHttpActionResult> Borrar(Guid id)
         {
             try
             {
                 string cadena = SqlConnectionFactory.ObtenerCadenaConexion();
                 using (SqlConnection connection = new SqlConnection(cadena))
                 {
                     connection.Open();
                     string sQuery = $@"UPDATE ListasProgramacion SET Status = '0' WHERE id = '{id}'";
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

        private async Task<bool> Existe(Guid cualId)
        {
            bool regresa = false;
            if (cualId != Guid.Empty)
            {
                try
                {
                   
                    using (SqlConnection connection = _connectionFactory.CreateConnection())
                    {
                        connection.Open();
                        string sQuery = string.Format("SELECT COUNT(*) FROM ListasProgramacion WHERE id = '{0}'", cualId);
                        using (SqlCommand command = new SqlCommand(sQuery, connection))
                        {
                            using (SqlDataReader reader = await command.ExecuteReaderAsync())
                            {
                                if (reader.HasRows)
                                {
                                    reader.Read();
                                    if (Convert.ToInt32(reader[0]) > 0) regresa = true;
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
