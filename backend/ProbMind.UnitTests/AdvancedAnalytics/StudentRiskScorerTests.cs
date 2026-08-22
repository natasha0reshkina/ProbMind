using ProbMind.Domain.Analytics;

namespace ProbMind.UnitTests.AdvancedAnalytics;

public sealed class StudentRiskScorerTests
{
    [Fact]
    public void StrongActiveLearner_HasMinimalRisk()
    {
        var signal = new StudentRiskSignal(.9d, .02d, 0, 0, 1, 1d, .9d);
        var result = new StudentRiskScorer().Score(signal);
        Assert.True(result.Risk < .2d);
        Assert.Equal("minimal", result.Level);
    }

    [Fact]
    public void WeakInactiveLearner_HasHighRisk()
    {
        var signal = new StudentRiskSignal(.2d, -.08d, 7, 3, 45, .2d, .1d);
        var result = new StudentRiskScorer().Score(signal);
        Assert.True(result.Risk > .7d);
        Assert.Contains(result.Level, new[] { "high", "critical" });
        Assert.NotEmpty(result.Drivers);
    }

    [Fact]
    public void Risk_IsAlwaysBounded()
    {
        var signal = new StudentRiskSignal(-10d, -10d, 100, 100, 1000, -1d, -1d);
        Assert.InRange(new StudentRiskScorer().Score(signal).Risk, 0d, 1d);
    }
}
