using System.Collections.Concurrent;

namespace ProbMind.Api.Operations;

public sealed class ApiMetricsRegistry
{
    private sealed class MutableMetric
    {
        private long _requests;
        private long _failures;
        private long _totalMicroseconds;
        private long _maxMicroseconds;
        private long _lastSeenUnixMilliseconds;

        public void Record(TimeSpan elapsed, bool failed, DateTimeOffset now)
        {
            var microseconds = Math.Max(0L, elapsed.Ticks / 10L);
            Interlocked.Increment(ref _requests);
            if (failed) Interlocked.Increment(ref _failures);
            Interlocked.Add(ref _totalMicroseconds, microseconds);
            UpdateMax(microseconds);
            Interlocked.Exchange(ref _lastSeenUnixMilliseconds, now.ToUnixTimeMilliseconds());
        }

        public (long Requests, long Failures, double MeanMs, double MaxMs, DateTimeOffset? LastSeenAt) Snapshot()
        {
            var requests = Interlocked.Read(ref _requests);
            var failures = Interlocked.Read(ref _failures);
            var totalMicroseconds = Interlocked.Read(ref _totalMicroseconds);
            var maxMicroseconds = Interlocked.Read(ref _maxMicroseconds);
            var lastSeen = Interlocked.Read(ref _lastSeenUnixMilliseconds);
            return (
                requests,
                failures,
                requests == 0 ? 0d : totalMicroseconds / 1000d / requests,
                maxMicroseconds / 1000d,
                lastSeen <= 0 ? null : DateTimeOffset.FromUnixTimeMilliseconds(lastSeen));
        }

        private void UpdateMax(long value)
        {
            while (true)
            {
                var current = Interlocked.Read(ref _maxMicroseconds);
                if (value <= current) return;
                if (Interlocked.CompareExchange(ref _maxMicroseconds, value, current) == current) return;
            }
        }
    }

    private readonly ConcurrentDictionary<(string Method, string Route), MutableMetric> _metrics = new();

    public void Record(string method, string route, TimeSpan elapsed, int statusCode, DateTimeOffset now)
    {
        var normalizedMethod = string.IsNullOrWhiteSpace(method) ? "UNKNOWN" : method.ToUpperInvariant();
        var normalizedRoute = string.IsNullOrWhiteSpace(route) ? "/" : route;
        var metric = _metrics.GetOrAdd((normalizedMethod, normalizedRoute), _ => new MutableMetric());
        metric.Record(elapsed, statusCode >= 500, now);
    }

    public ApiRuntimeSnapshot Snapshot(DateTimeOffset now, int top = 100)
    {
        var endpoints = _metrics
            .Select(pair =>
            {
                var snapshot = pair.Value.Snapshot();
                return new ApiEndpointMetric(
                    pair.Key.Method,
                    pair.Key.Route,
                    snapshot.Requests,
                    snapshot.Failures,
                    snapshot.MeanMs,
                    snapshot.MaxMs,
                    snapshot.LastSeenAt);
            })
            .OrderByDescending(x => x.Requests)
            .ThenBy(x => x.Route)
            .Take(Math.Clamp(top, 1, 500))
            .ToArray();

        var requests = endpoints.Sum(x => x.Requests);
        var failures = endpoints.Sum(x => x.Failures);
        var weightedMean = requests == 0
            ? 0d
            : endpoints.Sum(x => x.MeanMilliseconds * x.Requests) / requests;
        return new ApiRuntimeSnapshot(requests, failures, weightedMean, endpoints, now);
    }
}
