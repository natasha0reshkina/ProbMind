using ProbMind.IntegrationTests.Infrastructure;

namespace ProbMind.IntegrationTests.Api;

public sealed class TeacherApiTests
{
    [Fact]
    public async Task TeacherDashboard_HasDemoCohort()
    {
        await using var api = await ApiTestClient.CreateAuthenticatedAsync(
            "teacher@probmind.local",
            "Teacher123!");
        var response = await api.Client.GetAsync("/api/teacher/students");
        var students = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(students.GetArrayLength() >= 10);
    }

    [Fact]
    public async Task QuestionAnalytics_CoversPublishedBank()
    {
        await using var api = await ApiTestClient.CreateAuthenticatedAsync(
            "teacher@probmind.local",
            "Teacher123!");
        var response = await api.Client.GetAsync("/api/teacher/questions/analytics");
        var analytics = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(analytics.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task ReliabilityReport_ReturnsInterpretation()
    {
        await using var api = await ApiTestClient.CreateAuthenticatedAsync(
            "teacher@probmind.local",
            "Teacher123!");
        var response = await api.Client.GetAsync("/api/teacher/diagnostics/reliability");
        var report = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(report.TryGetProperty("cronbachAlpha", out _));
        Assert.False(string.IsNullOrWhiteSpace(report.GetProperty("interpretation").GetString()));
    }
}
