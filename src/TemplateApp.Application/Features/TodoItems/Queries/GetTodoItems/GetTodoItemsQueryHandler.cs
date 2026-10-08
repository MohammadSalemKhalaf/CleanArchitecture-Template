using Microsoft.EntityFrameworkCore;

using TemplateApp.Application.Common.Interfaces;
using TemplateApp.Application.Common.Models;
using TemplateApp.Application.Features.TodoItems.Dtos;
using TemplateApp.Application.Features.TodoItems.Mappers;
using TemplateApp.Domain.Common.Results;

namespace TemplateApp.Application.Features.TodoItems.Queries.GetTodoItems;

public sealed class GetTodoItemsQueryHandler(IAppDbContext context, ICacheService cache)
    : IQueryHandler<GetTodoItemsQuery, PaginatedList<TodoItemDto>>
{
    public async Task<Result<PaginatedList<TodoItemDto>>> Handle(GetTodoItemsQuery query, CancellationToken ct)
    {
        return await cache.GetOrCreateAsync(
            query,
            async token => await PaginatedList<TodoItemDto>.CreateAsync(
                context.TodoItems
                    .AsNoTracking()
                    .OrderByDescending(item => item.CreatedAtUtc)
                    .ThenBy(item => item.Id)
                    .Select(TodoItemMapper.Projection),
                query.Page,
                query.PageSize,
                token),
            ct);
    }
}
