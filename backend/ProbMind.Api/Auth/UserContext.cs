using System.Security.Claims;
using ProbMind.Domain.Enums;

namespace ProbMind.Api.Auth;

public static class UserContext
{
    public static Guid UserId(ClaimsPrincipal principal)
    {
        var raw = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? principal.FindFirstValue("sub")
                  ?? throw new UnauthorizedAccessException("Authenticated user identifier is missing.");
        return Guid.Parse(raw);
    }

    public static UserRole Role(ClaimsPrincipal principal)
    {
        var raw = principal.FindFirstValue(ClaimTypes.Role)
                  ?? throw new UnauthorizedAccessException("Authenticated user role is missing.");
        return Enum.Parse<UserRole>(raw, ignoreCase: true);
    }
}
