using ProbMind.Application.Contracts;
using ProbMind.Application.Services;
using ProbMind.Domain.Entities;
using ProbMind.Domain.Enums;
using ProbMind.UnitTests.TestDoubles;

namespace ProbMind.UnitTests.Application;

public sealed class AdminServiceTests
{
    private readonly InMemoryUnitOfWork _uow = new();

    [Fact]
    public async Task SetRole_UpdatesUserAndWritesAudit()
    {
        var actor = Guid.NewGuid();
        var user = new User { Email = "student@test", DisplayName = "Student", Role = UserRole.Student };
        _uow.UsersStore.Seed(user);

        var result = await Create().SetRoleAsync(
            actor,
            new SetUserRoleRequest(user.Id, UserRole.Teacher));

        Assert.Equal(UserRole.Teacher, result.Role);
        var audit = Assert.Single(_uow.AuditLogsStore.Items);
        Assert.Equal(AuditAction.RoleChanged, audit.Action);
        Assert.Equal(actor, audit.ActorUserId);
        Assert.Contains("Student", audit.OldValueJson);
        Assert.Contains("Teacher", audit.NewValueJson);
    }

    [Fact]
    public async Task SetActive_ChangesAccountState()
    {
        var user = new User { Email = "student@test", DisplayName = "Student", IsActive = true };
        _uow.UsersStore.Seed(user);

        var result = await Create().SetActiveAsync(
            Guid.NewGuid(),
            new SetUserActiveRequest(user.Id, false));

        Assert.False(result.IsActive);
        Assert.False(user.IsActive);
    }

    [Fact]
    public async Task AdminCannotDeactivateSelf()
    {
        var actor = Guid.NewGuid();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Create().SetActiveAsync(actor, new SetUserActiveRequest(actor, false)));
    }

    [Fact]
    public async Task Audit_ReturnsNewestFirst()
    {
        _uow.AuditLogsStore.Seed(
            new AuditLog { CreatedAt = DateTimeOffset.UtcNow.AddDays(-2), EntityType = "A" },
            new AuditLog { CreatedAt = DateTimeOffset.UtcNow, EntityType = "B" });

        var result = await Create().AuditAsync(100);

        Assert.Equal("B", result[0].EntityType);
        Assert.Equal("A", result[1].EntityType);
    }

    [Fact]
    public async Task ListUsers_ReturnsNewestFirst()
    {
        _uow.UsersStore.Seed(
            new User { Email = "old@test", DisplayName = "Old", CreatedAt = DateTimeOffset.UtcNow.AddDays(-2) },
            new User { Email = "new@test", DisplayName = "New", CreatedAt = DateTimeOffset.UtcNow });

        var result = await Create().ListUsersAsync();
        Assert.Equal("new@test", result[0].Email);
    }

    private AdminService Create() => new(_uow);
}
