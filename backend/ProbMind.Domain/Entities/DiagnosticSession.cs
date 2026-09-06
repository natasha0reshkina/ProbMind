using ProbMind.Domain.Common;
using ProbMind.Domain.Enums;

namespace ProbMind.Domain.Entities;

public sealed class DiagnosticSession : Entity
{
    public Guid UserId { get; set; }
    public DiagnosticStatus Status { get; set; } = DiagnosticStatus.Created;
    public int PlannedQuestionCount { get; set; } = 15;
    public int AnsweredQuestionCount { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public double? OverallScore { get; set; }
    public string SelectionPolicyVersion { get; set; } = "adaptive-v2";
    public Guid? DiagnosticTemplateId { get; set; }
}
