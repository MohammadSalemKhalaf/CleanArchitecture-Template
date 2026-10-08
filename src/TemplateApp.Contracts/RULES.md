# Rules: TemplateApp.Contracts

Read [../../RULES.md](../../RULES.md) first.

## Responsibility

The HTTP wire format that clients use: request bodies and shared query models (as in the original project, where a Blazor client referenced the same assembly). Responses reuse the Application DTOs unless a client needs a shape of its own.

## Dependencies

| Allowed | Forbidden |
|---|---|
| .NET base class library only | Every other project and package |

Enforced by `LayerDependencyTests.Contracts_DependOnNothing`. Application never references Contracts: controllers map requests to commands.

## Code placement

```
Common/                    shapes shared by several features (PageRequest)
Requests/<Feature>/        <UseCase>Request.cs, e.g. CreateTodoItemRequest
Responses/<Feature>/       only when a response differs from the Application DTO
```

## Conventions

- Requests are `sealed` classes with public `get; set;` properties and safe defaults (`string.Empty`), as in the original project, so model binding and clients can construct them.
- Name a request after the use case it feeds: `CreateTodoItemRequest` → `CreateTodoItemCommand`.
- Route values (ids) are not repeated in the body.
- Do not put business validation here. Rules live in the Application validators, which return the field errors clients see. DataAnnotations are allowed only for client-side UX and must not contradict the validators.
- Changing a published request shape is a breaking change. Add properties as optional, or introduce a new API version.
