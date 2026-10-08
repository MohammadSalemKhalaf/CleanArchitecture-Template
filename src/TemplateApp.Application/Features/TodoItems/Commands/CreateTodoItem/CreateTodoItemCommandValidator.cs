using FluentValidation;

using TemplateApp.Domain.TodoItems;

namespace TemplateApp.Application.Features.TodoItems.Commands.CreateTodoItem;

public sealed class CreateTodoItemCommandValidator : AbstractValidator<CreateTodoItemCommand>
{
    public CreateTodoItemCommandValidator()
    {
        RuleFor(command => command.Title)
            .NotEmpty()
            .MaximumLength(TodoItem.TitleMaxLength);

        RuleFor(command => command.Description)
            .MaximumLength(TodoItem.DescriptionMaxLength);
    }
}
