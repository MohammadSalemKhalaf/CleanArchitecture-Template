using FluentValidation;

using TemplateApp.Application.Common.Models;
using TemplateApp.Application.Features.TodoItems.Dtos;

namespace TemplateApp.Application.Features.TodoItems.Queries.GetTodoItems;

public sealed class GetTodoItemsQueryValidator : AbstractValidator<GetTodoItemsQuery>
{
    public GetTodoItemsQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, PaginatedList<TodoItemDto>.MaxPageSize);
    }
}
