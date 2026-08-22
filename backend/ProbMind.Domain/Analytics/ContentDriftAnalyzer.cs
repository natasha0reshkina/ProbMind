namespace ProbMind.Domain.Analytics;

public sealed record ContentWindowMetric(DateTimeOffset WindowStart, int Responses, double CorrectRate, double MedianResponseSeconds, double DistractorEntropy);
public sealed record ContentDriftResult(double AccuracyShift, double TimeShift, double EntropyShift, double DriftScore, bool RequiresReview, string Reason);

public sealed class ContentDriftAnalyzer
{
    public ContentDriftResult Analyze(
        IReadOnlyCollection<ContentWindowMetric> baseline,
        IReadOnlyCollection<ContentWindowMetric> recent)
    {
        if (baseline.Count == 0 || recent.Count == 0)
            return new ContentDriftResult(0d, 0d, 0d, 0d, false, "insufficient_data");

        var oldAccuracy = WeightedAverage(baseline, x => x.CorrectRate);
        var newAccuracy = WeightedAverage(recent, x => x.CorrectRate);
        var oldTime = WeightedAverage(baseline, x => x.MedianResponseSeconds);
        var newTime = WeightedAverage(recent, x => x.MedianResponseSeconds);
        var oldEntropy = WeightedAverage(baseline, x => x.DistractorEntropy);
        var newEntropy = WeightedAverage(recent, x => x.DistractorEntropy);

        var accuracyShift = newAccuracy - oldAccuracy;
        var timeShift = oldTime <= 1e-9d ? 0d : (newTime - oldTime) / oldTime;
        var entropyShift = newEntropy - oldEntropy;
        var drift = Math.Clamp(
            Math.Abs(accuracyShift) * .50d +
            Math.Min(1d, Math.Abs(timeShift)) * .28d +
            Math.Abs(entropyShift) * .22d,
            0d,
            1d);
        var requiresReview = drift >= .18d || Math.Abs(accuracyShift) >= .20d || Math.Abs(timeShift) >= .45d;
        var reason = requiresReview
            ? $"material_shift accuracy={accuracyShift:+0.00;-0.00}; time={timeShift:+0.00;-0.00}; entropy={entropyShift:+0.00;-0.00}"
            : "stable";
        return new ContentDriftResult(accuracyShift, timeShift, entropyShift, drift, requiresReview, reason);
    }

    private static double WeightedAverage(
        IEnumerable<ContentWindowMetric> windows,
        Func<ContentWindowMetric, double> selector)
    {
        var data = windows.ToArray();
        var total = data.Sum(x => Math.Max(0, x.Responses));
        if (total <= 0) return data.Average(selector);
        return data.Sum(x => selector(x) * Math.Max(0, x.Responses)) / total;
    }
}
