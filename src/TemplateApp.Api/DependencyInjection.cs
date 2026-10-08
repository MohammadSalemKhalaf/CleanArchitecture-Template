using System.Diagnostics;

using Microsoft.AspNetCore.Authentication.JwtBearer;

using Scalar.AspNetCore;

using Serilog;
using Serilog.Events;

using TemplateApp.Api.Endpoints.Todos;
using TemplateApp.Api.Health;
using TemplateApp.Api.Http;
using TemplateApp.Api.OpenApi;
using TemplateApp.Application.Common.Identity;

namespace TemplateApp.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
        {
            var httpContext = context.HttpContext;

            context.ProblemDetails.Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}";
            context.ProblemDetails.Extensions.TryAdd("traceId", Activity.Current?.Id ?? httpContext.TraceIdentifier);
            context.ProblemDetails.Extensions.TryAdd("correlationId", CorrelationIdMiddleware.GetCorrelationId(httpContext));
        });

        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        // Token validation is bound from the "Authentication:Schemes:Bearer" configuration section
        // (Authority/ValidAudiences for an identity provider, or SigningKeys for self-issued tokens).
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options => options.MapInboundClaims = false);
        services.AddAuthorization();

        services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

        return services;
    }

    public static WebApplication UseApi(this WebApplication app)
    {
        // Outermost, so the correlation id is on every log event, including the request summary below.
        app.UseMiddleware<CorrelationIdMiddleware>();

        app.UseSerilogRequestLogging(options =>
        {
            // Path only: query strings and bodies may carry personal data or secrets.
            options.GetLevel = (httpContext, _, exception) =>
                exception is not null || httpContext.Response.StatusCode >= 500 ? LogEventLevel.Error
                : httpContext.Request.Path.StartsWithSegments("/health") ? LogEventLevel.Verbose
                : LogEventLevel.Information;
            options.EnrichDiagnosticContext = (diagnostics, httpContext) =>
                diagnostics.Set("UserId", httpContext.User.FindFirst("sub")?.Value);
        });

        app.UseExceptionHandler();
        app.UseStatusCodePages();

        app.UseAuthentication();
        app.UseAuthorization();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
        }

        app.MapHealthEndpoints();
        app.MapTodoEndpoints();

        return app;
    }
}
