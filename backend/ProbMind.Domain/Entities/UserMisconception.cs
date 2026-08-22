using ProbMind.Domain.Common;
using ProbMind.Domain.Enums;

namespace ProbMind.Domain.Entities;

public sealed class UserMisconception : Entity
{
    public Guid UserId { get; set; }
    public Guid MisconceptionId { get; set; }
    public double Confidence { get; set; }
    public MisconceptionStatus Status { get; set; } = MisconceptionStatus.Unknown;
    public int EvidenceCount { get; set; }
    public int PositiveEvidenceCount { get; set; }
    public int NegativeEvidenceCount { get; set; }
    public DateTimeOffset? FirstDetectedAt { get; set; }
    public DateTimeOffset? LastDetectedAt { get; set; }
    public DateTimeOffset? CorrectedAt { get; set; }
}
