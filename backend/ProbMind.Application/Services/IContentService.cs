using ProbMind.Application.Contracts;
using ProbMind.Domain.Enums;

namespace ProbMind.Application.Services;

public interface IContentService
{
    Task<IReadOnlyList<TopicDto>> ListTopicsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<MisconceptionCatalogDto>> ListMisconceptionsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<QuestionSummaryDto>> ListQuestionsAsync(Guid? topicId, ContentStatus? status, CancellationToken ct = default);
    Task<QuestionDetailDto> GetQuestionAsync(Guid questionId, CancellationToken ct = default);
    Task<QuestionDetailDto> CreateQuestionAsync(Guid actorId, CreateQuestionRequest request, CancellationToken ct = default);
    Task<QuestionDetailDto> CreateVersionAsync(Guid actorId, CreateQuestionVersionRequest request, CancellationToken ct = default);
    Task<QuestionDetailDto> PublishAsync(Guid actorId, Guid questionId, CancellationToken ct = default);
    Task<QuestionDetailDto> ArchiveAsync(Guid actorId, Guid questionId, CancellationToken ct = default);
}
