using ProbMind.Application.Abstractions.Clock;

namespace ProbMind.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
