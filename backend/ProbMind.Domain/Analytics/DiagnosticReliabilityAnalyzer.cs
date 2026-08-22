namespace ProbMind.Domain.Analytics;

public sealed record LearnerItemVector(Guid UserId, IReadOnlyList<int> BinaryScores);
public sealed record ReliabilityResult(double CronbachAlpha, int Learners, int Items, string Interpretation);

public sealed class DiagnosticReliabilityAnalyzer
{
    public ReliabilityResult CronbachAlpha(IReadOnlyCollection<LearnerItemVector> vectors)
    {
        var valid = vectors.Where(x => x.BinaryScores.Count > 1).ToArray();
        if (valid.Length < 2)
            return new ReliabilityResult(0d, valid.Length, valid.FirstOrDefault()?.BinaryScores.Count ?? 0, "insufficient_data");

        var itemCount = valid.Min(x => x.BinaryScores.Count);
        if (itemCount < 2)
            return new ReliabilityResult(0d, valid.Length, itemCount, "insufficient_items");

        var itemVariances = new double[itemCount];
        for (var item = 0; item < itemCount; item++)
        {
            var scores = valid.Select(x => (double)x.BinaryScores[item]).ToArray();
            itemVariances[item] = Variance(scores);
        }

        var totals = valid.Select(x => x.BinaryScores.Take(itemCount).Sum()).Select(x => (double)x).ToArray();
        var totalVariance = Variance(totals);

        if (totalVariance < 1e-9)
            return new ReliabilityResult(0d, valid.Length, itemCount, "zero_total_variance");

        var alpha = itemCount / (double)(itemCount - 1) *
                    (1d - itemVariances.Sum() / totalVariance);
        alpha = Math.Clamp(alpha, -1d, 1d);

        var interpretation = alpha switch
        {
            >= .90d => "excellent",
            >= .80d => "good",
            >= .70d => "acceptable",
            >= .60d => "questionable",
            _ => "poor"
        };

        return new ReliabilityResult(alpha, valid.Length, itemCount, interpretation);
    }

    private static double Variance(IReadOnlyCollection<double> values)
    {
        if (values.Count <= 1) return 0d;
        var mean = values.Average();
        return values.Sum(x => Math.Pow(x - mean, 2)) / (values.Count - 1);
    }
}
