using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProbMind.Api.Auth;
using ProbMind.Application.Contracts;
using ProbMind.Application.Services;

namespace ProbMind.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("register")]
    [AllowAnonymous]
    public Task<AuthResponse> Register(RegisterRequest request, CancellationToken ct) =>
        _auth.RegisterAsync(request, ct);

    [HttpPost("login")]
    [AllowAnonymous]
    public Task<AuthResponse> Login(LoginRequest request, CancellationToken ct) =>
        _auth.LoginAsync(request, ct);

    [HttpPost("refresh")]
    [AllowAnonymous]
    public Task<AuthResponse> Refresh(RefreshRequest request, CancellationToken ct) =>
        _auth.RefreshAsync(request, ct);

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken ct)
    {
        await _auth.LogoutAsync(UserContext.UserId(User), request, ct);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public Task<AuthUserDto> Me(CancellationToken ct) =>
        _auth.GetMeAsync(UserContext.UserId(User), ct);

    [HttpPatch("me")]
    [Authorize]
    public Task<AuthUserDto> UpdateMe(UpdateProfileRequest request, CancellationToken ct) =>
        _auth.UpdateProfileAsync(UserContext.UserId(User), request, ct);
}
