namespace ProbMind.Domain.Analytics;

public sealed record StudentRiskSignal(
    double OverallMastery,
    double RecentMasteryTrend,
    int ActiveMisconceptions,
    int CriticalMisconceptions,
    int DaysInactive,
    double DiagnosticCompletionRate,
    double PracticeTransferPassRate);

public sealed record StudentRiskResult(double Risk, string Level, IReadOnlyList<string> Drivers);

public sealed class StudentRiskScorer
{
    public StudentRiskResult Score(StudentRiskSignal signal)
    {
        var masteryRisk = 1d - Math.Clamp(signal.OverallMastery, 0d, 1d);
        var trendRisk = Math.Clamp(-signal.RecentMasteryTrend * 10d, 0d, 1d);
        var misconceptionRisk = Math.Clamp(signal.ActiveMisconceptions / 6d, 0d, 1d);
        var criticalRisk = Math.Clamp(signal.CriticalMisconceptions / 3d, 0d, 1d);
        var inactivityRisk = Math.Clamp(signal.DaysInactive / 30d, 0d, 1d);
        var completionRisk = 1d - Math.Clamp(signal.DiagnosticCompletionRate, 0d, 1d);
        var transferRisk = 1d - Math.Clamp(signal.PracticeTransferPassRate, 0d, 1d);

        var risk = masteryRisk * .22d + trendRisk * .12d + misconceptionRisk * .18d +
                   criticalRisk * .16d + inactivityRisk * .12d + completionRisk * .08d + transferRisk * .12d;
        risk = Math.Clamp(risk, 0d, 1d);

        var drivers = new List<(string Name, double Value)>
        {
            ("low_mastery", masteryRisk),
            ("negative_trend", trendRisk),
            ("active_misconceptions", misconceptionRisk),
            ("critical_misconceptions", criticalRisk),
            ("inactivity", inactivityRisk),
            ("low_completion", completionRisk),
            ("transfer_failures", transferRisk)
        };

        var level = risk switch
        {
            >= .75d => "critical",
            >= .55d => "high",
            >= .35d => "moderate",
            >= .20d => "low",
            _ => "minimal"
        };
        return new StudentRiskResult(
            risk,
            level,
            drivers.OrderByDescending(x => x.Value).Take(3).Where(x => x.Value > .25d).Select(x => x.Name).ToArray());
    }
}
