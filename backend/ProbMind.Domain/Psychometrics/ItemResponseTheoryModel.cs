namespace ProbMind.Domain.Psychometrics;

public sealed record IrtItem(
    Guid ItemId,
    double Difficulty,
    double Discrimination = 1d,
    double Guessing = 0d);

public sealed record IrtResponse(IrtItem Item, bool Correct);
public sealed record AbilityEstimate(double Theta, double StandardError, int Iterations, bool Converged);

public sealed class ItemResponseTheoryModel
{
    public double Probability(double theta, IrtItem item)
    {
        var discrimination = Math.Clamp(item.Discrimination, .1d, 3d);
        var guessing = Math.Clamp(item.Guessing, 0d, .35d);
        var exponent = -discrimination * (theta - item.Difficulty);
        var logistic = exponent > 35d
            ? 0d
            : exponent < -35d
                ? 1d
                : 1d / (1d + Math.Exp(exponent));
        return guessing + (1d - guessing) * logistic;
    }

    public double Information(double theta, IrtItem item)
    {
        var p = Math.Clamp(Probability(theta, item), 1e-7d, 1d - 1e-7d);
        var q = 1d - p;
        var a = Math.Clamp(item.Discrimination, .1d, 3d);
        var c = Math.Clamp(item.Guessing, 0d, .35d);
        var denominator = Math.Max(1e-9d, 1d - c);
        var adjusted = Math.Pow((p - c) / denominator, 2d);
        return Math.Max(0d, a * a * q / p * adjusted);
    }

    public AbilityEstimate EstimateAbility(
        IReadOnlyCollection<IrtResponse> responses,
        double initialTheta = 0d,
        int maxIterations = 30)
    {
        if (responses.Count == 0)
            return new AbilityEstimate(initialTheta, 9.99d, 0, false);

        var theta = Math.Clamp(initialTheta, -4d, 4d);
        var converged = false;
        var iterations = 0;

        for (var iteration = 1; iteration <= maxIterations; iteration++)
        {
            iterations = iteration;
            var score = 0d;
            var information = 0d;

            foreach (var response in responses)
            {
                var p = Math.Clamp(Probability(theta, response.Item), 1e-6d, 1d - 1e-6d);
                var observed = response.Correct ? 1d : 0d;
                var a = Math.Clamp(response.Item.Discrimination, .1d, 3d);
                score += a * (observed - p);
                information += Math.Max(1e-6d, Information(theta, response.Item));
            }

            var step = Math.Clamp(score / information, -.75d, .75d);
            theta = Math.Clamp(theta + step, -4d, 4d);
            if (Math.Abs(step) < .001d)
            {
                converged = true;
                break;
            }
        }

        var finalInformation = responses.Sum(x => Information(theta, x.Item));
        var standardError = finalInformation <= 1e-9d
            ? 9.99d
            : 1d / Math.Sqrt(finalInformation);

        return new AbilityEstimate(theta, standardError, iterations, converged);
    }
}
