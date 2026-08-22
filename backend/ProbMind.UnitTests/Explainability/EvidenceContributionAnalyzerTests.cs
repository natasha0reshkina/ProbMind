using ProbMind.Domain.Enums;
using ProbMind.Domain.Explainability;

namespace ProbMind.UnitTests.Explainability;

public sealed class EvidenceContributionAnalyzerTests
{
    [Fact]
    public void SupportingAndContradictingEvidence_AreSeparated()
    {
        var data = new[]
        {
            (Guid.NewGuid(), EvidenceKind.Distractor, 1d, .9d, "selected misconception distractor"),
            (Guid.NewGuid(), EvidenceKind.TransferSuccess, -1d, .8d, "passed transfer")
        };

        var report = new EvidenceContributionAnalyzer().Analyze(data);

        Assert.True(report.SupportingMass > 0d);
        Assert.True(report.ContradictingMass > 0d);
        Assert.Contains(report.Contributions, x => x.Direction == "supports");
        Assert.Contains(report.Contributions, x => x.Direction == "contradicts");
    }

    [Fact]
    public void TransferSuccess_HasStrongNegativeContribution()
    {
        var report = new EvidenceContributionAnalyzer().Analyze(new[]
        {
            (Guid.NewGuid(), EvidenceKind.TransferSuccess, -1d, 1d, "transfer passed")
        });

        Assert.True(report.Contributions.Single().SignedContribution < -1d);
    }

    [Fact]
    public void Contributions_AreNormalizedToUnitMass()
    {
        var report = new EvidenceContributionAnalyzer().Analyze(new[]
        {
            (Guid.NewGuid(), EvidenceKind.Distractor, 1d, .8d, "a"),
            (Guid.NewGuid(), EvidenceKind.ManualTeacherEvidence, .7d, .9d, "b"),
            (Guid.NewGuid(), EvidenceKind.CorrectAnswer, -.5d, 1d, "c")
        });

        Assert.Equal(1d, report.Contributions.Sum(x => x.Share), 6);
    }
}
