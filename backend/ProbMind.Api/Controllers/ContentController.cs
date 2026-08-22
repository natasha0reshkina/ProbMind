using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProbMind.Api.Auth;
using ProbMind.Application.Contracts;
using ProbMind.Application.Services;
using ProbMind.Domain.Enums;

namespace ProbMind.Api.Controllers;

[ApiController]
[Route("api/content")]
[Authorize]
public sealed class ContentController : ControllerBase
{
    private readonly IContentService _content;

    public ContentController(IContentService content) => _content = content;

    [HttpGet("topics")]
    public Task<IReadOnlyList<TopicDto>> Topics(CancellationToken ct) =>
        _content.ListTopicsAsync(ct);

    [HttpGet("misconceptions")]
    public Task<IReadOnlyList<MisconceptionCatalogDto>> Misconceptions(CancellationToken ct) =>
        _content.ListMisconceptionsAsync(ct);

    [HttpGet("questions")]
    [Authorize(Roles = "Teacher,Admin")]
    public Task<IReadOnlyList<QuestionSummaryDto>> Questions(
        [FromQuery] Guid? topicId,
        [FromQuery] ContentStatus? status,
        CancellationToken ct) =>
        _content.ListQuestionsAsync(topicId, status, ct);

    [HttpGet("questions/{questionId:guid}")]
    [Authorize(Roles = "Teacher,Admin")]
    public Task<QuestionDetailDto> Question(Guid questionId, CancellationToken ct) =>
        _content.GetQuestionAsync(questionId, ct);

    [HttpPost("questions")]
    [Authorize(Roles = "Teacher,Admin")]
    public Task<QuestionDetailDto> Create(CreateQuestionRequest request, CancellationToken ct) =>
        _content.CreateQuestionAsync(UserContext.UserId(User), request, ct);

    [HttpPost("questions/versions")]
    [Authorize(Roles = "Teacher,Admin")]
    public Task<QuestionDetailDto> Version(CreateQuestionVersionRequest request, CancellationToken ct) =>
        _content.CreateVersionAsync(UserContext.UserId(User), request, ct);

    [HttpPost("questions/{questionId:guid}/publish")]
    [Authorize(Roles = "Teacher,Admin")]
    public Task<QuestionDetailDto> Publish(Guid questionId, CancellationToken ct) =>
        _content.PublishAsync(UserContext.UserId(User), questionId, ct);

    [HttpPost("questions/{questionId:guid}/archive")]
    [Authorize(Roles = "Teacher,Admin")]
    public Task<QuestionDetailDto> Archive(Guid questionId, CancellationToken ct) =>
        _content.ArchiveAsync(UserContext.UserId(User), questionId, ct);
}
