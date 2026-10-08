using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

using TemplateApp.Application.Common.Interfaces;
using TemplateApp.Infrastructure.IntegrationTests.Common;

namespace TemplateApp.Infrastructure.IntegrationTests.Caching;

/// <summary>Cache behaviour with the in-process tier only, and with an unreachable Redis.</summary>
public sealed class HybridCacheServiceTests : IAsyncDisposable
{
    private readonly ServiceProvider _services = InfrastructureHost.Build(new Dictionary<string, string?>
    {
        ["ConnectionStrings:Database"] = "Server=unused;Database=unused",
    });

    private ICacheService Cache => _services.GetRequiredService<ICacheService>();

    [Fact]
    public async Task SecondRead_IsServedFromCache()
    {
        var calls = 0;

        var first = await Cache.GetOrCreateAsync(new TestCachedQuery("thing:1"), _ => ValueTask.FromResult($"value-{++calls}"));
        var second = await Cache.GetOrCreateAsync(new TestCachedQuery("thing:1"), _ => ValueTask.FromResult($"value-{++calls}"));

        Assert.Equal("value-1", first);
        Assert.Equal("value-1", second);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task RemoveByTag_ForcesTheNextReadToMiss()
    {
        var calls = 0;
        await Cache.GetOrCreateAsync(new TestCachedQuery("thing:1"), _ => ValueTask.FromResult(++calls));
        await Cache.GetOrCreateAsync(new TestCachedQuery("thing:2"), _ => ValueTask.FromResult(++calls));

        await Cache.RemoveByTagAsync("things");

        Assert.Equal(3, await Cache.GetOrCreateAsync(new TestCachedQuery("thing:1"), _ => ValueTask.FromResult(++calls)));
        Assert.Equal(4, await Cache.GetOrCreateAsync(new TestCachedQuery("thing:2"), _ => ValueTask.FromResult(++calls)));
    }

    [Fact]
    public async Task RemoveByTag_LeavesOtherTagsCached()
    {
        var other = new TestCachedQuery("other:1", "others");
        var calls = 0;
        await Cache.GetOrCreateAsync(other, _ => ValueTask.FromResult(++calls));

        await Cache.RemoveByTagAsync("things");

        Assert.Equal(1, await Cache.GetOrCreateAsync(other, _ => ValueTask.FromResult(++calls)));
    }

    [Fact]
    public async Task Remove_EvictsASingleKey()
    {
        var calls = 0;
        await Cache.GetOrCreateAsync(new TestCachedQuery("thing:1"), _ => ValueTask.FromResult(++calls));

        await Cache.RemoveAsync("thing:1");

        Assert.Equal(2, await Cache.GetOrCreateAsync(new TestCachedQuery("thing:1"), _ => ValueTask.FromResult(++calls)));
    }

    [Fact]
    public async Task FactoryFailure_PropagatesOnceAndIsNotCached()
    {
        var calls = 0;

        ValueTask<int> FailingFactory(CancellationToken cancellationToken)
        {
            calls++;
            throw new InvalidOperationException("database down");
        }

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await Cache.GetOrCreateAsync(new TestCachedQuery("thing:1"), FailingFactory));

        Assert.Equal(1, calls); // Not retried by the fail-open path.
        Assert.Equal(7, await Cache.GetOrCreateAsync(new TestCachedQuery("thing:1"), _ => ValueTask.FromResult(7)));
    }

    [Fact]
    public async Task UnreachableRedis_DegradesToUncachedReadsInsteadOfFailing()
    {
        await using var services = InfrastructureHost.Build(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Database"] = "Server=unused;Database=unused",

            // Nothing listens on port 1.
            ["ConnectionStrings:Redis"] = "127.0.0.1:1,connectTimeout=500,syncTimeout=500,asyncTimeout=500",
        });
        var cache = services.GetRequiredService<ICacheService>();

        var value = await cache.GetOrCreateAsync(new TestCachedQuery("thing:1"), _ => ValueTask.FromResult("from source"));
        await cache.RemoveByTagAsync("things");

        Assert.Equal("from source", value);

        var health = await services.GetRequiredService<HealthCheckService>()
            .CheckHealthAsync(check => check.Name == "redis");
        Assert.Equal(HealthStatus.Degraded, health.Status);
    }

    public ValueTask DisposeAsync() => _services.DisposeAsync();
}
