using ProbMind.Application.Services;
using ProbMind.Domain.Entities;
using ProbMind.Domain.Enums;
using ProbMind.UnitTests.TestDoubles;

namespace ProbMind.UnitTests.Application;

public sealed class NotificationServiceTests
{
    private readonly InMemoryUnitOfWork _uow = new();
    private readonly MutableClock _clock = new();

    [Fact]
    public async Task List_UnreadOnlyFiltersReadItems()
    {
        var user = Guid.NewGuid();
        _uow.NotificationsStore.Seed(
            new Notification { UserId = user, Title = "Unread", IsRead = false },
            new Notification { UserId = user, Title = "Read", IsRead = true });

        var result = await Create().ListAsync(user, true);

        Assert.Single(result);
        Assert.Equal("Unread", result[0].Title);
    }

    [Fact]
    public async Task MarkRead_RecordsTimestamp()
    {
        var user = Guid.NewGuid();
        var notification = new Notification { UserId = user, Title = "Test" };
        _uow.NotificationsStore.Seed(notification);

        await Create().MarkReadAsync(user, notification.Id);

        Assert.True(notification.IsRead);
        Assert.Equal(_clock.UtcNow, notification.ReadAt);
    }

    [Fact]
    public async Task MarkRead_ProtectsOtherUsers()
    {
        var notification = new Notification { UserId = Guid.NewGuid(), Title = "Private" };
        _uow.NotificationsStore.Seed(notification);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            Create().MarkReadAsync(Guid.NewGuid(), notification.Id));
    }

    [Fact]
    public async Task MarkAllRead_ChangesOnlyCurrentUsersItems()
    {
        var user = Guid.NewGuid();
        var other = Guid.NewGuid();
        var mineA = new Notification { UserId = user, Title = "A" };
        var mineB = new Notification { UserId = user, Title = "B" };
        var theirs = new Notification { UserId = other, Title = "C" };
        _uow.NotificationsStore.Seed(mineA, mineB, theirs);

        await Create().MarkAllReadAsync(user);

        Assert.True(mineA.IsRead);
        Assert.True(mineB.IsRead);
        Assert.False(theirs.IsRead);
    }

    private NotificationService Create() => new(_uow, _clock);
}
