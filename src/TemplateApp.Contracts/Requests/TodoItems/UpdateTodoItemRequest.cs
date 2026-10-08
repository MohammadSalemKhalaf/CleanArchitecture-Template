namespace TemplateApp.Contracts.Requests.TodoItems;

/// <summary>Body of <c>PUT /api/v1/todo-items/{todoItemId}</c>. The id comes from the route.</summary>
public sealed class UpdateTodoItemRequest
{
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsCompleted { get; set; }
}
