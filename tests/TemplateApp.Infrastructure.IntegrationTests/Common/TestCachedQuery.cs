using TemplateApp.Application.Common.Interfaces;

namespace TemplateApp.Infrastructure.IntegrationTests.Common;

public sealed record TestCachedQuery(string CacheKey, string Tag = "things") : ICachedQuery
{
    public string[] Tags => [Tag];

    public TimeSpan Expiration => TimeSpan.FromMinutes(5);
}
