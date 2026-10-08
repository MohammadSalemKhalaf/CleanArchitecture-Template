using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

using StackExchange.Redis;

using TemplateApp.Application.Common.Caching;
using TemplateApp.Application.Common.Data;
using TemplateApp.Infrastructure.Caching;
using TemplateApp.Infrastructure.Persistence;
using TemplateApp.Infrastructure.Persistence.Interceptors;

namespace TemplateApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);

        services
            .AddPersistence(configuration)
            .AddCaching(configuration);

        return services;
    }

    private static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        // Fail at startup, not on the first query. Not evaluated by EF tooling or --migrate, which never start the host.
        services.AddOptions<DatabaseOptions>()
            .Configure<IConfiguration>((options, config) => options.ConnectionString = config.GetConnectionString(ConnectionStringNames.Database))
            .Validate(options => !string.IsNullOrWhiteSpace(options.ConnectionString), $"ConnectionStrings:{ConnectionStringNames.Database} is not configured.")
            .ValidateOnStart();

        services.AddScoped<AuditableEntityInterceptor>();

        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            // Read lazily so tests and tooling can supply the connection string after registration.
            var connectionString = serviceProvider.GetRequiredService<IConfiguration>()
                .GetConnectionString(ConnectionStringNames.Database);

            options
                .UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure())
                .AddInterceptors(serviceProvider.GetRequiredService<AuditableEntityInterceptor>());
        });

        services.AddScoped<IAppDbContext>(serviceProvider => serviceProvider.GetRequiredService<AppDbContext>());
        services.AddScoped<DatabaseMigrator>();

        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>("database", tags: [HealthCheckTags.Ready]);

        return services;
    }

    private static IServiceCollection AddCaching(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CachingOptions>(configuration.GetSection(CachingOptions.SectionName));

        services.AddHybridCache();
        services.AddSingleton<ICacheService, HybridCacheService>();

        var redisConnectionString = configuration.GetConnectionString(ConnectionStringNames.Redis);

        if (string.IsNullOrWhiteSpace(redisConnectionString))
        {
            // No Redis: HybridCache runs with its in-process tier only.
            services.AddSingleton<ICacheTagIndex, NullCacheTagIndex>();
            return services;
        }

        services.AddSingleton<ICacheTagIndex, RedisCacheTagIndex>();

        var redisOptions = ConfigurationOptions.Parse(redisConnectionString);

        // Start even if Redis is down and keep reconnecting in the background; the cache fails open meanwhile.
        redisOptions.AbortOnConnectFail = false;

        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisOptions));

        // HybridCache picks up the registered IDistributedCache as its L2 tier.
        services.AddStackExchangeRedisCache(_ => { });
        services.AddOptions<RedisCacheOptions>()
            .Configure<IServiceProvider, IOptions<CachingOptions>>((options, serviceProvider, caching) =>
            {
                options.InstanceName = caching.Value.RedisInstanceName;
                options.ConnectionMultiplexerFactory = () =>
                    Task.FromResult(serviceProvider.GetRequiredService<IConnectionMultiplexer>());
            });

        services.AddHealthChecks()
            .AddCheck<RedisHealthCheck>("redis", failureStatus: HealthStatus.Degraded, tags: [HealthCheckTags.Ready]);

        return services;
    }
}
