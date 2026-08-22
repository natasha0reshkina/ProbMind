using ProbMind.Application.Contracts;

namespace ProbMind.Application.Validation;

public sealed class LoginRequestValidator : RequestValidator<LoginRequest>
{
    public override IReadOnlyList<ValidationIssue> Validate(LoginRequest request)
    {
        var issues = new List<ValidationIssue>();
        Required(issues, "email", request.Email, 254);
        Required(issues, "password", request.Password, 200);
        return issues;
    }
}
