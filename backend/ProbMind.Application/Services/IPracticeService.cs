using ProbMind.Application.Contracts;

namespace ProbMind.Application.Services;

public interface IPracticeService
{
    Task<PracticeSessionDto> StartAsync(Guid userId, StartPracticeRequest request, CancellationToken ct = default);
    Task<PracticeSessionDto> GetAsync(Guid userId, Guid sessionId, CancellationToken ct = default);
    Task<DiagnosticQuestionDto?> NextQuestionAsync(Guid userId, Guid sessionId, CancellationToken ct = default);
    Task<AnswerFeedbackDto> SubmitAsync(Guid userId, SubmitPracticeAnswerRequest request, CancellationToken ct = default);
    Task<PracticeResultDto> CompleteAsync(Guid userId, Guid sessionId, CancellationToken ct = default);
    Task<IReadOnlyList<PracticeSessionDto>> HistoryAsync(Guid userId, CancellationToken ct = default);
}
