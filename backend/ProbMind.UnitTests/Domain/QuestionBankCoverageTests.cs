using ProbMind.Domain.Enums;
using ProbMind.Infrastructure.Seeding;

namespace ProbMind.UnitTests.Domain;

public sealed class QuestionBankCoverageTests
{
    private static readonly string[] ExpectedTopics =
    [
        "conditional_probability",
        "independence",
        "p_value",
        "law_large_numbers",
        "randomness"
    ];

    private static readonly string[] ExpectedMisconceptions =
    [
        "conditional_reverse",
        "base_rate_neglect",
        "independence_vs_exclusive",
        "zero_corr_independent",
        "pvalue_probability_null",
        "significance_effect",
        "lln_short_run",
        "gamblers_fallacy",
        "random_must_look_messy",
        "random_equal_probability"
    ];

    [Fact]
    public void BankContainsAllDeclaredTopicsWithBalancedCoverage()
    {
        var questions = AllQuestions();

        Assert.Equal(540, questions.Length);
        Assert.Equal(540, questions.Select(x => x.Code).Distinct().Count());
        Assert.Equal(ExpectedTopics.OrderBy(x => x).ToArray(), questions.Select(x => x.TopicCode).Distinct().OrderBy(x => x).ToArray());

        foreach (var topic in ExpectedTopics)
        {
            Assert.Equal(108, questions.Count(x => x.TopicCode == topic));
            Assert.Equal(100, questions.Count(x => x.TopicCode == topic && x.Kind == QuestionKind.Diagnostic));
        }
    }

    [Fact]
    public void BankContainsDiagnosticCorrectionAndTransferMaterial()
    {
        var questions = AllQuestions();

        Assert.Equal(500, questions.Count(x => x.Kind == QuestionKind.Diagnostic));
        Assert.Equal(20, questions.Count(x => x.Kind == QuestionKind.Corrective));
        Assert.Equal(20, questions.Count(x => x.Kind == QuestionKind.Transfer));
        Assert.True(questions.Count(x => x.IsTransfer) >= 40);
    }

    [Fact]
    public void EveryDiagnosticDistractorMapsToAMisconception()
    {
        var diagnostic = AllQuestions().Where(x => x.Kind == QuestionKind.Diagnostic).ToArray();

        Assert.All(diagnostic, question =>
        {
            Assert.NotEmpty(question.TestedMisconceptionCodes);
            Assert.Single(question.Options, x => x.IsCorrect);
            Assert.All(
                question.Options.Where(x => !x.IsCorrect),
                option => Assert.False(string.IsNullOrWhiteSpace(option.MisconceptionCode)));
        });
    }

    [Fact]
    public void EveryDeclaredMisconceptionAppearsInDiagnosticMaterial()
    {
        var diagnosticCodes = AllQuestions()
            .Where(x => x.Kind == QuestionKind.Diagnostic)
            .SelectMany(x => x.Options)
            .Where(x => !x.IsCorrect && x.MisconceptionCode is not null)
            .Select(x => x.MisconceptionCode!)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(ExpectedMisconceptions.OrderBy(x => x).ToArray(), diagnosticCodes.OrderBy(x => x).ToArray());
    }

    private static SeedQuestionDefinition[] AllQuestions() =>
    [
        .. ConditionalProbabilityQuestionSeed.All,
        .. IndependenceQuestionSeed.All,
        .. PValueQuestionSeed.All,
        .. LawLargeNumbersQuestionSeed.All,
        .. RandomnessQuestionSeed.All,
        .. ExtendedQuestionSeed.All,
        .. LargeQuestionBankSeed.All
    ];
}
