using ProbMind.Application.Contracts;

namespace ProbMind.Application.Services;

public interface IAdminService
{
    Task<IReadOnlyList<AdminUserDto>> ListUsersAsync(CancellationToken ct = default);
    Task<AdminUserDto> SetRoleAsync(Guid actorId, SetUserRoleRequest request, CancellationToken ct = default);
    Task<AdminUserDto> SetActiveAsync(Guid actorId, SetUserActiveRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<AuditLogDto>> AuditAsync(int take, CancellationToken ct = default);
}
