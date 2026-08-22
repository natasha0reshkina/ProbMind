using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using ProbMind.Api.Middleware;
using ProbMind.Api.Operations;
using ProbMind.Api.Filters;
using ProbMind.Application;
using ProbMind.Infrastructure;
using ProbMind.Infrastructure.Persistence;
using ProbMind.Infrastructure.Seeding;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProbMindApplication();
builder.Services.AddProbMindInfrastructure(builder.Configuration);
builder.Services.AddTransient<DatabaseSeeder>();
builder.Services.AddScoped<RequestValidationFilter>();
builder.Services.AddSingleton<ApiMetricsRegistry>();

builder.Services
    .AddControllers(options => options.Filters.Add<RequestValidationFilter>())
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("web", policy =>
        policy
            .WithOrigins(
                "http://localhost:5173",
                "http://127.0.0.1:5173",
                "http://localhost:8080",
                "http://127.0.0.1:8080")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<ApiMetricsMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors("web");

app.UseSwagger(options =>
{
    options.RouteTemplate = "api/swagger/{documentName}/swagger.json";
});
app.UseSwaggerUI(options =>
{
    options.RoutePrefix = "api/swagger";
    options.SwaggerEndpoint("/api/swagger/v1/swagger.json", "ProbMind API v1");
});

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<IdempotencyMiddleware>();

app.MapControllers();

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "healthy",
    service = "probmind-api",
    timestamp = DateTimeOffset.UtcNow
}));

app.MapGet("/api/ready", async (
    ProbMindDbContext db,
    IConnectionMultiplexer redis,
    CancellationToken ct) =>
{
    var databaseOk = await db.Database.CanConnectAsync(ct);
    var redisOk = redis.IsConnected;

    return databaseOk && redisOk
        ? Results.Ok(new { status = "ready", database = true, redis = true })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
});

await using (var scope = app.Services.CreateAsyncScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
}

app.Run();

public partial class Program { }
