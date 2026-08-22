namespace ProbMind.Domain.Analytics;

public sealed record ItemResponse(Guid UserId, bool Correct, double LearnerMastery);
public sealed record ItemDifficultyResult(double PValue, double Difficulty, double Discrimination, int Responses, string QualityBand);

public sealed class ItemDifficultyEstimator
{
    public ItemDifficultyResult Estimate(IEnumerable<ItemResponse> responses)
    {
        var data = responses.ToArray();
        if (data.Length == 0)
            return new ItemDifficultyResult(0, .5, 0, 0, "no_data");

        var p = data.Count(x => x.Correct) / (double)data.Length;
        var difficulty = 1d - p;

        var ordered = data.OrderBy(x => x.LearnerMastery).ToArray();
        var groupSize = Math.Max(1, (int)Math.Floor(ordered.Length * .27d));
        var lower = ordered.Take(groupSize).ToArray();
        var upper = ordered.TakeLast(groupSize).ToArray();
        var upperP = upper.Count(x => x.Correct) / (double)upper.Length;
        var lowerP = lower.Count(x => x.Correct) / (double)lower.Length;
        var discrimination = upperP - lowerP;

        var quality = data.Length < 30
            ? "insufficient_sample"
            : discrimination switch
            {
                >= .35d => "excellent",
                >= .25d => "good",
                >= .15d => "acceptable",
                _ => "review"
            };

        return new ItemDifficultyResult(p, difficulty, discrimination, data.Length, quality);
    }
}
