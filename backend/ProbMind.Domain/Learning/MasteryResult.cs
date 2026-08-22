namespace ProbMind.Domain.Learning;

public sealed record MasteryResult(
    double Mastery,
    double Uncertainty,
    int ObservationCount,
    double EffectiveCorrect,
    double EffectiveTotal,
    string Explanation);
