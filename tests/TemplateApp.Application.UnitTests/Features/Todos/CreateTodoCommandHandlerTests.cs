using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using TemplateApp.Application.Common.Data;
using TemplateApp.Application.Features.Todos;
using TemplateApp.Application.Features.Todos.CreateTodo;
using TemplateApp.Application.UnitTests.TestDoubles;
using TemplateApp.Domain.Common.Results;
using TemplateApp.Domain.Todos;

namespace TemplateApp.Application.UnitTests.Features.Todos;

public sealed class CreateTodoCommandHandlerTests : IDisposable
{
    private readonly InMemoryAppDbContext _db = InMemoryAppDbContext.Create();
    private readonly CreateTodoCommandHandler _handler;

    public CreateTodoCommandHandlerTests()
    {
        _handler = new CreateTodoCommandHandler(_db, NullLogger<CreateTodoCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_PersistsTheItemAndReturnsIt()
    {
        var result = await _handler.Handle(new CreateTodoCommand(" Buy milk ", "2 litres"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Buy milk", result.Value.Title);

        var stored = await _db.TodoItems.SingleAsync();
        Assert.Equal(result.Value.Id, stored.Id);
        Assert.Equal("2 litres", stored.Description);
    }

    [Fact]
    public async Task Handle_WithExistingTitle_ReturnsConflictAndDoesNotPersist()
    {
        _db.TodoItems.Add(TodoItem.Create("Buy milk", null).Value);
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new CreateTodoCommand("  Buy milk", null), CancellationToken.None);

        Assert.Equal(TodoErrors.DuplicateTitle, result.FirstError);
        Assert.Equal(1, await _db.TodoItems.CountAsync());
    }

    [Fact]
    public async Task Handle_PropagatesDomainValidationErrors()
    {
        // The validator normally rejects this first; the domain must still protect its invariants on its own.
        var result = await _handler.Handle(new CreateTodoCommand("   ", null), CancellationToken.None);

        Assert.Equal(ErrorKind.Validation, result.FirstError.Kind);
        Assert.Equal(TodoItemErrors.TitleRequired, result.FirstError);
        Assert.Empty(_db.TodoItems);
    }

    [Fact]
    public async Task Handle_WhenTheUniqueIndexRejectsAConcurrentDuplicate_ReturnsConflict()
    {
        // Another request inserted the same title between the duplicate check and SaveChanges.
        var handler = new CreateTodoCommandHandler(new UniqueViolationOnSave(_db), NullLogger<CreateTodoCommandHandler>.Instance);

        var result = await handler.Handle(new CreateTodoCommand("Raced", null), CancellationToken.None);

        Assert.Equal(TodoErrors.DuplicateTitle, result.FirstError);
    }

    public void Dispose() => _db.Dispose();

    private sealed class UniqueViolationOnSave(InMemoryAppDbContext inner) : IAppDbContext
    {
        public DbSet<TodoItem> TodoItems => inner.TodoItems;

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new UniqueConstraintViolationException("duplicate", new InvalidOperationException());
    }
}
