using ProbMind.Domain.Enums;

namespace ProbMind.Application.Contracts;

public sealed record CreateStudentStudyItemRequest(
    string Title,
    string Body,
    bool ShareWithTeacher);

public sealed record CreateTeacherAssignmentRequest(
    string Title,
    string Body);

public sealed record SaveStudyItemNoteRequest(string Body);

public sealed record TeacherStudyResponseRequest(
    string Response,
    bool DiscussInClass);

public sealed record StudyItemDto(
    Guid Id,
    StudyItemKind Kind,
    StudyItemVisibility Visibility,
    string Title,
    string Body,
    Guid? StudentId,
    string? StudentName,
    string? CreatedByName,
    string TeacherResponse,
    bool DiscussInClass,
    DateTimeOffset? TeacherRespondedAt,
    string StudentNote,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);


public sealed record StudyItemCommentDto(
    Guid ItemId,
    string ItemTitle,
    string ItemBody,
    StudyItemKind ItemKind,
    Guid StudentId,
    string StudentName,
    string Comment,
    DateTimeOffset CommentedAt);
