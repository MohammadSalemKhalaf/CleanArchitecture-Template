using Microsoft.Extensions.Logging.Abstractions;

using TemplateApp.Application.Features.TodoItems.Commands.DeleteTodoItem;
using TemplateApp.Application.UnitTests.Common;
using TemplateApp.Domain.TodoItems;
using TemplateApp.Tests.Common.TodoItems;

namespace TemplateApp.Application.UnitTests.Features.TodoItems.Commands.DeleteTodoItem;

public sealed class DeleteTodoItemCommandHandlerTests : IDisposable
{
    private readonly InMemoryAppDbContext _context = InMemoryAppDbContext.Create();
    private readonly DeleteTodoItemCommandHandler _handler;

    public DeleteTodoItemCommandHandlerTests()
    {
        _handler = new DeleteTodoItemCommandHandler(_context, NullLogger<DeleteTodoItemCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_RemovesTheItem()
    {
        var todoItem = TodoItemFactory.CreateTodoItem();
        _context.TodoItems.Add(todoItem);
        await _context.SaveChangesAsync();

        var result = await _handler.Handle(new DeleteTodoItemCommand(todoItem.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(_context.TodoItems);
    }

    [Fact]
    public async Task Handle_UnknownId_ReturnsNotFound()
    {
        var id = Guid.NewGuid();

        var result = await _handler.Handle(new DeleteTodoItemCommand(id), CancellationToken.None);

        Assert.Equal(TodoItemErrors.NotFound(id), result.FirstError);
    }

    public void Dispose() => _context.Dispose();
}
