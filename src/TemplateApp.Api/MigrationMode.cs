using TemplateApp.Infrastructure;
using TemplateApp.Infrastructure.Data;

namespace TemplateApp.Api;

/// <summary>
/// <c>dotnet TemplateApp.Api.dll --migrate</c> applies pending migrations and exits without serving traffic.
/// Docker Compose and the deployment pipeline run it as a one-off step before starting the API, so schema changes
/// happen once per release instead of racing between replicas at startup.
/// </summary>
internal static class MigrationMode
{
    public const string Argument = "--migrate";

    public static async Task<int> RunAsync(WebApplication app)
    {
        var logger = app.Services.GetRequiredService<ILogger<WebApplication>>();

        if (string.IsNullOrWhiteSpace(app.Configuration.GetConnectionString(ConnectionStringNames.Database)))
        {
            logger.LogCritical("ConnectionStrings:{Name} is not configured", ConnectionStringNames.Database);
            return 1;
        }

        await using var scope = app.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>().MigrateAsync();

        return 0;
    }
}
