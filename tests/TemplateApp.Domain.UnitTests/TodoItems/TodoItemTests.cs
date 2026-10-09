using TemplateApp.Domain.Common.Results;
using TemplateApp.Domain.TodoItems;
using TemplateApp.Tests.Common.TodoItems;

namespace TemplateApp.Domain.UnitTests.TodoItems;

public sealed class TodoItemTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 10, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Create_TrimsTitleAndTurnsBlankDescriptionIntoNull()
    {
        var result = TodoItem.Create("  Write tests  ", "   ");

        Assert.True(result.IsSuccess);
        Assert.Equal("Write tests", result.Value.Title);
        Assert.Null(result.Value.Description);
        Assert.False(result.Value.IsCompleted);
        Assert.NotEqual(Guid.Empty, result.Value.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankTitle_FailsWithTitleRequired(string title)
    {
        var result = TodoItem.Create(title, null);

        Assert.Equal([TodoItemErrors.TitleRequired], result.Errors);
    }

    [Fact]
    public void Create_MeasuresTitleLengthAfterTrimming()
    {
        var title = new string('a', TodoItem.TitleMaxLength);

        var result = TodoItem.Create($"  {title}  ", null);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_ReportsEveryViolatedRule()
    {
        var result = TodoItem.Create(
            new string('a', TodoItem.TitleMaxLength + 1),
            new string('b', TodoItem.DescriptionMaxLength + 1));

        Assert.Equal([TodoItemErrors.TitleTooLong, TodoItemErrors.DescriptionTooLong], result.Errors);
    }

    [Fact]
    public void Update_WithInvalidTitle_LeavesTheItemUnchanged()
    {
        var todo = TodoItemFactory.CreateTodoItem("Original", "Notes");

        var result = todo.Update(" ", "Changed");

        Assert.True(result.IsError);
        Assert.Equal("Original", todo.Title);
        Assert.Equal("Notes", todo.Description);
    }

    [Fact]
    public void Complete_RecordsCompletionTime()
    {
        var todo = TodoItemFactory.CreateTodoItem();

        var result = todo.Complete(Now);

        Assert.True(result.IsSuccess);
        Assert.True(todo.IsCompleted);
        Assert.Equal(Now, todo.CompletedAtUtc);
    }

    [Fact]
    public void Complete_WhenAlreadyCompleted_FailsAndKeepsOriginalCompletionTime()
    {
        var todo = TodoItemFactory.CreateTodoItem();
        todo.Complete(Now);

        var result = todo.Complete(Now.AddHours(1));

        Assert.Equal(TodoItemErrors.AlreadyCompleted, result.FirstError);
        Assert.Equal(Now, todo.CompletedAtUtc);
    }

    [Fact]
    public void Reopen_ClearsCompletion()
    {
        var todo = TodoItemFactory.CreateTodoItem();
        todo.Complete(Now);

        var result = todo.Reopen();

        Assert.True(result.IsSuccess);
        Assert.False(todo.IsCompleted);
        Assert.Null(todo.CompletedAtUtc);
    }

    [Fact]
    public void Reopen_WhenNotCompleted_FailsWithConflict()
    {
        var todo = TodoItemFactory.CreateTodoItem();

        var result = todo.Reopen();

        Assert.Equal(ErrorKind.Conflict, result.FirstError.Kind);
        Assert.Equal(TodoItemErrors.NotCompleted, result.FirstError);
    }
}
