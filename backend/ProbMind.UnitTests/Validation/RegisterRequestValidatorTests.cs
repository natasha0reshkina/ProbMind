using ProbMind.Application.Contracts;
using ProbMind.Application.Validation;

namespace ProbMind.UnitTests.Validation;

public sealed class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _sut = new();

    [Fact]
    public void Case_1_ExpectedValidity()
    {
        var result = _sut.Validate(new RegisterRequest("student@example.org", "StrongPassword1", "Студент"));
        Assert.Equal(true, result.Count == 0);
    }

    [Fact]
    public void Case_2_ExpectedValidity()
    {
        var result = _sut.Validate(new RegisterRequest("", "StrongPassword1", "Студент"));
        Assert.Equal(false, result.Count == 0);
    }

    [Fact]
    public void Case_3_ExpectedValidity()
    {
        var result = _sut.Validate(new RegisterRequest("invalid", "StrongPassword1", "Студент"));
        Assert.Equal(false, result.Count == 0);
    }

    [Fact]
    public void Case_4_ExpectedValidity()
    {
        var result = _sut.Validate(new RegisterRequest("student@example.org", "weak", "Студент"));
        Assert.Equal(false, result.Count == 0);
    }

    [Fact]
    public void Case_5_ExpectedValidity()
    {
        var result = _sut.Validate(new RegisterRequest("student@example.org", "alllowercase1", "Студент"));
        Assert.Equal(false, result.Count == 0);
    }

    [Fact]
    public void Case_6_ExpectedValidity()
    {
        var result = _sut.Validate(new RegisterRequest("student@example.org", "ALLUPPERCASE1", "Студент"));
        Assert.Equal(false, result.Count == 0);
    }

}
