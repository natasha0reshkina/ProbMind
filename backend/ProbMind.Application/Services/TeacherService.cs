using ProbMind.Application.Abstractions.Persistence;
using ProbMind.Application.Contracts;
using ProbMind.Domain.Analytics;
using ProbMind.Domain.Entities;
using ProbMind.Domain.Enums;

namespace ProbMind.Application.Services;

public sealed class TeacherService : ITeacherService
{
    private readonly IUnitOfWork _uow;
    private readonly ILearnerModelService _learner;
    private readonly ItemDifficultyEstimator _itemDifficulty;
    private readonly DistractorAnalysisEngine _distractors;
    private readonly DiagnosticReliabilityAnalyzer _reliability;
    private readonly InterventionEffectivenessAnalyzer _interventions;
    private readonly CohortSegmentationEngine _segments;

    public TeacherService(
        IUnitOfWork uow,
        ILearnerModelService learner,
        ItemDifficultyEstimator itemDifficulty,
        DistractorAnalysisEngine distractors,
        DiagnosticReliabilityAnalyzer reliability,
        InterventionEffectivenessAnalyzer interventions,
        CohortSegmentationEngine segments)
    {
        _uow = uow;
        _learner = learner;
        _itemDifficulty = itemDifficulty;
        _distractors = distractors;
        _reliability = reliability;
        _interventions = interventions;
        _segments = segments;
    }

    public async Task<IReadOnlyList<StudentListItemDto>> ListStudentsAsync(CancellationToken ct = default)
    {
        var students = await _uow.Users.WhereAsync(
            x => x.Role == UserRole.Student && x.IsActive,
            ct);
        var masteries = await _uow.TopicMasteries.ListAsync(ct);
        var misconceptions = await _uow.UserMisconceptions.ListAsync(ct);
        var misconceptionCatalog = (await _uow.Misconceptions.ListAsync(ct)).ToDictionary(x => x.Id);
        var diagnostics = await _uow.DiagnosticSessions.ListAsync(ct);
        var answers = await _uow.DiagnosticAnswers.ListAsync(ct);
        var events = await _uow.ActivityEvents.ListAsync(ct);

        return students
            .Select(student =>
            {
                var userMastery = masteries.Where(x => x.UserId == student.Id && x.ObservationCount > 0).ToArray();
                var userMisconceptions = misconceptions.Where(x => x.UserId == student.Id).ToArray();
                var userAnswers = answers.Where(x => x.UserId == student.Id).ToArray();
                var lastActivity = events
                    .Where(x => x.UserId == student.Id)
                    .OrderByDescending(x => x.OccurredAt)
                    .Select(x => (DateTimeOffset?)x.OccurredAt)
                    .FirstOrDefault();
                var activeStates = userMisconceptions
                    .Where(IsActive)
                    .OrderByDescending(x => x.Confidence)
                    .ToArray();
                var activeTitles = activeStates
                    .Where(x => misconceptionCatalog.ContainsKey(x.MisconceptionId))
                    .Select(x => misconceptionCatalog[x.MisconceptionId].Title)
                    .Distinct()
                    .ToArray();
                var answered = userAnswers.Length;
                var wrong = userAnswers.Count(x => !x.IsCorrect);
                var accuracy = answered == 0 ? 0d : userAnswers.Count(x => x.IsCorrect) / (double)answered;
                var completedDiagnostics = diagnostics.Count(x =>
                    x.UserId == student.Id && x.Status == DiagnosticStatus.ReportReady);

                return new StudentListItemDto(
                    student.Id,
                    student.DisplayName,
                    student.Email,
                    userMastery.Length == 0 ? 0d : userMastery.Average(x => x.Mastery),
                    activeStates.Length,
                    completedDiagnostics,
                    answered,
                    wrong,
                    accuracy,
                    activeTitles,
                    lastActivity);
            })
            .OrderByDescending(x => x.WrongAnswers)
            .ThenBy(x => x.DiagnosticAccuracy)
            .ThenBy(x => x.DisplayName)
            .ToArray();
    }

