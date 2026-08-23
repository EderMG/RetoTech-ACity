using MailKit.Net.Smtp;
using MimeKit;
using NotificationService.Domain.Abstractions;

namespace NotificationService.Infrastructure.Email;

public class MailKitEmailSender : IEmailSender
{
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _fromAddress;

    public MailKitEmailSender(string smtpHost, int smtpPort, string fromAddress)
    {
        _smtpHost = smtpHost;
        _smtpPort = smtpPort;
        _fromAddress = fromAddress;
    }

    public async Task SendEventCreatedEmailAsync(string toAddress, string eventName, DateTime eventDate, string venue, CancellationToken ct = default)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_fromAddress));
        message.To.Add(MailboxAddress.Parse(toAddress));
        message.Subject = $"Nuevo evento publicado: {eventName}";
        message.Body = new TextPart("plain")
        {
            Text = $"Se ha publicado el evento \"{eventName}\" el {eventDate:dd/MM/yyyy} en {venue}."
        };

        using var client = new SmtpClient();
        // En entorno local usamos MailHog/Mailpit (sin TLS); en producción se debe forzar StartTls y credenciales.
        await client.ConnectAsync(_smtpHost, _smtpPort, MailKit.Security.SecureSocketOptions.Auto, ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }
}
