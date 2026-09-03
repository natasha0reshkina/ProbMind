using ProbMind.Application.Abstractions.Clock;
using ProbMind.Application.Abstractions.Persistence;
using ProbMind.Application.Contracts;
using ProbMind.Domain.Analytics;
using ProbMind.Domain.Entities;
using ProbMind.Domain.Enums;
using ProbMind.Domain.Psychometrics;

namespace ProbMind.Application.Services;

public sealed class AdvancedAnalyticsService : IAdvancedAnalyticsService
{
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;
    private readonly ItemResponseTheoryModel _irt;
    private readonly MasteryForecastEngine _forecast;
    private readonly CohortBenchmarkEngine _benchmark;
    private readonly StudentRiskScorer _risk;
    private readonly MisconceptionClusterer _clusterer;
    private readonly ContentDriftAnalyzer _drift;

    public AdvancedAnalyticsService(
        IUnitOfWork uow,
        IClock clock,
        ItemResponseTheoryModel irt,
        MasteryForecastEngine forecast,
        CohortBenchmarkEngine benchmark,
        StudentRiskScorer risk,
        MisconceptionClusterer clusterer,
        ContentDriftAnalyzer drift)
    {
        _uow = uow;
        _clock = clock;
        _irt = irt;
        _forecast = forecast;
        _benchmark = benchmark;
        _risk = risk;
        _clusterer = clusterer;
        _drift = drift;
    }

    public async Task<IReadOnlyList<PsychometricItemDto>> PsychometricItemsAsync(
        Guid? topicId = null,
        CancellationToken ct = default)
    {
        var questions = (await _uow.Questions.ListAsync(ct))
            .Where(x => x.Status == ContentStatus.Published && (topicId is null || x.TopicId == topicId))
            .ToArray();
        var versions = await _uow.QuestionVersions.ListAsync(ct);
        var answers = await _uow.DiagnosticAnswers.ListAsync(ct);
        var topics = await _uow.Topics.ListAsync(ct);
        var topicById = topics.ToDictionary(x => x.Id);
        var result = new List<PsychometricItemDto>(questions.Length);

        foreach (var question in questions)
        {
            var itemAnswers = answers.Where(x => x.QuestionId == question.Id).ToArray();
            var currentVersion = versions.FirstOrDefault(x =>
                x.QuestionId == question.Id && x.VersionNumber == question.CurrentVersionNumber);
            if (currentVersion is null)
                continue;

            var responseCount = itemAnswers.Length;
            var correctRate = responseCount == 0 ? .5d : itemAnswers.Count(x => x.IsCorrect) / (double)responseCount;
            var difficulty = EstimateDifficulty(correctRate);
            var discrimination = EstimateDiscrimination(itemAnswers, answers);
            var information = _irt.Information(0d, new IrtItem(question.Id, difficulty, discrimination));
            var medianSeconds = Median(itemAnswers.Select(x => x.ResponseTimeMs / 1000d));
            var quality = QualityBand(responseCount, correctRate, discrimination, medianSeconds);
            var topicCode = topicById.TryGetValue(question.TopicId, out var topic) ? topic.Code : "unknown";

            result.Add(new PsychometricItemDto(
                question.Id,
                question.Code,
                topicCode,
                responseCount,
                correctRate,
                difficulty,
                discrimination,
                information,
                medianSeconds,
                quality));
        }

        return result
            .OrderByDescending(x => x.Responses)
            .ThenBy(x => x.Code)
            .ToArray();
    }

