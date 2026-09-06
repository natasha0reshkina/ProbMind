using ProbMind.Application.Abstractions.Clock;
using ProbMind.Application.Abstractions.Persistence;
using ProbMind.Application.Contracts;
using ProbMind.Domain.Entities;
using ProbMind.Domain.Enums;

namespace ProbMind.Application.Services;

public sealed class DiagnosticTemplateService : IDiagnosticTemplateService
{
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;

    public DiagnosticTemplateService(IUnitOfWork uow, IClock clock)
    {
        _uow = uow;
        _clock = clock;
    }

    public async Task<IReadOnlyList<DiagnosticTemplateDto>> ListForTeacherAsync(CancellationToken ct = default)
    {
        var templates = await _uow.DiagnosticTemplates.ListAsync(ct);
        var items = await _uow.DiagnosticTemplateQuestions.ListAsync(ct);
        return templates
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => Map(x, items.Count(q => q.DiagnosticTemplateId == x.Id)))
            .ToArray();
    }

    public async Task<IReadOnlyList<DiagnosticTemplateDto>> ListForStudentAsync(Guid studentId, CancellationToken ct = default)
    {
        var templates = await _uow.DiagnosticTemplates.WhereAsync(x => x.IsPublished, ct);
        var items = await _uow.DiagnosticTemplateQuestions.ListAsync(ct);
        var examTemplateIds = (await _uow.ExamDefinitions.ListAsync(ct)).Select(x => x.DiagnosticTemplateId).ToHashSet();
        var materialTemplateIds = (await _uow.MaterialStudyCycles.ListAsync(ct))
            .SelectMany(x => new[] { x.PreDiagnosticTemplateId, x.PostDiagnosticTemplateId })
            .ToHashSet();
        var memberships = (await _uow.StudentGroupMembers.WhereAsync(x => x.StudentId == studentId, ct))
            .Select(x => x.GroupId)
            .ToHashSet();
        return templates
            .Where(x => !examTemplateIds.Contains(x.Id) && !materialTemplateIds.Contains(x.Id))
            .Where(x =>
                (!x.StudentId.HasValue && !x.GroupId.HasValue) ||
                x.StudentId == studentId ||
                (x.GroupId.HasValue && memberships.Contains(x.GroupId.Value)))
            .OrderByDescending(x => x.PublishedAt ?? x.CreatedAt)
            .Select(x => Map(x, items.Count(q => q.DiagnosticTemplateId == x.Id)))
            .ToArray();
    }

    public async Task<IReadOnlyList<DiagnosticTemplateQuestionCandidateDto>> ListQuestionsAsync(Guid actorId, CancellationToken ct = default)
    {
        var questions = await _uow.Questions.WhereAsync(
            x => x.Status == ContentStatus.Published && x.Kind == QuestionKind.Diagnostic,
            ct);
        var versions = await _uow.QuestionVersions.ListAsync(ct);
        var topics = (await _uow.Topics.ListAsync(ct)).ToDictionary(x => x.Id);
        var createdByActor = (await _uow.AuditLogs.WhereAsync(
            x => x.ActorUserId == actorId &&
                 x.Action == AuditAction.Created &&
                 x.EntityType == nameof(Question) &&
                 x.EntityId.HasValue,
            ct))
            .Select(x => x.EntityId!.Value)
            .ToHashSet();

        return questions
            .Select(question =>
            {
                var version = versions.SingleOrDefault(x =>
                    x.QuestionId == question.Id && x.VersionNumber == question.CurrentVersionNumber);
                if (version is null || !topics.TryGetValue(question.TopicId, out var topic))
                    return null;
                return new DiagnosticTemplateQuestionCandidateDto(
                    question.Id,
                    question.Code,
                    question.TopicId,
                    topic.Code,
                    topic.NameRu,
                    version.Prompt,
                    version.Difficulty,
                    question.CreatedAt,
                    createdByActor.Contains(question.Id));
            })
            .Where(x => x is not null)
            .Cast<DiagnosticTemplateQuestionCandidateDto>()
            .GroupBy(x => NormalizePrompt(x.Prompt))
            .Select(group => group
                .OrderByDescending(x => x.AddedByCurrentUser)
                .ThenByDescending(x => x.CreatedAt)
                .First())
            .OrderByDescending(x => x.CreatedAt)
            .ThenBy(x => x.TopicName)
            .ThenBy(x => x.Code)
            .ToArray();
    }



    private static string NormalizePrompt(string prompt) =>
        string.Join(" ", prompt
            .Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            .Trim()
            .ToLowerInvariant();

    public async Task<DiagnosticTemplateQuestionCandidateDto> CreateQuestionAsync(
        Guid actorId,
        CreateDiagnosticTemplateQuestionRequest request,
        CancellationToken ct = default)
    {
        var topic = await _uow.Topics.GetByIdAsync(request.TopicId, ct)
            ?? throw new KeyNotFoundException("Тема не найдена.");
        var prompt = (request.Prompt ?? string.Empty).Trim();
        var explanation = (request.CorrectExplanation ?? string.Empty).Trim();
        if (prompt.Length < 5 || prompt.Length > 8000)
            throw new InvalidOperationException("Введите условие задачи длиной от 5 до 8000 символов.");
        if (explanation.Length < 2 || explanation.Length > 12000)
            throw new InvalidOperationException("Добавьте краткий разбор правильного ответа.");
        if (request.Options is null || request.Options.Count is < 2 or > 8)
            throw new InvalidOperationException("У задачи должно быть от 2 до 8 вариантов ответа.");
        if (request.Options.Count(x => x.IsCorrect) != 1)
            throw new InvalidOperationException("Отметьте ровно один правильный вариант ответа.");
        if (request.Options.Any(x => string.IsNullOrWhiteSpace(x.Text)))
            throw new InvalidOperationException("Все варианты ответа должны быть заполнены.");

        var code = $"teacher_{_clock.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}"[..38];
        var question = new Question
        {
            TopicId = topic.Id,
            Code = code,
            Kind = QuestionKind.Diagnostic,
            Status = ContentStatus.Published,
            CurrentVersionNumber = 1,
            PublishedAt = _clock.UtcNow
        };
        var version = new QuestionVersion
        {
            QuestionId = question.Id,
            VersionNumber = 1,
            Prompt = prompt,
            CorrectExplanation = explanation,
            Difficulty = request.Difficulty,
            IsTransferQuestion = request.Difficulty == QuestionDifficulty.Transfer,
            EstimatedSeconds = request.Difficulty is QuestionDifficulty.Advanced or QuestionDifficulty.Transfer ? 150 : 90,
            AuthorNotes = string.Empty
        };

        await _uow.Questions.AddAsync(question, ct);
        await _uow.QuestionVersions.AddAsync(version, ct);
        var position = 0;
        foreach (var optionRequest in request.Options)
        {
            position++;
            await _uow.AnswerOptions.AddAsync(new AnswerOption
            {
                QuestionVersionId = version.Id,
                Text = optionRequest.Text.Trim(),
                IsCorrect = optionRequest.IsCorrect,
                Feedback = string.IsNullOrWhiteSpace(optionRequest.Feedback)
                    ? optionRequest.IsCorrect ? "Верно." : "Ответ неверный. Сверьтесь с разбором."
                    : optionRequest.Feedback.Trim(),
                SortOrder = position
            }, ct);
        }

        await _uow.AuditLogs.AddAsync(new AuditLog
        {
            ActorUserId = actorId,
            Action = AuditAction.Created,
            EntityType = nameof(Question),
            EntityId = question.Id,
            OldValueJson = "{}",
            NewValueJson = $"{{\"source\":\"diagnostic-constructor\",\"code\":\"{code}\"}}",
            RequestId = Guid.NewGuid().ToString("N")
        }, ct);

        await _uow.SaveChangesAsync(ct);
        return new DiagnosticTemplateQuestionCandidateDto(
            question.Id,
            question.Code,
            topic.Id,
            topic.Code,
            topic.NameRu,
            version.Prompt,
            version.Difficulty,
            question.CreatedAt,
            true);
    }

    public async Task<DiagnosticTemplateDto> CreateAsync(
        Guid actorId,
        CreateDiagnosticTemplateRequest request,
        CancellationToken ct = default)
    {
        var title = (request.Title ?? string.Empty).Trim();
        var description = (request.Description ?? string.Empty).Trim();
        if (title.Length == 0)
            throw new InvalidOperationException("Введите название диагностики.");
        if (title.Length > 300)
            throw new InvalidOperationException("Название диагностики длиннее 300 символов.");
        if (description.Length > 2000)
            throw new InvalidOperationException("Описание диагностики длиннее 2000 символов.");

        if (request.GroupId.HasValue && request.StudentId.HasValue)
            throw new InvalidOperationException("Выберите либо группу, либо конкретного студента.");
        if (request.GroupId.HasValue)
            _ = await _uow.StudentGroups.GetByIdAsync(request.GroupId.Value, ct)
                ?? throw new KeyNotFoundException("Группа не найдена.");
        if (request.StudentId.HasValue)
        {
            var student = await _uow.Users.GetByIdAsync(request.StudentId.Value, ct)
                ?? throw new KeyNotFoundException("Студент не найден.");
            if (student.Role != UserRole.Student || !student.IsActive || student.Email.EndsWith("@probmind.test", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Выбранный студент недоступен для назначения.");
        }

        var questionIds = request.QuestionIds.Distinct().ToArray();
        if (questionIds.Length < 1 || questionIds.Length > 30)
            throw new InvalidOperationException("В диагностике должно быть от 1 до 30 заданий.");

        foreach (var questionId in questionIds)
        {
            var question = await _uow.Questions.GetByIdAsync(questionId, ct)
                ?? throw new KeyNotFoundException("Одно из выбранных заданий не найдено.");
            if (question.Status != ContentStatus.Published || question.Kind != QuestionKind.Diagnostic)
                throw new InvalidOperationException($"Задание '{question.Code}' недоступно для диагностики.");
        }

        var template = new DiagnosticTemplate
        {
            CreatedByUserId = actorId,
            GroupId = request.GroupId,
            StudentId = request.StudentId,
            Title = title,
            Description = description,
            IsPublished = request.PublishForStudents,
            PublishedAt = request.PublishForStudents ? _clock.UtcNow : null
        };

        await _uow.DiagnosticTemplates.AddAsync(template, ct);
        for (var index = 0; index < questionIds.Length; index++)
        {
            await _uow.DiagnosticTemplateQuestions.AddAsync(new DiagnosticTemplateQuestion
            {
                DiagnosticTemplateId = template.Id,
                QuestionId = questionIds[index],
                Position = index + 1
            }, ct);
        }

        await _uow.SaveChangesAsync(ct);
        return Map(template, questionIds.Length);
    }

    public async Task<DiagnosticTemplateDto> SetPublishedAsync(
        Guid actorId,
        Guid templateId,
        bool isPublished,
        CancellationToken ct = default)
    {
        var template = await _uow.DiagnosticTemplates.GetByIdAsync(templateId, ct)
            ?? throw new KeyNotFoundException("Диагностика не найдена.");
        template.IsPublished = isPublished;
        template.PublishedAt = isPublished ? _clock.UtcNow : null;
        template.Touch();
        _uow.DiagnosticTemplates.Update(template);
        await _uow.SaveChangesAsync(ct);
        var items = await _uow.DiagnosticTemplateQuestions.WhereAsync(
            x => x.DiagnosticTemplateId == templateId,
            ct);
        return Map(template, items.Count);
    }


    public async Task<DiagnosticTemplateDto> SetAudienceAsync(
        Guid actorId,
        Guid templateId,
        Guid? groupId,
        Guid? studentId,
        CancellationToken ct = default)
    {
        if (groupId.HasValue && studentId.HasValue)
            throw new InvalidOperationException("Выберите либо группу, либо конкретного студента.");
        if (groupId.HasValue)
            _ = await _uow.StudentGroups.GetByIdAsync(groupId.Value, ct)
                ?? throw new KeyNotFoundException("Группа не найдена.");
        if (studentId.HasValue)
        {
            var student = await _uow.Users.GetByIdAsync(studentId.Value, ct)
                ?? throw new KeyNotFoundException("Студент не найден.");
            if (student.Role != UserRole.Student || !student.IsActive || student.Email.EndsWith("@probmind.test", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Выбранный студент недоступен для назначения.");
        }

        var template = await _uow.DiagnosticTemplates.GetByIdAsync(templateId, ct)
            ?? throw new KeyNotFoundException("Диагностика не найдена.");
        template.GroupId = groupId;
        template.StudentId = studentId;
        template.Touch();
        _uow.DiagnosticTemplates.Update(template);
        await _uow.SaveChangesAsync(ct);
        var items = await _uow.DiagnosticTemplateQuestions.WhereAsync(x => x.DiagnosticTemplateId == templateId, ct);
        return Map(template, items.Count);
    }


    public async Task<DiagnosticTemplateDto> AddQuestionsAsync(
        Guid actorId,
        Guid templateId,
        IReadOnlyList<Guid> questionIds,
        CancellationToken ct = default)
    {
        var template = await _uow.DiagnosticTemplates.GetByIdAsync(templateId, ct)
            ?? throw new KeyNotFoundException("Диагностика не найдена.");
        var existingItems = (await _uow.DiagnosticTemplateQuestions.WhereAsync(
            x => x.DiagnosticTemplateId == templateId,
            ct)).OrderBy(x => x.Position).ToArray();
        var existingIds = existingItems.Select(x => x.QuestionId).ToHashSet();
        var additions = questionIds.Distinct().Where(x => !existingIds.Contains(x)).ToArray();

        if (additions.Length == 0)
            return Map(template, existingItems.Length);
        if (existingItems.Length + additions.Length > 30)
            throw new InvalidOperationException("В диагностике может быть не больше 30 заданий.");

        foreach (var questionId in additions)
        {
            var question = await _uow.Questions.GetByIdAsync(questionId, ct)
                ?? throw new KeyNotFoundException("Одно из выбранных заданий не найдено.");
            if (question.Status != ContentStatus.Published || question.Kind != QuestionKind.Diagnostic)
                throw new InvalidOperationException($"Задание '{question.Code}' недоступно для диагностики.");
        }

        var position = existingItems.Length;
        foreach (var questionId in additions)
        {
            position++;
            await _uow.DiagnosticTemplateQuestions.AddAsync(new DiagnosticTemplateQuestion
            {
                DiagnosticTemplateId = templateId,
                QuestionId = questionId,
                Position = position
            }, ct);
        }

        template.Touch();
        _uow.DiagnosticTemplates.Update(template);
        await _uow.SaveChangesAsync(ct);
        return Map(template, position);
    }

    private static DiagnosticTemplateDto Map(DiagnosticTemplate template, int questionCount) =>
        new(
            template.Id,
            template.Title,
            template.Description,
            questionCount,
            template.GroupId,
            template.StudentId,
            template.IsPublished,
            template.PublishedAt,
            template.CreatedAt);
}
