using ProbMind.Domain.Explainability;

namespace ProbMind.UnitTests.Explainability;

public sealed class DiagnosticExplanationBuilderTests
{
    [Fact]
    public void HighConfidence_DescribesStablePattern()
    {
        var result = new DiagnosticExplanationBuilder().Build(
            "Ошибка игрока",
            .88d,
            new[] { new ExplanationEvidence("Паттерн повторился", 1d, DateTimeOffset.UtcNow) },
            false);

        Assert.Contains("устойчиво", result.Summary);
        Assert.Equal("very_high", result.ConfidenceBand);
    }

    [Fact]
    public void LowConfidence_AsksForMoreEvidence()
    {
        var result = new DiagnosticExplanationBuilder().Build(
            "Ошибка игрока",
            .2d,
            Array.Empty<ExplanationEvidence>(),
            false);

        Assert.Contains("дополнительные", result.NextAction);
    }

    [Fact]
    public void OngoingCorrection_IsContinued()
    {
        var result = new DiagnosticExplanationBuilder().Build(
            "Ошибка игрока",
            .8d,
            Array.Empty<ExplanationEvidence>(),
            true);

        Assert.Contains("Завершить", result.NextAction);
    }

    [Fact]
    public void Reasons_AreLimitedToMostImportantSignals()
    {
        var evidence = Enumerable.Range(0, 20)
            .Select(i => new ExplanationEvidence(
                $"reason-{i}",
                i % 2 == 0 ? i + 1 : -(i + 1),
                DateTimeOffset.UtcNow));

        var result = new DiagnosticExplanationBuilder().Build(
            "Ошибка игрока",
            .7d,
            evidence,
            false);

        Assert.True(result.SupportingReasons.Count <= 4);
        Assert.True(result.ContradictingReasons.Count <= 3);
    }
}
