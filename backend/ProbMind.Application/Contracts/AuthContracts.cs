using ProbMind.Domain.Enums;

namespace ProbMind.Application.Contracts;

public sealed record RegisterRequest(string Email, string Password, string DisplayName);
public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshRequest(string RefreshToken);
public sealed record LogoutRequest(string RefreshToken);
public sealed record UpdateProfileRequest(string DisplayName);
public sealed record AuthUserDto(Guid Id, string Email, string DisplayName, UserRole Role);
public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset RefreshTokenExpiresAt,
    AuthUserDto User);
