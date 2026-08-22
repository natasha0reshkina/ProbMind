using ProbMind.Application.Contracts;

namespace ProbMind.Application.Services;

public interface IResearchAnalyticsService
{
    Task<IReadOnlyList<CooccurrenceEdgeDto>> MisconceptionCooccurrenceAsync(CancellationToken ct = default);
    Task<CalibrationReportDto> ConfidenceCalibrationAsync(CancellationToken ct = default);
    Task<PathStabilityDto> LearningPathStabilityAsync(Guid userId, CancellationToken ct = default);
}
