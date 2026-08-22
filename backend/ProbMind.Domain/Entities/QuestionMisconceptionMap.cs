using ProbMind.Domain.Common;

namespace ProbMind.Domain.Entities;

public sealed class QuestionMisconceptionMap : Entity
{
    public Guid QuestionId { get; set; }
    public Guid MisconceptionId { get; set; }
    public double RelevanceWeight { get; set; } = 1d;
    public bool CanDisconfirm { get; set; } = true;
}
