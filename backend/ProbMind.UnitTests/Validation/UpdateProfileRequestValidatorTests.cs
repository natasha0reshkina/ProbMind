using ProbMind.Application.Contracts;
using ProbMind.Application.Validation;

namespace ProbMind.UnitTests.Validation;

public sealed class UpdateProfileRequestValidatorTests
{
    private readonly UpdateProfileRequestValidator _sut = new();

    [Fact]
    public void ValidDisplayName_IsAccepted()
    {
        var result = _sut.Validate(new UpdateProfileRequest("Наталия"));
        Assert.Empty(result);
    }

    [Fact]
    public void EmptyDisplayName_IsRejected()
    {
        var result = _sut.Validate(new UpdateProfileRequest(""));
        Assert.NotEmpty(result);
    }
}
