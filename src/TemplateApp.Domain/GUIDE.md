# Domain layer guide

This project holds the business objects and the rules that must always be true about them. It is the center of the architecture: every other project may depend on it, and it depends on nothing.

- Rules: [RULES.md](RULES.md)
- The whole picture: [docs/ARCHITECTURE-GUIDE.md](../../docs/ARCHITECTURE-GUIDE.md)

## References

None: no project references and no packages ([TemplateApp.Domain.csproj](TemplateApp.Domain.csproj)). This keeps business rules free of databases, web frameworks and libraries, so they are easy to test and hard to break by accident. `LayerDependencyTests.Domain_DependsOnNoOtherLayerOrFramework` enforces it.

## Folders and files

| Path | Contents |
|---|---|
| [Common/Entity.cs](Common/Entity.cs) | Base class with a `Guid Id`. |
| [Common/AuditableEntity.cs](Common/AuditableEntity.cs) | Adds `CreatedAtUtc`, `CreatedBy`, `LastModifiedAtUtc` and `LastModifiedBy`. The setters are private; Infrastructure fills these columns on save. |
| [Common/Results/](Common/Results/Result.cs) | `Result<T>`, `Error`, `ErrorKind`, and the marker results `Success`, `Created`, `Updated` and `Deleted`. |
| [Common/Results/Abstractions/](Common/Results/Abstractions/IOperationResult.cs) | Two small interfaces that let Application pipeline behaviours handle any `Result<T>` generically. |
| [TodoItems/TodoItem.cs](TodoItems/TodoItem.cs) | The sample entity. |
| [TodoItems/TodoItemErrors.cs](TodoItems/TodoItemErrors.cs) | Every error the feature can produce, each with a stable code. |

## How an entity is shaped

`TodoItem` shows the pattern:

```csharp
public sealed class TodoItem : AuditableEntity
{
    private TodoItem() { }                                   // for EF Core only

    public string Title { get; private set; } = string.Empty; // no public setters (architecture test)

    public static Result<TodoItem> Create(string title, string? description)
    {
        var errors = Validate(title, description);           // the invariant lives here
        if (errors.Count > 0) return errors;                 // List<Error> → failed Result
        return new TodoItem(Guid.NewGuid(), title.Trim(), Normalize(description));
    }

    public Result<Updated> Complete(DateTimeOffset completedAtUtc) { ... }  // time is passed in
}
```

- **Creation goes through a factory method** that returns `Result<T>`, so an invalid entity cannot exist.
- **State changes are methods** (`Update`, `Complete`, `Reopen`), not property assignments. Each method returns `Result<Updated>`.
- **Expected failures are returned, not thrown.** `Complete` on a completed item returns `TodoItemErrors.AlreadyCompleted`.
- **No clock and no current user.** `Complete` receives the time as a parameter. Audit fields are set by Infrastructure.

## What belongs here and what does not

| Belongs here | Stays outside |
|---|---|
| Entities, value checks, state transitions | EF Core attributes or configuration (Infrastructure) |
| `<Entity>Errors` with stable codes | HTTP status codes (Api) |
| Constants such as `TitleMaxLength` | Queries, `DbContext`, I/O (Application, Infrastructure) |
| `Result<T>` and `Error` | Logging, `TimeProvider`, `IUser`, configuration |

## When you edit it

This layer comes first when you build a feature ([RULES.md § 2](../../RULES.md#2-development-workflow-one-use-case-at-a-time)):

1. add `Domain/<Feature>/<Entity>.cs` deriving from `AuditableEntity`;
2. add `<Entity>Errors.cs`;
3. add tests in `tests/TemplateApp.Domain.UnitTests/<Feature>/` for every rule, both success and failure.
