using ProbMind.Application.Abstractions.Clock;
using ProbMind.Application.Abstractions.Persistence;
using ProbMind.Application.Contracts;

namespace ProbMind.Application.Services;

public sealed class NotificationService : INotificationService
{
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;

    public NotificationService(IUnitOfWork uow, IClock clock)
    {
        _uow = uow;
        _clock = clock;
    }

    public async Task<IReadOnlyList<NotificationDto>> ListAsync(
        Guid userId,
        bool unreadOnly,
        CancellationToken ct = default)
    {
        var items = await _uow.Notifications.WhereAsync(
            x => x.UserId == userId && (!unreadOnly || !x.IsRead),
            ct);

        return items
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new NotificationDto(
                x.Id, x.Type, x.Title, x.Body, x.IsRead, x.CreatedAt, x.ReadAt))
            .ToArray();
    }

    public async Task MarkReadAsync(Guid userId, Guid notificationId, CancellationToken ct = default)
    {
        var item = await _uow.Notifications.GetByIdAsync(notificationId, ct)
            ?? throw new KeyNotFoundException("Notification not found.");
        if (item.UserId != userId)
            throw new UnauthorizedAccessException();

        item.IsRead = true;
        item.ReadAt = _clock.UtcNow;
        item.Touch();
        _uow.Notifications.Update(item);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task MarkAllReadAsync(Guid userId, CancellationToken ct = default)
    {
        var items = await _uow.Notifications.WhereAsync(x => x.UserId == userId && !x.IsRead, ct);
        foreach (var item in items)
        {
            item.IsRead = true;
            item.ReadAt = _clock.UtcNow;
            item.Touch();
            _uow.Notifications.Update(item);
        }
        await _uow.SaveChangesAsync(ct);
    }
}
