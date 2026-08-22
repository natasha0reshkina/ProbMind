using ProbMind.Domain.Common;
using ProbMind.Domain.Enums;

namespace ProbMind.Domain.Entities;

public sealed class Topic : Entity
{
    public string Code { get; set; } = string.Empty;
    public string NameRu { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public ContentStatus Status { get; set; } = ContentStatus.Published;
}
