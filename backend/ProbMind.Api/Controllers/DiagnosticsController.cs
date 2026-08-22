using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProbMind.Api.Auth;
using ProbMind.Application.Contracts;
using ProbMind.Application.Services;

namespace ProbMind.Api.Controllers;

[ApiController]
[Route("api/diagnostics")]
[Authorize(Roles = "Student")]
public sealed class DiagnosticsController : ControllerBase
{
    private readonly IDiagnosticService _diagnostics;

    public DiagnosticsController(IDiagnosticService diagnostics) => _diagnostics = diagnostics;

    [HttpPost]
    public Task<DiagnosticSessionDto> Start(StartDiagnosticRequest request, CancellationToken ct) =>
        _diagnostics.StartAsync(UserContext.UserId(User), request, ct);

    [HttpGet]
    public Task<IReadOnlyList<DiagnosticSessionDto>> List(CancellationToken ct) =>
        _diagnostics.ListAsync(UserContext.UserId(User), ct);

    [HttpGet("{sessionId:guid}")]
    public Task<DiagnosticSessionDto> Get(Guid sessionId, CancellationToken ct) =>
        _diagnostics.GetAsync(UserContext.UserId(User), sessionId, ct);

    [HttpGet("{sessionId:guid}/next")]
    public Task<DiagnosticQuestionDto?> Next(Guid sessionId, CancellationToken ct) =>
        _diagnostics.NextQuestionAsync(UserContext.UserId(User), sessionId, ct);

    [HttpPost("answers")]
    public Task<AnswerFeedbackDto> Submit(SubmitDiagnosticAnswerRequest request, CancellationToken ct) =>
        _diagnostics.SubmitAsync(UserContext.UserId(User), request, ct);

    [HttpPost("{sessionId:guid}/complete")]
    public Task<DiagnosticReportDto> Complete(Guid sessionId, CancellationToken ct) =>
        _diagnostics.CompleteAsync(UserContext.UserId(User), sessionId, ct);

    [HttpGet("{sessionId:guid}/report")]
    public Task<DiagnosticReportDto> Report(Guid sessionId, CancellationToken ct) =>
        _diagnostics.ReportAsync(UserContext.UserId(User), sessionId, ct);
}
