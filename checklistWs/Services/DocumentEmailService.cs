using System.Net.Sockets;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using checklistWs.Models.Configuracion;

namespace checklistWs.Services
{
    public sealed class DocumentEmailService
    {
        public async Task SendTestEmailAsync(SmtpDocumentConfiguration configuration, CancellationToken cancellationToken = default)
        {
            using SmtpClient client = new SmtpClient();

            try
            {
                client.CheckCertificateRevocation = false;
                await client.ConnectAsync(
                    configuration.ServidorSmtp,
                    configuration.Puerto,
                    ResolveSecurity(configuration.Seguridad),
                    cancellationToken);
            }
            catch (Exception ex) when (IsConnectionException(ex))
            {
                throw new DocumentEmailConnectionException(ex);
            }

            try
            {
                await client.AuthenticateAsync(configuration.Cuenta, configuration.Contrasena, cancellationToken);
            }
            catch (Exception ex)
            {
                try
                {
                    if (client.IsConnected)
                    {
                        await client.DisconnectAsync(true, cancellationToken);
                    }
                }
                catch
                {
                }

                throw new DocumentEmailAuthenticationException(ex);
            }

            try
            {
                MimeMessage message = new MimeMessage();
                message.From.Add(MailboxAddress.Parse(configuration.Cuenta));
                message.To.Add(MailboxAddress.Parse(configuration.DestinatarioPrueba));
                message.Subject = "Prueba de correo saliente — CheckApp";
                message.Body = new TextPart("plain")
                {
                    Text = "Esta es una prueba de configuración del correo saliente de CheckApp."
                };

                await client.SendAsync(message, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new DocumentEmailSendException(ex);
            }
            finally
            {
                try
                {
                    if (client.IsConnected)
                    {
                        await client.DisconnectAsync(true, cancellationToken);
                    }
                }
                catch
                {
                }
            }
        }

        private static SecureSocketOptions ResolveSecurity(string? value)
        {
            string normalized = CorreoSalienteSeguridad.Normalize(value);
            return normalized == CorreoSalienteSeguridad.StartTls
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.SslOnConnect;
        }

        private static bool IsConnectionException(Exception exception)
        {
            return exception is SocketException
                || exception is IOException
                || exception is TimeoutException
                || exception is SslHandshakeException
                || exception is SmtpProtocolException
                || exception is SmtpCommandException;
        }
    }

    public sealed class DocumentEmailConnectionException : Exception
    {
        public DocumentEmailConnectionException(Exception innerException)
            : base("No fue posible conectar con el servidor de correo.", innerException)
        {
        }
    }

    public sealed class DocumentEmailAuthenticationException : Exception
    {
        public DocumentEmailAuthenticationException(Exception innerException)
            : base("No fue posible autenticar la cuenta de correo.", innerException)
        {
        }
    }

    public sealed class DocumentEmailSendException : Exception
    {
        public DocumentEmailSendException(Exception innerException)
            : base("No fue posible enviar el correo de prueba.", innerException)
        {
        }
    }
}
