using ProbMind.Domain.Psychometrics;

namespace ProbMind.UnitTests.Psychometrics;

public sealed class MasteryForecastEngineTests
{
    private readonly MasteryForecastEngine _engine = new();
    private readonly DateTimeOffset _start = new(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void IncreasingHistory_ProducesImprovingForecast()
    {
        var data = Enumerable.Range(0, 10).Select(i => new MasterySnapshot(_start.AddDays(i), .3d + i * .04d, i + 1));
        var result = _engine.Forecast(data, _start.AddDays(10));
        Assert.Equal("improving", result.Direction);
        Assert.True(result.Forecast7Days >= result.Current);
    }

    [Fact]
    public void DecreasingHistory_ProducesDecliningForecast()
    {
        var data = Enumerable.Range(0, 10).Select(i => new MasterySnapshot(_start.AddDays(i), .8d - i * .04d, i + 1));
        var result = _engine.Forecast(data, _start.AddDays(10));
        Assert.Equal("declining", result.Direction);
        Assert.True(result.Forecast7Days <= result.Current);
    }

    [Fact]
    public void Forecast_IsBounded()
    {
        var data = Enumerable.Range(0, 10).Select(i => new MasterySnapshot(_start.AddDays(i), .9d + i * .01d, i + 1));
        var result = _engine.Forecast(data, _start.AddDays(10));
        Assert.InRange(result.Forecast30Days, 0d, 1d);
    }

    [Fact]
    public void SinglePoint_HasLowConfidenceStableForecast()
    {
        var result = _engine.Forecast(new[] { new MasterySnapshot(_start, .5d, 1) }, _start);
        Assert.Equal("stable", result.Direction);
        Assert.True(result.Confidence <= .2d);
    }
}
