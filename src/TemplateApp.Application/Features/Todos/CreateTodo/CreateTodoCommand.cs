using TemplateApp.Application.Common.Caching;
using TemplateApp.Application.Common.Messaging;

namespace TemplateApp.Application.Features.Todos.CreateTodo;

public sealed record CreateTodoCommand(string Title, string? Description) : ICommand<TodoItemResponse>, IInvalidatesCache
{
    public IReadOnlyCollection<string> CacheTags => [TodoCache.Tag];
}
