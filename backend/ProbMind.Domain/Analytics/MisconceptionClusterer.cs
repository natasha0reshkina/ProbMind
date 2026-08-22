namespace ProbMind.Domain.Analytics;

public sealed record MisconceptionVector(Guid UserId, IReadOnlyList<double> Confidences);
public sealed record LearnerCluster(Guid UserId, int Cluster, double Distance, string Label);
public sealed record ClusterSummary(int Cluster, int Size, IReadOnlyList<double> Centroid, string Label);
public sealed record ClusteringResult(IReadOnlyList<LearnerCluster> Assignments, IReadOnlyList<ClusterSummary> Clusters, int Iterations);

public sealed class MisconceptionClusterer
{
    public ClusteringResult Cluster(IReadOnlyCollection<MisconceptionVector> vectors, int k = 3, int maxIterations = 25)
    {
        var data = vectors.Where(x => x.Confidences.Count > 0).ToArray();
        if (data.Length == 0)
            return new ClusteringResult(Array.Empty<LearnerCluster>(), Array.Empty<ClusterSummary>(), 0);

        var dimensions = data.Min(x => x.Confidences.Count);
        k = Math.Clamp(k, 1, Math.Min(data.Length, 8));
        var normalized = data.Select(x => new MisconceptionVector(
            x.UserId,
            x.Confidences.Take(dimensions).Select(v => Math.Clamp(v, 0d, 1d)).ToArray())).ToArray();

        var centroids = Initialize(normalized, k, dimensions);
        var assignments = new int[normalized.Length];
        Array.Fill(assignments, -1);
        var iterations = 0;

        for (var iteration = 1; iteration <= maxIterations; iteration++)
        {
            iterations = iteration;
            var changed = false;
            for (var i = 0; i < normalized.Length; i++)
            {
                var best = Enumerable.Range(0, k)
                    .Select(cluster => new { Cluster = cluster, Distance = Distance(normalized[i].Confidences, centroids[cluster]) })
                    .OrderBy(x => x.Distance)
                    .First();
                if (assignments[i] != best.Cluster)
                {
                    assignments[i] = best.Cluster;
                    changed = true;
                }
            }

            var next = new double[k][];
            for (var cluster = 0; cluster < k; cluster++)
            {
                var members = normalized
                    .Where((_, index) => assignments[index] == cluster)
                    .ToArray();
                next[cluster] = members.Length == 0
                    ? centroids[cluster]
                    : Enumerable.Range(0, dimensions).Select(d => members.Average(x => x.Confidences[d])).ToArray();
            }
            centroids = next;
            if (!changed) break;
        }

        var learnerAssignments = normalized.Select((vector, index) =>
        {
            var cluster = assignments[index];
            var distance = Distance(vector.Confidences, centroids[cluster]);
            return new LearnerCluster(vector.UserId, cluster, distance, Label(centroids[cluster]));
        }).ToArray();

        var summaries = Enumerable.Range(0, k).Select(cluster => new ClusterSummary(
            cluster,
            assignments.Count(x => x == cluster),
            centroids[cluster],
            Label(centroids[cluster]))).ToArray();

        return new ClusteringResult(learnerAssignments, summaries, iterations);
    }

    private static double[][] Initialize(IReadOnlyList<MisconceptionVector> data, int k, int dimensions)
    {
        var ordered = data.OrderBy(x => x.Confidences.Average()).ToArray();
        var result = new double[k][];
        for (var i = 0; i < k; i++)
        {
            var index = k == 1 ? ordered.Length / 2 : (int)Math.Round(i * (ordered.Length - 1d) / (k - 1d));
            result[i] = ordered[index].Confidences.Take(dimensions).ToArray();
        }
        return result;
    }

    private static double Distance(IReadOnlyList<double> left, IReadOnlyList<double> right)
    {
        var count = Math.Min(left.Count, right.Count);
        if (count == 0) return 0d;
        return Math.Sqrt(Enumerable.Range(0, count).Average(i => Math.Pow(left[i] - right[i], 2d)));
    }

    private static string Label(IReadOnlyCollection<double> centroid)
    {
        if (centroid.Count == 0) return "empty";
        var mean = centroid.Average();
        var severe = centroid.Count(x => x >= .70d);
        return mean switch
        {
            >= .65d => "systemic_misconceptions",
            >= .42d when severe >= 2 => "several_persistent_patterns",
            >= .30d => "targeted_support",
            _ => "low_misconception_burden"
        };
    }
}
