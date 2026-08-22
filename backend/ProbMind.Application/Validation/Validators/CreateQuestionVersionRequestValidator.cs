using ProbMind.Application.Contracts;

namespace ProbMind.Application.Validation;

public sealed class CreateQuestionVersionRequestValidator : RequestValidator<CreateQuestionVersionRequest>
{
    public override IReadOnlyList<ValidationIssue> Validate(CreateQuestionVersionRequest request)
    {
        var issues = new List<ValidationIssue>();
        if (request.QuestionId == Guid.Empty) issues.Add(new("questionId", "required", "Question id is required."));
        Required(issues, "prompt", request.Prompt, 8000);
        Required(issues, "correctExplanation", request.CorrectExplanation, 12000);

        if (request.Options.Count is < 2 or > 8)
            issues.Add(new("options", "count", "Version must contain between 2 and 8 options."));
        if (request.Options.Count(x => x.IsCorrect) != 1)
            issues.Add(new("options", "correct_count", "Exactly one answer option must be correct."));

        return issues;
    }
}
