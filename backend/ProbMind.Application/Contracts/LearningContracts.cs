using ProbMind.Domain.Enums;

namespace ProbMind.Application.Contracts;

public sealed record LearningPathStepDto(
    Guid Id,
    int Position,
    Guid TopicId,
    string TopicCode,
    string TopicName,
    Guid? MisconceptionId,
    string? MisconceptionCode,
    string? MisconceptionTitle,
    double Priority,
    LearningStepStatus Status,
    string Reason);

public sealed record LearningPathDto(
    Guid Id,
    int Revision,
    LearningPathStatus Status,
    DateTimeOffset BuiltAt,
    string BuildReason,
    IReadOnlyList<LearningPathStepDto> Steps);

public sealed record RecommendationDto(
    Guid Id,
    RecommendationType Type,
    string Title,
    string Rationale,
    double Priority,
    Guid? TopicId,
    Guid? MisconceptionId,
    bool IsDismissed,
    DateTimeOffset GeneratedAt);

public sealed record EvidenceDto(
    Guid Id,
    EvidenceKind Kind,
    double Weight,
    double Relevance,
    string Explanation,
    DateTimeOffset ObservedAt);

public sealed record EvidenceContributionDto(
    Guid EvidenceId,
    EvidenceKind Kind,
    double SignedContribution,
    double Share,
    string Direction,
    string Reason);

public sealed record DiagnosticReasoningDto(
    string Summary,
    string ConfidenceBand,
    IReadOnlyList<string> SupportingReasons,
    IReadOnlyList<string> ContradictingReasons,
    string NextAction,
    IReadOnlyList<EvidenceContributionDto> Contributions);

public sealed record MisconceptionDetailDto(
    UserMisconceptionDto State,
    IReadOnlyList<EvidenceDto> Evidence,
    string CorrectiveExplanation,
    string DiagnosticRationale,
    DiagnosticReasoningDto Reasoning);
