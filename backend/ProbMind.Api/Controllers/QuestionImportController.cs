using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProbMind.Api.Auth;
using ProbMind.Application.Contracts;
using ProbMind.Application.Services;

namespace ProbMind.Api.Controllers;

[ApiController]
[Route("api/content/questions")]
[Authorize(Roles = "Teacher,Admin")]
public sealed class QuestionImportController : ControllerBase
{
    private readonly IQuestionCsvImportService _import;

    public QuestionImportController(IQuestionCsvImportService import) => _import = import;

    [HttpPost("import-csv")]
    [RequestSizeLimit(2_000_000)]
    public async Task<QuestionCsvImportResultDto> ImportCsv([FromForm] IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0)
            throw new InvalidOperationException("CSV-файл пуст.");
        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Загрузите файл с расширением .csv.");

        await using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream, Encoding.UTF8, true);
        var csv = await reader.ReadToEndAsync();
        return await _import.ImportAsync(UserContext.UserId(User), csv, ct);
    }
}
