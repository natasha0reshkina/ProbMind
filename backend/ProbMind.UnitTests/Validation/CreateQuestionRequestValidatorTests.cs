using ProbMind.Application.Contracts;
using ProbMind.Application.Validation;
using ProbMind.Domain.Enums;

namespace ProbMind.UnitTests.Validation;

public sealed class CreateQuestionRequestValidatorTests
{
    private readonly CreateQuestionRequestValidator _sut = new();

    [Fact]
    public void ValidDiagnosticQuestion_HasNoIssues()
    {
        var result = _sut.Validate(Valid());
        Assert.Empty(result);
    }

    [Fact]
    public void EmptyTopic_IsRejected()
    {
        var request = Valid() with { TopicId = Guid.Empty };
        Assert.Contains(_sut.Validate(request), x => x.Field == "topicId");
    }

    [Fact]
    public void QuestionRequiresExactlyOneCorrectOption()
    {
        var request = Valid() with
        {
            Options =
            [
                new("A", false, Guid.NewGuid(), "A", 1),
                new("B", false, Guid.NewGuid(), "B", 2)
            ]
        };
        Assert.Contains(_sut.Validate(request), x => x.Code == "correct_count");
    }

    [Fact]
    public void DiagnosticDistractorShouldMapToMisconception()
    {
        var request = Valid() with
        {
            Options =
            [
                new("Correct", true, null, "Yes", 1),
                new("Wrong", false, null, "No", 2)
            ]
        };
        Assert.Contains(_sut.Validate(request), x => x.Code == "diagnostic_mapping");
    }

    [Fact]
    public void DiagnosticQuestionRequiresTestedMisconception()
    {
        var request = Valid() with { TestedMisconceptionIds = [] };
        Assert.Contains(_sut.Validate(request), x => x.Code == "coverage");
    }

    [Fact]
    public void TwoToEightOptionsAreAllowed()
    {
        foreach (var count in Enumerable.Range(2, 7))
        {
            var options = Enumerable.Range(0, count)
                .Select(i => new CreateAnswerOptionRequest(
                    $"Option {i}",
                    i == 0,
                    i == 0 ? null : Guid.NewGuid(),
                    "Feedback",
                    i))
                .ToArray();
            var request = Valid() with { Options = options };
            Assert.DoesNotContain(_sut.Validate(request), x => x.Code == "count");
        }
    }

    private static CreateQuestionRequest Valid() =>
        new(
            Guid.NewGuid(),
            "question_code",
            QuestionKind.Diagnostic,
            "Question prompt?",
            "Correct explanation.",
            QuestionDifficulty.Intermediate,
            false,
            [
                new("Correct", true, null, "Correct.", 1),
                new("Distractor", false, Guid.NewGuid(), "Misconception-linked.", 2)
            ],
            [Guid.NewGuid()]);
}
