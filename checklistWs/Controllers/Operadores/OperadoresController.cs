using System.Data;
using System.Data.SqlClient;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Text;
using checklistWs.Models.Operadores;
using checklistWs.Utiles;
using Microsoft.AspNetCore.Mvc;

namespace checklistWs.Controllers.Operadores
{
    [Route("api/[controller]")]
    [ApiController]
    public class OperadoresController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly OperatorFirebaseIdentityService _firebaseIdentityService;

        public OperadoresController(IConfiguration configuration)
        {
            _configuration = configuration;
            _firebaseIdentityService = new OperatorFirebaseIdentityService(configuration);
        }

        [HttpGet("ObtenerOperadores")]
        public async Task<IActionResult> ObtenerOperadores(
            Guid idEmpresa,
            string cadena,
            string busqueda = "",
            Guid? idSucursal = null,
            string estado = "")
        {
            try
            {
                using SqlConnection connection = CreateConnection(cadena);
                await connection.OpenAsync();

                StringBuilder query = new StringBuilder(@"
SELECT
    o.id,
    o.idEmpresa,
    o.idFirebase,
    o.nombre,
    o.apellidoPaterno,
    o.apellidoMaterno,
    o.correo,
    o.estatus,
    o.activo,
    o.fechaAlta,
    o.fechaSuspension,
    o.versionRow,
    os.idSucursal,
    os.activo AS sucursalActiva,
    s.Nombre AS nombreSucursal
FROM dbo.Operadores o
LEFT JOIN dbo.OperadoresSucursales os ON os.idOperador = o.id
LEFT JOIN dbo.Sucursales s ON s.id = os.idSucursal
WHERE o.idEmpresa = @IdEmpresa");

                using SqlCommand command = new SqlCommand();
                command.Connection = connection;
                command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);

                if (!string.IsNullOrWhiteSpace(busqueda))
                {
                    query.Append(@"
  AND (
        o.nombre LIKE @Busqueda
        OR o.apellidoPaterno LIKE @Busqueda
        OR ISNULL(o.apellidoMaterno, '') LIKE @Busqueda
        OR o.correo LIKE @Busqueda
        OR CONCAT(o.nombre, ' ', o.apellidoPaterno, ' ', ISNULL(o.apellidoMaterno, '')) LIKE @Busqueda
      )");
                    command.Parameters.AddWithValue("@Busqueda", $"%{busqueda.Trim()}%");
                }

                if (idSucursal.HasValue && idSucursal.Value != Guid.Empty)
                {
                    query.Append(" AND os.idSucursal = @IdSucursal AND os.activo = 1");
                    command.Parameters.AddWithValue("@IdSucursal", idSucursal.Value);
                }

                if (!string.IsNullOrWhiteSpace(estado))
                {
                    switch (estado.Trim().ToLowerInvariant())
                    {
                        case "activos":
                            query.Append(" AND o.activo = 1 AND o.estatus = 1");
                            break;
                        case "suspendidos":
                        case "inactivos":
                            query.Append(" AND o.activo = 0");
                            break;
                    }
                }

                query.Append(" ORDER BY o.nombre, o.apellidoPaterno, o.apellidoMaterno, s.Nombre");
                command.CommandText = query.ToString();

                Dictionary<Guid, OperadorDetalleDto> operadores = new Dictionary<Guid, OperadorDetalleDto>();
                using SqlDataReader reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    Guid idOperador = ReadGuid(reader, "id");
                    if (!operadores.TryGetValue(idOperador, out OperadorDetalleDto? operador))
                    {
                        operador = new OperadorDetalleDto
                        {
                            IdOperador = idOperador,
                            IdEmpresa = ReadGuid(reader, "idEmpresa"),
                            IdFirebase = ReadString(reader, "idFirebase"),
                            Nombre = ReadString(reader, "nombre"),
                            ApellidoPaterno = ReadString(reader, "apellidoPaterno"),
                            ApellidoMaterno = ReadString(reader, "apellidoMaterno"),
                            NombreCompleto = BuildNombreCompleto(
                                ReadString(reader, "nombre"),
                                ReadString(reader, "apellidoPaterno"),
                                ReadString(reader, "apellidoMaterno")),
                            Correo = ReadString(reader, "correo"),
                            Activo = ReadBool(reader, "activo"),
                            Estatus = ReadByte(reader, "estatus"),
                            FechaAlta = ReadDateTime(reader, "fechaAlta"),
                            FechaSuspension = ReadDateTime(reader, "fechaSuspension"),
                            VersionRow = ReadVersionRow(reader, "versionRow")
                        };
                        operadores[idOperador] = operador;
                    }

                    Guid idSucursalActual = ReadGuid(reader, "idSucursal");
                    if (idSucursalActual != Guid.Empty)
                    {
                        operador.Sucursales.Add(new OperadorSucursalDto
                        {
                            IdSucursal = idSucursalActual,
                            Sucursal = ReadString(reader, "nombreSucursal"),
                            Activo = ReadBool(reader, "sucursalActiva")
                        });
                    }
                }

                List<OperadorListadoDto> respuesta = operadores.Values
                    .Select(item => new OperadorListadoDto
                    {
                        IdOperador = item.IdOperador,
                        IdEmpresa = item.IdEmpresa,
                        IdFirebase = item.IdFirebase,
                        Nombre = item.Nombre,
                        ApellidoPaterno = item.ApellidoPaterno,
                        ApellidoMaterno = item.ApellidoMaterno,
                        NombreCompleto = item.NombreCompleto,
                        Correo = item.Correo,
                        Sucursales = string.Join(", ", item.Sucursales.Where(s => s.Activo).Select(s => s.Sucursal).Distinct()),
                        Activo = item.Activo,
                        Estatus = item.Estatus,
                        CorreoVerificado = item.CorreoVerificado,
                        Estado = ResolveEstado(item.Activo, item.Estatus, item.CorreoVerificado),
                        FechaAlta = item.FechaAlta,
                        FechaSuspension = item.FechaSuspension,
                        VersionRow = item.VersionRow,
                        SucursalesDetalle = item.Sucursales
                            .Where(sucursal => sucursal.Activo)
                            .GroupBy(sucursal => sucursal.IdSucursal)
                            .Select(group => group.First())
                            .ToList()
                    })
                    .OrderBy(item => item.NombreCompleto)
                    .ToList();

                await EnrichWithVerificationStateAsync(respuesta);

                return Ok(respuesta);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new OperadorOperacionResponse { Mensaje = $"Error interno del servidor: {ex.Message}" });
            }
        }

        [HttpGet("ObtenerOperador")]
        public async Task<IActionResult> ObtenerOperador(Guid idEmpresa, Guid idOperador, string cadena)
        {
            try
            {
                using SqlConnection connection = CreateConnection(cadena);
                await connection.OpenAsync();

                OperadorDetalleDto? operador = await ObtenerOperadorDetalleAsync(connection, null, idEmpresa, idOperador);
                if (operador == null)
                {
                    return NotFound(new OperadorOperacionResponse { Mensaje = "El operador no está disponible." });
                }

                await EnrichWithVerificationStateAsync(new List<OperadorDetalleDto> { operador });

                return Ok(operador);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new OperadorOperacionResponse { Mensaje = $"Error interno del servidor: {ex.Message}" });
            }
        }

        [HttpGet("ObtenerAccesoOperador")]
        public async Task<IActionResult> ObtenerAccesoOperador(Guid idEmpresa, string idFirebase, string cadena)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(idFirebase))
                {
                    return Ok(new OperadorAccesoDto());
                }

                using SqlConnection connection = CreateConnection(cadena);
                await connection.OpenAsync();

                OperadorDetalleDto? operador = await ObtenerOperadorPorFirebaseAsync(connection, null, idEmpresa, idFirebase.Trim());
                if (operador == null)
                {
                    return Ok(new OperadorAccesoDto());
                }

                bool operadorActivo = operador.Activo && operador.Estatus == 1;
                List<OperadorSucursalDto> sucursalesActivas = operador.Sucursales
                    .Where(item => item.Activo)
                    .GroupBy(item => item.IdSucursal)
                    .Select(group => group.First())
                    .ToList();

                OperadorAccesoDto acceso = new OperadorAccesoDto
                {
                    TieneAcceso = operadorActivo && sucursalesActivas.Count > 0,
                    OperadorActivo = operadorActivo,
                    CuentaActiva = operadorActivo,
                    IdOperador = operador.IdOperador,
                    IdEmpresa = operador.IdEmpresa,
                    IdFirebase = operador.IdFirebase,
                    NombreCompleto = operador.NombreCompleto,
                    Correo = operador.Correo,
                    Estado = ResolveEstado(operador.Activo, operador.Estatus, null),
                    VersionRow = operador.VersionRow,
                    Sucursales = sucursalesActivas
                };

                return Ok(acceso);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new OperadorOperacionResponse { Mensaje = $"Error interno del servidor: {ex.Message}" });
            }
        }

        [HttpGet("ObtenerCandidatoIdentidadDual")]
        public async Task<IActionResult> ObtenerCandidatoIdentidadDual(Guid idEmpresa, string cadena, string correo)
        {
            try
            {
                string correoNormalizado = NormalizeEmail(correo);
                using SqlConnection connection = CreateConnection(cadena);
                await connection.OpenAsync();

                OperadorIdentidadDualCandidatoDto candidato = await ObtenerCandidatoIdentidadDualAsync(connection, null, idEmpresa, correoNormalizado);
                return Ok(candidato);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new OperadorOperacionResponse { Mensaje = ResolveOperatorError(ex) });
            }
        }

        [HttpPost("Crear")]
        public async Task<IActionResult> Crear([FromBody] CrearOperadorRequest request, Guid idEmpresa, string cadena, string empresa)
        {
            string correoNormalizado = NormalizeEmail(request.Correo);
            string nombreCompleto = BuildNombreCompleto(request.Nombre, request.ApellidoPaterno, request.ApellidoMaterno);
            string firebaseUid = string.Empty;

            try
            {
                string validacion = ValidateCreateRequest(request, idEmpresa, cadena, empresa);
                if (!string.IsNullOrWhiteSpace(validacion))
                {
                    return BadRequest(new OperadorOperacionResponse { Mensaje = validacion });
                }

                List<Guid> sucursales = request.Sucursales
                    .Where(item => item != Guid.Empty)
                    .Distinct()
                    .ToList();

                using SqlConnection connection = CreateConnection(cadena);
                await connection.OpenAsync();

                await EnsureEmailAvailableAsync(connection, null, correoNormalizado);
                await EnsureSucursalesBelongToEmpresaAsync(connection, null, idEmpresa, sucursales);

                OperatorFirebaseCreateResult firebaseResult = await _firebaseIdentityService.CreateOperatorAsync(
                    nombreCompleto,
                    correoNormalizado,
                    request.Password,
                    empresa,
                    idEmpresa,
                    Guid.NewGuid(),
                    true);
                firebaseUid = firebaseResult.Uid;

                using SqlTransaction transaction = connection.BeginTransaction();

                try
                {
                    Guid nuevoId = Guid.NewGuid();
                    Guid? actorId = await ObtenerActorIdAsync(connection, transaction, idEmpresa, request.CorreoActor);

                    string insertOperador = @"
INSERT INTO dbo.Operadores
    (id, idEmpresa, idFirebase, nombre, apellidoPaterno, apellidoMaterno, correo, estatus, activo, fechaAlta, fechaCreacion, creadoPor)
VALUES
    (@Id, @IdEmpresa, @IdFirebase, @Nombre, @ApellidoPaterno, @ApellidoMaterno, @Correo, 1, 1, GETDATE(), GETDATE(), @CreadoPor)";

                    using (SqlCommand insertCommand = new SqlCommand(insertOperador, connection, transaction))
                    {
                        insertCommand.Parameters.AddWithValue("@Id", nuevoId);
                        insertCommand.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                        insertCommand.Parameters.AddWithValue("@IdFirebase", firebaseUid);
                        insertCommand.Parameters.AddWithValue("@Nombre", request.Nombre.Trim());
                        insertCommand.Parameters.AddWithValue("@ApellidoPaterno", request.ApellidoPaterno.Trim());
                        insertCommand.Parameters.AddWithValue("@ApellidoMaterno", ToDbValue(request.ApellidoMaterno));
                        insertCommand.Parameters.AddWithValue("@Correo", correoNormalizado);
                        insertCommand.Parameters.AddWithValue("@CreadoPor", actorId.HasValue ? actorId.Value : DBNull.Value);
                        await insertCommand.ExecuteNonQueryAsync();
                    }

                    await InsertSucursalesAsync(connection, transaction, nuevoId, sucursales, actorId);
                    transaction.Commit();

                    await _firebaseIdentityService.UpdateOperatorNodeAsync(firebaseUid, nombreCompleto, correoNormalizado, empresa, idEmpresa, nuevoId, true);

                    OperadorDetalleDto? detalle = await ObtenerOperadorDetalleAsync(connection, null, idEmpresa, nuevoId);
                    return Ok(new OperadorOperacionResponse
                    {
                        Ok = true,
                        Mensaje = firebaseResult.VerificationEmailSent
                            ? "El Operador fue registrado. Se envió un correo para verificar su cuenta."
                            : "El Operador fue registrado, pero no fue posible enviar el correo de verificación. Puedes reenviarlo desde el listado.",
                        VersionRow = detalle?.VersionRow ?? string.Empty
                    });
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (InvalidOperationException ex)
            {
                if (!string.IsNullOrWhiteSpace(firebaseUid))
                {
                    await _firebaseIdentityService.DeleteProvisionedOperatorAsync(correoNormalizado, request.Password, firebaseUid);
                }

                return BuildOperatorErrorResult(ex);
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrWhiteSpace(firebaseUid))
                {
                    await _firebaseIdentityService.DeleteProvisionedOperatorAsync(correoNormalizado, request.Password, firebaseUid);
                }

                return BuildOperatorErrorResult(ex);
            }
        }

        [HttpPost("VincularIdentidadExistente")]
        public async Task<IActionResult> VincularIdentidadExistente([FromBody] VincularOperadorExistenteRequest request, Guid idEmpresa, string cadena, string empresa)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new OperadorOperacionResponse { Mensaje = "No fue posible completar la asignación. Revisa la información de la persona." });
                }

                string correoNormalizado = NormalizeEmail(request.Correo);
                if (!IsValidEmail(correoNormalizado))
                {
                    return BadRequest(new OperadorOperacionResponse { Mensaje = "Ingresa un correo válido." });
                }

                List<Guid> sucursales = request.Sucursales
                    .Where(item => item != Guid.Empty)
                    .Distinct()
                    .ToList();

                if (!sucursales.Any())
                {
                    return BadRequest(new OperadorOperacionResponse { Mensaje = "Selecciona al menos una sucursal." });
                }

                using SqlConnection connection = CreateConnection(cadena);
                await connection.OpenAsync();
                using SqlTransaction transaction = connection.BeginTransaction();

                await EnsureSucursalesBelongToEmpresaAsync(connection, transaction, idEmpresa, sucursales);
                OperadorIdentidadDualCandidatoDto candidato = await ObtenerCandidatoIdentidadDualAsync(connection, transaction, idEmpresa, correoNormalizado);

                if (!candidato.IdentidadValida)
                {
                    transaction.Rollback();
                    return BadRequest(new OperadorOperacionResponse
                    {
                        Mensaje = string.IsNullOrWhiteSpace(candidato.Mensaje)
                            ? "No fue posible completar la asignación. Revisa la información de la persona."
                            : candidato.Mensaje
                    });
                }

                if (candidato.YaEsOperador)
                {
                    transaction.Rollback();
                    return Ok(new OperadorOperacionResponse
                    {
                        Ok = true,
                        Mensaje = "La persona ya cuenta con ambos accesos."
                    });
                }

                Guid nuevoId = Guid.NewGuid();
                Guid? actorId = await ObtenerActorIdAsync(connection, transaction, idEmpresa, request.CorreoActor);
                string insertOperador = @"
