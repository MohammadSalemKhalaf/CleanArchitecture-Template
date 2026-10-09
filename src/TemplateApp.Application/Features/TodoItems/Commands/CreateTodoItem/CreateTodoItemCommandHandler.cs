using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using TemplateApp.Application.Common.Exceptions;
using TemplateApp.Application.Common.Interfaces;
using TemplateApp.Application.Features.TodoItems.Dtos;
using TemplateApp.Application.Features.TodoItems.Mappers;
using TemplateApp.Domain.Common.Results;
using TemplateApp.Domain.TodoItems;

namespace TemplateApp.Application.Features.TodoItems.Commands.CreateTodoItem;

public sealed class CreateTodoItemCommandHandler(IAppDbContext context, ILogger<CreateTodoItemCommandHandler> logger)
    : ICommandHandler<CreateTodoItemCommand, TodoItemDto>
{
    public async Task<Result<TodoItemDto>> Handle(CreateTodoItemCommand command, CancellationToken ct)
    {
        var title = command.Title.Trim();

        if (await context.TodoItems.AnyAsync(item => item.Title == title, ct))
        {
            return TodoItemErrors.DuplicateTitle;
        }

        var createResult = TodoItem.Create(command.Title, command.Description);

        if (createResult.IsError)
        {
            return Result.Failure<TodoItemDto>(createResult.Errors);
        }

        context.TodoItems.Add(createResult.Value);

        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (UniqueConstraintViolationException)
        {
            return TodoItemErrors.DuplicateTitle;
        }

        logger.LogInformation("Todo item created. Id: {TodoItemId}", createResult.Value.Id);

        return createResult.Value.ToDto();
    }
}
