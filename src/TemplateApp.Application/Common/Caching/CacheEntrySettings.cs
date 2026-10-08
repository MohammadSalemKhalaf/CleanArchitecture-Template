namespace TemplateApp.Application.Common.Caching;

/// <param name="Expiration">Absolute lifetime of the entry in every cache tier.</param>
/// <param name="Tags">Tags used for group invalidation, typically one per feature.</param>
public sealed record CacheEntrySettings(TimeSpan Expiration, IReadOnlyCollection<string> Tags);
