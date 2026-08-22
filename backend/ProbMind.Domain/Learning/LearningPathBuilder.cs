namespace ProbMind.Domain.Learning;

public sealed class LearningPathBuilder
{
    public IReadOnlyList<LearningPathPriority> Build(
        IEnumerable<LearningPathCandidate> candidates,
        DateTimeOffset now)
    {
        return candidates
            .Select(candidate => BuildPriority(candidate, now))
            .Where(x => x.Priority >= 0.18d)
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.TopicId)
            .ToArray();
    }

    private static LearningPathPriority BuildPriority(
        LearningPathCandidate candidate,
        DateTimeOffset now)
    {
        var weakness = Math.Clamp(1d - candidate.Mastery, 0d, 1d);
        var misconception = Math.Clamp(candidate.MisconceptionConfidence, 0d, 1d);

        var days = candidate.LastPracticedAt is null
            ? 30d
            : Math.Max(0d, (now - candidate.LastPracticedAt.Value).TotalDays);

        var spacing = Math.Clamp(days / 30d, 0d, 1d);

        var priority =
            weakness * 0.48d +
            misconception * 0.42d +
            spacing * 0.10d;

        var reason = candidate.MisconceptionId is not null
            ? $"{candidate.TopicName}: confidence in «{candidate.MisconceptionTitle}» is {misconception:P0}; mastery {candidate.Mastery:P0}."
            : $"{candidate.TopicName}: topic mastery is {candidate.Mastery:P0}; spaced-review priority {spacing:P0}.";

        return new LearningPathPriority(
            candidate.TopicId,
            candidate.MisconceptionId,
            Math.Clamp(priority, 0d, 1d),
            reason);
    }
}
