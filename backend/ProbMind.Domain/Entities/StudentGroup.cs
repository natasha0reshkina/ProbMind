using ProbMind.Domain.Common;

namespace ProbMind.Domain.Entities;

public sealed class StudentGroup : Entity
{
    public Guid CreatedByUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
