namespace ProbMind.Application.Abstractions.Messaging;

public sealed record BackgroundJob(
    string Type,
    Guid? UserId,
    Guid? AggregateId,
    string PayloadJson,
    string CorrelationId);

public interface IBackgroundJobQueue
{
    Task EnqueueAsync(BackgroundJob job, CancellationToken cancellationToken = default);
}
