using ProbMind.Domain.Common;
using ProbMind.Domain.Enums;

namespace ProbMind.Domain.Entities;

public sealed class LearningPathStep : Entity
{
    public Guid LearningPathId { get; set; }
    public Guid TopicId { get; set; }
    public Guid? MisconceptionId { get; set; }
    public int Position { get; set; }
    public double PriorityScore { get; set; }
    public LearningStepStatus Status { get; set; } = LearningStepStatus.Pending;
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset? CompletedAt { get; set; }
}
