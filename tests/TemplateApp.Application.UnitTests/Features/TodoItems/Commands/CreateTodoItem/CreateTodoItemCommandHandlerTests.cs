using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using TemplateApp.Application.Common.Exceptions;
using TemplateApp.Application.Common.Interfaces;
using TemplateApp.Application.Features.TodoItems.Commands.CreateTodoItem;
using TemplateApp.Application.UnitTests.Common;
using TemplateApp.Domain.Common.Results;
using TemplateApp.Domain.TodoItems;
using TemplateApp.Tests.Common.TodoItems;

namespace TemplateApp.Application.UnitTests.Features.TodoItems.Commands.CreateTodoItem;

public sealed class CreateTodoItemCommandHandlerTests : IDisposable
{
    private readonly InMemoryAppDbContext _context = InMemoryAppDbContext.Create();
    private readonly CreateTodoItemCommandHandler _handler;

    public CreateTodoItemCommandHandlerTests()
    {
        _handler = new CreateTodoItemCommandHandler(_context, NullLogger<CreateTodoItemCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_PersistsTheItemAndReturnsIt()
    {
        var result = await _handler.Handle(new CreateTodoItemCommand(" Buy milk ", "2 litres"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Buy milk", result.Value.Title);

        var stored = await _context.TodoItems.SingleAsync();
        Assert.Equal(result.Value.TodoItemId, stored.Id);
        Assert.Equal("2 litres", stored.Description);
    }

    [Fact]
    public async Task Handle_WithExistingTitle_ReturnsConflictAndDoesNotPersist()
    {
        _context.TodoItems.Add(TodoItemFactory.CreateTodoItem("Buy milk"));
        await _context.SaveChangesAsync();

        var result = await _handler.Handle(new CreateTodoItemCommand("  Buy milk", null), CancellationToken.None);

        Assert.Equal(TodoItemErrors.DuplicateTitle, result.FirstError);
        Assert.Equal(1, await _context.TodoItems.CountAsync());
    }

    [Fact]
    public async Task Handle_PropagatesDomainValidationErrors()
    {
        // The validator normally rejects this first; the domain must still protect its invariants on its own.
        var result = await _handler.Handle(new CreateTodoItemCommand("   ", null), CancellationToken.None);

        Assert.Equal(ErrorKind.Validation, result.FirstError.Kind);
        Assert.Equal(TodoItemErrors.TitleRequired, result.FirstError);
        Assert.Empty(_context.TodoItems);
    }

    [Fact]
    public async Task Handle_WhenTheUniqueIndexRejectsAConcurrentDuplicate_ReturnsConflict()
    {
        // Another request inserted the same title between the duplicate check and SaveChanges.
        var handler = new CreateTodoItemCommandHandler(new UniqueViolationOnSave(_context), NullLogger<CreateTodoItemCommandHandler>.Instance);

        var result = await handler.Handle(new CreateTodoItemCommand("Raced", null), CancellationToken.None);

        Assert.Equal(TodoItemErrors.DuplicateTitle, result.FirstError);
    }

    public void Dispose() => _context.Dispose();

    private sealed class UniqueViolationOnSave(InMemoryAppDbContext inner) : IAppDbContext
    {
        public DbSet<TodoItem> TodoItems => inner.TodoItems;

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new UniqueConstraintViolationException("duplicate", new InvalidOperationException());
    }
}
