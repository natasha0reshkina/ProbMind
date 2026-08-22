namespace ProbMind.IntegrationTests.Infrastructure;

public sealed class ApiTestClient : IAsyncDisposable
{
    private readonly ProbMindWebApplicationFactory _factory;
    public HttpClient Client { get; }

    private ApiTestClient(ProbMindWebApplicationFactory factory, HttpClient client)
    {
        _factory = factory;
        Client = client;
    }

    public static Task<ApiTestClient> CreateAnonymousAsync()
    {
        var factory = new ProbMindWebApplicationFactory();
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        return Task.FromResult(new ApiTestClient(factory, client));
    }

    public static async Task<ApiTestClient> CreateAuthenticatedAsync(
        string email = "student@probmind.local",
        string password = "Student123!")
    {
        var instance = await CreateAnonymousAsync();
        var login = await instance.Client.PostAsJsonAsync("/api/auth/login", new { email, password });
        login.EnsureSuccessStatusCode();
        var json = await login.Content.ReadFromJsonAsync<JsonElement>();
        var token = json.GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("Access token was not returned.");
        instance.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return instance;
    }

    public static async Task<ApiTestClient> CreateNewStudentAsync()
    {
        var instance = await CreateAnonymousAsync();
        var email = $"integration-{Guid.NewGuid():N}@probmind.test";
        var register = await instance.Client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Integration123!",
            displayName = "Integration Student"
        });
        register.EnsureSuccessStatusCode();
        var json = await register.Content.ReadFromJsonAsync<JsonElement>();
        var token = json.GetProperty("accessToken").GetString()
            ?? throw new InvalidOperationException("Access token was not returned.");
        instance.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return instance;
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _factory.DisposeAsync();
    }
}
