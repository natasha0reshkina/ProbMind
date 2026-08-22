using ProbMind.Application.Services;
using ProbMind.Domain.Entities;
using ProbMind.Domain.Enums;
using ProbMind.UnitTests.TestDoubles;

namespace ProbMind.UnitTests.Application;

public sealed class RecommendationServiceTests
{
    private readonly InMemoryUnitOfWork _uow = new();
    private readonly MutableClock _clock = new();

    [Fact]
    public async Task ActiveMisconception_CreatesCorrectionRecommendation()
    {
        var userId = Guid.NewGuid();
        var topic = new Topic { Code = "t", NameRu = "Тема" };
        var mc = new Misconception { TopicId = topic.Id, Code = "mc", Title = "Ошибка" };

        _uow.TopicsStore.Seed(topic);
        _uow.MisconceptionsStore.Seed(mc);
        _uow.UserMisconceptionsStore.Seed(new UserMisconception
        {
            UserId = userId,
            MisconceptionId = mc.Id,
            Confidence = .82,
            Status = MisconceptionStatus.Detected
        });

        var result = await Create().RebuildAsync(userId);

        Assert.Contains(result, x => x.Type == RecommendationType.StartCorrection);
        Assert.Contains(result, x => x.MisconceptionId == mc.Id);
    }

    [Fact]
    public async Task WeakTopic_CreatesReviewRecommendation()
    {
        var userId = Guid.NewGuid();
        var topic = new Topic { Code = "t", NameRu = "Тема" };
        _uow.TopicsStore.Seed(topic);
        _uow.TopicMasteriesStore.Seed(new TopicMastery
        {
            UserId = userId,
            TopicId = topic.Id,
            Mastery = .31,
            Uncertainty = .20
        });

        var result = await Create().RebuildAsync(userId);

        Assert.Contains(result, x =>
            x.TopicId == topic.Id &&
            x.Type == RecommendationType.ReviewTheory);
    }

    [Fact]
    public async Task HealthyLearner_GetsMaintenanceRecommendation()
    {
        var userId = Guid.NewGuid();
        var result = await Create().RebuildAsync(userId);
        Assert.Single(result);
        Assert.Equal(RecommendationType.MaintainMastery, result[0].Type);
    }

    [Fact]
    public async Task Rebuild_DismissesPreviousActiveRecommendations()
    {
        var userId = Guid.NewGuid();
        var previous = new Recommendation
        {
            UserId = userId,
            Type = RecommendationType.ReviewTheory,
            Priority = .8,
            Title = "Old",
            IsDismissed = false
        };
        _uow.RecommendationsStore.Seed(previous);

        await Create().RebuildAsync(userId);

        Assert.True(previous.IsDismissed);
    }

    [Fact]
    public async Task Dismiss_RejectsOtherUsersRecommendation()
    {
        var item = new Recommendation { UserId = Guid.NewGuid(), Title = "Private" };
        _uow.RecommendationsStore.Seed(item);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            Create().DismissAsync(Guid.NewGuid(), item.Id));
    }

    private RecommendationService Create() => new(_uow, _clock);
}
