namespace TemplateApp.Api.Endpoints.Todos;

/// <summary>
/// HTTP body for creating a todo. Kept separate from the command so the wire format can evolve independently of the use case.
/// A missing title binds as null and is rejected by the command validator.
/// </summary>
public sealed record CreateTodoRequest(string Title, string? Description);
