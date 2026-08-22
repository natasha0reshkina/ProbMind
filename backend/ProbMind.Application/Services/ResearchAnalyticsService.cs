using System.Text.Json;
using ProbMind.Application.Abstractions.Persistence;
using ProbMind.Application.Contracts;
using ProbMind.Domain.Analytics;
using ProbMind.Domain.Enums;

namespace ProbMind.Application.Services;

public sealed class ResearchAnalyticsService : IResearchAnalyticsService
{
    private readonly IUnitOfWork _uow;
    private readonly MisconceptionCooccurrenceAnalyzer _cooccurrence;
    private readonly ProbMind.Domain.Diagnostics.ConfidenceCalibrationService _calibration;
    private readonly LearningPathStabilityAnalyzer _pathStability;

    public ResearchAnalyticsService(
        IUnitOfWork uow,
        MisconceptionCooccurrenceAnalyzer cooccurrence,
        ProbMind.Domain.Diagnostics.ConfidenceCalibrationService calibration,
        LearningPathStabilityAnalyzer pathStability)
    {
        _uow = uow;
        _cooccurrence = cooccurrence;
        _calibration = calibration;
        _pathStability = pathStability;
    }

    public async Task<IReadOnlyList<CooccurrenceEdgeDto>> MisconceptionCooccurrenceAsync(
        CancellationToken ct = default)
    {
        var students = await _uow.Users.WhereAsync(
            x => x.Role == UserRole.Student && x.IsActive,
            ct);
        var states = await _uow.UserMisconceptions.ListAsync(ct);
        var catalog = (await _uow.Misconceptions.ListAsync(ct)).ToDictionary(x => x.Id);

        var sets = students.Select(student =>
        {
            var active = states
                .Where(x => x.UserId == student.Id)
                .Where(x => x.Status is MisconceptionStatus.Suspected
                    or MisconceptionStatus.Detected
                    or MisconceptionStatus.CorrectionInProgress
                    or MisconceptionStatus.RecheckRequired)
                .Select(x => x.MisconceptionId)
                .ToHashSet();

            return new LearnerMisconceptionSet(student.Id, active);
        }).ToArray();

        return _cooccurrence.Analyze(sets)
            .Where(x => catalog.ContainsKey(x.A) && catalog.ContainsKey(x.B))
            .Select(x => new CooccurrenceEdgeDto(
                x.A,
                catalog[x.A].Code,
                x.B,
                catalog[x.B].Code,
                x.Together,
                x.Jaccard,
                x.Lift))
            .ToArray();
    }

    public async Task<CalibrationReportDto> ConfidenceCalibrationAsync(
        CancellationToken ct = default)
    {
        var states = await _uow.UserMisconceptions.ListAsync(ct);
        var evidence = await _uow.MisconceptionEvidence.ListAsync(ct);

        var observations = new List<(double Predicted, bool Confirmed)>();
        foreach (var state in states)
        {
            var transfer = evidence
                .Where(x => x.UserId == state.UserId && x.MisconceptionId == state.MisconceptionId)
                .Where(x => x.Kind is EvidenceKind.TransferSuccess or EvidenceKind.TransferFailure)
                .OrderByDescending(x => x.ObservedAt)
                .FirstOrDefault();

            if (transfer is null)
                continue;

            observations.Add((
                Math.Clamp(state.Confidence, 0d, 1d),
                transfer.Kind == EvidenceKind.TransferFailure));
        }

        var result = _calibration.Calibrate(observations, 10);
        return new CalibrationReportDto(
            result.Buckets.Select(x => new CalibrationBucketDto(
                x.From,
                x.To,
                x.Predictions,
                x.Confirmed,
                x.ObservedRate)).ToArray(),
            result.BrierScore,
            result.MeanAbsoluteCalibrationError);
    }

    public async Task<PathStabilityDto> LearningPathStabilityAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var revisions = await _uow.LearningPathRevisions.WhereAsync(x => x.UserId == userId, ct);
        var snapshots = new List<PathRevisionSnapshot>();

        foreach (var revision in revisions.OrderBy(x => x.RevisionNumber))
        {
            var keys = new List<Guid>();
            try
            {
                using var doc = JsonDocument.Parse(revision.SnapshotJson);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in doc.RootElement.EnumerateArray())
                    {
                        if (item.TryGetProperty("TopicId", out var raw) &&
                            raw.ValueKind == JsonValueKind.String &&
                            Guid.TryParse(raw.GetString(), out var id))
                        {
                            keys.Add(id);
                        }
                    }
                }
            }
            catch (JsonException)
            {
            }

            snapshots.Add(new PathRevisionSnapshot(
                revision.RevisionNumber,
                revision.RecordedAt,
                keys));
        }

        var result = _pathStability.Analyze(snapshots);
        return new PathStabilityDto(
            result.Stability,
            result.Revisions,
            result.MeanRetention,
            result.Interpretation);
    }
}
