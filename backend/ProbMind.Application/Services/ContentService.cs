using System.Text.Json;
using ProbMind.Application.Abstractions.Clock;
using ProbMind.Application.Abstractions.Persistence;
using ProbMind.Application.Common;
using ProbMind.Application.Contracts;
using ProbMind.Domain.Entities;
using ProbMind.Domain.Enums;

namespace ProbMind.Application.Services;

public sealed class ContentService : IContentService
{
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;

    public ContentService(IUnitOfWork uow, IClock clock)
    {
        _uow = uow;
        _clock = clock;
    }

    public async Task<IReadOnlyList<TopicDto>> ListTopicsAsync(CancellationToken ct = default) =>
        (await _uow.Topics.ListAsync(ct))
            .OrderBy(x => x.SortOrder)
            .Select(x => new TopicDto(x.Id, x.Code, x.NameRu, x.NameEn, x.Description, x.SortOrder, x.Status))
            .ToArray();

    public async Task<IReadOnlyList<MisconceptionCatalogDto>> ListMisconceptionsAsync(CancellationToken ct = default) =>
        (await _uow.Misconceptions.ListAsync(ct))
            .OrderBy(x => x.Code)
            .Select(Map)
            .ToArray();

    public async Task<IReadOnlyList<QuestionSummaryDto>> ListQuestionsAsync(
        Guid? topicId,
        ContentStatus? status,
        CancellationToken ct = default)
    {
        var questions = await _uow.Questions.ListAsync(ct);
        return questions
            .Where(x => !topicId.HasValue || x.TopicId == topicId.Value)
            .Where(x => !status.HasValue || x.Status == status.Value)
            .OrderBy(x => x.Code)
            .Select(Map)
            .ToArray();
    }

    public async Task<QuestionDetailDto> GetQuestionAsync(Guid questionId, CancellationToken ct = default)
    {
        var question = await _uow.Questions.GetByIdAsync(questionId, ct)
            ?? throw new KeyNotFoundException("Question not found.");
        return await MapDetailAsync(question, ct);
    }

    public async Task<QuestionDetailDto> CreateQuestionAsync(
        Guid actorId,
        CreateQuestionRequest request,
        CancellationToken ct = default)
    {
        ValidateQuestion(request.Code, request.Prompt, request.Options);
        _ = await _uow.Topics.GetByIdAsync(request.TopicId, ct)
            ?? throw new KeyNotFoundException("Topic not found.");

        if (await _uow.Questions.AnyAsync(x => x.Code == request.Code.Trim(), ct))
            throw new InvalidOperationException("Question code must be unique.");

        var question = new Question
        {
            TopicId = request.TopicId,
            Code = request.Code.Trim(),
            Kind = request.Kind,
            Status = ContentStatus.Draft,
            CurrentVersionNumber = 1
        };

        var version = new QuestionVersion
        {
            QuestionId = question.Id,
            VersionNumber = 1,
            Prompt = request.Prompt.Trim(),
            CorrectExplanation = request.CorrectExplanation.Trim(),
            Difficulty = request.Difficulty,
            IsTransferQuestion = request.IsTransfer,
            EstimatedSeconds = 90,
            AuthorNotes = "Created in teacher content editor."
        };

        await _uow.Questions.AddAsync(question, ct);
        await _uow.QuestionVersions.AddAsync(version, ct);
        await AddOptionsAsync(version.Id, request.Options, ct);
        await AddMapsAsync(question.Id, request.TestedMisconceptionIds, ct);
        await AddAuditAsync(actorId, AuditAction.Created, question.Id, "{}", JsonSerializer.Serialize(request), ct);
        await _uow.SaveChangesAsync(ct);

        return await MapDetailAsync(question, ct);
    }

    public async Task<QuestionDetailDto> CreateVersionAsync(
        Guid actorId,
        CreateQuestionVersionRequest request,
        CancellationToken ct = default)
    {
        ValidateQuestion("existing", request.Prompt, request.Options);
        var question = await _uow.Questions.GetByIdAsync(request.QuestionId, ct)
            ?? throw new KeyNotFoundException("Question not found.");

        var oldVersion = question.CurrentVersionNumber;
        var newVersionNumber = oldVersion + 1;

        var version = new QuestionVersion
        {
            QuestionId = question.Id,
            VersionNumber = newVersionNumber,
            Prompt = request.Prompt.Trim(),
            CorrectExplanation = request.CorrectExplanation.Trim(),
            Difficulty = request.Difficulty,
            IsTransferQuestion = request.IsTransfer,
            EstimatedSeconds = 90,
            AuthorNotes = $"Version {newVersionNumber}; previous version remains immutable."
        };

        await _uow.QuestionVersions.AddAsync(version, ct);
        await AddOptionsAsync(version.Id, request.Options, ct);

        question.CurrentVersionNumber = newVersionNumber;
        question.Touch();
        _uow.Questions.Update(question);

        await AddAuditAsync(
            actorId,
            AuditAction.Updated,
            question.Id,
            JsonSerializer.Serialize(new { currentVersionNumber = oldVersion }),
            JsonSerializer.Serialize(new { currentVersionNumber = newVersionNumber }),
            ct);

        await _uow.SaveChangesAsync(ct);
        return await MapDetailAsync(question, ct);
    }

