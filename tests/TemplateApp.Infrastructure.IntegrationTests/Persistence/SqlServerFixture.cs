using Microsoft.Extensions.DependencyInjection;

using TemplateApp.Infrastructure.IntegrationTests.Support;
using TemplateApp.Infrastructure.Persistence;

using Testcontainers.MsSql;

namespace TemplateApp.Infrastructure.IntegrationTests.Persistence;

/// <summary>One disposable SQL Server per test collection, migrated with the production migrator.</summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder(InfrastructureHost.SqlServerImage).Build();

    public TestCurrentUser CurrentUser { get; } = new();

    public TestClock Clock { get; } = new();

    public ServiceProvider Services { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        Services = InfrastructureHost.Build(
            new Dictionary<string, string?> { ["ConnectionStrings:Database"] = _container.GetConnectionString() },
            CurrentUser,
            Clock);

        await using var scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DatabaseMigrator>().MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await Services.DisposeAsync();
        await _container.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
    public const string Name = "SqlServer";
}
