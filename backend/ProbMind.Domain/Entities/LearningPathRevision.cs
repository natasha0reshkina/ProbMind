using ProbMind.Domain.Common;

namespace ProbMind.Domain.Entities;

public sealed class LearningPathRevision : Entity
{
    public Guid UserId { get; set; }
    public Guid LearningPathId { get; set; }
    public int RevisionNumber { get; set; }
    public string SnapshotJson { get; set; } = "{}";
    public string ChangeReason { get; set; } = string.Empty;
    public DateTimeOffset RecordedAt { get; set; } = DateTimeOffset.UtcNow;
}
