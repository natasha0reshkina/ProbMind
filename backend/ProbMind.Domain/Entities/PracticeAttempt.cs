using ProbMind.Domain.Common;
using ProbMind.Domain.Enums;

namespace ProbMind.Domain.Entities;

public sealed class PracticeAttempt : Entity
{
    public Guid PracticeSessionId { get; set; }
    public Guid UserId { get; set; }
    public Guid QuestionId { get; set; }
    public Guid QuestionVersionId { get; set; }
    public Guid AnswerOptionId { get; set; }
    public ExerciseType ExerciseType { get; set; }
    public bool IsCorrect { get; set; }
    public int ResponseTimeMs { get; set; }
    public string? StudentNote { get; set; }
    public int? ConfidenceLevel { get; set; }
    public string? Reasoning { get; set; }
    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;
}
