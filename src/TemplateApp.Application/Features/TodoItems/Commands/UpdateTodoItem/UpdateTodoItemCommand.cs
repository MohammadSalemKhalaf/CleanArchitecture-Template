using TemplateApp.Application.Common.Interfaces;
using TemplateApp.Application.Features.TodoItems.Dtos;

namespace TemplateApp.Application.Features.TodoItems.Commands.UpdateTodoItem;

public sealed record UpdateTodoItemCommand(Guid TodoItemId, string Title, string? Description, bool IsCompleted)
    : ICommand<TodoItemDto>, IInvalidatesCache
{
    public string[] CacheTags => [TodoItemCache.Tag];
}
