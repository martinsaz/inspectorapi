using checklistWs.Models.RazonSocial;
using checklistWs.Services.Security;
using checklistWs.Services.Tenant;
using checklistWs.Utiles;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using System.Text;

namespace checklistWs.Controllers.RazonSocial
{
	public class RazonSocialController : Controller
	{

		private readonly IConfiguration _configuration;
		private readonly SqlConnectionFactory _connectionFactory;
		private readonly ISucursalesScopeRequestContextResolver? _scopeContext;

		public RazonSocialController(IConfiguration configuration, ISucursalesScopeRequestContextResolver? scopeContext = null)
		{
			_configuration = configuration;
			_connectionFactory = new SqlConnectionFactory(configuration);
			_scopeContext = scopeContext;
		}
		public IActionResult Index()
		{
			return View();
		}
		[HttpGet("ObtenerRazonSocial")]
		public async Task<ActionResult<IEnumerable<RazonSociales>>> ObtenerRazonSocial(Guid idEmpresa, Guid id, string empresa, string cadena)
		{
			try
			{

                byte[] data = Convert.FromBase64String(cadena);


                cadena = Encoding.UTF8.GetString(data);

                using (SqlConnection connection = new SqlConnection(cadena))
				{
					await connection.OpenAsync();

					string query = "SELECT IdEmpresa, Nombre, Representante, RFC, Direccion, Colonia, CodigoPostal, Ciudad, Estado, " +
								   "Pais, Telefono, Regimen1, Fecha, IMGFIREBASE, Id, Notas, borrado " +
								   "FROM RazonesSociales " +
								   "WHERE IdEmpresa = @IdEmpresa AND Id = @Id " +
								   "ORDER BY Nombre";

					using (SqlCommand command = new SqlCommand(query, connection))
					{
						command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
						command.Parameters.AddWithValue("@Id", id);

						using (SqlDataReader reader = await command.ExecuteReaderAsync())
						{
							List<RazonSociales> razonesSociales = new List<RazonSociales>();

							while (await reader.ReadAsync())
							{
								RazonSociales razonSocial = new RazonSociales
								{
									IdEmpresa = reader["IdEmpresa"] != DBNull.Value ? Guid.Parse(reader["IdEmpresa"].ToString()) : (Guid?)null,
									Nombre = reader["Nombre"].ToString(),
									Fecha = DateTime.Parse(reader["Fecha"].ToString()),
									Representante = reader["Representante"].ToString(),
									RFC = reader["RFC"].ToString(),
									Direccion = reader["Direccion"].ToString(),
									CodigoPostal = reader["CodigoPostal"].ToString(),
									Ciudad = reader["Ciudad"].ToString(),
									Colonia = reader["Colonia"].ToString(),
									Estado = reader["Estado"].ToString(),
									Pais = reader["Pais"].ToString(),
									Regimen1 = reader["Regimen1"].ToString(),
									Telefono = reader["Telefono"].ToString(),
									Notas = reader["Notas"].ToString(),
									IMGFIREBASE = reader["IMGFIREBASE"].ToString(),
									borrado = Convert.ToBoolean(reader["borrado"].ToString()),
									Id = reader["Id"] != DBNull.Value ? Guid.Parse(reader["Id"].ToString()) : (Guid?)null
								};

								razonesSociales.Add(razonSocial);
							}

							return Ok(razonesSociales);
						}
					}
				}
			}
			catch (Exception e)
			{
				Console.WriteLine($"Error: {e.Message}");
				return StatusCode(500, e.Message);
			}
		}

