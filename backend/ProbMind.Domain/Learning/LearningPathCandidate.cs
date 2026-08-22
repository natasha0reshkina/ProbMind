namespace ProbMind.Domain.Learning;

public sealed record LearningPathCandidate(
    Guid TopicId,
    Guid? MisconceptionId,
    double Mastery,
    double MisconceptionConfidence,
    DateTimeOffset? LastPracticedAt,
    string TopicName,
    string? MisconceptionTitle);
