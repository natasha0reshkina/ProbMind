namespace ProbMind.Application.Common;

public static class Guard
{
    public static string Required(string? value, string field, int maxLength = 500)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
            throw new ArgumentException($"{field} is required.", field);
        if (normalized.Length > maxLength)
            throw new ArgumentException($"{field} exceeds {maxLength} characters.", field);
        return normalized;
    }

    public static string Email(string? value)
    {
        var normalized = Required(value, "email", 254).ToLowerInvariant();
        if (!normalized.Contains('@') || normalized.StartsWith('@') || normalized.EndsWith('@'))
            throw new ArgumentException("Invalid e-mail.", "email");
        return normalized;
    }

    public static int Range(int value, int min, int max, string field)
    {
        if (value < min || value > max)
            throw new ArgumentOutOfRangeException(field, $"{field} must be between {min} and {max}.");
        return value;
    }

    public static double Probability(double value, string field)
    {
        if (value is < 0d or > 1d)
            throw new ArgumentOutOfRangeException(field, $"{field} must be in [0,1].");
        return value;
    }
}
