using System.Text.Json;
using ProbMind.Application.Abstractions.Clock;
using ProbMind.Application.Abstractions.Persistence;
using ProbMind.Application.Contracts;
using ProbMind.Domain.Entities;
using ProbMind.Domain.Enums;
using ProbMind.Domain.Learning;

namespace ProbMind.Application.Services;

public sealed class LearningPathService : ILearningPathService
{
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;
    private readonly LearningPathBuilder _builder;

    public LearningPathService(IUnitOfWork uow, IClock clock, LearningPathBuilder builder)
    {
        _uow = uow;
        _clock = clock;
        _builder = builder;
    }

    public async Task<LearningPathDto> GetCurrentAsync(Guid userId, CancellationToken ct = default)
    {
        var paths = await _uow.LearningPaths.WhereAsync(
            x => x.UserId == userId && x.Status == LearningPathStatus.Active,
            ct);

        var current = paths.OrderByDescending(x => x.Revision).FirstOrDefault();
        return current is null
            ? await RebuildAsync(userId, "Первичное построение плана повторения", ct)
            : await MapAsync(current, ct);
    }

    public async Task<LearningPathDto> RebuildAsync(
        Guid userId,
        string reason,
        CancellationToken ct = default)
    {
        var existing = await _uow.LearningPaths.WhereAsync(
            x => x.UserId == userId && x.Status == LearningPathStatus.Active,
            ct);

        var nextRevision = (await _uow.LearningPaths.WhereAsync(x => x.UserId == userId, ct))
            .Select(x => x.Revision)
            .DefaultIfEmpty(0)
            .Max() + 1;

        foreach (var previous in existing)
        {
            previous.Status = LearningPathStatus.Rebuilt;
            previous.Touch();
            _uow.LearningPaths.Update(previous);
        }

        var topics = await _uow.Topics.ListAsync(ct);
        var misconceptions = await _uow.Misconceptions.ListAsync(ct);
        var mastery = await _uow.TopicMasteries.WhereAsync(x => x.UserId == userId, ct);
        var learnerMisconceptions = await _uow.UserMisconceptions.WhereAsync(
            x => x.UserId == userId &&
                 x.Status != MisconceptionStatus.Unknown &&
                 x.Status != MisconceptionStatus.Corrected,
            ct);
        var practice = await _uow.PracticeSessions.WhereAsync(x => x.UserId == userId, ct);

        var masteryByTopic = mastery.ToDictionary(x => x.TopicId);
        var misconceptionById = misconceptions.ToDictionary(x => x.Id);
        var candidates = new List<LearningPathCandidate>();

        foreach (var topic in topics.Where(x => x.Status == ContentStatus.Published))
        {
            var topicMastery = masteryByTopic.TryGetValue(topic.Id, out var m) ? m.Mastery : 0.5d;
            var relevantStates = learnerMisconceptions
                .Where(x =>
                    misconceptionById.TryGetValue(x.MisconceptionId, out var mc) &&
                    mc.TopicId == topic.Id)
                .OrderByDescending(x => x.Confidence)
                .ToArray();

            var lastPractice = practice
                .Where(x => x.TopicId == topic.Id)
                .OrderByDescending(x => x.CompletedAt ?? x.CreatedAt)
                .Select(x => x.CompletedAt ?? x.CreatedAt)
                .FirstOrDefault();

            if (relevantStates.Length == 0)
            {
                candidates.Add(new LearningPathCandidate(
                    topic.Id,
                    null,
                    topicMastery,
                    0d,
                    lastPractice == default ? null : lastPractice,
                    topic.NameRu,
                    null));
            }
            else
            {
                foreach (var state in relevantStates.Take(2))
                {
                    var mc = misconceptionById[state.MisconceptionId];
                    candidates.Add(new LearningPathCandidate(
                        topic.Id,
                        mc.Id,
                        topicMastery,
                        state.Confidence,
                        lastPractice == default ? null : lastPractice,
                        topic.NameRu,
                        mc.Title));
                }
            }
        }

        var priorities = _builder.Build(candidates, _clock.UtcNow);
        var path = new LearningPath
        {
            UserId = userId,
            Revision = nextRevision,
            Status = LearningPathStatus.Active,
            BuildReason = reason,
            BuiltAt = _clock.UtcNow
        };

        await _uow.LearningPaths.AddAsync(path, ct);

        var steps = priorities
            .Take(10)
            .Select((priority, index) => new LearningPathStep
            {
                LearningPathId = path.Id,
                TopicId = priority.TopicId,
                MisconceptionId = priority.MisconceptionId,
                Position = index + 1,
                PriorityScore = priority.Priority,
                Status = index == 0 ? LearningStepStatus.InProgress : LearningStepStatus.Pending,
                Reason = priority.Reason
            })
            .ToArray();

        await _uow.LearningPathSteps.AddRangeAsync(steps, ct);

        await _uow.LearningPathRevisions.AddAsync(new LearningPathRevision
        {
            UserId = userId,
            LearningPathId = path.Id,
            RevisionNumber = nextRevision,
            SnapshotJson = JsonSerializer.Serialize(priorities),
            ChangeReason = reason,
            RecordedAt = _clock.UtcNow
        }, ct);

        await _uow.ActivityEvents.AddAsync(new ActivityEvent
        {
            UserId = userId,
            EventType = ActivityEventType.LearningPathRebuilt,
            AggregateType = nameof(LearningPath),
            AggregateId = path.Id,
            PayloadJson = JsonSerializer.Serialize(new { revision = nextRevision, reason })
        }, ct);

        if (nextRevision > 1)
        {
            await _uow.Notifications.AddAsync(new Notification
            {
                UserId = userId,
                Type = NotificationType.LearningPathChanged,
                Title = "План повторения обновлён",
                Body = "Приоритеты пересчитаны с учётом последних ответов и результатов практики."
            }, ct);
        }

        await _uow.SaveChangesAsync(ct);
        return await MapAsync(path, ct);
    }

