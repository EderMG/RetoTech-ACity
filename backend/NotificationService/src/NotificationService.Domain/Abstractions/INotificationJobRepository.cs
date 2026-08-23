using NotificationService.Domain.Entities;

namespace NotificationService.Domain.Abstractions;

public interface INotificationJobRepository
{
    Task<bool> ExistsByMessageIdAsync(Guid messageId, CancellationToken ct = default);
    Task AddAsync(NotificationJob job, CancellationToken ct = default);
    Task UpdateAsync(NotificationJob job, CancellationToken ct = default);
}
