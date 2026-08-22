using ProbMind.IntegrationTests.Infrastructure;

namespace ProbMind.IntegrationTests.Api;

public sealed class LearningPathApiTests
{
    [Fact]
    public async Task CurrentPath_CanBeBuiltAndRetrieved()
    {
        await using var api = await ApiTestClient.CreateAuthenticatedAsync();
        var rebuild = await api.Client.PostAsJsonAsync("/api/learning-paths/rebuild", new { });
        rebuild.EnsureSuccessStatusCode();

        var current = await api.Client.GetAsync("/api/learning-paths/current");
        var path = await current.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, current.StatusCode);
        Assert.True(path.GetProperty("revision").GetInt32() >= 1);
        Assert.True(path.TryGetProperty("steps", out _));
    }

    [Fact]
    public async Task PathHistory_IsVersioned()
    {
        await using var api = await ApiTestClient.CreateAuthenticatedAsync();
        await api.Client.PostAsJsonAsync("/api/learning-paths/rebuild", new { });
        await api.Client.PostAsJsonAsync("/api/learning-paths/rebuild", new { });

        var history = await api.Client.GetAsync("/api/learning-paths/history");
        var revisions = await history.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, history.StatusCode);
        Assert.True(revisions.GetArrayLength() >= 1);
    }
}
