namespace ProbMind.Domain.Analytics;

public sealed record DistractorSelection(Guid OptionId, bool IsCorrect, Guid UserId, double LearnerMastery);
public sealed record DistractorMetric(Guid OptionId, int Selections, double Share, double MeanMastery, bool IsFunctioning);
public sealed record DistractorAnalysis(IReadOnlyList<DistractorMetric> Options, double Entropy, int TotalResponses);

public sealed class DistractorAnalysisEngine
{
    public DistractorAnalysis Analyze(IEnumerable<DistractorSelection> selections)
    {
        var data = selections.ToArray();
        if (data.Length == 0)
            return new DistractorAnalysis(Array.Empty<DistractorMetric>(), 0d, 0);

        var metrics = data
            .GroupBy(x => x.OptionId)
            .Select(group =>
            {
                var values = group.ToArray();
                var share = values.Length / (double)data.Length;
                return new DistractorMetric(
                    group.Key,
                    values.Length,
                    share,
                    values.Average(x => x.LearnerMastery),
                    share >= .05d);
            })
            .OrderByDescending(x => x.Share)
            .ToArray();

        var entropy = -metrics
            .Where(x => x.Share > 0d)
            .Sum(x => x.Share * Math.Log(x.Share));

        var maxEntropy = metrics.Length <= 1 ? 1d : Math.Log(metrics.Length);
        var normalized = Math.Clamp(entropy / maxEntropy, 0d, 1d);

        return new DistractorAnalysis(metrics, normalized, data.Length);
    }
}
