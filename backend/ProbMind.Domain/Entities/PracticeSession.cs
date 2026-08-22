using ProbMind.Domain.Common;
using ProbMind.Domain.Enums;

namespace ProbMind.Domain.Entities;

public sealed class PracticeSession : Entity
{
    public Guid UserId { get; set; }
    public Guid TopicId { get; set; }
    public Guid? MisconceptionId { get; set; }
    public PracticeStatus Status { get; set; } = PracticeStatus.Created;
    public int TargetExercises { get; set; } = 5;
    public int CompletedExercises { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
