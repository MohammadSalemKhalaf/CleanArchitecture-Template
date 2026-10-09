using FluentValidation;

using TemplateApp.Application.Common.Behaviours;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Three layers declare a DependencyInjection class in this namespace (as in the original), so never refer to it by name.
        var assembly = typeof(ValidationBehaviour<,>).Assembly;

        services.AddValidatorsFromAssembly(assembly);

        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(assembly);

            // Registration order is execution order, outermost first.
            config.AddOpenBehavior(typeof(PerformanceBehaviour<,>));
            config.AddOpenBehavior(typeof(ValidationBehaviour<,>));
            config.AddOpenBehavior(typeof(CacheInvalidationBehaviour<,>));
        });

        return services;
    }
}
