namespace ProbMind.Application.Validation;

public interface IRequestValidator
{
    Type RequestType { get; }
    IReadOnlyList<ValidationIssue> ValidateObject(object request);
}

public interface IRequestValidator<in TRequest> : IRequestValidator
{
    IReadOnlyList<ValidationIssue> Validate(TRequest request);
}

public abstract class RequestValidator<TRequest> : IRequestValidator<TRequest>
{
    public Type RequestType => typeof(TRequest);

    public abstract IReadOnlyList<ValidationIssue> Validate(TRequest request);

    public IReadOnlyList<ValidationIssue> ValidateObject(object request) =>
        request is TRequest typed
            ? Validate(typed)
            : [new ValidationIssue("$", "type_mismatch", $"Expected {typeof(TRequest).Name}.")];

    protected static void Required(
        List<ValidationIssue> issues,
        string field,
        string? value,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            issues.Add(new ValidationIssue(field, "required", $"{field} is required."));
        else if (value.Trim().Length > maxLength)
            issues.Add(new ValidationIssue(field, "too_long", $"{field} must be at most {maxLength} characters."));
    }

    protected static void Range(
        List<ValidationIssue> issues,
        string field,
        int value,
        int min,
        int max)
    {
        if (value < min || value > max)
            issues.Add(new ValidationIssue(field, "range", $"{field} must be between {min} and {max}."));
    }
}
