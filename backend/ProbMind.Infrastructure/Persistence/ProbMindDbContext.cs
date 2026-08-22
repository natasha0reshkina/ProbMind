using Microsoft.EntityFrameworkCore;
using ProbMind.Domain.Entities;

namespace ProbMind.Infrastructure.Persistence;

public sealed class ProbMindDbContext : DbContext
{
    public ProbMindDbContext(DbContextOptions<ProbMindDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<Misconception> Misconceptions => Set<Misconception>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<QuestionVersion> QuestionVersions => Set<QuestionVersion>();
    public DbSet<AnswerOption> AnswerOptions => Set<AnswerOption>();
    public DbSet<QuestionMisconceptionMap> QuestionMisconceptionMaps => Set<QuestionMisconceptionMap>();
    public DbSet<DiagnosticSession> DiagnosticSessions => Set<DiagnosticSession>();
    public DbSet<DiagnosticAnswer> DiagnosticAnswers => Set<DiagnosticAnswer>();
    public DbSet<MisconceptionEvidence> MisconceptionEvidence => Set<MisconceptionEvidence>();
    public DbSet<UserMisconception> UserMisconceptions => Set<UserMisconception>();
    public DbSet<TopicMastery> TopicMasteries => Set<TopicMastery>();
    public DbSet<TopicMasteryHistory> TopicMasteryHistory => Set<TopicMasteryHistory>();
    public DbSet<LearningPath> LearningPaths => Set<LearningPath>();
    public DbSet<LearningPathStep> LearningPathSteps => Set<LearningPathStep>();
    public DbSet<Recommendation> Recommendations => Set<Recommendation>();
    public DbSet<CorrectiveModule> CorrectiveModules => Set<CorrectiveModule>();
    public DbSet<PracticeSession> PracticeSessions => Set<PracticeSession>();
    public DbSet<PracticeAttempt> PracticeAttempts => Set<PracticeAttempt>();
    public DbSet<ActivityEvent> ActivityEvents => Set<ActivityEvent>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<QuestionExposure> QuestionExposures => Set<QuestionExposure>();
    public DbSet<DiagnosticReport> DiagnosticReports => Set<DiagnosticReport>();
    public DbSet<LearningPathRevision> LearningPathRevisions => Set<LearningPathRevision>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProbMindDbContext).Assembly);
    }
}
