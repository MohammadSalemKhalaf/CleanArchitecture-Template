using TemplateApp.Application.Common.Caching;
using TemplateApp.Application.Common.Messaging;
using TemplateApp.Domain.Common.Results;

namespace TemplateApp.Application.Features.Todos.DeleteTodo;

public sealed record DeleteTodoCommand(Guid Id) : ICommand<Deleted>, IInvalidatesCache
{
    public IReadOnlyCollection<string> CacheTags => [TodoCache.Tag];
}
