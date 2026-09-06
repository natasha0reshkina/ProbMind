using ProbMind.Application.Contracts;

namespace ProbMind.Application.Services;

public interface IGamificationService
{
    Task<GamificationProfileDto> ProfileAsync(Guid userId, CancellationToken ct = default);
    Task<GamificationProfileDto> SetParticipationAsync(Guid userId, bool participate, CancellationToken ct = default);
    Task<LeaderboardDto> LeaderboardAsync(Guid userId, string category, CancellationToken ct = default);
    Task<LeaderboardDto> TeacherLeaderboardAsync(string category, CancellationToken ct = default);
    Task<IReadOnlyList<TeacherGamificationStudentDto>> StudentsAsync(CancellationToken ct = default);
    Task<bool> SetLeaderboardEnabledAsync(bool enabled, CancellationToken ct = default);
}
