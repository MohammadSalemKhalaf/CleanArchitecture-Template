using System.Reflection;

using TemplateApp.Api;
using TemplateApp.Application.Common.Interfaces;
using TemplateApp.Contracts.Common;
using TemplateApp.Domain.Common;
using TemplateApp.Infrastructure.Data;

namespace TemplateApp.ArchitectureTests;

internal static class Layers
{
    public const string DomainNamespace = "TemplateApp.Domain";
    public const string ApplicationNamespace = "TemplateApp.Application";
    public const string ContractsNamespace = "TemplateApp.Contracts";
    public const string InfrastructureNamespace = "TemplateApp.Infrastructure";
    public const string ApiNamespace = "TemplateApp.Api";

    public static readonly Assembly Domain = typeof(Entity).Assembly;
    public static readonly Assembly Application = typeof(IAppDbContext).Assembly;
    public static readonly Assembly Contracts = typeof(PageRequest).Assembly;
    public static readonly Assembly Infrastructure = typeof(AppDbContext).Assembly;
    public static readonly Assembly Api = typeof(IAssemblyMarker).Assembly;
}
