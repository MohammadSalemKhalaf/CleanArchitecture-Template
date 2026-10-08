using Microsoft.EntityFrameworkCore;

using TemplateApp.Application.Common.Caching;
using TemplateApp.Application.Common.Data;
using TemplateApp.Application.Common.Messaging;
using TemplateApp.Domain.Common.Results;

namespace TemplateApp.Application.Features.Todos.GetTodoById;

public sealed class GetTodoByIdQueryHandler(IAppDbContext db, ICacheService cache)
    : IQueryHandler<GetTodoByIdQuery, TodoItemResponse>
{
    public async Task<Result<TodoItemResponse>> Handle(GetTodoByIdQuery query, CancellationToken cancellationToken)
    {
        // A miss is cached too (as null). Writes invalidate the whole tag, so a later create cannot be hidden by it.
        var todo = await cache.GetOrCreateAsync(
            TodoCache.ById(query.Id),
            async ct => await db.TodoItems
                .AsNoTracking()
                .Where(item => item.Id == query.Id)
                .Select(TodoItemResponse.Projection)
                .FirstOrDefaultAsync(ct),
            TodoCache.Entry,
            cancellationToken);

        if (todo is null)
        {
            return TodoErrors.NotFound(query.Id);
        }

        return todo;
    }
}
