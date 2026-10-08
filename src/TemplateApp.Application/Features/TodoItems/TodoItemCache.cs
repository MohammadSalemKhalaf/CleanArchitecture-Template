namespace TemplateApp.Application.Features.TodoItems;

/// <summary>
/// Cache settings shared by the feature's queries (which declare their keys) and commands (which invalidate the tag).
/// Todo items are shared by all users, so keys carry no user id. Per-user or per-tenant data must include that id in the key.
/// </summary>
public static class TodoItemCache
{
    public const string Tag = "todo-item";

    public static readonly TimeSpan Expiration = TimeSpan.FromMinutes(5);
}
