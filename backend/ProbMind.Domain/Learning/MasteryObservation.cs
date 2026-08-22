using ProbMind.Domain.Enums;

namespace ProbMind.Domain.Learning;

public sealed record MasteryObservation(
    bool IsCorrect,
    QuestionDifficulty Difficulty,
    DateTimeOffset ObservedAt,
    double DiagnosticRelevance = 1d,
    bool IsTransfer = false);
