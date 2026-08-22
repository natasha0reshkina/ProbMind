using ProbMind.Domain.Common;
using ProbMind.Domain.Enums;

namespace ProbMind.Domain.Entities;

public sealed class Misconception : Entity
{
    public Guid TopicId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CorrectiveExplanation { get; set; } = string.Empty;
    public string DiagnosticRationale { get; set; } = string.Empty;
    public ContentStatus Status { get; set; } = ContentStatus.Published;
}
