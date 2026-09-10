using ProbMind.Application.Abstractions.Persistence;
using ProbMind.Application.Contracts;
using ProbMind.Domain.Entities;
using ProbMind.Domain.Enums;

namespace ProbMind.Application.Services;

public sealed class GamificationService : IGamificationService
{
    private static readonly Guid SettingsId = Guid.Parse("3188df33-4f7d-46f9-87f1-9f0d1f2a15a4");
    private readonly IUnitOfWork _uow;

    public GamificationService(IUnitOfWork uow) => _uow = uow;

    public async Task<GamificationProfileDto> ProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await RequireStudentAsync(userId, ct);
        var settings = await SettingsAsync(ct);
        var stats = await BuildStatsAsync(ct);
        var own = stats.Single(x => x.UserId == userId);
        var participants = stats.Where(x => x.Participates).ToArray();
        var rank = user.LeaderboardOptIn
            ? Rank(participants, "overall").FirstOrDefault(x => x.UserId == userId)?.Rank
            : null;

        return new GamificationProfileDto(
            settings.LeaderboardEnabled,
            user.LeaderboardOptIn,
            own.CurrentStreak,
            own.LongestStreak,
            own.ActiveDays,
            own.OverallPoints,
            rank,
            participants.Length);
    }

    public async Task<GamificationProfileDto> SetParticipationAsync(Guid userId, bool participate, CancellationToken ct = default)
    {
        var user = await RequireStudentAsync(userId, ct);
        user.LeaderboardOptIn = participate;
        user.Touch();
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync(ct);
        return await ProfileAsync(userId, ct);
    }

    public async Task<LeaderboardDto> LeaderboardAsync(Guid userId, string category, CancellationToken ct = default)
    {
        await RequireStudentAsync(userId, ct);
        var settings = await SettingsAsync(ct);
        var normalized = NormalizeCategory(category);
        var stats = await BuildStatsAsync(ct);
        var participants = stats.Where(x => x.Participates).ToArray();
        var entries = settings.LeaderboardEnabled ? Rank(participants, normalized) : Array.Empty<LeaderboardEntryDto>();
        return new LeaderboardDto(settings.LeaderboardEnabled, normalized, participants.Length, entries);
    }

    public async Task<LeaderboardDto> TeacherLeaderboardAsync(string category, CancellationToken ct = default)
    {
        var settings = await SettingsAsync(ct);
        var normalized = NormalizeCategory(category);
        var stats = await BuildStatsAsync(ct);
        var participants = stats.Where(x => x.Participates).ToArray();
        return new LeaderboardDto(settings.LeaderboardEnabled, normalized, participants.Length, Rank(participants, normalized));
    }

    public async Task<IReadOnlyList<TeacherGamificationStudentDto>> StudentsAsync(CancellationToken ct = default)
    {
        var stats = await BuildStatsAsync(ct);
        return stats
            .OrderByDescending(x => x.CurrentStreak)
            .ThenByDescending(x => x.LongestStreak)
            .ThenBy(x => x.DisplayName)
            .Select(x => new TeacherGamificationStudentDto(
                x.UserId,
                x.DisplayName,
                x.Email,
                x.Participates,
                x.CurrentStreak,
                x.LongestStreak,
                x.ActiveDays,
                x.OverallPoints,
                x.LastActivityAt))
            .ToArray();
    }

    public async Task<bool> SetLeaderboardEnabledAsync(bool enabled, CancellationToken ct = default)
    {
        var existing = await _uow.GamificationSettings.GetByIdAsync(SettingsId, ct);
        if (existing is null)
        {
            existing = new GamificationSettings
            {
                Id = SettingsId,
                LeaderboardEnabled = enabled
            };
            await _uow.GamificationSettings.AddAsync(existing, ct);
        }
        else
        {
            existing.LeaderboardEnabled = enabled;
            existing.Touch();
            _uow.GamificationSettings.Update(existing);
        }

        await _uow.SaveChangesAsync(ct);
        return existing.LeaderboardEnabled;
    }

    private async Task<User> RequireStudentAsync(Guid userId, CancellationToken ct)
    {
        var user = await _uow.Users.GetByIdAsync(userId, ct)
            ?? throw new KeyNotFoundException("Пользователь не найден.");
        if (user.Role != UserRole.Student)
            throw new InvalidOperationException("Геймификация доступна только студентам.");
        return user;
    }

    private async Task<GamificationSettings> SettingsAsync(CancellationToken ct)
    {
        return await _uow.GamificationSettings.GetByIdAsync(SettingsId, ct)
            ?? new GamificationSettings
            {
                Id = SettingsId,
                LeaderboardEnabled = true
            };
    }

    private async Task<IReadOnlyList<StudentStats>> BuildStatsAsync(CancellationToken ct)
    {
        var students = await _uow.Users.WhereAsync(x => x.Role == UserRole.Student && x.IsActive && !x.Email.EndsWith("@probmind.test"), ct);
        var events = await _uow.ActivityEvents.ListAsync(ct);
        var masteries = await _uow.TopicMasteries.ListAsync(ct);
        var hiddenExamSessions = await ExamSessionRules.ActiveSessionIdsAsync(_uow, null, ct);
        var diagnosticAnswers = (await _uow.DiagnosticAnswers.ListAsync(ct))
            .Where(x => !hiddenExamSessions.Contains(x.SessionId))
            .ToArray();
        var diagnosticSessions = await _uow.DiagnosticSessions.ListAsync(ct);
        var practiceAttempts = await _uow.PracticeAttempts.ListAsync(ct);
        var practiceSessions = await _uow.PracticeSessions.ListAsync(ct);

        var result = new List<StudentStats>();
        foreach (var student in students)
        {
            var userEvents = events
                .Where(x => x.UserId == student.Id && x.EventType != ActivityEventType.UserRegistered)
                .OrderBy(x => x.OccurredAt)
                .ToArray();
            var streak = CalculateStreak(userEvents.Select(x => x.OccurredAt));
            var userMastery = masteries.Where(x => x.UserId == student.Id && x.ObservationCount > 0).ToArray();
            var mastery = userMastery.Length == 0 ? 0d : userMastery.Average(x => x.Mastery);
            var diag = diagnosticAnswers.Where(x => x.UserId == student.Id).ToArray();
            var practice = practiceAttempts.Where(x => x.UserId == student.Id).ToArray();
            var completedDiagnostics = diagnosticSessions.Count(x => x.UserId == student.Id && x.Status == DiagnosticStatus.ReportReady);
            var completedPractice = practiceSessions.Count(x => x.UserId == student.Id && x.Status == PracticeStatus.Completed);
            var diagCorrect = diag.Count(x => x.IsCorrect);
            var practiceCorrect = practice.Count(x => x.IsCorrect);
            var diagAccuracy = diag.Length == 0 ? 0d : diagCorrect / (double)diag.Length;
            var practiceAccuracy = practice.Length == 0 ? 0d : practiceCorrect / (double)practice.Length;
            var points = (int)Math.Round(mastery * 100d)
                + diagCorrect * 5
                + practiceCorrect * 4
                + completedDiagnostics * 25
                + completedPractice * 20
                + streak.Current * 5;

            result.Add(new StudentStats(
                student.Id,
                student.DisplayName,
                student.Email,
                student.LeaderboardOptIn,
                mastery,
                diagAccuracy,
                practiceAccuracy,
                diag.Length,
                practice.Length,
                streak.Current,
                streak.Longest,
                streak.ActiveDays,
                points,
                userEvents.LastOrDefault()?.OccurredAt));
        }

        return result;
    }

    private static IReadOnlyList<LeaderboardEntryDto> Rank(IEnumerable<StudentStats> source, string category)
    {
        var ordered = source
            .Select(x => new { Stats = x, Score = Score(x, category), Detail = Detail(x, category) })
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Stats.CurrentStreak)
            .ThenBy(x => x.Stats.DisplayName)
            .ToArray();

        return ordered
            .Select((x, index) => new LeaderboardEntryDto(
                x.Stats.UserId,
                x.Stats.DisplayName,
                index + 1,
                x.Score,
                x.Stats.CurrentStreak,
                x.Detail))
            .ToArray();
    }

    private static double Score(StudentStats stats, string category) => category switch
    {
        "mastery" => stats.Mastery * 100d,
        "diagnostics" => stats.DiagnosticAccuracy * 100d,
        "practice" => stats.PracticeAccuracy * 100d,
        "streak" => stats.CurrentStreak,
        _ => stats.OverallPoints
    };

    private static string Detail(StudentStats stats, string category) => category switch
    {
        "mastery" => $"Освоение тем: {Math.Round(stats.Mastery * 100d)}%",
        "diagnostics" => stats.DiagnosticAnswers == 0 ? "Диагностических ответов пока нет" : $"Правильных: {Math.Round(stats.DiagnosticAccuracy * 100d)}% · ответов {stats.DiagnosticAnswers}",
        "practice" => stats.PracticeAnswers == 0 ? "Практических ответов пока нет" : $"Правильных: {Math.Round(stats.PracticeAccuracy * 100d)}% · ответов {stats.PracticeAnswers}",
        "streak" => $"Текущий стрик: {stats.CurrentStreak} дн.",
        _ => $"{stats.OverallPoints} баллов · стрик {stats.CurrentStreak} дн."
    };

    private static string NormalizeCategory(string? category) => category?.Trim().ToLowerInvariant() switch
    {
        "mastery" => "mastery",
        "diagnostics" => "diagnostics",
        "practice" => "practice",
        "streak" => "streak",
        _ => "overall"
    };

    private static StreakStats CalculateStreak(IEnumerable<DateTimeOffset> timestamps)
    {
        var days = timestamps
            .Select(x => DateOnly.FromDateTime(x.UtcDateTime))
            .Distinct()
            .OrderBy(x => x)
            .ToArray();
        if (days.Length == 0)
            return new StreakStats(0, 0, 0);

        var longest = 1;
        var run = 1;
        for (var i = 1; i < days.Length; i++)
        {
            run = days[i].DayNumber - days[i - 1].DayNumber == 1 ? run + 1 : 1;
            longest = Math.Max(longest, run);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var last = days[^1];
        var current = 0;
        if (today.DayNumber - last.DayNumber <= 1)
        {
            current = 1;
            for (var i = days.Length - 1; i > 0; i--)
            {
                if (days[i].DayNumber - days[i - 1].DayNumber != 1)
                    break;
                current++;
            }
        }

        return new StreakStats(current, longest, days.Length);
    }

    private sealed record StreakStats(int Current, int Longest, int ActiveDays);

    private sealed record StudentStats(
        Guid UserId,
        string DisplayName,
        string Email,
        bool Participates,
        double Mastery,
        double DiagnosticAccuracy,
        double PracticeAccuracy,
        int DiagnosticAnswers,
        int PracticeAnswers,
        int CurrentStreak,
        int LongestStreak,
        int ActiveDays,
        int OverallPoints,
        DateTimeOffset? LastActivityAt);
}
