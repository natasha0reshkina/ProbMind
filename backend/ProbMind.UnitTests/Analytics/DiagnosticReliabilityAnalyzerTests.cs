using ProbMind.Domain.Analytics;

namespace ProbMind.UnitTests.Analytics;

public sealed class DiagnosticReliabilityAnalyzerTests
{
    private readonly DiagnosticReliabilityAnalyzer _sut = new();

    [Fact]
    public void InsufficientLearners_ReturnsInsufficientData()
    {
        var result = _sut.CronbachAlpha([
            new LearnerItemVector(Guid.NewGuid(), new[] { 1, 0, 1 })
        ]);
        Assert.Equal("insufficient_data", result.Interpretation);
    }

    [Fact]
    public void ConsistentItemsProducePositiveReliability()
    {
        var vectors = Enumerable.Range(0, 100)
            .Select(i =>
            {
                var strong = i >= 50;
                return new LearnerItemVector(
                    Guid.NewGuid(),
                    new[] {
                        strong ? 1 : 0,
                        strong ? 1 : 0,
                        strong ? 1 : 0,
                        strong ? 1 : 0,
                        strong ? 1 : 0
                    });
            }).ToArray();

        var result = _sut.CronbachAlpha(vectors);
        Assert.True(result.CronbachAlpha > .9);
    }

    [Fact]
    public void AlphaIsBounded()
    {
        var random = new Random(1);
        var vectors = Enumerable.Range(0, 80)
            .Select(_ => new LearnerItemVector(
                Guid.NewGuid(),
                Enumerable.Range(0, 12).Select(_ => random.Next(2)).ToArray()))
            .ToArray();

        var result = _sut.CronbachAlpha(vectors);
        Assert.InRange(result.CronbachAlpha, -1d, 1d);
    }
}
