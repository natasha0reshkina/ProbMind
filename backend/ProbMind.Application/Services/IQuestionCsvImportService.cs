using ProbMind.Application.Contracts;

namespace ProbMind.Application.Services;

public interface IQuestionCsvImportService
{
    Task<QuestionCsvImportResultDto> ImportAsync(Guid actorId, string csv, CancellationToken ct = default);
}
