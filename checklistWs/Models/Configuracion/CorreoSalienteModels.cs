using System.ComponentModel.DataAnnotations;

namespace checklistWs.Models.Configuracion
{
    public sealed class CorreoSalienteConfiguracionViewModel
    {
        public bool ConfiguracionGuardada { get; set; }
        public string Cuenta { get; set; } = string.Empty;
        public string ServidorSmtp { get; set; } = string.Empty;
        public int Puerto { get; set; }
        public string Seguridad { get; set; } = CorreoSalienteSeguridad.SslTls;
        public string DestinatarioPrueba { get; set; } = string.Empty;
        public bool PasswordConfigurado { get; set; }
        public bool Verificada { get; set; }
        public DateTime? FechaUltimaPrueba { get; set; }
        public DateTime? FechaActualizacion { get; set; }
    }

    public sealed class ProbarCorreoSalienteRequest
    {
        public string Cuenta { get; set; } = string.Empty;
        public string Contrasena { get; set; } = string.Empty;
        public string ServidorSmtp { get; set; } = string.Empty;
        public int Puerto { get; set; }
        public string Seguridad { get; set; } = CorreoSalienteSeguridad.SslTls;
        public string DestinatarioPrueba { get; set; } = string.Empty;
    }

    public sealed class GuardarCorreoSalienteRequest
    {
        public string Cuenta { get; set; } = string.Empty;
        public string Contrasena { get; set; } = string.Empty;
        public string ServidorSmtp { get; set; } = string.Empty;
        public int Puerto { get; set; }
        public string Seguridad { get; set; } = CorreoSalienteSeguridad.SslTls;
        public string DestinatarioPrueba { get; set; } = string.Empty;
        public string TokenVerificacion { get; set; } = string.Empty;
    }

    public sealed class CorreoSalienteOperacionResponse
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string TokenVerificacion { get; set; } = string.Empty;
        public CorreoSalienteConfiguracionViewModel? Configuracion { get; set; }
    }

    public sealed class CorreoSalientePersistedConfiguration
    {
        public Guid Id { get; set; }
        public Guid IdEmpresa { get; set; }
        public Guid IdentityKey { get; set; }
        public string Cuenta { get; set; } = string.Empty;
        public string ServidorSmtp { get; set; } = string.Empty;
        public int Puerto { get; set; }
        public string Seguridad { get; set; } = CorreoSalienteSeguridad.SslTls;
        public string CredencialProtegida { get; set; } = string.Empty;
        public string DestinatarioPrueba { get; set; } = string.Empty;
        public bool ConfiguracionVerificada { get; set; }
        public DateTime? FechaUltimaPrueba { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaActualizacion { get; set; }
        public bool Activo { get; set; }
    }

    public sealed class SmtpDocumentConfiguration
    {
        public string Cuenta { get; set; } = string.Empty;
        public string Contrasena { get; set; } = string.Empty;
        public string ServidorSmtp { get; set; } = string.Empty;
        public int Puerto { get; set; }
        public string Seguridad { get; set; } = CorreoSalienteSeguridad.SslTls;
        public string DestinatarioPrueba { get; set; } = string.Empty;
    }

    public static class CorreoSalienteSeguridad
    {
        public const string SslTls = "SSL_TLS";
        public const string StartTls = "STARTTLS";

        public static string Normalize(string? value)
        {
            string normalized = (value ?? string.Empty).Trim().ToUpperInvariant();
            return normalized switch
            {
                "SSL" => SslTls,
                "TLS" => SslTls,
                "SSL/TLS" => SslTls,
                "SSL_TLS" => SslTls,
                "STARTTLS" => StartTls,
                _ => string.Empty
            };
        }
    }
}
