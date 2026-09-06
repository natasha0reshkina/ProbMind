using ProbMind.Application.Contracts;

namespace ProbMind.Application.Services;

public interface IStudyWorkspaceService
{
    Task<IReadOnlyList<StudyItemDto>> ListForStudentAsync(Guid studentId, CancellationToken ct = default);
    Task<StudyItemDto> CreateStudentItemAsync(Guid studentId, CreateStudentStudyItemRequest request, CancellationToken ct = default);
    Task<StudyItemDto> SaveStudentNoteAsync(Guid studentId, Guid itemId, SaveStudyItemNoteRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<StudyItemDto>> ListForTeacherAsync(CancellationToken ct = default);
    Task<IReadOnlyList<StudyItemCommentDto>> ListCommentsForTeacherAsync(CancellationToken ct = default);
    Task<StudyItemDto> CreateAssignmentAsync(Guid teacherId, CreateTeacherAssignmentRequest request, CancellationToken ct = default);
    Task<StudyItemDto> RespondAsync(Guid teacherId, Guid itemId, TeacherStudyResponseRequest request, CancellationToken ct = default);
}
