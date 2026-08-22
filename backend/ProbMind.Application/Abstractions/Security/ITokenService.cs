using ProbMind.Domain.Entities;

namespace ProbMind.Application.Abstractions.Security;

public sealed record IssuedTokens(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset RefreshTokenExpiresAt);

public interface ITokenService
{
    IssuedTokens Issue(User user);
    string HashRefreshToken(string refreshToken);
}
