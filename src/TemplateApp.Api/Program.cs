using Serilog;

using TemplateApp.Api;
using TemplateApp.Application;
using TemplateApp.Infrastructure;

// Console-only logger for failures before the host's configuration is loaded.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddSerilog((services, logger) => logger
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services));

    builder.Services
        .AddApplication()
        .AddInfrastructure(builder.Configuration)
        .AddApi();

    var app = builder.Build();

    if (args.Contains(MigrationMode.Argument))
    {
        return await MigrationMode.RunAsync(app);
    }

    app.UseApi();

    await app.RunAsync();
    return 0;
}
catch (Exception exception) when (exception is not HostAbortedException)
{
    // HostAbortedException is how EF Core tooling stops the host after reading its services; it is not a failure.
    Log.Fatal(exception, "Host terminated unexpectedly");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

/// <summary>Entry point marker for <c>WebApplicationFactory</c> in the API integration tests.</summary>
public partial class Program;
