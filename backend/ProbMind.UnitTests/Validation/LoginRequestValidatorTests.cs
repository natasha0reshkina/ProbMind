using ProbMind.Application.Contracts;
using ProbMind.Application.Validation;

namespace ProbMind.UnitTests.Validation;

public sealed class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _sut = new();

    [Fact]
    public void Case_1_ExpectedValidity()
    {
        var result = _sut.Validate(new LoginRequest("student@example.org", "Password1"));
        Assert.Equal(true, result.Count == 0);
    }

    [Fact]
    public void Case_2_ExpectedValidity()
    {
        var result = _sut.Validate(new LoginRequest("", "Password1"));
        Assert.Equal(false, result.Count == 0);
    }

    [Fact]
    public void Case_3_ExpectedValidity()
    {
        var result = _sut.Validate(new LoginRequest("student@example.org", ""));
        Assert.Equal(false, result.Count == 0);
    }

}
