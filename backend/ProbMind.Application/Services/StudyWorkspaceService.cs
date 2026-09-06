using ProbMind.Application.Abstractions.Clock;
using ProbMind.Application.Abstractions.Persistence;
using ProbMind.Application.Contracts;
using ProbMind.Domain.Entities;
using ProbMind.Domain.Enums;

namespace ProbMind.Application.Services;

public sealed class StudyWorkspaceService : IStudyWorkspaceService
{
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;

    public StudyWorkspaceService(IUnitOfWork uow, IClock clock)
    {
        _uow = uow;
        _clock = clock;
    }

    public async Task<IReadOnlyList<StudyItemDto>> ListForStudentAsync(
        Guid studentId,
        CancellationToken ct = default)
    {
        await EnsureStudentAsync(studentId, ct);
        var items = await _uow.StudyItems.WhereAsync(
            x => x.StudentId == studentId || x.Visibility == StudyItemVisibility.AllStudents,
            ct);
        var notes = await _uow.StudyItemNotes.WhereAsync(x => x.StudentId == studentId, ct);
        var users = (await _uow.Users.ListAsync(ct)).ToDictionary(x => x.Id);
        var noteByItem = notes.ToDictionary(x => x.StudyItemId, x => x.Body);

        return items
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => Map(x, users, noteByItem.GetValueOrDefault(x.Id, string.Empty)))
            .ToArray();
    }

    public async Task<StudyItemDto> CreateStudentItemAsync(
        Guid studentId,
        CreateStudentStudyItemRequest request,
        CancellationToken ct = default)
    {
        var student = await EnsureStudentAsync(studentId, ct);
        var title = NormalizeTitle(request.Title);
        var body = NormalizeBody(request.Body, 10000, "Текст материала не может быть пустым.");
        var item = new StudyItem
        {
            CreatedByUserId = studentId,
            StudentId = studentId,
            Kind = StudyItemKind.StudentMaterial,
            Visibility = request.ShareWithTeacher
                ? StudyItemVisibility.SharedWithTeacher
                : StudyItemVisibility.Private,
            Title = title,
            Body = body,
            TeacherResponse = string.Empty
        };

        await _uow.StudyItems.AddAsync(item, ct);
        await _uow.SaveChangesAsync(ct);
        return Map(item, new Dictionary<Guid, User> { [student.Id] = student }, string.Empty);
    }

    public async Task<StudyItemDto> SaveStudentNoteAsync(
        Guid studentId,
        Guid itemId,
        SaveStudyItemNoteRequest request,
        CancellationToken ct = default)
    {
        await EnsureStudentAsync(studentId, ct);
        var item = await _uow.StudyItems.GetByIdAsync(itemId, ct)
            ?? throw new KeyNotFoundException("Материал не найден.");
        if (item.StudentId != studentId && item.Visibility != StudyItemVisibility.AllStudents)
            throw new UnauthorizedAccessException("Материал недоступен студенту.");

        var body = (request.Body ?? string.Empty).Trim();
        if (body.Length > 5000)
            throw new InvalidOperationException("Заметка не может быть длиннее 5000 символов.");

        var existing = (await _uow.StudyItemNotes.WhereAsync(
            x => x.StudyItemId == itemId && x.StudentId == studentId,
            ct)).SingleOrDefault();

        if (existing is null)
        {
            existing = new StudyItemNote
            {
                StudyItemId = itemId,
                StudentId = studentId,
                Body = body
            };
            await _uow.StudyItemNotes.AddAsync(existing, ct);
        }
        else
        {
            existing.Body = body;
            existing.Touch();
            _uow.StudyItemNotes.Update(existing);
        }

        await _uow.SaveChangesAsync(ct);
        var users = (await _uow.Users.ListAsync(ct)).ToDictionary(x => x.Id);
        return Map(item, users, body);
    }

    public async Task<IReadOnlyList<StudyItemDto>> ListForTeacherAsync(CancellationToken ct = default)
    {
        var items = await _uow.StudyItems.WhereAsync(
            x => x.Visibility == StudyItemVisibility.SharedWithTeacher ||
                 x.Visibility == StudyItemVisibility.AllStudents,
            ct);
        var users = (await _uow.Users.ListAsync(ct)).ToDictionary(x => x.Id);

        return items
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => Map(x, users, string.Empty))
            .ToArray();
    }

    public async Task<IReadOnlyList<StudyItemCommentDto>> ListCommentsForTeacherAsync(
        CancellationToken ct = default)
    {
        var visibleItems = await _uow.StudyItems.WhereAsync(
            x => x.Visibility == StudyItemVisibility.SharedWithTeacher ||
                 x.Visibility == StudyItemVisibility.AllStudents,
            ct);
        var itemById = visibleItems.ToDictionary(x => x.Id);
        if (itemById.Count == 0)
            return Array.Empty<StudyItemCommentDto>();

        var notes = await _uow.StudyItemNotes.ListAsync(ct);
        var users = (await _uow.Users.ListAsync(ct)).ToDictionary(x => x.Id);

        return notes
            .Where(note => itemById.ContainsKey(note.StudyItemId) && !string.IsNullOrWhiteSpace(note.Body))
            .Select(note =>
            {
                var item = itemById[note.StudyItemId];
                var studentName = users.TryGetValue(note.StudentId, out var student)
                    ? student.DisplayName
                    : "Студент";
                return new StudyItemCommentDto(
                    item.Id,
                    item.Title,
                    item.Body,
                    item.Kind,
                    note.StudentId,
                    studentName,
                    note.Body,
                    note.UpdatedAt);
            })
            .OrderByDescending(x => x.CommentedAt)
            .ToArray();
    }

    public async Task<StudyItemDto> CreateAssignmentAsync(
        Guid teacherId,
        CreateTeacherAssignmentRequest request,
        CancellationToken ct = default)
    {
        var teacher = await EnsureTeacherAsync(teacherId, ct);
        var item = new StudyItem
        {
            CreatedByUserId = teacherId,
            StudentId = null,
            Kind = StudyItemKind.TeacherAssignment,
            Visibility = StudyItemVisibility.AllStudents,
            Title = NormalizeTitle(request.Title),
            Body = NormalizeBody(request.Body, 10000, "Текст задания не может быть пустым."),
            TeacherResponse = string.Empty
        };

        await _uow.StudyItems.AddAsync(item, ct);
        await _uow.SaveChangesAsync(ct);
        return Map(item, new Dictionary<Guid, User> { [teacher.Id] = teacher }, string.Empty);
    }

    public async Task<StudyItemDto> RespondAsync(
        Guid teacherId,
        Guid itemId,
        TeacherStudyResponseRequest request,
        CancellationToken ct = default)
    {
        await EnsureTeacherAsync(teacherId, ct);
        var item = await _uow.StudyItems.GetByIdAsync(itemId, ct)
            ?? throw new KeyNotFoundException("Материал не найден.");
        if (item.Visibility != StudyItemVisibility.SharedWithTeacher || !item.StudentId.HasValue)
            throw new InvalidOperationException("Ответ преподавателя доступен только для материалов, отправленных студентом.");

        var response = (request.Response ?? string.Empty).Trim();
        if (response.Length > 5000)
            throw new InvalidOperationException("Ответ не может быть длиннее 5000 символов.");

        item.TeacherResponse = response;
        item.DiscussInClass = request.DiscussInClass;
        item.TeacherRespondedAt = response.Length > 0 || request.DiscussInClass ? _clock.UtcNow : null;
        item.Touch();
        _uow.StudyItems.Update(item);
        await _uow.SaveChangesAsync(ct);

        var users = (await _uow.Users.ListAsync(ct)).ToDictionary(x => x.Id);
        return Map(item, users, string.Empty);
    }

    private async Task<User> EnsureStudentAsync(Guid userId, CancellationToken ct)
    {
        var user = await _uow.Users.GetByIdAsync(userId, ct)
            ?? throw new KeyNotFoundException("Пользователь не найден.");
        if (user.Role != UserRole.Student)
            throw new UnauthorizedAccessException("Доступно только студенту.");
        return user;
    }

    private async Task<User> EnsureTeacherAsync(Guid userId, CancellationToken ct)
    {
        var user = await _uow.Users.GetByIdAsync(userId, ct)
            ?? throw new KeyNotFoundException("Пользователь не найден.");
        if (user.Role is not (UserRole.Teacher or UserRole.Admin))
            throw new UnauthorizedAccessException("Доступно только преподавателю или администратору.");
        return user;
    }

    private static string NormalizeTitle(string? value)
    {
        var title = (value ?? string.Empty).Trim();
        if (title.Length == 0)
            return "Без названия";
        if (title.Length > 300)
            throw new InvalidOperationException("Название не может быть длиннее 300 символов.");
        return title;
    }

    private static string NormalizeBody(string? value, int maxLength, string emptyMessage)
    {
        var body = (value ?? string.Empty).Trim();
        if (body.Length == 0)
            throw new InvalidOperationException(emptyMessage);
        if (body.Length > maxLength)
            throw new InvalidOperationException($"Текст не может быть длиннее {maxLength} символов.");
        return body;
    }

    private static StudyItemDto Map(
        StudyItem item,
        IReadOnlyDictionary<Guid, User> users,
        string studentNote)
    {
        var studentName = item.StudentId.HasValue && users.TryGetValue(item.StudentId.Value, out var student)
            ? student.DisplayName
            : null;
        var createdByName = users.TryGetValue(item.CreatedByUserId, out var creator)
            ? creator.DisplayName
            : null;

        return new StudyItemDto(
            item.Id,
            item.Kind,
            item.Visibility,
            item.Title,
            item.Body,
            item.StudentId,
            studentName,
            createdByName,
            item.TeacherResponse,
            item.DiscussInClass,
            item.TeacherRespondedAt,
            studentNote,
            item.CreatedAt,
            item.UpdatedAt);
    }
}
