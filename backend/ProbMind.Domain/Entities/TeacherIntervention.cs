using ProbMind.Domain.Common;

namespace ProbMind.Domain.Entities;

public sealed class TeacherIntervention : Entity
{
    public Guid CreatedByUserId { get; set; }
    public Guid StudentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Kind { get; set; } = "Practice";
    public bool IsCompleted { get; set; }
    public DateTimeOffset? DueAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
