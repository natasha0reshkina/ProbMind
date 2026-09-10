using ProbMind.Domain.Enums;

namespace ProbMind.Application.Contracts;

public sealed record AdminUserDto(
    Guid Id,
    string Email,
    string DisplayName,
    UserRole Role,
    bool IsActive,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAt,
    IReadOnlyList<string> GroupNames);

public sealed record SetUserRoleRequest(Guid UserId, UserRole Role);
public sealed record SetUserActiveRequest(Guid UserId, bool IsActive);

public sealed record AuditLogDto(
    Guid Id,
    Guid? ActorUserId,
    AuditAction Action,
    string EntityType,
    Guid? EntityId,
    string OldValueJson,
    string NewValueJson,
    string RequestId,
    DateTimeOffset CreatedAt);
