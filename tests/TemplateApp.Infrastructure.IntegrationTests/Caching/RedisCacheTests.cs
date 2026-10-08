using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

using TemplateApp.Application.Common.Interfaces;
using TemplateApp.Infrastructure.IntegrationTests.Common;

using Testcontainers.Redis;

namespace TemplateApp.Infrastructure.IntegrationTests.Caching;

/// <summary>Two service providers sharing one Redis behave like two API instances behind a load balancer.</summary>
public sealed class RedisCacheTests : IAsyncLifetime
{
    private static readonly TimeSpan LocalExpiration = TimeSpan.FromSeconds(1);

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
        await CacheOf(_instanceA).GetOrCreateAsync(new TestCachedQuery(key), _ => ValueTask.FromResult("from A"));

        var seenByB = await CacheOf(_instanceB).GetOrCreateAsync(new TestCachedQuery(key), _ => ValueTask.FromResult("from B's source"));

        Assert.Equal("from A", seenByB);
    }

    [Fact]
    public async Task InvalidationOnOneInstance_ReachesAnotherWithinTheLocalExpiration()
    {
        var key = $"thing:{Guid.NewGuid():N}";
        await CacheOf(_instanceA).GetOrCreateAsync(new TestCachedQuery(key), _ => ValueTask.FromResult("v1"));
        await CacheOf(_instanceB).GetOrCreateAsync(new TestCachedQuery(key), _ => ValueTask.FromResult("unused"));

        await CacheOf(_instanceA).RemoveByTagAsync("things");
        await Task.Delay(LocalExpiration + TimeSpan.FromMilliseconds(500));

        var seenByB = await CacheOf(_instanceB).GetOrCreateAsync(new TestCachedQuery(key), _ => ValueTask.FromResult("v2"));

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
