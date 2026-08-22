using ProbMind.Domain.Enums;
using ProbMind.Domain.Learning;

namespace ProbMind.UnitTests.Domain;

public sealed class MasteryEngineTests
{
    private readonly MasteryEngine _sut = new();
    private readonly DateTimeOffset _now = new(2026, 8, 14, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void NoObservations_UsesNeutralPrior()
    {
        var result = _sut.Calculate(Array.Empty<MasteryObservation>(), _now);
        Assert.Equal(0.50d, result.Mastery);
        Assert.Equal(1d, result.Uncertainty);
    }

    [Fact]
    public void CorrectAnswers_RaiseMastery()
    {
        var result = _sut.Calculate(
            Enumerable.Range(0, 10)
                .Select(i => new MasteryObservation(true, QuestionDifficulty.Intermediate, _now.AddDays(-i)))
                .ToArray(),
            _now);

        Assert.True(result.Mastery > 0.70d);
        Assert.True(result.Uncertainty < 0.40d);
    }

    [Fact]
    public void WrongAnswers_LowerMastery()
    {
        var result = _sut.Calculate(
            Enumerable.Range(0, 10)
                .Select(i => new MasteryObservation(false, QuestionDifficulty.Intermediate, _now.AddDays(-i)))
                .ToArray(),
            _now);

        Assert.True(result.Mastery < 0.30d);
    }

    [Fact]
    public void TransferAnswer_HasMoreWeight()
    {
        var baseResult = _sut.Calculate([
            new MasteryObservation(true, QuestionDifficulty.Intermediate, _now)
        ], _now);
        var transferResult = _sut.Calculate([
            new MasteryObservation(true, QuestionDifficulty.Transfer, _now, 1d, true)
        ], _now);

        Assert.True(transferResult.Mastery > baseResult.Mastery);
    }

    [Fact]
    public void OldObservations_AreDiscounted()
    {
        var fresh = _sut.Calculate([
            new MasteryObservation(true, QuestionDifficulty.Advanced, _now)
        ], _now);
        var old = _sut.Calculate([
            new MasteryObservation(true, QuestionDifficulty.Advanced, _now.AddDays(-240))
        ], _now);

        Assert.True(fresh.Mastery > old.Mastery);
        Assert.True(fresh.EffectiveTotal > old.EffectiveTotal);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(20)]
    [InlineData(100)]
    public void Uncertainty_DecreasesAsEvidenceGrows(int observations)
    {
        var result = _sut.Calculate(
            Enumerable.Range(0, observations)
                .Select(i => new MasteryObservation(i % 3 != 0, QuestionDifficulty.Intermediate, _now))
                .ToArray(),
            _now);

        Assert.InRange(result.Uncertainty, 0.05d, 1d);
        if (observations >= 20) Assert.True(result.Uncertainty < 0.30d);
    }
}
