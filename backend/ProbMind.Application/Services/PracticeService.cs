using System.Text.Json;
using ProbMind.Application.Abstractions.Clock;
using ProbMind.Application.Abstractions.Persistence;
using ProbMind.Application.Common;
using ProbMind.Application.Contracts;
using ProbMind.Domain.Entities;
using ProbMind.Domain.Enums;
using ProbMind.Domain.Learning;

namespace ProbMind.Application.Services;

public sealed class PracticeService : IPracticeService
{
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;
    private readonly ILearnerModelService _learner;
    private readonly ILearningPathService _paths;
    private readonly IRecommendationService _recommendations;
    private readonly CorrectionEngine _correction;

    public PracticeService(
        IUnitOfWork uow,
        IClock clock,
        ILearnerModelService learner,
        ILearningPathService paths,
        IRecommendationService recommendations,
        CorrectionEngine correction)
    {
        _uow = uow;
        _clock = clock;
        _learner = learner;
        _paths = paths;
        _recommendations = recommendations;
        _correction = correction;
    }

    public async Task<PracticeSessionDto> StartAsync(
        Guid userId,
        StartPracticeRequest request,
        CancellationToken ct = default)
    {
        Guard.Range(request.TargetExercises, 3, 12, "targetExercises");

        if (await ExamSessionRules.ActiveAttemptAsync(_uow, userId, ct) is not null)
            throw new InvalidOperationException("Во время активного экзамена нельзя начинать практику.");

        _ = await _uow.Topics.GetByIdAsync(request.TopicId, ct)
            ?? throw new KeyNotFoundException("Topic not found.");

        if (request.MisconceptionId.HasValue)
        {
            var mc = await _uow.Misconceptions.GetByIdAsync(request.MisconceptionId.Value, ct)
                ?? throw new KeyNotFoundException("Misconception not found.");
            if (mc.TopicId != request.TopicId)
                throw new InvalidOperationException("Misconception belongs to another topic.");
        }

        var session = new PracticeSession
        {
            UserId = userId,
            TopicId = request.TopicId,
            MisconceptionId = request.MisconceptionId,
            Status = PracticeStatus.InProgress,
            TargetExercises = request.TargetExercises,
            StartedAt = _clock.UtcNow
        };

        await _uow.PracticeSessions.AddAsync(session, ct);
        await _uow.ActivityEvents.AddAsync(new ActivityEvent
        {
            UserId = userId,
            EventType = ActivityEventType.PracticeStarted,
            AggregateType = nameof(PracticeSession),
            AggregateId = session.Id,
            PayloadJson = JsonSerializer.Serialize(request)
        }, ct);

        await _uow.SaveChangesAsync(ct);
        return Map(session);
    }

