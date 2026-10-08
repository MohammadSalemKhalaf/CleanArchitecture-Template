namespace TemplateApp.Infrastructure.Settings;

/// <summary>Bound from the "Database" section. The connection string itself comes from <c>ConnectionStrings:Database</c>.</summary>
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string? ConnectionString { get; set; }

    /// <summary>Gets or sets a value indicating whether pending migrations are applied when the API starts. Development only; deployments use <c>--migrate</c>.</summary>
    public bool InitializeOnStartup { get; set; }

    /// <summary>Gets or sets a value indicating whether sample data is inserted on startup when the database is empty.</summary>
    public bool SeedSampleData { get; set; }
}
