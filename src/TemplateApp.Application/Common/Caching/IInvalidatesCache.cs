namespace TemplateApp.Application.Common.Caching;

/// <summary>
/// Implemented by commands whose success makes cached reads stale. <see cref="Behaviours.CacheInvalidationBehaviour{TRequest, TResponse}"/>
/// removes these tags after the handler returns a successful result, so handlers never invalidate by hand.
/// </summary>
public interface IInvalidatesCache
{
    IReadOnlyCollection<string> CacheTags { get; }
}
