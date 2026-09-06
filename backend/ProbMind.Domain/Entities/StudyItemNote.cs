using ProbMind.Domain.Common;

namespace ProbMind.Domain.Entities;

public sealed class StudyItemNote : Entity
{
    public Guid StudyItemId { get; set; }
    public Guid StudentId { get; set; }
    public string Body { get; set; } = string.Empty;
}
