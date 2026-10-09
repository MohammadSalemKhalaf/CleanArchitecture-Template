using TemplateApp.Application.Common.Interfaces;

namespace TemplateApp.Infrastructure.IntegrationTests.Common;

public sealed class TestUser : IUser
{
    public string? Id { get; set; }
}
