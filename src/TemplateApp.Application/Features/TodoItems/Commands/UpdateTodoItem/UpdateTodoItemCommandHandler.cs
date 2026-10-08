using Microsoft.EntityFrameworkCore;

using TemplateApp.Application.Common.Exceptions;
using TemplateApp.Application.Common.Interfaces;
using TemplateApp.Application.Features.TodoItems.Dtos;
using TemplateApp.Application.Features.TodoItems.Mappers;
using TemplateApp.Domain.Common.Results;
using TemplateApp.Domain.TodoItems;

namespace TemplateApp.Application.Features.TodoItems.Commands.UpdateTodoItem;

public sealed class UpdateTodoItemCommandHandler(IAppDbContext context, TimeProvider timeProvider)
    : ICommandHandler<UpdateTodoItemCommand, TodoItemDto>
{
    public async Task<Result<TodoItemDto>> Handle(UpdateTodoItemCommand command, CancellationToken ct)
    {
        var todoItem = await context.TodoItems.FirstOrDefaultAsync(item => item.Id == command.TodoItemId, ct);

        if (todoItem is null)
        {
            return TodoItemErrors.NotFound(command.TodoItemId);
        }

        var title = command.Title.Trim();

        if (await context.TodoItems.AnyAsync(item => item.Id != command.TodoItemId && item.Title == title, ct))
        {
            return TodoItemErrors.DuplicateTitle;
        }

        var updateResult = todoItem.Update(command.Title, command.Description);

        if (updateResult.IsError)
        {
            return Result.Failure<TodoItemDto>(updateResult.Errors);
        }

        if (command.IsCompleted != todoItem.IsCompleted)
        {
            var transition = command.IsCompleted
                ? todoItem.Complete(timeProvider.GetUtcNow())
                : todoItem.Reopen();

            if (transition.IsError)
            {
                return Result.Failure<TodoItemDto>(transition.Errors);
            }
        }

        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (UniqueConstraintViolationException)
        {
            return TodoItemErrors.DuplicateTitle;
        }

        return todoItem.ToDto();
    }
}
