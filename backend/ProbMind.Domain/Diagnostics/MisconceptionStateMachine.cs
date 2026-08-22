using ProbMind.Domain.Enums;

namespace ProbMind.Domain.Diagnostics;

public sealed class MisconceptionStateMachine
{
    public MisconceptionStatus Resolve(
        MisconceptionStatus current,
        double confidence,
        bool correctionActive = false)
    {
        confidence = Math.Clamp(confidence, 0d, 1d);

        if (correctionActive && confidence >= 0.35d &&
            current is MisconceptionStatus.Detected or MisconceptionStatus.Suspected)
        {
            return MisconceptionStatus.CorrectionInProgress;
        }

        return current switch
        {
            MisconceptionStatus.Unknown when confidence >= 0.62d
                => MisconceptionStatus.Detected,
            MisconceptionStatus.Unknown when confidence >= 0.35d
                => MisconceptionStatus.Suspected,

            MisconceptionStatus.Suspected when confidence >= 0.62d
                => MisconceptionStatus.Detected,
            MisconceptionStatus.Suspected when confidence < 0.20d
                => MisconceptionStatus.Unknown,

            MisconceptionStatus.Detected when confidence < 0.28d
                => MisconceptionStatus.Corrected,

            MisconceptionStatus.CorrectionInProgress when confidence < 0.28d
                => MisconceptionStatus.Corrected,
            MisconceptionStatus.CorrectionInProgress when confidence >= 0.62d
                => MisconceptionStatus.Detected,

            MisconceptionStatus.Corrected when confidence >= 0.62d
                => MisconceptionStatus.Detected,
            MisconceptionStatus.Corrected when confidence >= 0.48d
                => MisconceptionStatus.RecheckRequired,

            MisconceptionStatus.RecheckRequired when confidence >= 0.62d
                => MisconceptionStatus.Detected,
            MisconceptionStatus.RecheckRequired when confidence < 0.24d
                => MisconceptionStatus.Corrected,

            _ => current
        };
    }

    public bool IsActive(MisconceptionStatus status) =>
        status is MisconceptionStatus.Suspected
            or MisconceptionStatus.Detected
            or MisconceptionStatus.CorrectionInProgress
            or MisconceptionStatus.RecheckRequired;
}
