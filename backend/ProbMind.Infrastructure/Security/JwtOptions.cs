namespace ProbMind.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string Section = "Jwt";
    public string Issuer { get; set; } = "ProbMind";
    public string Audience { get; set; } = "ProbMind.Web";
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 20;
    public int RefreshTokenDays { get; set; } = 30;
}
