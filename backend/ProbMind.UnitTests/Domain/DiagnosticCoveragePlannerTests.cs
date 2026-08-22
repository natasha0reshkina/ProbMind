using ProbMind.Domain.Learning;

namespace ProbMind.UnitTests.Domain;

public sealed class DiagnosticCoveragePlannerTests
{
    [Fact]
    public void Allocate_PreservesMinimumCoverageForEveryTopic()
    {
        var topics = Enumerable.Range(0, 5)
            .Select(_ => Guid.NewGuid())
            .ToArray();
        var targets = topics
            .Select(id => new CoverageTarget(id, 2, 1d))
            .ToArray();

        var result = new DiagnosticCoveragePlanner().Allocate(targets, 15);

        Assert.Equal(15, result.Values.Sum());
        Assert.All(topics, id => Assert.True(result[id] >= 2));
    }

    [Fact]
    public void Allocate_GivesMoreQuestionsToHigherPriorityTopic()
    {
        var weakTopic = Guid.NewGuid();
        var stableTopic = Guid.NewGuid();
        var targets = new[]
        {
            new CoverageTarget(weakTopic, 1, 3d),
            new CoverageTarget(stableTopic, 1, 1d)
        };

        var result = new DiagnosticCoveragePlanner().Allocate(targets, 8);

        Assert.True(result[weakTopic] > result[stableTopic]);
    }
}
