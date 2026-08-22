using ProbMind.Domain.Common;

namespace ProbMind.Domain.Entities;

public sealed class AnswerOption : Entity
{
    public Guid QuestionVersionId { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public Guid? MisconceptionId { get; set; }
    public string Feedback { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
