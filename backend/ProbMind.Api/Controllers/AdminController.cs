using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProbMind.Api.Auth;
using ProbMind.Application.Contracts;
using ProbMind.Application.Services;

namespace ProbMind.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public sealed class AdminController : ControllerBase
{
    private readonly IAdminService _admin;

    public AdminController(IAdminService admin) => _admin = admin;

    [HttpGet("users")]
    public Task<IReadOnlyList<AdminUserDto>> Users(CancellationToken ct) =>
        _admin.ListUsersAsync(ct);

    [HttpPost("users/role")]
    public Task<AdminUserDto> SetRole(SetUserRoleRequest request, CancellationToken ct) =>
        _admin.SetRoleAsync(UserContext.UserId(User), request, ct);

    [HttpPost("users/active")]
    public Task<AdminUserDto> SetActive(SetUserActiveRequest request, CancellationToken ct) =>
        _admin.SetActiveAsync(UserContext.UserId(User), request, ct);

    [HttpGet("audit")]
    public Task<IReadOnlyList<AuditLogDto>> Audit([FromQuery] int take = 100, CancellationToken ct = default) =>
        _admin.AuditAsync(take, ct);
}
