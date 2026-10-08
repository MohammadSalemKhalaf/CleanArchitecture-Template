using Microsoft.EntityFrameworkCore;

using TemplateApp.Application.Common.Data;
using TemplateApp.Domain.Todos;

namespace TemplateApp.Application.UnitTests.TestDoubles;

/// <summary>Isolated, in-process <see cref="IAppDbContext"/>. It does not enforce unique indexes or SQL semantics.</summary>
public sealed class InMemoryAppDbContext(DbContextOptions<InMemoryAppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();

    public static InMemoryAppDbContext Create() => new(
        new DbContextOptionsBuilder<InMemoryAppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
