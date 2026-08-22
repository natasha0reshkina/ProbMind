using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProbMind.Api.Auth;
using ProbMind.Application.Contracts;
using ProbMind.Application.Services;

namespace ProbMind.Api.Controllers;

[ApiController]
[Route("api/learning-paths")]
[Authorize(Roles = "Student")]
public sealed class LearningPathsController : ControllerBase
{
    private readonly ILearningPathService _paths;

    public LearningPathsController(ILearningPathService paths) => _paths = paths;

    [HttpGet("current")]
    public Task<LearningPathDto> Current(CancellationToken ct) =>
        _paths.GetCurrentAsync(UserContext.UserId(User), ct);

    [HttpPost("rebuild")]
    public Task<LearningPathDto> Rebuild(CancellationToken ct) =>
        _paths.RebuildAsync(UserContext.UserId(User), "Manual learner request", ct);

    [HttpPost("steps/{stepId:guid}/complete")]
    public Task<LearningPathDto> CompleteStep(Guid stepId, CancellationToken ct) =>
        _paths.CompleteStepAsync(UserContext.UserId(User), stepId, ct);

    [HttpPost("steps/{stepId:guid}/skip")]
    public Task<LearningPathDto> SkipStep(Guid stepId, CancellationToken ct) =>
        _paths.SkipStepAsync(UserContext.UserId(User), stepId, ct);

    [HttpGet("history")]
    public Task<IReadOnlyList<LearningPathDto>> History(CancellationToken ct) =>
        _paths.HistoryAsync(UserContext.UserId(User), ct);
}
