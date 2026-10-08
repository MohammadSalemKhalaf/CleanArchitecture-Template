using System.Reflection;

using TemplateApp.Api.Http;
using TemplateApp.Application.Common.Messaging;
using TemplateApp.Domain.Common;
using TemplateApp.Infrastructure.Persistence;

namespace TemplateApp.ArchitectureTests;

internal static class Layers
{
    public const string DomainNamespace = "TemplateApp.Domain";
    public const string ApplicationNamespace = "TemplateApp.Application";
    public const string InfrastructureNamespace = "TemplateApp.Infrastructure";
    public const string ApiNamespace = "TemplateApp.Api";

    public static readonly Assembly Domain = typeof(Entity).Assembly;
    public static readonly Assembly Application = typeof(ICommand<>).Assembly;
    public static readonly Assembly Infrastructure = typeof(AppDbContext).Assembly;
    public static readonly Assembly Api = typeof(ResultHttpExtensions).Assembly;
}
