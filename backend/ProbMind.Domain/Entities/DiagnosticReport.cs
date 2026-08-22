using ProbMind.Domain.Common;

namespace ProbMind.Domain.Entities;

public sealed class DiagnosticReport : Entity
{
    public Guid SessionId { get; set; }
    public Guid UserId { get; set; }
    public double Accuracy { get; set; }
    public string SummaryMarkdown { get; set; } = string.Empty;
    public string StrongestTopicCode { get; set; } = string.Empty;
    public string WeakestTopicCode { get; set; } = string.Empty;
    public int DetectedMisconceptionCount { get; set; }
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;
}
