using ProbMind.IntegrationTests.Infrastructure;

namespace ProbMind.IntegrationTests.Api;

public sealed class RoleSecurityApiTests
{
    [Theory]
    [InlineData("/api/admin/users")]
    [InlineData("/api/admin/audit")]
    [InlineData("/api/content/questions")]
    [InlineData("/api/teacher/students")]
    [InlineData("/api/research/misconceptions/cooccurrence")]
    public async Task Student_IsDeniedPrivilegedEndpoints(string endpoint)
    {
        await using var api = await ApiTestClient.CreateAuthenticatedAsync();
        var response = await api.Client.GetAsync(endpoint);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/teacher/students")]
    [InlineData("/api/teacher/questions/analytics")]
    [InlineData("/api/teacher/diagnostics/reliability")]
    [InlineData("/api/research/misconceptions/cooccurrence")]
    public async Task Teacher_CanReadTeacherAndResearchEndpoints(string endpoint)
    {
        await using var api = await ApiTestClient.CreateAuthenticatedAsync(
            "teacher@probmind.local",
            "Teacher123!");
        var response = await api.Client.GetAsync(endpoint);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
