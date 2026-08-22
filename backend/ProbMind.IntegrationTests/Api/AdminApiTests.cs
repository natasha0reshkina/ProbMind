using ProbMind.IntegrationTests.Infrastructure;

namespace ProbMind.IntegrationTests.Api;

public sealed class AdminApiTests
{
    [Fact]
    public async Task Admin_CanListUsers()
    {
        await using var api = await ApiTestClient.CreateAuthenticatedAsync(
            "admin@probmind.local",
            "Admin123!");
        var response = await api.Client.GetAsync("/api/admin/users");
        var users = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(users.GetArrayLength() >= 3);
    }

    [Fact]
    public async Task Admin_CanReadAuditLog()
    {
        await using var api = await ApiTestClient.CreateAuthenticatedAsync(
            "admin@probmind.local",
            "Admin123!");
        var response = await api.Client.GetAsync("/api/admin/audit");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Teacher_CannotUseAdminApi()
    {
        await using var api = await ApiTestClient.CreateAuthenticatedAsync(
            "teacher@probmind.local",
            "Teacher123!");
        var response = await api.Client.GetAsync("/api/admin/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
