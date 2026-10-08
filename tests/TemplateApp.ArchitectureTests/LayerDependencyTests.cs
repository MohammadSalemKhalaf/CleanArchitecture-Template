using System.Reflection;

using NetArchTest.Rules;

namespace TemplateApp.ArchitectureTests;

/// <summary>The dependency rule: source code dependencies point inwards only.</summary>
public sealed class LayerDependencyTests
{
    [Fact]
    public void Domain_DependsOnNoOtherLayerOrFramework()
    {
        AssertNoDependency(
            Types.InAssembly(Layers.Domain),
            Layers.ApplicationNamespace,
            Layers.InfrastructureNamespace,
            Layers.ApiNamespace,
            "MediatR",
            "FluentValidation",
            "Microsoft.EntityFrameworkCore",
            "Microsoft.AspNetCore",
            "Microsoft.Extensions");

        AssertReferencesOnlyFramework(Layers.Domain);
    }

    [Fact]
    public void Application_DoesNotDependOnInfrastructureApiOrProviders()
    {
        AssertNoDependency(
            Types.InAssembly(Layers.Application),
            Layers.InfrastructureNamespace,
            Layers.ApiNamespace,
            "Microsoft.AspNetCore",
            "Microsoft.EntityFrameworkCore.SqlServer",
            "Microsoft.Data.SqlClient",
            "Microsoft.Extensions.Caching",
            "StackExchange.Redis",
            "Serilog");
    }

    [Fact]
    public void Infrastructure_DoesNotDependOnTheApi()
    {
        AssertNoDependency(
            Types.InAssembly(Layers.Infrastructure),
            Layers.ApiNamespace,
            "Microsoft.AspNetCore");
    }

    [Fact]
    public void Endpoints_GoThroughUseCasesInsteadOfPersistence()
    {
        AssertNoDependency(
            Types.InAssembly(Layers.Api).That().ResideInNamespace($"{Layers.ApiNamespace}.Endpoints"),
            Layers.InfrastructureNamespace,
            "TemplateApp.Application.Common.Data",
            "Microsoft.EntityFrameworkCore");
    }

    private static void AssertNoDependency(PredicateList types, params string[] forbidden) =>
        AssertNoDependency(types.ShouldNot().HaveDependencyOnAny(forbidden).GetResult());

    private static void AssertNoDependency(Types types, params string[] forbidden) =>
        AssertNoDependency(types.ShouldNot().HaveDependencyOnAny(forbidden).GetResult());

    private static void AssertNoDependency(TestResult result) =>
        Assert.True(result.IsSuccessful, $"Forbidden dependency in: {string.Join(", ", result.FailingTypeNames ?? [])}");

    // Catches a project or package reference even when no type uses it yet.
    private static void AssertReferencesOnlyFramework(Assembly assembly)
    {
        var references = assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name!)
            .Where(name => !name.StartsWith("System", StringComparison.Ordinal) && name != "netstandard")
            .ToList();

        Assert.Empty(references);
    }
}
