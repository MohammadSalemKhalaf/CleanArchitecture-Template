using TemplateApp.Application.Common.Caching;

namespace TemplateApp.Application.Features.Todos;

/// <summary>
/// Every cache key and tag used by the feature, in one place. Todo items are shared by all users, so keys carry no
/// user identifier. If a feature returns per-user or per-tenant data, that identifier must be part of the key.
/// </summary>
public static class TodoCache
{
    public const string Tag = "todos";

    public static readonly CacheEntrySettings Entry = new(TimeSpan.FromMinutes(5), [Tag]);

    public static string ById(Guid id) => $"todos:by-id:{id:N}";

    public static string Page(int page, int pageSize) => $"todos:page:{page}:size:{pageSize}";
}
