using ProbMind.Application.Contracts;

namespace ProbMind.Application.Services;

public interface ILearningPathService
{
    Task<LearningPathDto> GetCurrentAsync(Guid userId, CancellationToken ct = default);
    Task<LearningPathDto> RebuildAsync(Guid userId, string reason, CancellationToken ct = default);
    Task<LearningPathDto> CompleteStepAsync(Guid userId, Guid stepId, CancellationToken ct = default);
    Task<LearningPathDto> SkipStepAsync(Guid userId, Guid stepId, CancellationToken ct = default);
    Task<IReadOnlyList<LearningPathDto>> HistoryAsync(Guid userId, CancellationToken ct = default);
}
