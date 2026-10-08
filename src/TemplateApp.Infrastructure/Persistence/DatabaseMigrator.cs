using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace TemplateApp.Infrastructure.Persistence;

/// <summary>
/// Applies pending EF Core migrations. Invoked by the API's <c>--migrate</c> mode (Docker Compose and deployments)
/// and by integration tests. The API never migrates as a side effect of normal startup.
/// </summary>
public sealed class DatabaseMigrator(AppDbContext db, ILogger<DatabaseMigrator> logger)
{
    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();

        if (pending.Count == 0)
        {
            logger.LogInformation("Database is up to date");
            return;
        }

        logger.LogInformation("Applying {Count} migration(s): {Migrations}", pending.Count, pending);

        await db.Database.MigrateAsync(cancellationToken);

        logger.LogInformation("Database migrations applied");
    }
}
