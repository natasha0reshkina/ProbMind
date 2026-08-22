using ProbMind.Application.Contracts;

namespace ProbMind.Application.Services;

public interface IAdvancedAnalyticsService
{
    Task<IReadOnlyList<PsychometricItemDto>> PsychometricItemsAsync(Guid? topicId = null, CancellationToken ct = default);
    Task<LearnerForecastDto> LearnerForecastAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<CohortBenchmarkDto>> CohortBenchmarksAsync(CancellationToken ct = default);
    Task<IReadOnlyList<StudentRiskDto>> RiskRosterAsync(CancellationToken ct = default);
    Task<IReadOnlyList<MisconceptionClusterDto>> MisconceptionClustersAsync(int clusters = 3, CancellationToken ct = default);
    Task<IReadOnlyList<ContentDriftDto>> ContentDriftAsync(CancellationToken ct = default);
    Task<AdvancedSystemOverviewDto> SystemOverviewAsync(CancellationToken ct = default);
}