		[HttpGet("ObtenerRazonesSocialesCompleta")]
		public async Task<ActionResult<IEnumerable<RazonSocialWeb>>> ObtenerRazonesSocialesCompleta(Guid idEmpresa, string empresa, string cadena)
		{
			try
			{

                byte[] data = Convert.FromBase64String(cadena);


                cadena = Encoding.UTF8.GetString(data);

                using (SqlConnection connection = new SqlConnection(cadena))
				{
					await connection.OpenAsync();

					string query = "SELECT rs.Id, rs.IdEmpresa, rs.Nombre, rs.Fecha, rs.Representante, rs.RFC, rs.Direccion, " +
								   "rs.CodigoPostal, rs.Ciudad, rs.Colonia, rs.Estado, rs.Pais, rs.Regimen1, " +
								   "Concat(rf.c_RegimenFiscal,' ',rf.Descripcion) AS 'nombreRegimenFiscal', rs.Telefono, rs.IMGFIREBASE, rs.Notas " +
								   "FROM RazonesSociales rs " +
								   "LEFT JOIN CatalogoClientesRegimenFiscal rf ON rs.Regimen1 = rf.Id " +
								   "WHERE rs.IdEmpresa = @IdEmpresa AND rs.borrado = 0 " +
								   "ORDER BY rs.Nombre";

					using (SqlCommand command = new SqlCommand(query, connection))
					{
						command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);

						using (SqlDataReader reader = await command.ExecuteReaderAsync())
						{
							List<RazonSocialWeb> razonesSociales = new List<RazonSocialWeb>();

							while (await reader.ReadAsync())
							{
								RazonSocialWeb razonSocial = new RazonSocialWeb
								{
									IdEmpresa = reader["IdEmpresa"] != DBNull.Value ? Guid.Parse(reader["IdEmpresa"].ToString()) : (Guid?)null,
									Nombre = reader["Nombre"].ToString(),
									Fecha = DateTime.Parse(reader["Fecha"].ToString()),
									Representante = reader["Representante"].ToString(),
									RFC = reader["RFC"].ToString(),
									Direccion = reader["Direccion"].ToString(),
									CodigoPostal = reader["CodigoPostal"].ToString(),
									Ciudad = reader["Ciudad"].ToString(),
									Colonia = reader["Colonia"].ToString(),
									Estado = reader["Estado"].ToString(),
									Pais = reader["Pais"].ToString(),
									Regimen1 = reader["Regimen1"].ToString(),
									NombreRegimen1 = reader["nombreRegimenFiscal"].ToString(),
									Telefono = reader["Telefono"].ToString(),
									Notas = reader["Notas"].ToString(),
									IMGFIREBASE = reader["IMGFIREBASE"].ToString(),
									Id = reader["Id"] != DBNull.Value ? Guid.Parse(reader["Id"].ToString()) : (Guid?)null
								};

								razonesSociales.Add(razonSocial);
							}

							return Ok(razonesSociales);
						}
					}
				}
			}
			catch (Exception e)
			{
				Console.WriteLine($"Error: {e.Message}");
				return StatusCode(500, e.Message);
			}
		}

