namespace TemplateApp.Application.Features.TodoItems.Dtos;

/// <summary>Read model returned by every TodoItems use case. Immutable, so it can be cached safely.</summary>
public sealed record TodoItemDto(
    Guid TodoItemId,
    string Title,
    string? Description,
    bool IsCompleted,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset LastModifiedAtUtc);
