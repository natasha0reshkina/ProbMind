using ProbMind.Domain.Common;

namespace ProbMind.Domain.Entities;

public sealed class StudentGroupMember : Entity
{
    public Guid GroupId { get; set; }
    public Guid StudentId { get; set; }
}
