using ProbMind.IntegrationTests.Infrastructure;

namespace ProbMind.IntegrationTests.Api;

public sealed class ExportsApiTests
{
    [Theory]
    [InlineData("/api/exports/cohort/misconceptions")]
    [InlineData("/api/exports/questions")]
    public async Task Teacher_CanExportAnalyticalXlsx(string endpoint)
    {
        await using var api = await ApiTestClient.CreateAuthenticatedAsync(
            "teacher@probmind.local",
            "Teacher123!");

        var response = await api.Client.GetAsync(endpoint);
        var content = await response.Content.ReadAsByteArrayAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            response.Content.Headers.ContentType?.MediaType);

        Assert.True(content.Length > 2);
        Assert.Equal((byte)'P', content[0]);
        Assert.Equal((byte)'K', content[1]);
    }
}
