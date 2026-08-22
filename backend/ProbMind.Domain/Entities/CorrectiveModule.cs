using ProbMind.Domain.Common;
using ProbMind.Domain.Enums;

namespace ProbMind.Domain.Entities;

public sealed class CorrectiveModule : Entity
{
    public Guid MisconceptionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string TheoryMarkdown { get; set; } = string.Empty;
    public string WorkedExampleMarkdown { get; set; } = string.Empty;
    public int Revision { get; set; } = 1;
    public ContentStatus Status { get; set; } = ContentStatus.Published;
}
