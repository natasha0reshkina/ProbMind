using ProbMind.Domain.Common;
using ProbMind.Domain.Enums;

namespace ProbMind.Domain.Entities;

public sealed class Question : Entity
{
    public Guid TopicId { get; set; }
    public string Code { get; set; } = string.Empty;
    public QuestionKind Kind { get; set; } = QuestionKind.Diagnostic;
    public ContentStatus Status { get; set; } = ContentStatus.Draft;
    public int CurrentVersionNumber { get; set; } = 1;
    public DateTimeOffset? PublishedAt { get; set; }
}
