using ProbMind.IntegrationTests.Infrastructure;

namespace ProbMind.IntegrationTests.Api;

public sealed class ResearchApiTests
{
    [Fact]
    public async Task CooccurrenceGraph_ReturnsEdges()
    {
        await using var api = await ApiTestClient.CreateAuthenticatedAsync(
            "teacher@probmind.local",
            "Teacher123!");
        var response = await api.Client.GetAsync("/api/research/misconceptions/cooccurrence");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task CalibrationReport_ReturnsBrierScore()
    {
        await using var api = await ApiTestClient.CreateAuthenticatedAsync(
            "teacher@probmind.local",
            "Teacher123!");
        var response = await api.Client.GetAsync("/api/research/diagnostics/calibration");
        var report = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(report.TryGetProperty("brierScore", out _));
        Assert.True(report.TryGetProperty("meanAbsoluteCalibrationError", out _));
    }
}
