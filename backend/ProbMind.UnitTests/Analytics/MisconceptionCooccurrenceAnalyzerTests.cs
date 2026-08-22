using ProbMind.Domain.Analytics;

namespace ProbMind.UnitTests.Analytics;

public sealed class MisconceptionCooccurrenceAnalyzerTests
{
    private readonly MisconceptionCooccurrenceAnalyzer _sut = new();

    [Fact]
    public void CooccurringPairCreatesEdge()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var learners = new[]
        {
            Set(a, b), Set(a, b), Set(a), Set(b)
        };
        var result = _sut.Analyze(learners);
        var edge = Assert.Single(result);
        Assert.Equal(2, edge.Together);
        Assert.True(edge.Jaccard > 0);
    }

    [Fact]
    public void NeverCooccurringPairHasNoEdge()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var result = _sut.Analyze([Set(a), Set(b)]);
        Assert.Empty(result);
    }

    [Fact]
    public void PerfectCooccurrenceHasJaccardOne()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var result = _sut.Analyze([Set(a,b), Set(a,b), Set(a,b)]);
        Assert.Equal(1d, result.Single().Jaccard);
    }

    private static LearnerMisconceptionSet Set(params Guid[] ids) =>
        new(Guid.NewGuid(), ids.ToHashSet());
}
