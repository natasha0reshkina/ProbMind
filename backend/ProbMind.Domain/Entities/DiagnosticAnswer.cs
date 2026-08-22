using ProbMind.Domain.Common;

namespace ProbMind.Domain.Entities;

public sealed class DiagnosticAnswer : Entity
{
    public Guid SessionId { get; set; }
    public Guid UserId { get; set; }
    public Guid QuestionId { get; set; }
    public Guid QuestionVersionId { get; set; }
    public Guid AnswerOptionId { get; set; }
    public bool IsCorrect { get; set; }
    public int ResponseTimeMs { get; set; }
    public int SequenceNumber { get; set; }
    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;
}
