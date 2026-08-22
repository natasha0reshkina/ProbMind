using ProbMind.IntegrationTests.Infrastructure;

namespace ProbMind.IntegrationTests.Api;

public sealed class LearnerApiTests
{
    [Fact]
    public async Task LearnerModel_ReturnsMasteryForOfficialTopics()
    {
        await using var api = await ApiTestClient.CreateAuthenticatedAsync();
        var response = await api.Client.GetAsync("/api/learner/mastery");
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(data.GetArrayLength() >= 5);
    }

    [Fact]
    public async Task LearnerModel_ReturnsMisconceptionStates()
    {
        await using var api = await ApiTestClient.CreateAuthenticatedAsync();
        var response = await api.Client.GetAsync("/api/learner/misconceptions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Recalculation_IsAvailableToStudent()
    {
        await using var api = await ApiTestClient.CreateAuthenticatedAsync();
        var response = await api.Client.PostAsJsonAsync("/api/learner/recalculate", new { });

        Assert.True(response.IsSuccessStatusCode);
    }
}
