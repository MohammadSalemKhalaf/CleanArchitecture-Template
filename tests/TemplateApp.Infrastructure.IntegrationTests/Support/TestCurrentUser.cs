using TemplateApp.Application.Common.Identity;

namespace TemplateApp.Infrastructure.IntegrationTests.Support;

public sealed class TestCurrentUser : ICurrentUser
{
    public string? UserId { get; set; }
}
