using ProbMind.Domain.Analytics;

namespace ProbMind.UnitTests.Analytics;

public sealed class ItemDifficultyEstimatorTests
{
    private readonly ItemDifficultyEstimator _sut = new();

    [Fact]
    public void EmptyInput_ReturnsNoData()
    {
        var result = _sut.Estimate(Array.Empty<ItemResponse>());
        Assert.Equal(0, result.Responses);
        Assert.Equal("no_data", result.QualityBand);
    }

    [Fact]
    public void CorrectRateMapsToDifficulty()
    {
        var data = Enumerable.Range(0, 100)
            .Select(i => new ItemResponse(Guid.NewGuid(), i < 70, i / 100d))
            .ToArray();
        var result = _sut.Estimate(data);
        Assert.Equal(.70, result.PValue, 2);
        Assert.Equal(.30, result.Difficulty, 2);
    }

    [Fact]
    public void DiscriminatingItemHasPositiveDiscrimination()
    {
        var data = Enumerable.Range(0, 100)
            .Select(i => new ItemResponse(Guid.NewGuid(), i >= 50, i / 100d))
            .ToArray();
        var result = _sut.Estimate(data);
        Assert.True(result.Discrimination > .5);
        Assert.Contains(result.QualityBand, new[] { "excellent", "good" });
    }

    [Fact]
    public void ReversedItemHasNegativeDiscrimination()
    {
        var data = Enumerable.Range(0, 100)
            .Select(i => new ItemResponse(Guid.NewGuid(), i < 40, i / 100d))
            .ToArray();
        var result = _sut.Estimate(data);
        Assert.True(result.Discrimination < 0);
        Assert.Equal("review", result.QualityBand);
    }
}
