using ProbMind.Application.Contracts;

namespace ProbMind.Application.Services;

public interface IDiagnosticService
{
    Task<DiagnosticSessionDto> StartAsync(Guid userId, StartDiagnosticRequest request, CancellationToken ct = default);
    Task<DiagnosticSessionDto> StartTemplateAsync(Guid userId, Guid templateId, CancellationToken ct = default);
    Task<IReadOnlyList<DiagnosticSessionDto>> ListAsync(Guid userId, CancellationToken ct = default);
    Task<DiagnosticSessionDto> GetAsync(Guid userId, Guid sessionId, CancellationToken ct = default);
    Task<DiagnosticQuestionDto?> NextQuestionAsync(Guid userId, Guid sessionId, CancellationToken ct = default);
    Task<AnswerFeedbackDto> SubmitAsync(Guid userId, SubmitDiagnosticAnswerRequest request, CancellationToken ct = default);
    Task<DiagnosticReportDto> CompleteAsync(Guid userId, Guid sessionId, CancellationToken ct = default);
    Task<DiagnosticReportDto> ReportAsync(Guid userId, Guid sessionId, CancellationToken ct = default);
    Task<DiagnosticSessionDto> CancelAsync(Guid userId, Guid sessionId, CancellationToken ct = default);
}
