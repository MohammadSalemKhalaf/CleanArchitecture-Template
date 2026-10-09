# Rules: whole solution

These rules apply to every change. Layer-specific rules live next to each project and are indexed in [RULES-COMPOSE.md](RULES-COMPOSE.md). When a layer rule is stricter than a rule here, the layer rule wins.

## 1. Architecture

The solution follows Clean Architecture with feature-based vertical slices inside the Application layer.

```
TemplateApp.Api ──► TemplateApp.Application ──► TemplateApp.Domain
   │      │                ▲
   │      └──► TemplateApp.Contracts (HTTP request shapes, no dependencies)
   └──► TemplateApp.Infrastructure ─┘  (implements Application interfaces)
```

| Project | Responsibility |
|---|---|
| `TemplateApp.Domain` | Entities, invariants, domain errors, `Result<T>`/`Error` |
| `TemplateApp.Application` | Use cases (commands, queries, handlers, validators), DTOs, mappers, pipeline behaviours, interfaces it needs |
| `TemplateApp.Contracts` | HTTP request/response shapes shared with clients |
| `TemplateApp.Infrastructure` | EF Core/SQL Server, migrations, interceptors, caching provider, health checks, option classes |
| `TemplateApp.Api` | Controllers, middleware, authentication, OpenAPI, problem details, composition root |

Allowed dependencies point **inwards only**. Architecture tests fail the build on any violation (see [RULES-COMPOSE.md § Enforcement](RULES-COMPOSE.md#enforcement)).

## 2. Development workflow (one use case at a time)

Every application operation is one use case, built in this order:

1. **Domain:** entity behaviour and errors (`Domain/<Feature>/<Entity>.cs`, `<Entity>Errors.cs`).
2. **Persistence:** `DbSet` on `IAppDbContext` and `AppDbContext`, an `IEntityTypeConfiguration`, then a migration.
3. **Application:** `Features/<Feature>/{Commands|Queries}/<UseCase>/` with the request, its handler and its validator. Add `Dtos/` and `Mappers/` for the feature.
4. **Contracts:** request bodies in `Contracts/Requests/<Feature>/` when the use case takes a body.
5. **Api:** one action in `Controllers/<Feature>Controller.cs` that maps the request to the command or query and the `Result` to HTTP with `Problem`.
6. **Tests:** at the levels listed in [tests/RULES.md](tests/RULES.md).

Infrastructure (DI, behaviours, caching, logging, error mapping) is shared. A new feature must not modify it unless the change is genuinely cross-cutting.

## 3. Clean code

- **Naming:** PascalCase types and members, `_camelCase` private fields, `Async` suffix on async methods that are not handlers or controller actions. Names describe intent (`DuplicateTitle`, not `Error2`).
- **Small, focused types:** one public type per file; the file name matches the type. Handlers do one operation.
- **No speculative abstractions:** add an interface only when there are two implementations or a layer boundary requires it (as with `IAppDbContext`, `ICacheService`, `IUser`). No generic repositories, base handlers or "manager" classes.
- **No duplicated behaviour:** validation lives in Application validators (and domain invariants), error mapping only in `ApiController`, cache invalidation only in `CacheInvalidationBehaviour`, audit stamping only in `AuditableEntityInterceptor`.
- **Expected failures are values, not exceptions.** Return `Error`s through `Result<T>`. Throw only for programming errors and infrastructure failures.
- **Comments explain why, not what.** Delete commented-out code.
- **Immutability by default:** DTOs and requests sent through MediatR are `sealed record`s, entities use private setters, and collections are exposed read-only.
- **Style:** `.editorconfig` and StyleCop run in every build with warnings as errors. Do not suppress a warning without a comment explaining why.

## 4. Separation of concerns and coupling

- **Controllers:** translate HTTP ↔ use case only. No business rules, no data access.
- **Handlers:** orchestrate. Business rules belong in domain methods; queries belong in the handler that needs them.
- **Domain:** no I/O, no framework types, no time source (`TimeProvider`) or user context. Pass values in.
- **Cross-feature references:** a feature must not reference another feature's handlers or DTOs. Share through Domain or `Application/Common` only when two features genuinely need the same thing.
- **Configuration:** read through option classes (`Infrastructure/Settings`) or `IConfiguration` in composition code only, never inside handlers or entities.

## 5. Performance

- Propagate `CancellationToken` through every async call. Never block on async code (`.Result`, `.Wait()` are banned by analyzer in `src/`).
- Read queries use `AsNoTracking()` and project to DTOs with the feature mapper's `Projection` expression. Do not load entities to map them in memory.
- List endpoints are paginated with `PaginatedList<T>` (page size ≤ 100) and have a deterministic order.
- Cache read queries that are hot and tolerant of 30 s staleness by implementing `ICachedQuery`. Every command that changes their data implements `IInvalidatesCache` with the same tag.
- Cache keys include every parameter that changes the result, plus the user or tenant id for user-scoped data.
- Add indexes in the entity configuration for columns used in filters, ordering or uniqueness.
- Use `TimeProvider`, never `DateTime.Now`/`UtcNow` (banned by analyzer).

## 6. Security and logging

- Never log request bodies, tokens, passwords, connection strings or personal data. Log identifiers (`{TodoItemId}`), not objects.
- Use structured logging (`logger.LogInformation("… {Id}", id)`), never string interpolation in log messages, and never `Console.WriteLine` (banned).
- Secrets come from user secrets, environment variables or the server `.env`. Never from `appsettings*.json` or source.
- Every controller is `[Authorize]` unless explicitly public, and the reason is written next to the attribute.

## 7. Testing (summary)

Every use case ships with tests. See [tests/RULES.md](tests/RULES.md) for what to test where. CI runs the full suite; architecture tests run first.

## 8. Definition of done

- [ ] Code is in the folder its layer rules prescribe; architecture tests pass.
- [ ] `dotnet build` passes with zero warnings.
- [ ] `dotnet test` passes (Docker running for integration tests).
- [ ] A migration exists for every model change (`Migrations_MatchTheCurrentModel` passes).
- [ ] README or rules are updated when a convention changes.
