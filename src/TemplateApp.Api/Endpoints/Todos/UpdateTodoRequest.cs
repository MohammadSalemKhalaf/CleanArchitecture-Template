namespace TemplateApp.Api.Endpoints.Todos;

/// <summary>HTTP body for replacing a todo's editable fields. The id comes from the route.</summary>
public sealed record UpdateTodoRequest(string Title, string? Description, bool IsCompleted);
