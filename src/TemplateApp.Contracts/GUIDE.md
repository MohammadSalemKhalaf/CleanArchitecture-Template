# Contracts layer guide

This project defines the shapes clients send to the API: JSON request bodies and query-string objects. It is small on purpose.

- Rules: [RULES.md](RULES.md)
- The whole picture: [docs/ARCHITECTURE-GUIDE.md](../../docs/ARCHITECTURE-GUIDE.md)

## References

None ([TemplateApp.Contracts.csproj](TemplateApp.Contracts.csproj)). With no dependencies, the request shapes could be shared with a client, such as a .NET SDK or tests, without dragging in server code. `LayerDependencyTests.Contracts_DependOnNothing` enforces it.

The Api references Contracts. Application does not: it has its own commands and queries, so the HTTP shape and the use-case shape can change independently.

## Folders and files

| Path | Contents |
|---|---|
| [Requests/TodoItems/CreateTodoItemRequest.cs](Requests/TodoItems/CreateTodoItemRequest.cs) | Body of `POST /api/v1/todo-items`. |
| [Requests/TodoItems/UpdateTodoItemRequest.cs](Requests/TodoItems/UpdateTodoItemRequest.cs) | Body of `PUT /api/v1/todo-items/{todoItemId}`. The id comes from the route, not the body. |
| [Common/PageRequest.cs](Common/PageRequest.cs) | `?page=&pageSize=`, bound with `[FromQuery]`. |

## How it is used

The controller receives the contract and copies it into a command:

```csharp
public async Task<IActionResult> Create([FromBody] CreateTodoItemRequest request, CancellationToken ct)
{
    var result = await sender.Send(new CreateTodoItemCommand(request.Title, request.Description), ct);
    ...
}
```

## What belongs here and what does not

| Belongs here | Stays outside |
|---|---|
| Plain classes or records with properties | Validation rules (Application validators) |
| Defaults such as `Page = 1` | Logic, attributes from other libraries, references to Domain |
| | Response DTOs: responses currently return Application DTOs such as `TodoItemDto` |

## When you edit it

Add `Requests/<Feature>/<UseCase>Request.cs` when a new endpoint takes a body or new query parameters. If the endpoint takes only route values, nothing is needed here.
