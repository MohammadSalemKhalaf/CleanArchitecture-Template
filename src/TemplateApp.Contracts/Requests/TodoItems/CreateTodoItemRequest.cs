namespace TemplateApp.Contracts.Requests.TodoItems;

/// <summary>Body of <c>POST /api/v1/todo-items</c>. Validation rules live in the application command validator.</summary>
public sealed class CreateTodoItemRequest
{
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }
}
