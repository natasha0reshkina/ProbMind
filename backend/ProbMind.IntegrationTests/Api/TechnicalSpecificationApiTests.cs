using ProbMind.IntegrationTests.Infrastructure;

namespace ProbMind.IntegrationTests.Api;

public sealed class TechnicalSpecificationApiTests
{
    [Fact]
    public async Task DiagnosticFlow_BuildsReportHistoryRecommendationsAndLearningPath()
    {
        await using var api = await ApiTestClient.CreateNewStudentAsync();

        var started = await api.Client.PostAsJsonAsync("/api/diagnostics", new { questionCount = 10 });
        started.EnsureSuccessStatusCode();
        var session = await started.Content.ReadFromJsonAsync<JsonElement>();
        var sessionId = session.GetProperty("id").GetGuid();

        var topicCodes = new HashSet<string>();

        for (var i = 0; i < 10; i++)
        {
            var next = await api.Client.GetAsync($"/api/diagnostics/{sessionId}/next");
            next.EnsureSuccessStatusCode();
            var question = await next.Content.ReadFromJsonAsync<JsonElement>();
            topicCodes.Add(question.GetProperty("topicCode").GetString() ?? string.Empty);
            var optionId = question.GetProperty("options")[0].GetProperty("id").GetGuid();

            var submitted = await api.Client.PostAsJsonAsync("/api/diagnostics/answers", new
            {
                sessionId,
                questionId = question.GetProperty("questionId").GetGuid(),
                questionVersionId = question.GetProperty("versionId").GetGuid(),
                answerOptionId = optionId,
                responseTimeMs = 1200
            });

            submitted.EnsureSuccessStatusCode();
        }

        Assert.Equal(5, topicCodes.Count);

        var completed = await api.Client.PostAsync($"/api/diagnostics/{sessionId}/complete", null);
        completed.EnsureSuccessStatusCode();
        var report = await completed.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(10, report.GetProperty("answered").GetInt32());
        Assert.Equal(5, report.GetProperty("topics").GetArrayLength());
        Assert.True(report.TryGetProperty("misconceptions", out _));
        Assert.False(string.IsNullOrWhiteSpace(report.GetProperty("summary").GetString()));

        var history = await api.Client.GetFromJsonAsync<JsonElement>("/api/diagnostics");
        Assert.Contains(history.EnumerateArray(), item => item.GetProperty("id").GetGuid() == sessionId);

        var recommendations = await api.Client.GetFromJsonAsync<JsonElement>("/api/recommendations");
        Assert.True(recommendations.GetArrayLength() >= 1);

        var path = await api.Client.GetFromJsonAsync<JsonElement>("/api/learning-paths/current");
        Assert.True(path.GetProperty("steps").GetArrayLength() >= 1);

        var mastery = await api.Client.GetFromJsonAsync<JsonElement>("/api/learner/mastery");
        Assert.Equal(5, mastery.GetArrayLength());
    }

    [Fact]
    public async Task Diagnostic_CannotCompleteBeforePlannedQuestionCount()
    {
        await using var api = await ApiTestClient.CreateNewStudentAsync();

        var started = await api.Client.PostAsJsonAsync("/api/diagnostics", new { questionCount = 10 });
        started.EnsureSuccessStatusCode();
        var session = await started.Content.ReadFromJsonAsync<JsonElement>();
        var sessionId = session.GetProperty("id").GetGuid();

        var next = await api.Client.GetFromJsonAsync<JsonElement>($"/api/diagnostics/{sessionId}/next");
        var submitted = await api.Client.PostAsJsonAsync("/api/diagnostics/answers", new
        {
            sessionId,
            questionId = next.GetProperty("questionId").GetGuid(),
            questionVersionId = next.GetProperty("versionId").GetGuid(),
            answerOptionId = next.GetProperty("options")[0].GetProperty("id").GetGuid(),
            responseTimeMs = 1000
        });
        submitted.EnsureSuccessStatusCode();

        var completed = await api.Client.PostAsync($"/api/diagnostics/{sessionId}/complete", null);
        Assert.Equal(HttpStatusCode.Conflict, completed.StatusCode);

        var unchanged = await api.Client.GetFromJsonAsync<JsonElement>($"/api/diagnostics/{sessionId}");
        Assert.Equal("InProgress", unchanged.GetProperty("status").GetString());
        Assert.Equal(1, unchanged.GetProperty("answeredQuestionCount").GetInt32());
    }

