using TemplateApp.Application.Common.Interfaces;
using TemplateApp.Application.Features.TodoItems.Dtos;

namespace TemplateApp.Application.Features.TodoItems.Queries.GetTodoItemById;

public sealed record GetTodoItemByIdQuery(Guid TodoItemId) : ICachedQuery<TodoItemDto>
{
    public string CacheKey => $"todo-item:{TodoItemId:N}";

    public string[] Tags => [TodoItemCache.Tag];

    public TimeSpan Expiration => TodoItemCache.Expiration;
}
