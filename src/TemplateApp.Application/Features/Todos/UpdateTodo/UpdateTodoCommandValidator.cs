using FluentValidation;

using TemplateApp.Domain.Todos;

namespace TemplateApp.Application.Features.Todos.UpdateTodo;

public sealed class UpdateTodoCommandValidator : AbstractValidator<UpdateTodoCommand>
{
    public UpdateTodoCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

        RuleFor(command => command.Title)
            .NotEmpty()
            .MaximumLength(TodoItem.TitleMaxLength);

        RuleFor(command => command.Description)
            .MaximumLength(TodoItem.DescriptionMaxLength);
    }
}
