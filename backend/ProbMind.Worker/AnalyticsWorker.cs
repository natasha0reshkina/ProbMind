using System.Text.Json;
using ProbMind.Application.Abstractions.Messaging;
using ProbMind.Application.Services;
using ProbMind.Infrastructure.Messaging;
using StackExchange.Redis;

namespace ProbMind.Worker;

public sealed class AnalyticsWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<AnalyticsWorker> _logger;

    public AnalyticsWorker(
        IServiceScopeFactory scopeFactory,
        IConnectionMultiplexer redis,
        ILogger<AnalyticsWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _redis = redis;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var database = _redis.GetDatabase();
        _logger.LogInformation("ProbMind analytics worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var value = await database.ListRightPopAsync(RedisBackgroundJobQueue.QueueName);

                if (value.IsNullOrEmpty)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(500), stoppingToken);
                    continue;
                }

                var job = JsonSerializer.Deserialize<BackgroundJob>(value.ToString());
                if (job is null)
                    continue;

                await ProcessAsync(job, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Worker iteration failed.");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }

    private async Task ProcessAsync(BackgroundJob job, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();

        switch (job.Type)
        {
            case "analytics.rebuild-user" when job.UserId.HasValue:
            {
                var learner = scope.ServiceProvider.GetRequiredService<ILearnerModelService>();
                var recommendations = scope.ServiceProvider.GetRequiredService<IRecommendationService>();
                await learner.RecalculateAllAsync(job.UserId.Value, ct);
                await recommendations.RebuildAsync(job.UserId.Value, ct);
                _logger.LogInformation(
                    "Rebuilt learner analytics for user {UserId}; correlation {CorrelationId}",
                    job.UserId,
                    job.CorrelationId);
                break;
            }

            case "analytics.rebuild-cohort":
            {
                var statistics = scope.ServiceProvider.GetRequiredService<IStatisticsService>();
                _ = await statistics.CohortAsync(ct);
                _logger.LogInformation(
                    "Rebuilt cohort analytics; correlation {CorrelationId}",
                    job.CorrelationId);
                break;
            }

            default:
                _logger.LogWarning(
                    "Unknown background job type {JobType}; correlation {CorrelationId}",
                    job.Type,
                    job.CorrelationId);
                break;
        }
    }
}
