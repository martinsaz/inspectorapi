using System.Data.SqlClient;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using checklistWs.Models.Configuracion;
using checklistWs.Services;
using checklistWs.Utiles;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;

namespace checklistWs.Controllers.Configuracion
{
    [Route("api/[controller]")]
    [ApiController]
    public sealed class CorreoSalienteController : ControllerBase
    {
        private static readonly TimeSpan ProxyHeaderTolerance = TimeSpan.FromMinutes(5);
        private static readonly string[] EmpresaClaimKeys = new[] { "idEmpresa", "empresaId", "tenantId", "companyId", "tenant", "idempresa" };
        private static readonly string[] EmpresaNombreClaimKeys = new[] { "empresa", "empresaNombre", "tenantName", "companyName", "nombreEmpresa" };
        private static readonly string[] UsuarioClaimKeys = new[] { ClaimTypes.NameIdentifier, "sub", "idUsuario", "userid", "uid" };
        private const string ProxyEmpresaIdHeader = "X-ProductosServicios-Proxy-EmpresaId";
        private const string ProxyEmpresaKeyHeader = "X-ProductosServicios-Proxy-Empresa";
        private const string ProxyUsuarioIdHeader = "X-ProductosServicios-Proxy-UsuarioId";
        private const string ProxyTimestampHeader = "X-ProductosServicios-Proxy-Timestamp";
        private const string ProxySignatureHeader = "X-ProductosServicios-Proxy-Signature";
        private const string ProxyContextItemKey = "__CorreoSalienteProxyContext";
        private const int MaxFieldLength = 200;
        private readonly IConfiguration _configuration;
        private readonly SqlConnectionFactory _connectionFactory;
        private readonly ILogger<CorreoSalienteController> _logger;
        private readonly IDataProtector _protector;
        private readonly DocumentEmailService _documentEmailService;

        public CorreoSalienteController(
            IConfiguration configuration,
            ILogger<CorreoSalienteController> logger,
            IDataProtectionProvider dataProtectionProvider,
            DocumentEmailService documentEmailService)
        {
            _configuration = configuration;
            _connectionFactory = new SqlConnectionFactory(configuration);
            _logger = logger;
            _protector = dataProtectionProvider.CreateProtector("checklistWs.Configuracion.CorreoSaliente.Password.v1");
            _documentEmailService = documentEmailService;
        }

