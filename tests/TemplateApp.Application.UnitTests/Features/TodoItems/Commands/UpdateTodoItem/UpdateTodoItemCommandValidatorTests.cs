using TemplateApp.Application.Features.TodoItems.Commands.UpdateTodoItem;

namespace TemplateApp.Application.UnitTests.Features.TodoItems.Commands.UpdateTodoItem;

public sealed class UpdateTodoItemCommandValidatorTests
{
    [Fact]
    public void Validate_RequiresId()
    {
        var result = new UpdateTodoItemCommandValidator().Validate(new UpdateTodoItemCommand(Guid.Empty, "Title", null, false));

        Assert.Equal([nameof(UpdateTodoItemCommand.TodoItemId)], result.Errors.Select(error => error.PropertyName));
    }
}
