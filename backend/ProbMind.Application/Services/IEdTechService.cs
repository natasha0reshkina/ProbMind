using ProbMind.Application.Contracts;

namespace ProbMind.Application.Services;

public interface IEdTechService
{
    Task<IReadOnlyList<SpacedReviewDto>> RepetitionAsync(Guid userId, CancellationToken ct = default);
    Task<SpacedReviewDto> CompleteReviewAsync(Guid userId, Guid topicId, int quality, CancellationToken ct = default);
    Task<ConfidenceSummaryDto> ConfidenceAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<TeacherConfidenceStudentDto>> TeacherConfidenceAsync(CancellationToken ct = default);
    Task<IReadOnlyList<StudentGroupDto>> GroupsAsync(CancellationToken ct = default);
    Task<StudentGroupDto> CreateGroupAsync(Guid actorId, CreateStudentGroupRequest request, CancellationToken ct = default);
    Task<StudentGroupDto> UpdateGroupMembersAsync(Guid groupId, IReadOnlyList<Guid> studentIds, CancellationToken ct = default);
    Task<IReadOnlyList<InterventionSuggestionDto>> InterventionSuggestionsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TeacherInterventionDto>> TeacherInterventionsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TeacherInterventionDto>> StudentInterventionsAsync(Guid userId, CancellationToken ct = default);
    Task<TeacherInterventionDto> CreateInterventionAsync(Guid actorId, CreateTeacherInterventionRequest request, CancellationToken ct = default);
    Task<TeacherInterventionDto> CompleteInterventionAsync(Guid userId, Guid interventionId, CancellationToken ct = default);
    Task<IReadOnlyList<ExamDto>> TeacherExamsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ExamDto>> StudentExamsAsync(Guid userId, CancellationToken ct = default);
    Task<TeacherExamReviewDto> TeacherExamReviewAsync(Guid examId, CancellationToken ct = default);
    Task<ExamDto> CreateExamAsync(Guid actorId, CreateExamRequest request, CancellationToken ct = default);
    Task<ExamStartDto> StartExamAsync(Guid userId, Guid examId, CancellationToken ct = default);
    Task<ExamSessionContextDto> ExamContextAsync(Guid userId, Guid sessionId, CancellationToken ct = default);
    Task<IReadOnlyList<AchievementDto>> AchievementsAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<MaterialStudyCycleDto>> StudentMaterialCyclesAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<TeacherMaterialCycleAnalyticsDto>> TeacherMaterialCyclesAsync(CancellationToken ct = default);
    Task<MaterialStudyCycleDto> CreateMaterialCycleAsync(Guid actorId, CreateMaterialStudyCycleRequest request, CancellationToken ct = default);
    Task<MaterialStudyCycleDto> OpenMaterialAsync(Guid userId, Guid cycleId, CancellationToken ct = default);
    Task<DiagnosticSessionDto> StartMaterialDiagnosticAsync(Guid userId, Guid cycleId, string stage, CancellationToken ct = default);
}
