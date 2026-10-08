using System.Diagnostics;

using Serilog.Context;

namespace TemplateApp.Api.Infrastructure;

/// <summary>
/// Accepts a caller-supplied <c>X-Correlation-Id</c> (when well-formed) or falls back to the W3C trace id, echoes it in the
/// response and attaches it to every log event of the request as <c>CorrelationId</c>.
/// </summary>
public sealed class RequestLogContextMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";

    private const int MaxLength = 64;
    private static readonly object ItemKey = new();

    public static string? GetCorrelationId(HttpContext context) => context.Items[ItemKey] as string;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = Resolve(context);

        context.Items[ItemKey] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }

    private static string Resolve(HttpContext context)
    {
        var incoming = context.Request.Headers[HeaderName].ToString();

        // Reject anything that could be used for log forging or to bloat log storage.
        if (incoming.Length is > 0 and <= MaxLength && incoming.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.'))
        {
            return incoming;
        }

        return Activity.Current?.TraceId.ToHexString() ?? context.TraceIdentifier;
    }
}
