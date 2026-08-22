using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProbMind.Api.Auth;
using ProbMind.Application.Contracts;
using ProbMind.Application.Services;

namespace ProbMind.Api.Controllers;

[ApiController]
[Route("api/learner")]
[Authorize(Roles = "Student")]
public sealed class LearnerModelController : ControllerBase
{
    private readonly ILearnerModelService _learner;

    public LearnerModelController(ILearnerModelService learner) => _learner = learner;

    [HttpGet("misconceptions")]
    public Task<IReadOnlyList<UserMisconceptionDto>> Misconceptions(CancellationToken ct) =>
        _learner.ListMisconceptionsAsync(UserContext.UserId(User), ct);

    [HttpGet("misconceptions/{misconceptionId:guid}")]
    public Task<MisconceptionDetailDto> Misconception(Guid misconceptionId, CancellationToken ct) =>
        _learner.GetMisconceptionAsync(UserContext.UserId(User), misconceptionId, ct);

    [HttpGet("mastery")]
    public Task<IReadOnlyList<TopicProgressDto>> Mastery(CancellationToken ct) =>
        _learner.GetTopicMasteryAsync(UserContext.UserId(User), ct);

    [HttpPost("recalculate")]
    public async Task<IActionResult> Recalculate(CancellationToken ct)
    {
        await _learner.RecalculateAllAsync(UserContext.UserId(User), ct);
        return Accepted();
    }
}
