namespace ProbMind.Domain.Analytics;

public sealed record InterventionObservation(
    Guid UserId,
    Guid MisconceptionId,
    double BeforeConfidence,
    double AfterConfidence,
    double BeforeMastery,
    double AfterMastery,
    int Exercises,
    bool TransferPassed);

public sealed record InterventionEffectiveness(
    int Learners,
    double MeanConfidenceReduction,
    double MeanMasteryGain,
    double TransferPassRate,
    double MeanExercises,
    double CompositeEffectiveness);

public sealed class InterventionEffectivenessAnalyzer
{
    public InterventionEffectiveness Analyze(IEnumerable<InterventionObservation> observations)
    {
        var data = observations.ToArray();
        if (data.Length == 0)
            return new InterventionEffectiveness(0, 0, 0, 0, 0, 0);

        var confidenceReduction = data.Average(x => x.BeforeConfidence - x.AfterConfidence);
        var masteryGain = data.Average(x => x.AfterMastery - x.BeforeMastery);
        var transfer = data.Count(x => x.TransferPassed) / (double)data.Length;
        var exercises = data.Average(x => x.Exercises);

        var efficiency = Math.Clamp(1d - Math.Max(0d, exercises - 4d) / 12d, 0d, 1d);
        var composite =
            Math.Clamp(confidenceReduction, -1d, 1d) * .35d +
            Math.Clamp(masteryGain, -1d, 1d) * .30d +
            transfer * .25d +
            efficiency * .10d;

        return new InterventionEffectiveness(
            data.Select(x => x.UserId).Distinct().Count(),
            confidenceReduction,
            masteryGain,
            transfer,
            exercises,
            Math.Clamp(composite, -1d, 1d));
    }
}
