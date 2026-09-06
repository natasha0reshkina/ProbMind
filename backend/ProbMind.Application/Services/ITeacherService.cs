using ProbMind.Application.Contracts;

namespace ProbMind.Application.Services;

public interface ITeacherService
{
    Task<IReadOnlyList<StudentListItemDto>> ListStudentsAsync(CancellationToken ct = default);
    Task<StudentOverviewDto> StudentAsync(Guid studentId, CancellationToken ct = default);
    Task<IReadOnlyList<StudentMistakeDto>> StudentMistakesAsync(Guid studentId, int limit = 50, CancellationToken ct = default);
    Task<IReadOnlyList<StudentAnswerNoteDto>> StudentAnswerNotesAsync(Guid studentId, int limit = 100, CancellationToken ct = default);
    Task<IReadOnlyList<QuestionAnalyticsDto>> QuestionAnalyticsAsync(Guid? topicId, CancellationToken ct = default);
    Task<ReliabilityDto> DiagnosticReliabilityAsync(CancellationToken ct = default);
    Task<IReadOnlyList<InterventionEffectivenessDto>> InterventionEffectivenessAsync(CancellationToken ct = default);
    Task<IReadOnlyList<CohortSegmentDto>> SegmentsAsync(CancellationToken ct = default);
}
