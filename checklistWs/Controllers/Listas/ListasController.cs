using checklistWs.Models.Lista;
using checklistWs.Utiles;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Text;

namespace checklistWs.Controllers.Listas
{
    public class ListasController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly SqlConnectionFactory _connectionFactory;

        public ListasController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionFactory = new SqlConnectionFactory(configuration);
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        [Route("Listas/GetElemento")]
        public async Task<IActionResult> GetElemento(Guid id, string empresa, string cadena)
        {
            try
            {

                List<ListaCompleta> regresa = new List<ListaCompleta>();
                byte[] data = Convert.FromBase64String(cadena);

                // Si quieres convertir los bytes a una cadena original
                cadena = Encoding.UTF8.GetString(data);

                using (SqlConnection connection = new SqlConnection(cadena))
                {
                    connection.Open();
                    string sQuery = string.Format("SELECT l.id, l.Activo, l.idEmpresa, l.idPrograma, l.idInstructor, l.idUsuario, l.Nombre, l. FechaCreacion, l.Notas, l.Status, l.Estado, l.UsaActivos, l.idTipoActivo, ISNULL(at.Nombre, '') AS TipoActivo,  u.Nombre + ' ' + u.ApellidoPaterno + ' ' + u.ApellidoMaterno as Instructor from Listas l LEFT JOIN usuarios u on l.idUsuario = u.id LEFT JOIN dbo.ActivosTipos at ON l.idTipoActivo = at.id AND at.idEmpresa = l.idEmpresa where l.id = '{0}'", id);
                    using (SqlCommand command = new SqlCommand(sQuery, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ListaCompleta item = new ListaCompleta();
                                item.id = reader["id"] != DBNull.Value ? Guid.Parse(reader["id"].ToString()) : Guid.Empty;
                                item.fechacreacion = reader["FechaCreacion"] != DBNull.Value ? DateTimeOffset.Parse(reader["FechaCreacion"].ToString()) : DateTimeOffset.MinValue;
                                item.Activo = reader["Activo"] != DBNull.Value ? bool.Parse(reader["Activo"].ToString()) : false;
                                item.idEmpresa = reader["idEmpresa"] != DBNull.Value ? Guid.Parse(reader["idEmpresa"].ToString()) : Guid.Empty;
                                // item.idPrograma = reader["idPrograma"] != DBNull.Value ? Guid.Parse(reader["idPrograma"].ToString()) : Guid.Empty;
                                item.idusuario = reader["idUsuario"] != DBNull.Value ? Guid.Parse(reader["idUsuario"].ToString()) : Guid.Empty;
                                item.Nombre = reader["Nombre"] != DBNull.Value ? reader["Nombre"].ToString().Trim() : string.Empty;
                                item.Notas = reader["Notas"] != DBNull.Value ? reader["Notas"].ToString().Trim() : string.Empty;
                                item.Status = reader["Status"] != DBNull.Value ? bool.Parse(reader["Status"].ToString()) : false;
                                item.Estado = reader["Estado"] != DBNull.Value ? decimal.Parse(reader["Estado"].ToString()) : 0;
                                item.UsaActivos = reader["UsaActivos"] != DBNull.Value ? bool.Parse(reader["UsaActivos"].ToString()) : false;
                                item.idTipoActivo = reader["idTipoActivo"] != DBNull.Value ? Guid.Parse(reader["idTipoActivo"].ToString()) : null;
                                item.TipoActivo = reader["TipoActivo"] != DBNull.Value ? reader["TipoActivo"].ToString().Trim() : string.Empty;
                                item.Instructor = reader["Instructor"] != DBNull.Value ? reader["Instructor"].ToString().Trim() : string.Empty;
                                item.idInstructor = reader["idInstructor"] != DBNull.Value ? Guid.Parse(reader["idInstructor"].ToString()) : Guid.Empty;

                                regresa.Add(item);
                            }
                        }
                    }
                    return Ok(regresa);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                // Retornar un código de error HTTP 500 (Internal Server Error)
                return StatusCode(500, $"Error interno del servidor {e.Message}");
            }
        }

