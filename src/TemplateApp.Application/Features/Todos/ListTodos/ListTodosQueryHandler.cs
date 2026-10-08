using Microsoft.EntityFrameworkCore;

using TemplateApp.Application.Common.Caching;
using TemplateApp.Application.Common.Data;
using TemplateApp.Application.Common.Messaging;
using TemplateApp.Application.Common.Models;
using TemplateApp.Domain.Common.Results;

namespace TemplateApp.Application.Features.Todos.ListTodos;

public sealed class ListTodosQueryHandler(IAppDbContext db, ICacheService cache)
    : IQueryHandler<ListTodosQuery, PagedResult<TodoItemResponse>>
{
    public async Task<Result<PagedResult<TodoItemResponse>>> Handle(ListTodosQuery query, CancellationToken cancellationToken)
    {
        return await cache.GetOrCreateAsync(
            TodoCache.Page(query.Page, query.PageSize),
            async ct => await PagedResult<TodoItemResponse>.CreateAsync(
                db.TodoItems
                    .AsNoTracking()
                    .OrderByDescending(todo => todo.CreatedAtUtc)
                    .ThenBy(todo => todo.Id)
                    .Select(TodoItemResponse.Projection),
                query.Page,
                query.PageSize,
                ct),
            TodoCache.Entry,
            cancellationToken);
    }
}
