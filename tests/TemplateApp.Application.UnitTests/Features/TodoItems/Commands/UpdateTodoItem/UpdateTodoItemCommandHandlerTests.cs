using TemplateApp.Application.Features.TodoItems.Commands.UpdateTodoItem;
using TemplateApp.Application.UnitTests.Common;
using TemplateApp.Domain.TodoItems;
using TemplateApp.Tests.Common.TodoItems;

namespace TemplateApp.Application.UnitTests.Features.TodoItems.Commands.UpdateTodoItem;

public sealed class UpdateTodoItemCommandHandlerTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 8, 0, 0, TimeSpan.Zero);

    private readonly InMemoryAppDbContext _context = InMemoryAppDbContext.Create();
    private readonly UpdateTodoItemCommandHandler _handler;

    public UpdateTodoItemCommandHandlerTests()
    {
        _handler = new UpdateTodoItemCommandHandler(_context, new FixedTimeProvider(Now));
    }

    [Fact]
    public async Task Handle_UnknownId_ReturnsNotFound()
    {
        var id = Guid.NewGuid();

        var result = await _handler.Handle(new UpdateTodoItemCommand(id, "Title", null, false), CancellationToken.None);

        Assert.Equal(TodoItemErrors.NotFound(id), result.FirstError);
    }

    [Fact]
    public async Task Handle_TitleTakenByAnotherItem_ReturnsConflict()
    {
        var target = await AddAsync("First");
        await AddAsync("Second");

        var result = await _handler.Handle(new UpdateTodoItemCommand(target.Id, "Second", null, false), CancellationToken.None);

        Assert.Equal(TodoItemErrors.DuplicateTitle, result.FirstError);
        Assert.Equal("First", target.Title);
    }

    [Fact]
    public async Task Handle_KeepingItsOwnTitle_IsNotAConflict()
    {
        var target = await AddAsync("Same");

        var result = await _handler.Handle(new UpdateTodoItemCommand(target.Id, "Same", "New notes", false), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("New notes", result.Value.Description);
    }

    [Fact]
    public async Task Handle_MarkingCompleted_UsesTheCurrentTime()
    {
        var target = await AddAsync("Task");

        var result = await _handler.Handle(new UpdateTodoItemCommand(target.Id, "Task", null, true), CancellationToken.None);

        Assert.True(result.Value.IsCompleted);
        Assert.Equal(Now, result.Value.CompletedAtUtc);
    }

    [Fact]
    public async Task Handle_MarkingNotCompleted_ReopensTheItem()
    {
        var target = await AddAsync("Task");
        target.Complete(Now.AddDays(-1));
        await _context.SaveChangesAsync();

        var result = await _handler.Handle(new UpdateTodoItemCommand(target.Id, "Task", null, false), CancellationToken.None);

        Assert.False(result.Value.IsCompleted);
        Assert.Null(result.Value.CompletedAtUtc);
    }

    public void Dispose() => _context.Dispose();

    private async Task<TodoItem> AddAsync(string title)
    {
        var todoItem = TodoItemFactory.CreateTodoItem(title);
        _context.TodoItems.Add(todoItem);
        await _context.SaveChangesAsync();
        return todoItem;
    }
}
