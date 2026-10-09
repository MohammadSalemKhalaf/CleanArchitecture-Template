using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using TemplateApp.Application.Common.Interfaces;
using TemplateApp.Infrastructure.Settings;

namespace TemplateApp.Infrastructure.Caching;

/// <summary>
/// <see cref="ICacheService"/> over <see cref="HybridCache"/>: an in-process L1 plus Redis as L2 when configured.
/// Cache failures are logged and bypassed (fail open). Failures of the factory itself always propagate.
/// </summary>
internal sealed class HybridCacheService(
    HybridCache cache,
    ICacheTagIndex tagIndex,
    IOptions<CachingOptions> options,
    ILogger<HybridCacheService> logger) : ICacheService
{
    private readonly TimeSpan _localExpiration = options.Value.LocalExpiration;

    public async ValueTask<T> GetOrCreateAsync<T>(
        ICachedQuery query,
        Func<CancellationToken, ValueTask<T>> factory,
        CancellationToken cancellationToken = default)
    {
        var key = query.CacheKey;
        var entryOptions = new HybridCacheEntryOptions
        {
            Expiration = query.Expiration,
            LocalCacheExpiration = query.Expiration < _localExpiration ? query.Expiration : _localExpiration,
        };

        var factoryFailed = false;

        async ValueTask<T> TrackedFactory(CancellationToken ct)
        {
            T value;

            try
            {
                value = await factory(ct);
            }
            catch
            {
                factoryFailed = true;
                throw;
            }

            await TrackAsync(query);
            return value;
        }

        try
        {
            return await cache.GetOrCreateAsync(key, TrackedFactory, entryOptions, query.Tags, cancellationToken);
        }
        catch (Exception exception) when (!factoryFailed && exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Cache read failed for {CacheKey}; serving uncached data", key);
            return await factory(cancellationToken);
        }
    }

    public async ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await cache.RemoveAsync(key, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Cache removal failed for {CacheKey}", key);
        }
    }

    public async ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        // 1. Delete the tag's entries from both tiers, so other instances cannot reload them from Redis.
        try
        {
            var keys = await tagIndex.TakeKeysAsync(tag);

            if (keys.Count > 0)
            {
                await cache.RemoveAsync(keys, cancellationToken);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Shared cache invalidation failed for tag {CacheTag}", tag);
        }

        // 2. Invalidate this instance's in-memory entries, including any written since the keys were read.
        try
        {
            await cache.RemoveByTagAsync(tag, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Cache invalidation failed for tag {CacheTag}", tag);
        }
    }

    private async ValueTask TrackAsync(ICachedQuery query)
    {
        try
        {
            await tagIndex.TrackAsync(query.CacheKey, query.Tags, query.Expiration);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not index {CacheKey} under its tags", query.CacheKey);
        }
    }
}
