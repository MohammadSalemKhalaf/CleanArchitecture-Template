namespace TemplateApp.Application.Common.Interfaces;

/// <summary>
/// Read-through cache for query results. Implementations must fail open: when the cache backend is unavailable,
/// <see cref="GetOrCreateAsync{T}"/> still returns the factory's value and invalidation does not throw.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Returns the cached value for <paramref name="query"/>, or runs <paramref name="factory"/>, caches and returns its result.
    /// Only cache immutable DTOs. Never cache tracked EF entities.
    /// </summary>
    ValueTask<T> GetOrCreateAsync<T>(
        ICachedQuery query,
        Func<CancellationToken, ValueTask<T>> factory,
        CancellationToken cancellationToken = default);

    ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Invalidates every entry that was stored with <paramref name="tag"/>.</summary>
    ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default);
}
