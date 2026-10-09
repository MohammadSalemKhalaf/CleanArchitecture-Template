using FluentValidation;

using TemplateApp.Domain.TodoItems;

namespace TemplateApp.Application.Features.TodoItems.Commands.UpdateTodoItem;

public sealed class UpdateTodoItemCommandValidator : AbstractValidator<UpdateTodoItemCommand>
{
    public UpdateTodoItemCommandValidator()
    {
        RuleFor(command => command.TodoItemId)
            .NotEmpty();

        RuleFor(command => command.Title)
            .NotEmpty()
            .MaximumLength(TodoItem.TitleMaxLength);

        RuleFor(command => command.Description)
            .MaximumLength(TodoItem.DescriptionMaxLength);
    }
}