    public async Task<LearnerForecastDto> LearnerForecastAsync(Guid userId, CancellationToken ct = default)
    {
        var topics = (await _uow.Topics.ListAsync(ct)).OrderBy(x => x.SortOrder).ToArray();
        var current = await _uow.TopicMasteries.WhereAsync(x => x.UserId == userId, ct);
        var history = await _uow.TopicMasteryHistory.WhereAsync(x => x.UserId == userId, ct);
        var currentByTopic = current.ToDictionary(x => x.TopicId);
        var rows = new List<LearnerForecastTopicDto>();

        foreach (var topic in topics)
        {
            var snapshots = history
                .Where(x => x.TopicId == topic.Id)
                .OrderBy(x => x.RecordedAt)
                .Select(x => new MasterySnapshot(x.RecordedAt, x.Mastery, 1))
                .ToList();

            if (currentByTopic.TryGetValue(topic.Id, out var currentMastery) &&
                (snapshots.Count == 0 || snapshots[^1].Mastery != currentMastery.Mastery))
            {
                snapshots.Add(new MasterySnapshot(
                    currentMastery.UpdatedAt,
                    currentMastery.Mastery,
                    currentMastery.ObservationCount));
            }

            var forecast = _forecast.Forecast(snapshots, _clock.UtcNow);
            rows.Add(new LearnerForecastTopicDto(
                topic.Id,
                topic.Code,
                topic.NameRu,
                forecast.Current,
                forecast.Forecast7Days,
                forecast.Forecast30Days,
                forecast.DailyTrend,
                forecast.Confidence,
                forecast.Direction));
        }

        var meanCurrent = rows.Count == 0 ? 0d : rows.Average(x => x.CurrentMastery);
        var meanFuture = rows.Count == 0 ? 0d : rows.Average(x => x.Forecast30Days);
        var overall = (meanFuture - meanCurrent) switch
        {
            > .04d => "improving",
            < -.04d => "declining",
            _ => "stable"
        };
        return new LearnerForecastDto(userId, rows, meanCurrent, meanFuture, overall);
    }

    public async Task<IReadOnlyList<CohortBenchmarkDto>> CohortBenchmarksAsync(CancellationToken ct = default)
    {
        var users = (await _uow.Users.ListAsync(ct))
            .Where(x => x.Role == UserRole.Student && x.IsActive)
            .ToArray();
        var masteries = await _uow.TopicMasteries.ListAsync(ct);
        var misconceptions = await _uow.UserMisconceptions.ListAsync(ct);
        var observations = users.Select(user =>
        {
            var userMastery = masteries.Where(x => x.UserId == user.Id).ToArray();
            var meanMastery = userMastery.Length == 0 ? .5d : userMastery.Average(x => x.Mastery);
            var active = misconceptions.Count(x => x.UserId == user.Id && IsActive(x.Status));
            return new BenchmarkObservation(user.Id, meanMastery, active, meanMastery, 0);
        }).ToArray();

        return users.Select(user =>
        {
            var observation = observations.Single(x => x.UserId == user.Id);
            var benchmark = _benchmark.Compare(user.Id, observations);
            return new CohortBenchmarkDto(
                user.Id,
                user.DisplayName,
                observation.Mastery,
                benchmark.Percentile,
                benchmark.MasteryZScore,
                benchmark.Band,
                observation.ActiveMisconceptions);
        })
        .OrderByDescending(x => x.Percentile)
        .ToArray();
    }

    public async Task<IReadOnlyList<StudentRiskDto>> RiskRosterAsync(CancellationToken ct = default)
    {
        var users = (await _uow.Users.ListAsync(ct))
            .Where(x => x.Role == UserRole.Student && x.IsActive)
            .ToArray();
        var masteries = await _uow.TopicMasteries.ListAsync(ct);
        var masteryHistory = await _uow.TopicMasteryHistory.ListAsync(ct);
        var misconceptions = await _uow.UserMisconceptions.ListAsync(ct);
        var sessions = await _uow.DiagnosticSessions.ListAsync(ct);
        var practice = await _uow.PracticeSessions.ListAsync(ct);
        var practiceAttempts = await _uow.PracticeAttempts.ListAsync(ct);
        var events = await _uow.ActivityEvents.ListAsync(ct);
        var result = new List<StudentRiskDto>();

        foreach (var user in users)
        {
            var userMasteries = masteries.Where(x => x.UserId == user.Id).ToArray();
            var meanMastery = userMasteries.Length == 0 ? .5d : userMasteries.Average(x => x.Mastery);
            var trend = RecentTrend(user.Id, masteryHistory);
            var states = misconceptions.Where(x => x.UserId == user.Id).ToArray();
            var active = states.Count(x => IsActive(x.Status));
            var critical = states.Count(x => IsActive(x.Status) && x.Confidence >= .80d);
            var lastActivity = events.Where(x => x.UserId == user.Id).OrderByDescending(x => x.CreatedAt).FirstOrDefault()?.CreatedAt;
            var daysInactive = lastActivity is null ? 30 : Math.Max(0, (int)(_clock.UtcNow - lastActivity.Value).TotalDays);
            var userDiagnostics = sessions.Where(x => x.UserId == user.Id).ToArray();
            var completed = userDiagnostics.Count(x => x.Status == DiagnosticStatus.Completed);
            var completionRate = userDiagnostics.Length == 0 ? .5d : completed / (double)userDiagnostics.Length;
            var userPractice = practice.Where(x => x.UserId == user.Id).ToArray();
            var userPracticeIds = userPractice.Select(x => x.Id).ToHashSet();
            var transferAttempts = practiceAttempts
                .Where(x => userPracticeIds.Contains(x.PracticeSessionId) && x.ExerciseType == ExerciseType.Transfer)
                .ToArray();
            var transferRate = transferAttempts.Length == 0
                ? .5d
                : transferAttempts.Count(x => x.IsCorrect) / (double)transferAttempts.Length;

            var score = _risk.Score(new StudentRiskSignal(
                meanMastery,
                trend,
                active,
                critical,
                daysInactive,
                completionRate,
                transferRate));
            result.Add(new StudentRiskDto(
                user.Id,
                user.DisplayName,
                score.Risk,
                score.Level,
                score.Drivers,
                meanMastery,
                active,
                daysInactive));
        }

        return result.OrderByDescending(x => x.Risk).ToArray();
    }

