using ProbMind.Application.Contracts;

namespace ProbMind.Application.Services;

public interface ILearnerModelService
{
    Task<IReadOnlyList<UserMisconceptionDto>> ListMisconceptionsAsync(Guid userId, CancellationToken ct = default);
    Task<MisconceptionDetailDto> GetMisconceptionAsync(Guid userId, Guid misconceptionId, CancellationToken ct = default);
    Task<IReadOnlyList<TopicProgressDto>> GetTopicMasteryAsync(Guid userId, CancellationToken ct = default);
    Task RecalculateMisconceptionAsync(Guid userId, Guid misconceptionId, CancellationToken ct = default);
    Task RecalculateTopicAsync(Guid userId, Guid topicId, CancellationToken ct = default);
    Task RecalculateAllAsync(Guid userId, CancellationToken ct = default);
}
