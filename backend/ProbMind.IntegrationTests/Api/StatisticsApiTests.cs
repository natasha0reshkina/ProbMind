using ProbMind.IntegrationTests.Infrastructure;

namespace ProbMind.IntegrationTests.Api;

public sealed class StatisticsApiTests
{
    [Fact]
    public async Task Dashboard_ReturnsLearnerMetrics()
    {
        await using var api = await ApiTestClient.CreateAuthenticatedAsync();
        var response = await api.Client.GetAsync("/api/statistics/dashboard");
        var dashboard = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(dashboard.TryGetProperty("overallMastery", out _));
        Assert.True(dashboard.TryGetProperty("activeMisconceptions", out _));
        Assert.True(dashboard.TryGetProperty("topics", out _));
        Assert.True(dashboard.TryGetProperty("recommendations", out _));
    }

    [Fact]
    public async Task Teacher_CanReadCohortPrevalence()
    {
        await using var api = await ApiTestClient.CreateAuthenticatedAsync(
            "teacher@probmind.local",
            "Teacher123!");
        var response = await api.Client.GetAsync("/api/statistics/prevalence");
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(data.ValueKind is JsonValueKind.Array or JsonValueKind.Object);
    }

    [Fact]
    public async Task DiagnosticComparison_RejectsSameSession()
    {
        await using var api = await ApiTestClient.CreateAuthenticatedAsync();
        var id = Guid.NewGuid();
        var response = await api.Client.GetAsync($"/api/statistics/diagnostics/compare?fromSessionId={id}&toSessionId={id}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Student_CannotReadSystemStatistics()
    {
        await using var api = await ApiTestClient.CreateAuthenticatedAsync();
        var response = await api.Client.GetAsync("/api/statistics/system");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
