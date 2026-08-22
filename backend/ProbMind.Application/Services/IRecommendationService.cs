using ProbMind.Application.Contracts;

namespace ProbMind.Application.Services;

public interface IRecommendationService
{
    Task<IReadOnlyList<RecommendationDto>> ListAsync(Guid userId, bool includeDismissed, CancellationToken ct = default);
    Task<IReadOnlyList<RecommendationDto>> RebuildAsync(Guid userId, CancellationToken ct = default);
    Task DismissAsync(Guid userId, Guid recommendationId, CancellationToken ct = default);
}
