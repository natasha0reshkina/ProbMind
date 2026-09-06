using ProbMind.Domain.Common;
using ProbMind.Domain.Enums;

namespace ProbMind.Domain.Entities;

public sealed class StudyItem : Entity
{
    public Guid CreatedByUserId { get; set; }
    public Guid? StudentId { get; set; }
    public StudyItemKind Kind { get; set; }
    public StudyItemVisibility Visibility { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string TeacherResponse { get; set; } = string.Empty;
    public bool DiscussInClass { get; set; }
    public DateTimeOffset? TeacherRespondedAt { get; set; }
}
