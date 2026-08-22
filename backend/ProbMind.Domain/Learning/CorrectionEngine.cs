using ProbMind.Domain.Enums;

namespace ProbMind.Domain.Learning;

public sealed class CorrectionEngine
{
    public CorrectionPlan Build(Guid misconceptionId, double confidence)
    {
        var steps = new List<CorrectionPlanStep>
        {
            new(ExerciseType.Explanation, 1, true,
                "Replace the incorrect mental model with an explicit conceptual distinction."),
            new(ExerciseType.WorkedExample, 2, true,
                "Trace a complete worked example and expose the decision point where the misconception fails."),
            new(ExerciseType.ConceptCheck, 3, true,
                "Check the corrected rule on a small near-transfer example.")
        };

        if (confidence >= 0.55d)
        {
            steps.Add(new CorrectionPlanStep(
                ExerciseType.GuidedPractice,
                steps.Count + 1,
                true,
                "Apply the corrected model with scaffolding."));
        }

        steps.Add(new CorrectionPlanStep(
            ExerciseType.IndependentPractice,
            steps.Count + 1,
            true,
            "Solve an isomorphic task without hints."));

        steps.Add(new CorrectionPlanStep(
            ExerciseType.Transfer,
            steps.Count + 1,
            true,
            "Demonstrate transfer to a differently worded context."));

        return new CorrectionPlan(
            misconceptionId,
            Math.Clamp(confidence, 0d, 1d),
            steps);
    }

    public bool IsCorrectionComplete(
        IReadOnlyCollection<(ExerciseType Type, bool Correct)> attempts)
    {
        if (attempts.Count == 0)
            return false;

        var requiredTypes = new[]
        {
            ExerciseType.ConceptCheck,
            ExerciseType.IndependentPractice,
            ExerciseType.Transfer
        };

        return requiredTypes.All(type =>
            attempts.Any(x => x.Type == type && x.Correct));
    }
}
