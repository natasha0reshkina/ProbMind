using ProbMind.Domain.Enums;
using ProbMind.Domain.Learning;

namespace ProbMind.UnitTests.Domain;

public sealed class CorrectionEngineTests
{
    private readonly CorrectionEngine _sut = new();

    [Fact]
    public void LowConfidencePlan_HasCoreStages()
    {
        var plan = _sut.Build(Guid.NewGuid(), .30);
        Assert.Contains(plan.Steps, x => x.Type == ExerciseType.Explanation);
        Assert.Contains(plan.Steps, x => x.Type == ExerciseType.WorkedExample);
        Assert.Contains(plan.Steps, x => x.Type == ExerciseType.ConceptCheck);
        Assert.Contains(plan.Steps, x => x.Type == ExerciseType.IndependentPractice);
        Assert.Contains(plan.Steps, x => x.Type == ExerciseType.Transfer);
    }

    [Fact]
    public void HighConfidencePlan_AddsGuidedPractice()
    {
        var plan = _sut.Build(Guid.NewGuid(), .80);
        Assert.Contains(plan.Steps, x => x.Type == ExerciseType.GuidedPractice);
    }

    [Fact]
    public void CompletionRequiresTransfer()
    {
        var attempts = new[]
        {
            (ExerciseType.ConceptCheck, true),
            (ExerciseType.IndependentPractice, true)
        };
        Assert.False(_sut.IsCorrectionComplete(attempts));
    }

    [Fact]
    public void CompletionRequiresIndependentPractice()
    {
        var attempts = new[]
        {
            (ExerciseType.ConceptCheck, true),
            (ExerciseType.Transfer, true)
        };
        Assert.False(_sut.IsCorrectionComplete(attempts));
    }

    [Fact]
    public void SuccessfulCoreStages_CompleteCorrection()
    {
        var attempts = new[]
        {
            (ExerciseType.ConceptCheck, true),
            (ExerciseType.GuidedPractice, false),
            (ExerciseType.IndependentPractice, true),
            (ExerciseType.Transfer, true)
        };
        Assert.True(_sut.IsCorrectionComplete(attempts));
    }
}
