using ProbMind.Domain.Enums;

namespace ProbMind.Domain.Learning;

public sealed record CorrectionPlanStep(
    ExerciseType Type,
    int Position,
    bool Required,
    string Goal);

public sealed record CorrectionPlan(
    Guid MisconceptionId,
    double StartingConfidence,
    IReadOnlyList<CorrectionPlanStep> Steps);
