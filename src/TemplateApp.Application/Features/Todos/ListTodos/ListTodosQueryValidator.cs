using FluentValidation;

using TemplateApp.Application.Common.Models;

namespace TemplateApp.Application.Features.Todos.ListTodos;

public sealed class ListTodosQueryValidator : AbstractValidator<ListTodosQuery>
{
    public ListTodosQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, PagedResult<TodoItemResponse>.MaxPageSize);
    }
}
