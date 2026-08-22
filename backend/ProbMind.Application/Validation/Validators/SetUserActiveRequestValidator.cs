using ProbMind.Application.Contracts;

namespace ProbMind.Application.Validation;

public sealed class SetUserActiveRequestValidator : RequestValidator<SetUserActiveRequest>
{
    public override IReadOnlyList<ValidationIssue> Validate(SetUserActiveRequest request)
    {
        var issues = new List<ValidationIssue>();
        if (request.UserId == Guid.Empty) issues.Add(new("userId", "required", "User id is required."));
        return issues;
    }
}
