# Rules: TemplateApp.Application

Read [../../RULES.md](../../RULES.md) first.

## Responsibility

Use cases. Each application operation is one command or query with exactly one handler, coordinating the domain and persistence and returning `Result<T>`. This layer also defines the interfaces it needs from the outside (`Common/Interfaces`) and the cross-cutting pipeline (`Common/Behaviours`).

## Dependencies

| Allowed | Forbidden |
|---|---|
| Domain, MediatR, FluentValidation, `Microsoft.EntityFrameworkCore` (query composition through `IAppDbContext`), logging abstractions | Infrastructure, Api, Contracts, ASP.NET Core, SQL Server provider, `Microsoft.Data.SqlClient`, Redis, HybridCache, Serilog |

Enforced by `LayerDependencyTests.Application_DoesNotDependOnInfrastructureApiContractsOrProviders`.

Exposing `DbSet<T>` through `IAppDbContext` is the original project's persistence approach. Handlers compose LINQ queries directly. Do not add repositories or a generic data-access layer on top of it.

## Code placement

```
Common/
  Behaviours/      PerformanceBehaviour, ValidationBehaviour, CacheInvalidationBehaviour (registration order = execution order)
  Exceptions/      exceptions the outer layers translate (UniqueConstraintViolationException)
  Interfaces/      IAppDbContext, ICacheService, ICachedQuery, IInvalidatesCache, ICommand, IQuery, IUser
  Models/          PaginatedList<T>
Features/<Feature>/
  Commands/<UseCase>/   <UseCase>Command.cs, <UseCase>CommandHandler.cs, <UseCase>CommandValidator.cs
  Queries/<UseCase>/    <UseCase>Query.cs, <UseCase>QueryHandler.cs, <UseCase>QueryValidator.cs
  Dtos/                 <Entity>Dto.cs (sealed records)
  Mappers/              <Entity>Mapper.cs (static: Projection expression + ToDto extension)
  <Feature>Cache.cs     cache tag and lifetime shared by the feature's queries and commands
DependencyInjection.cs  AddApplication()
```

`ConventionTests` enforce:
- the `Commands|Queries/<UseCase>` folders;
- request names ending in `Command` or `Query`;
- exactly one sealed `<Request>Handler` in the same folder;
- sealed `<Request>Validator`s next to their request;
- DTOs in `Dtos/` and mappers in `Mappers/`.

## Conventions

- **Requests** are `sealed record`s implementing `ICommand<TResponse>` or `IQuery<TResponse>`. A command changes state; a query never does.
- **Handlers** implement `ICommandHandler<,>` or `IQueryHandler<,>`, are `sealed`, receive dependencies through the primary constructor, and pass the `CancellationToken` to every call.
- **Validators** check shape (required fields, lengths, ranges) using the entity's constants. Business rules stay in the domain or the handler. `ValidationBehaviour` runs every validator before the handler; no other code calls them.
- **Errors:** return them, never throw them. Use `Result.Failure<T>(other.Errors)` to propagate errors from a domain result. NotFound and conflict errors live in the domain `<Entity>Errors`.
- **Reads:**
  - `AsNoTracking()` plus `.Select(<Entity>Mapper.Projection)`;
  - deterministic ordering for lists;
  - `PaginatedList<T>.CreateAsync` for paging.
- **Caching:** a cacheable query implements `ICachedQuery<TResponse>` and declares `CacheKey`, `Tags` and `Expiration`. Its handler wraps the database read in `cache.GetOrCreateAsync(query, …)`. Cache only DTOs. The key includes every parameter, plus the user or tenant id for user-scoped data.
- **Invalidation:** a command that changes cached data implements `IInvalidatesCache` with the feature's tag. Never call `RemoveByTagAsync` from a handler.
- **Concurrency on unique data:** check first (for a friendly error), and also catch `UniqueConstraintViolationException` around `SaveChangesAsync` for the race.
- **Logging:** log business events with identifiers only (`"Todo item created. Id: {TodoItemId}"`). Never log a request object.
- **Time and user** come from `TimeProvider` and `IUser`. No static clocks or `HttpContext`.

## Testing

`tests/TemplateApp.Application.UnitTests/Features/<Feature>/{Commands|Queries}/<UseCase>/`. Each use case gets handler tests (success, not found, conflict, propagated domain errors) and validator tests. These run against `InMemoryAppDbContext` and `RecordingCache`. Behaviour tests run requests through the real MediatR pipeline (`Behaviours/PipelineTests`).
