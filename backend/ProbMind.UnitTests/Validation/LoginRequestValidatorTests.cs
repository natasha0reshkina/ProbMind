using ProbMind.Application.Contracts;
using ProbMind.Application.Validation;

namespace ProbMind.UnitTests.Validation;

public sealed class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _sut = new();

    [Fact]
    public void ValidLogin_IsAccepted()
    {
        var result = _sut.Validate(new LoginRequest("student@example.org", "Password1"));
        Assert.Empty(result);
    }

    [Fact]
    public void EmptyEmail_IsRejected()
    {
        var result = _sut.Validate(new LoginRequest("", "Password1"));
        Assert.NotEmpty(result);
    }

    [Fact]
    public void EmptyPassword_IsRejected()
    {
        var result = _sut.Validate(new LoginRequest("student@example.org", ""));
        Assert.NotEmpty(result);
    }
}
