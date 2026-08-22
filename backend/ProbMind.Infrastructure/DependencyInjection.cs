using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using ProbMind.Application.Abstractions.Cache;
using ProbMind.Application.Abstractions.Clock;
using ProbMind.Application.Abstractions.Messaging;
using ProbMind.Application.Abstractions.Persistence;
using ProbMind.Application.Abstractions.Security;
using ProbMind.Infrastructure.Cache;
using ProbMind.Infrastructure.Messaging;
using ProbMind.Infrastructure.Persistence;
using ProbMind.Infrastructure.Security;
using ProbMind.Infrastructure.Time;
using StackExchange.Redis;

namespace ProbMind.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddProbMindInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres is required.");
        var redisConnection = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("ConnectionStrings:Redis is required.");

        services.AddDbContext<ProbMindDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(5), null)));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.Section));
        services.AddScoped<ITokenService, JwtTokenService>();

        var jwt = configuration.GetSection(JwtOptions.Section).Get<JwtOptions>()
            ?? throw new InvalidOperationException("Jwt configuration is missing.");

        if (jwt.SigningKey.Length < 32)
            throw new InvalidOperationException("Jwt signing key must contain at least 32 characters.");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        services.AddAuthorization();

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisConnection));
        services.AddSingleton<IApplicationCache, RedisApplicationCache>();
        services.AddSingleton<IBackgroundJobQueue, RedisBackgroundJobQueue>();

        return services;
    }
}