    public async Task<LearningPathDto> CompleteStepAsync(
        Guid userId,
        Guid stepId,
        CancellationToken ct = default)
    {
        var step = await GetOwnedStepAsync(userId, stepId, ct);
        step.Status = LearningStepStatus.Completed;
        step.CompletedAt = _clock.UtcNow;
        step.Touch();
        _uow.LearningPathSteps.Update(step);

        await ActivateNextAsync(step.LearningPathId, step.Position, ct);
        await _uow.SaveChangesAsync(ct);

        var path = await _uow.LearningPaths.GetByIdAsync(step.LearningPathId, ct)
            ?? throw new KeyNotFoundException("Learning path not found.");

        return await MapAsync(path, ct);
    }

    public async Task<LearningPathDto> SkipStepAsync(
        Guid userId,
        Guid stepId,
        CancellationToken ct = default)
    {
        var step = await GetOwnedStepAsync(userId, stepId, ct);
        step.Status = LearningStepStatus.Skipped;
        step.CompletedAt = _clock.UtcNow;
        step.Touch();
        _uow.LearningPathSteps.Update(step);

        await ActivateNextAsync(step.LearningPathId, step.Position, ct);
        await _uow.SaveChangesAsync(ct);

        var path = await _uow.LearningPaths.GetByIdAsync(step.LearningPathId, ct)
            ?? throw new KeyNotFoundException("Learning path not found.");

        return await MapAsync(path, ct);
    }

    public async Task<IReadOnlyList<LearningPathDto>> HistoryAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var paths = await _uow.LearningPaths.WhereAsync(x => x.UserId == userId, ct);
        var result = new List<LearningPathDto>();

        foreach (var path in paths.OrderByDescending(x => x.Revision))
            result.Add(await MapAsync(path, ct));

        return result;
    }

    private async Task<LearningPathStep> GetOwnedStepAsync(
        Guid userId,
        Guid stepId,
        CancellationToken ct)
    {
        var step = await _uow.LearningPathSteps.GetByIdAsync(stepId, ct)
            ?? throw new KeyNotFoundException("Learning path step not found.");
        var path = await _uow.LearningPaths.GetByIdAsync(step.LearningPathId, ct)
            ?? throw new KeyNotFoundException("Learning path not found.");

        if (path.UserId != userId)
            throw new UnauthorizedAccessException("Learning path belongs to another user.");

        return step;
    }

    private async Task ActivateNextAsync(Guid pathId, int currentPosition, CancellationToken ct)
    {
        var steps = await _uow.LearningPathSteps.WhereAsync(x => x.LearningPathId == pathId, ct);
        var next = steps
            .Where(x => x.Position > currentPosition && x.Status == LearningStepStatus.Pending)
            .OrderBy(x => x.Position)
            .FirstOrDefault();

        if (next is not null)
        {
            next.Status = LearningStepStatus.InProgress;
            next.Touch();
            _uow.LearningPathSteps.Update(next);
        }
        else if (steps.All(x => x.Status is LearningStepStatus.Completed or LearningStepStatus.Skipped))
        {
            var path = await _uow.LearningPaths.GetByIdAsync(pathId, ct);
            if (path is not null)
            {
                path.Status = LearningPathStatus.Completed;
                path.Touch();
                _uow.LearningPaths.Update(path);
            }
        }
    }

    private async Task<LearningPathDto> MapAsync(LearningPath path, CancellationToken ct)
    {
        var steps = await _uow.LearningPathSteps.WhereAsync(x => x.LearningPathId == path.Id, ct);
        var topics = (await _uow.Topics.ListAsync(ct)).ToDictionary(x => x.Id);
        var misconceptions = (await _uow.Misconceptions.ListAsync(ct)).ToDictionary(x => x.Id);

        return new LearningPathDto(
            path.Id,
            path.Revision,
            path.Status,
            path.BuiltAt,
            path.BuildReason,
            steps.OrderBy(x => x.Position).Select(step =>
            {
                var topic = topics[step.TopicId];
                Misconception? mc = null;
                if (step.MisconceptionId.HasValue)
                    misconceptions.TryGetValue(step.MisconceptionId.Value, out mc);

                return new LearningPathStepDto(
                    step.Id,
                    step.Position,
                    topic.Id,
                    topic.Code,
                    topic.NameRu,
                    mc?.Id,
                    mc?.Code,
                    mc?.Title,
                    step.PriorityScore,
                    step.Status,
                    step.Reason);
            }).ToArray());
    }
}
