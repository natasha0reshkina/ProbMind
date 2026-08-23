using ProbMind.IntegrationTests.Infrastructure;

namespace ProbMind.IntegrationTests.Api;

public sealed class DiagnosticsApiTests
{
    [Fact]
    public async Task Student_CanStartAndReadDiagnosticSession()
    {
        await using var api = await ApiTestClient.CreateNewStudentAsync();
        var started = await api.Client.PostAsJsonAsync("/api/diagnostics", new { questionCount = 10 });
        started.EnsureSuccessStatusCode();
        var session = await started.Content.ReadFromJsonAsync<JsonElement>();
        var id = session.GetProperty("id").GetGuid();

        var fetched = await api.Client.GetAsync($"/api/diagnostics/{id}");
        var body = await fetched.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);
        Assert.Equal(id, body.GetProperty("id").GetGuid());
        Assert.Equal("InProgress", body.GetProperty("status").GetString());
        Assert.Equal(10, body.GetProperty("plannedQuestionCount").GetInt32());
    }

    [Fact]
    public async Task NextQuestion_ContainsAdaptiveExplanationAndOptions()
    {
        await using var api = await ApiTestClient.CreateNewStudentAsync();
        var started = await api.Client.PostAsJsonAsync("/api/diagnostics", new { questionCount = 10 });
        var session = await started.Content.ReadFromJsonAsync<JsonElement>();
        var id = session.GetProperty("id").GetGuid();

        var next = await api.Client.GetAsync($"/api/diagnostics/{id}/next");
        var question = await next.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, next.StatusCode);
        Assert.NotEqual(Guid.Empty, question.GetProperty("questionId").GetGuid());
        Assert.True(question.GetProperty("options").GetArrayLength() >= 4);
        Assert.False(string.IsNullOrWhiteSpace(question.GetProperty("selectionExplanation").GetString()));
    }


    [Fact]
    public async Task Student_CanSubmitFirstDiagnosticAnswer()
    {
        await using var api = await ApiTestClient.CreateNewStudentAsync();
        var started = await api.Client.PostAsJsonAsync("/api/diagnostics", new { questionCount = 10 });
        started.EnsureSuccessStatusCode();
        var session = await started.Content.ReadFromJsonAsync<JsonElement>();
        var sessionId = session.GetProperty("id").GetGuid();

        var next = await api.Client.GetAsync($"/api/diagnostics/{sessionId}/next");
        next.EnsureSuccessStatusCode();
        var question = await next.Content.ReadFromJsonAsync<JsonElement>();
        var optionId = question.GetProperty("options")[0].GetProperty("id").GetGuid();

        var submitted = await api.Client.PostAsJsonAsync("/api/diagnostics/answers", new
        {
            sessionId,
            questionId = question.GetProperty("questionId").GetGuid(),
            questionVersionId = question.GetProperty("versionId").GetGuid(),
            answerOptionId = optionId,
            responseTimeMs = 1000
        });

        Assert.Equal(HttpStatusCode.OK, submitted.StatusCode);
        var feedback = await submitted.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(feedback.TryGetProperty("isCorrect", out _));

        var fetched = await api.Client.GetAsync($"/api/diagnostics/{sessionId}");
        fetched.EnsureSuccessStatusCode();
        var updated = await fetched.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, updated.GetProperty("answeredQuestionCount").GetInt32());
    }

    [Fact]
    public async Task SessionHistory_IncludesNewlyCreatedSession()
    {
        await using var api = await ApiTestClient.CreateNewStudentAsync();
        var started = await api.Client.PostAsJsonAsync("/api/diagnostics", new { questionCount = 12 });
        started.EnsureSuccessStatusCode();

        var history = await api.Client.GetAsync("/api/diagnostics");
        var items = await history.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, history.StatusCode);
        Assert.True(items.GetArrayLength() >= 1);
    }
}