    [Fact]
    public async Task Practice_DuplicateAnswer_IsRejectedWithoutChangingProgressTwice()
    {
        await using var api = await ApiTestClient.CreateNewStudentAsync();
        var topics = await api.Client.GetFromJsonAsync<JsonElement>("/api/content/topics");
        var topicId = topics[0].GetProperty("id").GetGuid();

        var started = await api.Client.PostAsJsonAsync("/api/practice", new
        {
            topicId,
            misconceptionId = (Guid?)null,
            targetExercises = 3
        });
        started.EnsureSuccessStatusCode();
        var session = await started.Content.ReadFromJsonAsync<JsonElement>();
        var sessionId = session.GetProperty("id").GetGuid();

        var next = await api.Client.GetAsync($"/api/practice/{sessionId}/next");
        next.EnsureSuccessStatusCode();
        var question = await next.Content.ReadFromJsonAsync<JsonElement>();

        var request = new
        {
            sessionId,
            questionId = question.GetProperty("questionId").GetGuid(),
            questionVersionId = question.GetProperty("versionId").GetGuid(),
            answerOptionId = question.GetProperty("options")[0].GetProperty("id").GetGuid(),
            exerciseType = "ConceptCheck",
            responseTimeMs = 900
        };

        var first = await api.Client.PostAsJsonAsync("/api/practice/answers", request);
        first.EnsureSuccessStatusCode();

        var duplicate = await api.Client.PostAsJsonAsync("/api/practice/answers", request);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var updated = await api.Client.GetFromJsonAsync<JsonElement>($"/api/practice/{sessionId}");
        Assert.Equal(1, updated.GetProperty("completedExercises").GetInt32());
    }

    [Fact]
    public async Task Practice_CannotCompleteBeforeTargetExerciseCount()
    {
        await using var api = await ApiTestClient.CreateNewStudentAsync();
        var topics = await api.Client.GetFromJsonAsync<JsonElement>("/api/content/topics");
        var topicId = topics[0].GetProperty("id").GetGuid();

        var started = await api.Client.PostAsJsonAsync("/api/practice", new
        {
            topicId,
            misconceptionId = (Guid?)null,
            targetExercises = 5
        });
        started.EnsureSuccessStatusCode();
        var session = await started.Content.ReadFromJsonAsync<JsonElement>();
        var sessionId = session.GetProperty("id").GetGuid();

        for (var i = 0; i < 3; i++)
        {
            var next = await api.Client.GetFromJsonAsync<JsonElement>($"/api/practice/{sessionId}/next");
            var submitted = await api.Client.PostAsJsonAsync("/api/practice/answers", new
            {
                sessionId,
                questionId = next.GetProperty("questionId").GetGuid(),
                questionVersionId = next.GetProperty("versionId").GetGuid(),
                answerOptionId = next.GetProperty("options")[0].GetProperty("id").GetGuid(),
                exerciseType = "ConceptCheck",
                responseTimeMs = 800
            });
            submitted.EnsureSuccessStatusCode();
        }

        var completed = await api.Client.PostAsync($"/api/practice/{sessionId}/complete", null);
        Assert.Equal(HttpStatusCode.Conflict, completed.StatusCode);

        var unchanged = await api.Client.GetFromJsonAsync<JsonElement>($"/api/practice/{sessionId}");
        Assert.Equal("InProgress", unchanged.GetProperty("status").GetString());
        Assert.Equal(3, unchanged.GetProperty("completedExercises").GetInt32());
    }

    [Fact]
    public async Task InvalidPracticeExerciseType_IsRejectedBeforeDataChanges()
    {
        await using var api = await ApiTestClient.CreateNewStudentAsync();
        var topics = await api.Client.GetFromJsonAsync<JsonElement>("/api/content/topics");
        var topicId = topics[0].GetProperty("id").GetGuid();

        var started = await api.Client.PostAsJsonAsync("/api/practice", new
        {
            topicId,
            misconceptionId = (Guid?)null,
            targetExercises = 3
        });
        started.EnsureSuccessStatusCode();
        var session = await started.Content.ReadFromJsonAsync<JsonElement>();
        var sessionId = session.GetProperty("id").GetGuid();

        var next = await api.Client.GetFromJsonAsync<JsonElement>($"/api/practice/{sessionId}/next");
        var response = await api.Client.PostAsJsonAsync("/api/practice/answers", new
        {
            sessionId,
            questionId = next.GetProperty("questionId").GetGuid(),
            questionVersionId = next.GetProperty("versionId").GetGuid(),
            answerOptionId = next.GetProperty("options")[0].GetProperty("id").GetGuid(),
            exerciseType = 999,
            responseTimeMs = 1000
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var unchanged = await api.Client.GetFromJsonAsync<JsonElement>($"/api/practice/{sessionId}");
        Assert.Equal(0, unchanged.GetProperty("completedExercises").GetInt32());
    }
}
