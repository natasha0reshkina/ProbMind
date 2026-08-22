namespace ProbMind.Domain.Analytics;

public sealed record LearnerFeatureVector(
    Guid UserId,
    double OverallMastery,
    double ActiveMisconceptionBurden,
    double LearningVelocity,
    double Uncertainty,
    double Engagement);

public sealed record LearnerSegment(Guid UserId, string Segment, double Priority, string Rationale);

public sealed class CohortSegmentationEngine
{
    public IReadOnlyList<LearnerSegment> Segment(IEnumerable<LearnerFeatureVector> learners)
    {
        return learners.Select(SegmentOne).ToArray();
    }

    private static LearnerSegment SegmentOne(LearnerFeatureVector x)
    {
        var weakness = 1d - Math.Clamp(x.OverallMastery, 0d, 1d);
        var burden = Math.Clamp(x.ActiveMisconceptionBurden, 0d, 1d);
        var uncertainty = Math.Clamp(x.Uncertainty, 0d, 1d);
        var engagementGap = 1d - Math.Clamp(x.Engagement, 0d, 1d);

        var priority =
            weakness * .35d +
            burden * .30d +
            uncertainty * .15d +
            engagementGap * .12d +
            Math.Clamp(-x.LearningVelocity * 5d, 0d, .08d);

        var segment =
            x.OverallMastery >= .80d && burden < .20d ? "advanced" :
            x.Engagement < .25d ? "low_engagement" :
            burden >= .65d ? "misconception_intensive" :
            x.OverallMastery < .45d ? "foundational_support" :
            x.LearningVelocity > .04d ? "rapidly_improving" :
            "developing";

        return new LearnerSegment(
            x.UserId,
            segment,
            Math.Clamp(priority, 0d, 1d),
            $"mastery={x.OverallMastery:F2}; burden={burden:F2}; velocity={x.LearningVelocity:F3}; engagement={x.Engagement:F2}");
    }
}
