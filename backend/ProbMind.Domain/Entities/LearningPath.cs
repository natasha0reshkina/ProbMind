using ProbMind.Domain.Common;
using ProbMind.Domain.Enums;

namespace ProbMind.Domain.Entities;

public sealed class LearningPath : Entity
{
    public Guid UserId { get; set; }
    public int Revision { get; set; } = 1;
    public LearningPathStatus Status { get; set; } = LearningPathStatus.Active;
    public string BuildReason { get; set; } = string.Empty;
    public DateTimeOffset BuiltAt { get; set; } = DateTimeOffset.UtcNow;
}
