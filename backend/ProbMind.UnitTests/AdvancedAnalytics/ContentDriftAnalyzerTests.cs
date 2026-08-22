using ProbMind.Domain.Analytics;

namespace ProbMind.UnitTests.AdvancedAnalytics;

public sealed class ContentDriftAnalyzerTests
{
    private static ContentWindowMetric Metric(double accuracy, double time, double entropy, int responses = 100) =>
        new(DateTimeOffset.UtcNow, responses, accuracy, time, entropy);

    [Fact]
    public void StableMetrics_DoNotRequireReview()
    {
        var baseline = new[] { Metric(.65d, 20d, .8d) };
        var recent = new[] { Metric(.66d, 20.5d, .79d) };
        var result = new ContentDriftAnalyzer().Analyze(baseline, recent);
        Assert.False(result.RequiresReview);
    }

    [Fact]
    public void LargeAccuracyShift_RequiresReview()
    {
        var result = new ContentDriftAnalyzer().Analyze(
            new[] { Metric(.75d, 20d, .8d) },
            new[] { Metric(.45d, 20d, .8d) });
        Assert.True(result.RequiresReview);
        Assert.True(Math.Abs(result.AccuracyShift) >= .2d);
    }

    [Fact]
    public void LargeResponseTimeShift_RequiresReview()
    {
        var result = new ContentDriftAnalyzer().Analyze(
            new[] { Metric(.6d, 10d, .8d) },
            new[] { Metric(.6d, 20d, .8d) });
        Assert.True(result.RequiresReview);
    }

    [Fact]
    public void MissingWindow_IsInsufficient()
    {
        var result = new ContentDriftAnalyzer().Analyze(Array.Empty<ContentWindowMetric>(), new[] { Metric(.5d, 10d, .5d) });
        Assert.Equal("insufficient_data", result.Reason);
    }
}
