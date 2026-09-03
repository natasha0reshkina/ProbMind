using ProbMind.Application.Contracts;

namespace ProbMind.Application.Validation;

public sealed class RefreshRequestValidator : RequestValidator<RefreshRequest>
{
    public override IReadOnlyList<ValidationIssue> Validate(RefreshRequest request)
    {
        var issues = new List<ValidationIssue>();
        Required(issues, "refreshToken", request.RefreshToken, 4096);
        return issues;
    }
}
