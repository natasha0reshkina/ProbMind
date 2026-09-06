using ProbMind.Application.Contracts;

namespace ProbMind.Application.Validation;

public sealed class SubmitPracticeAnswerRequestValidator : RequestValidator<SubmitPracticeAnswerRequest>
{
    public override IReadOnlyList<ValidationIssue> Validate(SubmitPracticeAnswerRequest request)
    {
        var issues = new List<ValidationIssue>();
        if (request.SessionId == Guid.Empty) issues.Add(new("sessionId", "required", "Session id is required."));
        if (request.QuestionId == Guid.Empty) issues.Add(new("questionId", "required", "Question id is required."));
        if (request.QuestionVersionId == Guid.Empty) issues.Add(new("questionVersionId", "required", "Version id is required."));
        if (request.AnswerOptionId == Guid.Empty) issues.Add(new("answerOptionId", "required", "Answer option id is required."));
        if (!Enum.IsDefined(request.ExerciseType)) issues.Add(new("exerciseType", "enum", "Unknown exercise type."));
        if (request.ResponseTimeMs < 0) issues.Add(new("responseTimeMs", "range", "Response time cannot be negative."));
        if (request.ResponseTimeMs > 3_600_000) issues.Add(new("responseTimeMs", "range", "Response time is implausibly high."));
        if ((request.StudentNote?.Length ?? 0) > 2000) issues.Add(new("studentNote", "length", "Student note is too long."));
        if (request.ConfidenceLevel.HasValue && request.ConfidenceLevel.Value is < 1 or > 4) issues.Add(new("confidenceLevel", "range", "Confidence level must be between 1 and 4."));
        if ((request.Reasoning?.Length ?? 0) > 4000) issues.Add(new("reasoning", "length", "Reasoning is too long."));
        return issues;
    }
}
