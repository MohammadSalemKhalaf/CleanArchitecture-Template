using System.Collections.Concurrent;

using TemplateApp.Application.Common.Interfaces;

namespace TemplateApp.Application.UnitTests.Common;

/// <summary>Minimal working cache that records how it was used, so tests can assert on keys and invalidations.</summary>
public sealed class RecordingCache : ICacheService
{
    private readonly ConcurrentDictionary<string, (object? Value, string[] Tags)> _entries = new();

    public List<string> RequestedKeys { get; } = [];

    public List<string> InvalidatedTags { get; } = [];

    public async ValueTask<T> GetOrCreateAsync<T>(
        ICachedQuery query,
        Func<CancellationToken, ValueTask<T>> factory,
        CancellationToken cancellationToken = default)
    {
        RequestedKeys.Add(query.CacheKey);

        if (_entries.TryGetValue(query.CacheKey, out var entry))
        {
            return (T)entry.Value!;
        }

        var value = await factory(cancellationToken);
        _entries[query.CacheKey] = (value, query.Tags);
        return value;
    }

    public ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _entries.TryRemove(key, out _);
        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        InvalidatedTags.Add(tag);

        foreach (var (key, entry) in _entries)
        {
            if (entry.Tags.Contains(tag))
            {
                _entries.TryRemove(key, out _);
            }
        }

        return ValueTask.CompletedTask;
    }
}
