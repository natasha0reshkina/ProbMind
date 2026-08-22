using ProbMind.Domain.Diagnostics;
using ProbMind.Domain.Enums;

namespace ProbMind.UnitTests.Domain;

public sealed class MisconceptionStateMachineTests
{
    private readonly MisconceptionStateMachine _sut = new();

    [Theory]
    [InlineData(0.00, MisconceptionStatus.Unknown)]
    [InlineData(0.19, MisconceptionStatus.Unknown)]
    [InlineData(0.35, MisconceptionStatus.Suspected)]
    [InlineData(0.49, MisconceptionStatus.Suspected)]
    [InlineData(0.62, MisconceptionStatus.Detected)]
    [InlineData(0.95, MisconceptionStatus.Detected)]
    public void Unknown_TransitionsByConfidence(double confidence, MisconceptionStatus expected)
    {
        Assert.Equal(expected, _sut.Resolve(MisconceptionStatus.Unknown, confidence));
    }

    [Theory]
    [InlineData(0.19, MisconceptionStatus.Unknown)]
    [InlineData(0.30, MisconceptionStatus.Suspected)]
    [InlineData(0.62, MisconceptionStatus.Detected)]
    public void Suspected_TransitionsByConfidence(double confidence, MisconceptionStatus expected)
    {
        Assert.Equal(expected, _sut.Resolve(MisconceptionStatus.Suspected, confidence));
    }

    [Theory]
    [InlineData(0.10, MisconceptionStatus.Corrected)]
    [InlineData(0.27, MisconceptionStatus.Corrected)]
    [InlineData(0.28, MisconceptionStatus.Detected)]
    [InlineData(0.80, MisconceptionStatus.Detected)]
    public void Detected_BecomesCorrectedOnlyBelowThreshold(double confidence, MisconceptionStatus expected)
    {
        Assert.Equal(expected, _sut.Resolve(MisconceptionStatus.Detected, confidence));
    }

    [Theory]
    [InlineData(0.20, MisconceptionStatus.Corrected)]
    [InlineData(0.47, MisconceptionStatus.Corrected)]
    [InlineData(0.48, MisconceptionStatus.RecheckRequired)]
    [InlineData(0.61, MisconceptionStatus.RecheckRequired)]
    [InlineData(0.62, MisconceptionStatus.Detected)]
    public void Corrected_CanRelapse(double confidence, MisconceptionStatus expected)
    {
        Assert.Equal(expected, _sut.Resolve(MisconceptionStatus.Corrected, confidence));
    }

    [Fact]
    public void CorrectionMode_ChangesActiveDetectedState()
    {
        var result = _sut.Resolve(MisconceptionStatus.Detected, 0.70d, correctionActive: true);
        Assert.Equal(MisconceptionStatus.CorrectionInProgress, result);
    }

    [Theory]
    [InlineData(MisconceptionStatus.Unknown, false)]
    [InlineData(MisconceptionStatus.Corrected, false)]
    [InlineData(MisconceptionStatus.Suspected, true)]
    [InlineData(MisconceptionStatus.Detected, true)]
    [InlineData(MisconceptionStatus.CorrectionInProgress, true)]
    [InlineData(MisconceptionStatus.RecheckRequired, true)]
    public void IsActive_MatchesDefinition(MisconceptionStatus status, bool expected)
    {
        Assert.Equal(expected, _sut.IsActive(status));
    }
}
