using Scalar.AspNetCore;

using Serilog;

using TemplateApp.Api;
using TemplateApp.Api.Extensions;
using TemplateApp.Infrastructure.Data;

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
        .AddPresentation()
        .AddApplication()
        .AddInfrastructure(builder.Configuration);

    var app = builder.Build();

    if (args.Contains(MigrationMode.Argument))
    {
        return await MigrationMode.RunAsync(app);
    }

    // Optional services, switched by configuration (see README > Configuration).
    if (app.Configuration.GetValue<bool>("OpenApi:Enabled"))
    {
        app.MapOpenApi().WithDocumentPerVersion();
        app.MapScalarApiReference(options =>
        {
            foreach (var description in app.DescribeApiVersions())
            {
                options.AddDocument(description.GroupName, description.GroupName);
            }
        });
    }

    await app.Services.InitialiseDatabaseAsync();

    app.UseCoreMiddlewares();

    app.MapControllers();
    app.MapHealthEndpoints();

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
