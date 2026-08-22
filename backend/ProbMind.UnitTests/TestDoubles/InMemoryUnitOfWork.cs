using ProbMind.Application.Abstractions.Persistence;
using ProbMind.Domain.Entities;

namespace ProbMind.UnitTests.TestDoubles;

public sealed class InMemoryUnitOfWork : IUnitOfWork
{
    public InMemoryRepository<User> UsersStore { get; } = new();
    public InMemoryRepository<RefreshToken> RefreshTokensStore { get; } = new();
    public InMemoryRepository<Topic> TopicsStore { get; } = new();
    public InMemoryRepository<Misconception> MisconceptionsStore { get; } = new();
    public InMemoryRepository<Question> QuestionsStore { get; } = new();
    public InMemoryRepository<QuestionVersion> QuestionVersionsStore { get; } = new();
    public InMemoryRepository<AnswerOption> AnswerOptionsStore { get; } = new();
    public InMemoryRepository<QuestionMisconceptionMap> QuestionMisconceptionMapsStore { get; } = new();
    public InMemoryRepository<DiagnosticSession> DiagnosticSessionsStore { get; } = new();
    public InMemoryRepository<DiagnosticAnswer> DiagnosticAnswersStore { get; } = new();
    public InMemoryRepository<MisconceptionEvidence> MisconceptionEvidenceStore { get; } = new();
    public InMemoryRepository<UserMisconception> UserMisconceptionsStore { get; } = new();
    public InMemoryRepository<TopicMastery> TopicMasteriesStore { get; } = new();
    public InMemoryRepository<TopicMasteryHistory> TopicMasteryHistoryStore { get; } = new();
    public InMemoryRepository<LearningPath> LearningPathsStore { get; } = new();
    public InMemoryRepository<LearningPathStep> LearningPathStepsStore { get; } = new();
    public InMemoryRepository<LearningPathRevision> LearningPathRevisionsStore { get; } = new();
    public InMemoryRepository<Recommendation> RecommendationsStore { get; } = new();
    public InMemoryRepository<CorrectiveModule> CorrectiveModulesStore { get; } = new();
    public InMemoryRepository<PracticeSession> PracticeSessionsStore { get; } = new();
    public InMemoryRepository<PracticeAttempt> PracticeAttemptsStore { get; } = new();
    public InMemoryRepository<ActivityEvent> ActivityEventsStore { get; } = new();
    public InMemoryRepository<AuditLog> AuditLogsStore { get; } = new();
    public InMemoryRepository<Notification> NotificationsStore { get; } = new();
    public InMemoryRepository<QuestionExposure> QuestionExposuresStore { get; } = new();
    public InMemoryRepository<DiagnosticReport> DiagnosticReportsStore { get; } = new();

    public IRepository<User> Users => UsersStore;
    public IRepository<RefreshToken> RefreshTokens => RefreshTokensStore;
    public IRepository<Topic> Topics => TopicsStore;
    public IRepository<Misconception> Misconceptions => MisconceptionsStore;
    public IRepository<Question> Questions => QuestionsStore;
    public IRepository<QuestionVersion> QuestionVersions => QuestionVersionsStore;
    public IRepository<AnswerOption> AnswerOptions => AnswerOptionsStore;
    public IRepository<QuestionMisconceptionMap> QuestionMisconceptionMaps => QuestionMisconceptionMapsStore;
    public IRepository<DiagnosticSession> DiagnosticSessions => DiagnosticSessionsStore;
    public IRepository<DiagnosticAnswer> DiagnosticAnswers => DiagnosticAnswersStore;
    public IRepository<MisconceptionEvidence> MisconceptionEvidence => MisconceptionEvidenceStore;
    public IRepository<UserMisconception> UserMisconceptions => UserMisconceptionsStore;
    public IRepository<TopicMastery> TopicMasteries => TopicMasteriesStore;
    public IRepository<TopicMasteryHistory> TopicMasteryHistory => TopicMasteryHistoryStore;
    public IRepository<LearningPath> LearningPaths => LearningPathsStore;
    public IRepository<LearningPathStep> LearningPathSteps => LearningPathStepsStore;
    public IRepository<LearningPathRevision> LearningPathRevisions => LearningPathRevisionsStore;
    public IRepository<Recommendation> Recommendations => RecommendationsStore;
    public IRepository<CorrectiveModule> CorrectiveModules => CorrectiveModulesStore;
    public IRepository<PracticeSession> PracticeSessions => PracticeSessionsStore;
    public IRepository<PracticeAttempt> PracticeAttempts => PracticeAttemptsStore;
    public IRepository<ActivityEvent> ActivityEvents => ActivityEventsStore;
    public IRepository<AuditLog> AuditLogs => AuditLogsStore;
    public IRepository<Notification> Notifications => NotificationsStore;
    public IRepository<QuestionExposure> QuestionExposures => QuestionExposuresStore;
    public IRepository<DiagnosticReport> DiagnosticReports => DiagnosticReportsStore;

    public int SaveCalls { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCalls++;
        return Task.FromResult(1);
    }
}
