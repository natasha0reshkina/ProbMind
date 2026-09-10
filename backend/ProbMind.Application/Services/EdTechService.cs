using ProbMind.Application.Abstractions.Clock;
using ProbMind.Application.Abstractions.Persistence;
using ProbMind.Application.Contracts;
using ProbMind.Domain.Entities;
using ProbMind.Domain.Enums;

namespace ProbMind.Application.Services;

public sealed class EdTechService : IEdTechService
{
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;
    private readonly IDiagnosticService _diagnostics;

    public EdTechService(IUnitOfWork uow, IClock clock, IDiagnosticService diagnostics)
    {
        _uow = uow;
        _clock = clock;
        _diagnostics = diagnostics;
    }

    public async Task<IReadOnlyList<SpacedReviewDto>> RepetitionAsync(Guid userId, CancellationToken ct = default)
    {
        await RequireStudentAsync(userId, ct);
        var topics = (await _uow.Topics.ListAsync(ct))
            .Where(x => x.Status == ContentStatus.Published)
            .OrderBy(x => x.SortOrder)
            .ToArray();
        var mastery = (await _uow.TopicMasteries.WhereAsync(x => x.UserId == userId, ct)).ToDictionary(x => x.TopicId);
        var existing = (await _uow.SpacedReviewItems.WhereAsync(x => x.UserId == userId, ct)).ToDictionary(x => x.TopicId);

        return topics.Select(topic =>
        {
            existing.TryGetValue(topic.Id, out var item);
            var value = mastery.TryGetValue(topic.Id, out var m) ? m.Mastery : 0.5d;
            return new SpacedReviewDto(
                topic.Id,
                topic.Code,
                topic.NameRu,
                value,
                item?.NextReviewAt,
                item?.LastReviewedAt,
                item?.IntervalDays ?? 0,
                item?.Repetitions ?? 0,
                item is not null && item.NextReviewAt <= _clock.UtcNow);
        })
        .OrderByDescending(x => x.IsDue)
        .ThenBy(x => x.NextReviewAt ?? DateTimeOffset.MaxValue)
        .ToArray();
    }

    public async Task<SpacedReviewDto> CompleteReviewAsync(Guid userId, Guid topicId, int quality, CancellationToken ct = default)
    {
        await RequireStudentAsync(userId, ct);
        quality = Math.Clamp(quality, 0, 5);
        var topic = await _uow.Topics.GetByIdAsync(topicId, ct) ?? throw new KeyNotFoundException("Тема не найдена.");
        var items = await _uow.SpacedReviewItems.WhereAsync(x => x.UserId == userId && x.TopicId == topicId, ct);
        var item = items.SingleOrDefault() ?? new SpacedReviewItem { UserId = userId, TopicId = topicId };

        if (quality < 3)
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
            var delta = 0.1d - (5 - quality) * (0.08d + (5 - quality) * 0.02d);
            item.EaseFactor = Math.Clamp(item.EaseFactor + delta, 1.3d, 3.0d);
        }

        item.LastReviewedAt = _clock.UtcNow;
        item.NextReviewAt = _clock.UtcNow.AddDays(item.IntervalDays);
        item.Touch();
        if (items.Count == 0)
            await _uow.SpacedReviewItems.AddAsync(item, ct);
        else
            _uow.SpacedReviewItems.Update(item);
        await _uow.ActivityEvents.AddAsync(new ActivityEvent
        {
            UserId = userId,
            EventType = ActivityEventType.ReviewCompleted,
            AggregateType = nameof(SpacedReviewItem),
            AggregateId = item.Id,
            OccurredAt = _clock.UtcNow
        }, ct);
        await _uow.SaveChangesAsync(ct);

