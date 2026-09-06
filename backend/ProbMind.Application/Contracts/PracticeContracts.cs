using ProbMind.Domain.Enums;

namespace ProbMind.Application.Contracts;

public sealed record StartPracticeRequest(Guid TopicId, Guid? MisconceptionId, int TargetExercises = 5);
public sealed record SubmitPracticeAnswerRequest(
    Guid SessionId,
    Guid QuestionId,
    Guid QuestionVersionId,
    Guid AnswerOptionId,
    ExerciseType ExerciseType,
    int ResponseTimeMs,
    string? StudentNote = null,
    int? ConfidenceLevel = null,
    string? Reasoning = null);

public sealed record PracticeSessionDto(
    Guid Id,
    Guid TopicId,
    Guid? MisconceptionId,
    PracticeStatus Status,
    int TargetExercises,
    int CompletedExercises,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt);

public sealed record PracticeResultDto(
    PracticeSessionDto Session,
    int Correct,
    int Total,
    bool TransferPassed,
    double? MisconceptionConfidence,
    MisconceptionStatus? MisconceptionStatus,
    LearningPathDto? UpdatedLearningPath);
