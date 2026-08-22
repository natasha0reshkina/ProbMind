using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ProbMind.Application.Abstractions.Clock;
using ProbMind.Application.Abstractions.Security;
using ProbMind.Domain.Entities;

namespace ProbMind.Infrastructure.Security;

public sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions _options;
    private readonly IClock _clock;

    public JwtTokenService(IOptions<JwtOptions> options, IClock clock)
    {
        _options = options.Value;
        _clock = clock;
    }

    public IssuedTokens Issue(User user)
    {
        var now = _clock.UtcNow;
        var accessExpiry = now.AddMinutes(_options.AccessTokenMinutes);
        var refreshExpiry = now.AddDays(_options.RefreshTokenDays);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.DisplayName),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: accessExpiry.UtcDateTime,
            signingCredentials: credentials);

        var access = new JwtSecurityTokenHandler().WriteToken(token);
        var refresh = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

        return new IssuedTokens(access, refresh, accessExpiry, refreshExpiry);
    }

    public string HashRefreshToken(string refreshToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToHexString(bytes);
    }
}
