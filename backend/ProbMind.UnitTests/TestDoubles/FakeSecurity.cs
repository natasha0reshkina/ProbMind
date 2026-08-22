using ProbMind.Application.Abstractions.Security;
using ProbMind.Domain.Entities;

namespace ProbMind.UnitTests.TestDoubles;

public sealed class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => "hashed:" + password;
    public bool Verify(string password, string encodedHash) => encodedHash == "hashed:" + password;
}

public sealed class FakeTokenService : ITokenService
{
    private int _counter;

    public IssuedTokens Issue(User user)
    {
        _counter++;
        return new IssuedTokens(
            $"access-{user.Id:N}-{_counter}",
            $"refresh-{user.Id:N}-{_counter}",
            DateTimeOffset.UtcNow.AddMinutes(20),
            DateTimeOffset.UtcNow.AddDays(30));
    }

    public string HashRefreshToken(string refreshToken) => "hash:" + refreshToken;
}
