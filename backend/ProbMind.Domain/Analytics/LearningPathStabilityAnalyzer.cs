namespace ProbMind.Domain.Analytics;

public sealed record PathRevisionSnapshot(int Revision, DateTimeOffset At, IReadOnlyList<Guid> OrderedStepKeys);
public sealed record PathStabilityResult(double Stability, int Revisions, double MeanRetention, string Interpretation);

public sealed class LearningPathStabilityAnalyzer
{
    public PathStabilityResult Analyze(IReadOnlyCollection<PathRevisionSnapshot> revisions)
    {
        var ordered = revisions.OrderBy(x => x.Revision).ToArray();
        if (ordered.Length < 2)
            return new PathStabilityResult(1d, ordered.Length, 1d, "insufficient_history");

        var retentions = new List<double>();
        for (var i = 1; i < ordered.Length; i++)
        {
            var previous = ordered[i - 1].OrderedStepKeys;
            var current = ordered[i].OrderedStepKeys;
            var currentList = current.ToList();
            if (previous.Count == 0)
            {
                retentions.Add(current.Count == 0 ? 1d : 0d);
                continue;
            }

            var retained = previous.Count(x => current.Contains(x));
            var setRetention = retained / (double)previous.Count;

            var common = previous.Where(current.Contains).ToArray();
            var orderMatches = 0;
            for (var j = 1; j < common.Length; j++)
            {
                if (currentList.IndexOf(common[j - 1]) < currentList.IndexOf(common[j]))
                    orderMatches++;
            }

            var orderRetention = common.Length <= 1
                ? 1d
                : orderMatches / (double)(common.Length - 1);

            retentions.Add(setRetention * .7d + orderRetention * .3d);
        }

        var mean = retentions.Average();
        var interpretation = mean switch
        {
            >= .85d => "stable",
            >= .65d => "moderately_stable",
            >= .45d => "adaptive",
            _ => "highly_dynamic"
        };

        return new PathStabilityResult(mean, ordered.Length, mean, interpretation);
    }
}
