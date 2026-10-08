using TemplateApp.Domain.Common.Results;

namespace TemplateApp.Application.Features.Todos;

/// <summary>Application-level failures for the feature. Invariants of the entity itself live in <c>TodoItemErrors</c>.</summary>
public static class TodoErrors
{
    public static readonly Error DuplicateTitle =
        Error.Conflict("Todo.DuplicateTitle", "A todo item with the same title already exists.");

    public static Error NotFound(Guid id) =>
        Error.NotFound("Todo.NotFound", $"Todo item '{id}' was not found.");
}
