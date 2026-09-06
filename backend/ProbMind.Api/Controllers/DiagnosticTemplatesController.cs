using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProbMind.Api.Auth;
using ProbMind.Application.Contracts;
using ProbMind.Application.Services;

namespace ProbMind.Api.Controllers;

[ApiController]
[Route("api/diagnostic-templates")]
[Authorize]
public sealed class DiagnosticTemplatesController : ControllerBase
{
    private readonly IDiagnosticTemplateService _templates;
    private readonly IDiagnosticService _diagnostics;

    public DiagnosticTemplatesController(
        IDiagnosticTemplateService templates,
        IDiagnosticService diagnostics)
    {
        _templates = templates;
        _diagnostics = diagnostics;
    }

    [HttpGet("teacher")]
    [Authorize(Roles = "Teacher,Admin")]
    public Task<IReadOnlyList<DiagnosticTemplateDto>> TeacherList(CancellationToken ct) =>
        _templates.ListForTeacherAsync(ct);

    [HttpGet("student")]
    [Authorize(Roles = "Student")]
    public Task<IReadOnlyList<DiagnosticTemplateDto>> StudentList(CancellationToken ct) =>
        _templates.ListForStudentAsync(UserContext.UserId(User), ct);

    [HttpGet("questions")]
    [Authorize(Roles = "Teacher,Admin")]
    public Task<IReadOnlyList<DiagnosticTemplateQuestionCandidateDto>> Questions(CancellationToken ct) =>
        _templates.ListQuestionsAsync(UserContext.UserId(User), ct);


    [HttpPost("questions/create")]
    [Authorize(Roles = "Teacher,Admin")]
    public Task<DiagnosticTemplateQuestionCandidateDto> CreateQuestion(
        CreateDiagnosticTemplateQuestionRequest request,
        CancellationToken ct) =>
        _templates.CreateQuestionAsync(UserContext.UserId(User), request, ct);

    [HttpPost]
    [Authorize(Roles = "Teacher,Admin")]
    public Task<DiagnosticTemplateDto> Create(CreateDiagnosticTemplateRequest request, CancellationToken ct) =>
        _templates.CreateAsync(UserContext.UserId(User), request, ct);

    [HttpPut("{templateId:guid}/publication")]
    [Authorize(Roles = "Teacher,Admin")]
    public Task<DiagnosticTemplateDto> SetPublication(
        Guid templateId,
        SetDiagnosticTemplatePublishedRequest request,
        CancellationToken ct) =>
        _templates.SetPublishedAsync(UserContext.UserId(User), templateId, request.IsPublished, ct);


    [HttpPut("{templateId:guid}/audience")]
    [Authorize(Roles = "Teacher,Admin")]
    public Task<DiagnosticTemplateDto> SetAudience(
        Guid templateId,
        SetDiagnosticTemplateAudienceRequest request,
        CancellationToken ct) =>
        _templates.SetAudienceAsync(UserContext.UserId(User), templateId, request.GroupId, request.StudentId, ct);


    [HttpPost("{templateId:guid}/questions")]
    [Authorize(Roles = "Teacher,Admin")]
    public Task<DiagnosticTemplateDto> AddQuestions(
        Guid templateId,
        AddDiagnosticTemplateQuestionsRequest request,
        CancellationToken ct) =>
        _templates.AddQuestionsAsync(UserContext.UserId(User), templateId, request.QuestionIds, ct);

    [HttpPost("{templateId:guid}/start")]
    [Authorize(Roles = "Student")]
    public Task<DiagnosticSessionDto> Start(Guid templateId, CancellationToken ct) =>
        _diagnostics.StartTemplateAsync(UserContext.UserId(User), templateId, ct);
}
