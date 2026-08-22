namespace ProbMind.Domain.Analytics;

public sealed record BenchmarkObservation(Guid UserId, double Mastery, int ActiveMisconceptions, double Accuracy, int PracticeCount);
public sealed record CohortBenchmark(double Percentile, double MasteryZScore, string Band, double CohortMean, double CohortMedian, int CohortSize);

public sealed class CohortBenchmarkEngine
{
    public CohortBenchmark Compare(Guid userId, IEnumerable<BenchmarkObservation> observations)
    {
        var data = observations.ToArray();
        var target = data.FirstOrDefault(x => x.UserId == userId);
        if (target is null || data.Length == 0)
            return new CohortBenchmark(0d, 0d, "unknown", 0d, 0d, data.Length);

        var mastery = data.Select(x => x.Mastery).OrderBy(x => x).ToArray();
        var mean = mastery.Average();
        var median = Quantile(mastery, .5d);
        var sd = Math.Sqrt(mastery.Average(x => Math.Pow(x - mean, 2d)));
        var z = sd <= 1e-9d ? 0d : (target.Mastery - mean) / sd;
        var belowOrEqual = mastery.Count(x => x <= target.Mastery);
        var percentile = (belowOrEqual - .5d) / mastery.Length;
        percentile = Math.Clamp(percentile, 0d, 1d);

        var band = percentile switch
        {
            >= .90d => "top_10",
            >= .75d => "upper_quartile",
            >= .40d => "middle",
            >= .25d => "lower_middle",
            _ => "support_needed"
        };
        return new CohortBenchmark(percentile, z, band, mean, median, data.Length);
    }

    private static double Quantile(IReadOnlyList<double> sorted, double q)
    {
        if (sorted.Count == 0) return 0d;
        if (sorted.Count == 1) return sorted[0];
        var pos = (sorted.Count - 1) * q;
        var lo = (int)Math.Floor(pos);
        var hi = (int)Math.Ceiling(pos);
        var weight = pos - lo;
        return sorted[lo] * (1d - weight) + sorted[hi] * weight;
    }
}
