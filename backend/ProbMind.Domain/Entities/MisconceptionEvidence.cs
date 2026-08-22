using ProbMind.Domain.Common;
using ProbMind.Domain.Enums;

namespace ProbMind.Domain.Entities;

public sealed class MisconceptionEvidence : Entity
{
    public Guid UserId { get; set; }
    public Guid MisconceptionId { get; set; }
    public Guid? DiagnosticAnswerId { get; set; }
    public Guid? PracticeAttemptId { get; set; }
    public EvidenceKind Kind { get; set; }
    public double RawWeight { get; set; }
    public double RelevanceWeight { get; set; } = 1d;
    public string Explanation { get; set; } = string.Empty;
    public DateTimeOffset ObservedAt { get; set; } = DateTimeOffset.UtcNow;
}
