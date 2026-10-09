using Microsoft.EntityFrameworkCore;

using TemplateApp.Application.Common.Exceptions;
using TemplateApp.Domain.TodoItems;

namespace TemplateApp.Application.Common.Interfaces;

/// <summary>
/// Unit of work used by handlers. Exposing <see cref="DbSet{TEntity}"/> is deliberate (as in the original project):
/// handlers compose queries directly instead of going through per-entity repositories.
/// </summary>
public interface IAppDbContext
{
    DbSet<TodoItem> TodoItems { get; }

    /// <exception cref="UniqueConstraintViolationException">A unique index rejected the change.</exception>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
