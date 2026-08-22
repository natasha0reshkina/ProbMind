using ProbMind.Domain.Enums;

namespace ProbMind.Domain.Explainability;

public sealed record EvidenceContribution(
    Guid EvidenceId,
    EvidenceKind Kind,
    double SignedContribution,
    double Share,
    string Direction,
    string Reason);

public sealed record ContributionReport(
    double SupportingMass,
    double ContradictingMass,
    double NetEvidence,
    IReadOnlyList<EvidenceContribution> Contributions);

public sealed class EvidenceContributionAnalyzer
{
    public ContributionReport Analyze(
        IEnumerable<(Guid Id, EvidenceKind Kind, double Weight, double Relevance, string Reason)> evidence)
    {
        var raw = evidence.Select(item =>
        {
            var contribution = Math.Clamp(Math.Abs(item.Weight), 0d, 2d)
                * Math.Clamp(item.Relevance, 0d, 1d)
                * Direction(item.Kind);

            return new
            {
                item.Id,
                item.Kind,
                item.Reason,
                Contribution = contribution
            };
        }).ToArray();

        var supporting = raw.Where(x => x.Contribution > 0d).Sum(x => x.Contribution);
        var contradicting = -raw.Where(x => x.Contribution < 0d).Sum(x => x.Contribution);
        var total = Math.Max(1e-9d, supporting + contradicting);

        var contributions = raw
            .Where(x => Math.Abs(x.Contribution) > 1e-9d)
            .OrderByDescending(x => Math.Abs(x.Contribution))
            .Select(x => new EvidenceContribution(
                x.Id,
                x.Kind,
                x.Contribution,
                Math.Abs(x.Contribution) / total,
                x.Contribution > 0d ? "supports" : "contradicts",
                x.Reason))
            .ToArray();

        return new ContributionReport(
            supporting,
            contradicting,
            supporting - contradicting,
            contributions);
    }

    private static double Direction(EvidenceKind kind) => kind switch
    {
        EvidenceKind.Distractor => 1d,
        EvidenceKind.ManualTeacherEvidence => 1d,
        EvidenceKind.TransferFailure => 1.15d,
        EvidenceKind.CorrectAnswer => -0.72d,
        EvidenceKind.CorrectionSuccess => -0.95d,
        EvidenceKind.TransferSuccess => -1.20d,
        _ => 0d
    };
}