    public async Task<IReadOnlyList<MisconceptionClusterDto>> MisconceptionClustersAsync(
        int clusters = 3,
        CancellationToken ct = default)
    {
        var users = (await _uow.Users.ListAsync(ct))
            .Where(x => x.Role == UserRole.Student && x.IsActive)
            .ToArray();
        var catalog = (await _uow.Misconceptions.ListAsync(ct)).OrderBy(x => x.Code).ToArray();
        var states = await _uow.UserMisconceptions.ListAsync(ct);
        if (users.Length == 0 || catalog.Length == 0)
            return Array.Empty<MisconceptionClusterDto>();

        var vectors = users.Select(user =>
        {
            var lookup = states.Where(x => x.UserId == user.Id).ToDictionary(x => x.MisconceptionId, x => x.Confidence);
            return new MisconceptionVector(
                user.Id,
                catalog.Select(m => lookup.TryGetValue(m.Id, out var confidence) ? confidence : 0d).ToArray());
        }).ToArray();
        var result = _clusterer.Cluster(vectors, clusters);

        return result.Clusters.Select(cluster => new MisconceptionClusterDto(
            cluster.Cluster,
            cluster.Label,
            cluster.Size,
            cluster.Centroid,
            result.Assignments.Where(x => x.Cluster == cluster.Cluster).Select(x => x.UserId).ToArray()))
            .OrderByDescending(x => x.Learners)
            .ToArray();
    }

    public async Task<IReadOnlyList<ContentDriftDto>> ContentDriftAsync(CancellationToken ct = default)
    {
        var questions = await _uow.Questions.ListAsync(ct);
        var answers = await _uow.DiagnosticAnswers.ListAsync(ct);
        var cutoff = _clock.UtcNow.AddDays(-30);
        var previousCutoff = cutoff.AddDays(-60);
        var result = new List<ContentDriftDto>();

        foreach (var question in questions.Where(x => x.Status == ContentStatus.Published))
        {
            var itemAnswers = answers.Where(x => x.QuestionId == question.Id).ToArray();
            var baselineAnswers = itemAnswers.Where(x => x.SubmittedAt >= previousCutoff && x.SubmittedAt < cutoff).ToArray();
            var recentAnswers = itemAnswers.Where(x => x.SubmittedAt >= cutoff).ToArray();
            if (baselineAnswers.Length < 5 || recentAnswers.Length < 5)
                continue;

            var baseline = new[] { BuildWindow(previousCutoff, baselineAnswers) };
            var recent = new[] { BuildWindow(cutoff, recentAnswers) };
            var drift = _drift.Analyze(baseline, recent);
            result.Add(new ContentDriftDto(
                question.Id,
                question.Code,
                baselineAnswers.Length,
                recentAnswers.Length,
                drift.AccuracyShift,
                drift.TimeShift,
                drift.EntropyShift,
                drift.DriftScore,
                drift.RequiresReview,
                drift.Reason));
        }

        return result.OrderByDescending(x => x.DriftScore).ToArray();
    }

