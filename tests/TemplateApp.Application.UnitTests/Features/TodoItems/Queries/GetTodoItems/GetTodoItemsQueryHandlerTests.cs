using TemplateApp.Application.Features.TodoItems.Queries.GetTodoItems;
using TemplateApp.Application.UnitTests.Common;
using TemplateApp.Tests.Common.TodoItems;

namespace TemplateApp.Application.UnitTests.Features.TodoItems.Queries.GetTodoItems;

public sealed class GetTodoItemsQueryHandlerTests : IDisposable
{
    private readonly InMemoryAppDbContext _context = InMemoryAppDbContext.Create();
    private readonly RecordingCache _cache = new();

    [Fact]
    public async Task Handle_ReturnsTheRequestedPageAndTotals()
    {
        for (var i = 0; i < 5; i++)
        {
            _context.TodoItems.Add(TodoItemFactory.CreateTodoItem());
        }

        await _context.SaveChangesAsync();
        var query = new GetTodoItemsQuery(Page: 2, PageSize: 2);

        var result = await new GetTodoItemsQueryHandler(_context, _cache).Handle(query, CancellationToken.None);

        var page = result.Value;
        Assert.Equal(2, page.Items.Count);
        Assert.Equal(2, page.PageNumber);
        Assert.Equal(5, page.TotalCount);
        Assert.Equal(3, page.TotalPages);
        Assert.Equal([query.CacheKey], _cache.RequestedKeys);
    }

    [Fact]
    public void CacheKey_DiffersPerPage()
    {
        Assert.NotEqual(new GetTodoItemsQuery(1, 10).CacheKey, new GetTodoItemsQuery(2, 10).CacheKey);
        Assert.NotEqual(new GetTodoItemsQuery(1, 10).CacheKey, new GetTodoItemsQuery(1, 20).CacheKey);
    }

    public void Dispose() => _context.Dispose();
}
