using ProbMind.Application.Abstractions.Cache;
using ProbMind.Application.Abstractions.Clock;
using ProbMind.Application.Abstractions.Persistence;
using ProbMind.Application.Contracts;
using ProbMind.Domain.Enums;

namespace ProbMind.Application.Services;

public sealed class StatisticsService : IStatisticsService
{
    private readonly IUnitOfWork _uow;
    private readonly IApplicationCache _cache;
    private readonly IClock _clock;
    private readonly ILearnerModelService _learner;
    private readonly IRecommendationService _recommendations;

    public StatisticsService(
        IUnitOfWork uow,
        IApplicationCache cache,
        IClock clock,
        ILearnerModelService learner,
        IRecommendationService recommendations)
    {
        _uow = uow;
        _cache = cache;
        _clock = clock;
        _learner = learner;
        _recommendations = recommendations;
    }

    public async Task<DashboardDto> DashboardAsync(Guid userId, CancellationToken ct = default)
    {
        var topics = await _learner.GetTopicMasteryAsync(userId, ct);
        var misconceptions = await _learner.ListMisconceptionsAsync(userId, ct);
        var diagnostics = await _uow.DiagnosticSessions.WhereAsync(x => x.UserId == userId, ct);
        var practice = await _uow.PracticeSessions.WhereAsync(x => x.UserId == userId, ct);
        var recommendations = await _recommendations.ListAsync(userId, includeDismissed: false, ct);

        var observedTopics = topics.Where(x => x.ObservationCount > 0).ToArray();
        var overall = observedTopics.Length == 0 ? 0d : observedTopics.Average(x => x.Mastery);
        var active = misconceptions.Count(x =>
            x.Status is MisconceptionStatus.Suspected
                or MisconceptionStatus.Detected
                or MisconceptionStatus.CorrectionInProgress
                or MisconceptionStatus.RecheckRequired);
        var corrected = misconceptions.Count(x => x.Status == MisconceptionStatus.Corrected);

        return new DashboardDto(
            overall,
            active,
            corrected,
            diagnostics.Count(x => x.Status == DiagnosticStatus.ReportReady),
            practice.Count(x => x.Status == PracticeStatus.Completed),
            topics,
            recommendations.Take(8).ToArray());
    }

    public async Task<IReadOnlyList<TimelinePoint>> MasteryTimelineAsync(
        Guid userId,
        Guid topicId,
        CancellationToken ct = default)
    {
        var history = await _uow.TopicMasteryHistory.WhereAsync(
            x => x.UserId == userId && x.TopicId == topicId,
            ct);

        return history
            .OrderBy(x => x.RecordedAt)
            .Select(x => new TimelinePoint(x.RecordedAt, x.Mastery, x.Reason))
            .ToArray();
    }

    public async Task<IReadOnlyList<TimelinePoint>> MisconceptionTimelineAsync(
        Guid userId,
        Guid misconceptionId,
        CancellationToken ct = default)
    {
        var evidence = await _uow.MisconceptionEvidence.WhereAsync(
            x => x.UserId == userId && x.MisconceptionId == misconceptionId,
            ct);

        var sorted = evidence.OrderBy(x => x.ObservedAt).ToArray();
        var points = new List<TimelinePoint>();
        var support = 0d;
        var contradiction = 0d;

        foreach (var item in sorted)
        {
            if (item.RawWeight >= 0d)
                support += item.RawWeight * item.RelevanceWeight;
            else
                contradiction += Math.Abs(item.RawWeight * item.RelevanceWeight);

            var total = support + contradiction;
            var proxy = total <= 0d ? 0d : support / total;
            points.Add(new TimelinePoint(
                item.ObservedAt,
                Math.Clamp(proxy, 0d, 1d),
                item.Kind.ToString()));
        }

        return points;
    }

    public async Task<CohortAnalyticsDto> CohortAsync(CancellationToken ct = default)
    {
        const string key = "analytics:cohort:v3";
        var cached = await _cache.GetAsync<CohortAnalyticsDto>(key, ct);
        if (cached is not null)
            return cached;

        var users = await _uow.Users.WhereAsync(x => x.Role == UserRole.Student && x.IsActive, ct);
        var diagnostics = await _uow.DiagnosticSessions.ListAsync(ct);
        var answers = await _uow.DiagnosticAnswers.ListAsync(ct);
        var topicStates = await _uow.TopicMasteries.ListAsync(ct);
        var topics = await _uow.Topics.ListAsync(ct);

        var studentsWithResults = users.Count(user => answers.Any(answer => answer.UserId == user.Id));
        var prevalence = await BuildPrevalenceAsync(studentsWithResults, ct);
        var meanTopic = topics
            .OrderBy(x => x.SortOrder)
            .Select(topic =>
            {
                var states = topicStates.Where(x => x.TopicId == topic.Id).ToArray();
                return new TopicProgressDto(
                    topic.Id,
                    topic.Code,
                    topic.NameRu,
                    states.Length == 0 ? 0.5d : states.Average(x => x.Mastery),
                    states.Length == 0 ? 1d : states.Average(x => x.Uncertainty),
                    states.Sum(x => x.ObservationCount));
            })
            .ToArray();

        var completed = diagnostics
            .Where(x => x.Status == DiagnosticStatus.ReportReady && x.OverallScore.HasValue)
            .ToArray();

        var result = new CohortAnalyticsDto(
            users.Count,
            completed.Length,
            answers.Count,
            completed.Length == 0 ? 0d : completed.Average(x => x.OverallScore!.Value),
            prevalence,
            meanTopic);

        await _cache.SetAsync(key, result, TimeSpan.FromMinutes(10), ct);
        return result;
    }

