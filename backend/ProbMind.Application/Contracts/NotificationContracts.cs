using ProbMind.Domain.Enums;

namespace ProbMind.Application.Contracts;

public sealed record NotificationDto(
    Guid Id,
    NotificationType Type,
    string Title,
    string Body,
    bool IsRead,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt);
