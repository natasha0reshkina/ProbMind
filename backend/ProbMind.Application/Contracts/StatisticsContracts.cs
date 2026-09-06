namespace ProbMind.Application.Contracts;

public sealed record TimelinePoint(DateTimeOffset At, double Value, string Label);
public sealed record DashboardDto(
    double OverallMastery,
    int ActiveMisconceptions,
    int CorrectedMisconceptions,
    int CompletedDiagnostics,
    int CompletedPracticeSessions,
    IReadOnlyList<TopicProgressDto> Topics,
    IReadOnlyList<RecommendationDto> Recommendations);

public sealed record MisconceptionPrevalenceDto(
    Guid MisconceptionId,
    string Code,
    string Title,
    int StudentsAffected,
    int TotalStudents,
    double Prevalence,
    double MeanConfidence,
    int EvidenceCount);

public sealed record CohortAnalyticsDto(
    int Students,
    int DiagnosticSessions,
    int Answers,
    double MeanAccuracy,
    IReadOnlyList<MisconceptionPrevalenceDto> Misconceptions,
    IReadOnlyList<TopicProgressDto> MeanTopicMastery);

public sealed record SystemMetricsDto(
    int Users,
    int Students,
    int Teachers,
    int Questions,
    int PublishedQuestions,
    int Diagnostics,
    int PracticeSessions,
    int EvidenceRecords,
    DateTimeOffset CalculatedAt);

public sealed record DiagnosticComparisonEndpointDto(
    Guid SessionId,
    DateTimeOffset GeneratedAt,
    double Accuracy,
    int DetectedMisconceptions);

public sealed record TopicComparisonDto(
    Guid TopicId,
    string Code,
    string Name,
    double? FromMastery,
    double? ToMastery,
    double? Delta);

public sealed record DiagnosticComparisonDto(
    DiagnosticComparisonEndpointDto From,
    DiagnosticComparisonEndpointDto To,
    double AccuracyDelta,
    int DetectedMisconceptionDelta,
    IReadOnlyList<TopicComparisonDto> Topics);
