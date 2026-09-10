namespace ProbMind.Domain.Explainability;

public sealed record ExplanationEvidence(
    string Description,
    double Contribution,
    DateTimeOffset ObservedAt);

public sealed record DiagnosticExplanation(
    string Summary,
    string ConfidenceBand,
    IReadOnlyList<string> SupportingReasons,
    IReadOnlyList<string> ContradictingReasons,
    string NextAction);

public sealed class DiagnosticExplanationBuilder
{
    public DiagnosticExplanation Build(
        string misconceptionTitle,
        double confidence,
        IEnumerable<ExplanationEvidence> evidence,
        bool correctionInProgress)
    {
        var items = evidence
            .OrderByDescending(x => Math.Abs(x.Contribution))
            .ToArray();

        var supporting = items
            .Where(x => x.Contribution > 0d)
            .Take(4)
            .Select(x => x.Description)
            .ToArray();

        var contradicting = items
            .Where(x => x.Contribution < 0d)
            .Take(3)
            .Select(x => x.Description)
            .ToArray();

        var band = confidence switch
        {
            >= 0.85d => "very_high",
            >= 0.70d => "high",
            >= 0.50d => "moderate",
            >= 0.30d => "possible",
            _ => "low"
        };

        var summary = confidence switch
        {
            >= 0.70d => $"Ошибка {misconceptionTitle} устойчиво повторяется в ответах. Уровень подтверждения: {confidence:P0}.",
            >= 0.40d => $"Есть признаки ошибки {misconceptionTitle}, но нужны дополнительные ответы. Уровень подтверждения: {confidence:P0}.",
            _ => $"Данных по ошибке {misconceptionTitle} пока недостаточно. Уровень подтверждения: {confidence:P0}."
        };

        var nextAction = correctionInProgress
            ? "Завершить текущую практику и решить дополнительную задачу без подсказок."
            : confidence >= 0.50d
                ? "Повторить объяснение по теме и выполнить несколько практических заданий."
                : "Пройти дополнительные диагностические задания по этой теме, чтобы уточнить результат.";

        return new DiagnosticExplanation(
            summary,
            band,
            supporting,
            contradicting,
            nextAction);
    }
}