		[HttpGet("ObtenerRazonesSociales")]
		public async Task<IActionResult> ObtenerRazonesSociales(Guid idEmpresa, string empresa, string cadena, string estatus = "")
		{
			SucursalesScopeRequestContext? context = await ResolveSucursalesContextAsync(ProductosServiciosAuthorizationDefaults.RazonesSocialesPermissionCode, ProductosServiciosPermissionRequirement.Read);
			if (context == null) return _scopeContext?.ToErrorResult(this) ?? StatusCode(503, "Razones Sociales no está disponible temporalmente.");

			try
			{

				using (SqlConnection connection = _scopeContext!.CreateConnection(context))
				{
					await connection.OpenAsync();

					string statusClause = string.Equals(estatus, "inactivo", StringComparison.OrdinalIgnoreCase)
						? " AND ISNULL(borrado, 0) = 1 "
						: string.Equals(estatus, "todos", StringComparison.OrdinalIgnoreCase)
							? string.Empty
							: " AND ISNULL(borrado, 0) = 0 ";
					string query = "SELECT IdEmpresa, Nombre, Representante, RFC, Direccion, Colonia, CodigoPostal, Ciudad, Estado, " +
								   "Pais, Telefono, Regimen1, Fecha, IMGFIREBASE, Id, Notas, borrado " +
								   "FROM RazonesSociales " +
								   "WHERE IdEmpresa = @IdEmpresa " + statusClause +
								   "ORDER BY Nombre";

					using (SqlCommand command = new SqlCommand(query, connection))
					{
						command.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);

						using (SqlDataReader reader = await command.ExecuteReaderAsync())
						{
							List<RazonSociales> razonesSociales = new List<RazonSociales>();

							while (await reader.ReadAsync())
							{
								RazonSociales razonSocial = new RazonSociales
								{
									IdEmpresa = reader["IdEmpresa"] != DBNull.Value ? Guid.Parse(reader["IdEmpresa"].ToString()) : (Guid?)null,
									Nombre = reader["Nombre"].ToString(),
									Fecha = DateTime.Parse(reader["Fecha"].ToString()),
									Representante = reader["Representante"].ToString(),
									RFC = reader["RFC"].ToString(),
									Direccion = reader["Direccion"].ToString(),
									CodigoPostal = reader["CodigoPostal"].ToString(),
									Ciudad = reader["Ciudad"].ToString(),
									Colonia = reader["Colonia"].ToString(),
									Estado = reader["Estado"].ToString(),
									Pais = reader["Pais"].ToString(),
									Regimen1 = reader["Regimen1"].ToString(),
									Telefono = reader["Telefono"].ToString(),
									Notas = reader["Notas"].ToString(),
									IMGFIREBASE = reader["IMGFIREBASE"].ToString(),
									Id = reader["Id"] != DBNull.Value ? Guid.Parse(reader["Id"].ToString()) : (Guid?)null,
									borrado = reader["borrado"] != DBNull.Value && Convert.ToBoolean(reader["borrado"])
								};

								razonesSociales.Add(razonSocial);
							}

							return Ok(razonesSociales);
						}
					}
				}
			}
			catch (Exception e)
			{
				Console.WriteLine($"Error: {e.Message}");
				return StatusCode(500, e.Message);
			}
		}

        [HttpPut("ActualizarRazonSocial")]
        public async Task<IActionResult> ActualizarRazonSocial(Guid id, [FromBody] RazonSociales razonSocialActualizada, Guid idEmpresa, string empresa, string cadena)
        {
            IActionResult? auth = await AuthorizeSucursalesAsync(ProductosServiciosAuthorizationDefaults.RazonesSocialesPermissionCode, ProductosServiciosPermissionRequirement.Write);
            if (auth != null) return auth;

            try
            {
                byte[] data = Convert.FromBase64String(cadena);
                cadena = Encoding.UTF8.GetString(data);

                using (SqlConnection connection = new SqlConnection(cadena))
                {
                    await connection.OpenAsync();

                    string query = "UPDATE RazonesSociales SET Nombre = @Nombre, Representante = @Representante, RFC = @RFC, Direccion = @Direccion, " +
                                   "Colonia = @Colonia, CodigoPostal = @CodigoPostal, Ciudad = @Ciudad, Estado = @Estado, Pais = @Pais, " +
                                   "Telefono = @Telefono, Regimen1 = @Regimen1, Fecha = @Fecha, IMGFIREBASE = @IMGFIREBASE, Notas = @Notas " +
                                   "WHERE Id = @Id AND IdEmpresa = @IdEmpresa";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Nombre", string.IsNullOrEmpty(razonSocialActualizada.Nombre) ? (object)DBNull.Value : razonSocialActualizada.Nombre);
                        command.Parameters.AddWithValue("@Representante", string.IsNullOrEmpty(razonSocialActualizada.Representante) ? (object)DBNull.Value : razonSocialActualizada.Representante);
                        command.Parameters.AddWithValue("@RFC", string.IsNullOrEmpty(razonSocialActualizada.RFC) ? (object)DBNull.Value : razonSocialActualizada.RFC);
                        command.Parameters.AddWithValue("@Direccion", string.IsNullOrEmpty(razonSocialActualizada.Direccion) ? (object)DBNull.Value : razonSocialActualizada.Direccion);
                        command.Parameters.AddWithValue("@Colonia", string.IsNullOrEmpty(razonSocialActualizada.Colonia) ? (object)DBNull.Value : razonSocialActualizada.Colonia);
                        command.Parameters.AddWithValue("@CodigoPostal", string.IsNullOrEmpty(razonSocialActualizada.CodigoPostal) ? (object)DBNull.Value : razonSocialActualizada.CodigoPostal);
                        command.Parameters.AddWithValue("@Ciudad", string.IsNullOrEmpty(razonSocialActualizada.Ciudad) ? (object)DBNull.Value : razonSocialActualizada.Ciudad);
                        command.Parameters.AddWithValue("@Estado", string.IsNullOrEmpty(razonSocialActualizada.Estado) ? (object)DBNull.Value : razonSocialActualizada.Estado);
                        command.Parameters.AddWithValue("@Pais", string.IsNullOrEmpty(razonSocialActualizada.Pais) ? (object)DBNull.Value : razonSocialActualizada.Pais);
                        command.Parameters.AddWithValue("@Telefono", string.IsNullOrEmpty(razonSocialActualizada.Telefono) ? (object)DBNull.Value : razonSocialActualizada.Telefono);
                        command.Parameters.AddWithValue("@Regimen1", string.IsNullOrEmpty(razonSocialActualizada.Regimen1) ? (object)DBNull.Value : razonSocialActualizada.Regimen1);
                        command.Parameters.AddWithValue("@Fecha", razonSocialActualizada.Fecha ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@IMGFIREBASE", string.IsNullOrEmpty(razonSocialActualizada.IMGFIREBASE) ? (object)DBNull.Value : razonSocialActualizada.IMGFIREBASE);
                        string notas = CheckAppRichTextSanitizer.Sanitize(razonSocialActualizada.Notas);
                        command.Parameters.AddWithValue("@Notas", string.IsNullOrEmpty(notas) ? (object)DBNull.Value : notas);
                        command.Parameters.AddWithValue("@Id", id);
                        command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);

                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            return Ok("Ok");
                        }
                        else
                        {
                            return NotFound();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return StatusCode(500, e.Message);
            }
        }

		[HttpPost("BajaRazonSocial")]
		public async Task<IActionResult> BajaRazonSocial(Guid id)
		{
			return await CambiarEstatusRazonSocialAsync(id, true);
		}

		[HttpPost("ReactivarRazonSocial")]
		public async Task<IActionResult> ReactivarRazonSocial(Guid id)
		{
			return await CambiarEstatusRazonSocialAsync(id, false);
		}

		private async Task<IActionResult> CambiarEstatusRazonSocialAsync(Guid id, bool borrado)
		{
			SucursalesScopeRequestContext? context = await ResolveSucursalesContextAsync(ProductosServiciosAuthorizationDefaults.RazonesSocialesPermissionCode, ProductosServiciosPermissionRequirement.Write);
			if (context == null) return _scopeContext?.ToErrorResult(this) ?? StatusCode(503, "Razones Sociales no está disponible temporalmente.");

			try
			{
				const string query = @"UPDATE RazonesSociales
									   SET borrado = @Borrado
									   WHERE Id = @Id AND IdEmpresa = @IdEmpresa";

				using (SqlConnection connection = _scopeContext!.CreateConnection(context))
				{
					await connection.OpenAsync();
					using (SqlCommand command = new SqlCommand(query, connection))
					{
						command.Parameters.AddWithValue("@Borrado", borrado);
						command.Parameters.AddWithValue("@Id", id);
						command.Parameters.AddWithValue("@IdEmpresa", context.IdEmpresa);
						int rowsAffected = await command.ExecuteNonQueryAsync();
						if (rowsAffected == 0)
						{
							return NotFound("La razón social no fue encontrada.");
						}
					}
				}

				return Ok("Ok");
			}
			catch (Exception e)
			{
				Console.WriteLine($"Error: {e.Message}");
				return StatusCode(500, e.Message);
			}
		}

        [HttpPost("InsertarRazonSocial")]
        public async Task<IActionResult> InsertarRazonSocial([FromBody] RazonSociales nuevaRazonSocial, string empresa, string cadena)
        {
            IActionResult? auth = await AuthorizeSucursalesAsync(ProductosServiciosAuthorizationDefaults.RazonesSocialesPermissionCode, ProductosServiciosPermissionRequirement.Write);
            if (auth != null) return auth;

            try
            {
                byte[] data = Convert.FromBase64String(cadena);
                cadena = Encoding.UTF8.GetString(data);

                using (SqlConnection connection = new SqlConnection(cadena))
                {
                    await connection.OpenAsync();

                    string query = "INSERT INTO RazonesSociales (IdEmpresa, Nombre, Representante, RFC, Direccion, Colonia, CodigoPostal, Ciudad, Estado, " +
                                   "Pais, Telefono, Regimen1, Fecha, IMGFIREBASE, Id, Notas, borrado) " +
                                   "VALUES (@IdEmpresa, @Nombre, @Representante, @RFC, @Direccion, @Colonia, @CodigoPostal, @Ciudad, @Estado, " +
                                   "@Pais, @Telefono, @Regimen1, @Fecha, @IMGFIREBASE, @Id, @Notas, 0)";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@IdEmpresa", nuevaRazonSocial.IdEmpresa);
                        command.Parameters.AddWithValue("@Nombre", string.IsNullOrEmpty(nuevaRazonSocial.Nombre) ? (object)DBNull.Value : nuevaRazonSocial.Nombre);
                        command.Parameters.AddWithValue("@Representante", string.IsNullOrEmpty(nuevaRazonSocial.Representante) ? (object)DBNull.Value : nuevaRazonSocial.Representante);
                        command.Parameters.AddWithValue("@RFC", string.IsNullOrEmpty(nuevaRazonSocial.RFC) ? (object)DBNull.Value : nuevaRazonSocial.RFC);
                        command.Parameters.AddWithValue("@Direccion", string.IsNullOrEmpty(nuevaRazonSocial.Direccion) ? (object)DBNull.Value : nuevaRazonSocial.Direccion);
                        command.Parameters.AddWithValue("@Colonia", string.IsNullOrEmpty(nuevaRazonSocial.Colonia) ? (object)DBNull.Value : nuevaRazonSocial.Colonia);
                        command.Parameters.AddWithValue("@CodigoPostal", string.IsNullOrEmpty(nuevaRazonSocial.CodigoPostal) ? (object)DBNull.Value : nuevaRazonSocial.CodigoPostal);
                        command.Parameters.AddWithValue("@Ciudad", string.IsNullOrEmpty(nuevaRazonSocial.Ciudad) ? (object)DBNull.Value : nuevaRazonSocial.Ciudad);
                        command.Parameters.AddWithValue("@Estado", string.IsNullOrEmpty(nuevaRazonSocial.Estado) ? (object)DBNull.Value : nuevaRazonSocial.Estado);
                        command.Parameters.AddWithValue("@Pais", string.IsNullOrEmpty(nuevaRazonSocial.Pais) ? (object)DBNull.Value : nuevaRazonSocial.Pais);
                        command.Parameters.AddWithValue("@Telefono", string.IsNullOrEmpty(nuevaRazonSocial.Telefono) ? (object)DBNull.Value : nuevaRazonSocial.Telefono);
                        command.Parameters.AddWithValue("@Regimen1", string.IsNullOrEmpty(nuevaRazonSocial.Regimen1) ? (object)DBNull.Value : nuevaRazonSocial.Regimen1);
                        command.Parameters.AddWithValue("@Fecha", nuevaRazonSocial.Fecha ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@IMGFIREBASE", string.IsNullOrEmpty(nuevaRazonSocial.IMGFIREBASE) ? (object)DBNull.Value : nuevaRazonSocial.IMGFIREBASE);
                        command.Parameters.AddWithValue("@Id", Guid.NewGuid());
                        string notas = CheckAppRichTextSanitizer.Sanitize(nuevaRazonSocial.Notas);
                        command.Parameters.AddWithValue("@Notas", string.IsNullOrEmpty(notas) ? (object)DBNull.Value : notas);

                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            return Ok("Ok");
                        }
                        else
                        {
                            return BadRequest("Error inserting the record.");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return StatusCode(500, e.Message);
            }
        }


        [HttpPost("InsertarPrimerRazonSocial")]
		public async Task<ActionResult> InsertarPrimerRazonSocial([FromBody] RazonSociales nuevaRazonSocial, string empresa, string cadena)
		{
			try
			{

				byte[] data = Convert.FromBase64String(cadena);


				cadena = Encoding.UTF8.GetString(data);

				using (SqlConnection connection = new SqlConnection(cadena))
				{
					await connection.OpenAsync();

					string query = "INSERT INTO RazonesSociales (Id,IdEmpresa, Nombre, Representante, RFC, Direccion, Colonia, CodigoPostal, Ciudad, Estado, " +
								   "Pais, Telefono, Regimen1, Fecha, IMGFIREBASE, Notas, borrado) " +
								   "VALUES (@Id,@IdEmpresa, @Nombre, @Representante, @RFC, @Direccion, @Colonia, @CodigoPostal, @Ciudad, @Estado, " +
								   "@Pais, @Telefono, @Regimen1, @Fecha, @IMGFIREBASE, @Notas, 0)";

					using (SqlCommand command = new SqlCommand(query, connection))
					{
						command.Parameters.AddWithValue("@IdEmpresa", nuevaRazonSocial.IdEmpresa);
						command.Parameters.AddWithValue("@Nombre", nuevaRazonSocial.Nombre);
						command.Parameters.AddWithValue("@Representante", nuevaRazonSocial.Representante);
						command.Parameters.AddWithValue("@RFC", nuevaRazonSocial.RFC);
						command.Parameters.AddWithValue("@Direccion", nuevaRazonSocial.Direccion);
						command.Parameters.AddWithValue("@Colonia", nuevaRazonSocial.Colonia);
						command.Parameters.AddWithValue("@CodigoPostal", nuevaRazonSocial.CodigoPostal);
						command.Parameters.AddWithValue("@Ciudad", nuevaRazonSocial.Ciudad);
						command.Parameters.AddWithValue("@Estado", nuevaRazonSocial.Estado);
						command.Parameters.AddWithValue("@Pais", nuevaRazonSocial.Pais);
						command.Parameters.AddWithValue("@Telefono", nuevaRazonSocial.Telefono);
						command.Parameters.AddWithValue("@Regimen1", nuevaRazonSocial.Regimen1);
						command.Parameters.AddWithValue("@Fecha", nuevaRazonSocial.Fecha);
						command.Parameters.AddWithValue("@IMGFIREBASE", nuevaRazonSocial.IMGFIREBASE);
						command.Parameters.AddWithValue("@Id", nuevaRazonSocial.Id);
							string notas = CheckAppRichTextSanitizer.Sanitize(nuevaRazonSocial.Notas);
							command.Parameters.AddWithValue("@Notas", string.IsNullOrEmpty(notas) ? (object)DBNull.Value : notas);

						int rowsAffected = await command.ExecuteNonQueryAsync();

						if (rowsAffected > 0)
						{

							return Ok("Ok");
						}
						else
						{
							return BadRequest("Error inserting the record.");
						}
					}
				}
			}
			catch (Exception e)
			{
				Console.WriteLine($"Error: {e.Message}");
				return StatusCode(500, e.Message);
			}
		}

		private async Task<IActionResult?> AuthorizeSucursalesAsync(string permissionCode, ProductosServiciosPermissionRequirement requirement)
		{
			SucursalesScopeRequestContext? context = await ResolveSucursalesContextAsync(permissionCode, requirement);
			return context == null ? _scopeContext?.ToErrorResult(this) : null;
		}

		private Task<SucursalesScopeRequestContext?> ResolveSucursalesContextAsync(string permissionCode, ProductosServiciosPermissionRequirement requirement)
		{
			return _scopeContext == null
				? Task.FromResult<SucursalesScopeRequestContext?>(null)
				: _scopeContext.TryResolveAsync(this, permissionCode, requirement);
		}
		}
	}
