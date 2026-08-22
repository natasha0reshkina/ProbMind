using ProbMind.IntegrationTests.Infrastructure;

namespace ProbMind.IntegrationTests.Api;

public sealed class ExportsApiTests
{
    [Theory]
    [InlineData("/api/exports/cohort/misconceptions")]
    [InlineData("/api/exports/questions")]
    public async Task Teacher_CanExportAnalyticalCsv(string endpoint)
    {
        await using var api = await ApiTestClient.CreateAuthenticatedAsync(
            "teacher@probmind.local",
            "Teacher123!");
        var response = await api.Client.GetAsync(endpoint);
        var text = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("text/csv", response.Content.Headers.ContentType?.MediaType ?? string.Empty);
        Assert.Contains(',', text);
    }
}
