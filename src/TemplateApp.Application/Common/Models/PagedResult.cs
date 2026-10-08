using Microsoft.EntityFrameworkCore;

namespace TemplateApp.Application.Common.Models;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public const int MaxPageSize = 100;

    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage => Page < TotalPages;

    /// <summary>Executes <paramref name="query"/> as one count and one page query. The query must already be ordered.</summary>
    public static async Task<PagedResult<T>> CreateAsync(IQueryable<T> query, int page, int pageSize, CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(items, page, pageSize, totalCount);
    }
}
