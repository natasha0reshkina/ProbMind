using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProbMind.Application.Contracts;
using ProbMind.Application.Services;

namespace ProbMind.Api.Controllers;

[ApiController]
[Route("api/teacher")]
[Authorize(Roles = "Teacher,Admin")]
public sealed class TeacherController : ControllerBase
{
    private readonly ITeacherService _teacher;

    public TeacherController(ITeacherService teacher) => _teacher = teacher;

    [HttpGet("students")]
    public Task<IReadOnlyList<StudentListItemDto>> Students(CancellationToken ct) =>
        _teacher.ListStudentsAsync(ct);

    [HttpGet("students/{studentId:guid}")]
    public Task<StudentOverviewDto> Student(Guid studentId, CancellationToken ct) =>
        _teacher.StudentAsync(studentId, ct);

    [HttpGet("students/{studentId:guid}/mistakes")]
    public Task<IReadOnlyList<StudentMistakeDto>> StudentMistakes(
        Guid studentId,
        [FromQuery] int limit,
        CancellationToken ct) =>
        _teacher.StudentMistakesAsync(studentId, limit <= 0 ? 50 : Math.Min(limit, 200), ct);

    [HttpGet("students/{studentId:guid}/answer-notes")]
    public Task<IReadOnlyList<StudentAnswerNoteDto>> StudentAnswerNotes(
        Guid studentId,
        [FromQuery] int limit,
        CancellationToken ct) =>
        _teacher.StudentAnswerNotesAsync(studentId, limit <= 0 ? 100 : Math.Min(limit, 300), ct);

    [HttpGet("questions/analytics")]
    public Task<IReadOnlyList<QuestionAnalyticsDto>> Questions(
        [FromQuery] Guid? topicId,
        CancellationToken ct) =>
        _teacher.QuestionAnalyticsAsync(topicId, ct);

    [HttpGet("diagnostics/reliability")]
    public Task<ReliabilityDto> Reliability(CancellationToken ct) =>
        _teacher.DiagnosticReliabilityAsync(ct);

    [HttpGet("interventions/effectiveness")]
    public Task<IReadOnlyList<InterventionEffectivenessDto>> Interventions(CancellationToken ct) =>
        _teacher.InterventionEffectivenessAsync(ct);

    [HttpGet("students/segments")]
    public Task<IReadOnlyList<CohortSegmentDto>> Segments(CancellationToken ct) =>
        _teacher.SegmentsAsync(ct);
}
