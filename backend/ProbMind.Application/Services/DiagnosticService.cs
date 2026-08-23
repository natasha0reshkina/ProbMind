using System.Text.Json;
using ProbMind.Application.Abstractions.Clock;
using ProbMind.Application.Abstractions.Messaging;
using ProbMind.Application.Abstractions.Persistence;
using ProbMind.Application.Common;
using ProbMind.Application.Contracts;
using ProbMind.Domain.Entities;
using ProbMind.Domain.Enums;
using ProbMind.Domain.Learning;

namespace ProbMind.Application.Services;

public sealed class DiagnosticService : IDiagnosticService
{
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;
    private readonly AdaptiveQuestionSelector _selector;
    private readonly DiagnosticCoveragePlanner _coveragePlanner;
    private readonly ILearnerModelService _learner;
    private readonly ILearningPathService _paths;
    private readonly IRecommendationService _recommendations;
    private readonly IBackgroundJobQueue _jobs;

    public DiagnosticService(
        IUnitOfWork uow,
        IClock clock,
        AdaptiveQuestionSelector selector,
        DiagnosticCoveragePlanner coveragePlanner,
        ILearnerModelService learner,
        ILearningPathService paths,
        IRecommendationService recommendations,
        IBackgroundJobQueue jobs)
    {
        _uow = uow;
        _clock = clock;
        _selector = selector;
        _coveragePlanner = coveragePlanner;
        _learner = learner;
        _paths = paths;
        _recommendations = recommendations;
        _jobs = jobs;
    }

    public async Task<DiagnosticSessionDto> StartAsync(
        Guid userId,
        StartDiagnosticRequest request,
        CancellationToken ct = default)
    {
        var count = Guard.Range(request.QuestionCount, 10, 30, "questionCount");

        var active = await _uow.DiagnosticSessions.WhereAsync(
            x => x.UserId == userId &&
                 (x.Status == DiagnosticStatus.Created || x.Status == DiagnosticStatus.InProgress),
            ct);

        foreach (var stale in active)
        {
            stale.Status = DiagnosticStatus.Cancelled;
            stale.Touch();
            _uow.DiagnosticSessions.Update(stale);
        }

        var session = new DiagnosticSession
        {
            UserId = userId,
            PlannedQuestionCount = count,
            AnsweredQuestionCount = 0,
            Status = DiagnosticStatus.InProgress,
            StartedAt = _clock.UtcNow,
            SelectionPolicyVersion = "adaptive-v3"
        };

        await _uow.DiagnosticSessions.AddAsync(session, ct);
        await _uow.ActivityEvents.AddAsync(new ActivityEvent
        {
            UserId = userId,
            EventType = ActivityEventType.DiagnosticStarted,
            AggregateType = nameof(DiagnosticSession),
            AggregateId = session.Id,
            PayloadJson = JsonSerializer.Serialize(new { count })
        }, ct);

        await _uow.SaveChangesAsync(ct);
        return Map(session);
    }

