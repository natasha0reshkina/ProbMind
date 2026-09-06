using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProbMind.Api.Auth;
using ProbMind.Application.Contracts;
using ProbMind.Application.Services;

namespace ProbMind.Api.Controllers;

[ApiController]
[Route("api/study-workspace")]
[Authorize]
public sealed class StudyWorkspaceController : ControllerBase
{
    private readonly IStudyWorkspaceService _workspace;

    public StudyWorkspaceController(IStudyWorkspaceService workspace) => _workspace = workspace;

    [HttpGet("student")]
    [Authorize(Roles = "Student")]
    public Task<IReadOnlyList<StudyItemDto>> StudentItems(CancellationToken ct) =>
        _workspace.ListForStudentAsync(UserContext.UserId(User), ct);

    [HttpPost("student")]
    [Authorize(Roles = "Student")]
    public Task<StudyItemDto> CreateStudentItem(CreateStudentStudyItemRequest request, CancellationToken ct) =>
        _workspace.CreateStudentItemAsync(UserContext.UserId(User), request, ct);

    [HttpPut("student/{itemId:guid}/note")]
    [Authorize(Roles = "Student")]
    public Task<StudyItemDto> SaveStudentNote(Guid itemId, SaveStudyItemNoteRequest request, CancellationToken ct) =>
        _workspace.SaveStudentNoteAsync(UserContext.UserId(User), itemId, request, ct);

    [HttpGet("teacher")]
    [Authorize(Roles = "Teacher,Admin")]
    public Task<IReadOnlyList<StudyItemDto>> TeacherItems(CancellationToken ct) =>
        _workspace.ListForTeacherAsync(ct);

    [HttpGet("teacher/comments")]
    [Authorize(Roles = "Teacher,Admin")]
    public Task<IReadOnlyList<StudyItemCommentDto>> TeacherComments(CancellationToken ct) =>
        _workspace.ListCommentsForTeacherAsync(ct);

    [HttpPost("teacher/assignments")]
    [Authorize(Roles = "Teacher,Admin")]
    public Task<StudyItemDto> CreateAssignment(CreateTeacherAssignmentRequest request, CancellationToken ct) =>
        _workspace.CreateAssignmentAsync(UserContext.UserId(User), request, ct);

    [HttpPut("teacher/{itemId:guid}/response")]
    [Authorize(Roles = "Teacher,Admin")]
    public Task<StudyItemDto> Respond(Guid itemId, TeacherStudyResponseRequest request, CancellationToken ct) =>
        _workspace.RespondAsync(UserContext.UserId(User), itemId, request, ct);
}
