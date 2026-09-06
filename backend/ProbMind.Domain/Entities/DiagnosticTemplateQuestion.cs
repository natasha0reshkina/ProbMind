using ProbMind.Domain.Common;

namespace ProbMind.Domain.Entities;

public sealed class DiagnosticTemplateQuestion : Entity
{
    public Guid DiagnosticTemplateId { get; set; }
    public Guid QuestionId { get; set; }
    public int Position { get; set; }
}
