using System.Diagnostics;

using MediatR;

using Microsoft.Extensions.Logging;

namespace TemplateApp.Application.Common.Behaviours;

/// <summary>
/// Warns about slow requests. Logs the request type only: request payloads can carry personal data or credentials.
/// </summary>
public sealed class PerformanceBehaviour<TRequest, TResponse>(ILogger<PerformanceBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly TimeSpan Threshold = TimeSpan.FromMilliseconds(500);

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();

        var response = await next(cancellationToken);

        var elapsed = Stopwatch.GetElapsedTime(started);

        if (elapsed > Threshold)
        {
            logger.LogWarning(
                "Slow request {RequestName} took {ElapsedMilliseconds} ms",
                typeof(TRequest).Name,
                (long)elapsed.TotalMilliseconds);
        }

        return response;
    }
}
