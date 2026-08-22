namespace ProbMind.Domain.Learning;

public sealed record LearningPathPriority(
    Guid TopicId,
    Guid? MisconceptionId,
    double Priority,
    string Reason);
