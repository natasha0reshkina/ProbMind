using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProbMind.Api.Auth;
using ProbMind.Application.Contracts;
using ProbMind.Application.Services;

namespace ProbMind.Api.Controllers;

[ApiController]
[Route("api/recommendations")]
[Authorize(Roles = "Student")]
public sealed class RecommendationsController : ControllerBase
{
    private readonly IRecommendationService _recommendations;

    public RecommendationsController(IRecommendationService recommendations) =>
        _recommendations = recommendations;

    [HttpGet]
    public Task<IReadOnlyList<RecommendationDto>> List(
        [FromQuery] bool includeDismissed = false,
        CancellationToken ct = default) =>
        _recommendations.ListAsync(UserContext.UserId(User), includeDismissed, ct);

    [HttpPost("rebuild")]
    public Task<IReadOnlyList<RecommendationDto>> Rebuild(CancellationToken ct) =>
        _recommendations.RebuildAsync(UserContext.UserId(User), ct);

    [HttpPost("{recommendationId:guid}/dismiss")]
    public async Task<IActionResult> Dismiss(Guid recommendationId, CancellationToken ct)
    {
        await _recommendations.DismissAsync(UserContext.UserId(User), recommendationId, ct);
        return NoContent();
    }
}
