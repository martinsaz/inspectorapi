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
            DocumentEmailMessage message = new DocumentEmailMessage
            {
                Destinatario = configuration.DestinatarioPrueba,
                Asunto = "Prueba de correo saliente — CheckApp",
                TextoPlano = "Esta es una prueba de configuración del correo saliente de CheckApp."
            };

            await SendDocumentEmailAsync(configuration, message, cancellationToken);
        }

        public async Task SendDocumentEmailAsync(
            SmtpDocumentConfiguration configuration,
            DocumentEmailMessage message,
            CancellationToken cancellationToken = default)
        {
            using SmtpClient client = new SmtpClient();

            await ConnectAndAuthenticateAsync(client, configuration, cancellationToken);

            try
            {
                MimeMessage mail = BuildMessage(configuration, message);
                await client.SendAsync(mail, cancellationToken);
            }
            catch (Exception ex)
            {
                throw new DocumentEmailSendException(ex);
            }
            finally
            {
                await SafeDisconnectAsync(client, cancellationToken);
            }
        }

        private static MimeMessage BuildMessage(SmtpDocumentConfiguration configuration, DocumentEmailMessage message)
        {
            MimeMessage mail = new MimeMessage();
            mail.From.Add(MailboxAddress.Parse(configuration.Cuenta));
            mail.To.Add(MailboxAddress.Parse(message.Destinatario));
            mail.Subject = message.Asunto;

            BodyBuilder builder = new BodyBuilder
            {
                TextBody = message.TextoPlano
            };

            if (!string.IsNullOrWhiteSpace(message.Html))
            {
                builder.HtmlBody = message.Html;
            }

            foreach (DocumentEmailAttachment attachment in message.Adjuntos)
            {
                builder.Attachments.Add(
                    attachment.FileName,
                    attachment.Content,
                    ContentType.Parse(attachment.ContentType));
            }

            mail.Body = builder.ToMessageBody();
            return mail;
        }

        private static async Task ConnectAndAuthenticateAsync(
            SmtpClient client,
            SmtpDocumentConfiguration configuration,
            CancellationToken cancellationToken)
        {
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
                await SafeDisconnectAsync(client, cancellationToken);
                throw new DocumentEmailAuthenticationException(ex);
            }
        }

        private static async Task SafeDisconnectAsync(SmtpClient client, CancellationToken cancellationToken)
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

    public sealed class DocumentEmailMessage
    {
        public string Destinatario { get; set; } = string.Empty;
        public string Asunto { get; set; } = string.Empty;
        public string TextoPlano { get; set; } = string.Empty;
        public string? Html { get; set; }
        public IReadOnlyCollection<DocumentEmailAttachment> Adjuntos { get; set; } = Array.Empty<DocumentEmailAttachment>();
    }

    public sealed class DocumentEmailAttachment
    {
        public string FileName { get; set; } = string.Empty;
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = "application/octet-stream";
    }
}
