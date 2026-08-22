using ProbMind.Domain.Enums;

namespace ProbMind.Infrastructure.Seeding;

public sealed record SeedOptionDefinition(
    string Text,
    bool IsCorrect,
    string? MisconceptionCode,
    string Feedback);

public sealed record SeedQuestionDefinition(
    string Code,
    string TopicCode,
    QuestionKind Kind,
    QuestionDifficulty Difficulty,
    bool IsTransfer,
    string Prompt,
    string CorrectExplanation,
    IReadOnlyList<string> TestedMisconceptionCodes,
    IReadOnlyList<SeedOptionDefinition> Options);
