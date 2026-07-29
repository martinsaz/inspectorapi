using checklistWs.Models.Preguntas;
using checklistWs.Utiles;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Text;

namespace checklistWs.Controllers.Preguntas
{
    //[Route("api/[controller]")]
    //[ApiController]
    public class PreguntasController : ControllerBase
    {

        private readonly IConfiguration _configuration;
        private readonly SqlConnectionFactory _connectionFactory;

        public PreguntasController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionFactory = new SqlConnectionFactory(configuration);
        }
      

        [HttpGet]
        [Route("ListasPreguntas/GetElemento")]
        public async Task<IActionResult> GetElemento(Guid id, string empresa, string cadena)
        {
            try
            {
               
                List<Pregunta> regresa = new List<Pregunta>();
                byte[] data = Convert.FromBase64String(cadena);

                // Si quieres convertir los bytes a una cadena original
                cadena = Encoding.UTF8.GetString(data);

                using (SqlConnection connection = new SqlConnection(cadena))
				{
                    connection.Open();
                    string sQuery = string.Format("SELECT lp.id, lp.idEmpresa, lp.idLista, lp.Pregunta, lp.Explicacion, lp.Tipo, lp.Valor, lp.Obligatorio, lp.Status, \r\nl.Nombre as Lista, lp.valorCorrecto, idCategoria, lpc.Nombre as 'nombreCategoria', idSubcategoria, lps.Nombre as 'nombreSubcategoria' from ListasPreguntas lp LEFT JOIN Listas l on lp.idLista = l.id \r\nINNER JOIN ListasPreguntasCategorias lpc ON lp.idCategoria = lpc.id INNER JOIN ListasPreguntasSubCategorias lps ON lp.idSubCategoria = lps.id  where lp.Status = 1 AND lp.id = '{0}'", id);
                    using (SqlCommand command = new SqlCommand(sQuery, connection))
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (reader.Read())
                            {
                                Pregunta item = new Pregunta();
                                item.id = reader["id"] != DBNull.Value ? Guid.Parse(reader["id"].ToString()) : Guid.Empty;
                                item.idEmpresa = reader["idEmpresa"] != DBNull.Value ? Guid.Parse(reader["idEmpresa"].ToString()) : Guid.Empty;
                                item.idLista = reader["idLista"] != DBNull.Value ? Guid.Parse(reader["idLista"].ToString()) : Guid.Empty;
                                item.pregunta = reader["Pregunta"] != DBNull.Value ? reader["Pregunta"].ToString().Trim() : string.Empty;
                                item.Explicacion = reader["Explicacion"] != DBNull.Value ? reader["Explicacion"].ToString().Trim() : string.Empty;
                                item.Tipo = reader["Tipo"] != DBNull.Value ? Convert.ToDecimal(reader["Tipo"]) : 0;
                                item.Valor = reader["Valor"] != DBNull.Value ? Convert.ToDecimal(reader["Valor"]) : 0;
                                item.Obligatorio = reader["Obligatorio"] != DBNull.Value ? bool.Parse(reader["Obligatorio"].ToString()) : false;
                                item.Status = reader["Status"] != DBNull.Value ? bool.Parse(reader["Status"].ToString()) : false;
                                item.Lista = reader["Lista"] != DBNull.Value ? reader["Lista"].ToString().Trim() : string.Empty;
                                item.RespuestaCorrecta = reader["valorCorrecto"] != DBNull.Value ? reader["valorCorrecto"].ToString().Trim() : string.Empty;
                                item.idCategoria = reader["idCategoria"] != DBNull.Value ? Guid.Parse(reader["idCategoria"].ToString()) : Guid.Empty;
                                item.idSubcategoria = reader["idSubcategoria"] != DBNull.Value ? Guid.Parse(reader["idSubcategoria"].ToString()) : Guid.Empty;
                                item.Categoria = reader["nombreCategoria"] != DBNull.Value ? reader["nombreCategoria"].ToString().Trim() : string.Empty;
                                item.Subcategoria = reader["nombreSubcategoria"] != DBNull.Value ? reader["nombreSubcategoria"].ToString().Trim() : string.Empty;
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
        [Route("ListasPreguntas/GetTodos")]
        public async Task<IActionResult> GetTodos(Guid idEmpresa, string empresa, string cadena)
        {
            try
            {
                List<Pregunta> regresa = new List<Pregunta>();
                byte[] data = Convert.FromBase64String(cadena);

                
                cadena = Encoding.UTF8.GetString(data);

                using (SqlConnection connection = new SqlConnection(cadena))
				{
                    connection.Open();
                    string sQuery = string.Format(" SELECT lp.id, lp.idEmpresa, lp.idLista, lp.Pregunta, lp.Explicacion, lp.Tipo, lp.Valor, lp.Obligatorio, lp.Status, l.Nombre as Lista, lp.valorCorrecto, lp.idCategoria, lp.idSubcategoria from ListasPreguntas lp LEFT JOIN Listas l on lp.idLista = l.id where lp.Status = 1 AND lp.idEmpresa = '{0}' AND status = 1  ORDER BY Pregunta", idEmpresa);
                    using (SqlCommand command = new SqlCommand(sQuery, connection))
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (reader.Read())
                            {
                                Pregunta item = new Pregunta();
                                item.id = reader["id"] != DBNull.Value ? Guid.Parse(reader["id"].ToString()) : Guid.Empty;
                                item.idEmpresa = reader["idEmpresa"] != DBNull.Value ? Guid.Parse(reader["idEmpresa"].ToString()) : Guid.Empty;
                                item.idLista = reader["idLista"] != DBNull.Value ? Guid.Parse(reader["idLista"].ToString()) : Guid.Empty;
                                item.pregunta = reader["Pregunta"] != DBNull.Value ? reader["Pregunta"].ToString().Trim() : string.Empty;
                                item.Explicacion = reader["Explicacion"] != DBNull.Value ? reader["Explicacion"].ToString().Trim() : string.Empty;
                                item.Tipo = reader["Tipo"] != DBNull.Value ? Convert.ToDecimal(reader["Tipo"]) : 0;
                                item.Valor = reader["Valor"] != DBNull.Value ? Convert.ToDecimal(reader["Valor"]) : 0;
                                item.Obligatorio = reader["Obligatorio"] != DBNull.Value ? bool.Parse(reader["Obligatorio"].ToString()) : false;
                                item.Status = reader["Status"] != DBNull.Value ? bool.Parse(reader["Status"].ToString()) : false;
                                item.Lista = reader["Lista"] != DBNull.Value ? reader["Lista"].ToString().Trim() : string.Empty;
                                item.RespuestaCorrecta = reader["valorCorrecto"] != DBNull.Value ? reader["valorCorrecto"].ToString().Trim() : string.Empty;
                                item.idCategoria = reader["idCategoria"] != DBNull.Value ? Guid.Parse(reader["idCategoria"].ToString()) : Guid.Empty;
                                item.idSubcategoria = reader["idSubcategoria"] != DBNull.Value ? Guid.Parse(reader["idSubcategoria"].ToString()) : Guid.Empty;

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
        [Route("ListasPreguntas/GetLista")]
        public async Task<IActionResult> GetLista(Guid idLista, string empresa, string cadena, string cualPrograma = "")
        {
            try
            {
                
                List<Pregunta> regresa = new List<Pregunta>();
				byte[] data = Convert.FromBase64String(cadena);

				// Si quieres convertir los bytes a una cadena original
				cadena = Encoding.UTF8.GetString(data);

				using (SqlConnection connection = new SqlConnection(cadena))
				{
                    connection.Open();
					string sComp = string.Empty;
					if (!string.IsNullOrEmpty(cualPrograma))
					{
						sComp = string.Format(" AND lp.Pregunta LIKE '%{0}%'", cualPrograma);
					}
					string sQuery = string.Format("SELECT lp.id, lp.idEmpresa, lp.idLista, lp.Pregunta, lp.Explicacion, lp.Tipo, lp.Valor, lp.Obligatorio, lp.Status, l.Nombre as Lista, lp.valorCorrecto, lp.idCategoria, lp.idSubcategoria FROM ListasPreguntas lp LEFT JOIN Listas l ON lp.idLista = l.id WHERE lp.idLista = '{0}' AND lp.status = 1 {1} ORDER BY CASE WHEN PATINDEX('[0-9]%.%', lp.Pregunta) = 1 THEN CAST(SUBSTRING(lp.Pregunta, 1, CHARINDEX('.', lp.Pregunta) - 1) AS INT) WHEN PATINDEX('[0-9]%', lp.Pregunta) = 1 THEN CAST(SUBSTRING(lp.Pregunta, 1, PATINDEX('%[^0-9]%', lp.Pregunta + ' ') - 1) AS INT) ELSE NULL END, lp.Pregunta;", idLista, sComp);
                    using (SqlCommand command = new SqlCommand(sQuery, connection))
                    {
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (reader.Read())
                            {
                                Pregunta item = new Pregunta();
                                item.id = reader["id"] != DBNull.Value ? Guid.Parse(reader["id"].ToString()) : Guid.Empty;
                                item.idEmpresa = reader["idEmpresa"] != DBNull.Value ? Guid.Parse(reader["idEmpresa"].ToString()) : Guid.Empty;
                                item.idLista = reader["idLista"] != DBNull.Value ? Guid.Parse(reader["idLista"].ToString()) : Guid.Empty;
                                item.pregunta = reader["Pregunta"] != DBNull.Value ? reader["Pregunta"].ToString().Trim() : string.Empty;
                                item.Explicacion = reader["Explicacion"] != DBNull.Value ? reader["Explicacion"].ToString().Trim() : string.Empty;
                                item.Tipo = reader["Tipo"] != DBNull.Value ? Convert.ToDecimal(reader["Tipo"]) : 0;
                                item.Valor = reader["Valor"] != DBNull.Value ? Convert.ToDecimal(reader["Valor"]) : 0;
                                item.Obligatorio = reader["Obligatorio"] != DBNull.Value ? bool.Parse(reader["Obligatorio"].ToString()) : false;
                                item.Status = reader["Status"] != DBNull.Value ? bool.Parse(reader["Status"].ToString()) : false;
                                item.Lista = reader["Lista"] != DBNull.Value ? reader["Lista"].ToString().Trim() : string.Empty;
                                item.RespuestaCorrecta = reader["valorCorrecto"] != DBNull.Value ? reader["valorCorrecto"].ToString().Trim() : string.Empty;
                                item.idCategoria = reader["idCategoria"] != DBNull.Value ? Guid.Parse(reader["idCategoria"].ToString()) : Guid.Empty;
                                item.idSubcategoria = reader["idSubcategoria"] != DBNull.Value ? Guid.Parse(reader["idSubcategoria"].ToString()) : Guid.Empty;

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


        [HttpPost]
        [Route("ListasPreguntas/Guardar")]
        public async Task<IActionResult> Guardar([FromBody] Pregunta datos, string empresa, string cadena)
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
                    if (datos.id == null) {
                        datos.id = insertado; 
                    };
                    if (await Existe((Guid)datos.id, empresa, cadena))
                    {
                        sQuery = string.Format("UPDATE ListasPreguntas SET  idEmpresa = @idEmpresa, idLista = @idLista, Pregunta = @Pregunta, Obligatorio = @Obligatorio, Status = @Status, idCategoria = @idCategoria, idSubCategoria = @idSubcategoria where id = '{0}'", datos.id);
                        actualiza = true;
                    }
                    else
                    {
                        sQuery = string.Format("INSERT INTO ListasPreguntas (id, idEmpresa, idLista, Pregunta, Explicacion, Tipo, Valor, Obligatorio, Status, valorCorrecto, idCategoria, idSubCategoria) values ('{0}',@idEmpresa,@idLista,@Pregunta,@Explicacion, @Tipo, @Valor, @Obligatorio, @Status, @respuestaCorrecta, @idCategoria, @idSubcategoria)", insertado);
                    }
                    using (SqlCommand command = new SqlCommand(sQuery, connection))
                    {

                        if (datos.idEmpresa != null) command.Parameters.AddWithValue("@idEmpresa", datos.idEmpresa); else command.Parameters.AddWithValue("@idEmpresa", DBNull.Value);
                        if (datos.idLista != null) command.Parameters.AddWithValue("@idLista", datos.idLista); else command.Parameters.AddWithValue("@idLista", DBNull.Value);
                        if (datos.pregunta != null) command.Parameters.AddWithValue("@Pregunta", datos.pregunta); else command.Parameters.AddWithValue("@Pregunta", DBNull.Value);
                        if (datos.Explicacion != null) command.Parameters.AddWithValue("@Explicacion", datos.Explicacion); else command.Parameters.AddWithValue("@Explicacion", DBNull.Value);
                        if (datos.Tipo != null) command.Parameters.AddWithValue("@Tipo", datos.Tipo); else command.Parameters.AddWithValue("@Tipo", DBNull.Value);
                        if (datos.Valor != null) command.Parameters.AddWithValue("@Valor", datos.Valor); else command.Parameters.AddWithValue("@Valor", DBNull.Value);
                        if (datos.RespuestaCorrecta != null) command.Parameters.AddWithValue("@respuestaCorrecta", datos.RespuestaCorrecta); else command.Parameters.AddWithValue("@respuestaCorrecta", DBNull.Value);
                        if (datos.Obligatorio != null) command.Parameters.AddWithValue("@Obligatorio", datos.Obligatorio); else command.Parameters.AddWithValue("@Obligatorio", DBNull.Value);
                        if (datos.idCategoria != null) command.Parameters.AddWithValue("@idCategoria", datos.idCategoria); else command.Parameters.AddWithValue("@idCategoria", DBNull.Value);
                        if (datos.idSubcategoria != null) command.Parameters.AddWithValue("@idSubcategoria", datos.idSubcategoria); else command.Parameters.AddWithValue("@idSubcategoria", DBNull.Value);
                        command.Parameters.AddWithValue("@Status", datos.Status);

                        await command.ExecuteNonQueryAsync();
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


        [HttpPost]
        [Route("ListasPreguntas/GuardarConstructor")]
        public async Task<IActionResult> GuardarConstructor([FromBody] Pregunta datos, string empresa, string cadena)
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
                    if (datos.id == null)
                    {
                        datos.id = insertado;
                    };
                    if (await Existe((Guid)datos.id, empresa, cadena))
                    {
                        sQuery = string.Format("UPDATE ListasPreguntas SET  idEmpresa = @idEmpresa, idLista = @idLista, Pregunta = @Pregunta, Explicacion = @Explicacion, Tipo = @Tipo, Valor = @Valor, Obligatorio = @Obligatorio, Status = @Status, valorCorrecto = @respuestaCorrecta where id = '{0}'", datos.id);
                        actualiza = true;
                    }
                    else
                    {
                        sQuery = string.Format("INSERT INTO ListasPreguntas (id, idEmpresa, idLista, Pregunta, Explicacion, Tipo, Valor, Obligatorio, valorCorrecto) values ('{0}',@idEmpresa,@idLista,@Pregunta,@Explicacion, @Tipo, @Valor, @Obligatorio, @respuestaCorrecta)", insertado);
                    }
                    using (SqlCommand command = new SqlCommand(sQuery, connection))
                    {

                        if (datos.idEmpresa != null) command.Parameters.AddWithValue("@idEmpresa", datos.idEmpresa); else command.Parameters.AddWithValue("@idEmpresa", DBNull.Value);
                        if (datos.idLista != null) command.Parameters.AddWithValue("@idLista", datos.idLista); else command.Parameters.AddWithValue("@idLista", DBNull.Value);
                        if (datos.pregunta != null) command.Parameters.AddWithValue("@Pregunta", datos.pregunta); else command.Parameters.AddWithValue("@Pregunta", DBNull.Value);
                        if (datos.Explicacion != null) command.Parameters.AddWithValue("@Explicacion", datos.Explicacion); else command.Parameters.AddWithValue("@Explicacion", DBNull.Value);
                        if (datos.Tipo != null) command.Parameters.AddWithValue("@Tipo", datos.Tipo); else command.Parameters.AddWithValue("@Tipo", DBNull.Value);
                        if (datos.Valor != null) command.Parameters.AddWithValue("@Valor", datos.Valor); else command.Parameters.AddWithValue("@Valor", DBNull.Value);
                        if (datos.RespuestaCorrecta != null) command.Parameters.AddWithValue("@respuestaCorrecta", datos.RespuestaCorrecta); else command.Parameters.AddWithValue("@respuestaCorrecta", DBNull.Value);
                        if (datos.Obligatorio != null) command.Parameters.AddWithValue("@Obligatorio", datos.Obligatorio); else command.Parameters.AddWithValue("@Obligatorio", DBNull.Value);
                      

                        if (actualiza)
                        {
                            command.Parameters.AddWithValue("@Status", datos.Status);

                        }

                        await command.ExecuteNonQueryAsync();
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


        [HttpDelete]
        [Route("ListasPreguntas/Borrar")]
        public async Task<IActionResult> Borrar(Guid id, string empresa, string cadena)
        {
            try
            {

                byte[] data = Convert.FromBase64String(cadena);

                
                cadena = Encoding.UTF8.GetString(data);

                using (SqlConnection connection = new SqlConnection(cadena))
				{
                    connection.Open();
                    string sQuery = $@"UPDATE ListasPreguntas SET Status = '0' WHERE id = '{id}'";
                    using (SqlCommand command = new SqlCommand(sQuery, connection))
                    {
                        await command.ExecuteNonQueryAsync();
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

        private async Task<bool> Existe(Guid cualId, string empresa, string cadena)
        {
            bool regresa = false;
            if (cualId != Guid.Empty)
            {
                try
                {

                   

                    using (SqlConnection connection = new SqlConnection(cadena))
					{
                        connection.Open();
                        string sQuery = string.Format("SELECT COUNT(*) FROM ListasPreguntas WHERE id = '{0}'", cualId);
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
