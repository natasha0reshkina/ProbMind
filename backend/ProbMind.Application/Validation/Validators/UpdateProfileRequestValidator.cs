using ProbMind.Application.Contracts;

namespace ProbMind.Application.Validation;

public sealed class UpdateProfileRequestValidator : RequestValidator<UpdateProfileRequest>
{
    public override IReadOnlyList<ValidationIssue> Validate(UpdateProfileRequest request)
    {
        var issues = new List<ValidationIssue>();
        Required(issues, "displayName", request.DisplayName, 120);
        return issues;
    }
}
