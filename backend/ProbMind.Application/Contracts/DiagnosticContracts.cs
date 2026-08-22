using ProbMind.Domain.Enums;

namespace ProbMind.Application.Contracts;

public sealed record StartDiagnosticRequest(int QuestionCount = 15);
public sealed record DiagnosticSessionDto(
    Guid Id,
    DiagnosticStatus Status,
    int PlannedQuestionCount,
    int AnsweredQuestionCount,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    double? OverallScore);

public sealed record AnswerOptionDto(Guid Id, string Text, int SortOrder);
public sealed record DiagnosticQuestionDto(
    Guid QuestionId,
    Guid VersionId,
    string TopicCode,
    string TopicName,
    string Prompt,
    QuestionDifficulty Difficulty,
    bool IsTransfer,
    IReadOnlyList<AnswerOptionDto> Options,
    string SelectionExplanation);

public sealed record SubmitDiagnosticAnswerRequest(
    Guid SessionId,
    Guid QuestionId,
    Guid QuestionVersionId,
    Guid AnswerOptionId,
    int ResponseTimeMs);

public sealed record AnswerFeedbackDto(
    bool IsCorrect,
    string Feedback,
    string CorrectExplanation,
    string? SuspectedMisconceptionCode,
    double? UpdatedConfidence,
    MisconceptionStatus? UpdatedStatus);

public sealed record DiagnosticReportDto(
    Guid SessionId,
    double Accuracy,
    int Answered,
    int Correct,
    IReadOnlyList<TopicProgressDto> Topics,
    IReadOnlyList<UserMisconceptionDto> Misconceptions,
    string Summary,
    DateTimeOffset GeneratedAt);

public sealed record TopicProgressDto(
    Guid TopicId,
    string Code,
    string Name,
    double Mastery,
    double Uncertainty,
    int ObservationCount);

public sealed record UserMisconceptionDto(
    Guid MisconceptionId,
    string Code,
    string Title,
    string Description,
    double Confidence,
    MisconceptionStatus Status,
    int EvidenceCount,
    DateTimeOffset? LastDetectedAt);
