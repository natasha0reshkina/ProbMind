using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProbMind.Api.Auth;
using ProbMind.Application.Contracts;
using ProbMind.Application.Services;

namespace ProbMind.Api.Controllers;

[ApiController]
[Route("api/advanced-analytics")]
[Authorize]
public sealed class AdvancedAnalyticsController : ControllerBase
{
    private readonly IAdvancedAnalyticsService _analytics;

    public AdvancedAnalyticsController(IAdvancedAnalyticsService analytics) => _analytics = analytics;

    [HttpGet("forecast/me")]
    public Task<LearnerForecastDto> Forecast(CancellationToken ct) =>
        _analytics.LearnerForecastAsync(UserContext.UserId(User), ct);

    [HttpGet("psychometrics/items")]
    [Authorize(Roles = "Teacher,Admin")]
    public Task<IReadOnlyList<PsychometricItemDto>> Items([FromQuery] Guid? topicId, CancellationToken ct) =>
        _analytics.PsychometricItemsAsync(topicId, ct);

    [HttpGet("cohort/benchmarks")]
    [Authorize(Roles = "Teacher,Admin")]
    public Task<IReadOnlyList<CohortBenchmarkDto>> Benchmarks(CancellationToken ct) =>
        _analytics.CohortBenchmarksAsync(ct);

    [HttpGet("cohort/risk")]
    [Authorize(Roles = "Teacher,Admin")]
    public Task<IReadOnlyList<StudentRiskDto>> Risk(CancellationToken ct) =>
        _analytics.RiskRosterAsync(ct);

    [HttpGet("cohort/clusters")]
    [Authorize(Roles = "Teacher,Admin")]
    public Task<IReadOnlyList<MisconceptionClusterDto>> Clusters([FromQuery] int clusters = 3, CancellationToken ct = default) =>
        _analytics.MisconceptionClustersAsync(clusters, ct);

    [HttpGet("content/drift")]
    [Authorize(Roles = "Teacher,Admin")]
    public Task<IReadOnlyList<ContentDriftDto>> Drift(CancellationToken ct) =>
        _analytics.ContentDriftAsync(ct);

    [HttpGet("system")]
    [Authorize(Roles = "Teacher,Admin")]
    public Task<AdvancedSystemOverviewDto> System(CancellationToken ct) =>
        _analytics.SystemOverviewAsync(ct);
}
