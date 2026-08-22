using ProbMind.Domain.Learning;

namespace ProbMind.UnitTests.Domain;

public sealed class AdaptiveQuestionSelectorTests
{
    private readonly AdaptiveQuestionSelector _sut = new();
    private readonly DateTimeOffset _now = new(2026, 8, 14, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void WeakTopic_IsPrioritized()
    {
        var weak = Candidate(mastery: .20, confidence: .10);
        var strong = Candidate(mastery: .90, confidence: .10);
        Assert.True(_sut.Score(weak, _now).Score > _sut.Score(strong, _now).Score);
    }

    [Fact]
    public void StrongMisconceptionSignal_IsPrioritized()
    {
        var active = Candidate(mastery: .60, confidence: .90);
        var neutral = Candidate(mastery: .60, confidence: .05);
        Assert.True(_sut.Score(active, _now).Score > _sut.Score(neutral, _now).Score);
    }

    [Fact]
    public void RecentlyRepeatedQuestion_IsPenalized()
    {
        var fresh = Candidate(mastery: .50, confidence: .50, exposure: 0, lastSeen: null);
        var repeated = Candidate(mastery: .50, confidence: .50, exposure: 5, lastSeen: _now.AddMinutes(-5));
        Assert.True(_sut.Score(fresh, _now).Score > _sut.Score(repeated, _now).Score);
    }

    [Fact]
    public void TransferQuestion_GetsBonusWhenMisconceptionIsActive()
    {
        var transfer = Candidate(mastery: .60, confidence: .80, transfer: true);
        var regular = Candidate(mastery: .60, confidence: .80, transfer: false);
        Assert.True(_sut.Score(transfer, _now).Score > _sut.Score(regular, _now).Score);
    }

    [Fact]
    public void Rank_ExcludesUnpublished()
    {
        var published = Candidate(.5, .5);
        var hidden = Candidate(.1, .9) with { IsPublished = false };
        var ranked = _sut.Rank([hidden, published], _now);
        Assert.Single(ranked);
        Assert.Equal(published.QuestionId, ranked[0].QuestionId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(20)]
    public void Scores_AreBounded(int exposure)
    {
        var result = _sut.Score(Candidate(.4, .8, exposure), _now);
        Assert.InRange(result.Score, 0d, 1d);
    }

    private AdaptiveCandidate Candidate(
        double mastery,
        double confidence,
        int exposure = 0,
        DateTimeOffset? lastSeen = null,
        bool transfer = false) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            mastery,
            1d,
            confidence,
            lastSeen,
            exposure,
            .1d,
            transfer);
}
