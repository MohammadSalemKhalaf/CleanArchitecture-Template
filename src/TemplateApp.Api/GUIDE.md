# Api layer guide

This project is the HTTP entry point and the **composition root**: the one place where every layer is registered and the app is started. Controllers translate HTTP into use cases and results back into HTTP. They do nothing else.

- Rules: [RULES.md](RULES.md)
- The whole picture: [docs/ARCHITECTURE-GUIDE.md](../../docs/ARCHITECTURE-GUIDE.md)

## References

- **Application:** controllers send its commands and queries through MediatR's `ISender`.
- **Contracts:** controllers bind request bodies and query strings to these types.
- **Infrastructure:** for registration only, in `Program.cs`, plus `--migrate` and health checks. Controllers must not use `IAppDbContext`, EF Core or Infrastructure types. `LayerDependencyTests.Controllers_GoThroughUseCasesInsteadOfPersistence` enforces this.
- **Packages:**
  - API versioning;
  - OpenAPI and Scalar;
  - JWT bearer authentication;
  - Serilog with the Seq sink;
  - EF Core Design, which the migration tooling needs.

## Folders and files

| Path | Contents |
|---|---|
| [Program.cs](Program.cs) | Startup. Sets up Serilog, calls `AddPresentation`, `AddApplication` and `AddInfrastructure`, handles `--migrate`, maps OpenAPI when `OpenApi:Enabled` is true, initialises the database, adds middleware, and maps controllers and health endpoints. |
| [DependencyInjection.cs](DependencyInjection.cs) | `AddPresentation()`: problem details, API versioning and OpenAPI, the exception handler, controllers and JSON, and authentication (`IUser` → `CurrentUser`). `UseCoreMiddlewares()`: the middleware order. |
| [Controllers/ApiController.cs](Controllers/ApiController.cs) | Base class for every controller. `Problem(errors)` is the **only** place where `ErrorKind` becomes an HTTP status. |
| [Controllers/TodoItemsController.cs](Controllers/TodoItemsController.cs) | The sample: one action per use case. |
| [Infrastructure/GlobalExceptionHandler.cs](Infrastructure/GlobalExceptionHandler.cs) | Unexpected exceptions become a 500 response with no internal details. |
| [Infrastructure/RequestLogContextMiddleware.cs](Infrastructure/RequestLogContextMiddleware.cs) | Correlation id: taken from `X-Correlation-Id` or the trace id, echoed in the response, and added to every log event. |
| [Extensions/HealthCheckExtensions.cs](Extensions/HealthCheckExtensions.cs) | `/health/live` (process only) and `/health/ready` (database, and Redis when configured). Both allow anonymous access. |
| [OpenApi/Transformers/BearerSecuritySchemeTransformer.cs](OpenApi/Transformers/BearerSecuritySchemeTransformer.cs) | Adds the bearer-token scheme to the OpenAPI document. |
| [Services/CurrentUser.cs](Services/CurrentUser.cs) | `IUser` implementation: the JWT `sub` claim. |
| [MigrationMode.cs](MigrationMode.cs) | `dotnet TemplateApp.Api.dll --migrate` applies migrations and exits. Docker and deployment use it. |
| [appsettings.json](appsettings.json), [appsettings.Development.json](appsettings.Development.json) | Defaults: OpenAPI and database initialisation are off in production and on in Development. Seq is configured for Development. |
| [IAssemblyMarker.cs](IAssemblyMarker.cs) | Lets the integration tests start this app with `WebApplicationFactory`. |

## How an action is written

```csharp
[Route("api/v{version:apiVersion}/todo-items")]
[ApiVersion("1.0")]
[Authorize]                                         // every controller, unless documented as public
public sealed class TodoItemsController(ISender sender) : ApiController
{
    [HttpGet("{todoItemId:guid}", Name = "GetTodoItemById")]
    [ProducesResponseType(typeof(TodoItemDto), StatusCodes.Status200OK)]        // feeds OpenAPI
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid todoItemId, CancellationToken ct)
    {
        var result = await sender.Send(new GetTodoItemByIdQuery(todoItemId), ct);  // HTTP → use case

        return result.Match(response => Ok(response), Problem);                  // Result → HTTP
    }
}
```

## Middleware order

`UseCoreMiddlewares` runs these in order:

1. correlation id;
2. Serilog request logging;
3. exception handler and status code pages;
4. authentication;
5. authorization.

The order matters. For example, the correlation id must exist before the request is logged.

## What belongs here and what does not

| Belongs here | Stays outside |
|---|---|
| Routes, versioning, `[Authorize]`, response attributes | Business rules, queries, `IAppDbContext` |
| Mapping a contract → command or query | A second error-to-status mapping |
| Middleware, authentication, OpenAPI, health endpoints | Secrets in `appsettings*.json` (use user secrets or environment variables) |

## When you edit it

Add one action per new use case in `Controllers/<Feature>Controller.cs`. The class is sealed, derives from `ApiController`, has `[Authorize]`, and its actions return `result.Match(..., Problem)`.

Then add tests in `tests/TemplateApp.Api.IntegrationTests/Controllers/<Feature>ControllerTests.cs`, with at least one success and one failure per endpoint.

Edit `DependencyInjection.cs` or `Program.cs` only for genuinely cross-cutting changes, such as adding CORS or rate limiting.