    public async Task<IReadOnlyList<MisconceptionPrevalenceDto>> PrevalenceAsync(
        CancellationToken ct = default)
    {
        var students = await _uow.Users.WhereAsync(
            x => x.Role == UserRole.Student && x.IsActive,
            ct);
        var answers = await _uow.DiagnosticAnswers.ListAsync(ct);
        var studentsWithResults = students.Count(user => answers.Any(answer => answer.UserId == user.Id));
        return await BuildPrevalenceAsync(studentsWithResults, ct);
    }

    public async Task<SystemMetricsDto> SystemMetricsAsync(CancellationToken ct = default)
    {
        var users = await _uow.Users.ListAsync(ct);
        var questions = await _uow.Questions.ListAsync(ct);
        var diagnostics = await _uow.DiagnosticSessions.CountAsync(cancellationToken: ct);
        var practice = await _uow.PracticeSessions.CountAsync(cancellationToken: ct);
        var evidence = await _uow.MisconceptionEvidence.CountAsync(cancellationToken: ct);

        return new SystemMetricsDto(
            users.Count,
            users.Count(x => x.Role == UserRole.Student),
            users.Count(x => x.Role == UserRole.Teacher),
            questions.Count,
            questions.Count(x => x.Status == ContentStatus.Published),
            diagnostics,
            practice,
            evidence,
            _clock.UtcNow);
    }

    public async Task<DiagnosticComparisonDto> CompareDiagnosticsAsync(
        Guid userId,
        Guid fromSessionId,
        Guid toSessionId,
        CancellationToken ct = default)
    {
        if (fromSessionId == toSessionId)
            throw new ArgumentException("Для сравнения нужно выбрать две разные диагностики.");

        var reports = await _uow.DiagnosticReports.WhereAsync(
            x => x.UserId == userId && (x.SessionId == fromSessionId || x.SessionId == toSessionId),
            ct);

        var fromReport = reports.SingleOrDefault(x => x.SessionId == fromSessionId)
            ?? throw new KeyNotFoundException("Первая диагностика не найдена или ещё не завершена.");
        var toReport = reports.SingleOrDefault(x => x.SessionId == toSessionId)
            ?? throw new KeyNotFoundException("Вторая диагностика не найдена или ещё не завершена.");

        if (fromReport.GeneratedAt > toReport.GeneratedAt)
            (fromReport, toReport) = (toReport, fromReport);

        var topics = (await _uow.Topics.ListAsync(ct))
            .OrderBy(x => x.SortOrder)
            .ToArray();
        var history = await _uow.TopicMasteryHistory.WhereAsync(x => x.UserId == userId, ct);

        double? MasteryAt(Guid topicId, DateTimeOffset at)
        {
            return history
                .Where(x => x.TopicId == topicId && x.RecordedAt <= at)
                .OrderByDescending(x => x.RecordedAt)
                .Select(x => (double?)x.Mastery)
                .FirstOrDefault();
        }

        var topicRows = topics.Select(topic =>
        {
            var from = MasteryAt(topic.Id, fromReport.GeneratedAt);
            var to = MasteryAt(topic.Id, toReport.GeneratedAt);
            return new TopicComparisonDto(
                topic.Id,
                topic.Code,
                topic.NameRu,
                from,
                to,
                from.HasValue && to.HasValue ? to.Value - from.Value : (double?)null);
        }).ToArray();

        return new DiagnosticComparisonDto(
            new DiagnosticComparisonEndpointDto(
                fromReport.SessionId,
                fromReport.GeneratedAt,
                fromReport.Accuracy,
                fromReport.DetectedMisconceptionCount),
            new DiagnosticComparisonEndpointDto(
                toReport.SessionId,
                toReport.GeneratedAt,
                toReport.Accuracy,
                toReport.DetectedMisconceptionCount),
            toReport.Accuracy - fromReport.Accuracy,
            toReport.DetectedMisconceptionCount - fromReport.DetectedMisconceptionCount,
            topicRows);
    }

    private async Task<IReadOnlyList<MisconceptionPrevalenceDto>> BuildPrevalenceAsync(
        int totalStudents,
        CancellationToken ct)
    {
        var catalog = await _uow.Misconceptions.ListAsync(ct);
        var states = await _uow.UserMisconceptions.ListAsync(ct);
        var evidence = await _uow.MisconceptionEvidence.ListAsync(ct);

        return catalog
            .Select(mc =>
            {
                var activeStates = states.Where(x =>
                    x.MisconceptionId == mc.Id &&
                    x.Status is MisconceptionStatus.Suspected
                        or MisconceptionStatus.Detected
                        or MisconceptionStatus.CorrectionInProgress
                        or MisconceptionStatus.RecheckRequired)
                    .ToArray();

                var evidenceCount = evidence.Count(x => x.MisconceptionId == mc.Id);
                var affected = activeStates.Select(x => x.UserId).Distinct().Count();
                var prevalence = totalStudents == 0 ? 0d : affected / (double)totalStudents;

                return new MisconceptionPrevalenceDto(
                    mc.Id,
                    mc.Code,
                    mc.Title,
                    affected,
                    totalStudents,
                    prevalence,
                    activeStates.Length == 0 ? 0d : activeStates.Average(x => x.Confidence),
                    evidenceCount);
            })
            .OrderByDescending(x => x.Prevalence)
            .ThenByDescending(x => x.MeanConfidence)
            .ToArray();
    }
}
