namespace ProbMind.Domain.Enums;

public enum ActivityEventType
{
    UserRegistered,
    DiagnosticStarted,
    AnswerSubmitted,
    DiagnosticCompleted,
    MisconceptionDetected,
    MisconceptionCorrected,
    LearningPathRebuilt,
    PracticeStarted,
    PracticeCompleted,
}
