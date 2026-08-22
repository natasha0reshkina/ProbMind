using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProbMind.Api.Auth;
using ProbMind.Application.Contracts;
using ProbMind.Application.Services;

namespace ProbMind.Api.Controllers;

[ApiController]
[Route("api/statistics")]
[Authorize]
public sealed class StatisticsController : ControllerBase
{
    private readonly IStatisticsService _statistics;

    public StatisticsController(IStatisticsService statistics) => _statistics = statistics;

    [HttpGet("dashboard")]
    [Authorize(Roles = "Student")]
    public Task<DashboardDto> Dashboard(CancellationToken ct) =>
        _statistics.DashboardAsync(UserContext.UserId(User), ct);

    [HttpGet("mastery/{topicId:guid}/timeline")]
    [Authorize(Roles = "Student")]
    public Task<IReadOnlyList<TimelinePoint>> MasteryTimeline(Guid topicId, CancellationToken ct) =>
        _statistics.MasteryTimelineAsync(UserContext.UserId(User), topicId, ct);

    [HttpGet("misconceptions/{misconceptionId:guid}/timeline")]
    [Authorize(Roles = "Student")]
    public Task<IReadOnlyList<TimelinePoint>> MisconceptionTimeline(Guid misconceptionId, CancellationToken ct) =>
        _statistics.MisconceptionTimelineAsync(UserContext.UserId(User), misconceptionId, ct);

    [HttpGet("diagnostics/compare")]
    [Authorize(Roles = "Student")]
    public Task<DiagnosticComparisonDto> CompareDiagnostics(
        [FromQuery] Guid fromSessionId,
        [FromQuery] Guid toSessionId,
        CancellationToken ct) =>
        _statistics.CompareDiagnosticsAsync(
            UserContext.UserId(User),
            fromSessionId,
            toSessionId,
            ct);

    [HttpGet("cohort")]
    [Authorize(Roles = "Teacher,Admin")]
    public Task<CohortAnalyticsDto> Cohort(CancellationToken ct) =>
        _statistics.CohortAsync(ct);

    [HttpGet("prevalence")]
    [Authorize(Roles = "Teacher,Admin")]
    public Task<IReadOnlyList<MisconceptionPrevalenceDto>> Prevalence(CancellationToken ct) =>
        _statistics.PrevalenceAsync(ct);

    [HttpGet("system")]
    [Authorize(Roles = "Admin")]
    public Task<SystemMetricsDto> System(CancellationToken ct) =>
        _statistics.SystemMetricsAsync(ct);
}
