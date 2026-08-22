namespace ProbMind.Domain.Diagnostics;

public sealed record ConfidenceResult(
    double Confidence,
    double WeightedSupport,
    double WeightedContradiction,
    double DiversityScore,
    double RecencyScore,
    double TransferAdjustment,
    int EvidenceCount,
    int DistinctQuestions,
    string Explanation);
