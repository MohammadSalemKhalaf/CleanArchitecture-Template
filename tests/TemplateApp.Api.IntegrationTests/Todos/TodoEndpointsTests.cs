using System.Net;
using System.Net.Http.Json;
using System.Text;

using TemplateApp.Api.IntegrationTests.Support;
using TemplateApp.Application.Common.Models;
using TemplateApp.Application.Features.Todos;

namespace TemplateApp.Api.IntegrationTests.Todos;

[Collection(ApiCollection.Name)]
public sealed class TodoEndpointsTests(ApiFactory factory)
{
    private const string Route = "/api/v1/todos";

    private readonly HttpClient _client = factory.CreateAuthenticatedClient();

    [Fact]
    public async Task Create_Returns201WithLocation_AndTheItemCanBeFetched()
    {
        var title = UniqueTitle();

        var response = await _client.PostAsJsonAsync(Route, new { title, description = "details" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<TodoItemResponse>();
        Assert.Equal(title, created!.Title);
        Assert.Equal($"{Route}/{created.Id}", response.Headers.Location!.AbsolutePath, ignoreCase: true);

        var fetched = await _client.GetFromJsonAsync<TodoItemResponse>(response.Headers.Location);
        Assert.Equal(created, fetched);
    }

    [Fact]
    public async Task Create_WithInvalidBody_Returns400WithFieldErrors()
    {
        var response = await _client.PostAsJsonAsync(Route, new { title = string.Empty, description = new string('x', 2001) });

        var problem = await response.AssertProblemAsync(HttpStatusCode.BadRequest);
        var errors = problem.GetProperty("errors");
        Assert.True(errors.TryGetProperty("Title", out _));
        Assert.True(errors.TryGetProperty("Description", out _));
    }

    [Fact]
    public async Task Create_WithMalformedJson_Returns400()
    {
        var response = await _client.PostAsync(Route, new StringContent("{ \"title\": ", Encoding.UTF8, "application/json"));

        await response.AssertProblemAsync(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithDuplicateTitle_Returns409WithErrorCode()
    {
        var title = UniqueTitle();
        await _client.PostAsJsonAsync(Route, new { title });

        var response = await _client.PostAsJsonAsync(Route, new { title });

        var problem = await response.AssertProblemAsync(HttpStatusCode.Conflict);
        Assert.Equal("Todo.DuplicateTitle", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Get_UnknownId_Returns404WithErrorCode()
    {
        var response = await _client.GetAsync($"{Route}/{Guid.NewGuid()}");

        var problem = await response.AssertProblemAsync(HttpStatusCode.NotFound);
        Assert.Equal("Todo.NotFound", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task List_ReturnsRequestedPage()
    {
        for (var i = 0; i < 3; i++)
        {
            await _client.PostAsJsonAsync(Route, new { title = UniqueTitle() });
        }

        var page = await _client.GetFromJsonAsync<PagedResult<TodoItemResponse>>($"{Route}?page=1&pageSize=2");

        Assert.Equal(2, page!.Items.Count);
        Assert.Equal(1, page.Page);
        Assert.True(page.TotalCount >= 3);
    }

    [Theory]
    [InlineData("page=0&pageSize=10", "Page")]
    [InlineData("page=1&pageSize=101", "PageSize")]
    public async Task List_WithInvalidPaging_Returns400(string query, string invalidField)
    {
        var response = await _client.GetAsync($"{Route}?{query}");

        var problem = await response.AssertProblemAsync(HttpStatusCode.BadRequest);
        Assert.True(problem.GetProperty("errors").TryGetProperty(invalidField, out _));
    }

    [Fact]
    public async Task Update_IsVisibleOnTheNextRead_EvenAfterTheItemWasCached()
    {
        var created = await CreateAsync();
        await _client.GetFromJsonAsync<TodoItemResponse>($"{Route}/{created.Id}"); // Populate the cache.

        var response = await _client.PutAsJsonAsync($"{Route}/{created.Id}", new { title = created.Title + " (done)", isCompleted = true });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var reread = await _client.GetFromJsonAsync<TodoItemResponse>($"{Route}/{created.Id}");
        Assert.Equal(created.Title + " (done)", reread!.Title);
        Assert.True(reread.IsCompleted);
        Assert.NotNull(reread.CompletedAtUtc);
    }

    [Fact]
    public async Task Update_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"{Route}/{Guid.NewGuid()}", new { title = "Anything" });

        await response.AssertProblemAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_Returns204_ThenTheItemIsGone()
    {
        var created = await CreateAsync();
        await _client.GetAsync($"{Route}/{created.Id}"); // Populate the cache.

        var deleted = await _client.DeleteAsync($"{Route}/{created.Id}");
        var fetched = await _client.GetAsync($"{Route}/{created.Id}");
        var deletedAgain = await _client.DeleteAsync($"{Route}/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        await fetched.AssertProblemAsync(HttpStatusCode.NotFound);
        await deletedAgain.AssertProblemAsync(HttpStatusCode.NotFound);
    }

    private static string UniqueTitle() => $"Todo {Guid.NewGuid():N}";

    private async Task<TodoItemResponse> CreateAsync()
    {
        var response = await _client.PostAsJsonAsync(Route, new { title = UniqueTitle() });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TodoItemResponse>())!;
    }
}
