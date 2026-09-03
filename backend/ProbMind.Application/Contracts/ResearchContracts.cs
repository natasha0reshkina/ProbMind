namespace ProbMind.Application.Contracts;

public sealed record CooccurrenceEdgeDto(
    Guid MisconceptionAId,
    string MisconceptionATitle,
    Guid MisconceptionBId,
    string MisconceptionBTitle,
    int Together,
    int StudentsA,
    int StudentsB,
    int TotalStudents,
    double Jaccard,
    double Lift);

public sealed record CalibrationBucketDto(
    double From,
    double To,
    int Predictions,
    int Confirmed,
    double ObservedRate);

public sealed record CalibrationReportDto(
    IReadOnlyList<CalibrationBucketDto> Buckets,
    double BrierScore,
    double MeanAbsoluteCalibrationError);

public sealed record PathStabilityDto(
    double Stability,
    int Revisions,
    double MeanRetention,
    string Interpretation);

public sealed record ExportFileDto(
    string FileName,
    string ContentType,
    byte[] Content);
