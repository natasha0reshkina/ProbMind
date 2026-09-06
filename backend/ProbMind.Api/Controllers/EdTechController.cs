using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProbMind.Api.Auth;
using ProbMind.Application.Contracts;
using ProbMind.Application.Services;

namespace ProbMind.Api.Controllers;

[ApiController]
[Route("api/edtech")]
[Authorize]
public sealed class EdTechController : ControllerBase
{
    private readonly IEdTechService _service;

    public EdTechController(IEdTechService service) => _service = service;

    [HttpGet("repetition")]
    [Authorize(Roles = "Student")]
    public Task<IReadOnlyList<SpacedReviewDto>> Repetition(CancellationToken ct) =>
        _service.RepetitionAsync(UserContext.UserId(User), ct);

    [HttpPost("repetition/{topicId:guid}/complete")]
    [Authorize(Roles = "Student")]
    public Task<SpacedReviewDto> CompleteReview(Guid topicId, CompleteSpacedReviewRequest request, CancellationToken ct) =>
        _service.CompleteReviewAsync(UserContext.UserId(User), topicId, request.Quality, ct);

    [HttpGet("confidence")]
    [Authorize(Roles = "Student")]
    public Task<ConfidenceSummaryDto> Confidence(CancellationToken ct) =>
        _service.ConfidenceAsync(UserContext.UserId(User), ct);

    [HttpGet("teacher/confidence")]
    [Authorize(Roles = "Teacher,Admin")]
    public Task<IReadOnlyList<TeacherConfidenceStudentDto>> TeacherConfidence(CancellationToken ct) =>
        _service.TeacherConfidenceAsync(ct);

    [HttpGet("groups")]
    [Authorize(Roles = "Teacher,Admin")]
    public Task<IReadOnlyList<StudentGroupDto>> Groups(CancellationToken ct) => _service.GroupsAsync(ct);

    [HttpPost("groups")]
    [Authorize(Roles = "Teacher,Admin")]
    public Task<StudentGroupDto> CreateGroup(CreateStudentGroupRequest request, CancellationToken ct) =>
        _service.CreateGroupAsync(UserContext.UserId(User), request, ct);

    [HttpPut("groups/{groupId:guid}/members")]
    [Authorize(Roles = "Teacher,Admin")]
    public Task<StudentGroupDto> UpdateGroupMembers(Guid groupId, UpdateStudentGroupMembersRequest request, CancellationToken ct) =>
        _service.UpdateGroupMembersAsync(groupId, request.StudentIds, ct);

    [HttpGet("teacher/interventions/suggestions")]
    [Authorize(Roles = "Teacher,Admin")]
    public Task<IReadOnlyList<InterventionSuggestionDto>> InterventionSuggestions(CancellationToken ct) =>
        _service.InterventionSuggestionsAsync(ct);

    [HttpGet("teacher/interventions")]
    [Authorize(Roles = "Teacher,Admin")]
    public Task<IReadOnlyList<TeacherInterventionDto>> TeacherInterventions(CancellationToken ct) =>
        _service.TeacherInterventionsAsync(ct);

    [HttpPost("teacher/interventions")]
    [Authorize(Roles = "Teacher,Admin")]
    public Task<TeacherInterventionDto> CreateIntervention(CreateTeacherInterventionRequest request, CancellationToken ct) =>
        _service.CreateInterventionAsync(UserContext.UserId(User), request, ct);

    [HttpGet("interventions")]
    [Authorize(Roles = "Student")]
    public Task<IReadOnlyList<TeacherInterventionDto>> StudentInterventions(CancellationToken ct) =>
        _service.StudentInterventionsAsync(UserContext.UserId(User), ct);

    [HttpPost("interventions/{interventionId:guid}/complete")]
    [Authorize(Roles = "Student")]
    public Task<TeacherInterventionDto> CompleteIntervention(Guid interventionId, CancellationToken ct) =>
        _service.CompleteInterventionAsync(UserContext.UserId(User), interventionId, ct);

    [HttpGet("teacher/exams")]
    [Authorize(Roles = "Teacher,Admin")]
    public Task<IReadOnlyList<ExamDto>> TeacherExams(CancellationToken ct) => _service.TeacherExamsAsync(ct);

    [HttpGet("teacher/exams/{examId:guid}/review")]
    [Authorize(Roles = "Teacher,Admin")]
    public Task<TeacherExamReviewDto> TeacherExamReview(Guid examId, CancellationToken ct) =>
        _service.TeacherExamReviewAsync(examId, ct);

    [HttpGet("exams")]
    [Authorize(Roles = "Student")]
    public Task<IReadOnlyList<ExamDto>> StudentExams(CancellationToken ct) =>
        _service.StudentExamsAsync(UserContext.UserId(User), ct);

    [HttpPost("teacher/exams")]
    [Authorize(Roles = "Teacher,Admin")]
    public Task<ExamDto> CreateExam(CreateExamRequest request, CancellationToken ct) =>
        _service.CreateExamAsync(UserContext.UserId(User), request, ct);

    [HttpPost("exams/{examId:guid}/start")]
    [Authorize(Roles = "Student")]
    public Task<ExamStartDto> StartExam(Guid examId, CancellationToken ct) =>
        _service.StartExamAsync(UserContext.UserId(User), examId, ct);

    [HttpGet("exams/session/{sessionId:guid}")]
    [Authorize(Roles = "Student")]
    public Task<ExamSessionContextDto> ExamContext(Guid sessionId, CancellationToken ct) =>
        _service.ExamContextAsync(UserContext.UserId(User), sessionId, ct);

    [HttpGet("achievements")]
    [Authorize(Roles = "Student")]
    public Task<IReadOnlyList<AchievementDto>> Achievements(CancellationToken ct) =>
        _service.AchievementsAsync(UserContext.UserId(User), ct);

    [HttpGet("materials/cycles")]
    [Authorize(Roles = "Student")]
    public Task<IReadOnlyList<MaterialStudyCycleDto>> StudentMaterialCycles(CancellationToken ct) =>
        _service.StudentMaterialCyclesAsync(UserContext.UserId(User), ct);

    [HttpGet("teacher/materials/cycles")]
    [Authorize(Roles = "Teacher,Admin")]
    public Task<IReadOnlyList<TeacherMaterialCycleAnalyticsDto>> TeacherMaterialCycles(CancellationToken ct) =>
        _service.TeacherMaterialCyclesAsync(ct);

    [HttpPost("teacher/materials/cycles")]
    [Authorize(Roles = "Teacher,Admin")]
    public Task<MaterialStudyCycleDto> CreateMaterialCycle(CreateMaterialStudyCycleRequest request, CancellationToken ct) =>
        _service.CreateMaterialCycleAsync(UserContext.UserId(User), request, ct);

    [HttpPost("materials/cycles/{cycleId:guid}/open")]
    [Authorize(Roles = "Student")]
    public Task<MaterialStudyCycleDto> OpenMaterial(Guid cycleId, CancellationToken ct) =>
        _service.OpenMaterialAsync(UserContext.UserId(User), cycleId, ct);

    [HttpPost("materials/cycles/{cycleId:guid}/{stage}/start")]
    [Authorize(Roles = "Student")]
    public Task<DiagnosticSessionDto> StartMaterialDiagnostic(Guid cycleId, string stage, CancellationToken ct) =>
        _service.StartMaterialDiagnosticAsync(UserContext.UserId(User), cycleId, stage, ct);
}
