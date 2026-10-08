using TemplateApp.Domain.Common;
using TemplateApp.Domain.Common.Results;

namespace TemplateApp.Domain.TodoItems;

/// <summary>
/// Sample aggregate. It exists to demonstrate the template's conventions end to end and is meant to be deleted
/// (see README, "Removing the sample feature").
/// </summary>
public sealed class TodoItem : AuditableEntity
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 2000;

    private TodoItem()
    {
    }

    private TodoItem(Guid id, string title, string? description)
        : base(id)
    {
        Title = title;
        Description = description;
    }

    public string Title { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public bool IsCompleted { get; private set; }

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public static Result<TodoItem> Create(string title, string? description)
    {
        var errors = Validate(title, description);

        if (errors.Count > 0)
        {
            return errors;
        }

        return new TodoItem(Guid.NewGuid(), title.Trim(), Normalize(description));
    }

    public Result<Updated> Update(string title, string? description)
    {
        var errors = Validate(title, description);

        if (errors.Count > 0)
        {
            return errors;
        }

        Title = title.Trim();
        Description = Normalize(description);

        return Result.Updated;
    }

    public Result<Updated> Complete(DateTimeOffset completedAtUtc)
    {
        if (IsCompleted)
        {
            return TodoItemErrors.AlreadyCompleted;
        }

        IsCompleted = true;
        CompletedAtUtc = completedAtUtc;

        return Result.Updated;
    }

    public Result<Updated> Reopen()
    {
        if (!IsCompleted)
        {
            return TodoItemErrors.NotCompleted;
        }

        IsCompleted = false;
        CompletedAtUtc = null;

        return Result.Updated;
    }

    private static List<Error> Validate(string title, string? description)
    {
        List<Error> errors = [];

        if (string.IsNullOrWhiteSpace(title))
        {
            errors.Add(TodoItemErrors.TitleRequired);
        }
        else if (title.Trim().Length > TitleMaxLength)
        {
            errors.Add(TodoItemErrors.TitleTooLong);
        }

        if (description?.Trim().Length > DescriptionMaxLength)
        {
            errors.Add(TodoItemErrors.DescriptionTooLong);
        }

        return errors;
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
