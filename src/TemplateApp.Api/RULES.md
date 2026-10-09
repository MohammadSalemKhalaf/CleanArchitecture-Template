# Rules: TemplateApp.Api

Read [../../RULES.md](../../RULES.md) first.

## Responsibility

HTTP transport and the composition root:
- controllers;
- API versioning and OpenAPI;
- authentication and authorization;
- problem-details error mapping;
- exception handling;
- request logging and correlation;
- health endpoints;
- service registration;
- the `--migrate` mode.

## Dependencies

| Allowed | Forbidden |
|---|---|
| Application (send requests, read DTOs), Contracts, Domain result types, ASP.NET Core, Asp.Versioning, Serilog | Controllers using `IAppDbContext`, EF Core or any Infrastructure type |

Infrastructure is referenced only for registration (`AddInfrastructure`), database initialisation and `MigrationMode`. Enforced by `LayerDependencyTests.Controllers_GoThroughUseCasesInsteadOfPersistence`.

## Code placement (as in the original project)

```
Controllers/
  ApiController.cs                 base class: [ApiController] + Problem(IReadOnlyList<Error>), the only error→HTTP mapping
  <Feature>Controller.cs           e.g. TodoItemsController
Extensions/                        endpoint/extension helpers (HealthCheckExtensions)
Infrastructure/                    GlobalExceptionHandler, RequestLogContextMiddleware (correlation id)
OpenApi/Transformers/              OpenAPI document transformers
Services/                          IUser implementation (CurrentUser)
DependencyInjection.cs             AddPresentation(), UseCoreMiddlewares()
IAssemblyMarker.cs                 entry point marker for WebApplicationFactory
MigrationMode.cs, Program.cs
```

## Controller conventions

Enforced by `ConventionTests.Controllers_AreSealedAndDeriveFromApiController`.

- `public sealed class <Feature>Controller(ISender sender) : ApiController`.
- Class attributes: `[Route("api/v{version:apiVersion}/<feature-kebab-case>")]`, `[ApiVersion("1.0")]`, `[Authorize]`.
- One action per use case. The action:
  1. maps route values and the Contracts request to the command or query;
  2. calls `sender.Send(request, ct)`;
  3. returns `result.Match(response => Ok(response), Problem)` (or `CreatedAtRoute` / `NoContent`).
- Every action declares `[ProducesResponseType]` for each status it returns, plus `[EndpointSummary]`, `[EndpointDescription]`, `[EndpointName]` and `[MapToApiVersion]`, so the OpenAPI document is complete.
- No `try/catch`, no business `if`, no logging of request bodies in controllers.
- New status mappings go in `ApiController.StatusCodeFor` only.

## Cross-cutting conventions

- **Middleware order** lives in `UseCoreMiddlewares`: correlation id → Serilog request logging → exception handler → status code pages → authentication → authorization. Do not reorder without a reason in a comment.
- **Problem details** always carry `traceId` and `correlationId`. Unexpected exceptions return a generic 500 with no exception details.
- **Optional features are switched by configuration:**
  - `OpenApi:Enabled` (OpenAPI document and Scalar UI);
  - `Database:InitializeOnStartup` / `Database:SeedSampleData`;
  - `ConnectionStrings:Redis`;
  - the `Serilog:WriteTo:Seq` sink.
- **Authentication** is configured only through `Authentication:Schemes:Bearer`. The API validates tokens and never issues them.

## Testing

`tests/TemplateApp.Api.IntegrationTests`:
- `Controllers/<Feature>ControllerTests.cs`: routing, status codes, problem bodies, Location headers, and full request flows against SQL Server;
- `Infrastructure/PlatformTests.cs`: authentication, health, correlation, and 500 handling.
