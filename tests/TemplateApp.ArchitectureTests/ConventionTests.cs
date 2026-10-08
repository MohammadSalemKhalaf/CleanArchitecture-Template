using System.Reflection;

using FluentValidation;

using MediatR;

using TemplateApp.Api.Controllers;
using TemplateApp.Application.Common.Interfaces;

namespace TemplateApp.ArchitectureTests;

/// <summary>
/// Feature conventions from the original project: Features/&lt;Feature&gt;/{Commands|Queries}/&lt;UseCase&gt;/ holds one request,
/// its handler and its validator; Dtos/ and Mappers/ sit next to them; controllers only translate HTTP.
/// </summary>
public sealed class ConventionTests
{
    private static readonly Type[] ApplicationTypes = Layers.Application.GetTypes();

    public static TheoryData<Type> Requests() =>
        [.. ApplicationTypes.Where(type => type is { IsClass: true, IsAbstract: false } && ImplementsGeneric(type, typeof(IRequest<>)))];

    [Fact]
    public void ThereAreRequestsToCheck()
    {
        // Guards against scanning the wrong assembly, which would make every other convention test pass vacuously.
        Assert.NotEmpty(Requests());
    }

    [Theory]
    [MemberData(nameof(Requests))]
    public void Request_IsACommandOrAQuery_InItsOwnUseCaseFolder(Type request)
    {
        var isCommand = ImplementsGeneric(request, typeof(ICommand<>));
        var isQuery = ImplementsGeneric(request, typeof(IQuery<>));
        Assert.True(isCommand ^ isQuery, $"{request.Name} must implement exactly one of ICommand<T> or IQuery<T>.");

        var suffix = isCommand ? "Command" : "Query";
        var folder = isCommand ? "Commands" : "Queries";
        var useCase = request.Name[..^suffix.Length];

        Assert.EndsWith(suffix, request.Name, StringComparison.Ordinal);
        Assert.Matches($@"^{Layers.ApplicationNamespace}\.Features\.\w+\.{folder}\.{useCase}$", request.Namespace);
    }

    [Theory]
    [MemberData(nameof(Requests))]
    public void Request_HasOneSealedHandlerNextToIt(Type request)
    {
        var handlers = ApplicationTypes
            .Where(type => type.GetInterfaces().Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>) &&
                i.GenericTypeArguments[0] == request))
            .ToList();

        var handler = Assert.Single(handlers);
        Assert.True(handler.IsSealed, $"{handler.Name} should be sealed.");
        Assert.Equal(request.Namespace, handler.Namespace);
        Assert.Equal($"{request.Name}Handler", handler.Name);
    }

    [Fact]
    public void Validators_AreSealedNamedAfterAndPlacedNextToTheirRequest()
    {
        var validators = ApplicationTypes
            .Where(type => type is { IsAbstract: false } && ImplementsGeneric(type, typeof(IValidator<>)))
            .ToList();

        Assert.NotEmpty(validators);

        foreach (var validator in validators)
        {
            var validated = validator.GetInterfaces()
                .Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>))
                .GenericTypeArguments[0];

            Assert.True(validator.IsSealed, $"{validator.Name} should be sealed.");
            Assert.Equal(validated.Namespace, validator.Namespace);
            Assert.Equal($"{validated.Name}Validator", validator.Name);
        }
    }

    [Fact]
    public void DtosAndMappers_LiveInTheirFeatureFolders()
    {
        var misplaced = ApplicationTypes
            .Where(type => type.Namespace?.StartsWith($"{Layers.ApplicationNamespace}.Features.", StringComparison.Ordinal) == true)
            .Where(type => (type.Name.EndsWith("Dto", StringComparison.Ordinal) && !type.Namespace!.EndsWith(".Dtos", StringComparison.Ordinal))
                        || (type.Name.EndsWith("Mapper", StringComparison.Ordinal) && !type.Namespace!.EndsWith(".Mappers", StringComparison.Ordinal)))
            .Select(type => type.FullName)
            .ToList();

        Assert.Empty(misplaced);
    }

    [Fact]
    public void CacheInterfaces_AreUsedOnTheRightKindOfRequest()
    {
        var cachedNonQueries = ApplicationTypes
            .Where(type => typeof(ICachedQuery).IsAssignableFrom(type) && type.IsClass && !ImplementsGeneric(type, typeof(IQuery<>)));
        var invalidatingNonCommands = ApplicationTypes
            .Where(type => typeof(IInvalidatesCache).IsAssignableFrom(type) && type.IsClass && !ImplementsGeneric(type, typeof(ICommand<>)));

        Assert.Empty(cachedNonQueries);
        Assert.Empty(invalidatingNonCommands);
    }

    [Fact]
    public void Controllers_AreSealedAndDeriveFromApiController()
    {
        var controllers = Layers.Api.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && type.Name.EndsWith("Controller", StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(controllers);

        foreach (var controller in controllers)
        {
            Assert.True(controller.IsSealed, $"{controller.Name} should be sealed.");
            Assert.True(controller.IsSubclassOf(typeof(ApiController)), $"{controller.Name} should derive from ApiController.");
            Assert.Equal($"{Layers.ApiNamespace}.Controllers", controller.Namespace);
        }
    }

    [Fact]
    public void DomainEntities_DoNotExposePublicSetters()
    {
        var publicSetters = Layers.Domain.GetTypes()
            .Where(type => type.IsClass && !IsRecord(type))
            .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(property => property.SetMethod?.IsPublic == true)
            .Select(property => $"{property.DeclaringType!.Name}.{property.Name}")
            .ToList();

        Assert.Empty(publicSetters);
    }

    private static bool ImplementsGeneric(Type type, Type openGeneric) =>
        type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == openGeneric);

    private static bool IsRecord(Type type) => type.GetMethod("<Clone>$") is not null;
}
