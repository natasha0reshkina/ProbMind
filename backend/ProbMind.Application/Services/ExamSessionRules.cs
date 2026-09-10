using ProbMind.Application.Abstractions.Persistence;
using ProbMind.Domain.Entities;
using ProbMind.Domain.Enums;

namespace ProbMind.Application.Services;

internal static class ExamSessionRules
{
    public static async Task<HashSet<Guid>> ActiveSessionIdsAsync(
        IUnitOfWork uow,
        Guid? userId = null,
        CancellationToken ct = default)
    {
        var attempts = userId.HasValue
            ? await uow.ExamAttempts.WhereAsync(x => x.StudentId == userId.Value, ct)
            : await uow.ExamAttempts.ListAsync(ct);

        if (attempts.Count == 0)
            return new HashSet<Guid>();

        var ids = attempts.Select(x => x.DiagnosticSessionId).ToHashSet();
        var sessions = userId.HasValue
            ? await uow.DiagnosticSessions.WhereAsync(
                x => x.UserId == userId.Value && ids.Contains(x.Id),
                ct)
            : await uow.DiagnosticSessions.WhereAsync(x => ids.Contains(x.Id), ct);

        return sessions
            .Where(x => x.Status is DiagnosticStatus.Created or DiagnosticStatus.InProgress)
            .Select(x => x.Id)
            .ToHashSet();
    }

    public static async Task<(ExamAttempt Attempt, DiagnosticSession Session)?> ActiveAttemptAsync(
        IUnitOfWork uow,
        Guid userId,
        CancellationToken ct = default)
    {
        var attempts = await uow.ExamAttempts.WhereAsync(x => x.StudentId == userId, ct);
        if (attempts.Count == 0)
            return null;

        var ids = attempts.Select(x => x.DiagnosticSessionId).ToHashSet();
        var sessions = await uow.DiagnosticSessions.WhereAsync(
            x => x.UserId == userId && ids.Contains(x.Id),
            ct);
        var activeSession = sessions
            .Where(x => x.Status is DiagnosticStatus.Created or DiagnosticStatus.InProgress)
            .OrderByDescending(x => x.UpdatedAt)
            .FirstOrDefault();
        if (activeSession is null)
            return null;

        var attempt = attempts.First(x => x.DiagnosticSessionId == activeSession.Id);
        return (attempt, activeSession);
    }

    public static Task<bool> IsExamSessionAsync(
        IUnitOfWork uow,
        Guid sessionId,
        CancellationToken ct = default) =>
        uow.ExamAttempts.AnyAsync(x => x.DiagnosticSessionId == sessionId, ct);
}
