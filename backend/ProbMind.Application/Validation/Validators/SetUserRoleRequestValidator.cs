using ProbMind.Application.Contracts;

namespace ProbMind.Application.Validation;

public sealed class SetUserRoleRequestValidator : RequestValidator<SetUserRoleRequest>
{
    public override IReadOnlyList<ValidationIssue> Validate(SetUserRoleRequest request)
    {
        var issues = new List<ValidationIssue>();
        if (request.UserId == Guid.Empty) issues.Add(new("userId", "required", "User id is required."));
        if (!Enum.IsDefined(request.Role)) issues.Add(new("role", "enum", "Unknown role."));
        return issues;
    }
}
