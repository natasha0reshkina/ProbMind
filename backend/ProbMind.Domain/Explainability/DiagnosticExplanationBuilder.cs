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
            >= 0.70d => $"В ответах устойчиво проявляется паттерн «{misconceptionTitle}». Уверенность диагностики — {confidence:P0}.",
            >= 0.40d => $"Есть признаки паттерна «{misconceptionTitle}», но для уверенного вывода нужны дополнительные ответы. Текущая оценка — {confidence:P0}.",
            _ => $"Данных в пользу паттерна «{misconceptionTitle}» пока недостаточно. Текущая оценка — {confidence:P0}."
        };

        var nextAction = correctionInProgress
            ? "Завершить текущую коррекционную практику и выполнить задание на перенос."
            : confidence >= 0.50d
                ? "Пройти объяснение и коррекционные задания по этой ошибке."
                : "Продолжить диагностику, чтобы собрать дополнительные наблюдения.";

        return new DiagnosticExplanation(
            summary,
            band,
            supporting,
            contradicting,
            nextAction);
    }
}
