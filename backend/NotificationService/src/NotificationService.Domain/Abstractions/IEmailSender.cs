namespace NotificationService.Domain.Abstractions;

public interface IEmailSender
{
    Task SendEventCreatedEmailAsync(string toAddress, string eventName, DateTime eventDate, string venue, CancellationToken ct = default);
}
