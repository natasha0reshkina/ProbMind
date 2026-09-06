using ProbMind.Domain.Enums;

namespace ProbMind.Application.Contracts;

public sealed record DiagnosticTemplateDto(
    Guid Id,
    string Title,
    string Description,
    int QuestionCount,
    Guid? GroupId,
    Guid? StudentId,
    bool IsPublished,
    DateTimeOffset? PublishedAt,
    DateTimeOffset CreatedAt);

public sealed record DiagnosticTemplateQuestionCandidateDto(
    Guid Id,
    string Code,
    Guid TopicId,
    string TopicCode,
    string TopicName,
    string Prompt,
    QuestionDifficulty Difficulty,
    DateTimeOffset CreatedAt,
    bool AddedByCurrentUser);

public sealed record CreateDiagnosticTemplateRequest(
    string Title,
    string Description,
    IReadOnlyList<Guid> QuestionIds,
    bool PublishForStudents = true,
    Guid? GroupId = null,
    Guid? StudentId = null);

public sealed record SetDiagnosticTemplatePublishedRequest(bool IsPublished);

public sealed record SetDiagnosticTemplateAudienceRequest(Guid? GroupId, Guid? StudentId);

public sealed record AddDiagnosticTemplateQuestionsRequest(IReadOnlyList<Guid> QuestionIds);

public sealed record CreateDiagnosticTemplateQuestionOptionRequest(
    string Text,
    bool IsCorrect,
    string Feedback);

public sealed record CreateDiagnosticTemplateQuestionRequest(
    Guid TopicId,
    string Prompt,
    string CorrectExplanation,
    QuestionDifficulty Difficulty,
    IReadOnlyList<CreateDiagnosticTemplateQuestionOptionRequest> Options);

public sealed record QuestionCsvImportResultDto(
    int ImportedCount,
    IReadOnlyList<Guid> QuestionIds,
    IReadOnlyList<string> Codes);
