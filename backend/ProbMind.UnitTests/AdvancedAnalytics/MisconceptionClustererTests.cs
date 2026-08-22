using ProbMind.Domain.Analytics;

namespace ProbMind.UnitTests.AdvancedAnalytics;

public sealed class MisconceptionClustererTests
{
    [Fact]
    public void DistinctLowAndHighGroups_AreSeparated()
    {
        var low = Enumerable.Range(0, 10).Select(_ => new MisconceptionVector(Guid.NewGuid(), new[] { .1d, .15d, .2d }));
        var high = Enumerable.Range(0, 10).Select(_ => new MisconceptionVector(Guid.NewGuid(), new[] { .8d, .85d, .9d }));
        var result = new MisconceptionClusterer().Cluster(low.Concat(high).ToArray(), 2);
        Assert.Equal(2, result.Clusters.Count);
        Assert.Equal(20, result.Assignments.Count);
        Assert.NotEqual(result.Assignments.First().Cluster, result.Assignments.Last().Cluster);
    }

    [Fact]
    public void EmptyInput_ReturnsNoClusters()
    {
        var result = new MisconceptionClusterer().Cluster(Array.Empty<MisconceptionVector>());
        Assert.Empty(result.Clusters);
        Assert.Empty(result.Assignments);
    }

    [Fact]
    public void MoreClustersThanLearners_IsClamped()
    {
        var data = new[]
        {
            new MisconceptionVector(Guid.NewGuid(), new[] { .1d, .2d }),
            new MisconceptionVector(Guid.NewGuid(), new[] { .8d, .9d })
        };
        Assert.Equal(2, new MisconceptionClusterer().Cluster(data, 10).Clusters.Count);
    }
}
