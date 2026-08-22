using ProbMind.Domain.Psychometrics;

namespace ProbMind.UnitTests.Psychometrics;

public sealed class ItemResponseTheoryModelTests
{
    private readonly ItemResponseTheoryModel _model = new();

    [Fact]
    public void Probability_IsHalfWhenThetaEqualsDifficulty_ForOnePlItem()
    {
        var item = new IrtItem(Guid.NewGuid(), 0d, 1d, 0d);
        Assert.Equal(.5d, _model.Probability(0d, item), 6);
    }

    [Fact]
    public void Probability_IncreasesWithAbility()
    {
        var item = new IrtItem(Guid.NewGuid(), .3d, 1.4d, .1d);
        Assert.True(_model.Probability(1.5d, item) > _model.Probability(-1.5d, item));
    }

    [Fact]
    public void Guessing_ProvidesLowerAsymptote()
    {
        var item = new IrtItem(Guid.NewGuid(), 0d, 1d, .25d);
        Assert.True(_model.Probability(-10d, item) >= .249d);
    }

    [Fact]
    public void Information_IsHighestNearUsefulDifficulty()
    {
        var item = new IrtItem(Guid.NewGuid(), 0d, 1.5d, 0d);
        var center = _model.Information(0d, item);
        var far = _model.Information(4d, item);
        Assert.True(center > far);
    }

    [Fact]
    public void AllCorrectResponses_EstimatePositiveAbility()
    {
        var responses = Enumerable.Range(-2, 5)
            .Select(i => new IrtResponse(new IrtItem(Guid.NewGuid(), i * .5d), true))
            .ToArray();
        var estimate = _model.EstimateAbility(responses);
        Assert.True(estimate.Theta > 0d);
        Assert.True(estimate.StandardError > 0d);
    }

    [Fact]
    public void AllIncorrectResponses_EstimateNegativeAbility()
    {
        var responses = Enumerable.Range(-2, 5)
            .Select(i => new IrtResponse(new IrtItem(Guid.NewGuid(), i * .5d), false))
            .ToArray();
        var estimate = _model.EstimateAbility(responses);
        Assert.True(estimate.Theta < 0d);
    }

    [Fact]
    public void MixedResponses_EstimateFiniteAbility()
    {
        var responses = Enumerable.Range(0, 20)
            .Select(i => new IrtResponse(new IrtItem(Guid.NewGuid(), (i - 10) / 5d, 1.2d), i < 12))
            .ToArray();
        var estimate = _model.EstimateAbility(responses);
        Assert.InRange(estimate.Theta, -4d, 4d);
        Assert.True(double.IsFinite(estimate.StandardError));
    }

    [Fact]
    public void EmptyResponses_ReturnHighUncertainty()
    {
        var estimate = _model.EstimateAbility(Array.Empty<IrtResponse>());
        Assert.False(estimate.Converged);
        Assert.True(estimate.StandardError > 5d);
    }
}
