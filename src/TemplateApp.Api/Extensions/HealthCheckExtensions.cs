using System.Text.Json;

using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

using TemplateApp.Infrastructure;

namespace TemplateApp.Api.Extensions;

public static class HealthCheckExtensions
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        // Liveness: the process is up. No dependency checks, so a database outage does not restart the container.
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = WriteResponse,
        }).AllowAnonymous();

        // Readiness: dependencies are reachable. Database down = 503; Redis down = Degraded (still 200).
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(HealthCheckTags.Ready),
            ResponseWriter = WriteResponse,
        }).AllowAnonymous();

        return app;
    }

    // Exception details are deliberately left out of the response.
    private static Task WriteResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var body = new
        {
            status = report.Status.ToString(),
            durationMs = (long)report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new
                {
                    status = entry.Value.Status.ToString(),
                    durationMs = (long)entry.Value.Duration.TotalMilliseconds,
                    description = entry.Value.Description,
                }),
        };

        return JsonSerializer.SerializeAsync(context.Response.Body, body, cancellationToken: context.RequestAborted);
    }
}
