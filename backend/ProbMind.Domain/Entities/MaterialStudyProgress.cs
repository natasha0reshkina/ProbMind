using ProbMind.Domain.Common;

namespace ProbMind.Domain.Entities;

public sealed class MaterialStudyProgress : Entity
{
    public Guid MaterialStudyCycleId { get; set; }
    public Guid StudentId { get; set; }
    public Guid? PreSessionId { get; set; }
    public DateTimeOffset? MaterialOpenedAt { get; set; }
    public Guid? PostSessionId { get; set; }
}
