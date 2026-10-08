using System.Linq.Expressions;

using TemplateApp.Domain.Todos;

namespace TemplateApp.Application.Features.Todos;

public sealed record TodoItemResponse(
    Guid Id,
    string Title,
    string? Description,
    bool IsCompleted,
    DateTimeOffset? CompletedAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset LastModifiedAtUtc)
{
    /// <summary>Server-side projection: queries select straight into the DTO, so no entity is tracked or cached.</summary>
    public static readonly Expression<Func<TodoItem, TodoItemResponse>> Projection = todo => new TodoItemResponse(
        todo.Id,
        todo.Title,
        todo.Description,
        todo.IsCompleted,
        todo.CompletedAtUtc,
        todo.CreatedAtUtc,
        todo.LastModifiedAtUtc);

    private static readonly Func<TodoItem, TodoItemResponse> FromEntity = Projection.Compile();

    /// <summary>In-memory mapping for command handlers that already hold the entity. Shares the projection, so both stay in sync.</summary>
    public static TodoItemResponse From(TodoItem todo) => FromEntity(todo);
}
