using Microsoft.EntityFrameworkCore;

using TemplateApp.Application.Common.Data;
using TemplateApp.Application.Common.Messaging;
using TemplateApp.Domain.Common.Results;

namespace TemplateApp.Application.Features.Todos.UpdateTodo;

public sealed class UpdateTodoCommandHandler(IAppDbContext db, TimeProvider timeProvider)
    : ICommandHandler<UpdateTodoCommand, TodoItemResponse>
{
    public async Task<Result<TodoItemResponse>> Handle(UpdateTodoCommand command, CancellationToken cancellationToken)
    {
        var todo = await db.TodoItems.FirstOrDefaultAsync(item => item.Id == command.Id, cancellationToken);

        if (todo is null)
        {
            return TodoErrors.NotFound(command.Id);
        }

        var title = command.Title.Trim();

        if (await db.TodoItems.AnyAsync(item => item.Id != command.Id && item.Title == title, cancellationToken))
        {
            return TodoErrors.DuplicateTitle;
        }

        var updated = todo.Update(command.Title, command.Description);

        if (updated.IsError)
        {
            return Result.Failure<TodoItemResponse>(updated.Errors);
        }

        if (command.IsCompleted != todo.IsCompleted)
        {
            var transition = command.IsCompleted
                ? todo.Complete(timeProvider.GetUtcNow())
                : todo.Reopen();

            if (transition.IsError)
            {
                return Result.Failure<TodoItemResponse>(transition.Errors);
            }
        }

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            return TodoErrors.DuplicateTitle;
        }

        return TodoItemResponse.From(todo);
    }
}
