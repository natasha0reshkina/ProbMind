using ProbMind.Application.Contracts;

namespace ProbMind.Application.Validation;

public sealed class RegisterRequestValidator : RequestValidator<RegisterRequest>
{
    public override IReadOnlyList<ValidationIssue> Validate(RegisterRequest request)
    {
        var issues = new List<ValidationIssue>();
        Required(issues, "email", request.Email, 254);
        Required(issues, "displayName", request.DisplayName, 120);
        Required(issues, "password", request.Password, 200);

        if (!string.IsNullOrWhiteSpace(request.Email) && !request.Email.Contains('@'))
            issues.Add(new ValidationIssue("email", "format", "Email must contain @."));

        if (!string.IsNullOrEmpty(request.Password))
        {
            if (request.Password.Length < 10)
                issues.Add(new ValidationIssue("password", "min_length", "Password must contain at least 10 characters."));
            if (!request.Password.Any(char.IsUpper))
                issues.Add(new ValidationIssue("password", "uppercase", "Password must contain an uppercase letter."));
            if (!request.Password.Any(char.IsLower))
                issues.Add(new ValidationIssue("password", "lowercase", "Password must contain a lowercase letter."));
            if (!request.Password.Any(char.IsDigit))
                issues.Add(new ValidationIssue("password", "digit", "Password must contain a digit."));
        }

        return issues;
    }
}
