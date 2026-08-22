using ProbMind.IntegrationTests.Infrastructure;

namespace ProbMind.IntegrationTests.Api;

public sealed class AuthApiTests
{
    [Fact]
    public async Task Register_ThenMe_ReturnsSameStudent()
    {
        await using var api = await ApiTestClient.CreateAnonymousAsync();
        var email = $"auth-{Guid.NewGuid():N}@probmind.test";

        var register = await api.Client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "VerySecure123!",
            displayName = "New Student"
        });

        register.EnsureSuccessStatusCode();
        var auth = await register.Content.ReadFromJsonAsync<JsonElement>();
        api.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            auth.GetProperty("accessToken").GetString());

        var me = await api.Client.GetAsync("/api/auth/me");
        var user = await me.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        Assert.Equal(email, user.GetProperty("email").GetString());
        Assert.Equal("Student", user.GetProperty("role").GetString());
    }

    [Fact]
    public async Task InvalidPassword_IsRejected()
    {
        await using var api = await ApiTestClient.CreateAnonymousAsync();
        var response = await api.Client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "student@probmind.local",
            password = "DefinitelyWrong123!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_RequiresAuthentication()
    {
        await using var api = await ApiTestClient.CreateAnonymousAsync();
        var response = await api.Client.GetAsync("/api/statistics/dashboard");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
