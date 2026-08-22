using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProbMind.Api.Auth;
using ProbMind.Application.Contracts;
using ProbMind.Application.Services;

namespace ProbMind.Api.Controllers;

[ApiController]
[Route("api/practice")]
[Authorize(Roles = "Student")]
public sealed class PracticeController : ControllerBase
{
    private readonly IPracticeService _practice;

    public PracticeController(IPracticeService practice) => _practice = practice;

    [HttpPost]
    public Task<PracticeSessionDto> Start(StartPracticeRequest request, CancellationToken ct) =>
        _practice.StartAsync(UserContext.UserId(User), request, ct);

    [HttpGet("{sessionId:guid}")]
    public Task<PracticeSessionDto> Get(Guid sessionId, CancellationToken ct) =>
        _practice.GetAsync(UserContext.UserId(User), sessionId, ct);

    [HttpGet("{sessionId:guid}/next")]
    public Task<DiagnosticQuestionDto?> Next(Guid sessionId, CancellationToken ct) =>
        _practice.NextQuestionAsync(UserContext.UserId(User), sessionId, ct);

    [HttpPost("answers")]
    public Task<AnswerFeedbackDto> Submit(SubmitPracticeAnswerRequest request, CancellationToken ct) =>
        _practice.SubmitAsync(UserContext.UserId(User), request, ct);

    [HttpPost("{sessionId:guid}/complete")]
    public Task<PracticeResultDto> Complete(Guid sessionId, CancellationToken ct) =>
        _practice.CompleteAsync(UserContext.UserId(User), sessionId, ct);

    [HttpGet("history")]
    public Task<IReadOnlyList<PracticeSessionDto>> History(CancellationToken ct) =>
        _practice.HistoryAsync(UserContext.UserId(User), ct);
}
