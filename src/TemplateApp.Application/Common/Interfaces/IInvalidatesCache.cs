namespace TemplateApp.Application.Common.Interfaces;

/// <summary>
/// Implemented by commands whose success makes cached reads stale. <c>CacheInvalidationBehaviour</c> removes these tags
/// after the handler returns a successful result, so handlers never invalidate by hand.
/// </summary>
public interface IInvalidatesCache
{
    string[] CacheTags { get; }
}
