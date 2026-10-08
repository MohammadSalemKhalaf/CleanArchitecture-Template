using TemplateApp.Application.Features.TodoItems.Queries.GetTodoItems;

namespace TemplateApp.Application.UnitTests.Features.TodoItems.Queries.GetTodoItems;

public sealed class GetTodoItemsQueryValidatorTests
{
    private readonly GetTodoItemsQueryValidator _validator = new();

    [Theory]
    [InlineData(0, 20, nameof(GetTodoItemsQuery.Page))]
    [InlineData(1, 0, nameof(GetTodoItemsQuery.PageSize))]
    [InlineData(1, 101, nameof(GetTodoItemsQuery.PageSize))]
    public void Validate_RejectsOutOfRangePaging(int page, int pageSize, string invalidProperty)
    {
        var result = _validator.Validate(new GetTodoItemsQuery(page, pageSize));

        Assert.Equal([invalidProperty], result.Errors.Select(error => error.PropertyName));
    }

    [Fact]
    public void Validate_AcceptsTheMaximumPageSize()
    {
        Assert.True(_validator.Validate(new GetTodoItemsQuery(1, 100)).IsValid);
    }
}
