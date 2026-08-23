using ProbMind.Domain.Enums;

namespace ProbMind.Domain.Diagnostics;

public sealed class ConfidenceEngine
{
    private const double HalfLifeDays = 45d;

    private static readonly IReadOnlyDictionary<EvidenceKind, double> KindWeights =
        new Dictionary<EvidenceKind, double>
        {
            [EvidenceKind.Distractor] = 1.00,
            [EvidenceKind.CorrectAnswer] = 0.55,
            [EvidenceKind.CorrectionSuccess] = 0.85,
            [EvidenceKind.TransferSuccess] = 1.15,
            [EvidenceKind.TransferFailure] = 1.20,
            [EvidenceKind.ManualTeacherEvidence] = 1.30
        };

    public ConfidenceResult Calculate(
        IReadOnlyCollection<EvidenceObservation> observations,
        DateTimeOffset now)
    {
        if (observations.Count == 0)
            return Empty();

        var support = 0d;
        var contradiction = 0d;
        var weightedRecency = 0d;
        var transferAdjustment = 0d;

        foreach (var observation in observations)
        {
            var ageDays = Math.Max(0d, (now - observation.ObservedAt).TotalDays);
            var decay = Math.Pow(0.5d, ageDays / HalfLifeDays);
            var kindWeight = KindWeights[observation.Kind];
            var contribution = kindWeight *
                               observation.RawWeight *
                               observation.RelevanceWeight *
                               decay;

            weightedRecency += decay;

            if (contribution >= 0d)
                support += contribution;
            else
                contradiction += Math.Abs(contribution);

            if (observation.IsTransfer ||
                observation.Kind is EvidenceKind.TransferSuccess or EvidenceKind.TransferFailure)
            {
                transferAdjustment += contribution * 0.20d;
            }
        }

        var totalMagnitude = support + contradiction;
        var balance = totalMagnitude <= 0.0001
            ? 0d
            : (support - contradiction) / totalMagnitude;

        var distinctQuestions = observations
            .Where(x => x.QuestionId.HasValue)
            .Select(x => x.QuestionId!.Value)
            .Distinct()
            .Count();

        var diversity = Math.Clamp(distinctQuestions / 5d, 0d, 1d);
        var repetition = Math.Clamp(Math.Log2(observations.Count + 1d) / 3.5d, 0d, 1d);
        var recency = Math.Clamp(weightedRecency / observations.Count, 0d, 1d);

        var normalizedBalance = (balance + 1d) / 2d;
        var raw =
            normalizedBalance * 0.52d +
            repetition * 0.18d +
            diversity * 0.18d +
            recency * 0.12d +
            Math.Clamp(transferAdjustment, -0.25d, 0.25d);

        var confidence = Math.Clamp(raw, 0d, 1d);

        var explanation =
            $"support={support:F3}; contradiction={contradiction:F3}; " +
            $"distinct={distinctQuestions}; diversity={diversity:F2}; " +
            $"recency={recency:F2}; transfer={transferAdjustment:F3}";

        return new ConfidenceResult(
            confidence,
            support,
            contradiction,
            diversity,
            recency,
            transferAdjustment,
            observations.Count,
            distinctQuestions,
            explanation);
    }

    private static ConfidenceResult Empty() =>
        new(0d, 0d, 0d, 0d, 0d, 0d, 0, 0, "No diagnostic evidence.");
}
