using TemplateApp.Contracts.Requests.TodoItems;
using TemplateApp.Domain.TodoItems;

namespace TemplateApp.Tests.Common.TodoItems;

/// <summary>Valid sample data for the TodoItems feature. Tests override only what they are about.</summary>
public static class TodoItemFactory
{
    public static string UniqueTitle(string prefix = "Todo") => $"{prefix} {Guid.NewGuid():N}";

    public static TodoItem CreateTodoItem(string? title = null, string? description = null) =>
        TodoItem.Create(title ?? UniqueTitle(), description).Value;

    public static CreateTodoItemRequest CreateTodoItemRequest(string? title = null, string? description = null) =>
        new() { Title = title ?? UniqueTitle(), Description = description };

    public static UpdateTodoItemRequest UpdateTodoItemRequest(string? title = null, string? description = null, bool isCompleted = false) =>
        new() { Title = title ?? UniqueTitle(), Description = description, IsCompleted = isCompleted };
}
