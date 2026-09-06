using ProbMind.Domain.Common;

namespace ProbMind.Domain.Entities;

public sealed class MaterialStudyCycle : Entity
{
    public Guid CreatedByUserId { get; set; }
    public Guid PreDiagnosticTemplateId { get; set; }
    public Guid PostDiagnosticTemplateId { get; set; }
    public Guid? GroupId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string MaterialText { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
}
