using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProbMind.Application.Services;

namespace ProbMind.Api.Controllers;

[ApiController]
[Route("api/exports")]
[Authorize(Roles = "Teacher,Admin")]
public sealed class ExportsController : ControllerBase
{
    private readonly IExportService _exports;

    public ExportsController(IExportService exports) => _exports = exports;

    [HttpGet("students/{studentId:guid}")]
    public async Task<IActionResult> Student(Guid studentId, CancellationToken ct)
    {
        var file = await _exports.StudentProfileCsvAsync(studentId, ct);
        return File(Encoding.UTF8.GetBytes(file.TextContent), file.ContentType, file.FileName);
    }

    [HttpGet("cohort/misconceptions")]
    public async Task<IActionResult> Cohort(CancellationToken ct)
    {
        var file = await _exports.CohortMisconceptionsCsvAsync(ct);
        return File(Encoding.UTF8.GetBytes(file.TextContent), file.ContentType, file.FileName);
    }

    [HttpGet("questions")]
    public async Task<IActionResult> Questions([FromQuery] Guid? topicId, CancellationToken ct)
    {
        var file = await _exports.QuestionAnalyticsCsvAsync(topicId, ct);
        return File(Encoding.UTF8.GetBytes(file.TextContent), file.ContentType, file.FileName);
    }
}
