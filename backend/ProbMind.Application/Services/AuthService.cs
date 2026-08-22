using ProbMind.Application.Abstractions.Clock;
using ProbMind.Application.Abstractions.Persistence;
using ProbMind.Application.Abstractions.Security;
using ProbMind.Application.Common;
using ProbMind.Application.Contracts;
using ProbMind.Domain.Entities;
using ProbMind.Domain.Enums;

namespace ProbMind.Application.Services;

public sealed class AuthService : IAuthService
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IClock _clock;

    public AuthService(
        IUnitOfWork uow,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IClock clock)
    {
        _uow = uow;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _clock = clock;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var email = Guard.Email(request.Email);
        var displayName = Guard.Required(request.DisplayName, "displayName", 120);
        ValidatePassword(request.Password);

        if (await _uow.Users.AnyAsync(x => x.Email == email, ct))
            throw new InvalidOperationException("User with this e-mail already exists.");

        var user = new User
        {
            Email = email,
            DisplayName = displayName,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = UserRole.Student,
            IsActive = true
        };

        await _uow.Users.AddAsync(user, ct);
        await _uow.ActivityEvents.AddAsync(new ActivityEvent
        {
            UserId = user.Id,
            EventType = ActivityEventType.UserRegistered,
            AggregateType = nameof(User),
            AggregateId = user.Id,
            PayloadJson = "{}"
        }, ct);

        var response = await IssueAndPersistAsync(user, ct);
        await _uow.SaveChangesAsync(ct);
        return response;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = Guard.Email(request.Email);
        var users = await _uow.Users.WhereAsync(x => x.Email == email, ct);
        var user = users.SingleOrDefault()
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is inactive.");

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        user.LastLoginAt = _clock.UtcNow;
        user.Touch();

        var response = await IssueAndPersistAsync(user, ct);
        await _uow.SaveChangesAsync(ct);
        return response;
    }

    public async Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken ct = default)
    {
        var tokenHash = _tokenService.HashRefreshToken(request.RefreshToken);
        var tokens = await _uow.RefreshTokens.WhereAsync(
            x => x.TokenHash == tokenHash && x.RevokedAt == null,
            ct);

        var stored = tokens.SingleOrDefault()
            ?? throw new UnauthorizedAccessException("Refresh token is invalid.");

        if (stored.ExpiresAt <= _clock.UtcNow)
            throw new UnauthorizedAccessException("Refresh token has expired.");

        var user = await _uow.Users.GetByIdAsync(stored.UserId, ct)
            ?? throw new UnauthorizedAccessException("User no longer exists.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is inactive.");

        stored.RevokedAt = _clock.UtcNow;
        stored.Touch();

        var response = await IssueAndPersistAsync(user, ct);
        stored.ReplacedByHash = _tokenService.HashRefreshToken(response.RefreshToken);
        await _uow.SaveChangesAsync(ct);

        return response;
    }

    public async Task LogoutAsync(Guid userId, LogoutRequest request, CancellationToken ct = default)
    {
        var hash = _tokenService.HashRefreshToken(request.RefreshToken);
        var tokens = await _uow.RefreshTokens.WhereAsync(
            x => x.UserId == userId && x.TokenHash == hash && x.RevokedAt == null,
            ct);

        foreach (var token in tokens)
        {
            token.RevokedAt = _clock.UtcNow;
            token.Touch();
            _uow.RefreshTokens.Update(token);
        }

        await _uow.SaveChangesAsync(ct);
    }

    public async Task<AuthUserDto> GetMeAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _uow.Users.GetByIdAsync(userId, ct)
            ?? throw new KeyNotFoundException("User not found.");

        return Map(user);
    }

    public async Task<AuthUserDto> UpdateProfileAsync(
        Guid userId,
        UpdateProfileRequest request,
        CancellationToken ct = default)
    {
        var user = await _uow.Users.GetByIdAsync(userId, ct)
            ?? throw new KeyNotFoundException("User not found.");

        user.DisplayName = Guard.Required(request.DisplayName, "displayName", 120);
        user.Touch();
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync(ct);

        return Map(user);
    }

    private async Task<AuthResponse> IssueAndPersistAsync(User user, CancellationToken ct)
    {
        var tokens = _tokenService.Issue(user);
        await _uow.RefreshTokens.AddAsync(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = _tokenService.HashRefreshToken(tokens.RefreshToken),
            ExpiresAt = tokens.RefreshTokenExpiresAt
        }, ct);

        return new AuthResponse(
            tokens.AccessToken,
            tokens.RefreshToken,
            tokens.AccessTokenExpiresAt,
            tokens.RefreshTokenExpiresAt,
            Map(user));
    }

    private static AuthUserDto Map(User user) =>
        new(user.Id, user.Email, user.DisplayName, user.Role);

    private static void ValidatePassword(string password)
    {
        if (password.Length < 10)
            throw new ArgumentException("Password must contain at least 10 characters.", "password");
        if (!password.Any(char.IsUpper))
            throw new ArgumentException("Password must contain an uppercase letter.", "password");
        if (!password.Any(char.IsLower))
            throw new ArgumentException("Password must contain a lowercase letter.", "password");
        if (!password.Any(char.IsDigit))
            throw new ArgumentException("Password must contain a digit.", "password");
    }
}
