using ProbMind.Application.Contracts;

namespace ProbMind.Application.Services;

public interface IExportService
{
    Task<ExportFileDto> StudentProfileCsvAsync(Guid studentId, CancellationToken ct = default);
    Task<ExportFileDto> CohortMisconceptionsCsvAsync(CancellationToken ct = default);
    Task<ExportFileDto> QuestionAnalyticsCsvAsync(Guid? topicId, CancellationToken ct = default);
}
