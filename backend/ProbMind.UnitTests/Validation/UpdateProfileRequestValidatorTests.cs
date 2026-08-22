using ProbMind.Application.Contracts;
using ProbMind.Application.Validation;

namespace ProbMind.UnitTests.Validation;

public sealed class UpdateProfileRequestValidatorTests
{
    private readonly UpdateProfileRequestValidator _sut = new();

    [Fact]
    public void Case_1_ExpectedValidity()
    {
        var result = _sut.Validate(new UpdateProfileRequest("Наталия"));
        Assert.Equal(true, result.Count == 0);
    }

    [Fact]
    public void Case_2_ExpectedValidity()
    {
        var result = _sut.Validate(new UpdateProfileRequest(""));
        Assert.Equal(false, result.Count == 0);
    }

}
