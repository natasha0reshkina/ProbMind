using ProbMind.Application.Abstractions.Clock;
using ProbMind.Application.Abstractions.Persistence;
using ProbMind.Application.Contracts;
using ProbMind.Domain.Entities;
using ProbMind.Domain.Enums;

namespace ProbMind.Application.Services;

public sealed class RecommendationService : IRecommendationService
{
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;

    public RecommendationService(IUnitOfWork uow, IClock clock)
    {
        _uow = uow;
        _clock = clock;
    }

    public async Task<IReadOnlyList<RecommendationDto>> ListAsync(
        Guid userId,
        bool includeDismissed,
        CancellationToken ct = default)
    {
        var items = await _uow.Recommendations.WhereAsync(
            x => x.UserId == userId && (includeDismissed || !x.IsDismissed),
            ct);

        return items
            .OrderByDescending(x => x.Priority)
            .ThenByDescending(x => x.GeneratedAt)
            .Select(Map)
            .ToArray();
    }

    public async Task<IReadOnlyList<RecommendationDto>> RebuildAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var previous = await _uow.Recommendations.WhereAsync(
            x => x.UserId == userId && !x.IsDismissed,
            ct);

        foreach (var item in previous)
        {
            item.IsDismissed = true;
            item.Touch();
            _uow.Recommendations.Update(item);
        }

        var states = await _uow.UserMisconceptions.WhereAsync(x => x.UserId == userId, ct);
        var catalog = (await _uow.Misconceptions.ListAsync(ct)).ToDictionary(x => x.Id);
        var masteries = await _uow.TopicMasteries.WhereAsync(x => x.UserId == userId, ct);
        var topics = (await _uow.Topics.ListAsync(ct)).ToDictionary(x => x.Id);

        var generated = new List<Recommendation>();

        foreach (var state in states
                     .Where(x => x.Status != MisconceptionStatus.Unknown && x.Status != MisconceptionStatus.Corrected)
                     .OrderByDescending(x => x.Confidence))
        {
            if (!catalog.TryGetValue(state.MisconceptionId, out var mc))
                continue;

            generated.Add(new Recommendation
            {
                UserId = userId,
                TopicId = mc.TopicId,
                MisconceptionId = mc.Id,
                Type = state.Status == MisconceptionStatus.RecheckRequired
                    ? RecommendationType.Recheck
                    : RecommendationType.StartCorrection,
                Priority = Math.Clamp(0.45d + state.Confidence * 0.55d, 0d, 1d),
                Title = state.Status == MisconceptionStatus.RecheckRequired
                    ? $"Перепроверьте: {mc.Title}"
                    : $"Исправьте заблуждение: {mc.Title}",
                Rationale = $"Паттерн ошибки подтверждён предыдущими ответами. Диагностическая уверенность — {state.Confidence:P0}.",
                GeneratedAt = _clock.UtcNow
            });
        }

        foreach (var mastery in masteries.Where(x => x.Mastery < 0.72d))
        {
            if (!topics.TryGetValue(mastery.TopicId, out var topic))
                continue;

            generated.Add(new Recommendation
            {
                UserId = userId,
                TopicId = topic.Id,
                Type = mastery.Mastery < 0.45d
                    ? RecommendationType.ReviewTheory
                    : RecommendationType.ContinuePractice,
                Priority = Math.Clamp(1d - mastery.Mastery, 0d, 1d) * 0.8d,
                Title = $"Повторите тему «{topic.NameRu}»",
                Rationale = $"Текущий уровень освоения: {mastery.Mastery:P0}; неопределённость оценки: {mastery.Uncertainty:P0}.",
                GeneratedAt = _clock.UtcNow
            });
        }

        if (generated.Count == 0)
        {
            generated.Add(new Recommendation
            {
                UserId = userId,
                Type = RecommendationType.MaintainMastery,
                Priority = 0.35d,
                Title = "Поддерживающая диагностика",
                Rationale = "Выраженных слабых мест нет. Рекомендуется интервальная перепроверка знаний.",
                GeneratedAt = _clock.UtcNow
            });
        }

        await _uow.Recommendations.AddRangeAsync(generated, ct);
        await _uow.SaveChangesAsync(ct);

        return generated
            .OrderByDescending(x => x.Priority)
            .Select(Map)
            .ToArray();
    }

    public async Task DismissAsync(Guid userId, Guid recommendationId, CancellationToken ct = default)
    {
        var item = await _uow.Recommendations.GetByIdAsync(recommendationId, ct)
            ?? throw new KeyNotFoundException("Recommendation not found.");

        if (item.UserId != userId)
            throw new UnauthorizedAccessException("Recommendation belongs to another user.");

        item.IsDismissed = true;
        item.Touch();
        _uow.Recommendations.Update(item);
        await _uow.SaveChangesAsync(ct);
    }

    private static RecommendationDto Map(Recommendation x) =>
        new(
            x.Id,
            x.Type,
            x.Title,
            x.Rationale,
            x.Priority,
            x.TopicId,
            x.MisconceptionId,
            x.IsDismissed,
            x.GeneratedAt);
}
