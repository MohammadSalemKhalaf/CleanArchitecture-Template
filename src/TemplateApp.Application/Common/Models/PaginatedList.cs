using Microsoft.EntityFrameworkCore;

namespace TemplateApp.Application.Common.Models;

public sealed class PaginatedList<T>
{
    public const int MaxPageSize = 100;

    public int PageNumber { get; init; }

    public int PageSize { get; init; }

    public int TotalPages { get; init; }

    public int TotalCount { get; init; }

    public IReadOnlyCollection<T> Items { get; init; } = [];

    /// <summary>Executes <paramref name="query"/> as one count and one page query. The query must already be ordered.</summary>
    public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> query, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedList<T>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            Items = items,
        };
    }
}
