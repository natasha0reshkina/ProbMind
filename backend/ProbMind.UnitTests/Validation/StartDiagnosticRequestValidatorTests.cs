using ProbMind.Application.Contracts;
using ProbMind.Application.Validation;

namespace ProbMind.UnitTests.Validation;

public sealed class StartDiagnosticRequestValidatorTests
{
    private readonly StartDiagnosticRequestValidator _sut = new();

    [Fact]
    public void Case_1_ExpectedValidity()
    {
        var result = _sut.Validate(new StartDiagnosticRequest(10));
        Assert.Equal(true, result.Count == 0);
    }

    [Fact]
    public void Case_2_ExpectedValidity()
    {
        var result = _sut.Validate(new StartDiagnosticRequest(15));
        Assert.Equal(true, result.Count == 0);
    }

    [Fact]
    public void Case_3_ExpectedValidity()
    {
        var result = _sut.Validate(new StartDiagnosticRequest(30));
        Assert.Equal(true, result.Count == 0);
    }

    [Fact]
    public void Case_4_ExpectedValidity()
    {
        var result = _sut.Validate(new StartDiagnosticRequest(9));
        Assert.Equal(false, result.Count == 0);
    }

    [Fact]
    public void Case_5_ExpectedValidity()
    {
        var result = _sut.Validate(new StartDiagnosticRequest(31));
        Assert.Equal(false, result.Count == 0);
    }

}
