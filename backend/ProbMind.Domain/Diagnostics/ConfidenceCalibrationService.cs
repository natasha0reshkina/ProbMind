namespace ProbMind.Domain.Diagnostics;

public sealed record CalibrationBucket(
    double From,
    double To,
    int Predictions,
    int Confirmed,
    double ObservedRate);

public sealed record CalibrationResult(
    IReadOnlyList<CalibrationBucket> Buckets,
    double BrierScore,
    double MeanAbsoluteCalibrationError);

public sealed class ConfidenceCalibrationService
{
    public CalibrationResult Calibrate(
        IReadOnlyCollection<(double Predicted, bool Confirmed)> observations,
        int bucketCount = 10)
    {
        if (observations.Count == 0)
            return new CalibrationResult(Array.Empty<CalibrationBucket>(), 0d, 0d);

        bucketCount = Math.Clamp(bucketCount, 2, 20);
        var width = 1d / bucketCount;
        var buckets = new List<CalibrationBucket>();
        var weightedError = 0d;

        for (var index = 0; index < bucketCount; index++)
        {
            var from = index * width;
            var to = index == bucketCount - 1 ? 1.000001d : (index + 1) * width;
            var values = observations
                .Where(x => x.Predicted >= from && x.Predicted < to)
                .ToArray();

            if (values.Length == 0)
                continue;

            var confirmed = values.Count(x => x.Confirmed);
            var observedRate = confirmed / (double)values.Length;
            var predictedMean = values.Average(x => x.Predicted);
            weightedError += Math.Abs(predictedMean - observedRate) * values.Length;

            buckets.Add(new CalibrationBucket(
                from,
                Math.Min(1d, to),
                values.Length,
                confirmed,
                observedRate));
        }

        var brier = observations.Average(x =>
        {
            var outcome = x.Confirmed ? 1d : 0d;
            var difference = Math.Clamp(x.Predicted, 0d, 1d) - outcome;
            return difference * difference;
        });

        return new CalibrationResult(
            buckets,
            brier,
            weightedError / observations.Count);
    }
}
