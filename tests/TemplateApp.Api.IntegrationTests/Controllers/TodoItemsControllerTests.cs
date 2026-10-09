using System.Net;
using System.Net.Http.Json;
using System.Text;

using TemplateApp.Api.IntegrationTests.Common;
using TemplateApp.Application.Common.Models;
using TemplateApp.Application.Features.TodoItems.Dtos;
using TemplateApp.Tests.Common.TodoItems;

namespace TemplateApp.Api.IntegrationTests.Controllers;

[Collection(WebAppFactoryCollection.Name)]
public sealed class TodoItemsControllerTests(WebAppFactory factory)
{
    private const string Route = "/api/v1/todo-items";

    private readonly HttpClient _client = factory.CreateAuthenticatedClient();

    [Fact]
    public async Task Create_Returns201WithLocation_AndTheItemCanBeFetched()
    {
        var request = TodoItemFactory.CreateTodoItemRequest(description: "details");

        var response = await _client.PostAsJsonAsync(Route, request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<TodoItemDto>();
        Assert.Equal(request.Title, created!.Title);
        Assert.EndsWith($"/todo-items/{created.TodoItemId}", response.Headers.Location!.AbsolutePath, StringComparison.OrdinalIgnoreCase);

        var fetched = await _client.GetFromJsonAsync<TodoItemDto>(response.Headers.Location);
        Assert.Equal(created, fetched);
    }

    [Fact]
    public async Task Create_WithInvalidBody_Returns400WithFieldErrors()
    {
        var request = TodoItemFactory.CreateTodoItemRequest(title: string.Empty, description: new string('x', 2001));

        var response = await _client.PostAsJsonAsync(Route, request);

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
        var request = TodoItemFactory.CreateTodoItemRequest();
        await _client.PostAsJsonAsync(Route, request);

        var response = await _client.PostAsJsonAsync(Route, request);

        var problem = await response.AssertProblemAsync(HttpStatusCode.Conflict);
        Assert.Equal("TodoItem.DuplicateTitle", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task GetById_UnknownId_Returns404WithErrorCode()
    {
        var response = await _client.GetAsync($"{Route}/{Guid.NewGuid()}");

        var problem = await response.AssertProblemAsync(HttpStatusCode.NotFound);
        Assert.Equal("TodoItem.NotFound", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Get_ReturnsRequestedPage()
    {
        for (var i = 0; i < 3; i++)
        {
            await _client.PostAsJsonAsync(Route, TodoItemFactory.CreateTodoItemRequest());
        }

        var page = await _client.GetFromJsonAsync<PaginatedList<TodoItemDto>>($"{Route}?page=1&pageSize=2");

        Assert.Equal(2, page!.Items.Count);
        Assert.Equal(1, page.PageNumber);
        Assert.True(page.TotalCount >= 3);
    }

    [Theory]
    [InlineData("page=0&pageSize=10", "Page")]
    [InlineData("page=1&pageSize=101", "PageSize")]
    public async Task Get_WithInvalidPaging_Returns400(string query, string invalidField)
    {
        var response = await _client.GetAsync($"{Route}?{query}");

        var problem = await response.AssertProblemAsync(HttpStatusCode.BadRequest);
        Assert.True(problem.GetProperty("errors").TryGetProperty(invalidField, out _));
    }

    [Fact]
    public async Task Update_IsVisibleOnTheNextRead_EvenAfterTheItemWasCached()
    {
        var created = await CreateAsync();
        await _client.GetFromJsonAsync<TodoItemDto>($"{Route}/{created.TodoItemId}"); // Populate the cache.

        var response = await _client.PutAsJsonAsync(
            $"{Route}/{created.TodoItemId}",
            TodoItemFactory.UpdateTodoItemRequest(title: created.Title + " (done)", isCompleted: true));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var reread = await _client.GetFromJsonAsync<TodoItemDto>($"{Route}/{created.TodoItemId}");
        Assert.Equal(created.Title + " (done)", reread!.Title);
        Assert.True(reread.IsCompleted);
        Assert.NotNull(reread.CompletedAtUtc);
    }

    [Fact]
    public async Task Update_UnknownId_Returns404()
    {
        var response = await _client.PutAsJsonAsync($"{Route}/{Guid.NewGuid()}", TodoItemFactory.UpdateTodoItemRequest());

        await response.AssertProblemAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_Returns204_ThenTheItemIsGone()
    {
        var created = await CreateAsync();
        await _client.GetAsync($"{Route}/{created.TodoItemId}"); // Populate the cache.

        var deleted = await _client.DeleteAsync($"{Route}/{created.TodoItemId}");
        var fetched = await _client.GetAsync($"{Route}/{created.TodoItemId}");
        var deletedAgain = await _client.DeleteAsync($"{Route}/{created.TodoItemId}");

        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        await fetched.AssertProblemAsync(HttpStatusCode.NotFound);
        await deletedAgain.AssertProblemAsync(HttpStatusCode.NotFound);
    }

    private async Task<TodoItemDto> CreateAsync()
    {
        var response = await _client.PostAsJsonAsync(Route, TodoItemFactory.CreateTodoItemRequest());
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TodoItemDto>())!;
    }
}
