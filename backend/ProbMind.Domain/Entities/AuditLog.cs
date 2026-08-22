using ProbMind.Domain.Common;
using ProbMind.Domain.Enums;

namespace ProbMind.Domain.Entities;

public sealed class AuditLog : Entity
{
    public Guid? ActorUserId { get; set; }
    public AuditAction Action { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string OldValueJson { get; set; } = "{}";
    public string NewValueJson { get; set; } = "{}";
    public string RequestId { get; set; } = string.Empty;
}
