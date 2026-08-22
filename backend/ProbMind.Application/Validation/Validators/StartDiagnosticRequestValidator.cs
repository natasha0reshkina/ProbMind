using ProbMind.Application.Contracts;

namespace ProbMind.Application.Validation;

public sealed class StartDiagnosticRequestValidator : RequestValidator<StartDiagnosticRequest>
{
    public override IReadOnlyList<ValidationIssue> Validate(StartDiagnosticRequest request)
    {
        var issues = new List<ValidationIssue>();
        Range(issues, "questionCount", request.QuestionCount, 10, 30);
        return issues;
    }
}
