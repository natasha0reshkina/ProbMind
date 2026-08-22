using ProbMind.Domain.Diagnostics;
using ProbMind.Domain.Enums;

namespace ProbMind.UnitTests.Domain;

public sealed class ConfidenceEngineTests
{
    private readonly ConfidenceEngine _sut = new();
    private readonly DateTimeOffset _now = new(2026, 8, 14, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void EmptyEvidence_ReturnsZero()
    {
        var result = _sut.Calculate(Array.Empty<EvidenceObservation>(), _now);
        Assert.Equal(0d, result.Confidence);
        Assert.Equal(0, result.EvidenceCount);
    }

    [Fact]
    public void RepeatedDistractors_IncreaseConfidence()
    {
        var one = _sut.Calculate([E(EvidenceKind.Distractor, 0)], _now);
        var many = _sut.Calculate([
            E(EvidenceKind.Distractor, 0, Guid.NewGuid()),
            E(EvidenceKind.Distractor, 2, Guid.NewGuid()),
            E(EvidenceKind.Distractor, 4, Guid.NewGuid()),
            E(EvidenceKind.Distractor, 6, Guid.NewGuid())
        ], _now);

        Assert.True(many.Confidence > one.Confidence);
        Assert.True(many.DiversityScore > one.DiversityScore);
    }

    [Fact]
    public void TransferSuccess_StronglyReducesConfidence()
    {
        var questionIds = Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToArray();
        var before = _sut.Calculate(questionIds.Select((id, i) =>
            E(EvidenceKind.Distractor, i, id)).ToArray(), _now);

        var evidence = questionIds.Select((id, i) =>
            E(EvidenceKind.Distractor, i, id)).ToList();
        evidence.Add(new EvidenceObservation(
            EvidenceKind.TransferSuccess, -1d, 1d, _now, Guid.NewGuid(), true));
        evidence.Add(new EvidenceObservation(
            EvidenceKind.CorrectionSuccess, -1d, 1d, _now, Guid.NewGuid(), false));

        var after = _sut.Calculate(evidence, _now);

        Assert.True(after.Confidence < before.Confidence);
        Assert.True(after.TransferAdjustment < 0d);
    }

    [Fact]
    public void OldEvidence_Decays()
    {
        var fresh = _sut.Calculate([E(EvidenceKind.Distractor, 0)], _now);
        var old = _sut.Calculate([
            new EvidenceObservation(EvidenceKind.Distractor, 1d, 1d, _now.AddDays(-180), Guid.NewGuid())
        ], _now);

        Assert.True(fresh.WeightedSupport > old.WeightedSupport);
        Assert.True(fresh.RecencyScore > old.RecencyScore);
    }

    [Theory]
    [InlineData(EvidenceKind.Distractor, true)]
    [InlineData(EvidenceKind.TransferFailure, true)]
    [InlineData(EvidenceKind.ManualTeacherEvidence, true)]
    [InlineData(EvidenceKind.CorrectAnswer, false)]
    [InlineData(EvidenceKind.CorrectionSuccess, false)]
    [InlineData(EvidenceKind.TransferSuccess, false)]
    public void EvidenceKinds_HaveExpectedDirection(EvidenceKind kind, bool positive)
    {
        var observation = new EvidenceObservation(kind, positive ? 1d : -1d, 1d, _now, Guid.NewGuid(), kind.ToString().Contains("Transfer"));
        var result = _sut.Calculate([observation], _now);

        if (positive) Assert.True(result.WeightedSupport > 0d);
        else Assert.True(result.WeightedContradiction > 0d);
    }

    [Fact]
    public void Confidence_IsAlwaysBounded()
    {
        var extreme = Enumerable.Range(0, 100)
            .Select(_ => E(EvidenceKind.Distractor, 0, Guid.NewGuid()))
            .ToArray();
        var result = _sut.Calculate(extreme, _now);

        Assert.InRange(result.Confidence, 0d, 1d);
    }

    private EvidenceObservation E(EvidenceKind kind, int ageDays, Guid? questionId = null) =>
        new(kind, 1d, 1d, _now.AddDays(-ageDays), questionId ?? Guid.NewGuid());
}
