using ProbMind.Domain.Common;

namespace ProbMind.Domain.Entities;

public sealed class SpacedReviewItem : Entity
{
    public Guid UserId { get; set; }
    public Guid TopicId { get; set; }
    public DateTimeOffset NextReviewAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastReviewedAt { get; set; }
    public int IntervalDays { get; set; } = 1;
    public double EaseFactor { get; set; } = 2.5d;
    public int Repetitions { get; set; }
}