    public async Task<StudentOverviewDto> StudentAsync(Guid studentId, CancellationToken ct = default)
    {
        var student = await _uow.Users.GetByIdAsync(studentId, ct)
            ?? throw new KeyNotFoundException("Студент не найден.");
        if (student.Role != UserRole.Student)
            throw new InvalidOperationException("Указанная учётная запись не принадлежит студенту.");

        var topics = await _learner.GetTopicMasteryAsync(studentId, ct);
        var misconceptions = await _learner.ListMisconceptionsAsync(studentId, ct);
        var diagnostics = await _uow.DiagnosticSessions.WhereAsync(x => x.UserId == studentId, ct);
        var practice = await _uow.PracticeSessions.WhereAsync(x => x.UserId == studentId, ct);
        var events = await _uow.ActivityEvents.WhereAsync(x => x.UserId == studentId, ct);

        return new StudentOverviewDto(
            student.Id,
            student.DisplayName,
            student.Email,
            topics.Any(x => x.ObservationCount > 0)
                ? topics.Where(x => x.ObservationCount > 0).Average(x => x.Mastery)
                : 0d,
            misconceptions.Count(x => IsActiveStatus(x.Status)),
            misconceptions.Count(x => x.Status == MisconceptionStatus.Corrected),
            diagnostics.Count(x => x.Status == DiagnosticStatus.ReportReady),
            practice.Count(x => x.Status == PracticeStatus.Completed),
            events.OrderByDescending(x => x.OccurredAt).Select(x => (DateTimeOffset?)x.OccurredAt).FirstOrDefault(),
            topics,
            misconceptions);
    }

    public async Task<IReadOnlyList<StudentMistakeDto>> StudentMistakesAsync(
        Guid studentId,
        int limit = 50,
        CancellationToken ct = default)
    {
        var student = await _uow.Users.GetByIdAsync(studentId, ct)
            ?? throw new KeyNotFoundException("Студент не найден.");
        if (student.Role != UserRole.Student)
            throw new InvalidOperationException("Указанная учётная запись не принадлежит студенту.");

        var diagnostic = (await _uow.DiagnosticAnswers.WhereAsync(
                x => x.UserId == studentId && !x.IsCorrect,
                ct))
            .Select(x => (x.Id, x.SubmittedAt, x.QuestionId, x.QuestionVersionId, x.AnswerOptionId, Source: "Диагностика"));
        var practice = (await _uow.PracticeAttempts.WhereAsync(
                x => x.UserId == studentId && !x.IsCorrect,
                ct))
            .Select(x => (x.Id, x.SubmittedAt, x.QuestionId, x.QuestionVersionId, x.AnswerOptionId, Source: "Тренировка"));

        var attempts = diagnostic
            .Concat(practice)
            .OrderByDescending(x => x.SubmittedAt)
            .Take(Math.Clamp(limit, 1, 200))
            .ToArray();

        if (attempts.Length == 0)
            return Array.Empty<StudentMistakeDto>();

        var questions = (await _uow.Questions.ListAsync(ct)).ToDictionary(x => x.Id);
        var versions = (await _uow.QuestionVersions.ListAsync(ct)).ToDictionary(x => x.Id);
        var options = await _uow.AnswerOptions.ListAsync(ct);
        var optionById = options.ToDictionary(x => x.Id);
        var topics = (await _uow.Topics.ListAsync(ct)).ToDictionary(x => x.Id);
        var misconceptions = (await _uow.Misconceptions.ListAsync(ct)).ToDictionary(x => x.Id);

        var result = new List<StudentMistakeDto>();
        foreach (var attempt in attempts)
        {
            if (!questions.TryGetValue(attempt.QuestionId, out var question) ||
                !versions.TryGetValue(attempt.QuestionVersionId, out var version) ||
                !optionById.TryGetValue(attempt.AnswerOptionId, out var selected))
                continue;

            var correct = options.FirstOrDefault(x =>
                x.QuestionVersionId == attempt.QuestionVersionId && x.IsCorrect);
            var topicName = topics.TryGetValue(question.TopicId, out var topic)
                ? topic.NameRu
                : "Без темы";
            string? misconceptionTitle = null;
            if (selected.MisconceptionId.HasValue &&
                misconceptions.TryGetValue(selected.MisconceptionId.Value, out var misconception))
            {
                misconceptionTitle = misconception.Title;
            }

            result.Add(new StudentMistakeDto(
                attempt.Id,
                attempt.SubmittedAt,
                attempt.Source,
                topicName,
                version.Prompt,
                selected.Text,
                correct?.Text ?? "Правильный ответ не указан",
                misconceptionTitle));
        }

        return result;
    }

