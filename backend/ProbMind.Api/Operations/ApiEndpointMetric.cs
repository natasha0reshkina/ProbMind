namespace ProbMind.Api.Operations;

public sealed record ApiEndpointMetric(
    string Method,
    string Route,
    long Requests,
    long Failures,
    double MeanMilliseconds,
    double MaxMilliseconds,
    DateTimeOffset? LastSeenAt);

public sealed record ApiRuntimeSnapshot(
    long TotalRequests,
    long TotalFailures,
    double MeanMilliseconds,
    IReadOnlyList<ApiEndpointMetric> Endpoints,
    DateTimeOffset CapturedAt);
