using System.Text.Json;
using ProbMind.Application.Abstractions.Persistence;
using ProbMind.Application.Contracts;
using ProbMind.Domain.Entities;
using ProbMind.Domain.Enums;

namespace ProbMind.Application.Services;

public sealed class AdminService : IAdminService
{
    private readonly IUnitOfWork _uow;

    public AdminService(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<AdminUserDto>> ListUsersAsync(CancellationToken ct = default)
    {
        var users = (await _uow.Users.ListAsync(ct))
            .Where(x => !x.Email.EndsWith("@probmind.test", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.CreatedAt)
            .ToArray();
        var groups = (await _uow.StudentGroups.ListAsync(ct)).ToDictionary(x => x.Id, x => x.Name);
        var memberships = await _uow.StudentGroupMembers.ListAsync(ct);

        return users
            .Select(user => Map(user, memberships
                .Where(x => x.StudentId == user.Id && groups.ContainsKey(x.GroupId))
                .Select(x => groups[x.GroupId])
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToArray()))
            .ToArray();
    }

    public async Task<AdminUserDto> SetRoleAsync(
        Guid actorId,
        SetUserRoleRequest request,
        CancellationToken ct = default)
    {
        var user = await _uow.Users.GetByIdAsync(request.UserId, ct)
            ?? throw new KeyNotFoundException("User not found.");
        var old = user.Role;
        user.Role = request.Role;
        user.Touch();
        _uow.Users.Update(user);

        await _uow.AuditLogs.AddAsync(new AuditLog
        {
            ActorUserId = actorId,
            Action = AuditAction.RoleChanged,
            EntityType = nameof(User),
            EntityId = user.Id,
            OldValueJson = JsonSerializer.Serialize(new { role = old.ToString() }),
            NewValueJson = JsonSerializer.Serialize(new { role = user.Role.ToString() }),
            RequestId = Guid.NewGuid().ToString("N")
        }, ct);

        await _uow.SaveChangesAsync(ct);
        return Map(user, await GroupNamesAsync(user.Id, ct));
    }

    public async Task<AdminUserDto> SetActiveAsync(
        Guid actorId,
        SetUserActiveRequest request,
        CancellationToken ct = default)
    {
        if (actorId == request.UserId && !request.IsActive)
            throw new InvalidOperationException("Administrator cannot deactivate the current account.");

        var user = await _uow.Users.GetByIdAsync(request.UserId, ct)
            ?? throw new KeyNotFoundException("User not found.");
        var old = user.IsActive;
        user.IsActive = request.IsActive;
        user.Touch();
        _uow.Users.Update(user);

        await _uow.AuditLogs.AddAsync(new AuditLog
        {
            ActorUserId = actorId,
            Action = AuditAction.Updated,
            EntityType = nameof(User),
            EntityId = user.Id,
            OldValueJson = JsonSerializer.Serialize(new { isActive = old }),
            NewValueJson = JsonSerializer.Serialize(new { isActive = user.IsActive }),
            RequestId = Guid.NewGuid().ToString("N")
        }, ct);

        await _uow.SaveChangesAsync(ct);
        return Map(user, await GroupNamesAsync(user.Id, ct));
    }

    public async Task<IReadOnlyList<AuditLogDto>> AuditAsync(int take, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 500);
        return (await _uow.AuditLogs.ListAsync(ct))
            .OrderByDescending(x => x.CreatedAt)
            .Take(take)
            .Select(x => new AuditLogDto(
                x.Id, x.ActorUserId, x.Action, x.EntityType, x.EntityId,
                x.OldValueJson, x.NewValueJson, x.RequestId, x.CreatedAt))
            .ToArray();
    }

    private async Task<IReadOnlyList<string>> GroupNamesAsync(Guid userId, CancellationToken ct)
    {
        var memberships = await _uow.StudentGroupMembers.WhereAsync(x => x.StudentId == userId, ct);
        if (memberships.Count == 0) return Array.Empty<string>();
        var ids = memberships.Select(x => x.GroupId).ToHashSet();
        return (await _uow.StudentGroups.ListAsync(ct))
            .Where(x => ids.Contains(x.Id))
            .Select(x => x.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToArray();
    }

    private static AdminUserDto Map(User x, IReadOnlyList<string> groupNames) =>
        new(x.Id, x.Email, x.DisplayName, x.Role, x.IsActive, x.LastLoginAt, x.CreatedAt, groupNames);
}
