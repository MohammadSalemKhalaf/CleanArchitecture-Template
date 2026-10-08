using Microsoft.EntityFrameworkCore;

using TemplateApp.Domain.Todos;

namespace TemplateApp.Application.Common.Data;

/// <summary>
/// Unit of work used by handlers. Exposing <see cref="DbSet{TEntity}"/> is a deliberate trade-off:
/// handlers compose queries directly instead of going through per-entity repositories.
/// </summary>
public interface IAppDbContext
{
    DbSet<TodoItem> TodoItems { get; }

    /// <exception cref="UniqueConstraintViolationException">A unique index rejected the change.</exception>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