        [HttpGet("ObtenerConfiguracion")]
        public async Task<IActionResult> ObtenerConfiguracion(Guid idEmpresa)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                using SqlConnection connection = CreateConnection();
                await connection.OpenAsync();
                CorreoSalientePersistedConfiguration? stored = await LoadConfigurationAsync(connection, context.IdEmpresa);
                return Ok(ToViewModel(stored));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ObtenerConfiguracion", "No fue posible consultar la configuración de correo saliente.");
            }
        }

        [HttpPost("ProbarConfiguracion")]
        public async Task<IActionResult> ProbarConfiguracion(Guid idEmpresa, [FromBody] ProbarCorreoSalienteRequest? request)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                SmtpDocumentConfiguration configuration = NormalizeForTest(request, null, out string? validationMessage);
                if (!string.IsNullOrWhiteSpace(validationMessage))
                {
                    return BadRequest(new CorreoSalienteOperacionResponse
                    {
                        Exito = false,
                        Mensaje = validationMessage
                    });
                }

                await _documentEmailService.SendTestEmailAsync(configuration);

                return Ok(new CorreoSalienteOperacionResponse
                {
                    Exito = true,
                    Mensaje = "Correo de prueba enviado correctamente.",
                    Estado = "Verificada",
                    TokenVerificacion = CreateVerificationToken(configuration),
                    Configuracion = new CorreoSalienteConfiguracionViewModel
                    {
                        ConfiguracionGuardada = false,
                        Cuenta = configuration.Cuenta,
                        ServidorSmtp = configuration.ServidorSmtp,
                        Puerto = configuration.Puerto,
                        Seguridad = configuration.Seguridad,
                        DestinatarioPrueba = configuration.DestinatarioPrueba,
                        PasswordConfigurado = true,
                        Verificada = true,
                        FechaUltimaPrueba = DateTime.UtcNow
                    }
                });
            }
            catch (DocumentEmailConnectionException)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new CorreoSalienteOperacionResponse
                {
                    Exito = false,
                    Mensaje = "No fue posible conectar con el servidor de correo."
                });
            }
            catch (DocumentEmailAuthenticationException)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new CorreoSalienteOperacionResponse
                {
                    Exito = false,
                    Mensaje = "No fue posible autenticar la cuenta de correo."
                });
            }
            catch (DocumentEmailSendException)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new CorreoSalienteOperacionResponse
                {
                    Exito = false,
                    Mensaje = "No fue posible enviar el correo de prueba."
                });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ProbarConfiguracion", "No fue posible enviar el correo de prueba.");
            }
        }

        [HttpPost("GuardarConfiguracion")]
        public async Task<IActionResult> GuardarConfiguracion(Guid idEmpresa, [FromBody] GuardarCorreoSalienteRequest? request)
        {
            if (!TryResolveRequestContext(idEmpresa, null, out RequestContext context, out IActionResult? error))
            {
                return error!;
            }

            try
            {
                using SqlConnection connection = CreateConnection();
                await connection.OpenAsync();

                CorreoSalientePersistedConfiguration? stored = await LoadConfigurationAsync(connection, context.IdEmpresa);
                SmtpDocumentConfiguration configuration = NormalizeForSave(request, stored, out string? validationMessage, out bool coreChanged);
                if (!string.IsNullOrWhiteSpace(validationMessage))
                {
                    return BadRequest(new CorreoSalienteOperacionResponse
                    {
                        Exito = false,
                        Mensaje = validationMessage
                    });
                }

                bool requiresVerification = stored == null || !stored.ConfiguracionVerificada || coreChanged;
                bool configurationVerified = !requiresVerification;
                if (requiresVerification)
                {
                    string currentToken = CreateVerificationToken(configuration);
                    if (!string.Equals(currentToken, (request?.TokenVerificacion ?? string.Empty).Trim(), StringComparison.Ordinal))
                    {
                        return BadRequest(new CorreoSalienteOperacionResponse
                        {
                            Exito = false,
                            Mensaje = "Realiza una prueba exitosa antes de guardar la configuración."
                        });
                    }

                    configurationVerified = true;
                }

                CorreoSalientePersistedConfiguration persisted = await UpsertConfigurationAsync(connection, context.IdEmpresa, configuration, stored, configurationVerified);
                return Ok(new CorreoSalienteOperacionResponse
                {
                    Exito = true,
                    Mensaje = "La configuración de correo saliente se guardó correctamente.",
                    Estado = persisted.ConfiguracionVerificada ? "Verificada" : "No verificada",
                    Configuracion = ToViewModel(persisted)
                });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "GuardarConfiguracion", "No fue posible guardar la configuración de correo saliente.");
            }
        }

        private SqlConnection CreateConnection() => _connectionFactory.CreateConnection();

        private async Task<CorreoSalientePersistedConfiguration?> LoadConfigurationAsync(SqlConnection connection, Guid idEmpresa)
        {
            using SqlCommand command = new SqlCommand(@"
SELECT TOP (1)
    id,
    idEmpresa,
    identityKey,
    Cuenta,
    ServidorSmtp,
    Puerto,
    Seguridad,
    CredencialProtegida,
    DestinatarioPrueba,
    ConfiguracionVerificada,
    FechaUltimaPrueba,
    FechaCreacion,
    FechaActualizacion,
    Activo
FROM dbo.ConfiguracionCorreoSaliente
WHERE idEmpresa = @IdEmpresa
  AND Activo = 1
  AND FechaArchivado IS NULL
ORDER BY FechaCreacion DESC", connection);
            command.Parameters.AddWithValue("@IdEmpresa", idEmpresa);

            using SqlDataReader reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }

            return new CorreoSalientePersistedConfiguration
            {
                Id = reader.GetGuid(reader.GetOrdinal("id")),
                IdEmpresa = reader.GetGuid(reader.GetOrdinal("idEmpresa")),
                IdentityKey = reader.GetGuid(reader.GetOrdinal("identityKey")),
                Cuenta = ReadString(reader, "Cuenta"),
                ServidorSmtp = ReadString(reader, "ServidorSmtp"),
                Puerto = reader.GetInt32(reader.GetOrdinal("Puerto")),
                Seguridad = ReadString(reader, "Seguridad"),
                CredencialProtegida = ReadString(reader, "CredencialProtegida"),
                DestinatarioPrueba = ReadString(reader, "DestinatarioPrueba"),
                ConfiguracionVerificada = reader.GetBoolean(reader.GetOrdinal("ConfiguracionVerificada")),
                FechaUltimaPrueba = ReadNullableDateTime(reader, "FechaUltimaPrueba"),
                FechaCreacion = reader.GetDateTime(reader.GetOrdinal("FechaCreacion")),
                FechaActualizacion = ReadNullableDateTime(reader, "FechaActualizacion"),
                Activo = reader.GetBoolean(reader.GetOrdinal("Activo"))
            };
        }

        private async Task<CorreoSalientePersistedConfiguration> UpsertConfigurationAsync(
            SqlConnection connection,
            Guid idEmpresa,
            SmtpDocumentConfiguration configuration,
            CorreoSalientePersistedConfiguration? existing,
            bool verified)
        {
            DateTime utcNow = DateTime.UtcNow;
            string protectedPassword = _protector.Protect(configuration.Contrasena);

            if (existing == null)
            {
                CorreoSalientePersistedConfiguration created = new CorreoSalientePersistedConfiguration
                {
                    Id = Guid.NewGuid(),
                    IdEmpresa = idEmpresa,
                    IdentityKey = Guid.NewGuid(),
                    Cuenta = configuration.Cuenta,
                    ServidorSmtp = configuration.ServidorSmtp,
                    Puerto = configuration.Puerto,
                    Seguridad = configuration.Seguridad,
                    CredencialProtegida = protectedPassword,
                    DestinatarioPrueba = configuration.DestinatarioPrueba,
                    ConfiguracionVerificada = verified,
                    FechaUltimaPrueba = verified ? utcNow : null,
                    FechaCreacion = utcNow,
                    FechaActualizacion = utcNow,
                    Activo = true
                };

                using SqlCommand insert = new SqlCommand(@"
INSERT INTO dbo.ConfiguracionCorreoSaliente
(
    id,
    idEmpresa,
    identityKey,
    Cuenta,
    ServidorSmtp,
    Puerto,
    Seguridad,
    CredencialProtegida,
    DestinatarioPrueba,
    ConfiguracionVerificada,
    FechaUltimaPrueba,
    FechaCreacion,
    FechaActualizacion,
    FechaArchivado,
    Activo
)
VALUES
(
    @Id,
    @IdEmpresa,
    @IdentityKey,
    @Cuenta,
    @ServidorSmtp,
    @Puerto,
    @Seguridad,
    @CredencialProtegida,
    @DestinatarioPrueba,
    @ConfiguracionVerificada,
    @FechaUltimaPrueba,
    @FechaCreacion,
    @FechaActualizacion,
    NULL,
    1
)", connection);

                FillPersistParameters(insert, created);
                await insert.ExecuteNonQueryAsync();
                return created;
            }

            existing.Cuenta = configuration.Cuenta;
            existing.ServidorSmtp = configuration.ServidorSmtp;
            existing.Puerto = configuration.Puerto;
            existing.Seguridad = configuration.Seguridad;
            existing.CredencialProtegida = protectedPassword;
            existing.DestinatarioPrueba = configuration.DestinatarioPrueba;
            existing.ConfiguracionVerificada = verified;
            existing.FechaUltimaPrueba = verified ? utcNow : existing.FechaUltimaPrueba;
            existing.FechaActualizacion = utcNow;
            existing.Activo = true;

            using SqlCommand update = new SqlCommand(@"
UPDATE dbo.ConfiguracionCorreoSaliente
SET
    Cuenta = @Cuenta,
    ServidorSmtp = @ServidorSmtp,
    Puerto = @Puerto,
    Seguridad = @Seguridad,
    CredencialProtegida = @CredencialProtegida,
    DestinatarioPrueba = @DestinatarioPrueba,
    ConfiguracionVerificada = @ConfiguracionVerificada,
    FechaUltimaPrueba = @FechaUltimaPrueba,
    FechaActualizacion = @FechaActualizacion,
    Activo = 1,
    FechaArchivado = NULL
WHERE id = @Id
  AND idEmpresa = @IdEmpresa", connection);

            FillPersistParameters(update, existing);
            await update.ExecuteNonQueryAsync();
            return existing;
        }

        private static void FillPersistParameters(SqlCommand command, CorreoSalientePersistedConfiguration configuration)
        {
            command.Parameters.AddWithValue("@Id", configuration.Id);
            command.Parameters.AddWithValue("@IdEmpresa", configuration.IdEmpresa);
            if (command.CommandText.Contains("@IdentityKey", StringComparison.Ordinal))
            {
                command.Parameters.AddWithValue("@IdentityKey", configuration.IdentityKey);
                command.Parameters.AddWithValue("@FechaCreacion", configuration.FechaCreacion);
            }

            command.Parameters.AddWithValue("@Cuenta", configuration.Cuenta);
            command.Parameters.AddWithValue("@ServidorSmtp", configuration.ServidorSmtp);
            command.Parameters.AddWithValue("@Puerto", configuration.Puerto);
            command.Parameters.AddWithValue("@Seguridad", configuration.Seguridad);
            command.Parameters.AddWithValue("@CredencialProtegida", configuration.CredencialProtegida);
            command.Parameters.AddWithValue("@DestinatarioPrueba", (object?)configuration.DestinatarioPrueba ?? DBNull.Value);
            command.Parameters.AddWithValue("@ConfiguracionVerificada", configuration.ConfiguracionVerificada);
            command.Parameters.AddWithValue("@FechaUltimaPrueba", configuration.FechaUltimaPrueba.HasValue ? configuration.FechaUltimaPrueba.Value : DBNull.Value);
            command.Parameters.AddWithValue("@FechaActualizacion", configuration.FechaActualizacion.HasValue ? configuration.FechaActualizacion.Value : DBNull.Value);
        }

        private CorreoSalienteConfiguracionViewModel ToViewModel(CorreoSalientePersistedConfiguration? stored)
        {
            if (stored == null)
            {
                return new CorreoSalienteConfiguracionViewModel
                {
                    ConfiguracionGuardada = false,
                    Cuenta = string.Empty,
                    ServidorSmtp = string.Empty,
                    Puerto = 465,
                    Seguridad = CorreoSalienteSeguridad.SslTls,
                    DestinatarioPrueba = string.Empty,
                    PasswordConfigurado = false,
                    Verificada = false
                };
            }

            return new CorreoSalienteConfiguracionViewModel
            {
                ConfiguracionGuardada = true,
                Cuenta = stored.Cuenta,
                ServidorSmtp = stored.ServidorSmtp,
                Puerto = stored.Puerto,
                Seguridad = stored.Seguridad,
                DestinatarioPrueba = stored.DestinatarioPrueba,
                PasswordConfigurado = !string.IsNullOrWhiteSpace(stored.CredencialProtegida),
                Verificada = stored.ConfiguracionVerificada,
                FechaUltimaPrueba = stored.FechaUltimaPrueba,
                FechaActualizacion = stored.FechaActualizacion
            };
        }

        private SmtpDocumentConfiguration NormalizeForTest(
            ProbarCorreoSalienteRequest? request,
            CorreoSalientePersistedConfiguration? existing,
            out string? validationMessage)
        {
            return NormalizeCoreConfiguration(
                request?.Cuenta,
                request?.Contrasena,
                request?.ServidorSmtp,
                request?.Puerto ?? 0,
                request?.Seguridad,
                request?.DestinatarioPrueba,
                existing,
                out validationMessage);
        }

        private SmtpDocumentConfiguration NormalizeForSave(
            GuardarCorreoSalienteRequest? request,
            CorreoSalientePersistedConfiguration? existing,
            out string? validationMessage,
            out bool coreChanged)
        {
            SmtpDocumentConfiguration normalized = NormalizeCoreConfiguration(
                request?.Cuenta,
                request?.Contrasena,
                request?.ServidorSmtp,
                request?.Puerto ?? 0,
                request?.Seguridad,
                request?.DestinatarioPrueba,
                existing,
                out validationMessage);

            coreChanged = existing == null
                || !string.Equals(existing.Cuenta, normalized.Cuenta, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(existing.ServidorSmtp, normalized.ServidorSmtp, StringComparison.OrdinalIgnoreCase)
                || existing.Puerto != normalized.Puerto
                || !string.Equals(CorreoSalienteSeguridad.Normalize(existing.Seguridad), normalized.Seguridad, StringComparison.Ordinal)
                || !string.IsNullOrWhiteSpace(request?.Contrasena);

            return normalized;
        }

        private SmtpDocumentConfiguration NormalizeCoreConfiguration(
            string? cuenta,
            string? contrasena,
            string? servidor,
            int puerto,
            string? seguridad,
            string? destinatarioPrueba,
            CorreoSalientePersistedConfiguration? existing,
            out string? validationMessage)
        {
            validationMessage = null;
            string normalizedCuenta = (cuenta ?? string.Empty).Trim();
            string normalizedServidor = (servidor ?? string.Empty).Trim();
            string normalizedSeguridad = CorreoSalienteSeguridad.Normalize(seguridad);
            string normalizedDestinatario = (destinatarioPrueba ?? string.Empty).Trim();
            string normalizedPassword = (contrasena ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(normalizedCuenta) ||
                string.IsNullOrWhiteSpace(normalizedServidor) ||
                string.IsNullOrWhiteSpace(normalizedSeguridad) ||
                string.IsNullOrWhiteSpace(normalizedDestinatario))
            {
                validationMessage = "Completa cuenta, servidor, seguridad y destinatario de prueba.";
                return new SmtpDocumentConfiguration();
            }

            if (normalizedCuenta.Length > MaxFieldLength || normalizedServidor.Length > MaxFieldLength || normalizedDestinatario.Length > MaxFieldLength)
            {
                validationMessage = "Alguno de los campos capturados excede la longitud permitida.";
                return new SmtpDocumentConfiguration();
            }

            if (!IsValidEmail(normalizedCuenta) || !IsValidEmail(normalizedDestinatario))
            {
                validationMessage = "Captura correos válidos para la cuenta remitente y el destinatario de prueba.";
                return new SmtpDocumentConfiguration();
            }

            if (puerto <= 0 || puerto > 65535)
            {
                validationMessage = "Captura un puerto SMTP válido.";
                return new SmtpDocumentConfiguration();
            }

            if (string.IsNullOrWhiteSpace(normalizedPassword))
            {
                if (existing == null || string.IsNullOrWhiteSpace(existing.CredencialProtegida))
                {
                    validationMessage = "Captura la contraseña de la cuenta de correo.";
                    return new SmtpDocumentConfiguration();
                }

                normalizedPassword = _protector.Unprotect(existing.CredencialProtegida);
            }

            return new SmtpDocumentConfiguration
            {
                Cuenta = normalizedCuenta,
                Contrasena = normalizedPassword,
                ServidorSmtp = normalizedServidor,
                Puerto = puerto,
                Seguridad = normalizedSeguridad,
                DestinatarioPrueba = normalizedDestinatario
            };
        }

        private string CreateVerificationToken(SmtpDocumentConfiguration configuration)
        {
            string secret = _configuration["fireBdata:fireClave"] ?? string.Empty;
            using HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            string payload = string.Join('\n',
                configuration.Cuenta.Trim().ToLowerInvariant(),
                configuration.Contrasena.Trim(),
                configuration.ServidorSmtp.Trim().ToLowerInvariant(),
                configuration.Puerto.ToString(),
                CorreoSalienteSeguridad.Normalize(configuration.Seguridad));
            return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
        }

        private bool TryResolveRequestContext(Guid? clientEmpresaId, string? clientEmpresaKey, out RequestContext context, out IActionResult? error)
        {
            context = null!;
            error = null;

            Guid? effectiveEmpresaId = TryResolveEmpresaId(out string? proxyEmpresaKey);
            if (!effectiveEmpresaId.HasValue || effectiveEmpresaId.Value == Guid.Empty)
            {
                error = Unauthorized(new CorreoSalienteOperacionResponse { Exito = false, Mensaje = "No fue posible resolver la empresa activa." });
                return false;
            }

            if (clientEmpresaId.HasValue && clientEmpresaId.Value != Guid.Empty && clientEmpresaId.Value != effectiveEmpresaId.Value)
            {
                error = BadRequest(new CorreoSalienteOperacionResponse { Exito = false, Mensaje = "La empresa solicitada no coincide con la sesión activa." });
                return false;
            }

            string empresaStorageKey = TryResolveEmpresaStorageKey(effectiveEmpresaId.Value, proxyEmpresaKey);
            if (!string.IsNullOrWhiteSpace(clientEmpresaKey) &&
                !string.Equals(clientEmpresaKey.Trim(), empresaStorageKey, StringComparison.OrdinalIgnoreCase))
            {
                error = BadRequest(new CorreoSalienteOperacionResponse { Exito = false, Mensaje = "La empresa solicitada no coincide con la sesión activa." });
                return false;
            }

            context = new RequestContext
            {
                IdEmpresa = effectiveEmpresaId.Value,
                EmpresaStorageKey = empresaStorageKey
            };
            return true;
        }

        private Guid? TryResolveEmpresaId(out string? proxyEmpresaKey)
        {
            proxyEmpresaKey = null;

            foreach (string claimKey in EmpresaClaimKeys)
            {
                string? value = User.FindFirstValue(claimKey);
                if (Guid.TryParse(value, out Guid parsed) && parsed != Guid.Empty)
                {
                    return parsed;
                }
            }

            if (TryResolveSignedProxyContext(out SignedProxyContext? proxyContext) && proxyContext != null)
            {
                proxyEmpresaKey = proxyContext.EmpresaStorageKey;
                return proxyContext.IdEmpresa;
            }

            return null;
        }

        private string TryResolveEmpresaStorageKey(Guid empresaId, string? proxyEmpresaKey = null)
        {
            foreach (string claimKey in EmpresaNombreClaimKeys)
            {
                string? value = User.FindFirstValue(claimKey);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim().ToUpperInvariant();
                }
            }

            if (!string.IsNullOrWhiteSpace(proxyEmpresaKey))
            {
                return proxyEmpresaKey.Trim().ToUpperInvariant();
            }

            return empresaId.ToString("N").ToUpperInvariant();
        }

        private Guid? TryResolveUsuarioId()
        {
            foreach (string claimKey in UsuarioClaimKeys)
            {
                string? value = User.FindFirstValue(claimKey);
                if (Guid.TryParse(value, out Guid parsed) && parsed != Guid.Empty)
                {
                    return parsed;
                }
            }

            if (TryResolveSignedProxyContext(out SignedProxyContext? proxyContext) &&
                proxyContext != null &&
                proxyContext.UsuarioId.HasValue)
            {
                return proxyContext.UsuarioId.Value;
            }

            return null;
        }

        private bool TryResolveSignedProxyContext(out SignedProxyContext? context)
        {
            if (HttpContext.Items.TryGetValue(ProxyContextItemKey, out object? cached))
            {
                context = cached as SignedProxyContext;
                return context != null;
            }

            context = null;

            if (!Request.Headers.TryGetValue(ProxyEmpresaIdHeader, out var empresaIdHeader) ||
                !Request.Headers.TryGetValue(ProxyEmpresaKeyHeader, out var empresaKeyHeader) ||
                !Request.Headers.TryGetValue(ProxyTimestampHeader, out var timestampHeader) ||
                !Request.Headers.TryGetValue(ProxySignatureHeader, out var signatureHeader))
            {
                return false;
            }

            string secret = _configuration["fireBdata:fireClave"] ?? string.Empty;
            if (string.IsNullOrWhiteSpace(secret))
            {
                _logger.LogWarning("CorreoSaliente proxy headers recibidos sin secreto compartido configurado.");
                return false;
            }

            string empresaIdRaw = empresaIdHeader.ToString().Trim();
            string empresaKeyRaw = empresaKeyHeader.ToString().Trim();
            string usuarioIdRaw = Request.Headers.TryGetValue(ProxyUsuarioIdHeader, out var usuarioIdHeader)
                ? usuarioIdHeader.ToString().Trim()
                : string.Empty;
            string timestampRaw = timestampHeader.ToString().Trim();
            string signatureRaw = signatureHeader.ToString().Trim();

            if (!Guid.TryParse(empresaIdRaw, out Guid empresaId) || empresaId == Guid.Empty)
            {
                return false;
            }

            if (!DateTimeOffset.TryParse(timestampRaw, out DateTimeOffset timestamp))
            {
                return false;
            }

            TimeSpan drift = DateTimeOffset.UtcNow - timestamp;
            if (drift.Duration() > ProxyHeaderTolerance)
            {
                return false;
            }

            string normalizedUsuarioId = string.Empty;
            Guid? usuarioId = null;
            if (!string.IsNullOrWhiteSpace(usuarioIdRaw))
            {
                if (!Guid.TryParse(usuarioIdRaw, out Guid parsedUsuarioId) || parsedUsuarioId == Guid.Empty)
                {
                    return false;
                }

                usuarioId = parsedUsuarioId;
                normalizedUsuarioId = parsedUsuarioId.ToString();
            }

            string expectedSignature = ComputeProxySignature(secret, empresaIdRaw, empresaKeyRaw, normalizedUsuarioId, timestampRaw);
            if (!FixedTimeEquals(signatureRaw, expectedSignature))
            {
                _logger.LogWarning("Firma inválida en proxy de CorreoSaliente para empresa {EmpresaId}.", empresaId);
                return false;
            }

            context = new SignedProxyContext
            {
                IdEmpresa = empresaId,
                EmpresaStorageKey = empresaKeyRaw.Trim().ToUpperInvariant(),
                UsuarioId = usuarioId
            };

            HttpContext.Items[ProxyContextItemKey] = context;
            return true;
        }

        private static string ComputeProxySignature(string secret, string empresaId, string empresaKey, string usuarioId, string timestamp)
        {
            using HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            string payload = string.Join('\n', empresaId.Trim(), empresaKey.Trim().ToUpperInvariant(), usuarioId.Trim(), timestamp.Trim());
            return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
        }

        private static bool FixedTimeEquals(string left, string right)
        {
            byte[] leftBytes = Encoding.UTF8.GetBytes(left ?? string.Empty);
            byte[] rightBytes = Encoding.UTF8.GetBytes(right ?? string.Empty);
            return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                _ = new MailAddress(email);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string ReadString(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
        }

        private static DateTime? ReadNullableDateTime(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal))
            {
                return null;
            }

            DateTime value = reader.GetDateTime(ordinal);
            return DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        private IActionResult HandleException(Exception ex, string actionName, string userMessage)
        {
            _logger.LogError(ex, "Error en {ActionName} de CorreoSaliente.", actionName);
            return StatusCode(StatusCodes.Status500InternalServerError, new CorreoSalienteOperacionResponse
            {
                Exito = false,
                Mensaje = userMessage
            });
        }

        private sealed class RequestContext
        {
            public Guid IdEmpresa { get; set; }
            public string EmpresaStorageKey { get; set; } = string.Empty;
        }

        private sealed class SignedProxyContext
        {
            public Guid IdEmpresa { get; set; }
            public string EmpresaStorageKey { get; set; } = string.Empty;
            public Guid? UsuarioId { get; set; }
        }
    }
}
