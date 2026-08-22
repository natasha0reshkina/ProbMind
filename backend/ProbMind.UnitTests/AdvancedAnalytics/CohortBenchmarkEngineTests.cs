using ProbMind.Domain.Analytics;

namespace ProbMind.UnitTests.AdvancedAnalytics;

public sealed class CohortBenchmarkEngineTests
{
    [Fact]
    public void HighestMasteryLearner_IsTopBand()
    {
        var ids = Enumerable.Range(0, 20).Select(_ => Guid.NewGuid()).ToArray();
        var data = ids.Select((id, index) => new BenchmarkObservation(id, index / 20d, 0, .5d, 0)).ToArray();
        var result = new CohortBenchmarkEngine().Compare(ids[^1], data);
        Assert.Equal("top_10", result.Band);
        Assert.True(result.Percentile > .9d);
    }

    [Fact]
    public void LowestMasteryLearner_NeedsSupport()
    {
        var ids = Enumerable.Range(0, 20).Select(_ => Guid.NewGuid()).ToArray();
        var data = ids.Select((id, index) => new BenchmarkObservation(id, index / 20d, 0, .5d, 0)).ToArray();
        Assert.Equal("support_needed", new CohortBenchmarkEngine().Compare(ids[0], data).Band);
    }

    [Fact]
    public void MissingUser_ReturnsUnknown()
    {
        var result = new CohortBenchmarkEngine().Compare(Guid.NewGuid(), Array.Empty<BenchmarkObservation>());
        Assert.Equal("unknown", result.Band);
    }
}
