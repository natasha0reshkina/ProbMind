using ProbMind.Application.Contracts;

namespace ProbMind.Application.Services;

public interface IExportService
{
    Task<ExportFileDto> StudentProfileXlsxAsync(Guid studentId, CancellationToken ct = default);
    Task<ExportFileDto> CohortMisconceptionsXlsxAsync(CancellationToken ct = default);
    Task<ExportFileDto> QuestionAnalyticsXlsxAsync(Guid? topicId, CancellationToken ct = default);
}