    public async Task<IReadOnlyList<DiagnosticSessionDto>> ListAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var sessions = await _uow.DiagnosticSessions.WhereAsync(x => x.UserId == userId, ct);
        return sessions
            .OrderByDescending(x => x.CreatedAt)
            .Select(Map)
            .ToArray();
    }

    public async Task<DiagnosticSessionDto> GetAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken ct = default)
    {
        return Map(await GetOwnedSessionAsync(userId, sessionId, ct));
    }

    public async Task<DiagnosticQuestionDto?> NextQuestionAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken ct = default)
    {
        var session = await GetOwnedSessionAsync(userId, sessionId, ct);

        if (session.Status != DiagnosticStatus.InProgress)
            throw new InvalidOperationException("Diagnostic session is not in progress.");

        if (session.AnsweredQuestionCount >= session.PlannedQuestionCount)
            return null;

        var questions = (await _uow.Questions.WhereAsync(
            x => x.Status == ContentStatus.Published && x.Kind == QuestionKind.Diagnostic,
            ct)).ToArray();

        var versions = await _uow.QuestionVersions.ListAsync(ct);
        var maps = await _uow.QuestionMisconceptionMaps.ListAsync(ct);
        var masteries = await _uow.TopicMasteries.WhereAsync(x => x.UserId == userId, ct);
        var misconceptionStates = await _uow.UserMisconceptions.WhereAsync(x => x.UserId == userId, ct);
        var exposures = await _uow.QuestionExposures.WhereAsync(x => x.UserId == userId, ct);
        var answered = await _uow.DiagnosticAnswers.WhereAsync(
            x => x.UserId == userId && x.SessionId == sessionId,
            ct);

        var answeredQuestionIds = answered.Select(x => x.QuestionId).ToHashSet();
        var masteryByTopic = masteries.ToDictionary(x => x.TopicId, x => x.Mastery);
        var confidenceByMisconception = misconceptionStates.ToDictionary(x => x.MisconceptionId, x => x.Confidence);
        var exposureByQuestion = exposures.ToDictionary(x => x.QuestionId);
        var questionById = questions.ToDictionary(x => x.Id);
        var answeredByTopic = answered
            .Where(x => questionById.ContainsKey(x.QuestionId))
            .GroupBy(x => questionById[x.QuestionId].TopicId)
            .ToDictionary(x => x.Key, x => x.Count());

        var topicIds = questions.Select(x => x.TopicId).Distinct().ToArray();
        var minimumPerTopic = session.PlannedQuestionCount >= topicIds.Length * 2 ? 2 : 1;
        var coverageTargets = topicIds.Select(topicId =>
        {
            var mastery = masteryByTopic.TryGetValue(topicId, out var value) ? value : 0.5d;
            var mappedMisconceptions = questions
                .Where(x => x.TopicId == topicId)
                .SelectMany(question => maps.Where(map => map.QuestionId == question.Id))
                .Select(map => map.MisconceptionId)
                .Distinct();
            var confidence = mappedMisconceptions
                .Select(id => confidenceByMisconception.TryGetValue(id, out var value) ? value : 0d)
                .DefaultIfEmpty(0d)
                .Max();
            return new CoverageTarget(topicId, minimumPerTopic, 1d + (1d - mastery) + confidence);
        }).ToArray();

        var allocation = _coveragePlanner.Allocate(coverageTargets, session.PlannedQuestionCount);
        var topicsBelowTarget = allocation
            .Where(x => answeredByTopic.GetValueOrDefault(x.Key) < x.Value)
            .Select(x => x.Key)
            .ToHashSet();

        var unansweredQuestions = questions
            .Where(x => !answeredQuestionIds.Contains(x.Id))
            .ToArray();
        var coveragePool = unansweredQuestions.Where(x => topicsBelowTarget.Contains(x.TopicId)).ToArray();
        var eligibleQuestions = coveragePool.Length > 0 ? coveragePool : unansweredQuestions;

        var candidates = new List<AdaptiveCandidate>();

        foreach (var question in eligibleQuestions)
        {
            var currentVersion = versions
                .Where(x => x.QuestionId == question.Id && x.VersionNumber == question.CurrentVersionNumber)
                .SingleOrDefault();

            if (currentVersion is null)
                continue;

            var questionMaps = maps.Where(x => x.QuestionId == question.Id).ToArray();
            var highestMisconceptionSignal = questionMaps
                .Select(x =>
                    x.RelevanceWeight *
                    (confidenceByMisconception.TryGetValue(x.MisconceptionId, out var c) ? c : 0d))
                .DefaultIfEmpty(0d)
                .Max();

            var highestRelevance = questionMaps
                .Select(x => x.RelevanceWeight)
                .DefaultIfEmpty(0d)
                .Max();

            exposureByQuestion.TryGetValue(question.Id, out var exposure);
            var topicMastery = masteryByTopic.TryGetValue(question.TopicId, out var mastery)
                ? mastery
                : 0.50d;

            var targetDifficulty = topicMastery switch
            {
                < 0.35d => 0.25d,
                < 0.55d => 0.45d,
                < 0.75d => 0.65d,
                _ => 0.82d
            };

            var actualDifficulty = currentVersion.Difficulty switch
            {
                QuestionDifficulty.Introductory => 0.18d,
                QuestionDifficulty.Basic => 0.32d,
                QuestionDifficulty.Intermediate => 0.52d,
                QuestionDifficulty.Advanced => 0.75d,
                QuestionDifficulty.Transfer => 0.90d,
                _ => 0.5d
            };

            candidates.Add(new AdaptiveCandidate(
                question.Id,
                question.TopicId,
                topicMastery,
                highestRelevance,
                highestMisconceptionSignal,
                exposure?.LastSeenAt,
                exposure?.ExposureCount ?? 0,
                Math.Abs(targetDifficulty - actualDifficulty),
                currentVersion.IsTransferQuestion));
        }

        var ranking = _selector.Rank(candidates, _clock.UtcNow, take: 10);
        var best = ranking.FirstOrDefault();

        if (best is null)
            return null;

        var selected = questions.Single(x => x.Id == best.QuestionId);
        var version = versions.Single(x =>
            x.QuestionId == selected.Id &&
            x.VersionNumber == selected.CurrentVersionNumber);

        var topic = await _uow.Topics.GetByIdAsync(selected.TopicId, ct)
            ?? throw new InvalidOperationException("Question topic does not exist.");

        var optionEntities = await _uow.AnswerOptions.WhereAsync(
            x => x.QuestionVersionId == version.Id,
            ct);
        var options = AnswerOptionOrdering.ForSession(optionEntities, session.Id, selected.Id);

        return new DiagnosticQuestionDto(
            selected.Id,
            version.Id,
            topic.Code,
            topic.NameRu,
            version.Prompt,
            version.Difficulty,
            version.IsTransferQuestion,
            options,
            best.Explanation);
    }

    public async Task<AnswerFeedbackDto> SubmitAsync(
        Guid userId,
        SubmitDiagnosticAnswerRequest request,
        CancellationToken ct = default)
    {
        var session = await GetOwnedSessionAsync(userId, request.SessionId, ct);

        if (session.Status != DiagnosticStatus.InProgress)
            throw new InvalidOperationException("Diagnostic session is not in progress.");

        var duplicate = await _uow.DiagnosticAnswers.AnyAsync(
            x => x.SessionId == request.SessionId && x.QuestionId == request.QuestionId,
            ct);

        if (duplicate)
            throw new InvalidOperationException("This question has already been answered in the session.");

        var question = await _uow.Questions.GetByIdAsync(request.QuestionId, ct)
            ?? throw new KeyNotFoundException("Question not found.");

        var version = await _uow.QuestionVersions.GetByIdAsync(request.QuestionVersionId, ct)
            ?? throw new KeyNotFoundException("Question version not found.");

        if (version.QuestionId != question.Id)
            throw new InvalidOperationException("Question version does not belong to the question.");

        var option = await _uow.AnswerOptions.GetByIdAsync(request.AnswerOptionId, ct)
            ?? throw new KeyNotFoundException("Answer option not found.");

        if (option.QuestionVersionId != version.Id)
            throw new InvalidOperationException("Answer option does not belong to the selected version.");

        var answer = new DiagnosticAnswer
        {
            SessionId = session.Id,
            UserId = userId,
            QuestionId = question.Id,
            QuestionVersionId = version.Id,
            AnswerOptionId = option.Id,
            IsCorrect = option.IsCorrect,
            ResponseTimeMs = Math.Max(0, request.ResponseTimeMs),
            SequenceNumber = session.AnsweredQuestionCount + 1,
            SubmittedAt = _clock.UtcNow
        };

        await _uow.DiagnosticAnswers.AddAsync(answer, ct);

        Guid? affectedMisconceptionId = null;

        if (!option.IsCorrect && option.MisconceptionId.HasValue)
        {
            affectedMisconceptionId = option.MisconceptionId.Value;
            await AddEvidenceAsync(
                userId,
                option.MisconceptionId.Value,
                answer.Id,
                EvidenceKind.Distractor,
                1d,
                option.Feedback,
                ct);
        }
        else if (option.IsCorrect)
        {
            var tested = await _uow.QuestionMisconceptionMaps.WhereAsync(
                x => x.QuestionId == question.Id && x.CanDisconfirm,
                ct);

            foreach (var map in tested)
            {
                affectedMisconceptionId ??= map.MisconceptionId;
                await AddEvidenceAsync(
                    userId,
                    map.MisconceptionId,
                    answer.Id,
                    version.IsTransferQuestion ? EvidenceKind.TransferSuccess : EvidenceKind.CorrectAnswer,
                    version.IsTransferQuestion ? -1.15d : -0.55d,
                    "Правильный ответ снижает уверенность в наличии связанного заблуждения.",
                    ct,
                    map.RelevanceWeight);
            }
        }

        session.AnsweredQuestionCount++;
        session.Touch();
        _uow.DiagnosticSessions.Update(session);

        await RegisterExposureAsync(userId, question.Id, option.IsCorrect, ct);

        await _uow.ActivityEvents.AddAsync(new ActivityEvent
        {
            UserId = userId,
            EventType = ActivityEventType.AnswerSubmitted,
            AggregateType = nameof(DiagnosticSession),
            AggregateId = session.Id,
            PayloadJson = JsonSerializer.Serialize(new
            {
                questionId = question.Id,
                answerOptionId = option.Id,
                correct = option.IsCorrect,
                responseTimeMs = answer.ResponseTimeMs
            })
        }, ct);

        await _uow.SaveChangesAsync(ct);

        if (affectedMisconceptionId.HasValue)
            await _learner.RecalculateMisconceptionAsync(userId, affectedMisconceptionId.Value, ct);

        await _learner.RecalculateTopicAsync(userId, question.TopicId, ct);

        UserMisconception? state = null;
        if (affectedMisconceptionId.HasValue)
        {
            state = (await _uow.UserMisconceptions.WhereAsync(
                x => x.UserId == userId && x.MisconceptionId == affectedMisconceptionId.Value,
                ct)).SingleOrDefault();
        }

        var correctOption = (await _uow.AnswerOptions.WhereAsync(
            x => x.QuestionVersionId == version.Id && x.IsCorrect,
            ct)).Single();

        return new AnswerFeedbackDto(
            option.IsCorrect,
            option.Feedback,
            version.CorrectExplanation,
            affectedMisconceptionId.HasValue
                ? (await _uow.Misconceptions.GetByIdAsync(affectedMisconceptionId.Value, ct))?.Code
                : null,
            state?.Confidence,
            state?.Status);
    }

    public async Task<DiagnosticReportDto> CompleteAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken ct = default)
    {
        var session = await GetOwnedSessionAsync(userId, sessionId, ct);

        if (session.Status is DiagnosticStatus.ReportReady or DiagnosticStatus.Completed)
            return await ReportAsync(userId, sessionId, ct);

        if (session.AnsweredQuestionCount < Math.Min(5, session.PlannedQuestionCount))
            throw new InvalidOperationException("Not enough answers to complete diagnostic.");

        session.Status = DiagnosticStatus.Analyzing;
        session.CompletedAt = _clock.UtcNow;
        session.Touch();
        _uow.DiagnosticSessions.Update(session);
        await _uow.SaveChangesAsync(ct);

        await _learner.RecalculateAllAsync(userId, ct);
        await _paths.RebuildAsync(userId, "Завершена диагностика", ct);
        await _recommendations.RebuildAsync(userId, ct);

        var answers = await _uow.DiagnosticAnswers.WhereAsync(
            x => x.SessionId == sessionId && x.UserId == userId,
            ct);

        var correct = answers.Count(x => x.IsCorrect);
        var accuracy = answers.Count == 0 ? 0d : correct / (double)answers.Count;

        session.OverallScore = accuracy;
        session.Status = DiagnosticStatus.ReportReady;
        session.Touch();
        _uow.DiagnosticSessions.Update(session);

        var topics = await _learner.GetTopicMasteryAsync(userId, ct);
        var misconceptions = await _learner.ListMisconceptionsAsync(userId, ct);
        var detected = misconceptions.Count(x =>
            x.Status is MisconceptionStatus.Detected
                or MisconceptionStatus.CorrectionInProgress
                or MisconceptionStatus.RecheckRequired);

        var strongest = topics.OrderByDescending(x => x.Mastery).FirstOrDefault();
        var weakest = topics.OrderBy(x => x.Mastery).FirstOrDefault();

        var summary =
            $"Диагностика завершена: правильных ответов {correct} из {answers.Count} ({accuracy:P0}). " +
            $"Наиболее сильная тема: {strongest?.Name ?? "н/д"}; " +
            $"наиболее приоритетная для повторения: {weakest?.Name ?? "н/д"}. " +
            $"Активных устойчивых заблуждений: {detected}.";

        var report = new DiagnosticReport
        {
            SessionId = session.Id,
            UserId = userId,
            Accuracy = accuracy,
            SummaryMarkdown = summary,
            StrongestTopicCode = strongest?.Code ?? string.Empty,
            WeakestTopicCode = weakest?.Code ?? string.Empty,
            DetectedMisconceptionCount = detected,
            GeneratedAt = _clock.UtcNow
        };

        await _uow.DiagnosticReports.AddAsync(report, ct);
        await _uow.ActivityEvents.AddAsync(new ActivityEvent
        {
            UserId = userId,
            EventType = ActivityEventType.DiagnosticCompleted,
            AggregateType = nameof(DiagnosticSession),
            AggregateId = session.Id,
            PayloadJson = JsonSerializer.Serialize(new { accuracy, detected })
        }, ct);

        await _uow.Notifications.AddAsync(new Notification
        {
            UserId = userId,
            Type = NotificationType.DiagnosticReady,
            Title = "Диагностика завершена",
            Body = summary
        }, ct);

        await _uow.SaveChangesAsync(ct);

        await _jobs.EnqueueAsync(new BackgroundJob(
            "analytics.rebuild-user",
            userId,
            session.Id,
            JsonSerializer.Serialize(new { sessionId }),
            Guid.NewGuid().ToString("N")), ct);

        return new DiagnosticReportDto(
            session.Id,
            accuracy,
            answers.Count,
            correct,
            topics,
            misconceptions,
            summary,
            report.GeneratedAt);
    }

    public async Task<DiagnosticReportDto> ReportAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken ct = default)
    {
        var session = await GetOwnedSessionAsync(userId, sessionId, ct);
        var reports = await _uow.DiagnosticReports.WhereAsync(
            x => x.UserId == userId && x.SessionId == sessionId,
            ct);

        var report = reports.OrderByDescending(x => x.GeneratedAt).FirstOrDefault();
        if (report is null)
            return await CompleteAsync(userId, sessionId, ct);

        var answers = await _uow.DiagnosticAnswers.WhereAsync(
            x => x.SessionId == sessionId && x.UserId == userId,
            ct);

        var topics = await _learner.GetTopicMasteryAsync(userId, ct);
        var misconceptions = await _learner.ListMisconceptionsAsync(userId, ct);

        return new DiagnosticReportDto(
            session.Id,
            report.Accuracy,
            answers.Count,
            answers.Count(x => x.IsCorrect),
            topics,
            misconceptions,
            report.SummaryMarkdown,
            report.GeneratedAt);
    }

    private async Task<DiagnosticSession> GetOwnedSessionAsync(
        Guid userId,
        Guid sessionId,
        CancellationToken ct)
    {
        var session = await _uow.DiagnosticSessions.GetByIdAsync(sessionId, ct)
            ?? throw new KeyNotFoundException("Diagnostic session not found.");

        if (session.UserId != userId)
            throw new UnauthorizedAccessException("Diagnostic session belongs to another user.");

        return session;
    }

    private async Task AddEvidenceAsync(
        Guid userId,
        Guid misconceptionId,
        Guid answerId,
        EvidenceKind kind,
        double rawWeight,
        string explanation,
        CancellationToken ct,
        double relevanceWeight = 1d)
    {
        await _uow.MisconceptionEvidence.AddAsync(new MisconceptionEvidence
        {
            UserId = userId,
            MisconceptionId = misconceptionId,
            DiagnosticAnswerId = answerId,
            Kind = kind,
            RawWeight = rawWeight,
            RelevanceWeight = relevanceWeight,
            Explanation = explanation,
            ObservedAt = _clock.UtcNow
        }, ct);
    }

    private async Task RegisterExposureAsync(
        Guid userId,
        Guid questionId,
        bool wasCorrect,
        CancellationToken ct)
    {
        var records = await _uow.QuestionExposures.WhereAsync(
            x => x.UserId == userId && x.QuestionId == questionId,
            ct);

        var exposure = records.SingleOrDefault();
        var isNew = exposure is null;
        if (exposure is null)
        {
            exposure = new QuestionExposure
            {
                UserId = userId,
                QuestionId = questionId,
                ExposureCount = 0
            };
            await _uow.QuestionExposures.AddAsync(exposure, ct);
        }

        exposure.ExposureCount++;
        exposure.LastSeenAt = _clock.UtcNow;
        exposure.NextEligibleAt = _clock.UtcNow.AddHours(
            wasCorrect ? Math.Min(336, 6 * exposure.ExposureCount) : 2);
        exposure.Touch();
        if (!isNew)
            _uow.QuestionExposures.Update(exposure);
    }

    private static DiagnosticSessionDto Map(DiagnosticSession session) =>
        new(
            session.Id,
            session.Status,
            session.PlannedQuestionCount,
            session.AnsweredQuestionCount,
            session.StartedAt,
            session.CompletedAt,
            session.OverallScore);
}
