using ProbMind.Domain.Common;
using ProbMind.Domain.Enums;

namespace ProbMind.Domain.Entities;

public sealed class ActivityEvent : Entity
{
    public Guid? UserId { get; set; }
    public ActivityEventType EventType { get; set; }
    public string AggregateType { get; set; } = string.Empty;
    public Guid? AggregateId { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public string CorrelationId { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}
