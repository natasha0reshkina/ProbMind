using ProbMind.Domain.Enums;

namespace ProbMind.Domain.Learning;

public sealed class MasteryEngine
{
    private static double DifficultyWeight(QuestionDifficulty difficulty) =>
        difficulty switch
        {
            QuestionDifficulty.Introductory => 0.75d,
            QuestionDifficulty.Basic => 0.90d,
            QuestionDifficulty.Intermediate => 1.00d,
            QuestionDifficulty.Advanced => 1.20d,
            QuestionDifficulty.Transfer => 1.35d,
            _ => 1d
        };

    public MasteryResult Calculate(
        IReadOnlyCollection<MasteryObservation> observations,
        DateTimeOffset now)
    {
        if (observations.Count == 0)
            return new MasteryResult(0.50d, 1d, 0, 0d, 0d, "No observations.");

        var alpha = 2d;
        var beta = 2d;
        var effectiveCorrect = 0d;
        var effectiveTotal = 0d;

        foreach (var observation in observations)
        {
            var ageDays = Math.Max(0d, (now - observation.ObservedAt).TotalDays);
            var recency = Math.Pow(0.5d, ageDays / 60d);
            var weight =
                DifficultyWeight(observation.Difficulty) *
                observation.DiagnosticRelevance *
                recency *
                (observation.IsTransfer ? 1.15d : 1d);

            effectiveTotal += weight;

            if (observation.IsCorrect)
            {
                alpha += weight;
                effectiveCorrect += weight;
            }
            else
            {
                beta += weight;
            }
        }

        var mastery = alpha / (alpha + beta);
        var uncertainty = Math.Clamp(
            1d / Math.Sqrt(effectiveTotal + 1d),
            0.05d,
            1d);

        return new MasteryResult(
            Math.Clamp(mastery, 0d, 1d),
            uncertainty,
            observations.Count,
            effectiveCorrect,
            effectiveTotal,
            $"beta-posterior alpha={alpha:F2}, beta={beta:F2}, effectiveN={effectiveTotal:F2}");
    }
}
