using ProbMind.Application.Contracts;
using ProbMind.Application.Validation;

namespace ProbMind.UnitTests.Validation;

public sealed class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _sut = new();

    [Fact]
    public void ValidRegistration_IsAccepted()
    {
        var result = _sut.Validate(new RegisterRequest("student@example.org", "StrongPassword1", "Студент"));
        Assert.Empty(result);
    }

    [Fact]
    public void EmptyEmail_IsRejected()
    {
        var result = _sut.Validate(new RegisterRequest("", "StrongPassword1", "Студент"));
        Assert.NotEmpty(result);
    }

    [Fact]
    public void MalformedEmail_IsRejected()
    {
        var result = _sut.Validate(new RegisterRequest("invalid", "StrongPassword1", "Студент"));
        Assert.NotEmpty(result);
    }

    [Fact]
    public void ShortPassword_IsRejected()
    {
        var result = _sut.Validate(new RegisterRequest("student@example.org", "weak", "Студент"));
        Assert.NotEmpty(result);
    }

    [Fact]
    public void PasswordWithoutUppercase_IsRejected()
    {
        var result = _sut.Validate(new RegisterRequest("student@example.org", "alllowercase1", "Студент"));
        Assert.NotEmpty(result);
    }

    [Fact]
    public void PasswordWithoutLowercase_IsRejected()
    {
        var result = _sut.Validate(new RegisterRequest("student@example.org", "ALLUPPERCASE1", "Студент"));
        Assert.NotEmpty(result);
    }
}
