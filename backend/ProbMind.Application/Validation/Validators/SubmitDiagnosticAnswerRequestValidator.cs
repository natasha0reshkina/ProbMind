using ProbMind.Application.Contracts;

namespace ProbMind.Application.Validation;

public sealed class SubmitDiagnosticAnswerRequestValidator : RequestValidator<SubmitDiagnosticAnswerRequest>
{
    public override IReadOnlyList<ValidationIssue> Validate(SubmitDiagnosticAnswerRequest request)
    {
        var issues = new List<ValidationIssue>();
        if (request.SessionId == Guid.Empty) issues.Add(new("sessionId", "required", "Session id is required."));
        if (request.QuestionId == Guid.Empty) issues.Add(new("questionId", "required", "Question id is required."));
        if (request.QuestionVersionId == Guid.Empty) issues.Add(new("questionVersionId", "required", "Question version id is required."));
        if (request.AnswerOptionId == Guid.Empty) issues.Add(new("answerOptionId", "required", "Answer option id is required."));
        if (request.ResponseTimeMs < 0) issues.Add(new("responseTimeMs", "range", "Response time cannot be negative."));
        if (request.ResponseTimeMs > 3_600_000) issues.Add(new("responseTimeMs", "range", "Response time is implausibly high."));
        return issues;
    }
}
