namespace ProbMind.Domain.Psychometrics;

public sealed record MasterySnapshot(DateTimeOffset At, double Mastery, int ObservationCount);
public sealed record MasteryForecast(double Current, double Forecast7Days, double Forecast30Days, double DailyTrend, double Confidence, string Direction);

public sealed class MasteryForecastEngine
{
    public MasteryForecast Forecast(IEnumerable<MasterySnapshot> snapshots, DateTimeOffset now)
    {
        var data = snapshots.OrderBy(x => x.At).ToArray();
        if (data.Length == 0)
            return new MasteryForecast(0d, 0d, 0d, 0d, 0d, "unknown");

        var current = Math.Clamp(data[^1].Mastery, 0d, 1d);
        if (data.Length == 1)
            return new MasteryForecast(current, current, current, 0d, .20d, "stable");

        var origin = data[0].At;
        var xs = data.Select(x => Math.Max(0d, (x.At - origin).TotalDays)).ToArray();
        var ys = data.Select(x => Math.Clamp(x.Mastery, 0d, 1d)).ToArray();
        var xMean = xs.Average();
        var yMean = ys.Average();
        var covariance = xs.Zip(ys).Sum(pair => (pair.First - xMean) * (pair.Second - yMean));
        var variance = xs.Sum(x => Math.Pow(x - xMean, 2d));
        var trend = variance <= 1e-9d ? 0d : covariance / variance;
        trend = Math.Clamp(trend, -.04d, .04d);

        var residual = ys.Zip(xs).Select(pair =>
        {
            var fitted = yMean + trend * (pair.Second - xMean);
            return Math.Pow(pair.First - fitted, 2d);
        }).Average();
        var confidence = Math.Clamp(1d - Math.Sqrt(residual) * 2d, .1d, .95d);
        confidence *= Math.Clamp(data.Length / 8d, .25d, 1d);

        var direction = trend switch
        {
            > .008d => "improving",
            < -.008d => "declining",
            _ => "stable"
        };

        return new MasteryForecast(
            current,
            Math.Clamp(current + trend * 7d, 0d, 1d),
            Math.Clamp(current + trend * 30d, 0d, 1d),
            trend,
            confidence,
            direction);
    }
}
