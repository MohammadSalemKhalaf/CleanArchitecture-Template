using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;

using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

using TemplateApp.Api.Http;
using TemplateApp.Api.IntegrationTests.Support;
using TemplateApp.Application.Common.Caching;

namespace TemplateApp.Api.IntegrationTests.Platform;

/// <summary>Cross-cutting HTTP behaviour: authentication, health, correlation and unexpected failures.</summary>
[Collection(ApiCollection.Name)]
public sealed class PlatformTests(ApiFactory factory)
{
    [Fact]
    public async Task ApiRoutes_RequireAToken()
    {
        var response = await factory.CreateClient().GetAsync("/api/v1/todos");

        await response.AssertProblemAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ApiRoutes_RejectTokensSignedWithAnotherKey()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            ApiFactory.CreateToken(signingKey: RandomNumberGenerator.GetBytes(32)));

        var response = await client.GetAsync("/api/v1/todos");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task HealthEndpoints_AreAnonymousAndHealthy(string path)
    {
        var response = await factory.CreateClient().GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Healthy", body.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task ReadinessCheck_IncludesTheDatabase()
    {
        using var body = JsonDocument.Parse(await factory.CreateClient().GetStringAsync("/health/ready"));

        Assert.Equal("Healthy", body.RootElement.GetProperty("checks").GetProperty("database").GetProperty("status").GetString());
    }

    [Fact]
    public async Task CorrelationId_FromTheCaller_IsEchoedAndAddedToProblems()
    {
        var client = factory.CreateAuthenticatedClient();
        client.DefaultRequestHeaders.Add(CorrelationIdMiddleware.HeaderName, "order-checkout-42");

        var response = await client.GetAsync($"/api/v1/todos/{Guid.NewGuid()}");

        Assert.Equal("order-checkout-42", response.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single());
        var problem = await response.AssertProblemAsync(HttpStatusCode.NotFound);
        Assert.Equal("order-checkout-42", problem.GetProperty("correlationId").GetString());
    }

    [Fact]
    public async Task CorrelationId_ThatIsMalformed_IsReplaced()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(CorrelationIdMiddleware.HeaderName, new string('x', 65));

        var response = await client.GetAsync("/health/live");

        var echoed = response.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single();
        Assert.NotEqual(new string('x', 65), echoed);
        Assert.False(string.IsNullOrWhiteSpace(echoed));
    }

    [Fact]
    public async Task UnexpectedException_Returns500WithoutInternalDetails()
    {
        const string secret = "connection to db-prod-01 failed for user sa";

        await using var failingFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddSingleton<ICacheService>(new ThrowingCache(secret))));
        var client = failingFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiFactory.CreateToken());

        var response = await client.GetAsync($"/api/v1/todos/{Guid.NewGuid()}");

        var problem = await response.AssertProblemAsync(HttpStatusCode.InternalServerError);
        Assert.Equal("An unexpected error occurred.", problem.GetProperty("title").GetString());
        Assert.DoesNotContain(secret, problem.GetRawText(), StringComparison.Ordinal);
        Assert.DoesNotContain(nameof(InvalidOperationException), problem.GetRawText(), StringComparison.Ordinal);
    }

    private sealed class ThrowingCache(string message) : ICacheService
    {
        public ValueTask<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, ValueTask<T>> factory, CacheEntrySettings settings, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException(message);

        public ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

        public ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }
}
