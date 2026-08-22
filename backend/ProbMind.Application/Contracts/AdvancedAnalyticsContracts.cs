namespace ProbMind.Application.Contracts;

public sealed record PsychometricItemDto(
    Guid QuestionId,
    string Code,
    string TopicCode,
    int Responses,
    double CorrectRate,
    double IrtDifficulty,
    double Discrimination,
    double InformationAtAverageAbility,
    double MedianResponseSeconds,
    string QualityBand);

public sealed record LearnerForecastTopicDto(
    Guid TopicId,
    string TopicCode,
    string TopicName,
    double CurrentMastery,
    double Forecast7Days,
    double Forecast30Days,
    double DailyTrend,
    double Confidence,
    string Direction);

public sealed record LearnerForecastDto(
    Guid UserId,
    IReadOnlyList<LearnerForecastTopicDto> Topics,
    double MeanCurrentMastery,
    double MeanForecast30Days,
    string OverallDirection);

public sealed record CohortBenchmarkDto(
    Guid UserId,
    string DisplayName,
    double Mastery,
    double Percentile,
    double ZScore,
    string Band,
    int ActiveMisconceptions);

public sealed record StudentRiskDto(
    Guid UserId,
    string DisplayName,
    double Risk,
    string Level,
    IReadOnlyList<string> Drivers,
    double Mastery,
    int ActiveMisconceptions,
    int DaysInactive);

public sealed record MisconceptionClusterDto(
    int Cluster,
    string Label,
    int Learners,
    IReadOnlyList<double> Centroid,
    IReadOnlyList<Guid> UserIds);

public sealed record ContentDriftDto(
    Guid QuestionId,
    string Code,
    int BaselineResponses,
    int RecentResponses,
    double AccuracyShift,
    double ResponseTimeShift,
    double EntropyShift,
    double DriftScore,
    bool RequiresReview,
    string Reason);

public sealed record AdvancedSystemOverviewDto(
    int Learners,
    int Questions,
    int Answers,
    int ActiveMisconceptions,
    int AtRiskLearners,
    int QuestionsRequiringReview,
    double MeanMastery,
    double MeanDiagnosticAccuracy,
    DateTimeOffset GeneratedAt);
