using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using TemplateApp.Application.Common.Data;
using TemplateApp.Application.Common.Messaging;
using TemplateApp.Domain.Common.Results;
using TemplateApp.Domain.Todos;

namespace TemplateApp.Application.Features.Todos.CreateTodo;

public sealed class CreateTodoCommandHandler(IAppDbContext db, ILogger<CreateTodoCommandHandler> logger)
    : ICommandHandler<CreateTodoCommand, TodoItemResponse>
{
    public async Task<Result<TodoItemResponse>> Handle(CreateTodoCommand command, CancellationToken cancellationToken)
    {
        var title = command.Title.Trim();

        if (await db.TodoItems.AnyAsync(todo => todo.Title == title, cancellationToken))
        {
            return TodoErrors.DuplicateTitle;
        }

        var created = TodoItem.Create(command.Title, command.Description);

        if (created.IsError)
        {
            return Result.Failure<TodoItemResponse>(created.Errors);
        }

        db.TodoItems.Add(created.Value);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            return TodoErrors.DuplicateTitle;
        }

        logger.LogInformation("Created todo item {TodoId}", created.Value.Id);

        return TodoItemResponse.From(created.Value);
    }
}
