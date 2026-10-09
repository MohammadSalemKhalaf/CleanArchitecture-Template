namespace TemplateApp.Application.Common.Interfaces;

/// <summary>
/// A query whose result may be cached. As in the original project, the query itself declares its cache key, tags and
/// lifetime; the handler reads through <see cref="ICacheService"/> with it.
/// </summary>
public interface ICachedQuery
{
    /// <summary>Gets the unique key. Include every parameter that changes the result, and the user or tenant id for user-scoped data.</summary>
    string CacheKey { get; }

    /// <summary>Gets the tags used for group invalidation, typically one per feature.</summary>
    string[] Tags { get; }

    TimeSpan Expiration { get; }
}

public interface ICachedQuery<TResponse> : IQuery<TResponse>, ICachedQuery;
