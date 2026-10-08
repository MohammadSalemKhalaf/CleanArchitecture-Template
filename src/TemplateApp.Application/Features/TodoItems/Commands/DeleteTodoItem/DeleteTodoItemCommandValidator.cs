using FluentValidation;

namespace TemplateApp.Application.Features.TodoItems.Commands.DeleteTodoItem;

public sealed class DeleteTodoItemCommandValidator : AbstractValidator<DeleteTodoItemCommand>
{
    public DeleteTodoItemCommandValidator()
    {
        RuleFor(command => command.TodoItemId)
            .NotEmpty();
    }
}
