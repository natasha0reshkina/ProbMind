namespace ProbMind.Application.Contracts;

public sealed record UpdateLeaderboardParticipationRequest(bool Participate);
public sealed record UpdateLeaderboardAvailabilityRequest(bool Enabled);

public sealed record GamificationProfileDto(
    bool LeaderboardEnabled,
    bool ParticipatesInLeaderboard,
    int CurrentStreak,
    int LongestStreak,
    int ActiveDays,
    int OverallPoints,
    int? OverallRank,
    int Participants);

public sealed record LeaderboardEntryDto(
    Guid UserId,
    string DisplayName,
    int Rank,
    double Score,
    int CurrentStreak,
    string Detail);

public sealed record LeaderboardDto(
    bool LeaderboardEnabled,
    string Category,
    int Participants,
    IReadOnlyList<LeaderboardEntryDto> Entries);

public sealed record TeacherGamificationStudentDto(
    Guid UserId,
    string DisplayName,
    string Email,
    bool ParticipatesInLeaderboard,
    int CurrentStreak,
    int LongestStreak,
    int ActiveDays,
    int OverallPoints,
    DateTimeOffset? LastActivityAt);