    public async Task<PracticeSessionDto> GetAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken ct = default)
    {
        return Map(await GetOwnedAsync(userId, sessionId, ct));
    }

    public async Task<DiagnosticQuestionDto?> NextQuestionAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken ct = default)
    {
        var session = await GetOwnedAsync(userId, sessionId, ct);

        if (session.Status != PracticeStatus.InProgress)
            throw new InvalidOperationException("Practice session is not in progress.");

        if (session.CompletedExercises >= session.TargetExercises)
            return null;

        var attempts = await _uow.PracticeAttempts.WhereAsync(
            x => x.PracticeSessionId == session.Id,
            ct);
        var attemptedIds = attempts.Select(x => x.QuestionId).ToHashSet();

        var questions = await _uow.Questions.WhereAsync(
            x => x.TopicId == session.TopicId &&
                 x.Status == ContentStatus.Published &&
                 x.Kind != QuestionKind.Diagnostic,
            ct);

        var maps = await _uow.QuestionMisconceptionMaps.ListAsync(ct);
        var eligible = questions.Where(x => !attemptedIds.Contains(x.Id));

        if (session.MisconceptionId.HasValue)
        {
            var relatedIds = maps
                .Where(x => x.MisconceptionId == session.MisconceptionId.Value)
                .Select(x => x.QuestionId)
                .ToHashSet();
            eligible = eligible.Where(x => relatedIds.Contains(x.Id));
        }

        var question = eligible
            .OrderBy(x => x.Kind == QuestionKind.Transfer ? 1 : 0)
            .ThenBy(x => x.Code)
            .FirstOrDefault();

        if (question is null)
        {
            question = questions
                .Where(x => !attemptedIds.Contains(x.Id))
                .OrderBy(x => x.Kind == QuestionKind.Transfer ? 1 : 0)
                .ThenBy(x => x.Code)
                .FirstOrDefault();
        }

        if (question is null)
            return null;

        var versions = await _uow.QuestionVersions.WhereAsync(
            x => x.QuestionId == question.Id &&
                 x.VersionNumber == question.CurrentVersionNumber,
            ct);
        var version = versions.Single();

        var topic = await _uow.Topics.GetByIdAsync(question.TopicId, ct)
            ?? throw new InvalidOperationException("Topic missing.");
        var optionEntities = await _uow.AnswerOptions.WhereAsync(x => x.QuestionVersionId == version.Id, ct);
        var options = AnswerOptionOrdering.ForSession(optionEntities, session.Id, question.Id);

        return new DiagnosticQuestionDto(
            question.Id,
            version.Id,
            topic.Code,
            topic.NameRu,
            version.Prompt,
            version.Difficulty,
            version.IsTransferQuestion,
            options,
            session.MisconceptionId.HasValue
                ? "Задание выбрано для коррекции активного типа ошибки."
                : "Задание выбрано для практики по текущей теме.");
    }

    public async Task<AnswerFeedbackDto> SubmitAsync(
        Guid userId,
        SubmitPracticeAnswerRequest request,
        CancellationToken ct = default)
    {
        var session = await GetOwnedAsync(userId, request.SessionId, ct);

        if (session.Status != PracticeStatus.InProgress)
            throw new InvalidOperationException("Practice session is not in progress.");

        if (session.CompletedExercises >= session.TargetExercises)
            throw new InvalidOperationException("Лимит ответов для этой тренировки исчерпан.");

        var expected = await NextQuestionAsync(userId, session.Id, ct)
            ?? throw new InvalidOperationException("Для этой тренировки больше нет доступных заданий.");
        if (expected.QuestionId != request.QuestionId || expected.VersionId != request.QuestionVersionId)
            throw new InvalidOperationException("Этот вопрос или его версия не назначены текущей тренировке.");

        var duplicate = await _uow.PracticeAttempts.AnyAsync(
            x => x.PracticeSessionId == session.Id && x.QuestionId == request.QuestionId,
            ct);

        if (duplicate)
            throw new InvalidOperationException("This question has already been answered in the practice session.");

        var question = await _uow.Questions.GetByIdAsync(request.QuestionId, ct)
            ?? throw new KeyNotFoundException("Question not found.");

        if (question.Kind == QuestionKind.Diagnostic || question.Status != ContentStatus.Published)
            throw new InvalidOperationException("Question is not available for practice use.");

        var version = await _uow.QuestionVersions.GetByIdAsync(request.QuestionVersionId, ct)
            ?? throw new KeyNotFoundException("Question version not found.");
        var option = await _uow.AnswerOptions.GetByIdAsync(request.AnswerOptionId, ct)
            ?? throw new KeyNotFoundException("Answer option not found.");

        if (question.TopicId != session.TopicId ||
            version.QuestionId != question.Id ||
            option.QuestionVersionId != version.Id)
            throw new InvalidOperationException("Practice answer references inconsistent content.");

        var isTransferQuestion = question.Kind == QuestionKind.Transfer || version.IsTransferQuestion;
        var effectiveExerciseType = isTransferQuestion
            ? ExerciseType.Transfer
            : request.ExerciseType == ExerciseType.Transfer
                ? ExerciseType.IndependentPractice
                : request.ExerciseType;

        var attempt = new PracticeAttempt
        {
            PracticeSessionId = session.Id,
            UserId = userId,
            QuestionId = question.Id,
            QuestionVersionId = version.Id,
            AnswerOptionId = option.Id,
            ExerciseType = effectiveExerciseType,
            IsCorrect = option.IsCorrect,
            ResponseTimeMs = Math.Max(0, request.ResponseTimeMs),
            StudentNote = string.IsNullOrWhiteSpace(request.StudentNote) ? null : request.StudentNote.Trim(),
            ConfidenceLevel = request.ConfidenceLevel.HasValue ? Math.Clamp(request.ConfidenceLevel.Value, 1, 4) : null,
            Reasoning = string.IsNullOrWhiteSpace(request.Reasoning) ? null : request.Reasoning.Trim(),
            SubmittedAt = _clock.UtcNow
        };

        await _uow.PracticeAttempts.AddAsync(attempt, ct);

        Guid? affected = session.MisconceptionId ?? option.MisconceptionId;
        if (affected.HasValue)
        {
            var (kind, weight) = ResolveEvidence(effectiveExerciseType, option.IsCorrect);
            await _uow.MisconceptionEvidence.AddAsync(new MisconceptionEvidence
            {
                UserId = userId,
                MisconceptionId = affected.Value,
                PracticeAttemptId = attempt.Id,
                Kind = kind,
                RawWeight = weight,
                RelevanceWeight = 1d,
                Explanation = option.IsCorrect
                    ? "Правильный ответ снижает вероятность повторения выбранной ошибки."
                    : "Ошибка повторилась в коррекционной практике.",
                ObservedAt = _clock.UtcNow
            }, ct);
        }

        await _uow.ActivityEvents.AddAsync(new ActivityEvent
        {
            UserId = userId,
            EventType = ActivityEventType.AnswerSubmitted,
            AggregateType = "PracticeAttempt",
            AggregateId = attempt.Id,
            OccurredAt = _clock.UtcNow
        }, ct);

        session.CompletedExercises++;
        session.Touch();
        _uow.PracticeSessions.Update(session);
        await _uow.SaveChangesAsync(ct);

        if (affected.HasValue)
            await _learner.RecalculateMisconceptionAsync(userId, affected.Value, ct);
        await _learner.RecalculateTopicAsync(userId, session.TopicId, ct);

        UserMisconception? state = null;
        if (affected.HasValue)
        {
            state = (await _uow.UserMisconceptions.WhereAsync(
                x => x.UserId == userId && x.MisconceptionId == affected.Value,
                ct)).SingleOrDefault();
        }

        return new AnswerFeedbackDto(
            true,
            option.IsCorrect,
            option.Feedback,
            version.CorrectExplanation,
            affected.HasValue
                ? (await _uow.Misconceptions.GetByIdAsync(affected.Value, ct))?.Code
                : null,
            state?.Confidence,
            state?.Status);
    }

    public async Task<PracticeResultDto> CompleteAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken ct = default)
    {
        var session = await GetOwnedAsync(userId, sessionId, ct);

        if (session.Status == PracticeStatus.Completed)
            return await BuildResultAsync(session, ct);

        var attempts = await _uow.PracticeAttempts.WhereAsync(
            x => x.PracticeSessionId == session.Id,
            ct);

        if (attempts.Count < session.TargetExercises)
            throw new InvalidOperationException("Not enough practice attempts to complete the session.");

        session.Status = PracticeStatus.Completed;
        session.CompletedAt = _clock.UtcNow;
        session.Touch();
        _uow.PracticeSessions.Update(session);

        await _uow.ActivityEvents.AddAsync(new ActivityEvent
        {
            UserId = userId,
            EventType = ActivityEventType.PracticeCompleted,
            AggregateType = nameof(PracticeSession),
            AggregateId = session.Id,
            PayloadJson = JsonSerializer.Serialize(new
            {
                correct = attempts.Count(x => x.IsCorrect),
                total = attempts.Count
            })
        }, ct);

        await _uow.SaveChangesAsync(ct);
        await _learner.RecalculateAllAsync(userId, ct);
        await _recommendations.RebuildAsync(userId, ct);
        await _paths.RebuildAsync(userId, "Завершена практика", ct);
        await UpdateSpacedReviewAfterPracticeAsync(session, attempts, ct);

        return await BuildResultAsync(session, ct);
    }

    public async Task<IReadOnlyList<PracticeSessionDto>> HistoryAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var sessions = await _uow.PracticeSessions.WhereAsync(x => x.UserId == userId, ct);
        var sessionIds = sessions.Select(x => x.Id).ToHashSet();
        var attempts = (await _uow.PracticeAttempts.WhereAsync(x => x.UserId == userId, ct))
            .Where(x => sessionIds.Contains(x.PracticeSessionId))
            .GroupBy(x => x.PracticeSessionId)
            .ToDictionary(x => x.Key, x => x.ToArray());

        return sessions
            .OrderByDescending(x => x.CreatedAt)
            .Select(session =>
            {
                var rows = attempts.GetValueOrDefault(session.Id) ?? Array.Empty<PracticeAttempt>();
                return new PracticeSessionDto(
                    session.Id,
                    session.TopicId,
                    session.MisconceptionId,
                    session.Status,
                    session.TargetExercises,
                    session.CompletedExercises,
                    session.StartedAt,
                    session.CompletedAt,
                    rows.Count(x => x.IsCorrect),
                    rows.Count(x => !x.IsCorrect));
            })
            .ToArray();
    }

    private async Task<PracticeResultDto> BuildResultAsync(PracticeSession session, CancellationToken ct)
    {
        var attempts = await _uow.PracticeAttempts.WhereAsync(
            x => x.PracticeSessionId == session.Id,
            ct);
        var transferPassed = _correction.IsCorrectionComplete(
            attempts.Select(x => (x.ExerciseType, x.IsCorrect)).ToArray());

        UserMisconception? state = null;
        if (session.MisconceptionId.HasValue)
        {
            state = (await _uow.UserMisconceptions.WhereAsync(
                x => x.UserId == session.UserId &&
                     x.MisconceptionId == session.MisconceptionId.Value,
                ct)).SingleOrDefault();
        }

        var path = await _paths.GetCurrentAsync(session.UserId, ct);

        return new PracticeResultDto(
            Map(session),
            attempts.Count(x => x.IsCorrect),
            attempts.Count,
            transferPassed,
            state?.Confidence,
            state?.Status,
            path);
    }

    private async Task UpdateSpacedReviewAfterPracticeAsync(
        PracticeSession session,
        IReadOnlyList<PracticeAttempt> attempts,
        CancellationToken ct)
    {
        var records = await _uow.SpacedReviewItems.WhereAsync(
            x => x.UserId == session.UserId && x.TopicId == session.TopicId,
            ct);
        var item = records.SingleOrDefault() ?? new SpacedReviewItem
        {
            UserId = session.UserId,
            TopicId = session.TopicId
        };

        var accuracy = attempts.Count == 0 ? 0d : attempts.Count(x => x.IsCorrect) / (double)attempts.Count;
        if (accuracy < .6d)
        {
            item.Repetitions = 0;
            item.IntervalDays = 0;
            item.EaseFactor = Math.Max(1.3d, item.EaseFactor - .2d);
        }
        else
        {
            item.Repetitions++;
            item.IntervalDays = item.Repetitions switch
            {
                1 => 1,
                2 => 3,
                3 => 7,
                _ => Math.Max(7, (int)Math.Round(item.IntervalDays * item.EaseFactor))
            };
            if (accuracy >= .85d)
                item.EaseFactor = Math.Min(3d, item.EaseFactor + .1d);
        }

        item.LastReviewedAt = _clock.UtcNow;
        item.NextReviewAt = _clock.UtcNow.AddDays(item.IntervalDays);
        item.Touch();
        if (records.Count == 0)
            await _uow.SpacedReviewItems.AddAsync(item, ct);
        else
            _uow.SpacedReviewItems.Update(item);
        await _uow.SaveChangesAsync(ct);
    }

    private async Task<PracticeSession> GetOwnedAsync(Guid userId, Guid sessionId, CancellationToken ct)
    {
        var session = await _uow.PracticeSessions.GetByIdAsync(sessionId, ct)
            ?? throw new KeyNotFoundException("Practice session not found.");

        if (session.UserId != userId)
            throw new UnauthorizedAccessException("Practice session belongs to another user.");

        return session;
    }

    private static (EvidenceKind kind, double weight) ResolveEvidence(ExerciseType type, bool correct)
    {
        if (type == ExerciseType.Transfer)
            return correct
                ? (EvidenceKind.TransferSuccess, -1.15d)
                : (EvidenceKind.TransferFailure, 1.20d);

        if (correct)
            return (EvidenceKind.CorrectionSuccess, -0.85d);

        return (EvidenceKind.Distractor, 0.85d);
    }

    private static PracticeSessionDto Map(PracticeSession session) =>
        new(
            session.Id,
            session.TopicId,
            session.MisconceptionId,
            session.Status,
            session.TargetExercises,
            session.CompletedExercises,
            session.StartedAt,
            session.CompletedAt);
}
