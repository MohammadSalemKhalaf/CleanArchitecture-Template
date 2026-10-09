namespace TemplateApp.Infrastructure.Settings;

public sealed class CachingOptions
{
    public const string SectionName = "Caching";

    /// <summary>
    /// Gets or sets the upper bound on how long an entry lives in a single instance's memory (L1). When Redis is enabled,
    /// this also bounds how long another instance can serve a value after it was invalidated elsewhere.
    /// </summary>
    public TimeSpan LocalExpiration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Gets or sets the prefix for every Redis key, so several applications can share one Redis.</summary>
    public string RedisInstanceName { get; set; } = "templateapp:";
}
