namespace ProbMind.Application.Contracts;

public sealed record StudentOverviewDto(
    Guid UserId,
    string DisplayName,
    string Email,
    double OverallMastery,
    int ActiveMisconceptions,
    int CorrectedMisconceptions,
    int CompletedDiagnostics,
    int CompletedPracticeSessions,
    DateTimeOffset? LastActivityAt,
    IReadOnlyList<TopicProgressDto> Topics,
    IReadOnlyList<UserMisconceptionDto> Misconceptions);

public sealed record StudentListItemDto(
    Guid UserId,
    string DisplayName,
    string Email,
    double OverallMastery,
    int ActiveMisconceptions,
    DateTimeOffset? LastActivityAt);

public sealed record QuestionAnalyticsDto(
    Guid QuestionId,
    string Code,
    int Responses,
    double CorrectRate,
    double Difficulty,
    double Discrimination,
    string QualityBand,
    double DistractorEntropy,
    double MedianResponseSeconds);

public sealed record ReliabilityDto(
    double CronbachAlpha,
    int Learners,
    int Items,
    string Interpretation);

public sealed record InterventionEffectivenessDto(
    Guid MisconceptionId,
    string Code,
    string Title,
    int Learners,
    double MeanConfidenceReduction,
    double MeanMasteryGain,
    double TransferPassRate,
    double MeanExercises,
    double CompositeEffectiveness);

public sealed record CohortSegmentDto(
    Guid UserId,
    string DisplayName,
    string Segment,
    double Priority,
    string Rationale);
