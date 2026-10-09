using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using TemplateApp.Domain.TodoItems;
using TemplateApp.Infrastructure.Settings;

namespace TemplateApp.Infrastructure.Data;

/// <summary>Applies EF Core migrations and seeds sample data. Used by startup initialisation, <c>--migrate</c> and tests.</summary>
public sealed class ApplicationDbContextInitialiser(AppDbContext context, ILogger<ApplicationDbContextInitialiser> logger)
{
    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        var pending = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();

        if (pending.Count == 0)
        {
            logger.LogInformation("Database is up to date");
            return;
        }

        logger.LogInformation("Applying {Count} migration(s): {Migrations}", pending.Count, pending);

        await context.Database.MigrateAsync(cancellationToken);

        logger.LogInformation("Database migrations applied");
    }

    /// <summary>Inserts sample rows into an empty database. Part of the removable TodoItems example.</summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await context.TodoItems.AnyAsync(cancellationToken))
        {
            return;
        }

        context.TodoItems.AddRange(
            TodoItem.Create("Read RULES-COMPOSE.md", "Rules for every layer, linked from one index.").Value,
            TodoItem.Create("Replace the TodoItems example", "Add your entities, configurations and use cases.").Value);

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Sample data seeded");
    }
}

public static class InitialiserExtensions
{
    /// <summary>
    /// Development convenience, as in the original project: applies migrations and seeds sample data on startup when
    /// <c>Database:InitializeOnStartup</c> / <c>Database:SeedSampleData</c> are enabled. Deployments use <c>--migrate</c> instead.
    /// </summary>
    public static async Task InitialiseDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var options = services.GetRequiredService<IOptions<DatabaseOptions>>().Value;

        if (!options.InitializeOnStartup && !options.SeedSampleData)
        {
            return;
        }

        await using var scope = services.CreateAsyncScope();
        var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();

        if (options.InitializeOnStartup)
        {
            await initialiser.MigrateAsync(cancellationToken);
        }

        if (options.SeedSampleData)
        {
            await initialiser.SeedAsync(cancellationToken);
        }
    }
}
