using ProbMind.Domain.Common;

namespace ProbMind.Domain.Entities;

public sealed class GamificationSettings : Entity
{
    public bool LeaderboardEnabled { get; set; } = true;
}
