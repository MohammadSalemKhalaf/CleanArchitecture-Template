using MediatR;

using Microsoft.Extensions.DependencyInjection;

using TemplateApp.Application.Common.Interfaces;
using TemplateApp.Application.Features.TodoItems;
using TemplateApp.Application.Features.TodoItems.Commands.CreateTodoItem;
using TemplateApp.Application.Features.TodoItems.Commands.UpdateTodoItem;
using TemplateApp.Application.Features.TodoItems.Queries.GetTodoItemById;
using TemplateApp.Application.UnitTests.Common;
using TemplateApp.Domain.Common.Results;

namespace TemplateApp.Application.UnitTests.Behaviours;

/// <summary>Runs requests through the real MediatR pipeline as registered by <c>AddApplication</c>.</summary>
public sealed class PipelineTests : IAsyncDisposable
{
    private readonly RecordingCache _cache = new();
    private readonly ServiceProvider _services;
    private readonly AsyncServiceScope _scope;

    public PipelineTests()
    {
        var db = InMemoryAppDbContext.Create();

        _services = new ServiceCollection()
            .AddLogging()
            .AddApplication()
            .AddSingleton<IAppDbContext>(db)
            .AddSingleton<ICacheService>(_cache)
            .AddSingleton(TimeProvider.System)
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });

        // One scope per test, like one scope per HTTP request in the API.
        _scope = _services.CreateAsyncScope();
    }

    private ISender Sender => _scope.ServiceProvider.GetRequiredService<ISender>();

    [Fact]
    public async Task InvalidCommand_IsRejectedBeforeTheHandlerRuns()
    {
        var result = await Sender.Send(new CreateTodoItemCommand(string.Empty, new string('x', 3000)));

        Assert.All(result.Errors, error => Assert.Equal(ErrorKind.Validation, error.Kind));
        Assert.Equal(["Title", "Description"], result.Errors.Select(error => error.Code));
        Assert.Empty(_cache.InvalidatedTags);
    }

    [Fact]
    public async Task SuccessfulCommand_InvalidatesItsDeclaredTags()
    {
        var result = await Sender.Send(new CreateTodoItemCommand("New item", null));

        Assert.True(result.IsSuccess);
        Assert.Equal([TodoItemCache.Tag], _cache.InvalidatedTags);
    }

    [Fact]
    public async Task FailedCommand_DoesNotInvalidate()
    {
        var result = await Sender.Send(new UpdateTodoItemCommand(Guid.NewGuid(), "Missing", null, false));

        Assert.Equal(ErrorKind.NotFound, result.FirstError.Kind);
        Assert.Empty(_cache.InvalidatedTags);
    }

    [Fact]
    public async Task Update_AfterRead_IsVisibleOnTheNextRead()
    {
        var created = (await Sender.Send(new CreateTodoItemCommand("Before", null))).Value;
        await Sender.Send(new GetTodoItemByIdQuery(created.TodoItemId));

        await Sender.Send(new UpdateTodoItemCommand(created.TodoItemId, "After", null, false));
        var reread = await Sender.Send(new GetTodoItemByIdQuery(created.TodoItemId));

        Assert.Equal("After", reread.Value.Title);
    }

    public async ValueTask DisposeAsync()
    {
        await _scope.DisposeAsync();
        await _services.DisposeAsync();
    }
}
