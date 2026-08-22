using ProbMind.Domain.Enums;

namespace ProbMind.Application.Contracts;

public sealed record TopicDto(
    Guid Id,
    string Code,
    string NameRu,
    string NameEn,
    string Description,
    int SortOrder,
    ContentStatus Status);

public sealed record MisconceptionCatalogDto(
    Guid Id,
    Guid TopicId,
    string Code,
    string Title,
    string Description,
    string CorrectiveExplanation,
    string DiagnosticRationale,
    ContentStatus Status);

public sealed record QuestionSummaryDto(
    Guid Id,
    Guid TopicId,
    string Code,
    QuestionKind Kind,
    ContentStatus Status,
    int CurrentVersionNumber,
    DateTimeOffset? PublishedAt);

public sealed record CreateQuestionRequest(
    Guid TopicId,
    string Code,
    QuestionKind Kind,
    string Prompt,
    string CorrectExplanation,
    QuestionDifficulty Difficulty,
    bool IsTransfer,
    IReadOnlyList<CreateAnswerOptionRequest> Options,
    IReadOnlyList<Guid> TestedMisconceptionIds);

public sealed record CreateAnswerOptionRequest(
    string Text,
    bool IsCorrect,
    Guid? MisconceptionId,
    string Feedback,
    int SortOrder);

public sealed record CreateQuestionVersionRequest(
    Guid QuestionId,
    string Prompt,
    string CorrectExplanation,
    QuestionDifficulty Difficulty,
    bool IsTransfer,
    IReadOnlyList<CreateAnswerOptionRequest> Options);

public sealed record QuestionDetailDto(
    QuestionSummaryDto Question,
    QuestionVersionDto CurrentVersion,
    IReadOnlyList<Guid> TestedMisconceptionIds);

public sealed record QuestionVersionDto(
    Guid Id,
    int VersionNumber,
    string Prompt,
    string CorrectExplanation,
    QuestionDifficulty Difficulty,
    bool IsTransfer,
    IReadOnlyList<AnswerOptionAdminDto> Options);

public sealed record AnswerOptionAdminDto(
    Guid Id,
    string Text,
    bool IsCorrect,
    Guid? MisconceptionId,
    string Feedback,
    int SortOrder);
