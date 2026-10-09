using TemplateApp.Application.Common.Interfaces;
using TemplateApp.Application.Features.TodoItems.Dtos;

namespace TemplateApp.Application.Features.TodoItems.Commands.CreateTodoItem;

public sealed record CreateTodoItemCommand(string Title, string? Description) : ICommand<TodoItemDto>, IInvalidatesCache
{
    public string[] CacheTags => [TodoItemCache.Tag];
}
