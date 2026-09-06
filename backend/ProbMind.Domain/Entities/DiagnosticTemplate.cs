using ProbMind.Domain.Common;

namespace ProbMind.Domain.Entities;

public sealed class DiagnosticTemplate : Entity
{
    public Guid CreatedByUserId { get; set; }
    public Guid? GroupId { get; set; }
    public Guid? StudentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
}
