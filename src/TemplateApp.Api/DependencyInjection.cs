using System.Diagnostics;
using System.Text.Json.Serialization;

using Asp.Versioning;

using Microsoft.AspNetCore.Authentication.JwtBearer;

using Serilog;
using Serilog.Events;

using TemplateApp.Api.Infrastructure;
using TemplateApp.Api.OpenApi.Transformers;
using TemplateApp.Api.Services;
using TemplateApp.Application.Common.Interfaces;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddCustomProblemDetails()
                .AddCustomApiVersioning()
                .AddExceptionHandling()
                .AddControllerWithJsonConfiguration()
                .AddIdentityInfrastructure();

        return services;
    }

    public static IServiceCollection AddCustomProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
        {
            var httpContext = context.HttpContext;

            context.ProblemDetails.Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}";
            context.ProblemDetails.Extensions.TryAdd("traceId", Activity.Current?.Id ?? httpContext.TraceIdentifier);
            context.ProblemDetails.Extensions.TryAdd("correlationId", RequestLogContextMiddleware.GetCorrelationId(httpContext));
        });

        return services;
    }

    public static IServiceCollection AddCustomApiVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1);
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        }).AddMvc()
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        })

        // One OpenAPI document per API version (/openapi/v1.json). Title and description come from the project file.
        .AddOpenApi(options => options.Document.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

        return services;
    }

    public static IServiceCollection AddExceptionHandling(this IServiceCollection services)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        return services;
    }

    public static IServiceCollection AddControllerWithJsonConfiguration(this IServiceCollection services)
    {
        services.AddControllers(options =>
            {
                // Business validation lives in the application validators; do not duplicate it with implicit [Required].
                options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
            })
            .AddJsonOptions(options => options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);

        return services;
    }

    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IUser, CurrentUser>();

        // Token validation is bound from "Authentication:Schemes:Bearer"
        // (Authority/ValidAudiences for an identity provider, or SigningKeys for self-issued tokens).
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options => options.MapInboundClaims = false);
        services.AddAuthorization();

        return services;
    }

    public static WebApplication UseCoreMiddlewares(this WebApplication app)
    {
        // 1. Correlation id first, so it is on every log event, including the request summary below.
        app.UseMiddleware<RequestLogContextMiddleware>();

        // 2. One structured event per request. Path only: query strings and bodies may carry personal data or secrets.
        app.UseSerilogRequestLogging(options =>
        {
            options.GetLevel = (httpContext, _, exception) =>
                exception is not null || httpContext.Response.StatusCode >= 500 ? LogEventLevel.Error
                : httpContext.Request.Path.StartsWithSegments("/health") ? LogEventLevel.Verbose
                : LogEventLevel.Information;
            options.EnrichDiagnosticContext = (diagnostics, httpContext) =>
                diagnostics.Set("UserId", httpContext.User.FindFirst("sub")?.Value);
        });

        // 3. Exceptions become problem details; empty error responses (401, 404 routes) get a problem body.
        app.UseExceptionHandler();
        app.UseStatusCodePages();

        // 4. Authentication before authorization.
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }
}
