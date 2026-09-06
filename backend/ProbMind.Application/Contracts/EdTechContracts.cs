namespace ProbMind.Application.Contracts;

public sealed record SpacedReviewDto(
    Guid TopicId,
    string TopicCode,
    string TopicName,
    double Mastery,
    DateTimeOffset NextReviewAt,
    DateTimeOffset? LastReviewedAt,
    int IntervalDays,
    int Repetitions,
    bool IsDue);

public sealed record CompleteSpacedReviewRequest(int Quality);

public sealed record ConfidenceAnswerDto(
    Guid Id,
    string Source,
    string TopicName,
    string Prompt,
    bool IsCorrect,
    int ConfidenceLevel,
    string Reasoning,
    DateTimeOffset SubmittedAt,
    Guid? StudentId = null,
    string? StudentName = null);

public sealed record ConfidenceSummaryDto(
    int AnswersWithConfidence,
    double MeanConfidence,
    double Accuracy,
    int OverconfidentWrong,
    int LowConfidenceCorrect,
    IReadOnlyList<ConfidenceAnswerDto> Answers);

public sealed record TeacherConfidenceStudentDto(
    Guid StudentId,
    string DisplayName,
    int AnswersWithConfidence,
    double MeanConfidence,
    double Accuracy,
    int OverconfidentWrong,
    int LowConfidenceCorrect);

public sealed record StudentGroupMemberDto(Guid StudentId, string DisplayName, string Email);

public sealed record StudentGroupDto(
    Guid Id,
    string Name,
    string Description,
    IReadOnlyList<StudentGroupMemberDto> Members,
    DateTimeOffset CreatedAt);

public sealed record CreateStudentGroupRequest(string Name, string Description, IReadOnlyList<Guid> StudentIds);
public sealed record UpdateStudentGroupMembersRequest(IReadOnlyList<Guid> StudentIds);

public sealed record InterventionSuggestionDto(
    Guid StudentId,
    string DisplayName,
    double Mastery,
    int ActiveMisconceptions,
    int DaysInactive,
    string Reason);

public sealed record TeacherInterventionDto(
    Guid Id,
    Guid StudentId,
    string StudentName,
    string Title,
    string Body,
    string Kind,
    bool IsCompleted,
    DateTimeOffset? DueAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);

public sealed record CreateTeacherInterventionRequest(
    Guid StudentId,
    string Title,
    string Body,
    string Kind,
    DateTimeOffset? DueAt);

public sealed record CreateExamRequest(
    Guid DiagnosticTemplateId,
    Guid? GroupId,
    Guid? StudentId,
    string Title,
    string Description,
    int TimeLimitMinutes,
    bool IsPublished,
    DateTimeOffset? AvailableFrom,
    DateTimeOffset? AvailableUntil);

public sealed record ExamDto(
    Guid Id,
    Guid DiagnosticTemplateId,
    Guid? GroupId,
    string? GroupName,
    Guid? StudentId,
    string? StudentName,
    string Title,
    string Description,
    int TimeLimitMinutes,
    int QuestionCount,
    bool IsPublished,
    DateTimeOffset? AvailableFrom,
    DateTimeOffset? AvailableUntil,
    Guid? SessionId,
    string? SessionStatus,
    double? Score,
    DateTimeOffset? StartedAt,
    int AssignedStudents,
    int CompletedStudents,
    bool AllCompleted);

public sealed record ExamAnswerReviewDto(
    int Position,
    string Prompt,
    string? SelectedAnswer,
    string CorrectAnswer,
    bool? IsCorrect,
    int? ConfidenceLevel,
    string? Reasoning,
    string? StudentNote,
    int? ResponseTimeMs);

public sealed record ExamStudentReviewDto(
    Guid StudentId,
    string StudentName,
    string Email,
    string Status,
    double? Score,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<ExamAnswerReviewDto> Answers);

public sealed record TeacherExamReviewDto(
    Guid ExamId,
    string Title,
    string Audience,
    int AssignedStudents,
    int CompletedStudents,
    bool AllCompleted,
    IReadOnlyList<ExamStudentReviewDto> Students);

public sealed record ExamStartDto(Guid ExamId, Guid SessionId, int TimeLimitMinutes, DateTimeOffset StartedAt, DateTimeOffset ExpiresAt);
public sealed record ExamSessionContextDto(bool IsExam, Guid? ExamId, string? Title, int? TimeLimitMinutes, DateTimeOffset? StartedAt, DateTimeOffset? ExpiresAt);

public sealed record AchievementDto(
    string Code,
    string Title,
    string Description,
    bool Unlocked,
    int Progress,
    int Target);

public sealed record CreateMaterialStudyCycleRequest(
    string Title,
    string MaterialText,
    Guid PreDiagnosticTemplateId,
    Guid PostDiagnosticTemplateId,
    Guid? GroupId,
    bool IsPublished);

public sealed record MaterialStudyCycleDto(
    Guid Id,
    string Title,
    string MaterialText,
    Guid PreDiagnosticTemplateId,
    Guid PostDiagnosticTemplateId,
    Guid? GroupId,
    string? GroupName,
    bool IsPublished,
    Guid? PreSessionId,
    string? PreStatus,
    double? PreScore,
    DateTimeOffset? MaterialOpenedAt,
    Guid? PostSessionId,
    string? PostStatus,
    double? PostScore,
    double? ScoreDelta,
    DateTimeOffset CreatedAt);

public sealed record TeacherMaterialCycleAnalyticsDto(
    Guid Id,
    string Title,
    string? GroupName,
    bool IsPublished,
    int Students,
    int CompletedPre,
    int OpenedMaterial,
    int CompletedPost,
    double? MeanPreScore,
    double? MeanPostScore,
    double? MeanDelta,
    DateTimeOffset CreatedAt);