    public async Task<QuestionDetailDto> PublishAsync(Guid actorId, Guid questionId, CancellationToken ct = default)
    {
        var question = await _uow.Questions.GetByIdAsync(questionId, ct)
            ?? throw new KeyNotFoundException("Question not found.");
        var old = question.Status;
        question.Status = ContentStatus.Published;
        question.PublishedAt = _clock.UtcNow;
        question.Touch();
        _uow.Questions.Update(question);
        await AddAuditAsync(actorId, AuditAction.Published, question.Id,
            JsonSerializer.Serialize(new { status = old }),
            JsonSerializer.Serialize(new { status = question.Status }), ct);
        await _uow.SaveChangesAsync(ct);
        return await MapDetailAsync(question, ct);
    }

    public async Task<QuestionDetailDto> ArchiveAsync(Guid actorId, Guid questionId, CancellationToken ct = default)
    {
        var question = await _uow.Questions.GetByIdAsync(questionId, ct)
            ?? throw new KeyNotFoundException("Question not found.");
        var old = question.Status;
        question.Status = ContentStatus.Archived;
        question.Touch();
        _uow.Questions.Update(question);
        await AddAuditAsync(actorId, AuditAction.Archived, question.Id,
            JsonSerializer.Serialize(new { status = old }),
            JsonSerializer.Serialize(new { status = question.Status }), ct);
        await _uow.SaveChangesAsync(ct);
        return await MapDetailAsync(question, ct);
    }

    private async Task AddOptionsAsync(Guid versionId, IReadOnlyList<CreateAnswerOptionRequest> options, CancellationToken ct)
    {
        await _uow.AnswerOptions.AddRangeAsync(options.Select(x => new AnswerOption
        {
            QuestionVersionId = versionId,
            Text = x.Text.Trim(),
            IsCorrect = x.IsCorrect,
            MisconceptionId = x.MisconceptionId,
            Feedback = x.Feedback.Trim(),
            SortOrder = x.SortOrder
        }), ct);
    }

    private async Task AddMapsAsync(Guid questionId, IReadOnlyList<Guid> misconceptionIds, CancellationToken ct)
    {
        foreach (var id in misconceptionIds.Distinct())
        {
            _ = await _uow.Misconceptions.GetByIdAsync(id, ct)
                ?? throw new KeyNotFoundException($"Misconception {id} not found.");
            await _uow.QuestionMisconceptionMaps.AddAsync(new QuestionMisconceptionMap
            {
                QuestionId = questionId,
                MisconceptionId = id,
                RelevanceWeight = 1d,
                CanDisconfirm = true
            }, ct);
        }
    }

    private async Task AddAuditAsync(
        Guid actorId,
        AuditAction action,
        Guid entityId,
        string oldValue,
        string newValue,
        CancellationToken ct)
    {
        await _uow.AuditLogs.AddAsync(new AuditLog
        {
            ActorUserId = actorId,
            Action = action,
            EntityType = nameof(Question),
            EntityId = entityId,
            OldValueJson = oldValue,
            NewValueJson = newValue,
            RequestId = Guid.NewGuid().ToString("N")
        }, ct);
    }

    private async Task<QuestionDetailDto> MapDetailAsync(Question question, CancellationToken ct)
    {
        var versions = await _uow.QuestionVersions.WhereAsync(
            x => x.QuestionId == question.Id && x.VersionNumber == question.CurrentVersionNumber, ct);
        var version = versions.Single();
        var options = await _uow.AnswerOptions.WhereAsync(x => x.QuestionVersionId == version.Id, ct);
        var maps = await _uow.QuestionMisconceptionMaps.WhereAsync(x => x.QuestionId == question.Id, ct);

        return new QuestionDetailDto(
            Map(question),
            new QuestionVersionDto(
                version.Id,
                version.VersionNumber,
                version.Prompt,
                version.CorrectExplanation,
                version.Difficulty,
                version.IsTransferQuestion,
                options.OrderBy(x => x.SortOrder).Select(x =>
                    new AnswerOptionAdminDto(x.Id, x.Text, x.IsCorrect, x.MisconceptionId, x.Feedback, x.SortOrder)).ToArray()),
            maps.Select(x => x.MisconceptionId).Distinct().ToArray());
    }

    private static QuestionSummaryDto Map(Question x) =>
        new(x.Id, x.TopicId, x.Code, x.Kind, x.Status, x.CurrentVersionNumber, x.PublishedAt);

    private static MisconceptionCatalogDto Map(Misconception x) =>
        new(x.Id, x.TopicId, x.Code, x.Title, x.Description, x.CorrectiveExplanation, x.DiagnosticRationale, x.Status);

    private static void ValidateQuestion(
        string code,
        string prompt,
        IReadOnlyList<CreateAnswerOptionRequest> options)
    {
        Guard.Required(code, "code", 120);
        Guard.Required(prompt, "prompt", 4000);

        if (options.Count < 2 || options.Count > 8)
            throw new ArgumentException("Question must contain between 2 and 8 options.", "options");

        if (options.Count(x => x.IsCorrect) != 1)
            throw new ArgumentException("Exactly one answer option must be correct.", "options");

        foreach (var option in options)
            Guard.Required(option.Text, "option.text", 1200);
    }
}
