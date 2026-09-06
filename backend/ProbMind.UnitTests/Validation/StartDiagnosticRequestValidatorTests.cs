using ProbMind.Application.Contracts;
using ProbMind.Application.Validation;

namespace ProbMind.UnitTests.Validation;

public sealed class StartDiagnosticRequestValidatorTests
{
    private readonly StartDiagnosticRequestValidator _sut = new();

    [Fact]
    public void TenQuestions_IsAccepted()
    {
        var result = _sut.Validate(new StartDiagnosticRequest(10));
        Assert.Empty(result);
    }

    [Fact]
    public void FifteenQuestions_IsAccepted()
    {
        var result = _sut.Validate(new StartDiagnosticRequest(15));
        Assert.Empty(result);
    }

    [Fact]
    public void ThirtyQuestions_IsAccepted()
    {
        var result = _sut.Validate(new StartDiagnosticRequest(30));
        Assert.Empty(result);
    }

    [Fact]
    public void NineQuestions_IsRejected()
    {
        var result = _sut.Validate(new StartDiagnosticRequest(9));
        Assert.NotEmpty(result);
    }

    [Fact]
    public void ThirtyOneQuestions_IsRejected()
    {
        var result = _sut.Validate(new StartDiagnosticRequest(31));
        Assert.NotEmpty(result);
    }
}
