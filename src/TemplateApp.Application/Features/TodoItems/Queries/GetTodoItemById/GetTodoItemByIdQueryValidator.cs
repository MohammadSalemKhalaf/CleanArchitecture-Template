using FluentValidation;

namespace TemplateApp.Application.Features.TodoItems.Queries.GetTodoItemById;

public sealed class GetTodoItemByIdQueryValidator : AbstractValidator<GetTodoItemByIdQuery>
{
    public GetTodoItemByIdQueryValidator()
    {
        RuleFor(query => query.TodoItemId)
            .NotEmpty();
    }
}
