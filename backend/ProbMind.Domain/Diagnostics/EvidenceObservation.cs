using ProbMind.Domain.Enums;

namespace ProbMind.Domain.Diagnostics;

public sealed record EvidenceObservation(
    EvidenceKind Kind,
    double RawWeight,
    double RelevanceWeight,
    DateTimeOffset ObservedAt,
    Guid? QuestionId = null,
    bool IsTransfer = false);
