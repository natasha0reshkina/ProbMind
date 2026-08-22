using ProbMind.Domain.Common;

namespace ProbMind.Domain.Entities;

public sealed class QuestionExposure : Entity
{
    public Guid UserId { get; set; }
    public Guid QuestionId { get; set; }
    public int ExposureCount { get; set; }
    public DateTimeOffset LastSeenAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? NextEligibleAt { get; set; }
}
