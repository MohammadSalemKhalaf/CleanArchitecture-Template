using Microsoft.EntityFrameworkCore;

using TemplateApp.Application.Common.Interfaces;
using TemplateApp.Application.Features.TodoItems.Dtos;
using TemplateApp.Application.Features.TodoItems.Mappers;
using TemplateApp.Domain.Common.Results;
using TemplateApp.Domain.TodoItems;

namespace TemplateApp.Application.Features.TodoItems.Queries.GetTodoItemById;

public sealed class GetTodoItemByIdQueryHandler(IAppDbContext context, ICacheService cache)
    : IQueryHandler<GetTodoItemByIdQuery, TodoItemDto>
{
    public async Task<Result<TodoItemDto>> Handle(GetTodoItemByIdQuery query, CancellationToken ct)
    {
        // A miss is cached too (as null). Writes invalidate the whole tag, so a later create cannot be hidden by it.
        var todoItem = await cache.GetOrCreateAsync(
            query,
            async token => await context.TodoItems
                .AsNoTracking()
                .Where(item => item.Id == query.TodoItemId)
                .Select(TodoItemMapper.Projection)
                .FirstOrDefaultAsync(token),
            ct);

        if (todoItem is null)
        {
            return TodoItemErrors.NotFound(query.TodoItemId);
        }

        return todoItem;
    }
}
