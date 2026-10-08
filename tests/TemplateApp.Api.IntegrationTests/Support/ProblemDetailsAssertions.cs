using System.Net;
using System.Text.Json;

namespace TemplateApp.Api.IntegrationTests.Support;

public static class ProblemDetailsAssertions
{
    /// <summary>Asserts an RFC 9457 problem response and returns its body for further checks.</summary>
    public static async Task<JsonElement> AssertProblemAsync(this HttpResponseMessage response, HttpStatusCode expectedStatus)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var problem = document.RootElement.Clone();

        Assert.Equal((int)expectedStatus, problem.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrEmpty(problem.GetProperty("traceId").GetString()));

        return problem;
    }
}
