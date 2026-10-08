using Microsoft.Extensions.Logging.Abstractions;

using TemplateApp.Application.Features.Todos;
using TemplateApp.Application.Features.Todos.DeleteTodo;
using TemplateApp.Application.Features.Todos.GetTodoById;
using TemplateApp.Application.Features.Todos.ListTodos;
using TemplateApp.Application.UnitTests.TestDoubles;
using TemplateApp.Domain.Todos;

namespace TemplateApp.Application.UnitTests.Features.Todos;

public sealed class QueryAndDeleteHandlerTests : IDisposable
{
    private readonly InMemoryAppDbContext _db = InMemoryAppDbContext.Create();
    private readonly RecordingCache _cache = new();

    [Fact]
    public async Task GetById_ReadsThroughTheItemCacheKey()
    {
        var todo = await AddAsync("Cached");
        var handler = new GetTodoByIdQueryHandler(_db, _cache);

        var result = await handler.Handle(new GetTodoByIdQuery(todo.Id), CancellationToken.None);

        Assert.Equal("Cached", result.Value.Title);
        Assert.Equal([TodoCache.ById(todo.Id)], _cache.RequestedKeys);
    }

    [Fact]
    public async Task GetById_UnknownId_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        var handler = new GetTodoByIdQueryHandler(_db, _cache);

        var result = await handler.Handle(new GetTodoByIdQuery(id), CancellationToken.None);

        Assert.Equal(TodoErrors.NotFound(id), result.FirstError);
    }

    [Fact]
    public async Task List_ReturnsTheRequestedPageAndTotals()
    {
        for (var i = 1; i <= 5; i++)
        {
            await AddAsync($"Item {i}");
        }

        var handler = new ListTodosQueryHandler(_db, _cache);

        var result = await handler.Handle(new ListTodosQuery(Page: 2, PageSize: 2), CancellationToken.None);

        var page = result.Value;
        Assert.Equal(2, page.Items.Count);
        Assert.Equal(5, page.TotalCount);
        Assert.Equal(3, page.TotalPages);
        Assert.True(page.HasPreviousPage);
        Assert.True(page.HasNextPage);
        Assert.Equal([TodoCache.Page(2, 2)], _cache.RequestedKeys);
    }

    [Fact]
    public async Task Delete_RemovesTheItem()
    {
        var todo = await AddAsync("Delete me");
        var handler = new DeleteTodoCommandHandler(_db, NullLogger<DeleteTodoCommandHandler>.Instance);

        var result = await handler.Handle(new DeleteTodoCommand(todo.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(_db.TodoItems);
    }

    [Fact]
    public async Task Delete_UnknownId_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        var handler = new DeleteTodoCommandHandler(_db, NullLogger<DeleteTodoCommandHandler>.Instance);

        var result = await handler.Handle(new DeleteTodoCommand(id), CancellationToken.None);

        Assert.Equal(TodoErrors.NotFound(id), result.FirstError);
    }

    public void Dispose() => _db.Dispose();

    private async Task<TodoItem> AddAsync(string title)
    {
        var todo = TodoItem.Create(title, null).Value;
        _db.TodoItems.Add(todo);
        await _db.SaveChangesAsync();
        return todo;
    }
}
