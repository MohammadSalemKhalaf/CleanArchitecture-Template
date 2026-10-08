using TemplateApp.Application.Common.Interfaces;
using TemplateApp.Application.Common.Models;
using TemplateApp.Application.Features.TodoItems.Dtos;

namespace TemplateApp.Application.Features.TodoItems.Queries.GetTodoItems;

public sealed record GetTodoItemsQuery(int Page = 1, int PageSize = 10) : ICachedQuery<PaginatedList<TodoItemDto>>
{
    public string CacheKey => $"todo-items:page:{Page}:size:{PageSize}";

    public string[] Tags => [TodoItemCache.Tag];

    public TimeSpan Expiration => TodoItemCache.Expiration;
}
