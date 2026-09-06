using ProbMind.Domain.Common;

namespace ProbMind.Domain.Entities;

public sealed class StudentAchievement : Entity
{
    public Guid UserId { get; set; }
    public string Code { get; set; } = string.Empty;
    public int Progress { get; set; }
    public int Target { get; set; }
    public DateTimeOffset? UnlockedAt { get; set; }
}
