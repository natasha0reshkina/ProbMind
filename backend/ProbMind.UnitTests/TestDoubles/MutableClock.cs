using ProbMind.Application.Abstractions.Clock;

namespace ProbMind.UnitTests.TestDoubles;

public sealed class MutableClock : IClock
{
    public MutableClock(DateTimeOffset? initial = null)
    {
        UtcNow = initial ?? new DateTimeOffset(2026, 8, 14, 0, 0, 0, TimeSpan.Zero);
    }

    public DateTimeOffset UtcNow { get; private set; }

    public void Advance(TimeSpan amount) => UtcNow = UtcNow.Add(amount);
    public void Set(DateTimeOffset value) => UtcNow = value;
}
