using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

using TemplateApp.Application.Common.Data;
using TemplateApp.Domain.Todos;

namespace TemplateApp.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    // SQL Server error numbers for duplicate keys in a unique index (2601) or unique constraint (2627).
    private const int DuplicateKeyInUniqueIndex = 2601;
    private const int DuplicateKeyInUniqueConstraint = 2627;

    public DbSet<TodoItem> TodoItems => Set<TodoItem>();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqlException { Number: DuplicateKeyInUniqueIndex or DuplicateKeyInUniqueConstraint })
        {
            throw new UniqueConstraintViolationException("A unique index rejected the change.", exception);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
