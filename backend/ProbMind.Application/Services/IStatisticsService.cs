using ProbMind.Application.Contracts;

namespace ProbMind.Application.Services;

public interface IStatisticsService
{
    Task<DashboardDto> DashboardAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<TimelinePoint>> MasteryTimelineAsync(Guid userId, Guid topicId, CancellationToken ct = default);
    Task<IReadOnlyList<TimelinePoint>> MisconceptionTimelineAsync(Guid userId, Guid misconceptionId, CancellationToken ct = default);
    Task<CohortAnalyticsDto> CohortAsync(CancellationToken ct = default);
    Task<IReadOnlyList<MisconceptionPrevalenceDto>> PrevalenceAsync(CancellationToken ct = default);
    Task<SystemMetricsDto> SystemMetricsAsync(CancellationToken ct = default);
    Task<DiagnosticComparisonDto> CompareDiagnosticsAsync(Guid userId, Guid fromSessionId, Guid toSessionId, CancellationToken ct = default);
}
