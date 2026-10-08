namespace TemplateApp.Infrastructure;

/// <summary>Keys under <c>ConnectionStrings</c>. Set them with environment variables such as <c>ConnectionStrings__Database</c>.</summary>
public static class ConnectionStringNames
{
    public const string Database = "Database";

    /// <summary>Optional. When empty, caching is in-process only.</summary>
    public const string Redis = "Redis";
}
