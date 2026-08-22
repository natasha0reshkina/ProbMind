namespace ProbMind.Domain.Learning;

public sealed record AdaptiveCandidate(
    Guid QuestionId,
    Guid TopicId,
    double TopicMastery,
    double MisconceptionRelevance,
    double MisconceptionConfidence,
    DateTimeOffset? LastSeenAt,
    int ExposureCount,
    double DifficultyDistance,
    bool IsTransfer,
    bool IsPublished = true);
