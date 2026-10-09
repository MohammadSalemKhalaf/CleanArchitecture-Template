using System.Linq.Expressions;

using TemplateApp.Application.Features.TodoItems.Dtos;
using TemplateApp.Domain.TodoItems;

namespace TemplateApp.Application.Features.TodoItems.Mappers;

public static class TodoItemMapper
{
    /// <summary>Server-side projection: queries select straight into the DTO, so no entity is tracked or cached.</summary>
    public static readonly Expression<Func<TodoItem, TodoItemDto>> Projection = entity => new TodoItemDto(
        entity.Id,
        entity.Title,
        entity.Description,
        entity.IsCompleted,
        entity.CompletedAtUtc,
        entity.CreatedAtUtc,
        entity.LastModifiedAtUtc);

    private static readonly Func<TodoItem, TodoItemDto> Map = Projection.Compile();

    /// <summary>In-memory mapping for handlers that already hold the entity. Shares <see cref="Projection"/>, so both stay in sync.</summary>
    public static TodoItemDto ToDto(this TodoItem entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return Map(entity);
    }
}
