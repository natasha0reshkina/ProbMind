using ProbMind.Application.Contracts;

namespace ProbMind.Application.Validation;

public sealed class StartPracticeRequestValidator : RequestValidator<StartPracticeRequest>
{
    public override IReadOnlyList<ValidationIssue> Validate(StartPracticeRequest request)
    {
        var issues = new List<ValidationIssue>();
        if (request.TopicId == Guid.Empty) issues.Add(new("topicId", "required", "Topic id is required."));
        if (request.MisconceptionId == Guid.Empty) issues.Add(new("misconceptionId", "invalid", "Misconception id cannot be empty."));
        Range(issues, "targetExercises", request.TargetExercises, 3, 12);
        return issues;
    }
}
