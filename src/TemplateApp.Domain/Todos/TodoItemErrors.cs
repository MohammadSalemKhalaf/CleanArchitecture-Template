using TemplateApp.Domain.Common.Results;

namespace TemplateApp.Domain.Todos;

public static class TodoItemErrors
{
    public static readonly Error TitleRequired =
        Error.Validation(nameof(TodoItem.Title), "Title is required.");

    public static readonly Error TitleTooLong =
        Error.Validation(nameof(TodoItem.Title), $"Title must not exceed {TodoItem.TitleMaxLength} characters.");

    public static readonly Error DescriptionTooLong =
        Error.Validation(nameof(TodoItem.Description), $"Description must not exceed {TodoItem.DescriptionMaxLength} characters.");

    public static readonly Error AlreadyCompleted =
        Error.Conflict("TodoItem.AlreadyCompleted", "The todo item is already completed.");

    public static readonly Error NotCompleted =
        Error.Conflict("TodoItem.NotCompleted", "The todo item is not completed.");
}