        [HttpGet]
        [Route("Listas/GetTodos")]
        public async Task<IActionResult> GetTodos(Guid idEmpresa, string empresa, string cadena, string cualPrograma = "")
        {
            try
            {

                List<ListaCompleta> regresa = new List<ListaCompleta>();
                byte[] data = Convert.FromBase64String(cadena);

                // Si quieres convertir los bytes a una cadena original
                cadena = Encoding.UTF8.GetString(data);

                using (SqlConnection connection = new SqlConnection(cadena))
                {
                    connection.Open();
                    string sComp = string.Empty;
                    if (!string.IsNullOrEmpty(cualPrograma))
                    {
                        sComp = string.Format(" AND l.Nombre LIKE '%{0}%'", cualPrograma);
                    }
                    string sQuery = string.Format("SELECT l.id, l.Activo, l.idEmpresa, l.idPrograma, l.idInstructor, l.idUsuario, l.Nombre, l. FechaCreacion, l.Notas, l.Status,  u.Nombre + ' ' + u.ApellidoPaterno + ' ' + u.ApellidoMaterno as Instructor from Listas l LEFT JOIN usuarios u on l.idUsuario = u.id where l.idEmpresa = '{0}' AND l.Estado = 1 AND status = 1 {1} ORDER BY l.Nombre", idEmpresa, sComp);
                    using (SqlCommand command = new SqlCommand(sQuery, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ListaCompleta item = new ListaCompleta();
                                item.id = reader["id"] != DBNull.Value ? Guid.Parse(reader["id"].ToString()) : Guid.Empty;
                                item.fechacreacion = reader["FechaCreacion"] != DBNull.Value ? DateTimeOffset.Parse(reader["FechaCreacion"].ToString()) : DateTimeOffset.MinValue;
                                item.Activo = reader["Activo"] != DBNull.Value ? bool.Parse(reader["Activo"].ToString()) : false;
                                item.idEmpresa = reader["idEmpresa"] != DBNull.Value ? Guid.Parse(reader["idEmpresa"].ToString()) : Guid.Empty;
                                // item.idPrograma = reader["idPrograma"] != DBNull.Value ? Guid.Parse(reader["idPrograma"].ToString()) : Guid.Empty;
                                item.idusuario = reader["idUsuario"] != DBNull.Value ? Guid.Parse(reader["idUsuario"].ToString()) : Guid.Empty;
                                item.Nombre = reader["Nombre"] != DBNull.Value ? reader["Nombre"].ToString().Trim() : string.Empty;
                                item.Notas = reader["Notas"] != DBNull.Value ? reader["Notas"].ToString().Trim() : string.Empty;
                                item.Status = reader["Status"] != DBNull.Value ? bool.Parse(reader["Status"].ToString()) : false;
                                item.Instructor = reader["Instructor"] != DBNull.Value ? reader["Instructor"].ToString().Trim() : string.Empty;
                                item.idusuario = reader["idInstructor"] != DBNull.Value ? Guid.Parse(reader["idInstructor"].ToString().Trim()) : Guid.Empty;

                                //
                                regresa.Add(item);
                            }
                        }
                    }
                    return Ok(regresa);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                // Retornar un código de error HTTP 500 (Internal Server Error)
                return StatusCode(500, $"Error interno del servidor {e.Message}");
            }
        }


