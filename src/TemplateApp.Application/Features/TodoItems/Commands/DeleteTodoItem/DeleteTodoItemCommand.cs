using TemplateApp.Application.Common.Interfaces;
using TemplateApp.Domain.Common.Results;

namespace TemplateApp.Application.Features.TodoItems.Commands.DeleteTodoItem;

public sealed record DeleteTodoItemCommand(Guid TodoItemId) : ICommand<Deleted>, IInvalidatesCache
{
    public string[] CacheTags => [TodoItemCache.Tag];
}
