using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using TemplateApp.Application.Common.Interfaces;
using TemplateApp.Domain.Common.Results;
using TemplateApp.Domain.TodoItems;

namespace TemplateApp.Application.Features.TodoItems.Commands.DeleteTodoItem;

public sealed class DeleteTodoItemCommandHandler(IAppDbContext context, ILogger<DeleteTodoItemCommandHandler> logger)
    : ICommandHandler<DeleteTodoItemCommand, Deleted>
{
    public async Task<Result<Deleted>> Handle(DeleteTodoItemCommand command, CancellationToken ct)
    {
        var todoItem = await context.TodoItems.FirstOrDefaultAsync(item => item.Id == command.TodoItemId, ct);

        if (todoItem is null)
        {
            return TodoItemErrors.NotFound(command.TodoItemId);
        }

        context.TodoItems.Remove(todoItem);

        await context.SaveChangesAsync(ct);

        logger.LogInformation("Todo item deleted. Id: {TodoItemId}", command.TodoItemId);

        return Result.Deleted;
    }
}
