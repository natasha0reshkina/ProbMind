using ProbMind.IntegrationTests.Infrastructure;

namespace ProbMind.IntegrationTests.Api;

public sealed class ValidationApiTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(9)]
    [InlineData(31)]
    [InlineData(100)]
    public async Task DiagnosticQuestionCount_OutsideContract_IsRejected(int count)
    {
        await using var api = await ApiTestClient.CreateNewStudentAsync();
        var response = await api.Client.PostAsJsonAsync("/api/diagnostics", new { questionCount = count });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Registration_WithMalformedEmail_IsRejected()
    {
        await using var api = await ApiTestClient.CreateAnonymousAsync();
        var response = await api.Client.PostAsJsonAsync("/api/auth/register", new
        {
            email = "not-an-email",
            password = "SecurePassword123!",
            displayName = "Bad Email"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
