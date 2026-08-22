using ProbMind.Domain.Common;
using ProbMind.Domain.Enums;

namespace ProbMind.Domain.Entities;

public sealed class QuestionVersion : Entity
{
    public Guid QuestionId { get; set; }
    public int VersionNumber { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public string CorrectExplanation { get; set; } = string.Empty;
    public QuestionDifficulty Difficulty { get; set; } = QuestionDifficulty.Intermediate;
    public bool IsTransferQuestion { get; set; }
    public int EstimatedSeconds { get; set; } = 90;
    public string AuthorNotes { get; set; } = string.Empty;
}
