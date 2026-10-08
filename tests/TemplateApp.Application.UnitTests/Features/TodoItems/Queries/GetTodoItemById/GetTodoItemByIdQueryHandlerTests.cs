using TemplateApp.Application.Features.TodoItems.Queries.GetTodoItemById;
using TemplateApp.Application.UnitTests.Common;
using TemplateApp.Domain.TodoItems;
using TemplateApp.Tests.Common.TodoItems;

namespace TemplateApp.Application.UnitTests.Features.TodoItems.Queries.GetTodoItemById;

public sealed class GetTodoItemByIdQueryHandlerTests : IDisposable
{
    private readonly InMemoryAppDbContext _context = InMemoryAppDbContext.Create();
    private readonly RecordingCache _cache = new();

    [Fact]
    public async Task Handle_ReadsThroughTheQueryCacheKey()
    {
        var todoItem = TodoItemFactory.CreateTodoItem("Cached");
        _context.TodoItems.Add(todoItem);
        await _context.SaveChangesAsync();
        var query = new GetTodoItemByIdQuery(todoItem.Id);

        var result = await new GetTodoItemByIdQueryHandler(_context, _cache).Handle(query, CancellationToken.None);

        Assert.Equal("Cached", result.Value.Title);
        Assert.Equal([query.CacheKey], _cache.RequestedKeys);
    }

    [Fact]
    public async Task Handle_UnknownId_ReturnsNotFound()
    {
        var id = Guid.NewGuid();

        var result = await new GetTodoItemByIdQueryHandler(_context, _cache).Handle(new GetTodoItemByIdQuery(id), CancellationToken.None);

        Assert.Equal(TodoItemErrors.NotFound(id), result.FirstError);
    }

    public void Dispose() => _context.Dispose();
}
