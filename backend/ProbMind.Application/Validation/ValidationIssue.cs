namespace ProbMind.Application.Validation;

public sealed record ValidationIssue(string Field, string Code, string Message);
