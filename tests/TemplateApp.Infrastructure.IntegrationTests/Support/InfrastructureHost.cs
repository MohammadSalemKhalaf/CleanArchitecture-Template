using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using TemplateApp.Application.Common.Identity;

namespace TemplateApp.Infrastructure.IntegrationTests.Support;

/// <summary>Builds the Infrastructure layer exactly as the API registers it, with test-controlled user and clock.</summary>
public static class InfrastructureHost
{
    public const string SqlServerImage = "mcr.microsoft.com/mssql/server:2022-CU27-ubuntu-22.04";
    public const string RedisImage = "redis:7.4-alpine";

    public static ServiceProvider Build(
        IDictionary<string, string?> settings,
        ICurrentUser? currentUser = null,
        TimeProvider? timeProvider = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IConfiguration>(configuration)
            .AddSingleton(currentUser ?? new TestCurrentUser())
            .AddSingleton(timeProvider ?? TimeProvider.System);

        services.AddInfrastructure(configuration);

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }
}
