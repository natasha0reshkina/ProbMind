using ProbMind.Application.Abstractions.Clock;
using ProbMind.Application.Abstractions.Persistence;
using ProbMind.Application.Contracts;
using ProbMind.Domain.Diagnostics;
using ProbMind.Domain.Entities;
using ProbMind.Domain.Enums;
using ProbMind.Domain.Explainability;
using ProbMind.Domain.Learning;

namespace ProbMind.Application.Services;

public sealed class LearnerModelService : ILearnerModelService
{
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;
    private readonly ConfidenceEngine _confidence;
    private readonly MisconceptionStateMachine _stateMachine;
    private readonly MasteryEngine _mastery;
    private readonly EvidenceContributionAnalyzer _contributions;
    private readonly DiagnosticExplanationBuilder _explanations;

    public LearnerModelService(
        IUnitOfWork uow,
        IClock clock,
        ConfidenceEngine confidence,
        MisconceptionStateMachine stateMachine,
        MasteryEngine mastery,
        EvidenceContributionAnalyzer contributions,
        DiagnosticExplanationBuilder explanations)
    {
        _uow = uow;
        _clock = clock;
        _confidence = confidence;
        _stateMachine = stateMachine;
        _mastery = mastery;
        _contributions = contributions;
        _explanations = explanations;
    }

    public async Task<IReadOnlyList<UserMisconceptionDto>> ListMisconceptionsAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var states = await _uow.UserMisconceptions.WhereAsync(x => x.UserId == userId, ct);
        var catalog = await _uow.Misconceptions.ListAsync(ct);
        var byId = catalog.ToDictionary(x => x.Id);

