using ProbMind.Domain.Common;

namespace ProbMind.Domain.Entities;

public sealed class ExamAttempt : Entity
{
    public Guid ExamId { get; set; }
    public Guid StudentId { get; set; }
    public Guid DiagnosticSessionId { get; set; }
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
}