        [HttpGet]
        [Route("Listas/GetLista")]
        public async Task<IActionResult> GetLista(Guid idLista, Guid idEmpresa, string cadena)
        {
            try
            {

                List<ListaDetalle> regresa = new List<ListaDetalle>();
                byte[] data = Convert.FromBase64String(cadena);


                cadena = Encoding.UTF8.GetString(data);

                using (SqlConnection connection = new SqlConnection(cadena))
                {
                    connection.Open();

                    string sQuery = string.Format("SELECT Listas.Nombre AS Lista, ListasPreguntas.Pregunta, ListasPreguntasCategorias.Nombre AS Categoria, ListasPreguntasSubCategorias.Nombre AS SubCategoria,  CASE WHEN ListasPreguntas.Tipo = 1 THEN 'Estrellas' WHEN ListasPreguntas.Tipo = 2 THEN 'Opcion simple' WHEN ListasPreguntas.Tipo = 3 THEN 'Opcion multiple' WHEN ListasPreguntas.Tipo = 4 THEN 'Texto' WHEN ListasPreguntas.Tipo = 5 THEN 'Numero' WHEN ListasPreguntas.Tipo = 6 THEN 'Fecha' END AS Tipo, ISNULL(ListasPreguntasOpciones.opcion, '') AS Opciones, ISNULL(ListasPreguntas.ValorCorrecto, '') AS ValorCorrecto,  ListasPreguntas.Explicacion FROM dbo.Listas INNER JOIN dbo.ListasPreguntas ON  Listas.idEmpresa = ListasPreguntas.idEmpresa AND Listas.id = ListasPreguntas.idLista LEFT JOIN dbo.ListasPreguntasOpciones ON  ListasPreguntas.idLista = ListasPreguntasOpciones.idLista AND ListasPreguntas.idEmpresa = ListasPreguntasOpciones.idEmpresa AND ListasPreguntas.id = ListasPreguntasOpciones.idPregunta INNER JOIN dbo.ListasPreguntasCategorias ON  ListasPreguntas.idCategoria = ListasPreguntasCategorias.id AND ListasPreguntas.idEmpresa = ListasPreguntasCategorias.idEmpresa INNER JOIN dbo.ListasPreguntasSubCategorias ON  ListasPreguntas.idSubCategoria = ListasPreguntasSubCategorias.id AND ListasPreguntas.idEmpresa = ListasPreguntasSubCategorias.idEmpresa WHERE Listas.idEmpresa = '{1}' AND Listas.id = '{0}'ORDER BY ListasPreguntas.Pregunta ASC,  ListasPreguntasOpciones.opcion ASC", idLista, idEmpresa);
                    using (SqlCommand command = new SqlCommand(sQuery, connection))
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (reader.Read())
                            {

                                ListaDetalle item = new ListaDetalle();

                                item.Lista = reader["Lista"] != DBNull.Value ? (reader["Lista"].ToString()) : string.Empty;
                                item.Pregunta = reader["Pregunta"] != DBNull.Value ? (reader["Pregunta"].ToString()) : string.Empty;
                                item.Categoria = reader["Categoria"] != DBNull.Value ? (reader["Categoria"].ToString()) : string.Empty;
                                item.Subcategoria = reader["SubCategoria"] != DBNull.Value ? (reader["SubCategoria"].ToString()) : string.Empty;
                                item.Tipo = reader["Tipo"] != DBNull.Value ? (reader["Tipo"].ToString()) : string.Empty;
                                item.Opciones = reader["Opciones"] != DBNull.Value ? (reader["Opciones"].ToString()) : string.Empty;
                                item.ValorCorrecto = reader["ValorCorrecto"] != DBNull.Value ? (reader["ValorCorrecto"].ToString()) : string.Empty;
                                item.Explicacion = reader["Explicacion"] != DBNull.Value ? (reader["Explicacion"].ToString()) : string.Empty;

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
                return StatusCode(500, $"Error interno del servidor {ex.Message}");
            }
        }


        [HttpGet]
        [Route("Listas/GetTodosSinFiltro")]
        public async Task<IActionResult> GetTodosSinFiltro(Guid idEmpresa, string empresa, string cadena, string cualPrograma = "")
        {
            try
            {

                List<ListaCompleta> regresa = new List<ListaCompleta>();
                byte[] data = Convert.FromBase64String(cadena);

                // Si quieres convertir los bytes a una cadena original
                cadena = Encoding.UTF8.GetString(data);

                using (SqlConnection connection = new SqlConnection(cadena))
                {
                    connection.Open();
                    string sComp = string.Empty;
                    if (!string.IsNullOrEmpty(cualPrograma))
                    {
                        sComp = string.Format(" AND l.Nombre LIKE '%{0}%'", cualPrograma);
                    }
                    string sQuery = string.Format("SELECT l.id, l.Activo, l.idEmpresa, l.idPrograma, l.idInstructor, l.idUsuario, l.Nombre, l. FechaCreacion, l.Notas, l.Status,  u.Nombre + ' ' + u.ApellidoPaterno + ' ' + u.ApellidoMaterno as Instructor from Listas l LEFT JOIN usuarios u on l.idUsuario = u.id where l.idEmpresa = '{0}' AND status = 1 and l.estado = 1 {1} ORDER BY l.Nombre", idEmpresa, sComp);
                    using (SqlCommand command = new SqlCommand(sQuery, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ListaCompleta item = new ListaCompleta();
                                item.id = reader["id"] != DBNull.Value ? Guid.Parse(reader["id"].ToString()) : Guid.Empty;
                                item.fechacreacion = reader["FechaCreacion"] != DBNull.Value ? DateTimeOffset.Parse(reader["FechaCreacion"].ToString()) : DateTimeOffset.MinValue;
                                item.Activo = reader["Activo"] != DBNull.Value ? bool.Parse(reader["Activo"].ToString()) : false;
                                item.idEmpresa = reader["idEmpresa"] != DBNull.Value ? Guid.Parse(reader["idEmpresa"].ToString()) : Guid.Empty;
                                // item.idPrograma = reader["idPrograma"] != DBNull.Value ? Guid.Parse(reader["idPrograma"].ToString()) : Guid.Empty;
                                item.idusuario = reader["idUsuario"] != DBNull.Value ? Guid.Parse(reader["idUsuario"].ToString()) : Guid.Empty;
                                item.Nombre = reader["Nombre"] != DBNull.Value ? reader["Nombre"].ToString().Trim() : string.Empty;
                                item.Notas = reader["Notas"] != DBNull.Value ? reader["Notas"].ToString().Trim() : string.Empty;
                                item.Status = reader["Status"] != DBNull.Value ? bool.Parse(reader["Status"].ToString()) : false;
                                item.Instructor = reader["Instructor"] != DBNull.Value ? reader["Instructor"].ToString().Trim() : string.Empty;
                                item.idusuario = reader["idInstructor"] != DBNull.Value ? Guid.Parse(reader["idInstructor"].ToString().Trim()) : Guid.Empty;

                                //
                                regresa.Add(item);
                            }
                        }
                    }
                    return Ok(regresa);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                // Retornar un código de error HTTP 500 (Internal Server Error)
                return StatusCode(500, $"Error interno del servidor {e.Message}");
            }
        }

        [HttpGet]
        [Route("Listas/GetTodosCerradas")]
        public async Task<IActionResult> GetTodosCerradas(Guid idEmpresa, string empresa, string cadena)
        {
            try
            {
                List<ListaCompleta> regresa = new List<ListaCompleta>();
                byte[] data = Convert.FromBase64String(cadena);

                // Si quieres convertir los bytes a una cadena original
                cadena = Encoding.UTF8.GetString(data);

                using (SqlConnection connection = new SqlConnection(cadena))
                {
                    connection.Open();
                    string sQuery = string.Format("SELECT l.id, l.Activo, l.idEmpresa, l.idPrograma, l.idInstructor, l.idUsuario, l.Nombre, l. FechaCreacion, l.Notas, l.Status,  u.Nombre + ' ' + u.ApellidoPaterno + ' ' + u.ApellidoMaterno as Instructor, u.Nombre + ' ' + u.ApellidoPaterno + ' ' + u.ApellidoMaterno as Usuario from Listas l LEFT JOIN usuarios u on l.idUsuario = u.id  where l.estado = 2 and  l.idEmpresa = '{0}' ORDER BY  l.Nombre", idEmpresa);
                    using (SqlCommand command = new SqlCommand(sQuery, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ListaCompleta item = new ListaCompleta();
                                item.id = reader["id"] != DBNull.Value ? Guid.Parse(reader["id"].ToString()) : Guid.Empty;
                                item.fechacreacion = reader["FechaCreacion"] != DBNull.Value ? DateTimeOffset.Parse(reader["FechaCreacion"].ToString()) : DateTimeOffset.MinValue;
                                item.Activo = reader["Activo"] != DBNull.Value ? bool.Parse(reader["Activo"].ToString()) : false;
                                item.idEmpresa = reader["idEmpresa"] != DBNull.Value ? Guid.Parse(reader["idEmpresa"].ToString()) : Guid.Empty;
                                item.idPrograma = reader["idPrograma"] != DBNull.Value ? Guid.Parse(reader["idPrograma"].ToString()) : Guid.Empty;
                                //   item.idInstructor = reader["idInstructor"] != DBNull.Value ? Guid.Parse(reader["idInstructor"].ToString()) : Guid.Empty;
                                item.idusuario = reader["idUsuario"] != DBNull.Value ? Guid.Parse(reader["idUsuario"].ToString()) : Guid.Empty;
                                item.Nombre = reader["Nombre"] != DBNull.Value ? reader["Nombre"].ToString().Trim() : string.Empty;
                                item.Notas = reader["Notas"] != DBNull.Value ? reader["Notas"].ToString().Trim() : string.Empty;
                                item.Status = reader["Status"] != DBNull.Value ? bool.Parse(reader["Status"].ToString()) : false;
                                item.Instructor = reader["Instructor"] != DBNull.Value ? reader["Instructor"].ToString().Trim() : string.Empty;
                                item.Usuario = reader["Usuario"] != DBNull.Value ? reader["Usuario"].ToString().Trim() : string.Empty;
                                //
                                regresa.Add(item);
                            }
                        }
                    }
                    return Ok(regresa);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                // Retornar un código de error HTTP 500 (Internal Server Error)
                return StatusCode(500, $"Error interno del servidor {e.Message}");
            }
        }

        [HttpGet]
        [Route("Listas/GetTodosEstadosBL26")]
        public async Task<IActionResult> GetTodosEstadosBL26(Guid idEmpresa, string empresa, string cadena, string cualPrograma = "")
        {
            try
            {
                List<ListaCompleta> regresa = new List<ListaCompleta>();
                byte[] data = Convert.FromBase64String(cadena);
                cadena = Encoding.UTF8.GetString(data);

                using (SqlConnection connection = new SqlConnection(cadena))
                {
                    connection.Open();
                    string sComp = string.Empty;
                    if (!string.IsNullOrEmpty(cualPrograma))
                    {
                        sComp = string.Format(" AND l.Nombre LIKE '%{0}%'", cualPrograma);
                    }

                    string sQuery = string.Format(@"SELECT
    l.id,
    l.Activo,
    l.idEmpresa,
    l.idPrograma,
    l.idInstructor,
    l.idUsuario,
    l.Nombre,
    l.FechaCreacion,
    l.Notas,
    l.Status,
    l.Estado,
    l.UsaActivos,
    l.idTipoActivo,
    ISNULL(at.Nombre, '') AS TipoActivo,
    ISNULL(lp.CantidadTareas, 0) AS CantidadTareas,
    u.Nombre + ' ' + u.ApellidoPaterno + ' ' + u.ApellidoMaterno as Instructor
FROM Listas l
LEFT JOIN usuarios u on l.idUsuario = u.id
LEFT JOIN dbo.ActivosTipos at ON l.idTipoActivo = at.id AND at.idEmpresa = l.idEmpresa
OUTER APPLY (
    SELECT COUNT(1) AS CantidadTareas
    FROM dbo.ListasPreguntas lp
    WHERE lp.idLista = l.id
      AND ISNULL(lp.[Status], 0) = 1
) lp
where l.idEmpresa = '{0}' {1}
ORDER BY l.Status DESC, l.Estado ASC, l.Nombre", idEmpresa, sComp);
                    using (SqlCommand command = new SqlCommand(sQuery, connection))
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ListaCompleta item = new ListaCompleta();
                            item.id = reader["id"] != DBNull.Value ? Guid.Parse(reader["id"].ToString()) : Guid.Empty;
                            item.fechacreacion = reader["FechaCreacion"] != DBNull.Value ? DateTimeOffset.Parse(reader["FechaCreacion"].ToString()) : DateTimeOffset.MinValue;
                            item.Activo = reader["Activo"] != DBNull.Value ? bool.Parse(reader["Activo"].ToString()) : false;
                            item.idEmpresa = reader["idEmpresa"] != DBNull.Value ? Guid.Parse(reader["idEmpresa"].ToString()) : Guid.Empty;
                            item.idPrograma = reader["idPrograma"] != DBNull.Value ? Guid.Parse(reader["idPrograma"].ToString()) : Guid.Empty;
                            item.idInstructor = reader["idInstructor"] != DBNull.Value ? Guid.Parse(reader["idInstructor"].ToString()) : Guid.Empty;
                            item.idusuario = reader["idUsuario"] != DBNull.Value ? Guid.Parse(reader["idUsuario"].ToString()) : Guid.Empty;
                            item.Nombre = reader["Nombre"] != DBNull.Value ? reader["Nombre"].ToString().Trim() : string.Empty;
                            item.Notas = reader["Notas"] != DBNull.Value ? reader["Notas"].ToString().Trim() : string.Empty;
                            item.Status = reader["Status"] != DBNull.Value ? bool.Parse(reader["Status"].ToString()) : false;
                            item.Estado = reader["Estado"] != DBNull.Value ? decimal.Parse(reader["Estado"].ToString()) : 0;
                            item.UsaActivos = reader["UsaActivos"] != DBNull.Value ? bool.Parse(reader["UsaActivos"].ToString()) : false;
                            item.idTipoActivo = reader["idTipoActivo"] != DBNull.Value ? Guid.Parse(reader["idTipoActivo"].ToString()) : null;
                            item.TipoActivo = reader["TipoActivo"] != DBNull.Value ? reader["TipoActivo"].ToString().Trim() : string.Empty;
                            item.CantidadTareas = reader["CantidadTareas"] != DBNull.Value ? int.Parse(reader["CantidadTareas"].ToString()) : 0;
                            item.Instructor = reader["Instructor"] != DBNull.Value ? reader["Instructor"].ToString().Trim() : string.Empty;

                            regresa.Add(item);
                        }
                    }
                }

                return Ok(regresa);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return StatusCode(500, $"Error interno del servidor {e.Message}");
            }
        }


        [HttpPost]
        [Route("Listas/Guardar")]
        public async Task<IActionResult> Guardar([FromBody] ListaCompleta datos, string empresa, string cadena)
        {
            try
            {
                byte[] data = Convert.FromBase64String(cadena);
                cadena = Encoding.UTF8.GetString(data);


                using (SqlConnection connection = new SqlConnection(cadena))
                {
                    connection.Open();
                    string sQuery = string.Empty;
                    bool actualiza = false;
                    Guid insertado = Guid.NewGuid();

                    datos.UsaActivos ??= false;
                    if (datos.UsaActivos != true)
                    {
                        datos.idTipoActivo = null;
                    }
                    else
                    {
                        if (!datos.idTipoActivo.HasValue || datos.idTipoActivo.Value == Guid.Empty)
                        {
                            return BadRequest("Selecciona un tipo de activo vigente.");
                        }

                        if (!await ExisteTipoActivoVigenteAsync(connection, datos.idEmpresa, datos.idTipoActivo.Value))
                        {
                            return BadRequest("Selecciona un tipo de activo vigente.");
                        }
                    }

                    if (await Existe((Guid)datos.id, empresa, cadena, datos.idEmpresa.ToString()))
                    {
                        sQuery = string.Format("UPDATE Listas SET Activo = @Activo, idEmpresa = @idEmpresa, idPrograma = @idPrograma, idInstructor = @idInstructor, idUsuario = @idUsuario, Nombre = @Nombre, FechaCreacion = @FechaCreacion, Notas = @Notas, Status = @Status, Estado=@Estado, UsaActivos = @UsaActivos, idTipoActivo = @idTipoActivo where  id = '{0}'", datos.id);
                        actualiza = true;
                    }
                    else
                    {
                        if (await Existe((Guid)datos.id, empresa, datos.Nombre, datos.idEmpresa.ToString(), cadena))
                        {
                            sQuery = string.Format("INSERT INTO Listas (id, idEmpresa, idPrograma, idInstructor, idUsuario, Nombre, Notas, Estado, latitud, longitud, UsaActivos, idTipoActivo) values ('{0}',@idEmpresa,@idPrograma,@idInstructor,@idUsuario,@Nombre,@Notas, @Estado, @latitud, @longitud, @UsaActivos, @idTipoActivo)", insertado);
                        }
                        else
                        {
                            return Ok("Ya existe una lista con este nombre");
                        }
                    }

                    using (SqlCommand command = new SqlCommand(sQuery, connection))
                    {

                        if (datos.idEmpresa != null) command.Parameters.AddWithValue("@idEmpresa", datos.idEmpresa); else command.Parameters.AddWithValue("@idEmpresa", DBNull.Value);
                        if (datos.idPrograma != null) command.Parameters.AddWithValue("@idPrograma", datos.idPrograma); else command.Parameters.AddWithValue("@idPrograma", DBNull.Value);
                        if (datos.idInstructor != null) command.Parameters.AddWithValue("@idInstructor", datos.idInstructor); else command.Parameters.AddWithValue("@idInstructor", DBNull.Value);
                        if (datos.idusuario != null) command.Parameters.AddWithValue("@idUsuario", datos.idusuario); else command.Parameters.AddWithValue("@idUsuario", DBNull.Value);
                        if (datos.Nombre != null) command.Parameters.AddWithValue("@Nombre", datos.Nombre); else command.Parameters.AddWithValue("@Nombre", DBNull.Value);
                        if (datos.Notas != null) command.Parameters.AddWithValue("@Notas", datos.Notas); else command.Parameters.AddWithValue("@Notas", DBNull.Value);
                        if (datos.Estado != null) command.Parameters.AddWithValue("@Estado", datos.Estado); else command.Parameters.AddWithValue("@Estado", DBNull.Value);
                        if (datos.latitud != null) command.Parameters.AddWithValue("@latitud", datos.latitud); else command.Parameters.AddWithValue("@latitud", DBNull.Value);
                        if (datos.longitud != null) command.Parameters.AddWithValue("@longitud", datos.longitud); else command.Parameters.AddWithValue("@longitud", DBNull.Value);
                        command.Parameters.AddWithValue("@UsaActivos", (object?)datos.UsaActivos ?? false);
                        if (datos.idTipoActivo.HasValue) command.Parameters.AddWithValue("@idTipoActivo", datos.idTipoActivo.Value); else command.Parameters.AddWithValue("@idTipoActivo", DBNull.Value);

                        if (actualiza)
                        {
                            command.Parameters.AddWithValue("@Status", datos.Status);
                            command.Parameters.AddWithValue("@Activo", datos.Activo);
                            if (datos.fechacreacion != null) command.Parameters.AddWithValue("@FechaCreacion", datos.fechacreacion); else command.Parameters.AddWithValue("@FechaCreacion", DBNull.Value);

                        }

                        command.ExecuteReader();
                    }
                }
                return Ok("Ok");

            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                // Retornar un código de error HTTP 500 (Internal Server Error)
                return StatusCode(500, $"Error interno del servidor {e.Message}");
            }
        }


        private async Task<bool> Existe(Guid? cualId, string empresa, string nombre, string idEmpresa, string cadena)
        {
            bool regresa = false;

            try
            {

                using (SqlConnection connection = new SqlConnection(cadena))
                {
                    connection.Open();
                    string sQuery = string.Format("SELECT COUNT(*) FROM [listas] WHERE  nombre='{0}' AND idEmpresa = '{1}' and status = 1", nombre, idEmpresa);
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

            return regresa;
        }

        [HttpDelete]
        [Route("Listas/Borrar")]
        public async Task<IActionResult> Borrar(Guid id, string empresa, string cadena)
        {
            try
            {
                byte[] data = Convert.FromBase64String(cadena);

                // Si quieres convertir los bytes a una cadena original
                cadena = Encoding.UTF8.GetString(data);

                using (SqlConnection connection = new SqlConnection(cadena))
                {
                    connection.Open();
                    string sQuery = $@"UPDATE Listas SET Status = '0' WHERE id = '{id}'";
                    using (SqlCommand command = new SqlCommand(sQuery, connection))
                    {
                        command.ExecuteReader();
                    }
                }
                return Ok("Ok");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                // Retornar un código de error HTTP 500 (Internal Server Error)
                return StatusCode(500, $"Error interno del servidor {e.Message}");
            }
        }

        //Utilerias

        private async Task<bool> Existe(Guid cualId, string empresa, string cadena, string idEmpresa)
        {
            bool regresa = false;
            if (cualId != Guid.Empty)
            {
                try
                {

                    using (SqlConnection connection = new SqlConnection(cadena))
                    {
                        connection.Open();
                        string sQuery = string.Format("SELECT COUNT(*) FROM Listas WHERE id = '{0}' and status = 1 and idEmpresa = '{1}'", cualId, idEmpresa);
                        using (SqlCommand command = new SqlCommand(sQuery, connection))
                        {
                            using (SqlDataReader reader = await command.ExecuteReaderAsync())
                            {
                                if (reader.HasRows)
                                {
                                    reader.Read();
                                    if (Convert.ToInt32(reader[0]) != 0) regresa = true;
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {

                }
            }
            return regresa;
        }

        private static async Task<bool> ExisteTipoActivoVigenteAsync(SqlConnection connection, Guid idEmpresa, Guid idTipoActivo)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT COUNT(1)
FROM dbo.ActivosTipos
WHERE idEmpresa = @IdEmpresa
  AND id = @IdTipoActivo
  AND Activo = 1", connection);

            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@IdTipoActivo", idTipoActivo);

            object? result = await command.ExecuteScalarAsync();
            return result != null && Convert.ToInt32(result) > 0;
        }
    }
}