INSERT INTO dbo.Operadores
    (id, idEmpresa, idFirebase, nombre, apellidoPaterno, apellidoMaterno, correo, estatus, activo, fechaAlta, fechaCreacion, creadoPor)
VALUES
    (@Id, @IdEmpresa, @IdFirebase, @Nombre, @ApellidoPaterno, @ApellidoMaterno, @Correo, 1, 1, GETDATE(), GETDATE(), @CreadoPor)";

                using (SqlCommand insertCommand = new SqlCommand(insertOperador, connection, transaction))
                {
                    insertCommand.Parameters.AddWithValue("@Id", nuevoId);
                    insertCommand.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                    insertCommand.Parameters.AddWithValue("@IdFirebase", candidato.IdFirebase);
                    insertCommand.Parameters.AddWithValue("@Nombre", candidato.Nombre.Trim());
                    insertCommand.Parameters.AddWithValue("@ApellidoPaterno", candidato.ApellidoPaterno.Trim());
                    insertCommand.Parameters.AddWithValue("@ApellidoMaterno", ToDbValue(candidato.ApellidoMaterno));
                    insertCommand.Parameters.AddWithValue("@Correo", candidato.Correo);
                    insertCommand.Parameters.AddWithValue("@CreadoPor", actorId.HasValue ? actorId.Value : DBNull.Value);
                    await insertCommand.ExecuteNonQueryAsync();
                }

                await InsertSucursalesAsync(connection, transaction, nuevoId, sucursales, actorId);
                transaction.Commit();

                await _firebaseIdentityService.UpdateOperatorNodeAsync(
                    candidato.IdFirebase,
                    candidato.NombreCompleto,
                    candidato.Correo,
                    empresa,
                    idEmpresa,
                    nuevoId,
                    true);

                return Ok(new OperadorOperacionResponse
                {
                    Ok = true,
                    Mensaje = "El acceso como operador fue agregado correctamente."
                });
            }
            catch (Exception ex)
            {
                return BuildOperatorErrorResult(ex);
            }
        }

        [HttpPut("Actualizar")]
        public async Task<IActionResult> Actualizar([FromBody] ActualizarOperadorRequest request, Guid idEmpresa, string cadena, string empresa)
        {
            try
            {
                string validacion = ValidateUpdateRequest(request);
                if (!string.IsNullOrWhiteSpace(validacion))
                {
                    return BadRequest(new OperadorOperacionResponse { Mensaje = validacion });
                }

                List<Guid> sucursales = request.Sucursales
                    .Where(item => item != Guid.Empty)
                    .Distinct()
                    .ToList();

                using SqlConnection connection = CreateConnection(cadena);
                await connection.OpenAsync();
                using SqlTransaction transaction = connection.BeginTransaction();

                OperadorDetalleDto? operador = await ObtenerOperadorDetalleAsync(connection, transaction, idEmpresa, request.IdOperador);
                if (operador == null)
                {
                    return BadRequest(new OperadorOperacionResponse { Mensaje = "El operador no está disponible." });
                }

                await EnsureSucursalesBelongToEmpresaAsync(connection, transaction, idEmpresa, sucursales);
                Guid? actorId = await ObtenerActorIdAsync(connection, transaction, idEmpresa, request.CorreoActor);
                byte[] versionRow = DecodeVersionRow(request.VersionRow);

                string update = @"
UPDATE dbo.Operadores
SET nombre = @Nombre,
    apellidoPaterno = @ApellidoPaterno,
    apellidoMaterno = @ApellidoMaterno,
    fechaModificacion = GETDATE(),
    modificadoPor = @ModificadoPor
WHERE id = @IdOperador
  AND idEmpresa = @IdEmpresa
  AND versionRow = @VersionRow";

                using (SqlCommand command = new SqlCommand(update, connection, transaction))
                {
                    command.Parameters.AddWithValue("@Nombre", request.Nombre.Trim());
                    command.Parameters.AddWithValue("@ApellidoPaterno", request.ApellidoPaterno.Trim());
                    command.Parameters.AddWithValue("@ApellidoMaterno", ToDbValue(request.ApellidoMaterno));
                    command.Parameters.AddWithValue("@ModificadoPor", actorId.HasValue ? actorId.Value : DBNull.Value);
                    command.Parameters.AddWithValue("@IdOperador", request.IdOperador);
                    command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                    command.Parameters.Add("@VersionRow", SqlDbType.Timestamp, 8).Value = versionRow;

                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    if (rowsAffected == 0)
                    {
                        transaction.Rollback();
                        return Conflict(new OperadorOperacionResponse { Mensaje = "El operador cambió antes de guardar. Actualiza la pantalla e inténtalo nuevamente." });
                    }
                }

                await SyncSucursalesAsync(connection, transaction, request.IdOperador, sucursales, actorId);
                transaction.Commit();

                string nombreCompleto = BuildNombreCompleto(request.Nombre, request.ApellidoPaterno, request.ApellidoMaterno);
                await _firebaseIdentityService.UpdateOperatorNodeAsync(
                    operador.IdFirebase,
                    nombreCompleto,
                    operador.Correo,
                    empresa,
                    idEmpresa,
                    operador.IdOperador,
                    operador.Activo && operador.Estatus == 1);

                OperadorDetalleDto? actualizado = await ObtenerOperadorDetalleAsync(connection, null, idEmpresa, request.IdOperador);
                if (actualizado != null)
                {
                    await EnrichWithVerificationStateAsync(new List<OperadorDetalleDto> { actualizado });
                }
                return Ok(new OperadorOperacionResponse
                {
                    Ok = true,
                    Mensaje = "El operador fue actualizado.",
                    VersionRow = actualizado?.VersionRow ?? string.Empty
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new OperadorOperacionResponse { Mensaje = ResolveOperatorError(ex) });
            }
        }

        [HttpPut("Suspender")]
        public async Task<IActionResult> Suspender([FromBody] EstadoOperadorRequest request, Guid idEmpresa, string cadena, string empresa)
        {
            return await UpdateEstadoAsync(request, idEmpresa, cadena, empresa, activo: false, estatus: 2, mensaje: "El operador fue suspendido.");
        }

        [HttpPut("Reactivar")]
        public async Task<IActionResult> Reactivar([FromBody] EstadoOperadorRequest request, Guid idEmpresa, string cadena, string empresa)
        {
            return await UpdateEstadoAsync(request, idEmpresa, cadena, empresa, activo: true, estatus: 1, mensaje: "El operador fue reactivado.");
        }

        [HttpPost("EnviarRecuperacion")]
        public async Task<IActionResult> EnviarRecuperacion([FromBody] RecuperacionOperadorRequest request, Guid idEmpresa, string cadena)
        {
            try
            {
                using SqlConnection connection = CreateConnection(cadena);
                await connection.OpenAsync();

                OperadorDetalleDto? operador = await ObtenerOperadorDetalleAsync(connection, null, idEmpresa, request.IdOperador);
                if (operador == null)
                {
                    return BadRequest(new OperadorOperacionResponse { Mensaje = "El operador no está disponible." });
                }

                await _firebaseIdentityService.SendResetPasswordAsync(operador.Correo);
                return Ok(new OperadorOperacionResponse
                {
                    Ok = true,
                    Mensaje = "Se envió el correo de recuperación al operador."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new OperadorOperacionResponse { Mensaje = ResolveOperatorError(ex) });
            }
        }

        [HttpPost("ReenviarVerificacion")]
        public async Task<IActionResult> ReenviarVerificacion([FromBody] RecuperacionOperadorRequest request, Guid idEmpresa, string cadena)
        {
            try
            {
                using SqlConnection connection = CreateConnection(cadena);
                await connection.OpenAsync();

                OperadorDetalleDto? operador = await ObtenerOperadorDetalleAsync(connection, null, idEmpresa, request.IdOperador);
                if (operador == null)
                {
                    return BadRequest(new OperadorOperacionResponse { Mensaje = "No fue posible reenviar el correo. Intenta nuevamente." });
                }

                await EnrichWithVerificationStateAsync(new List<OperadorDetalleDto> { operador });

                if (operador.CorreoVerificado == true)
                {
                    return Ok(new OperadorOperacionResponse
                    {
                        Ok = true,
                        Mensaje = "La cuenta ya está verificada."
                    });
                }

                OperatorVerificationResendResult resendResult = await _firebaseIdentityService.ResendVerificationAsync(operador.IdFirebase);
                return Ok(new OperadorOperacionResponse
                {
                    Ok = resendResult == OperatorVerificationResendResult.Sent || resendResult == OperatorVerificationResendResult.AlreadyVerified,
                    Mensaje = resendResult switch
                    {
                        OperatorVerificationResendResult.Sent => "Se envió un nuevo correo de verificación.",
                        OperatorVerificationResendResult.AlreadyVerified => "La cuenta ya está verificada.",
                        _ => "No fue posible reenviar el correo. Intenta nuevamente."
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new OperadorOperacionResponse { Mensaje = ResolveOperatorError(ex) });
            }
        }

        [HttpGet("ObtenerSucursalesOperador")]
        public async Task<IActionResult> ObtenerSucursalesOperador(Guid idEmpresa, Guid idOperador, string cadena)
        {
            try
            {
                using SqlConnection connection = CreateConnection(cadena);
                await connection.OpenAsync();

                OperadorDetalleDto? operador = await ObtenerOperadorDetalleAsync(connection, null, idEmpresa, idOperador);
                if (operador == null)
                {
                    return Ok(new List<OperadorSucursalDto>());
                }

                return Ok(operador.Sucursales.Where(item => item.Activo).ToList());
            }
            catch (Exception ex)
            {
                return StatusCode(500, new OperadorOperacionResponse { Mensaje = ResolveOperatorError(ex) });
            }
        }

        private async Task<IActionResult> UpdateEstadoAsync(
            EstadoOperadorRequest request,
            Guid idEmpresa,
            string cadena,
            string empresa,
            bool activo,
            byte estatus,
            string mensaje)
        {
            try
            {
                using SqlConnection connection = CreateConnection(cadena);
                await connection.OpenAsync();
                using SqlTransaction transaction = connection.BeginTransaction();

                OperadorDetalleDto? operador = await ObtenerOperadorDetalleAsync(connection, transaction, idEmpresa, request.IdOperador);
                if (operador == null)
                {
                    return BadRequest(new OperadorOperacionResponse { Mensaje = "El operador no está disponible." });
                }

                byte[] versionRow = DecodeVersionRow(request.VersionRow);
                Guid? actorId = await ObtenerActorIdAsync(connection, transaction, idEmpresa, request.CorreoActor);

                string update = @"
UPDATE dbo.Operadores
SET activo = @Activo,
    estatus = @Estatus,
    fechaSuspension = @FechaSuspension,
    fechaModificacion = GETDATE(),
    modificadoPor = @ModificadoPor
WHERE id = @IdOperador
  AND idEmpresa = @IdEmpresa
  AND versionRow = @VersionRow";

                using (SqlCommand command = new SqlCommand(update, connection, transaction))
                {
                    command.Parameters.AddWithValue("@Activo", activo);
                    command.Parameters.AddWithValue("@Estatus", estatus);
                    command.Parameters.AddWithValue("@FechaSuspension", activo ? DBNull.Value : DateTime.Now);
                    command.Parameters.AddWithValue("@ModificadoPor", actorId.HasValue ? actorId.Value : DBNull.Value);
                    command.Parameters.AddWithValue("@IdOperador", request.IdOperador);
                    command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
                    command.Parameters.Add("@VersionRow", SqlDbType.Timestamp, 8).Value = versionRow;

                    int rowsAffected = await command.ExecuteNonQueryAsync();
                    if (rowsAffected == 0)
                    {
                        transaction.Rollback();
                        return Conflict(new OperadorOperacionResponse { Mensaje = "El operador cambió antes de guardar. Actualiza la pantalla e inténtalo nuevamente." });
                    }
                }

                transaction.Commit();

                await _firebaseIdentityService.UpdateOperatorNodeAsync(
                    operador.IdFirebase,
                    operador.NombreCompleto,
                    operador.Correo,
                    empresa,
                    idEmpresa,
                    operador.IdOperador,
                    activo && estatus == 1);
                await _firebaseIdentityService.RevokeSessionAsync(operador.IdFirebase);

                OperadorDetalleDto? actualizado = await ObtenerOperadorDetalleAsync(connection, null, idEmpresa, request.IdOperador);
                return Ok(new OperadorOperacionResponse
                {
                    Ok = true,
                    Mensaje = mensaje,
                    VersionRow = actualizado?.VersionRow ?? string.Empty
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new OperadorOperacionResponse { Mensaje = ResolveOperatorError(ex) });
            }
        }

        private async Task<OperadorDetalleDto?> ObtenerOperadorDetalleAsync(SqlConnection connection, SqlTransaction? transaction, Guid idEmpresa, Guid idOperador)
        {
            string query = @"
SELECT
    o.id,
    o.idEmpresa,
    o.idFirebase,
    o.nombre,
    o.apellidoPaterno,
    o.apellidoMaterno,
    o.correo,
    o.estatus,
    o.activo,
    o.fechaAlta,
    o.fechaSuspension,
    o.versionRow,
    os.idSucursal,
    os.activo AS sucursalActiva,
    s.Nombre AS nombreSucursal
FROM dbo.Operadores o
LEFT JOIN dbo.OperadoresSucursales os ON os.idOperador = o.id
LEFT JOIN dbo.Sucursales s ON s.id = os.idSucursal
WHERE o.idEmpresa = @IdEmpresa
  AND o.id = @IdOperador
ORDER BY s.Nombre";

            using SqlCommand command = new SqlCommand(query, connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@IdOperador", idOperador);

            using SqlDataReader reader = await command.ExecuteReaderAsync();
            OperadorDetalleDto? operador = null;
            while (await reader.ReadAsync())
            {
                operador ??= new OperadorDetalleDto
                {
                    IdOperador = ReadGuid(reader, "id"),
                    IdEmpresa = ReadGuid(reader, "idEmpresa"),
                    IdFirebase = ReadString(reader, "idFirebase"),
                    Nombre = ReadString(reader, "nombre"),
                    ApellidoPaterno = ReadString(reader, "apellidoPaterno"),
                    ApellidoMaterno = ReadString(reader, "apellidoMaterno"),
                    NombreCompleto = BuildNombreCompleto(
                        ReadString(reader, "nombre"),
                        ReadString(reader, "apellidoPaterno"),
                        ReadString(reader, "apellidoMaterno")),
                    Correo = ReadString(reader, "correo"),
                    Activo = ReadBool(reader, "activo"),
                    Estatus = ReadByte(reader, "estatus"),
                    Estado = ResolveEstado(
                        ReadBool(reader, "activo"),
                        ReadByte(reader, "estatus"),
                        null),
                    FechaAlta = ReadDateTime(reader, "fechaAlta"),
                    FechaSuspension = ReadDateTime(reader, "fechaSuspension"),
                    VersionRow = ReadVersionRow(reader, "versionRow")
                };

                Guid idSucursal = ReadGuid(reader, "idSucursal");
                if (idSucursal != Guid.Empty)
                {
                    operador.Sucursales.Add(new OperadorSucursalDto
                    {
                        IdSucursal = idSucursal,
                        Sucursal = ReadString(reader, "nombreSucursal"),
                        Activo = ReadBool(reader, "sucursalActiva")
                    });
                }
            }

            return operador;
        }

        private async Task<OperadorDetalleDto?> ObtenerOperadorPorFirebaseAsync(SqlConnection connection, SqlTransaction? transaction, Guid idEmpresa, string idFirebase)
        {
            string query = "SELECT TOP 1 id FROM dbo.Operadores WHERE idEmpresa = @IdEmpresa AND idFirebase = @IdFirebase";
            using SqlCommand command = new SqlCommand(query, connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@IdFirebase", idFirebase);

            object? result = await command.ExecuteScalarAsync();
            if (result == null || result == DBNull.Value || !Guid.TryParse(result.ToString(), out Guid idOperador))
            {
                return null;
            }

            return await ObtenerOperadorDetalleAsync(connection, transaction, idEmpresa, idOperador);
        }

        private async Task<OperadorIdentidadDualCandidatoDto> ObtenerCandidatoIdentidadDualAsync(SqlConnection connection, SqlTransaction? transaction, Guid idEmpresa, string correoNormalizado)
        {
            OperadorIdentidadDualCandidatoDto candidato = new OperadorIdentidadDualCandidatoDto
            {
                Correo = correoNormalizado,
                IdEmpresa = idEmpresa,
                IdentidadValida = false,
                Mensaje = "No fue posible completar la asignación. Revisa la información de la persona."
            };

            string usuarioQuery = @"
SELECT
    TOP 2
    u.id,
    u.idEmpresa,
    u.idFirebase,
    u.Nombre,
    u.ApellidoPaterno,
    u.ApellidoMaterno,
    u.CorreoInstitucional,
    u.CorreoPersonal,
    ISNULL(u.Estado, 0) AS Estado,
    ISNULL(u.Estatus, 0) AS Estatus,
    ISNULL(u.borrado, 0) AS Borrado
FROM dbo.Usuarios u
WHERE u.idEmpresa = @IdEmpresa
  AND (
        LOWER(LTRIM(RTRIM(ISNULL(u.CorreoInstitucional, '')))) = @Correo
        OR LOWER(LTRIM(RTRIM(ISNULL(u.CorreoPersonal, '')))) = @Correo
      )";

            using SqlCommand usuarioCommand = new SqlCommand(usuarioQuery, connection, transaction);
            usuarioCommand.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            usuarioCommand.Parameters.AddWithValue("@Correo", correoNormalizado);

            List<(Guid IdUsuario, string IdFirebase, string Nombre, string ApellidoPaterno, string ApellidoMaterno, string Correo, bool Estado, bool Estatus, bool Borrado)> coincidencias = new();
            using (SqlDataReader reader = await usuarioCommand.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    string correoInstitucional = NormalizeEmail(ReadString(reader, "CorreoInstitucional"));
                    string correoPersonal = NormalizeEmail(ReadString(reader, "CorreoPersonal"));
                    string correoUsuario = !string.IsNullOrWhiteSpace(correoInstitucional)
                        ? correoInstitucional
                        : correoPersonal;

                    coincidencias.Add((
                        ReadGuid(reader, "id"),
                        ReadString(reader, "idFirebase"),
                        ReadString(reader, "Nombre"),
                        ReadString(reader, "ApellidoPaterno"),
                        ReadString(reader, "ApellidoMaterno"),
                        string.IsNullOrWhiteSpace(correoUsuario) ? correoNormalizado : correoUsuario,
                        ReadBool(reader, "Estado"),
                        ReadBool(reader, "Estatus"),
                        ReadBool(reader, "Borrado")));
                }
            }

            if (coincidencias.Count == 0)
            {
                candidato.Mensaje = "No se encontró una cuenta administrativa activa con ese correo.";
                return candidato;
            }

            if (coincidencias.Count > 1)
            {
                candidato.Mensaje = "No fue posible completar la asignación. Revisa la información de la persona.";
                return candidato;
            }

            var usuario = coincidencias[0];
            if (!IsAdministrativeUserActive(usuario.Estado, usuario.Estatus, usuario.Borrado))
            {
                candidato.Mensaje = "La persona no tiene una cuenta administrativa activa.";
                return candidato;
            }

            string resolvedFirebaseUid = usuario.IdFirebase.Trim();
            if (!IsRealFirebaseUid(resolvedFirebaseUid))
            {
                string? historicalUid = await _firebaseIdentityService.FindAdministrativeUidByEmailAsync(idEmpresa, usuario.Correo);
                if (IsRealFirebaseUid(historicalUid))
                {
                    resolvedFirebaseUid = historicalUid!.Trim();
                }
                else
                {
                    candidato.Mensaje = "La cuenta existe, pero requiere actualizar su acceso antes de vincularla como operador.";
                    return candidato;
                }
            }

            string duplicidadUidQuery = @"
SELECT COUNT(1)
FROM dbo.Usuarios
WHERE idFirebase = @IdFirebase
  AND id <> @IdUsuario";
            using (SqlCommand duplicidadUidCommand = new SqlCommand(duplicidadUidQuery, connection, transaction))
            {
                duplicidadUidCommand.Parameters.AddWithValue("@IdFirebase", resolvedFirebaseUid);
                duplicidadUidCommand.Parameters.AddWithValue("@IdUsuario", usuario.IdUsuario);
                int duplicadosUid = Convert.ToInt32(await duplicidadUidCommand.ExecuteScalarAsync() ?? 0);
                if (duplicadosUid > 0)
                {
                    candidato.Mensaje = "No fue posible completar la asignación. Revisa la información de la persona.";
                    return candidato;
                }
            }

            if (!string.Equals(usuario.IdFirebase?.Trim(), resolvedFirebaseUid, StringComparison.Ordinal))
            {
                string updateUsuarioUid = @"
UPDATE dbo.Usuarios
SET idFirebase = @IdFirebase
WHERE id = @IdUsuario";
                using SqlCommand updateUsuarioUidCommand = new SqlCommand(updateUsuarioUid, connection, transaction);
                updateUsuarioUidCommand.Parameters.AddWithValue("@IdFirebase", resolvedFirebaseUid);
                updateUsuarioUidCommand.Parameters.AddWithValue("@IdUsuario", usuario.IdUsuario);
                await updateUsuarioUidCommand.ExecuteNonQueryAsync();
            }

            string operadorQuery = @"
SELECT TOP 1 id, idEmpresa
FROM dbo.Operadores
WHERE idFirebase = @IdFirebase OR LOWER(correo) = @Correo";
            using SqlCommand operadorCommand = new SqlCommand(operadorQuery, connection, transaction);
            operadorCommand.Parameters.AddWithValue("@IdFirebase", resolvedFirebaseUid);
            operadorCommand.Parameters.AddWithValue("@Correo", correoNormalizado);

            using SqlDataReader operadorReader = await operadorCommand.ExecuteReaderAsync();
            if (await operadorReader.ReadAsync())
            {
                Guid idOperador = ReadGuid(operadorReader, "id");
                Guid idEmpresaOperador = ReadGuid(operadorReader, "idEmpresa");
                if (idEmpresaOperador != idEmpresa)
                {
                    candidato.Mensaje = "No fue posible completar la asignación. Revisa la información de la persona.";
                    return candidato;
                }

                candidato.YaEsOperador = true;
                candidato.IdOperador = idOperador;
            }

            candidato.IdUsuario = usuario.IdUsuario;
            candidato.IdFirebase = resolvedFirebaseUid;
            candidato.Nombre = usuario.Nombre;
            candidato.ApellidoPaterno = usuario.ApellidoPaterno;
            candidato.ApellidoMaterno = usuario.ApellidoMaterno;
            candidato.Correo = usuario.Correo;
            candidato.NombreCompleto = BuildNombreCompleto(usuario.Nombre, usuario.ApellidoPaterno, usuario.ApellidoMaterno);
            candidato.UsuarioActivo = true;
            candidato.IdentidadValida = true;
            candidato.Mensaje = candidato.YaEsOperador
                ? "La persona ya cuenta con ambos accesos."
                : "Identidad disponible.";

            return candidato;
        }

        private async Task EnsureEmailAvailableAsync(SqlConnection connection, SqlTransaction? transaction, string correo)
        {
            string query = "SELECT COUNT(1) FROM dbo.Operadores WHERE correo = @Correo";
            using SqlCommand command = new SqlCommand(query, connection, transaction);
            command.Parameters.AddWithValue("@Correo", correo);
            int total = Convert.ToInt32(await command.ExecuteScalarAsync());
            if (total > 0)
            {
                throw new InvalidOperationException("Ya existe un operador registrado con ese correo.");
            }
        }

        private async Task EnsureSucursalesBelongToEmpresaAsync(SqlConnection connection, SqlTransaction? transaction, Guid idEmpresa, List<Guid> sucursales)
        {
            if (sucursales.Count == 0)
            {
                throw new InvalidOperationException("Selecciona al menos una sucursal.");
            }

            string ids = string.Join(",", sucursales.Select((_, index) => $"@Sucursal{index}"));
            string query = $"SELECT COUNT(1) FROM dbo.Sucursales WHERE idEmpresa = @IdEmpresa AND id IN ({ids})";
            using SqlCommand command = new SqlCommand(query, connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            for (int index = 0; index < sucursales.Count; index++)
            {
                command.Parameters.AddWithValue($"@Sucursal{index}", sucursales[index]);
            }

            int total = Convert.ToInt32(await command.ExecuteScalarAsync());
            if (total != sucursales.Count)
            {
                throw new InvalidOperationException("Una o más sucursales no pertenecen a la empresa activa.");
            }
        }

        private async Task InsertSucursalesAsync(SqlConnection connection, SqlTransaction transaction, Guid idOperador, List<Guid> sucursales, Guid? actorId)
        {
            foreach (Guid idSucursal in sucursales)
            {
                string insert = @"
INSERT INTO dbo.OperadoresSucursales
    (id, idOperador, idSucursal, activo, fechaCreacion, creadoPor)
VALUES
    (@Id, @IdOperador, @IdSucursal, 1, GETDATE(), @CreadoPor)";

                using SqlCommand command = new SqlCommand(insert, connection, transaction);
                command.Parameters.AddWithValue("@Id", Guid.NewGuid());
                command.Parameters.AddWithValue("@IdOperador", idOperador);
                command.Parameters.AddWithValue("@IdSucursal", idSucursal);
                command.Parameters.AddWithValue("@CreadoPor", actorId.HasValue ? actorId.Value : DBNull.Value);
                await command.ExecuteNonQueryAsync();
            }
        }

        private async Task SyncSucursalesAsync(SqlConnection connection, SqlTransaction transaction, Guid idOperador, List<Guid> sucursalesObjetivo, Guid? actorId)
        {
            string query = "SELECT id, idSucursal, activo FROM dbo.OperadoresSucursales WHERE idOperador = @IdOperador";
            using SqlCommand command = new SqlCommand(query, connection, transaction);
            command.Parameters.AddWithValue("@IdOperador", idOperador);

            Dictionary<Guid, (Guid IdRelacion, bool Activo)> actuales = new Dictionary<Guid, (Guid, bool)>();
            using (SqlDataReader reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    actuales[ReadGuid(reader, "idSucursal")] = (ReadGuid(reader, "id"), ReadBool(reader, "activo"));
                }
            }

            foreach (Guid idSucursal in sucursalesObjetivo)
            {
                if (actuales.TryGetValue(idSucursal, out (Guid IdRelacion, bool Activo) actual))
                {
                    if (!actual.Activo)
                    {
                        string reactiva = @"
UPDATE dbo.OperadoresSucursales
SET activo = 1,
    fechaModificacion = GETDATE(),
    modificadoPor = @ModificadoPor
WHERE id = @IdRelacion";
                        using SqlCommand update = new SqlCommand(reactiva, connection, transaction);
                        update.Parameters.AddWithValue("@ModificadoPor", actorId.HasValue ? actorId.Value : DBNull.Value);
                        update.Parameters.AddWithValue("@IdRelacion", actual.IdRelacion);
                        await update.ExecuteNonQueryAsync();
                    }
                }
                else
                {
                    await InsertSucursalesAsync(connection, transaction, idOperador, new List<Guid> { idSucursal }, actorId);
                }
            }

            foreach (KeyValuePair<Guid, (Guid IdRelacion, bool Activo)> actual in actuales)
            {
                if (sucursalesObjetivo.Contains(actual.Key) || !actual.Value.Activo)
                {
                    continue;
                }

                string desactiva = @"
UPDATE dbo.OperadoresSucursales
SET activo = 0,
    fechaModificacion = GETDATE(),
    modificadoPor = @ModificadoPor
WHERE id = @IdRelacion";
                using SqlCommand update = new SqlCommand(desactiva, connection, transaction);
                update.Parameters.AddWithValue("@ModificadoPor", actorId.HasValue ? actorId.Value : DBNull.Value);
                update.Parameters.AddWithValue("@IdRelacion", actual.Value.IdRelacion);
                await update.ExecuteNonQueryAsync();
            }
        }

        private async Task<Guid?> ObtenerActorIdAsync(SqlConnection connection, SqlTransaction transaction, Guid idEmpresa, string correoActor)
        {
            if (string.IsNullOrWhiteSpace(correoActor))
            {
                return null;
            }

            string query = @"
SELECT TOP 1 id
FROM dbo.Usuarios
WHERE idEmpresa = @IdEmpresa
  AND (
      LOWER(ISNULL(CorreoInstitucional, '')) = @Correo
      OR LOWER(ISNULL(CorreoPersonal, '')) = @Correo
  )";

            using SqlCommand command = new SqlCommand(query, connection, transaction);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);
            command.Parameters.AddWithValue("@Correo", correoActor.Trim().ToLowerInvariant());

            object? result = await command.ExecuteScalarAsync();
            if (result == null || result == DBNull.Value)
            {
                return null;
            }

            return Guid.Parse(result.ToString()!);
        }

        private string ValidateCreateRequest(CrearOperadorRequest request, Guid idEmpresa, string cadena, string empresa)
        {
            if (idEmpresa == Guid.Empty)
            {
                return "La empresa activa no está disponible. Vuelve a iniciar sesión.";
            }

            if (string.IsNullOrWhiteSpace(cadena) || string.IsNullOrWhiteSpace(empresa))
            {
                return "La sesión actual no es válida para registrar operadores. Vuelve a iniciar sesión.";
            }

            if (string.IsNullOrWhiteSpace(request.Nombre))
            {
                return "Ingresa el nombre del operador.";
            }

            if (string.IsNullOrWhiteSpace(request.ApellidoPaterno))
            {
                return "Ingresa el apellido paterno del operador.";
            }

            if (string.IsNullOrWhiteSpace(request.Correo))
            {
                return "Ingresa el correo del operador.";
            }

            if (!IsValidEmail(request.Correo))
            {
                return "Ingresa un correo válido.";
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return "Ingresa la contraseña inicial.";
            }

            if (!MeetsPasswordPolicy(request.Password))
            {
                return "La contraseña no cumple los requisitos de seguridad.";
            }

            if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
            {
                return "La contraseña y su confirmación no coinciden.";
            }

            if (request.Sucursales == null || request.Sucursales.Count == 0)
            {
                return "Selecciona al menos una sucursal.";
            }

            if (request.Sucursales.Any(item => item == Guid.Empty))
            {
                return "Selecciona una sucursal disponible para esta empresa.";
            }

            return string.Empty;
        }

        private string ValidateUpdateRequest(ActualizarOperadorRequest request)
        {
            if (request.IdOperador == Guid.Empty)
            {
                return "El operador no está disponible.";
            }

            if (string.IsNullOrWhiteSpace(request.Nombre))
            {
                return "Ingresa el nombre del operador.";
            }

            if (string.IsNullOrWhiteSpace(request.ApellidoPaterno))
            {
                return "Ingresa el apellido paterno del operador.";
            }

            if (request.Sucursales == null || request.Sucursales.Count == 0)
            {
                return "Selecciona al menos una sucursal.";
            }

            if (string.IsNullOrWhiteSpace(request.VersionRow))
            {
                return "La versión del operador no está disponible.";
            }

            return string.Empty;
        }

        private string ResolveOperatorError(Exception ex)
        {
            string message = FlattenExceptionMessages(ex);
            if (ContainsAny(message,
                    "EMAIL_EXISTS",
                    "EmailExists",
                    "email address is already in use",
                    "ya existe un operador registrado con ese correo"))
            {
                return "Ya existe una cuenta registrada con este correo.";
            }

            if (ContainsAny(message,
                    "INVALID_EMAIL",
                    "email address is badly formatted",
                    "correo válido"))
            {
                return "Ingresa un correo válido.";
            }

            if (ContainsAny(message,
                    "WEAK_PASSWORD",
                    "Password should be at least",
                    "La contraseña no cumple los requisitos de seguridad"))
            {
                return "La contraseña no cumple los requisitos de seguridad.";
            }

            if (ContainsAny(message,
                    "Selecciona al menos una sucursal",
                    "sucursales no pertenecen a la empresa activa",
                    "Selecciona una sucursal disponible para esta empresa"))
            {
                return message.Contains("al menos una sucursal", StringComparison.OrdinalIgnoreCase)
                    ? "Selecciona al menos una sucursal."
                    : "Selecciona una sucursal disponible para esta empresa.";
            }

            if (ContainsAny(message,
                    "empresa activa no está disponible",
                    "sesión actual no es válida"))
            {
                return message.Contains("empresa activa", StringComparison.OrdinalIgnoreCase)
                    ? "La empresa activa no está disponible. Vuelve a iniciar sesión."
                    : "La sesión actual no es válida para registrar operadores. Vuelve a iniciar sesión.";
            }

            if (ContainsAny(message,
                    "confirmación no coinciden"))
            {
                return "La contraseña y su confirmación no coinciden.";
            }

            return "No fue posible registrar al Operador. Revisa la información e intenta nuevamente.";
        }

        private IActionResult BuildOperatorErrorResult(Exception ex)
        {
            string mensaje = ResolveOperatorError(ex);
            int statusCode = mensaje switch
            {
                "Ya existe una cuenta registrada con este correo." => StatusCodes.Status409Conflict,
                "Ingresa un correo válido." => StatusCodes.Status400BadRequest,
                "La contraseña no cumple los requisitos de seguridad." => StatusCodes.Status400BadRequest,
                "La contraseña y su confirmación no coinciden." => StatusCodes.Status400BadRequest,
                "Selecciona al menos una sucursal." => StatusCodes.Status400BadRequest,
                "Selecciona una sucursal disponible para esta empresa." => StatusCodes.Status400BadRequest,
                "La empresa activa no está disponible. Vuelve a iniciar sesión." => StatusCodes.Status400BadRequest,
                "La sesión actual no es válida para registrar operadores. Vuelve a iniciar sesión." => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            };

            return StatusCode(statusCode, new OperadorOperacionResponse
            {
                Mensaje = mensaje
            });
        }

        private static bool IsValidEmail(string correo)
        {
            try
            {
                MailAddress address = new MailAddress((correo ?? string.Empty).Trim());
                return string.Equals(address.Address, (correo ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static bool MeetsPasswordPolicy(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            {
                return false;
            }

            return Regex.IsMatch(password, "[A-Z]")
                && Regex.IsMatch(password, "[a-z]")
                && Regex.IsMatch(password, "[0-9]")
                && Regex.IsMatch(password, "[^A-Za-z0-9]");
        }

        private static string FlattenExceptionMessages(Exception ex)
        {
            List<string> messages = new List<string>();
            Exception? current = ex;
            while (current != null)
            {
                if (!string.IsNullOrWhiteSpace(current.Message))
                {
                    messages.Add(current.Message);
                }

                current = current.InnerException;
            }

            return string.Join(" | ", messages);
        }

        private static bool ContainsAny(string message, params string[] patterns)
        {
            return patterns.Any(pattern => message.Contains(pattern, StringComparison.OrdinalIgnoreCase));
        }

        private SqlConnection CreateConnection(string cadena)
        {
            byte[] data = Convert.FromBase64String(cadena);
            string connectionString = Encoding.UTF8.GetString(data);
            return new SqlConnection(connectionString);
        }

        private static string NormalizeEmail(string correo)
        {
            return (correo ?? string.Empty).Trim().ToLowerInvariant();
        }

        private static bool IsRealFirebaseUid(string? idFirebase)
        {
            string value = (idFirebase ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return !string.Equals(value, "uid", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsAdministrativeUserActive(bool estado, bool estatus, bool borrado)
        {
            return !borrado && (estado || estatus);
        }

        private static object ToDbValue(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? DBNull.Value : value.Trim();
        }

        private static Guid ReadGuid(SqlDataReader reader, string columnName)
        {
            object value = reader[columnName];
            return value == DBNull.Value ? Guid.Empty : Guid.Parse(value.ToString()!);
        }

        private static string ReadString(SqlDataReader reader, string columnName)
        {
            object value = reader[columnName];
            return value == DBNull.Value ? string.Empty : value.ToString() ?? string.Empty;
        }

        private static bool ReadBool(SqlDataReader reader, string columnName)
        {
            object value = reader[columnName];
            return value != DBNull.Value && Convert.ToBoolean(value);
        }

        private static byte ReadByte(SqlDataReader reader, string columnName)
        {
            object value = reader[columnName];
            return value == DBNull.Value ? (byte)0 : Convert.ToByte(value);
        }

        private static DateTime? ReadDateTime(SqlDataReader reader, string columnName)
        {
            object value = reader[columnName];
            return value == DBNull.Value ? null : Convert.ToDateTime(value);
        }

        private static string ReadVersionRow(SqlDataReader reader, string columnName)
        {
            object value = reader[columnName];
            return value == DBNull.Value ? string.Empty : Convert.ToBase64String((byte[])value);
        }

        private static byte[] DecodeVersionRow(string versionRow)
        {
            return Convert.FromBase64String(versionRow);
        }

        private static string BuildNombreCompleto(string? nombre, string? apellidoPaterno, string? apellidoMaterno)
        {
            List<string> partes = new List<string>
            {
                nombre?.Trim() ?? string.Empty,
                apellidoPaterno?.Trim() ?? string.Empty,
                apellidoMaterno?.Trim() ?? string.Empty
            };

            return string.Join(" ", partes.Where(item => !string.IsNullOrWhiteSpace(item)));
        }

        private async Task EnrichWithVerificationStateAsync(List<OperadorListadoDto> operadores)
        {
            if (!operadores.Count.Equals(0))
            {
                Dictionary<string, OperatorFirebaseNodeState> nodes = await _firebaseIdentityService.GetOperatorNodeStatesAsync(operadores.Select(item => item.IdFirebase));
                foreach (OperadorListadoDto operador in operadores)
                {
                    bool? correoVerificado = ResolveVerifiedState(nodes, operador.IdFirebase);
                    operador.CorreoVerificado = correoVerificado;
                    operador.Estado = ResolveEstado(operador.Activo, operador.Estatus, correoVerificado);
                }
            }
        }

        private async Task EnrichWithVerificationStateAsync(List<OperadorDetalleDto> operadores)
        {
            if (!operadores.Count.Equals(0))
            {
                Dictionary<string, OperatorFirebaseNodeState> nodes = await _firebaseIdentityService.GetOperatorNodeStatesAsync(operadores.Select(item => item.IdFirebase));
                foreach (OperadorDetalleDto operador in operadores)
                {
                    bool? correoVerificado = ResolveVerifiedState(nodes, operador.IdFirebase);
                    operador.CorreoVerificado = correoVerificado;
                    operador.Estado = ResolveEstado(operador.Activo, operador.Estatus, correoVerificado);
                }
            }
        }

        private static bool? ResolveVerifiedState(Dictionary<string, OperatorFirebaseNodeState> nodes, string idFirebase)
        {
            if (string.IsNullOrWhiteSpace(idFirebase))
            {
                return null;
            }

            if (!nodes.TryGetValue(idFirebase.Trim(), out OperatorFirebaseNodeState? nodeState))
            {
                return null;
            }

            return nodeState.EmailVerified;
        }

        private static string ResolveEstado(bool activo, byte estatus, bool? correoVerificado)
        {
            if (!activo)
            {
                return "Suspendido";
            }

            if (correoVerificado == false)
            {
                return "Pendiente de verificar";
            }

            return estatus == 1 ? "Activo" : "Suspendido";
        }
    }
}
