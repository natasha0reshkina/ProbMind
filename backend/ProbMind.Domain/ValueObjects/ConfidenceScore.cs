namespace ProbMind.Domain.ValueObjects;

public readonly record struct ConfidenceScore
{
    public double Value { get; }

    public ConfidenceScore(double value)
    {
        Value = Math.Clamp(value, 0d, 1d);
    }

    public int Percent => (int)Math.Round(Value * 100d);

    public static implicit operator double(ConfidenceScore score) => score.Value;
    public static implicit operator ConfidenceScore(double value) => new(value);
}
