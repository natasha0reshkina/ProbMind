using ProbMind.Application.Contracts;
using ProbMind.Application.Services;
using ProbMind.Domain.Entities;
using ProbMind.Domain.Enums;
using ProbMind.UnitTests.TestDoubles;

namespace ProbMind.UnitTests.Application;

public sealed class AuthServiceTests
{
    private readonly InMemoryUnitOfWork _uow = new();
    private readonly MutableClock _clock = new();
    private readonly FakePasswordHasher _hasher = new();
    private readonly FakeTokenService _tokens = new();

    private AuthService Create() => new(_uow, _hasher, _tokens, _clock);

    [Fact]
    public async Task Register_CreatesStudentAndRefreshToken()
    {
        var result = await Create().RegisterAsync(
            new RegisterRequest("new@example.org", "VerySecure1", "Новый студент"));

        Assert.Equal("new@example.org", result.User.Email);
        Assert.Equal(UserRole.Student, result.User.Role);
        Assert.Single(_uow.UsersStore.Items);
        Assert.Single(_uow.RefreshTokensStore.Items);
        Assert.Single(_uow.ActivityEventsStore.Items);
        Assert.True(_uow.SaveCalls > 0);
    }

    [Fact]
    public async Task Register_NormalizesEmail()
    {
        var result = await Create().RegisterAsync(
            new RegisterRequest("  PERSON@Example.ORG ", "VerySecure1", "Person"));

        Assert.Equal("person@example.org", result.User.Email);
    }

    [Fact]
    public async Task Register_RejectsDuplicateEmail()
    {
        _uow.UsersStore.Seed(new User
        {
            Email = "same@example.org",
            DisplayName = "Existing",
            PasswordHash = "hashed:VerySecure1",
            Role = UserRole.Student
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Create().RegisterAsync(new RegisterRequest(
                "same@example.org",
                "VerySecure1",
                "Duplicate")));
    }

    [Theory]
    [InlineData("short1A")]
    [InlineData("alllowercase1")]
    [InlineData("ALLUPPERCASE1")]
    [InlineData("NoDigitsHere")]
    public async Task Register_RejectsWeakPasswords(string password)
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Create().RegisterAsync(new RegisterRequest(
                "weak@example.org",
                password,
                "Weak")));
    }

    [Fact]
    public async Task Login_RejectsWrongPassword()
    {
        _uow.UsersStore.Seed(new User
        {
            Email = "person@example.org",
            DisplayName = "Person",
            PasswordHash = _hasher.Hash("VerySecure1"),
            Role = UserRole.Student,
            IsActive = true
        });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            Create().LoginAsync(new LoginRequest("person@example.org", "WrongPassword1")));
    }

    [Fact]
    public async Task Login_UpdatesLastLogin()
    {
        var user = new User
        {
            Email = "person@example.org",
            DisplayName = "Person",
            PasswordHash = _hasher.Hash("VerySecure1"),
            Role = UserRole.Student,
            IsActive = true
        };
        _uow.UsersStore.Seed(user);

        await Create().LoginAsync(new LoginRequest("person@example.org", "VerySecure1"));

        Assert.Equal(_clock.UtcNow, user.LastLoginAt);
        Assert.Single(_uow.RefreshTokensStore.Items);
    }

    [Fact]
    public async Task Login_RejectsInactiveUser()
    {
        _uow.UsersStore.Seed(new User
        {
            Email = "inactive@example.org",
            DisplayName = "Inactive",
            PasswordHash = _hasher.Hash("VerySecure1"),
            Role = UserRole.Student,
            IsActive = false
        });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            Create().LoginAsync(new LoginRequest("inactive@example.org", "VerySecure1")));
    }

    [Fact]
    public async Task UpdateProfile_ChangesOnlyDisplayName()
    {
        var user = new User
        {
            Email = "person@example.org",
            DisplayName = "Old",
            PasswordHash = _hasher.Hash("VerySecure1"),
            Role = UserRole.Student
        };
        _uow.UsersStore.Seed(user);

        var result = await Create().UpdateProfileAsync(
            user.Id,
            new UpdateProfileRequest("Новое имя"));

        Assert.Equal("Новое имя", result.DisplayName);
        Assert.Equal("person@example.org", result.Email);
    }
}
