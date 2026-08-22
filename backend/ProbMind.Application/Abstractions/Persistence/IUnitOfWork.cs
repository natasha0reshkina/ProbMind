using ProbMind.Domain.Entities;

namespace ProbMind.Application.Abstractions.Persistence;

public interface IUnitOfWork
{
    IRepository<User> Users { get; }
    IRepository<RefreshToken> RefreshTokens { get; }
    IRepository<Topic> Topics { get; }
    IRepository<Misconception> Misconceptions { get; }
    IRepository<Question> Questions { get; }
    IRepository<QuestionVersion> QuestionVersions { get; }
    IRepository<AnswerOption> AnswerOptions { get; }
    IRepository<QuestionMisconceptionMap> QuestionMisconceptionMaps { get; }
    IRepository<DiagnosticSession> DiagnosticSessions { get; }
    IRepository<DiagnosticAnswer> DiagnosticAnswers { get; }
    IRepository<MisconceptionEvidence> MisconceptionEvidence { get; }
    IRepository<UserMisconception> UserMisconceptions { get; }
    IRepository<TopicMastery> TopicMasteries { get; }
    IRepository<TopicMasteryHistory> TopicMasteryHistory { get; }
    IRepository<LearningPath> LearningPaths { get; }
    IRepository<LearningPathStep> LearningPathSteps { get; }
    IRepository<LearningPathRevision> LearningPathRevisions { get; }
    IRepository<Recommendation> Recommendations { get; }
    IRepository<CorrectiveModule> CorrectiveModules { get; }
    IRepository<PracticeSession> PracticeSessions { get; }
    IRepository<PracticeAttempt> PracticeAttempts { get; }
    IRepository<ActivityEvent> ActivityEvents { get; }
    IRepository<AuditLog> AuditLogs { get; }
    IRepository<Notification> Notifications { get; }
    IRepository<QuestionExposure> QuestionExposures { get; }
    IRepository<DiagnosticReport> DiagnosticReports { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
