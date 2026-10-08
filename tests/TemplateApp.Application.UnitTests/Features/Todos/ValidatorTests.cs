using TemplateApp.Application.Features.Todos.CreateTodo;
using TemplateApp.Application.Features.Todos.ListTodos;
using TemplateApp.Application.Features.Todos.UpdateTodo;
using TemplateApp.Domain.Todos;

namespace TemplateApp.Application.UnitTests.Features.Todos;

public sealed class ValidatorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void CreateTodo_RequiresTitle(string? title)
    {
        var result = new CreateTodoCommandValidator().Validate(new CreateTodoCommand(title!, null));

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateTodoCommand.Title));
    }

    [Fact]
    public void CreateTodo_LimitsTitleAndDescriptionLength()
    {
        var command = new CreateTodoCommand(
            new string('a', TodoItem.TitleMaxLength + 1),
            new string('b', TodoItem.DescriptionMaxLength + 1));

        var result = new CreateTodoCommandValidator().Validate(command);

        Assert.Equal(
            [nameof(CreateTodoCommand.Title), nameof(CreateTodoCommand.Description)],
            result.Errors.Select(error => error.PropertyName));
    }

    [Fact]
    public void UpdateTodo_RequiresId()
    {
        var result = new UpdateTodoCommandValidator().Validate(new UpdateTodoCommand(Guid.Empty, "Title", null, false));

        Assert.Equal([nameof(UpdateTodoCommand.Id)], result.Errors.Select(error => error.PropertyName));
    }

    [Theory]
    [InlineData(0, 20, nameof(ListTodosQuery.Page))]
    [InlineData(1, 0, nameof(ListTodosQuery.PageSize))]
    [InlineData(1, 101, nameof(ListTodosQuery.PageSize))]
    public void ListTodos_RejectsOutOfRangePaging(int page, int pageSize, string invalidProperty)
    {
        var result = new ListTodosQueryValidator().Validate(new ListTodosQuery(page, pageSize));

        Assert.Equal([invalidProperty], result.Errors.Select(error => error.PropertyName));
    }

    [Fact]
    public void ListTodos_AcceptsTheMaximumPageSize()
    {
        Assert.True(new ListTodosQueryValidator().Validate(new ListTodosQuery(1, 100)).IsValid);
    }
}
