using ProbMind.Domain.Common;

namespace ProbMind.Domain.Entities;

public sealed class TopicMasteryHistory : Entity
{
    public Guid UserId { get; set; }
    public Guid TopicId { get; set; }
    public double Mastery { get; set; }
    public double Uncertainty { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset RecordedAt { get; set; } = DateTimeOffset.UtcNow;
}
