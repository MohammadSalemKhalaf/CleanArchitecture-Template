using TemplateApp.Application.Common.Caching;
using TemplateApp.Application.Common.Messaging;

namespace TemplateApp.Application.Features.Todos.UpdateTodo;

public sealed record UpdateTodoCommand(Guid Id, string Title, string? Description, bool IsCompleted)
    : ICommand<TodoItemResponse>, IInvalidatesCache
{
    public IReadOnlyCollection<string> CacheTags => [TodoCache.Tag];
}
