using ProbMind.Domain.Common;

namespace ProbMind.Domain.Entities;

public sealed class TopicMastery : Entity
{
    public Guid UserId { get; set; }
    public Guid TopicId { get; set; }
    public double Mastery { get; set; } = 0.5d;
    public double Uncertainty { get; set; } = 1d;
    public int ObservationCount { get; set; }
    public DateTimeOffset? LastObservedAt { get; set; }
}
