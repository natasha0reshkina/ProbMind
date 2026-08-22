using ProbMind.IntegrationTests.Infrastructure;

namespace ProbMind.IntegrationTests.Api;

public sealed class HealthApiTests
{
    [Fact]
    public async Task Health_ReturnsHealthyServiceDocument()
    {
        await using var api = await ApiTestClient.CreateAnonymousAsync();
        var response = await api.Client.GetAsync("/api/health");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("healthy", body.GetProperty("status").GetString());
        Assert.Equal("probmind-api", body.GetProperty("service").GetString());
    }

    [Fact]
    public async Task Ready_ReportsInfrastructureAvailability()
    {
        await using var api = await ApiTestClient.CreateAnonymousAsync();
        var response = await api.Client.GetAsync("/api/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
