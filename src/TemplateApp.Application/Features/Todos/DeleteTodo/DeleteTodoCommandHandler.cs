using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using TemplateApp.Application.Common.Data;
using TemplateApp.Application.Common.Messaging;
using TemplateApp.Domain.Common.Results;

namespace TemplateApp.Application.Features.Todos.DeleteTodo;

public sealed class DeleteTodoCommandHandler(IAppDbContext db, ILogger<DeleteTodoCommandHandler> logger)
    : ICommandHandler<DeleteTodoCommand, Deleted>
{
    public async Task<Result<Deleted>> Handle(DeleteTodoCommand command, CancellationToken cancellationToken)
    {
        var todo = await db.TodoItems.FirstOrDefaultAsync(item => item.Id == command.Id, cancellationToken);

        if (todo is null)
        {
            return TodoErrors.NotFound(command.Id);
        }

        db.TodoItems.Remove(todo);

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Deleted todo item {TodoId}", command.Id);

        return Result.Deleted;
    }
}