    public async Task<AdvancedSystemOverviewDto> SystemOverviewAsync(CancellationToken ct = default)
    {
        var users = await _uow.Users.ListAsync(ct);
        var students = users.Where(x => x.Role == UserRole.Student && x.IsActive).ToArray();
        var questions = await _uow.Questions.ListAsync(ct);
        var answers = await _uow.DiagnosticAnswers.ListAsync(ct);
        var states = await _uow.UserMisconceptions.ListAsync(ct);
        var masteries = await _uow.TopicMasteries.ListAsync(ct);
        var sessions = await _uow.DiagnosticSessions.ListAsync(ct);
        var risks = await RiskRosterAsync(ct);
        var drift = await ContentDriftAsync(ct);
        var completed = sessions.Where(x => x.Status == DiagnosticStatus.ReportReady && x.OverallScore.HasValue).ToArray();

        return new AdvancedSystemOverviewDto(
            students.Length,
            questions.Count(x => x.Status == ContentStatus.Published),
            answers.Count,
            states.Count(x => IsActive(x.Status)),
            risks.Count(x => x.Risk >= .55d),
            drift.Count(x => x.RequiresReview),
            masteries.Count == 0 ? 0d : masteries.Average(x => x.Mastery),
            completed.Length == 0 ? 0d : completed.Average(x => x.OverallScore!.Value),
            _clock.UtcNow);
    }

    private static string QualityBand(int responses, double correctRate, double discrimination, double medianSeconds)
    {
        if (responses == 0)
            return "no_data";
        if (responses < 30)
            return "insufficient_sample";

        if (correctRate < .10d || correctRate > .95d || medianSeconds < 1d || medianSeconds > 600d)
            return "review";

        return discrimination switch
        {
            >= 1.20d => "excellent",
            >= .90d => "good",
            >= .55d => "acceptable",
            _ => "review"
        };
    }

    private static double EstimateDifficulty(double correctRate)
    {
        var p = Math.Clamp(correctRate, .03d, .97d);
        return Math.Clamp(Math.Log((1d - p) / p), -3d, 3d);
    }

    private static double EstimateDiscrimination(
        IReadOnlyCollection<DiagnosticAnswer> itemAnswers,
        IReadOnlyCollection<DiagnosticAnswer> allAnswers)
    {
        if (itemAnswers.Count < 8)
            return .8d;
        var userAccuracy = allAnswers
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Count(x => x.IsCorrect) / (double)g.Count());
        var high = itemAnswers.Where(x => userAccuracy.TryGetValue(x.UserId, out var a) && a >= .65d).ToArray();
        var low = itemAnswers.Where(x => userAccuracy.TryGetValue(x.UserId, out var a) && a < .65d).ToArray();
        if (high.Length == 0 || low.Length == 0)
            return .8d;
        var highRate = high.Count(x => x.IsCorrect) / (double)high.Length;
        var lowRate = low.Count(x => x.IsCorrect) / (double)low.Length;
        return Math.Clamp(.5d + (highRate - lowRate) * 2d, .2d, 2.5d);
    }

    private static double RecentTrend(Guid userId, IReadOnlyCollection<TopicMasteryHistory> history)
    {
        var recent = history
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.RecordedAt)
            .Take(12)
            .OrderBy(x => x.RecordedAt)
            .ToArray();
        if (recent.Length < 2) return 0d;
        return (recent[^1].Mastery - recent[0].Mastery) / Math.Max(1d, (recent[^1].RecordedAt - recent[0].RecordedAt).TotalDays);
    }

    private static ContentWindowMetric BuildWindow(DateTimeOffset start, IReadOnlyCollection<DiagnosticAnswer> answers)
    {
        var correctRate = answers.Count == 0 ? 0d : answers.Count(x => x.IsCorrect) / (double)answers.Count;
        var times = answers.Select(x => x.ResponseTimeMs / 1000d).ToArray();
        var entropy = correctRate is <= 0d or >= 1d
            ? 0d
            : -(correctRate * Math.Log(correctRate) + (1d - correctRate) * Math.Log(1d - correctRate));
        return new ContentWindowMetric(start, answers.Count, correctRate, Median(times), entropy);
    }

    private static double Median(IEnumerable<double> values)
    {
        var ordered = values.OrderBy(x => x).ToArray();
        if (ordered.Length == 0) return 0d;
        var mid = ordered.Length / 2;
        return ordered.Length % 2 == 0 ? (ordered[mid - 1] + ordered[mid]) / 2d : ordered[mid];
    }

    private static bool IsActive(MisconceptionStatus status) => status is
        MisconceptionStatus.Suspected or
        MisconceptionStatus.Detected or
        MisconceptionStatus.CorrectionInProgress or
        MisconceptionStatus.RecheckRequired;
}