        var mastery = (await _uow.TopicMasteries.WhereAsync(x => x.UserId == userId && x.TopicId == topicId, ct)).SingleOrDefault()?.Mastery ?? .5d;
        return new SpacedReviewDto(topic.Id, topic.Code, topic.NameRu, mastery, item.NextReviewAt, item.LastReviewedAt, item.IntervalDays, item.Repetitions, false);
    }

    public async Task<ConfidenceSummaryDto> ConfidenceAsync(Guid userId, CancellationToken ct = default)
    {
        await RequireStudentAsync(userId, ct);
        var answers = await BuildConfidenceAnswersAsync(userId, ct);
        return SummarizeConfidence(answers);
    }

    public async Task<IReadOnlyList<TeacherConfidenceStudentDto>> TeacherConfidenceAsync(CancellationToken ct = default)
    {
        var students = await _uow.Users.WhereAsync(x => x.Role == UserRole.Student && x.IsActive && !x.Email.EndsWith("@probmind.test"), ct);
        var result = new List<TeacherConfidenceStudentDto>();
        foreach (var student in students.OrderBy(x => x.DisplayName))
        {
            var summary = SummarizeConfidence(await BuildConfidenceAnswersAsync(student.Id, ct));
            result.Add(new TeacherConfidenceStudentDto(
                student.Id,
                student.DisplayName,
                summary.AnswersWithConfidence,
                summary.MeanConfidence,
                summary.Accuracy,
                summary.OverconfidentWrong,
                summary.LowConfidenceCorrect));
        }
        return result;
    }

    public async Task<IReadOnlyList<StudentGroupDto>> GroupsAsync(CancellationToken ct = default)
    {
        var groups = await _uow.StudentGroups.ListAsync(ct);
        var members = await _uow.StudentGroupMembers.ListAsync(ct);
        var users = (await _uow.Users.ListAsync(ct)).ToDictionary(x => x.Id);
        return groups.OrderByDescending(x => x.CreatedAt).Select(group => MapGroup(group, members, users)).ToArray();
    }

    public async Task<StudentGroupDto> CreateGroupAsync(Guid actorId, CreateStudentGroupRequest request, CancellationToken ct = default)
    {
        var name = (request.Name ?? string.Empty).Trim();
        if (name.Length < 2 || name.Length > 200)
            throw new InvalidOperationException("Введите название группы длиной от 2 до 200 символов.");
        var group = new StudentGroup
        {
            CreatedByUserId = actorId,
            Name = name,
            Description = (request.Description ?? string.Empty).Trim()
        };
        await _uow.StudentGroups.AddAsync(group, ct);
        await AddGroupMembersAsync(group.Id, request.StudentIds, ct);
        await _uow.SaveChangesAsync(ct);
        return (await GroupsAsync(ct)).Single(x => x.Id == group.Id);
    }

    public async Task<StudentGroupDto> UpdateGroupMembersAsync(Guid groupId, IReadOnlyList<Guid> studentIds, CancellationToken ct = default)
    {
        _ = await _uow.StudentGroups.GetByIdAsync(groupId, ct) ?? throw new KeyNotFoundException("Группа не найдена.");
        var old = await _uow.StudentGroupMembers.WhereAsync(x => x.GroupId == groupId, ct);
        _uow.StudentGroupMembers.RemoveRange(old);
        await AddGroupMembersAsync(groupId, studentIds, ct);
        await _uow.SaveChangesAsync(ct);
        return (await GroupsAsync(ct)).Single(x => x.Id == groupId);
    }

    public async Task<IReadOnlyList<InterventionSuggestionDto>> InterventionSuggestionsAsync(CancellationToken ct = default)
    {
        var students = await _uow.Users.WhereAsync(x => x.Role == UserRole.Student && x.IsActive && !x.Email.EndsWith("@probmind.test"), ct);
        var masteries = await _uow.TopicMasteries.ListAsync(ct);
        var states = await _uow.UserMisconceptions.ListAsync(ct);
        var events = await _uow.ActivityEvents.ListAsync(ct);
        var result = new List<InterventionSuggestionDto>();
        foreach (var student in students)
        {
            var sm = masteries.Where(x => x.UserId == student.Id && x.ObservationCount > 0).ToArray();
            var mastery = sm.Length == 0 ? .5d : sm.Average(x => x.Mastery);
            var active = states.Count(x => x.UserId == student.Id && x.Status is MisconceptionStatus.Suspected or MisconceptionStatus.Detected or MisconceptionStatus.CorrectionInProgress or MisconceptionStatus.RecheckRequired);
            var last = events.Where(x => x.UserId == student.Id).OrderByDescending(x => x.OccurredAt).FirstOrDefault()?.OccurredAt;
            var inactive = last.HasValue ? Math.Max(0, (int)(_clock.UtcNow - last.Value).TotalDays) : 30;
            if (mastery >= .62d && active < 2 && inactive < 7)
                continue;
            var reasons = new List<string>();
            if (mastery < .62d) reasons.Add($"освоение {Math.Round(mastery * 100)}%");
            if (active >= 2) reasons.Add($"активных ошибок: {active}");
            if (inactive >= 7) reasons.Add($"нет активности {inactive} дн.");
            result.Add(new InterventionSuggestionDto(student.Id, student.DisplayName, mastery, active, inactive, string.Join(" · ", reasons)));
        }
        return result.OrderBy(x => x.Mastery).ThenByDescending(x => x.ActiveMisconceptions).ToArray();
    }

    public async Task<IReadOnlyList<TeacherInterventionDto>> TeacherInterventionsAsync(CancellationToken ct = default)
    {
        var interventions = await _uow.TeacherInterventions.ListAsync(ct);
        var users = (await _uow.Users.ListAsync(ct)).ToDictionary(x => x.Id);
        return interventions.OrderBy(x => x.IsCompleted).ThenByDescending(x => x.CreatedAt).Select(x => MapIntervention(x, users)).ToArray();
    }

    public async Task<IReadOnlyList<TeacherInterventionDto>> StudentInterventionsAsync(Guid userId, CancellationToken ct = default)
    {
        await RequireStudentAsync(userId, ct);
        var interventions = await _uow.TeacherInterventions.WhereAsync(x => x.StudentId == userId, ct);
        var users = (await _uow.Users.ListAsync(ct)).ToDictionary(x => x.Id);
        return interventions.OrderBy(x => x.IsCompleted).ThenByDescending(x => x.CreatedAt).Select(x => MapIntervention(x, users)).ToArray();
    }

    public async Task<TeacherInterventionDto> CreateInterventionAsync(Guid actorId, CreateTeacherInterventionRequest request, CancellationToken ct = default)
    {
        var student = await RequireStudentAsync(request.StudentId, ct);
        var title = (request.Title ?? string.Empty).Trim();
        if (title.Length == 0) throw new InvalidOperationException("Введите название индивидуального задания.");
        var item = new TeacherIntervention
        {
            CreatedByUserId = actorId,
            StudentId = student.Id,
            Title = title,
            Body = (request.Body ?? string.Empty).Trim(),
            Kind = string.IsNullOrWhiteSpace(request.Kind) ? "Practice" : request.Kind.Trim(),
            DueAt = request.DueAt
        };
        await _uow.TeacherInterventions.AddAsync(item, ct);
        await _uow.SaveChangesAsync(ct);
        var users = (await _uow.Users.ListAsync(ct)).ToDictionary(x => x.Id);
        return MapIntervention(item, users);
    }

    public async Task<TeacherInterventionDto> CompleteInterventionAsync(Guid userId, Guid interventionId, CancellationToken ct = default)
    {
        var item = await _uow.TeacherInterventions.GetByIdAsync(interventionId, ct) ?? throw new KeyNotFoundException("Назначение не найдено.");
        if (item.StudentId != userId) throw new UnauthorizedAccessException("Это назначение принадлежит другому студенту.");
        item.IsCompleted = true;
        item.CompletedAt = _clock.UtcNow;
        item.Touch();
        _uow.TeacherInterventions.Update(item);
        await _uow.SaveChangesAsync(ct);
        var users = (await _uow.Users.ListAsync(ct)).ToDictionary(x => x.Id);
        return MapIntervention(item, users);
    }

    public async Task<IReadOnlyList<ExamDto>> TeacherExamsAsync(CancellationToken ct = default) =>
        await BuildExamDtosAsync(null, ct);

    public async Task<IReadOnlyList<ExamDto>> StudentExamsAsync(Guid userId, CancellationToken ct = default)
    {
        await RequireStudentAsync(userId, ct);
        var all = await BuildExamDtosAsync(userId, ct);
        var result = new List<ExamDto>();
        foreach (var exam in all)
        {
            if (exam.IsPublished && IsAvailable(exam) && await CanAccessAudience(userId, exam.GroupId, exam.StudentId, ct))
                result.Add(exam);
        }
        return result;
    }

    public async Task<TeacherExamReviewDto> TeacherExamReviewAsync(Guid examId, CancellationToken ct = default)
    {
        var exam = await _uow.ExamDefinitions.GetByIdAsync(examId, ct) ?? throw new KeyNotFoundException("Экзамен не найден.");
        var assignedIds = await ExamAudienceStudentIdsAsync(exam, ct);
        var users = (await _uow.Users.ListAsync(ct)).ToDictionary(x => x.Id);
        var groups = (await _uow.StudentGroups.ListAsync(ct)).ToDictionary(x => x.Id);
        var attempts = await _uow.ExamAttempts.WhereAsync(x => x.ExamId == exam.Id, ct);
        var sessionIds = attempts.Select(x => x.DiagnosticSessionId).ToHashSet();
        var sessions = (await _uow.DiagnosticSessions.ListAsync(ct))
            .Where(x => sessionIds.Contains(x.Id))
            .ToDictionary(x => x.Id);

        var completedStudents = assignedIds.Count(studentId =>
        {
            var attempt = attempts.SingleOrDefault(x => x.StudentId == studentId);
            return attempt is not null
                && sessions.TryGetValue(attempt.DiagnosticSessionId, out var session)
                && session.Status == DiagnosticStatus.ReportReady;
        });
        var allCompleted = assignedIds.Count > 0 && completedStudents == assignedIds.Count;

        var templateQuestions = (await _uow.DiagnosticTemplateQuestions.WhereAsync(x => x.DiagnosticTemplateId == exam.DiagnosticTemplateId, ct))
            .OrderBy(x => x.Position)
            .ToArray();
        var questionIds = templateQuestions.Select(x => x.QuestionId).ToHashSet();
        var questions = (await _uow.Questions.ListAsync(ct))
            .Where(x => questionIds.Contains(x.Id))
            .ToDictionary(x => x.Id);
        var versions = (await _uow.QuestionVersions.ListAsync(ct))
            .Where(x => questionIds.Contains(x.QuestionId))
            .ToArray();
        var versionsById = versions.ToDictionary(x => x.Id);
        var currentVersions = versions
            .Where(v => questions.TryGetValue(v.QuestionId, out var q) && v.VersionNumber == q.CurrentVersionNumber)
            .GroupBy(x => x.QuestionId)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(v => v.VersionNumber).First());
        var versionIds = versions.Select(x => x.Id).ToHashSet();
        var options = (await _uow.AnswerOptions.ListAsync(ct))
            .Where(x => versionIds.Contains(x.QuestionVersionId))
            .ToArray();
        var optionById = options.ToDictionary(x => x.Id);
        var answers = allCompleted
            ? (await _uow.DiagnosticAnswers.ListAsync(ct)).Where(x => sessionIds.Contains(x.SessionId)).ToArray()
            : Array.Empty<DiagnosticAnswer>();

        var studentReviews = new List<ExamStudentReviewDto>();
        foreach (var studentId in assignedIds)
        {
            if (!users.TryGetValue(studentId, out var student))
                continue;

            var attempt = attempts.SingleOrDefault(x => x.StudentId == studentId);
            DiagnosticSession? session = attempt is not null && sessions.TryGetValue(attempt.DiagnosticSessionId, out var foundSession) ? foundSession : null;
            var status = session?.Status == DiagnosticStatus.ReportReady
                ? "Завершён"
                : attempt is null
                    ? "Не начинал"
                    : "В процессе";

            IReadOnlyList<ExamAnswerReviewDto> studentAnswers = Array.Empty<ExamAnswerReviewDto>();
            if (allCompleted && attempt is not null)
            {
                var byQuestion = answers
                    .Where(x => x.SessionId == attempt.DiagnosticSessionId)
                    .GroupBy(x => x.QuestionId)
                    .ToDictionary(x => x.Key, x => x.OrderByDescending(a => a.SubmittedAt).First());
                var rows = new List<ExamAnswerReviewDto>();
                foreach (var templateQuestion in templateQuestions)
                {
                    byQuestion.TryGetValue(templateQuestion.QuestionId, out var answer);
                    QuestionVersion? version = null;
                    if (answer is not null)
                        versionsById.TryGetValue(answer.QuestionVersionId, out version);
                    if (version is null && templateQuestion.QuestionVersionId.HasValue)
                        versionsById.TryGetValue(templateQuestion.QuestionVersionId.Value, out version);
                    if (version is null)
                        currentVersions.TryGetValue(templateQuestion.QuestionId, out version);

                    var selected = answer is not null && optionById.TryGetValue(answer.AnswerOptionId, out var selectedOption)
                        ? selectedOption.Text
                        : null;
                    var correct = version is null
                        ? "-"
                        : string.Join(" / ", options
                            .Where(x => x.QuestionVersionId == version.Id && x.IsCorrect)
                            .OrderBy(x => x.SortOrder)
                            .Select(x => x.Text));

                    rows.Add(new ExamAnswerReviewDto(
                        templateQuestion.Position,
                        version?.Prompt ?? "Задание недоступно",
                        selected,
                        string.IsNullOrWhiteSpace(correct) ? "-" : correct,
                        answer?.IsCorrect,
                        answer?.ConfidenceLevel,
                        answer?.Reasoning,
                        answer?.StudentNote,
                        answer?.ResponseTimeMs));
                }
                studentAnswers = rows;
            }

            studentReviews.Add(new ExamStudentReviewDto(
                student.Id,
                student.DisplayName,
                student.Email,
                status,
                session?.OverallScore,
                attempt?.StartedAt,
                session?.CompletedAt,
                studentAnswers));
        }

        var audience = exam.StudentId.HasValue && users.TryGetValue(exam.StudentId.Value, out var assignedStudent)
            ? $"Студент: {assignedStudent.DisplayName}"
            : exam.GroupId.HasValue && groups.TryGetValue(exam.GroupId.Value, out var group)
                ? $"Группа: {group.Name}"
                : "Все студенты";

        return new TeacherExamReviewDto(
            exam.Id,
            exam.Title,
            audience,
            assignedIds.Count,
            completedStudents,
            allCompleted,
            studentReviews);
    }

    public async Task<ExamDto> CreateExamAsync(Guid actorId, CreateExamRequest request, CancellationToken ct = default)
    {
        var template = await _uow.DiagnosticTemplates.GetByIdAsync(request.DiagnosticTemplateId, ct) ?? throw new KeyNotFoundException("Диагностика не найдена.");
        var count = await _uow.DiagnosticTemplateQuestions.CountAsync(x => x.DiagnosticTemplateId == template.Id, ct);
        if (count < 1) throw new InvalidOperationException("В выбранной диагностике нет заданий.");
        if (request.TimeLimitMinutes is < 1 or > 300) throw new InvalidOperationException("Лимит времени должен быть от 1 до 300 минут.");
        if (request.GroupId.HasValue && request.StudentId.HasValue)
            throw new InvalidOperationException("Выберите либо группу, либо конкретного студента.");
        if (request.GroupId.HasValue)
            _ = await _uow.StudentGroups.GetByIdAsync(request.GroupId.Value, ct) ?? throw new KeyNotFoundException("Группа не найдена.");
        if (request.StudentId.HasValue)
            await RequireStudentAsync(request.StudentId.Value, ct);
        template.GroupId = null;
        template.StudentId = null;
        if (!template.IsPublished)
        {
            template.IsPublished = true;
            template.PublishedAt = _clock.UtcNow;
        }
        template.Touch();
        _uow.DiagnosticTemplates.Update(template);
        var exam = new ExamDefinition
        {
            CreatedByUserId = actorId,
            DiagnosticTemplateId = template.Id,
            GroupId = request.GroupId,
            StudentId = request.StudentId,
            Title = string.IsNullOrWhiteSpace(request.Title) ? template.Title : request.Title.Trim(),
            Description = (request.Description ?? string.Empty).Trim(),
            TimeLimitMinutes = request.TimeLimitMinutes,
            IsPublished = request.IsPublished,
            AvailableFrom = request.AvailableFrom,
            AvailableUntil = request.AvailableUntil
        };
        await _uow.ExamDefinitions.AddAsync(exam, ct);
        await _uow.SaveChangesAsync(ct);
        return (await BuildExamDtosAsync(null, ct)).Single(x => x.Id == exam.Id);
    }

    public async Task<ExamStartDto> StartExamAsync(Guid userId, Guid examId, CancellationToken ct = default)
    {
        await RequireStudentAsync(userId, ct);
        var exam = await _uow.ExamDefinitions.GetByIdAsync(examId, ct) ?? throw new KeyNotFoundException("Экзамен не найден.");
        if (!exam.IsPublished || !ExamAvailable(exam)) throw new InvalidOperationException("Экзамен сейчас недоступен.");
        if (!await CanAccessAudience(userId, exam.GroupId, exam.StudentId, ct)) throw new UnauthorizedAccessException("Экзамен назначен другому студенту или группе.");
        var existing = (await _uow.ExamAttempts.WhereAsync(x => x.ExamId == examId && x.StudentId == userId, ct)).SingleOrDefault();
        if (existing is not null)
            return new ExamStartDto(exam.Id, existing.DiagnosticSessionId, exam.TimeLimitMinutes, existing.StartedAt, existing.StartedAt.AddMinutes(exam.TimeLimitMinutes));

        var activeExam = await ExamSessionRules.ActiveAttemptAsync(_uow, userId, ct);
        if (activeExam is not null)
            throw new InvalidOperationException("Сначала завершите уже начатый экзамен.");

        var activePractice = await _uow.PracticeSessions.AnyAsync(
            x => x.UserId == userId && (x.Status == PracticeStatus.Created || x.Status == PracticeStatus.InProgress),
            ct);
        if (activePractice)
            throw new InvalidOperationException("Сначала завершите текущую практику.");

        var session = await _diagnostics.StartTemplateAsync(userId, exam.DiagnosticTemplateId, ct);
        var attempt = new ExamAttempt
        {
            ExamId = exam.Id,
            StudentId = userId,
            DiagnosticSessionId = session.Id,
            StartedAt = _clock.UtcNow
        };
        await _uow.ExamAttempts.AddAsync(attempt, ct);
        await _uow.SaveChangesAsync(ct);
        return new ExamStartDto(exam.Id, session.Id, exam.TimeLimitMinutes, attempt.StartedAt, attempt.StartedAt.AddMinutes(exam.TimeLimitMinutes));
    }

    public async Task<ExamSessionContextDto> ExamContextAsync(Guid userId, Guid sessionId, CancellationToken ct = default)
    {
        var attempt = (await _uow.ExamAttempts.WhereAsync(x => x.DiagnosticSessionId == sessionId && x.StudentId == userId, ct)).SingleOrDefault();
        if (attempt is null) return new ExamSessionContextDto(false, null, null, null, null, null);
        var exam = await _uow.ExamDefinitions.GetByIdAsync(attempt.ExamId, ct);
        if (exam is null) return new ExamSessionContextDto(false, null, null, null, null, null);
        return new ExamSessionContextDto(true, exam.Id, exam.Title, exam.TimeLimitMinutes, attempt.StartedAt, attempt.StartedAt.AddMinutes(exam.TimeLimitMinutes));
    }

    public async Task<IReadOnlyList<AchievementDto>> AchievementsAsync(Guid userId, CancellationToken ct = default)
    {
        await RequireStudentAsync(userId, ct);

        var diagnostics = await _uow.DiagnosticSessions.WhereAsync(x => x.UserId == userId && x.Status == DiagnosticStatus.ReportReady, ct);
        var hiddenExamSessions = await ExamSessionRules.ActiveSessionIdsAsync(_uow, userId, ct);
        var diagnosticAnswers = (await _uow.DiagnosticAnswers.WhereAsync(x => x.UserId == userId, ct))
            .Where(x => !hiddenExamSessions.Contains(x.SessionId))
            .ToArray();
        var practiceAttempts = await _uow.PracticeAttempts.WhereAsync(x => x.UserId == userId, ct);
        var mastery = await _uow.TopicMasteries.WhereAsync(x => x.UserId == userId, ct);
        var masteryHistory = await _uow.TopicMasteryHistory.WhereAsync(x => x.UserId == userId, ct);
        var states = await _uow.UserMisconceptions.WhereAsync(x => x.UserId == userId, ct);
        var events = await _uow.ActivityEvents.WhereAsync(x => x.UserId == userId, ct);

        var activeDays = events
            .Where(x => x.EventType != ActivityEventType.UserRegistered)
            .Select(x => DateOnly.FromDateTime(x.OccurredAt.UtcDateTime))
            .Distinct()
            .OrderBy(x => x)
            .ToArray();

        var streak = LongestStreak(activeDays);
        var answers = diagnosticAnswers.Length + practiceAttempts.Count;
        var transferCorrect = practiceAttempts.Count(x => x.ExerciseType == ExerciseType.Transfer && x.IsCorrect);
        var masteredTopicIds = mastery
            .Where(x => x.ObservationCount > 0 && x.Mastery >= .8d)
            .Select(x => x.TopicId)
            .Concat(masteryHistory.Where(x => x.Mastery >= .8d).Select(x => x.TopicId))
            .Distinct()
            .ToArray();
        var masteredTopics = masteredTopicIds.Length;
        var corrected = states.Count(x => x.CorrectedAt.HasValue);

        var catalogue = new[]
        {
            new { Code = "first_diagnostic", Title = "Первая диагностика", Description = "Завершить первую диагностику.", Progress = diagnostics.Count, Target = 1 },
            new { Code = "answers_25", Title = "25 осмысленных ответов", Description = "Дать 25 ответов в диагностике и практике.", Progress = answers, Target = 25 },
            new { Code = "answers_100", Title = "100 ответов", Description = "Набрать 100 учебных ответов.", Progress = answers, Target = 100 },
            new { Code = "mastery_80", Title = "Тема освоена", Description = "Довести хотя бы одну тему до 80% освоения.", Progress = masteredTopics, Target = 1 },
            new { Code = "streak_3", Title = "Три дня подряд", Description = "Учиться три дня подряд.", Progress = streak, Target = 3 },
            new { Code = "streak_7", Title = "Неделя ритма", Description = "Учиться семь дней подряд.", Progress = streak, Target = 7 },
            new { Code = "corrected", Title = "Исправленная ошибка", Description = "Довести типичную ошибку до статуса исправлена.", Progress = corrected, Target = 1 },
            new { Code = "transfer_3", Title = "Применение знания", Description = "Правильно решить три задания, где знание применяется в новой ситуации.", Progress = transferCorrect, Target = 3 }
        };

        return catalogue.Select(item =>
            new AchievementDto(
                item.Code,
                item.Title,
                item.Description,
                item.Progress >= item.Target,
                Math.Min(Math.Max(0, item.Progress), item.Target),
                item.Target))
            .ToArray();
    }

    public async Task<IReadOnlyList<MaterialStudyCycleDto>> StudentMaterialCyclesAsync(Guid userId, CancellationToken ct = default)
    {
        await RequireStudentAsync(userId, ct);
        var cycles = await _uow.MaterialStudyCycles.WhereAsync(x => x.IsPublished, ct);
        var result = new List<MaterialStudyCycleDto>();
        foreach (var cycle in cycles.OrderByDescending(x => x.CreatedAt))
        {
            if (!await CanAccessGroup(userId, cycle.GroupId, ct)) continue;
            result.Add(await MapMaterialCycleAsync(cycle, userId, ct));
        }
        return result;
    }

    public async Task<IReadOnlyList<TeacherMaterialCycleAnalyticsDto>> TeacherMaterialCyclesAsync(CancellationToken ct = default)
    {
        var cycles = await _uow.MaterialStudyCycles.ListAsync(ct);
        var groups = (await _uow.StudentGroups.ListAsync(ct)).ToDictionary(x => x.Id);
        var students = await _uow.Users.WhereAsync(x => x.Role == UserRole.Student && x.IsActive && !x.Email.EndsWith("@probmind.test"), ct);
        var memberships = await _uow.StudentGroupMembers.ListAsync(ct);
        var progress = await _uow.MaterialStudyProgress.ListAsync(ct);
        var sessions = (await _uow.DiagnosticSessions.ListAsync(ct)).ToDictionary(x => x.Id);
        var result = new List<TeacherMaterialCycleAnalyticsDto>();
        foreach (var cycle in cycles.OrderByDescending(x => x.CreatedAt))
        {
            var eligible = cycle.GroupId.HasValue
                ? memberships.Where(x => x.GroupId == cycle.GroupId.Value).Select(x => x.StudentId).ToHashSet()
                : students.Select(x => x.Id).ToHashSet();
            var cp = progress.Where(x => x.MaterialStudyCycleId == cycle.Id && eligible.Contains(x.StudentId)).ToArray();
            var preScores = cp.Where(x => x.PreSessionId.HasValue && sessions.TryGetValue(x.PreSessionId.Value, out var s) && s.Status == DiagnosticStatus.ReportReady && s.OverallScore.HasValue)
                .Select(x => sessions[x.PreSessionId!.Value].OverallScore!.Value).ToArray();
            var postScores = cp.Where(x => x.PostSessionId.HasValue && sessions.TryGetValue(x.PostSessionId.Value, out var s) && s.Status == DiagnosticStatus.ReportReady && s.OverallScore.HasValue)
                .Select(x => sessions[x.PostSessionId!.Value].OverallScore!.Value).ToArray();
            var deltas = cp.Where(x => x.PreSessionId.HasValue && x.PostSessionId.HasValue && sessions.ContainsKey(x.PreSessionId.Value) && sessions.ContainsKey(x.PostSessionId.Value) && sessions[x.PreSessionId.Value].OverallScore.HasValue && sessions[x.PostSessionId.Value].OverallScore.HasValue)
                .Select(x => sessions[x.PostSessionId!.Value].OverallScore!.Value - sessions[x.PreSessionId!.Value].OverallScore!.Value).ToArray();
            result.Add(new TeacherMaterialCycleAnalyticsDto(
                cycle.Id,
                cycle.Title,
                cycle.GroupId.HasValue && groups.TryGetValue(cycle.GroupId.Value, out var group) ? group.Name : null,
                cycle.IsPublished,
                eligible.Count,
                cp.Count(x => x.PreSessionId.HasValue && sessions.TryGetValue(x.PreSessionId.Value, out var ps) && ps.Status == DiagnosticStatus.ReportReady),
                cp.Count(x => x.MaterialOpenedAt.HasValue),
                cp.Count(x => x.PostSessionId.HasValue && sessions.TryGetValue(x.PostSessionId.Value, out var post) && post.Status == DiagnosticStatus.ReportReady),
                preScores.Length == 0 ? null : preScores.Average(),
                postScores.Length == 0 ? null : postScores.Average(),
                deltas.Length == 0 ? null : deltas.Average(),
                cycle.CreatedAt));
        }
        return result;
    }

    public async Task<MaterialStudyCycleDto> CreateMaterialCycleAsync(Guid actorId, CreateMaterialStudyCycleRequest request, CancellationToken ct = default)
    {
        var pre = await _uow.DiagnosticTemplates.GetByIdAsync(request.PreDiagnosticTemplateId, ct) ?? throw new KeyNotFoundException("Диагностика до материала не найдена.");
        var post = await _uow.DiagnosticTemplates.GetByIdAsync(request.PostDiagnosticTemplateId, ct) ?? throw new KeyNotFoundException("Диагностика после материала не найдена.");
        if (request.GroupId.HasValue)
            _ = await _uow.StudentGroups.GetByIdAsync(request.GroupId.Value, ct) ?? throw new KeyNotFoundException("Группа не найдена.");
        foreach (var template in new[] { pre, post })
        {
            template.GroupId = null;
            template.StudentId = null;
            if (!template.IsPublished)
            {
                template.IsPublished = true;
                template.PublishedAt = _clock.UtcNow;
            }
            template.Touch();
            _uow.DiagnosticTemplates.Update(template);
        }
        var cycle = new MaterialStudyCycle
        {
            CreatedByUserId = actorId,
            Title = (request.Title ?? string.Empty).Trim(),
            MaterialText = (request.MaterialText ?? string.Empty).Trim(),
            PreDiagnosticTemplateId = pre.Id,
            PostDiagnosticTemplateId = post.Id,
            GroupId = request.GroupId,
            IsPublished = request.IsPublished
        };
        if (cycle.Title.Length == 0 || cycle.MaterialText.Length == 0)
            throw new InvalidOperationException("Заполните название и текст материала.");
        await _uow.MaterialStudyCycles.AddAsync(cycle, ct);
        await _uow.SaveChangesAsync(ct);
        return await MapMaterialCycleAsync(cycle, null, ct);
    }

    public async Task<MaterialStudyCycleDto> OpenMaterialAsync(Guid userId, Guid cycleId, CancellationToken ct = default)
    {
        var cycle = await RequireCycleForStudentAsync(userId, cycleId, ct);
        var progress = await GetOrCreateProgressAsync(userId, cycle.Id, ct);
        if (progress.PreSessionId.HasValue)
        {
            var pre = await _uow.DiagnosticSessions.GetByIdAsync(progress.PreSessionId.Value, ct);
            if (pre?.Status != DiagnosticStatus.ReportReady)
                throw new InvalidOperationException("Сначала завершите диагностику до материала.");
        }
        else
        {
            throw new InvalidOperationException("Сначала пройдите диагностику до материала.");
        }
        progress.MaterialOpenedAt ??= _clock.UtcNow;
        progress.Touch();
        _uow.MaterialStudyProgress.Update(progress);
        await _uow.ActivityEvents.AddAsync(new ActivityEvent
        {
            UserId = userId,
            EventType = ActivityEventType.MaterialStudied,
            AggregateType = nameof(MaterialStudyCycle),
            AggregateId = cycle.Id,
            OccurredAt = _clock.UtcNow
        }, ct);
        await _uow.SaveChangesAsync(ct);
        return await MapMaterialCycleAsync(cycle, userId, ct);
    }

    public async Task<DiagnosticSessionDto> StartMaterialDiagnosticAsync(Guid userId, Guid cycleId, string stage, CancellationToken ct = default)
    {
        var cycle = await RequireCycleForStudentAsync(userId, cycleId, ct);
        var progress = await GetOrCreateProgressAsync(userId, cycle.Id, ct);
        var isPost = string.Equals(stage, "post", StringComparison.OrdinalIgnoreCase);
        if (isPost && !progress.MaterialOpenedAt.HasValue)
            throw new InvalidOperationException("Сначала откройте и изучите материал.");
        var existingId = isPost ? progress.PostSessionId : progress.PreSessionId;
        if (existingId.HasValue)
            return await _diagnostics.GetAsync(userId, existingId.Value, ct);
        var session = await _diagnostics.StartTemplateAsync(userId, isPost ? cycle.PostDiagnosticTemplateId : cycle.PreDiagnosticTemplateId, ct);
        if (isPost) progress.PostSessionId = session.Id; else progress.PreSessionId = session.Id;
        progress.Touch();
        _uow.MaterialStudyProgress.Update(progress);
        await _uow.SaveChangesAsync(ct);
        return session;
    }

    private async Task<IReadOnlyList<ConfidenceAnswerDto>> BuildConfidenceAnswersAsync(Guid userId, CancellationToken ct)
    {
        var hiddenExamSessions = await ExamSessionRules.ActiveSessionIdsAsync(_uow, userId, ct);
        var diagnostic = (await _uow.DiagnosticAnswers.WhereAsync(x => x.UserId == userId && x.ConfidenceLevel.HasValue, ct))
            .Where(x => !hiddenExamSessions.Contains(x.SessionId))
            .ToArray();
        var practice = await _uow.PracticeAttempts.WhereAsync(x => x.UserId == userId && x.ConfidenceLevel.HasValue, ct);
        var questions = (await _uow.Questions.ListAsync(ct)).ToDictionary(x => x.Id);
        var versions = (await _uow.QuestionVersions.ListAsync(ct)).ToDictionary(x => x.Id);
        var topics = (await _uow.Topics.ListAsync(ct)).ToDictionary(x => x.Id);
        string TopicName(Guid questionId) => questions.TryGetValue(questionId, out var q) && topics.TryGetValue(q.TopicId, out var topic) ? topic.NameRu : "Тема";
        string Prompt(Guid versionId) => versions.TryGetValue(versionId, out var version) ? version.Prompt : "Задание";
        return diagnostic.Select(x => new ConfidenceAnswerDto(x.Id, "Диагностика", TopicName(x.QuestionId), Prompt(x.QuestionVersionId), x.IsCorrect, x.ConfidenceLevel!.Value, x.Reasoning ?? string.Empty, x.SubmittedAt))
            .Concat(practice.Select(x => new ConfidenceAnswerDto(x.Id, "Практика", TopicName(x.QuestionId), Prompt(x.QuestionVersionId), x.IsCorrect, x.ConfidenceLevel!.Value, x.Reasoning ?? string.Empty, x.SubmittedAt)))
            .OrderByDescending(x => x.SubmittedAt)
            .ToArray();
    }

    private static ConfidenceSummaryDto SummarizeConfidence(IReadOnlyList<ConfidenceAnswerDto> answers)
    {
        if (answers.Count == 0) return new ConfidenceSummaryDto(0, 0, 0, 0, 0, answers);
        return new ConfidenceSummaryDto(
            answers.Count,
            answers.Average(x => x.ConfidenceLevel),
            answers.Count(x => x.IsCorrect) / (double)answers.Count,
            answers.Count(x => !x.IsCorrect && x.ConfidenceLevel >= 4),
            answers.Count(x => x.IsCorrect && x.ConfidenceLevel <= 2),
            answers);
    }

    private async Task AddGroupMembersAsync(Guid groupId, IReadOnlyList<Guid> studentIds, CancellationToken ct)
    {
        foreach (var studentId in studentIds.Distinct())
        {
            await RequireStudentAsync(studentId, ct);
            await _uow.StudentGroupMembers.AddAsync(new StudentGroupMember { GroupId = groupId, StudentId = studentId }, ct);
        }
    }

    private static StudentGroupDto MapGroup(StudentGroup group, IReadOnlyList<StudentGroupMember> members, IReadOnlyDictionary<Guid, User> users) =>
        new(group.Id, group.Name, group.Description, members.Where(x => x.GroupId == group.Id && users.ContainsKey(x.StudentId)).Select(x => users[x.StudentId]).Where(x => !x.Email.EndsWith("@probmind.test", StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.DisplayName).Select(x => new StudentGroupMemberDto(x.Id, x.DisplayName, x.Email)).ToArray(), group.CreatedAt);

    private static TeacherInterventionDto MapIntervention(TeacherIntervention item, IReadOnlyDictionary<Guid, User> users) =>
        new(item.Id, item.StudentId, users.TryGetValue(item.StudentId, out var student) ? student.DisplayName : "Студент", item.Title, item.Body, item.Kind, item.IsCompleted, item.DueAt, item.CreatedAt, item.CompletedAt);

    private async Task<IReadOnlyList<ExamDto>> BuildExamDtosAsync(Guid? studentId, CancellationToken ct)
    {
        var exams = await _uow.ExamDefinitions.ListAsync(ct);
        var templateQuestions = await _uow.DiagnosticTemplateQuestions.ListAsync(ct);
        var groups = (await _uow.StudentGroups.ListAsync(ct)).ToDictionary(x => x.Id);
        var users = (await _uow.Users.ListAsync(ct)).ToDictionary(x => x.Id);
        var attempts = studentId.HasValue
            ? await _uow.ExamAttempts.WhereAsync(x => x.StudentId == studentId.Value, ct)
            : await _uow.ExamAttempts.ListAsync(ct);
        var sessions = studentId.HasValue
            ? (await _uow.DiagnosticSessions.WhereAsync(x => x.UserId == studentId.Value, ct)).ToDictionary(x => x.Id)
            : (await _uow.DiagnosticSessions.ListAsync(ct)).ToDictionary(x => x.Id);

        var result = new List<ExamDto>();
        foreach (var exam in exams.OrderByDescending(x => x.CreatedAt))
        {
            var studentAttempt = studentId.HasValue
                ? attempts.SingleOrDefault(x => x.ExamId == exam.Id && x.StudentId == studentId.Value)
                : null;
            DiagnosticSession? studentSession = studentAttempt is not null && sessions.TryGetValue(studentAttempt.DiagnosticSessionId, out var ownSession) ? ownSession : null;

            var assignedIds = await ExamAudienceStudentIdsAsync(exam, ct);
            var examAttempts = attempts.Where(x => x.ExamId == exam.Id).ToArray();
            var completed = assignedIds.Count(id =>
            {
                var attempt = examAttempts.SingleOrDefault(x => x.StudentId == id);
                return attempt is not null
                    && sessions.TryGetValue(attempt.DiagnosticSessionId, out var session)
                    && session.Status == DiagnosticStatus.ReportReady;
            });

            result.Add(new ExamDto(
                exam.Id,
                exam.DiagnosticTemplateId,
                exam.GroupId,
                exam.GroupId.HasValue && groups.TryGetValue(exam.GroupId.Value, out var group) ? group.Name : null,
                exam.StudentId,
                exam.StudentId.HasValue && users.TryGetValue(exam.StudentId.Value, out var student) ? student.DisplayName : null,
                exam.Title,
                exam.Description,
                exam.TimeLimitMinutes,
                templateQuestions.Count(x => x.DiagnosticTemplateId == exam.DiagnosticTemplateId),
                exam.IsPublished,
                exam.AvailableFrom,
                exam.AvailableUntil,
                studentSession?.Id,
                studentSession?.Status.ToString(),
                studentSession?.OverallScore,
                studentAttempt?.StartedAt,
                assignedIds.Count,
                completed,
                assignedIds.Count > 0 && completed == assignedIds.Count));
        }

        return result;
    }

    private async Task<IReadOnlyList<Guid>> ExamAudienceStudentIdsAsync(ExamDefinition exam, CancellationToken ct)
    {
        var activeStudents = await _uow.Users.WhereAsync(
            x => x.Role == UserRole.Student && x.IsActive && !x.Email.EndsWith("@probmind.test"),
            ct);
        var activeIds = activeStudents.Select(x => x.Id).ToHashSet();

        if (exam.StudentId.HasValue)
            return activeIds.Contains(exam.StudentId.Value) ? new[] { exam.StudentId.Value } : Array.Empty<Guid>();

        if (exam.GroupId.HasValue)
        {
            var members = await _uow.StudentGroupMembers.WhereAsync(x => x.GroupId == exam.GroupId.Value, ct);
            return members.Select(x => x.StudentId).Where(activeIds.Contains).Distinct().ToArray();
        }

        return activeStudents.OrderBy(x => x.DisplayName).Select(x => x.Id).ToArray();
    }

    private static bool IsAvailable(ExamDto exam)
    {
        var now = DateTimeOffset.UtcNow;
        return (!exam.AvailableFrom.HasValue || exam.AvailableFrom <= now) && (!exam.AvailableUntil.HasValue || exam.AvailableUntil >= now);
    }

    private bool ExamAvailable(ExamDefinition exam) =>
        (!exam.AvailableFrom.HasValue || exam.AvailableFrom <= _clock.UtcNow) && (!exam.AvailableUntil.HasValue || exam.AvailableUntil >= _clock.UtcNow);

    private async Task<bool> CanAccessGroup(Guid studentId, Guid? groupId, CancellationToken ct)
    {
        if (!groupId.HasValue) return true;
        return await _uow.StudentGroupMembers.AnyAsync(x => x.GroupId == groupId.Value && x.StudentId == studentId, ct);
    }

    private async Task<bool> CanAccessAudience(Guid studentId, Guid? groupId, Guid? assignedStudentId, CancellationToken ct)
    {
        if (assignedStudentId.HasValue) return assignedStudentId.Value == studentId;
        if (!groupId.HasValue) return true;
        return await _uow.StudentGroupMembers.AnyAsync(x => x.GroupId == groupId.Value && x.StudentId == studentId, ct);
    }

    private static int LongestStreak(IReadOnlyList<DateOnly> days)
    {
        if (days.Count == 0) return 0;
        var longest = 1;
        var current = 1;
        for (var i = 1; i < days.Count; i++)
        {
            current = days[i].DayNumber - days[i - 1].DayNumber == 1 ? current + 1 : 1;
            longest = Math.Max(longest, current);
        }
        return longest;
    }

    private async Task<MaterialStudyCycle> RequireCycleForStudentAsync(Guid userId, Guid cycleId, CancellationToken ct)
    {
        await RequireStudentAsync(userId, ct);
        var cycle = await _uow.MaterialStudyCycles.GetByIdAsync(cycleId, ct) ?? throw new KeyNotFoundException("Учебный цикл не найден.");
        if (!cycle.IsPublished || !await CanAccessGroup(userId, cycle.GroupId, ct)) throw new UnauthorizedAccessException("Материал недоступен.");
        return cycle;
    }

    private async Task<MaterialStudyProgress> GetOrCreateProgressAsync(Guid userId, Guid cycleId, CancellationToken ct)
    {
        var existing = (await _uow.MaterialStudyProgress.WhereAsync(x => x.StudentId == userId && x.MaterialStudyCycleId == cycleId, ct)).SingleOrDefault();
        if (existing is not null) return existing;
        var progress = new MaterialStudyProgress { StudentId = userId, MaterialStudyCycleId = cycleId };
        await _uow.MaterialStudyProgress.AddAsync(progress, ct);
        await _uow.SaveChangesAsync(ct);
        return progress;
    }

    private async Task<MaterialStudyCycleDto> MapMaterialCycleAsync(MaterialStudyCycle cycle, Guid? userId, CancellationToken ct)
    {
        var groups = (await _uow.StudentGroups.ListAsync(ct)).ToDictionary(x => x.Id);
        MaterialStudyProgress? progress = null;
        if (userId.HasValue)
            progress = (await _uow.MaterialStudyProgress.WhereAsync(x => x.StudentId == userId.Value && x.MaterialStudyCycleId == cycle.Id, ct)).SingleOrDefault();
        DiagnosticSession? pre = progress?.PreSessionId.HasValue == true ? await _uow.DiagnosticSessions.GetByIdAsync(progress.PreSessionId.Value, ct) : null;
        DiagnosticSession? post = progress?.PostSessionId.HasValue == true ? await _uow.DiagnosticSessions.GetByIdAsync(progress.PostSessionId.Value, ct) : null;
        double? delta = pre?.OverallScore.HasValue == true && post?.OverallScore.HasValue == true ? post.OverallScore.Value - pre.OverallScore.Value : null;
        return new MaterialStudyCycleDto(
            cycle.Id,
            cycle.Title,
            cycle.MaterialText,
            cycle.PreDiagnosticTemplateId,
            cycle.PostDiagnosticTemplateId,
            cycle.GroupId,
            cycle.GroupId.HasValue && groups.TryGetValue(cycle.GroupId.Value, out var group) ? group.Name : null,
            cycle.IsPublished,
            progress?.PreSessionId,
            pre?.Status.ToString(),
            pre?.OverallScore,
            progress?.MaterialOpenedAt,
            progress?.PostSessionId,
            post?.Status.ToString(),
            post?.OverallScore,
            delta,
            cycle.CreatedAt);
    }

    private async Task<User> RequireStudentAsync(Guid userId, CancellationToken ct)
    {
        var user = await _uow.Users.GetByIdAsync(userId, ct) ?? throw new KeyNotFoundException("Студент не найден.");
        if (user.Role != UserRole.Student) throw new InvalidOperationException("Операция доступна только студенту.");
        return user;
    }
}
