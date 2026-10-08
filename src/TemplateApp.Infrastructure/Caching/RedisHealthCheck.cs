using Microsoft.Extensions.Diagnostics.HealthChecks;

using StackExchange.Redis;

namespace TemplateApp.Infrastructure.Caching;

/// <summary>
/// Reports Redis reachability. Registered with <see cref="HealthStatus.Degraded"/> as failure status:
/// the cache fails open, so the API keeps serving requests without it.
/// </summary>
internal sealed class RedisHealthCheck(IConnectionMultiplexer redis) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await redis.GetDatabase().PingAsync();
            return HealthCheckResult.Healthy();
        }
        catch (Exception exception) when (exception is RedisException or TimeoutException)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "Redis is unreachable.", exception);
        }
    }
}
