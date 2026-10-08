using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

using TemplateApp.Application.Common.Caching;
using TemplateApp.Infrastructure.IntegrationTests.Support;

using Testcontainers.Redis;

namespace TemplateApp.Infrastructure.IntegrationTests.Caching;

/// <summary>Two service providers sharing one Redis behave like two API instances behind a load balancer.</summary>
public sealed class RedisCacheTests : IAsyncLifetime
{
    private static readonly TimeSpan LocalExpiration = TimeSpan.FromSeconds(1);
    private static readonly CacheEntrySettings Entry = new(TimeSpan.FromMinutes(5), ["things"]);

    private readonly RedisContainer _redis = new RedisBuilder(InfrastructureHost.RedisImage).Build();
    private ServiceProvider _instanceA = null!;
    private ServiceProvider _instanceB = null!;

    public async Task InitializeAsync()
    {
        await _redis.StartAsync();

        _instanceA = BuildInstance();
        _instanceB = BuildInstance();
    }

    [Fact]
    public async Task ValueCachedByOneInstance_IsServedToAnotherFromRedis()
    {
        var key = $"thing:{Guid.NewGuid():N}";
        await CacheOf(_instanceA).GetOrCreateAsync(key, _ => ValueTask.FromResult("from A"), Entry);

        var seenByB = await CacheOf(_instanceB).GetOrCreateAsync(key, _ => ValueTask.FromResult("from B's source"), Entry);

        Assert.Equal("from A", seenByB);
    }

    [Fact]
    public async Task InvalidationOnOneInstance_ReachesAnotherWithinTheLocalExpiration()
    {
        var key = $"thing:{Guid.NewGuid():N}";
        await CacheOf(_instanceA).GetOrCreateAsync(key, _ => ValueTask.FromResult("v1"), Entry);
        await CacheOf(_instanceB).GetOrCreateAsync(key, _ => ValueTask.FromResult("unused"), Entry);

        await CacheOf(_instanceA).RemoveByTagAsync("things");
        await Task.Delay(LocalExpiration + TimeSpan.FromMilliseconds(500));

        var seenByB = await CacheOf(_instanceB).GetOrCreateAsync(key, _ => ValueTask.FromResult("v2"), Entry);

        Assert.Equal("v2", seenByB);
    }

    [Fact]
    public async Task HealthCheck_ReportsHealthyRedis()
    {
        var report = await _instanceA.GetRequiredService<HealthCheckService>().CheckHealthAsync(check => check.Name == "redis");

        Assert.Equal(HealthStatus.Healthy, report.Status);
    }

    public async Task DisposeAsync()
    {
        await _instanceA.DisposeAsync();
        await _instanceB.DisposeAsync();
        await _redis.DisposeAsync();
    }

    private static ICacheService CacheOf(ServiceProvider instance) => instance.GetRequiredService<ICacheService>();

    private ServiceProvider BuildInstance() => InfrastructureHost.Build(new Dictionary<string, string?>
    {
        ["ConnectionStrings:Database"] = "Server=unused;Database=unused",
        ["ConnectionStrings:Redis"] = _redis.GetConnectionString(),
        ["Caching:LocalExpiration"] = LocalExpiration.ToString(),
    });
}
