using ProbMind.IntegrationTests.Infrastructure;

namespace ProbMind.IntegrationTests.Api;

public sealed class ContentApiTests
{
    [Fact]
    public async Task Teacher_CanReadAllOfficialTopics()
    {
        await using var api = await ApiTestClient.CreateAuthenticatedAsync(
            "teacher@probmind.local",
            "Teacher123!");
        var response = await api.Client.GetAsync("/api/content/topics");
        var topics = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(5, topics.GetArrayLength());
    }

    [Fact]
    public async Task Teacher_CanReadMisconceptionCatalog()
    {
        await using var api = await ApiTestClient.CreateAuthenticatedAsync(
            "teacher@probmind.local",
            "Teacher123!");
        var response = await api.Client.GetAsync("/api/content/misconceptions");
        var misconceptions = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(misconceptions.GetArrayLength() >= 10);
    }

    [Fact]
    public async Task Teacher_CanReadQuestionBank()
    {
        await using var api = await ApiTestClient.CreateAuthenticatedAsync(
            "teacher@probmind.local",
            "Teacher123!");
        var response = await api.Client.GetAsync("/api/content/questions");
        var questions = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(questions.GetArrayLength() >= 60);
    }
}
