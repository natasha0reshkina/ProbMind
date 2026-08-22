using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProbMind.Application.Contracts;
using ProbMind.Application.Services;

namespace ProbMind.Api.Controllers;

[ApiController]
[Route("api/research")]
[Authorize(Roles = "Teacher,Admin")]
public sealed class ResearchController : ControllerBase
{
    private readonly IResearchAnalyticsService _research;

    public ResearchController(IResearchAnalyticsService research) => _research = research;

    [HttpGet("misconceptions/cooccurrence")]
    public Task<IReadOnlyList<CooccurrenceEdgeDto>> Cooccurrence(CancellationToken ct) =>
        _research.MisconceptionCooccurrenceAsync(ct);

    [HttpGet("diagnostics/calibration")]
    public Task<CalibrationReportDto> Calibration(CancellationToken ct) =>
        _research.ConfidenceCalibrationAsync(ct);

    [HttpGet("students/{studentId:guid}/path-stability")]
    public Task<PathStabilityDto> PathStability(Guid studentId, CancellationToken ct) =>
        _research.LearningPathStabilityAsync(studentId, ct);
}
