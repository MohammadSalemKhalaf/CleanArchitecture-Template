# Application layer guide

This project contains the use cases: everything the application can do, each as one command or query with its own handler and validator. It decides **what** happens. Infrastructure decides **how** data is stored, and the Api decides how it is exposed over HTTP.

- Rules: [RULES.md](RULES.md)
- The whole picture: [docs/ARCHITECTURE-GUIDE.md](../../docs/ARCHITECTURE-GUIDE.md)

## References

- **Domain:** use cases create and change entities, and return `Result<T>`.
- **MediatR:** sends each request to its handler, through the pipeline behaviours.
- **FluentValidation:** validators.
- **Microsoft.EntityFrameworkCore:** used for `DbSet<T>` and LINQ query composition only, never for a database provider. This is a deliberate tradeoff, explained in [the central guide](../../docs/ARCHITECTURE-GUIDE.md#8-deliberate-tradeoffs).

It must **not** reference Infrastructure, Api or Contracts. Architecture tests enforce this.

## Folders and files

| Path | Contents |
|---|---|
| [DependencyInjection.cs](DependencyInjection.cs) | `AddApplication()`: scans this assembly for handlers and validators, and registers the behaviours in order. |
| [Common/Interfaces/](Common/Interfaces/IAppDbContext.cs) | Contracts this layer needs from outside: `IAppDbContext`, `ICacheService`, `IUser`. It also defines the request markers `ICommand<T>`, `IQuery<T>`, `ICachedQuery<T>` and `IInvalidatesCache`. |
| [Common/Behaviours/](Common/Behaviours/ValidationBehaviour.cs) | `PerformanceBehaviour` (warns above 500 ms), `ValidationBehaviour` (runs validators and returns errors before the handler), and `CacheInvalidationBehaviour` (removes cache tags after a successful command). |
| [Common/Models/PaginatedList.cs](Common/Models/PaginatedList.cs) | Paging for list queries, with a maximum page size of 100. |
| [Common/Exceptions/](Common/Exceptions/UniqueConstraintViolationException.cs) | `UniqueConstraintViolationException`, thrown by Infrastructure when a unique index rejects a write. |
| `Features/<Feature>/Commands/<UseCase>/` | One request, one handler, one validator. |
| `Features/<Feature>/Queries/<UseCase>/` | Same shape, for reads. |
| `Features/<Feature>/Dtos/` and `Mappers/` | Data returned to clients, and entity-to-DTO mapping. |
| [Features/TodoItems/TodoItemCache.cs](Features/TodoItems/TodoItemCache.cs) | The feature's cache tag and lifetime. |

## How a use case is built

**Command** ([CreateTodoItem](Features/TodoItems/Commands/CreateTodoItem/CreateTodoItemCommand.cs)):

```csharp
public sealed record CreateTodoItemCommand(string Title, string? Description)
    : ICommand<TodoItemDto>, IInvalidatesCache            // returns Result<TodoItemDto>
{
    public string[] CacheTags => [TodoItemCache.Tag];     // removed after success
}
```

**Query** ([GetTodoItemById](Features/TodoItems/Queries/GetTodoItemById/GetTodoItemByIdQueryHandler.cs)):

- reads with `AsNoTracking()`;
- projects straight into the DTO with `TodoItemMapper.Projection`;
- reads through `ICacheService.GetOrCreateAsync(query, ...)`, using the key, tags and lifetime the query declares.

**Handler rules:**

- Handlers orchestrate: they load data, call domain methods, save, and return a `Result`.
- Business rules stay in the entity.
- Return `Error`s for expected failures, such as `TodoItemErrors.NotFound(id)`. Do not throw for them.
- Never call another feature's handler.

**Validators** check the request's shape, such as required fields and lengths. They reuse the Domain's constants (`TodoItem.TitleMaxLength`) so the limits cannot drift apart.

## What belongs here and what does not

| Belongs here | Stays outside |
|---|---|
| Commands, queries, handlers, validators | HTTP: status codes, `IActionResult`, routes (Api) |
| DTOs and mappers | SQL Server, Redis, `HybridCache` (Infrastructure) |
| Interfaces for outside services | Their implementations (Infrastructure or Api) |
| Cache keys and tags on requests | Manual cache invalidation inside handlers |
| | Reading `IConfiguration` |

## When you edit it

For every new operation:

1. add `IAppDbContext.<Entities>` when the feature has a new entity;
2. create the `Commands/` or `Queries/` use-case folder;
3. add `Dtos/` and `Mappers/` for a new feature;
4. add tests in `tests/TemplateApp.Application.UnitTests/Features/<Feature>/...` for the handler outcomes and the validator rules.

Registration is automatic. Change `Common/` only for something genuinely shared by several features.
