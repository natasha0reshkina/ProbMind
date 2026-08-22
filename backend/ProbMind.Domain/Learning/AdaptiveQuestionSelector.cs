namespace ProbMind.Domain.Learning;

public sealed class AdaptiveQuestionSelector
{
    public IReadOnlyList<QuestionPriority> Rank(
        IEnumerable<AdaptiveCandidate> candidates,
        DateTimeOffset now,
        int take = 20)
    {
        return candidates
            .Where(x => x.IsPublished)
            .Select(x => Score(x, now))
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.QuestionId)
            .Take(Math.Max(1, take))
            .ToArray();
    }

    public QuestionPriority Score(AdaptiveCandidate candidate, DateTimeOffset now)
    {
        var weakness = Math.Clamp(1d - candidate.TopicMastery, 0d, 1d);

        var misconception = Math.Clamp(
            candidate.MisconceptionRelevance *
            candidate.MisconceptionConfidence,
            0d,
            1d);

        var daysSinceSeen = candidate.LastSeenAt is null
            ? 180d
            : Math.Max(0d, (now - candidate.LastSeenAt.Value).TotalDays);

        var spacing = Math.Clamp(daysSinceSeen / 21d, 0d, 1d);
        var difficultyFit = Math.Clamp(1d - candidate.DifficultyDistance, 0d, 1d);
        var novelty = candidate.ExposureCount == 0
            ? 1d
            : 1d / (candidate.ExposureCount + 1d);

        var repetitionPenalty = candidate.ExposureCount switch
        {
            <= 0 => 0d,
            1 => 0.03d,
            2 => 0.07d,
            3 => 0.12d,
            _ => Math.Min(0.28d, 0.12d + (candidate.ExposureCount - 3) * 0.04d)
        };

        var transferBonus =
            candidate.IsTransfer && candidate.MisconceptionConfidence >= 0.45d
                ? 0.06d
                : 0d;

        var total =
            weakness * 0.31d +
            misconception * 0.34d +
            spacing * 0.15d +
            difficultyFit * 0.12d +
            novelty * 0.08d +
            transferBonus -
            repetitionPenalty;

        total = Math.Clamp(total, 0d, 1d);

        var explanation =
            $"Приоритет {total:F2}: слабость темы {weakness:F2}, связь с выявленными ошибками {misconception:F2}, " +
            $"давность последней попытки {spacing:F2}, соответствие сложности {difficultyFit:F2}, новизна {novelty:F2}.";

        return new QuestionPriority(
            candidate.QuestionId,
            total,
            weakness * 0.31d,
            misconception * 0.34d,
            spacing * 0.15d,
            difficultyFit * 0.12d,
            novelty * 0.08d,
            repetitionPenalty,
            explanation);
    }
}
