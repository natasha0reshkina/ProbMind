namespace ProbMind.Domain.Learning;

public sealed record CoverageTarget(Guid TopicId, int MinimumQuestions, double Weight);

public sealed class DiagnosticCoveragePlanner
{
    public IReadOnlyDictionary<Guid, int> Allocate(
        IReadOnlyCollection<CoverageTarget> targets,
        int totalQuestions)
    {
        if (targets.Count == 0)
            return new Dictionary<Guid, int>();

        totalQuestions = Math.Max(totalQuestions, targets.Sum(x => x.MinimumQuestions));

        var allocation = targets.ToDictionary(x => x.TopicId, x => x.MinimumQuestions);
        var remaining = totalQuestions - allocation.Values.Sum();
        var totalWeight = targets.Sum(x => Math.Max(0.01d, x.Weight));

        while (remaining > 0)
        {
            var best = targets
                .Select(t => new
                {
                    Target = t,
                    Desire = t.Weight / totalWeight * totalQuestions,
                    Current = allocation[t.TopicId]
                })
                .OrderByDescending(x => x.Desire - x.Current)
                .First();

            allocation[best.Target.TopicId]++;
            remaining--;
        }

        return allocation;
    }
}
