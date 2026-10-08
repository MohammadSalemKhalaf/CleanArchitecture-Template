using System.Collections.Concurrent;

using TemplateApp.Application.Common.Caching;

namespace TemplateApp.Application.UnitTests.TestDoubles;

/// <summary>Minimal working cache that records how it was used, so tests can assert on keys and invalidations.</summary>
public sealed class RecordingCache : ICacheService
{
    private readonly ConcurrentDictionary<string, (object? Value, IReadOnlyCollection<string> Tags)> _entries = new();

    public List<string> RequestedKeys { get; } = [];

    public List<string> InvalidatedTags { get; } = [];

    public async ValueTask<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T>> factory,
        CacheEntrySettings settings,
        CancellationToken cancellationToken = default)
    {
        RequestedKeys.Add(key);

        if (_entries.TryGetValue(key, out var entry))
        {
            return (T)entry.Value!;
        }

        var value = await factory(cancellationToken);
        _entries[key] = (value, settings.Tags);
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
