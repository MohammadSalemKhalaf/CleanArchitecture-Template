using FluentValidation;

using Microsoft.Extensions.DependencyInjection;

using TemplateApp.Application.Common.Behaviours;

namespace TemplateApp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

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
