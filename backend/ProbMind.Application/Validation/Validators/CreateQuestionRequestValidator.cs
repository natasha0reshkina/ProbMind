using ProbMind.Application.Contracts;

namespace ProbMind.Application.Validation;

public sealed class CreateQuestionRequestValidator : RequestValidator<CreateQuestionRequest>
{
    public override IReadOnlyList<ValidationIssue> Validate(CreateQuestionRequest request)
    {
        var issues = new List<ValidationIssue>();
        if (request.TopicId == Guid.Empty) issues.Add(new("topicId", "required", "Topic is required."));
        Required(issues, "code", request.Code, 160);
        Required(issues, "prompt", request.Prompt, 8000);
        Required(issues, "correctExplanation", request.CorrectExplanation, 12000);

        if (request.Options.Count is < 2 or > 8)
            issues.Add(new("options", "count", "Question must have between 2 and 8 answer options."));

        if (request.Options.Count(x => x.IsCorrect) != 1)
            issues.Add(new("options", "correct_count", "Exactly one option must be correct."));

        foreach (var option in request.Options)
        {
            if (string.IsNullOrWhiteSpace(option.Text))
                issues.Add(new("options.text", "required", "Every answer option must contain text."));
            if (!option.IsCorrect && option.MisconceptionId is null && request.Kind.ToString() == "Diagnostic")
                issues.Add(new("options.misconceptionId", "diagnostic_mapping", "Diagnostic distractors should map to a misconception."));
        }

        if (request.TestedMisconceptionIds.Count == 0 && request.Kind.ToString() == "Diagnostic")
            issues.Add(new("testedMisconceptionIds", "coverage", "Diagnostic question must test at least one misconception."));

        return issues;
    }
}
