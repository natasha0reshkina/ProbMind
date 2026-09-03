using ProbMind.Application.Contracts;

namespace ProbMind.Application.Validation;

public sealed class LogoutRequestValidator : RequestValidator<LogoutRequest>
{
    public override IReadOnlyList<ValidationIssue> Validate(LogoutRequest request)
    {
        var issues = new List<ValidationIssue>();
        Required(issues, "refreshToken", request.RefreshToken, 4096);
        return issues;
    }
}
