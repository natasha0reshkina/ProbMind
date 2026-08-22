using ProbMind.Domain.Diagnostics;

namespace ProbMind.UnitTests.Domain;

public sealed class ConfidenceCalibrationServiceTests
{
    private readonly ConfidenceCalibrationService _sut = new();

    [Fact]
    public void EmptyInput_ReturnsEmptyCalibration()
    {
        var result = _sut.Calibrate(Array.Empty<(double, bool)>());
        Assert.Empty(result.Buckets);
        Assert.Equal(0d, result.BrierScore);
    }

    [Fact]
    public void PerfectPredictions_HaveLowBrierScore()
    {
        var observations = new[]
        {
            (.99, true), (.95, true), (.97, true),
            (.01, false), (.05, false), (.03, false)
        };
        var result = _sut.Calibrate(observations);
        Assert.True(result.BrierScore < .01);
    }

    [Fact]
    public void WrongConfidentPredictions_HaveHighBrierScore()
    {
        var observations = new[]
        {
            (.99, false), (.95, false), (.01, true), (.05, true)
        };
        var result = _sut.Calibrate(observations);
        Assert.True(result.BrierScore > .80);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(20)]
    public void BucketCount_IsRespected(int buckets)
    {
        var observations = Enumerable.Range(0, 100)
            .Select(i => (i / 100d, i % 2 == 0))
            .ToArray();
        var result = _sut.Calibrate(observations, buckets);
        Assert.True(result.Buckets.Count <= buckets);
    }
}
