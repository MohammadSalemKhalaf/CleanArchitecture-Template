using System.Reflection;

using FluentValidation;

using MediatR;

using TemplateApp.Application.Common.Messaging;

namespace TemplateApp.ArchitectureTests;

/// <summary>Vertical-slice conventions: each use case is one request, one handler and its validator, side by side.</summary>
public sealed class ConventionTests
{
    private static readonly Type[] ApplicationTypes = Layers.Application.GetTypes();

    public static TheoryData<Type> Requests() =>
        [.. ApplicationTypes.Where(type => type is { IsClass: true, IsAbstract: false } && ImplementsGeneric(type, typeof(IRequest<>)))];

    [Fact]
    public void ThereAreRequestsToCheck()
    {
        Assert.NotEmpty(Requests());
    }

    [Theory]
    [MemberData(nameof(Requests))]
    public void Request_IsExactlyOneOfCommandOrQuery_AndNamedAccordingly(Type request)
    {
        var isCommand = ImplementsGeneric(request, typeof(ICommand<>));
        var isQuery = ImplementsGeneric(request, typeof(IQuery<>));

        Assert.True(isCommand ^ isQuery, $"{request.Name} must implement exactly one of ICommand<T> or IQuery<T>.");
        Assert.EndsWith(isCommand ? "Command" : "Query", request.Name, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(Requests))]
    public void Request_HasOneSealedHandlerInTheSameUseCaseFolder(Type request)
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
    public void Validators_AreSealedAndLiveNextToTheirRequest()
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
