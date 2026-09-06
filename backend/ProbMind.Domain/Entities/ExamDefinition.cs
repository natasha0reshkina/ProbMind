using ProbMind.Domain.Common;

namespace ProbMind.Domain.Entities;

public sealed class ExamDefinition : Entity
{
    public Guid CreatedByUserId { get; set; }
    public Guid DiagnosticTemplateId { get; set; }
    public Guid? GroupId { get; set; }
    public Guid? StudentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int TimeLimitMinutes { get; set; } = 30;
    public bool IsPublished { get; set; }
    public DateTimeOffset? AvailableFrom { get; set; }
    public DateTimeOffset? AvailableUntil { get; set; }
}
