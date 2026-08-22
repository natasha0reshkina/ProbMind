using ProbMind.Application.Contracts;

namespace ProbMind.Application.Services;

public interface INotificationService
{
    Task<IReadOnlyList<NotificationDto>> ListAsync(Guid userId, bool unreadOnly, CancellationToken ct = default);
    Task MarkReadAsync(Guid userId, Guid notificationId, CancellationToken ct = default);
    Task MarkAllReadAsync(Guid userId, CancellationToken ct = default);
}