        return states
            .Where(x => byId.ContainsKey(x.MisconceptionId))
            .OrderByDescending(x => x.Confidence)
            .Select(x => Map(x, byId[x.MisconceptionId]))
            .ToArray();
    }

    public async Task<MisconceptionDetailDto> GetMisconceptionAsync(
        Guid userId,
        Guid misconceptionId,
        CancellationToken ct = default)
    {
        var misconception = await _uow.Misconceptions.GetByIdAsync(misconceptionId, ct)
            ?? throw new KeyNotFoundException("Misconception not found.");

        var states = await _uow.UserMisconceptions.WhereAsync(
            x => x.UserId == userId && x.MisconceptionId == misconceptionId,
            ct);

        var state = states.SingleOrDefault() ?? new UserMisconception
        {
            UserId = userId,
            MisconceptionId = misconceptionId,
            Confidence = 0d,
            Status = MisconceptionStatus.Unknown
        };

        var evidence = await _uow.MisconceptionEvidence.WhereAsync(
            x => x.UserId == userId && x.MisconceptionId == misconceptionId,
            ct);

        var evidenceDtos = evidence
            .OrderByDescending(x => x.ObservedAt)
            .Select(x => new EvidenceDto(
                x.Id,
                x.Kind,
                x.RawWeight,
                x.RelevanceWeight,
                x.Explanation,
                x.ObservedAt))
            .ToArray();

        var contributionReport = _contributions.Analyze(evidence.Select(x => (
            x.Id,
            x.Kind,
            x.RawWeight,
            x.RelevanceWeight,
            x.Explanation)));

        var explanation = _explanations.Build(
            misconception.Title,
            state.Confidence,
            contributionReport.Contributions.Select(x => new ExplanationEvidence(
                x.Reason,
                x.SignedContribution,
                evidence.First(e => e.Id == x.EvidenceId).ObservedAt)),
            state.Status == MisconceptionStatus.CorrectionInProgress);

        var reasoning = new DiagnosticReasoningDto(
            explanation.Summary,
            explanation.ConfidenceBand,
            explanation.SupportingReasons,
            explanation.ContradictingReasons,
            explanation.NextAction,
            contributionReport.Contributions.Select(x => new EvidenceContributionDto(
                x.EvidenceId,
                x.Kind,
                x.SignedContribution,
                x.Share,
                x.Direction,
                x.Reason)).ToArray());

        return new MisconceptionDetailDto(
            Map(state, misconception),
            evidenceDtos,
            misconception.CorrectiveExplanation,
            misconception.DiagnosticRationale,
            reasoning);
    }

    public async Task<IReadOnlyList<TopicProgressDto>> GetTopicMasteryAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var topics = await _uow.Topics.ListAsync(ct);
        var masteries = await _uow.TopicMasteries.WhereAsync(x => x.UserId == userId, ct);
        var byTopic = masteries.ToDictionary(x => x.TopicId);

        return topics
            .OrderBy(x => x.SortOrder)
            .Select(topic =>
            {
                byTopic.TryGetValue(topic.Id, out var state);
                return new TopicProgressDto(
                    topic.Id,
                    topic.Code,
                    topic.NameRu,
                    state?.Mastery ?? 0.50d,
                    state?.Uncertainty ?? 1d,
                    state?.ObservationCount ?? 0);
            })
            .ToArray();
    }

    public async Task RecalculateMisconceptionAsync(
        Guid userId,
        Guid misconceptionId,
        CancellationToken ct = default)
    {
        var evidence = await _uow.MisconceptionEvidence.WhereAsync(
            x => x.UserId == userId && x.MisconceptionId == misconceptionId,
            ct);

        var observations = evidence.Select(x => new EvidenceObservation(
            x.Kind,
            x.RawWeight,
            x.RelevanceWeight,
            x.ObservedAt,
            QuestionId: null,
            IsTransfer: x.Kind is EvidenceKind.TransferSuccess or EvidenceKind.TransferFailure))
            .ToArray();

        var result = _confidence.Calculate(observations, _clock.UtcNow);
        var states = await _uow.UserMisconceptions.WhereAsync(
            x => x.UserId == userId && x.MisconceptionId == misconceptionId,
            ct);

        var state = states.SingleOrDefault();
        var isNewState = state is null;
        if (state is null)
        {
            state = new UserMisconception
            {
                UserId = userId,
                MisconceptionId = misconceptionId
            };
            await _uow.UserMisconceptions.AddAsync(state, ct);
        }

        var oldStatus = state.Status;
        state.Confidence = result.Confidence;
        state.EvidenceCount = evidence.Count;
        state.PositiveEvidenceCount = evidence.Count(x => x.RawWeight > 0d);
        state.NegativeEvidenceCount = evidence.Count(x => x.RawWeight < 0d);
        state.LastDetectedAt = evidence
            .Where(x => x.RawWeight > 0d)
            .OrderByDescending(x => x.ObservedAt)
            .Select(x => (DateTimeOffset?)x.ObservedAt)
            .FirstOrDefault();
        state.FirstDetectedAt ??= evidence
            .Where(x => x.RawWeight > 0d)
            .OrderBy(x => x.ObservedAt)
            .Select(x => (DateTimeOffset?)x.ObservedAt)
            .FirstOrDefault();

        state.Status = _stateMachine.Resolve(state.Status, state.Confidence);
        if (state.Status == MisconceptionStatus.Corrected && oldStatus != MisconceptionStatus.Corrected)
            state.CorrectedAt = _clock.UtcNow;

        state.Touch();
        if (!isNewState)
            _uow.UserMisconceptions.Update(state);

        if (oldStatus != state.Status)
        {
            await _uow.ActivityEvents.AddAsync(new ActivityEvent
            {
                UserId = userId,
                EventType = state.Status == MisconceptionStatus.Corrected
                    ? ActivityEventType.MisconceptionCorrected
                    : ActivityEventType.MisconceptionDetected,
                AggregateType = nameof(UserMisconception),
                AggregateId = state.Id,
                PayloadJson = $$"""{"old":"{{oldStatus}}","new":"{{state.Status}}","confidence":{{state.Confidence.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}"""
            }, ct);

            if (state.Status is MisconceptionStatus.Detected or MisconceptionStatus.Corrected)
            {
                var misconception = await _uow.Misconceptions.GetByIdAsync(misconceptionId, ct);
                if (misconception is not null)
                {
                    await _uow.Notifications.AddAsync(new Notification
                    {
                        UserId = userId,
                        Type = state.Status == MisconceptionStatus.Corrected
                            ? NotificationType.CorrectionCompleted
                            : NotificationType.MisconceptionDetected,
                        Title = state.Status == MisconceptionStatus.Corrected
                            ? "Ошибка скорректирована"
                            : "Обнаружен устойчивый паттерн ошибки",
                        Body = state.Status == MisconceptionStatus.Corrected
                            ? $"Паттерн «{misconception.Title}» больше не подтверждается последними ответами."
                            : $"В нескольких ответах повторяется паттерн «{misconception.Title}». Уверенность диагностики — {state.Confidence:P0}."
                    }, ct);
                }
            }
        }

        await _uow.SaveChangesAsync(ct);
    }

    public async Task RecalculateTopicAsync(
        Guid userId,
        Guid topicId,
        CancellationToken ct = default)
    {
        var questions = await _uow.Questions.WhereAsync(x => x.TopicId == topicId, ct);
        var questionIds = questions.Select(x => x.Id).ToHashSet();
        var versions = await _uow.QuestionVersions.ListAsync(ct);
        var versionsById = versions.ToDictionary(x => x.Id);

        var diagnosticAnswers = await _uow.DiagnosticAnswers.WhereAsync(
            x => x.UserId == userId,
            ct);
        var practiceAttempts = await _uow.PracticeAttempts.WhereAsync(
            x => x.UserId == userId,
            ct);

        var observations = new List<MasteryObservation>();

        foreach (var answer in diagnosticAnswers.Where(x => questionIds.Contains(x.QuestionId)))
        {
            if (!versionsById.TryGetValue(answer.QuestionVersionId, out var version))
                continue;

            observations.Add(new MasteryObservation(
                answer.IsCorrect,
                version.Difficulty,
                answer.SubmittedAt,
                1d,
                version.IsTransferQuestion));
        }

        foreach (var attempt in practiceAttempts.Where(x => questionIds.Contains(x.QuestionId)))
        {
            if (!versionsById.TryGetValue(attempt.QuestionVersionId, out var version))
                continue;

            observations.Add(new MasteryObservation(
                attempt.IsCorrect,
                version.Difficulty,
                attempt.SubmittedAt,
                0.85d,
                version.IsTransferQuestion));
        }

        var result = _mastery.Calculate(observations, _clock.UtcNow);
        var states = await _uow.TopicMasteries.WhereAsync(
            x => x.UserId == userId && x.TopicId == topicId,
            ct);

        var state = states.SingleOrDefault();
        var isNewState = state is null;
        if (state is null)
        {
            state = new TopicMastery
            {
                UserId = userId,
                TopicId = topicId
            };
            await _uow.TopicMasteries.AddAsync(state, ct);
        }

        var before = state.Mastery;
        state.Mastery = result.Mastery;
        state.Uncertainty = result.Uncertainty;
        state.ObservationCount = result.ObservationCount;
        state.LastObservedAt = observations.Count == 0
            ? null
            : observations.Max(x => x.ObservedAt);
        state.Touch();
        if (!isNewState)
            _uow.TopicMasteries.Update(state);

        if (Math.Abs(before - state.Mastery) >= 0.015d || state.ObservationCount == observations.Count)
        {
            await _uow.TopicMasteryHistory.AddAsync(new TopicMasteryHistory
            {
                UserId = userId,
                TopicId = topicId,
                Mastery = state.Mastery,
                Uncertainty = state.Uncertainty,
                Reason = result.Explanation
            }, ct);
        }

        await _uow.SaveChangesAsync(ct);
    }

    public async Task RecalculateAllAsync(Guid userId, CancellationToken ct = default)
    {
        var misconceptions = await _uow.Misconceptions.ListAsync(ct);
        foreach (var misconception in misconceptions)
            await RecalculateMisconceptionAsync(userId, misconception.Id, ct);

        var topics = await _uow.Topics.ListAsync(ct);
        foreach (var topic in topics)
            await RecalculateTopicAsync(userId, topic.Id, ct);
    }

    private static UserMisconceptionDto Map(UserMisconception state, Misconception misconception) =>
        new(
            misconception.Id,
            misconception.Code,
            misconception.Title,
            misconception.Description,
            state.Confidence,
            state.Status,
            state.EvidenceCount,
            state.LastDetectedAt);
}
