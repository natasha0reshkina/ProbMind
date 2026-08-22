using ProbMind.Domain.Common;
using ProbMind.Domain.Enums;

namespace ProbMind.Domain.Entities;

public sealed class Recommendation : Entity
{
    public Guid UserId { get; set; }
    public Guid? TopicId { get; set; }
    public Guid? MisconceptionId { get; set; }
    public RecommendationType Type { get; set; }
    public double Priority { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Rationale { get; set; } = string.Empty;
    public bool IsDismissed { get; set; }
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
}