    public async Task<IReadOnlyList<QuestionAnalyticsDto>> QuestionAnalyticsAsync(
        Guid? topicId,
        CancellationToken ct = default)
    {
        var questions = await _uow.Questions.ListAsync(ct);
        var answers = await _uow.DiagnosticAnswers.ListAsync(ct);
        var attempts = await _uow.PracticeAttempts.ListAsync(ct);
        var options = await _uow.AnswerOptions.ListAsync(ct);
        var masteries = await _uow.TopicMasteries.ListAsync(ct);

        var result = new List<QuestionAnalyticsDto>();

        foreach (var question in questions
                     .Where(x => !topicId.HasValue || x.TopicId == topicId.Value)
                     .Where(x => x.Status == ContentStatus.Published))
        {
            var diagnostic = answers.Where(x => x.QuestionId == question.Id).ToArray();
            var practice = attempts.Where(x => x.QuestionId == question.Id).ToArray();
            var responseCount = diagnostic.Length + practice.Length;

            if (responseCount == 0)
            {
                result.Add(new QuestionAnalyticsDto(
                    question.Id,
                    question.Code,
                    0,
                    0d,
                    .5d,
                    0d,
                    "no_data",
                    0d,
                    0d));
                continue;
            }

            var itemResponses = diagnostic.Select(answer =>
            {
                var mastery = masteries
                    .Where(x => x.UserId == answer.UserId && x.TopicId == question.TopicId)
                    .Select(x => x.Mastery)
                    .DefaultIfEmpty(.5d)
                    .Average();

                return new ItemResponse(answer.UserId, answer.IsCorrect, mastery);
            }).ToArray();

            var item = _itemDifficulty.Estimate(itemResponses);

            var optionSelections = diagnostic.Select(answer =>
            {
                var option = options.SingleOrDefault(x => x.Id == answer.AnswerOptionId);
                var mastery = masteries
                    .Where(x => x.UserId == answer.UserId && x.TopicId == question.TopicId)
                    .Select(x => x.Mastery)
                    .DefaultIfEmpty(.5d)
                    .Average();

                return new DistractorSelection(
                    answer.AnswerOptionId,
                    option?.IsCorrect ?? answer.IsCorrect,
                    answer.UserId,
                    mastery);
            }).ToArray();

            var distractor = _distractors.Analyze(optionSelections);
            var times = diagnostic.Select(x => x.ResponseTimeMs / 1000d)
                .Concat(practice.Select(x => x.ResponseTimeMs / 1000d))
                .OrderBy(x => x)
                .ToArray();
            var median = Median(times);
            var correctCount = diagnostic.Count(x => x.IsCorrect) + practice.Count(x => x.IsCorrect);

            result.Add(new QuestionAnalyticsDto(
                question.Id,
                question.Code,
                responseCount,
                correctCount / (double)responseCount,
                item.Difficulty,
                item.Discrimination,
                item.QualityBand,
                distractor.Entropy,
                median));
        }

        return result
            .OrderByDescending(x => x.Responses)
            .ThenBy(x => x.Code)
            .ToArray();
    }

    public async Task<ReliabilityDto> DiagnosticReliabilityAsync(CancellationToken ct = default)
    {
        var sessions = await _uow.DiagnosticSessions.WhereAsync(
            x => x.Status == DiagnosticStatus.ReportReady,
            ct);
        var answers = await _uow.DiagnosticAnswers.ListAsync(ct);

        var vectors = sessions
            .Select(session =>
            {
                var scores = answers
                    .Where(x => x.SessionId == session.Id)
                    .OrderBy(x => x.SequenceNumber)
                    .Select(x => x.IsCorrect ? 1 : 0)
                    .ToArray();
                return new LearnerItemVector(session.UserId, scores);
            })
            .Where(x => x.BinaryScores.Count >= 2)
            .ToArray();

        var result = _reliability.CronbachAlpha(vectors);
        return new ReliabilityDto(
            result.CronbachAlpha,
            result.Learners,
            result.Items,
            result.Interpretation);
    }

