using System.Text.Json;
using ProbMind.Application.Abstractions.Messaging;
using StackExchange.Redis;

namespace ProbMind.Infrastructure.Messaging;

public sealed class RedisBackgroundJobQueue : IBackgroundJobQueue
{
    public const string QueueName = "probmind:jobs";
    private readonly IDatabase _database;

    public RedisBackgroundJobQueue(IConnectionMultiplexer multiplexer)
    {
        _database = multiplexer.GetDatabase();
    }

    public Task EnqueueAsync(BackgroundJob job, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(job);
        return _database.ListLeftPushAsync(QueueName, json);
    }
}
