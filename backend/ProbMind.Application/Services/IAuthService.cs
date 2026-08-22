using ProbMind.Application.Contracts;

namespace ProbMind.Application.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken ct = default);
    Task LogoutAsync(Guid userId, LogoutRequest request, CancellationToken ct = default);
    Task<AuthUserDto> GetMeAsync(Guid userId, CancellationToken ct = default);
    Task<AuthUserDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken ct = default);
}
