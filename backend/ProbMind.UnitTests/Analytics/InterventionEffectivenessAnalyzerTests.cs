using ProbMind.Domain.Analytics;

namespace ProbMind.UnitTests.Analytics;

public sealed class InterventionEffectivenessAnalyzerTests
{
    private readonly InterventionEffectivenessAnalyzer _sut = new();

    [Fact]
    public void EmptyInput_ReturnsZeros()
    {
        var result = _sut.Analyze(Array.Empty<InterventionObservation>());
        Assert.Equal(0, result.Learners);
        Assert.Equal(0d, result.CompositeEffectiveness);
    }

    [Fact]
    public void SuccessfulIntervention_HasPositiveEffectiveness()
    {
        var mc = Guid.NewGuid();
        var data = Enumerable.Range(0, 10).Select(i =>
            new InterventionObservation(
                Guid.NewGuid(), mc, .80, .25, .45, .72, 5, true)).ToArray();
        var result = _sut.Analyze(data);
        Assert.True(result.CompositeEffectiveness > .4);
        Assert.True(result.MeanConfidenceReduction > 0);
        Assert.True(result.MeanMasteryGain > 0);
        Assert.Equal(1d, result.TransferPassRate);
    }

    [Fact]
    public void FailedIntervention_HasLowerScore()
    {
        var mc = Guid.NewGuid();
        var successful = _sut.Analyze([
            new(Guid.NewGuid(), mc, .8, .2, .4, .75, 5, true)
        ]);
        var failed = _sut.Analyze([
            new(Guid.NewGuid(), mc, .8, .85, .4, .35, 9, false)
        ]);
        Assert.True(successful.CompositeEffectiveness > failed.CompositeEffectiveness);
    }

    [Fact]
    public void LearnerCountIsDistinct()
    {
        var user = Guid.NewGuid();
        var mc = Guid.NewGuid();
        var result = _sut.Analyze([
            new(user, mc, .8, .4, .4, .6, 4, true),
            new(user, mc, .6, .2, .6, .8, 4, true)
        ]);
        Assert.Equal(1, result.Learners);
    }
}