    public async Task<IReadOnlyList<InterventionEffectivenessDto>> InterventionEffectivenessAsync(
        CancellationToken ct = default)
    {
        var catalog = await _uow.Misconceptions.ListAsync(ct);
        var practice = await _uow.PracticeSessions.WhereAsync(x => x.Status == PracticeStatus.Completed, ct);
        var attempts = await _uow.PracticeAttempts.ListAsync(ct);
        var evidence = await _uow.MisconceptionEvidence.ListAsync(ct);
        var masteries = await _uow.TopicMasteries.ListAsync(ct);

        var result = new List<InterventionEffectivenessDto>();

        foreach (var mc in catalog)
        {
            var sessions = practice.Where(x => x.MisconceptionId == mc.Id).ToArray();
            var observations = new List<InterventionObservation>();

            foreach (var session in sessions)
            {
                var sessionAttempts = attempts.Where(x => x.PracticeSessionId == session.Id).ToArray();
                var userEvidence = evidence
                    .Where(x => x.UserId == session.UserId && x.MisconceptionId == mc.Id)
                    .OrderBy(x => x.ObservedAt)
                    .ToArray();

                if (userEvidence.Length == 0)
                    continue;

                var positiveBefore = userEvidence
                    .Where(x => x.ObservedAt <= (session.StartedAt ?? session.CreatedAt))
                    .Count(x => x.RawWeight > 0d);
                var negativeAfter = userEvidence
                    .Where(x => x.ObservedAt >= (session.StartedAt ?? session.CreatedAt))
                    .Count(x => x.RawWeight < 0d);

                var beforeConfidence = Math.Clamp(.35d + positiveBefore * .10d, 0d, .95d);
                var afterConfidence = Math.Clamp(beforeConfidence - negativeAfter * .12d, 0d, 1d);
                var topicMastery = masteries
                    .Where(x => x.UserId == session.UserId && x.TopicId == mc.TopicId)
                    .Select(x => x.Mastery)
                    .DefaultIfEmpty(.5d)
                    .Average();

                observations.Add(new InterventionObservation(
                    session.UserId,
                    mc.Id,
                    beforeConfidence,
                    afterConfidence,
                    Math.Max(0d, topicMastery - .08d),
                    topicMastery,
                    sessionAttempts.Length,
                    sessionAttempts.Any(x => x.ExerciseType == ExerciseType.Transfer && x.IsCorrect)));
            }

            if (observations.Count == 0)
                continue;

            var analysis = _interventions.Analyze(observations);
            result.Add(new InterventionEffectivenessDto(
                mc.Id,
                mc.Code,
                mc.Title,
                analysis.Learners,
                analysis.MeanConfidenceReduction,
                analysis.MeanMasteryGain,
                analysis.TransferPassRate,
                analysis.MeanExercises,
                analysis.CompositeEffectiveness));
        }

        return result.OrderByDescending(x => x.CompositeEffectiveness).ToArray();
    }

    public async Task<IReadOnlyList<CohortSegmentDto>> SegmentsAsync(CancellationToken ct = default)
    {
        var students = await _uow.Users.WhereAsync(x => x.Role == UserRole.Student && x.IsActive, ct);
        var mastery = await _uow.TopicMasteries.ListAsync(ct);
        var misconceptions = await _uow.UserMisconceptions.ListAsync(ct);
        var events = await _uow.ActivityEvents.ListAsync(ct);
        var history = await _uow.TopicMasteryHistory.ListAsync(ct);

        var features = new List<LearnerFeatureVector>();
        foreach (var student in students)
        {
            var userMastery = mastery.Where(x => x.UserId == student.Id).ToArray();
            var userStates = misconceptions.Where(x => x.UserId == student.Id).ToArray();
            var userEvents = events.Where(x => x.UserId == student.Id).ToArray();
            var userHistory = history.Where(x => x.UserId == student.Id).OrderBy(x => x.RecordedAt).ToArray();

            var velocity = userHistory.Length < 2
                ? 0d
                : (userHistory.TakeLast(5).Average(x => x.Mastery) -
                   userHistory.Take(5).Average(x => x.Mastery)) / 5d;

            features.Add(new LearnerFeatureVector(
                student.Id,
                userMastery.Length == 0 ? .5d : userMastery.Average(x => x.Mastery),
                userStates.Length == 0 ? 0d : userStates.Where(IsActive).Select(x => x.Confidence).DefaultIfEmpty(0d).Average(),
                velocity,
                userMastery.Length == 0 ? 1d : userMastery.Average(x => x.Uncertainty),
                Math.Clamp(userEvents.Count(x => x.OccurredAt > DateTimeOffset.UtcNow.AddDays(-14)) / 10d, 0d, 1d)));
        }

        var segments = _segments.Segment(features).ToDictionary(x => x.UserId);
        return students
            .Where(x => segments.ContainsKey(x.Id))
            .Select(x =>
            {
                var segment = segments[x.Id];
                return new CohortSegmentDto(
                    x.Id,
                    x.DisplayName,
                    segment.Segment,
                    segment.Priority,
                    segment.Rationale);
            })
            .OrderByDescending(x => x.Priority)
            .ToArray();
    }

    private static bool IsActive(UserMisconception x) => IsActiveStatus(x.Status);

    private static bool IsActiveStatus(MisconceptionStatus status) =>
        status is MisconceptionStatus.Suspected
            or MisconceptionStatus.Detected
            or MisconceptionStatus.CorrectionInProgress
            or MisconceptionStatus.RecheckRequired;

    private static double Median(IReadOnlyList<double> ordered)
    {
        if (ordered.Count == 0) return 0d;
        if (ordered.Count % 2 == 1) return ordered[ordered.Count / 2];
        var upper = ordered.Count / 2;
        return (ordered[upper - 1] + ordered[upper]) / 2d;
    }
}
