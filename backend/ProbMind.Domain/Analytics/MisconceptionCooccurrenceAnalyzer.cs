namespace ProbMind.Domain.Analytics;

public sealed record LearnerMisconceptionSet(Guid UserId, IReadOnlySet<Guid> ActiveMisconceptions);
public sealed record CooccurrenceEdge(Guid A, Guid B, int Together, double Jaccard, double Lift);

public sealed class MisconceptionCooccurrenceAnalyzer
{
    public IReadOnlyList<CooccurrenceEdge> Analyze(
        IReadOnlyCollection<LearnerMisconceptionSet> learners)
    {
        var all = learners
            .SelectMany(x => x.ActiveMisconceptions)
            .Distinct()
            .OrderBy(x => x)
            .ToArray();

        var result = new List<CooccurrenceEdge>();
        for (var i = 0; i < all.Length; i++)
        {
            for (var j = i + 1; j < all.Length; j++)
            {
                var a = all[i];
                var b = all[j];
                var countA = learners.Count(x => x.ActiveMisconceptions.Contains(a));
                var countB = learners.Count(x => x.ActiveMisconceptions.Contains(b));
                var together = learners.Count(x =>
                    x.ActiveMisconceptions.Contains(a) &&
                    x.ActiveMisconceptions.Contains(b));

                if (together == 0)
                    continue;

                var union = countA + countB - together;
                var jaccard = union == 0 ? 0d : together / (double)union;
                var expected = learners.Count == 0
                    ? 0d
                    : countA / (double)learners.Count * countB / (double)learners.Count;
                var observed = learners.Count == 0 ? 0d : together / (double)learners.Count;
                var lift = expected <= 1e-9 ? 0d : observed / expected;

                result.Add(new CooccurrenceEdge(a, b, together, jaccard, lift));
            }
        }

        return result
            .OrderByDescending(x => x.Lift)
            .ThenByDescending(x => x.Together)
            .ToArray();
    }
}
