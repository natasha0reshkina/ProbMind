using ProbMind.Application.Abstractions.Persistence;
using ProbMind.Domain.Entities;

namespace ProbMind.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ProbMindDbContext _db;

    public UnitOfWork(ProbMindDbContext db)
    {
        _db = db;
        Users = new EfRepository<User>(db);
        RefreshTokens = new EfRepository<RefreshToken>(db);
        Topics = new EfRepository<Topic>(db);
        Misconceptions = new EfRepository<Misconception>(db);
        Questions = new EfRepository<Question>(db);
        QuestionVersions = new EfRepository<QuestionVersion>(db);
        AnswerOptions = new EfRepository<AnswerOption>(db);
        QuestionMisconceptionMaps = new EfRepository<QuestionMisconceptionMap>(db);
        DiagnosticSessions = new EfRepository<DiagnosticSession>(db);
        DiagnosticAnswers = new EfRepository<DiagnosticAnswer>(db);
        MisconceptionEvidence = new EfRepository<MisconceptionEvidence>(db);
        UserMisconceptions = new EfRepository<UserMisconception>(db);
        TopicMasteries = new EfRepository<TopicMastery>(db);
        TopicMasteryHistory = new EfRepository<TopicMasteryHistory>(db);
        LearningPaths = new EfRepository<LearningPath>(db);
        LearningPathSteps = new EfRepository<LearningPathStep>(db);
        LearningPathRevisions = new EfRepository<LearningPathRevision>(db);
        Recommendations = new EfRepository<Recommendation>(db);
        CorrectiveModules = new EfRepository<CorrectiveModule>(db);
        PracticeSessions = new EfRepository<PracticeSession>(db);
        PracticeAttempts = new EfRepository<PracticeAttempt>(db);
        ActivityEvents = new EfRepository<ActivityEvent>(db);
        AuditLogs = new EfRepository<AuditLog>(db);
        Notifications = new EfRepository<Notification>(db);
        QuestionExposures = new EfRepository<QuestionExposure>(db);
        DiagnosticReports = new EfRepository<DiagnosticReport>(db);
        StudyItems = new EfRepository<StudyItem>(db);
        StudyItemNotes = new EfRepository<StudyItemNote>(db);
        DiagnosticTemplates = new EfRepository<DiagnosticTemplate>(db);
        DiagnosticTemplateQuestions = new EfRepository<DiagnosticTemplateQuestion>(db);
        GamificationSettings = new EfRepository<GamificationSettings>(db);
        SpacedReviewItems = new EfRepository<SpacedReviewItem>(db);
        StudentGroups = new EfRepository<StudentGroup>(db);
        StudentGroupMembers = new EfRepository<StudentGroupMember>(db);
        TeacherInterventions = new EfRepository<TeacherIntervention>(db);
        ExamDefinitions = new EfRepository<ExamDefinition>(db);
        ExamAttempts = new EfRepository<ExamAttempt>(db);
        MaterialStudyCycles = new EfRepository<MaterialStudyCycle>(db);
        MaterialStudyProgress = new EfRepository<MaterialStudyProgress>(db);
        StudentAchievements = new EfRepository<StudentAchievement>(db);
    }

    public IRepository<User> Users { get; }
    public IRepository<RefreshToken> RefreshTokens { get; }
    public IRepository<Topic> Topics { get; }
    public IRepository<Misconception> Misconceptions { get; }
    public IRepository<Question> Questions { get; }
    public IRepository<QuestionVersion> QuestionVersions { get; }
    public IRepository<AnswerOption> AnswerOptions { get; }
    public IRepository<QuestionMisconceptionMap> QuestionMisconceptionMaps { get; }
    public IRepository<DiagnosticSession> DiagnosticSessions { get; }
    public IRepository<DiagnosticAnswer> DiagnosticAnswers { get; }
    public IRepository<MisconceptionEvidence> MisconceptionEvidence { get; }
    public IRepository<UserMisconception> UserMisconceptions { get; }
    public IRepository<TopicMastery> TopicMasteries { get; }
    public IRepository<TopicMasteryHistory> TopicMasteryHistory { get; }
    public IRepository<LearningPath> LearningPaths { get; }
    public IRepository<LearningPathStep> LearningPathSteps { get; }
    public IRepository<LearningPathRevision> LearningPathRevisions { get; }
    public IRepository<Recommendation> Recommendations { get; }
    public IRepository<CorrectiveModule> CorrectiveModules { get; }
    public IRepository<PracticeSession> PracticeSessions { get; }
    public IRepository<PracticeAttempt> PracticeAttempts { get; }
    public IRepository<ActivityEvent> ActivityEvents { get; }
    public IRepository<AuditLog> AuditLogs { get; }
    public IRepository<Notification> Notifications { get; }
    public IRepository<QuestionExposure> QuestionExposures { get; }
    public IRepository<DiagnosticReport> DiagnosticReports { get; }
    public IRepository<StudyItem> StudyItems { get; }
    public IRepository<StudyItemNote> StudyItemNotes { get; }
    public IRepository<DiagnosticTemplate> DiagnosticTemplates { get; }
    public IRepository<DiagnosticTemplateQuestion> DiagnosticTemplateQuestions { get; }
    public IRepository<GamificationSettings> GamificationSettings { get; }
    public IRepository<SpacedReviewItem> SpacedReviewItems { get; }
    public IRepository<StudentGroup> StudentGroups { get; }
    public IRepository<StudentGroupMember> StudentGroupMembers { get; }
    public IRepository<TeacherIntervention> TeacherInterventions { get; }
    public IRepository<ExamDefinition> ExamDefinitions { get; }
    public IRepository<ExamAttempt> ExamAttempts { get; }
    public IRepository<MaterialStudyCycle> MaterialStudyCycles { get; }
    public IRepository<MaterialStudyProgress> MaterialStudyProgress { get; }
    public IRepository<StudentAchievement> StudentAchievements { get; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
