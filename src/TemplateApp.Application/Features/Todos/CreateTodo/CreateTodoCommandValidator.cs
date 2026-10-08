using FluentValidation;

using TemplateApp.Domain.Todos;

namespace TemplateApp.Application.Features.Todos.CreateTodo;

public sealed class CreateTodoCommandValidator : AbstractValidator<CreateTodoCommand>
{
    public CreateTodoCommandValidator()
    {
        RuleFor(command => command.Title)
            .NotEmpty()
            .MaximumLength(TodoItem.TitleMaxLength);

        RuleFor(command => command.Description)
            .MaximumLength(TodoItem.DescriptionMaxLength);
    }
}
