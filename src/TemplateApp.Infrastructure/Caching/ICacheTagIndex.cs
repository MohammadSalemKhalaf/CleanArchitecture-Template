namespace TemplateApp.Infrastructure.Caching;

/// <summary>
/// Remembers which keys were cached under which tag, so a tag invalidation can delete the entries from the shared tier.
/// HybridCache's own tag invalidation is per process: without this, other instances keep reading invalidated entries
/// from Redis until they expire.
/// </summary>
internal interface ICacheTagIndex
{
    ValueTask TrackAsync(string key, IReadOnlyCollection<string> tags, TimeSpan lifetime);

    /// <summary>Returns the keys recorded for <paramref name="tag"/> and forgets them, atomically.</summary>
    ValueTask<IReadOnlyCollection<string>> TakeKeysAsync(string tag);
}

/// <summary>Used when there is no shared tier: HybridCache's in-process tag invalidation is then sufficient.</summary>
internal sealed class NullCacheTagIndex : ICacheTagIndex
{
    public ValueTask TrackAsync(string key, IReadOnlyCollection<string> tags, TimeSpan lifetime) => ValueTask.CompletedTask;

    public ValueTask<IReadOnlyCollection<string>> TakeKeysAsync(string tag) => ValueTask.FromResult<IReadOnlyCollection<string>>([]);
}
