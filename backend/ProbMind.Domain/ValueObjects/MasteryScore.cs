namespace ProbMind.Domain.ValueObjects;

public readonly record struct MasteryScore
{
    public double Value { get; }

    public MasteryScore(double value)
    {
        Value = Math.Clamp(value, 0d, 1d);
    }

    public int Percent => (int)Math.Round(Value * 100d);

    public static implicit operator double(MasteryScore score) => score.Value;
    public static implicit operator MasteryScore(double value) => new(value);
}
