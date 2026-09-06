using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProbMind.Api.Auth;
using ProbMind.Application.Contracts;
using ProbMind.Application.Services;

namespace ProbMind.Api.Controllers;

[ApiController]
[Route("api/gamification")]
[Authorize]
public sealed class GamificationController : ControllerBase
{
    private readonly IGamificationService _gamification;

    public GamificationController(IGamificationService gamification) => _gamification = gamification;

    [HttpGet("profile")]
    [Authorize(Roles = "Student")]
    public Task<GamificationProfileDto> Profile(CancellationToken ct) =>
        _gamification.ProfileAsync(UserContext.UserId(User), ct);

    [HttpPut("participation")]
    [Authorize(Roles = "Student")]
    public Task<GamificationProfileDto> Participation(
        UpdateLeaderboardParticipationRequest request,
        CancellationToken ct) =>
        _gamification.SetParticipationAsync(UserContext.UserId(User), request.Participate, ct);

    [HttpGet("leaderboard")]
    [Authorize(Roles = "Student")]
    public Task<LeaderboardDto> Leaderboard([FromQuery] string? category, CancellationToken ct) =>
        _gamification.LeaderboardAsync(UserContext.UserId(User), category ?? "overall", ct);

    [HttpGet("teacher/leaderboard")]
    [Authorize(Roles = "Teacher,Admin")]
    public Task<LeaderboardDto> TeacherLeaderboard([FromQuery] string? category, CancellationToken ct) =>
        _gamification.TeacherLeaderboardAsync(category ?? "overall", ct);

    [HttpGet("teacher/students")]
    [Authorize(Roles = "Teacher,Admin")]
    public Task<IReadOnlyList<TeacherGamificationStudentDto>> Students(CancellationToken ct) =>
        _gamification.StudentsAsync(ct);

    [HttpPut("teacher/enabled")]
    [Authorize(Roles = "Teacher,Admin")]
    public async Task<object> SetEnabled(UpdateLeaderboardAvailabilityRequest request, CancellationToken ct) =>
        new { enabled = await _gamification.SetLeaderboardEnabledAsync(request.Enabled, ct) };
}
