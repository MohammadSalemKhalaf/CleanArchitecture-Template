# Rules: TemplateApp.Domain

Read [../../RULES.md](../../RULES.md) first.

## Responsibility

The business model: entities, their invariants and state transitions, and the errors they can produce. It also owns the result types (`Common/Results`) that every other layer uses to express expected failures.

## Dependencies

| Allowed | Forbidden |
|---|---|
| .NET base class library only | Every other project, MediatR, FluentValidation, EF Core, ASP.NET Core, `Microsoft.Extensions.*`, any NuGet package |

Enforced by `LayerDependencyTests.Domain_DependsOnNoOtherLayerOrFramework`, which also fails on unused package or project references.

## Code placement

```
Common/
  Entity.cs, AuditableEntity.cs        base types (identity, audit columns)
  Results/Result.cs, Error.cs, ErrorKind.cs
  Results/Abstractions/                IOperationResult, IErrorResultFactory (used by pipeline code)
<Feature>/                             one folder per aggregate, plural (e.g. TodoItems/)
  <Entity>.cs                          the entity
  <Entity>Errors.cs                    every Error the feature can produce, including NotFound and conflicts
```

## Conventions

- **Construction:** entities are created through a static `Create(...)` that returns `Result<TEntity>`. The constructor is private, plus a parameterless private constructor for EF Core.
- **State changes:** go through methods that return `Result<Updated>` (or another marker) and validate first. All validation errors are reported together (see `TodoItem.Validate`).
- **No public setters** (enforced by `ConventionTests.DomainEntities_DoNotExposePublicSetters`). Audit columns are set by the persistence interceptor.
- **Errors:** `Error.Validation(code: <PropertyName>, …)` for invalid input, `Error.Conflict` for rule or state violations, `Error.NotFound` for missing aggregates. Codes are stable strings (`TodoItem.DuplicateTitle`) because clients may depend on them.
- **Limits** (max lengths and similar) are `public const` on the entity, so validators and EF configurations reuse them instead of repeating numbers.
- **Time and identity are parameters.** A method that needs "now" receives a `DateTimeOffset`; the caller gets it from `TimeProvider`.
- **No I/O, logging or async code** in the domain.

## Testing

`tests/TemplateApp.Domain.UnitTests/<Feature>/<Entity>Tests.cs`: one test per invariant and transition, success and failure. Use `Tests.Common` factories for valid instances.
