using Microsoft.Extensions.Options;

using StackExchange.Redis;

using TemplateApp.Infrastructure.Settings;

namespace TemplateApp.Infrastructure.Caching;

/// <summary>Keeps one Redis set of cache keys per tag. Requires Redis 7.0+ (conditional EXPIRE).</summary>
internal sealed class RedisCacheTagIndex(IConnectionMultiplexer redis, IOptions<CachingOptions> options) : ICacheTagIndex
{
    // Read and delete in one step, so a key tracked concurrently is either returned now or kept for the next invalidation.
    private const string TakeScript = "local keys = redis.call('SMEMBERS', KEYS[1]) redis.call('DEL', KEYS[1]) return keys";

    private readonly string _prefix = options.Value.RedisInstanceName + "tag:";

    public async ValueTask TrackAsync(string key, IReadOnlyCollection<string> tags, TimeSpan lifetime)
    {
        var batch = redis.GetDatabase().CreateBatch();
        List<Task> pending = [];

        foreach (var tag in tags)
        {
            var setKey = SetKey(tag);
            pending.Add(batch.SetAddAsync(setKey, key));

            // The set must outlive the longest entry it indexes: set a TTL when there is none, and only ever extend it.
            pending.Add(batch.KeyExpireAsync(setKey, lifetime, ExpireWhen.HasNoExpiry));
            pending.Add(batch.KeyExpireAsync(setKey, lifetime, ExpireWhen.GreaterThanCurrentExpiry));
        }

        batch.Execute();
        await Task.WhenAll(pending);
    }

    public async ValueTask<IReadOnlyCollection<string>> TakeKeysAsync(string tag)
    {
        var result = await redis.GetDatabase().ScriptEvaluateAsync(TakeScript, [SetKey(tag)]);

        return ((RedisValue[]?)result ?? []).Select(value => value.ToString()).ToArray();
    }

    private RedisKey SetKey(string tag) => _prefix + tag;
}
