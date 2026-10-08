using System.Net.Http.Headers;
using System.Security.Cryptography;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

using TemplateApp.Infrastructure.Persistence;

using Testcontainers.MsSql;

namespace TemplateApp.Api.IntegrationTests.Support;

/// <summary>
/// Runs the real API against a disposable SQL Server. Authentication is configured through the same
/// <c>Authentication:Schemes:Bearer</c> settings a deployment uses, with a signing key generated per test run.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string Issuer = "templateapp-tests";
    public const string Audience = "templateapp-api";

    private static readonly byte[] SigningKey = RandomNumberGenerator.GetBytes(32);

    private readonly MsSqlContainer _database = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-CU27-ubuntu-22.04").Build();

    public static string CreateToken(string userId = "test-user", byte[]? signingKey = null) =>
        new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            Expires = DateTime.UtcNow.AddMinutes(10),
            Claims = new Dictionary<string, object> { ["sub"] = userId },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(signingKey ?? SigningKey),
                SecurityAlgorithms.HmacSha256),
        });

    public HttpClient CreateAuthenticatedClient(string userId = "test-user")
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(userId));
        return client;
    }

    public async Task InitializeAsync()
    {
        await _database.StartAsync();

        await using var scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DatabaseMigrator>().MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        await _database.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting("ConnectionStrings:Database", _database.GetConnectionString());
        builder.UseSetting("Authentication:Schemes:Bearer:ValidIssuer", Issuer);
        builder.UseSetting("Authentication:Schemes:Bearer:ValidAudiences:0", Audience);
        builder.UseSetting("Authentication:Schemes:Bearer:SigningKeys:0:Issuer", Issuer);
        builder.UseSetting("Authentication:Schemes:Bearer:SigningKeys:0:Value", Convert.ToBase64String(SigningKey));
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "Api";
}
