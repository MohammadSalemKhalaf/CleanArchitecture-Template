using TemplateApp.Application.Features.Todos;
using TemplateApp.Application.Features.Todos.UpdateTodo;
using TemplateApp.Application.UnitTests.TestDoubles;
using TemplateApp.Domain.Todos;

namespace TemplateApp.Application.UnitTests.Features.Todos;

public sealed class UpdateTodoCommandHandlerTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 8, 0, 0, TimeSpan.Zero);

    private readonly InMemoryAppDbContext _db = InMemoryAppDbContext.Create();
    private readonly UpdateTodoCommandHandler _handler;

    public UpdateTodoCommandHandlerTests()
    {
        _handler = new UpdateTodoCommandHandler(_db, new FixedTimeProvider(Now));
    }

    [Fact]
    public async Task Handle_UnknownId_ReturnsNotFound()
    {
        var id = Guid.NewGuid();

        var result = await _handler.Handle(new UpdateTodoCommand(id, "Title", null, false), CancellationToken.None);

        Assert.Equal(TodoErrors.NotFound(id), result.FirstError);
    }

    [Fact]
    public async Task Handle_TitleTakenByAnotherItem_ReturnsConflict()
    {
        var target = await AddAsync("First");
        await AddAsync("Second");

        var result = await _handler.Handle(new UpdateTodoCommand(target.Id, "Second", null, false), CancellationToken.None);

        Assert.Equal(TodoErrors.DuplicateTitle, result.FirstError);
        Assert.Equal("First", target.Title);
    }

    [Fact]
    public async Task Handle_KeepingItsOwnTitle_IsNotAConflict()
    {
        var target = await AddAsync("Same");

        var result = await _handler.Handle(new UpdateTodoCommand(target.Id, "Same", "New notes", false), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("New notes", result.Value.Description);
    }

    [Fact]
    public async Task Handle_MarkingCompleted_UsesTheCurrentTime()
    {
        var target = await AddAsync("Task");

        var result = await _handler.Handle(new UpdateTodoCommand(target.Id, "Task", null, true), CancellationToken.None);

        Assert.True(result.Value.IsCompleted);
        Assert.Equal(Now, result.Value.CompletedAtUtc);
    }

    [Fact]
    public async Task Handle_MarkingNotCompleted_ReopensTheItem()
    {
        var target = await AddAsync("Task");
        target.Complete(Now.AddDays(-1));
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new UpdateTodoCommand(target.Id, "Task", null, false), CancellationToken.None);

        Assert.False(result.Value.IsCompleted);
        Assert.Null(result.Value.CompletedAtUtc);
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
