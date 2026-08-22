using System.Globalization;
using System.Text;
using ProbMind.Application.Contracts;

namespace ProbMind.Application.Services;

public sealed class ExportService : IExportService
{
    private readonly ITeacherService _teacher;
    private readonly IStatisticsService _statistics;

    public ExportService(ITeacherService teacher, IStatisticsService statistics)
    {
        _teacher = teacher;
        _statistics = statistics;
    }

    public async Task<ExportFileDto> StudentProfileCsvAsync(
        Guid studentId,
        CancellationToken ct = default)
    {
        var student = await _teacher.StudentAsync(studentId, ct);
        var csv = new StringBuilder();
        csv.AppendLine("section,code,name,value,status");

        foreach (var topic in student.Topics)
        {
            csv.AppendLine(string.Join(",",
                "topic",
                Escape(topic.Code),
                Escape(topic.Name),
                Percent(topic.Mastery),
                Escape($"uncertainty={topic.Uncertainty:F3}")));
        }

        foreach (var mc in student.Misconceptions)
        {
            csv.AppendLine(string.Join(",",
                "misconception",
                Escape(mc.Code),
                Escape(mc.Title),
                Percent(mc.Confidence),
                Escape(mc.Status.ToString())));
        }

        return new ExportFileDto(
            $"probmind-student-{studentId:N}.csv",
            "text/csv; charset=utf-8",
            csv.ToString());
    }

    public async Task<ExportFileDto> CohortMisconceptionsCsvAsync(CancellationToken ct = default)
    {
        var items = await _statistics.PrevalenceAsync(ct);
        var csv = new StringBuilder();
        csv.AppendLine("code,title,students_affected,total_students,prevalence,mean_confidence,evidence_count");

        foreach (var item in items)
        {
            csv.AppendLine(string.Join(",",
                Escape(item.Code),
                Escape(item.Title),
                item.StudentsAffected,
                item.TotalStudents,
                item.Prevalence.ToString("F4", CultureInfo.InvariantCulture),
                item.MeanConfidence.ToString("F4", CultureInfo.InvariantCulture),
                item.EvidenceCount));
        }

        return new ExportFileDto(
            "probmind-cohort-misconceptions.csv",
            "text/csv; charset=utf-8",
            csv.ToString());
    }

    public async Task<ExportFileDto> QuestionAnalyticsCsvAsync(
        Guid? topicId,
        CancellationToken ct = default)
    {
        var items = await _teacher.QuestionAnalyticsAsync(topicId, ct);
        var csv = new StringBuilder();
        csv.AppendLine("code,responses,correct_rate,difficulty,discrimination,quality,distractor_entropy,median_seconds");

        foreach (var item in items)
        {
            csv.AppendLine(string.Join(",",
                Escape(item.Code),
                item.Responses,
                item.CorrectRate.ToString("F4", CultureInfo.InvariantCulture),
                item.Difficulty.ToString("F4", CultureInfo.InvariantCulture),
                item.Discrimination.ToString("F4", CultureInfo.InvariantCulture),
                Escape(item.QualityBand),
                item.DistractorEntropy.ToString("F4", CultureInfo.InvariantCulture),
                item.MedianResponseSeconds.ToString("F2", CultureInfo.InvariantCulture)));
        }

        return new ExportFileDto(
            "probmind-question-analytics.csv",
            "text/csv; charset=utf-8",
            csv.ToString());
    }

    private static string Percent(double value) =>
        value.ToString("P1", CultureInfo.InvariantCulture);

    private static string Escape(string value) =>
        "\"" + value.Replace("\"", "\"\"") + "\"";
}
