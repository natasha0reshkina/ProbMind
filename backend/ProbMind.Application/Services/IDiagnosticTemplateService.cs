using ProbMind.Application.Contracts;

namespace ProbMind.Application.Services;

public interface IDiagnosticTemplateService
{
    Task<IReadOnlyList<DiagnosticTemplateDto>> ListForTeacherAsync(CancellationToken ct = default);
    Task<IReadOnlyList<DiagnosticTemplateDto>> ListForStudentAsync(Guid studentId, CancellationToken ct = default);
    Task<IReadOnlyList<DiagnosticTemplateQuestionCandidateDto>> ListQuestionsAsync(Guid actorId, CancellationToken ct = default);
    Task<DiagnosticTemplateQuestionCandidateDto> CreateQuestionAsync(Guid actorId, CreateDiagnosticTemplateQuestionRequest request, CancellationToken ct = default);
    Task<DiagnosticTemplateDto> CreateAsync(Guid actorId, CreateDiagnosticTemplateRequest request, CancellationToken ct = default);
    Task<DiagnosticTemplateDto> SetPublishedAsync(Guid actorId, Guid templateId, bool isPublished, CancellationToken ct = default);
    Task<DiagnosticTemplateDto> SetAudienceAsync(Guid actorId, Guid templateId, Guid? groupId, Guid? studentId, CancellationToken ct = default);
    Task<DiagnosticTemplateDto> AddQuestionsAsync(Guid actorId, Guid templateId, IReadOnlyList<Guid> questionIds, CancellationToken ct = default);
}
