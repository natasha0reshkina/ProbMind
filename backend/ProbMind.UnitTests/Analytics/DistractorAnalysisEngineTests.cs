using ProbMind.Domain.Analytics;

namespace ProbMind.UnitTests.Analytics;

public sealed class DistractorAnalysisEngineTests
{
    private readonly DistractorAnalysisEngine _sut = new();

    [Fact]
    public void EmptySelections_ReturnEmptyAnalysis()
    {
        var result = _sut.Analyze(Array.Empty<DistractorSelection>());
        Assert.Empty(result.Options);
        Assert.Equal(0, result.TotalResponses);
    }

    [Fact]
    public void BalancedOptionsHaveHighEntropy()
    {
        var options = Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToArray();
        var data = Enumerable.Range(0, 400)
            .Select(i => new DistractorSelection(options[i % 4], i % 4 == 0, Guid.NewGuid(), .5))
            .ToArray();
        var result = _sut.Analyze(data);
        Assert.True(result.Entropy > .95);
    }

    [Fact]
    public void DominantOptionHasLowEntropy()
    {
        var dominant = Guid.NewGuid();
        var rare = Guid.NewGuid();
        var data = Enumerable.Range(0, 100)
            .Select(i => new DistractorSelection(i < 95 ? dominant : rare, false, Guid.NewGuid(), .5))
            .ToArray();
        var result = _sut.Analyze(data);
        Assert.True(result.Entropy < .4);
    }

    [Fact]
    public void FivePercentOptionCountsAsFunctioning()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var data = Enumerable.Range(0, 100)
            .Select(i => new DistractorSelection(i < 5 ? a : b, false, Guid.NewGuid(), .5))
            .ToArray();
        var result = _sut.Analyze(data);
        Assert.True(result.Options.Single(x => x.OptionId == a).IsFunctioning);
    }
}
