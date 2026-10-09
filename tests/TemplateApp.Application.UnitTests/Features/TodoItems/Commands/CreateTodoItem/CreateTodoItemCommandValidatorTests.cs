using TemplateApp.Application.Features.TodoItems.Commands.CreateTodoItem;
using TemplateApp.Domain.TodoItems;

namespace TemplateApp.Application.UnitTests.Features.TodoItems.Commands.CreateTodoItem;

public sealed class CreateTodoItemCommandValidatorTests
{
    private readonly CreateTodoItemCommandValidator _validator = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Validate_RequiresTitle(string? title)
    {
        var result = _validator.Validate(new CreateTodoItemCommand(title!, null));

        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateTodoItemCommand.Title));
    }

    [Fact]
    public void Validate_LimitsTitleAndDescriptionLength()
    {
        var command = new CreateTodoItemCommand(
            new string('a', TodoItem.TitleMaxLength + 1),
            new string('b', TodoItem.DescriptionMaxLength + 1));

        var result = _validator.Validate(command);

        Assert.Equal(
            [nameof(CreateTodoItemCommand.Title), nameof(CreateTodoItemCommand.Description)],
            result.Errors.Select(error => error.PropertyName));
    }
}
