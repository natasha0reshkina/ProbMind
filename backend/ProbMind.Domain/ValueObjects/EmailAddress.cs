using System.Text.RegularExpressions;

namespace ProbMind.Domain.ValueObjects;

public readonly record struct EmailAddress
{
    private static readonly Regex Pattern = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public string Value { get; }

    public EmailAddress(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        if (!Pattern.IsMatch(normalized))
            throw new ArgumentException("Invalid e-mail address.", nameof(value));
        Value = normalized;
    }

    public override string ToString() => Value;
}
